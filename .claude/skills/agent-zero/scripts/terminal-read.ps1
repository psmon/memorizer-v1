# terminal-read.ps1 — Read console output text from a terminal session
# Usage: terminal-read.ps1 <group_index> <tab_index> [-Last <N>]
param(
    [Parameter(Mandatory)][int]$GroupIndex,
    [Parameter(Mandatory)][int]$TabIndex,
    [int]$Last = 0
)
. "$PSScriptRoot\_common.ps1"

$a = @("terminal-read", $GroupIndex, $TabIndex)
if ($Last -gt 0) { $a += @("--last", $Last) }
Invoke-AgentZero $a
exit $LASTEXITCODE
