# Version

`eng/Version.props` is the one public version. The current public version is `0.260`.

Any user-visible code change increments `major.minor`, then
`scripts/Set-ANLAbelReleaseVersion.ps1` plus remaining app/installer
projections. The gate `release metadata stays synchronized` fails on drift.

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/Set-ANLAbelReleaseVersion.ps1 -Version <major.minor>
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/Invoke-ANLAbelQualityLoop.ps1 -Mode Fast
```

One local desktop release. No editions. Spool accept is not physical print.
