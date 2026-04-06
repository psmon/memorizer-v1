# status.ps1 — Query AgentZero WPF app state
. "$PSScriptRoot\_common.ps1"
Invoke-AgentZero @("status")
exit $LASTEXITCODE
