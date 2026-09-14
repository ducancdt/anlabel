<#
.SYNOPSIS
    Run the unit test suite and commit the working tree only if every test passes.

.DESCRIPTION
    Automated "test-then-commit" gate for ANLAbel plan increments.
    - Runs the ANLAbel.UnitTests suite.
    - If (and only if) the run is green, stages all changes and creates a commit
      with the supplied message.
    - Fails closed: on any test failure, or when there is nothing to commit, no
      commit is created and a non-zero exit code is returned.

    It never pushes and never switches branch, matching the repo's manual-release
    discipline.

.PARAMETER Message
    The commit message subject/body. Required.

.PARAMETER Filter
    Optional dotnet test --filter expression to scope the run (default: full suite).

.EXAMPLE
    powershell -NoProfile -File scripts/Test-And-Commit.ps1 -Message "test: strengthen X contract"
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$Message,

    [string]$Filter
)

$ErrorActionPreference = 'Stop'
$repoRoot = Resolve-Path (Join-Path $PSScriptRoot '..')
Set-Location $repoRoot

$testArgs = @(
    'test',
    'src/ANLAbel.UnitTests/ANLAbel.UnitTests.csproj',
    '--nologo', '-v', 'quiet'
)
if ($Filter) { $testArgs += @('--filter', $Filter) }

Write-Host "==> Running unit tests..." -ForegroundColor Cyan
& dotnet @testArgs
if ($LASTEXITCODE -ne 0) {
    Write-Error "Tests failed (exit $LASTEXITCODE). No commit created."
    exit 1
}

# Only commit when there is something staged to commit.
$pending = git status --porcelain
if ([string]::IsNullOrWhiteSpace($pending)) {
    Write-Host "==> Working tree clean. Nothing to commit." -ForegroundColor Yellow
    exit 0
}

Write-Host "==> Tests green. Committing..." -ForegroundColor Green
git add -A
git commit -m $Message
if ($LASTEXITCODE -ne 0) {
    Write-Error "git commit failed (exit $LASTEXITCODE)."
    exit 1
}

Write-Host "==> Committed:" -ForegroundColor Green
git --no-pager log -1 --oneline
