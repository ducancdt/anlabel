using ANLAbel.App.ViewModels;
using ANLAbel.Data.Preferences;

using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Shapes;

namespace ANLAbel.App;

public partial class HelpWindow : Window
{
    private readonly Dictionary<string, UIElement> _sections;
    private string _currentKey = "overview";
    private readonly bool _initialized;

    public HelpWindow()
    {
        InitializeComponent();
        _sections = BuildSections(true);
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

    private void ShowSection(string key)
    {
        ContentStack.Children.Clear();
        if (_sections.TryGetValue(key, out var section))
        {
            ContentStack.Children.Add(section);
        }
    }

    // =====================================================================
    //  TEXT HELPERS
    // =====================================================================
    private static SolidColorBrush Hex(string s) => (SolidColorBrush)new BrushConverter().ConvertFromString(s)!;

    private static TextBlock H1(string text) => new()
    {
        Text = text, FontSize = 21, FontWeight = FontWeights.Bold,
        Foreground = Hex("#0F172A"),
        Margin = new Thickness(0, 0, 0, 4)
    };

    private static TextBlock Sub(string text) => new()
    {
        Text = text, FontSize = 13, TextWrapping = TextWrapping.Wrap,
        Foreground = Hex("#64748B"),
        Margin = new Thickness(0, 0, 0, 14)
    };

    private static TextBlock H2(string text) => new()
    {
        Text = text, FontSize = 15, FontWeight = FontWeights.SemiBold,
        Foreground = Hex("#1E40AF"),
        Margin = new Thickness(0, 20, 0, 6)
    };

    private static TextBlock P(string text) => new()
    {
        Text = text, TextWrapping = TextWrapping.Wrap,
        Foreground = Hex("#334155"), LineHeight = 19,
        Margin = new Thickness(0, 0, 0, 7)
    };

    private static TextBlock Key(string text) => new()
    {
        Text = text, FontSize = 12,
        Foreground = Hex("#475569"),
        Margin = new Thickness(16, 0, 0, 4)
    };

    private static Border InfoBox(string text, string color = "#EFF6FF", string border = "#93C5FD", string emoji = "💡")
    {
        var panel = new StackPanel { Orientation = Orientation.Horizontal };
        panel.Children.Add(new TextBlock { Text = emoji, FontSize = 14, Margin = new Thickness(0, 0, 8, 0), VerticalAlignment = VerticalAlignment.Top });
        panel.Children.Add(new TextBlock
        {
            Text = text, TextWrapping = TextWrapping.Wrap, FontSize = 12.5,
            Foreground = Hex("#1E293B"), MaxWidth = 540
        });
        return new Border
        {
            Background = Hex(color), BorderBrush = Hex(border),
            BorderThickness = new Thickness(1), CornerRadius = new CornerRadius(6),
            Padding = new Thickness(11, 9, 11, 9), Margin = new Thickness(0, 8, 0, 8),
            HorizontalAlignment = HorizontalAlignment.Left, Child = panel
        };
    }

    // A numbered step card: blue circle badge + title + body.
    private static Border StepCard(int n, string title, string body)
    {
        var grid = new Grid();
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        var badge = new Border
        {
            Width = 30, Height = 30, CornerRadius = new CornerRadius(15),
            Background = Hex("#1464D2"), Margin = new Thickness(0, 0, 13, 0),
            VerticalAlignment = VerticalAlignment.Top,
            Child = new TextBlock
            {
                Text = n.ToString(), Foreground = Brushes.White, FontWeight = FontWeights.Bold,
                FontSize = 14, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center
            }
        };
        Grid.SetColumn(badge, 0);

        var txt = new StackPanel();
        txt.Children.Add(new TextBlock
        {
            Text = title, FontWeight = FontWeights.SemiBold, FontSize = 13.5,
            Foreground = Hex("#0F172A"), TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 4, 0, 3)
        });
        if (!string.IsNullOrEmpty(body))
            txt.Children.Add(new TextBlock { Text = body, TextWrapping = TextWrapping.Wrap, Foreground = Hex("#334155"), FontSize = 12.5, LineHeight = 18 });
        Grid.SetColumn(txt, 1);

        grid.Children.Add(badge);
        grid.Children.Add(txt);

        return new Border
        {
            Child = grid, Background = Hex("#F8FAFC"), BorderBrush = Hex("#E2E8F0"),
            BorderThickness = new Thickness(1), CornerRadius = new CornerRadius(9),
            Padding = new Thickness(13, 10, 13, 11), Margin = new Thickness(0, 0, 0, 8)
        };
    }

    // Wraps a drawn illustration with a small italic caption underneath.
    private static StackPanel Figure(UIElement fig, string caption)
    {
        var sp = new StackPanel { Margin = new Thickness(0, 8, 0, 16), HorizontalAlignment = HorizontalAlignment.Left };
        var frame = new Border
        {
            Background = Hex("#FFFFFF"), BorderBrush = Hex("#CBD5E1"),
            BorderThickness = new Thickness(1), CornerRadius = new CornerRadius(8),
            Padding = new Thickness(14), Child = fig,
            HorizontalAlignment = HorizontalAlignment.Left,
            Effect = new System.Windows.Media.Effects.DropShadowEffect { BlurRadius = 14, ShadowDepth = 2, Opacity = 0.10, Color = Color.FromRgb(0x33, 0x41, 0x55) }
        };
        sp.Children.Add(frame);
        sp.Children.Add(new TextBlock
        {
            Text = "🖼  " + caption, FontSize = 11.5, FontStyle = FontStyles.Italic,
            Foreground = Hex("#64748B"), Margin = new Thickness(2, 7, 0, 0),
            TextWrapping = TextWrapping.Wrap, MaxWidth = 560
        });
        return sp;
    }

    // =====================================================================
    //  ILLUSTRATION BUILDERS (drawn with WPF primitives — no image files)
    // =====================================================================

    private static Border Zone(string label, string sub, string bg, string fg, double? height = null)
    {
        var sp = new StackPanel { HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center };
        sp.Children.Add(new TextBlock { Text = label, FontWeight = FontWeights.SemiBold, FontSize = 12, Foreground = Hex(fg), HorizontalAlignment = HorizontalAlignment.Center, TextAlignment = TextAlignment.Center });
        if (!string.IsNullOrEmpty(sub))
            sp.Children.Add(new TextBlock { Text = sub, FontSize = 10, Foreground = Hex(fg), Opacity = 0.85, HorizontalAlignment = HorizontalAlignment.Center, TextAlignment = TextAlignment.Center, TextWrapping = TextWrapping.Wrap });
        var b = new Border
        {
            Background = Hex(bg), BorderBrush = Hex("#94A3B8"), BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(5), Margin = new Thickness(3), Child = sp
        };
        if (height.HasValue) b.Height = height.Value;
        return b;
    }

    // Whole-app wireframe: title bar, ribbon, toolbox / canvas / properties, status bar.
    private static UIElement FigAppLayout(bool vi)
    {
        var root = new Grid { Width = 540, Height = 330 };
        root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(26) });
        root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(48) });
        root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(22) });

        var title = Zone(vi ? "① Thanh tiêu đề — tên máy in & khổ giấy" : "① Title bar — printer name & paper size", "", "#0D237A", "#FFFFFF");
        Grid.SetRow(title, 0); root.Children.Add(title);

        var ribbon = Zone(vi ? "② Thanh công cụ — Thư viện · Dữ liệu · In ấn · Làm việc · Chọn · Trợ giúp" : "② Ribbon — File · Edit · Data · Printer · View · Zoom · Help", "", "#EAF3FF", "#0F4EA8");
        Grid.SetRow(ribbon, 1); root.Children.Add(ribbon);

        var mid = new Grid();
        mid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(150) });
        mid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        mid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(140) });

        var toolbox = Zone(vi ? "③ Hộp công cụ" : "③ Toolbox", vi ? "Công cụ + Nguồn dữ liệu" : "Tools + Data sources", "#F8FAFC", "#334155");
        Grid.SetColumn(toolbox, 0); mid.Children.Add(toolbox);

        var canvas = Zone(vi ? "④ Bảng thiết kế" : "④ Design Canvas", vi ? "Nơi bạn vẽ nhãn, có thước kẻ mm" : "Where you draw the label, with mm rulers", "#FFFFFF", "#0F172A");
        Grid.SetColumn(canvas, 1); mid.Children.Add(canvas);

        var props = Zone(vi ? "⑤ Thuộc tính" : "⑤ Properties", vi ? "Sửa đối tượng đang chọn" : "Edit selected object", "#F8FAFC", "#334155");
        Grid.SetColumn(props, 2); mid.Children.Add(props);

        Grid.SetRow(mid, 2); root.Children.Add(mid);

        var status = Zone(vi ? "⑥ Thanh trạng thái — thông báo & thu phóng" : "⑥ Status bar — messages & Zoom slider", "", "#F1F5F9", "#64748B");
        Grid.SetRow(status, 3); root.Children.Add(status);

        return root;
    }

    // Mock of a single ribbon button (icon glyph + 2 lines).
    private static Border RibbonBtn(string glyph, string l1, string l2, string glyphColor = "#1464D2")
    {
        var sp = new StackPanel { HorizontalAlignment = HorizontalAlignment.Center };
        sp.Children.Add(new TextBlock { Text = glyph, FontSize = 18, HorizontalAlignment = HorizontalAlignment.Center, Foreground = Hex(glyphColor), Margin = new Thickness(0, 0, 0, 1) });
        sp.Children.Add(new TextBlock { Text = l1, FontSize = 10, HorizontalAlignment = HorizontalAlignment.Center, Foreground = Hex("#334155") });
        if (!string.IsNullOrEmpty(l2))
            sp.Children.Add(new TextBlock { Text = l2, FontSize = 9, HorizontalAlignment = HorizontalAlignment.Center, Foreground = Hex("#94A3B8") });
        return new Border { Width = 56, Height = 52, Background = Hex("#FFFFFF"), BorderBrush = Hex("#E2E8F0"), BorderThickness = new Thickness(1), CornerRadius = new CornerRadius(5), Margin = new Thickness(2, 0, 2, 0), Child = sp };
    }

    private static Border RibbonGroup(string name, params Border[] buttons)
    {
        var row = new StackPanel { Orientation = Orientation.Horizontal };
        foreach (var b in buttons) row.Children.Add(b);
        var outer = new StackPanel { Margin = new Thickness(4, 0, 4, 0) };
        outer.Children.Add(row);
        outer.Children.Add(new TextBlock { Text = name, FontSize = 9, Foreground = Hex("#94A3B8"), HorizontalAlignment = HorizontalAlignment.Center, Margin = new Thickness(0, 2, 0, 0) });
        return new Border { Child = outer };
    }

    private static UIElement FigRibbon(bool vi)
    {
        var row = new StackPanel { Orientation = Orientation.Horizontal };
        void Divider() => row.Children.Add(new Border { Width = 1, Background = Hex("#E5EAF1"), Margin = new Thickness(6, 4, 6, 4) });

        row.Children.Add(RibbonGroup("Thư viện", RibbonBtn("📚", "Mẫu tem", string.Empty)));
        Divider();
        row.Children.Add(RibbonGroup("Dữ liệu",
            RibbonBtn("📊", "Nhập", "Excel", "#1A7F37"), RibbonBtn("↻", "Cập nhật", "Excel", "#1A7F37")));
        Divider();
        row.Children.Add(RibbonGroup("In ấn",
            RibbonBtn("🖨", "Máy in", "Cài đặt"), RibbonBtn("🔍", "Xem trước", "Ctrl+P"),
            RibbonBtn("▶", "In", "Dòng này"), RibbonBtn("⏩", "In mọi", "dòng")));
        Divider();
        row.Children.Add(RibbonGroup("Trợ giúp", RibbonBtn("?", "Trợ giúp", "F1", "#2563EB")));

        var sv = new ScrollViewer { HorizontalScrollBarVisibility = ScrollBarVisibility.Auto, VerticalScrollBarVisibility = ScrollBarVisibility.Disabled, Content = row, MaxWidth = 560 };
        return sv;
    }

    // Toolbox 2×3 tool grid.
    private static Border ToolTile(string glyph, string name)
    {
        var sp = new StackPanel { HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center };
        sp.Children.Add(new TextBlock { Text = glyph, FontSize = 20, HorizontalAlignment = HorizontalAlignment.Center, Foreground = Hex("#1464D2") });
        sp.Children.Add(new TextBlock { Text = name, FontSize = 11, HorizontalAlignment = HorizontalAlignment.Center, Foreground = Hex("#334155") });
        return new Border { Width = 96, Height = 58, Background = Hex("#FFFFFF"), BorderBrush = Hex("#DFE8F2"), BorderThickness = new Thickness(1), CornerRadius = new CornerRadius(6), Margin = new Thickness(4), Child = sp };
    }

    private static UIElement FigToolbox(bool vi)
    {
        var grid = new UniformGrid { Columns = 2, Rows = 4, Width = 230 };
        grid.Children.Add(ToolTile("T", vi ? "Chữ" : "Text"));
        grid.Children.Add(ToolTile("🔤", vi ? "Ô chữ" : "Text Box"));
        grid.Children.Add(ToolTile("|||", vi ? "Mã vạch" : "Barcode"));
        grid.Children.Add(ToolTile("╱", vi ? "Đường kẻ" : "Line"));
        grid.Children.Add(ToolTile("▭", vi ? "Hình chữ nhật" : "Rectangle"));
        grid.Children.Add(ToolTile("◯", vi ? "Hình bầu dục" : "Ellipse"));
        grid.Children.Add(ToolTile("🖼", vi ? "Hình ảnh" : "Image"));
        return grid;
    }

    // Canvas with rulers, a label sheet, and a selected object with handles.
    private static UIElement FigCanvas(bool vi)
    {
        var root = new Grid { Width = 420, Height = 250, Background = Hex("#EEF2F6") };
        root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(20) });
        root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        root.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(28) });
        root.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        var corner = new Border { Background = Hex("#F8FAFC"), BorderBrush = Hex("#D9E2EC"), BorderThickness = new Thickness(0, 0, 1, 1), Child = new TextBlock { Text = "mm", FontSize = 9, Foreground = Hex("#94A3B8"), HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center } };
        Grid.SetRow(corner, 0); Grid.SetColumn(corner, 0); root.Children.Add(corner);

        Border Ruler(bool horizontal)
        {
            var b = new Border { Background = Hex("#F8FAFC"), BorderBrush = Hex("#D9E2EC"), BorderThickness = horizontal ? new Thickness(0, 0, 0, 1) : new Thickness(0, 0, 1, 0) };
            var ticks = new StackPanel { Orientation = horizontal ? Orientation.Horizontal : Orientation.Vertical, Margin = new Thickness(4, 2, 0, 0) };
            for (int i = 0; i <= 8; i++)
            {
                var t = new TextBlock { Text = (i * 5).ToString(), FontSize = 8, Foreground = Hex("#94A3B8") };
                t.Margin = horizontal ? new Thickness(0, 0, 32, 0) : new Thickness(0, 0, 0, 18);
                ticks.Children.Add(t);
            }
            b.Child = ticks;
            return b;
        }
        var hr = Ruler(true); Grid.SetRow(hr, 0); Grid.SetColumn(hr, 1); root.Children.Add(hr);
        var vr = Ruler(false); Grid.SetRow(vr, 1); Grid.SetColumn(vr, 0); root.Children.Add(vr);

        // The label sheet
        var sheet = new Border
        {
            Background = Hex("#FFFFFF"), BorderBrush = Hex("#94A3B8"), BorderThickness = new Thickness(1),
            Width = 300, Height = 160, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center,
            Effect = new System.Windows.Media.Effects.DropShadowEffect { BlurRadius = 12, ShadowDepth = 3, Opacity = 0.18, Color = Color.FromRgb(0x33, 0x41, 0x55) }
        };
        var inner = new Canvas();
        // a sample text object
        inner.Children.Add(new TextBlock { Text = "PRODUCT NAME", FontWeight = FontWeights.Bold, FontSize = 13, Foreground = Hex("#0F172A") });
        Canvas.SetLeft(inner.Children[0], 18); Canvas.SetTop(inner.Children[0], 16);
        // barcode bars
        var bars = MakeBars(110, 36);
        inner.Children.Add(bars); Canvas.SetLeft(bars, 18); Canvas.SetTop(bars, 60);

        // a "selected" object with dashed border + handles
        var selected = new Border { Width = 96, Height = 30, BorderBrush = Hex("#1464D2"), BorderThickness = new Thickness(1.5), Background = Hex("#EAF3FF") };
        selected.Child = new TextBlock { Text = "12.50 ₫", FontWeight = FontWeights.SemiBold, Foreground = Hex("#0F4EA8"), HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center };
        inner.Children.Add(selected); Canvas.SetLeft(selected, 180); Canvas.SetTop(selected, 110);
        foreach (var (hx, hy) in new[] { (180.0, 110.0), (276.0, 110.0), (180.0, 140.0), (276.0, 140.0) })
        {
            var handle = new Rectangle { Width = 7, Height = 7, Fill = Brushes.White, Stroke = Hex("#1464D2"), StrokeThickness = 1.5 };
            inner.Children.Add(handle); Canvas.SetLeft(handle, hx - 3.5); Canvas.SetTop(handle, hy - 3.5);
        }
        sheet.Child = inner;
        Grid.SetRow(sheet, 1); Grid.SetColumn(sheet, 1); root.Children.Add(sheet);

        return root;
    }

    // Draw a fake 1D barcode of the given size.
    private static FrameworkElement MakeBars(double width, double height)
    {
        var panel = new StackPanel { Orientation = Orientation.Horizontal, Height = height };
        int[] pattern = { 2, 1, 3, 1, 1, 2, 1, 4, 1, 2, 3, 1, 1, 2, 4, 1, 2, 1, 3, 2, 1, 2, 3, 1, 2, 1, 4, 1, 1, 2 };
        bool black = true;
        double scale = width / pattern.Sum();
        foreach (var w in pattern)
        {
            panel.Children.Add(new Rectangle { Width = w * scale, Height = height, Fill = black ? Brushes.Black : Brushes.White });
            black = !black;
        }
        return panel;
    }

    private static UIElement FigBarcode()
    {
        var sp = new StackPanel { HorizontalAlignment = HorizontalAlignment.Center };
        sp.Children.Add(MakeBars(220, 70));
        sp.Children.Add(new TextBlock { Text = "8 935049 501280", FontFamily = new FontFamily("Consolas"), FontSize = 14, HorizontalAlignment = HorizontalAlignment.Center, Margin = new Thickness(0, 4, 0, 0), Foreground = Hex("#0F172A") });
        return sp;
    }

    // Draw a fake QR code grid.
    private static UIElement FigQr()
    {
        const int n = 15;
        var grid = new UniformGrid { Rows = n, Columns = n, Width = 150, Height = 150, HorizontalAlignment = HorizontalAlignment.Center };
        bool Finder(int r, int c) // 7×7 finder pattern
        {
            if (r == 0 || r == 6 || c == 0 || c == 6) return true;
            if (r >= 2 && r <= 4 && c >= 2 && c <= 4) return true;
            return false;
        }
        for (int r = 0; r < n; r++)
        {
            for (int c = 0; c < n; c++)
            {
                bool on;
                if (r < 7 && c < 7) on = Finder(r, c);
                else if (r < 7 && c >= n - 7) on = Finder(r, c - (n - 7));
                else if (r >= n - 7 && c < 7) on = Finder(r - (n - 7), c);
                else on = ((r * 3 + c * 5 + r * c) % 3 == 0);
                grid.Children.Add(new Border { Background = on ? Brushes.Black : Brushes.White });
            }
        }
        return new Border { Background = Brushes.White, Padding = new Thickness(6), Child = grid, HorizontalAlignment = HorizontalAlignment.Center };
    }

    // Object-type gallery: small visual of each shape.
    private static UIElement FigObjectGallery(bool vi)
    {
        var wrap = new WrapPanel { Width = 540 };
        Border Cell(UIElement visual, string name, string note)
        {
            var sp = new StackPanel { Width = 160, Margin = new Thickness(6) };
            var box = new Border { Height = 64, Background = Hex("#FFFFFF"), BorderBrush = Hex("#E2E8F0"), BorderThickness = new Thickness(1), CornerRadius = new CornerRadius(6), Child = visual };
            sp.Children.Add(box);
            sp.Children.Add(new TextBlock { Text = name, FontWeight = FontWeights.SemiBold, FontSize = 12, Foreground = Hex("#0F172A"), Margin = new Thickness(2, 5, 0, 1) });
            sp.Children.Add(new TextBlock { Text = note, FontSize = 11, Foreground = Hex("#64748B"), TextWrapping = TextWrapping.Wrap });
            return new Border { Child = sp };
        }
        UIElement Center(UIElement e) { e.SetValue(FrameworkElement.HorizontalAlignmentProperty, HorizontalAlignment.Center); e.SetValue(FrameworkElement.VerticalAlignmentProperty, VerticalAlignment.Center); return e; }

        wrap.Children.Add(Cell(Center(new TextBlock { Text = "Xin chào", FontWeight = FontWeights.Bold, FontSize = 18, Foreground = Hex("#0F172A") }), vi ? "Chữ" : "Text", vi ? "Một dòng chữ theo nội dung" : "Text that follows its content"));
        wrap.Children.Add(Cell(Center(new TextBlock { Text = "{Field}", FontSize = 16, Foreground = Hex("#0F4EA8") }), vi ? "Ô chữ" : "Text Box", vi ? "Chữ trong khung cố định, tự xuống dòng" : "Text wrapped inside a fixed frame"));
        wrap.Children.Add(Cell(Center(MakeBars(110, 38)), vi ? "Mã vạch" : "Barcode", vi ? "Mã vạch 1D / 2D" : "1D / 2D barcode"));
        wrap.Children.Add(Cell(Center(new Rectangle { Width = 110, Height = 2, Fill = Hex("#0F172A") }), vi ? "Đường kẻ" : "Line", vi ? "Đường phân cách" : "Divider line"));
        wrap.Children.Add(Cell(Center(new Rectangle { Width = 100, Height = 36, Stroke = Hex("#0F172A"), StrokeThickness = 1.5 }), vi ? "Hình chữ nhật" : "Rectangle", vi ? "Khung viền" : "Border / frame"));
        wrap.Children.Add(Cell(Center(new Ellipse { Width = 56, Height = 38, Stroke = Hex("#0F172A"), StrokeThickness = 1.5 }), vi ? "Hình bầu dục" : "Ellipse", vi ? "Hình tròn / bầu dục" : "Circle / ellipse"));
        return wrap;
    }

    // Data binding flow: Excel column → arrow → label field.
    private static UIElement FigBindingFlow(bool vi)
    {
        var row = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };

        var excel = new StackPanel();
        excel.Children.Add(new TextBlock { Text = vi ? "Cột Excel \"Giá\"" : "Excel column \"Price\"", FontSize = 11, Foreground = Hex("#1A7F37"), FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, 0, 0, 3) });
        foreach (var v in new[] { "12.50", "8.00", "25.90" })
            excel.Children.Add(new Border { Background = Hex("#F0FDF4"), BorderBrush = Hex("#86EFAC"), BorderThickness = new Thickness(1), Padding = new Thickness(10, 4, 10, 4), Margin = new Thickness(0, 0, 0, 2), Child = new TextBlock { Text = v, FontFamily = new FontFamily("Consolas") } });
        row.Children.Add(new Border { Child = excel });

        var arrowSp = new StackPanel { VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(16, 0, 16, 0), HorizontalAlignment = HorizontalAlignment.Center };
        arrowSp.Children.Add(new TextBlock { Text = vi ? "Liên kết" : "Bind", FontSize = 10, Foreground = Hex("#64748B"), HorizontalAlignment = HorizontalAlignment.Center });
        arrowSp.Children.Add(new TextBlock { Text = "➜", FontSize = 30, Foreground = Hex("#1464D2"), HorizontalAlignment = HorizontalAlignment.Center });
        arrowSp.Children.Add(new TextBlock { Text = vi ? "Nguồn = Trường Excel" : "Source = Excel Field", FontSize = 9, Foreground = Hex("#94A3B8") });
        row.Children.Add(arrowSp);

        var labelBox = new Border
        {
            Background = Hex("#FFFFFF"), BorderBrush = Hex("#1464D2"), BorderThickness = new Thickness(1.5), CornerRadius = new CornerRadius(6),
            Width = 150, Height = 96, VerticalAlignment = VerticalAlignment.Center
        };
        var lbl = new StackPanel { HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center };
        lbl.Children.Add(new TextBlock { Text = vi ? "Nhãn in ra" : "Printed label", FontSize = 10, Foreground = Hex("#64748B") });
        lbl.Children.Add(new TextBlock { Text = "12.50 ₫", FontSize = 20, FontWeight = FontWeights.Bold, Foreground = Hex("#0F172A"), HorizontalAlignment = HorizontalAlignment.Center });
        labelBox.Child = lbl;
        row.Children.Add(labelBox);

        return row;
    }

    // Mock Excel data grid.
    private static UIElement FigDataGrid(bool vi)
    {
        var grid = new Grid { Width = 420 };
        string[] headers = { vi ? "Tên" : "Name", vi ? "Giá" : "Price", "SKU", vi ? "Bản" : "Copies" };
        string[,] data = { { "Sữa 1L", "12.50", "8935049501280", "1 ▲▼" }, { "Bánh mì", "8.00", "8935049512345", "2 ▲▼" }, { "Trứng ×10", "25.90", "8935049599887", "1 ▲▼" } };
        for (int c = 0; c < headers.Length; c++) grid.ColumnDefinitions.Add(new ColumnDefinition());
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        for (int r = 0; r < data.GetLength(0); r++) grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        for (int c = 0; c < headers.Length; c++)
        {
            var h = new Border { Background = Hex("#EAF3FF"), BorderBrush = Hex("#CBD5E1"), BorderThickness = new Thickness(0.5), Padding = new Thickness(8, 5, 8, 5), Child = new TextBlock { Text = headers[c], FontWeight = FontWeights.SemiBold, FontSize = 11, Foreground = Hex("#0F4EA8") } };
            Grid.SetRow(h, 0); Grid.SetColumn(h, c); grid.Children.Add(h);
        }
        for (int r = 0; r < data.GetLength(0); r++)
        {
            for (int c = 0; c < headers.Length; c++)
            {
                var cell = new Border { Background = r % 2 == 0 ? Hex("#FFFFFF") : Hex("#F8FAFC"), BorderBrush = Hex("#E5EDF5"), BorderThickness = new Thickness(0.5), Padding = new Thickness(8, 4, 8, 4), Child = new TextBlock { Text = data[r, c], FontSize = 11, Foreground = c == headers.Length - 1 ? Hex("#1464D2") : Hex("#334155") } };
                Grid.SetRow(cell, r + 1); Grid.SetColumn(cell, c); grid.Children.Add(cell);
            }
        }
        return grid;
    }

    // Print preview layout: preview left, settings right, data table bottom.
    private static UIElement FigPrintPreview(bool vi)
    {
        var root = new Grid { Width = 480, Height = 270 };
        root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(70) });
        root.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        root.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(150) });

        var previewArea = new Border { Background = Hex("#EEF2F6"), BorderBrush = Hex("#D9E2EC"), BorderThickness = new Thickness(1), Margin = new Thickness(2) };
        var label = new Border { Background = Brushes.White, Width = 180, Height = 110, BorderBrush = Hex("#94A3B8"), BorderThickness = new Thickness(1), HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center, Effect = new System.Windows.Media.Effects.DropShadowEffect { BlurRadius = 10, ShadowDepth = 2, Opacity = 0.15 } };
        var ls = new StackPanel { HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center };
        ls.Children.Add(new TextBlock { Text = "Sữa 1L", FontWeight = FontWeights.Bold, FontSize = 13, HorizontalAlignment = HorizontalAlignment.Center });
        ls.Children.Add(MakeBars(120, 30));
        ls.Children.Add(new TextBlock { Text = "8935049501280", FontFamily = new FontFamily("Consolas"), FontSize = 9, HorizontalAlignment = HorizontalAlignment.Center });
        label.Child = ls;
        previewArea.Child = label;
        Grid.SetRow(previewArea, 0); Grid.SetColumn(previewArea, 0); root.Children.Add(previewArea);

        var settings = new Border { Background = Hex("#F8FAFC"), BorderBrush = Hex("#D9E2EC"), BorderThickness = new Thickness(1), Margin = new Thickness(2), Padding = new Thickness(8) };
        var ss = new StackPanel();
        ss.Children.Add(new TextBlock { Text = vi ? "⚙ Cài đặt in" : "⚙ Print settings", FontWeight = FontWeights.SemiBold, FontSize = 11, Foreground = Hex("#0F4EA8"), Margin = new Thickness(0, 0, 0, 6) });
        foreach (var t in new[] { vi ? "Máy in" : "Printer", vi ? "▸ Kích thước tem và cài đặt máy in…" : "▸ Label size and printer setup…", vi ? "▸ Lưu cài đặt in vào tem" : "▸ Save print settings to the label", vi ? "☑ Chọn dòng trong bảng dưới" : "☑ Tick rows in the table below" })
            ss.Children.Add(new TextBlock { Text = t, FontSize = 11, Foreground = Hex("#334155"), Margin = new Thickness(0, 0, 0, 4) });
        settings.Child = ss;
        Grid.SetRow(settings, 0); Grid.SetColumn(settings, 1); root.Children.Add(settings);

        var bottom = new Border { Background = Hex("#FFFFFF"), BorderBrush = Hex("#D9E2EC"), BorderThickness = new Thickness(1), Margin = new Thickness(2) };
        var bs = new StackPanel { Margin = new Thickness(6) };
        bs.Children.Add(new TextBlock { Text = vi ? "📑 Bảng dữ liệu Excel — đánh dấu dòng cần in, cột \"Bản\" chỉnh số bản" : "📑 Excel data table — \"Copies\" column sets prints per row", FontSize = 10.5, Foreground = Hex("#64748B"), Margin = new Thickness(0, 0, 0, 4) });
        bs.Children.Add(FigDataGrid(vi));
        bottom.Child = bs;
        Grid.SetRow(bottom, 1); Grid.SetColumn(bottom, 0); Grid.SetColumnSpan(bottom, 2); root.Children.Add(bottom);

        return root;
    }

    // Calibration diagram: design position vs printed position with offset arrows.
    private static UIElement FigCalibration(bool vi)
    {
        var canvas = new Canvas { Width = 360, Height = 180, Background = Hex("#FAFCFF") };
        // Designed position (blue dashed)
        var designed = new Rectangle { Width = 120, Height = 64, Stroke = Hex("#1464D2"), StrokeThickness = 1.5, StrokeDashArray = new DoubleCollection { 3, 2 } };
        canvas.Children.Add(designed); Canvas.SetLeft(designed, 60); Canvas.SetTop(designed, 50);
        var dlbl = new TextBlock { Text = vi ? "Vị trí thiết kế" : "Designed", FontSize = 10, Foreground = Hex("#1464D2") };
        canvas.Children.Add(dlbl); Canvas.SetLeft(dlbl, 60); Canvas.SetTop(dlbl, 34);

        // Printed position (red solid), shifted right+down
        var printed = new Rectangle { Width = 120, Height = 64, Stroke = Hex("#EF4444"), StrokeThickness = 1.5 };
        canvas.Children.Add(printed); Canvas.SetLeft(printed, 96); Canvas.SetTop(printed, 78);
        var plbl = new TextBlock { Text = vi ? "Bản in thực tế (lệch)" : "Printed (drifted)", FontSize = 10, Foreground = Hex("#EF4444") };
        canvas.Children.Add(plbl); Canvas.SetLeft(plbl, 150); Canvas.SetTop(plbl, 146);

        // Offset arrow
        var arrow = new Line { X1 = 120, Y1 = 82, X2 = 156, Y2 = 110, Stroke = Hex("#64748B"), StrokeThickness = 1.5 };
        canvas.Children.Add(arrow);
        var off = new TextBlock { Text = vi ? "Lệch X / Y" : "Offset X / Y", FontSize = 10, FontWeight = FontWeights.SemiBold, Foreground = Hex("#64748B") };
        canvas.Children.Add(off); Canvas.SetLeft(off, 200); Canvas.SetTop(off, 80);

        return canvas;
    }

    // A pressable keyboard key cap.
    private static Border KeyCap(string t) => new()
    {
        Background = Hex("#FFFFFF"), BorderBrush = Hex("#94A3B8"), BorderThickness = new Thickness(1, 1, 1, 2.5),
        CornerRadius = new CornerRadius(5), Padding = new Thickness(9, 3, 9, 4), Margin = new Thickness(0, 0, 5, 6),
        Child = new TextBlock { Text = t, FontSize = 12, FontWeight = FontWeights.SemiBold, Foreground = Hex("#334155") }
    };

    private static UIElement ShortcutRow(string combo, string desc)
    {
        var sp = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 4) };
        var keys = combo.Split('+');
        for (int i = 0; i < keys.Length; i++)
        {
            sp.Children.Add(KeyCap(keys[i].Trim()));
            if (i < keys.Length - 1)
                sp.Children.Add(new TextBlock { Text = "+", Margin = new Thickness(0, 0, 5, 6), VerticalAlignment = VerticalAlignment.Bottom, Foreground = Hex("#94A3B8") });
        }
        sp.Children.Add(new TextBlock { Text = "—  " + desc, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(6, 0, 0, 6), Foreground = Hex("#334155") });
        return sp;
    }

    // FAQ accordion-style item.
    private static UIElement FaqItem(string q, string a)
    {
        var sp = new StackPanel { Margin = new Thickness(0, 0, 0, 10) };
        sp.Children.Add(new TextBlock { Text = "❓ " + q, FontWeight = FontWeights.SemiBold, FontSize = 13, Foreground = Hex("#0F172A"), TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 0, 0, 3) });
        sp.Children.Add(new TextBlock { Text = a, FontSize = 12.5, Foreground = Hex("#334155"), TextWrapping = TextWrapping.Wrap, LineHeight = 18, Margin = new Thickness(16, 0, 0, 0) });
        return new Border { Background = Hex("#F8FAFC"), BorderBrush = Hex("#E2E8F0"), BorderThickness = new Thickness(1), CornerRadius = new CornerRadius(8), Padding = new Thickness(12, 10, 12, 11), Margin = new Thickness(0, 0, 0, 8), Child = sp };
    }

    private static Border LicenseBox(string title, string licenseText)
    {
        var inner = new StackPanel();
        inner.Children.Add(new TextBlock { Text = title, FontWeight = FontWeights.SemiBold, FontSize = 12, Foreground = Hex("#1E40AF"), Margin = new Thickness(0, 0, 0, 4) });
        inner.Children.Add(new TextBlock { Text = licenseText, FontFamily = new FontFamily("Consolas, Courier New"), FontSize = 11, TextWrapping = TextWrapping.Wrap, Foreground = Hex("#334155") });
        return new Border
        {
            Background = Hex("#F8FAFC"), BorderBrush = Hex("#CBD5E1"), BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(5), Padding = new Thickness(10, 8, 10, 8), Margin = new Thickness(0, 6, 0, 6), Child = inner
        };
    }

    // =====================================================================
    //  BILINGUAL SECTION BUILDER
    // =====================================================================
    private Dictionary<string, UIElement> BuildSections(bool vi)
    {
        var d = new Dictionary<string, UIElement>();

        // ---------- OVERVIEW ----------
        var s = new StackPanel();
        s.Children.Add(H1(vi ? "Chào mừng đến với ANLAbel 👋" : "Welcome to ANLAbel 👋"));
        s.Children.Add(Sub(vi
            ? "ANLAbel là phần mềm thiết kế và in nhãn (tem) chuyên nghiệp. Bạn vẽ nhãn bằng cách kéo thả, gắn dữ liệu từ Excel, rồi in hàng loạt ra bất kỳ máy in Windows nào. Hướng dẫn này viết cho người mới — cứ đọc theo thứ tự từ trên xuống là dùng được."
            : "ANLAbel is a professional label design and printing app. You draw a label by dragging objects, feed it data from Excel, then print in bulk to any Windows printer. This guide is written for absolute beginners — read top to bottom and you'll be productive."));
        s.Children.Add(InfoBox(vi
            ? "Mẹo: Nhấn F1 bất cứ lúc nào để mở lại hướng dẫn này."
            : "Tip: Switch English / Tiếng Việt at the top-left. Press F1 anytime to reopen this guide."));
        s.Children.Add(H2(vi ? "Toàn cảnh màn hình làm việc" : "The workspace at a glance"));
        s.Children.Add(P(vi ? "Cửa sổ chính chia thành 6 vùng. Hãy ghi nhớ sơ đồ này — phần còn lại của hướng dẫn sẽ nhắc đến chúng:" : "The main window has 6 areas. Memorize this map — the rest of the guide refers to them:"));
        s.Children.Add(Figure(FigAppLayout(vi), vi ? "Sơ đồ 6 vùng của cửa sổ ANLAbel." : "The 6 areas of the ANLAbel window."));
        s.Children.Add(H2(vi ? "ANLAbel làm được gì?" : "What ANLAbel can do"));
        s.Children.Add(P(vi ? "• Thiết kế nhãn trực quan bằng cách kéo thả văn bản, mã vạch, đường kẻ, hình." : "• Design labels visually by dragging text, barcodes, lines and shapes."));
        s.Children.Add(P(vi ? "• Mã vạch 1D (Code 128, EAN-13, UPC-A, Code 39, ITF-14…) và 2D (QR, Data Matrix)." : "• 1D barcodes (Code 128, EAN-13, UPC-A, Code 39, ITF-14…) and 2D (QR Code, DataMatrix)."));
        s.Children.Add(P(vi ? "• Gắn từng ô trên nhãn vào một cột trong file Excel — in mỗi dòng thành một nhãn khác nhau." : "• Bind label fields to Excel columns — print one different label per row."));
        s.Children.Add(P(vi ? "• In dòng đang chọn hoặc tất cả các dòng, đặt số bản in riêng cho từng dòng." : "• Print the current row or all rows, with a per-row copy count."));
        s.Children.Add(P(vi ? "• Hiệu chỉnh (calibration) để bản in khớp chính xác với thiết kế." : "• Calibrate so the print matches the on-screen design exactly."));
        d["overview"] = s;

        // ---------- QUICK START ----------
        var qs = new StackPanel();
        qs.Children.Add(H1(vi ? "⚡ Bắt đầu trong 5 bước" : "⚡ Get started in 5 steps"));
        qs.Children.Add(Sub(vi ? "Làm lần lượt theo 5 thẻ dưới đây là bạn đã in được nhãn đầu tiên." : "Follow these 5 cards in order and you'll print your first label."));
        qs.Children.Add(StepCard(1, vi ? "Tạo hoặc mở một mẫu tem" : "Create or open a template",
            vi ? "Nhấn Tạo tem mới (Ctrl+N) để tạo tem trống, hoặc Mở tem (Ctrl+O) để mở tệp .anlabel có sẵn; cũng có thể bắt đầu từ Thư viện → Mẫu tem. Khi tạo mới, bạn chọn kích thước tem (rộng × cao tính bằng mm)." : "Click New (Ctrl+N) for a blank label, or Open (Ctrl+O) for an existing .anlabel file. When creating, choose the label size (width × height in mm)."));
        qs.Children.Add(StepCard(2, vi ? "Chọn máy in & khổ nhãn" : "Pick the printer & label size",
            vi ? "Nhấn Máy in · Cài đặt, chọn máy in, kích thước tem và DPI (độ phân giải). Tên máy in sẽ hiện trên thanh tiêu đề." : "Click Printer Setup, choose your printer, label size and DPI. The printer name and paper size appear on the title bar."));
        qs.Children.Add(StepCard(3, vi ? "Vẽ nhãn" : "Design the label",
            vi ? "Trong Hộp công cụ (bên trái), nhấn một công cụ (Ô chữ, Mã vạch…). Con trỏ thành dấu cộng — nhấn-giữ-kéo trên bảng thiết kế để đặt. Chọn đối tượng để chỉnh ở bảng Thuộc tính bên phải." : "In the Toolbox (left), click a tool (Text Box, Barcode…). The cursor becomes a crosshair — click-drag on the canvas to place it. Select it to edit in the Properties panel on the right."));
        qs.Children.Add(StepCard(4, vi ? "Nhập dữ liệu Excel & liên kết" : "Import Excel & bind",
            vi ? "Nhấn Nhập Excel, chọn tệp .xlsx và sheet (dòng đầu là tên cột). Chọn một đối tượng, trong thẻ Nội dung đổi Nguồn thành “Trường Excel”, rồi chọn cột." : "Click Import Excel, pick a .xlsx file and sheet (first row = column names). Select an object, set Content → Source to \"Excel Field\", then choose the column."));
        qs.Children.Add(StepCard(5, vi ? "Xem trước & In" : "Preview & print",
            vi ? "Nhấn Xem trước (Ctrl+P) để xem tem với dữ liệu thật. Đánh dấu các dòng cần in, chỉnh số bản ở cột Bản, rồi nhấn In. Đừng quên Lưu (Ctrl+S) để dùng lại." : "Click Preview (Ctrl+P) to see real data. Choose \"Current row\" or \"All rows\", set the Copies column, then Print. Save (Ctrl+S) to reuse later."));
        qs.Children.Add(InfoBox(vi ? "Bạn chưa cần hiểu hết. Mỗi mục trong menu bên trái sẽ giải thích kỹ kèm hình minh họa." : "You don't need to understand everything yet. Each left-menu topic explains the details with pictures.", "#ECFDF5", "#6EE7B7", "✅"));
        d["quickstart"] = qs;

        // ---------- RIBBON ----------
        var rb = new StackPanel();
        rb.Children.Add(H1(vi ? "🔧 Thanh công cụ" : "🔧 Ribbon Toolbar"));
        rb.Children.Add(Sub(vi ? "Thanh công cụ trên cùng, chia thành các nhóm. Đây là nơi bạn ra lệnh chính." : "The top toolbar, split into groups. This is where you trigger the main commands."));
        rb.Children.Add(Figure(FigRibbon(vi), vi ? "Mô phỏng thanh công cụ với các nhóm và nút chính." : "Mock-up of the ribbon with its groups and main buttons."));
        rb.Children.Add(H2("Nút nhanh trên thanh tiêu đề"));
        rb.Children.Add(P("• Tạo tem mới (Ctrl+N), Mở tem (Ctrl+O), Lưu tem (Ctrl+S), Lưu thành… (Ctrl+Shift+S)."));
        rb.Children.Add(P("• Hoàn tác (Ctrl+Z) / Làm lại (Ctrl+Y)."));
        rb.Children.Add(P("• Phiên bản mẫu tem — xem và khôi phục các bản đã lưu trước đó."));
        rb.Children.Add(H2("Nhóm Thư viện"));
        rb.Children.Add(P("• Mẫu tem — bắt đầu từ một mẫu có sẵn."));
        rb.Children.Add(H2("Nhóm Dữ liệu"));
        rb.Children.Add(P("• Nhập Excel — liên kết một tệp Excel (.xlsx) hoặc CSV làm nguồn dữ liệu."));
        rb.Children.Add(P("• Cập nhật Excel — nạp lại dữ liệu khi tệp đã thay đổi."));
        rb.Children.Add(H2("Nhóm In ấn"));
        rb.Children.Add(P("• Máy in · Cài đặt — chọn máy in, khổ tem, DPI, hướng in và căn chỉnh khổ in."));
        rb.Children.Add(P("• Xem trước (Ctrl+P) — mở cửa sổ xem trước khi in."));
        rb.Children.Add(P("• In · Dòng này / In mọi dòng — in nhanh dòng đang chọn / mọi dòng đã nhập."));
        rb.Children.Add(P("• Lịch sử in — xem các lệnh in đã ghi lại."));
        rb.Children.Add(P("• Xuất Excel — xuất lịch sử in ra một tệp Excel định dạng sẵn."));
        rb.Children.Add(P("• In thử — in tem hiệu chỉnh để căn chỉnh máy in."));
        rb.Children.Add(H2("Nhóm Làm việc · Chọn · Trợ giúp"));
        rb.Children.Add(P("• Bảng · hiện lại — hiện lại Hộp công cụ và bảng Thuộc tính nếu lỡ ẩn."));
        rb.Children.Add(P("• Đối tượng · dính / Lưới · dính — bật hoặc tắt việc hít vào đối tượng và lưới khi kéo."));
        rb.Children.Add(P("• Xóa · Chọn — xóa đối tượng đang chọn (hoặc nhấn phím Delete)."));
        rb.Children.Add(P("• Trợ giúp (F1) — mở cửa sổ hướng dẫn này; Cập nhật — kiểm tra phiên bản mới."));
        d["ribbon"] = rb;

        // ---------- CANVAS ----------
        var cv = new StackPanel();
        cv.Children.Add(H1(vi ? "🎨 Bảng thiết kế" : "🎨 Design Canvas"));
        cv.Children.Add(Sub(vi ? "Vùng trung tâm — nơi bạn vẽ và sắp xếp nhãn. Có thước kẻ mm ở trên và bên trái." : "The central area where you draw and arrange the label. mm rulers run across the top and down the left."));
        cv.Children.Add(Figure(FigCanvas(vi), vi ? "Bảng thiết kế: thước mm (xám), tờ tem (trắng) và một đối tượng đang chọn với các tay cầm xanh để kéo đổi kích thước." : "Canvas: mm rulers (grey), the white label sheet, and a selected object with 4 blue handles to resize."));
        cv.Children.Add(H2(vi ? "Vẽ một đối tượng mới" : "Drawing a new object"));
        cv.Children.Add(StepCard(1, vi ? "Chọn công cụ" : "Pick a tool", vi ? "Nhấn một nút trong Hộp công cụ (ví dụ Mã vạch)." : "Click a button in the Toolbox (e.g. Barcode)."));
        cv.Children.Add(StepCard(2, vi ? "Kéo trên bảng thiết kế" : "Drag on the canvas", vi ? "Con trỏ thành dấu cộng. Nhấn-giữ chuột trái và kéo để định kích thước, rồi thả ra." : "The cursor becomes a crosshair. Press and hold the left button, drag to size, then release."));
        cv.Children.Add(StepCard(3, vi ? "Công cụ tự tắt" : "Tool auto-deactivates", vi ? "Sau khi đặt xong một đối tượng, công cụ tự tắt để bạn không vẽ nhầm cái thứ hai." : "After placing one object the tool turns off, so you don't accidentally draw a second one."));
        cv.Children.Add(H2(vi ? "Chọn, di chuyển, xóa" : "Select, move, delete"));
        cv.Children.Add(P(vi ? "• Nhấp vào một đối tượng để chọn — sẽ hiện 4 tay cầm vuông ở góc." : "• Click an object to select it — four square handles appear at the corners."));
        cv.Children.Add(P(vi ? "• Kéo thân đối tượng để di chuyển; kéo tay cầm để đổi kích thước." : "• Drag the body to move; drag a handle to resize."));
        cv.Children.Add(P(vi ? "• Nhấn phím Delete (hoặc nút Xóa · Chọn trên thanh công cụ) để xóa." : "• Press Delete (or the Delete Selection button) to remove it."));
        cv.Children.Add(H2(vi ? "Phóng to / thu nhỏ" : "Zooming"));
        cv.Children.Add(P(vi ? "• Giữ Ctrl rồi lăn chuột để zoom quanh con trỏ." : "• Hold Ctrl and scroll the wheel to zoom around the cursor."));
        cv.Children.Add(P(vi ? "• Hoặc dùng mức Thu phóng ở góc dưới bên phải." : "• Use the Zoom − / + buttons on the ribbon, or the slider at the bottom-right."));
        cv.Children.Add(InfoBox(vi ? "Thước kẻ luôn tính bằng milimet (mm) — đúng với kích thước in thật, không phải pixel màn hình." : "Rulers are always in millimeters (mm) — matching the real printed size, not screen pixels."));
        d["canvas"] = cv;

        // ---------- OBJECTS ----------
        var ob = new StackPanel();
        ob.Children.Add(H1(vi ? "📦 Các loại đối tượng" : "📦 Object types"));
        ob.Children.Add(Sub(vi ? "Đây là các “khối xây dựng” để ghép thành tem. Mỗi loại có một mục đích riêng." : "These are the 6 building blocks of every label. Each has its own purpose."));
        ob.Children.Add(Figure(FigObjectGallery(vi), vi ? "Các loại đối tượng trong Hộp công cụ." : "The gallery of 6 object types in the Toolbox."));
        ob.Children.Add(H2(vi ? "Chữ" : "Text"));
        ob.Children.Add(P(vi ? "Một dòng chữ tự co giãn theo nội dung. Dùng cho tiêu đề, tên cửa hàng, ghi chú; nội dung cũng có thể lấy từ Excel." : "Fixed text, identical on every label. Use for titles, shop name, notes."));
        ob.Children.Add(H2(vi ? "Ô chữ" : "Text Box"));
        ob.Children.Add(P(vi ? "Chữ nằm trong khung do bạn kéo: tự xuống dòng trong khung và không in tràn ra ngoài khung. Hợp với nội dung dài lấy từ Excel (ví dụ tên sản phẩm)." : "Text that can be bound to Excel; the content changes per data row (e.g. product name, price)."));
        ob.Children.Add(H2(vi ? "Mã vạch" : "Barcode"));
        ob.Children.Add(P(vi ? "Mã vạch 1D hoặc 2D (QR, Data Matrix). Nội dung cũng có thể lấy từ Excel. Xem mục “Mã vạch & QR” để biết chi tiết." : "A 1D or 2D (QR) barcode. Its content can also come from Excel. See the \"Barcode & QR\" topic for details."));
        ob.Children.Add(H2(vi ? "Đường kẻ / Hình chữ nhật / Hình bầu dục" : "Line / Rectangle / Ellipse"));
        ob.Children.Add(P(vi ? "Dùng để phân tách khu vực, tạo viền hoặc trang trí." : "Lines, rectangles and ellipses — for dividing sections, drawing borders, or decoration."));
        ob.Children.Add(H2(vi ? "Hình ảnh" : "Image"));
        ob.Children.Add(P(vi ? "Logo hoặc ký hiệu (PNG, JPG, BMP, GIF). Ảnh được lưu kèm trong tệp mẫu tem." : "A logo or symbol (PNG, JPG, BMP, GIF), stored inside the template file."));
        d["objects"] = ob;

        // ---------- PROPERTIES PANEL ----------
        var pp = new StackPanel();
        pp.Children.Add(H1(vi ? "🎛️ Bảng Thuộc tính" : "🎛️ Properties Panel"));
        pp.Children.Add(Sub(vi ? "Bảng bên phải. Khi bạn chọn một đối tượng, nó hiện đúng các tùy chỉnh cho loại đối tượng đó." : "The right-hand panel. When you select an object it shows exactly the options for that object type."));
        pp.Children.Add(InfoBox(vi ? "Chưa chọn gì? Bảng sẽ ghi “Chưa chọn đối tượng”. Hãy nhấp vào một đối tượng trên bảng thiết kế trước." : "Nothing selected? The panel says \"No object selected\". Click an object on the canvas first.", "#FEF9C3", "#FDE047", "ℹ️"));
        pp.Children.Add(H2(vi ? "Nội dung — chữ & nguồn" : "Content — text & source"));
        pp.Children.Add(P(vi ? "• Nguồn = Chữ: bạn tự gõ nội dung cố định." : "• Source = Text: you type fixed content yourself."));
        pp.Children.Add(P(vi ? "• Nguồn = Trường Excel: nội dung lấy tự động từ cột Excel đã chọn. Nguồn = Ràng buộc / Công thức: ghép nhiều cột hoặc dùng công thức." : "• Source = Excel Field: content is pulled from the chosen Excel column."));
        pp.Children.Add(H2(vi ? "Nâng cao — xoay, căn, sắp xếp, lớp" : "Transform — rotation"));
        pp.Children.Add(P(vi ? "Xoay đối tượng 0° / 90° / 180° / 270°. Hữu ích khi in mã vạch dọc." : "Rotate the object 0° / 90° / 180° / 270°. Handy for vertical barcodes."));
        pp.Children.Add(H2(vi ? "Kiểu chữ" : "Text Style"));
        pp.Children.Add(P(vi ? "Chỉ hiện với Chữ và Ô chữ: chọn phông, cỡ chữ (pt), căn lề, chữ đậm / nghiêng / gạch chân." : "Only for Text/Text Box: choose Font, Size (points), alignment, and Bold / Italic / Underline."));
        pp.Children.Add(H2(vi ? "Mã vạch" : "Barcode options"));
        pp.Children.Add(P(vi ? "Chỉ hiện với mã vạch: chọn Chuẩn; với QR còn có Chế độ 2D, Mức ECC, Phiên bản ký hiệu và Cỡ mô-đun." : "Only for barcodes: choose the Standard; for QR you also get sizing mode, EC level, DPI, Version and Module px."));
        pp.Children.Add(H2(vi ? "Liên kết & công thức" : "Binding & Formula"));
        pp.Children.Add(P(vi ? "Hiện khi đối tượng được liên kết: cho thấy cột nguồn, giá trị xem trước, và cảnh báo (màu đỏ) nếu thiếu cột hoặc giá trị không hợp lệ." : "Shown when an object is bound: it reveals the source column, a preview value, and red warnings if a column is missing or a value is invalid."));
        d["properties"] = pp;

        // ---------- BARCODE ----------
        var bc = new StackPanel();
        bc.Children.Add(H1(vi ? "📊 Mã vạch & Mã QR" : "📊 Barcode & QR Code"));
        bc.Children.Add(Sub(vi ? "ANLAbel tạo mã vạch sắc nét, in được, từ nội dung bạn nhập hoặc lấy từ Excel." : "ANLAbel generates crisp, scannable barcodes from text you type or pull from Excel."));
        var bcRow = new StackPanel { Orientation = Orientation.Horizontal };
        bcRow.Children.Add(Figure(FigBarcode(), vi ? "Mã vạch 1D (ví dụ EAN-13)." : "A 1D barcode (e.g. EAN-13)."));
        bcRow.Children.Add(new Border { Width = 18 });
        bcRow.Children.Add(Figure(FigQr(), vi ? "Mã QR 2D — chứa được nhiều dữ liệu hơn." : "A 2D QR code — holds much more data."));
        bc.Children.Add(bcRow);
        bc.Children.Add(H2(vi ? "Các chuẩn được hỗ trợ" : "Supported symbologies"));
        bc.Children.Add(P("1D: Code 128, EAN-13, EAN-8, UPC-A, UPC-E, Code 39, ITF-14, Codabar"));
        bc.Children.Add(P("2D: QR, Data Matrix"));
        bc.Children.Add(H2(vi ? "Cài đặt mã QR" : "QR Code settings"));
        bc.Children.Add(P(vi ? "• Chế độ 2D: Tự động theo dữ liệu, hoặc Cố định phiên bản / cỡ mô-đun." : "• Sizing Mode: Auto (size by data amount) or Fixed (constant size)."));
        bc.Children.Add(P(vi ? "• Mức ECC (sửa lỗi): L 7% · M 15% · Q 25% · H 30%. Mức cao vẫn quét được khi mã bị xước/bẩn, nhưng mã sẽ dày hơn." : "• Error Correction: L 7% · M 15% · Q 25% · H 30%. Higher levels still scan when scratched/dirty, but make a denser code."));
        bc.Children.Add(P(vi ? "• Phiên bản ký hiệu 1–40 và Cỡ mô-đun (px) dùng ở chế độ cố định." : "• Fixed Version 1–40, Module Size 1–10 px, and DPI."));
        bc.Children.Add(InfoBox(vi ? "Quan trọng: ở chế độ cố định, dữ liệu phải vừa dung lượng của phiên bản đã chọn — nếu không sẽ báo lỗi đỏ ở bảng Thuộc tính." : "Important: in Fixed mode the data must fit the chosen Version's capacity — otherwise a red error shows in the Properties panel.", "#FEF2F2", "#FCA5A5", "⚠️"));
        d["barcode"] = bc;

        // ---------- DATA BINDING ----------
        var bd = new StackPanel();
        bd.Children.Add(H1(vi ? "🔗 Liên kết dữ liệu" : "🔗 Data Binding"));
        bd.Children.Add(Sub(vi ? "Đây là tính năng mạnh nhất: thay vì gõ tay từng nhãn, bạn nối một ô trên nhãn vào một cột Excel. Mỗi dòng Excel thành một nhãn." : "This is the most powerful feature: instead of typing each label, you connect a label field to an Excel column. Each Excel row becomes a label."));
        bd.Children.Add(Figure(FigBindingFlow(vi), vi ? "Giá trị trong cột Excel chảy vào ô tương ứng trên nhãn khi in." : "Values from the Excel column flow into the matching label field at print time."));
        bd.Children.Add(H2(vi ? "Các bước liên kết" : "How to bind"));
        bd.Children.Add(StepCard(1, vi ? "Nhập Excel" : "Import Excel", vi ? "Nhấn Nhập Excel trên thanh công cụ, chọn tệp và sheet." : "Click Import Excel on the ribbon, choose the file and sheet."));
        bd.Children.Add(StepCard(2, vi ? "Chọn đối tượng" : "Select the object", vi ? "Nhấp vào Chữ, Ô chữ hoặc Mã vạch trên bảng thiết kế." : "Click the Text Box or Barcode on the canvas."));
        bd.Children.Add(StepCard(3, vi ? "Đổi Nguồn" : "Change Source", vi ? "Trong thẻ Nội dung, đặt Nguồn = “Trường Excel”." : "In Content, set Source = \"Excel Field\"."));
        bd.Children.Add(StepCard(4, vi ? "Chọn cột" : "Pick the column", vi ? "Chọn cột trong danh sách “Trường Excel”. Xong! Xem trước để kiểm tra." : "Choose the Excel column from the \"Excel field\" list. Done! Preview to verify."));
        bd.Children.Add(InfoBox(vi ? "Nếu một đối tượng báo đỏ trong danh sách Lớp hoặc bảng Thuộc tính, nghĩa là cột bị thiếu hoặc đổi tên — hãy liên kết lại." : "If a field shows a red background in the Objects tree or Properties, the column is missing or renamed — rebind it.", "#FEF2F2", "#FCA5A5", "⚠️"));
        d["binding"] = bd;

        // ---------- EXCEL ----------
        var xl = new StackPanel();
        xl.Children.Add(H1(vi ? "📑 Dữ liệu Excel" : "📑 Excel Data"));
        xl.Children.Add(Sub(vi ? "ANLAbel dùng file Excel (.xlsx) như một cơ sở dữ liệu cho nhãn. Mỗi dòng = một nhãn, mỗi cột = một trường." : "ANLAbel uses an Excel (.xlsx) file as the label database. Each row = one label, each column = one field."));
        xl.Children.Add(Figure(FigDataGrid(vi), vi ? "Bảng dữ liệu Excel sau khi nhập. Dòng đầu là tên cột; cột Bản do ANLAbel thêm vào để chỉnh số bản in." : "The imported Excel data. The first row holds column names; the Copies column is added by ANLAbel to set prints per row."));
        xl.Children.Add(H2(vi ? "Chuẩn bị file Excel" : "Preparing the Excel file"));
        xl.Children.Add(P(vi ? "• Dòng đầu tiên phải là tên cột (dòng tiêu đề), ví dụ: Tên, Giá, SKU." : "• The first row must be column headers, e.g. Name, Price, SKU."));
        xl.Children.Add(P(vi ? "• Mỗi dòng tiếp theo là dữ liệu cho một nhãn." : "• Each following row is the data for one label."));
        xl.Children.Add(P(vi ? "• Tránh ô gộp (merged cells) và dòng trống xen giữa." : "• Avoid merged cells and blank rows in the middle."));
        xl.Children.Add(H2(vi ? "Nhập & cập nhật" : "Import & update"));
        xl.Children.Add(P(vi ? "Nhấn Nhập Excel để liên kết tệp. Khi sửa tệp Excel bên ngoài, nhấn Cập nhật Excel để nạp lại dữ liệu mới." : "Click Import Excel to link the file. After editing the Excel file externally, click Update Excel to reload the new data."));
        d["excel"] = xl;

        // ---------- PRINTING ----------
        var pr = new StackPanel();
        pr.Children.Add(H1(vi ? "🖨️ In ấn" : "🖨️ Printing"));
        pr.Children.Add(Sub(vi ? "Khi thiết kế xong, đây là cách đưa nhãn ra giấy/decal." : "Once the design is ready, here's how to get labels onto paper/decal."));
        pr.Children.Add(Figure(FigPrintPreview(vi), vi ? "Cửa sổ Xem trước: nhãn ở giữa, cài đặt in bên phải, bảng dữ liệu Excel phía dưới." : "Print Preview window: the label in the middle, print settings on the right, the Excel data table at the bottom."));
        pr.Children.Add(H2(vi ? "1. Thiết lập máy in" : "1. Printer setup"));
        pr.Children.Add(P(vi ? "Nhấn Máy in · Cài đặt để chọn máy in, khổ tem, DPI và hướng in. Kiểm tra tên máy in hiển thị trên thanh tiêu đề." : "Click Printer Setup to choose printer, label size, DPI and orientation. Check the printer name & paper size on the title bar."));
        pr.Children.Add(H2(vi ? "2. Xem trước (Ctrl+P)" : "2. Preview (Ctrl+P)"));
        pr.Children.Add(P(vi ? "• Giữa: nhãn với dữ liệu thật, có nút chuyển trang." : "• Center: the label with real data, with page navigation."));
        pr.Children.Add(P(vi ? "• Phải: cài đặt in và thiết lập tem." : "• Right: print settings, calibration, label setup."));
        pr.Children.Add(P(vi ? "• Dưới: bảng dữ liệu Excel hiển thị tất cả các cột." : "• Bottom: the Excel data table showing all columns."));
        pr.Children.Add(H2(vi ? "3. Số bản in mỗi dòng" : "3. Copies per row"));
        pr.Children.Add(P(vi ? "Mỗi dòng có cột Bản với nút ▲▼. Mặc định 1 bản. Tăng lên nếu cần in cùng một tem nhiều lần." : "Each row has a Copies column with ▲▼ buttons. Default is 1. Increase it to print the same label multiple times."));
        pr.Children.Add(H2(vi ? "4. Chọn dòng & In" : "4. Print mode & print"));
        pr.Children.Add(P(vi ? "Đánh dấu các dòng cần in trong bảng Dữ liệu Excel (☑ Tất cả để chọn hết), rồi nhấn In. Có thể in nhanh bằng In · Dòng này / In mọi dòng ngay trên thanh công cụ." : "Choose \"Current row only\" or \"All rows\", then Print. You can also print directly from Print Current / Print All Rows on the ribbon."));
        pr.Children.Add(InfoBox(vi ? "Mỗi lần in đều được ghi vào lịch sử — nhấn Lịch sử in để xem. Lệnh nào chưa rõ kết quả sẽ hiện trong Trung tâm in để đối soát trước khi in lại." : "Every print is logged to a history file — click Print History to open and review it.", "#EFF6FF", "#93C5FD", "🗒️"));
        d["printer"] = pr;

        // ---------- CALIBRATION ----------
        var ca = new StackPanel();
        ca.Children.Add(H1(vi ? "🎯 Hiệu chỉnh khổ in" : "🎯 Calibration"));
        ca.Children.Add(Sub(vi ? "Đôi khi bản in bị lệch vài milimet so với thiết kế (do máy in). Hiệu chỉnh giúp kéo nó về đúng vị trí." : "Sometimes the print drifts a few millimeters from the design (printer quirk). Calibration nudges it back into place."));
        ca.Children.Add(Figure(FigCalibration(vi), vi ? "Khung xanh nét đứt = vị trí thiết kế; khung đỏ = bản in bị lệch. Lệch X/Y dịch bản in về khớp." : "Blue dashed box = designed position; red box = drifted print. Offset X/Y shifts the print back into alignment."));
        ca.Children.Add(H2(vi ? "Các bước hiệu chỉnh" : "How to calibrate"));
        ca.Children.Add(StepCard(1, vi ? "Mở Cài đặt máy in" : "Open Printer Setup", vi ? "Nhấn Máy in · Cài đặt trên thanh công cụ." : "Click Printer Setup on the ribbon."));
        ca.Children.Add(StepCard(2, vi ? "Tìm phần Căn chỉnh khổ in" : "Find the calibration section", vi ? "Ở phần dưới của cửa sổ Cài đặt máy in." : "Near the bottom of Printer Setup."));
        ca.Children.Add(StepCard(3, vi ? "In mẫu thử" : "Print the test pattern", vi ? "Nhấn “In hiệu chỉnh” (hoặc In thử trên thanh công cụ) và đo độ lệch trên giấy bằng thước." : "Click \"Print calibration\" (or Test Print on the ribbon) and measure the drift on paper with a ruler."));
        ca.Children.Add(StepCard(4, vi ? "Nhập Lệch & Tỉ lệ" : "Enter Offset & Scale", vi ? "Lệch X/Y dịch chuyển (mm); Tỉ lệ X/Y sửa sai số kích thước. Không dùng chúng để đổi kích thước tem thiết kế." : "Offset X/Y shifts (mm); Scale X/Y corrects size error."));
        ca.Children.Add(InfoBox(vi ? "Ví dụ: nếu tem in lệch sang phải 2 mm, đặt Lệch X = -2 để kéo ngược lại." : "Example: if the label prints 2mm too far right, set Offset X = -2.00 to pull it back."));
        d["calibration"] = ca;

        // ---------- SHORTCUTS ----------
        var sh = new StackPanel();
        sh.Children.Add(H1(vi ? "⌨️ Phím tắt" : "⌨️ Keyboard Shortcuts"));
        sh.Children.Add(Sub(vi ? "Học vài phím tắt giúp bạn làm nhanh hơn rất nhiều." : "A few shortcuts make you much faster."));
        sh.Children.Add(H2(vi ? "Toàn cục" : "Global"));
        sh.Children.Add(ShortcutRow("Ctrl+N", vi ? "Tạo mẫu tem mới" : "New template"));
        sh.Children.Add(ShortcutRow("Ctrl+O", vi ? "Mở mẫu tem" : "Open template"));
        sh.Children.Add(ShortcutRow("Ctrl+S", vi ? "Lưu mẫu tem" : "Save template"));
        sh.Children.Add(ShortcutRow("Ctrl+Shift+S", vi ? "Lưu thành tệp mới" : "Save as a new file"));
        sh.Children.Add(ShortcutRow("Ctrl+Z", vi ? "Hoàn tác" : "Undo"));
        sh.Children.Add(ShortcutRow("Ctrl+Y", vi ? "Làm lại" : "Redo"));
        sh.Children.Add(ShortcutRow("Ctrl+P", vi ? "Xem trước khi in" : "Print Preview"));
        sh.Children.Add(ShortcutRow("F1", vi ? "Mở hướng dẫn" : "Open Help"));
        sh.Children.Add(ShortcutRow("Delete", vi ? "Xóa đối tượng đang chọn" : "Delete selected object"));
        sh.Children.Add(H2(vi ? "Bảng thiết kế" : "Canvas"));
        sh.Children.Add(ShortcutRow(vi ? "Ctrl + lăn chuột" : "Ctrl+Wheel", vi ? "Phóng to / thu nhỏ" : "Zoom in/out"));
        d["shortcuts"] = sh;

        // ---------- FAQ ----------
        var fq = new StackPanel();
        fq.Children.Add(H1(vi ? "❓ Hỏi đáp nhanh" : "❓ Quick FAQ"));
        fq.Children.Add(Sub(vi ? "Những trục trặc hay gặp của người mới và cách xử lý." : "Common beginner snags and how to fix them."));
        fq.Children.Add(FaqItem(
            vi ? "Bảng Thuộc tính trống?" : "The Properties panel is empty?",
            vi ? "Bạn chưa chọn đối tượng nào. Nhấp vào một đối tượng trên bảng thiết kế. Nếu lỡ ẩn bảng, nhấn Bảng · hiện lại trên thanh công cụ." : "Nothing is selected. Click an object on the canvas. If the panel is hidden, click Panels restore on the ribbon."));
        fq.Children.Add(FaqItem(
            vi ? "Liên kết Excel hiện nền đỏ?" : "An Excel binding shows red?",
            vi ? "Cột nguồn bị thiếu hoặc đã đổi tên trong tệp Excel. Chọn lại cột trong thẻ Nội dung → Trường Excel, hoặc nhấn Cập nhật Excel." : "The source column is missing or was renamed in the Excel file. Re-pick it in Content → Excel field, or click Update Excel."));
        fq.Children.Add(FaqItem(
            vi ? "Mã vạch báo lỗi đỏ?" : "The barcode shows a red error?",
            vi ? "Dữ liệu không hợp lệ với chuẩn đã chọn (ví dụ EAN-13 cần đúng 13 chữ số), hoặc phiên bản QR cố định quá nhỏ so với dữ liệu. Đổi chuẩn hoặc tăng phiên bản." : "The data is invalid for the chosen symbology (e.g. EAN-13 needs exactly 13 digits), or the QR Fixed Version is too small. Change the symbology or raise the Version."));
        fq.Children.Add(FaqItem(
            vi ? "Bản in bị lệch so với thiết kế?" : "The print is offset from the design?",
            vi ? "Dùng hiệu chỉnh: in tem hiệu chỉnh, đo độ lệch, rồi nhập Lệch X/Y trong Cài đặt máy in. Xem mục 🎯 Hiệu chỉnh." : "Use Calibration: print the test pattern, measure the drift, then enter Offset X/Y. See the 🎯 Calibration topic."));
        fq.Children.Add(FaqItem(
            vi ? "Sửa Excel xong mà nhãn không đổi?" : "Edited Excel but the labels didn't change?",
            vi ? "Nhấn Cập nhật Excel trên thanh công cụ để nạp lại dữ liệu mới nhất." : "Click Update Excel on the ribbon to reload the latest data."));
        fq.Children.Add(FaqItem(
            vi ? "Lỡ thao tác sai?" : "Made a mistake?",
            vi ? "Nhấn Ctrl+Z để hoàn tác, Ctrl+Y để làm lại." : "Press Ctrl+Z to undo, Ctrl+Y to redo."));
        d["faq"] = fq;

        // ---------- ABOUT ----------
        var ab = new StackPanel();
        ab.Children.Add(H1(vi ? "ℹ️ Giới thiệu ANLAbel" : "ℹ️ About ANLAbel"));
        ab.Children.Add(P("ANLAbel - " + (vi ? "Thiết kế Nhãn" : "Label Designer")));
        ab.Children.Add(P((vi ? "Tác giả: " : "Created by ") + "Duc An"));
        ab.Children.Add(P("Email: ducancdt@gmail.com"));
        ab.Children.Add(P((vi ? "Phiên bản: " : "Version: ") + "v0.319"));
        var updateBtn = new Button
        {
            Content = vi ? "🔄 Kiểm tra bản cập nhật mới (GitHub)" : "🔄 Check for Updates (GitHub)",
            Padding = new Thickness(12, 6, 12, 6),
            Margin = new Thickness(0, 8, 0, 14),
            HorizontalAlignment = HorizontalAlignment.Left,
            Background = Hex("#1464D2"),
            Foreground = Brushes.White,
            FontWeight = FontWeights.SemiBold,
            Cursor = System.Windows.Input.Cursors.Hand
        };
        updateBtn.Click += (_, _) => new UpdateWindow { Owner = this }.ShowDialog();
        ab.Children.Add(updateBtn);
        ab.Children.Add(H2(vi ? "Bản quyền" : "Copyright"));
        ab.Children.Add(P("Copyright © 2024–2026 Duc An."));
        ab.Children.Add(P(vi
            ? "ANLAbel là phần mềm tự do, được cấp phép theo GNU General Public License phiên bản 3.0 (GPL-3.0-only). Bạn được quyền sử dụng, nghiên cứu, sửa đổi và phân phối lại theo các điều khoản của GPL. Mã nguồn: https://github.com/ducancdt/anlabel"
            : "ANLAbel is free software licensed under the GNU General Public License version 3.0 (GPL-3.0-only). You may use, study, modify, and redistribute it under the GPL terms. Source code: https://github.com/ducancdt/anlabel"));
        ab.Children.Add(P(vi
            ? "Phần mềm được cung cấp KHÔNG KÈM BẤT KỲ BẢO HÀNH NÀO. Bản sửa đổi được phân phối phải giữ giấy phép GPL và cung cấp mã nguồn tương ứng."
            : "This software is provided WITHOUT ANY WARRANTY. Distributed modified versions must preserve the GPL and provide the corresponding source code."));
        ab.Children.Add(H2(vi ? "Giấy phép thư viện bên thứ ba" : "Third-Party Licenses"));
        ab.Children.Add(P(vi ? "ANLAbel sử dụng các thư viện mã nguồn mở. Thông báo giấy phép được ghi lại bên dưới theo yêu cầu." : "ANLAbel includes open-source components. Their license notices are reproduced below as required."));
        ab.Children.Add(LicenseBox("ZXing.Net — Apache License 2.0",
            "Copyright 2012 ZXing.Net authors\n\n" +
            "Licensed under the Apache License, Version 2.0 (the \"License\"); you may not use\n" +
            "this file except in compliance with the License. You may obtain a copy of the\n" +
            "License at: http://www.apache.org/licenses/LICENSE-2.0\n\n" +
            "Unless required by applicable law or agreed to in writing, software distributed\n" +
            "under the License is distributed on an \"AS IS\" BASIS, WITHOUT WARRANTIES OR\n" +
            "CONDITIONS OF ANY KIND, either express or implied.\n\n" +
            (vi ? "ZXing.Net là bản chuyển đổi của thư viện mã vạch ZXing viết bằng Java." : "ZXing.Net is a port of the ZXing barcode library originally written in Java.")));
        ab.Children.Add(LicenseBox("ClosedXML — MIT License",
            "Copyright (c) 2016-present ClosedXML contributors\n\n" +
            "Permission is hereby granted, free of charge, to any person obtaining a copy\n" +
            "of this software and associated documentation files (the \"Software\"), to deal\n" +
            "in the Software without restriction, including without limitation the rights\n" +
            "to use, copy, modify, merge, publish, distribute, sublicense, and/or sell\n" +
            "copies of the Software, and to permit persons to whom the Software is\n" +
            "furnished to do so, subject to the following conditions:\n\n" +
            "The above copyright notice and this permission notice shall be included in all\n" +
            "copies or substantial portions of the Software.\n\n" +
            "THE SOFTWARE IS PROVIDED \"AS IS\", WITHOUT WARRANTY OF ANY KIND."));
        ab.Children.Add(LicenseBox(".NET Runtime & WPF — MIT License",
            "Copyright (c) .NET Foundation and Contributors\n\n" +
            "Permission is hereby granted, free of charge, to any person obtaining a copy\n" +
            "of this software and associated documentation files (the \"Software\"), to deal\n" +
            "in the Software without restriction, including without limitation the rights\n" +
            "to use, copy, modify, merge, publish, distribute, sublicense, and/or sell\n" +
            "copies of the Software, and to permit persons to whom the Software is\n" +
            "furnished to do so, subject to the following conditions:\n\n" +
            "The above copyright notice and this permission notice shall be included in all\n" +
            "copies or substantial portions of the Software.\n\n" +
            "THE SOFTWARE IS PROVIDED \"AS IS\", WITHOUT WARRANTY OF ANY KIND."));
        ab.Children.Add(H2(vi ? "Font chữ" : "Fonts"));
        ab.Children.Add(P(vi ? "ANLAbel sử dụng font chữ hệ thống của Microsoft Windows (Arial, Calibri, Segoe UI, Tahoma, Verdana, Consolas, Courier New, Bahnschrift, Lucida Console). Các font này không đóng gói cùng ANLAbel. Cần có giấy phép Windows hợp lệ để sử dụng phần mềm." : "ANLAbel uses system fonts provided by Microsoft Windows (Arial, Calibri, Segoe UI, Tahoma, Verdana, Consolas, Courier New, Bahnschrift, Lucida Console). These fonts are not bundled with ANLAbel. A valid Windows license is required to use this software."));
        d["about"] = ab;

        return d;
    }
}
