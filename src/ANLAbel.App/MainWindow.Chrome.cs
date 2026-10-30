using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Shell;

namespace ANLAbel.App;

/// <summary>
/// NiceLabel 10 title bar (docs/NICELABEL_10_UI_LAYOUT.md, "Shell rebuild"): the window
/// draws its own navy caption with the quick access commands, so the minimise, maximise
/// and close buttons, the system menu and the maximised bounds are handled here.
/// </summary>
public partial class MainWindow
{
    private const double TitleBarHeight = 34;
    private const double ResizeBorder = 6;

    private void InitializeWindowChrome()
    {
        SourceInitialized += (_, _) =>
            HwndSource.FromHwnd(new WindowInteropHelper(this).Handle)?.AddHook(ChromeWindowProc);
        StateChanged += (_, _) => ApplyWindowChromeState();
        ApplyWindowChromeState();
    }

    /// <summary>
    /// A maximised window has no resize border: the whole title bar drags (and restores)
    /// the window, and the maximise button turns into restore.
    /// </summary>
    private void ApplyWindowChromeState()
    {
        var maximized = WindowState == WindowState.Maximized;
        WindowChrome.SetWindowChrome(this, new WindowChrome
        {
            CaptionHeight = maximized ? TitleBarHeight : TitleBarHeight - ResizeBorder,
            ResizeBorderThickness = new Thickness(maximized ? 0 : ResizeBorder),
            GlassFrameThickness = new Thickness(0, 0, 0, 1),
            CornerRadius = new CornerRadius(0),
            UseAeroCaptionButtons = false
        });
        MaximizeGlyph.Text = maximized ? "" : "";
        var label = maximized ? "Khôi phục" : "Phóng to";
        MaximizeButton.ToolTip = label;
        AutomationProperties.SetName(MaximizeButton, maximized ? "Restore" : "Maximize");
    }

    private void MinimizeWindow_Click(object sender, RoutedEventArgs e) => SystemCommands.MinimizeWindow(this);

    private void MaximizeRestoreWindow_Click(object sender, RoutedEventArgs e)
    {
        if (WindowState == WindowState.Maximized)
        {
            SystemCommands.RestoreWindow(this);
        }
        else
        {
            SystemCommands.MaximizeWindow(this);
        }
    }

    // Goes through the normal close path, so unsaved changes are still asked about.
    private void CloseWindow_Click(object sender, RoutedEventArgs e) => SystemCommands.CloseWindow(this);

    /// <summary>Windows convention for the caption icon: click opens the system menu, double-click closes.</summary>
    private void AppIcon_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        e.Handled = true;
        if (e.ClickCount == 2)
        {
            SystemCommands.CloseWindow(this);
            return;
        }

        var icon = (FrameworkElement)sender;
        var corner = icon.PointToScreen(new Point(0, icon.ActualHeight));
        var fromDevice = PresentationSource.FromVisual(this)?.CompositionTarget?.TransformFromDevice;
        SystemCommands.ShowSystemMenu(this, fromDevice?.Transform(corner) ?? corner);
    }

    /// <summary>
    /// A custom caption keeps the maximised window inside the monitor's work area (not
    /// under the taskbar, no edge cut off by the hidden frame).
    /// </summary>
    private static IntPtr ChromeWindowProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        const int WmGetMinMaxInfo = 0x0024;
        if (msg == WmGetMinMaxInfo)
        {
            FitMaximizedBoundsToWorkArea(hwnd, lParam);
        }

        return IntPtr.Zero;
    }

    private static void FitMaximizedBoundsToWorkArea(IntPtr hwnd, IntPtr lParam)
    {
        const uint MonitorDefaultToNearest = 2;
        var monitor = MonitorFromWindow(hwnd, MonitorDefaultToNearest);
        var info = new MonitorInfo { Size = Marshal.SizeOf<MonitorInfo>() };
        if (monitor == IntPtr.Zero || !GetMonitorInfo(monitor, ref info))
        {
            return;
        }

        var limits = Marshal.PtrToStructure<MinMaxInfo>(lParam);
        limits.MaxPosition.X = info.Work.Left - info.Monitor.Left;
        limits.MaxPosition.Y = info.Work.Top - info.Monitor.Top;
        limits.MaxSize.X = info.Work.Right - info.Work.Left;
        limits.MaxSize.Y = info.Work.Bottom - info.Work.Top;
        Marshal.StructureToPtr(limits, lParam, false);
    }

    [DllImport("user32.dll")]
    private static extern IntPtr MonitorFromWindow(IntPtr hwnd, uint flags);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetMonitorInfo(IntPtr monitor, ref MonitorInfo info);

    [StructLayout(LayoutKind.Sequential)]
    private struct NativePoint
    {
        public int X;
        public int Y;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct NativeRect
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct MinMaxInfo
    {
        public NativePoint Reserved;
        public NativePoint MaxSize;
        public NativePoint MaxPosition;
        public NativePoint MinTrackSize;
        public NativePoint MaxTrackSize;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct MonitorInfo
    {
        public int Size;
        public NativeRect Monitor;
        public NativeRect Work;
        public uint Flags;
    }
}
