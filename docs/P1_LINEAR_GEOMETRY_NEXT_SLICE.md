# P1 closure record — Linear barcode physical geometry

**Status:** Completed implementation/acceptance record (2026-08-13)
**Parent spine:** [`INDUSTRIAL_BARCODE_EXECUTION_PLAN.md`](INDUSTRIAL_BARCODE_EXECUTION_PLAN.md) §P1
**Competitive matrix:** [`BARCODE_NICELABEL_BARTENDER_RESEARCH.md`](BARCODE_NICELABEL_BARTENDER_RESEARCH.md) M2 / M5 / M6
**Baseline:** P0 shipped — `BarcodeModuleWidthMm` + `LinearBarcodeModuleContract` + linear preflight + designer warning

This document was the **actionable “plan tiếp”** for the P1 coding session. It is intentionally narrower and more precise than the original P1 stub, and is now retained as the closure record rather than an unstarted next slice.

> P1 software gates are complete in the current checkout. The next open barcode phase is P3; P2 HRI placement is also closed as a software slice. Hardware verifier, printer-native command output, full GS1/catalog parity, and a clean owning implementation checkpoint remain open.

---

## 1. Problem statement

P0 lets operators author an X-dimension (mm) and fail-closes when quantized modules are unscannable.
**Legacy FrameOwned layout still fills the object frame:** bars are distributed across `WidthMm`; the completed `SizedFromX` path defines production width from effective X×logical modules when explicitly enabled.

NiceLabel/BarTender treat **X as the physical module** and overall width as a consequence of X×modules (plus quiet zones). ANLAbel must close that gap **without** breaking legacy frame-owned labels (`BarcodeModuleWidthMm == 0`).

### Critical code fact (must drive design)

`ZxingBarcodeRenderer.RenderBarcodeVector` sets:

```text
WidthModules = ZXing BitMatrix.Width  // pixel columns after scale-to-frame
```

Probe (Code128, various widths @ 203/300 DPI): `WidthModules ≈ round(widthMm/25.4*dpi)` and
`widthMm/WidthModules ≈ 25.4/dpi` (**one printer dot**).

Therefore:

- **Do not** use `vector.WidthModules` as logical module count for auto-width.
- **Do not** use it for legacy “estimate X from frame” industrial risk (false ~1-dot always).

---

## 2. Goals / non-goals

### Goals

1. **Logical module count** independent of frame width and print DPI scaling.
2. **SizedFromX** production width = quantized effective module mm × logical modules.
3. **Read-only effective module** mm / mils / dots @ plan DPI (same `Resolve` as preflight).
4. **Legacy-safe** FrameOwned default.
5. Honest legacy preflight estimate (or explicit skip)—no pixel-column lie.

### Non-goals

- HRI Above (P2), check-digit policy (P3), ratio/density controls (P4).
- Printer-native ZPL (P7), hardware grade (P8), full GS1 cert.
- New bar-height model field (docs/tooltip only).
- Text/TextBox contract changes.
- Claiming P0 incomplete.

---

## 3. Implementation slices (strict order)

### P1.0 — Logical module count (blocking)

| Item | Spec |
| --- | --- |
| API | e.g. `IBarcodeRenderer.CountLinearModules(...)` or Core helper calling encoder pure path |
| Input | payload, symbology, quietZoneModules, GS1 flags as needed |
| Output | positive int logical modules (include quiet zones consistently with print options) |
| Method | Minimum/pure ZXing encode (or equivalent)—**not** encode-to-target-mm then read width |
| Tests | Same payload → same count for “virtual” frames 20 mm and 60 mm; count changes when payload length changes |

### P1.0b — Fix legacy preflight estimate

When `BarcodeModuleWidthMm <= 0`:

```text
moduleMm = frameWidthMm / LogicalModuleCount(...)
resolution = LinearBarcodeModuleContract.Resolve(moduleMm, planDpi)
```

Remove / replace use of `vector.WidthModules` as totalModules for this estimate.

### P1.b — Effective module readout (ship early after P1.0)

Properties (linear selection, X>0 or after estimate):

| Field | Source |
| --- | --- |
| Effective X (mm) | `resolution.EffectiveModuleWidthMm` |
| Effective X (mil) | `effMm / 25.4 * 1000` |
| Module dots | `resolution.ModuleDots` |
| At DPI | `resolution.Dpi` (PrinterProfile first) |

Must match `BarcodeModuleSizeWarningText` / preflight.

### P1.a — SizedFromX width

| Mode | When | Width behavior |
| --- | --- | --- |
| **FrameOwned** | default; `X==0` or user chooses frame | User WidthMm; render fills frame (today) |
| **SizedFromX** | user enables + `X>0` | `WidthMm` (or production draw width) = `effMm * logicalCount` |

Wire:

- Model flag or convention documented in cloner/snapshot.
- `LabelVisualRenderer` 1D path + designer canvas parity.
- On payload / X / DPI change: recompute under one undo gesture when editing Properties.

Tolerance: within **one printer-dot** in mm at plan DPI:

```text
abs(width - effMm * N) <= (25.4 / dpi) + epsilon
```

### P1.c — Bar height clarity

- Tooltip: height is object frame; bars use `SymbolHeightMm` after HRI Below reservation (`BarcodeHriLayoutContract`).
- No second height property in this slice.

---

## 4. Named regression risks

| Risk | Mitigation |
| --- | --- |
| Using scaled `WidthModules` as logical N | P1.0 tests forbid frame-dependent count |
| Silent shrink of old templates | FrameOwned default; open/save fixture |
| Readout ≠ preflight | Single `Resolve` path |
| HRI Above later breaks width | Width formula independent of HRI vertical layout |
| Bound Excel payload changes module count | Recompute SizedFromX width; test one bound row |
| GS1 quiet zone | Logical count must include QZ modules used at print |

**Must stay green (P0):**

- `LinearBarcodeModuleContractTests`
- `linear barcode X-dim warning flags sub-2-dot modules`
- `print preflight blocks undersized linear X-dim at print dpi`
- `print preflight accepts comfortable linear X-dim`
- `preflight warns when barcode module too small at real print dpi`
- `barcode HRI reserves a shared symbol layout`
- `barcode application profile preflight`
- `gs1 industrial AI subset validates weight and variable fields`

---

## 5. Suggested file touch list (implementation)

| Area | Paths |
| --- | --- |
| Logical count | `ZxingBarcodeRenderer` (+ interface), maybe `LinearBarcodeModuleContract` helpers |
| Preflight | `PrintPreflightValidator.ValidateLinearBarcodeModuleAtPrintDpi` |
| Model | `LabelObject` width-mode flag; cloner; `DocumentSnapshot` |
| Render | `LabelVisualRenderer` 1D vector path |
| Designer | `LabelDesignerCanvas` barcode preview; `MainViewModel` readout; `MainWindow.xaml` |
| Tests | `LinearBarcodeModuleContractTests` + new logical-count tests; `ANLAbel.Tests/Program.cs` gates |
| Docs | This file; execution plan P1 checkbox; research M2/M6 rows |

---

## 6. Definition of done (P1 phase)

- [x] P1.0 logical count API + unit tests (`IBarcodeRenderer.CountLinearModules`, 2026-08-12)
- [x] P1.0b legacy estimate fixed (preflight uses logical count, not pixel `WidthModules`)
- [x] P1.b readout in Properties (`BarcodeEffectiveModuleReadoutText`)
- [x] P1.a SizedFromX render/designer/preflight consistent (`BarcodeWidthMode` + `LinearBarcodeProductionWidth`, 2026-08-12)
- [x] P1.c copy (X-dim tooltip notes bar height vs HRI strip)
- [x] Named foundation gates green (P0 + legacy logical preflight + SizedFromX gates)
- [x] Research matrix M2/M6 updated after SizedFromX
- [x] Execution plan P1 marked complete for software geometry slice

### Verification evidence (2026-08-13)

- `dotnet build ANLAbel.slnx --no-restore --nologo -v quiet -p:UseSharedCompilation=false -nodeReuse:false` — PASS, 0 warnings, 0 errors.
- `dotnet test src/ANLAbel.UnitTests/ANLAbel.UnitTests.csproj --no-build --nologo -v quiet` — PASS, 356/356.
- `dotnet run --project src/ANLAbel.Tests/ANLAbel.Tests.csproj --no-build` — PASS, exit 0; named P1 gates for logical modules, `SizedFromX`, compiled production width, and legacy `FrameOwned` all pass.

---

## 7. References

- Loftware Barcode: X dimension, height, actual printer properties, HRI placement
  https://help.loftware.com/cloud/Designer/Barcode.html
- BarTender X Dimension (mils): https://help.seagullscientific.com/10.1/en/content/mod_bar_xdim.htm
- BarTender Symbology and Size: https://help.seagullscientific.com/10.1/en/content/HIDD_BARCODEPAGE.htm
- GS1 UK POS size (nominal 0.33 mm): https://www.gs1uk.org/knowledge-hub/barcodes/how-big-should-a-point-of-sale-barcode-be
- Thermal practical ~0.19 mm / GS1 nominal 0.33 mm (industry guide)
  https://www.printerjournal.com/guides/barcode-printer/minimum-barcode-size-standards-limits-guide.html
