# Builds the Desktop Launchpad installer on your own PC.
# Needs Inno Setup 6 (https://jrsoftware.org/isdl.php) and the .NET 8 SDK (comes with Visual Studio).
# Run from the repo folder:  powershell -ExecutionPolicy Bypass -File installer\build-installer.ps1
param([string]$Version = "1.0.0")

$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $PSScriptRoot
$publishDir = Join-Path $root "artifacts\publish"

if (Test-Path $publishDir) { Remove-Item $publishDir -Recurse -Force }

dotnet publish (Join-Path $root "src\DesktopLaunchpad\DesktopLaunchpad.csproj") `
    -c Release -r win-x64 --self-contained true -p:Version=$Version -o $publishDir
if ($LASTEXITCODE -ne 0) { throw "dotnet publish failed" }

$iscc = @(
    "${env:ProgramFiles(x86)}\Inno Setup 6\ISCC.exe",
    "$env:ProgramFiles\Inno Setup 6\ISCC.exe",
    "$env:LocalAppData\Programs\Inno Setup 6\ISCC.exe"
) | Where-Object { Test-Path $_ } | Select-Object -First 1
if (-not $iscc) { throw "Inno Setup 6 not found. Install it from https://jrsoftware.org/isdl.php" }

& $iscc "/DMyAppVersion=$Version" "/DPublishDir=$publishDir" (Join-Path $PSScriptRoot "DesktopLaunchpad.iss")
if ($LASTEXITCODE -ne 0) { throw "Inno Setup failed" }

Write-Host "Installer ready in artifacts\installer"
