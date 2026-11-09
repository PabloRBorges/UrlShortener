using System.Net;
using UrlShortener.Api.Domain;

namespace UrlShortener.Api.Pages;

/// <summary>Server-rendered shell; the numbers are fetched from /api/stats/{code} by the page itself.</summary>
public static class AnalyticsPage
{
    public static string Render(ShortLink link, string shortUrl)
    {
        var code = WebUtility.HtmlEncode(link.ShortCode);
        var original = WebUtility.HtmlEncode(link.OriginalUrl);
        var shortUrlEncoded = WebUtility.HtmlEncode(shortUrl);
        var expires = link.ExpiresAt is { } e ? $"expira em {e:yyyy-MM-dd HH:mm} UTC" : "sem expiração";

        return $$"""
<!doctype html>
<html lang="pt-BR">
<head>
<meta charset="utf-8">
<meta name="viewport" content="width=device-width, initial-scale=1">
<title>Analytics · {{code}}</title>
<script src="https://cdn.jsdelivr.net/npm/chart.js@4.4.1/dist/chart.umd.min.js"></script>
<style>
  :root { color-scheme: light dark; --fg: #1a1a1a; --muted: #666; --bg: #fafafa; --card: #fff; --line: #e5e5e5; --accent: #2563eb; }
  @media (prefers-color-scheme: dark) { :root { --fg: #eee; --muted: #aaa; --bg: #111; --card: #1c1c1c; --line: #333; } }
  * { box-sizing: border-box; }
  body { margin: 0; padding: 24px 16px; font: 15px/1.5 system-ui, sans-serif; color: var(--fg); background: var(--bg); }
  main { max-width: 860px; margin: 0 auto; }
  h1 { font-size: 22px; margin: 0 0 4px; }
  .sub { color: var(--muted); word-break: break-all; margin: 0 0 20px; }
  .sub a { color: var(--accent); }
  .grid { display: grid; gap: 16px; grid-template-columns: 1fr; }
  @media (min-width: 640px) { .grid { grid-template-columns: 1fr 2fr; } .wide { grid-column: 1 / -1; } }
  .card { background: var(--card); border: 1px solid var(--line); border-radius: 10px; padding: 16px; }
  .card h2 { font-size: 13px; text-transform: uppercase; letter-spacing: .04em; color: var(--muted); margin: 0 0 8px; }
  .big { font-size: 40px; font-weight: 700; line-height: 1; }
  table { width: 100%; border-collapse: collapse; }
  td { padding: 6px 0; border-top: 1px solid var(--line); }
  td:last-child { text-align: right; font-variant-numeric: tabular-nums; }
  .empty { color: var(--muted); }
</style>
</head>
<body>
<main>
  <h1>Analytics · <code>{{code}}</code></h1>
  <p class="sub"><a href="{{shortUrlEncoded}}">{{shortUrlEncoded}}</a> → {{original}} · {{expires}}</p>

  <div class="grid">
    <section class="card">
      <h2>Total de cliques</h2>
      <div class="big" id="total">…</div>
    </section>
    <section class="card">
      <h2>Top referrers</h2>
      <table id="referrers"><tbody></tbody></table>
    </section>
    <section class="card wide">
      <h2>Cliques por dia (últimos 30 dias)</h2>
      <canvas id="chart" height="110"></canvas>
    </section>
  </div>
</main>
<script>
(async () => {
  const res = await fetch('/api/stats/{{code}}');
  if (!res.ok) { document.getElementById('total').textContent = 'erro'; return; }
  const s = await res.json();

  document.getElementById('total').textContent = s.totalClicks.toLocaleString('pt-BR');

  const tbody = document.querySelector('#referrers tbody');
  if (s.topReferrers.length === 0) {
    tbody.innerHTML = '<tr><td class="empty">Nenhum clique ainda</td></tr>';
  } else {
    for (const r of s.topReferrers) {
      const tr = document.createElement('tr');
      const name = document.createElement('td'); name.textContent = r.referrer;
      const count = document.createElement('td'); count.textContent = r.count;
      tr.append(name, count); tbody.append(tr);
    }
  }

  new Chart(document.getElementById('chart'), {
    type: 'bar',
    data: {
      labels: s.clicksByDay.map(d => d.day.slice(5)),
      datasets: [{ label: 'Cliques', data: s.clicksByDay.map(d => d.count), backgroundColor: '#2563eb' }]
    },
    options: {
      plugins: { legend: { display: false } },
      scales: { y: { beginAtZero: true, ticks: { precision: 0 } } }
    }
  });
})();
</script>
</body>
</html>
""";
    }
}
