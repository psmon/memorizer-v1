# mousemove.ps1 — Move mouse cursor to screen coordinates
# Usage: mousemove.ps1 <x> <y>
param(
    [Parameter(Mandatory)][int]$X,
    [Parameter(Mandatory)][int]$Y
)
. "$PSScriptRoot\_common.ps1"
Invoke-AgentZero @("mousemove", $X, $Y)
exit $LASTEXITCODE
