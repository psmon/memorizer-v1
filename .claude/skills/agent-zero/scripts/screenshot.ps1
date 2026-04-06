# screenshot.ps1 — Desktop or region screenshot
# Usage: screenshot.ps1 [-Color] [-Original] [-AtX <x> -AtY <y>] [-Size <N>]
param(
    [switch]$Color,
    [switch]$Original,
    [int]$AtX = -1,
    [int]$AtY = -1,
    [int]$Size = 0
)
. "$PSScriptRoot\_common.ps1"

$a = @("screenshot")
if ($Color)    { $a += "--color" }
if ($Original) { $a += "--original" }
if ($AtX -ge 0 -and $AtY -ge 0) { $a += @("--at", $AtX, $AtY) }
if ($Size -gt 0) { $a += @("--size", $Size) }
Invoke-AgentZero $a
exit $LASTEXITCODE
