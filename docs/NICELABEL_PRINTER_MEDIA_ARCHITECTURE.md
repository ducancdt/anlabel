# Kien truc ket noi may in va kho tem theo NiceLabel

> Reference status (2026-09-15): competitor research, not a full implementation
> requirement or parity backlog. Apply [reference policy](REFERENCE_POLICY.md)
> and [core standard](CORE_SOFTWARE_STANDARD.md) to select ANLAbel core behavior.

**Trang thai:** nghien cuu va kien truc muc tieu (2026-09-14)  
**Nguon chinh:** `ug-NiceLabel_2019_PowerForms_User_Guide-en.md`, dac biet cac muc 2.5, 3.1.1-3.1.5, 3.2.1-3.2.4, 5.1, 5.3-5.7 va 7.6.2.2.

Tai lieu nay de xuat kien truc tham khao cho ket noi may in, page/media va kho tem; pham vi duoc chap nhan do `CORE_SOFTWARE_STANDARD.md` quyet dinh. No khong thay doi contract `Text`/`TextBox`; label geometry van la du lieu tac gia va renderer khong duoc ghi nguoc vao document.

## Ket qua doc tai lieu NiceLabel

NiceLabel coi may in la nguon capability, khong chi la ten queue:

- Danh sach may in den tu cac printer da cai tren Windows; printer properties mo truc tiep dialog cua driver.
- Khi doi printer, page size ve lai che do automatic va kich thuoc hien thi duoc tai danh theo capability cua driver.
- Thiet lap printer co hai nguon: dung thiet lap driver, hoac luu custom settings theo tung label de label co tinh portable.
- Roll/thermal media uu tien page size automatic do driver quyet dinh. Sheet media cho phep khai bao paper width/height, margins, labels across va gaps.
- Label width/height, margins, radius, labels across va variable label size la mien thiet ke rieng. Stock la preset co the override cac gia tri thu cong, nhung stock khong tu tao capability cho mot printer khong tuong thich.
- Speed, darkness, media type va offsets la capability/parameter phu thuoc driver; gia tri hop le khong nen hard-code chung cho moi hang may.
- Truoc khi gui job, effective printer/ticket/media phai duoc resolve lai. Khong duoc coi preview hoac gia tri cache la bang chung rang queue/driver hien tai van con hop le.

## Van de can sua trong kien truc hien tai

`PrinterDiscoveryService` hien gan `StandardLabelSizes.All` vao moi `PrinterInfo`. Cach nay lam UI co ve nhu moi driver deu ho tro moi preset, trong khi tai lieu NiceLabel phan biet ro driver media, stock preset va custom label size. `PrinterSetupWindow` cung dang dung mot selection de dong thoi doi label geometry va physical paper, nen chua bieu dien duoc roll automatic ma khong sua khung thiet ke.

Nhung phan dang dung va can giu:

- queue name duoc luu trong document va khong am tham fallback sang queue dau tien;
- preview/print resolve lai queue va effective `PrintTicket`;
- `PrintContractGuard`, output-contract hash va preflight fail closed;
- mm la don vi persisted, DIP chi dung tren man hinh va dots chi xuat hien trong effective print plan;
- print calibration la output correction, khong phai label geometry.

## Kien truc muc tieu

~~~mermaid
flowchart LR
    UI["Printer & Label Setup\noperator intent"] --> QD["PrinterQueueDiscovery\nqueue identity only"]
    UI --> CS["PrinterCapabilityService\nlazy snapshot"]
    QD --> CS
    CS --> CAP["PrinterCapabilitySnapshot\ndriver media, DPI, parameters, diagnostics"]
    UI --> LS["LabelStockService\nbuilt-in/vendor/user presets"]
    CAP --> RES["PrinterMediaResolver"]
    LS --> RES
    DOC["LabelTemplate\nauthored label geometry"] --> RES
    RES --> BIND["ResolvedPrinterBinding\nqueue + effective media + provenance"]
    BIND --> PLAN["Immutable PrintRenderPlan"]
    PLAN --> PREFLIGHT["Preflight + contract hash"]
    PREFLIGHT --> DISPATCH["PrintService dispatch"]
~~~

### 1. Queue discovery

`PrinterQueueDiscovery` chi liet ke Windows queues va identity on dinh:

- canonical queue name;
- driver name/version neu doc duoc;
- local/network/default markers;
- discovery timestamp va diagnostic rieng neu Windows enumeration loi.

Buoc nay phai nhanh, read-only va khong doc tat ca capabilities cua tat ca printers tren UI thread.

### 2. Capability snapshot theo printer dang chon

`PrinterCapabilityService` duoc goi lazy khi operator chon printer hoac khi preview/dispatch can revalidate. Snapshot nen immutable va co:

- driver paper/media list voi `name`, `widthMm`, `heightMm`, source API va raw identity;
- default/effective DPI, imageable area va ho tro custom media;
- speed/darkness/media-type values neu driver expose;
- `IsComplete`, `CapturedAt`, `DriverIdentity` va diagnostics.

Thu tu doc de nghi:

1. WPF `PrintQueue.GetPrintCapabilities(PrintTicket)`;
2. Windows/GDI paper catalog gan driver hon (`PrinterSettings.PaperSizes` hoac `DeviceCapabilitiesW`) neu WPF thieu;
3. khong biet capability thi hien ro `Unavailable/Incomplete`, khong gan catalog chung vao printer nhu the driver da khai bao.

Cache chi la toi uu. Cache key phai gom queue + driver identity; preview va dispatch van phai revalidate effective ticket.

### 3. Page/media tach khoi label geometry

Ba khai niem duoc luu rieng:

| Khai niem | Owner | Y nghia |
| --- | --- | --- |
| Authored label | `LabelTemplate` | Width/height, margins va noi dung ma designer dang thiet ke. Chi explicit user edit/stock apply moi thay doi. |
| Physical page/media | `PrinterProfile` + resolved binding | Roll/sheet, automatic/manual, driver media identity va kich thuoc physical gui cho driver. |
| Stock preset | `LabelStockService` | Goi gia tri tien loi cho label/page/labels-across; co provenance va compatibility, khong phai capability cua driver. |

Che do page size:

- `DriverAutomatic`: phu hop thermal roll. Driver/ticket la nguon page media; khong tu dong viet nguoc width/height vao label dang mo.
- `Manual`: operator khai bao physical page/media; label van giu kich thuoc rieng va resolver kiem tra label co nam trong media hay khong.
- `Stock`: operator chu dong apply preset. Day la thao tac ro rang co the doi label geometry, labels-across va gaps sau khi UI preview impact.

### 4. Provenance va compatibility

Moi media item can source ro rang: `DriverWpf`, `DriverWin32`, `VendorStock`, `BuiltInStock`, hoac `UserCustom`. UI khong tron cac item nay trong mot list khong nhan dien.

`PrinterMediaResolver` tra ve mot trong cac trang thai:

- `Compatible`: driver media trung dung chieu/kich thuoc;
- `CustomSupported`: driver chap nhan custom width/height;
- `DriverAutomatic`: driver so huu roll length/page size;
- `Mismatch`: co media nhung khong chua duoc label;
- `Unknown`: capability thieu, can operator/driver properties xac nhan;
- `Unavailable`: queue/driver khong con truy cap duoc.

Khong swap width/height am tham de “lam cho khop”. Orientation va rotation la quyet dinh rieng, duoc dua vao effective binding va hash.

### 5. State machine cua UI

~~~mermaid
stateDiagram-v2
    [*] --> QueueUnselected
    QueueUnselected --> LoadingCapabilities: select queue
    LoadingCapabilities --> Ready: snapshot complete
    LoadingCapabilities --> CapabilityWarning: partial/failed
    Ready --> LoadingCapabilities: change queue
    Ready --> ResolveMedia: choose automatic/manual/stock
    CapabilityWarning --> DriverProperties: inspect driver
    CapabilityWarning --> ResolveMedia: explicit manual/custom
    ResolveMedia --> Compatible
    ResolveMedia --> Incompatible
    Incompatible --> ResolveMedia: edit media or stock
    Compatible --> Applied: explicit Apply
    Applied --> [*]
~~~

UI setup theo workflow NiceLabel (khong gom Stocks) co ba khoi:

1. `Printer`: queue, settings source (`Label`/`Driver`), driver properties va capability status.
2. `Label Dimensions`: width/height va orientation; day la authored geometry, chi Apply moi ghi.
3. `Paper`: roll automatic hoac sheet/manual, driver media list, hoac milimet nhap tren tab Label Dimensions.

Nut Apply phai hien tom tat nhung field nao se thay doi. Doi printer chi reload capability; khong commit document cho den khi operator bam Apply.

## Mapping vao source tree

| Boundary | Noi dat de nghi |
| --- | --- |
| Queue enumeration | `ANLAbel.Printing/PrinterProfiles/PrinterDiscoveryService.cs` (giu queue-only) |
| Lazy capability read | them `PrinterCapabilityService` va adapter WPF/Win32 trong `ANLAbel.Printing/PrinterProfiles/` |
| Pure media resolution | them contract/resolver deterministic trong `ANLAbel.Core/Printing/` |
| Stock catalog | doi `StandardLabelSizes` thanh stock source; khong gan truc tiep vao moi `PrinterInfo` |
| Setup orchestration | ViewModel rieng cho `PrinterSetupWindow`; code-behind chi mo dialog driver va dong window |
| Persisted data | mo rong `PrinterProfile` theo schema migration, giu doc duoc document cu |
| Effective validation | tiep tuc dung `PrintService`, `PrintContractGuard`, `DispatchRevalidationContract` va manifest hash |

## Lo trinh trien khai an toan

1. Them pure contracts, provenance enum, resolver va unit tests; chua doi UI.
2. Them capability adapters va fixture tests cho WPF/Win32 normalization/deduplication; timeout va cancellation la bat buoc.
3. Chuyen `PrinterDiscoveryService` ve queue-only, load capability lazy trong setup/preview.
4. Tach UI Printer, Page/Media, Label va Stock; them impact summary truoc Apply.
5. Persist schema moi voi migration tu `PrinterProfile` hien tai.
6. Revalidate tai preview va dispatch, bo moi fallback lam sai queue/media identity.
7. Test voi printer families thuc (Zebra/SATO/TSC/Brother va mot office printer); ghi lai driver version, media, DPI, orientation va physical output evidence.

## Regression gates bat buoc

- doi queue khong sua `LabelTemplate.WidthMm/HeightMm` truoc explicit Apply;
- driver automatic khong gan stock catalog thanh driver capability;
- manual/stock apply giu provenance va round-trip qua `.anlabel`;
- queue/driver thay doi sau preview bi dispatch revalidation chan;
- media mismatch/unknown co diagnostic ro, khong fallback sang printer dau tien;
- preview va print dung cung effective media identity/hash;
- tat ca gate Text/TextBox trong `AGENTS.md` tiep tuc pass nguyen ven.

## Trang thai trien khai 2026-09-14

Foundation va luong Printer Setup da duoc dua vao code:

- `PrinterSetupWindow` hien tach ro ba buoc `Printer` / `Label Dimensions` / `Paper`. Tab `Stocks` / **Khổ có sẵn** da bi go khoi duong operator; khong con danh muc nhieu loai kho giay cua phan mem. Figma: `https://www.figma.com/design/zdN71qfzrYV6pPt1b2FRRc` wrapper `26:22`, Printer `26:23`, Label Dimensions `30:56`, Paper `31:82`, Preview printer/paper `26:26`.
- Compatibility duoc tinh lai ngay khi operator doi label W/H, page-source hoac driver media. Nut `Apply` chi enabled khi exact queue da co snapshot va `PrinterMediaResolver` tra ve trang thai co the dispatch; diagnostic duoc hien ngay trong tab Paper/Media thay vi doi den luc bam Apply.
- Cac tab, queue/media selector va Apply co UI Automation identity on dinh de regression/UI automation co the truy vet dung workflow.

- `PrinterDiscoveryService` chi enumerate Windows queues; khong con gan stock catalog vao `PrinterInfo.PaperSizes`.
- `PrinterCapabilityService` load capability lazy tren STA, uu tien WPF Print Schema va fallback `PrinterSettings.PaperSizes`; moi row giu `DriverWpf`/`DriverWin32` provenance va raw driver identity.
- `PrinterMediaResolver` la pure policy, giu tolerance 0.5 mm, khong swap hai truc am tham va tra ve status compatibility ro rang.
- `PrinterProfile` persist rieng authored stock name, physical media name, provenance, driver media identity va driver identity; document cu van doc duoc qua gia tri mac dinh.
- `PrinterSetupWindow` tach software stock, page/media source, driver media va authored label. Doi queue chi load lai capability, khong viet vao width/height label.
- `PrintService` chi ep custom/named `PageMediaSize` trong Manual mode. `DriverAutomatic` giu media cua driver va van qua effective-ticket validation truoc dispatch.
- Capability read co timeout 8 giay de driver ben thu ba khong treo UI/preview vo han. WPF Print Schema va `PrinterSettings.PaperSizes` duoc doc bo sung cho nhau thay vi chi goi Win32 khi WPF tra ve rong; neu mot nguon loi, snapshot giu diagnostic va `IsComplete=false` de non-match khong bi ket luan sai thanh mismatch.
- UI van cho phep exact media match tu snapshot partial, nhung hien canh bao partial capability; raw Win32 identity giu nguyen dinh dang cu de document da luu tiep tuc chon lai dung media.
- Pure resolver xac minh ca hai quan he: driver candidate phai match dung chieu physical request, va physical request phai match authored label (cho phep orientation swap ro rang). Mot media nho/khac kho khong con duoc bao `Compatible` chi vi no match voi chinh row driver dang chon.
- Snapshot cung giu resolution pairs va effective ticket DPI tu driver. UI scalar DPI chi dung square modes driver advertise; authored DPI duoc giu lai kem warning neu khong co trong capability, con non-square resolution chi duoc revalidate trong effective output contract thay vi bi bien thanh mot gia tri scalar sai.

Phan con can evidence ben ngoai code la matrix driver/may in that (Zebra/SATO/TSC/Brother), hot-unplug/spooler restart va do tem vat ly.

Probe capability chi-doc:

```powershell
dotnet run --project tools/PrinterCapabilityProbe -- "Exact Windows queue name"
dotnet run --project tools/PrinterCapabilityProbe -- --all
dotnet run --project tools/PrinterCapabilityProbe -- --all --require-fully-observed
```

Probe chi enumerate queue va doc snapshot media/DPI/diagnostic thanh JSON. Truong `DriverEvidence` tong hop ro `QueueBlocked`, `QueueHealthUnknown`, missing/invalid effective page-imageable area, partial catalog hoac `FullyObserved`. Mac dinh probe chi thu thap; `--require-fully-observed` tra exit `3` neu queue ton tai nhung evidence chua day du (`2` danh rieng cho unavailable queue). No khong mo PrintDialog, khong ghi DEVMODE va khong gui print job; vi vay output la driver observation, khong phai bang chung tem da in vat ly. Ke ca `FullyObserved` cung khong tu cho phep dispatch: template-specific ticket/media/DPI van phai revalidate ngay truoc spool.

### Read-only driver observation 2026-09-14

Lenh probe da chay tren hai queue tem dang cai tren may phat trien:

- `Xprinter XP-DT426B`: available/complete, effective DPI `203 x 203`; WPF khong advertise resolution row, Win32 expose mot media `USER` `103.89 x 180.09 mm` (`RawKind=256`).
- `Datamax E-4203`: available/complete, effective DPI `203 x 203`; WPF advertise mot resolution `203 x 203`, Win32 expose 21 media rows. Mot so ten media va dimension khong trung tuyet doi (vi du `50x30mm` duoc driver bao `52.58 x 29.97 mm`), nen tolerance/resolver khong duoc suy dien kich thuoc tu ten.
- Page observation bo sung: Xprinter effective page `104 x 180 mm` nhung driver tra imageable origin `2 x 0 mm`, extent xap xi `8196.31 x 0 mm`; geometry nay invalid va bi danh dau unverified. Datamax effective page `62.5 x 40 mm`, imageable origin `1.25 x 0 mm`, extent `60.05 x 40.03 mm`; shared bounded-tolerance contract xac nhan usable.
- Queue-health observation hien tai cua ca hai queue la `Ready`, nhung `PrinterDriverEvidenceContract` van phan loai Xprinter la `ImageableAreaInvalid`; Datamax chi duoc `FullyObserved`. Ket qua nay co chu y tach “khong thay Windows queue blocker” khoi “driver evidence day du”.
- Custom-media observation dung `DeviceCapabilitiesW`: chi khi `DC_FIELDS` co ca `dmPaperWidth/dmPaperLength` va `DC_MINEXTENT/DC_MAXEXTENT` tao thanh range hop le moi dat `SupportsCustomMedia`. Xprinter tra sentinel suy bien `0.1 x 0.1` den `0.1 x 0.1 mm` nen bi loai; Datamax tra range `12.7 x 6.4` den `109.2 x 1270 mm`. Resolver khong con chap nhan mot custom size chi dua tren boolean khong co bounds.
- Standard-feature observation: Xprinter current ticket co `PageMediaType=Plain` nhung `PageMediaTypeCapability` rong, nen gia tri nay duoc danh dau “current, not advertised” thay vi goi la supported choice. Datamax expose rong ca media type va output quality. Khong driver nao trong hai queue nay cung cap generic speed/darkness evidence qua cac feature chuan.
- Secure feature inventory chi doc QName/namespace cua `psf:Feature`, khong retain Option/Value/raw XML; DTD/external entity bi prohibit va input bi gioi han. Xprinter expose 5 standard features (`JobInputBin`, `PageMediaSize`, `PageOrientation`, `PageOutputColor`, `PageTrueTypeFontMode`) va 0 private feature. Datamax expose cung tap tren + `PageResolution`, cung 0 private feature. Do do darkness/speed tren hai driver nay khong ton tai nhu Print Schema feature co the mapping portable; no van thuoc driver preferences/private DEVMODE.

Observation nay chung minh capability sources co the khac nhau, ten media khong phai geometry evidence, va queue available khong dong nghia imageable geometry hop le. No chua chung minh output vat ly, spooler restart/hot-unplug hay barcode scan.

### Driver identity revalidation

`PrinterProfile.DriverIdentity` duoc xem la evidence sau khi Printer Setup capture thanh cong. Moi lan tao effective plan, `PrintService` doc lai `PrintQueue.QueueDriver.Name`: mismatch hoac khong doc duoc identity da save se block truoc khi merge PrintTicket va truoc spool. Document cu co identity rong tiep tuc theo legacy-unverified policy de giu backward compatibility. Win32 media fallback khong duoc dung queue name thay cho driver identity.

Printer Setup mo dung Windows printing preferences cua selected queue bang `rundll32 printui.dll,PrintUIEntry /e /n <queue>`, theo Microsoft PrintUI command contract. Queue name duoc truyen qua `ProcessStartInfo.ArgumentList`, khong noi thanh shell command. Sau khi operator dong preferences, snapshot media/DPI cu bi huy va capability duoc doc lai; action nay khong gui print job.

Capability snapshot va probe cung ghi effective `PageMediaSize` va `PageImageableArea` (origin/extent). Observation dung shared `PrintableAreaContract` de danh dau verified chi khi cac gia tri finite/positive va rectangle nam trong effective media; thieu geometry van hien unverified, khong gia dinh toan bo label printable.

`PrinterSettingsSource` co runtime semantics ro: `Label` cho phep app apply PageMediaSize/DPI da save vao requested ticket; `Driver` giu current driver preferences va khong ghi de hai field nay. Ca hai mode deu di qua `MergeAndValidatePrintTicket`, effective media/DPI/imageable validation va last-mile revalidation; Driver mode khong phai bo qua preflight.

Queue existence va submission readiness la hai evidence khac nhau. `PrinterQueueHealthReader` refresh queue va map cac Windows signal `Paused`, `Offline`, `OutOfPaper`, `PaperJam`, `NeedUserIntervention`, `IsInError` qua pure contract. Blocking/unknown state dung dispatch va automation preflight; `Ready` chi nghia la Windows khong bao blocker tai timestamp quan sat, khong chung minh thiet bi se in hoac tem da ra vat ly.

Media resolver kiem tra known queue blocker truoc ca `DriverAutomatic`. Vi vay Printer Setup khong the Apply mot automatic-media profile khi snapshot vua bao offline/paused/media fault; day la early feedback, khong thay the queue refresh va effective-ticket validation ngay truoc dispatch.

Capability probe fingerprint current driver `PrintTicket` bang SHA-256 va chi xuat hash, khong xuat raw XML/DEVMODE. Capability read va dispatch dung chung mot implementation; neu driver khong serialize duoc ticket, evidence la `ObservedTicketUnverified` thay vi van goi `FullyObserved`.

Snapshot cung doc `PageMediaTypeCapability` va `OutputQualityCapability` chuan Print Schema, kem gia tri effective trong current ticket. Collection rong van co nghia la driver khong expose lua chon qua feature chuan; ANLAbel khong suy dien darkness/speed tu cac enum nay va khong hard-code gia tri vendor-specific.

Tai lieu doi chieu chinh thuc:

- NiceLabel `Label Properties`: automatic page size tren roll duoc driver dinh nghia; manual page size dung cho sheet/custom paper: <https://help.nicelabel.com/hc/en-001/articles/4403719015697-Label-Properties>
- NiceLabel `New Label Setup Wizard`: thermal printer uu tien automatic sizing va printer properties di thang vao driver: <https://help.nicelabel.com/hc/en-001/articles/4402145578513-New-Label-Setup-Wizard>
- Microsoft `DeviceCapabilitiesW`: `DC_PAPERNAMES`, `DC_PAPERS` va `DC_PAPERSIZE`; kich thuoc driver tra theo 0.1 mm va portrait orientation: <https://learn.microsoft.com/en-us/windows/win32/api/wingdi/nf-wingdi-devicecapabilitiesw>
- Microsoft `PageMediaTypeCapability`: media types driver expose qua Print Schema; collection rong neu feature khong duoc support: <https://learn.microsoft.com/en-us/dotnet/api/system.printing.printcapabilities.pagemediatypecapability>
- Microsoft `OutputQualityCapability`: quality modes driver expose qua `PageOutputQuality`: <https://learn.microsoft.com/en-us/dotnet/api/system.printing.printcapabilities.outputqualitycapability>
- Microsoft `Print Schema`: cho phep third-party extension trong namespace rieng, nhung client phai bao toan namespace/identity va khong suy dien semantics: <https://learn.microsoft.com/en-us/windows/win32/printdocs/printschema>

## Figma

Moc UI tham chieu nam trong file `ANLAbel - NiceLabel Shell Recreation`: `https://www.figma.com/design/zdN71qfzrYV6pPt1b2FRRc`, frame `ANLAbel - NiceLabel Workspace Overview v2` (`12:2`). Frame moi duoc dung de so sanh bo cuc workspace; Figma la design input, khong phai bang chung runtime hay physical printer compatibility.
