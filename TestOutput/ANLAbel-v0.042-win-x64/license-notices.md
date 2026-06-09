# License Notices

**ANLAbel - Label Designer**
Created by **Duc An**
Email: ducancdt@gmail.com

---

Phase 1 used only .NET/WPF platform libraries.

Phase 2 adds:
- ClosedXML `0.105.0`, MIT License, for `.xlsx/.xlsm` workbook reading.
- ClosedXML transitive packages are restored through NuGet and should be reviewed before release packaging.

Phase 3 adds:
- ZXing.Net `0.16.11`, Apache-2.0 License, for Code 128, QR Code and Data Matrix rendering.

Future dependency candidates:
- Zint as an optional alternate barcode engine.

Before adding a package, record:
- package name;
- version;
- license;
- source URL;
- redistribution notes.