# activate.ps1 — Restore and focus a window
# Usage: activate.ps1 <hwnd>
param(
    [Parameter(Mandatory)][string]$Hwnd
)
. "$PSScriptRoot\_common.ps1"
Invoke-AgentZero @("activate", $Hwnd)
exit $LASTEXITCODE
