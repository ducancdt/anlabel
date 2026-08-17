# Version

`eng/Version.props` is the one public version. The current public version is `0.262`.

Any user-visible code change increments `major.minor`, then
`scripts/Set-ANLAbelReleaseVersion.ps1` plus remaining app/installer
projections. The gate `release metadata stays synchronized` fails on drift.

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/Set-ANLAbelReleaseVersion.ps1 -Version <major.minor>
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/Invoke-ANLAbelQualityLoop.ps1 -Mode Fast
```

## Release Cadence
- **Incremental commits & Quality Loop**: Mỗi lần cập nhật code đều tăng version vi mô, chạy quality loop và push git.
- **GitHub Release (Installer / Setup)**: Đóng gói bộ cài đặt Inno Setup `.exe` + `.zip` và phát hành lên GitHub Releases mỗi chu kỳ ~100 lần chỉnh sửa/cập nhật hoặc khi có yêu cầu phát hành chính thức.

One local desktop release. No editions. Spool accept is not physical print.
