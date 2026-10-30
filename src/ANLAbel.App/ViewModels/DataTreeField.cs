namespace ANLAbel.App.ViewModels;

/// <summary>
/// A value the Data panel offers for dragging onto the label besides prompts,
/// sheet fields and functions: a field given at print time (the serial, the
/// print date), with the binding a dropped object gets.
/// </summary>
public sealed record DataTreeField(string Label, string Binding);
