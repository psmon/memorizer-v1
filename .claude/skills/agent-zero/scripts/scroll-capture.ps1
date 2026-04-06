# scroll-capture.ps1 — Scroll capture with date filtering (stdout)
# Usage: scroll-capture.ps1 <hwnd> [-Start DATE] [-End DATE] [-Delay N] [-Max N] [-Delta N] [-PickX X -PickY Y]
param(
    [Parameter(Mandatory)][string]$Hwnd,
    [string]$Start,
    [string]$End,
    [int]$Delay = 0,
    [int]$Max = 0,
    [int]$Delta = 0,
    [int]$PickX = -1,
    [int]$PickY = -1
)
. "$PSScriptRoot\_common.ps1"

$a = @("scroll-capture", $Hwnd)
if ($Start)          { $a += @("--start", $Start) }
if ($End)            { $a += @("--end", $End) }
if ($Delay -gt 0)   { $a += @("--delay", $Delay) }
if ($Max -gt 0)     { $a += @("--max", $Max) }
if ($Delta -gt 0)   { $a += @("--delta", $Delta) }
if ($PickX -ge 0 -and $PickY -ge 0) { $a += @("--pick", $PickX, $PickY) }
Invoke-AgentZero $a
exit $LASTEXITCODE
