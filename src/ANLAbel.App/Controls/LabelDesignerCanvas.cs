using System.Collections.Specialized;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Brushes = System.Windows.Media.Brushes;
using Pen = System.Windows.Media.Pen;
using Brush = System.Windows.Media.Brush;
using Color = System.Windows.Media.Color;
using ANLAbel.Barcode.Options;
using ANLAbel.Barcode.Renderers;
using ANLAbel.Core.Enums;
using ANLAbel.Core.Expressions;
using ANLAbel.Core.Expressions.Formulas;
using ANLAbel.Core.Barcode;
using ANLAbel.Core.Geometry;
using ANLAbel.Core.Models;
using ANLAbel.Data.Preferences;
using ANLAbel.Printing.RenderPipeline;

namespace ANLAbel.App.Controls;

public sealed class LabelDesignerCanvas : Canvas
{
    public static readonly DependencyProperty TemplateProperty =
        DependencyProperty.Register(nameof(Template), typeof(LabelTemplate), typeof(LabelDesignerCanvas),
            new FrameworkPropertyMetadata(null, OnTemplateChanged));

    public static readonly DependencyProperty SelectedObjectProperty =
        DependencyProperty.Register(nameof(SelectedObject), typeof(LabelObject), typeof(LabelDesignerCanvas),
            new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnSelectedObjectChanged));

    public static readonly DependencyProperty ZoomProperty =
        DependencyProperty.Register(nameof(Zoom), typeof(double), typeof(LabelDesignerCanvas),
            new FrameworkPropertyMetadata(1.0, OnZoomChanged));

    public static readonly DependencyProperty PreviewRowProperty =
        DependencyProperty.Register(nameof(PreviewRow), typeof(IReadOnlyDictionary<string, string>), typeof(LabelDesignerCanvas),
            new FrameworkPropertyMetadata(null, OnPreviewRowChanged));

    public static readonly DependencyProperty DrawingToolProperty =
        DependencyProperty.Register(nameof(DrawingTool), typeof(ObjectType?), typeof(LabelDesignerCanvas),
            new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnDrawingToolChanged));

    public static readonly DependencyProperty DrawingCommandTextProperty =
        DependencyProperty.Register(nameof(DrawingCommandText), typeof(string), typeof(LabelDesignerCanvas),
            new FrameworkPropertyMetadata(string.Empty, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

    public static readonly DependencyProperty IsSnapToObjectsEnabledProperty =
        DependencyProperty.Register(nameof(IsSnapToObjectsEnabled), typeof(bool), typeof(LabelDesignerCanvas),
            new FrameworkPropertyMetadata(true, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnSnapPreferenceChanged));

    public static readonly DependencyProperty InteractionStatusTextProperty =
        DependencyProperty.Register(nameof(InteractionStatusText), typeof(string), typeof(LabelDesignerCanvas),
            new FrameworkPropertyMetadata(string.Empty, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

    public static readonly RoutedCommand DeleteSelectionCommand = new(nameof(DeleteSelectionCommand), typeof(LabelDesignerCanvas));

    private readonly Dictionary<LabelObject, FrameworkElement> _objectElements = new();
    private readonly HashSet<LabelObject> _selectedObjects = new();
    private readonly HashSet<LabelObject> _matrixAutoSizingObjects = new();
    private readonly List<LabelObject> _clipboardObjects = new();
    private readonly Dictionary<LabelObject, (double X, double Y, double EndX, double EndY)> _groupDragStarts = new();
    private readonly IBarcodeRenderer _barcodeRenderer = new ZxingBarcodeRenderer();
    private readonly DesignerPreferencesService _designerPreferencesService = new();
    private readonly MenuItem _snapMenuItem;
    private SelectionResizeAdorner? _selectionAdorner;
    private LabelObject? _adornedObject;
    private Border? _marqueeElement;
    private Point _dragStart;
    private Point _marqueeStart;
    private double _startXMm;
    private double _startYMm;
    private double _startLineEndXMm;
    private double _startLineEndYMm;
    private LabelObject? _dragObject;
    private bool _isMarqueeSelecting;
    private LabelObject? _drawingObject;
    private Point _drawingStartMm;
    private Point _lastDrawingEndMm;
    private string _dimensionBuffer = string.Empty;
    private int _pasteCount;

    // Alignment guide system
    private const double SnapThresholdMm = 1.0;
    private Line? _guideVertical;
    private Line? _guideHorizontal;

    public LabelDesignerCanvas()
    {
        Background = Brushes.Transparent;
        ClipToBounds = false;
        Focusable = true;
        MouseLeftButtonDown += CanvasMouseButtonDown;
        MouseRightButtonDown += CanvasMouseButtonDown;
        MouseMove += CanvasMouseMove;
        MouseLeftButtonUp += CanvasMouseButtonUp;
        MouseRightButtonUp += CanvasMouseButtonUp;
        LostMouseCapture += CanvasLostMouseCapture;
        KeyDown += CanvasKeyDown;
        _snapMenuItem = new MenuItem
        {
            Header = "Snap to objects",
            IsCheckable = true
        };
        _snapMenuItem.Click += (_, _) => IsSnapToObjectsEnabled = _snapMenuItem.IsChecked;
        ContextMenu = new ContextMenu { Items = { _snapMenuItem } };
        ContextMenuOpening += (_, e) =>
        {
            if (DrawingTool is not null)
            {
                e.Handled = true;
                return;
            }

            _snapMenuItem.IsChecked = IsSnapToObjectsEnabled;
        };

        SetCurrentValue(IsSnapToObjectsEnabledProperty, _designerPreferencesService.Load().SnapToObjects);
        CommandBindings.Add(new CommandBinding(DeleteSelectionCommand, (_, e) =>
        {
            e.Handled = DeleteSelection();
        }));
    }

    public LabelTemplate? Template
    {
        get => (LabelTemplate?)GetValue(TemplateProperty);
        set => SetValue(TemplateProperty, value);
    }

    public LabelObject? SelectedObject
    {
        get => (LabelObject?)GetValue(SelectedObjectProperty);
        set => SetValue(SelectedObjectProperty, value);
    }

    public double Zoom
    {
        get => (double)GetValue(ZoomProperty);
        set => SetValue(ZoomProperty, value);
    }

    public IReadOnlyDictionary<string, string>? PreviewRow
    {
        get => (IReadOnlyDictionary<string, string>?)GetValue(PreviewRowProperty);
        set => SetValue(PreviewRowProperty, value);
    }

    public ObjectType? DrawingTool
    {
        get => (ObjectType?)GetValue(DrawingToolProperty);
        set => SetValue(DrawingToolProperty, value);
    }

    public string DrawingCommandText
    {
        get => (string)GetValue(DrawingCommandTextProperty);
        set => SetValue(DrawingCommandTextProperty, value);
    }

    public bool IsSnapToObjectsEnabled
    {
        get => (bool)GetValue(IsSnapToObjectsEnabledProperty);
        set => SetValue(IsSnapToObjectsEnabledProperty, value);
    }

    public string InteractionStatusText
    {
        get => (string)GetValue(InteractionStatusTextProperty);
        set => SetValue(InteractionStatusTextProperty, value);
    }

    protected override void OnRender(DrawingContext dc)
    {
        base.OnRender(dc);
        if (Template is null)
        {
            return;
        }

        var width = MmToDip(Template.WidthMm);
        var height = MmToDip(Template.HeightMm);
        dc.DrawRectangle(Brushes.White, new Pen(new SolidColorBrush(Color.FromRgb(148, 163, 184)), 1), new Rect(0, 0, width, height));

        var gridStep = MmToDip(1);
        var gridPen = new Pen(new SolidColorBrush(Color.FromRgb(226, 232, 240)), 0.6);
        for (var x = gridStep; x < width; x += gridStep)
        {
            dc.DrawLine(gridPen, new Point(x, 0), new Point(x, height));
        }

        for (var y = gridStep; y < height; y += gridStep)
        {
            dc.DrawLine(gridPen, new Point(0, y), new Point(width, y));
        }

        DrawGroupSelection(dc);
        DrawObjectErrors(dc);
    }

    private void Rebuild()
    {
        Children.Clear();
        _objectElements.Clear();
        _selectedObjects.Clear();
        _groupDragStarts.Clear();
        _marqueeElement = null;
        _isMarqueeSelecting = false;
        _guideVertical = null;
        _guideHorizontal = null;
        RemoveSelectionAdorner();

        if (Template is null)
        {
            Width = 300;
            Height = 200;
            InvalidateVisual();
            return;
        }

        var drawingBounds = GetDrawingBoundsDip();
        Width = drawingBounds.Width;
        Height = drawingBounds.Height;

        foreach (var item in Template.Objects.OrderBy(item => item.ZIndex))
        {
            item.PropertyChanged -= ObjectOnPropertyChanged;
            item.Style.PropertyChanged -= StyleOnPropertyChanged;
            item.PropertyChanged += ObjectOnPropertyChanged;
            item.Style.PropertyChanged += StyleOnPropertyChanged;
            AddObjectElement(item);
        }

        InvalidateVisual();
    }

    private void AddObjectElement(LabelObject item)
    {
        var element = CreateObjectElement(item);
        element.Cursor = item.IsLocked ? Cursors.Arrow : Cursors.SizeAll;
        element.PreviewMouseLeftButtonDown += (sender, e) =>
        {
            Focus();
            if (DrawingTool is not null)
            {
                return;
            }

            e.Handled = true;
            SelectedObject = item;
            if (!_selectedObjects.Contains(item))
            {
                _selectedObjects.Clear();
                _selectedObjects.Add(item);
            }

            if (_selectedObjects.Count > 1)
            {
                RemoveSelectionAdorner();
            }

            if (item.IsLocked)
            {
                return;
            }

            _dragObject = item;
            _dragStart = e.GetPosition(this);
            _startXMm = item.XMm;
            _startYMm = item.YMm;
            _startLineEndXMm = item.LineEndXMm;
            _startLineEndYMm = item.LineEndYMm;
            CaptureGroupDragStarts();
            ((FrameworkElement)sender).CaptureMouse();
        };
        element.PreviewMouseMove += (sender, e) =>
        {
            if (_dragObject != item || e.LeftButton != MouseButtonState.Pressed || item.IsLocked)
            {
                return;
            }

            var current = e.GetPosition(this);
            var deltaXMm = DipToMm(current.X - _dragStart.X);
            var deltaYMm = DipToMm(current.Y - _dragStart.Y);
            if (_selectedObjects.Count > 1)
            {
                MoveSelectedGroup(deltaXMm, deltaYMm);
            }
            else if (item.Type == ObjectType.Line)
            {
                var endXMm = _startLineEndXMm == 0 && _startLineEndYMm == 0 ? _startXMm + item.WidthMm : _startLineEndXMm;
                var endYMm = _startLineEndXMm == 0 && _startLineEndYMm == 0 ? _startYMm + item.HeightMm : _startLineEndYMm;
                var minX = Math.Min(_startXMm, endXMm);
                var maxX = Math.Max(_startXMm, endXMm);
                var minY = Math.Min(_startYMm, endYMm);
                var maxY = Math.Max(_startYMm, endYMm);
                var clampedDeltaX = Math.Max(-minX, Math.Min(Template!.WidthMm - maxX, deltaXMm));
                var clampedDeltaY = Math.Max(-minY, Math.Min(Template!.HeightMm - maxY, deltaYMm));
                item.XMm = _startXMm + clampedDeltaX;
                item.YMm = _startYMm + clampedDeltaY;
                item.LineEndXMm = endXMm + clampedDeltaX;
                item.LineEndYMm = endYMm + clampedDeltaY;
            }
            else
            {
                var proposedX = _startXMm + deltaXMm;
                var proposedY = _startYMm + deltaYMm;

                // Alignment guide: compute snap position against other objects
                // Hold Alt to temporarily disable snapping
                var snap = new AlignmentSnapResult(null, null, new List<double>(), new List<double>());
                if (IsSnapToObjectsEnabled
                    && !Keyboard.IsKeyDown(Key.LeftAlt)
                    && !Keyboard.IsKeyDown(Key.RightAlt))
                {
                    snap = ComputeAlignmentSnap(item, proposedX, proposedY);
                    if (snap.SnapX is not null)
                    {
                        proposedX = snap.SnapX.Value;
                    }
                    if (snap.SnapY is not null)
                    {
                        proposedY = snap.SnapY.Value;
                    }
                }

                // Clamp all 4 sides (consistent with group drag behavior)
                item.XMm = Math.Max(0, Math.Min(Template!.WidthMm - item.WidthMm, proposedX));
                item.YMm = Math.Max(0, Math.Min(Template!.HeightMm - item.HeightMm, proposedY));

                // Show/hide guide lines
                ShowAlignmentGuides(snap);
            }

            UpdateObjectElement(item);
        };
        element.PreviewMouseLeftButtonUp += (sender, _) =>
        {
            _dragObject = null;
            _groupDragStarts.Clear();
            HideAlignmentGuides();
            ((FrameworkElement)sender).ReleaseMouseCapture();
        };

        _objectElements[item] = element;
        Children.Add(element);

        UpdateObjectElement(item);
    }

    private FrameworkElement CreateObjectElement(LabelObject item)
    {
        return item.Type switch
        {
            ObjectType.Text => new Border
            {
                Background = Brushes.Transparent,
                ClipToBounds = false,
                Child = new VisualPreviewHost()
            },
            ObjectType.TextBox => new Border
            {
                Background = Brushes.Transparent,
                ClipToBounds = true,
                Child = new VisualPreviewHost()
            },
            ObjectType.Rectangle => new StrokeHitTestRectangleElement(),
            ObjectType.Ellipse => new Ellipse(),
            ObjectType.Line => new Line { StrokeStartLineCap = PenLineCap.Square, StrokeEndLineCap = PenLineCap.Square },
            ObjectType.BarcodeCode128 or ObjectType.QRCode or ObjectType.DataMatrix => new Border
            {
                Background = Brushes.White,
                BorderBrush = Brushes.Transparent,
                BorderThickness = new Thickness(0),
                Child = new Image
                {
                    Stretch = Stretch.Fill,
                    SnapsToDevicePixels = true,
                    UseLayoutRounding = true
                }
            },
            _ => new Border()
        };
    }

    private void UpdateObjectElement(LabelObject item)
    {
        if (!_objectElements.TryGetValue(item, out var element))
        {
            return;
        }

        var x = MmToDip(item.XMm);
        var y = MmToDip(item.YMm);
        var width = MmToDip(item.WidthMm);
        var height = MmToDip(item.HeightMm);
        element.Visibility = item.IsVisible ? Visibility.Visible : Visibility.Collapsed;
        SetLeft(element, x);
        SetTop(element, y);
        SetZIndex(element, item.ZIndex);

        if (element is Line line)
        {
            var endXMm = item.LineEndXMm == 0 && item.LineEndYMm == 0 ? item.XMm + item.WidthMm : item.LineEndXMm;
            var endYMm = item.LineEndXMm == 0 && item.LineEndYMm == 0 ? item.YMm + item.HeightMm : item.LineEndYMm;
            var minXMm = Math.Min(item.XMm, endXMm);
            var minYMm = Math.Min(item.YMm, endYMm);
            var lineWidth = MmToDip(Math.Abs(endXMm - item.XMm));
            var lineHeight = MmToDip(Math.Abs(endYMm - item.YMm));
            line.StrokeThickness = item.Style.OutlineStyle == OutlineStyle.None
                ? 0
                : Math.Max(1, MmToDip(item.Style.BorderThicknessMm));
            var strokePadding = Math.Ceiling(line.StrokeThickness / 2) + 2;
            SetLeft(element, MmToDip(minXMm) - strokePadding);
            SetTop(element, MmToDip(minYMm) - strokePadding);
            line.X1 = MmToDip(item.XMm - minXMm) + strokePadding;
            line.Y1 = MmToDip(item.YMm - minYMm) + strokePadding;
            line.X2 = MmToDip(endXMm - minXMm) + strokePadding;
            line.Y2 = MmToDip(endYMm - minYMm) + strokePadding;
            line.Stroke = ParseBrush(item.Style.StrokeColor, Brushes.Black);
            line.StrokeDashArray = GetDashArray(item.Style.OutlineStyle);
            element.Width = Math.Max(1, lineWidth) + strokePadding * 2;
            element.Height = Math.Max(1, lineHeight) + strokePadding * 2;
        }
        else if (element is StrokeHitTestRectangleElement rectangleElement)
        {
            element.Width = width;
            element.Height = height;
            rectangleElement.Visual.Fill = item.Style.FillStyle == FillStyle.None
                ? Brushes.Transparent
                : ParseBrush(item.Style.FillColor, Brushes.Transparent);
            rectangleElement.Visual.Stroke = item.Style.OutlineStyle == OutlineStyle.None
                ? Brushes.Transparent
                : ParseBrush(item.Style.StrokeColor, Brushes.Black);
            rectangleElement.Visual.StrokeThickness = item.Style.OutlineStyle == OutlineStyle.None
                ? 0
                : Math.Max(0, MmToDip(item.Style.BorderThicknessMm));
            rectangleElement.Visual.StrokeDashArray = GetDashArray(item.Style.OutlineStyle);
            rectangleElement.Visual.RadiusX = MmToDip(item.Style.CornerRadiusMm);
            rectangleElement.Visual.RadiusY = MmToDip(item.Style.CornerRadiusMm);
            rectangleElement.UpdateHitZones(width, height, rectangleElement.Visual.StrokeThickness);
        }
        else if (element is Ellipse ellipse)
        {
            element.Width = width;
            element.Height = height;
            ellipse.Fill = item.Style.FillStyle == FillStyle.None
                ? Brushes.Transparent
                : ParseBrush(item.Style.FillColor, Brushes.Transparent);
            ellipse.Stroke = item.Style.OutlineStyle == OutlineStyle.None
                ? Brushes.Transparent
                : ParseBrush(item.Style.StrokeColor, Brushes.Black);
            ellipse.StrokeThickness = item.Style.OutlineStyle == OutlineStyle.None
                ? 0
                : Math.Max(0, MmToDip(item.Style.BorderThicknessMm));
            ellipse.StrokeDashArray = GetDashArray(item.Style.OutlineStyle);
        }
        else
        {
            if (element is Border border)
            {
                var objectError = GetObjectError(item);
                border.BorderBrush = ParseBrush(item.Style.StrokeColor, Brushes.Black);
                border.BorderThickness = item.Type == ObjectType.TextBox || item.Style.OutlineStyle == OutlineStyle.None
                    ? new Thickness(0)
                    : new Thickness(Math.Max(0, MmToDip(item.Style.BorderThicknessMm)));
                border.ToolTip = objectError;
                border.Background = item.Type == ObjectType.Rectangle
                    ? ParseBrush(item.Style.FillColor, Brushes.Transparent)
                    : Brushes.Transparent;

                    if (border.Child is VisualPreviewHost textHost && item.Type == ObjectType.Text)
                    {
                        border.ClipToBounds = false;
                        border.Clip = null;
                        FitTextObjectToContent(item, ref width, ref height);
                        textHost.Width = Math.Max(1, width);
                    textHost.Height = Math.Max(1, height);
                    textHost.PreviewVisual = CreateTextVisual(item, width, height);
                }
                else if (border.Child is VisualPreviewHost textBoxHost)
                {
                    border.ClipToBounds = true;
                    border.Clip = new RectangleGeometry(new Rect(0, 0, Math.Max(1, width), Math.Max(1, height)));
                    textBoxHost.Width = Math.Max(1, width);
                    textBoxHost.Height = Math.Max(1, height);
                    textBoxHost.PreviewVisual = CreateTextBoxVisual(item, width, height);
                }
                else if (border.Child is Image image)
                {
                    image.Source = CreateBarcodeImageSource(item);
                }
            }

            element.Width = width;
            element.Height = height;
        }

        ApplyObjectRotation(element, item);

        element.InvalidateMeasure();
        element.InvalidateArrange();
        if (ReferenceEquals(item, _adornedObject))
        {
            _selectionAdorner?.InvalidateMeasure();
            _selectionAdorner?.InvalidateArrange();
            _selectionAdorner?.InvalidateVisual();
        }

        if (ReferenceEquals(item, SelectedObject) && _selectedObjects.Count <= 1)
        {
            ShowSelectionAdorner(item);
        }
    }

    private void ObjectOnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (sender is LabelObject item)
        {
            // Matrix auto-fit: run when user explicitly changes barcode properties (not during render).
            // This is the only place where WidthMm/HeightMm should change due to auto-sizing.
            if (ShouldApplyMatrixAutoSize(e.PropertyName) && IsMatrixBarcode(item) && !_matrixAutoSizingObjects.Contains(item))
            {
                TryApplyMatrixAutoSize(item);
            }

            // Enforce square for non-auto-sized matrix barcodes when user changes W or H
            if (e.PropertyName is nameof(LabelObject.WidthMm) or nameof(LabelObject.HeightMm)
                && IsMatrixBarcode(item)
                && !IsAutoSizedMatrixBarcode(item)
                && !_matrixAutoSizingObjects.Contains(item)
                && Math.Abs(item.WidthMm - item.HeightMm) > 0.01)
            {
                var oldW = item.WidthMm;
                var oldH = item.HeightMm;
                var fittedSizeMm = e.PropertyName == nameof(LabelObject.WidthMm)
                    ? oldW
                    : oldH;
                _matrixAutoSizingObjects.Add(item);
                try
                {
                    if (e.PropertyName == nameof(LabelObject.WidthMm))
                    {
                        item.HeightMm = fittedSizeMm;
                        item.YMm += (oldH - fittedSizeMm) / 2.0;
                    }
                    else
                    {
                        item.WidthMm = fittedSizeMm;
                        item.XMm += (oldW - fittedSizeMm) / 2.0;
                    }
                }
                finally
                {
                    _matrixAutoSizingObjects.Remove(item);
                }
            }

            UpdateObjectElement(item);
            UpdateCanvasExtent();
        }

        InvalidateVisual();
    }

    private void DrawObjectErrors(DrawingContext dc)
    {
        if (Template is null)
        {
            return;
        }

        var pen = new Pen(Brushes.Red, 1.6)
        {
            DashStyle = DashStyles.Dash
        };
        foreach (var item in Template.Objects.Where(item => item.IsVisible))
        {
            if (GetObjectError(item) is null)
            {
                continue;
            }

            var rect = InflateRect(GetObjectBounds(item), 2);
            dc.DrawRectangle(null, pen, rect);
            DrawErrorBadge(dc, rect);
        }
    }

    private static void DrawErrorBadge(DrawingContext dc, Rect rect)
    {
        const double radius = 7;
        var center = new Point(rect.Right - radius, rect.Top + radius);
        dc.DrawEllipse(Brushes.Red, null, center, radius, radius);
        var text = new FormattedText(
            "!",
            CultureInfo.CurrentCulture,
            FlowDirection.LeftToRight,
            new Typeface("Segoe UI"),
            11,
            Brushes.White,
            1.0)
        {
            TextAlignment = TextAlignment.Center
        };
        dc.DrawText(text, new Point(center.X, center.Y - text.Height / 2 - 0.5));
    }

    private void UpdateCanvasExtent()
    {
        if (Template is null)
        {
            return;
        }

        var drawingBounds = GetDrawingBoundsDip();
        Width = drawingBounds.Width;
        Height = drawingBounds.Height;
    }

    private Rect GetDrawingBoundsDip()
    {
        if (Template is null)
        {
            return new Rect(0, 0, 300, 200);
        }

        var rightMm = Template.WidthMm;
        var bottomMm = Template.HeightMm;
        foreach (var item in Template.Objects)
        {
            var bounds = GetObjectBoundsMm(item);
            rightMm = Math.Max(rightMm, bounds.Right);
            bottomMm = Math.Max(bottomMm, bounds.Bottom);
        }

        // Extra workspace keeps overflowed objects visible and selectable outside the label edge.
        return new Rect(0, 0, MmToDip(rightMm + 10), MmToDip(bottomMm + 10));
    }

    private void StyleOnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        var item = _objectElements.Keys.FirstOrDefault(candidate => ReferenceEquals(candidate.Style, sender));
        if (item is not null)
        {
            UpdateObjectElement(item);
        }
    }

    private double MmToDip(double mm)
    {
        return MmConverter.MmToDip(mm) * Zoom;
    }

    private double DipToMm(double dip)
    {
        return MmConverter.DipToMm(dip / Zoom);
    }

    private static void ApplyObjectRotation(FrameworkElement element, LabelObject item)
    {
        if (item.Rotation == 0)
        {
            element.RenderTransform = null;
            element.RenderTransformOrigin = new Point(0.5, 0.5);
            return;
        }

        element.RenderTransformOrigin = new Point(0.5, 0.5);
        element.RenderTransform = new RotateTransform(item.Rotation);
    }

    private static Brush ParseBrush(string color, Brush fallback)
    {
        try
        {
            return (Brush)new BrushConverter().ConvertFromString(color)!;
        }
        catch
        {
            return fallback;
        }
    }

    private static DoubleCollection? GetDashArray(OutlineStyle outlineStyle)
    {
        return outlineStyle switch
        {
            OutlineStyle.Dash => new DoubleCollection { 4, 2 },
            OutlineStyle.Dot => new DoubleCollection { 1, 2 },
            _ => null
        };
    }

    private static bool IsMatrixBarcode(LabelObject item)
    {
        return item.Type == ObjectType.QRCode
            || item.Type == ObjectType.DataMatrix
            || item.Type == ObjectType.BarcodeCode128
                && item.BarcodeSymbology is BarcodeSymbology.QRCode
                    or BarcodeSymbology.DataMatrix
                    or BarcodeSymbology.Aztec
                    or BarcodeSymbology.Pdf417;
    }

    private static bool IsAutoSizedMatrixBarcode(LabelObject item)
    {
        return IsMatrixBarcode(item) && item.QrSizingMode == QrSizingMode.AutoSizeByData;
    }

    private static bool ShouldApplyMatrixAutoSize(string? propertyName)
    {
        return propertyName is nameof(LabelObject.Text)
            or nameof(LabelObject.BindingExpression)
            or nameof(LabelObject.BarcodeSymbology)
            or nameof(LabelObject.Type)
            or nameof(LabelObject.QrSizingMode)
            or nameof(LabelObject.QrErrorCorrection)
            or nameof(LabelObject.QrFixedVersion)
            or nameof(LabelObject.QrModuleSizePx)
            or nameof(LabelObject.QrQuietZoneModules)
            or nameof(LabelObject.QrDpi);
    }

    private static void OnTemplateChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (e.OldValue is LabelTemplate oldTemplate)
        {
            oldTemplate.Objects.CollectionChanged -= ((LabelDesignerCanvas)d).ObjectsOnCollectionChanged;
        }

        if (e.NewValue is LabelTemplate newTemplate)
        {
            newTemplate.Objects.CollectionChanged += ((LabelDesignerCanvas)d).ObjectsOnCollectionChanged;
        }

        ((LabelDesignerCanvas)d).Rebuild();
    }

    private void ObjectsOnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        Rebuild();
    }

    private static void OnSelectedObjectChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var canvas = (LabelDesignerCanvas)d;
        if (e.OldValue is LabelObject oldObject)
        {
            canvas.UpdateObjectElement(oldObject);
        }

        if (e.NewValue is LabelObject newObject)
        {
            canvas.UpdateObjectElement(newObject);
        }
        else
        {
            canvas.RemoveSelectionAdorner();
        }
    }

    private static void OnZoomChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        ((LabelDesignerCanvas)d).Rebuild();
    }

    private static void OnPreviewRowChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var canvas = (LabelDesignerCanvas)d;
        foreach (var item in canvas._objectElements.Keys)
        {
            canvas.UpdateObjectElement(item);
        }
    }

    private static void OnDrawingToolChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var canvas = (LabelDesignerCanvas)d;
        if (e.NewValue is null)
        {
            canvas.CancelDrawingObject();
            return;
        }

        canvas.Focus();
        canvas.SelectedObject = null;
        canvas.RemoveSelectionAdorner();
        canvas.Cursor = Cursors.Cross;
    }

    private static void OnSnapPreferenceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var canvas = (LabelDesignerCanvas)d;
        var enabled = (bool)e.NewValue;
        canvas._snapMenuItem.IsChecked = enabled;
        canvas.InteractionStatusText = enabled
            ? "Snap to objects enabled (hold Alt to bypass)"
            : "Snap to objects disabled";

        try
        {
            canvas._designerPreferencesService.Save(new DesignerPreferences { SnapToObjects = enabled });
        }
        catch (IOException)
        {
            canvas.InteractionStatusText += " — preference could not be saved";
        }
        catch (UnauthorizedAccessException)
        {
            canvas.InteractionStatusText += " — preference could not be saved";
        }
    }

    private void CanvasMouseButtonDown(object sender, MouseButtonEventArgs e)
    {
        Focus();
        if (DrawingTool is ObjectType.Line or ObjectType.Rectangle or ObjectType.Ellipse)
        {
            if (_drawingObject is null)
            {
                BeginSnapDrawing(e.GetPosition(this));
            }
            else
            {
                UpdateSnapDrawing(e.GetPosition(this));
                CompleteSnapDrawing();
            }

            e.Handled = true;
            return;
        }

        if (e.ChangedButton == MouseButton.Left)
        {
            BeginMarqueeSelection(e.GetPosition(this));
            e.Handled = true;
        }
    }

    private void CanvasMouseMove(object sender, MouseEventArgs e)
    {
        if (_isMarqueeSelecting)
        {
            UpdateMarqueeSelection(e.GetPosition(this));
            e.Handled = true;
            return;
        }

        if (_drawingObject is null)
        {
            return;
        }

        UpdateSnapDrawing(e.GetPosition(this));
        e.Handled = true;
    }

    private void CanvasMouseButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (_isMarqueeSelecting)
        {
            CompleteMarqueeSelection();
            e.Handled = true;
            return;
        }

        if (_drawingObject is not null)
        {
            e.Handled = true;
        }
    }

    private void CanvasLostMouseCapture(object sender, MouseEventArgs e)
    {
        // Safety: if mouse capture is lost (Alt+Tab, popup, etc.), clean up drag state
        // to prevent object teleporting when the user re-clicks.
        if (_dragObject is not null)
        {
            // Revert every object in a group drag, not only the object that owns capture.
            foreach (var pair in _groupDragStarts)
            {
                pair.Key.XMm = pair.Value.X;
                pair.Key.YMm = pair.Value.Y;
                if (pair.Key.Type == ObjectType.Line)
                {
                    pair.Key.LineEndXMm = pair.Value.EndX;
                    pair.Key.LineEndYMm = pair.Value.EndY;
                }
            }

            _dragObject = null;
            _groupDragStarts.Clear();
            HideAlignmentGuides();
        }

        if (_isMarqueeSelecting)
        {
            _isMarqueeSelecting = false;
            if (_marqueeElement is not null)
            {
                Children.Remove(_marqueeElement);
                _marqueeElement = null;
            }
        }
    }

    private void CanvasKeyDown(object sender, KeyEventArgs e)
    {
        if (DrawingTool is null)
        {
            // Esc while dragging = cancel drag, revert to start position
            if (e.Key == Key.Escape && _dragObject is not null)
            {
                foreach (var pair in _groupDragStarts)
                {
                    pair.Key.XMm = pair.Value.X;
                    pair.Key.YMm = pair.Value.Y;
                    if (pair.Key.Type == ObjectType.Line)
                    {
                        pair.Key.LineEndXMm = pair.Value.EndX;
                        pair.Key.LineEndYMm = pair.Value.EndY;
                    }
                }

                _dragObject = null;
                _groupDragStarts.Clear();
                HideAlignmentGuides();
                e.Handled = true;
                return;
            }

            if (e.Key == Key.Delete && DeleteSelection())
            {
                e.Handled = true;
                return;
            }

            if (TryMoveSelectionWithArrowKey(e.Key, Keyboard.Modifiers))
            {
                e.Handled = true;
                return;
            }

            if ((Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control && e.Key == Key.C)
            {
                e.Handled = CopySelection();
                return;
            }

            if ((Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control && e.Key == Key.V)
            {
                e.Handled = PasteSelection();
            }

            return;
        }

        if (e.Key == Key.Escape)
        {
            CancelDrawingObject();
            DrawingTool = null;
            e.Handled = true;
            return;
        }

        if (_drawingObject is null)
        {
            return;
        }

        if (e.Key == Key.Enter)
        {
            if (TryApplyTypedDimensions())
            {
                CompleteSnapDrawing();
            }

            e.Handled = true;
            return;
        }

        if (e.Key == Key.Back)
        {
            if (_dimensionBuffer.Length > 0)
            {
                _dimensionBuffer = _dimensionBuffer[..^1];
                UpdateCommandText();
            }

            e.Handled = true;
            return;
        }

        var token = KeyToDimensionToken(e.Key);
        if (token is not null)
        {
            _dimensionBuffer += token;
            UpdateCommandText();
            e.Handled = true;
        }
    }

    private void BeginSnapDrawing(Point dipPoint)
    {
        if (Template is null || DrawingTool is not (ObjectType.Line or ObjectType.Rectangle or ObjectType.Ellipse))
        {
            return;
        }

        _drawingStartMm = SnapToGridMm(dipPoint);
        _lastDrawingEndMm = _drawingStartMm;
        _dimensionBuffer = string.Empty;
        UpdateCommandText();
        _drawingObject = DrawingTool switch
        {
            ObjectType.Line => new LabelObject
            {
                Type = ObjectType.Line,
                Name = "Line",
                XMm = _drawingStartMm.X,
                YMm = _drawingStartMm.Y,
                LineEndXMm = _drawingStartMm.X,
                LineEndYMm = _drawingStartMm.Y,
                WidthMm = 0.5,
                HeightMm = 0.5,
                Style = { BorderThicknessMm = 0.35, OutlineStyle = OutlineStyle.Solid, FillStyle = FillStyle.None }
            },
            ObjectType.Ellipse => new LabelObject
            {
                Type = ObjectType.Ellipse,
                Name = "Ellipse",
                XMm = _drawingStartMm.X,
                YMm = _drawingStartMm.Y,
                WidthMm = 0.5,
                HeightMm = 0.5,
                Style = { FillColor = "#00FFFFFF", BorderThicknessMm = 0.3, OutlineStyle = OutlineStyle.Solid, FillStyle = FillStyle.None }
            },
            _ => new LabelObject
            {
                Type = ObjectType.Rectangle,
                Name = "Rectangle",
                XMm = _drawingStartMm.X,
                YMm = _drawingStartMm.Y,
                WidthMm = 0.5,
                HeightMm = 0.5,
                Style = { FillColor = "#00FFFFFF", BorderThicknessMm = 0.3, OutlineStyle = OutlineStyle.Solid, FillStyle = FillStyle.None }
            }
        };

        _drawingObject.ZIndex = Template.Objects.Count == 0 ? 1 : Template.Objects.Max(item => item.ZIndex) + 1;
        Template.Objects.Add(_drawingObject);
        SelectedObject = _drawingObject;
        CaptureMouse();
        UpdateCommandText();
    }

    private void UpdateSnapDrawing(Point dipPoint)
    {
        if (_drawingObject is null || Template is null)
        {
            return;
        }

        var endMm = SnapToGridMm(dipPoint);
        _lastDrawingEndMm = endMm;
        if (_drawingObject.Type == ObjectType.Line)
        {
            _drawingObject.LineEndXMm = endMm.X;
            _drawingObject.LineEndYMm = endMm.Y;
            _drawingObject.WidthMm = Math.Max(0.5, Math.Abs(endMm.X - _drawingObject.XMm));
            _drawingObject.HeightMm = Math.Max(0.5, Math.Abs(endMm.Y - _drawingObject.YMm));
        }
        else
        {
            _drawingObject.XMm = Math.Min(_drawingStartMm.X, endMm.X);
            _drawingObject.YMm = Math.Min(_drawingStartMm.Y, endMm.Y);
            _drawingObject.WidthMm = Math.Max(0.5, Math.Abs(endMm.X - _drawingStartMm.X));
            _drawingObject.HeightMm = Math.Max(0.5, Math.Abs(endMm.Y - _drawingStartMm.Y));
        }

        UpdateObjectElement(_drawingObject);
    }

    private void CompleteSnapDrawing()
    {
        if (_drawingObject is null)
        {
            return;
        }

        SelectedObject = _drawingObject;
        _drawingObject = null;
        _dimensionBuffer = string.Empty;
        DrawingCommandText = string.Empty;
        DrawingTool = null;
        ReleaseMouseCapture();
        Cursor = Cursors.Arrow;
    }

    private void CancelDrawingObject()
    {
        if (_drawingObject is not null && Template is not null)
        {
            Template.Objects.Remove(_drawingObject);
        }

        _drawingObject = null;
        _dimensionBuffer = string.Empty;
        DrawingCommandText = string.Empty;
        ReleaseMouseCapture();
        Cursor = Cursors.Arrow;
    }

    private void BeginMarqueeSelection(Point start)
    {
        if (Template is null || DrawingTool is not null)
        {
            return;
        }

        SelectedObject = null;
        _selectedObjects.Clear();
        RemoveSelectionAdorner();
        _marqueeStart = start;
        _isMarqueeSelecting = true;
        _marqueeElement = new Border
        {
            Background = new SolidColorBrush(Color.FromArgb(38, 59, 130, 246)),
            BorderBrush = new SolidColorBrush(Color.FromRgb(37, 99, 235)),
            BorderThickness = new Thickness(1),
            IsHitTestVisible = false
        };
        SetLeft(_marqueeElement, start.X);
        SetTop(_marqueeElement, start.Y);
        SetZIndex(_marqueeElement, int.MaxValue);
        Children.Add(_marqueeElement);
        CaptureMouse();
    }

    private void UpdateMarqueeSelection(Point current)
    {
        if (_marqueeElement is null)
        {
            return;
        }

        var rect = CreateRect(_marqueeStart, current);
        SetLeft(_marqueeElement, rect.Left);
        SetTop(_marqueeElement, rect.Top);
        _marqueeElement.Width = rect.Width;
        _marqueeElement.Height = rect.Height;
    }

    private void CompleteMarqueeSelection()
    {
        if (_marqueeElement is null)
        {
            _isMarqueeSelecting = false;
            ReleaseMouseCapture();
            return;
        }

        var selectionRect = new Rect(GetLeft(_marqueeElement), GetTop(_marqueeElement), _marqueeElement.Width, _marqueeElement.Height);
        Children.Remove(_marqueeElement);
        _marqueeElement = null;
        _isMarqueeSelecting = false;
        ReleaseMouseCapture();

        if (selectionRect.Width < 2 && selectionRect.Height < 2)
        {
            _selectedObjects.Clear();
            SelectedObject = null;
            InvalidateVisual();
            return;
        }

        foreach (var pair in _objectElements)
        {
            if (selectionRect.IntersectsWith(GetObjectBounds(pair.Key)))
            {
                _selectedObjects.Add(pair.Key);
            }
        }

        if (_selectedObjects.Count == 0)
        {
            SelectedObject = null;
            InvalidateVisual();
            return;
        }

        SelectedObject = _selectedObjects.OrderByDescending(item => item.ZIndex).First();
        if (_selectedObjects.Count > 1)
        {
            RemoveSelectionAdorner();
        }

        Focus();
        InvalidateVisual();
    }

    private void DrawGroupSelection(DrawingContext dc)
    {
        if (_selectedObjects.Count == 0)
        {
            return;
        }

        var fill = new SolidColorBrush(Color.FromArgb(35, 37, 99, 235));
        var pen = new Pen(new SolidColorBrush(Color.FromRgb(37, 99, 235)), 1.4)
        {
            DashStyle = DashStyles.Dash
        };

        foreach (var item in _selectedObjects)
        {
            dc.DrawRectangle(fill, pen, InflateRect(GetObjectBounds(item), 3));
        }
    }

    private bool DeleteSelection()
    {
        if (Template is null || _selectedObjects.Count == 0)
        {
            return false;
        }

        var selected = _selectedObjects.ToArray();
        foreach (var item in selected)
        {
            Template.Objects.Remove(item);
        }

        _selectedObjects.Clear();
        SelectedObject = null;
        RemoveSelectionAdorner();
        InvalidateVisual();
        return true;
    }

    private bool CopySelection()
    {
        var items = _selectedObjects.Count > 0
            ? _selectedObjects.OrderBy(item => item.ZIndex).ToArray()
            : SelectedObject is null ? Array.Empty<LabelObject>() : new[] { SelectedObject };
        if (items.Length == 0)
        {
            return false;
        }

        _clipboardObjects.Clear();
        _clipboardObjects.AddRange(items.Select(CloneObject));
        _pasteCount = 0;
        return true;
    }

    private bool PasteSelection()
    {
        if (Template is null || _clipboardObjects.Count == 0)
        {
            return false;
        }

        _pasteCount++;
        var offset = Math.Min(20, 3 * _pasteCount);
        var pasted = _clipboardObjects.Select(item => CloneObject(item)).ToArray();
        var bounds = GetObjectsBoundsMm(pasted);
        var deltaX = Math.Max(-bounds.Left, Math.Min(Template.WidthMm - bounds.Right, offset));
        var deltaY = Math.Max(-bounds.Top, Math.Min(Template.HeightMm - bounds.Bottom, offset));
        var nextZ = Template.Objects.Count == 0 ? 1 : Template.Objects.Max(item => item.ZIndex) + 1;

        _selectedObjects.Clear();
        foreach (var item in pasted)
        {
            item.Id = Guid.NewGuid().ToString("N");
            item.Name = CreateCopyName(item.Name);
            item.ZIndex = nextZ++;
            MoveObject(item, deltaX, deltaY);
            Template.Objects.Add(item);
            _selectedObjects.Add(item);
        }

        SelectedObject = pasted.LastOrDefault();
        if (_selectedObjects.Count > 1)
        {
            RemoveSelectionAdorner();
        }
        else if (SelectedObject is not null)
        {
            ShowSelectionAdorner(SelectedObject);
        }

        InvalidateVisual();
        return true;
    }

    private static LabelObject CloneObject(LabelObject source)
    {
        return new LabelObject
        {
            Id = Guid.NewGuid().ToString("N"),
            Type = source.Type,
            Name = source.Name,
            XMm = source.XMm,
            YMm = source.YMm,
            WidthMm = source.WidthMm,
            HeightMm = source.HeightMm,
            LineEndXMm = source.LineEndXMm,
            LineEndYMm = source.LineEndYMm,
            Rotation = source.Rotation,
            ZIndex = source.ZIndex,
            IsLocked = source.IsLocked,
            IsVisible = source.IsVisible,
            BindingExpression = source.BindingExpression,
            Text = source.Text,
            BarcodeSymbology = source.BarcodeSymbology,
            Style = CloneStyle(source.Style)
        };
    }

    private static ObjectStyle CloneStyle(ObjectStyle source)
    {
        return new ObjectStyle
        {
            FontFamily = source.FontFamily,
            FontSizePt = source.FontSizePt,
            Bold = source.Bold,
            Italic = source.Italic,
            Underline = source.Underline,
            Alignment = source.Alignment,
            BorderThicknessMm = source.BorderThicknessMm,
            OutlineStyle = source.OutlineStyle,
            FillStyle = source.FillStyle,
            CornerRadiusMm = source.CornerRadiusMm,
            FillColor = source.FillColor,
            StrokeColor = source.StrokeColor
        };
    }

    private static string CreateCopyName(string name)
    {
        return string.IsNullOrWhiteSpace(name) ? "Copy" : $"{name} Copy";
    }

    private static void MoveObject(LabelObject item, double deltaXMm, double deltaYMm)
    {
        item.XMm += deltaXMm;
        item.YMm += deltaYMm;
        if (item.Type == ObjectType.Line)
        {
            item.LineEndXMm += deltaXMm;
            item.LineEndYMm += deltaYMm;
        }
    }

    private static Rect GetObjectsBoundsMm(IEnumerable<LabelObject> items)
    {
        var left = double.MaxValue;
        var top = double.MaxValue;
        var right = double.MinValue;
        var bottom = double.MinValue;
        foreach (var item in items)
        {
            var bounds = GetObjectBoundsMm(item);
            left = Math.Min(left, bounds.Left);
            top = Math.Min(top, bounds.Top);
            right = Math.Max(right, bounds.Right);
            bottom = Math.Max(bottom, bounds.Bottom);
        }

        return new Rect(new Point(left, top), new Point(right, bottom));
    }

    private static Rect GetObjectBoundsMm(LabelObject item)
    {
        if (item.Type == ObjectType.Line)
        {
            var endXMm = item.LineEndXMm == 0 && item.LineEndYMm == 0 ? item.XMm + item.WidthMm : item.LineEndXMm;
            var endYMm = item.LineEndXMm == 0 && item.LineEndYMm == 0 ? item.YMm + item.HeightMm : item.LineEndYMm;
            return new Rect(
                new Point(Math.Min(item.XMm, endXMm), Math.Min(item.YMm, endYMm)),
                new Point(Math.Max(item.XMm, endXMm), Math.Max(item.YMm, endYMm)));
        }

        return new Rect(item.XMm, item.YMm, item.WidthMm, item.HeightMm);
    }

    private bool TryMoveSelectionWithArrowKey(Key key, ModifierKeys modifiers)
    {
        if (Template is null || _selectedObjects.Count == 0)
        {
            return false;
        }

        var stepMm = (modifiers & ModifierKeys.Shift) == ModifierKeys.Shift ? 10 : 1;
        var deltaX = 0d;
        var deltaY = 0d;
        switch (key)
        {
            case Key.Left:
                deltaX = -stepMm;
                break;
            case Key.Right:
                deltaX = stepMm;
                break;
            case Key.Up:
                deltaY = -stepMm;
                break;
            case Key.Down:
                deltaY = stepMm;
                break;
            default:
                return false;
        }

        CaptureGroupDragStarts();
        MoveSelectedGroup(deltaX, deltaY);
        _groupDragStarts.Clear();
        InvalidateVisual();
        InteractionStatusText = _selectedObjects.Count == 1
            ? $"{_selectedObjects.First().Name}: X {_selectedObjects.First().XMm:0.##} mm, Y {_selectedObjects.First().YMm:0.##} mm (nudge {stepMm:0.##} mm)"
            : $"Moved {_selectedObjects.Count} objects by {stepMm:0.##} mm";
        return true;
    }

    private void CaptureGroupDragStarts()
    {
        _groupDragStarts.Clear();
        foreach (var selected in _selectedObjects.Where(item => !item.IsLocked))
        {
            var endXMm = selected.LineEndXMm == 0 && selected.LineEndYMm == 0 ? selected.XMm + selected.WidthMm : selected.LineEndXMm;
            var endYMm = selected.LineEndXMm == 0 && selected.LineEndYMm == 0 ? selected.YMm + selected.HeightMm : selected.LineEndYMm;
            _groupDragStarts[selected] = (selected.XMm, selected.YMm, endXMm, endYMm);
        }
    }

    private void MoveSelectedGroup(double deltaXMm, double deltaYMm)
    {
        if (Template is null || _groupDragStarts.Count == 0)
        {
            return;
        }

        var bounds = GetGroupBoundsMm();
        var clampedDeltaX = Math.Max(-bounds.Left, Math.Min(Template.WidthMm - bounds.Right, deltaXMm));
        var clampedDeltaY = Math.Max(-bounds.Top, Math.Min(Template.HeightMm - bounds.Bottom, deltaYMm));

        foreach (var pair in _groupDragStarts)
        {
            var item = pair.Key;
            var start = pair.Value;
            item.XMm = start.X + clampedDeltaX;
            item.YMm = start.Y + clampedDeltaY;
            if (item.Type == ObjectType.Line)
            {
                item.LineEndXMm = start.EndX + clampedDeltaX;
                item.LineEndYMm = start.EndY + clampedDeltaY;
            }

            UpdateObjectElement(item);
        }
    }

    private Rect GetGroupBoundsMm()
    {
        var left = double.MaxValue;
        var top = double.MaxValue;
        var right = double.MinValue;
        var bottom = double.MinValue;

        foreach (var pair in _groupDragStarts)
        {
            var item = pair.Key;
            var start = pair.Value;
            if (item.Type == ObjectType.Line)
            {
                left = Math.Min(left, Math.Min(start.X, start.EndX));
                top = Math.Min(top, Math.Min(start.Y, start.EndY));
                right = Math.Max(right, Math.Max(start.X, start.EndX));
                bottom = Math.Max(bottom, Math.Max(start.Y, start.EndY));
            }
            else
            {
                left = Math.Min(left, start.X);
                top = Math.Min(top, start.Y);
                right = Math.Max(right, start.X + item.WidthMm);
                bottom = Math.Max(bottom, start.Y + item.HeightMm);
            }
        }

        return new Rect(new Point(left, top), new Point(right, bottom));
    }

    private Rect GetObjectBounds(LabelObject item)
    {
        if (item.Type == ObjectType.Line)
        {
            var endXMm = item.LineEndXMm == 0 && item.LineEndYMm == 0 ? item.XMm + item.WidthMm : item.LineEndXMm;
            var endYMm = item.LineEndXMm == 0 && item.LineEndYMm == 0 ? item.YMm + item.HeightMm : item.LineEndYMm;
            var left = MmToDip(Math.Min(item.XMm, endXMm));
            var top = MmToDip(Math.Min(item.YMm, endYMm));
            var right = MmToDip(Math.Max(item.XMm, endXMm));
            var bottom = MmToDip(Math.Max(item.YMm, endYMm));
            return new Rect(new Point(left, top), new Point(right, bottom));
        }

        return new Rect(MmToDip(item.XMm), MmToDip(item.YMm), MmToDip(item.WidthMm), MmToDip(item.HeightMm));
    }

    private static Rect InflateRect(Rect rect, double amount)
    {
        rect.Inflate(amount, amount);
        return rect;
    }

    private static Rect CreateRect(Point start, Point end)
    {
        return new Rect(
            Math.Min(start.X, end.X),
            Math.Min(start.Y, end.Y),
            Math.Abs(end.X - start.X),
            Math.Abs(end.Y - start.Y));
    }

    private bool TryApplyTypedDimensions()
    {
        if (_drawingObject is null || Template is null || string.IsNullOrWhiteSpace(_dimensionBuffer))
        {
            return _drawingObject is not null;
        }

        var parts = _dimensionBuffer.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (!double.TryParse(parts[0], out var first) || first <= 0)
        {
            return false;
        }

        if (_drawingObject.Type == ObjectType.Line)
        {
            var dx = _lastDrawingEndMm.X - _drawingStartMm.X;
            var dy = _lastDrawingEndMm.Y - _drawingStartMm.Y;
            var length = Math.Sqrt(dx * dx + dy * dy);
            if (length <= 0)
            {
                dx = 1;
                dy = 0;
                length = 1;
            }

            var end = new Point(
                _drawingStartMm.X + dx / length * first,
                _drawingStartMm.Y + dy / length * first);
            ApplyLineEnd(ClampMm(end));
            return true;
        }

        var second = parts.Length > 1 && double.TryParse(parts[1], out var parsedSecond) && parsedSecond > 0
            ? parsedSecond
            : first;
        var directionX = _lastDrawingEndMm.X < _drawingStartMm.X ? -1 : 1;
        var directionY = _lastDrawingEndMm.Y < _drawingStartMm.Y ? -1 : 1;
        ApplyBoxEnd(ClampMm(new Point(_drawingStartMm.X + first * directionX, _drawingStartMm.Y + second * directionY)));
        return true;
    }

    private void ApplyLineEnd(Point endMm)
    {
        if (_drawingObject is null)
        {
            return;
        }

        _drawingObject.LineEndXMm = endMm.X;
        _drawingObject.LineEndYMm = endMm.Y;
        _drawingObject.WidthMm = Math.Max(0.5, Math.Abs(endMm.X - _drawingObject.XMm));
        _drawingObject.HeightMm = Math.Max(0.5, Math.Abs(endMm.Y - _drawingObject.YMm));
        UpdateObjectElement(_drawingObject);
    }

    private void ApplyBoxEnd(Point endMm)
    {
        if (_drawingObject is null)
        {
            return;
        }

        _drawingObject.XMm = Math.Min(_drawingStartMm.X, endMm.X);
        _drawingObject.YMm = Math.Min(_drawingStartMm.Y, endMm.Y);
        _drawingObject.WidthMm = Math.Max(0.5, Math.Abs(endMm.X - _drawingStartMm.X));
        _drawingObject.HeightMm = Math.Max(0.5, Math.Abs(endMm.Y - _drawingStartMm.Y));
        UpdateObjectElement(_drawingObject);
    }

    private Point ClampMm(Point point)
    {
        return Template is null
            ? point
            : new Point(Math.Max(0, Math.Min(Template.WidthMm, point.X)), Math.Max(0, Math.Min(Template.HeightMm, point.Y)));
    }

    private void UpdateCommandText()
    {
        if (_drawingObject is null)
        {
            DrawingCommandText = DrawingTool switch
            {
                ObjectType.Line => "Line: specify first point",
                ObjectType.Rectangle => "Rectangle: specify first corner",
                ObjectType.Ellipse => "Ellipse/Circle: specify first corner",
                _ => string.Empty
            };
            return;
        }

        DrawingCommandText = string.IsNullOrWhiteSpace(_dimensionBuffer)
            ? _drawingObject.Type switch
            {
                ObjectType.Line => "Line: specify next point or type length, Enter",
                ObjectType.Rectangle => "Rectangle: specify opposite corner or type width,height, Enter",
                ObjectType.Ellipse => "Ellipse/Circle: specify opposite corner or type width,height, Enter",
                _ => string.Empty
            }
            : $"Size: {_dimensionBuffer}";
    }

    private static string? KeyToDimensionToken(Key key)
    {
        return key switch
        {
            >= Key.D0 and <= Key.D9 => ((int)(key - Key.D0)).ToString(),
            >= Key.NumPad0 and <= Key.NumPad9 => ((int)(key - Key.NumPad0)).ToString(),
            Key.OemComma or Key.Decimal => ",",
            Key.OemPeriod => ".",
            _ => null
        };
    }

    private Point SnapToGridMm(Point dipPoint)
    {
        if (Template is null)
        {
            return new Point();
        }

        var xMm = Math.Round(DipToMm(dipPoint.X));
        var yMm = Math.Round(DipToMm(dipPoint.Y));
        return new Point(
            Math.Max(0, Math.Min(Template.WidthMm, xMm)),
            Math.Max(0, Math.Min(Template.HeightMm, yMm)));
    }

    private string GetDisplayText(LabelObject item)
    {
        if (item.Type is not (ObjectType.Text or ObjectType.TextBox) || string.IsNullOrWhiteSpace(item.BindingExpression))
        {
            return item.Text;
        }

        return ResolveExpression(item.BindingExpression, PreviewRow);
    }

    private bool IsTextBoxOverflowing(LabelObject item, double widthDip, double heightDip)
    {
        return TextBoxOverflowDetector.IsOverflowing(
            item,
            GetDisplayText(item),
            widthDip,
            heightDip);
    }

    private DrawingVisual CreateTextVisual(LabelObject item, double widthDip, double heightDip)
    {
        var visual = new DrawingVisual();
        using var dc = visual.RenderOpen();
        const double previewPixelsPerDip = 1.0;
        var renderScale = Math.Max(0.01, Zoom);
        var previewWidthDip = Math.Max(1, widthDip / renderScale);
        var previewHeightDip = Math.Max(1, heightDip / renderScale);
        var displayText = string.IsNullOrEmpty(GetDisplayText(item)) ? " " : GetDisplayText(item);
        var text = TextBoxOverflowDetector.CreateFormattedText(
            item,
            displayText,
            ParseBrush(item.Style.StrokeColor, Brushes.Black),
            previewPixelsPerDip);
        text.MaxTextWidth = previewWidthDip;
        text.MaxTextHeight = previewHeightDip;
        var x = 2.0;
        var y = Math.Max(0, (previewHeightDip - text.Height) / 2.0);
        dc.PushTransform(new ScaleTransform(renderScale, renderScale));
        dc.DrawText(text, new Point(x, y));
        dc.Pop();
        return visual;
    }

    private DrawingVisual CreateTextBoxVisual(LabelObject item, double widthDip, double heightDip)
    {
        var visual = new DrawingVisual();
        using var dc = visual.RenderOpen();
        const double previewPixelsPerDip = 1.0;
        var renderScale = Math.Max(0.01, Zoom);
        var previewWidthDip = Math.Max(1, widthDip / renderScale);
        var previewHeightDip = Math.Max(1, heightDip / renderScale);
        var wrapped = TextBoxOverflowDetector.WrapTextToBox(item, GetDisplayText(item), previewWidthDip, previewPixelsPerDip);
        var text = TextBoxOverflowDetector.CreateFormattedText(
            item,
            wrapped,
            ParseBrush(item.Style.StrokeColor, Brushes.Black),
            previewPixelsPerDip);
        text.MaxTextWidth = previewWidthDip;
        text.MaxTextHeight = previewHeightDip;
        dc.PushTransform(new ScaleTransform(renderScale, renderScale));
        dc.PushClip(new RectangleGeometry(new Rect(0, 0, previewWidthDip, previewHeightDip)));
        dc.DrawText(text, new Point(0, 0));
        dc.Pop();
        dc.Pop();
        return visual;
    }

    private string? GetObjectError(LabelObject item)
    {
        return item.Type switch
        {
            ObjectType.TextBox when IsTextBoxOverflowing(item, MmConverter.MmToDip(item.WidthMm), MmConverter.MmToDip(item.HeightMm)) =>
                "Text exceeds this text box. Increase the object size or reduce text/font size.",
            ObjectType.BarcodeCode128 or ObjectType.QRCode or ObjectType.DataMatrix => GetBarcodeError(item),
            _ => null
        };
    }

    private string? GetBarcodeError(LabelObject item)
    {
        var data = ResolveObjectData(item);
        var type = item.Type switch
        {
            ObjectType.QRCode => BarcodeType.QRCode,
            ObjectType.DataMatrix => BarcodeType.DataMatrix,
            _ => BarcodeTypeMapper.ToRendererType(item.BarcodeSymbology)
        };

        if (!_barcodeRenderer.ValidateData(data, type))
        {
            return $"Invalid {type} data.";
        }

        try
        {
            _barcodeRenderer.RenderBarcode(data, type, item.WidthMm, item.HeightMm, item.QrDpi, CreateBarcodeRenderOptions(item));
            return null;
        }
        catch (Exception ex) when (ex is not OutOfMemoryException)
        {
            return $"{type} cannot be rendered: {ex.Message}";
        }
    }

    private void FitTextObjectToContent(LabelObject item, ref double width, ref double height)
    {
        // VISUAL-ONLY fit: compute the visual size for rendering without mutating WidthMm/HeightMm.
        // The model keeps its user-set dimensions. If text overflows, it renders larger (Text) or clips (TextBox).
        if (Template is null)
        {
            return;
        }

        var displayText = string.IsNullOrEmpty(GetDisplayText(item)) ? " " : GetDisplayText(item);
        var text = TextBoxOverflowDetector.CreateFormattedText(
            item,
            displayText,
            Brushes.Black,
            1.0);

        var minSizeMm = 1.0;
        var fitWidthMm = MmConverter.DipToMm(Math.Ceiling(text.WidthIncludingTrailingWhitespace))
            + MmConverter.DipToMm(4)   // 2 left + 2 right DIP padding (matches CreateTextVisual x=2)
            + 0.6;                     // extra mm padding
        var fitHeightMm = MmConverter.DipToMm(Math.Ceiling(text.Height)) + 0.6;

        fitWidthMm = Math.Max(minSizeMm, fitWidthMm);
        fitHeightMm = Math.Max(minSizeMm, fitHeightMm);

        // Do NOT clamp to template bounds or mutate item.WidthMm/HeightMm.
        // Text can render larger than the model size — this is intentional for designer preview.
        width = MmToDip(fitWidthMm);
        height = MmToDip(fitHeightMm);
    }

    private ImageSource? CreateBarcodeImageSource(LabelObject item)
    {
        var data = ResolveObjectData(item);
        var type = item.Type switch
        {
            ObjectType.QRCode => BarcodeType.QRCode,
            ObjectType.DataMatrix => BarcodeType.DataMatrix,
            _ => BarcodeTypeMapper.ToRendererType(item.BarcodeSymbology)
        };

        if (!_barcodeRenderer.ValidateData(data, type))
        {
            return null;
        }

        try
        {
            var pixels = _barcodeRenderer.RenderBarcode(data, type, item.WidthMm, item.HeightMm, item.QrDpi, CreateBarcodeRenderOptions(item));
            var source = BitmapSource.Create(
                pixels.WidthPixels,
                pixels.HeightPixels,
                item.QrDpi,
                item.QrDpi,
                PixelFormats.Bgra32,
                null,
                pixels.BgraPixels,
                pixels.Stride);
            source.Freeze();

            // Return barcode only — text below barcode is handled by Linked Text objects.
            // Inline text compositing was removed because it caused misalignment between
            // the barcode and text in print/preview (text appeared too far from the bars).
            return source;
        }
        catch (ArgumentException)
        {
            return null;
        }
        catch (InvalidOperationException)
        {
            return null;
        }
        catch (Exception ex) when (ex is not OutOfMemoryException)
        {
            return null;
        }
    }

    private static BarcodeRenderOptions CreateBarcodeRenderOptions(LabelObject item)
    {
        return new BarcodeRenderOptions
        {
            ErrorCorrection = item.QrErrorCorrection.ToString(),
            QuietZoneModules = item.QrQuietZoneModules
        };
    }

    private bool TryApplyMatrixAutoSize(LabelObject item)
    {
        if (!IsMatrixBarcode(item) || Template is null)
        {
            return false;
        }

        var fitSizeMm = item.QrSizingMode == QrSizingMode.FixedVersionAndModuleSize
            ? QrAutoSizeHelper.CalculateFixedSizeMm(item.QrFixedVersion, item.QrModuleSizePx, item.QrQuietZoneModules, item.QrDpi, GetAvailableQrSizeMm(item))
            : QrAutoSizeHelper.CalculateRequiredSizeMm(
                ResolveObjectData(item),
                item.WidthMm,
                item.HeightMm,
                item.QrErrorCorrection,
                item.QrModuleSizePx,
                item.QrQuietZoneModules,
                item.QrDpi,
                maxSizeMm: GetAvailableQrSizeMm(item));
        if (fitSizeMm is null)
        {
            return false;
        }

        if (Math.Abs(item.WidthMm - fitSizeMm.Value) <= 0.05 && Math.Abs(item.HeightMm - fitSizeMm.Value) <= 0.05)
        {
            return false;
        }

        _matrixAutoSizingObjects.Add(item);
        try
        {
            item.WidthMm = fitSizeMm.Value;
            item.HeightMm = fitSizeMm.Value;
        }
        finally
        {
            _matrixAutoSizingObjects.Remove(item);
        }

        return true;
    }

    private string ResolveObjectData(LabelObject item)
    {
        if (string.IsNullOrWhiteSpace(item.BindingExpression))
        {
            return item.Text;
        }

        return ResolveExpression(item.BindingExpression, PreviewRow);
    }

    private double GetAvailableQrSizeMm(LabelObject item)
    {
        if (Template is null)
        {
            return Math.Max(1, Math.Max(item.WidthMm, item.HeightMm));
        }

        var availableWidthMm = Template.WidthMm - item.XMm;
        var availableHeightMm = Template.HeightMm - item.YMm;
        return Math.Max(1, Math.Min(availableWidthMm, availableHeightMm));
    }

    private static string ResolveExpression(string expression, IReadOnlyDictionary<string, string>? row)
    {
        if (row is null)
        {
            return expression;
        }

        return FormulaBindingEvaluator.LooksLikeFormula(expression)
            ? FormulaBindingEvaluator.Evaluate(expression, row).Value
            : BindingExpressionEvaluator.Evaluate(expression, row);
    }

    private void ShowSelectionAdorner(LabelObject item)
    {
        if (!_objectElements.TryGetValue(item, out var element) || item.IsLocked)
        {
            RemoveSelectionAdorner();
            return;
        }

        if (ReferenceEquals(_adornedObject, item) && _selectionAdorner is not null)
        {
            _selectionAdorner.InvalidateMeasure();
            _selectionAdorner.InvalidateArrange();
            _selectionAdorner.InvalidateVisual();
            return;
        }

        RemoveSelectionAdorner();
        var layer = AdornerLayer.GetAdornerLayer(element);
        if (layer is null)
        {
            return;
        }

        _selectionAdorner = new SelectionResizeAdorner(element);
        _selectionAdorner.ResizeRequested += (_, delta) => ResizeSelectedObject(item, delta);
        _adornedObject = item;
        layer.Add(_selectionAdorner);
    }

    private void RemoveSelectionAdorner()
    {
        if (_selectionAdorner is null || _adornedObject is null)
        {
            _selectionAdorner = null;
            _adornedObject = null;
            return;
        }

        if (_objectElements.TryGetValue(_adornedObject, out var element))
        {
            AdornerLayer.GetAdornerLayer(element)?.Remove(_selectionAdorner);
        }

        _selectionAdorner = null;
        _adornedObject = null;
    }

    private void ResizeSelectedObject(LabelObject item, ResizeDelta delta)
    {
        if (Template is null || item.IsLocked)
        {
            return;
        }

        var deltaXMm = DipToMm(delta.DeltaX);
        var deltaYMm = DipToMm(delta.DeltaY);
        var deltaWidthMm = DipToMm(delta.DeltaWidth);
        var deltaHeightMm = DipToMm(delta.DeltaHeight);

        const double minSizeMm = 1;
        var proposedX = item.XMm + deltaXMm;
        var proposedY = item.YMm + deltaYMm;
        var proposedWidth = item.WidthMm + deltaWidthMm;
        var proposedHeight = item.HeightMm + deltaHeightMm;

        var newWidth = Math.Max(minSizeMm, proposedWidth);
        var newHeight = Math.Max(minSizeMm, proposedHeight);
        var newX = deltaXMm > 0 && proposedWidth < minSizeMm
            ? item.XMm + item.WidthMm - minSizeMm
            : proposedX;
        var newY = deltaYMm > 0 && proposedHeight < minSizeMm
            ? item.YMm + item.HeightMm - minSizeMm
            : proposedY;

        newX = Math.Max(0, newX);
        newY = Math.Max(0, newY);

        if (newX + newWidth > Template.WidthMm)
        {
            newWidth = Template.WidthMm - newX;
        }

        if (newY + newHeight > Template.HeightMm)
        {
            newHeight = Template.HeightMm - newY;
        }

        item.XMm = newX;
        item.YMm = newY;
        item.WidthMm = newWidth;
        item.HeightMm = newHeight;
        UpdateObjectElement(item);
        _selectionAdorner?.InvalidateArrange();
        _selectionAdorner?.InvalidateVisual();
    }

    // ==================== Alignment Guide System ====================

    private readonly record struct AlignmentSnapResult(double? SnapX, double? SnapY, List<double> GuideXPositions, List<double> GuideYPositions);

    private AlignmentSnapResult ComputeAlignmentSnap(LabelObject dragged, double proposedXMm, double proposedYMm)
    {
        if (Template is null)
        {
            return new AlignmentSnapResult(null, null, new List<double>(), new List<double>());
        }

        var snapX = (double?)null;
        var snapY = (double?)null;
        var guideXPositions = new List<double>();
        var guideYPositions = new List<double>();
        var bestDistX = double.MaxValue;
        var bestDistY = double.MaxValue;

        // Dragged object edges and center
        var dragLeft = proposedXMm;
        var dragRight = proposedXMm + dragged.WidthMm;
        var dragCenterX = proposedXMm + dragged.WidthMm / 2.0;
        var dragTop = proposedYMm;
        var dragBottom = proposedYMm + dragged.HeightMm;
        var dragCenterY = proposedYMm + dragged.HeightMm / 2.0;

        foreach (var other in Template.Objects)
        {
            if (ReferenceEquals(other, dragged) || !other.IsVisible)
            {
                continue;
            }

            double otherLeft, otherRight, otherTop, otherBottom;
            if (other.Type == ObjectType.Line)
            {
                var endX = other.LineEndXMm == 0 && other.LineEndYMm == 0 ? other.XMm + other.WidthMm : other.LineEndXMm;
                var endY = other.LineEndXMm == 0 && other.LineEndYMm == 0 ? other.YMm + other.HeightMm : other.LineEndYMm;
                otherLeft = Math.Min(other.XMm, endX);
                otherRight = Math.Max(other.XMm, endX);
                otherTop = Math.Min(other.YMm, endY);
                otherBottom = Math.Max(other.YMm, endY);
            }
            else
            {
                otherLeft = other.XMm;
                otherRight = other.XMm + other.WidthMm;
                otherTop = other.YMm;
                otherBottom = other.YMm + other.HeightMm;
            }

            var otherCenterX = (otherLeft + otherRight) / 2.0;
            var otherCenterY = (otherTop + otherBottom) / 2.0;

            // Check X alignment: compare 9 edge/center pairs
            CheckSnap(dragLeft, otherLeft, SnapThresholdMm, ref bestDistX, ref snapX, guideXPositions, otherLeft);
            CheckSnap(dragLeft, otherRight, SnapThresholdMm, ref bestDistX, ref snapX, guideXPositions, otherRight);
            CheckSnap(dragLeft, otherCenterX, SnapThresholdMm, ref bestDistX, ref snapX, guideXPositions, otherCenterX);
            CheckSnap(dragRight, otherLeft, SnapThresholdMm, ref bestDistX, ref snapX, guideXPositions, otherLeft);
            CheckSnap(dragRight, otherRight, SnapThresholdMm, ref bestDistX, ref snapX, guideXPositions, otherRight);
            CheckSnap(dragRight, otherCenterX, SnapThresholdMm, ref bestDistX, ref snapX, guideXPositions, otherCenterX);
            CheckSnap(dragCenterX, otherLeft, SnapThresholdMm, ref bestDistX, ref snapX, guideXPositions, otherLeft);
            CheckSnap(dragCenterX, otherRight, SnapThresholdMm, ref bestDistX, ref snapX, guideXPositions, otherRight);
            CheckSnap(dragCenterX, otherCenterX, SnapThresholdMm, ref bestDistX, ref snapX, guideXPositions, otherCenterX);

            // Check Y alignment
            CheckSnap(dragTop, otherTop, SnapThresholdMm, ref bestDistY, ref snapY, guideYPositions, otherTop);
            CheckSnap(dragTop, otherBottom, SnapThresholdMm, ref bestDistY, ref snapY, guideYPositions, otherBottom);
            CheckSnap(dragTop, otherCenterY, SnapThresholdMm, ref bestDistY, ref snapY, guideYPositions, otherCenterY);
            CheckSnap(dragBottom, otherTop, SnapThresholdMm, ref bestDistY, ref snapY, guideYPositions, otherTop);
            CheckSnap(dragBottom, otherBottom, SnapThresholdMm, ref bestDistY, ref snapY, guideYPositions, otherBottom);
            CheckSnap(dragBottom, otherCenterY, SnapThresholdMm, ref bestDistY, ref snapY, guideYPositions, otherCenterY);
            CheckSnap(dragCenterY, otherTop, SnapThresholdMm, ref bestDistY, ref snapY, guideYPositions, otherTop);
            CheckSnap(dragCenterY, otherBottom, SnapThresholdMm, ref bestDistY, ref snapY, guideYPositions, otherBottom);
            CheckSnap(dragCenterY, otherCenterY, SnapThresholdMm, ref bestDistY, ref snapY, guideYPositions, otherCenterY);
        }

        // Also snap to canvas center lines
        var canvasCenterX = Template.WidthMm / 2.0;
        var canvasCenterY = Template.HeightMm / 2.0;
        CheckSnap(dragCenterX, canvasCenterX, SnapThresholdMm, ref bestDistX, ref snapX, guideXPositions, canvasCenterX);
        CheckSnap(dragCenterY, canvasCenterY, SnapThresholdMm, ref bestDistY, ref snapY, guideYPositions, canvasCenterY);

        // Convert snap delta to final position
        double? finalSnapX = null;
        if (snapX is not null)
        {
            finalSnapX = proposedXMm + snapX.Value;
        }
        double? finalSnapY = null;
        if (snapY is not null)
        {
            finalSnapY = proposedYMm + snapY.Value;
        }

        return new AlignmentSnapResult(finalSnapX, finalSnapY, guideXPositions, guideYPositions);
    }

    private static void CheckSnap(double dragEdge, double otherEdge, double threshold,
        ref double bestDist, ref double? bestDelta, List<double> guidePositions, double guidePosition)
    {
        var dist = Math.Abs(dragEdge - otherEdge);
        if (dist < threshold && dist < bestDist)
        {
            bestDist = dist;
            bestDelta = otherEdge - dragEdge;
            guidePositions.Clear();
            guidePositions.Add(guidePosition);
        }
        else if (dist < threshold && Math.Abs(dist - bestDist) < 0.01)
        {
            guidePositions.Add(guidePosition);
        }
    }

    private void ShowAlignmentGuides(AlignmentSnapResult snap)
    {
        if (Template is null)
        {
            return;
        }

        var labelHeightDip = MmToDip(Template.HeightMm);
        var labelWidthDip = MmToDip(Template.WidthMm);

        // Vertical guide lines (for X snaps)
        if (snap.SnapX is not null && snap.GuideXPositions.Count > 0)
        {
            if (_guideVertical is null)
            {
                _guideVertical = new Line
                {
                    Stroke = new SolidColorBrush(Color.FromRgb(37, 99, 235)),
                    StrokeThickness = 1.0,
                    StrokeDashArray = new DoubleCollection { 3, 2 },
                    IsHitTestVisible = false
                };
                SetZIndex(_guideVertical, int.MaxValue - 1);
                Children.Add(_guideVertical);
            }

            var guideXDip = MmToDip(snap.GuideXPositions[0]);
            _guideVertical.X1 = guideXDip;
            _guideVertical.Y1 = 0;
            _guideVertical.X2 = guideXDip;
            _guideVertical.Y2 = labelHeightDip;
            _guideVertical.Visibility = Visibility.Visible;
        }
        else
        {
            if (_guideVertical is not null)
            {
                _guideVertical.Visibility = Visibility.Collapsed;
            }
        }

        // Horizontal guide lines (for Y snaps)
        if (snap.SnapY is not null && snap.GuideYPositions.Count > 0)
        {
            if (_guideHorizontal is null)
            {
                _guideHorizontal = new Line
                {
                    Stroke = new SolidColorBrush(Color.FromRgb(37, 99, 235)),
                    StrokeThickness = 1.0,
                    StrokeDashArray = new DoubleCollection { 3, 2 },
                    IsHitTestVisible = false
                };
                SetZIndex(_guideHorizontal, int.MaxValue - 1);
                Children.Add(_guideHorizontal);
            }

            var guideYDip = MmToDip(snap.GuideYPositions[0]);
            _guideHorizontal.X1 = 0;
            _guideHorizontal.Y1 = guideYDip;
            _guideHorizontal.X2 = labelWidthDip;
            _guideHorizontal.Y2 = guideYDip;
            _guideHorizontal.Visibility = Visibility.Visible;
        }
        else
        {
            if (_guideHorizontal is not null)
            {
                _guideHorizontal.Visibility = Visibility.Collapsed;
            }
        }
    }

    private void HideAlignmentGuides()
    {
        if (_guideVertical is not null)
        {
            _guideVertical.Visibility = Visibility.Collapsed;
        }
        if (_guideHorizontal is not null)
        {
            _guideHorizontal.Visibility = Visibility.Collapsed;
        }
    }

    // ==================== End Alignment Guide System ====================

    private sealed class StrokeHitTestRectangleElement : Grid
    {
        private readonly Border _topHitZone = CreateHitZone();
        private readonly Border _rightHitZone = CreateHitZone();
        private readonly Border _bottomHitZone = CreateHitZone();
        private readonly Border _leftHitZone = CreateHitZone();

        public StrokeHitTestRectangleElement()
        {
            Visual.IsHitTestVisible = false;
            Children.Add(Visual);
            Children.Add(_topHitZone);
            Children.Add(_rightHitZone);
            Children.Add(_bottomHitZone);
            Children.Add(_leftHitZone);
        }

        public System.Windows.Shapes.Rectangle Visual { get; } = new();

        public void UpdateHitZones(double width, double height, double strokeThickness)
        {
            Visual.Width = width;
            Visual.Height = height;

            var tolerance = Math.Max(strokeThickness, 6);
            _topHitZone.Height = tolerance;
            _topHitZone.Width = width;
            _topHitZone.HorizontalAlignment = HorizontalAlignment.Stretch;
            _topHitZone.VerticalAlignment = VerticalAlignment.Top;

            _bottomHitZone.Height = tolerance;
            _bottomHitZone.Width = width;
            _bottomHitZone.HorizontalAlignment = HorizontalAlignment.Stretch;
            _bottomHitZone.VerticalAlignment = VerticalAlignment.Bottom;

            _leftHitZone.Width = tolerance;
            _leftHitZone.Height = height;
            _leftHitZone.HorizontalAlignment = HorizontalAlignment.Left;
            _leftHitZone.VerticalAlignment = VerticalAlignment.Stretch;

            _rightHitZone.Width = tolerance;
            _rightHitZone.Height = height;
            _rightHitZone.HorizontalAlignment = HorizontalAlignment.Right;
            _rightHitZone.VerticalAlignment = VerticalAlignment.Stretch;
        }

        private static Border CreateHitZone()
        {
            return new Border
            {
                Background = Brushes.Transparent
            };
        }
    }
}
