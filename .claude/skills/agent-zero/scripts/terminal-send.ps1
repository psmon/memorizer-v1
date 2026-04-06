# terminal-send.ps1 — Send text + Enter to a specific terminal session
# Usage: terminal-send.ps1 <group_index> <tab_index> <text...>
param(
    [Parameter(Mandatory)][int]$GroupIndex,
    [Parameter(Mandatory)][int]$TabIndex,
    [Parameter(Mandatory, ValueFromRemainingArguments)][string[]]$Text
)
. "$PSScriptRoot\_common.ps1"

$joined = $Text -join " "
Invoke-AgentZero @("terminal-send", $GroupIndex, $TabIndex, $joined)
exit $LASTEXITCODE
