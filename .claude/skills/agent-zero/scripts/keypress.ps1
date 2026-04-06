# keypress.ps1 — Type text or press special keys
# Usage: keypress.ps1 <text...>
#        keypress.ps1 -Key <keyname|combo>
#        keypress.ps1 <text...> -Delay <ms>
[CmdletBinding(PositionalBinding=$false)]
param(
    [string]$Key,
    [int]$Delay = 0,
    [Parameter(ValueFromRemainingArguments)][string[]]$Text
)
. "$PSScriptRoot\_common.ps1"

$a = @("keypress")
if ($Key) {
    $a += @("--key", $Key)
} else {
    $a += $Text
}
if ($Delay -gt 0) { $a += @("--delay", $Delay) }
Invoke-AgentZero $a
exit $LASTEXITCODE
