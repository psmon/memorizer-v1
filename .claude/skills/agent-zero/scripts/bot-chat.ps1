# bot-chat.ps1 — Send chat message to AgentBot window
# Usage: bot-chat.ps1 <message...> [-From <name>]
[CmdletBinding(PositionalBinding=$false)]
param(
    [string]$From = "CLI",
    [Parameter(Mandatory, ValueFromRemainingArguments)][string[]]$Message
)
. "$PSScriptRoot\_common.ps1"

$joined = $Message -join " "
$a = @("bot-chat")
if ($From -ne "CLI") { $a += @("--from", $From) }
$a += $joined
Invoke-AgentZero $a
exit $LASTEXITCODE
