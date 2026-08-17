[CmdletBinding()]
param(
    [ValidateSet('Fast', 'Mutation')]
    [string]$Mode = 'Fast',
    [string]$Root = ''
)

$ErrorActionPreference = 'Stop'
$repoRoot = if ([string]::IsNullOrWhiteSpace($Root)) { Split-Path -Parent $PSScriptRoot } else { [IO.Path]::GetFullPath($Root) }

function Invoke-Checked {
    param([Parameter(Mandatory)][string]$File, [Parameter(Mandatory)][string[]]$Arguments, [Parameter(Mandatory)][string]$Label)
    & $File @Arguments
    if ($LASTEXITCODE -ne 0) { throw "$Label failed with exit code $LASTEXITCODE." }
}

Push-Location $repoRoot
try {
    Invoke-Checked 'dotnet' @('build', 'ANLAbel.slnx', '--no-restore', '--nologo', '-v', 'quiet', '-p:UseSharedCompilation=false', '-nodeReuse:false') 'Solution build'
    Invoke-Checked 'dotnet' @('test', 'src/ANLAbel.UnitTests/ANLAbel.UnitTests.csproj', '--no-build', '--nologo') 'Unit tests'
    Invoke-Checked 'dotnet' @('run', '--project', 'src/ANLAbel.Tests/ANLAbel.Tests.csproj', '--no-build') 'Application regressions'

    if ($Mode -eq 'Mutation') {
        Invoke-Checked 'dotnet' @('tool', 'restore') 'Local tool restore'
        Push-Location (Join-Path $repoRoot 'src/ANLAbel.UnitTests')
        try { Invoke-Checked 'dotnet' @('stryker', '--config-file', 'stryker-config.json') 'Mutation gate (90%)' }
        finally { Pop-Location }
    }

    Write-Host "ANLAbel $Mode quality loop passed."
}
finally { Pop-Location }
