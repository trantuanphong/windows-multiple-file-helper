<#
.SYNOPSIS
    Kịch bản gỡ cài đặt sạch sẽ MultipleFileHelper khỏi máy tính.
#>

[CmdletBinding()]
param(
    [string]$TargetDir = "$env:LOCALAPPDATA\Programs\MultipleFileHelper"
)

$ErrorActionPreference = "SilentlyContinue"

Write-Host "==========================================================" -ForegroundColor Cyan
Write-Host "       GO CAI DAT MULTIPLEFILEHELPER                      " -ForegroundColor Cyan
Write-Host "==========================================================" -ForegroundColor Cyan

# 1. Dong tien trinh neu con chay
Get-Process -Name "MultipleFileHelper" | Stop-Process -Force

# 2. Xoa sach Registry Menu qua .NET API truc tiep
Write-Host "[1/2] Xoa Context Menu khoi Registry..." -ForegroundColor Yellow
$hkcu = [Microsoft.Win32.Registry]::CurrentUser
foreach ($k in @(
    "Software\Classes\*\shell\MultipleFileHelper",
    "Software\Classes\*\shell\BatchFileTools",
    "Software\Classes\*\shell\FileNumberingTool"
)) {
    try {
        $hkcu.DeleteSubKeyTree($k)
    } catch {}
}

# 3. Xoa thu muc ung dung
Write-Host "[2/2] Xoa thu muc cai dat ung dung..." -ForegroundColor Yellow
if (Test-Path -LiteralPath $TargetDir) {
    Remove-Item -LiteralPath $TargetDir -Recurse -Force
}

Write-Host ""
Write-Host "==========================================================" -ForegroundColor Green
Write-Host " DA GO CAI DAT SACH SE MULTIPLEFILEHELPER!                " -ForegroundColor Green
Write-Host "==========================================================" -ForegroundColor Green
