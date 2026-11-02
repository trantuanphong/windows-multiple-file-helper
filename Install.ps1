<#
.SYNOPSIS
    Kịch bản cài đặt tự động công cụ MultipleFileHelper cho máy bất kỳ.
#>

[CmdletBinding()]
param(
    [string]$TargetDir = "$env:LOCALAPPDATA\Programs\MultipleFileHelper"
)

$ErrorActionPreference = "Stop"

Write-Host "==========================================================" -ForegroundColor Cyan
Write-Host "       CAI DAT MULTIPLEFILEHELPER (WINDOWS EXPLORER)      " -ForegroundColor Cyan
Write-Host "==========================================================" -ForegroundColor Cyan

$sourceExe = Join-Path $PSScriptRoot "publish\MultipleFileHelper.exe"
if (!(Test-Path -LiteralPath $sourceExe)) {
    $sourceExe = Join-Path $PSScriptRoot "MultipleFileHelper.exe"
}

if (!(Test-Path -LiteralPath $sourceExe)) {
    Write-Host "[LOI] Khong tim thay MultipleFileHelper.exe!" -ForegroundColor Red
    Write-Host "Vui long chay build/publish truoc hoac dat file installer cung thu muc voi file EXE." -ForegroundColor Yellow
    exit 1
}

# 1. Tao thu muc cai dat trong LocalAppData (khong can quyen Admin)
Write-Host "[1/4] Tao thu muc cai dat tai: $TargetDir" -ForegroundColor Yellow
if (!(Test-Path -LiteralPath $TargetDir)) {
    New-Item -Path $TargetDir -ItemType Directory -Force | Out-Null
}

$destExe = Join-Path $TargetDir "MultipleFileHelper.exe"
$destConfig = Join-Path $TargetDir "MultipleFileHelper.exe.config"

# 2. Dong tien trinh neu dang chay va sao chep EXE
Write-Host "[2/4] Sao chep file ung dung..." -ForegroundColor Yellow
Get-Process -Name "MultipleFileHelper" -ErrorAction SilentlyContinue | Stop-Process -Force
Copy-Item -LiteralPath $sourceExe -Destination $destExe -Force

$sourceConfig = [System.IO.Path]::ChangeExtension($sourceExe, ".exe.config")
if (Test-Path -LiteralPath $sourceConfig) {
    Copy-Item -LiteralPath $sourceConfig -Destination $destConfig -Force
}

# 3. Dang ky Menu ngu canh truc tiep qua .NET Registry API (cuc nhanh < 10ms, khong bi PowerShell provider delay)
Write-Host "[3/4] Dang ky Context Menu vao Windows Explorer..." -ForegroundColor Yellow

$hkcu = [Microsoft.Win32.Registry]::CurrentUser
$basePath = "Software\Classes\*\shell\MultipleFileHelper"

$kRoot = $hkcu.CreateSubKey($basePath)
$kRoot.SetValue("MUIVerb", "Xử lý tên hàng loạt")
$kRoot.SetValue("SubCommands", "")
$kRoot.SetValue("Icon", "shell32.dll,133")
$kRoot.SetValue("MultiSelectModel", "Player")
$kRoot.Close()

$shellPath = "$basePath\shell"

# Submenu 1: Đánh số thứ tự
$kSub1 = $hkcu.CreateSubKey("$shellPath\01_Numbering")
$kSub1.SetValue("MUIVerb", "Đánh số thứ tự")
$kSub1.SetValue("MultiSelectModel", "Player")
$kSub1.SetValue("Icon", "shell32.dll,264")
$kSub1Cmd = $kSub1.CreateSubKey("command")
$kSub1Cmd.SetValue("", "`"$destExe`" --action=numbering `"%1`"")
$kSub1Cmd.Close()
$kSub1.Close()

# Submenu 2: Xóa n ký tự đầu
$kSub2 = $hkcu.CreateSubKey("$shellPath\02_RemovePrefix")
$kSub2.SetValue("MUIVerb", "Xóa n ký tự đầu")
$kSub2.SetValue("MultiSelectModel", "Player")
$kSub2.SetValue("Icon", "shell32.dll,131")
$kSub2Cmd = $kSub2.CreateSubKey("command")
$kSub2Cmd.SetValue("", "`"$destExe`" --action=remove-prefix `"%1`"")
$kSub2Cmd.Close()
$kSub2.Close()

# Xoa cac menu cu neu con ton tai tren he thong (bang .NET API cuc nhanh)
foreach ($old in @("Software\Classes\*\shell\BatchFileTools", "Software\Classes\*\shell\FileNumberingTool")) {
    try {
        $hkcu.DeleteSubKeyTree($old)
    } catch {}
}

# 4. Hoan tat
Write-Host "[4/4] Kiem tra he thong hoan tat!" -ForegroundColor Green
Write-Host ""
Write-Host "==========================================================" -ForegroundColor Green
Write-Host " CAI DAT THANH CONG!                                      " -ForegroundColor Green
Write-Host " Ban co the chon nhieu file -> Chuot phai -> Xu ly nhieu File " -ForegroundColor Green
Write-Host "==========================================================" -ForegroundColor Green
