# ADR: Chính sách xử lý lỗi cho trình phân tích CSV (CSV Parser Failure Policy)

- Trạng thái: Đề xuất (Proposed)
- Ngày: 2026-09-14
- Phạm vi: `CsvAutomationSourceParser` so với đường nhập CSV tương tác trong `ExcelDataService`

## Context (Bối cảnh)

Hệ thống hiện có HAI trình phân tích CSV với chính sách lỗi KHÁC nhau:

1. **Fail-open (bỏ dòng lỗi)** — `src/ANLAbel.Data/Automation/CsvAutomationSourceParser.cs`
   - Một dòng dữ liệu hỏng (sai số cột hoặc sai cú pháp dấu ngoặc kép) bị **bỏ qua** và ghi một chuỗi chẩn đoán (diagnostic), nhưng các dòng hợp lệ vẫn được trả về.
   - Cụ thể: `FormatException` khi đọc dòng được nuốt tại dòng 115-125 (`catch (FormatException ex) { AddDiagnostic(...); continue; }`), và số cột sai bị bỏ tại dòng 155-159 (`if (values.Count != headers.Count) { AddDiagnostic(...); continue; }`).
   - Chỉ lỗi ở cấp **header** mới khiến trả về rỗng (dòng 83-101: header hỏng, rỗng, chứa tên trường trống, hoặc trùng tên). Ở cấp **dòng dữ liệu**, chính sách là bỏ dòng chứ không dừng lô.
   - Ngoài chính sách fail-open cấp bản ghi ở trên, parser hiện còn **fail-closed** ở một loạt biên nghiêm ngặt khác: phát hiện BOM và ép encoding chặt (`UTF-8`/`UTF-16`/`UTF-32`), từ chối cả nguồn khi gặp byte không hợp lệ (`DecoderFallbackException`); chặn dòng trống trong nguồn một cột (blank row không bị âm thầm bỏ); từ chối header trùng tên hoặc có tên trường rỗng; và áp các hằng số giới hạn chống DoS: `MaxInputBytes` = 50 MB, `MaxRecords` = 1.000.000, `MaxScannedRecords` = 2.000.000, `MaxFieldsPerRow` = 4.096, `MaxFieldLength` = 1.000.000, `MaxRecordLength` = 10.000.000, `MaxDiagnostics` = 100. Các biên này bổ sung cho, chứ không thay thế, quyết định lõi (fail-open cấp bản ghi + fail-closed cấp lô ở người gọi).

2. **Fail-closed (từ chối cả tệp)** — đường tương tác `LoadCsv` / `ReadCsvRecord` trong `src/ANLAbel.Data/Excel/ExcelDataService.cs`
   - Trả về / ném `ExcelDataReadError.InvalidData` khi gặp dữ liệu hỏng: dấu ngoặc kép chưa đóng (dòng 543), dấu ngoặc kép bất thường (dòng 561), ký tự sau dấu ngoặc kép đóng (dòng 599), và dòng có nhiều trường hơn header (dòng 392-396).
   - Lưu ý: đường tương tác cũng không hoàn toàn nghiêm ngặt về số cột — nó **ném lỗi khi dòng có NHIỀU cột hơn header** nhưng **âm thầm đệm chuỗi rỗng khi dòng có ÍT cột hơn** (dòng 400-405). Đây là một điểm bất đối xứng riêng, không thuộc phạm vi quyết định này nhưng cần ghi nhận.

Kế hoạch thực thi `docs/reinvention/07-execution-plan.md` bắt buộc: *"Fail closed on missing queue, stale data, or ambiguous print outcome."* Trong sản xuất tem công nghiệp, một dòng bị bỏ âm thầm = một tem vật lý bị thiếu mà không có điểm dừng cứng (hard stop). Đây chính xác là loại "ambiguous outcome" mà triết lý fail-closed muốn loại trừ.

### Sự thật đã xác minh về việc tiêu thụ parser (verified usages)

`grep` cho `CsvAutomationSourceParser` chỉ trả về các nơi sau:

- `src/ANLAbel.Data/Automation/CsvAutomationSourceParser.cs` — định nghĩa.
- `src/ANLAbel.Data/Automation/FileDropCsvPreparationService.cs` — **người tiêu thụ thật duy nhất**.
- `src/ANLAbel.UnitTests/CsvAutomationSourceParserTests.cs` — test.
- `src/ANLAbel.Tests/CsvAutomationSourceParserRegression.cs` — test.
- `docs/CORE_STANDARD_EVIDENCE_AUDIT.md`, `docs/reinvention/11-verification-checkpoint-2026-08-13.md` — tài liệu.

**KHÔNG có đường dispatch (in ấn / hàng đợi / manifest) nào hiện đang tiêu thụ parser này.** `FileDropCsvPreparationService.TryPrepare` (dòng 17-41) chỉ đọc bản ghi vào bộ nhớ và ghi rõ trong thông điệp: *"document binding, manifest, queue and dispatch remain unavailable."*

Điểm mấu chốt: **người gọi duy nhất (`FileDropCsvPreparationService`) đã cổng hóa (gate) parser theo kiểu fail-closed ở cấp lô.** Tại dòng 25-31, nếu `parsed.Diagnostics.Count != 0` thì toàn bộ sự kiện được chuyển sang trạng thái `Blocked` và `TryPrepare` trả về `false` — không có bản ghi nào được giao. Vậy trên thực tế, bất kỳ dòng hỏng nào cũng chặn cả lô ngày hôm nay, thông qua người gọi chứ không phải qua parser.

## Decision (Quyết định)

**Giữ `CsvAutomationSourceParser` ở dạng fail-open ở cấp bản ghi, NHƯNG bắt buộc mọi người gọi phải cổng hóa fail-closed ở cấp lô (như `FileDropCsvPreparationService` đang làm).**

Lý do chọn phương án này thay vì làm parser tự fail-closed:

1. Tách bạch trách nhiệm (separation of concerns): parser báo cáo sự thật (records + diagnostics); người gọi quyết định chính sách. Điều này cho phép các bối cảnh không-dispatch (ví dụ xem trước / chẩn đoán) hiển thị cả dòng tốt lẫn lý do dòng xấu.
2. Người gọi hiện tại **đã** thực thi fail-closed đúng như triết lý, nên không có lỗ hổng thực tế hôm nay.
3. Việc để parser trả về diagnostics giàu thông tin (số dòng vật lý, số cột thực tế) tốt cho việc chẩn đoán hơn là ném một ngoại lệ đơn lẻ dừng ở dòng đầu tiên gặp lỗi.

**Điều kiện bắt buộc (invariant) đi kèm quyết định:** không được có đường dispatch nào tiêu thụ `AutomationSourceParseResult.Records` mà bỏ qua `Diagnostics`. Bất kỳ người gọi mới nào cũng PHẢI coi `Diagnostics.Count != 0` là chặn lô cứng, giống dòng 25-31 của `FileDropCsvPreparationService`.

### Phương án thay thế (nếu invariant trên không thể đảm bảo bằng review)

Nếu tổ chức không tin tưởng vào kỷ luật review để đảm bảo mọi người gọi tương lai đều kiểm tra `Diagnostics`, thì chuyển parser sang fail-closed để "an toàn theo mặc định" (safe-by-default). Đây là lựa chọn phòng thủ hơn và phù hợp chặt chẽ với đường tương tác. Dưới đây là bản phác thảo thay đổi cho phương án này.

## Bản phác thảo thay đổi để làm parser FAIL-CLOSED (KHÔNG áp dụng ở đây)

Mục tiêu: bất kỳ dòng hỏng nào (sai cú pháp HOẶC sai số cột) đều từ chối toàn bộ lô thay vì bỏ dòng.

Tệp: `src/ANLAbel.Data/Automation/CsvAutomationSourceParser.cs`

1. **Dòng 115-125** (bắt `FormatException` khi đọc dòng): thay vì thêm diagnostic và `continue`, dừng và trả về kết quả rỗng-có-lỗi. Ví dụ:

   ```csharp
   catch (FormatException ex)
   {
       return new([], [$"CSV row {recordStartLine}: {ex.Message}"]);
   }
   ```

   (Với hợp đồng "records rỗng khi có diagnostic" thì người gọi vẫn dùng được; hoặc đổi kiểu trả về sang một `bool Success` để rõ ràng hơn.)

2. **Dòng 155-159** (số cột không khớp header): thay `continue` bằng trả về ngay:

   ```csharp
   if (values.Count != headers.Count)
   {
       return new([], [$"CSV row {recordStartLine} has {values.Count} field(s); expected {headers.Count}."]);
   }
   ```

3. Hệ quả về hợp đồng: khi fail-closed, bất biến mới là *"Records không rỗng ⟺ Diagnostics rỗng"*. Cần cập nhật XML doc của `Parse` và các test:
   - `src/ANLAbel.UnitTests/CsvAutomationSourceParserTests.cs`
   - `src/ANLAbel.Tests/CsvAutomationSourceParserRegression.cs`

   (Các test này hiện có thể khẳng định "dòng tốt vẫn trả về khi có dòng xấu" — sẽ cần đổi kỳ vọng. KHÔNG chỉnh sửa trong ADR này.)

Lưu ý: KHÔNG áp dụng thay đổi trên trong nhiệm vụ này. Đây chỉ là bản phác thảo.

## Consequences (Hệ quả)

- **Nếu giữ fail-open + cổng người gọi (quyết định chính):** không thay đổi mã. Rủi ro còn lại là một người gọi tương lai có thể đọc `Records` mà quên kiểm tra `Diagnostics` → dòng bị thiếu âm thầm trong dispatch. Rủi ro này được giảm thiểu bằng invariant ghi trong ADR và bằng review; cần một test/độ bảo vệ để bắt các người gọi vi phạm.
- **Nếu chuyển fail-closed (phương án thay thế):** an toàn theo mặc định, khớp đường tương tác và triết lý 07-execution-plan; đánh đổi là mất khả năng "báo cáo một phần" và cần cập nhật test. Một lô lớn bị từ chối chỉ vì một dòng hỏng — đúng mong muốn với tem công nghiệp, nhưng cần thông điệp lỗi rõ để người vận hành sửa nguồn.
- Trong cả hai trường hợp, sự bất đối xứng "dòng ít cột hơn bị đệm âm thầm" ở đường tương tác (`ExcelDataService` dòng 400-405) vẫn tồn tại và nên được xem xét riêng.

## Non-claims (Những điều KHÔNG khẳng định)

- Không khẳng định parser tự động này đã được nối vào bất kỳ luồng in / hàng đợi / manifest nào. Đã xác minh: **chưa có**.
- Không khẳng định đường tương tác `ExcelDataService` là hoàn toàn nghiêm ngặt về số cột (nó đệm dòng thiếu cột).
- Không khẳng định đã thay đổi bất kỳ tệp `.cs` hay test nào; ADR này chỉ là quyết định và bản phác thảo.
- Không khẳng định đã chạy build hoặc test.
