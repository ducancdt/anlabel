namespace ANLAbel.App.ViewModels;

/// <summary>
/// Ribbon tabs in NiceLabel 10 order (docs/NICELABEL_10_UI_LAYOUT.md):
/// fixed Home, Data, View, Help, then the contextual Design tab and the
/// tab of the selected object's kind.
/// </summary>
public enum RibbonTab
{
    Home,
    Data,
    View,
    Help,
    Design,
    Text,
    Barcode,
    Picture,
    Shape
}
