using ANLAbel.Core.Models;

namespace ANLAbel.App.Controls;

/// <summary>
/// A field or prompt dropped on the label from the Data explorer (NiceLabel 10
/// drag and drop): its binding ({Field} or {?Prompt}), the drop point in label
/// millimetres, the object under it and whether Ctrl asked for a new object.
/// </summary>
public sealed class DataSourceDroppedEventArgs(string binding, double xMm, double yMm, LabelObject? target, bool newObject) : EventArgs
{
    public string Binding { get; } = binding;

    public double XMm { get; } = xMm;

    public double YMm { get; } = yMm;

    public LabelObject? Target { get; } = target;

    public bool NewObject { get; } = newObject;
}
