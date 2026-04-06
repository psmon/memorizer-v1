# log.ps1 — View CLI action history
# Usage: log.ps1 [-Last <N>] [-Clear]
param(
    [int]$Last = 0,
    [switch]$Clear
)
. "$PSScriptRoot\_common.ps1"

$a = @("log")
if ($Last -gt 0) { $a += @("--last", $Last) }
if ($Clear) { $a += "--clear" }
Invoke-AgentZero $a
exit $LASTEXITCODE
