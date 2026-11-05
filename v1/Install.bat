@echo off
setlocal
cd /d "%~dp0"
title Cai dat MultipleFileHelper

powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0Install.ps1"
echo.
pause
