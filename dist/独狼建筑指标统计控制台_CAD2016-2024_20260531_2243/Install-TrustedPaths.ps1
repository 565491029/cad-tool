$script = Join-Path (Split-Path -Parent $MyInvocation.MyCommand.Path) "Install-DulangLandIndexTool.ps1"
powershell -NoProfile -ExecutionPolicy Bypass -File $script -TrustOnly
exit $LASTEXITCODE
