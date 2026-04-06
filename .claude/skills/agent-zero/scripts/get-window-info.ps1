# get-window-info.ps1 — List all visible windows (Handle, Class, Title, Rect, Process, PID)
. "$PSScriptRoot\_common.ps1"
Invoke-AgentZero @("get-window-info")
exit $LASTEXITCODE
