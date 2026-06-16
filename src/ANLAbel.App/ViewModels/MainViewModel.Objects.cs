using System.Text;
using ANLAbel.Barcode.Options;
using ANLAbel.Barcode.Renderers;
using ANLAbel.Core.Barcode;
using ANLAbel.Core.Enums;
using ANLAbel.Core.Expressions;
using ANLAbel.Core.Expressions.Formulas;
using ANLAbel.Core.Models;
using ANLAbel.Core.Mvvm;
using ANLAbel.Printing.RenderPipeline;

namespace ANLAbel.App.ViewModels;

public sealed partial class MainViewModel
{
    private void AddText()
    {
        AddObject(new LabelObject
        {
            Type = ObjectType.Text,
            Name = "Text",
            Text = "Long text can overflow horizontally",
            BindingExpression = string.Empty,
            XMm = 5,
            YMm = 5,
            WidthMm = 35,
            HeightMm = 10,
            Style = { FontSizePt = 11, BorderThicknessMm = 0 }
        });
    }

    private void AddTextBox()
    {
        AddObject(new LabelObject
        {
            Type = ObjectType.TextBox,
            Name = "Text Box",
            Text = "Text box keeps content inside its bounds and wraps long lines.",
            BindingExpression = string.Empty,
            XMm = 5,
            YMm = 18,
            WidthMm = 42,
            HeightMm = 16,
            Style = { FontSizePt = 9, BorderThicknessMm = 0, OutlineStyle = OutlineStyle.None }
        });
    }

    private void AddExcelField(string? fieldName)
    {
        if (string.IsNullOrWhiteSpace(fieldName))
        {
            StatusText = "Select an Excel field first";
            return;
        }

        AddObject(new LabelObject
        {
            Type = ObjectType.Text,
            Name = $"Field: {fieldName}",
            Text = fieldName,
            BindingExpression = $"{{{fieldName}}}",
            XMm = 5,
            YMm = 5,
            WidthMm = 38,
            HeightMm = 10,
            Style = { FontSizePt = 11, BorderThicknessMm = 0 }
        });
    }

    private void BindSelectedAsExcelField(string? fieldName)
    {
        if (SelectedObject is null || string.IsNullOrWhiteSpace(fieldName))
        {
            return;
        }

        if (SelectedObject.Type is not (ObjectType.Text or ObjectType.TextBox or ObjectType.BarcodeCode128 or ObjectType.QRCode or ObjectType.DataMatrix))
        {
            SelectedObject.Type = ObjectType.Text;
        }

        SelectedObject.Name = SelectedObject.Type is ObjectType.BarcodeCode128 or ObjectType.QRCode or ObjectType.DataMatrix
            ? $"{SelectedObject.Type}: {fieldName}"
            : $"Field: {fieldName}";
        SelectedObject.Text = fieldName;
        SelectedObject.BindingExpression = $"{{{fieldName}}}";
        StatusText = $"Bound selected object to {{{fieldName}}}";
        RaiseFormulaPreviewChanged();
    }

    private void ClearSelectedBinding()
    {
        if (SelectedObject is null)
        {
            return;
        }

        SelectedObject.BindingExpression = string.Empty;
        if (string.IsNullOrWhiteSpace(SelectedObject.Text))
        {
            SelectedObject.Text = "Text";
        }

        if (SelectedObject.Type is ObjectType.Text or ObjectType.TextBox)
        {
            SelectedObject.Name = "Text";
        }
        StatusText = "Selected object changed to static text";
        RaiseFormulaPreviewChanged();
    }

    private void InsertFunctionFormula(string? formula)
    {
        if (SelectedObject is null || string.IsNullOrWhiteSpace(formula))
        {
            StatusText = "Select an object before inserting a function";
            return;
        }

        SelectedObject.BindingExpression = formula;
        if (SelectedObject.Type is ObjectType.BarcodeCode128 or ObjectType.QRCode or ObjectType.DataMatrix)
        {
            SelectedObject.Text = string.Empty;
        }

        StatusText = $"Inserted formula: {formula}";
        RaiseFormulaPreviewChanged();
    }

    private void AddFormulaFieldPart(DatabaseField? field)
    {
        if (field is null)
        {
            return;
        }

        AddFormulaPart(new FormulaBuilderPart(FormulaBuilderPartKind.Field, field.Name, field.DisplayName));
    }

    private void AddFormulaTextPart()
    {
        AddFormulaTextPart(FormulaBuilderText);
        FormulaBuilderText = string.Empty;
    }

    private void AddFormulaTextPart(string? text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return;
        }

        AddFormulaPart(new FormulaBuilderPart(FormulaBuilderPartKind.Text, text, text));
    }

    private void AddFormulaPart(FormulaBuilderPart part)
    {
        FormulaBuilderParts.Add(part);
        SelectedFormulaBuilderPart = part;
        RaiseFormulaBuilderChanged();
    }

    private void RemoveFormulaPart()
    {
        if (SelectedFormulaBuilderPart is null)
        {
            return;
        }

        var index = FormulaBuilderParts.IndexOf(SelectedFormulaBuilderPart);
        FormulaBuilderParts.Remove(SelectedFormulaBuilderPart);
        SelectedFormulaBuilderPart = FormulaBuilderParts.Count == 0 ? null : FormulaBuilderParts[Math.Clamp(index, 0, FormulaBuilderParts.Count - 1)];
        RaiseFormulaBuilderChanged();
    }

    private void ClearFormulaBuilder()
    {
        FormulaBuilderParts.Clear();
        SelectedFormulaBuilderPart = null;
        RaiseFormulaBuilderChanged();
    }

    private void ApplyFormulaBuilder()
    {
        if (SelectedObject is null || FormulaBuilderParts.Count == 0)
        {
            StatusText = "Select an object and add formula parts first";
            return;
        }

        var expression = BuildFormulaExpression();
        InsertFunctionFormula(expression);
        StatusText = $"Applied formula builder: {expression}";
    }

    private string BuildFormulaExpression()
    {
        if (FormulaBuilderParts.Count == 0)
        {
            return string.Empty;
        }

        var arguments = FormulaBuilderParts.Select(part => part.Kind == FormulaBuilderPartKind.Field
            ? $"FIELD(\"{EscapeFormulaString(part.Value)}\")"
            : $"\"{EscapeFormulaString(part.Value)}\"");
        return $"CONCAT({string.Join(", ", arguments)})";
    }

    private FormulaEvaluationResult EvaluateFormulaBuilder()
    {
        var expression = BuildFormulaExpression();
        if (string.IsNullOrWhiteSpace(expression))
        {
            return new FormulaEvaluationResult(string.Empty, Array.Empty<string>(), Array.Empty<string>());
        }

        return PreviewRow is null
            ? new FormulaEvaluationResult(expression, Array.Empty<string>(), Array.Empty<string>())
            : FormulaBindingEvaluator.Evaluate(expression, PreviewRow);
    }

    private void RaiseFormulaBuilderChanged()
    {
        OnPropertyChanged(nameof(FormulaBuilderExpression));
        OnPropertyChanged(nameof(FormulaBuilderPreviewValue));
        OnPropertyChanged(nameof(FormulaBuilderPreviewErrors));
        ((RelayCommand)RemoveFormulaPartCommand).RaiseCanExecuteChanged();
        ((RelayCommand)ClearFormulaBuilderCommand).RaiseCanExecuteChanged();
        ((RelayCommand)ApplyFormulaBuilderCommand).RaiseCanExecuteChanged();
    }

    private static string? GetFieldName(object? parameter)
    {
        return parameter switch
        {
            DatabaseField field => field.Name,
            string text => text,
            _ => parameter?.ToString()
        };
    }

    private static string? GetFormulaText(object? parameter)
    {
        return parameter switch
        {
            FormulaFunctionTemplate template => template.Template,
            DatabaseField field => $"FIELD(\"{EscapeFormulaString(field.Name)}\")",
            string text => text,
            _ => parameter?.ToString()
        };
    }

    private static string EscapeFormulaString(string value)
    {
        return value.Replace("\\", "\\\\", StringComparison.Ordinal).Replace("\"", "\\\"", StringComparison.Ordinal);
    }

    private void StartDrawingTool(ObjectType tool)
    {
        DrawingTool = tool;
        DrawingCommandText = tool switch
        {
            ObjectType.Line => "Line: specify first point",
            ObjectType.Ellipse => "Ellipse/Circle: specify first corner",
            _ => "Rectangle: specify first corner"
        };
        StatusText = tool switch
        {
            ObjectType.Line => "Line: click first point, move pointer, click next point or type length + Enter, Esc to cancel",
            ObjectType.Ellipse => "Ellipse/Circle: click first corner, move pointer, click opposite corner or type width,height + Enter, Esc to cancel",
            _ => "Rectangle: click first corner, move pointer, click opposite corner or type width,height + Enter, Esc to cancel"
        };
    }

    public void CompleteDrawingTool(LabelObject labelObject)
    {
        DrawingTool = null;
        SelectedObject = labelObject;
        StatusText = $"Added {labelObject.Name}";
    }

    private void AddBarcode()
    {
        AddObject(new LabelObject
        {
            Type = ObjectType.BarcodeCode128,
            Name = "Barcode",
            Text = "123456789012",
            BindingExpression = string.Empty,
            XMm = 8,
            YMm = 8,
            WidthMm = 32,
            HeightMm = 10,
            BarcodeSymbology = BarcodeSymbology.Code128,
            Style = { BorderThicknessMm = 0 }
        });
    }

    private void AddQrCode()
    {
        AddObject(new LabelObject
        {
            Type = ObjectType.QRCode,
            Name = "QR Code",
            Text = "QR Code",
            BindingExpression = string.Empty,
            XMm = 8,
            YMm = 8,
            WidthMm = 8,
            HeightMm = 8,
            Style = { BorderThicknessMm = 0 }
        });
    }

    private void AddDataMatrix()
    {
        AddObject(new LabelObject
        {
            Type = ObjectType.DataMatrix,
            Name = "Data Matrix",
            Text = "Data Matrix",
            BindingExpression = string.Empty,
            XMm = 8,
            YMm = 8,
            WidthMm = 18,
            HeightMm = 18,
            Style = { BorderThicknessMm = 0 }
        });
    }

    private void AddObject(LabelObject labelObject)
    {
        labelObject.ZIndex = Template.Objects.Count == 0 ? 1 : Template.Objects.Max(item => item.ZIndex) + 1;
        Template.Objects.Add(labelObject);
        SelectedObject = labelObject;
        StatusText = $"Added {labelObject.Name}";
    }

    private void DeleteSelected()
    {
        if (SelectedObject is null)
        {
            return;
        }

        Template.Objects.Remove(SelectedObject);
        SelectedObject = null;
        StatusText = "Deleted selected object";
    }

    private void Undo()
    {
        if (_undoStack.Count == 0)
        {
            return;
        }

        var currentSnapshot = CaptureTemplateSnapshot();
        var previousSnapshot = _undoStack.Pop();
        _redoStack.Push(currentSnapshot);
        RestoreTemplateSnapshot(previousSnapshot);
        StatusText = "Undo";
        RaiseHistoryCanExecuteChanged();
    }

    private void Redo()
    {
        if (_redoStack.Count == 0)
        {
            return;
        }

        var currentSnapshot = CaptureTemplateSnapshot();
        var nextSnapshot = _redoStack.Pop();
        _undoStack.Push(currentSnapshot);
        RestoreTemplateSnapshot(nextSnapshot);
        StatusText = "Redo";
        RaiseHistoryCanExecuteChanged();
    }

    private void ApplyQrAutoSizeFromModel(LabelObject item, string? propertyName)
    {
        if (_applyingQrAutoSize.Contains(item)
            || !IsSquare2DCodeLike(item)
            || propertyName is not (nameof(LabelObject.Text)
                or nameof(LabelObject.BindingExpression)
                or nameof(LabelObject.BarcodeSymbology)
                or nameof(LabelObject.Type)
                or nameof(LabelObject.QrSizingMode)
                or nameof(LabelObject.QrErrorCorrection)
                or nameof(LabelObject.QrFixedVersion)
                or nameof(LabelObject.QrModuleSizePx)
                or nameof(LabelObject.QrQuietZoneModules)
                or nameof(LabelObject.QrDpi)))
        {
            return;
        }

        var targetSizeMm = item.QrSizingMode == QrSizingMode.FixedVersionAndModuleSize
            ? QrAutoSizeHelper.CalculateFixedSizeMm(item.QrFixedVersion, item.QrModuleSizePx, item.QrQuietZoneModules, item.QrDpi, GetAvailableQrSizeMm(item))
            : QrAutoSizeHelper.CalculateRequiredSizeMm(
                BindingResolver.ResolveObject(item, PreviewRow),
                item.WidthMm,
                item.HeightMm,
                item.QrErrorCorrection,
                item.QrModuleSizePx,
                item.QrQuietZoneModules,
                item.QrDpi,
                _qrCapacityTable,
                GetAvailableQrSizeMm(item));
        if (targetSizeMm is null)
        {
            return;
        }

        if (Math.Abs(item.WidthMm - targetSizeMm.Value) <= 0.05 && Math.Abs(item.HeightMm - targetSizeMm.Value) <= 0.05)
        {
            return;
        }

        _applyingQrAutoSize.Add(item);
        try
        {
            item.WidthMm = targetSizeMm.Value;
            item.HeightMm = targetSizeMm.Value;
            OnPropertyChanged(nameof(SelectedObject));
        }
        finally
        {
            _applyingQrAutoSize.Remove(item);
        }
    }

    private static bool IsSquare2DCodeLike(LabelObject item)
    {
        return item.Type == ObjectType.QRCode
            || item.Type == ObjectType.DataMatrix
            || item.Type == ObjectType.BarcodeCode128
                && item.BarcodeSymbology is BarcodeSymbology.QRCode
                    or BarcodeSymbology.DataMatrix
                    or BarcodeSymbology.Aztec
                    or BarcodeSymbology.Pdf417;
    }

    private double GetAvailableQrSizeMm(LabelObject item)
    {
        var availableWidthMm = Template.WidthMm - item.XMm;
        var availableHeightMm = Template.HeightMm - item.YMm;
        return Math.Max(1, Math.Min(availableWidthMm, availableHeightMm));
    }

    private string ValidateSelectedBarcode()
    {
        if (SelectedObject is not { Type: ObjectType.BarcodeCode128 or ObjectType.QRCode or ObjectType.DataMatrix } item)
        {
            return string.Empty;
        }

        var data = BindingResolver.ResolveObject(item, PreviewRow);
        var type = item.Type switch
        {
            ObjectType.QRCode => BarcodeType.QRCode,
            ObjectType.DataMatrix => BarcodeType.DataMatrix,
            _ => BarcodeTypeMapper.ToRendererType(item.BarcodeSymbology)
        };

        var renderError = ValidateBarcodeCanRender(item, data, type);
        return renderError is null
            ? string.Empty
            : $"Invalid {type} data. {renderError}";
    }

    private string ValidateSelectedTextBox()
    {
        if (SelectedObject is not { Type: ObjectType.TextBox } item)
        {
            return string.Empty;
        }

        var data = BindingResolver.ResolveObject(item, PreviewRow);
        return IsTextBoxOverflowing(item, data)
            ? "Text box overflow: increase the object size or reduce text/font size."
            : string.Empty;
    }

    public string? ValidatePrintPreviewContent()
    {
        var rows = ExcelDataView is null || ExcelDataView.Count == 0
            ? new IReadOnlyDictionary<string, string>?[] { PreviewRow }
            : ExcelDataView
                .Cast<System.Data.DataRowView>()
                .Select(CreatePreviewRow)
                .Cast<IReadOnlyDictionary<string, string>?>()
                .ToArray();
        return ValidatePrintableContent(rows);
    }

    private string? ValidatePrintableContent(IReadOnlyList<IReadOnlyDictionary<string, string>?> rows)
    {
        foreach (var item in Template.Objects.Where(item => item.IsVisible && item.Type is ObjectType.BarcodeCode128 or ObjectType.QRCode or ObjectType.DataMatrix))
        {
            for (var i = 0; i < rows.Count; i++)
            {
                var data = BindingResolver.ResolveObject(item, rows[i]);
                var type = item.Type switch
                {
                    ObjectType.QRCode => BarcodeType.QRCode,
                    ObjectType.DataMatrix => BarcodeType.DataMatrix,
                    _ => BarcodeTypeMapper.ToRendererType(item.BarcodeSymbology)
                };

                var renderError = ValidateBarcodeCanRender(item, data, type);
                if (renderError is not null)
                {
                    return $"Print blocked: row {i + 1}, {item.Name} has invalid {type} data. {renderError}";
                }

                if (item.Type == ObjectType.QRCode && item.QrSizingMode == QrSizingMode.FixedVersionAndModuleSize)
                {
                    var byteCount = Encoding.UTF8.GetByteCount(data);
                    var capacity = EstimateFixedQrCapacity(item.WidthMm, item.HeightMm, item.QrErrorCorrection);
                    if (byteCount > capacity)
                    {
                        return $"Print blocked: row {i + 1}, {item.Name} has {byteCount} bytes but fixed QR {item.WidthMm:0.#}x{item.HeightMm:0.#}mm allows about {capacity}. Increase size or use Auto size.";
                    }
                }
            }
        }

        foreach (var item in Template.Objects.Where(item => item.IsVisible && item.Type == ObjectType.TextBox))
        {
            for (var i = 0; i < rows.Count; i++)
            {
                var data = BindingResolver.ResolveObject(item, rows[i]);
                if (IsTextBoxOverflowing(item, data))
                {
                    return $"Print blocked: row {i + 1}, {item.Name} text overflows its text box. Increase object size or reduce text/font size.";
                }
            }
        }

        return null;
    }

    private static bool IsTextBoxOverflowing(LabelObject item, string data)
    {
        return TextBoxOverflowDetector.IsOverflowing(
            item,
            data,
            ANLAbel.Core.Geometry.MmConverter.MmToDip(item.WidthMm),
            ANLAbel.Core.Geometry.MmConverter.MmToDip(item.HeightMm));
    }

    private string? ValidateBarcodeCanRender(LabelObject item, string data, BarcodeType type)
    {
        if (!_barcodeValidator.ValidateData(data, type))
        {
            return "Check empty text, unsupported characters, or required length.";
        }

        try
        {
            _barcodeValidator.RenderBarcode(data, type, item.WidthMm, item.HeightMm, item.QrDpi, CreateBarcodeRenderOptions(item));
            return null;
        }
        catch (ArgumentException ex)
        {
            return ex.Message;
        }
        catch (InvalidOperationException ex)
        {
            return ex.Message;
        }
        catch (Exception ex) when (ex is not OutOfMemoryException)
        {
            return ex.Message;
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

    private static int EstimateFixedQrCapacity(double widthMm, double heightMm, QrErrorCorrection errorCorrection)
    {
        var safeSize = Math.Max(1, Math.Min(widthMm, heightMm));
        var baseline = Math.Floor(safeSize * safeSize);
        var factor = errorCorrection switch
        {
            QrErrorCorrection.L => 1.15,
            QrErrorCorrection.M => 1.0,
            QrErrorCorrection.Q => 0.8,
            QrErrorCorrection.H => 0.65,
            _ => 1.0
        };

        return Math.Max(1, (int)Math.Floor(baseline * factor));
    }
}
