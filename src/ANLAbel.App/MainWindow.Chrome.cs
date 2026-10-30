using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Shell;
using System.Windows.Threading;

namespace ANLAbel.App;

/// <summary>
/// NiceLabel 10 title bar (docs/NICELABEL_10_UI_LAYOUT.md, "Shell rebuild"): the window
/// draws its own navy caption with the quick access commands, so the minimise, maximise
/// and close buttons, the system menu and the maximised layout are handled here.
/// </summary>
public partial class MainWindow
{
    private const double TitleBarHeight = 34;
    private const double ResizeBorder = 6;

    private void InitializeWindowChrome()
    {
        StateChanged += (_, _) =>
        {
            ApplyWindowChromeState();
            QueueFitMaximizedContent();
        };
        SizeChanged += (_, _) => QueueFitMaximizedContent();
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
        MaximizeButton.ToolTip = maximized ? "Khôi phục" : "Phóng to";
        AutomationProperties.SetName(MaximizeButton, maximized ? "Restore" : "Maximize");
    }

    private void QueueFitMaximizedContent() =>
        Dispatcher.BeginInvoke(FitMaximizedContent, DispatcherPriority.Loaded);

    /// <summary>
    /// Windows places a maximised window a frame's width beyond the monitor on every side.
    /// Measured on the shown content (2026-10-30, 150 %): WPF pulls the content back on the
    /// left, top and right but the bottom 11 px stay under the taskbar. Whatever part of
    /// the content still lies outside the work area becomes a margin, so the status bar
    /// ends exactly at the taskbar.
    /// </summary>
    private void FitMaximizedContent()
    {
        if (Content is not FrameworkElement root)
        {
            return;
        }

        var margin = new Thickness(0);
        var source = PresentationSource.FromVisual(this);
        var handle = new WindowInteropHelper(this).Handle;
        if (WindowState == WindowState.Maximized && source?.CompositionTarget is { } target && handle != IntPtr.Zero)
        {
            const uint MonitorDefaultToNearest = 2;
            var info = new MonitorInfo { Size = Marshal.SizeOf<MonitorInfo>() };
            if (GetMonitorInfo(MonitorFromWindow(handle, MonitorDefaultToNearest), ref info))
            {
                // Content bounds in device pixels without the margin being corrected.
                var current = root.Margin;
                var topLeft = root.PointToScreen(new Point(-current.Left, -current.Top));
                var bottomRight = root.PointToScreen(new Point(root.ActualWidth + current.Right, root.ActualHeight + current.Bottom));
                var toDip = target.TransformFromDevice;
                margin = new Thickness(
                    Math.Max(0, info.Work.Left - topLeft.X) * toDip.M11,
                    Math.Max(0, info.Work.Top - topLeft.Y) * toDip.M22,
                    Math.Max(0, bottomRight.X - info.Work.Right) * toDip.M11,
                    Math.Max(0, bottomRight.Y - info.Work.Bottom) * toDip.M22);
            }
        }

        if (root.Margin != margin)
        {
            root.Margin = margin;
        }
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

    [DllImport("user32.dll")]
    private static extern IntPtr MonitorFromWindow(IntPtr hwnd, uint flags);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetMonitorInfo(IntPtr monitor, ref MonitorInfo info);

    [StructLayout(LayoutKind.Sequential)]
    private struct NativeRect
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
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
