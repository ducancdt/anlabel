# Barcode Notes

Competitive object-model research (NiceLabel + BarTender → ANLAbel gap matrix):  
**[BARCODE_NICELABEL_BARTENDER_RESEARCH.md](./BARCODE_NICELABEL_BARTENDER_RESEARCH.md)**

Barcode was added in Phase 3 using `ZXing.Net`.

The abstraction is `IBarcodeRenderer` in `ANLAbel.Barcode`.

Phase 3 rules:
- Code 128, QR Code and Data Matrix must be regenerated for target print DPI.
- Do not scale a low-resolution preview bitmap for printing.
- Keep the barcode engine behind an interface so ZXing.Net or Zint can be swapped.
- Validate empty data and unsupported characters before printing.
- Preserve license notices for any third-party barcode engine.

Current implementation:
- `ZxingBarcodeRenderer` renders to BGRA pixel buffers.
- WPF preview converts the pixel buffer to `BitmapSource`.
- Current preview uses 300 DPI as a crisp screen/render baseline.
- Phase 4 printing must call `IBarcodeRenderer.RenderBarcode(...)` again with the actual printer DPI.
- Designer now uses one generic `Barcode` object. The selected barcode standard is stored per object as `BarcodeSymbology`.
- Exposed standards include Code 128, QR Code, Data Matrix, Code 39, Code 93, EAN-13, EAN-8, UPC-A, UPC-E, ITF, Codabar, PDF417, Aztec, MSI, and Plessey.
