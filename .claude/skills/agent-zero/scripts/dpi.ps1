# dpi.ps1 — Show DPI scaling and coordinate mapping info
. "$PSScriptRoot\_common.ps1"
Invoke-AgentZero @("dpi")
exit $LASTEXITCODE
