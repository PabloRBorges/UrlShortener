# Smoke test dos endpoints do README. Requer a API rodando (dotnet run --project src/UrlShortener.Api).
# Uso: .\scripts\smoke.ps1 [-Base http://localhost:5000]
param([string]$Base = "http://localhost:5000")
$ErrorActionPreference = "Continue"
$curl = "curl.exe"
$script:fail = $false

function Check($desc, $expected, $actual) {
  if ("$expected" -eq "$actual") { "  ok   {0,-55} {1}" -f $desc, $actual }
  else { "  FAIL {0,-55} esperado {1}, obtido {2}" -f $desc, $expected, $actual; $script:fail = $true }
}
function Status { (& $curl -s -o NUL -w '%{http_code}' @args) -join '' }
# PowerShell 5.1 remove aspas internas ao passar JSON para curl.exe; por isso o body vai via arquivo.
function PostStatus($json) {
  $tmp = [IO.Path]::GetTempFileName(); Set-Content -Path $tmp -Value $json -Encoding Ascii -NoNewline
  try { Status -X POST "$Base/api/shorten" -H 'content-type: application/json' --data-binary "@$tmp" } finally { Remove-Item $tmp }
}
function Post($json) {
  $tmp = [IO.Path]::GetTempFileName(); Set-Content -Path $tmp -Value $json -Encoding Ascii -NoNewline
  try { (& $curl -s -X POST "$Base/api/shorten" -H 'content-type: application/json' --data-binary "@$tmp") -join '' } finally { Remove-Item $tmp }
}

"== POST /api/shorten"
$body = Post '{"url":"https://jusbrasil.com.br"}'
"  $body"
$code = ($body | ConvertFrom-Json).shortCode
Check "201 ao criar"             201 (PostStatus '{"url":"https://example.com"}')
Check "shortCode com 7 alfanum." 7   $code.Length
Check "400 para ftp://"          400 (PostStatus '{"url":"ftp://example.com"}')
Check "400 para texto solto"     400 (PostStatus '{"url":"nao e url"}')
Check "400 sem url"              400 (PostStatus '{}')

"== GET /{code} (redirect + tracking)"
Check "301 no redirect"          301 (Status -e 'https://google.com/search?q=x' -A 'SmokeTest/1.0' "$Base/$code")
Check "301 segundo clique"       301 (Status -e 'https://twitter.com/' "$Base/$code")
Check "301 clique direto"        301 (Status "$Base/$code")
$location = (& $curl -s -D - -o NUL "$Base/$code") | Where-Object { $_ -match '^location:\s*(.+)$' } | ForEach-Object { $Matches[1].Trim() }
Check "Location = URL original"  "https://jusbrasil.com.br" $location
Check "404 codigo inexistente"   404 (Status "$Base/zzzzzzz")

"== expiracao"
$past = (Get-Date).ToUniversalTime().AddMinutes(-1).ToString("yyyy-MM-ddTHH:mm:ssZ")
$exp = (Post ('{"url":"https://example.com","expiresAt":"' + $past + '"}') | ConvertFrom-Json).shortCode
Check "410 para link expirado"   410 (Status "$Base/$exp")

"== GET /api/urls"
$urls = ((& $curl -s "$Base/api/urls") -join '') | ConvertFrom-Json
$mine = $urls | Where-Object shortCode -eq $code
Check "200 na listagem"          200 (Status "$Base/api/urls")
Check "lista contem o codigo"    $true ($null -ne $mine)
Check "contagem de cliques = 4"  4 $mine.clickCount
Check "mais recente primeiro"    $exp $urls[0].shortCode

"== GET /api/stats/{code}"
$stats = ((& $curl -s "$Base/api/stats/$code") -join '') | ConvertFrom-Json
Check "200 nas stats"            200 (Status "$Base/api/stats/$code")
Check "totalClicks = 4"          4  $stats.totalClicks
Check "clicksByDay com 30 dias"  30 $stats.clicksByDay.Count
Check "topReferrers tem google"  1  ($stats.topReferrers | Where-Object referrer -eq 'google.com').count
Check "topReferrers direct = 2"  2  ($stats.topReferrers | Where-Object referrer -eq 'direct').count
Check "404 stats inexistente"    404 (Status "$Base/api/stats/zzzzzzz")

"== GET /analytics/{code}"
Check "200 na pagina"            200 (Status "$Base/analytics/$code")
$ctype = (& $curl -s -o NUL -w '%{content_type}' "$Base/analytics/$code") -join ''
Check "content-type text/html"   $true ($ctype -like 'text/html*')
Check "404 pagina inexistente"   404 (Status "$Base/analytics/zzzzzzz")

""
"Abra no browser: $Base/analytics/$code"
if ($script:fail) { "SMOKE FALHOU"; exit 1 } else { "SMOKE OK" }
