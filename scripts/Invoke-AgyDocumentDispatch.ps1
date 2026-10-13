[CmdletBinding()]
param(
    [int]$MaxCycles = 4,
    [switch]$Live,
    [string]$Repo = '',
    [string]$AgyPath = ''
)

$ErrorActionPreference = 'Stop'
$repoRoot = if ([string]::IsNullOrWhiteSpace($Repo)) { Split-Path -Parent $PSScriptRoot } else { [IO.Path]::GetFullPath($Repo) }
$project = Join-Path $repoRoot 'tools\AgyDocumentDispatcher\AgyDocumentDispatcher.csproj'
$dotnetArgs = @('run', '--project', $project, '--', '--repo', $repoRoot, '--max-cycles', "$MaxCycles")
if ($Live) { $dotnetArgs += '--live' } else { $dotnetArgs += '--dry-run' }
if (-not [string]::IsNullOrWhiteSpace($AgyPath)) { $dotnetArgs += @('--agy', $AgyPath) }

Push-Location $repoRoot
try {
    & dotnet @dotnetArgs
    if ($LASTEXITCODE -ne 0) { throw "AgyDocumentDispatcher failed with exit code $LASTEXITCODE." }
}
finally { Pop-Location }
