# Print, printer, and label stock — fail closed

**Status:** live increment  
**Date:** 2026-08-18  
**Product:** [`LOCAL_LABEL_PRODUCT_CONTRACT.md`](LOCAL_LABEL_PRODUCT_CONTRACT.md)  
**Does not reopen:** driver `DEVMODE` / `DeviceCapabilitiesW` paper lists
([`print-preview-reliability-plan.md`](print-preview-reliability-plan.md) Đợt 3 mục 7,
`agent.md` rule 11)

This is the working note for making print, named-queue selection, and khổ giấy
less error-prone. `PLAN.md` remains history.

## What already holds

- Print goes to one named Windows queue. Missing queue fails closed. The
  Windows default is never a silent fallback (`ADR-016`).
- Spool accept is not physical completion (`ADR-017`).
- Paper sizes are **not** read from the driver. Discovery attaches
  `StandardLabelSizes` and the operator may type a custom millimetre size.
- `MediaDimensionContract` compares driver `PageMediaSize` (DIP) with the
  authored physical stock. Swapped axes do not match. Thermal drivers that
  omit custom media stay unverified instead of inventing a size.
- `PrintService.ApplyTemplateTicket` sends exact physical millimetres as
  `PageMediaSize` and does **not** set `PageOrientation` (thermal drivers treat
  that as a rotate command).
- Calibration remains a physical measurement workflow
  ([`print-calibration.md`](print-calibration.md)).

## Error-prone gaps this increment closes

These were still live in the 0.272 tree:

| Gap | Why it prints the wrong label |
| --- | --- |
| `StandardLabelSizes` listed **A4 210 × 297 mm** under Shipping & Warehouse | Office sheet mixed into thermal stock. Operators pick A4 the way they pick tray paper. |
| Catalog / custom stock was stored as `PaperSizeSource.DriverAutomatic` | The size never came from the driver. Later code can treat it as “driver-owned” and skip operator review. |
| Printer Setup selected `printers[0]` when the saved name was missing | Discovery sorts `IsDefault` first, so index 0 is the Windows default office queue. |
| Preflight had no stock check | A 50 × 30 mm design with 210 × 297 mm physical stock, or a non-finite size, reached preview/print until a driver ticket happened to complain. |
| Physical stock vs authored label was not compared at preflight | Landscape swap is valid; a completely different die size is not. That mismatch only appeared (if at all) after ticket merge. |

Out of scope here: reading driver paper lists, claiming hardware calibration,
Control Center, a second renderer, or changing the Text/TextBox contract.

## Rules for khổ giấy

1. Industrial label stock is a millimetre die size from the catalog or a
   typed custom size. It is not A4 / A3 / Letter / Legal.
2. Catalog pick and typed custom are both **operator stock**
   (`PaperSizeSource.Manual`). `DriverAutomatic` is leftover data only.
3. If physical width and height are both set, they must match the authored
   label in the same orientation **or** the designed landscape/portrait swap
   (`LabelGeometry.OrientSize`). Any other pair fails closed.
4. If only one physical axis is set, fail closed. Incomplete stock is not
   “use the label size”.
5. If both physical axes are 0, treat stock as unset (legacy files). The
   label size itself must still be finite and must not be an office sheet.
6. Do not enumerate `PrintCapabilities.PageMediaSizeCapability` or
   `DeviceCapabilitiesW` to fill the list. Do not add a hardcoded office
   tray list as a fallback.
7. Each stock edge must be between 8 mm and 400 mm. Smaller than the
   jewelry/Brother catalog or larger than a rack label is not thermal stock.
8. Authored DPI is an industrial thermal value (`152/203/300/305/600/609/1200`),
   not 72/96/150.
9. Setup/apply paths evaluate stock before mutating the document.
10. Preferences are not a printer. Only the document named queue is.
11. Preview/setup must not copy preference printer or DPI onto the template.
12. Main-window and Preview Printer Setup open on the document queue, paper,
    DPI, and orientation. `203` and Portrait are authored values, not unset.
13. Print scale stays in `0.5–2.0`. Zero on both axes is unset identity; one
    axis only is incomplete.

## Shipped functions

| Function | Role |
| --- | --- |
| `ANLAbel.Core.Printing.LabelStockContract` | Value-only stock policy. No printer. |
| `PrintPreflightValidator.Validate` | Calls the contract before object checks. |
| `MainViewModel.ApplyPrinterSelection` | Writes operator stock as `Manual` and keeps physical millimetres unswapped. |
| `PrinterDiscoveryService.ResolveNamedQueue` | Returns the named queue or null. Never `printers[0]`. |
| `PrinterSetupWindow` | Restores a named queue / saved catalog stock; does not auto-pick the Windows default. |
| `StandardLabelSizes` | Thermal / logistics / jewelry / Dymo / Brother / rack sizes only. No A4 sheet. |

## First increment (0.273)

1. Document the gaps and rules above.
2. Add `LabelStockContract` and drive it from preflight.
3. Stop storing catalog stock as `DriverAutomatic`.
4. Stop auto-selecting the Windows default in Printer Setup.
5. Drop A4 from the catalog.
6. Cover the shipped functions with unit tests plus one `PrintService.ValidateRows` regression.
7. Bump the public version and run Fast.

## Second increment (0.274)

Preflight after a bad Apply is too late: `ApplyPrinterSelection` and Preview
Printer Setup already overwrite `WidthMm` / `HeightMm`. Typing 210×297 or a
leftover 96 DPI file would mutate the document, then block print.

Added rules:

7. A stock edge must stay in `8–400 mm` (below jewelry/Brother catalog, above
   poster/office sheets). Catalog entries must all pass.
8. Authored print DPI is industrial only: `152`, `203`, `300`, `305`, `600`,
   `609`, `1200`. `72` / `96` / `150` are office/screen and fail closed.
9. Printer Setup Apply, `ApplyPrinterSelection`, and Preview stock apply must
   run `LabelStockContract` **before** writing the template. A blocked stock
   leaves the document unchanged.

Shipped additions:

| Function | Role |
| --- | --- |
| `LabelStockContract.MinimumEdgeMm` / `MaximumEdgeMm` | Bounds on die size. |
| `IndustrialPrintDpiContract` | Authored thermal DPI allow-list. |
| `PrintPreflightValidator.ValidatePrintDpi` | Blocks office/unknown DPI. |
| `MainViewModel.ApplyPrinterSelection` | Throws and does not mutate when stock/DPI is blocked. |

## Third increment (0.275)

A document without a named queue still inherited the last-used printer
from `%LocalAppData%\ANLAbel\printer-preferences.json`. Preview ctor wrote
that name (and prefs DPI) onto the template. Main-window Printer Setup did
not pass the document queue, so prefs — often the Windows default — won.

Added rules:

10. The document named queue is the only queue identity. Preferences never
    invent or restore a printer onto a document.
11. Opening Preview or Printer Setup must not mutate `PrinterName` / `Dpi`
    from preferences.
12. Main-window Printer Setup opens on the document’s queue, paper, DPI, and
    orientation. `203` and Portrait are real authored values, not “unset”.

Shipped additions:

| Function | Role |
| --- | --- |
| `DocumentPrinterIdentityContract` | Queue from the document only; paper hint may come from prefs when the document has no paper name. |
| `PrinterSetupWindow` | Resolves the combo through that contract. Never `prefs.PrinterName`. |
| `PrintPreviewWindow` ctor | No longer writes preferences onto the template. |
| `MainWindow.ShowPrinterSetupDialog` | Passes the document printer/stock into setup. |

## Fourth increment (0.276)

Preview Printer Setup still passed `null` for paper name, so a last-used
prefs stock could replace the document die when the operator clicked Apply.
`ScaleX`/`ScaleY` of `0` were silently treated as `1`, and a fit-to-page
scale (0.25, 4, …) could still reach the plan.

Added rules:

13. Preview Printer Setup opens on the document paper name, same as the
    main window. Preferences may hint only when the document has no paper.
14. Print scale is industrial 1:1 with a small calibration band
    (`0.5–2.0`). Non-finite or negative scale fails closed. Both axes `0`
    means unset (legacy) and is treated as identity. A single incomplete
    axis is not “use 1”.

Shipped additions:

| Function | Role |
| --- | --- |
| `PrintScaleContract` | Value-only scale policy. |
| `PrintPreflightValidator.ValidatePrintScale` | Blocks fit-to-page / invalid scale. |
| `PrintPreviewWindow.LabelPrinterSetup_Click` | Passes `PrinterProfile.PaperName`. |

Hardware proof stays open. Do not read driver paper lists.
