# Đối chiếu khoảng trống số học / ô lỗi Excel — data boundary

Ngày: 2026-09-16. Phạm vi: `src/ANLAbel.Data/Excel/ExcelDataService.cs`.
Quy chuẩn tham chiếu: [CORE_SOFTWARE_STANDARD.md](CORE_SOFTWARE_STANDARD.md), mục DB02 (không biến dạng mã) và DB04 (diagnostic ô lỗi).
Bối cảnh: tài liệu này ghi **hành vi hiện tại** của `ExcelDataReader` 3.7.0 trên workbook thật, không dùng audit 2026-09-14 làm chứng cứ lỗi runtime.

## Phương pháp

Tái hiện bằng workbook OOXML thật (ClosedXML + chỉnh XML, và workbook ZIP tối thiểu) rồi đọc bằng cùng đường `ExcelDataService` / `ExcelDataReader.ExcelOpenXmlReader` 3.7.0. Probe tạm: `TestOutput/ExcelReliabilityProbe`.

Không đánh giá công thức. Không tải engine tính toán thứ hai. Không chặn toàn bộ công thức. Không thêm importer song song. Policy General `R`, date/bool invariant, và number-format authored hợp lệ **không** đổi.

## Kết quả đã chạy (2026-09-16)

| Input workbook | Reader 3.7.0 | Import (`LoadSheetAsync`) | Preview (`PreviewRowsAsync` / `GetSheetsWithPreviewAsync`) |
| --- | --- | --- | --- |
| Ô trống không có node `<c>` | `GetValue=null`, `IsDBNull=true`, `GetCellError=null`, `GetNumberFormatIndex=-1` | `""` | `""` |
| Ô trống có `<c r="A2"/>` | `GetValue=null`, `IsDBNull=true`, `GetCellError=null`, format `General` | `""` | `""` |
| Công thức không cache (`<f>1+1</f>`, không `<v>`) | Giống ô trống: `GetValue=null`, `GetCellError=null`; **không** giữ `<f>` | `""` | `""` |
| Công thức `t="str"` không cache | Giống ô trống | `""` | `""` |
| Công thức trả chuỗi rỗng (`<f>""</f><v></v>`) | Giống ô trống (`<v>` rỗng bị bỏ trước khi convert) | `""` | `""` |
| Công thức cache chuỗi `" 000123 "` | `GetValue=" 000123 "` | `" 000123 "` (giữ khoảng trắng) | giống import |
| Công thức cache số `123` format `000000` | `GetValue=123d`, format `000000` | `"000123"` | `"000123"` |
| Công thức lỗi `#DIV/0!` | `GetCellError=DIV0` | `InvalidData` kèm file/sheet/row/column | `#DIV/0!` |
| Số `123` format `000000` hợp lệ | format `000000` | `"000123"` | `"000123"` |
| `numFmtId=164` **không** có `numFmts` | `GetNumberFormatString` **không** ném; trả `General` | `"123"` (mất padding) | `"123"` |
| `s="999999"` / `s="-1"` / không `styles.xml` | index 0 / `General`; **không** ném | `"123"` | `"123"` |

`GetNumberFormatString` trên các workbook này **không** ném `IndexOutOfRangeException` hay `NotSupportedException`. Catch trong `FormatNumber` vì vậy **không** chạy trên input reader thật đã thử. `CommonWorkbook.GetNumberFormatString` (ExcelDataReader 3.7.0) trả `"General"` khi index không có trong styles/builtin.

## Ranh giới API — không đoán

`IExcelDataReader` 3.7.0 chỉ công bố giá trị cache, `GetCellError`, và number-format. `XmlWorksheetReader.ReadCell` **bỏ** phần tử `<f>` (`SkipContent`) và chỉ convert `<v>`/`<is>` khác rỗng. Vì vậy pipeline hiện tại **không** phân biệt an toàn:

1. ô trống thật,
2. công thức chưa có cached result,
3. công thức đã tính ra chuỗi rỗng.

Chặn mọi `GetValue==null` sẽ biến ô trống và công thức trả rỗng thành `InvalidData`. Không chặn thì (2) vẫn thành payload rỗng. Cả hai đều sai DB04 nếu tuyên bố đã đóng. **Không** sửa bằng heuristic null/format-index. Sửa an toàn chỉ khi reader (hoặc seam hiện có) giữ sự hiện diện của `<f>` và `<v>`.

Lookup format hỏng **có thể** làm mất padding `000123` → `123`, nhưng nguyên nhân nằm **trong reader** (normalize về `General`), không phải exception ANLAbel nuốt được. Fail-closed trên chuỗi `"General"` sẽ phá numeric General hợp lệ. Không fail-closed trên giả thuyết catch.

## Trạng thái DB02 / DB04

| Mục | Trạng thái | Ghi chú |
| --- | --- | --- |
| Công thức cache hợp lệ (chuỗi, số padded, lỗi `#DIV/0!`) | Giữ / kiểm thử | `ExcelErrorCellTests.CachedFormula*` |
| Ô lỗi Excel không-công-thức | Đã đóng trước đó | import fail-closed, preview token |
| Công thức thiếu cache vs ô trống vs cache rỗng | **Không đóng DB04** | API reader không phân biệt |
| Tra number-format ném exception | **Không tái hiện** | catch dead trên probe thật |
| `numFmtId` dangling / style hỏng → mất padding | **Không đóng DB02** | reader trả `General` trước ANLAbel |
| General `R` / scientific | Policy hiện hành, **không đổi** | `0.00000000001` → `1E-11` |
| Date ISO / bool TRUE/FALSE / invariant group separator | Policy hiện hành, **không đổi** | |

## Regression giữ lại

- `ExcelNumericPayloadTests.NumericFormatPreservesAuthoredDisplayPayload` — format authored hợp lệ, kể cả `000000`.
- `ExcelErrorCellTests.CachedFormulaPreservesPayloadInImportAndBothPreviews` — cache chuỗi (kể cả rỗng và whitespace), cache số padded.
- `ExcelErrorCellTests.CachedFormulaErrorRejectsImportWithSourceCoordinatesAndRemainsVisibleInPreviews` — lỗi công thức có cache.

Không giữ test “lookup ném exception”: không có workbook/reader thật sinh exception đó. Không giữ test bắt buộc reject mọi formula-null: sẽ đụng ô trống và cache rỗng hợp lệ.

## Việc không làm

Không đánh giá công thức. Không ClosedXML DOM importer. Không chặn mọi công thức. Không đổi policy General/date/bool. Không sửa Text/TextBox. Không bump version: hành vi thực thi không đổi.

## Cổng chất lượng đã chạy (2026-09-16)

Không suy ra từ lịch sử. Lệnh:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/Invoke-ANLAbelQualityLoop.ps1 -Mode Fast
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/Invoke-ANLAbelQualityLoop.ps1 -Mode Mutation
```

- Fast: build 0 warning/0 error; unit 1050 passed; application regressions PASS, gồm bảy gate Text/TextBox named. `ANLAbel Fast quality loop passed.`
- Mutation (label-safety list hiện hành, ngưỡng 90 không đổi): score **94.80%** (killed 652, survived 31, timeout 4). `ANLAbel Mutation quality loop passed.` Báo cáo: `src/ANLAbel.UnitTests/StrykerOutput/2026-09-16.08-28-30/reports/mutation-report.md`.
- Mutation không mutate `ExcelDataService`; điểm này không đóng DB02/DB04.
