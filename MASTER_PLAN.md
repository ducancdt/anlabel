# ANLAbel — Master Plan

Tai lieu nay la ban do tong quan, cap nhat theo tung dot lam viec lon. `PLAN.md` la nhat ky chi tiet tung buoc/tung phase da lam (giu nguyen, khong xoa). `agent.md` la quy dinh bat buoc cho agent. `docs/audit-2026-07-02.md` la bao cao audit chi tiet dot nay.

## San pham la gi

ANLAbel la phan mem thiet ke & in tem nhan (label designer) cho may in tem nhan cong nghiep (Zebra, TSC, Godex, SATO, Argox, Honeywell, Intermec, Citizen, Toshiba TEC...), khong phai may in van phong. Kien truc WPF/.NET 8, MVVM, luu template `.anlabel` (JSON), binding du lieu tu Excel, ho tro barcode/QR/Data Matrix, print pipeline vector rieng cho driver may in tem.

## Trang thai hien tai (2026-07-02)

- Version hien thi trong app: `0.061`.
- Build: `dotnet build ANLAbel.slnx` PASS (0 loi). Test: `ANLAbel.Tests` 25/25 PASS; `ANLAbel.UnitTests` 33/33 PASS. Smoke: `ANLAbel.App.exe` (debug) khoi dong thanh cong, khong exception.
- Luong release chinh: `build-trial.ps1` / `build-license-system.ps1` -> `dotnet publish` -> `publish_out/{trial-x64,commercial-x64,license-master-x64}` -> Inno Setup (`installer/ANLAbel-Trial-x64.iss`, `ANLAbel-Commercial-x64.iss`, `ANLAbel-License-Master-x64.iss`) -> `releases/`.
- Deploy nhanh de test tren may dev: `deploy-desktop.ps1` (publish self-contained win-x64 -> robocopy vao `%LOCALAPPDATA%\Programs\ANLAbel`).

## Cac plan chi tiet

- `docs/audit-2026-07-02.md` — bao cao audit 2 bug + 5 van de phu.
- `docs/database-plan.md` — plan quan ly du lieu database/Excel dau vao (3 giai doan: sua ho loi "mat link" bang relative path + re-link UI; data source manager dung chung + FileSystemWatcher + KeyField; CSV/lazy-load/nguon khac).
- `docs/designer-stability-plan.md` — plan sua bug object tu nhay vi tri/kich thuoc trong designer (nguyen nhan goc: duong render ghi nguoc vao model — text auto-fit, matrix ep vuong, QR auto-size deu mutate WidthMm/HeightMm khi ve lai/doi PreviewRow; 6 buoc sua, can duyet vi co thay doi UX auto-fit). Dot 1 da lam xong (muc 10 ben duoi).
- `docs/print-preview-reliability-plan.md` — plan siet do tin cay In & Preview (preflight tung dong, kiem tra du lieu tuoi truoc khi in, test WYSIWYG 3 duong render, khop DPI barcode voi may in, bao cao lo in + chong trung tem). MOI 2026-07-02.
- `docs/properties-panel-plan.md` — audit + sap xep lai Properties panel (them Position & Size X/Y/W/H mm, Shape Style, mau chu; gop 3 card binding trung lap; Formula Builder thanh Expander). MOI 2026-07-02.

## Da xong trong dot audit nay

1. **Fix treo khi Import Excel**: `ExcelImportWindow.Browse_Click` chuyen `GetExcelSheetNames` sang chay nen (`Task.Run`) + wait cursor, khong con block UI thread. Chi tiet: `docs/audit-2026-07-02.md` muc 1.
2. **Fix template thu vien mat sample Excel/link**: 5 template `20_..24_...anlabel` (Deltec/Jabil) da duoc genericize (bo ten/ma khach hang, chi giu placeholder chung + bind vao `sample-data.xlsx`), bo khoi `Exclude` trong `ANLAbel.App.csproj` de duoc dong goi va hien trong Template Library nhu 12 template con lai. Chi tiet: `docs/audit-2026-07-02.md` muc 2.
3. Them 2 rule moi vao `agent.md` (so 7: cam I/O dong bo tren UI thread; so 8: quy tac genericize du lieu khach hang trong template thu vien) de tranh lap lai 2 loi tren.
4. Ghi lai audit report rieng: `docs/audit-2026-07-02.md`, liet ke them 5 van de phu (git index co ~1000 file build artifact bi add nham, file rac o root, installer legacy 0.042 con sot lai, version rai rac nhieu noi, chua co huy/timeout khi doc Excel qua mang).
5. **Don dep repo/git**: `git restore --staged publish_out/` (984 file), them `publish_out/` vao `.gitignore`, xoa 5 file rac o root, xoa 2 installer legacy 0.042. Commit `9939dda`, `12a30e5`.
6. **Dong bo version**: tao `build-common.ps1` doc `Version` tu `.csproj`, sua `build-trial.ps1` + `build-license-system.ps1` goi `build-common.ps1`, sua `deploy-desktop.ps1` lay version tu `.csproj`, sua 3 installer `.iss` dong bo version `0.057`. Commit `6212fa0`.
7. **Ra soat UI thread + Cancel Excel import**: kiem tra printer enumeration (OK — local nhan), them `CancelButton` + `CancellationTokenSource` cho `ExcelImportWindow` khi doc file Excel qua mang. Commit `f9f4047`.
8. **Template Library unit test**: them test `template library links sample-data.xlsx` kiem tra tat ca `*.anlabel` trong `TemplateLibrary/` co `DatabaseConfig.FilePath` tro toi `sample-data.xlsx`. 18/18 test PASS. Commit `69549a2`.
9. **GĐ1 database-plan — sua ho loi "mat link" Excel** (`docs/database-plan.md` muc 1-3, 3b):
   - `DatabaseConfig.RelativePath` + `ResolveExcelPath()` (absolute → relative → same dir → broken) trong `MainViewModel.cs`.
   - `UpdateRelativePath()` tu tinh RelativePath khi Save/Import Excel.
   - `RelinkExcelAsync()` + nut re-link khi link bi hong.
   - `RestoreLinkedExcelDataAsync()` tu dong restore Excel data khi mo template.
   - `ExtractSampleData()` bi xoa khoi `TemplateLibraryService.cs` — template thu vien khong ghi de `sample-data.xlsx` nua (muc 3b).
   - Test `template excel link survives folder move`: tao template + Excel cung thu muc, copy sang vi tri khac, xoa goc, mo lai → link tu dong resolve bang RelativePath. 19/19 test PASS.
10. **On dinh designer — dot 1** (`docs/designer-stability-plan.md`):
   - Render/doi `PreviewRow` khong con ghi nguoc `WidthMm/HeightMm` cua text/QR vao model.
   - Matrix barcode chi ep vuong khi property kich thuoc thay doi; giu tam cho chieu duoc app tu dieu chinh.
   - Snap giam tu `3 mm` xuong `1 mm`, giu `Alt` de tam tat snap, clamp drag du 4 canh.
   - Mat mouse capture hoac bam `Esc` khi dang keo se khoi phuc ca nhom object, khong de teleport o lan keo sau.
   - Them regression test `designer preview row keeps object geometry`.
   - Smoke app PASS: process responsive, title `ANLAbel - Label Designer v0.058`.
11. **Nen GĐ2 database-plan + I/O Excel an toan hon**:
   - Them model `DataSource`, registry JSON tai `%AppData%\ANLAbel\data-sources.json`, va `DatabaseConfig.DataSourceId` (chua noi UI manager).
   - Ghi nho row bang `KeyField` + `KeyValue`, fallback ve `LastSelectedRow` neu key khong con.
   - Doc Excel ho tro `CancellationToken`, timeout 30 giay cho UNC/network va `FileShare.ReadWrite`; luong re-link khong con doc workbook tren UI thread.
12. **Properties panel — Dot A** (`docs/properties-panel-plan.md` muc 6), v0.059:
   - Card moi "Position & Size (mm)" (X/Y/Width/Height; X1/Y1/X2/Y2 rieng cho Line) bind truc tiep vao model co san, khong can property ViewModel moi.
   - Card moi "Shape Style" cho Rectangle/Ellipse/Line (Fill, Corner radius, Outline, Thickness) + mau chu (StrokeColor) trong card Text Style — deu co hex TextBox + swatch xem truoc.
   - Moi o so moi dung `UpdateSourceTrigger=LostFocus` + handler `PositionSizeTextBox_KeyDown` (Enter de commit) — dung rule 9 `agent.md`, tan dung undo debounce co san (khong can code undo rieng).
   - Build PASS, `ANLAbel.Tests` 22/22 PASS, `ANLAbel.UnitTests` 31/31 PASS, smoke app PASS. Version `0.058` → `0.059`.
13. **Database plan — Giai doan TC (bat dau)**, v0.060:
   - Audit lai code truoc khi lam moi phat hien TC1 (bao cao schema) va TC3 (trang thai link tuong minh) **da co san phan lon** tu truoc (panel Binding Issues + `GetBindingIssues()`, panel Database + `LinkedExcelSourceText`/`ExcelLinkStatusText`/`RelinkExcelCommand`) — tranh lam trung lap, chi bo sung phan con thieu.
   - TC1: `StatusText` sau Import/Refresh Excel gio noi them so object co van de binding (vd "... — 2 object(s) have missing/broken bindings").
   - TC3+TC4: them `ExcelDataFreshnessText` ("Data read at HH:mm:ss") hien trong panel Database; `RefreshExcelDataAsync()` so `TryGetFileWriteTimeUtc()` voi lan doc truoc — file chua doi thi bo qua doc lai (`"...already up to date..."`), file da doi moi doc lai nhu cu.
   - Test moi: `excel refresh skips unchanged file` (kiem ca nhanh skip va nhanh doc lai khi file thay doi).
   - Build PASS, `ANLAbel.Tests` 23/23 PASS (them 1 test), `ANLAbel.UnitTests` 31/31 PASS, smoke app PASS. Version `0.059` → `0.060`.
   - Chi tiet + phan con thieu (TC2/TC5/TC6/TC7): `docs/database-plan.md` muc "Giai doan TC".
14. **Designer interaction — snap/nudge** (`docs/designer-stability-plan.md`), v0.061:
   - Toggle `Snap objects` tren ribbon va context menu canvas; Alt tam bypass snap van giu nguyen.
   - Preference luu rieng theo may trong `designer-preferences.json`, khong lam ban template.
   - Keyboard nudge hien toa do moi tren status bar; group nudge hien so object da di chuyen.
   - xUnit preference round-trip/corrupt JSON PASS; UI Automation toggle On → Off → On va file preference doi dung theo; app responsive title `v0.061`.
15. **Database plan — Giai doan TC (tiep tuc: TC2 + TC6)**, v0.061:
   - TC2: test moi `database config full round trip` — populate day du `DatabaseConfig` (DataSourceId, FilePath, RelativePath, SheetName, HeaderRowIndex, KeyField, KeyValue, LastSelectedRow, AvailableFields, LabelFields) qua `ProjectFileService`, assert khong mat field nao sau save/open.
   - TC6: model + service moi `DataOperationLogEntry`/`DataOperationLogService` (`src/ANLAbel.Data/DataLogs/`) ghi JSON-lines vao `%LocalAppData%\ANLAbel\logs\data-operations.jsonl` moi lan Import/Refresh/Relink/Open-restore — fire-and-forget, khong bao gio chan hoac lam hong thao tac du lieu chinh du ghi log that bai. Noi vao diem hoi tu chung `MainViewModel.ImportExcelAsync` (private overload nhan `operation` label: Import/Refresh/Relink/Open).
   - Test moi: `data operation log records import success and failure` (kiem ca nhanh thanh cong va nhanh that bai).
   - Build PASS, `ANLAbel.Tests` 23/23 → 25/25 PASS (them 2 test), `ANLAbel.UnitTests` 33/33 PASS, smoke app PASS. Version `0.060` → `0.061`.
   - Con lai trong Giai doan TC: TC5 (UI chon KeyField — backend da co san), TC7 (test them cac ca hong: mat file giua chung, sheet doi ten, file .xlsx hong). Chi tiet: `docs/database-plan.md`.

## Dinh huong moi tu chu du an (2026-07-02, chieu) — thu tu uu tien

1. **Day manh phan gan database + siet tin cay database**: dang lam "Giai doan TC" trong `docs/database-plan.md`. Da xong TC1, TC2, TC3, TC4, TC6 (xem muc 13, 15 ben duoi). Con lai TC5 (UI chon KeyField — backend da co san) va TC7 (test them cac ca hong) truoc khi mo rong GD3.
2. **Siet tin cay In & Preview**: theo `docs/print-preview-reliability-plan.md` — dot 1 (preflight tung dong + kiem tra du lieu tuoi + chuan hoa print log) lam truoc, gan chat voi Giai doan TC cua database.
3. **Template thu vien KHONG tu gan Excel nua**: DA XONG (commit `c3da135`) — sample-data.xlsx bi go khoi bundle, DatabaseConfig cua 17 template de trong, test `template library standalone (no sample-data link)` bao ve. Rule 8 `agent.md` da cap nhat theo quyet dinh nay.
4. **Kiem tra & sap xep lai Properties panel**: theo `docs/properties-panel-plan.md` — dot A DA XONG (v0.059, xem muc 12 ben duoi). Dot B/C (sap xep lai thu tu card, gop 3 card binding, Formula Builder Expander, Rotation 4 nut, Layer Forward/Backward) cho duyet.

## Viec can lam tiep (uu tien de xuat, chua ai duyet — hoi nguoi dung truoc khi lam)

### Uu tien cao — don dep repo/git truoc khi commit dot nay
- [x] Xac nhan voi nguoi dung roi `git restore --staged publish_out/` (984 file dang add nham, ~967 file trong do la build artifact).
- [x] Them `publish_out/` vao `.gitignore` de khong lap lai.
- [x] Xoa hoac di chuyen cac file rac o root (`345345345.anlabel`, `sdsds đ.anlabel`, `ANLAbel áaassTemplate.anlabel`, `ANLAbel 1 Template.anlabel`, `ANLAbel Template.anlabel`) neu xac nhan la file test khong can giu.
- [x] Quyet dinh giu hay xoa `installer/ANLAbel-x64.iss` + `ANLAbel-x86.iss` (installer legacy 0.042, nguon tu `TestOutput` da lac hau so voi luong Trial/Commercial hien tai).

### Uu tien trung binh — dong bo version
- [x] Gom viec bump version ve mot buoc duy nhat (script hoac doc `Version` tu `ANLAbel.App.csproj` khi build `.iss`/`ps1`) de tranh lech nhu da thay giua `build-trial.ps1` (v0.055) va `ANLAbel-Trial-x64.iss` (0.056) truoc do.
- [x] Cap nhat `installer/*.iss` con lai (Trial/Commercial/License-Master) va `build-*.ps1` len version moi nhat truoc khi cat release tiep theo.

### Uu tien trung binh — Excel import/UI thread
- [x] Ra soat cac cho khac co the goi I/O cham tren UI thread tuong tu bug Import Excel (rule 7 trong `agent.md`) — hien da kiem tra `MainWindow.xaml.cs` (Open/Save `.anlabel` la file JSON nho, khong dang lo), `PrinterPreferencesService` (file config nho, khong dang lo), va printer enumeration (local, nhan). Print pipeline dung `Task.Run` cho print job.
- [x] Can nhac them progress bar/huy (cancel) that su cho Import Excel voi file network, thay vi chi wait cursor. Da them `CancelButton` + `CancellationTokenSource` trong `ExcelImportWindow`.

### Uu tien thap — thu vien template
- [ ] Neu them template mau moi tu file thiet ke thuc te cua khach hang trong tuong lai: genericize + de trong `DatabaseConfig` (KHONG gan link Excel — quyet dinh 2026-07-02, rule 8 moi trong `agent.md`) + dam bao duoc match boi `EmbeddedResource Include="TemplateLibrary\*.anlabel"`.
- [x] Unit test bao ve: `template library standalone (no sample-data link)` — moi `*.anlabel` trong `TemplateLibrary/` phai co `DatabaseConfig.FilePath` rong (da thay the test cu "links sample-data.xlsx" sau khi doi dinh huong).

### Uu tien trung binh — database plan GĐ1 (sua ho loi "mat link" Excel)
- [x] Luu duong dan kep `FilePath` + `RelativePath` trong `DatabaseConfig` (`docs/database-plan.md` muc 1).
- [x] Thu tu tim Excel: tuyet doi → tuong doi → cung thu muc → broken (`ResolveExcelPath()`).
- [x] Nut re-link ro rang khi link hong (`RelinkExcelAsync()` + `RelinkExcelCommand`).
- [x] Khong ghi de `sample-data.xlsx` vo dieu kien (`ExtractSampleData()` da bi xoa, muc 3b).
- [x] Test `template excel link survives folder move` — 19/19 test PASS.

### Uu tien tiep theo — hoan thien GĐ2 data source manager
- [ ] Noi `DataSourceRegistry` vao panel Data Sources: them/sua/xoa/re-link source dung chung.
- [ ] Cho template chon `DataSourceId` va fallback ve `FilePath` cu de giu tuong thich nguoc.
- [ ] Them `FileSystemWatcher` debounce va badge "Data changed — Update".
- [ ] Them UI chon `KeyField`; backend `KeyValue`/restore theo key da co.
