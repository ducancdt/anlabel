# Deploy ANLAbel to the installed desktop app folder.
# Publishes self-contained win-x64 and copies over the location the
# desktop shortcut points to: %LOCALAPPDATA%\Programs\ANLAbel
#
# Usage:  powershell -ExecutionPolicy Bypass -File deploy-desktop.ps1

$ErrorActionPreference = "Stop"
$root      = $PSScriptRoot
$proj      = Join-Path $root "src\ANLAbel.App\ANLAbel.App.csproj"
$publishDir = Join-Path $root "publish_out\desktop-x64"
$installDir = Join-Path $env:LOCALAPPDATA "Programs\ANLAbel"

Write-Host "==> Stopping running ANLAbel (if any)..." -ForegroundColor Cyan
Get-Process -Name "ANLAbel.App" -ErrorAction SilentlyContinue | Stop-Process -Force
Start-Sleep -Milliseconds 500

Write-Host "==> Publishing self-contained win-x64..." -ForegroundColor Cyan
if (Test-Path $publishDir) { Remove-Item $publishDir -Recurse -Force }
dotnet publish $proj -c Release -r win-x64 --self-contained true -o $publishDir
if ($LASTEXITCODE -ne 0) { throw "dotnet publish failed (exit $LASTEXITCODE)" }

Write-Host "==> Updating installed app at $installDir ..." -ForegroundColor Cyan
if (-not (Test-Path $installDir)) {
    throw "Installed folder not found: $installDir  (run the installer once first)"
}
# Mirror published output into the install dir, keeping docs/ subfolder intact.
robocopy $publishDir $installDir /MIR /XD docs /NFL /NDL /NJH /NJS /NP | Out-Null
# robocopy exit codes 0-7 are success; 8+ are real errors.
if ($LASTEXITCODE -ge 8) { throw "robocopy failed (exit $LASTEXITCODE)" }

$exe = Join-Path $installDir "ANLAbel.App.exe"
$ver = (Get-Item $exe).VersionInfo.FileVersion
Write-Host "==> Done. Installed ANLAbel.App.exe version: $ver" -ForegroundColor Green
Write-Host "    Launch from the desktop shortcut to verify." -ForegroundColor Green
