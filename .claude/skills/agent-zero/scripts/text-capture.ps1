# text-capture.ps1 — Quick text capture from a window (stdout)
# Usage: text-capture.ps1 <hwnd> [-PickX <x> -PickY <y>]
param(
    [Parameter(Mandatory)][string]$Hwnd,
    [int]$PickX = -1,
    [int]$PickY = -1
)
. "$PSScriptRoot\_common.ps1"

$a = @("text-capture", $Hwnd)
if ($PickX -ge 0 -and $PickY -ge 0) { $a += @("--pick", $PickX, $PickY) }
Invoke-AgentZero $a
exit $LASTEXITCODE
