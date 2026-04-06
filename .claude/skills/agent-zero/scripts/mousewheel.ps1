# mousewheel.ps1 — Scroll wheel at screen coordinates
# Usage: mousewheel.ps1 <x> <y> [-Delta <N>]
param(
    [Parameter(Mandatory)][int]$X,
    [Parameter(Mandatory)][int]$Y,
    [int]$Delta = 3
)
. "$PSScriptRoot\_common.ps1"
Invoke-AgentZero @("mousewheel", $X, $Y, "--delta", $Delta)
exit $LASTEXITCODE
