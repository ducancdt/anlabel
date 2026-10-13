# Quy trình commit ANLAbel

Tài liệu này định nghĩa quy trình commit hợp lý cho ANLAbel: khi nào commit, gom
nhóm thế nào, viết message ra sao, và những gì tuyệt đối không được đưa vào repo.

## 1. Nguyên tắc cốt lõi

1. **Commit theo đơn vị logic, không theo thời gian.** Mỗi commit là một thay đổi
   hoàn chỉnh, tự đứng vững (build xanh, test xanh) — không trộn nhiều tính năng
   không liên quan vào một commit.
2. **Không commit khi build/test đỏ.** Mọi commit phải qua cổng kiểm thử ở mục 2.
3. **Không commit lên `master` trực tiếp.** Làm trên nhánh (`codex/...`,
   `feat/...`, `fix/...`) rồi mở PR.
4. **Chỉ commit mã nguồn và tài liệu dự án.** Dữ liệu khách hàng, tài liệu bên thứ
   ba, ảnh trích xuất, output build → không đưa vào repo (mục 5).
5. **Commit hoặc push chỉ khi người dùng yêu cầu.** Không tự ý push.

## 2. Cổng kiểm thử trước khi commit (bắt buộc)

Chạy tuần tự, tất cả phải xanh:

```bash
# 1. Build sạch (0 warning, 0 error)
dotnet build ANLAbel.slnx -c Debug

# 2. Unit tests
dotnet test src/ANLAbel.UnitTests/ANLAbel.UnitTests.csproj -c Debug --no-build

# 3. Integration/regression runner (WinExe console)
dotnet src/ANLAbel.Tests/bin/Debug/net8.0-windows/ANLAbel.Tests.dll
#    -> kiểm tra exit code = 0 và không có dòng bắt đầu bằng "FAIL"
```

Chỉ khi cả ba xanh mới tiếp tục. Mỗi tính năng/sửa lỗi nên đi kèm test hồi quy
trong cùng commit.

## 3. Gom nhóm commit

Chia working tree thành các commit theo **phân hệ**, mỗi commit gồm: mã nguồn +
test + tài liệu ADR/audit liên quan tới đúng phân hệ đó. Thứ tự đề xuất: các thay
đổi nền (Core/contracts) trước, rồi phân hệ dùng chúng, cuối cùng là commit
`release:` bump version + installer + docs tổng.

Lưu ý phụ thuộc chéo: `src/ANLAbel.Tests/Program.cs` là runner tích hợp dùng
chung nhiều phân hệ — nếu một commit thêm test tích hợp vào file này, phần hunk đó
đi cùng commit sở hữu tính năng tương ứng.

## 4. Định dạng message

Theo lịch sử repo:

```
<type>(<scope>): v<X.YYY> - <mô tả ngắn>

<thân bài: gạch đầu dòng những thay đổi chính, lý do, và test đã thêm>

Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>
```

- `type`: `feat` | `fix` | `release` | `docs` | `refactor` | `test` | `chore`
- `scope`: `print`, `barcode`, `data`, `ui`, `automation`, `figma`, ...
- `v<X.YYY>`: chỉ gắn version vào commit làm nên một bản phát hành người dùng thấy
  (thường là commit `release:` cuối cùng bump `eng/Version.props`). Các commit
  thành phần có thể bỏ version.
- Trailer `Co-Authored-By: <model đang dùng> <noreply@anthropic.com>` là bắt buộc
  với commit do agent tạo. Ví dụ: `Claude Opus 5.5`.

Version chuẩn nằm ở `eng/Version.props` (`ANLAbelReleaseVersion`); cổng regression
kiểm tra mọi projection khớp — dùng `scripts/Set-ANLAbelReleaseVersion.ps1` để
bump thay vì sửa tay nhiều nơi.

## 5. KHÔNG commit (loại trừ)

Các mục sau đang nằm trong working tree nhưng **không được** đưa vào repo — thêm
vào `.gitignore` nếu chưa có:

| Mục | Lý do |
|-----|-------|
| `TEM REDACTED-14 VN (1)/`, `CUSTOMER-1/`, `tem REDACTED-10/` | Dữ liệu nhãn của khách hàng |
| `form tem nhãn/`, `datasheet pf bfv.xlsx` | Mẫu tem và dữ liệu thật của khách hàng (kiểm tra chấp nhận chạy khi thư mục có trên máy; bỏ qua nếu không) |
| `images/` (ảnh trích từ NiceLabel guide) | Tài liệu bên thứ ba, bản quyền |
| `docs/assets/nicelabel-control-center/` | Ảnh trích từ hướng dẫn NiceLabel — tài liệu bên thứ ba, bản quyền |
| `ug-NiceLabel_2019_PowerForms_User_Guide-en.md` | Tài liệu bên thứ ba, bản quyền |
| `bin/`, `obj/`, `*.log`, `TestOutput/` | Output build (đã ignore) |

Kiểm tra nhanh trước khi `git add`:

```bash
git status --short | grep '^??'   # rà soát mọi file untracked trước khi add
```

Luôn `git add` theo đường dẫn cụ thể của phân hệ; **không** dùng `git add -A` /
`git add .` để tránh vô tình đưa dữ liệu khách hàng vào repo.

## 6. Quy trình rút gọn

```
1. Chạy cổng kiểm thử (mục 2) → phải xanh.
2. Rà file untracked, cập nhật .gitignore, loại trừ dữ liệu (mục 5).
3. Với từng nhóm phân hệ (mục 3):
     git add <đường dẫn cụ thể của nhóm>
     git commit -m "<message theo mục 4>"
4. Sau khi tách xong các commit thành phần, bump version + commit release:.
5. (Khi người dùng yêu cầu) push nhánh và mở PR.
```
