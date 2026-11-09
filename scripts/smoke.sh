#!/usr/bin/env bash
# Smoke test dos endpoints do README. Requer a API rodando (dotnet run --project src/UrlShortener.Api).
# Uso: ./scripts/smoke.sh [base_url]   (padrão http://localhost:5000)
set -u
BASE="${1:-http://localhost:5000}"
JSON='content-type: application/json'
fail=0

check() { # check <descricao> <esperado> <obtido>
  if [ "$2" = "$3" ]; then printf '  ok   %-55s %s\n' "$1" "$3"
  else printf '  FAIL %-55s esperado %s, obtido %s\n' "$1" "$2" "$3"; fail=1; fi
}
status() { curl -s -o /dev/null -w '%{http_code}' "$@"; }

echo "== POST /api/shorten"
BODY=$(curl -s -X POST "$BASE/api/shorten" -H "$JSON" -d '{"url":"https://jusbrasil.com.br"}')
echo "  $BODY"
CODE=$(echo "$BODY" | sed -E 's/.*"shortCode":"([^"]+)".*/\1/')
check "201 ao criar"              201 "$(status -X POST "$BASE/api/shorten" -H "$JSON" -d '{"url":"https://example.com"}')"
check "shortCode com 7 alfanum."  7   "${#CODE}"
check "400 para ftp://"           400 "$(status -X POST "$BASE/api/shorten" -H "$JSON" -d '{"url":"ftp://example.com"}')"
check "400 para texto solto"      400 "$(status -X POST "$BASE/api/shorten" -H "$JSON" -d '{"url":"nao e url"}')"
check "400 sem url"               400 "$(status -X POST "$BASE/api/shorten" -H "$JSON" -d '{}')"

echo "== GET /{code} (redirect + tracking)"
check "301 no redirect"           301 "$(status -e 'https://google.com/search?q=x' -A 'SmokeTest/1.0' "$BASE/$CODE")"
check "301 segundo clique"        301 "$(status -e 'https://twitter.com/' "$BASE/$CODE")"
check "301 clique direto"         301 "$(status "$BASE/$CODE")"
check "Location = URL original"   "https://jusbrasil.com.br" "$(curl -s -D - -o /dev/null "$BASE/$CODE" | tr -d '\r' | awk 'tolower($1)=="location:"{print $2}')"
check "404 código inexistente"    404 "$(status "$BASE/zzzzzzz")"

echo "== expiração"
PAST=$(date -u -d '-1 minute' +%Y-%m-%dT%H:%M:%SZ 2>/dev/null || date -u -v-1M +%Y-%m-%dT%H:%M:%SZ)
EXP=$(curl -s -X POST "$BASE/api/shorten" -H "$JSON" -d "{\"url\":\"https://example.com\",\"expiresAt\":\"$PAST\"}" | sed -E 's/.*"shortCode":"([^"]+)".*/\1/')
check "410 para link expirado"    410 "$(status "$BASE/$EXP")"

echo "== GET /api/urls"
URLS=$(curl -s "$BASE/api/urls")
check "200 na listagem"           200 "$(status "$BASE/api/urls")"
check "lista contém o código"     yes "$(echo "$URLS" | grep -q "\"shortCode\":\"$CODE\"" && echo yes || echo no)"
check "contagem de cliques = 4"   yes "$(echo "$URLS" | grep -q "\"shortCode\":\"$CODE\"[^}]*\"clickCount\":4" && echo yes || echo no)"

echo "== GET /api/stats/{code}"
STATS=$(curl -s "$BASE/api/stats/$CODE")
echo "  ${STATS:0:200}..."
check "200 nas stats"             200 "$(status "$BASE/api/stats/$CODE")"
check "totalClicks = 4"           yes "$(echo "$STATS" | grep -q '"totalClicks":4' && echo yes || echo no)"
check "clicksByDay com 30 dias"   30  "$(echo "$STATS" | grep -o '"day":' | wc -l | tr -d ' ')"
check "topReferrers tem google"   yes "$(echo "$STATS" | grep -q '"referrer":"google.com","count":1' && echo yes || echo no)"
check "topReferrers direct = 2"   yes "$(echo "$STATS" | grep -q '"referrer":"direct","count":2' && echo yes || echo no)"
check "404 stats inexistente"     404 "$(status "$BASE/api/stats/zzzzzzz")"

echo "== GET /analytics/{code}"
check "200 na página"             200 "$(status "$BASE/analytics/$CODE")"
check "content-type text/html"    yes "$(curl -s -o /dev/null -w '%{content_type}' "$BASE/analytics/$CODE" | grep -q '^text/html' && echo yes || echo no)"
check "404 página inexistente"    404 "$(status "$BASE/analytics/zzzzzzz")"

echo
echo "Abra no browser: $BASE/analytics/$CODE"
[ $fail -eq 0 ] && echo "SMOKE OK" || { echo "SMOKE FALHOU"; exit 1; }
