@echo off
setlocal
cd /d "%~dp0"
title Go cai dat MultipleFileHelper

powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0Uninstall.ps1"
echo.
pause
