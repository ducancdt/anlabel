# Version

`eng/Version.props` is the one public version. The current public version is `0.311`.

The version changes only in a release commit (ADR-022, plan 13 QĐ-2):

1. Group green slices.
2. Run `scripts/Set-ANLAbelReleaseVersion.ps1`.
3. Commit `release: vX.YYY` with a changelog entry.
4. Install the build on the desktop.

Do not bump the version for each edit, and never leave a version that exists only
in the worktree. The gate `release metadata stays synchronized` fails on drift.

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/Set-ANLAbelReleaseVersion.ps1 -Version <major.minor>
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/Invoke-ANLAbelQualityLoop.ps1 -Mode Fast
```

## Release Cadence
- **Commit từng lát**: mỗi lát công việc chạy quality loop rồi commit, không tăng
  version. Chỉ push khi chủ dự án yêu cầu (`COMMIT_WORKFLOW.md`).
- **Bản phát hành cục bộ**: gom các lát đã xanh, tăng version, commit `release:`,
  cài lên desktop và ghi changelog.
- **GitHub Release (Installer / Setup)**: Đóng gói bộ cài đặt Inno Setup `.exe` + `.zip` và phát hành lên GitHub Releases mỗi chu kỳ ~100 lần chỉnh sửa/cập nhật hoặc khi có yêu cầu phát hành chính thức.

One local desktop release. No editions. Spool accept is not physical print.
