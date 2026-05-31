@echo off
chcp 65001>nul
setlocal
set "INSTALL_SCRIPT=%~dp0Install-DulangLandIndexTool.ps1"

if not exist "%INSTALL_SCRIPT%" (
  echo 找不到安装脚本：%INSTALL_SCRIPT%
  pause
  exit /b 1
)

powershell -NoProfile -ExecutionPolicy Bypass -File "%INSTALL_SCRIPT%"
set "ERR=%ERRORLEVEL%"

echo.
if "%ERR%"=="0" (
  echo 安装完成。请关闭并重新打开 AutoCAD，然后输入命令 dulang1。
) else (
  echo 安装未完成，错误代码：%ERR%
)
pause
exit /b %ERR%
