# Version

`eng/Version.props` is the one public version. The current public version is `0.366`.

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

- **Commit từng lát**: mỗi lát công việc chạy cổng kiểm thử rồi commit, không tăng
  version. Ghi chú cho phần chưa phát hành nằm dưới `## Sau v<hiện tại>` ở đầu
  `PLAN.md`. Chỉ push khi chủ dự án yêu cầu (`COMMIT_WORKFLOW.md`).
- **Bản phát hành cục bộ — bắt buộc cuối mỗi đợt**: khi xong một mục tiêu của chủ
  dự án, khi kết thúc phiên làm việc, và chậm nhất sau 10 commit thay đổi app
  (`src/` trừ hai dự án test, `installer/`). Nâng +0.001 bằng script, đổi mục
  `Sau` thành `## v0.xyz — …`, chạy cổng, commit `release: v0.xyz - …`, gắn tag
  `v0.xyz`, cài lên desktop.
- **Commit chỉ có tài liệu hoặc test** có thể nằm sau bản phát hành mà không cần
  version mới.
- **Trước khi báo xong**: `git log <bản phát hành cuối>..HEAD` không được còn
  commit thay đổi app. Cổng `unreleased app changes stay within the release
  cadence` đỏ khi quá 10 commit như vậy.
- **GitHub Release (Installer / Setup)**: Đóng gói bộ cài đặt Inno Setup `.exe` +
  `.zip` và phát hành lên GitHub Releases mỗi chu kỳ ~100 lần chỉnh sửa/cập nhật
  hoặc khi có yêu cầu phát hành chính thức — luôn cần chủ dự án đồng ý.

## Backfill 2026-10-30

95 commits (2026-10-28 to 2026-10-30) were made on an unchanged v0.325, so the
installed build kept saying v0.325. On the owner's request they were re-cut as
releases v0.326–v0.341: the unpushed commits were replayed unchanged (same
messages, authors and dates) with a `release:` commit after each batch, each
tagged and listed in `PLAN.md`. The previous history is kept as branch
`backup/unversioned-after-v0.325`.

One local desktop release. No editions. Spool accept is not physical print.
