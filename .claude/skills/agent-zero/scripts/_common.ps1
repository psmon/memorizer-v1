# _common.ps1 — Shared helper for AgentZero skill scripts
# Source this at the top of each script: . "$PSScriptRoot\_common.ps1"

$ErrorActionPreference = "Stop"

function Invoke-AgentZero {
    param([string[]]$Arguments)
    $result = & AgentZero.ps1 @Arguments 2>&1
    $result | Write-Output
    return $LASTEXITCODE
}
