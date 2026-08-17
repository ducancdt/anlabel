# Tầm nhìn sản phẩm ANLAbel Next

## 1. Định vị

ANLAbel Next không cố trở thành bản sao rút gọn của NiceLabel hoặc BarTender. Sản phẩm sẽ chiếm khoảng trống giữa công cụ thiết kế tem đơn giản và enterprise labeling suite:

- cài đặt nhanh, chạy offline và không bắt buộc license server;
- đầu ra xác định cho máy in tem công nghiệp;
- document và dữ liệu có thể kiểm tra, diff và tự động hóa;
- trải nghiệm tách rõ Author, Operator và Administrator;
- tích hợp local qua CSV/Excel/file-drop nhưng không yêu cầu dịch vụ mạng.

Thông điệp sản phẩm đề xuất:

> Design once. Validate with real data. Print predictably anywhere.

## 2. Người dùng chính

| Persona | Công việc thật | Điều họ sợ nhất | Workspace mặc định |
| --- | --- | --- | --- |
| Label Author | tạo và sửa template, bind dữ liệu, kiểm tra barcode | layout nhảy, preview khác bản in, binding âm thầm hỏng | Design |
| Production Operator | chọn job, record, printer, số lượng và in | in sai tem, trùng tem, chọn nhầm printer/media | Print Center |
| Integration Engineer | nhận CSV/Excel/file-drop local và chạy job tự động | job mất, retry in trùng, lỗi không truy vết được | Automate |
| Template Maintainer | so sánh revision và test sample data | dùng nhầm revision hoặc binding cũ | Library |
| Printer Maintainer | quản lý profile, queue và chẩn đoán local | driver/media thay đổi làm sai kích thước | Print Center |

Doanh nghiệp nhỏ có thể dùng Author + Operator trong một executable. Các vai trò enterprise phải là capability bật thêm, không làm nặng đường dùng cơ bản.

## 3. Jobs to be done

### Thiết kế

- Tạo đúng kích thước vật lý mà không cần hiểu WPF DIP hoặc printer dots.
- Sắp xếp nhiều object nhanh bằng guides, align, distribute, group, layer và constraint.
- Biết ngay object nào bị tràn, thiếu dữ liệu, barcode khó quét hoặc dùng font không portable.
- Thay printer/profile mà nhìn thấy tác động trước khi lưu hoặc in.

### Dữ liệu

- Kết nối Excel nhanh nhưng không khóa kiến trúc vào Excel.
- Tạo variable có kiểu, sample value, validation và transform có thể dùng lại.
- Xem lineage: data nào đi từ source nào, qua transform nào, tới object nào.
- Test template với một bộ sample records có thể commit cùng document.

### In

- Chọn template đã publish, dữ liệu, printer và số lượng trong một flow ngắn.
- Chặn lỗi trước khi spool; giải thích rõ lỗi ở record/object nào.
- Truy vết từng job, reprint có kiểm soát và chống in trùng.
- Hoạt động ổn định khi printer offline, driver thay đổi hoặc data source chậm.

### Tích hợp và quản trị

- Nhận JSON/CSV/file-drop local, map vào variable và chạy job có idempotency.
- So sánh revision bằng semantic diff và visual diff.
- Tách Draft và Published bằng state local, không yêu cầu dịch vụ tài khoản hay người duyệt trong execution flow.
- Xuất audit mà không lộ dữ liệu nhạy cảm ngoài ý muốn.

## 4. Năm workspace

### Home

Recent documents, templates, pinned print jobs, trạng thái printer và quick start. Home không phải nơi nhồi mọi tính năng.

### Design

Scene tree/layers bên trái, viewport ở giữa, inspector/preflight bên phải, status bar ở dưới. Command surface đổi theo selection và mode.

### Data

Connections, typed variables, transforms, sample records, mapping và diagnostics. Object binding là kết quả của data graph, không còn chỉ là chuỗi nằm rải rác trong từng object.

### Print Center

Document đã publish, record filter, prompted variables, preview, profile/printer, quantity, preflight và job status. Không có lệnh sửa geometry.

### Automate / Maintain

Automation chỉ dành cho file-drop local; Maintain dành cho revisions, resources,
printer profiles, history và settings local. Không có permissions server, login hay cloud administration.

## 5. Nguyên tắc sản phẩm

1. **Physical truth first:** mm, DPI, media, feed và calibration là contract chứ không phải metadata trang trí.
2. **One semantic pipeline:** designer, preview, export và print resolve cùng scene.
3. **Progressive disclosure:** lệnh thường dùng lộ ra; cấu hình chuyên sâu nằm ở inspector/advanced sheet.
4. **Safe by default:** stale data, invalid barcode, missing font/media và revision chưa publish phải fail closed.
5. **Roles, not clutter:** author và operator không dùng cùng một màn hình dày đặc.
6. **Local-only:** toàn bộ workflow chạy không Internet; không có sync hay dịch vụ online.
7. **Explainable automation:** mỗi transform, validation, retry và print decision có log.
8. **Portable documents:** resource, font requirement, sample data và schema dependency được khai báo.
9. **No invisible mutation:** render, preview và refresh không được âm thầm sửa tài liệu.
10. **Open extension points:** connector, transform, validator, renderer và dispatcher có contract ổn định.

## 6. Lợi thế khác biệt cần xây

| Lợi thế | Ý nghĩa |
| --- | --- |
| Deterministic Label Compiler | Cùng input + document revision + printer profile tạo cùng RenderPlan và hash |
| Explainable Preflight | Lỗi gắn với node, record, rule, mức độ và hướng khắc phục |
| Portable Printer Profiles | Profile có version, test evidence và compatibility state thay vì state ngầm trong driver |
| Local Revision Safety | Revision, diff, publish state và audit đều chạy local |
| Open Job Manifest | Mỗi job có manifest dễ đọc, phục vụ audit, reprint và tích hợp |
| Operator-safe UX | Workflow in chuyên dụng giảm click và giảm khả năng sửa nhầm template |

## 7. Chỉ số thành công

### Reliability

- 100% golden cases render đồng nhất giữa viewport/preview/print compiler.
- Không có model mutation trong render path.
- Không có I/O chậm trên UI thread.
- Mọi print job đạt trạng thái cuối hoặc có recovery state rõ ràng.

### Automated interaction quality

- Automated UI evidence covers create/open, bind local data, preview and print preparation.
- Core actions remain reachable at 1024×600 and 100/125/150/200% scaling.
- Keyboard/focus/AutomationId contracts cover the main design and print-preparation flow.
- User testing may happen separately but is not an execution gate or assigned task.

### Maintainability

- Không class production nào vượt ngưỡng 1.000 dòng nếu không có ADR ngoại lệ.
- Mỗi domain service có contract và unit tests độc lập với WPF.
- Document migration được test từ mọi schema version được support.
- Feature mới không cần thêm logic trực tiếp vào `MainWindow.xaml.cs` hoặc mega-view-model.

## 8. Những thứ chưa làm ngay

- Không xây cloud SaaS, web app, login, sync hoặc remote service.
- Không làm full PowerForms clone; trước hết sinh operator form từ typed variables.
- Không hỗ trợ mọi database connector ngay; tạo abstraction rồi thêm theo nhu cầu có bằng chứng.
- Không thay Windows print pipeline bằng raw printer language trong product scope hiện tại.
- Không rewrite sang WinUI/Avalonia chỉ vì thẩm mỹ.
