# 12 — Khung kiến trúc Việt hóa 100% (tiếng Việt có dấu)

Trạng thái: đang thực thi. Chủ đề: chuyển toàn bộ chuỗi hiển thị của ANLAbel
sang **tiếng Việt có dấu**, loại bỏ **100% tiếng Anh** trong giao diện, và
đóng một **cổng test** chặn mọi hồi quy ngôn ngữ trên **mọi đối tượng UI**.

Đọc kèm: [`RULES.md`](RULES.md), [`../CORE_SOFTWARE_STANDARD.md`](../CORE_SOFTWARE_STANDARD.md),
[`../AUTOMATED_QUALITY_LOOP.md`](../AUTOMATED_QUALITY_LOOP.md).

---

## 1. Nguyên tắc kiến trúc

1. **Đơn ngữ, không đa ngữ.** Sản phẩm phục vụ vận hành công nghiệp trong nước.
   Không dựng tầng i18n/`.resx`/culture-switch — đó là hạ tầng thừa cho một
   ngôn ngữ duy nhất và trái nguyên tắc "keep work proportional" của repo. Chuỗi
   tiếng Việt nằm **tại chỗ** trong XAML/C#, đúng nơi lập trình viên đọc.
2. **Cưỡng chế bằng test, không bằng thiện chí.** Chất lượng ngôn ngữ được một
   scanner tự động canh giữ trên **mọi chuỗi hiển thị** (xem §4). Không có
   scanner thì "100% tiếng Việt" chỉ là khẩu hiệu.
3. **Phân định rõ chuỗi hiển thị và token kỹ thuật.** Không phải mọi ký tự Latin
   đều là lỗi. Tên riêng, mã, đơn vị, phím tắt được **allowlist** cho phép tồn
   tại (xem §3). Mọi từ tiếng Anh còn lại là **lỗi phải sửa**.
4. **Một nguồn thuật ngữ.** Bảng thuật ngữ chuẩn (§2) là từ điển bắt buộc. Cùng
   một khái niệm phải dùng đúng một từ tiếng Việt trên toàn app — "In", không
   lúc "In" lúc "Print" lúc "Xuất".
5. **Không đổi hành vi, chỉ đổi chữ.** Việt hóa là thay nhãn hiển thị. Không
   chạm `x:Name`, khóa binding, tên thuộc tính, ID máy in, tên file, hằng số
   giao thức. Đây là các mã định danh — dịch chúng là phá vỡ chức năng.

## 2. Bảng thuật ngữ chuẩn (trích)

Bảng đầy đủ được scanner nạp làm từ điển thay thế. Trích các cụm lõi:

| Tiếng Anh (cấm) | Tiếng Việt có dấu (chuẩn) |
|---|---|
| New / Open / Save | Tạo mới / Mở / Lưu |
| Undo / Redo | Hoàn tác / Làm lại |
| Print / Print All | In / In tất cả |
| Text / Text Box | Chữ / Ô chữ |
| Barcode | Mã vạch |
| Line / Rectangle / Ellipse | Đường kẻ / Hình chữ nhật / Hình bầu dục |
| Image | Hình ảnh |
| Workspace / Layers / Template / Objects | Không gian làm việc / Lớp / Mẫu / Đối tượng |
| Paper | Khổ giấy |
| Properties | Thuộc tính |
| Preview | Xem trước |
| Settings | Cài đặt |
| Queue / Print Queue | Hàng đợi / Hàng đợi in |
| Retry | Thử lại |
| Calibration | Hiệu chỉnh |
| History | Lịch sử |
| Export | Xuất |
| Cancel / OK / Close | Hủy / Đồng ý / Đóng |

## 3. Allowlist — token kỹ thuật được phép giữ nguyên

Được phép tồn tại trong chuỗi hiển thị vì là mã định danh, không phải văn xuôi:

- **Tên riêng/thương hiệu:** `ANLAbel`, `GitHub`, `Excel`, `NiceLabel`, `Windows`.
- **Mã/định dạng:** `CSV`, `QR`, `PDF`, `ZPL`, `GS1`, `DPI`, `ID`, `URL`, `RGB`.
- **Đơn vị:** `mm`, `cm`, `px`, `pt`, `dpi`.
- **Phím tắt:** `Ctrl`, `Alt`, `Shift`, `F1`–`F12`, `Ctrl+N` … (khối phím tắt).
- **Phiên bản/giấy phép:** `v0.288`, `GPL-3.0`.
- **Ký hiệu:** chữ cái đứng một mình đo lường (`x` trong `40 x 30 mm`).

Allowlist nằm cùng scanner (§4) — thêm token mới phải có lý do trong PR.

## 4. Bộ quy chuẩn test lỗi ngôn ngữ (scanner)

Vị trí: `src/ANLAbel.Tests` (runner đã có cơ chế đọc file nguồn theo repo root).

**Phạm vi quét (mọi đối tượng hiển thị):**

- XAML: thuộc tính `Content`, `Header`, `Text`, `ToolTip`, `Title`, `Watermark`,
  `Description`, `PlaceholderText` có giá trị **văn bản trực tiếp** (bỏ qua
  `{Binding}`, `{StaticResource}`, `{x:Static}`, chuỗi rỗng/khoảng trắng).
- C#: đối số chuỗi của `MessageBox.Show`, tiêu đề cửa sổ, và chuỗi trạng thái
  hiển thị cho người vận hành.

**Luật phán định một chuỗi:**

1. Tách allowlist token (§3), số, dấu câu, ký hiệu, phím tắt ra khỏi chuỗi.
2. Phần còn lại tách thành từ Latin `[A-Za-z]+`.
3. Chuỗi **vi phạm** nếu còn ≥1 từ Latin khớp **denylist tiếng Anh** (danh sách
   thực từ UI hiện tại: New, Open, Save, Print, Text, Barcode, Line, Layers,
   Template, Paper, Queue, Settings, …). Denylist tránh dương tính giả với từ
   tiếng Việt không dấu hợp lệ (`in`, `khi`, `cho`).
4. Báo cáo gom theo file → dòng → chuỗi → từ vi phạm, kèm gợi ý từ bảng §2.

**Kết quả:** test **fail** nếu còn bất kỳ vi phạm nào. Đây là cổng — "100%
tiếng Anh bị bỏ" chỉ đúng khi scanner xanh.

## 5. Quy trình chuyển đổi (slice nhỏ, có kiểm chứng)

1. Chạy scanner ở chế độ báo cáo → lấy bản kiểm kê vi phạm toàn app.
2. Chuyển từng file XAML theo thứ tự bề mặt lớn nhất: `MainWindow.xaml` trước.
3. Sau mỗi file: chạy scanner + build + unit test (theo
   [`anlabel-test-commands`]), rồi commit theo `docs/COMMIT_WORKFLOW.md`.
4. Bổ sung denylist khi phát hiện từ tiếng Anh mới; bổ sung allowlist khi gặp
   token kỹ thuật hợp lệ mới (có lý do).
5. Việt hóa xong toàn bộ XAML → chuyển sang chuỗi C# hiển thị.
6. Cổng đóng khi: scanner xanh + build xanh + unit/integration xanh trên toàn
   bộ 26 file XAML và các chuỗi C# hiển thị.

## 5b. Tiến độ (cập nhật liên tục)

- **Đã enforce xanh: 22/26 file XAML** — toàn bộ chuỗi hiển thị đã 100% tiếng
  Việt có dấu, scanner FAIL nếu tái xuất tiếng Anh: MainWindow, PrintPreview,
  PrinterSetup, DatabaseManager, LocalMaintenance, DataWorkspace, ExcelImport,
  OperationsOverview, PrintQueueConsole, PrintCenter, PrintHistory,
  TemplateRevision, AutomationConfiguration, Automation, DocumentLibrary,
  Analytics, DataSourceCleanup, DocumentWorkflow, ExcelHeaderRow, NewTemplate,
  ExcelSheet, TemplateLibrary. (~600 chuỗi.)
- **Còn lại:** `HelpWindow.xaml` (nút chuyển "English/Tiếng Việt") và
  `SettingsWindow.xaml` — thuộc **khung song ngữ**, xử lý cùng bước thu đơn ngữ.
  Rồi tới **chuỗi C# hiển thị** (`MessageBox.Show`, tiêu đề cửa sổ, chuỗi trạng
  thái). Con số sống ở dòng `[Việt hóa]` khi chạy runner.
- **Thu khung song ngữ về đơn ngữ Việt:** bỏ bảng `En` trong `UiCatalog`, bỏ
  `UiLanguage` + nút chuyển; cập nhật hai test `ui catalog matching en/vi keys`
  và `settings bilingual radios`.
- **Công cụ:** `scripts/viethoa_apply.py <xaml> <map.py>` (thay thế neo theo
  dòng+thuộc tính, an toàn với enum WPF). Mỗi file một `scripts/map_*.py`.
- **Cạm bẫy đã gặp:** vài test khớp cứng nhãn tiếng Anh → phải cập nhật theo nhãn
  Việt (padding presets, header tab máy in, mốc cắt `Text="Căn"` trong
  TextStylePicker). `git stash pop` khi `core.autocrlf=true` lật LF→CRLF, làm test
  đọc raw bytes (`\n`) đứt — khôi phục LF cho file bị lật.

## 6. Cấm — để không phá chức năng

Không dịch, không chạm: `x:Name`, `x:Key`, tên binding path, tên thuộc tính,
ID/queue máy in, tên sheet/cột dữ liệu, đường dẫn file, hằng số ZPL/GS1, khóa
`StaticResource`. Việt hóa dừng ở **nhãn người vận hành nhìn thấy**.
