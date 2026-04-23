# Solution build, merged TRX (all test DLLs), one HTML report.
# Run from repo root:  .\scripts\Export-TestReport.ps1
# Open browser:        .\scripts\Export-TestReport.ps1 -Open

param([switch] $Open)

$ErrorActionPreference = "Stop"
$repo = Split-Path -Parent $PSScriptRoot
Set-Location $repo

$out = Join-Path $repo "TestReport"
New-Item -ItemType Directory -Force -Path $out | Out-Null
$trx = Join-Path $out "test-results.trx"
$html = Join-Path $out "index.html"

Write-Host "Building Release..." -ForegroundColor Cyan
dotnet build (Join-Path $repo "NewsAggregator.sln") -c Release -v q | Out-Null

$dlls = @(
  "tests\NewsAggregator.UnitTests\bin\Release\net8.0\NewsAggregator.UnitTests.dll"
  "tests\NewsAggregator.DatabaseTests\bin\Release\net8.0\NewsAggregator.DatabaseTests.dll"
  "tests\NewsAggregator.IntegrationTests\bin\Release\net8.0\NewsAggregator.IntegrationTests.dll"
) | ForEach-Object { Join-Path $repo $_ }

foreach ($d in $dlls) { if (-not (Test-Path $d)) { throw "Missing DLL (build the solution first): $d" } }

# One vstest run, one TRX (not overwritten per project like "dotnet test" on sln).
Write-Host "Tests (merged TRX)..." -ForegroundColor Cyan
# Use absolute path for .trx; relative paths can land under TestResults\
$logger = "trx;LogFileName=$trx"
$vstestCode = 0
& dotnet vstest @dlls "/Logger:$logger"
if ($LASTEXITCODE -ne 0) { $vstestCode = $LASTEXITCODE }
if (-not (Test-Path $trx)) { throw "TRX not found: $trx. See console for 'Results File' path and fix Logger in script." }

$xml = New-Object System.Xml.XmlDocument
$xml.PreserveWhitespace = $false
$xml.Load($trx)
$ns = "http://microsoft.com/schemas/VisualStudio/TeamTest/2010"
$nsm = New-Object System.Xml.XmlNamespaceManager($xml.NameTable)
$nsm.AddNamespace("t", $ns)

$nodes = $xml.SelectNodes("//t:UnitTestResult", $nsm)
$counters = $xml.SelectSingleNode("//t:ResultSummary/t:Counters", $nsm)
$times = $xml.SelectSingleNode("//t:TestRun/t:Times", $nsm)
$summary = if ($counters) {
  [pscustomobject]@{
    total   = $counters.GetAttribute("total")
    passed  = $counters.GetAttribute("passed")
    failed  = $counters.GetAttribute("failed")
    skipped = $counters.GetAttribute("notExecuted")
  }
} else { $null }

$rows = foreach ($n in $nodes) {
  $name = $n.GetAttribute("testName")
  $outc = $n.GetAttribute("outcome")
  $dur = $n.GetAttribute("duration")
  if ($name -match "^NewsAggregator\.(Unit|Database|Integration)Tests") {
    $g = $Matches[1] + "Tests"
  } else { $g = "Інше" }
  [pscustomobject]@{
    group = $g
    name  = $name
    outc  = $outc
    dur   = $dur
  }
}
$byGroup = $rows | Group-Object -Property group | Sort-Object Name

function Get-Badge($outcome) {
  if ($outcome -eq "Passed") { "badge ok" }
  elseif ($outcome -eq "Failed") { "badge fail" }
  else { "badge skip" }
}

# HTML escape
$esc = { param($s) if ($null -eq $s) { "" } else { [System.Net.WebUtility]::HtmlEncode($s) } }

$rowsHtml = ($rows | ForEach-Object {
  $b = (Get-Badge $_.outc)
  "      <tr><td class='group'>{0}</td><td class='name'>{1}</td><td class='d'>{2}</td><td><span class='{3}'>{4}</span></td></tr>" -f `
    (& $esc $_.group), (& $esc $_.name), (& $esc $_.dur), $b, (& $esc $_.outc)
}) -join "`n"

$gHtml = ($byGroup | ForEach-Object {
  "      <div class='g'><b>{0}</b> <span class='c'>({1} тест.)</span></div>" -f $_.Name, $_.Count
}) -join "`n"

$when = (Get-Date -Format "yyyy-MM-dd HH:mm")
$timeStr = if ($times) { $times.GetAttribute("start") } else { "" }
$ts = (Get-Date -Format o)

$template = @"
<!DOCTYPE html>
<html lang="uk">
<head>
  <meta charset="utf-8" />
  <title>Результати тестів — $when</title>
  <style>
    :root { --bg: #0f1419; --card: #1a2129; --text: #e7e9ea; --muted: #8b98a5; --ok: #1d9bf0; --okbg: #061f35; --fail: #f4212e; --failbg: #2a1211; }
    * { box-sizing: border-box; }
    body { font-family: ui-sans-serif, system-ui, "Segoe UI", Roboto, sans-serif; background: var(--bg); color: var(--text); margin: 0; padding: 1.5rem; line-height: 1.45; }
    h1 { font-size: 1.25rem; font-weight: 600; margin: 0 0 0.5rem; }
    .meta { color: var(--muted); font-size: 0.875rem; margin-bottom: 1.25rem; }
    .cards { display: flex; flex-wrap: wrap; gap: 0.75rem; margin-bottom: 1.5rem; }
    .kpi { background: var(--card); border-radius: 8px; padding: 0.9rem 1.1rem; min-width: 5rem; }
    .kpi .n { font-size: 1.4rem; font-weight: 700; }
    .kpi .l { color: var(--muted); font-size: 0.8rem; text-transform: uppercase; letter-spacing: 0.04em; }
    .kpi.passed .n { color: #17bf63; }
    .kpi.failed .n { color: var(--fail); }
    .grows { background: var(--card); border-radius: 8px; padding: 0.9rem 1.1rem; margin-bottom: 1rem; }
    .g { margin: 0.2rem 0; }
    .c { color: var(--muted); }
    table { width: 100%; border-collapse: collapse; background: var(--card); border-radius: 8px; overflow: hidden; font-size: 0.875rem; }
    th { text-align: left; padding: 0.6rem 0.75rem; color: var(--muted); font-weight: 500; background: #141a21; }
    td { padding: 0.45rem 0.75rem; border-top: 1px solid #242d38; }
    .group { color: #8899a6; white-space: nowrap; }
    .name { word-break: break-all; }
    .d { color: var(--muted); font-variant-numeric: tabular-nums; }
    .badge { display: inline-block; padding: 0.15rem 0.4rem; border-radius: 4px; font-size: 0.8rem; }
    .badge.ok { background: var(--okbg); color: #1d9bf0; }
    .badge.fail { background: var(--failbg); color: #f7878c; }
    .badge.skip { background: #2a2a0a; color: #c9c20b; }
  </style>
</head>
<body>
  <h1>Результати автотестів</h1>
  <p class="meta">Звіт: $ts · $timeStr · <code>$(Split-Path -Leaf $trx)</code> → згенеровано скриптом <code>scripts/Export-TestReport.ps1</code></p>
  <div class="cards">
    <div class="kpi passed"><div class="l">успіх</div><div class="n">$($summary.passed)</div></div>
    <div class="kpi failed"><div class="l">провал</div><div class="n">$($summary.failed)</div></div>
    <div class="kpi"><div class="l">усього</div><div class="n">$($summary.total)</div></div>
  </div>
  <div class="grows">$gHtml</div>
  <table>
    <thead><tr><th>Група</th><th>Назва</th><th>Час</th><th>Статус</th></tr></thead>
    <tbody>
$rowsHtml
    </tbody>
  </table>
</body>
</html>
"@

[System.IO.File]::WriteAllText($html, $template, [System.Text.Encoding]::UTF8)
Write-Host "OK: $html" -ForegroundColor Green
if ($Open) { Start-Process $html }
if ($vstestCode -ne 0) {
  Write-Host "Some tests failed. Exit code: $vstestCode" -ForegroundColor Yellow
  exit $vstestCode
}
