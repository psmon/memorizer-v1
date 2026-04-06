# element-tree.ps1 — UI Automation element tree scan
# Usage: element-tree.ps1 <hwnd> [-Search <keyword>] [-Depth <N>]
param(
    [Parameter(Mandatory)][string]$Hwnd,
    [string]$Search,
    [int]$Depth = 0
)
. "$PSScriptRoot\_common.ps1"

$a = @("element-tree", $Hwnd)
if ($Search)     { $a += @("--search", $Search) }
if ($Depth -gt 0) { $a += @("--depth", $Depth) }
Invoke-AgentZero $a
exit $LASTEXITCODE
