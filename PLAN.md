# ANLAbel Phase 1 Plan

## Muc tieu Phase 1
- Tao ung dung Windows desktop WPF ten ANLAbel.
- Dung kien truc MVVM, tach model/service ro rang.
- Luu toa do va kich thuoc object bang mm, khong luu pixel.
- Tao label canvas theo kich thuoc that bang mm, preview chuyen doi mm sang WPF device-independent units.
- Ho tro object Text, Rectangle, Line.
- Ho tro chon, keo tha, resize va sua properties co ban.
- Luu/mo template JSON voi duoi `.anlabel`.

## Pham vi khong lam trong Phase 1
- Chua import Excel/CSV.
- Chua barcode/QR/Data Matrix.
- Chua in tem.
- Chua multi-select, ruler day du, undo/redo nang cao.
- Chua export PDF/PNG.

## Cau truc solution
- `src/ANLAbel.App`: WPF UI, Views, ViewModels, Controls.
- `src/ANLAbel.Core`: Models, Enums, Interfaces, Geometry, Commands dung chung.
- `src/ANLAbel.Project`: Service luu/mo file `.anlabel` bang JSON.
- `src/ANLAbel.Data`: Placeholder cho Excel/CSV Phase 2.
- `src/ANLAbel.Barcode`: Placeholder interface/engine barcode Phase 3.
- `src/ANLAbel.Printing`: Placeholder cho print pipeline Phase 4.
- `src/ANLAbel.Tests`: Test runner nhe cho conversion, expression, save/load.
- `docs`: Tai lieu kien truc va ghi chu in/barcode.

## Buoc thuc hien

### 1. Scaffold solution
- Tao `ANLAbel.sln`.
- Tao WPF project `ANLAbel.App`.
- Tao class library cho Core, Project, Data, Barcode, Printing.
- Tao test runner nhe `ANLAbel.Tests`.
- Them reference giua cac project.

### 2. Core models va geometry
- Tao `LabelTemplate`.
- Tao `LabelObject`.
- Tao `ObjectType`, `LabelOrientation`, `TextAlignmentMode`.
- Tao `MmConverter` cho:
  - mm -> WPF DIP theo 96 DPI.
  - mm -> printer dots theo DPI may in.
  - DIP -> mm cho thao tac keo tha/resize tren canvas.

### 3. Project service
- Tao `IProjectFileService`.
- Tao `ProjectFileService` dung `System.Text.Json`.
- Dam bao JSON doc duoc Unicode va object property ro rang.
- Luu/mo file `.anlabel`.

### 4. MVVM ha tang
- Tao `ObservableObject`.
- Tao `RelayCommand`.
- Tao `MainViewModel`.
- Tao selected object binding hai chieu voi properties panel.

### 5. Designer UI
- Tao `MainWindow`.
- Layout:
  - Toolbar tren cung.
  - Toolbox ben trai.
  - Canvas thiet ke o giua.
  - Properties panel ben phai.
- Them command:
  - New template.
  - Save.
  - Open.
  - Add Text.
  - Add Rectangle.
  - Add Line.
  - Zoom in/out.

### 6. Label canvas control
- Tao `LabelDesignerCanvas`.
- Ve label background theo width/height mm.
- Hien grid nhe theo mm.
- Render object theo toa do mm.
- Chon object bang click.
- Keo object bang mouse.
- Resize object bang grip goc duoi phai.
- Cap nhat model bang mm sau moi thao tac.

### 7. Properties panel
- Sua duoc:
  - X mm
  - Y mm
  - Width mm
  - Height mm
  - Text
  - Font size
- Disable khi chua chon object.

### 8. Documentation
- Tao `docs/architecture.md`.
- Tao `docs/print-calibration.md`.
- Tao `docs/barcode-notes.md`.
- Tao `docs/license-notices.md`.

### 9. Verification
- Build solution.
- Chay test runner:
  - mm conversion.
  - expression binding placeholder co ban.
  - save/load template.
- Neu co the, chay app bang `dotnet run --project src/ANLAbel.App`.

## Tieu chi hoan thanh Phase 1
- Build thanh cong.
- Mo duoc app WPF.
- Tao template moi theo width/height mm.
- Them Text/Rectangle/Line.
- Keo object tren canvas va toa do properties doi theo mm.
- Resize object va kich thuoc properties doi theo mm.
- Sua properties va object tren canvas cap nhat.
- Luu template `.anlabel`.
- Mo lai template va giu dung toa do/kich thuoc mm.

## Phase 2 - Excel Binding

### Pham vi theo dieu chinh moi
- Chi uu tien Excel `.xlsx` va `.xlsm`.
- Khong lam CSV trong buoc nay.

### Buoc thuc hien Phase 2
- Them `ClosedXML` vao `ANLAbel.Data`.
- Tao `ExcelDataService`:
  - Lay danh sach sheet.
  - Doc header dong 1.
  - Trim khoang trang.
  - Loai bo ky tu xuong dong trong cell.
  - Giu Unicode tieng Viet.
- Them nut `Import Excel` tren toolbar.
- Cho chon sheet khi import.
- Hien thi data bang DataGrid preview phia duoi.
- Cho chon mot row trong DataGrid.
- Them `BindingExpression` cho Text object.
- Khi chon row, Text object render theo cu phap `{ColumnName}`.

### Tieu chi hoan thanh Phase 2 buoc dau
- Import duoc `.xlsx/.xlsm`.
- Chon duoc sheet.
- Hien thi du lieu tren table preview.
- Chon dong preview lam tem cap nhat.
- Binding nhu `P{PartNo} Q{Qty} 1T{Lot}` hoat dong voi Text object.

## Phase 3 - Barcode

### Pham vi dang lam
- Dung `ZXing.Net` sau interface `IBarcodeRenderer`.
- Ho tro:
  - Code 128
  - QR Code
  - Data Matrix ECC200
- Render barcode thanh pixel buffer theo kich thuoc mm va DPI truyen vao.
- UI WPF chuyen pixel buffer thanh `BitmapSource` de preview.
- Khi du lieu binding hoac kich thuoc object thay doi, barcode duoc render lai.

### Tieu chi hoan thanh Phase 3 buoc dau
- Them duoc object Code 128, QR Code, Data Matrix tu toolbar/toolbox.
- Barcode lay du lieu tu `BindingExpression` neu co row Excel.
- Barcode lay static text khi khong binding Excel.
- Validate barcode data rong.
- Co test render Code 128, QR Code, Data Matrix.

### Dieu chinh barcode UI
- Toolbar/toolbox chi con mot nut `Barcode`.
- Chuan barcode duoc chon trong panel properties ben phai.
- Moi object barcode luu `BarcodeSymbology`.
- Danh sach barcode gom Code 128, QR Code, Data Matrix, Code 39, Code 93, EAN-13, EAN-8, UPC-A, UPC-E, ITF, Codabar, PDF417, Aztec, MSI, Plessey.

## Phase 4 - Printing

### Dinh huong san pham bat buoc
- ANLAbel duoc dinh huong truoc tien cho may in tem nhan cong nghiep va workflow tem san xuat/logistics.
- Moi quyet dinh lien quan den printer setup, khổ giay, orientation, DPI, calibration, paper feed, preview va print pipeline phai uu tien hanh vi cua may in tem nhan cong nghiep truoc may in van phong.
- Driver can duoc xem la nguon su that chinh cho kho giay/huong giay neu driver expose du lieu theo cach doc duoc.
- Khong duoc mac dinh chi can `PrintCapabilities.PageMediaSizeCapability` la du cho nhom may in nay.
- Neu WPF `PrintCapabilities` khong du, huong tiep theo hop ly la fallback doc danh sach kho giay qua API gan driver hon nhu `System.Drawing.Printing.PrinterSettings.PaperSizes`, sau do moi den driver preferences/DEVMODE, va cuoi cung moi la nhap tay neu van khong lay duoc.
- Khong dung lai danh sach kho giay hardcode cua phan mem cho luong printer setup chinh.

### Pham vi buoc dau
- In qua Windows PrintDialog / Windows printer driver.
- Them nut `Print` de in template voi row Excel hien tai.
- Them nut `Test Print` de in calibration ruler.
- Tao `PrintService` rieng trong `ANLAbel.Printing`.
- Tao `LabelVisualRenderer` render tu model mm sang WPF physical units.
- Barcode duoc render lai trong print pipeline theo DPI cua print ticket/profile.

### Nguyen tac da ap dung
- Khong in anh preview cua canvas.
- Khong luu toa do pixel.
- Page size tinh tu label width/height mm.
- Calibration visual co vach 10 mm de do sai lech thuc te.

### Viec can lam tiep trong Phase 4
- Them UI luu printer profile theo tung may in.
- In selected rows.
- Can test thuc te tren may in tem vi moi driver xu ly page media size khac nhau.

### Da bo sung trong Phase 4
- UI chinh printer profile:
  - Label width/height mm.
  - DPI.
  - Offset X/Y mm.
  - Scale X/Y.
- `Print All Rows` cho toan bo Excel preview.
- `Copies` co dinh.
- `Copy field` theo cot Excel, vi du `QtyPrint` hoac `{QtyPrint}`.
- Startup printer setup dialog:
  - Liet ke Windows printers bang `System.Printing`.
  - Uu tien printer co ten/driver giong may in tem nhan.
  - Lay paper size tu driver theo huong uu tien cho may in tem nhan cong nghiep, khong gioi han duy nhat o `PrintCapabilities.PageMediaSizeCapability`.
  - Chon paper size se set template width/height va printer profile.
  - PrintDialog tu chon lai printer da luu neu con ton tai.
- Print Preview bang `Ctrl+P`:
  - Preview tung tem/page theo row Excel.
  - 5 row Excel = 5 tem/page preview.
  - Panel output ben phai co printer name, driver setting va print mode.
  - Nut printer settings mo Windows PrintDialog de chon driver/output.
- Print history log:
  - Ghi log sau khi lenh in gui thanh cong.
  - Luu tap trung vao `%AppData%/ANLAbel/print-history.xlsx`.
  - Log ap dung cho moi template `.anlabel`.
  - Ghi template, printer, kich thuoc tem, DPI, mode in, so row, so label, file Excel va sheet.

## Dieu chinh UI/Designer sau Phase 4

### Da hoan thanh
- Dieu chinh lai logic Excel/database field theo huong dung workflow thiet ke tem:
  - Them model `DatabaseField` gom Name, DisplayName va SampleValue.
  - `DatabaseConfig` luu rieng `AvailableFields` la toan bo cot doc tu Excel va `LabelFields` la cac cot duoc phep dung trong tem.
  - Sau khi import Excel, panel Data Sources hien 2 danh sach: `All fields from Excel` va `Fields used on label`.
  - Co nut `Add`, `Add all`, `Remove`, `Clear` de chon truong dua vao tem.
  - Properties khong con lay field truc tiep tu tat ca header Excel; ComboBox `Excel field` chi dung `LabelFields`.
  - Function tab co danh sach selected label fields; bam field se chen `FIELD("TenCot")` vao object dang chon.
  - Function module van giu `FIELD`/`CONCAT` hien co de dam bao tuong thich, nhung luong su dung chinh da dua theo field nguoi dung chon.
- Them Function Builder de ghep cong thuc khong can tu viet `CONCAT(...)`:
  - Nguoi dung bam field trong `LabelFields` de add vao chuoi cong thuc.
  - Co cac nut separator nhanh nhu ` - `, ` | `, ` / `, `_`, khoang trang va `: `.
  - Co o nhap fixed text va nut `Add text`.
  - Danh sach formula parts hien tung thanh phan dang ghep, co `Remove`, `Clear`.
  - Preview chi hien ket qua theo row Excel dang chon; an toan bo expression/code ky thuat, error text va Advanced modules de UI chi con nut thao tac can dung.
  - Nut `Apply` gan cong thuc da build vao object Text/Barcode/QR/Data Matrix dang chon.
  - Chuyen Function Builder khoi panel trai sang Properties va phan nhom thanh `2D Code Data Builder`, chi hien khi barcode standard thuoc nhom ma 2D/ma tran: QR Code, Data Matrix, Aztec, PDF417.
  - Dropdown `Barcode standard` trong Properties da duoc nhom thanh `1D barcode` va `2D / matrix code` de nguoi dung chon dung loai ma nhanh hon.
- Chuyen quan ly Excel/database field ra dialog rieng khi bam `Import Excel`:
  - Tao `ExcelImportWindow` rieng.
  - Dialog co duong dan file Excel va nut `Browse...` de chon file bat ky trong may.
  - Sau khi chon file, app hoi sheet roi import data.
  - Dialog hien 2 bang `All fields from Excel` va `Fields used on label` de add/remove/clear field.
  - Data Sources panel ben trai khong con chua UI add/remove field, chi con thong tin cay Template/Objects/Database va nhac dung ribbon Import Excel.
- Bo bang `Excel Preview` phia duoi workspace:
  - Khong con panel preview Excel trong man hinh thiet ke chinh.
  - Khong con splitter/hang duoi chiem dien tich khi chua co data.
  - State/command `IsExcelPreviewVisible` va `HideExcelPreviewCommand` duoc go bo khoi ViewModel.
  - Excel data van duoc giu trong ViewModel de print/binding, nhung quan ly thong qua dialog `Import Excel`.
- Grid nen tem tren designer doi tu 5 mm thanh 1 mm.
- Gioi han font chu cho Text/TextBox theo nhom phu hop tem logistic/san xuat:
  - Khong hien toan bo font he thong nua.
  - Danh sach uu tien: Arial, Arial Narrow, Bahnschrift, Calibri, Consolas, Courier New, Lucida Console, Segoe UI Semibold, Tahoma, Verdana.
  - Chi hien font co cai tren may; neu may khong co font nao trong danh sach thi fallback `Segoe UI`.
  - Font mac dinh cua object moi doi sang `Arial`.
- Text thuong tu dong fit khung xanh theo noi dung:
  - Object `Text` do kich thuoc chu hien tai va cap nhat Width/Height theo noi dung.
  - Khung chon/resize mau xanh bam sat noi dung text hon, khong con rong dai du thua.
  - Van gioi han trong bien label de text khong vuot kho giay.
  - `TextBox` khong auto-fit, van giu kich thuoc box co dinh de wrap/clip noi dung.
- Them nut `Update Excel` tren ribbon Data:
  - Nut nam canh `Import Excel` de phu hop luong dung thuc te khi file Excel thay doi du lieu.
  - Reload lai dung file Excel va sheet da gan trong `Template.DatabaseConfig`.
  - Sau khi reload, cap nhat lai data rows, PreviewRow, AvailableFields va giu lai LabelFields neu cot con ton tai.
  - Nut tu disable khi chua co file/sheet Excel hop le hoac file da bi xoa/doi vi tri.
- Tang do tin cay cho ket noi Excel -> Text/Barcode theo huong gan NiceLabel hon:
  - `BindingExpression` dang `{Field}` va formula `FIELD("Field")` khong chi match exact/case-insensitive, ma con fallback theo ten field da normalize.
  - Normalize field name bo qua khoang trang, gach noi, dau cau va khac biet hoa-thuong de giam vo binding khi header Excel thay doi nhe.
  - Khi import/reload Excel, app co gang repair lai `BindingExpression`/formula ve ten cot thuc te cua workbook hien tai neu tim thay match hop ly.
  - Truong hop nguoi dung go `Part_No` va file Excel doi thanh `Part No`, preview/in va formula builder van tiep tuc resolve dung field thay vi mat lien ket.
- Bo sung feedback truc tiep cho ket noi Excel trong Properties:
  - Object co `BindingExpression` gio hien them khung `Binding Preview`.
  - Khung nay hien `Source type`, `Preview value`, `Linked fields`, `Missing fields`, `Errors`, va `Binding status`.
  - Cung luat resolve field voi print/preview engine duoc dung de bao tinh trang lien ket, giup kiem tra nhanh object nao dang hop le va object nao dang mat field.
  - Formula van co khung `Formula Output` rieng, con placeholder binding thong thuong gio cung co feedback ro rang hon theo workflow gan NiceLabel.
- Bo sung tong quan `Binding Issues` o panel trai:
  - Data Sources hien them danh sach object dang vo field hoac co loi formula theo workbook hien tai.
  - Moi dong hien object name/type, status, missing fields va error text.
  - Bam vao mot issue se chon ngay object tuong ung de sua trong Properties/Designer.
  - Muc tieu la ra soat template nhanh theo kieu NiceLabel, khong can mo tung object moi biet binding nao dang hong.
- Don dep UX properties panel:
  - Them object summary card o dau Properties de thay nhanh object name/type/kich thuoc.
  - Card `Content` hien them workbook/sheet dang link de nguoi dung biet object dang an theo nguon du lieu nao.
  - `Content source` duoc dong bo lai theo object dang chon, tranh UI hien `Text` trong khi object thuc te dang bind Excel.
  - Font mac dinh cho object moi doi sang `Arial`; danh sach font uu tien cung dua `Arial` len dau.
  - Sua luong chon `Excel Field` trong Properties: khi doi field trong combo, object dang chon duoc bind lai ngay vao `{Field}` va canvas cap nhat theo row Excel hien tai, khong can bam lai source hoac tao object moi.
  - Neu chuyen source sang `Excel Field` khi chua co field dang chon, app tu lay field dau tien trong danh sach label fields de bind ngay cho object.
  - Import Excel lan dau tu dong dua cac header vao `Label fields`, nen object co the bind field ngay ma khong can vao cua so import bam `Add all` truoc.
  - Panel `Data Sources` co them bang `Excel Rows`; click tung row se cap nhat `PreviewRow` va day du lieu vao text/barcode object dang bind tren canvas.
- Cung co do ben save/load file thiet ke voi Excel link:
  - `.anlabel` van luu `DatabaseConfig.FilePath` va `SheetName`.
  - Khi mo lai template, neu file Excel lien ket van con ton tai thi app tu restore Excel data/sheet thay vi chi nho duong dan.
  - Neu file Excel cu da mat, template van mo duoc va app bao ro link cu khong con ton tai.
  - Luu them `LastSelectedRow` va khi mo lai template se co gang quay dung row Excel cuoi cung da dung de preview/thiet ke.
  - Panel `Data Sources` hien them workbook/sheet va row hien tai de nguoi dung thay ngay context du lieu dang duoc restore.
- Cung lam cay `Objects` de gan cach quan sat cua NiceLabel hon:
  - Moi object hien ro `Name` + `Type` ngay trong danh sach thay vi chi co ten.
  - Object dang co `BindingExpression` se hien them badge trang thai theo dung tinh trang hien tai: `Linked Excel`, `Formula linked`, hoac bao loi nhu `Missing: PartNo`, `Formula error`.
  - Badge nay doi mau theo trang thai, giup nhin ngay object nao dang bind on dinh va object nao dang loi ma khong can bam vao tung item.
  - Muc nay giup ra soat template nhanh hon khi co nhieu text/barcode object dang bind Excel.
- Sua hit-test cho `Rectangle` tren designer:
  - Vung ruot rectangle khong con bat click/chon object nua, nen text/barcode nam ben trong khung co the duoc chon truc tiep.
  - Chi cac vung vien rectangle moi nhan hit-test de chon/keo rectangle.
  - Visual fill/stroke cua rectangle van giu nguyen; thay doi chi ap dung cho hanh vi chon tren canvas.
- Tang do on dinh cho print/preflight:
  - Tao shared `PrintPreflightValidator` trong `ANLAbel.Printing` de barcode invalid, fixed QR qua suc chua, va text box overflow deu bi chan bang cung mot luong.
  - `PrintService` tu dong chay preflight truoc khi tao visual/gui print job; neu co loi thi throw message ro rang va khong gui job loi xuong printer.
  - `Print Preview` hien trang thai preflight ngay trong panel settings, giup thay truoc label nao chua an toan de in.
  - Luong print tu preview gio block som va thong bao ro ly do thay vi de driver/renderer xu ly muon hon.
  - `Print Preview` hien them danh sach `Preflight issues` theo row/object va cho bam de nhay toi label page dang loi.
  - Sua bug giu khong on dinh driver paper trong `Print Preview`: neu khong match lai duoc `PaperName` thi khong tu dong nhay sang giay dau tien cua driver nua; uu tien match theo ten, sau do theo kich thuoc da luu, neu van khong thay thi giu nguyen khổ da save.
  - `Print Preview` hien them trang thai match paper de biet dang dung dung driver paper, dang manual, hay dang giu khổ da save vi driver khong match.
  - `Print Preview` co them bang `Label / Excel tracking` gom page, source row, copy, PartNo, Name, Lot, Qty; bam vao dong se nhay sang label page tuong ung.
  - Sap xep lai `Print Preview`: bang tracking Excel/page duoc chuyen sang duoi vung preview tem ben trai, con panel setting ben phai co scroll rieng de khong bi khuất thong tin cau hinh.
  - `Print Preview` hien them vung in driver/may in: overlay net dut tren tem preview va thong tin `PageImageableArea` gom origin + kich thuoc vung in driver.
  - Luong in that tu dong bu `PageImageableArea.OriginWidth/OriginHeight` cua driver de giam tinh trang tem bi lech phai/xuong do WPF/driver dat goc in vao vung imageable.
  - Calibration tool trong `Print Preview` hien ro offset/scale hien tai, co nut `Print calibration` va `Reset offset/scale`; offset/scale van luu theo printer profile de tinh chinh tung may.
  - Muc nay giup kiem tra lo tem loi nhanh hon khi in nhieu row Excel, gan voi workflow kiem tra print truoc khi xuat job.
- Them object `TextBox` rieng voi luat chu khong tran ra ngoai box:
  - TextBox wrap va clip noi dung trong kich thuoc object.
  - Text thuong van khong wrap va co the tran ngang theo noi dung.
- Toolbar/toolbox co nut them Text, Text Box, Barcode, QR Code, Data Matrix, Line, Rectangle.
- Nut QR Code goi dung command tao object QR Code.
- Properties panel co phan Style hoat dong that:
  - Chon font chu tu font he thong.
  - Chon co chu.
  - Bold, Italic, Underline.
  - Canh trai/giua/phai.
  - Mau stroke/text, mau fill, do day border.
- Print renderer da phan biet Text va TextBox theo dung luat tran/khong tran.
- Them zoom bang `Ctrl + con lan chuot`:
  - Tren designer canvas, thay doi zoom nen thiet ke tem tu 25% den 400%.
  - Tren Print Preview, scale toan bo danh sach tem preview tu 25% den 400%.
  - Khi khong giu Ctrl, con lan chuot van cuon binh thuong.
- Them che do ve snap 1 mm cho Line va Rectangle:
  - Bam tool Line/Rectangle se vao che do ve, chua tao object co dinh ngay.
  - Click hoac chuot phai tren canvas dat diem dau, diem nay duoc snap vao giao diem luoi 1 mm.
  - Keo chuot toi diem con lai thi diem cuoi tiep tuc snap theo luoi 1 mm lien tuc.
  - Nha chuot de hoan tat object.
  - Bam Esc de huy che do bat diem/dang ve.
  - Line luu diem dau va diem cuoi that, render/print theo hai diem nay.
- Them style Outline/Fill cho Line va Rectangle:
  - Thickness tinh bang mm.
  - Outline style: None, Solid, Dash, Dot.
  - Outline color dung ma mau WPF/hex.
  - Corner radius tinh bang mm cho Rectangle/TextBox border khi render/print.
  - Fill style: None, Solid.
  - Background color dung ma mau WPF/hex.
  - Canvas preview va print renderer deu ap dung cac style nay.
- Them Delete/Undo/Redo:
  - Xoa object dang chon bang nut Delete tren toolbar hoac phim Delete.
  - Xoa duoc Line, Rectangle va cac object khac khi dang duoc chon.
  - Undo bang nut Undo hoac `Ctrl+Z`.
  - Redo bang nut Redo hoac `Ctrl+Y`.
  - History dung snapshot template nen ap dung cho them/xoa object va chinh properties.
- Them copy/paste object tren canvas:
  - `Ctrl+C` copy object dang chon hoac ca group selection.
  - `Ctrl+V` paste object/group moi, tao Id moi, day ZIndex len tren va offset 3 mm moi lan paste.
  - Paste giu nguyen text, binding, barcode standard, line endpoint va style cua object goc.
  - Object/group vua paste duoc select ngay de co the di chuyen tiep.
- Viet lai logic ve shape theo huong CAD cho Line/Rectangle/Ellipse:
  - Phim tat `L` vao lenh Line.
  - Phim tat `R` vao lenh Rectangle.
  - Phim tat `C` vao lenh Ellipse/Circle.
  - Click diem dau bat vao giao diem luoi 1 mm.
  - Keo chuot de preview va bat diem cuoi theo luoi 1 mm.
  - Co the nhap kich thuoc bang ban phim khi dang ve:
    - Line: nhap `20` roi Enter de ve line dai 20 mm theo huong con tro dang chi.
    - Rectangle: nhap `30,10` roi Enter de ve khung 30 x 10 mm.
    - Ellipse/Circle: nhap `30,10` de ve ellipse, hoac `20` de ve circle duong kinh 20 mm.
  - Esc huy lenh dang ve.
  - Them object `Ellipse`, canvas preview va print renderer deu ho tro.
  - Properties panel tu loc nhom phu hop theo object:
    - Text/TextBox hien text source va font.
    - Line/Rectangle/Ellipse hien Outline.
    - Rectangle/Ellipse hien Fill.
    - Barcode/QR/Data Matrix hien Barcode standard.
- Sua loi mo app sau khi them phim tat CAD:
  - WPF khong ho tro `KeyGesture` mot phim chu don le nhu `L`, `R`, `C` trong `InputBindings`.
  - Chuyen `L/R/C` sang `PreviewKeyDown` cua `MainWindow`.
  - Bo qua phim tat khi focus dang o TextBox/ComboBox de khong pha thao tac nhap lieu.
  - Xac nhan app mo duoc va process `ANLAbel.App` dang chay.
- Dieu chinh lai luong ve CAD cho on dinh hon:
  - Khong hoan tat object khi nha chuot nua.
  - Click diem dau de bat dau lenh va tao preview object.
  - Re chuot de preview lien tuc theo luoi.
  - Click diem thu hai de hoan tat, hoac nhap kich thuoc roi Enter.
  - Text command hien trang thai cu the: specify first point, specify next point, hoac size dang nhap.
- Them chon vung va di chuyen nhom object:
  - Keo chuot tren nen trong de tao vung chon marquee.
  - Cac object giao voi vung chon duoc dua vao selected group.
  - Keo mot object bat ky trong group se di chuyen ca nhom.
  - Group move co clamp trong bien label, ap dung cho ca Line co diem dau/diem cuoi rieng.
- Dieu chinh chon vung/thickness:
  - Sau khi khoanh vung, canvas giu focus de nhan phim dieu huong.
  - Phim mui ten di chuyen group 1 mm moi lan.
  - `Shift + mui ten` di chuyen group 10 mm moi lan.
  - Sua render Line de bounds co padding theo stroke thickness, tranh bi cat net khi tang do day line.
- Hoan thien feedback group selection:
  - Moi object trong group selection duoc ve overlay xanh dashed de thay ro da duoc gom.
  - Nut Delete va phim Delete goi lenh xoa selection tren canvas.
  - Delete xoa toan bo group neu dang co group selection, khong chi xoa object selected dau tien.
- Sua do benh lenh in:
  - Kiem tra printer da duoc chon truoc khi ghi XPS/print job.
  - Dung PrintTicket mac dinh cua queue neu PrintDialog khong tra ve ticket rieng.
  - Bọc lỗi in Current/All/Calibration bang status message thay vi de app crash.
  - Bọc lỗi Print Preview bang MessageBox `Print failed`.
- Sua loi Print Preview bao `The calling thread cannot access this object because a different thread owns it`:
  - Nguyen nhan do cua so preview giu lai `PrintDialog/PrintQueue` WPF object roi dung lai luc in, trong khi object nay bi WPF gan thread.
  - Print Preview chi luu `PrinterName` dang chon, khong giu/reuse `PrintDialog` cu.
  - Khi bam Print, `PrintService` tao `PrintDialog/PrintQueue` moi tu ten printer ngay trong luong in hien tai.
  - Build/test xac nhan khong con loi compile va print pipeline van PASS.
- Sua crash Print Preview:
  - Loi goc: `VisualPreviewHost` gan cung mot `DrawingVisual` vao nhieu parent WPF khi preview refresh/show.
  - Chuyen Print Preview sang render tung page visual thanh `RenderTargetBitmap`/`ImageSource`.
  - XAML preview dung `Image` thay vi gan truc tiep `Visual`.
- Nang cap lich su in tem:
  - Log ghi theo tung tem/label thay vi chi tong hop job.
  - Them cot `LabelIndex`, `PartNo`, `ItemName`, `Lot`, `Quantity`, `RowData`.
  - `RowData` luu tat ca field Excel cua dong in theo dang `Field=Value`.
  - Ho tro ten cot pho bien: PartNo/Part No/PN, Name/ItemName/TenHang, Lot/LotNo/Batch, Qty/Quantity/SoLuong.
- Hoan thien print history theo yeu cau thuc te:
  - Tat ca lenh in ghi vao mot file duy nhat `%AppData%/ANLAbel/print-history.xlsx`.
  - Them cot `LabelContent` de ghi noi dung thuc te tren tung tem sau khi resolve binding/formula theo row Excel.
  - Moi tem in ra la mot dong log rieng, co LabelIndex, row Excel, noi dung tem va du lieu field goc.
  - Sau khi in tu ribbon hoac Ctrl+P preview, app mo file print history de nguoi dung thay lich su in ngay.
  - Them nut `Print History` tren ribbon Printer de mo dung file history duy nhat bat cu luc nao.
  - Header history luon duoc dong bo lai, giup file cu cung hien dung cot `LabelContent` nam truoc `RowData`.
  - Neu Excel dang mo/khoa `print-history.xlsx`, Print Preview se bao rieng `Print history is open` thay vi hieu nham la loi in chinh.
- Lam lai Ctrl+P Print Preview:
  - Preview chi hien 1 tem tai mot thoi diem.
  - Thanh dieu huong ben duoi co Previous/Next, o nhap so thu tu tem va trang thai `Label x of n`.
  - Ben duoi preview hien tom tat row hien tai: PartNo, Name, Lot, Qty neu co.
  - Panel printer ben phai hien printer dang chon trong khung rieng, kich thuoc tem va nut `Select printer / properties`.
  - Khi in tu preview, log cung ghi tung tem va du lieu row thuc te.
- Tang do phan giai Print Preview:
  - Preview page duoc render noi bo o 300 DPI thay vi 96 DPI.
  - Anh preview duoc scale ve dung kich thuoc hien thi theo mm, giu net hon khi zoom/kiem tra tem.
- Gon lai Properties/Printer profile:
  - Phan printer profile dai ben phai duoc thay bang `Label size` nho gon va `Printer calibration` dang thu gon.
  - Label size chinh bind vao `Template.WidthMm/HeightMm`, canvas theo dung kho tem hien tai.
  - Printer paper W/H trong calibration dong bo nguoc lai template size khi can chinh truc tiep.
  - `PrinterProfile` chuyen sang ObservableObject de cac thay doi printer/paper size cap nhat UI va history on dinh.
- Chuyen setup kho in vao dung ngu canh:
  - Bo `Label size`, `Copies`, `Copy field`, `Printer calibration` khoi Properties object ben phai.
  - Them `Print setup` trong cua so Ctrl+P/Print Preview.
  - Trong `Print setup` co Label W/H, Copies, Copy field va nut `Apply print setup`.
  - Calibration trong Print Preview dang thu gon, gom DPI, Scale X/Y, Offset X/Y.
  - Doi binding preview tu `Template` sang `LabelTemplate` de tranh trung ten WPF `Control.Template`.
- Bo fallback kho giay hardcode trong printer setup:
  - Danh sach `Driver paper sizes` khong duoc phu thuoc duy nhat vao `PrintCapabilities.PageMediaSizeCapability`.
  - Neu driver may in tem khong expose du lieu day du qua WPF, phai bo sung fallback doc paper size theo API phu hop voi driver cong nghiep truoc khi chap nhan trang thai khong co khổ giay.
  - Khong tu sinh danh sach kho giay hardcode cua phan mem cho workflow chinh.
- Them chon huong in trong printer setup:
  - Cua so setup may in co them `Portrait` va `Landscape`.
  - Lua chon nay cap nhat `Template.Orientation`, kich thuoc label theo huong chon, va luu `PrinterProfile.PaperName`.
  - Khi mo `PrintDialog` va khi gui job in, `PrintService` ap lai `PageOrientation` va co gang match dung `PageMediaSize` cua driver theo ten/kich thuoc giay da luu.
- Nang cap `Print setup` theo huong gan workflow NiceLabel cho may in tem:
  - Bo sung `Printer settings source`: `Label` hoac `Driver`.
  - Bo sung `Page size source`: `Driver automatic` hoac `Manual`.
  - Bo sung danh sach `Driver paper` theo may in dang chon ngay trong Print Preview.
  - Bo sung `Orientation` ngay trong Print Preview, khong phai quay lai man hinh chinh de doi.
  - Them nut `Label printer setup...` rieng voi nut `Driver properties...` de tach ro luong setup nhan/giay voi luong mo driver Windows.
  - `Copies` va `Copy field` trong Print Preview da anh huong that den preview page count va lenh in.
- Mo rong them nhom media handling theo huong NiceLabel:
  - Bo sung `Media type`: `Gap`, `BlackMark`, `Continuous`, `Notch`.
  - Bo sung `Gap mm`.
  - Bo sung `Feed direction`.
  - Bo sung `Printable margin mm`.
  - Bo sung `Rotate output 180°`.
  - Preview/in da su dung `Rotate output 180°` trong render pipeline.
  - Preview/in da su dung `Printable margin mm` de ve va clip `printable area`.
  - Calibration preview hien them `Media`, `Gap`, va `Rotated 180` de nguoi dung doi chieu nhanh khi test may.
- Bo sung fallback doc khổ giay theo driver cong nghiep:
  - Ngoai `PrintCapabilities.PageMediaSizeCapability`, app goi them Win32 spooler `DeviceCapabilitiesW` de doc `paper names` va `paper sizes`.
  - Huong nay uu tien phu hop cho nhieu driver may in tem nhan cong nghiep hon so voi chi dung WPF capabilities.
  - Da sua loi startup do buffer `DC_PAPERSIZE` dung sai kieu du lieu; app mo lai on dinh sau khi doi sang layout phu hop voi Win32.
- Don dep Properties panel:
  - Bo tab placeholder `Source / Barcode / Position / General` vi chua co hanh vi that va gay nhieu.
  - Khi chua chon object, chi hien `No object selected`; cac field X/Y/Width/Height bi an han.
- Sap xep lai workspace/module chinh:
  - Menu tren cung chia lai theo nhom chuyen nghiep hon: File, Edit, Data, View, Print, Insert.
  - Ribbon tren cung bo bot nut ve object trung lap; tool ve nam chinh trong toolbox ben trai, ribbon giu File/Edit/Data/Print/View/Zoom.
  - Toolbox/Data ben trai, Properties ben phai va Excel preview ben duoi co header rieng va nut dong `x`.
  - Menu `View` co checkbox mo/tat lai Toolbox/Data, Properties, Excel preview va lenh `Restore workspace`.
  - Cac panel trai/phai/duoi co `GridSplitter` de keo resize truc tiep.
  - Khi tat panel, cot/hang cua panel thu ve 0 de khong de lai khoang trong du thua tren designer.
- Test runner:
  - File test print log doi sang ten unique de tranh bi khoa file khi Excel/app dang giu file cu.
- Verify:
  - `dotnet build ANLAbel.slnx` thanh cong.
  - `dotnet run --project src/ANLAbel.Tests/ANLAbel.Tests.csproj` tat ca test PASS.
  - `dotnet build ANLAbel.slnx -p:UseSharedCompilation=false -nodeReuse:false` thanh cong sau khi sua printer setup.
  - Chay `src\ANLAbel.Tests\bin\Debug\net8.0-windows\ANLAbel.Tests.exe` tat ca test PASS.
  - Mo `src\ANLAbel.App\bin\Debug\net8.0-windows\ANLAbel.App.exe`, xac nhan app chay len voi title `ANLAbel - Label Designer v0.021`.
  - Sau khi them `Binding Preview` cho Excel/Text/Barcode, build/test van PASS va app van mo duoc binh thuong.
  - Sau khi them `Binding Issues` o panel trai, build/test van PASS va app van mo duoc binh thuong.
  - Sau khi them `PrintPreflightValidator` va preflight status trong Print Preview, build/test van PASS va app van mo duoc binh thuong.
  - Sau khi them danh sach `Preflight issues` va jump-to-page trong Print Preview, build/test van PASS va app van mo duoc binh thuong.
  - Sau khi don dep Properties va them auto-restore Excel link khi mo template, build/test van PASS va app van mo duoc binh thuong.
  - Sau khi them restore `LastSelectedRow` va hien row context trong `Data Sources`, build/test van PASS va app van mo duoc binh thuong.
  - Sau khi them overlay vung in driver, thong tin imageable area, bu origin driver va nut calibration/reset trong Print Preview, `dotnet build ANLAbel.slnx -p:UseSharedCompilation=false -nodeReuse:false` thanh cong.
  - Chay `src\ANLAbel.Tests\bin\Debug\net8.0-windows\ANLAbel.Tests.exe` tat ca test PASS sau thay doi print area/calibration.
  - Mo app debug va ban publish, xac nhan title `ANLAbel - Label Designer v0.026`.
  - Publish Release vao `dist\ANLAbel` thanh cong cho version `0.026`.
  - Sau khi lam lai print pipeline de template design la nguon kich thuoc duy nhat:
    - Preview/print dung `Template.WidthMm/HeightMm`, khong dung size cu trong `PrinterProfile` de render tem.
    - Print ticket chi chon driver paper neu kich thuoc khop template; neu khong thi dung custom page size theo template.
    - Bo bu am `PageImageableArea` mac dinh; driver imageable area chi de hien thi/canh bao, con chinh lech thuc te dung calibration offset/scale.
    - Bo clip theo printable margin trong visual in that de noi dung sat mep thiet ke khong bi mat; margin chi la thong tin setup/canh bao.
    - Text trong print renderer canh doc/ngang gan voi designer hon, tranh lech chu trong tem nho.
  - Them test `print preview follows design label size` va `print renderer keeps edge content`.
  - `dotnet build ANLAbel.slnx -p:UseSharedCompilation=false -nodeReuse:false` thanh cong cho version `0.027`.
  - Chay `src\ANLAbel.Tests\bin\Debug\net8.0-windows\ANLAbel.Tests.exe` tat ca test PASS cho version `0.027`.
  - Sau khi tiep tuc ra soat mismatch designer/print, print renderer cho barcode dung `LabelObject.QrDpi` giong designer, khong lay DPI driver lam doi rule render barcode ngoai y muon.
  - Them test `print barcode uses object dpi` de khoa luong nay.
  - Version hien thi duoc tang len `0.028` cho thay doi print alignment tiep theo.
  - Bo sung preflight de tranh case designer thay noi dung ngoai mep tem nhung print renderer cat theo kho tem:
    - Object visible nam vuot `Template.WidthMm/HeightMm` bi chan truoc khi in.
    - Text thuong sau khi resolve Excel theo tung row neu vuot kho tem se bi chan truoc khi in.
    - Thong bao preflight noi ro can move object, giam font, rut ngan data hoac dung Text Box.
  - Them test `print preflight blocks object outside label` va `print preflight blocks text outside label`.
  - Version hien thi duoc tang len `0.029` cho thay doi preflight chong in bi clip.
  - Sua dut diem luong ngang/doc cua print:
    - Print ticket khong con tin vao `Template.Orientation` cu neu no lech voi kich thuoc thiet ke.
    - `PageOrientation` duoc tinh truc tiep tu `Template.WidthMm/HeightMm`: width >= height la Landscape, nguoc lai la Portrait.
    - Driver paper match phai dung dung chieu width/height, khong con match bang min/max vi cach do co the lay kho giay doc cho tem ngang.
    - Khi tao template moi hoac chon paper tu driver, app dong bo `Template.Orientation` theo kich thuoc thuc te cua tem.
    - Them `LabelGeometry` de dung chung rule orient size, tranh moi noi swap ngang/doc mot kieu.
  - Them test `label orientation follows design dimensions`.
  - `dotnet build ANLAbel.slnx -p:UseSharedCompilation=false -nodeReuse:false` thanh cong cho version `0.032`.
  - Chay `src\ANLAbel.Tests\bin\Debug\net8.0-windows\ANLAbel.Tests.exe` tat ca test PASS cho version `0.032`.
  - Bo sung chuan hoa driver media size cho truong hop driver tra paper theo chieu doc nhung tem thiet ke dang ngang:
    - Neu driver co paper cung kich thuoc nhung bi dao width/height, app giu driver media name nhung tao `PageMediaSize` theo dung width/height cua thiet ke.
    - Cach nay tranh viec tem ngang 100x50 bi gui thanh paper doc 50x100 va lam mat chu.
  - Mo rong test `label orientation follows design dimensions` de kiem tra paper driver bi dao chieu van normalize ve landscape design.
   - Version hien thi duoc tang len `0.033` cho thay doi driver media size orientation.
   - Sua dut diem chieu in bi xoay tren may in tem cong nghiep:
     - `PrintService.ResolvePageOrientation()` gio luon tra ve `PageOrientation.Portrait` cho may in tem nhan cong nghiep.
     - Nguyen nhan: khi label la landscape (width > height), code cu set `PageOrientation.Landscape` lam driver xoay noi dung 90°, tem in ra bi doc.
     - Kich thuoc giay chinh xac van duoc truyen qua `PageMediaSize`, khong can page orientation thay doi.
     - Cap nhat test `label orientation follows design dimensions` de dung voi hanh vi moi.
   - Bo sung xoay object (Rotation) trong designer va print:
     - `LabelDesignerCanvas` ap dung `RenderTransform` voi `RotateTransform` theo `LabelObject.Rotation` (0°, 90°, 180°, 270°).
     - `LabelVisualRenderer.DrawObject()` ap dung `RotateTransform` trong print pipeline, centered on object.
     - Properties panel co phan `Transform` voi ComboBox chon goc xoay 0°/90°/180°/270°.
   - `dotnet build ANLAbel.slnx -p:UseSharedCompilation=false -nodeReuse:false` thanh cong cho version `0.034` (0 warning, 0 error).
   - Chay `src\ANLAbel.Tests\bin\Debug\net8.0-windows\ANLAbel.Tests.exe` tat ca 17 test PASS cho version `0.034`.
   - Sua loi in tem bi mo, bi cat con mot phan noi dung va bi xoay doc tren may in tem:
     - GDI print pipeline dat `Graphics.PageUnit = Pixel` va ve vao rectangle tinh theo DPI thiet bi, khong con dua pixel bitmap vao don vi 1/100 inch cua GDI.
     - Bitmap print duoc render theo DPI X/Y thuc te cua driver va clone khoi stream PNG truoc khi gui xuong printer.
     - Driver paper size chi match khi width/height cung chieu voi thiet ke tem; khong match kho giay bi dao chieu vi se lam driver xoay output 90 do.
     - Giu `Landscape = false`; kich thuoc tem ngang/doc do width/height thiet ke quyet dinh, phu hop workflow may in tem cong nghiep.
   - Them test khoa hoi quy cho driver paper match cung chieu va rectangle pixel theo DPI may in.
   - Version hien thi va assembly duoc dong bo len `0.037`.
   - Quay lai pipeline in vector cho ban `0.038` vi huong GDI bitmap lam tem in bi mo tren may in nhiet:
     - Bo `System.Drawing.Printing.PrintDocument`, `RenderTargetBitmap`, PNG trung gian va `Graphics.DrawImage` khoi lenh in that.
     - `PrintService` dung WPF `DocumentPaginator` + `PrintDialog.PrintDocument` de gui visual theo dang vector xuong driver.
     - Van ep `PageMediaSize` bang dung width/height thiet ke tem va `PageOrientation.Portrait` de tranh driver xoay tem ngang thanh doc.
     - Calibration print cung dung chung pipeline vector, khong con raster toan bo tem thanh anh bitmap.
     - Version hien thi va assembly duoc dong bo len `0.038`.

### Phien ban 0.040 - Barcode text, auto-grow, compact UI, window fixes
- Sua loi Printer Setup dialog hien 2 lan khi mo app:
  - Bo `ShowPrinterSetupDialog()` trong `MainWindow_Loaded`; chi hien khi user bam nut `Printer Setup` tren ribbon.
- Sua loi window bi khuut title bar tren may man hinh nho/DPI cao:
  - `MainWindow_Loaded` clamp window size vao `SystemParameters.WorkArea`.
  - `Width/Height` toi da bang kich thuoc man hinh thuc te, tru taskbar.
- Sua loi Print Preview nhay ra app rieng tren taskbar:
  - `PrintPreviewWindow` them `ShowInTaskbar="False"`.
  - Constructor clamp kich thuoc cua so vao work area.
- Them tinh nang barcode noi dung text (ShowBarcodeText):
  - `LabelObject` them `ShowBarcodeText` (mac dinh: true) va `BarcodeTextFontSizePt` (mac dinh: 7pt).
  - `LabelVisualRenderer.DrawBarcodeText()` ve text centered duoi barcode.
  - 1D barcodes (vector): reserve textHeight tu barcode height, ve text ben duoi.
  - 2D codes (QR, DataMatrix): scale barcode nho hon, text ben duoi.
  - Properties panel co checkbox `Show text` + TextBox font size cho barcode.
- Auto-grow barcode width khi noi dung dai:
  - Vector renderer tinh `requiredWidthDip` tu `WidthModules * moduleWidthDip`.
  - Neu content dai hon container, tu dong mo rong `rect.Width` de barcode khong bi compress.
  - Dam bao in ra luon doc duoc, khong bi "squished".
- Compact Properties panel UI/UX:
  - Giam padding: 7→5, 9→6.
  - Giam margin: `0,0,0,6` → `0,0,0,3`.
  - Giam fontSize header: 13→12.
  - Giam fontSize labels: them `FontSize="11"` cho labels.
  - Giam MinHeight TextBox: 30→26.
  - Giam spacing giua ComboBox/TextBox (margin bottom: 8→5).
  - Tiet kiem ~30% khong gian doc, chua cho tinh nang sau nay.
- Version hien thi va assembly duoc dong bo len `0.040`.
- `dotnet build ANLAbel.slnx --nologo -v q` thanh cong (0 warning, 0 error).

### Quy tac lam viec tiep theo
- Sau moi phan hoan thanh, cap nhat outline nay voi noi dung da lam va cach verify.
- Sau khi build/test xong, tu dong mo app de test neu thay doi lien quan UI hoac workflow nguoi dung.

### Phien ban 0.058 - Designer stability va nen Data Source Manager

- Loai bo mutation geometry trong render/PreviewRow cua designer; text auto-fit chi con la visual.
- Sua matrix square sizing theo property nguoi dung vua doi va giu tam cho chieu app tu dieu chinh.
- Giam snap 3 mm xuong 1 mm, Alt tam tat snap, clamp drag du bon canh.
- Lost mouse capture/Esc khoi phuc ca group drag de tranh object teleport.
- Excel async co cancel, timeout UNC/network 30 giay va `FileShare.ReadWrite`; re-link khong doc workbook tren UI thread.
- Them nen `DataSource`/`DataSourceRegistry`, `DataSourceId`, `KeyValue`; UI manager va watcher lam o dot tiep.
- Them regression test geometry khi doi preview row, cancel Excel va registry CRUD.

### Phien ban 0.061 - Designer interaction controls

- Them toggle `Snap objects` tren ribbon va context menu cua canvas; Alt van tam bo snap trong luc keo.
- Luu preference snap rieng theo may tai `%LocalAppData%\ANLAbel\designer-preferences.json`, khong ghi vao template.
- Keyboard nudge hien X/Y moi hoac so object da move tren status bar.
- Them xUnit cho round-trip preference va fallback an toan khi JSON bi hong.

### Phien ban 0.062 - Excel reliability TC7

- Them `ExcelDataReadException` voi ma loi file mat, workbook hong, sheet mat, header row sai.
- Missing-sheet message liet ke cac sheet hien co; workbook hong co message ro rang thay vi exception thu vien kho hieu.
- Them 6 xUnit cho missing/corrupt/renamed sheet/header trung-rong/file dang mo/header ngoai vung.
