# mouseclick.ps1 — Click at screen coordinates
# Usage: mouseclick.ps1 <x> <y> [-Right]
param(
    [Parameter(Mandatory)][int]$X,
    [Parameter(Mandatory)][int]$Y,
    [switch]$Right
)
. "$PSScriptRoot\_common.ps1"
$a = @("mouseclick", $X, $Y)
if ($Right) { $a += "--right" }
Invoke-AgentZero $a
exit $LASTEXITCODE
