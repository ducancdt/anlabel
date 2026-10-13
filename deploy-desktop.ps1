# Deploy ANLAbel to the installed desktop app folder.
# Publishes self-contained win-x64 and copies over the location the
# desktop shortcut points to: %LOCALAPPDATA%\Programs\ANLAbel
#
# Usage:  powershell -ExecutionPolicy Bypass -File deploy-desktop.ps1 [-Force]
#
# A running ANLAbel is asked to close normally (so it can prompt to save).
# If it is still running after the wait, the deploy stops instead of killing
# an editing session. -Force restores the old kill-and-replace behaviour.

param([switch]$Force)

$ErrorActionPreference = "Stop"
$root      = $PSScriptRoot
$proj      = Join-Path $root "src\ANLAbel.App\ANLAbel.App.csproj"
$publishDir = Join-Path $root "publish_out\desktop-x64"
$installDir = Join-Path $env:LOCALAPPDATA "Programs\ANLAbel"

Write-Host "==> Closing running ANLAbel (if any)..." -ForegroundColor Cyan
$running = @(Get-Process -Name "ANLAbel.App" -ErrorAction SilentlyContinue)
if ($running.Count -gt 0) {
    foreach ($process in $running) { [void]$process.CloseMainWindow() }
    $deadline = (Get-Date).AddSeconds(20)
    while ((Get-Date) -lt $deadline -and @(Get-Process -Name "ANLAbel.App" -ErrorAction SilentlyContinue).Count -gt 0) {
        Start-Sleep -Milliseconds 500
    }
    $still = @(Get-Process -Name "ANLAbel.App" -ErrorAction SilentlyContinue)
    if ($still.Count -gt 0) {
        if (-not $Force) {
            throw "ANLAbel is still running (it may be waiting for the operator to save). Close it, then deploy again, or use -Force to kill it."
        }
        $still | Stop-Process -Force
        Start-Sleep -Milliseconds 500
    }
}

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
