param(
    [string]$IsccPath
)

$ErrorActionPreference = 'Stop'
$root = $PSScriptRoot
$project = Join-Path $root 'src\ANLAbel.App\ANLAbel.App.csproj'
$publishDir = Join-Path $root 'publish_out\release-x64'

if (Test-Path -LiteralPath $publishDir) {
    Remove-Item -LiteralPath $publishDir -Recurse -Force
}

dotnet publish $project -c Release -r win-x64 --self-contained true -o $publishDir
if ($LASTEXITCODE -ne 0) { throw "Release publish failed (exit $LASTEXITCODE)" }

if ([string]::IsNullOrWhiteSpace($IsccPath)) {
    $candidates = @(
        "$env:LOCALAPPDATA\Programs\Inno Setup 6\ISCC.exe",
        "${env:ProgramFiles(x86)}\Inno Setup 6\ISCC.exe",
        "${env:ProgramFiles}\Inno Setup 6\ISCC.exe"
    )
    foreach ($cand in $candidates) {
        if (Test-Path -LiteralPath $cand) {
            $IsccPath = $cand
            break
        }
    }
    if ([string]::IsNullOrWhiteSpace($IsccPath)) {
        $cmd = Get-Command ISCC.exe -ErrorAction SilentlyContinue
        if ($cmd) { $IsccPath = $cmd.Source }
    }
}

if (-not (Test-Path -LiteralPath $IsccPath)) {
    throw "Inno Setup compiler not found. Specify -IsccPath or install Inno Setup 6."
}

& $IsccPath (Join-Path $root 'installer\ANLAbel-x64.iss')
if ($LASTEXITCODE -ne 0) { throw "Installer compile failed (exit $LASTEXITCODE)" }

Write-Host 'ANLAbel local release package built successfully.' -ForegroundColor Green
