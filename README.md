# URL Shortener

Serviço de encurtamento de URLs com redirect, tracking de cliques e página de analytics compartilhável.

## Stack

| Camada | Escolha | Por quê |
|---|---|---|
| Runtime | .NET 9 (C#) | Tipagem forte nos contratos da API e `dotnet run` sem build separado. Compila com SDK 9 ou 10 (precisa do runtime 9 instalado) |
| HTTP | ASP.NET Core Minimal APIs | Rotas enxutas, model binding e validação de URL com `Results.BadRequest` sem boilerplate de controllers |
| Persistência | Em memória (`ConcurrentDictionary` atrás de `IUrlRepository`) | Zero dependência externa e zero setup para um projeto de uma hora. Agregações (cliques por dia, top referrers) feitas com LINQ. A interface isola o domínio: trocar por SQLite/Postgres depois é implementar uma classe. Trade-off: os dados se perdem ao reiniciar o processo |
| Analytics | HTML servido pela API + Chart.js via CDN | Página acessível por URL pública, consome `/api/stats/{code}` no browser, sem front separado nem Razor |
| Testes | xUnit + `WebApplicationFactory` | Testes de integração sem abrir porta, um comando. O repositório em memória dispensa mocks de banco |

## Setup e execução

```bash
dotnet restore
dotnet run --project src/UrlShortener.Api     # http://localhost:5000
dotnet test                                   # roda unitários + integração
```

Configuração em `appsettings.json` ou variável de ambiente: `BaseUrl=http://localhost:5000` (sem ela, usa o host da requisição). Não há banco: os dados vivem na memória do processo e são descartados ao reiniciar.

Porta ocupada (`Normalmente é permitida apenas uma utilização de cada endereço de soquete`)? Há uma instância anterior rodando. Encerre com `taskkill /F /IM UrlShortener.Api.exe` ou suba em outra porta: `dotnet run --project src/UrlShortener.Api --urls http://localhost:5001`.

## Endpoints

| Método | Rota | Descrição |
|---|---|---|
| `POST` | `/api/shorten` | Body `{ url, expiresAt? }`. Retorna `201` com `{ shortCode, shortUrl, originalUrl, createdAt, expiresAt }`. `400` se URL inválida |
| `GET` | `/{code}` | Redirect `301` para a URL original e registra clique. `404` se não existe, `410` se expirou |
| `GET` | `/api/urls` | Lista links (mais recentes primeiro) com `clickCount` |
| `GET` | `/api/stats/{code}` | `{ totalClicks, clicksByDay (últimos 30 dias), topReferrers }` |
| `GET` | `/analytics/{code}` | Página visual com total, gráfico por dia e top referrers |

## Regras de negócio

- Short code com 7 caracteres alfanuméricos (`[a-zA-Z0-9]`), gerado com `RandomNumberGenerator` e retry em colisão.
- Apenas `http://` e `https://` são aceitos (`Uri.TryCreate` + checagem de scheme); qualquer outro retorna `400`.
- `expiresAt` é opcional (ISO 8601, UTC). Se definido e já passou, o redirect responde `410`.
- Cada acesso a `/{code}` grava `referrer`, `user-agent`, `ip` e `timestamp`.
- `topReferrers` agrupa por origem e devolve os 10 maiores; acessos sem referrer aparecem como `direct`.

## Demo rápido

Com a API no ar, `./scripts/smoke.sh` (Bash) ou `.\scripts\smoke.ps1` (PowerShell) exercita todos os endpoints e status codes acima e imprime a URL da página de analytics. Passo a passo manual:

```bash
H='content-type: application/json'
curl -X POST localhost:5000/api/shorten -H "$H" -d '{"url":"https://jusbrasil.com.br"}'        # 201 → anote o shortCode
curl -X POST localhost:5000/api/shorten -H "$H" -d '{"url":"ftp://x"}'                         # 400
curl -i -e https://google.com/ localhost:5000/<shortCode>                                       # 301 + Location, registra clique
curl -i localhost:5000/zzzzzzz                                                                  # 404
curl -X POST localhost:5000/api/shorten -H "$H" -d '{"url":"https://a.com","expiresAt":"2020-01-01T00:00:00Z"}'
curl -i localhost:5000/<codigoExpirado>                                                         # 410
curl localhost:5000/api/urls                                                                    # lista com clickCount
curl localhost:5000/api/stats/<shortCode>                                                       # totalClicks, clicksByDay, topReferrers
start http://localhost:5000/analytics/<shortCode>                                               # página visual
```

## Estrutura

```
UrlShortener.sln
src/UrlShortener.Api/
  Program.cs           # bootstrap Minimal API + DI
  Endpoints/           # /api/shorten, /api/urls, /api/stats, /{code}, /analytics/{code}
  Contracts/           # DTOs de request/response
  Pages/               # AnalyticsPage (HTML + Chart.js)
  Domain/              # ShortLink, Click, LinkStats, results
  Data/                # IUrlRepository, InMemoryUrlRepository
  Services/            # UrlShortenerService, LinkAnalyticsService, gerador, validador, clock
tests/UrlShortener.Tests/
  Unit/                # serviços, gerador, validador (23 testes)
  Integration/         # endpoints via WebApplicationFactory (15 testes)
  Fakes/               # FakeClock, SequenceShortCodeGenerator
```
