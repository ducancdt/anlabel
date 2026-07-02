param(
    [string]$IsccPath = ""
)

$ErrorActionPreference = "Stop"
$root = $PSScriptRoot
$project = Join-Path $root "src\ANLAbel.App\ANLAbel.App.csproj"
$publishDir = Join-Path $root "publish_out\trial-x64"
$releaseDir = Join-Path $root "releases\ANLAbel-Trial-7-Day-v0.057"

if (Test-Path $publishDir) { Remove-Item -LiteralPath $publishDir -Recurse -Force }
New-Item -ItemType Directory -Force -Path $publishDir, $releaseDir | Out-Null

dotnet publish $project -c Release -r win-x64 --self-contained true -p:TrialBuild=true -o $publishDir
if ($LASTEXITCODE -ne 0) { throw "Trial publish failed (exit $LASTEXITCODE)" }

$zipPath = Join-Path $releaseDir "ANLAbel-Trial-7-Day-v0.057-Portable-x64.zip"
if (Test-Path $zipPath) { Remove-Item -LiteralPath $zipPath -Force }
Compress-Archive -Path (Join-Path $publishDir "*") -DestinationPath $zipPath -CompressionLevel Optimal

if (-not $IsccPath) {
    $candidates = @(
        "$env:ProgramFiles(x86)\Inno Setup 6\ISCC.exe",
        "$env:ProgramFiles\Inno Setup 6\ISCC.exe"
    )
    $IsccPath = $candidates | Where-Object { $_ -and (Test-Path $_) } | Select-Object -First 1
}

if ($IsccPath -and (Test-Path $IsccPath)) {
    & $IsccPath (Join-Path $root "installer\ANLAbel-Trial-x64.iss")
    if ($LASTEXITCODE -ne 0) { throw "Inno Setup failed (exit $LASTEXITCODE)" }
} else {
    Write-Warning "Inno Setup 6 is not installed. Portable ZIP was created; setup EXE was skipped."
}

Write-Host "Trial release: $releaseDir" -ForegroundColor Green
