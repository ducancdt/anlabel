# Print Calibration Notes

Printing was started in Phase 4.

Current implementation:
- `PrintService` opens Windows `PrintDialog`.
- `LabelVisualRenderer` renders labels from mm model values.
- `Test Print` renders a 10 mm ruler grid for calibration.
- Barcode rendering is called from the print pipeline with print DPI/profile.
- Printer profile values are editable in the main properties panel.
- Batch printing supports all Excel rows, fixed copies, and copy count from an Excel field.
- On startup, ANLAbel opens a printer setup dialog. The selected Windows driver paper size becomes the template label size.
- Label printers are highlighted by common driver/name keywords such as Zebra, TSC, Godex, Argox, SATO, Intermec, Honeywell, Citizen, Dymo, Brother QL, Label, Barcode, and Seagull.

Required Phase 4 rules:
- Render from template mm coordinates directly to printer units.
- Do not print from screen preview bitmap.
- Do not let Windows auto-scale the label.
- Apply printer profile values:
  - `OffsetXMm`
  - `OffsetYMm`
  - `ScaleX`
  - `ScaleY`
- Add a test print with ruler marks for calibration.

Conversion reference:
- dots = `round(mm / 25.4 * dpi)`
- mm = `dots * 25.4 / dpi`
