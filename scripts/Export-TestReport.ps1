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

function Split-FullTestName([string] $name) {
  $lastDot = $name.LastIndexOf('.')
  if ($lastDot -lt 0) { return @{ group = "Other"; className = "?"; method = $name } }
  $rest = $name.Substring(0, $lastDot)
  $method = $name.Substring($lastDot + 1)
  $ld2 = $rest.LastIndexOf('.')
  if ($ld2 -lt 0) { $class = $rest } else { $class = $rest.Substring($ld2 + 1) }
  $g = "Other"
  if ($name -match "^NewsAggregator\.(Unit|Database|Integration)Tests\.") { $g = $Matches[1] + "Tests" }
  @{ group = $g; className = $class; method = $method; full = $name }
}

$rows = foreach ($n in $nodes) {
  $name = $n.GetAttribute("testName")
  $outc = $n.GetAttribute("outcome")
  $dur = $n.GetAttribute("duration")
  $sp = Split-FullTestName $name
  $errN = $n.SelectSingleNode("t:Output/t:ErrorInfo/t:Message", $nsm)
  $err = if ($errN) { $errN.InnerText } else { $null }
  [pscustomobject]@{
    group     = $sp.group
    className = $sp.className
    method    = $sp.method
    name      = $name
    outc      = $outc
    dur       = $dur
    err       = $err
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

# Latin-only: avoids PS encoding issues. Ukrainian text is in the HTML template below.
$sectionMeta = @{
  "UnitTests"        = @{ title = "Unit tests";            hint = "Fast, no real DB" }
  "DatabaseTests"    = @{ title = "Database (Testcontainers)"; hint = "Real PostgreSQL, constraints" }
  "IntegrationTests" = @{ title = "Integration (WAF+HTTP)";   hint = "In-process host, real HTTP" }
  "Other"            = @{ title = "Other"; hint = "" }
}

$sectionsHtml = ($byGroup | ForEach-Object {
  $gname = $_.Name
  $m = if ($sectionMeta.ContainsKey($gname)) { $sectionMeta[$gname] } else { @{ title = $gname; hint = "" } }
  $rowsForG = $_.Group | Sort-Object className, method
  $inner = ($rowsForG | ForEach-Object {
    $b = (Get-Badge $_.outc)
    $errRow = if ($_.err) { "      <tr class='err-row'><td colspan='4' class='err'>{0}</td></tr>" -f (& $esc $_.err) } else { "" }
    "      <tr>
        <td class='cls'>{0}</td>
        <td class='meth'>{1}</td>
        <td class='d'>{2}</td>
        <td><span class='{3}'>{4}</span></td>
      </tr>
$errRow" -f (& $esc $_.className), (& $esc $_.method), (& $esc $_.dur), $b, (& $esc $_.outc)
  }) -join "`n"
  $hintP = if ($m.hint) { "<p class='sechint'>$($m.hint)</p>" } else { "" }
@"

  <section class='section'>
    <h2>$( & $esc $m.title) <span class='cnt'>$($_.Count) tests</span></h2>
    $hintP
    <div class="tblwrap">
    <table>
      <thead><tr><th>Class</th><th>Method / case</th><th>Duration</th><th>Outcome</th></tr></thead>
      <tbody>
$inner
      </tbody>
    </table>
    </div>
  </section>
"@
}) -join "`n"

$gHtml = ($byGroup | ForEach-Object {
  "    <li><b>$($_.Name)</b> - <span class='c'>$($_.Count) tests</span></li>" }) -join "`n"

$when = (Get-Date -Format "yyyy-MM-dd HH:mm")
$timeStr = if ($times) { $times.GetAttribute("start") } else { "" }
$ts = (Get-Date -Format o)

$template = @"
<!DOCTYPE html>
<html lang="en">
<head>
  <meta charset="utf-8" />
  <title>Test report $when</title>
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
    details.int { background: var(--card); border-radius: 8px; padding: 0.75rem 1rem; margin-bottom: 1.25rem; }
    details.int summary { cursor: pointer; color: #8899a6; font-size: 0.9rem; }
    details.int ul { margin: 0.5rem 0 0 1.1rem; color: var(--muted); font-size: 0.875rem; }
    .section { margin-bottom: 1.75rem; }
    .section h2 { font-size: 1.05rem; font-weight: 600; margin: 0 0 0.35rem; }
    .section .cnt { color: #8899a6; font-weight: 500; }
    .sechint { color: var(--muted); font-size: 0.82rem; margin: 0 0 0.65rem; }
    .tblwrap { overflow-x: auto; }
    .c { color: var(--muted); }
    table { width: 100%; border-collapse: collapse; background: var(--card); border-radius: 8px; overflow: hidden; font-size: 0.85rem; }
    th { text-align: left; padding: 0.55rem 0.7rem; color: var(--muted); font-weight: 500; background: #141a21; }
    td { padding: 0.4rem 0.7rem; border-top: 1px solid #242d38; vertical-align: top; }
    .cls { color: #7eb8da; width: 28%; }
    .meth { word-break: break-word; }
    .d { color: var(--muted); font-variant-numeric: tabular-nums; white-space: nowrap; width: 5rem; }
    tr.err-row td { border-top: none; padding-top: 0; }
    td.err { color: #f7878c; font-size: 0.8rem; padding-left: 1.2rem; white-space: pre-wrap; }
    .badge { display: inline-block; padding: 0.15rem 0.4rem; border-radius: 4px; font-size: 0.8rem; }
    .badge.ok { background: var(--okbg); color: #1d9bf0; }
    .badge.fail { background: var(--failbg); color: #f7878c; }
    .badge.skip { background: #2a2a0a; color: #c9c20b; }
  </style>
</head>
<body>
  <h1>Test results (HTML)</h1>
  <p class="meta">Generated: $ts · run start: $timeStr · <code>$(Split-Path -Leaf $trx)</code> · <code>scripts/Export-TestReport.ps1</code></p>
  <div class="cards">
    <div class="kpi passed"><div class="l">passed</div><div class="n">$($summary.passed)</div></div>
    <div class="kpi failed"><div class="l">failed</div><div class="n">$($summary.failed)</div></div>
    <div class="kpi"><div class="l">total</div><div class="n">$($summary.total)</div></div>
  </div>
  <details class="int">
    <summary>What each level means (short)</summary>
    <ul>
$gHtml
    </ul>
  </details>
$sectionsHtml
</body>
</html>
"@

$enc = New-Object System.Text.UTF8Encoding $true
[System.IO.File]::WriteAllText($html, $template, $enc)
Write-Host "OK: $html" -ForegroundColor Green
if ($Open) { Start-Process $html }
if ($vstestCode -ne 0) {
  Write-Host "Some tests failed. Exit code: $vstestCode" -ForegroundColor Yellow
  exit $vstestCode
}
