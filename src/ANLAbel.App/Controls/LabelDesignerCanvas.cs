using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
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
    public event EventHandler? EditGestureStarted;
    public event EventHandler? EditGestureCompleted;
    public event EventHandler? EditGestureCanceled;

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

    public static readonly DependencyProperty IsSnapToGridEnabledProperty =
        DependencyProperty.Register(nameof(IsSnapToGridEnabled), typeof(bool), typeof(LabelDesignerCanvas),
            new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnGridPreferenceChanged));

    public static readonly DependencyProperty GridStepMmProperty =
        DependencyProperty.Register(nameof(GridStepMm), typeof(double), typeof(LabelDesignerCanvas),
            new FrameworkPropertyMetadata(SnapGridContract.DefaultStepMm, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnGridPreferenceChanged));

    public static readonly DependencyProperty InteractionStatusTextProperty =
        DependencyProperty.Register(nameof(InteractionStatusText), typeof(string), typeof(LabelDesignerCanvas),
            new FrameworkPropertyMetadata(string.Empty, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

    public static readonly DependencyProperty ShowPointerTelemetryProperty =
        DependencyProperty.Register(nameof(ShowPointerTelemetry), typeof(bool), typeof(LabelDesignerCanvas),
            new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnPointerTelemetryVisibilityChanged));

    public static readonly RoutedCommand DeleteSelectionCommand = new(nameof(DeleteSelectionCommand), typeof(LabelDesignerCanvas));

    private readonly Dictionary<LabelObject, FrameworkElement> _objectElements = new();
    private readonly HashSet<LabelObject> _selectedObjects = new();
    private readonly HashSet<LabelObject> _matrixAutoSizingObjects = new();
    private readonly HashSet<LabelObject> _textAutoSizingObjects = new();
    private readonly List<LabelObject> _clipboardObjects = new();
    private readonly Dictionary<LabelObject, (double X, double Y, double EndX, double EndY)> _groupDragStarts = new();
    private readonly Dictionary<LabelObject, GroupResizeObjectSnapshot> _groupResizeStarts = new();
    private readonly Dictionary<LabelObject, (double X, double Y, double EndX, double EndY)> _nudgeStarts = new();
    /// <summary>
    /// Bounded pointer-frame evidence for the current canvas instance. Record
    /// calls happen only after a drag preview frame has updated its visual;
    /// percentile snapshots are intentionally off the hot path.
    /// </summary>
    public PointerFrameTelemetry PointerTelemetry { get; } = new();
    private readonly IBarcodeRenderer _barcodeRenderer = new ZxingBarcodeRenderer();
    private readonly DesignerPreferencesService _designerPreferencesService = new();
    private readonly MenuItem _snapMenuItem;
    private readonly MenuItem _snapGridMenuItem;
    private readonly MenuItem _gridStepMenuItem;
    private readonly MenuItem _pointerTelemetryMenuItem;
    private readonly MenuItem _guidesMenuItem;
    private readonly MenuItem _addVerticalGuideMenuItem;
    private readonly MenuItem _addHorizontalGuideMenuItem;
    private readonly MenuItem _toggleGuideLockMenuItem;
    private readonly MenuItem _deleteGuideMenuItem;
    private readonly MenuItem _clearGuidesMenuItem;
    private SelectionResizeAdorner? _selectionAdorner;
    private SelectionResizeAdorner? _groupResizeAdorner;
    private GroupResizeObjectSnapshot _singleResizeStart;
    private bool _singleResizeActive;
    private LabelObject? _adornedObject;
    private AdornerLayer? _groupResizeAdornerLayer;
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
    private bool _nudgeGestureActive;
    private System.Windows.Threading.DispatcherTimer? _nudgeGestureTimer;
    private Point _drawingStartMm;
    private Point _lastDrawingEndMm;
    private string _dimensionBuffer = string.Empty;
    private int _pasteCount;

    // Alignment guide system. Interaction tolerance is expressed in screen DIP so
    // snapping feels consistent at 25% and 400% zoom alike; document geometry stays mm.
    private double SnapThresholdMm => SnapToleranceContract.AcquireToleranceMm(Zoom);
    private double SnapReleaseThresholdMm => SnapToleranceContract.ReleaseToleranceMm(Zoom);
    private readonly SnapHysteresisState _snapLockX = new();
    private readonly SnapHysteresisState _snapLockY = new();
    private Line? _guideVertical;
    private Line? _guideHorizontal;
    private Border? _guideVerticalLabel;
    private Border? _guideHorizontalLabel;
    private AlignmentSnapResult? _lastAlignmentSnap;
    private readonly Dictionary<LabelGuide, (Line Line, Border Label)> _persistentGuideVisuals = new();
    private LabelGuide? _contextGuide;
    private Point _contextMenuPoint;
    private LabelGuide? _draggedGuide;
    private bool _createdGuideForDrag;
    private double _draggedGuideStartPositionMm;

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
        LostKeyboardFocus += CanvasLostKeyboardFocus;
        _snapMenuItem = new MenuItem
        {
            Header = "Snap to objects",
            IsCheckable = true
        };
        _snapMenuItem.Click += (_, _) => IsSnapToObjectsEnabled = _snapMenuItem.IsChecked;
        _snapGridMenuItem = new MenuItem
        {
            Header = "Snap to grid",
            IsCheckable = true
        };
        _snapGridMenuItem.Click += (_, _) => IsSnapToGridEnabled = _snapGridMenuItem.IsChecked;
        _gridStepMenuItem = new MenuItem { Header = "Grid step" };
        foreach (var step in new[] { 0.5, 1.0, 2.0, 5.0, 10.0 })
        {
            var stepItem = new MenuItem
            {
                Header = $"{step:0.##} mm",
                IsCheckable = true,
                Tag = step
            };
            stepItem.Click += (_, _) =>
            {
                if (stepItem.Tag is double selectedStep)
                {
                    GridStepMm = selectedStep;
                    UpdateGridStepMenu();
                }
            };
            _gridStepMenuItem.Items.Add(stepItem);
        }
        _pointerTelemetryMenuItem = new MenuItem
        {
            Header = "Show pointer performance",
            IsCheckable = true,
            ToolTip = "Show opt-in P95/max drag-frame telemetry on the canvas"
        };
        _pointerTelemetryMenuItem.Click += (_, _) => ShowPointerTelemetry = _pointerTelemetryMenuItem.IsChecked;
        _guidesMenuItem = new MenuItem { Header = "Design guides" };
        _addVerticalGuideMenuItem = new MenuItem { Header = "Add vertical guide here" };
        _addHorizontalGuideMenuItem = new MenuItem { Header = "Add horizontal guide here" };
        _toggleGuideLockMenuItem = new MenuItem { Header = "Lock selected guide" };
        _deleteGuideMenuItem = new MenuItem { Header = "Delete selected guide" };
        _clearGuidesMenuItem = new MenuItem { Header = "Clear all guides" };
        _addVerticalGuideMenuItem.Click += (_, _) => AddGuideFromContext(LabelGuideOrientation.Vertical);
        _addHorizontalGuideMenuItem.Click += (_, _) => AddGuideFromContext(LabelGuideOrientation.Horizontal);
        _toggleGuideLockMenuItem.Click += (_, _) => ToggleContextGuideLock();
        _deleteGuideMenuItem.Click += (_, _) => DeleteContextGuide();
        _clearGuidesMenuItem.Click += (_, _) => ClearAllGuides();
        _guidesMenuItem.Items.Add(_addVerticalGuideMenuItem);
        _guidesMenuItem.Items.Add(_addHorizontalGuideMenuItem);
        _guidesMenuItem.Items.Add(new Separator());
        _guidesMenuItem.Items.Add(_toggleGuideLockMenuItem);
        _guidesMenuItem.Items.Add(_deleteGuideMenuItem);
        _guidesMenuItem.Items.Add(_clearGuidesMenuItem);
        ContextMenu = new ContextMenu { Items = { _snapMenuItem, _snapGridMenuItem, _gridStepMenuItem, new Separator(), _pointerTelemetryMenuItem, _guidesMenuItem } };
        ContextMenuOpening += (_, e) =>
        {
            if (DrawingTool is not null)
            {
                e.Handled = true;
                return;
            }

            _snapMenuItem.IsChecked = IsSnapToObjectsEnabled;
            _snapGridMenuItem.IsChecked = IsSnapToGridEnabled;
            _pointerTelemetryMenuItem.IsChecked = ShowPointerTelemetry;
            _gridStepMenuItem.IsEnabled = IsSnapToGridEnabled;
            UpdateGridStepMenu();
            _contextMenuPoint = Mouse.GetPosition(this);
            _contextGuide = FindNearestGuideAtPoint(_contextMenuPoint, includeLocked: true);
            UpdateGuideContextMenu();
        };

        var preferences = _designerPreferencesService.Load();
        SetCurrentValue(IsSnapToObjectsEnabledProperty, preferences.SnapToObjects);
        SetCurrentValue(IsSnapToGridEnabledProperty, preferences.SnapToGrid);
        SetCurrentValue(GridStepMmProperty, SnapGridContract.NormalizeStep(preferences.GridStepMm));
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

    public bool IsSnapToGridEnabled
    {
        get => (bool)GetValue(IsSnapToGridEnabledProperty);
        set => SetValue(IsSnapToGridEnabledProperty, value);
    }

    public double GridStepMm
    {
        get => SnapGridContract.NormalizeStep((double)GetValue(GridStepMmProperty));
        set => SetValue(GridStepMmProperty, SnapGridContract.NormalizeStep(value));
    }

    public string InteractionStatusText
    {
        get => (string)GetValue(InteractionStatusTextProperty);
        set => SetValue(InteractionStatusTextProperty, value);
    }

    public bool ShowPointerTelemetry
    {
        get => (bool)GetValue(ShowPointerTelemetryProperty);
        set => SetValue(ShowPointerTelemetryProperty, value);
    }

    /// <summary>
    /// Number of objects in the canvas selection.  The WPF shell uses this to
    /// decide whether arrange commands should be offered, while the canvas
    /// remains the single owner of multi-selection state.
    /// </summary>
    public int SelectedObjectCount => _selectedObjects.Count;

    /// <summary>
    /// Makes one member of the current multi-selection the key object without
    /// collapsing the selection.  This is intentionally separate from
    /// <see cref="SelectedObject"/> so the pointer path and arrange commands
    /// share one explicit key-object invariant.
    /// </summary>
    public bool SetKeyObject(LabelObject item)
    {
        if (Template is null
            || item is null
            || !_selectedObjects.Contains(item)
            || !Template.Objects.Contains(item))
        {
            return false;
        }

        var changed = !ReferenceEquals(SelectedObject, item);
        SelectedObject = item;
        RefreshSelectionAdorner();
        InvalidateVisual();
        InteractionStatusText = changed
            ? $"Key object: {item.Name}"
            : $"Key object remains {item.Name}";
        return true;
    }

    public void NotifyEditGestureStarted() => EditGestureStarted?.Invoke(this, EventArgs.Empty);
    public void NotifyEditGestureCompleted()
    {
        // Transform previews intentionally defer this document-wide extent
        // scan until the gesture boundary.  Otherwise every pointer tick
        // walks the entire scene before the event can be committed.
        UpdateCanvasExtent();
        EditGestureCompleted?.Invoke(this, EventArgs.Empty);
    }
    public void NotifyEditGestureCanceled()
    {
        // A canceled drag/resize restores model coordinates before this
        // notification. Recompute the workspace extent at the same gesture
        // boundary as a successful commit so an overflowed object cannot leave
        // a stale canvas size or selection viewport after Escape/lost capture.
        UpdateCanvasExtent();
        EditGestureCanceled?.Invoke(this, EventArgs.Empty);
    }

    public bool AlignSelectedObjects(LabelAlignmentMode alignment, LabelArrangeReferenceMode reference)
    {
        if (Template is null)
        {
            return false;
        }

        NotifyEditGestureStarted();
        try
        {
            var result = LabelArrangeEngine.Align(
                _selectedObjects.OrderBy(item => item.ZIndex).ThenBy(item => item.Id, StringComparer.Ordinal).ToArray(),
                SelectedObject,
                alignment,
                reference,
                Template.WidthMm,
                Template.HeightMm);
            var changed = ApplyArrangeResult(result, $"Aligned {alignment} ({reference})");
            if (changed)
            {
                NotifyEditGestureCompleted();
            }
            else
            {
                NotifyEditGestureCanceled();
            }

            return changed;
        }
        catch
        {
            NotifyEditGestureCanceled();
            throw;
        }
    }

    public bool DistributeSelectedObjects(LabelDistributionMode distribution)
    {
        NotifyEditGestureStarted();
        try
        {
            var result = LabelArrangeEngine.Distribute(
                _selectedObjects.OrderBy(item => item.ZIndex).ThenBy(item => item.Id, StringComparer.Ordinal).ToArray(),
                distribution);
            var changed = ApplyArrangeResult(result, $"Distributed {distribution}");
            if (changed)
            {
                NotifyEditGestureCompleted();
            }
            else
            {
                NotifyEditGestureCanceled();
            }

            return changed;
        }
        catch
        {
            NotifyEditGestureCanceled();
            throw;
        }
    }

    /// <summary>
    /// Aligns the first text baseline of the selected Text/TextBox objects to
    /// the selected primary text object.  The metric and vertical offset are
    /// measured through the same FormattedText/wrap path used by preview and
    /// print, so this is not a visual-only approximation.
    /// </summary>
    public bool AlignSelectedTextBaselines()
    {
        var textItems = _selectedObjects
            .Where(item => item.Type is ObjectType.Text or ObjectType.TextBox)
            .OrderBy(item => item.ZIndex)
            .ThenBy(item => item.Id, StringComparer.Ordinal)
            .ToArray();
        if (textItems.Length < 2)
        {
            InteractionStatusText = "Select at least two text objects for baseline alignment.";
            return false;
        }

        if (textItems.Any(item => item.IsLocked || !item.IsVisible))
        {
            InteractionStatusText = "Unlock and show every selected text object before baseline alignment.";
            return false;
        }

        var key = SelectedObject is not null && textItems.Contains(SelectedObject)
            ? SelectedObject
            : textItems[0];
        var targetBaselineMm = GetTextBaselineMm(key);
        NotifyEditGestureStarted();
        var changed = 0;
        foreach (var item in textItems)
        {
            if (ReferenceEquals(item, key))
            {
                continue;
            }

            var deltaY = targetBaselineMm - GetTextBaselineMm(item);
            if (Math.Abs(deltaY) <= 0.004)
            {
                continue;
            }

            item.YMm += deltaY;
            changed++;
        }

        if (changed == 0)
        {
            NotifyEditGestureCanceled();
            InteractionStatusText = "Baseline alignment: already aligned.";
            return false;
        }

        foreach (var item in textItems)
        {
            UpdateObjectTransformElement(item);
        }

        InvalidateVisual();
        NotifyEditGestureCompleted();
        InteractionStatusText = $"Baseline aligned to {key.Name}: {changed} object(s) changed.";
        return true;
    }

    /// <summary>
    /// Aligns selected text by the visible WPF glyph ink rather than by the
    /// authored frame. This is deliberately an explicit command: frame and
    /// baseline alignment remain the safe defaults for production labels.
    /// </summary>
    public bool AlignSelectedTextOptically(
        OpticalAlignmentAxis axis = OpticalAlignmentAxis.Horizontal,
        OpticalAlignmentAnchor anchor = OpticalAlignmentAnchor.Center)
    {
        var textItems = _selectedObjects
            .Where(item => item.Type is ObjectType.Text or ObjectType.TextBox)
            .OrderBy(item => item.ZIndex)
            .ThenBy(item => item.Id, StringComparer.Ordinal)
            .ToArray();
        if (textItems.Length < 2)
        {
            InteractionStatusText = "Select at least two text objects for optical alignment.";
            return false;
        }

        if (textItems.Any(item => item.IsLocked || !item.IsVisible))
        {
            InteractionStatusText = "Unlock and show every selected text object before optical alignment.";
            return false;
        }

        var key = SelectedObject is not null && textItems.Contains(SelectedObject)
            ? SelectedObject
            : textItems[0];
        var targetInk = GetTextInkBoundsMm(key);
        if (targetInk is null)
        {
            InteractionStatusText = $"Optical alignment could not measure visible ink in {key.Name}.";
            return false;
        }

        NotifyEditGestureStarted();
        var changed = 0;
        foreach (var item in textItems)
        {
            if (ReferenceEquals(item, key))
            {
                continue;
            }

            var sourceInk = GetTextInkBoundsMm(item);
            if (sourceInk is null)
            {
                NotifyEditGestureCanceled();
                InteractionStatusText = $"Optical alignment stopped: visible ink could not be measured in {item.Name}.";
                return false;
            }

            var result = OpticalAlignmentContract.Align(sourceInk.Value, targetInk.Value, axis, anchor);
            if (!result.Succeeded)
            {
                NotifyEditGestureCanceled();
                InteractionStatusText = result.ErrorMessage ?? "Optical alignment failed closed.";
                return false;
            }

            if (Math.Abs(result.DeltaX) <= 0.004 && Math.Abs(result.DeltaY) <= 0.004)
            {
                continue;
            }

            item.XMm += result.DeltaX;
            item.YMm += result.DeltaY;
            changed++;
        }

        if (changed == 0)
        {
            NotifyEditGestureCanceled();
            InteractionStatusText = "Optical alignment: visible ink is already aligned.";
            return false;
        }

        foreach (var item in textItems)
        {
            UpdateObjectElement(item);
        }

        InvalidateVisual();
        NotifyEditGestureCompleted();
        InteractionStatusText = $"Optical {anchor.ToString().ToLowerInvariant()} alignment to {key.Name}: {changed} object(s) changed.";
        return true;
    }

    private OpticalBounds? GetTextInkBoundsMm(LabelObject item)
    {
        if (item.Type is not (ObjectType.Text or ObjectType.TextBox))
        {
            return null;
        }

        var widthDip = Math.Max(1, MmConverter.MmToDip(item.WidthMm));
        var heightDip = Math.Max(1, MmConverter.MmToDip(item.HeightMm));
        var pixelsPerDip = GetPixelsPerDip();
        var value = string.IsNullOrEmpty(GetDisplayText(item)) ? " " : GetDisplayText(item);
        var constrained = TextBoxOverflowDetector.ShouldConstrainToBox(item);
        var originX = TextBoxOverflowDetector.GetHorizontalOriginDip(item, constrained);
        Rect ink;

        if (item.Type == ObjectType.Text
            || TextBoxOverflowDetector.HasExplicitLineHeight(item)
            || TextBoxOverflowDetector.UsesShrinkFont(item)
            || TextBoxOverflowDetector.UsesScaleWidth(item))
        {
            var layout = TextBoxOverflowDetector.CreateTextLayout(
                item,
                value,
                widthDip,
                heightDip,
                constrained,
                Brushes.Black,
                pixelsPerDip);
            ink = TextBoxOverflowDetector.GetInkBoundsDip(
                layout,
                new Point(originX, layout.Metrics.VerticalOffsetDip));
        }
        else
        {
            var displayValue = constrained
                ? TextBoxOverflowDetector.WrapTextToBox(item, value, TextBoxOverflowDetector.GetContentWidthDip(item, widthDip, constrained), pixelsPerDip)
                : value;
            var formatted = TextBoxOverflowDetector.CreateFormattedText(item, displayValue, Brushes.Black, pixelsPerDip);
            TextBoxOverflowDetector.ApplyLayoutBounds(formatted, item, widthDip, heightDip, constrained);
            var metrics = TextBoxOverflowDetector.Measure(formatted, item, widthDip, heightDip, constrained, value, pixelsPerDip: pixelsPerDip);
            ink = TextBoxOverflowDetector.GetInkBoundsDip(formatted, new Point(originX, metrics.VerticalOffsetDip));
        }

        if (constrained && !ink.IsEmpty)
        {
            ink.Intersect(new Rect(0, 0, widthDip, heightDip));
        }

        if (ink.IsEmpty
            || !double.IsFinite(ink.Left)
            || !double.IsFinite(ink.Top)
            || !double.IsFinite(ink.Right)
            || !double.IsFinite(ink.Bottom)
            || ink.Width <= 0
            || ink.Height <= 0)
        {
            return null;
        }

        return new OpticalBounds(
            item.XMm + MmConverter.DipToMm(ink.Left),
            item.YMm + MmConverter.DipToMm(ink.Top),
            item.XMm + MmConverter.DipToMm(ink.Right),
            item.YMm + MmConverter.DipToMm(ink.Bottom));
    }

    private double GetTextBaselineMm(LabelObject item)
    {
        var widthDip = MmConverter.MmToDip(item.WidthMm);
        var heightDip = MmConverter.MmToDip(item.HeightMm);
        var pixelsPerDip = GetPixelsPerDip();
        var value = ResolveObjectData(item);
        if (item.Type == ObjectType.Text
            || TextBoxOverflowDetector.HasExplicitLineHeight(item)
            || TextBoxOverflowDetector.UsesShrinkFont(item)
            || TextBoxOverflowDetector.UsesScaleWidth(item))
        {
            var explicitLayout = TextBoxOverflowDetector.CreateTextLayout(
                item,
                value,
                widthDip,
                heightDip,
                TextBoxOverflowDetector.ShouldConstrainToBox(item),
                Brushes.Black,
                pixelsPerDip);
            return item.YMm + MmConverter.DipToMm(explicitLayout.Metrics.VerticalOffsetDip + explicitLayout.Metrics.BaselineDip);
        }

        var constrained = TextBoxOverflowDetector.ShouldConstrainToBox(item);
        var displayValue = constrained
            ? TextBoxOverflowDetector.WrapTextToBox(item, value, TextBoxOverflowDetector.GetContentWidthDip(item, widthDip, constrained), pixelsPerDip)
            : value;
        var formatted = TextBoxOverflowDetector.CreateFormattedText(item, displayValue, Brushes.Black, pixelsPerDip);
        TextBoxOverflowDetector.ApplyLayoutBounds(formatted, item, widthDip, heightDip, constrained);
        var metrics = TextBoxOverflowDetector.Measure(formatted, item, widthDip, heightDip, constrained, value, pixelsPerDip: pixelsPerDip);
        return item.YMm + MmConverter.DipToMm(metrics.VerticalOffsetDip + metrics.BaselineDip);
    }

    private bool ApplyArrangeResult(LabelArrangeResult result, string action)
    {
        if (!result.Succeeded)
        {
            InteractionStatusText = result.ErrorMessage ?? "Arrange operation could not be applied.";
            return false;
        }

        if (!result.Changed)
        {
            InteractionStatusText = $"{action}: already aligned.";
            return false;
        }

        foreach (var item in _selectedObjects)
        {
            UpdateObjectElement(item);
        }

        InvalidateVisual();
        InteractionStatusText = $"{action}: {result.AffectedCount} object(s) changed.";
        return true;
    }

    /// <summary>
    /// Starts an authoring-only ruler guide gesture. A nearby unlocked guide is
    /// moved; otherwise a new guide is created at the ruler position. The
    /// caller must finish with <see cref="CompleteGuideDrag"/> or
    /// <see cref="CancelGuideDrag"/> so the operation remains one undo step.
    /// </summary>
    public bool BeginGuideDrag(LabelGuideOrientation orientation, double positionMm)
    {
        if (Template is null || !Enum.IsDefined(orientation))
        {
            return false;
        }

        var widthMm = Template.WidthMm;
        var heightMm = Template.HeightMm;
        var clamped = LabelGuideContract.ClampPosition(positionMm, orientation, widthMm, heightMm);
        var existing = LabelGuideContract.FindNearest(
            Template.Guides,
            orientation,
            clamped,
            Zoom,
            widthMm,
            heightMm,
            includeLocked: true);
        if (existing?.IsLocked == true)
        {
            InteractionStatusText = "The selected guide is locked. Unlock it from the Design guides menu before moving it.";
            return false;
        }

        NotifyEditGestureStarted();
        _draggedGuide = existing;
        _createdGuideForDrag = existing is null;
        if (_draggedGuide is null)
        {
            _draggedGuide = new LabelGuide
            {
                Orientation = orientation,
                PositionMm = clamped
            };
            Template.Guides.Add(_draggedGuide);
        }

        _draggedGuideStartPositionMm = _draggedGuide.PositionMm;
        UpdateGuideDrag(clamped);
        InteractionStatusText = $"Moving {orientation.ToString().ToLowerInvariant()} guide at {clamped:0.###} mm.";
        return true;
    }

    public void UpdateGuideDrag(double positionMm)
    {
        if (Template is null || _draggedGuide is null)
        {
            return;
        }

        var clamped = LabelGuideContract.ClampPosition(
            positionMm,
            _draggedGuide.Orientation,
            Template.WidthMm,
            Template.HeightMm);
        _draggedGuide.PositionMm = clamped;
        UpdatePersistentGuideVisual(_draggedGuide);
        InvalidateVisual();
    }

    public void CompleteGuideDrag(double positionMm)
    {
        if (_draggedGuide is null)
        {
            return;
        }

        UpdateGuideDrag(positionMm);
        var guide = _draggedGuide;
        var changed = Math.Abs(guide.PositionMm - _draggedGuideStartPositionMm) > 0.0001 || _createdGuideForDrag;
        _draggedGuide = null;
        _createdGuideForDrag = false;
        if (changed)
        {
            NotifyEditGestureCompleted();
            InteractionStatusText = $"Guide saved at {guide.PositionMm:0.###} mm.";
        }
        else
        {
            NotifyEditGestureCanceled();
        }
    }

    public void CancelGuideDrag()
    {
        if (_draggedGuide is null)
        {
            return;
        }

        var guide = _draggedGuide;
        if (_createdGuideForDrag)
        {
            Template?.Guides.Remove(guide);
        }
        else
        {
            guide.PositionMm = _draggedGuideStartPositionMm;
        }

        _draggedGuide = null;
        _createdGuideForDrag = false;
        NotifyEditGestureCanceled();
        InteractionStatusText = "Guide move cancelled.";
    }

    private void CreatePersistentGuideVisual(LabelGuide guide)
    {
        if (_persistentGuideVisuals.ContainsKey(guide))
        {
            UpdatePersistentGuideVisual(guide);
            return;
        }

        var line = new Line
        {
            StrokeThickness = 1.2,
            StrokeDashArray = new DoubleCollection { 4, 3 },
            IsHitTestVisible = false
        };
        var label = new Border
        {
            Padding = new Thickness(4, 1, 4, 1),
            CornerRadius = new CornerRadius(3),
            BorderThickness = new Thickness(1),
            IsHitTestVisible = false,
            Child = new TextBlock { FontSize = 10, FontWeight = FontWeights.SemiBold }
        };
        SetZIndex(line, int.MaxValue - 3);
        SetZIndex(label, int.MaxValue - 2);
        Children.Add(line);
        Children.Add(label);
        _persistentGuideVisuals[guide] = (line, label);
        UpdatePersistentGuideVisual(guide);
    }

    private void UpdatePersistentGuideVisual(LabelGuide guide)
    {
        if (Template is null || !_persistentGuideVisuals.TryGetValue(guide, out var visuals))
        {
            return;
        }

        var widthDip = MmToDip(Template.WidthMm);
        var heightDip = MmToDip(Template.HeightMm);
        var positionMm = LabelGuideContract.ClampPosition(
            guide.PositionMm,
            guide.Orientation,
            Template.WidthMm,
            Template.HeightMm);
        var positionDip = MmToDip(positionMm);
        var visibility = guide.IsVisible ? Visibility.Visible : Visibility.Collapsed;
        var accent = guide.IsLocked
            ? Color.FromRgb(100, 116, 139)
            : Color.FromRgb(234, 88, 12);
        visuals.Line.Stroke = new SolidColorBrush(accent);
        visuals.Line.Visibility = visibility;
        visuals.Label.Visibility = visibility;

        if (guide.Orientation == LabelGuideOrientation.Vertical)
        {
            visuals.Line.X1 = positionDip;
            visuals.Line.X2 = positionDip;
            visuals.Line.Y1 = 0;
            visuals.Line.Y2 = heightDip;
            SetLeft(visuals.Label, Math.Max(0, Math.Min(Math.Max(0, widthDip - 140), positionDip + 4)));
            SetTop(visuals.Label, 4);
            UpdatePersistentGuideLabel(visuals.Label, $"Guide X {positionMm:0.###} mm", accent);
        }
        else
        {
            visuals.Line.X1 = 0;
            visuals.Line.X2 = widthDip;
            visuals.Line.Y1 = positionDip;
            visuals.Line.Y2 = positionDip;
            SetLeft(visuals.Label, 4);
            SetTop(visuals.Label, Math.Max(0, Math.Min(Math.Max(0, heightDip - 28), positionDip + 4)));
            UpdatePersistentGuideLabel(visuals.Label, $"Guide Y {positionMm:0.###} mm", accent);
        }
    }

    private static void UpdatePersistentGuideLabel(Border label, string text, Color accent)
    {
        if (label.Child is TextBlock textBlock)
        {
            textBlock.Text = text;
            textBlock.Foreground = new SolidColorBrush(Colors.White);
        }

        label.Background = new SolidColorBrush(Color.FromArgb(235, accent.R, accent.G, accent.B));
        label.BorderBrush = new SolidColorBrush(accent);
    }

    private LabelGuide? FindNearestGuideAtPoint(Point point, bool includeLocked)
    {
        if (Template is null)
        {
            return null;
        }

        var xMm = DipToMm(point.X);
        var yMm = DipToMm(point.Y);
        var vertical = LabelGuideContract.FindNearest(
            Template.Guides,
            LabelGuideOrientation.Vertical,
            xMm,
            Zoom,
            Template.WidthMm,
            Template.HeightMm,
            includeLocked);
        var horizontal = LabelGuideContract.FindNearest(
            Template.Guides,
            LabelGuideOrientation.Horizontal,
            yMm,
            Zoom,
            Template.WidthMm,
            Template.HeightMm,
            includeLocked);
        if (vertical is null)
        {
            return horizontal;
        }

        if (horizontal is null)
        {
            return vertical;
        }

        return Math.Abs(vertical.PositionMm - xMm) <= Math.Abs(horizontal.PositionMm - yMm)
            ? vertical
            : horizontal;
    }

    private void AddGuideFromContext(LabelGuideOrientation orientation)
    {
        if (Template is null)
        {
            return;
        }

        var positionMm = orientation == LabelGuideOrientation.Vertical
            ? DipToMm(_contextMenuPoint.X)
            : DipToMm(_contextMenuPoint.Y);
        var guide = new LabelGuide
        {
            Orientation = orientation,
            PositionMm = LabelGuideContract.ClampPosition(positionMm, orientation, Template.WidthMm, Template.HeightMm)
        };
        NotifyEditGestureStarted();
        Template.Guides.Add(guide);
        NotifyEditGestureCompleted();
        InteractionStatusText = $"Added {orientation.ToString().ToLowerInvariant()} guide at {guide.PositionMm:0.###} mm.";
    }

    private void ToggleContextGuideLock()
    {
        if (_contextGuide is null)
        {
            return;
        }

        NotifyEditGestureStarted();
        _contextGuide.IsLocked = !_contextGuide.IsLocked;
        NotifyEditGestureCompleted();
        InteractionStatusText = _contextGuide.IsLocked ? "Guide locked." : "Guide unlocked.";
    }

    private void DeleteContextGuide()
    {
        if (_contextGuide is null || _contextGuide.IsLocked || Template is null)
        {
            return;
        }

        NotifyEditGestureStarted();
        Template.Guides.Remove(_contextGuide);
        NotifyEditGestureCompleted();
        InteractionStatusText = "Guide deleted.";
        _contextGuide = null;
    }

    private void ClearAllGuides()
    {
        if (Template is null || Template.Guides.Count == 0)
        {
            return;
        }

        NotifyEditGestureStarted();
        var removable = Template.Guides.Where(guide => !guide.IsLocked).ToArray();
        foreach (var guide in removable)
        {
            Template.Guides.Remove(guide);
        }

        NotifyEditGestureCompleted();
        InteractionStatusText = removable.Length == 0
            ? "All guides are locked."
            : $"Removed {removable.Length} guide(s).";
    }

    private void UpdateGuideContextMenu()
    {
        var hasGuide = _contextGuide is not null;
        _toggleGuideLockMenuItem.IsEnabled = hasGuide;
        _deleteGuideMenuItem.IsEnabled = hasGuide && !_contextGuide!.IsLocked;
        _toggleGuideLockMenuItem.Header = hasGuide && _contextGuide!.IsLocked
            ? "Unlock selected guide"
            : "Lock selected guide";
        _clearGuidesMenuItem.IsEnabled = Template?.Guides.Any(guide => !guide.IsLocked) == true;
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

        var gridStep = MmToDip(SnapGridContract.NormalizeStep(GridStepMm));
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
        DrawPointerTelemetryOverlay(dc);
    }

    private void DrawPointerTelemetryOverlay(DrawingContext dc)
    {
        if (!ShowPointerTelemetry)
        {
            return;
        }

        var snapshot = PointerTelemetry.Snapshot();
        var pixelsPerDip = GetPixelsPerDip();
        var zoom = SnapToleranceContract.NormalizeZoom(Zoom);
        var text = snapshot.HasSamples
            ? $"Pointer P95 {snapshot.P95Milliseconds:0.0} ms · max {snapshot.MaximumMilliseconds:0.0} ms · "
                + $"{snapshot.SampleCount}/{PointerTelemetry.Capacity} · zoom {zoom * 100:0}% · display {pixelsPerDip:0.##}x"
            : $"Pointer performance: waiting for drag sample · zoom {zoom * 100:0}% · display {pixelsPerDip:0.##}x";
        var formatted = new FormattedText(
            text,
            CultureInfo.InvariantCulture,
            FlowDirection.LeftToRight,
            new Typeface(
                new FontFamily("Segoe UI"),
                FontStyles.Normal,
                FontWeights.SemiBold,
                FontStretches.Normal),
            11,
            Brushes.White,
            pixelsPerDip);
        var bounds = new Rect(4, 4, formatted.Width + 14, formatted.Height + 8);
        var background = new SolidColorBrush(Color.FromArgb(224, 15, 23, 42));
        background.Freeze();
        dc.DrawRoundedRectangle(background, null, bounds, 4, 4);
        dc.DrawText(formatted, new Point(bounds.Left + 7, bounds.Top + 4));
    }

    private void Rebuild()
    {
        if (Template is not null)
        {
            UnobserveGuides(Template);
        }

        var previousSelectedItems = _selectedObjects.ToHashSet();
        var previousSelectedIds = _selectedObjects
            .Select(item => item.Id)
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .ToHashSet(StringComparer.Ordinal);
        var previousPrimary = SelectedObject;

        RemoveAllSelectionAdorners();
        foreach (var oldItem in _objectElements.Keys.ToArray())
        {
            oldItem.PropertyChanged -= ObjectOnPropertyChanged;
            oldItem.Style.PropertyChanged -= StyleOnPropertyChanged;
        }

        Children.Clear();
        _objectElements.Clear();
        _selectedObjects.Clear();
        _groupDragStarts.Clear();
        _marqueeElement = null;
        _isMarqueeSelecting = false;
        _guideVertical = null;
        _guideHorizontal = null;
        _guideVerticalLabel = null;
        _guideHorizontalLabel = null;
        _lastAlignmentSnap = null;
        _persistentGuideVisuals.Clear();
        RemoveAllSelectionAdorners();

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
            item.PropertyChanged += ObjectOnPropertyChanged;
            item.Style.PropertyChanged += StyleOnPropertyChanged;
            AddObjectElement(item);
        }

        foreach (var item in Template.Objects)
        {
            if (previousSelectedItems.Contains(item)
                || (!string.IsNullOrWhiteSpace(item.Id) && previousSelectedIds.Contains(item.Id)))
            {
                _selectedObjects.Add(item);
            }
        }

        ObserveGuides(Template);
        foreach (var guide in Template.Guides)
        {
            CreatePersistentGuideVisual(guide);
        }

        var restoredPrimary = Template.Objects.FirstOrDefault(item => ReferenceEquals(item, previousPrimary))
            ?? Template.Objects.FirstOrDefault(item => !string.IsNullOrWhiteSpace(previousPrimary?.Id)
                && string.Equals(item.Id, previousPrimary.Id, StringComparison.Ordinal));
        SelectedObject = restoredPrimary is not null && _selectedObjects.Contains(restoredPrimary)
            ? restoredPrimary
            : _selectedObjects.FirstOrDefault();
        RefreshSelectionAdorner();

        InvalidateVisual();
    }

    private void AddObjectElement(LabelObject item)
    {
        var element = CreateObjectElement(item);
        element.Cursor = item.IsLocked ? Cursors.Arrow : Cursors.SizeAll;
        element.PreviewMouseLeftButtonDown += (sender, e) =>
        {
            Focus();
            CommitNudgeGesture();
            if (DrawingTool is not null)
            {
                return;
            }

            e.Handled = true;
            var modifiers = Keyboard.Modifiers;
            var isToggle = (modifiers & ModifierKeys.Control) == ModifierKeys.Control;
            var isAdditive = (modifiers & (ModifierKeys.Control | ModifierKeys.Shift)) != ModifierKeys.None;
            if (isToggle && _selectedObjects.Contains(item))
            {
                _selectedObjects.Remove(item);
                SelectedObject = _selectedObjects.LastOrDefault();
                RefreshSelectionAdorner();
                InvalidateVisual();
                return;
            }

            // Clicking a member that is already in a multi-selection changes
            // the key object while retaining every selected peer.  Without
            // this branch a normal click cleared the group, making key-object
            // align/distribute commands impossible to use predictably.
            var preserveSelectionAsKey = !isAdditive
                && _selectedObjects.Count > 1
                && _selectedObjects.Contains(item);
            if (!preserveSelectionAsKey && !isAdditive)
            {
                _selectedObjects.Clear();
            }

            if (!_selectedObjects.Contains(item))
            {
                _selectedObjects.Add(item);
            }

            if (preserveSelectionAsKey)
            {
                SetKeyObject(item);
            }
            else
            {
                SelectedObject = item;
            }

            RefreshSelectionAdorner();

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
            ClearSnapLocks();
            CaptureGroupDragStarts();
            NotifyEditGestureStarted();
            ((FrameworkElement)sender).CaptureMouse();
        };
        element.PreviewMouseMove += (sender, e) =>
        {
            if (_dragObject != item || e.LeftButton != MouseButtonState.Pressed || item.IsLocked)
            {
                return;
            }

            var frameStart = Stopwatch.GetTimestamp();
            var current = e.GetPosition(this);
            var deltaXMm = DipToMm(current.X - _dragStart.X);
            var deltaYMm = DipToMm(current.Y - _dragStart.Y);
            if (_selectedObjects.Count > 1)
            {
                MoveSelectedGroup(deltaXMm, deltaYMm);
            }
            else if (item.Type == ObjectType.Line)
            {
                var snap = MoveSingleLine(item, deltaXMm, deltaYMm);
                ShowAlignmentGuides(snap);
            }
            else
            {
                var proposedX = _startXMm + deltaXMm;
                var proposedY = _startYMm + deltaYMm;

                // Alignment guide: compute snap position against other objects
                // Hold Alt to temporarily disable snapping
                var snap = new AlignmentSnapResult(null, null, new List<double>(), new List<double>());
                if ((IsSnapToObjectsEnabled || IsSnapToGridEnabled)
                    && !Keyboard.IsKeyDown(Key.LeftAlt)
                    && !Keyboard.IsKeyDown(Key.RightAlt))
                {
                    snap = ComputePriorityAlignmentSnap(item, proposedX, proposedY);
                    if (snap.SnapX is not null)
                    {
                        proposedX = snap.SnapX.Value;
                    }
                    if (snap.SnapY is not null)
                    {
                        proposedY = snap.SnapY.Value;
                    }
                }
                else
                {
                    ClearSnapLocks();
                }

                // Clamp all 4 sides (consistent with group drag behavior)
                item.XMm = Math.Max(0, Math.Min(Template!.WidthMm - item.WidthMm, proposedX));
                item.YMm = Math.Max(0, Math.Min(Template!.HeightMm - item.HeightMm, proposedY));

                // Show/hide guide lines
                ShowAlignmentGuides(snap);
            }

            UpdateObjectTransformElement(item);
            PointerTelemetry.Record(
                Stopwatch.GetElapsedTime(frameStart),
                Zoom,
                GetPixelsPerDip());
            if (ShowPointerTelemetry)
            {
                InvalidateVisual();
            }
        };
        element.PreviewMouseLeftButtonUp += (sender, _) =>
        {
            if (_dragObject == item)
            {
                NotifyEditGestureCompleted();
            }
            _dragObject = null;
            _groupDragStarts.Clear();
            ClearSnapLocks();
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
                Child = CreateBarcodePanel()
            },
            ObjectType.Image => new Border
            {
                Background = Brushes.Transparent,
                BorderBrush = Brushes.Transparent,
                BorderThickness = new Thickness(0),
                Child = new Image
                {
                    Stretch = Stretch.Uniform,
                    SnapsToDevicePixels = true,
                    UseLayoutRounding = true
                }
            },
            _ => new Border()
        };
    }

    private static Grid CreateBarcodePanel()
    {
        var panel = new Grid();
        panel.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        panel.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        var image = new Image
        {
            Stretch = Stretch.Uniform,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            SnapsToDevicePixels = true,
            UseLayoutRounding = true
        };
        Grid.SetRow(image, 0);

        var hri = new TextBlock
        {
            Visibility = Visibility.Collapsed,
            TextAlignment = TextAlignment.Center,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Center,
            TextWrapping = TextWrapping.NoWrap,
            Foreground = Brushes.Black
        };
        Grid.SetRow(hri, 1);

        panel.Children.Add(image);
        panel.Children.Add(hri);
        return panel;
    }

    private void UpdateObjectTransformElement(LabelObject item)
    {
        if (!_objectElements.TryGetValue(item, out var element))
        {
            return;
        }

        if (element is Line line)
        {
            var endXMm = item.LineEndXMm == 0 && item.LineEndYMm == 0
                ? item.XMm + item.WidthMm
                : item.LineEndXMm;
            var endYMm = item.LineEndXMm == 0 && item.LineEndYMm == 0
                ? item.YMm + item.HeightMm
                : item.LineEndYMm;
            var minXMm = Math.Min(item.XMm, endXMm);
            var minYMm = Math.Min(item.YMm, endYMm);
            var lineWidth = MmToDip(Math.Abs(endXMm - item.XMm));
            var lineHeight = MmToDip(Math.Abs(endYMm - item.YMm));
            var strokeThickness = item.Style.OutlineStyle == OutlineStyle.None
                ? 0
                : Math.Max(1, MmToDip(item.Style.BorderThicknessMm));
            var strokePadding = Math.Ceiling(strokeThickness / 2) + 2;
            SetLeft(element, MmToDip(minXMm) - strokePadding);
            SetTop(element, MmToDip(minYMm) - strokePadding);
            line.X1 = MmToDip(item.XMm - minXMm) + strokePadding;
            line.Y1 = MmToDip(item.YMm - minYMm) + strokePadding;
            line.X2 = MmToDip(endXMm - minXMm) + strokePadding;
            line.Y2 = MmToDip(endYMm - minYMm) + strokePadding;
            element.Width = Math.Max(1, lineWidth) + strokePadding * 2;
            element.Height = Math.Max(1, lineHeight) + strokePadding * 2;
        }
        else
        {
            SetLeft(element, MmToDip(item.XMm));
            SetTop(element, MmToDip(item.YMm));
        }

        element.InvalidateArrange();
        element.InvalidateVisual();
    }

    private static bool IsTransformOnlyProperty(string? propertyName)
    {
        return propertyName is nameof(LabelObject.XMm)
            or nameof(LabelObject.YMm)
            or nameof(LabelObject.LineEndXMm)
            or nameof(LabelObject.LineEndYMm);
    }

    private bool IsTransformGestureActive
        => _dragObject is not null
            || _singleResizeActive
            || _groupResizeAdorner?.IsResizeActive == true;

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
                // TextBox: never draw a permanent outline stroke (viền line).
                // Selection adorners still show when selected. Other objects
                // honor OutlineStyle as usual.
                border.BorderThickness = item.Type == ObjectType.TextBox
                    || item.Style.OutlineStyle == OutlineStyle.None
                    ? new Thickness(0)
                    : new Thickness(Math.Max(0, MmToDip(item.Style.BorderThicknessMm)));
                border.ToolTip = objectError;
                border.Background = item.Type == ObjectType.Rectangle
                    ? ParseBrush(item.Style.FillColor, Brushes.Transparent)
                    : Brushes.Transparent;

                    if (border.Child is VisualPreviewHost textHost && item.Type == ObjectType.Text)
                    {
                        // Always clip free Text to the object frame so border-drag
                        // WYSIWYG matches the selection (glyphs scale inside via
                        // CreateTextVisual frame-fit; they must not spill at full size).
                        var frameW = Math.Max(1, width);
                        var frameH = Math.Max(1, height);
                        border.ClipToBounds = true;
                        border.Clip = new RectangleGeometry(new Rect(0, 0, frameW, frameH));
                        textHost.Width = frameW;
                        textHost.Height = frameH;
                        textHost.PreviewVisual = CreateTextVisual(item, width, height);
                    }
                else if (border.Child is VisualPreviewHost textBoxHost && item.Type == ObjectType.TextBox)
                {
                    // Object bounds hug text after AutoFit; still clip so glyphs
                    // never paint outside the (tight) object frame.
                    var frameW = Math.Max(1, width);
                    var frameH = Math.Max(1, height);
                    border.ClipToBounds = true;
                    border.Clip = new RectangleGeometry(new Rect(0, 0, frameW, frameH));
                    textBoxHost.Width = frameW;
                    textBoxHost.Height = frameH;
                    textBoxHost.PreviewVisual = CreateTextBoxVisual(item, width, height);
                }
                else if (border.Child is Image image && item.Type == ObjectType.Image)
                {
                    image.Source = CreatePictureImageSource(item);
                }
                else if (border.Child is Grid barcodePanel
                    && item.Type is ObjectType.BarcodeCode128 or ObjectType.QRCode or ObjectType.DataMatrix)
                {
                    UpdateBarcodePanel(item, barcodePanel);
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

        if (_groupResizeAdorner is not null && _selectedObjects.Contains(item))
        {
            _groupResizeAdorner.InvalidateMeasure();
            _groupResizeAdorner.InvalidateArrange();
            _groupResizeAdorner.InvalidateVisual();
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
            // Text owns its measured bounds.  Rendering must not resize the visual element,
            // otherwise the selection frame and printed layout can diverge.
            // Content-owned AutoFit is only for static Text. TextBox frame is
            // user-owned (drag/properties): WidthMm/HeightMm changes reflow
            // wrap/clip via UpdateObjectElement so text stays fit to the frame.
            if (ShouldApplyTextAutoSize(e.PropertyName)
                && ShouldAutoSizeTextObject(item)
                && !_textAutoSizingObjects.Contains(item))
            {
                TryFitTextObjectToContent(item);
            }

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

            if (IsTransformOnlyProperty(e.PropertyName))
            {
                UpdateObjectTransformElement(item);
            }
            else
            {
                UpdateObjectElement(item);
            }

            if (!IsTransformGestureActive)
            {
                UpdateCanvasExtent();
            }
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
            if (ShouldAutoSizeTextObject(item)
                && !_textAutoSizingObjects.Contains(item))
            {
                TryFitTextObjectToContent(item);
            }

            // Style edits (font, alignment, padding, overflow policy, colors)
            // change the rendered text visual, not just its host position. The
            // transform-only path is reserved for X/Y/line-endpoint hot ticks;
            // using it here leaves the canvas showing stale text until a later
            // unrelated rebuild/zoom. TextBox size edits reflow here too.
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

    private double GetPixelsPerDip()
    {
        return IsLoaded
            ? PointerFrameTelemetry.NormalizePixelsPerDip(VisualTreeHelper.GetDpi(this).PixelsPerDip)
            : 1.0;
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
        return item.IsSquare2DCodeLike();
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

    private static bool ShouldApplyTextAutoSize(string? propertyName)
    {
        // Static Text only. TextBox width/height changes reflow via
        // UpdateObjectElement without mutating model size from content.
        return propertyName is nameof(LabelObject.Text)
            or nameof(LabelObject.BindingExpression)
            or nameof(LabelObject.Type);
    }

    private static void OnTemplateChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (e.OldValue is LabelTemplate oldTemplate)
        {
            oldTemplate.Objects.CollectionChanged -= ((LabelDesignerCanvas)d).ObjectsOnCollectionChanged;
            oldTemplate.Guides.CollectionChanged -= ((LabelDesignerCanvas)d).GuidesOnCollectionChanged;
            ((LabelDesignerCanvas)d).UnobserveGuides(oldTemplate);
        }

        if (e.NewValue is LabelTemplate newTemplate)
        {
            newTemplate.Objects.CollectionChanged += ((LabelDesignerCanvas)d).ObjectsOnCollectionChanged;
            newTemplate.Guides.CollectionChanged += ((LabelDesignerCanvas)d).GuidesOnCollectionChanged;
            ((LabelDesignerCanvas)d).ObserveGuides(newTemplate);
        }

        ((LabelDesignerCanvas)d).Rebuild();
    }

    private void ObjectsOnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.Action == NotifyCollectionChangedAction.Add && e.NewItems is not null)
        {
            foreach (var item in e.NewItems.OfType<LabelObject>()
                         .Where(ShouldAutoSizeTextObject))
            {
                TryFitTextObjectToContent(item);
            }
        }

        ReconcileObjectCollection(e);
    }

    /// <summary>
    /// Reconciles collection mutations without rebuilding every WPF visual.
    /// Reset/template replacement still takes the explicit rebuild path, but
    /// normal add/remove/replace/move operations retain existing image/text
    /// hosts, selection IDs and active key-object identity.
    /// </summary>
    private void ReconcileObjectCollection(NotifyCollectionChangedEventArgs e)
    {
        if (e.Action == NotifyCollectionChangedAction.Reset)
        {
            Rebuild();
            return;
        }

        var selectedIds = _selectedObjects
            .Select(item => item.Id)
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .ToHashSet(StringComparer.Ordinal);

        RemoveAllSelectionAdorners();

        if (e.OldItems is not null
            && e.Action is NotifyCollectionChangedAction.Remove or NotifyCollectionChangedAction.Replace)
        {
            foreach (var oldItem in e.OldItems.OfType<LabelObject>())
            {
                oldItem.PropertyChanged -= ObjectOnPropertyChanged;
                oldItem.Style.PropertyChanged -= StyleOnPropertyChanged;
                if (_objectElements.Remove(oldItem, out var element))
                {
                    Children.Remove(element);
                }

                _selectedObjects.Remove(oldItem);
                if (ReferenceEquals(SelectedObject, oldItem))
                {
                    SelectedObject = null;
                }
            }
        }

        if (e.NewItems is not null
            && e.Action is NotifyCollectionChangedAction.Add or NotifyCollectionChangedAction.Replace)
        {
            foreach (var newItem in e.NewItems.OfType<LabelObject>())
            {
                newItem.PropertyChanged -= ObjectOnPropertyChanged;
                newItem.PropertyChanged += ObjectOnPropertyChanged;
                newItem.Style.PropertyChanged -= StyleOnPropertyChanged;
                newItem.Style.PropertyChanged += StyleOnPropertyChanged;
                AddObjectElement(newItem);
            }
        }

        // A replace with a stable ID is a common undo/load path. Restore the
        // old selection by ID rather than forcing the user to reselect it.
        if (e.Action == NotifyCollectionChangedAction.Replace && e.NewItems is not null)
        {
            foreach (var newItem in e.NewItems.OfType<LabelObject>())
            {
                if (!string.IsNullOrWhiteSpace(newItem.Id)
                    && selectedIds.Contains(newItem.Id))
                {
                    _selectedObjects.Add(newItem);
                    SelectedObject = newItem;
                }
            }
        }

        foreach (var item in _objectElements.Keys.ToArray())
        {
            UpdateObjectElement(item);
        }

        if (SelectedObject is null && _selectedObjects.Count > 0)
        {
            SelectedObject = _selectedObjects.LastOrDefault();
        }

        UpdateCanvasExtent();
        RefreshSelectionAdorner();
        InvalidateVisual();
    }

    private void GuidesOnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.Action == NotifyCollectionChangedAction.Remove && e.OldItems is not null)
        {
            foreach (var guide in e.OldItems.OfType<LabelGuide>())
            {
                UnobserveGuide(guide);
                if (_persistentGuideVisuals.TryGetValue(guide, out var visuals))
                {
                    Children.Remove(visuals.Line);
                    Children.Remove(visuals.Label);
                    _persistentGuideVisuals.Remove(guide);
                }
            }
        }

        if (e.Action == NotifyCollectionChangedAction.Reset)
        {
            Rebuild();
            return;
        }

        if (e.NewItems is not null)
        {
            foreach (var guide in e.NewItems.OfType<LabelGuide>())
            {
                ObserveGuide(guide);
                CreatePersistentGuideVisual(guide);
            }
        }

        InvalidateVisual();
    }

    private void ObserveGuides(LabelTemplate template)
    {
        foreach (var guide in template.Guides)
        {
            ObserveGuide(guide);
        }
    }

    private void UnobserveGuides(LabelTemplate template)
    {
        foreach (var guide in template.Guides)
        {
            UnobserveGuide(guide);
        }
    }

    private void ObserveGuide(LabelGuide guide)
    {
        guide.PropertyChanged -= PersistentGuideOnPropertyChanged;
        guide.PropertyChanged += PersistentGuideOnPropertyChanged;
    }

    private void UnobserveGuide(LabelGuide guide)
    {
        guide.PropertyChanged -= PersistentGuideOnPropertyChanged;
    }

    private void PersistentGuideOnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (sender is LabelGuide guide)
        {
            UpdatePersistentGuideVisual(guide);
            InvalidateVisual();
        }
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
            // SelectedObject is two-way bound to the shell view model.  A
            // command-driven selection can therefore arrive without passing
            // through the pointer handlers that normally populate the
            // internal multi-selection set.  Add a genuinely new key object
            // as a single selection; pointer-driven additive selection already
            // contains it and is left intact.
            if (!canvas._selectedObjects.Contains(newObject))
            {
                canvas._selectedObjects.Clear();
                canvas._selectedObjects.Add(newObject);
            }
            canvas.UpdateObjectElement(newObject);
            canvas.RefreshSelectionAdorner();
        }
        else if (canvas._selectedObjects.Count == 0)
        {
            canvas.RemoveAllSelectionAdorners();
        }
        else
        {
            // Collection reconciliation can briefly clear the key property
            // while other selected objects remain.  Keep that set alive until
            // the replacement key is restored instead of dropping the group.
            canvas.RefreshSelectionAdorner();
        }
    }

    private static void OnZoomChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        ((LabelDesignerCanvas)d).RefreshForZoom();
    }

    private static void OnPointerTelemetryVisibilityChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var canvas = (LabelDesignerCanvas)d;
        canvas._pointerTelemetryMenuItem.IsChecked = (bool)e.NewValue;
        canvas.InvalidateVisual();
    }

    /// <summary>
    /// Zoom is a viewport concern, not a document/scene mutation. Reposition
    /// existing visuals and adorners in-place so a zoom slider cannot lose the
    /// current multi-selection, key object, mouse gesture or image/text host.
    /// </summary>
    private void RefreshForZoom()
    {
        UpdateCanvasExtent();
        foreach (var item in _objectElements.Keys.ToArray())
        {
            UpdateObjectElement(item);
        }

        if (Template is not null)
        {
            foreach (var guide in Template.Guides)
            {
                UpdatePersistentGuideVisual(guide);
            }
        }

        if (_lastAlignmentSnap is AlignmentSnapResult snap)
        {
            ShowAlignmentGuides(snap);
        }

        _selectionAdorner?.InvalidateMeasure();
        _selectionAdorner?.InvalidateArrange();
        _selectionAdorner?.InvalidateVisual();
        _groupResizeAdorner?.InvalidateMeasure();
        _groupResizeAdorner?.InvalidateArrange();
        _groupResizeAdorner?.InvalidateVisual();
        InvalidateVisual();
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
        canvas._selectedObjects.Clear();
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
            canvas.SaveDesignerPreferences();
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

    private static void OnGridPreferenceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var canvas = (LabelDesignerCanvas)d;
        if (e.Property == GridStepMmProperty)
        {
            var normalized = SnapGridContract.NormalizeStep((double)e.NewValue);
            if (Math.Abs(normalized - (double)e.NewValue) > 0.0001)
            {
                canvas.SetCurrentValue(GridStepMmProperty, normalized);
            }
        }

        canvas._snapGridMenuItem.IsChecked = canvas.IsSnapToGridEnabled;
        canvas._gridStepMenuItem.IsEnabled = canvas.IsSnapToGridEnabled;
        canvas.UpdateGridStepMenu();
        canvas.InvalidateVisual();
        canvas.InteractionStatusText = canvas.IsSnapToGridEnabled
            ? $"Snap to {canvas.GridStepMm:0.##} mm grid enabled (hold Alt to bypass)"
            : "Snap to grid disabled";
        try
        {
            canvas.SaveDesignerPreferences();
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

    private void SaveDesignerPreferences()
    {
        _designerPreferencesService.Save(new DesignerPreferences
        {
            SnapToObjects = IsSnapToObjectsEnabled,
            SnapToGrid = IsSnapToGridEnabled,
            GridStepMm = GridStepMm
        });
    }

    private void UpdateGridStepMenu()
    {
        foreach (var item in _gridStepMenuItem.Items.OfType<MenuItem>())
        {
            item.IsChecked = item.Tag is double step
                && Math.Abs(step - GridStepMm) < 0.0001;
        }
    }

    private void CanvasMouseButtonDown(object sender, MouseButtonEventArgs e)
    {
        Focus();
        CommitNudgeGesture();
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
            ClearSnapLocks();
            HideAlignmentGuides();
            NotifyEditGestureCanceled();
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

        if (_drawingObject is not null)
        {
            CancelDrawingObject();
        }

        // Resize thumbs own mouse capture and report both successful and
        // canceled gestures through DragCompleted. LostMouseCapture also
        // bubbles here on an ordinary mouse-up, so canceling a resize from
        // this canvas event would restore its start frame after every drag.
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
                ClearSnapLocks();
                HideAlignmentGuides();
                NotifyEditGestureCanceled();
                ReleaseMouseCapture();
                e.Handled = true;
                return;
            }

            if (e.Key == Key.Escape && _nudgeGestureActive)
            {
                CancelNudgeGesture();
                e.Handled = true;
                return;
            }

            if (e.Key == Key.Delete)
            {
                CancelNudgeGesture();
                if (DeleteSelection())
                {
                    e.Handled = true;
                    return;
                }
            }

            if (TryMoveSelectionWithArrowKey(e.Key, Keyboard.Modifiers))
            {
                e.Handled = true;
                return;
            }

            if ((Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control && e.Key == Key.C)
            {
                CommitNudgeGesture();
                e.Handled = CopySelection();
                return;
            }

            if ((Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control && e.Key == Key.V)
            {
                CommitNudgeGesture();
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

        ClearSnapLocks();
        _drawingStartMm = SnapDrawingPoint(dipPoint);
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
        NotifyEditGestureStarted();
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

        var endMm = SnapDrawingPoint(dipPoint);
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
        NotifyEditGestureCompleted();
        _drawingObject = null;
        _dimensionBuffer = string.Empty;
        DrawingCommandText = string.Empty;
        DrawingTool = null;
        ClearSnapLocks();
        HideAlignmentGuides();
        ReleaseMouseCapture();
        Cursor = Cursors.Arrow;
    }

    private void CancelDrawingObject()
    {
        var hadDrawingObject = _drawingObject is not null;
        if (_drawingObject is not null && Template is not null)
        {
            Template.Objects.Remove(_drawingObject);
        }

        _drawingObject = null;
        if (hadDrawingObject)
        {
            NotifyEditGestureCanceled();
        }
        _dimensionBuffer = string.Empty;
        DrawingCommandText = string.Empty;
        ClearSnapLocks();
        HideAlignmentGuides();
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
        RemoveAllSelectionAdorners();
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
            if (pair.Key.IsVisible && selectionRect.IntersectsWith(GetObjectBounds(pair.Key)))
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
        RefreshSelectionAdorner();

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
        var keyPen = new Pen(new SolidColorBrush(Color.FromRgb(15, 23, 42)), 2.0);

        foreach (var item in _selectedObjects)
        {
            dc.DrawRectangle(fill, ReferenceEquals(item, SelectedObject) && _selectedObjects.Count > 1 ? keyPen : pen, InflateRect(GetObjectBounds(item), 3));
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
        RemoveAllSelectionAdorners();
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
        _clipboardObjects.AddRange(items.Select(LabelObjectCloner.Clone));
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
        var pasted = _clipboardObjects.Select(LabelObjectCloner.Clone).ToArray();
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
        RefreshSelectionAdorner();

        InvalidateVisual();
        return true;
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
            var lineBounds = LineBoundsContract.GetBounds(item);
            return new Rect(
                new Point(lineBounds.Left, lineBounds.Top),
                new Point(lineBounds.Right, lineBounds.Bottom));
        }

        var transformed = TransformedBoundsContract.GetBounds(item);
        return new Rect(
            transformed.Left,
            transformed.Top,
            transformed.Width,
            transformed.Height);
    }

    private bool TryMoveSelectionWithArrowKey(Key key, ModifierKeys modifiers)
    {
        var direction = key switch
        {
            Key.Left => NudgeDirection.Left,
            Key.Up => NudgeDirection.Up,
            Key.Right => NudgeDirection.Right,
            Key.Down => NudgeDirection.Down,
            _ => (NudgeDirection?)null
        };
        if (direction is null)
        {
            return false;
        }

        var mode = (modifiers & ModifierKeys.Alt) == ModifierKeys.Alt
            ? NudgeStepMode.Fine
            : (modifiers & ModifierKeys.Shift) == ModifierKeys.Shift
                ? NudgeStepMode.Coarse
                : NudgeStepMode.Standard;
        return NudgeSelectedObjects(direction.Value, mode);
    }

    /// <summary>
    /// Applies one physical-mm keyboard nudge. Repeated calls within the idle
    /// window share one history transaction; unlike pointer movement, keyboard
    /// nudges do not acquire object/grid snap candidates.
    /// </summary>
    public bool NudgeSelectedObjects(NudgeDirection direction, NudgeStepMode mode)
    {
        var selectedItems = GetKeyboardSelection();
        if (Template is null || selectedItems.Count == 0)
        {
            return false;
        }

        if (!selectedItems.Any(item => !item.IsLocked))
        {
            InteractionStatusText = "Unlock at least one selected object before nudging.";
            return false;
        }

        if (!_nudgeGestureActive)
        {
            _nudgeStarts.Clear();
            foreach (var item in selectedItems.Where(item => !item.IsLocked))
            {
                _nudgeStarts[item] = CaptureObjectPosition(item);
            }

            _nudgeGestureActive = true;
            NotifyEditGestureStarted();
        }

        var before = _selectedObjects
            .Where(item => !item.IsLocked)
            .ToDictionary(item => item, CaptureObjectPosition);
        if (before.Count == 0)
        {
            before = selectedItems
                .Where(item => !item.IsLocked)
                .ToDictionary(item => item, CaptureObjectPosition);
        }
        var (deltaX, deltaY) = NudgeStepContract.ResolveDelta(direction, mode);
        CaptureGroupDragStarts(selectedItems);
        MoveSelectedGroup(deltaX, deltaY, allowSnap: false);
        _groupDragStarts.Clear();
        ClearSnapLocks();
        HideAlignmentGuides();
        InvalidateVisual();

        var changed = before.Any(pair => HasPositionChanged(pair.Key, pair.Value));
        if (!changed)
        {
            RestartNudgeGestureTimer();
            return false;
        }

        var stepMm = NudgeStepContract.ResolveStepMm(mode);
        InteractionStatusText = selectedItems.Count == 1
            ? $"{selectedItems[0].Name}: X {selectedItems[0].XMm:0.##} mm, Y {selectedItems[0].YMm:0.##} mm (nudge {stepMm:0.##} mm)"
            : $"Moved {selectedItems.Count} objects by {stepMm:0.##} mm";
        RestartNudgeGestureTimer();
        return true;
    }

    public void CommitNudgeGesture()
    {
        if (!_nudgeGestureActive)
        {
            return;
        }

        StopNudgeGestureTimer();
        _nudgeGestureActive = false;
        _nudgeStarts.Clear();
        _groupDragStarts.Clear();
        NotifyEditGestureCompleted();
    }

    public void CancelNudgeGesture()
    {
        if (!_nudgeGestureActive)
        {
            return;
        }

        StopNudgeGestureTimer();
        foreach (var pair in _nudgeStarts)
        {
            RestoreObjectPosition(pair.Key, pair.Value);
            UpdateObjectElement(pair.Key);
        }

        _nudgeGestureActive = false;
        _nudgeStarts.Clear();
        _groupDragStarts.Clear();
        ClearSnapLocks();
        HideAlignmentGuides();
        InvalidateVisual();
        NotifyEditGestureCanceled();
        InteractionStatusText = "Keyboard nudge cancelled.";
    }

    private void RestartNudgeGestureTimer()
    {
        _nudgeGestureTimer ??= new System.Windows.Threading.DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(300)
        };
        _nudgeGestureTimer.Stop();
        _nudgeGestureTimer.Tick -= NudgeGestureTimerOnTick;
        _nudgeGestureTimer.Tick += NudgeGestureTimerOnTick;
        _nudgeGestureTimer.Start();
    }

    private void StopNudgeGestureTimer()
    {
        if (_nudgeGestureTimer is null)
        {
            return;
        }

        _nudgeGestureTimer.Stop();
        _nudgeGestureTimer.Tick -= NudgeGestureTimerOnTick;
        _nudgeGestureTimer = null;
    }

    private void NudgeGestureTimerOnTick(object? sender, EventArgs e)
    {
        CommitNudgeGesture();
    }

    private void CanvasLostKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
    {
        // A property editor or another window must never inherit a half-open
        // nudge transaction. Restore the exact pre-nudge geometry instead.
        CancelNudgeGesture();
    }

    private static (double X, double Y, double EndX, double EndY) CaptureObjectPosition(LabelObject item)
    {
        // Preserve the raw endpoint fields so cancelling a nudge does not
        // materialize implicit line endpoints into the serialized template.
        return (item.XMm, item.YMm, item.LineEndXMm, item.LineEndYMm);
    }

    private static bool HasPositionChanged(LabelObject item, (double X, double Y, double EndX, double EndY) start)
    {
        return Math.Abs(item.XMm - start.X) > 0.000001
            || Math.Abs(item.YMm - start.Y) > 0.000001
            || item.Type == ObjectType.Line &&
                (Math.Abs(item.LineEndXMm - start.EndX) > 0.000001
                 || Math.Abs(item.LineEndYMm - start.EndY) > 0.000001);
    }

    private static void RestoreObjectPosition(LabelObject item, (double X, double Y, double EndX, double EndY) position)
    {
        item.XMm = position.X;
        item.YMm = position.Y;
        if (item.Type == ObjectType.Line)
        {
            item.LineEndXMm = position.EndX;
            item.LineEndYMm = position.EndY;
        }
    }

    private IReadOnlyList<LabelObject> GetKeyboardSelection()
    {
        return _selectedObjects.Count > 0
            ? _selectedObjects.ToArray()
            : SelectedObject is not null
                ? new[] { SelectedObject }
                : Array.Empty<LabelObject>();
    }

    private void CaptureGroupDragStarts(IEnumerable<LabelObject>? selection = null)
    {
        _groupDragStarts.Clear();
        foreach (var selected in (selection ?? _selectedObjects).Where(item => !item.IsLocked))
        {
            var endXMm = selected.LineEndXMm == 0 && selected.LineEndYMm == 0 ? selected.XMm + selected.WidthMm : selected.LineEndXMm;
            var endYMm = selected.LineEndXMm == 0 && selected.LineEndYMm == 0 ? selected.YMm + selected.HeightMm : selected.LineEndYMm;
            _groupDragStarts[selected] = (selected.XMm, selected.YMm, endXMm, endYMm);
        }
    }

    private AlignmentSnapResult MoveSingleLine(LabelObject item, double deltaXMm, double deltaYMm)
    {
        if (Template is null)
        {
            return new AlignmentSnapResult(null, null, new List<double>(), new List<double>());
        }

        var endXMm = _startLineEndXMm == 0 && _startLineEndYMm == 0
            ? _startXMm + item.WidthMm
            : _startLineEndXMm;
        var endYMm = _startLineEndXMm == 0 && _startLineEndYMm == 0
            ? _startYMm + item.HeightMm
            : _startLineEndYMm;
        var proposedX = _startXMm + deltaXMm;
        var proposedY = _startYMm + deltaYMm;

        // Lines participate in the same object/grid snap contract as every
        // other movable object. The old path only translated endpoints, so a
        // line could never acquire an edge, center, spacing, or grid target.
        var snap = new AlignmentSnapResult(null, null, new List<double>(), new List<double>());
        if ((IsSnapToObjectsEnabled || IsSnapToGridEnabled)
            && !Keyboard.IsKeyDown(Key.LeftAlt)
            && !Keyboard.IsKeyDown(Key.RightAlt))
        {
            snap = ComputePriorityAlignmentSnap(item, proposedX, proposedY);
            proposedX = snap.SnapX ?? proposedX;
            proposedY = snap.SnapY ?? proposedY;
        }
        else
        {
            ClearSnapLocks();
        }

        // Clamp the visible stroke hull, not just the mathematical endpoints,
        // so a thick line cannot be dragged half outside the label.
        var lineBounds = LineBoundsContract.GetBounds(
            _startXMm,
            _startYMm,
            endXMm,
            endYMm,
            item.Style.OutlineStyle,
            item.Style.BorderThicknessMm);
        var requestedDeltaX = proposedX - _startXMm;
        var requestedDeltaY = proposedY - _startYMm;
        var clampedDeltaX = Math.Max(-lineBounds.Left, Math.Min(Template.WidthMm - lineBounds.Right, requestedDeltaX));
        var clampedDeltaY = Math.Max(-lineBounds.Top, Math.Min(Template.HeightMm - lineBounds.Bottom, requestedDeltaY));
        item.XMm = _startXMm + clampedDeltaX;
        item.YMm = _startYMm + clampedDeltaY;
        item.LineEndXMm = endXMm + clampedDeltaX;
        item.LineEndYMm = endYMm + clampedDeltaY;
        return snap;
    }

    private void MoveSelectedGroup(double deltaXMm, double deltaYMm, bool allowSnap = true)
    {
        if (Template is null || _groupDragStarts.Count == 0)
        {
            return;
        }

        var snap = new AlignmentSnapResult(null, null, new List<double>(), new List<double>());
        if (allowSnap && (IsSnapToObjectsEnabled || IsSnapToGridEnabled)
            && !Keyboard.IsKeyDown(Key.LeftAlt)
            && !Keyboard.IsKeyDown(Key.RightAlt))
        {
            snap = ComputePriorityGroupAlignmentSnap(deltaXMm, deltaYMm);
            deltaXMm += snap.SnapX ?? 0;
            deltaYMm += snap.SnapY ?? 0;
        }
        else
        {
            ClearSnapLocks();
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

            UpdateObjectTransformElement(item);
        }

        if (snap.SnapX is not null || snap.SnapY is not null)
        {
            ShowAlignmentGuides(snap);
        }
        else
        {
            HideAlignmentGuides();
        }
    }

    private AlignmentSnapResult ComputeGroupAlignmentSnapLegacy(double proposedDeltaXMm, double proposedDeltaYMm)
    {
        if (Template is null || _groupDragStarts.Count == 0)
        {
            return new AlignmentSnapResult(null, null, new List<double>(), new List<double>());
        }

        var bounds = GetGroupBoundsMm();
        var left = bounds.Left + proposedDeltaXMm;
        var right = bounds.Right + proposedDeltaXMm;
        var centerX = (left + right) / 2;
        var top = bounds.Top + proposedDeltaYMm;
        var bottom = bounds.Bottom + proposedDeltaYMm;
        var centerY = (top + bottom) / 2;
        var selected = _groupDragStarts.Keys.ToHashSet();
        var snapX = (double?)null;
        var snapY = (double?)null;
        var bestDistX = double.MaxValue;
        var bestDistY = double.MaxValue;
        var guideX = new List<double>();
        var guideY = new List<double>();

        foreach (var other in Template.Objects.Where(item => item.IsVisible && !selected.Contains(item)))
        {
            var otherBounds = GetObjectBoundsMm(other);
            var otherCenterX = (otherBounds.Left + otherBounds.Right) / 2;
            var otherCenterY = (otherBounds.Top + otherBounds.Bottom) / 2;
            foreach (var source in new[] { left, right, centerX })
            {
                CheckSnap(source, otherBounds.Left, SnapThresholdMm, ref bestDistX, ref snapX, guideX, otherBounds.Left);
                CheckSnap(source, otherBounds.Right, SnapThresholdMm, ref bestDistX, ref snapX, guideX, otherBounds.Right);
                CheckSnap(source, otherCenterX, SnapThresholdMm, ref bestDistX, ref snapX, guideX, otherCenterX);
            }

            foreach (var source in new[] { top, bottom, centerY })
            {
                CheckSnap(source, otherBounds.Top, SnapThresholdMm, ref bestDistY, ref snapY, guideY, otherBounds.Top);
                CheckSnap(source, otherBounds.Bottom, SnapThresholdMm, ref bestDistY, ref snapY, guideY, otherBounds.Bottom);
                CheckSnap(source, otherCenterY, SnapThresholdMm, ref bestDistY, ref snapY, guideY, otherCenterY);
            }
        }

        CheckSnap(centerX, Template.WidthMm / 2, SnapThresholdMm, ref bestDistX, ref snapX, guideX, Template.WidthMm / 2);
        CheckSnap(centerY, Template.HeightMm / 2, SnapThresholdMm, ref bestDistY, ref snapY, guideY, Template.HeightMm / 2);
        var resolvedTargetX = ResolveSnapTarget(
            SnapPathKind.GroupMove,
            proposedDeltaXMm,
            snapX is null ? null : proposedDeltaXMm + snapX.Value,
            _snapLockX,
            guideX);
        var resolvedTargetY = ResolveSnapTarget(
            SnapPathKind.GroupMove,
            proposedDeltaYMm,
            snapY is null ? null : proposedDeltaYMm + snapY.Value,
            _snapLockY,
            guideY);
        return new AlignmentSnapResult(
            resolvedTargetX is null ? null : resolvedTargetX.Value - proposedDeltaXMm,
            resolvedTargetY is null ? null : resolvedTargetY.Value - proposedDeltaYMm,
            guideX,
            guideY);
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
                var lineBounds = LineBoundsContract.GetBounds(
                    start.X,
                    start.Y,
                    start.EndX,
                    start.EndY,
                    item.Style.OutlineStyle,
                    item.Style.BorderThicknessMm);
                left = Math.Min(left, lineBounds.Left);
                top = Math.Min(top, lineBounds.Top);
                right = Math.Max(right, lineBounds.Right);
                bottom = Math.Max(bottom, lineBounds.Bottom);
            }
            else
            {
                // Group move/snap/clamp must use the same transformed bounds
                // as selection arrange, resize and preflight.  Using the
                // authored width/height here makes a 90/270-degree object
                // report the wrong hull and can snap the whole group to a
                // target that the rendered selection does not occupy.
                var transformed = TransformedBoundsContract.GetBounds(
                    start.X,
                    start.Y,
                    item.WidthMm,
                    item.HeightMm,
                    item.Rotation);
                left = Math.Min(left, transformed.Left);
                top = Math.Min(top, transformed.Top);
                right = Math.Max(right, transformed.Right);
                bottom = Math.Max(bottom, transformed.Bottom);
            }
        }

        return new Rect(new Point(left, top), new Point(right, bottom));
    }

    private Rect GetObjectBounds(LabelObject item)
    {
        if (item.Type == ObjectType.Line)
        {
            var bounds = LineBoundsContract.GetBounds(item);
            return new Rect(
                MmToDip(bounds.Left),
                MmToDip(bounds.Top),
                Math.Max(0, MmToDip(bounds.Width)),
                Math.Max(0, MmToDip(bounds.Height)));
        }

        var transformed = TransformedBoundsContract.GetBounds(item);
        return new Rect(
            MmToDip(transformed.Left),
            MmToDip(transformed.Top),
            Math.Max(0, MmToDip(transformed.Width)),
            Math.Max(0, MmToDip(transformed.Height)));
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

        var step = SnapGridContract.NormalizeStep(GridStepMm);
        var xMm = SnapGridContract.Snap(DipToMm(dipPoint.X), step);
        var yMm = SnapGridContract.Snap(DipToMm(dipPoint.Y), step);
        return new Point(
            Math.Max(0, Math.Min(Template.WidthMm, xMm)),
            Math.Max(0, Math.Min(Template.HeightMm, yMm)));
    }

    /// <summary>
    /// Resolves a drawing endpoint through the same semantic candidate,
    /// tolerance and hysteresis policy used by move/resize.  Drawing used to
    /// consult only the grid, which made a freshly created line/rectangle miss
    /// an existing edge, center or persistent ruler guide by a few pixels.
    /// Typed dimensions still bypass this method deliberately: numeric input
    /// is an exact authoring instruction, not a pointer gesture.
    /// </summary>
    private Point SnapDrawingPoint(Point dipPoint)
    {
        if (Template is null)
        {
            return new Point();
        }

        var raw = new Point(DipToMm(dipPoint.X), DipToMm(dipPoint.Y));
        var bypass = Keyboard.IsKeyDown(Key.LeftAlt) || Keyboard.IsKeyDown(Key.RightAlt);
        if (bypass || (!IsSnapToObjectsEnabled && !IsSnapToGridEnabled))
        {
            ClearSnapLocks();
            HideAlignmentGuides();
            return ClampMm(raw);
        }

        var xCandidates = new List<SnapCandidate>();
        var yCandidates = new List<SnapCandidate>();

        void AddCandidate(
            List<SnapCandidate> target,
            double source,
            double position,
            int priority,
            string stableKey,
            string? caption = null)
        {
            if (!double.IsFinite(position))
            {
                return;
            }

            target.Add(new SnapCandidate(
                source,
                position,
                Math.Abs(source - position),
                priority,
                stableKey,
                caption));
        }

        if (IsSnapToObjectsEnabled)
        {
            foreach (var other in Template.Objects.Where(item =>
                         item.IsVisible && !ReferenceEquals(item, _drawingObject)))
            {
                var bounds = GetObjectBoundsMm(other);
                var stableKey = GetSnapStableKey(other);
                AddCandidate(xCandidates, raw.X, bounds.Left, 80, $"{stableKey}:draw:x:leading");
                AddCandidate(xCandidates, raw.X, (bounds.Left + bounds.Right) / 2, 65, $"{stableKey}:draw:x:center");
                AddCandidate(xCandidates, raw.X, bounds.Right, 80, $"{stableKey}:draw:x:trailing");
                AddCandidate(yCandidates, raw.Y, bounds.Top, 80, $"{stableKey}:draw:y:leading");
                AddCandidate(yCandidates, raw.Y, (bounds.Top + bounds.Bottom) / 2, 65, $"{stableKey}:draw:y:center");
                AddCandidate(yCandidates, raw.Y, bounds.Bottom, 80, $"{stableKey}:draw:y:trailing");
            }

            AddCandidate(xCandidates, raw.X, 0, 90, "canvas:edge:draw:x:leading", "artboard edge");
            AddCandidate(xCandidates, raw.X, Template.WidthMm / 2, 90, "canvas:center:draw:x", "artboard center");
            AddCandidate(xCandidates, raw.X, Template.WidthMm, 90, "canvas:edge:draw:x:trailing", "artboard edge");
            AddCandidate(yCandidates, raw.Y, 0, 90, "canvas:edge:draw:y:leading", "artboard edge");
            AddCandidate(yCandidates, raw.Y, Template.HeightMm / 2, 90, "canvas:center:draw:y", "artboard center");
            AddCandidate(yCandidates, raw.Y, Template.HeightMm, 90, "canvas:edge:draw:y:trailing", "artboard edge");

            foreach (var guide in Template.Guides.Where(item => item.IsVisible))
            {
                if (guide.Orientation == LabelGuideOrientation.Vertical)
                {
                    AddCandidate(xCandidates, raw.X, guide.PositionMm, 105, $"guide:{guide.Id}:draw:x", "guide");
                }
                else
                {
                    AddCandidate(yCandidates, raw.Y, guide.PositionMm, 105, $"guide:{guide.Id}:draw:y", "guide");
                }
            }
        }

        if (IsSnapToGridEnabled
            && SnapGridContract.TrySnap(raw.X, GridStepMm, SnapThresholdMm, out var gridX))
        {
            AddCandidate(xCandidates, raw.X, gridX, 30, $"grid:draw:x:{gridX:0.###}", $"grid {gridX:0.##} mm");
        }

        if (IsSnapToGridEnabled
            && SnapGridContract.TrySnap(raw.Y, GridStepMm, SnapThresholdMm, out var gridY))
        {
            AddCandidate(yCandidates, raw.Y, gridY, 30, $"grid:draw:y:{gridY:0.###}", $"grid {gridY:0.##} mm");
        }

        var winnerX = ChoosePathSnap(SnapPathKind.Draw, xCandidates);
        var winnerY = ChoosePathSnap(SnapPathKind.Draw, yCandidates);
        var guidesX = GetWinningGuides(xCandidates, winnerX);
        var guidesY = GetWinningGuides(yCandidates, winnerY);
        var resolvedX = ResolveSnapTarget(
            SnapPathKind.Draw,
            raw.X,
            winnerX is SnapCandidate x ? (double?)(raw.X + x.Delta) : null,
            _snapLockX,
            guidesX);
        var resolvedY = ResolveSnapTarget(
            SnapPathKind.Draw,
            raw.Y,
            winnerY is SnapCandidate y ? (double?)(raw.Y + y.Delta) : null,
            _snapLockY,
            guidesY);

        var snap = new AlignmentSnapResult(
            resolvedX,
            resolvedY,
            guidesX,
            guidesY,
            GetSnapCaption(winnerX),
            GetSnapCaption(winnerY));
        ShowAlignmentGuides(snap);
        return ClampMm(new Point(resolvedX ?? raw.X, resolvedY ?? raw.Y));
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
            heightDip,
            GetPixelsPerDip());
    }

    private DrawingVisual CreateTextVisual(LabelObject item, double widthDip, double heightDip)
    {
        // Free Text: measure natural glyphs on an unlimited frame, then force
        // HorizontalScale/VerticalScale so border-drag shrink always compresses
        // glyphs into the object frame (shared DrawTextLayout path with print).
        var visual = new DrawingVisual();
        using var dc = visual.RenderOpen();
        var previewPixelsPerDip = GetPixelsPerDip();
        var renderScale = Math.Max(0.01, Zoom);
        var previewWidthDip = Math.Max(1, widthDip / renderScale);
        var previewHeightDip = Math.Max(1, heightDip / renderScale);
        var displayText = string.IsNullOrEmpty(GetDisplayText(item)) ? " " : GetDisplayText(item);
        var brush = ParseBrush(item.Style.StrokeColor, Brushes.Black);
        var constrained = TextBoxOverflowDetector.ShouldConstrainToBox(item);
        const double naturalProbeDip = 10000;

        // Natural ink (no frame compress) — used to force designer scale even if
        // the framed CreateTextLayout path missed a compress case.
        var natural = TextBoxOverflowDetector.CreateTextLayout(
            item,
            displayText,
            naturalProbeDip,
            naturalProbeDip,
            constrainToBox: false,
            brush,
            previewPixelsPerDip);
        var naturalWidth = Math.Max(0.01, natural.Metrics.WidthDip);
        var naturalHeight = Math.Max(0.01, natural.Metrics.HeightDip);
        var contentWidth = TextBoxOverflowDetector.GetContentWidthDip(item, previewWidthDip, constrainToBox: false);
        var contentHeight = TextBoxOverflowDetector.GetContentHeightDip(item, previewHeightDip, constrainToBox: false);
        var fit = TextBoxOverflowDetector.ResolveTextFrameFitScale(
            naturalWidth,
            naturalHeight,
            contentWidth,
            contentHeight,
            natural.Metrics.LineHeightDip);

        // Framed layout for print-parity metrics (may also compress).
        var layout = TextBoxOverflowDetector.CreateTextLayout(
            item,
            displayText,
            previewWidthDip,
            previewHeightDip,
            constrainToBox: constrained,
            brush,
            previewPixelsPerDip);

        // Use the stronger compress of framed metrics vs natural-vs-frame fit.
        // Draw natural lines (full glyph runs) with forced scales so MaxTextWidth
        // on a tight frame cannot leave full-size glyphs unscaled.
        var scaleX = Math.Min(layout.Metrics.HorizontalScale, fit.ScaleX);
        var scaleY = Math.Min(layout.Metrics.VerticalScale, fit.ScaleY);
        scaleX = Math.Clamp(double.IsFinite(scaleX) ? scaleX : 1.0, 0.01, 1.0);
        scaleY = Math.Clamp(double.IsFinite(scaleY) ? scaleY : 1.0, 0.01, 1.0);
        var scaledHeight = naturalHeight * scaleY;
        var verticalOffset = TextBoxOverflowDetector.ResolveVerticalOffset(
            item,
            scaledHeight,
            previewHeightDip,
            constrainToBox: false);
        var drawLayout = new TextLayoutResult
        {
            Lines = natural.Lines,
            Metrics = natural.Metrics with
            {
                WidthDip = naturalWidth * scaleX,
                HeightDip = scaledHeight,
                ContentWidthDip = contentWidth,
                VerticalOffsetDip = verticalOffset,
                HorizontalScale = scaleX,
                VerticalScale = scaleY,
                HorizontalScaleAnchorFraction = layout.Metrics.HorizontalScaleAnchorFraction,
                EffectiveFontSizePt = layout.Metrics.EffectiveFontSizePt,
                InkExtentDip = natural.Metrics.InkExtentDip * scaleY,
                BaselineDip = natural.Metrics.BaselineDip * scaleY
            }
        };

        dc.PushTransform(new ScaleTransform(renderScale, renderScale));
        // Always clip free Text to the object frame (AutoFit frame ≈ natural so
        // clip is a no-op; after shrink, glyphs must not paint outside handles).
        dc.PushClip(new RectangleGeometry(new Rect(0, 0, previewWidthDip, previewHeightDip)));
        TextBoxOverflowDetector.DrawTextLayout(
            dc,
            drawLayout,
            new Point(
                TextBoxOverflowDetector.GetHorizontalOriginDip(item, constrainToBox: false),
                verticalOffset));
        dc.Pop();
        dc.Pop();
        return visual;
    }

    private DrawingVisual CreateTextBoxVisual(LabelObject item, double widthDip, double heightDip)
    {
        // TextBox is always constrained to the user-owned frame (drag/properties).
        // Layout must reflow to the current width/height every UpdateObjectElement
        // call so glyphs and object bounds stay fit together during resize.
        var visual = new DrawingVisual();
        using var dc = visual.RenderOpen();
        var previewPixelsPerDip = GetPixelsPerDip();
        var renderScale = Math.Max(0.01, Zoom);
        var previewWidthDip = Math.Max(1, widthDip / renderScale);
        var previewHeightDip = Math.Max(1, heightDip / renderScale);
        var brush = ParseBrush(item.Style.StrokeColor, Brushes.Black);
        const bool constrained = true;
        var originX = TextBoxOverflowDetector.GetHorizontalOriginDip(item, constrained);
        if (TextBoxOverflowDetector.HasExplicitLineHeight(item) || TextBoxOverflowDetector.UsesShrinkFont(item) || TextBoxOverflowDetector.UsesScaleWidth(item))
        {
            var layout = TextBoxOverflowDetector.CreateTextLayout(
                item,
                GetDisplayText(item),
                previewWidthDip,
                previewHeightDip,
                constrainToBox: constrained,
                brush,
                previewPixelsPerDip);
            dc.PushTransform(new ScaleTransform(renderScale, renderScale));
            dc.PushClip(new RectangleGeometry(new Rect(0, 0, previewWidthDip, previewHeightDip)));
            TextBoxOverflowDetector.DrawTextLayout(
                dc,
                layout,
                new Point(originX, layout.Metrics.VerticalOffsetDip));
            dc.Pop();
            dc.Pop();
            return visual;
        }

        var displayText = GetDisplayText(item);
        var wrapped = TextBoxOverflowDetector.WrapTextToBox(
            item,
            displayText,
            TextBoxOverflowDetector.GetContentWidthDip(item, previewWidthDip, constrained),
            previewPixelsPerDip);
        var text = TextBoxOverflowDetector.CreateFormattedText(
            item,
            wrapped,
            brush,
            previewPixelsPerDip);
        TextBoxOverflowDetector.ApplyLayoutBounds(text, item, previewWidthDip, previewHeightDip, constrained);
        var metrics = TextBoxOverflowDetector.Measure(
            text,
            item,
            previewWidthDip,
            previewHeightDip,
            constrained,
            sourceValue: displayText,
            pixelsPerDip: previewPixelsPerDip);
        dc.PushTransform(new ScaleTransform(renderScale, renderScale));
        dc.PushClip(new RectangleGeometry(new Rect(0, 0, previewWidthDip, previewHeightDip)));
        // Origin must include left padding so text stays inside the frame when
        // the user drags width/height — not Point(0,…) which shifted glyphs out.
        dc.DrawText(text, new Point(originX, metrics.VerticalOffsetDip));
        dc.Pop();
        dc.Pop();
        return visual;
    }

    private string? GetObjectError(LabelObject item)
    {
        return item.Type switch
        {
            ObjectType.Text when TextBoxOverflowDetector.ShouldBlockOverflow(item)
                && TextBoxOverflowDetector.ShouldConstrainToBox(item)
                && IsTextBoxOverflowing(item, MmConverter.MmToDip(item.WidthMm), MmConverter.MmToDip(item.HeightMm)) =>
                "Text exceeds the fixed text frame. Increase the frame or reduce text/font size.",
            ObjectType.TextBox when TextBoxOverflowDetector.ShouldBlockOverflow(item)
                && IsTextBoxOverflowing(item, MmConverter.MmToDip(item.WidthMm), MmConverter.MmToDip(item.HeightMm)) =>
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
            var planDpi = ResolvePrintPlanDpi(item);
            var productionWidthMm = LinearBarcodeProductionWidth.ResolveSymbolWidthMm(
                item, _barcodeRenderer, planDpi, data);
            var hriLayout = BarcodeHriTextLayout.Measure(
                type,
                data,
                productionWidthMm,
                item.HeightMm,
                item.BarcodeHriPlacement,
                item.BarcodeTextFontSizePt);
            if (!hriLayout.IsValid)
            {
                return hriLayout.ErrorMessage;
            }

            var symbolHeightMm = hriLayout.IsEnabled ? hriLayout.SymbolHeightMm : item.HeightMm;
            _barcodeRenderer.RenderBarcode(data, type, productionWidthMm, symbolHeightMm, item.QrDpi, CreateBarcodeRenderOptions(item));
            return null;
        }
        catch (Exception ex) when (ex is not OutOfMemoryException)
        {
            return $"{type} cannot be rendered: {ex.Message}";
        }
    }

    private void TryFitTextObjectToContent(LabelObject item)
    {
        // Static Text only. TextBox must never rewrite size from content — the
        // operator sets the frame by drag; text reflows inside that frame.
        if (!ShouldAutoSizeTextObject(item) || _textAutoSizingObjects.Contains(item))
        {
            return;
        }

        var displayText = string.IsNullOrEmpty(GetDisplayText(item)) ? " " : GetDisplayText(item);
        var (fitWidthMm, fitHeightMm) = TextBoxOverflowDetector.MeasureAutoFitFrameMm(
            item,
            displayText,
            pixelsPerDip: 1.0);

        _textAutoSizingObjects.Add(item);
        try
        {
            if (item.Type == ObjectType.Text
                && Math.Abs(item.WidthMm - fitWidthMm) > 0.005)
            {
                item.WidthMm = fitWidthMm;
            }

            if (Math.Abs(item.HeightMm - fitHeightMm) > 0.005)
            {
                item.HeightMm = fitHeightMm;
            }
        }
        finally
        {
            _textAutoSizingObjects.Remove(item);
        }
    }

    private static bool ShouldAutoSizeTextObject(LabelObject item)
        => item.Type == ObjectType.Text && item.Style.TextSizing == TextSizingMode.AutoFit;

    private void UpdateBarcodePanel(LabelObject item, Grid panel)
    {
        var image = panel.Children.OfType<Image>().FirstOrDefault();
        var hri = panel.Children.OfType<TextBlock>().FirstOrDefault();
        if (image is null || hri is null)
        {
            return;
        }

        var data = ResolveObjectData(item);
        var type = item.Type switch
        {
            ObjectType.QRCode => BarcodeType.QRCode,
            ObjectType.DataMatrix => BarcodeType.DataMatrix,
            _ => BarcodeTypeMapper.ToRendererType(item.BarcodeSymbology)
        };
        var planDpi = ResolvePrintPlanDpi(item);
        var productionWidthMm = LinearBarcodeProductionWidth.ResolveSymbolWidthMm(
            item, _barcodeRenderer, planDpi, data);
        var hriLayout = BarcodeHriTextLayout.Measure(
            type,
            data,
            productionWidthMm,
            item.HeightMm,
            item.BarcodeHriPlacement,
            item.BarcodeTextFontSizePt);

        image.Stretch = item.IsSquare2DCodeLike() || item.Type is ObjectType.QRCode or ObjectType.DataMatrix
            ? Stretch.Uniform
            : Stretch.Fill;
        image.HorizontalAlignment = HorizontalAlignment.Center;
        image.VerticalAlignment = VerticalAlignment.Center;
        image.Source = CreateBarcodeImageSource(item, hriLayout, productionWidthMm);
        if (hriLayout.IsEnabled && hriLayout.IsValid)
        {
            // Match print contract: Above puts HRI in row 0; Below keeps HRI under the bars.
            if (panel.RowDefinitions.Count >= 2)
            {
                if (hriLayout.Placement == Core.Enums.BarcodeHriPlacement.Above)
                {
                    panel.RowDefinitions[0].Height = GridLength.Auto;
                    panel.RowDefinitions[1].Height = new GridLength(1, GridUnitType.Star);
                    Grid.SetRow(hri, 0);
                    Grid.SetRow(image, 1);
                }
                else
                {
                    panel.RowDefinitions[0].Height = new GridLength(1, GridUnitType.Star);
                    panel.RowDefinitions[1].Height = GridLength.Auto;
                    Grid.SetRow(image, 0);
                    Grid.SetRow(hri, 1);
                }
            }

            var symbology = item.Type switch
            {
                ObjectType.QRCode => BarcodeSymbology.QRCode,
                ObjectType.DataMatrix => BarcodeSymbology.DataMatrix,
                _ => item.BarcodeSymbology
            };
            hri.Text = BarcodeCheckDigitContract.FormatHriText(
                symbology,
                data,
                item.BarcodeCheckDigitPolicy,
                item.BarcodeHriShowCheckDigit);
            hri.FontSize = item.BarcodeTextFontSizePt;
            hri.Height = MmToDip(hriLayout.HriHeightMm);
            hri.Visibility = Visibility.Visible;
        }
        else
        {
            if (panel.RowDefinitions.Count >= 2)
            {
                panel.RowDefinitions[0].Height = new GridLength(1, GridUnitType.Star);
                panel.RowDefinitions[1].Height = GridLength.Auto;
                Grid.SetRow(image, 0);
                Grid.SetRow(hri, 1);
            }

            hri.Text = string.Empty;
            hri.Height = 0;
            hri.Visibility = Visibility.Collapsed;
        }
    }

    private ImageSource? CreateBarcodeImageSource(LabelObject item, BarcodeHriLayout? measuredLayout = null, double? productionWidthMm = null)
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
            var planDpi = ResolvePrintPlanDpi(item);
            var widthMm = productionWidthMm
                ?? LinearBarcodeProductionWidth.ResolveSymbolWidthMm(item, _barcodeRenderer, planDpi, data);
            var hriLayout = measuredLayout ?? BarcodeHriTextLayout.Measure(
                type,
                data,
                widthMm,
                item.HeightMm,
                item.BarcodeHriPlacement,
                item.BarcodeTextFontSizePt);
            if (!hriLayout.IsValid)
            {
                return null;
            }

            var symbolHeightMm = hriLayout.IsEnabled ? hriLayout.SymbolHeightMm : item.HeightMm;
            var pixels = _barcodeRenderer.RenderBarcode(data, type, widthMm, symbolHeightMm, item.QrDpi, CreateBarcodeRenderOptions(item));
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

    private static ImageSource? CreatePictureImageSource(LabelObject item)
    {
        return ImageRasterizer.Decode(item.ImageDataBase64, item.ImageRasterMode);
    }

    private static BarcodeRenderOptions CreateBarcodeRenderOptions(LabelObject item)
    {
        return new BarcodeRenderOptions
        {
            ErrorCorrection = item.QrErrorCorrection.ToString(),
            QuietZoneModules = item.QrQuietZoneModules,
            IsGs1 = item.BarcodeApplicationProfile == BarcodeApplicationProfile.Gs1,
            Code39WideNarrowRatio = item.Code39WideNarrowRatio,
            BearerBarStyle = item.BearerBarStyle,
            BearerBarThicknessMm = item.BearerBarThicknessMm
        };
    }

    /// <summary>
    /// Same DPI priority as preflight / MainViewModel SizedFromX apply:
    /// PrinterProfile.Dpi → Template.Dpi → object QrDpi → 203.
    /// SizedFromX production width must not use design-only QrDpi alone.
    /// </summary>
    private int ResolvePrintPlanDpi(LabelObject item)
    {
        if (Template?.PrinterProfile is { Dpi: > 0 } profile)
        {
            return profile.Dpi;
        }

        if (Template is { Dpi: > 0 })
        {
            return Template.Dpi;
        }

        return item.QrDpi > 0 ? item.QrDpi : 203;
    }

    private bool TryApplyMatrixAutoSize(LabelObject item)
    {
        if (!IsMatrixBarcode(item) || Template is null)
        {
            return false;
        }

        var fitSizeMm = QrObjectGeometryContract.ResolveTargetSizeMm(
            item,
            ResolveObjectData(item),
            GetAvailableQrSizeMm(item));
        if (fitSizeMm is null)
        {
            return false;
        }

        if (!QrObjectGeometryContract.HasMeaningfulSizeDelta(item, fitSizeMm.Value))
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
        _selectionAdorner.ResizeStarted += (_, _) =>
        {
            CommitNudgeGesture();
            _singleResizeStart = CaptureGroupResizeSnapshot(item);
            _singleResizeActive = true;
            ClearSnapLocks();
            NotifyEditGestureStarted();
        };
        _selectionAdorner.ResizeCompleted += (_, _) =>
        {
            _singleResizeActive = false;
            ClearSnapLocks();
            HideAlignmentGuides();
            NotifyEditGestureCompleted();
        };
        _selectionAdorner.ResizeCanceled += (_, _) =>
        {
            RestoreSingleResizeStart(item);
            _singleResizeActive = false;
            ClearSnapLocks();
            HideAlignmentGuides();
            NotifyEditGestureCanceled();
        };
        _selectionAdorner.ResizeRequested += (_, delta) => ResizeSelectedObject(item, delta);
        _adornedObject = item;
        layer.Add(_selectionAdorner);
    }

    private void RefreshSelectionAdorner()
    {
        RemoveAllSelectionAdorners();
        if (_selectedObjects.Count == 1 && SelectedObject is not null)
        {
            ShowSelectionAdorner(SelectedObject);
        }
        else if (_selectedObjects.Count > 1)
        {
            var resizeItems = GetResizeSelection();
            if (resizeItems.Count > 1)
            {
                ShowGroupResizeAdorner();
            }
            else if (resizeItems.Count == 1)
            {
                ShowSelectionAdorner(resizeItems[0]);
            }
        }
    }

    private void ShowGroupResizeAdorner()
    {
        var resizeItems = GetResizeSelection();
        if (resizeItems.Count < 2 || Template is null)
        {
            RemoveGroupResizeAdorner();
            return;
        }

        var layer = AdornerLayer.GetAdornerLayer(this)
            ?? resizeItems.Select(item => _objectElements.TryGetValue(item, out var element)
                ? AdornerLayer.GetAdornerLayer(element)
                : null).FirstOrDefault(candidate => candidate is not null);
        if (layer is null)
        {
            return;
        }

        RemoveGroupResizeAdorner();
        _groupResizeStarts.Clear();
        foreach (var item in resizeItems)
        {
            _groupResizeStarts[item] = CaptureGroupResizeSnapshot(item);
        }

        _groupResizeAdorner = new SelectionResizeAdorner(this, GetSelectedGroupBoundsDip);
        _groupResizeAdorner.ResizeStarted += (_, _) =>
        {
            CommitNudgeGesture();
            CaptureGroupResizeStarts();
            ClearSnapLocks();
            NotifyEditGestureStarted();
        };
        _groupResizeAdorner.ResizeCompleted += (_, _) =>
        {
            _groupResizeStarts.Clear();
            ClearSnapLocks();
            HideAlignmentGuides();
            NotifyEditGestureCompleted();
        };
        _groupResizeAdorner.ResizeCanceled += (_, _) =>
        {
            RestoreGroupResizeStarts();
            _groupResizeStarts.Clear();
            ClearSnapLocks();
            HideAlignmentGuides();
            NotifyEditGestureCanceled();
        };
        _groupResizeAdorner.ResizeRequested += (_, delta) => ResizeSelectedGroup(delta);
        _groupResizeAdornerLayer = layer;
        layer.Add(_groupResizeAdorner);
    }

    private void CaptureGroupResizeStarts()
    {
        _groupResizeStarts.Clear();
        foreach (var item in GetResizeSelection())
        {
            _groupResizeStarts[item] = CaptureGroupResizeSnapshot(item);
        }
    }

    private IReadOnlyList<LabelObject> GetResizeSelection()
    {
        return _selectedObjects
            .Where(item => item.IsVisible && !item.IsLocked)
            .OrderBy(item => item.ZIndex)
            .ThenBy(item => item.Id, StringComparer.Ordinal)
            .ToArray();
    }

    private Rect GetSelectedGroupBoundsDip()
    {
        var bounds = GetSelectedGroupBoundsMm();
        return new Rect(
            MmToDip(bounds.Left),
            MmToDip(bounds.Top),
            Math.Max(0, MmToDip(bounds.Width)),
            Math.Max(0, MmToDip(bounds.Height)));
    }

    private Rect GetSelectedGroupBoundsMm()
    {
        var items = GetResizeSelection();
        if (items.Count == 0)
        {
            return Rect.Empty;
        }

        var bounds = items.Select(GetObjectBoundsMm).ToArray();
        return new Rect(
            new Point(bounds.Min(item => item.Left), bounds.Min(item => item.Top)),
            new Point(bounds.Max(item => item.Right), bounds.Max(item => item.Bottom)));
    }

    private static GroupResizeObjectSnapshot CaptureGroupResizeSnapshot(LabelObject item)
    {
        var endXMm = item.LineEndXMm == 0 && item.LineEndYMm == 0
            ? item.XMm + item.WidthMm
            : item.LineEndXMm;
        var endYMm = item.LineEndXMm == 0 && item.LineEndYMm == 0
            ? item.YMm + item.HeightMm
            : item.LineEndYMm;
        return new GroupResizeObjectSnapshot(
            item.XMm,
            item.YMm,
            item.WidthMm,
            item.HeightMm,
            endXMm,
            endYMm,
            item.Rotation);
    }

    private void RestoreGroupResizeStarts()
    {
        foreach (var pair in _groupResizeStarts)
        {
            var item = pair.Key;
            var start = pair.Value;
            item.XMm = start.XMm;
            item.YMm = start.YMm;
            item.WidthMm = start.WidthMm;
            item.HeightMm = start.HeightMm;
            if (item.Type == ObjectType.Line)
            {
                item.LineEndXMm = start.EndXMm;
                item.LineEndYMm = start.EndYMm;
            }

            UpdateObjectElement(item);
        }

        InvalidateVisual();
        _groupResizeAdorner?.InvalidateMeasure();
        _groupResizeAdorner?.InvalidateArrange();
        _groupResizeAdorner?.InvalidateVisual();
    }

    private void RestoreSingleResizeStart(LabelObject item)
    {
        if (!_singleResizeActive)
        {
            return;
        }

        var start = _singleResizeStart;

        item.XMm = start.XMm;
        item.YMm = start.YMm;
        item.WidthMm = start.WidthMm;
        item.HeightMm = start.HeightMm;
        if (item.Type == ObjectType.Line)
        {
            item.LineEndXMm = start.EndXMm;
            item.LineEndYMm = start.EndYMm;
        }

        UpdateObjectElement(item);
        InvalidateVisual();
        _selectionAdorner?.InvalidateMeasure();
        _selectionAdorner?.InvalidateArrange();
        _selectionAdorner?.InvalidateVisual();
    }

    private void RemoveAllSelectionAdorners()
    {
        RemoveSelectionAdorner();
        RemoveGroupResizeAdorner();
    }

    private void RemoveSelectionAdorner()
    {
        _selectionAdorner?.CancelResize();
        if (_selectionAdorner is null || _adornedObject is null)
        {
            _selectionAdorner = null;
            _adornedObject = null;
            _singleResizeActive = false;
            return;
        }

        if (_objectElements.TryGetValue(_adornedObject, out var element))
        {
            AdornerLayer.GetAdornerLayer(element)?.Remove(_selectionAdorner);
        }

        _selectionAdorner = null;
        _adornedObject = null;
        _singleResizeActive = false;
    }

    private void RemoveGroupResizeAdorner()
    {
        if (_groupResizeAdorner is not null)
        {
            _groupResizeAdorner.CancelResize();
            _groupResizeAdornerLayer?.Remove(_groupResizeAdorner);
            AdornerLayer.GetAdornerLayer(this)?.Remove(_groupResizeAdorner);
        }

        _groupResizeAdorner = null;
        _groupResizeAdornerLayer = null;
        _groupResizeStarts.Clear();
    }

    private void ResizeSelectedGroup(ResizeDelta delta)
    {
        if (Template is null || _groupResizeStarts.Count < 2)
        {
            return;
        }

        var source = GetGroupResizeSourceBounds();
        var deltaXMm = DipToMm(delta.DeltaX);
        var deltaYMm = DipToMm(delta.DeltaY);
        var deltaWidthMm = DipToMm(delta.DeltaWidth);
        var deltaHeightMm = DipToMm(delta.DeltaHeight);
        if (!GroupResizeGeometryContract.TryResize(
                source,
                deltaXMm,
                deltaYMm,
                deltaWidthMm,
                deltaHeightMm,
                minimumWidthMm: 1,
                minimumHeightMm: 1,
                out var target))
        {
            return;
        }

        var modifierFlags = GetResizeModifierFlags(delta);
        if (modifierFlags != ResizeModifierFlags.None)
        {
            target = ResizeModifierContract.Apply(
                source,
                target,
                delta.Handle,
                modifierFlags,
                minimumWidthMm: 1,
                minimumHeightMm: 1);
        }

        target = GroupResizeGeometryContract.ClampToCanvas(
            target,
            Template.WidthMm,
            Template.HeightMm,
            minimumWidthMm: 1,
            minimumHeightMm: 1);

        var resizeSnap = new AlignmentSnapResult(null, null, new List<double>(), new List<double>());
        if ((IsSnapToObjectsEnabled || IsSnapToGridEnabled)
            && !delta.DisableSnapping
            && !Keyboard.IsKeyDown(Key.LeftAlt)
            && !Keyboard.IsKeyDown(Key.RightAlt))
        {
            var selected = _groupResizeStarts.Keys.ToHashSet();
            var movesLeft = Math.Abs(deltaXMm) > 0.0001 && Math.Abs(deltaWidthMm) > 0.0001;
            var movesTop = Math.Abs(deltaYMm) > 0.0001 && Math.Abs(deltaHeightMm) > 0.0001;
            if (movesLeft || Math.Abs(deltaWidthMm) > 0.0001)
            {
                var leading = movesLeft;
                var edge = leading ? target.XMm : target.RightMm;
                var sourceAnchor = leading ? SnapAnchorKind.Leading : SnapAnchorKind.Trailing;
                var edgeSnap = ComputePriorityGroupResizeEdgeSnap(edge, horizontal: true, sourceAnchor, selected);
                if (edgeSnap.Delta is not null)
                {
                    target = ShiftGroupResizeEdge(target, horizontal: true, leading, edgeSnap.Delta.Value);
                    resizeSnap = resizeSnap with
                    {
                        SnapX = edgeSnap.Delta,
                        GuideXPositions = edgeSnap.Guides
                    };
                }
            }

            if (movesTop || Math.Abs(deltaHeightMm) > 0.0001)
            {
                var leading = movesTop;
                var edge = leading ? target.YMm : target.BottomMm;
                var sourceAnchor = leading ? SnapAnchorKind.Leading : SnapAnchorKind.Trailing;
                var edgeSnap = ComputePriorityGroupResizeEdgeSnap(edge, horizontal: false, sourceAnchor, selected);
                if (edgeSnap.Delta is not null)
                {
                    target = ShiftGroupResizeEdge(target, horizontal: false, leading, edgeSnap.Delta.Value);
                    resizeSnap = resizeSnap with
                    {
                        SnapY = edgeSnap.Delta,
                        GuideYPositions = edgeSnap.Guides
                    };
                }
            }
        }
        else
        {
            ClearSnapLocks();
        }

        // Snapping is a visual aid, while an explicit modifier is an
        // authoring invariant.  Re-apply the invariant after an edge target
        // is chosen so a snapped group cannot silently lose its aspect ratio
        // or centre anchor.  Any guide that no longer describes the final
        // frame is removed below rather than showing stale alignment.
        if (modifierFlags != ResizeModifierFlags.None)
        {
            var adjusted = ResizeModifierContract.Apply(
                source,
                target,
                delta.Handle,
                modifierFlags,
                minimumWidthMm: 1,
                minimumHeightMm: 1);
            if (!FramesEqual(target, adjusted))
            {
                resizeSnap = new AlignmentSnapResult(null, null, new List<double>(), new List<double>());
            }

            target = adjusted;
        }

        target = GroupResizeGeometryContract.ClampToCanvas(
            target,
            Template.WidthMm,
            Template.HeightMm,
            minimumWidthMm: 1,
            minimumHeightMm: 1);
        var transform = new GroupResizeTransform(source, target);
        foreach (var pair in _groupResizeStarts)
        {
            ApplyGroupResizeTransform(pair.Key, pair.Value, source, transform);
        }

        InvalidateVisual();
        _groupResizeAdorner?.InvalidateMeasure();
        _groupResizeAdorner?.InvalidateArrange();
        _groupResizeAdorner?.InvalidateVisual();
        if (resizeSnap.SnapX is not null || resizeSnap.SnapY is not null)
        {
            ShowAlignmentGuides(resizeSnap);
        }
        else
        {
            HideAlignmentGuides();
        }
    }

    private GroupResizeFrame GetGroupResizeSourceBounds()
    {
        var bounds = _groupResizeStarts
            .Select(pair => GetGroupResizeSnapshotBounds(pair.Key, pair.Value))
            .ToArray();
        return new GroupResizeFrame(
            bounds.Min(item => item.Left),
            bounds.Min(item => item.Top),
            bounds.Max(item => item.Right) - bounds.Min(item => item.Left),
            bounds.Max(item => item.Bottom) - bounds.Min(item => item.Top));
    }

    private static LabelLayoutBounds GetGroupResizeSnapshotBounds(
        LabelObject item,
        GroupResizeObjectSnapshot snapshot)
    {
        if (item.Type == ObjectType.Line)
        {
            return LineBoundsContract.GetBounds(
                snapshot.XMm,
                snapshot.YMm,
                snapshot.EndXMm,
                snapshot.EndYMm,
                item.Style.OutlineStyle,
                item.Style.BorderThicknessMm);
        }

        return TransformedBoundsContract.GetBounds(
            snapshot.XMm,
            snapshot.YMm,
            snapshot.WidthMm,
            snapshot.HeightMm,
            snapshot.Rotation);
    }

    private static GroupResizeFrame ShiftGroupResizeEdge(
        GroupResizeFrame frame,
        bool horizontal,
        bool leading,
        double delta)
    {
        if (!double.IsFinite(delta))
        {
            return frame;
        }

        if (horizontal)
        {
            var width = leading ? frame.WidthMm - delta : frame.WidthMm + delta;
            return width < 1
                ? frame
                : leading
                    ? frame with { XMm = frame.XMm + delta, WidthMm = width }
                    : frame with { WidthMm = width };
        }

        var height = leading ? frame.HeightMm - delta : frame.HeightMm + delta;
        return height < 1
            ? frame
            : leading
                ? frame with { YMm = frame.YMm + delta, HeightMm = height }
                : frame with { HeightMm = height };
    }

    private static void ApplyGroupResizeTransform(
        LabelObject item,
        GroupResizeObjectSnapshot snapshot,
        GroupResizeFrame source,
        GroupResizeTransform transform)
    {
        if (item.Type == ObjectType.Line)
        {
            item.XMm = transform.MapX(snapshot.XMm);
            item.YMm = transform.MapY(snapshot.YMm);
            item.LineEndXMm = transform.MapX(snapshot.EndXMm);
            item.LineEndYMm = transform.MapY(snapshot.EndYMm);
            item.WidthMm = Math.Max(0.5, Math.Abs(item.LineEndXMm - item.XMm));
            item.HeightMm = Math.Max(0.5, Math.Abs(item.LineEndYMm - item.YMm));
            return;
        }

        var bounds = TransformedBoundsContract.GetBounds(
            snapshot.XMm,
            snapshot.YMm,
            snapshot.WidthMm,
            snapshot.HeightMm,
            snapshot.Rotation);
        var transformed = GroupResizeGeometryContract.MapBounds(transform, bounds);
        var authored = GroupResizeGeometryContract.ToAuthoredFrame(transformed, snapshot.Rotation);
        if (item.Type == ObjectType.Text
            && item.Style.TextSizing == TextSizingMode.AutoFit
            && (Math.Abs(authored.WidthMm - snapshot.WidthMm) > 0.01
                || Math.Abs(authored.HeightMm - snapshot.HeightMm) > 0.01))
        {
            item.Style.TextSizing = TextSizingMode.FixedFrame;
        }

        item.XMm = authored.XMm;
        item.YMm = authored.YMm;
        item.WidthMm = authored.WidthMm;
        item.HeightMm = authored.HeightMm;
    }

    private (double? Delta, List<double> Guides) ComputePriorityGroupResizeEdgeSnap(
        double edge,
        bool horizontal,
        SnapAnchorKind sourceAnchor,
        IReadOnlySet<LabelObject> selected)
    {
        var candidates = new List<SnapCandidate>();
        if (IsSnapToObjectsEnabled)
        {
            foreach (var other in Template!.Objects.Where(item => !selected.Contains(item) && item.IsVisible))
            {
                var bounds = GetObjectBoundsMm(other);
                var targets = horizontal
                    ? new[]
                    {
                        (SnapAnchorKind.Leading, bounds.Left),
                        (SnapAnchorKind.Center, (bounds.Left + bounds.Right) / 2),
                        (SnapAnchorKind.Trailing, bounds.Right)
                    }
                    : new[]
                    {
                        (SnapAnchorKind.Leading, bounds.Top),
                        (SnapAnchorKind.Center, (bounds.Top + bounds.Bottom) / 2),
                        (SnapAnchorKind.Trailing, bounds.Bottom)
                    };
                var stableKey = GetSnapStableKey(other);
                foreach (var target in targets)
                {
                    candidates.Add(new SnapCandidate(
                        edge,
                        target.Item2,
                        Math.Abs(edge - target.Item2),
                        GetSnapPriority(sourceAnchor, target.Item1),
                        $"{stableKey}:group-resize:{(horizontal ? 'x' : 'y')}:{sourceAnchor}:{target.Item1}"));
                }
            }

            var canvasTarget = horizontal ? Template.WidthMm / 2 : Template.HeightMm / 2;
            candidates.Add(new SnapCandidate(
                edge,
                canvasTarget,
                Math.Abs(edge - canvasTarget),
                90,
                $"canvas:center:group-resize:{(horizontal ? 'x' : 'y')}"));
        }

        if (IsSnapToGridEnabled
            && SnapGridContract.TrySnap(edge, GridStepMm, SnapThresholdMm, out var gridTarget))
        {
            candidates.Add(new SnapCandidate(
                edge,
                gridTarget,
                Math.Abs(edge - gridTarget),
                GetSnapPriority(sourceAnchor, SnapAnchorKind.Grid),
                $"grid:group-resize:{(horizontal ? 'x' : 'y')}:{gridTarget:0.###}"));
        }

        var winner = ChoosePathSnap(SnapPathKind.Resize, candidates);
        var guides = GetWinningGuides(candidates, winner);
        var state = horizontal ? _snapLockX : _snapLockY;
        var resolvedTarget = ResolveSnapTarget(
            SnapPathKind.Resize,
            edge,
            winner is SnapCandidate selectedWinner ? (double?)(edge + selectedWinner.Delta) : null,
            state,
            guides);
        return (resolvedTarget is null ? null : resolvedTarget.Value - edge, guides);
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

        var modifierFlags = GetResizeModifierFlags(delta);
        if (modifierFlags != ResizeModifierFlags.None)
        {
            var adjusted = ResizeModifierContract.Apply(
                new ResizeFrame(item.XMm, item.YMm, item.WidthMm, item.HeightMm),
                new ResizeFrame(newX, newY, newWidth, newHeight),
                delta.Handle,
                modifierFlags,
                minimumWidthMm: minSizeMm,
                minimumHeightMm: minSizeMm);
            newX = adjusted.XMm;
            newY = adjusted.YMm;
            newWidth = adjusted.WidthMm;
            newHeight = adjusted.HeightMm;
        }

        var resizeSnap = new AlignmentSnapResult(null, null, new List<double>(), new List<double>());
        if ((IsSnapToObjectsEnabled || IsSnapToGridEnabled)
            && !delta.DisableSnapping
            && !Keyboard.IsKeyDown(Key.LeftAlt)
            && !Keyboard.IsKeyDown(Key.RightAlt))
        {
            var movingLeft = Math.Abs(deltaXMm) > 0.0001;
            var movingTop = Math.Abs(deltaYMm) > 0.0001;
            var frame = new ResizeFrame(newX, newY, newWidth, newHeight);
            if (movingLeft || Math.Abs(deltaWidthMm) > 0.0001)
            {
                var localEdge = movingLeft ? ResizeEdge.Left : ResizeEdge.Right;
                var worldEdge = ResizeGeometryContract.MapToWorldEdge(item.Rotation, localEdge);
                var horizontal = worldEdge is TransformedBoundsEdge.Left or TransformedBoundsEdge.Right;
                var edge = ResizeGeometryContract.GetWorldEdgePosition(frame, item.Rotation, localEdge);
                var sourceAnchor = worldEdge is TransformedBoundsEdge.Left or TransformedBoundsEdge.Top
                    ? SnapAnchorKind.Leading
                    : SnapAnchorKind.Trailing;
                var edgeSnap = ComputePriorityResizeEdgeSnap(item, edge, horizontal, sourceAnchor);
                if (edgeSnap.Delta is not null)
                {
                    frame = ResizeGeometryContract.ApplyWorldEdgeSnap(
                        frame,
                        item.Rotation,
                        localEdge,
                        edge + edgeSnap.Delta.Value,
                        minSizeMm);

                    resizeSnap = horizontal
                        ? resizeSnap with
                        {
                            SnapX = edgeSnap.Delta,
                            GuideXPositions = edgeSnap.Guides
                        }
                        : resizeSnap with
                        {
                            SnapY = edgeSnap.Delta,
                            GuideYPositions = edgeSnap.Guides
                        };
                }
            }

            if (movingTop || Math.Abs(deltaHeightMm) > 0.0001)
            {
                var localEdge = movingTop ? ResizeEdge.Top : ResizeEdge.Bottom;
                var worldEdge = ResizeGeometryContract.MapToWorldEdge(item.Rotation, localEdge);
                var horizontal = worldEdge is TransformedBoundsEdge.Left or TransformedBoundsEdge.Right;
                var edge = ResizeGeometryContract.GetWorldEdgePosition(frame, item.Rotation, localEdge);
                var sourceAnchor = worldEdge is TransformedBoundsEdge.Left or TransformedBoundsEdge.Top
                    ? SnapAnchorKind.Leading
                    : SnapAnchorKind.Trailing;
                var edgeSnap = ComputePriorityResizeEdgeSnap(item, edge, horizontal, sourceAnchor);
                if (edgeSnap.Delta is not null)
                {
                    frame = ResizeGeometryContract.ApplyWorldEdgeSnap(
                        frame,
                        item.Rotation,
                        localEdge,
                        edge + edgeSnap.Delta.Value,
                        minSizeMm);

                    resizeSnap = horizontal
                        ? resizeSnap with
                        {
                            SnapX = edgeSnap.Delta,
                            GuideXPositions = edgeSnap.Guides
                        }
                        : resizeSnap with
                        {
                            SnapY = edgeSnap.Delta,
                            GuideYPositions = edgeSnap.Guides
                        };
                }
            }

            newX = frame.XMm;
            newY = frame.YMm;
            newWidth = frame.WidthMm;
            newHeight = frame.HeightMm;
        }

        else
        {
            // A temporary Alt bypass must also release the previous target;
            // otherwise the hysteresis lock could reappear on the next tick.
            ClearSnapLocks();
        }

        if (modifierFlags != ResizeModifierFlags.None)
        {
            var adjusted = ResizeModifierContract.Apply(
                new ResizeFrame(item.XMm, item.YMm, item.WidthMm, item.HeightMm),
                new ResizeFrame(newX, newY, newWidth, newHeight),
                delta.Handle,
                modifierFlags,
                minimumWidthMm: minSizeMm,
                minimumHeightMm: minSizeMm);
            if (!FramesEqual(new ResizeFrame(newX, newY, newWidth, newHeight), adjusted))
            {
                resizeSnap = new AlignmentSnapResult(null, null, new List<double>(), new List<double>());
            }

            newX = adjusted.XMm;
            newY = adjusted.YMm;
            newWidth = adjusted.WidthMm;
            newHeight = adjusted.HeightMm;
        }

        newX = Math.Max(0, Math.Min(Template.WidthMm - minSizeMm, newX));
        newY = Math.Max(0, Math.Min(Template.HeightMm - minSizeMm, newY));
        newWidth = Math.Max(minSizeMm, Math.Min(Template.WidthMm - newX, newWidth));
        newHeight = Math.Max(minSizeMm, Math.Min(Template.HeightMm - newY, newHeight));

        // Free Text: NiceLabel does not allow manual Text size edits (size follows
        // font). ANLAbel allows border-drag WYSIWYG: lock the selection frame so
        // AutoFit cannot re-expand it, and glyph frame-fit compress tracks the
        // border (shared CreateTextLayout path). Still not TextBox ownership.
        if (item.Type == ObjectType.Text
            && item.Style.TextSizing == TextSizingMode.AutoFit
            && (Math.Abs(newWidth - item.WidthMm) > 0.01 || Math.Abs(newHeight - item.HeightMm) > 0.01))
        {
            item.Style.TextSizing = TextSizingMode.FixedFrame;
        }

        item.XMm = newX;
        item.YMm = newY;
        item.WidthMm = newWidth;
        item.HeightMm = newHeight;
        UpdateObjectElement(item);
        _selectionAdorner?.InvalidateArrange();
        _selectionAdorner?.InvalidateVisual();
        if (resizeSnap.SnapX is not null || resizeSnap.SnapY is not null)
        {
            ShowAlignmentGuides(resizeSnap);
        }
        else
        {
            HideAlignmentGuides();
        }
    }

    private static ResizeModifierFlags GetResizeModifierFlags(ResizeDelta delta)
    {
        var flags = ResizeModifierFlags.None;
        if (delta.PreserveAspectRatio)
        {
            flags |= ResizeModifierFlags.PreserveAspectRatio;
        }

        if (delta.ResizeFromCenter)
        {
            flags |= ResizeModifierFlags.ResizeFromCenter;
        }

        return flags;
    }

    private static bool FramesEqual(ResizeFrame left, ResizeFrame right)
    {
        return Math.Abs(left.XMm - right.XMm) <= 0.000001
            && Math.Abs(left.YMm - right.YMm) <= 0.000001
            && Math.Abs(left.WidthMm - right.WidthMm) <= 0.000001
            && Math.Abs(left.HeightMm - right.HeightMm) <= 0.000001;
    }

    private static bool FramesEqual(GroupResizeFrame left, GroupResizeFrame right)
    {
        return Math.Abs(left.XMm - right.XMm) <= 0.000001
            && Math.Abs(left.YMm - right.YMm) <= 0.000001
            && Math.Abs(left.WidthMm - right.WidthMm) <= 0.000001
            && Math.Abs(left.HeightMm - right.HeightMm) <= 0.000001;
    }

    private (double? Delta, List<double> Guides) ComputePriorityResizeEdgeSnap(LabelObject dragged, double edge, bool horizontal, SnapAnchorKind sourceAnchor)
    {
        var candidates = new List<SnapCandidate>();
        if (IsSnapToObjectsEnabled)
        {
            foreach (var other in Template!.Objects.Where(item => !ReferenceEquals(item, dragged) && item.IsVisible))
            {
                var bounds = GetObjectBoundsMm(other);
                var targets = horizontal
                    ? new[]
                    {
                        (SnapAnchorKind.Leading, bounds.Left),
                        (SnapAnchorKind.Center, (bounds.Left + bounds.Right) / 2),
                        (SnapAnchorKind.Trailing, bounds.Right)
                    }
                    : new[]
                    {
                        (SnapAnchorKind.Leading, bounds.Top),
                        (SnapAnchorKind.Center, (bounds.Top + bounds.Bottom) / 2),
                        (SnapAnchorKind.Trailing, bounds.Bottom)
                    };
                var stableKey = GetSnapStableKey(other);
                foreach (var target in targets)
                {
                    candidates.Add(new SnapCandidate(
                        edge,
                        target.Item2,
                        Math.Abs(edge - target.Item2),
                        GetSnapPriority(sourceAnchor, target.Item1),
                        $"{stableKey}:resize:{(horizontal ? 'x' : 'y')}:{sourceAnchor}:{target.Item1}"));
                }
            }

            var canvasTarget = horizontal ? Template!.WidthMm / 2 : Template.HeightMm / 2;
            candidates.Add(new SnapCandidate(edge, canvasTarget, Math.Abs(edge - canvasTarget), 90, $"canvas:center:resize:{(horizontal ? 'x' : 'y')}"));
        }
        if (IsSnapToGridEnabled
            && SnapGridContract.TrySnap(edge, GridStepMm, SnapThresholdMm, out var gridTarget))
        {
            candidates.Add(new SnapCandidate(
                edge,
                gridTarget,
                Math.Abs(edge - gridTarget),
                GetSnapPriority(sourceAnchor, SnapAnchorKind.Grid),
                $"grid:resize:{(horizontal ? 'x' : 'y')}:{gridTarget:0.###}"));
        }
        var winner = ChoosePathSnap(SnapPathKind.Resize, candidates);
        var guides = GetWinningGuides(candidates, winner);
        var state = horizontal ? _snapLockX : _snapLockY;
        var resolvedTarget = ResolveSnapTarget(
            SnapPathKind.Resize,
            edge,
            winner is SnapCandidate selected ? (double?)(edge + selected.Delta) : null,
            state,
            guides);
        return (resolvedTarget is null ? null : resolvedTarget.Value - edge, guides);
    }

    private (double? Delta, List<double> Guides) ComputeResizeEdgeSnapLegacy(LabelObject dragged, double edge, bool horizontal)
    {
        var bestDistance = double.MaxValue;
        var bestDelta = (double?)null;
        var guides = new List<double>();
        foreach (var other in Template!.Objects.Where(item => !ReferenceEquals(item, dragged) && item.IsVisible))
        {
            var bounds = GetObjectBoundsMm(other);
            var targets = horizontal
                ? new[] { bounds.Left, bounds.Right, (bounds.Left + bounds.Right) / 2 }
                : new[] { bounds.Top, bounds.Bottom, (bounds.Top + bounds.Bottom) / 2 };
            foreach (var target in targets)
            {
                CheckSnap(edge, target, SnapThresholdMm, ref bestDistance, ref bestDelta, guides, target);
            }
        }

        var canvasTarget = horizontal ? Template!.WidthMm / 2 : Template.HeightMm / 2;
        CheckSnap(edge, canvasTarget, SnapThresholdMm, ref bestDistance, ref bestDelta, guides, canvasTarget);
        var state = horizontal ? _snapLockX : _snapLockY;
        var resolvedTarget = ResolveSnapTarget(
            SnapPathKind.Resize,
            edge,
            bestDelta is null ? null : edge + bestDelta.Value,
            state,
            guides);
        return (resolvedTarget is null ? null : resolvedTarget.Value - edge, guides);
    }

    // ==================== Alignment Guide System ====================

    private readonly record struct AlignmentSnapResult(
        double? SnapX,
        double? SnapY,
        List<double> GuideXPositions,
        List<double> GuideYPositions,
        string? XCaption = null,
        string? YCaption = null);

    private enum SnapAnchorKind
    {
        Leading,
        Center,
        Trailing,
        Baseline,
        Grid,
        Spacing
    }

    private static int GetSnapPriority(SnapAnchorKind source, SnapAnchorKind target)
    {
        if (source == SnapAnchorKind.Spacing || target == SnapAnchorKind.Spacing)
        {
            return 40;
        }

        if (source == SnapAnchorKind.Grid || target == SnapAnchorKind.Grid)
        {
            return 30;
        }

        if (source == SnapAnchorKind.Baseline || target == SnapAnchorKind.Baseline)
        {
            return source == SnapAnchorKind.Baseline && target == SnapAnchorKind.Baseline
                ? 110
                : 55;
        }

        if (source == target)
        {
            return 85;
        }

        if (source is SnapAnchorKind.Leading or SnapAnchorKind.Trailing
            && target is SnapAnchorKind.Leading or SnapAnchorKind.Trailing)
        {
            return 80;
        }

        return 65;
    }

    private string GetSnapStableKey(LabelObject item)
    {
        var index = Template?.Objects.IndexOf(item) ?? -1;
        return $"{item.Id}|{item.Name}|{index:D6}";
    }

    private static bool SupportsBaselineSnap(LabelObject item)
    {
        return item.Type is ObjectType.Text or ObjectType.TextBox;
    }

    private void AddBaselineSource(List<(SnapAnchorKind Kind, double Position)> anchors, LabelObject item, double proposedY)
    {
        if (!SupportsBaselineSnap(item))
        {
            return;
        }

        var baselineOffset = GetTextBaselineMm(item) - item.YMm;
        if (double.IsFinite(baselineOffset))
        {
            anchors.Add((SnapAnchorKind.Baseline, proposedY + baselineOffset));
        }
    }

    private void AddBaselineTarget(List<(SnapAnchorKind Kind, double Position)> anchors, LabelObject item)
    {
        if (!SupportsBaselineSnap(item))
        {
            return;
        }

        var baseline = GetTextBaselineMm(item);
        if (double.IsFinite(baseline))
        {
            anchors.Add((SnapAnchorKind.Baseline, baseline));
        }
    }

    private void AddGridCandidates(
        List<SnapCandidate> xCandidates,
        List<SnapCandidate> yCandidates,
        IEnumerable<(SnapAnchorKind Kind, double Position)> sourceX,
        IEnumerable<(SnapAnchorKind Kind, double Position)> sourceY,
        string stablePrefix)
    {
        if (!IsSnapToGridEnabled)
        {
            return;
        }

        foreach (var source in sourceX)
        {
            if (SnapGridContract.TrySnap(source.Position, GridStepMm, SnapThresholdMm, out var target))
            {
                xCandidates.Add(new SnapCandidate(
                    source.Position,
                    target,
                    Math.Abs(target - source.Position),
                    GetSnapPriority(source.Kind, SnapAnchorKind.Grid),
                    $"{stablePrefix}:grid:x:{source.Kind}:{target:0.###}",
                    $"grid {target:0.##} mm"));
            }
        }

        foreach (var source in sourceY)
        {
            if (SnapGridContract.TrySnap(source.Position, GridStepMm, SnapThresholdMm, out var target))
            {
                yCandidates.Add(new SnapCandidate(
                    source.Position,
                    target,
                    Math.Abs(target - source.Position),
                    GetSnapPriority(source.Kind, SnapAnchorKind.Grid),
                    $"{stablePrefix}:grid:y:{source.Kind}:{target:0.###}",
                    $"grid {target:0.##} mm"));
            }
        }
    }

    private void AddSmartSpacingCandidates(
        List<SnapCandidate> xCandidates,
        List<SnapCandidate> yCandidates,
        double proposedLeft,
        double proposedRight,
        double proposedTop,
        double proposedBottom,
        double objectWidth,
        double objectHeight,
        IReadOnlySet<LabelObject> moving,
        string stablePrefix)
    {
        if (Template is null)
        {
            return;
        }

        var intervalsX = Template.Objects
            .Where(item => item.IsVisible && !moving.Contains(item))
            .Select(item =>
            {
                var bounds = GetObjectBoundsMm(item);
                return new SpacingInterval(bounds.Left, bounds.Right, GetSnapStableKey(item));
            });
        foreach (var gap in SmartSpacingContract.GetAdjacentGaps(intervalsX))
        {
            foreach (var targetLeading in SmartSpacingContract.CandidateLeadingPositions(objectWidth, gap))
            {
                xCandidates.Add(new SnapCandidate(
                    proposedLeft,
                    targetLeading,
                    Math.Abs(targetLeading - proposedLeft),
                    GetSnapPriority(SnapAnchorKind.Spacing, SnapAnchorKind.Spacing),
                    $"{stablePrefix}:spacing:x:{gap.BeforeKey}:{gap.AfterKey}:{targetLeading:0.###}",
                    $"gap {gap.Gap:0.##} mm"));
                xCandidates.Add(new SnapCandidate(
                    proposedRight,
                    targetLeading + objectWidth,
                    Math.Abs(targetLeading + objectWidth - proposedRight),
                    GetSnapPriority(SnapAnchorKind.Spacing, SnapAnchorKind.Spacing),
                    $"{stablePrefix}:spacing:x-trailing:{gap.BeforeKey}:{gap.AfterKey}:{targetLeading:0.###}",
                    $"gap {gap.Gap:0.##} mm"));
            }
        }

        var intervalsY = Template.Objects
            .Where(item => item.IsVisible && !moving.Contains(item))
            .Select(item =>
            {
                var bounds = GetObjectBoundsMm(item);
                return new SpacingInterval(bounds.Top, bounds.Bottom, GetSnapStableKey(item));
            });
        foreach (var gap in SmartSpacingContract.GetAdjacentGaps(intervalsY))
        {
            foreach (var targetLeading in SmartSpacingContract.CandidateLeadingPositions(objectHeight, gap))
            {
                yCandidates.Add(new SnapCandidate(
                    proposedTop,
                    targetLeading,
                    Math.Abs(targetLeading - proposedTop),
                    GetSnapPriority(SnapAnchorKind.Spacing, SnapAnchorKind.Spacing),
                    $"{stablePrefix}:spacing:y:{gap.BeforeKey}:{gap.AfterKey}:{targetLeading:0.###}",
                    $"gap {gap.Gap:0.##} mm"));
                yCandidates.Add(new SnapCandidate(
                    proposedBottom,
                    targetLeading + objectHeight,
                    Math.Abs(targetLeading + objectHeight - proposedBottom),
                    GetSnapPriority(SnapAnchorKind.Spacing, SnapAnchorKind.Spacing),
                    $"{stablePrefix}:spacing:y-trailing:{gap.BeforeKey}:{gap.AfterKey}:{targetLeading:0.###}",
                    $"gap {gap.Gap:0.##} mm"));
            }
        }
    }

    private static List<double> GetWinningGuides(IEnumerable<SnapCandidate> candidates, SnapCandidate? winner)
    {
        if (winner is not SnapCandidate selected)
        {
            return new List<double>();
        }

        return candidates
            .Where(candidate => candidate.Priority == selected.Priority
                && Math.Abs(candidate.Distance - selected.Distance) < 0.01)
            .Select(candidate => candidate.TargetPosition)
            .Distinct()
            .ToList();
    }

    private static string? GetSnapCaption(SnapCandidate? candidate)
    {
        if (candidate is not SnapCandidate selected)
        {
            return null;
        }

        if (!string.IsNullOrWhiteSpace(selected.Label))
        {
            return selected.Label;
        }

        var key = selected.StableKey ?? string.Empty;
        if (key.Contains("Baseline", StringComparison.OrdinalIgnoreCase))
        {
            return "baseline";
        }

        if (key.Contains("canvas:center", StringComparison.OrdinalIgnoreCase))
        {
            return "canvas center";
        }

        if (key.Contains(":Center", StringComparison.OrdinalIgnoreCase))
        {
            return "center";
        }

        if (key.Contains(":Leading", StringComparison.OrdinalIgnoreCase)
            || key.Contains(":Trailing", StringComparison.OrdinalIgnoreCase))
        {
            return "edge";
        }

        return null;
    }

    private AlignmentSnapResult ComputePriorityAlignmentSnap(LabelObject dragged, double proposedXMm, double proposedYMm)
    {
        if (Template is null)
        {
            return new AlignmentSnapResult(null, null, new List<double>(), new List<double>());
        }

        var xCandidates = new List<SnapCandidate>();
        var yCandidates = new List<SnapCandidate>();
        var draggedBounds = GetObjectBoundsMm(dragged);
        var sourceX = new[]
        {
            (SnapAnchorKind.Leading, proposedXMm + (draggedBounds.Left - dragged.XMm)),
            (SnapAnchorKind.Center, proposedXMm + ((draggedBounds.Left + draggedBounds.Right) / 2 - dragged.XMm)),
            (SnapAnchorKind.Trailing, proposedXMm + (draggedBounds.Right - dragged.XMm))
        };
        var sourceY = new List<(SnapAnchorKind Kind, double Position)>
        {
            (SnapAnchorKind.Leading, proposedYMm + (draggedBounds.Top - dragged.YMm)),
            (SnapAnchorKind.Center, proposedYMm + ((draggedBounds.Top + draggedBounds.Bottom) / 2 - dragged.YMm)),
            (SnapAnchorKind.Trailing, proposedYMm + (draggedBounds.Bottom - dragged.YMm))
        };
        AddBaselineSource(sourceY, dragged, proposedYMm);

        if (IsSnapToObjectsEnabled)
        {
            foreach (var other in Template.Objects.Where(item => !ReferenceEquals(item, dragged) && item.IsVisible))
            {
                var bounds = GetObjectBoundsMm(other);
                var stableKey = GetSnapStableKey(other);
                var targetsX = new[]
                {
                    (SnapAnchorKind.Leading, bounds.Left),
                    (SnapAnchorKind.Center, (bounds.Left + bounds.Right) / 2),
                    (SnapAnchorKind.Trailing, bounds.Right)
                };
                var targetsY = new List<(SnapAnchorKind Kind, double Position)>
                {
                    (SnapAnchorKind.Leading, bounds.Top),
                    (SnapAnchorKind.Center, (bounds.Top + bounds.Bottom) / 2),
                    (SnapAnchorKind.Trailing, bounds.Bottom)
                };
                AddBaselineTarget(targetsY, other);

                foreach (var source in sourceX)
                {
                    foreach (var target in targetsX)
                    {
                        var distance = Math.Abs(source.Item2 - target.Item2);
                        xCandidates.Add(new SnapCandidate(
                            source.Item2,
                            target.Item2,
                            distance,
                            GetSnapPriority(source.Item1, target.Item1),
                            $"{stableKey}:x:{source.Item1}:{target.Item1}"));
                    }
                }

                foreach (var source in sourceY)
                {
                    foreach (var target in targetsY)
                    {
                        var distance = Math.Abs(source.Item2 - target.Item2);
                        yCandidates.Add(new SnapCandidate(
                            source.Item2,
                            target.Item2,
                            distance,
                            GetSnapPriority(source.Item1, target.Item1),
                            $"{stableKey}:y:{source.Item1}:{target.Item1}"));
                    }
                }
            }
        }

        AddGridCandidates(xCandidates, yCandidates, sourceX, sourceY, $"{GetSnapStableKey(dragged)}:single");
        if (IsSnapToObjectsEnabled)
        {
            AddSmartSpacingCandidates(
                xCandidates,
                yCandidates,
                sourceX.First(source => source.Item1 == SnapAnchorKind.Leading).Item2,
                sourceX.First(source => source.Item1 == SnapAnchorKind.Trailing).Item2,
                sourceY.First(source => source.Item1 == SnapAnchorKind.Leading).Position,
                sourceY.First(source => source.Item1 == SnapAnchorKind.Trailing).Position,
                draggedBounds.Width,
                draggedBounds.Height,
                new HashSet<LabelObject> { dragged },
                $"{GetSnapStableKey(dragged)}:single");
        }

        if (IsSnapToObjectsEnabled)
        {
            var canvasCenterX = Template.WidthMm / 2;
            var canvasCenterY = Template.HeightMm / 2;
            var centerSourceX = sourceX.First(source => source.Item1 == SnapAnchorKind.Center).Item2;
            var centerSourceY = sourceY.First(source => source.Item1 == SnapAnchorKind.Center).Item2;
            xCandidates.Add(new SnapCandidate(centerSourceX, canvasCenterX, Math.Abs(centerSourceX - canvasCenterX), 90, "canvas:center:x"));
            yCandidates.Add(new SnapCandidate(centerSourceY, canvasCenterY, Math.Abs(centerSourceY - canvasCenterY), 90, "canvas:center:y"));
        }

        var winnerX = ChoosePathSnap(SnapPathKind.SingleMove, xCandidates);
        var winnerY = ChoosePathSnap(SnapPathKind.SingleMove, yCandidates);
        var guideXPositions = GetWinningGuides(xCandidates, winnerX);
        var guideYPositions = GetWinningGuides(yCandidates, winnerY);
        var finalSnapX = winnerX is SnapCandidate x ? (double?)(proposedXMm + x.Delta) : null;
        var finalSnapY = winnerY is SnapCandidate y ? (double?)(proposedYMm + y.Delta) : null;
        finalSnapX = ResolveSnapTarget(SnapPathKind.SingleMove, proposedXMm, finalSnapX, _snapLockX, guideXPositions);
        finalSnapY = ResolveSnapTarget(SnapPathKind.SingleMove, proposedYMm, finalSnapY, _snapLockY, guideYPositions);
        return new AlignmentSnapResult(
            finalSnapX,
            finalSnapY,
            guideXPositions,
            guideYPositions,
            GetSnapCaption(winnerX),
            GetSnapCaption(winnerY));
    }

    private AlignmentSnapResult ComputePriorityGroupAlignmentSnap(double proposedDeltaXMm, double proposedDeltaYMm)
    {
        if (Template is null || _groupDragStarts.Count == 0)
        {
            return new AlignmentSnapResult(null, null, new List<double>(), new List<double>());
        }

        var bounds = GetGroupBoundsMm();
        var sourceX = new[]
        {
            (SnapAnchorKind.Leading, bounds.Left + proposedDeltaXMm),
            (SnapAnchorKind.Center, (bounds.Left + bounds.Right) / 2 + proposedDeltaXMm),
            (SnapAnchorKind.Trailing, bounds.Right + proposedDeltaXMm)
        };
        var selected = _groupDragStarts.Keys.ToHashSet();
        var sourceY = new List<(SnapAnchorKind Kind, double Position)>
        {
            (SnapAnchorKind.Leading, bounds.Top + proposedDeltaYMm),
            (SnapAnchorKind.Center, (bounds.Top + bounds.Bottom) / 2 + proposedDeltaYMm),
            (SnapAnchorKind.Trailing, bounds.Bottom + proposedDeltaYMm)
        };
        foreach (var pair in _groupDragStarts)
        {
            AddBaselineSource(sourceY, pair.Key, pair.Value.Y + proposedDeltaYMm);
        }
        var xCandidates = new List<SnapCandidate>();
        var yCandidates = new List<SnapCandidate>();

        if (IsSnapToObjectsEnabled)
        {
            foreach (var other in Template.Objects.Where(item => item.IsVisible && !selected.Contains(item)))
            {
                var otherBounds = GetObjectBoundsMm(other);
                var stableKey = GetSnapStableKey(other);
                var targetsX = new[]
                {
                    (SnapAnchorKind.Leading, otherBounds.Left),
                    (SnapAnchorKind.Center, (otherBounds.Left + otherBounds.Right) / 2),
                    (SnapAnchorKind.Trailing, otherBounds.Right)
                };
                var targetsY = new List<(SnapAnchorKind Kind, double Position)>
                {
                    (SnapAnchorKind.Leading, otherBounds.Top),
                    (SnapAnchorKind.Center, (otherBounds.Top + otherBounds.Bottom) / 2),
                    (SnapAnchorKind.Trailing, otherBounds.Bottom)
                };
                AddBaselineTarget(targetsY, other);
                foreach (var source in sourceX)
                {
                    foreach (var target in targetsX)
                    {
                        xCandidates.Add(new SnapCandidate(source.Item2, target.Item2, Math.Abs(source.Item2 - target.Item2), GetSnapPriority(source.Item1, target.Item1), $"{stableKey}:group:x:{source.Item1}:{target.Item1}"));
                    }
                }
                foreach (var source in sourceY)
                {
                    foreach (var target in targetsY)
                    {
                        yCandidates.Add(new SnapCandidate(source.Item2, target.Item2, Math.Abs(source.Item2 - target.Item2), GetSnapPriority(source.Item1, target.Item1), $"{stableKey}:group:y:{source.Item1}:{target.Item1}"));
                    }
                }
            }
        }

        AddGridCandidates(xCandidates, yCandidates, sourceX, sourceY, "group");
        if (IsSnapToObjectsEnabled)
        {
            AddSmartSpacingCandidates(
                xCandidates,
                yCandidates,
                sourceX.First(source => source.Item1 == SnapAnchorKind.Leading).Item2,
                sourceX.First(source => source.Item1 == SnapAnchorKind.Trailing).Item2,
                sourceY.First(source => source.Item1 == SnapAnchorKind.Leading).Position,
                sourceY.First(source => source.Item1 == SnapAnchorKind.Trailing).Position,
                bounds.Width,
                bounds.Height,
                selected,
                "group");
        }

        if (IsSnapToObjectsEnabled)
        {
            var canvasCenterX = Template.WidthMm / 2;
            var canvasCenterY = Template.HeightMm / 2;
            var groupCenterX = (bounds.Left + bounds.Right) / 2 + proposedDeltaXMm;
            var groupCenterY = (bounds.Top + bounds.Bottom) / 2 + proposedDeltaYMm;
            xCandidates.Add(new SnapCandidate(groupCenterX, canvasCenterX, Math.Abs(groupCenterX - canvasCenterX), 90, "canvas:center:group:x"));
            yCandidates.Add(new SnapCandidate(groupCenterY, canvasCenterY, Math.Abs(groupCenterY - canvasCenterY), 90, "canvas:center:group:y"));
        }

        var winnerX = ChoosePathSnap(SnapPathKind.GroupMove, xCandidates);
        var winnerY = ChoosePathSnap(SnapPathKind.GroupMove, yCandidates);
        var guidesX = GetWinningGuides(xCandidates, winnerX);
        var guidesY = GetWinningGuides(yCandidates, winnerY);
        var targetX = ResolveSnapTarget(
            SnapPathKind.GroupMove,
            proposedDeltaXMm,
            winnerX is SnapCandidate x ? proposedDeltaXMm + x.Delta : null,
            _snapLockX,
            guidesX);
        var targetY = ResolveSnapTarget(
            SnapPathKind.GroupMove,
            proposedDeltaYMm,
            winnerY is SnapCandidate y ? proposedDeltaYMm + y.Delta : null,
            _snapLockY,
            guidesY);
        return new AlignmentSnapResult(
            targetX is null ? null : targetX.Value - proposedDeltaXMm,
            targetY is null ? null : targetY.Value - proposedDeltaYMm,
            guidesX,
            guidesY,
            GetSnapCaption(winnerX),
            GetSnapCaption(winnerY));
    }

    private AlignmentSnapResult ComputeAlignmentSnapLegacy(LabelObject dragged, double proposedXMm, double proposedYMm)
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

        finalSnapX = ResolveSnapTarget(SnapPathKind.SingleMove, proposedXMm, finalSnapX, _snapLockX, guideXPositions);
        finalSnapY = ResolveSnapTarget(SnapPathKind.SingleMove, proposedYMm, finalSnapY, _snapLockY, guideYPositions);

        return new AlignmentSnapResult(finalSnapX, finalSnapY, guideXPositions, guideYPositions);
    }

    /// <summary>
    /// Every interactive snap ranking path must enter the shared matrix so
    /// single/group/resize/draw cannot invent a second acquire budget.
    /// </summary>
    private SnapCandidate? ChoosePathSnap(SnapPathKind pathKind, IEnumerable<SnapCandidate> candidates)
        => SnapPathMatrixContract.Choose(pathKind, Zoom, candidates);

    private double? ResolveSnapTarget(
        SnapPathKind pathKind,
        double proposedPosition,
        double? candidateTarget,
        SnapHysteresisState state,
        List<double> guides,
        bool bypassSnap = false)
    {
        var resolved = SnapPathMatrixContract.ApplyHysteresis(
            pathKind,
            Zoom,
            state,
            proposedPosition,
            candidateTarget,
            bypassSnap);
        if (resolved is not null && guides.Count == 0)
        {
            guides.Add(resolved.Value);
        }

        return resolved;
    }

    private void ClearSnapLocks()
    {
        _snapLockX.Reset();
        _snapLockY.Reset();
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

        _lastAlignmentSnap = snap;

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

            if (_guideVerticalLabel is null)
            {
                _guideVerticalLabel = CreateGuideLabel();
                Children.Add(_guideVerticalLabel);
            }
            var verticalText = FormatGuideLabel("X", snap.GuideXPositions[0], snap.XCaption);
            UpdateGuideLabel(_guideVerticalLabel, verticalText);
            Canvas.SetLeft(_guideVerticalLabel, Math.Max(0, Math.Min(labelWidthDip - 120, guideXDip + 4)));
            Canvas.SetTop(_guideVerticalLabel, 4);
            _guideVerticalLabel.Visibility = Visibility.Visible;
        }
        else
        {
            if (_guideVertical is not null)
            {
                _guideVertical.Visibility = Visibility.Collapsed;
            }
            if (_guideVerticalLabel is not null)
            {
                _guideVerticalLabel.Visibility = Visibility.Collapsed;
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

            if (_guideHorizontalLabel is null)
            {
                _guideHorizontalLabel = CreateGuideLabel();
                Children.Add(_guideHorizontalLabel);
            }
            var horizontalText = FormatGuideLabel("Y", snap.GuideYPositions[0], snap.YCaption);
            UpdateGuideLabel(_guideHorizontalLabel, horizontalText);
            Canvas.SetLeft(_guideHorizontalLabel, 4);
            Canvas.SetTop(_guideHorizontalLabel, Math.Max(0, Math.Min(labelHeightDip - 28, guideYDip + 4)));
            _guideHorizontalLabel.Visibility = Visibility.Visible;
        }
        else
        {
            if (_guideHorizontal is not null)
            {
                _guideHorizontal.Visibility = Visibility.Collapsed;
            }
            if (_guideHorizontalLabel is not null)
            {
                _guideHorizontalLabel.Visibility = Visibility.Collapsed;
            }
        }
    }

    private static Border CreateGuideLabel()
    {
        var label = new Border
        {
            Background = new SolidColorBrush(Color.FromArgb(235, 37, 99, 235)),
            BorderBrush = new SolidColorBrush(Color.FromRgb(29, 78, 216)),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(3),
            Padding = new Thickness(4, 1, 4, 1),
            IsHitTestVisible = false,
            Child = new TextBlock
            {
                Foreground = Brushes.White,
                FontSize = 10,
                FontWeight = FontWeights.SemiBold
            }
        };
        SetZIndex(label, int.MaxValue);
        return label;
    }

    private static void UpdateGuideLabel(Border label, string text)
    {
        if (label.Child is TextBlock textBlock)
        {
            textBlock.Text = text;
        }

        var isSpacing = text.StartsWith("gap ", StringComparison.OrdinalIgnoreCase);
        label.Background = isSpacing
            ? new SolidColorBrush(Color.FromArgb(235, 5, 150, 105))
            : new SolidColorBrush(Color.FromArgb(235, 37, 99, 235));
        label.BorderBrush = isSpacing
            ? new SolidColorBrush(Color.FromRgb(4, 120, 87))
            : new SolidColorBrush(Color.FromRgb(29, 78, 216));
    }

    private static string FormatGuideLabel(string axis, double positionMm, string? caption)
    {
        return string.IsNullOrWhiteSpace(caption)
            ? $"{axis} {positionMm:0.##} mm"
            : $"{caption} · {axis} {positionMm:0.##} mm";
    }

    private void HideAlignmentGuides()
    {
        _lastAlignmentSnap = null;
        if (_guideVertical is not null)
        {
            _guideVertical.Visibility = Visibility.Collapsed;
        }
        if (_guideHorizontal is not null)
        {
            _guideHorizontal.Visibility = Visibility.Collapsed;
        }
        if (_guideVerticalLabel is not null)
        {
            _guideVerticalLabel.Visibility = Visibility.Collapsed;
        }
        if (_guideHorizontalLabel is not null)
        {
            _guideHorizontalLabel.Visibility = Visibility.Collapsed;
        }
    }

    // ==================== End Alignment Guide System ====================

    private readonly record struct GroupResizeObjectSnapshot(
        double XMm,
        double YMm,
        double WidthMm,
        double HeightMm,
        double EndXMm,
        double EndYMm,
        int Rotation);

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
