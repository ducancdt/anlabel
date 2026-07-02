$ErrorActionPreference = "Stop"
$root = $PSScriptRoot
$trialProject = Join-Path $root "src\ANLAbel.App\ANLAbel.App.csproj"
$commercialProject = $trialProject
$masterProject = Join-Path $root "src\ANLAbel.LicenseGenerator\ANLAbel.LicenseGenerator.csproj"
$trialPublish = Join-Path $root "publish_out\trial-x64"
$commercialPublish = Join-Path $root "publish_out\commercial-x64"
$masterPublish = Join-Path $root "publish_out\license-master-x64"
$trialRelease = Join-Path $root "releases\ANLAbel-Trial-7-Day-v0.057"
$commercialRelease = Join-Path $root "releases\ANLAbel-Commercial-v0.057"
$masterRelease = Join-Path $root "releases\ANLAbel-License-Master-v1.0"

foreach ($path in @($trialPublish, $commercialPublish, $masterPublish, $trialRelease, $commercialRelease, $masterRelease)) {
    if (Test-Path $path) { Remove-Item -LiteralPath $path -Recurse -Force }
    New-Item -ItemType Directory -Force -Path $path | Out-Null
}

dotnet publish $trialProject -c Release -r win-x64 --self-contained true -p:TrialBuild=true -o $trialPublish
if ($LASTEXITCODE -ne 0) { throw "Trial publish failed (exit $LASTEXITCODE)" }

dotnet publish $commercialProject -c Release -r win-x64 --self-contained true -o $commercialPublish
if ($LASTEXITCODE -ne 0) { throw "Commercial publish failed (exit $LASTEXITCODE)" }

dotnet publish $masterProject -c Release -r win-x64 --self-contained true -o $masterPublish
if ($LASTEXITCODE -ne 0) { throw "License Master publish failed (exit $LASTEXITCODE)" }

$trialZip = Join-Path $trialRelease "ANLAbel-Trial-7-Day-v0.057-Portable-x64.zip"
$commercialZip = Join-Path $commercialRelease "ANLAbel-Commercial-v0.057-Portable-x64.zip"
$masterZip = Join-Path $masterRelease "ANLAbel-License-Master-v1.0-Private-x64.zip"
Compress-Archive -Path (Join-Path $trialPublish "*") -DestinationPath $trialZip -CompressionLevel Optimal
Compress-Archive -Path (Join-Path $commercialPublish "*") -DestinationPath $commercialZip -CompressionLevel Optimal
Compress-Archive -Path (Join-Path $masterPublish "*") -DestinationPath $masterZip -CompressionLevel Optimal

Copy-Item -LiteralPath $masterPublish -Destination (Join-Path $masterRelease "app") -Recurse
Copy-Item -LiteralPath (Join-Path $root "docs\huong-dan-trial-va-kich-hoat.txt") -Destination $trialRelease
Copy-Item -LiteralPath (Join-Path $root "docs\huong-dan-trial-va-kich-hoat.txt") -Destination $masterRelease

$isccCandidates = @(
    "${env:ProgramFiles(x86)}\Inno Setup 6\ISCC.exe",
    "$env:ProgramFiles\Inno Setup 6\ISCC.exe",
    "$env:LOCALAPPDATA\Programs\Inno Setup 6\ISCC.exe"
)
$iscc = $isccCandidates | Where-Object { $_ -and (Test-Path $_) } | Select-Object -First 1
if (-not $iscc) { throw "Inno Setup 6 compiler was not found." }

& $iscc (Join-Path $root "installer\ANLAbel-Trial-x64.iss")
if ($LASTEXITCODE -ne 0) { throw "Trial installer compile failed (exit $LASTEXITCODE)" }
& $iscc (Join-Path $root "installer\ANLAbel-Commercial-x64.iss")
if ($LASTEXITCODE -ne 0) { throw "Commercial installer compile failed (exit $LASTEXITCODE)" }
& $iscc (Join-Path $root "installer\ANLAbel-License-Master-x64.iss")
if ($LASTEXITCODE -ne 0) { throw "Master installer compile failed (exit $LASTEXITCODE)" }

Write-Host "Trial package : $trialZip" -ForegroundColor Green
Write-Host "Commercial    : $commercialRelease" -ForegroundColor Green
Write-Host "Private Master: $masterRelease" -ForegroundColor Yellow
