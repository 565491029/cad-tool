@echo off
chcp 65001>nul
setlocal
set "UNINSTALL_SCRIPT=%~dp0Uninstall-DulangLandIndexTool.ps1"

if not exist "%UNINSTALL_SCRIPT%" (
  echo 找不到卸载脚本：%UNINSTALL_SCRIPT%
  pause
  exit /b 1
)

powershell -NoProfile -ExecutionPolicy Bypass -File "%UNINSTALL_SCRIPT%"
set "ERR=%ERRORLEVEL%"

echo.
if "%ERR%"=="0" (
  echo 卸载完成。
) else (
  echo 卸载未完成，错误代码：%ERR%
)
pause
exit /b %ERR%
