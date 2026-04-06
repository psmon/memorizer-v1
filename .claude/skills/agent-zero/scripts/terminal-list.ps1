# terminal-list.ps1 — List active terminal sessions in AgentZero
# Returns group/tab indices, titles, session IDs, and running state.
. "$PSScriptRoot\_common.ps1"
Invoke-AgentZero @("terminal-list")
exit $LASTEXITCODE
