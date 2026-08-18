param(
    [Parameter(Mandatory)]
    [ValidatePattern('^\d+\.\d+$')]
    [string]$Version,
    [string]$Root
)

$ErrorActionPreference = 'Stop'
$root = if ([string]::IsNullOrWhiteSpace($Root)) { Split-Path -Parent $PSScriptRoot } else { [IO.Path]::GetFullPath($Root) }

function Replace-Required([string]$relativePath, [string]$pattern, [string]$replacement) {
    $path = Join-Path $root $relativePath
    if (-not (Test-Path $path)) { return }
    $text = [IO.File]::ReadAllText($path)
    if (-not [regex]::IsMatch($text, $pattern)) {
        if ($text.IndexOf($replacement, [StringComparison]::Ordinal) -ge 0) { return }
        throw "Expected version projection was not found: $relativePath"
    }
    $updated = [regex]::Replace($text, $pattern, $replacement)
    if ($updated -eq $text) { return }
    [IO.File]::WriteAllText($path, $updated, [Text.UTF8Encoding]::new($false))
}

Replace-Required 'eng/Version.props' '<ANLAbelReleaseVersion>\d+\.\d+</ANLAbelReleaseVersion>' "<ANLAbelReleaseVersion>$Version</ANLAbelReleaseVersion>"

Replace-Required 'src/ANLAbel.App/ANLAbel.App.csproj' '<Version>\d+\.\d+</Version>' "<Version>$Version</Version>"
Replace-Required 'src/ANLAbel.App/ANLAbel.App.csproj' '<AssemblyVersion>\d+\.\d+\.0\.0</AssemblyVersion>' "<AssemblyVersion>$Version.0.0</AssemblyVersion>"
Replace-Required 'src/ANLAbel.App/ANLAbel.App.csproj' '<FileVersion>\d+\.\d+\.0\.0</FileVersion>' "<FileVersion>$Version.0.0</FileVersion>"
Replace-Required 'src/ANLAbel.App/ANLAbel.App.csproj' '<InformationalVersion>\d+\.\d+</InformationalVersion>' "<InformationalVersion>$Version</InformationalVersion>"
Replace-Required 'src/ANLAbel.App/ANLAbel.App.csproj' '<InformationalVersion>\d+\.\d+-trial\.7d</InformationalVersion>' "<InformationalVersion>$Version-trial.7d</InformationalVersion>"

foreach ($file in @('src/ANLAbel.App/MainWindow.xaml', 'src/ANLAbel.App/HelpWindow.xaml.cs', 'src/ANLAbel.App/App.xaml.cs')) {
    Replace-Required $file 'v\d+\.\d+' "v$Version"
}

foreach ($file in @('installer/ANLAbel-x64.iss', 'installer/ANLAbel-Commercial-x64.iss', 'installer/ANLAbel-Trial-x64.iss')) {
    Replace-Required $file 'AppVersion=\d+\.\d+' "AppVersion=$Version"
    Replace-Required $file 'v\d+\.\d+' "v$Version"
    Replace-Required $file 'VersionInfoVersion=\d+\.\d+\.0\.0' "VersionInfoVersion=$Version.0.0"
}

Replace-Required 'docs/VERSIONING.md' 'current public version is `\d+\.\d+`' ('current public version is `{0}`' -f $Version)
Replace-Required 'docs/AUTOMATED_QUALITY_LOOP.md' 'public version `\d+\.\d+`' ('public version `{0}`' -f $Version)
Replace-Required 'docs/audit-2026-07-02.md' 'public version `\d+\.\d+` is canonical' ('public version `{0}` is canonical' -f $Version)
Replace-Required 'docs/reinvention/11-verification-checkpoint-2026-08-13.md' 'Display/source version \| `\d+\.\d+` is canonical' ('Display/source version | `{0}` is canonical' -f $Version)
Replace-Required 'docs/reinvention/07-execution-plan.md' '## Now \(\d+\.\d+\)' "## Now ($Version)"
Replace-Required 'docs/reinvention/07-execution-plan.md' 'Public version `\d+\.\d+`' "Public version ``$Version``"
Replace-Required 'docs/reinvention/MEMORY.md' 'Version: `\d+\.\d+`' "Version: ``$Version``"
Replace-Required 'MASTER_PLAN.md' 'product display \*\*v\d+\.\d+\*\*' "product display **v$Version**"

Write-Host "ANLAbel public release projections updated to $Version. Run build, unit tests and the application regression suite before release."
