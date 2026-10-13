# Xác minh đối kháng Ma trận truy xuất nguồn gốc quy chuẩn

Ngày: 2026-09-14. Đối tượng: bảng "Ma trận truy xuất nguồn gốc quy chuẩn (Requirement Traceability Matrix)" trong [CORE_STANDARD_EVIDENCE_AUDIT.md](CORE_STANDARD_EVIDENCE_AUDIT.md), đối chiếu với mục 8 của [CORE_SOFTWARE_STANDARD.md](CORE_SOFTWARE_STANDARD.md).

Phương pháp: đọc trực tiếp source (`src/ANLAbel.UnitTests`, `src/ANLAbel.Tests/Program.cs` và các module impl). KHÔNG tin bảng gốc, KHÔNG chạy build/test. Chỉ xác minh: (a) tên test/gate được trích dẫn có thật hay không, (b) test có thật sự kiểm thứ mà yêu cầu tuyên bố hay không, (c) file impl được trích dẫn có tồn tại/chứa hành vi hay không.

Ký hiệu trạng thái:
- **CONFIRMED** — test/gate được trích dẫn tồn tại và kiểm đúng bản chất yêu cầu; file impl tồn tại.
- **WEAK** — một phần bằng chứng có thật nhưng ít nhất một artifact được trích dẫn (test class hoặc file impl) KHÔNG tồn tại, hoặc bị đặt sai tên; hàng chưa nên tuyên bố "Pass" nguyên trạng.
- **NOT-FOUND** — artifact trích dẫn không tồn tại và không có bằng chứng thay thế.

Lưu ý quan trọng chung: bảng gốc đánh dấu mọi hàng là "Pass". Mục 7 quy chuẩn nêu rõ "File test tồn tại chưa phải Pass" và mỗi ID cần hồ sơ input/expected/commit/kết quả chạy. Việc chỉ có test hiện diện (kể cả khi CONFIRMED ở đây) KHÔNG tự động chứng minh trạng thái "Pass" theo mục 7; xác minh này chỉ khẳng định sự tồn tại và độ phủ danh nghĩa của test, không phải kết quả chạy đã ghi hồ sơ.

## Ưu tiên: các hàng WEAK (trích dẫn sai — cần sửa bảng)

| ID | Tuyên bố của bảng | Verified? | Bằng chứng / khoảng trống |
| :--- | :--- | :--- | :--- |
| **B03** | Impl `Gs1BarcodeParser.cs`, `Gs1ApplicationIdentifierCatalog.cs`; gate `gs1 industrial AI subset validates weight and variable fields` | **WEAK** | Cả hai file/loại `Gs1BarcodeParser` và `Gs1ApplicationIdentifierCatalog` KHÔNG tồn tại trong toàn bộ source tree (0 occurrence). Impl GS1 thực tế nằm ở `src/ANLAbel.Core/Barcode/Gs1AiRegistry.cs`, `Gs1OfficialRegistrySnapshot.cs`, `Gs1OfficialRegistryBundle.cs`. Gate regression có thật: `Program.cs:221 -> TestGs1IndustrialAiSubset`. Kết luận: gate tồn tại nhưng hai file impl trích dẫn là hư cấu. |
| **B01** | Impl `BarcodeValidator.cs`, `BarcodeCheckDigitContract.cs`; test `BarcodeCheckDigitContractTests` + gate `code 39 wide narrow ratio...` | **WEAK** | `BarcodeValidator` KHÔNG tồn tại (0 occurrence). Validate/check-digit thực tế ở `src/ANLAbel.Core/Barcode/BarcodeCheckDigitContract.cs` và `BarcodeApplicationContract.cs`. Test `BarcodeCheckDigitContractTests.cs` có thật; gate `Program.cs:168 -> TestCode39RatioAndQuietZonePreflight` có thật. Một file impl trích dẫn không tồn tại. |
| **B07** | Impl `BarcodeSymbologyCatalog.cs`; bằng chứng: "Code128, Code39, EAN-13, ITF-14, QRCode, DataMatrix verified in suite" | **WEAK** | `BarcodeSymbologyCatalog` KHÔNG tồn tại (0 occurrence) — file impl trích dẫn cho "danh sách hỗ trợ lấy từ runtime" là hư cấu. Bằng chứng decode 6 symbology CÓ thật ở `ImportedBarcodeRoundTripTests.cs:22-25,80-83` (DataMatrix, Code39, Ean13, ITF, cộng Code128/QRCode). Chú ý sai lệch: bảng ghi "ITF-14" nhưng fixture dùng `BarcodeType.ITF` với payload `"1234567890"` (10 chữ số, không phải ITF-14). B07 yêu cầu danh sách lấy từ runtime — không có catalog runtime nào được chứng minh bởi file trích dẫn. |
| **D06** | Impl `ProjectFileService.cs`; gate `database config full round trip`, test `ProjectFileServiceTests` | **WEAK** | `ProjectFileServiceTests` KHÔNG tồn tại (0 occurrence; không có file trong `src/ANLAbel.UnitTests`). Impl `src/ANLAbel.Project/SaveLoad/ProjectFileService.cs` có thật. Gate `database config full round trip` có thật (`Program.cs:155 -> TestDatabaseConfigRoundTrip`). Một trong hai bằng chứng test được trích dẫn là hư cấu; độ phủ save/load geometry+font+binding+barcode options (D06) chỉ dựa một gate round-trip config, chưa đủ chứng minh toàn bộ invariant D06. |
| **P02** | Impl `PrinterCapabilityService.cs`, `PrinterMediaResolver.cs`; test `PrinterMediaResolverTests`, `PrinterCapabilityServiceTests` + gate discovery | **WEAK** | `PrinterCapabilityServiceTests` KHÔNG tồn tại (0 occurrence). `PrinterMediaResolverTests.cs` có thật; impl `PrinterCapabilityService.cs` và `PrinterMediaResolver` (trong `PrinterMediaContracts.cs`) có thật; gate `printer discovery keeps software stocks separate from driver media` có thật (`Program.cs:115`). Một test class trích dẫn là hư cấu. |

## Ghi chú độ chính xác tên file (không hạ trạng thái, nhưng bảng ghi sai đường dẫn)

| ID | Vấn đề | Thực tế |
| :--- | :--- | :--- |
| **DB08** | Bảng ghi impl `QuantityPlan.cs`, `PrintSequenceBuilder.cs` (không tồn tại dưới dạng file riêng) | Cả hai loại được định nghĩa trong `src/ANLAbel.Core/Printing/PrintPreviewContracts.cs` (`QuantityPlan` dòng 30, `PrintSequenceBuilder` dòng 63). Test `PrintPreviewContractsTests.InvalidRecordQuantityFailsClosed` (dòng 69) và `ExcessivePreparedLabelCountFailsBeforeAllocation` (dòng 96) có thật và kiểm đúng (throw khi copies không hợp lệ / vượt `MaximumCopiesPerRecord`). Hành vi đúng; chỉ tên file sai. |
| **P04** | Bảng ghi impl `LabelInstance.cs` (không tồn tại dưới dạng file riêng) | `LabelInstance` định nghĩa trong `PrintPreviewContracts.cs:46`. Test `PrintPreviewContractsTests.SelectedRowsAndCopiesProduceDeterministicStableInstances` (dòng 9) có thật, kiểm InstanceId `A:row-2:copy-1...` và SequenceOrdinal 1..6 — đúng yêu cầu record/copy identity. |

## Các hàng CONFIRMED

### Designer (D01–D07)
| ID | Verified? | Bằng chứng |
| :--- | :--- | :--- |
| **D01** | CONFIRMED | `ResizeGeometryContractTests.cs` tồn tại; gate `designer preview row keeps object geometry` (`Program.cs:148`). Impl `LabelGeometry.cs` tồn tại. |
| **D02** | CONFIRMED | Gate `canvas edit gesture is one undo step and cancel restores` (`Program.cs:152`). |
| **D03** | CONFIRMED | Gate `normal resize capture release does not cancel gesture` và `resize cancel restores the single-object frame` (`Program.cs:151`) đều tồn tại. |
| **D04** | CONFIRMED | Ba gate `Text stays free while TextBox stays bounded`, `Text shrink-frame compresses glyphs`, `text box does not resize object from text content` tồn tại trong `Program.cs`. |
| **D05** | CONFIRMED | Gate `designer preview row keeps object geometry` (`Program.cs:148`); impl `BindingExpressionEvaluator.cs`. Lưu ý: dùng chung một gate với D01. |
| **D07** | CONFIRMED | Gate `print preflight reports missing text font` (`Program.cs:136`) và `print preflight reports missing glyph coverage` (`:137`). |

### Excel/CSV & Binding (DB01–DB10)
| ID | Verified? | Bằng chứng |
| :--- | :--- | :--- |
| **DB01** | CONFIRMED | `CsvInputSafetyTests.cs`, `CsvAutomationSourceParserTests.cs` tồn tại; gate `excel preview rows use absolute row numbers and respect maxRows` (`Program.cs:208`). |
| **DB02** | CONFIRMED | `DataPayloadPreservationTests.cs`, `ExcelNumericPayloadTests.cs`, `ImportedBarcodeRoundTripTests.cs` đều tồn tại. |
| **DB03** | CONFIRMED | `ExcelDataServiceReliabilityTests.cs`; gate `print preflight blocks missing bound field` (`Program.cs:173`). |
| **DB04** | CONFIRMED | `ExcelErrorCellTests.cs` tồn tại (GetCellError). |
| **DB05** | CONFIRMED | Gates `excel read honors cancellation` (`:153`), `excel refresh skips unchanged file` (`:154`), `shared data source relink fixes every referencing template` (`:159`). |
| **DB06** | CONFIRMED | `FileSourceIdentityStaleProtectTests.cs`, `DataConnectorContractTests.cs` tồn tại; gate `linked excel file watcher flags stale data` (`:158`). |
| **DB07** | CONFIRMED | Gate `key field selection tracks row across refresh` (`:157`). |
| **DB08** | CONFIRMED* | Test đúng bản chất (xem trên); *tên file impl sai. |
| **DB09** | CONFIRMED | Gates `unlink excel clears database config but keeps bindings` (`:201`), `unlink excel works when link is broken` (`:202`). |
| **DB10** | CONFIRMED | Gate `print preflight blocks missing bound field` (`:173`); impl `AutomationPreparedBatchPreflightService.cs` tồn tại. |

### Barcode (B02, B04, B05, B06)
| ID | Verified? | Bằng chứng |
| :--- | :--- | :--- |
| **B02** | CONFIRMED | `ImportedBarcodeRoundTripTests.cs` tồn tại; impl `ZxingBarcodeRenderer` / `BindingExpressionEvaluator`. |
| **B04** | CONFIRMED | Gates `print preflight blocks undersized linear X-dim at print dpi` (`:164`), `barcode module size warning flags sub-2-dot modules` (`:162`); impl `DeviceBarcodeLayout.cs`, `MatrixSquareModuleFit.cs`. |
| **B05** | CONFIRMED | `MatrixSquareModuleFitTests.cs`; gate `linear barcode width follows quantized X-dim when SizedFromX` (`:167`). |
| **B06** | CONFIRMED | Gates `preview and print render the same geometry, offset by the plan` (`:177`), `compiled scene print uses SizedFromX production width` (`:171`). |

### Print (P01, P03–P08)
| ID | Verified? | Bằng chứng |
| :--- | :--- | :--- |
| **P01** | CONFIRMED | Gates `named queue resolve does not pick the Windows default` (`:125`), `missing named printer queue fails closed through lookup seam` (`:195`). |
| **P03** | CONFIRMED | Gates `effective output contract hash survives plan binding` (`:180`), `dispatch revalidation names output-contract drift and blocks submission` (`:213`); impl `PrintContractGuard.cs`. |
| **P04** | CONFIRMED* | `SelectedRowsAndCopiesProduceDeterministicStableInstances` đúng bản chất; *tên file impl `LabelInstance.cs` sai. |
| **P05** | CONFIRMED | Gate `dispatch revalidation uses full effective-output fields before PrintDocument` (`:218`); impl `DispatchRevalidationContract.cs`. |
| **P06** | CONFIRMED | Gates `print preview busy gate blocks duplicate dispatch` (`:193`), `async relay command rejects re-entry until the first call completes` (`:215`). |
| **P07** | CONFIRMED | Gates `spool accepted does not claim physical completion` (`:185`), `spool monitor preserves queue evidence and timeout semantics` (`:187`). |
| **P08** | CONFIRMED | Gates `print operation log records job-level trace` (`:175`), `print support evidence redacts raw payloads` (`:214`), `data operation log records import success and failure` (`:156`). |

## Tổng kết

- Tổng số hàng: 32 (D01–D07, DB01–DB10, B01–B07, P01–P08).
- **CONFIRMED: 27** (bao gồm DB08 và P04 có sai tên file impl nhưng test đúng bản chất).
- **WEAK: 5** — B01, B03, B07, D06, P02. Mỗi hàng có ít nhất một artifact được trích dẫn KHÔNG tồn tại trong source.
- **NOT-FOUND: 0** hàng bị mất hoàn toàn bằng chứng (mọi hàng WEAK vẫn còn bằng chứng thay thế thực).

Các artifact được trích dẫn nhưng KHÔNG tồn tại đâu trong source tree (xác nhận 0 occurrence): `Gs1BarcodeParser`, `Gs1ApplicationIdentifierCatalog`, `BarcodeValidator`, `BarcodeSymbologyCatalog`, `ProjectFileServiceTests`, `PrinterCapabilityServiceTests`. Ngoài ra `QuantityPlan.cs`, `PrintSequenceBuilder.cs`, `LabelInstance.cs` không tồn tại như file riêng (các loại nằm trong `PrintPreviewContracts.cs`).

Khuyến nghị: sửa bảng gốc để trỏ đúng file/test có thật, hoặc bổ sung các test/impl còn thiếu; và không tuyên bố "Pass" cho toàn bảng chỉ dựa vào sự hiện diện của gate — theo mục 7 cần hồ sơ input/expected/kết quả chạy cho từng ID.
