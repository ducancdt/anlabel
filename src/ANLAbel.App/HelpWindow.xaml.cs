using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace ANLAbel.App;

public partial class HelpWindow : Window
{
    private readonly Dictionary<string, UIElement> _enSections = new();
    private readonly Dictionary<string, UIElement> _viSections = new();
    private bool _isVietnamese;
    private string _currentKey = "overview";
    private bool _initialized;

    public HelpWindow()
    {
        InitializeComponent();
        BuildEnglishSections();
        BuildVietnameseSections();
        _initialized = true;
        ShowSection("overview");
    }

    private void NavListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!_initialized) return;
        if (NavListBox.SelectedItem is ListBoxItem item && item.Tag is string tag)
        {
            _currentKey = tag;
            ShowSection(tag);
        }
    }

    private void LangEn_Checked(object sender, RoutedEventArgs e)
    {
        if (!_initialized) return;
        _isVietnamese = false;
        GuideTitle.Text = "📖 User Guide";
        UpdateNavLabels();
        ShowSection(_currentKey);
    }

    private void LangVi_Checked(object sender, RoutedEventArgs e)
    {
        if (!_initialized) return;
        _isVietnamese = true;
        GuideTitle.Text = "📖 Hướng dẫn sử dụng";
        UpdateNavLabels();
        ShowSection(_currentKey);
    }

    private void UpdateNavLabels()
    {
        var items = NavListBox.Items.OfType<ListBoxItem>().ToArray();
        var vi = _isVietnamese;
        for (var i = 0; i < items.Length; i++)
        {
            items[i].Content = (items[i].Tag?.ToString()) switch
            {
                "overview" => vi ? "🏠 Tổng quan" : "🏠 Overview",
                "quickstart" => vi ? "⚡ Hướng dẫn nhanh" : "⚡ Quick Start",
                "ribbon" => vi ? "🔧 Thanh Ribbon" : "🔧 Ribbon Toolbar",
                "canvas" => vi ? "🎨 Bảng thiết kế" : "🎨 Design Canvas",
                "objects" => vi ? "📦 Đối tượng" : "📦 Objects",
                "barcode" => vi ? "📊 Mã vạch" : "📊 Barcode",
                "binding" => vi ? "🔗 Liên kết dữ liệu" : "🔗 Data Binding",
                "excel" => vi ? "📑 Dữ liệu Excel" : "📑 Excel Data",
                "printer" => vi ? "🖨️ In ấn" : "🖨️ Printing",
                "calibration" => vi ? "🎯 Hiệu chỉnh" : "🎯 Calibration",
                "shortcuts" => vi ? "⌨️ Phím tắt" : "⌨️ Keyboard Shortcuts",
                "about" => vi ? "ℹ️ Giới thiệu" : "ℹ️ About",
                _ => items[i].Content?.ToString() ?? ""
            };
        }
    }

    private void ShowSection(string key)
    {
        ContentStack.Children.Clear();
        var sections = _isVietnamese ? _viSections : _enSections;
        if (sections.TryGetValue(key, out var section))
        {
            ContentStack.Children.Add(section);
        }
    }

    // ===== Helper methods =====
    private static TextBlock H1(string text) => new()
    {
        Text = text, FontSize = 20, FontWeight = FontWeights.Bold,
        Foreground = new SolidColorBrush(Color.FromRgb(0x0F, 0x17, 0x2A)),
        Margin = new Thickness(0, 0, 0, 12)
    };
    private static TextBlock H2(string text) => new()
    {
        Text = text, FontSize = 15, FontWeight = FontWeights.SemiBold,
        Foreground = new SolidColorBrush(Color.FromRgb(0x1E, 0x40, 0xAF)),
        Margin = new Thickness(0, 18, 0, 6)
    };
    private static TextBlock P(string text) => new()
    {
        Text = text, TextWrapping = TextWrapping.Wrap,
        Foreground = new SolidColorBrush(Color.FromRgb(0x33, 0x41, 0x55)),
        Margin = new Thickness(0, 0, 0, 8)
    };
    private static TextBlock Key(string text) => new()
    {
        Text = text, FontSize = 12,
        Foreground = new SolidColorBrush(Color.FromRgb(0x47, 0x55, 0x69)),
        Margin = new Thickness(16, 0, 0, 4)
    };
    private static Border InfoBox(string text, string color = "#EFF6FF", string border = "#93C5FD")
    {
        return new Border
        {
            Background = (Brush)new BrushConverter().ConvertFromString(color)!,
            BorderBrush = (Brush)new BrushConverter().ConvertFromString(border)!,
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(6),
            Padding = new Thickness(10, 8, 10, 8),
            Margin = new Thickness(0, 8, 0, 8),
            Child = new TextBlock
            {
                Text = text, TextWrapping = TextWrapping.Wrap, FontSize = 12,
                Foreground = new SolidColorBrush(Color.FromRgb(0x1E, 0x29, 0x3B))
            }
        };
    }

    // ===== ENGLISH SECTIONS =====
    private void BuildEnglishSections()
    {
        var s = new StackPanel();
        s.Children.Add(H1("ANLAbel - Label Designer"));
        s.Children.Add(P("ANLAbel is a professional label design and printing application. It allows you to create labels with text, barcodes, lines, and shapes, bind them to Excel data, and print to any Windows printer."));
        s.Children.Add(H2("Key Features"));
        s.Children.Add(P("• Design labels visually with drag-and-drop canvas"));
        s.Children.Add(P("• Support 1D barcodes (Code128, EAN-13, UPC-A, Code39, ITF-14) and 2D codes (QR Code, DataMatrix)"));
        s.Children.Add(P("• Bind label fields to Excel database columns"));
        s.Children.Add(P("• Print current row or all rows from Excel"));
        s.Children.Add(P("• Per-row copies control with up/down buttons"));
        s.Children.Add(P("• Print calibration with offset and scale adjustment"));
        s.Children.Add(P("• Print history logging to Excel"));
        s.Children.Add(H2("Main Interface"));
        s.Children.Add(P("The application has three main areas:"));
        s.Children.Add(P("• Top: Ribbon toolbar with File, Edit, Data, Printer, View groups"));
        s.Children.Add(P("• Left: Toolbox (tools + data sources tree)"));
        s.Children.Add(P("• Center: Design canvas with rulers and zoom"));
        s.Children.Add(P("• Right: Properties panel for the selected object"));
        _enSections["overview"] = s;

        var qs = new StackPanel();
        qs.Children.Add(H1("⚡ Quick Start Guide"));
        qs.Children.Add(H2("Step 1: Create or Open a Template"));
        qs.Children.Add(P("• Click New (Ctrl+N) to create a blank template"));
        qs.Children.Add(P("• Click Open (Ctrl+O) to open an existing .anlabel file"));
        qs.Children.Add(H2("Step 2: Set Up Printer and Label Size"));
        qs.Children.Add(P("• Click Printer Setup in the ribbon"));
        qs.Children.Add(P("• Select your printer from the list"));
        qs.Children.Add(P("• Choose label size (preset or custom) and DPI"));
        qs.Children.Add(H2("Step 3: Design Your Label"));
        qs.Children.Add(P("• Add objects from the Toolbox (Text, Barcode, Line, Rectangle, Ellipse)"));
        qs.Children.Add(P("• Click and drag on the canvas to place objects"));
        qs.Children.Add(P("• Select objects to edit properties in the right panel"));
        qs.Children.Add(H2("Step 4: Import Excel Data"));
        qs.Children.Add(P("• Click Import Excel in the ribbon"));
        qs.Children.Add(P("• Select a .xlsx file and choose the sheet"));
        qs.Children.Add(P("• Map fields: In Properties panel, set Source to 'Excel Field' and select the column"));
        qs.Children.Add(H2("Step 5: Preview and Print"));
        qs.Children.Add(P("• Click Preview (Ctrl+P) to see how labels look with real data"));
        qs.Children.Add(P("• In Print Preview, adjust copies per row using arrow buttons"));
        qs.Children.Add(P("• Choose 'Current row only' or 'All rows' mode"));
        qs.Children.Add(P("• Click Print to send to printer"));
        qs.Children.Add(P("• Save your template (Ctrl+S) for reuse"));
        _enSections["quickstart"] = qs;

        var rb = new StackPanel();
        rb.Children.Add(H1("🔧 Ribbon Toolbar"));
        rb.Children.Add(P("The ribbon is the top toolbar organized into logical groups:"));
        rb.Children.Add(H2("File Group"));
        rb.Children.Add(P("• New (Ctrl+N) — Create a new blank template"));
        rb.Children.Add(P("• Open (Ctrl+O) — Open an existing .anlabel template file"));
        rb.Children.Add(P("• Save (Ctrl+S) — Save the current template"));
        rb.Children.Add(H2("Edit Group"));
        rb.Children.Add(P("• Undo (Ctrl+Z) — Undo last action"));
        rb.Children.Add(P("• Redo (Ctrl+Y) — Redo last undone action"));
        rb.Children.Add(H2("Data Group"));
        rb.Children.Add(P("• Import Excel — Link an Excel workbook (.xlsx) as data source"));
        rb.Children.Add(P("• Update Excel — Reload data from the linked Excel file"));
        rb.Children.Add(H2("Printer Group"));
        rb.Children.Add(P("• Printer Setup — Select printer, label size, DPI, and orientation"));
        rb.Children.Add(P("• Preview (Ctrl+P) — Open Print Preview window"));
        rb.Children.Add(P("• Print Current — Print label for the currently selected Excel row"));
        rb.Children.Add(P("• Print All Rows — Print labels for all Excel rows"));
        rb.Children.Add(P("• Print History — Open the print history Excel file"));
        rb.Children.Add(P("• Test Print — Print a calibration/test pattern"));
        rb.Children.Add(H2("View Group"));
        rb.Children.Add(P("• Panels restore — Show all hidden panels (Toolbox + Properties)"));
        rb.Children.Add(H2("Edit Object Group"));
        rb.Children.Add(P("• Delete Selection — Delete the currently selected object on canvas"));
        rb.Children.Add(H2("Zoom Group"));
        rb.Children.Add(P("• Minus / Plus buttons and percentage display — Zoom the design canvas in/out"));
        rb.Children.Add(H2("Help Group"));
        rb.Children.Add(P("• Help (F1) — Open this user guide window"));
        _enSections["ribbon"] = rb;

        var cv = new StackPanel();
        cv.Children.Add(H1("🎨 Design Canvas"));
        cv.Children.Add(P("The central area where you visually design your label."));
        cv.Children.Add(H2("Rulers"));
        cv.Children.Add(P("• Horizontal ruler (top) and vertical ruler (left) show dimensions in millimeters"));
        cv.Children.Add(P("• Rulers scale with the current zoom level"));
        cv.Children.Add(H2("Navigation"));
        cv.Children.Add(P("• Scrollbars to pan the canvas"));
        cv.Children.Add(P("• Ctrl+Mouse Wheel to zoom in/out"));
        cv.Children.Add(P("• Zoom slider in the bottom-right corner of the Properties panel"));
        cv.Children.Add(H2("Selecting Objects"));
        cv.Children.Add(P("• Click an object to select it (shows resize handles)"));
        cv.Children.Add(P("• Press Delete key or use Delete Selection in ribbon to remove"));
        cv.Children.Add(H2("Drawing New Objects"));
        cv.Children.Add(P("• Click a tool button in the Toolbox"));
        cv.Children.Add(P("• The cursor changes to crosshair — click and drag on canvas to draw"));
        cv.Children.Add(P("• The tool deactivates after placing one object"));
        _enSections["canvas"] = cv;

        var ob = new StackPanel();
        ob.Children.Add(H1("📦 Object Types"));
        ob.Children.Add(H2("Static Text"));
        ob.Children.Add(P("A text element with fixed content. Use for labels, headers, or constant information."));
        ob.Children.Add(H2("Text Box"));
        ob.Children.Add(P("A text element that can be bound to Excel data. The content changes per label."));
        ob.Children.Add(H2("Barcode"));
        ob.Children.Add(P("A barcode element supporting multiple symbologies. Content can be bound to Excel data."));
        ob.Children.Add(InfoBox("Tip: For QR codes, you can set QR size mode (Auto or Fixed), error correction level, and module size."));
        ob.Children.Add(H2("Line / Rectangle / Ellipse"));
        ob.Children.Add(P("Geometric shapes for separating sections, borders, and decorative elements."));
        _enSections["objects"] = ob;

        var bc = new StackPanel();
        bc.Children.Add(H1("📊 Barcode and QR Code"));
        bc.Children.Add(H2("Supported Symbologies"));
        bc.Children.Add(P("1D: Code 128, EAN-13, EAN-8, UPC-A, UPC-E, Code 39, ITF-14, Codabar"));
        bc.Children.Add(P("2D: QR Code, DataMatrix"));
        bc.Children.Add(H2("QR Code Settings"));
        bc.Children.Add(P("• Sizing Mode: Auto (size by data) or Fixed (constant size)"));
        bc.Children.Add(P("• Error Correction: L (7%), M (15%), Q (25%), H (30%)"));
        bc.Children.Add(P("• Fixed Version: 1-40, Module Size: 1-10 px, DPI"));
        bc.Children.Add(InfoBox("Important: If using Fixed mode, ensure the data fits the selected version capacity."));
        _enSections["barcode"] = bc;

        var bd = new StackPanel();
        bd.Children.Add(H1("🔗 Data Binding"));
        bd.Children.Add(P("Data binding connects label objects to Excel database columns."));
        bd.Children.Add(H2("How to Bind"));
        bd.Children.Add(P("1. Import an Excel file (Import Excel button in ribbon)"));
        bd.Children.Add(P("2. Select an object on the canvas"));
        bd.Children.Add(P("3. In Properties panel, set Source to 'Excel Field'"));
        bd.Children.Add(P("4. Select the Excel column from the dropdown"));
        _enSections["binding"] = bd;

        var xl = new StackPanel();
        xl.Children.Add(H1("📑 Excel Data"));
        xl.Children.Add(P("ANLAbel uses Excel (.xlsx) files as a database for label data."));
        xl.Children.Add(H2("Importing Excel"));
        xl.Children.Add(P("1. Click 'Import Excel' in the ribbon"));
        xl.Children.Add(P("2. Select a .xlsx file and choose the sheet"));
        xl.Children.Add(P("3. The first row should contain column headers"));
        _enSections["excel"] = xl;

        var pr = new StackPanel();
        pr.Children.Add(H1("🖨️ Printing"));
        pr.Children.Add(H2("Printer Setup"));
        pr.Children.Add(P("Click 'Printer Setup' to select printer, label size, DPI, and orientation."));
        pr.Children.Add(H2("Print Preview (Ctrl+P)"));
        pr.Children.Add(P("• Left: Label preview with page navigation"));
        pr.Children.Add(P("• Bottom: Excel Data table showing ALL columns"));
        pr.Children.Add(P("• Right: Print settings, calibration, label setup"));
        pr.Children.Add(H2("Copies Per Row"));
        pr.Children.Add(P("In the Excel Data table, each row has a 'Copies' column with up/down buttons."));
        pr.Children.Add(P("Default is 1 copy per row. Click up arrow to increase."));
        pr.Children.Add(H2("Print Modes"));
        pr.Children.Add(P("• Current Excel row only — Print only for the selected row"));
        pr.Children.Add(P("• All Excel rows — Print for every row in the Excel sheet"));
        _enSections["printer"] = pr;

        var ca = new StackPanel();
        ca.Children.Add(H1("🎯 Calibration"));
        ca.Children.Add(P("Calibration helps fix alignment issues between screen design and physical print output."));
        ca.Children.Add(H2("How to Calibrate"));
        ca.Children.Add(P("1. Open Print Preview (Ctrl+P)"));
        ca.Children.Add(P("2. Expand the 'Calibration' section"));
        ca.Children.Add(P("3. Click 'Print calibration' to print a test pattern"));
        ca.Children.Add(P("4. Adjust Offset X/Y (shift in mm) and Scale X/Y (size correction)"));
        ca.Children.Add(InfoBox("Example: If the label prints 2mm too far right, set Offset X to -2.00."));
        _enSections["calibration"] = ca;

        var sh = new StackPanel();
        sh.Children.Add(H1("⌨️ Keyboard Shortcuts"));
        sh.Children.Add(H2("Global"));
        sh.Children.Add(Key("Ctrl+N .............. New template"));
        sh.Children.Add(Key("Ctrl+O .............. Open template"));
        sh.Children.Add(Key("Ctrl+S .............. Save template"));
        sh.Children.Add(Key("Ctrl+Z .............. Undo"));
        sh.Children.Add(Key("Ctrl+Y .............. Redo"));
        sh.Children.Add(Key("Ctrl+P .............. Print Preview"));
        sh.Children.Add(Key("F1 .................. Open Help"));
        sh.Children.Add(Key("Delete .............. Delete selected object"));
        sh.Children.Add(H2("Canvas"));
        sh.Children.Add(Key("Ctrl+Mouse Wheel .... Zoom in/out"));
        sh.Children.Add(Key("Click+Drag .......... Move selected object"));
        sh.Children.Add(H2("Print Preview"));
        sh.Children.Add(Key("Ctrl+Mouse Wheel .... Zoom in/out"));
        sh.Children.Add(Key("Arrow buttons ....... Previous/Next label page"));
        _enSections["shortcuts"] = sh;

        var ab = new StackPanel();
        ab.Children.Add(H1("ℹ️ About ANLAbel"));
        ab.Children.Add(P("ANLAbel - Label Designer"));
        ab.Children.Add(P("Created by Duc An"));
        ab.Children.Add(P("Email: ducancdt@gmail.com"));
        ab.Children.Add(P("Version: closebeta v0.042"));
        ab.Children.Add(H2("License"));
        ab.Children.Add(P("This software was created by Duc An (ducancdt@gmail.com)."));
        ab.Children.Add(H2("Dependencies"));
        ab.Children.Add(P("• .NET 8 / WPF — Microsoft"));
        ab.Children.Add(P("• ClosedXML 0.105.0 — MIT License"));
        ab.Children.Add(P("• ZXing.Net 0.16.11 — Apache-2.0 License"));
        _enSections["about"] = ab;
    }

    // ===== VIETNAMESE SECTIONS =====
    private void BuildVietnameseSections()
    {
        var s = new StackPanel();
        s.Children.Add(H1("ANLAbel - Thiết kế Nhãn"));
        s.Children.Add(P("ANLAbel là ứng dụng thiết kế và in nhãn chuyên nghiệp. Cho phép bạn tạo nhãn với văn bản, mã vạch, đường kẻ và hình dạng, liên kết với dữ liệu Excel, và in trên bất kỳ máy in Windows nào."));
        s.Children.Add(H2("Tính năng chính"));
        s.Children.Add(P("• Thiết kế nhãn trực quan bằng canvas kéo thả"));
        s.Children.Add(P("• Hỗ trợ mã vạch 1D (Code128, EAN-13, UPC-A, Code39, ITF-14) và mã 2D (QR Code, DataMatrix)"));
        s.Children.Add(P("• Liên kết trường nhãn với cột cơ sở dữ liệu Excel"));
        s.Children.Add(P("• In dòng hiện tại hoặc tất cả các dòng từ Excel"));
        s.Children.Add(P("• Điều khiển số bản in cho từng dòng với nút lên/xuống"));
        s.Children.Add(P("• Hiệu chỉnh in với điều chỉnh offset và tỷ lệ"));
        s.Children.Add(P("• Ghi nhật ký in ấn ra Excel"));
        s.Children.Add(H2("Giao diện chính"));
        s.Children.Add(P("Ứng dụng có ba khu vực chính:"));
        s.Children.Add(P("• Trên: Thanh công cụ Ribbon với các nhóm File, Edit, Data, Printer, View"));
        s.Children.Add(P("• Trái: Hộp công cụ (cây công cụ + nguồn dữ liệu)"));
        s.Children.Add(P("• Giữa: Bảng thiết kế với thước kẻ và zoom"));
        s.Children.Add(P("• Phải: Bảng thuộc tính cho đối tượng được chọn"));
        _viSections["overview"] = s;

        var qs = new StackPanel();
        qs.Children.Add(H1("⚡ Hướng dẫn nhanh"));
        qs.Children.Add(H2("Bước 1: Tạo hoặc Mở Template"));
        qs.Children.Add(P("• Nhấn New (Ctrl+N) để tạo template trống"));
        qs.Children.Add(P("• Nhấn Open (Ctrl+O) để mở file .anlabel có sẵn"));
        qs.Children.Add(H2("Bước 2: Thiết lập Máy in và Kích thước Nhãn"));
        qs.Children.Add(P("• Nhấn Printer Setup trên thanh ribbon"));
        qs.Children.Add(P("• Chọn máy in từ danh sách"));
        qs.Children.Add(P("• Chọn kích thước nhãn (có sẵn hoặc tùy chỉnh) và DPI"));
        qs.Children.Add(H2("Bước 3: Thiết kế Nhãn"));
        qs.Children.Add(P("• Thêm đối tượng từ Hộp công cụ (Văn bản, Mã vạch, Đường kẻ, Hình chữ nhật, Hình elip)"));
        qs.Children.Add(P("• Nhấp và kéo trên canvas để đặt đối tượng"));
        qs.Children.Add(P("• Chọn đối tượng để chỉnh sửa thuộc tính ở bảng bên phải"));
        qs.Children.Add(H2("Bước 4: Nhập dữ liệu Excel"));
        qs.Children.Add(P("• Nhấn Import Excel trên thanh ribbon"));
        qs.Children.Add(P("• Chọn file .xlsx và chọn sheet"));
        qs.Children.Add(P("• Ánh xạ trường: Trong bảng thuộc tính, đặt Nguồn thành 'Excel Field' và chọn cột"));
        qs.Children.Add(H2("Bước 5: Xem trước và In"));
        qs.Children.Add(P("• Nhấn Preview (Ctrl+P) để xem nhãn với dữ liệu thực"));
        qs.Children.Add(P("• Trong Xem trước, điều chỉnh số bản in cho từng dòng bằng nút mũi tên"));
        qs.Children.Add(P("• Chọn chế độ 'Dòng hiện tại' hoặc 'Tất cả các dòng'"));
        qs.Children.Add(P("• Nhấn Print để gửi đến máy in"));
        qs.Children.Add(P("• Lưu template (Ctrl+S) để tái sử dụng"));
        _viSections["quickstart"] = qs;

        var rb = new StackPanel();
        rb.Children.Add(H1("🔧 Thanh Ribbon"));
        rb.Children.Add(P("Thanh ribbon là thanh công cụ phía trên được tổ chức thành các nhóm logic:"));
        rb.Children.Add(H2("Nhóm File"));
        rb.Children.Add(P("• New (Ctrl+N) — Tạo template trống mới"));
        rb.Children.Add(P("• Open (Ctrl+O) — Mở file template .anlabel"));
        rb.Children.Add(P("• Save (Ctrl+S) — Lưu template hiện tại"));
        rb.Children.Add(H2("Nhóm Edit"));
        rb.Children.Add(P("• Undo (Ctrl+Z) — Hoàn tác hành động trước"));
        rb.Children.Add(P("• Redo (Ctrl+Y) — Làm lại hành động đã hoàn tác"));
        rb.Children.Add(H2("Nhóm Data"));
        rb.Children.Add(P("• Import Excel — Liên kết file Excel (.xlsx) làm nguồn dữ liệu"));
        rb.Children.Add(P("• Update Excel — Tải lại dữ liệu từ file Excel đã liên kết"));
        rb.Children.Add(H2("Nhóm Printer"));
        rb.Children.Add(P("• Printer Setup — Chọn máy in, kích thước nhãn, DPI và hướng in"));
        rb.Children.Add(P("• Preview (Ctrl+P) — Mở cửa sổ Xem trước khi in"));
        rb.Children.Add(P("• Print Current — In nhãn cho dòng Excel đang chọn"));
        rb.Children.Add(P("• Print All Rows — In nhãn cho tất cả các dòng Excel"));
        rb.Children.Add(P("• Print History — Mở file lịch sử in ấn"));
        rb.Children.Add(P("• Test Print — In mẫu kiểm tra/hiệu chỉnh"));
        rb.Children.Add(H2("Nhóm View"));
        rb.Children.Add(P("• Panels restore — Hiển thị tất cả bảng đã ẩn"));
        rb.Children.Add(H2("Nhóm Edit Object"));
        rb.Children.Add(P("• Delete Selection — Xóa đối tượng đang chọn trên canvas"));
        rb.Children.Add(H2("Nhóm Zoom"));
        rb.Children.Add(P("• Nút Minus / Plus và hiển thị phần trăm — Phóng to/thu nhỏ bảng thiết kế"));
        rb.Children.Add(H2("Nhóm Help"));
        rb.Children.Add(P("• Help (F1) — Mở cửa sổ hướng dẫn sử dụng này"));
        _viSections["ribbon"] = rb;

        var cv = new StackPanel();
        cv.Children.Add(H1("🎨 Bảng thiết kế"));
        cv.Children.Add(P("Khu vực trung tâm nơi bạn thiết kế nhãn trực quan."));
        cv.Children.Add(H2("Thước kẻ"));
        cv.Children.Add(P("• Thước ngang (trên) và thước dọc (trái) hiển thị kích thước bằng milimet"));
        cv.Children.Add(H2("Điều hướng"));
        cv.Children.Add(P("• Thanh cuộn để di chuyển canvas"));
        cv.Children.Add(P("• Ctrl+Cuộn chuột để phóng to/thu nhỏ"));
        cv.Children.Add(H2("Chọn đối tượng"));
        cv.Children.Add(P("• Nhấp vào đối tượng để chọn (hiển thị tay cầm thay đổi kích thước)"));
        cv.Children.Add(P("• Nhấn Delete hoặc dùng Delete Selection trong ribbon để xóa"));
        cv.Children.Add(H2("Vẽ đối tượng mới"));
        cv.Children.Add(P("• Nhấp nút công cụ trong Hộp công cụ"));
        cv.Children.Add(P("• Con trỏ chuyển thành chữ thập — nhấp và kéo trên canvas để vẽ"));
        _viSections["canvas"] = cv;

        var ob = new StackPanel();
        ob.Children.Add(H1("📦 Các loại đối tượng"));
        ob.Children.Add(H2("Văn bản tĩnh (Static Text)"));
        ob.Children.Add(P("Phần tử văn bản với nội dung cố định. Dùng cho tiêu đề, nhãn hoặc thông tin không đổi."));
        ob.Children.Add(H2("Hộp văn bản (Text Box)"));
        ob.Children.Add(P("Phần tử văn bản có thể liên kết với dữ liệu Excel. Nội dung thay đổi theo từng nhãn."));
        ob.Children.Add(H2("Mã vạch (Barcode)"));
        ob.Children.Add(P("Phần tử mã vạch hỗ trợ nhiều loại ký hiệu. Nội dung có thể liên kết với dữ liệu Excel."));
        ob.Children.Add(InfoBox("Mẹo: Với mã QR, bạn có thể đặt chế độ kích thước (Tự động hoặc Cố định), mức sửa lỗi và kích thước module."));
        ob.Children.Add(H2("Đường kẻ / Hình chữ nhật / Hình elip"));
        ob.Children.Add(P("Hình dạng hình học để phân tách khu vực, viền và yếu tố trang trí."));
        _viSections["objects"] = ob;

        var bc = new StackPanel();
        bc.Children.Add(H1("📊 Mã vạch và Mã QR"));
        bc.Children.Add(H2("Các loại ký hiệu được hỗ trợ"));
        bc.Children.Add(P("1D: Code 128, EAN-13, EAN-8, UPC-A, UPC-E, Code 39, ITF-14, Codabar"));
        bc.Children.Add(P("2D: QR Code, DataMatrix"));
        bc.Children.Add(H2("Cài đặt mã QR"));
        bc.Children.Add(P("• Chế độ kích thước: Tự động (theo dữ liệu) hoặc Cố định"));
        bc.Children.Add(P("• Sửa lỗi: L (7%), M (15%), Q (25%), H (30%)"));
        bc.Children.Add(P("• Phiên bản cố định: 1-40, Kích thước module: 1-10 px, DPI"));
        bc.Children.Add(InfoBox("Quan trọng: Nếu dùng chế độ Cố định, hãy đảm bảo dữ liệu vừa với dung lượng phiên bản đã chọn."));
        _viSections["barcode"] = bc;

        var bd = new StackPanel();
        bd.Children.Add(H1("🔗 Liên kết dữ liệu"));
        bd.Children.Add(P("Liên kết dữ liệu kết nối đối tượng nhãn với cột cơ sở dữ liệu Excel."));
        bd.Children.Add(H2("Cách liên kết"));
        bd.Children.Add(P("1. Nhập file Excel (nút Import Excel trên ribbon)"));
        bd.Children.Add(P("2. Chọn đối tượng trên canvas"));
        bd.Children.Add(P("3. Trong bảng thuộc tính, đặt Nguồn thành 'Excel Field'"));
        bd.Children.Add(P("4. Chọn cột Excel từ danh sách thả xuống"));
        _viSections["binding"] = bd;

        var xl = new StackPanel();
        xl.Children.Add(H1("📑 Dữ liệu Excel"));
        xl.Children.Add(P("ANLAbel sử dụng file Excel (.xlsx) làm cơ sở dữ liệu cho dữ liệu nhãn."));
        xl.Children.Add(H2("Nhập Excel"));
        xl.Children.Add(P("1. Nhấn 'Import Excel' trên ribbon"));
        xl.Children.Add(P("2. Chọn file .xlsx và chọn sheet"));
        xl.Children.Add(P("3. Dòng đầu tiên phải chứa tên cột"));
        _viSections["excel"] = xl;

        var pr = new StackPanel();
        pr.Children.Add(H1("🖨️ In ấn"));
        pr.Children.Add(H2("Thiết lập máy in"));
        pr.Children.Add(P("Nhấn 'Printer Setup' để chọn máy in, kích thước nhãn, DPI và hướng in."));
        pr.Children.Add(H2("Xem trước khi in (Ctrl+P)"));
        pr.Children.Add(P("• Trái: Xem trước nhãn với điều hướng trang"));
        pr.Children.Add(P("• Dưới: Bảng dữ liệu Excel hiển thị TẤT CẢ các cột"));
        pr.Children.Add(P("• Phải: Cài đặt in, hiệu chỉnh, thiết lập nhãn"));
        pr.Children.Add(H2("Số bản in cho mỗi dòng"));
        pr.Children.Add(P("Trong bảng dữ liệu Excel, mỗi dòng có cột 'Copies' với nút lên/xuống."));
        pr.Children.Add(P("Mặc định là 1 bản/dòng. Nhấn mũi tên lên để tăng."));
        pr.Children.Add(H2("Chế độ in"));
        pr.Children.Add(P("• Chỉ dòng Excel hiện tại — In chỉ cho dòng đang chọn"));
        pr.Children.Add(P("• Tất cả các dòng Excel — In cho mọi dòng trong bảng Excel"));
        _viSections["printer"] = pr;

        var ca = new StackPanel();
        ca.Children.Add(H1("🎯 Hiệu chỉnh"));
        ca.Children.Add(P("Hiệu chỉnh giúp sửa lỗi căn chỉnh giữa thiết kế trên màn hình và kết quả in thực tế."));
        ca.Children.Add(H2("Cách hiệu chỉnh"));
        ca.Children.Add(P("1. Mở Xem trước khi in (Ctrl+P)"));
        ca.Children.Add(P("2. Mở phần 'Calibration'"));
        ca.Children.Add(P("3. Nhấn 'Print calibration' để in mẫu kiểm tra"));
        ca.Children.Add(P("4. Điều chỉnh Offset X/Y (dịch chuyển mm) và Scale X/Y (sửa tỷ lệ)"));
        ca.Children.Add(InfoBox("Ví dụ: Nếu nhãn in lệch phải 2mm, đặt Offset X = -2.00."));
        _viSections["calibration"] = ca;

        var sh = new StackPanel();
        sh.Children.Add(H1("⌨️ Phím tắt"));
        sh.Children.Add(H2("Toàn cục"));
        sh.Children.Add(Key("Ctrl+N .............. Tạo template mới"));
        sh.Children.Add(Key("Ctrl+O .............. Mở template"));
        sh.Children.Add(Key("Ctrl+S .............. Lưu template"));
        sh.Children.Add(Key("Ctrl+Z .............. Hoàn tác"));
        sh.Children.Add(Key("Ctrl+Y .............. Làm lại"));
        sh.Children.Add(Key("Ctrl+P .............. Xem trước khi in"));
        sh.Children.Add(Key("F1 .................. Mở hướng dẫn"));
        sh.Children.Add(Key("Delete .............. Xóa đối tượng đang chọn"));
        sh.Children.Add(H2("Canvas"));
        sh.Children.Add(Key("Ctrl+Cuộn chuột ..... Phóng to/thu nhỏ"));
        sh.Children.Add(Key("Kéo thả ............. Di chuyển đối tượng"));
        sh.Children.Add(H2("Xem trước khi in"));
        sh.Children.Add(Key("Ctrl+Cuộn chuột ..... Phóng to/thu nhỏ"));
        sh.Children.Add(Key("Nút mũi tên ......... Trang trước/sau"));
        _viSections["shortcuts"] = sh;

        var ab = new StackPanel();
        ab.Children.Add(H1("ℹ️ Giới thiệu ANLAbel"));
        ab.Children.Add(P("ANLAbel - Thiết kế Nhãn"));
        ab.Children.Add(P("Tác giả: Duc An"));
        ab.Children.Add(P("Email: ducancdt@gmail.com"));
        ab.Children.Add(P("Phiên bản: closebeta v0.042"));
        ab.Children.Add(H2("Giấy phép"));
        ab.Children.Add(P("Phần mềm này được tạo bởi Duc An (ducancdt@gmail.com)."));
        ab.Children.Add(H2("Thư viện sử dụng"));
        ab.Children.Add(P("• .NET 8 / WPF — Microsoft"));
        ab.Children.Add(P("• ClosedXML 0.105.0 — Giấy phép MIT"));
        ab.Children.Add(P("• ZXing.Net 0.16.11 — Giấy phép Apache-2.0"));
        _viSections["about"] = ab;
    }
}