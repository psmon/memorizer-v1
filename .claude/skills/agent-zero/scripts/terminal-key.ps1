# terminal-key.ps1 — Send raw key sequence to a specific terminal session
# Usage: terminal-key.ps1 <group_index> <tab_index> <key>
# Keys: cr, lf, crlf, esc, tab, backspace, del, ctrlc, ctrld, up, down, left, right, hex:XX
param(
    [Parameter(Mandatory)][int]$GroupIndex,
    [Parameter(Mandatory)][int]$TabIndex,
    [Parameter(Mandatory)][string]$Key
)
. "$PSScriptRoot\_common.ps1"

Invoke-AgentZero @("terminal-key", $GroupIndex, $TabIndex, $Key)
exit $LASTEXITCODE
