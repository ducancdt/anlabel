# ANLAbel

Open-source Windows app for **designing and printing industrial labels** from
local Excel/CSV data. Compact, basic, stable.

[Download](https://github.com/ducancdt/anlabel/releases/latest)
· [Quick start](docs/quick-start.md)
· [Docs](docs/README.md)

ANLAbel is a .NET 8 WPF desktop tool: millimeter canvas, Text/TextBox,
barcodes, preflight, and a named industrial printer queue. It is not a cloud
suite.

## Basics

1. Install the Windows x64 build.
2. Set label size. Place text, barcodes, images, shapes.
3. Import your Excel/CSV. Bind fields.
4. Preview. Fix preflight. Print to the named queue.

Industrial drivers (Zebra, TSC, Godex, SATO, and similar) — not office
printers. Templates do not ship customer data or a linked sample workbook.

## Docs that matter

- [Product contract](docs/LOCAL_LABEL_PRODUCT_CONTRACT.md)
- [Execution plan](docs/reinvention/07-execution-plan.md)
- [Quality loop](docs/AUTOMATED_QUALITY_LOOP.md)
- [Roadmap](ROADMAP.md)

Everything else in `docs/` is history or research.

## Build and test

```powershell
dotnet restore ANLAbel.slnx
dotnet build ANLAbel.slnx -c Release --no-restore
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/Invoke-ANLAbelQualityLoop.ps1 -Mode Fast
```

Windows 10/11 x64. GPL-3.0-only. See [LICENSE](LICENSE).
