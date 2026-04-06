# wininfo-layout.ps1 — Analyze window as 2-panel layout (3:7 ratio)
# Usage: wininfo-layout.ps1 <hwnd>
param(
    [Parameter(Mandatory)][string]$Hwnd
)
. "$PSScriptRoot\_common.ps1"
Invoke-AgentZero @("wininfo-layout", $Hwnd)
exit $LASTEXITCODE
