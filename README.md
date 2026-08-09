# ANLAbel

[![CI](https://github.com/ducancdt/anlabel/actions/workflows/ci.yml/badge.svg)](https://github.com/ducancdt/anlabel/actions/workflows/ci.yml)
[![License: GPL v3](https://img.shields.io/badge/License-GPLv3-blue.svg)](LICENSE)

ANLAbel is an open-source Windows desktop application for designing and printing industrial labels. It is built with C#/.NET 8 and WPF for manufacturing, warehouse, traceability, and product-identification workflows.

The application targets industrial label printers through their Windows drivers, including common Zebra, TSC, Godex, SATO, Argox, Honeywell, Intermec, Citizen, and Toshiba TEC workflows.

## Features

- Visual label designer with text, images, shapes, lines, barcodes, QR Code, Data Matrix, Aztec, and PDF417.
- Excel-driven label content with sheet selection, header-row preview, field binding, formulas, and row tracking.
- Shared Data Source Manager with connection tests, relinking, unlinking, usage tracking, and cleanup.
- Batch print preview with record filtering, per-record copy counts, row selection, and duplicate-print warnings.
- Print preflight for missing data, out-of-bounds objects, stale Excel data, barcode size, and real print DPI.
- CSV append-only print history with Excel report export.
- Portable `.anlabel` JSON templates and a built-in template library.
- Millimeter-accurate rendering for 203, 300, and 600 DPI printer workflows.

## Download

The current Windows x64 installer is available from the [ANLAbel Full v0.085 release](https://github.com/ducancdt/anlabel/releases/tag/v0.085-full).

The Full build has no trial period and does not require a license key or activation code.

> The installer is currently unsigned. Windows SmartScreen may display a warning when it is opened for the first time.

## Requirements

- Windows 10 or Windows 11, x64.
- A Windows-compatible label-printer driver for physical printing.
- Visual Studio 2022 or a compatible .NET SDK when building from source.

## Build from source

```powershell
git clone https://github.com/ducancdt/anlabel.git
cd anlabel
dotnet restore ANLAbel.slnx
dotnet build ANLAbel.slnx -c Release --no-restore
dotnet run --project src/ANLAbel.App/ANLAbel.App.csproj -c Release
```

## Tests

```powershell
dotnet run --project src/ANLAbel.Tests/ANLAbel.Tests.csproj -c Release
dotnet test src/ANLAbel.UnitTests/ANLAbel.UnitTests.csproj -c Release
```

The project currently contains application-level regression tests and xUnit tests covering data import, template persistence, barcode rendering, print geometry, DPI conversion, preflight validation, licensing-policy behavior, and reliability cases.

## Contributing

Bug reports, printer-compatibility feedback, documentation improvements, and code contributions are welcome. Read [CONTRIBUTING.md](CONTRIBUTING.md) before opening an issue or pull request.

When reporting a printing problem, include the printer model, Windows driver name/version, configured DPI, label dimensions, orientation, and a minimal `.anlabel` template when it is safe to share.

## License

ANLAbel source code is licensed under the [GNU General Public License v3.0 only](LICENSE) (`GPL-3.0-only`). You may use, study, modify, and redistribute the software under the conditions of that license. Distributed modified versions must preserve the GPL terms and provide the corresponding source code.

Third-party libraries and assets retain their respective licenses. See [docs/license-notices.md](docs/license-notices.md) for notices.

Copyright © 2026 Duc An.
