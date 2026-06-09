# QR Sizing Example

```csharp
using ANLAbel.Core.Barcode;

var calculator = new QrSizingCalculator();
var bounds = new BarcodeObjectBounds(Xmm: 10, Ymm: 10, WidthMm: 20, HeightMm: 20);

var auto = calculator.Calculate(
    data: "PN-001|LOT-A|QTY-20",
    currentBounds: bounds,
    options: new QrBarcodeOptions
    {
        SizingMode = QrSizingMode.AutoSizeByData,
        ErrorCorrection = QrErrorCorrection.M,
        ModuleSizePx = 6,
        QuietZoneModules = 4,
        Dpi = 300
    });

if (auto.IsValid)
{
    // Apply these values back to the barcode object.
    var widthMm = auto.WidthMm;
    var heightMm = auto.HeightMm;
}

var fixedSize = calculator.Calculate(
    data: "SHORT DATA",
    currentBounds: bounds,
    options: new QrBarcodeOptions
    {
        SizingMode = QrSizingMode.FixedVersionAndModuleSize,
        ErrorCorrection = QrErrorCorrection.Q,
        FixedVersion = 3,
        ModuleSizePx = 9,
        QuietZoneModules = 4,
        Dpi = 300
    });
```