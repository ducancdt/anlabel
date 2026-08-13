# Barcode object model: NiceLabel + BarTender → ANLAbel gap matrix

**Research date:** 2026-08-11
**Repo of record:** `H:\00_REPOS_PROJECTS\ANLABEL`
**Scope:** Defining **barcode-object mechanics** (symbology, size/module/X-dimension, check digit, human-readable/HRI) — not a full symbology catalog rewrite, GS1 certification, or physical verifier campaign.
**Method:** Official product help only for competitors; ANLAbel status from shipped source inspection (paths listed below).

## Current status cross-check (2026-08-13)

The P1 and P2 software slices are now **Have** in the matrix: the current checkout passes the named logical-module, `SizedFromX`, legacy `FrameOwned`, and HRI placement gates. “Have” here means an implemented and regression-covered ANLAbel software path; it does not mean full NiceLabel/BarTender parity, hardware verifier grade, printer-native command support, or GS1 certification. The worktree remains dirty, so these results are not a release approval.

---

## 1. Official sources

### NiceLabel / Loftware

| Topic | URL |
| --- | --- |
| Barcode object (X dimension, height, ratio, check digit, HRI, bearer, details) | https://help.loftware.com/cloud/Designer/Barcode.html |
| Barcode (Help Center mirror) | https://help.nicelabel.com/hc/en-001/articles/4403719462545-Barcode |
| Available barcodes and settings | https://help.nicelabel.com/hc/en-001/articles/4403726070161-Available-Barcodes-and-Their-Settings |
| Contextual ribbon (X dimension, HRI placement) | https://help.nicelabel.com/hc/en-001/articles/4403726066961-Tabs-and-Ribbons |
| Label Objects overview | https://help.nicelabel.com/hc/en-001/articles/4402152643729-Label-Objects |

### BarTender / Seagull Scientific

| Topic | URL |
| --- | --- |
| Symbology and Size property page (symbology, X Dimension, ratio, density, height, check digit / code set) | https://help.seagullscientific.com/10.1/en/content/HIDD_BARCODEPAGE.htm |
| Human Readable property page (visibility, position, hide check digit, symbology-specific HRI) | https://help.seagullscientific.com/10.1/en/content/HIDD_HUMANREADABLEPAGE.htm |
| Human Readable (v12 mirror) | https://help.seagullscientific.com/12.0/en/Content/HIDD_HUMANREADABLEPAGE.htm |
| Fixed QR size (Symbol Version on Symbology and Size) | https://support.seagullsoftware.com/hc/en-us/articles/115013572627-How-to-set-the-size-of-a-QR-barcode-to-a-fixed-dimension |

### Secondary (support only, not primary claims)

- Seagull KB videos on HRI visibility per data source.
- In-repo: `docs/barcode-notes.md`, `docs/reinvention/01-competitive-benchmark.md`.

---

## 2. NiceLabel barcode object (defining mechanics)

NiceLabel models a dedicated **barcode object** with property groups (double-click → Object Properties).

### 2.1 Symbology selection

- **Barcode Type** selects the standard (default often Code 128).
- Large catalog of 1D and 2D; details differ per standard (`Available Barcodes and Their Settings`).
- Content must obey character set / length / AI rules for the chosen type.

### 2.2 Size / module (X-dimension)

| Control | Role |
| --- | --- |
| **X dimension** | Width of the **narrowest bar** (module unit for linear codes). |
| **Height** | Vertical size of the bar region. |
| **Ratio** | Wide:narrow bar ratio (type-limited; default often 3). Available ratios depend on current X dimension. |
| **Row height** | For multi-row 2D: multiple of X (e.g. `3x`). |
| **Actual properties based on selected printer** | Shows how X would print on the current printer (driver/DPI awareness). |

Resize of object frame interacts with anchoring; Keep aspect ratio may apply on Position/Size.

### 2.3 Check digit

| Control | Role |
| --- | --- |
| **Include check digit** | Whether check digit is part of the symbol. |
| **Auto-generate check digit** | Designer computes check digit; invalid supplied digit can be replaced. |
| **Verify the provided check digit** | Fail if user-supplied check digit is wrong. |
| **Display check digit in human readable** | Whether HRI shows the check digit. |

Check digit is standard-derived from preceding digits and typically is the final digit.

### 2.4 Human readable (HRI)

| Control | Role |
| --- | --- |
| **No / Above / Below** | Presence and vertical position of HRI. |
| **Custom Font** | Font/size for HRI; custom font forces **graphic** print path (not internal printer barcode element). |
| **Auto font scaling** | HRI font tracks barcode size (default on). |
| **Bold / Italic** | Style. |
| **Content mask** | Display-only mask for HRI (does not change encoded payload). |

HRI is backup when the symbol is damaged/unreadable.

### 2.5 Other (documented, lower priority for this matrix)

- Bearer bar (fixed/variable thickness, vertical bars).
- Details (quiet zones, inter-character gap, 2D EC/version, etc.).
- Position / relative position / lock / not printable.

---

## 3. BarTender barcode object (defining mechanics)

BarTender uses **Barcode Properties** with dedicated pages.

### 3.1 Symbology selection

- **Symbology** on **Symbology and Size** chooses the encoding scheme.
- **Symbology Specific Options** panel is **dynamic** (empty if multi-select mixed types).
- Examples: Code 128 → Check Digit (mandatory), Code Set, GS1-128, AI wizard; Code 39 → start/stop, check digit type; QR → Symbol Version, ECC, mask, etc.

### 3.2 Size / module (X-dimension)

| Control | Role |
| --- | --- |
| **X Dimension** | Width of the narrowest unit (bar/space); all symbologies except US Postnet. Advanced dialog for fine control. |
| **Ratio** | Wide-to-narrow ratio when the symbology allows choice. |
| **Density** | Bar/space density (related sizing control). |
| **Height** | Symbol height (disabled when symbology uses Row Height). |
| **Print Method** | Thermal: BarTender-controlled vs printer-native barcode elements. |

QR fixed size is set via **Symbol Version** on Symbology and Size (not only by stretching the object).

### 3.3 Check digit

- Exposed per symbology under **Symbology Specific Options** (`Check Digit`, sometimes mandatory).
- Code 128: check digit selected by default and **cannot be changed** (always on).
- Code 39: optional check digit + check digit type.
- EAN/UPC: mandatory check digit; guard bars / supplements as related options.

### 3.4 Human readable (HRI)

| Control | Role |
| --- | --- |
| **Visibility: Full / None / Set per Data Source** | Show all, hide, or per-data-source visibility. |
| **Placement / Alignment / Offsets** | Position relative to symbol. |
| **Hide Check Digit** | HRI may omit check digit while symbol still encodes it (subset of symbologies). |
| **Show Start/Stop** | Code 39 / Codabar HRI asterisks etc. |
| **Split / shrink UPC digits** | Retail HRI layout. |
| **GS1 Template** | Parentheses/spaces in GS1 HRI. |
| **Transforms** | Character template, search/replace, prefix/suffix, VB script (display path). |

---

## 4. Shared industrial mechanics (NiceLabel ∩ BarTender)

Both products treat the following as first-class barcode object design:

1. **Symbology** as a discrete choice with type-specific options.
2. **X-dimension / module** as the physical unit of bar width (not only “stretch the picture”).
3. **Symbol height** separate from X (linear codes).
4. **Check digit policy** (auto / verify / mandatory / optional) per standard.
5. **HRI** as optional interpretation with position, font, and check-digit display policy.
6. Awareness that **custom HRI fonts / graphic barcodes** may disable printer-internal barcode commands.
7. Deep **GS1 / AI** tooling (wizards, templates) for industrial logistics.

---

## 5. ANLAbel shipped surfaces (paths inspected)

Evidence base for the gap matrix (2026-08-11):

| Area | Paths |
| --- | --- |
| Renderer abstraction | `src/ANLAbel.Barcode/Renderers/IBarcodeRenderer.cs`, `ZxingBarcodeRenderer.cs`, `BarcodeVectorData.cs` |
| Render options | `src/ANLAbel.Barcode/Options/BarcodeRenderOptions.cs` (`QuietZoneModules`, GS1 flag, ECC) |
| Model | `src/ANLAbel.Core/Models/LabelObject.cs` (`BarcodeSymbology`, `ShowBarcodeText`, `BarcodeTextFontSizePt`, `QrModuleSizePx`, `QrQuietZoneModules`, QR version/ECC/sizing mode) |
| HRI geometry | `src/ANLAbel.Core/Barcode/BarcodeHriLayout.cs`, `src/ANLAbel.Printing/RenderPipeline/BarcodeHriTextLayout.cs` |
| Device/module layout | `src/ANLAbel.Core/Geometry/DeviceBarcodeLayout.cs`, `DeviceDotQuantizer.cs` |
| Application / GS1 | `src/ANLAbel.Core/Barcode/BarcodeApplicationContract.cs`, `Gs1AiRegistry.cs`, `BarcodeVerificationContract.cs` |
| QR geometry | `src/ANLAbel.Core/Barcode/QrObjectGeometryContract.cs`, `QrSizingMode.cs`, `QrCapacityTable.cs` |
| Print | `src/ANLAbel.Printing/RenderPipeline/LabelVisualRenderer.cs`, `PrintPreflightValidator.cs` |
| Designer UI | `src/ANLAbel.App/MainWindow.xaml` (Standard, Quiet zone, Show text, HRI font pt, Module px, QR options) |
| Scene/persist | `src/ANLAbel.Core/Scene/DocumentSnapshot.cs` |
| Tests | `BarcodeHriLayoutTests`, `BarcodeApplicationContractTests`, app suite barcode/HRI/module preflight gates in `ANLAbel.Tests/Program.cs` |
| Prior notes | `docs/barcode-notes.md` |

---

## 6. Gap matrix (mechanic → ANLAbel → status → next)

Legend: **Have** = shipped and used on real paths; **Partial** = exists but incomplete vs NiceLabel/BarTender defining UX; **Missing** = no first-class product surface.

| # | Mechanic (NiceLabel / BarTender) | ANLAbel surface | Status | Deferred / next action |
| --- | --- | --- | --- | --- |
| M1 | **Symbology selection** (large catalog, default Code 128) | `LabelObject.BarcodeSymbology` + grouped ComboBox in Properties; `ZxingBarcodeRenderer` map; tests for several 1D/2D | **Have** (subset of catalog) | Expand catalog only when engine + validation + UI exist; do not claim full BT/NL list |
| M2 | **X-dimension** as explicit physical module width (mm/mil) for 1D | **Shipped:** `BarcodeModuleWidthMm` + quantize/preflight; **`BarcodeWidthMode.SizedFromX`** sets production width = effMm × pure logical modules (`CountLinearModules` / `LinearBarcodeProductionWidth`); default **FrameOwned** keeps legacy; Properties checkbox “Size width from X × modules”; mil readout via effective-module line | **Have** (authored X + optional size-from-X) | Optional: mil unit field; bar-height-only model field |
| M3 | **Wide/narrow ratio** (Code 39 etc.) | Not exposed on model/UI; ZXing defaults | **Missing** | P4 review contract proposes a Code 39-first legal ratio policy; see [`P4_BARCODE_RATIO_QUIET_ZONE_UI_SPEC.md`](P4_BARCODE_RATIO_QUIET_ZONE_UI_SPEC.md) before implementation |
| M4 | **Density** (BarTender) | Not a separate property; density emerges from frame + engine | **Missing** | P4 keeps density read-only as a presentation of effective X/ratio, never a third independent control; see [`P4_BARCODE_RATIO_QUIET_ZONE_UI_SPEC.md`](P4_BARCODE_RATIO_QUIET_ZONE_UI_SPEC.md) |
| M5 | **Symbol height** independent of width (1D) | Object `HeightMm` / HRI strip reserves symbol height via `BarcodeHriLayoutContract` | **Partial** | Keep frame-owned height; document that height is frame-driven not “bar height only” unless HRI disabled |
| M6 | **Printer-actual X preview** (NiceLabel “actual properties based on selected printer”) | **Shipped:** preflight + Properties warning + **`BarcodeEffectiveModuleReadoutText`** (mm / mil / dots @ plan DPI) from same `Resolve` path | **Have** | Optional polish of readout chrome only |
| M7 | **Check digit include / auto / verify** | GS1 path validates GTIN check digit (`BarcodeApplicationContract`); no general Code 39 optional check-digit toggle; Code 128 check digit left to engine (mandatory in standards) | **Partial** | P3 review contract proposes a Code 39-first `None`/`Auto`/`Verify` policy; keep GS1 verify fail-closed. See [`P3_BARCODE_CHECK_DIGIT_UI_SPEC.md`](P3_BARCODE_CHECK_DIGIT_UI_SPEC.md) before implementation. |
| M8 | **Display check digit in HRI** (NL) / **Hide check digit** in HRI (BT) | HRI text = resolved payload string; no separate “show/hide check digit in HRI” flag | **Missing** | P3 review contract proposes a display-only HRI policy that cannot alter encoded modules. See [`P3_BARCODE_CHECK_DIGIT_UI_SPEC.md`](P3_BARCODE_CHECK_DIGIT_UI_SPEC.md); implementation remains open. |
| M9 | **HRI presence** (none / above / below) | `BarcodeHriPlacement` enum (`None` / `Below` / `Above`) on shared `BarcodeHriLayoutContract`; legacy `ShowBarcodeText` maps on load | **Have** (P2 2026-08-12) | Optional horizontal offsets / UPC split later (M12) |
| M10 | **HRI font + size** | `BarcodeTextFontSizePt`; Windows font path for graphic HRI | **Have** | Optional bold/italic later |
| M11 | **HRI auto font scaling with barcode size** | Fixed point size unless user edits; no auto-scale with frame | **Missing** | Optional auto-scale HRI when frame resizes (must not mutate TextBox text contract) |
| M12 | **HRI placement offsets / alignment** | Centered-in-strip style geometry; no horizontal/vertical offset fields | **Partial** | Add offsets only if production labels require retail-style UPC split |
| M13 | **HRI per-data-source visibility** (BarTender) | Single data value per barcode object | **Missing** | N/A until multi-source barcode concatenation UI exists |
| M14 | **Quiet zones** | `QrQuietZoneModules` + `BarcodeRenderOptions.QuietZoneModules`; GS1 profile raises linear QZ requirements | **Have** (module-count) | P4 review contract maps the logical value to physical mm from the shared effective X resolution; implementation remains open |
| M15 | **GS1 / AI encoding** | `BarcodeApplicationProfile.Gs1`, FNC1 normalize, AI registry subset, preflight | **Partial** (industrial subset, not full BT AI wizard) | Grow AI registry; no claim of full GS1 certification |
| M16 | **QR version / ECC / fixed module** | `QrSizingMode`, fixed version, `QrModuleSizePx`, capacity table, preflight blocks undersized frame | **Have** | Align naming with BT “Symbol Version” in UI copy |
| M17 | **Data Matrix size / EC** | Rendered via ZXing; less UI than QR | **Partial** | Parity controls when industrial DM is priority |
| M18 | **Bearer / guard bars** | Not first-class model | **Missing** | Defer unless ITF-14 retail is a target vertical |
| M19 | **Print method: printer-native vs graphic** | App-owned raster/vector graphic path; no ZPL/EPL native barcode command emit | **Partial** (graphic only) | Open: vendor printer fonts / native barcode commands |
| M20 | **Physical verifier / grade** | `BarcodeVerificationContract` / physical verifier tests exist as software contracts | **Partial** / **open hardware** | Hardware verifier remains open — do not claim complete |
| M21 | **Engine swap** | `IBarcodeRenderer` abstraction; ZXing implementation | **Have** (seam) | Zint optional later |
| M22 | **Designer / print DPI parity** | Plan DPI drives render; tests forbid using only object QrDpi for print | **Have** | Keep regression gates |

### 1D vs 2D snapshot

| Path | Strong today | Weak vs NL/BT |
| --- | --- | --- |
| **1D** (Code 128/39, EAN/UPC family in engine) | Symbology list, HRI on/off + font pt + **None/Below/Above placement**, GS1 profile for logistics, quiet zone modules, shared HRI geometry, **authored X-dim mm + print-DPI quantize/preflight + opt-in `SizedFromX` width** | Wide/narrow **ratio**, check-digit UI, check digit in HRI, hardware grade |
| **2D** (QR / DataMatrix) | Module px + quiet zone, QR sizing modes, capacity, undersized-frame preflight, ECC | DM UI parity; X-dim language in UI; native printer path |

---

## 7. Deferred / open (not claimed complete)

1. Full NiceLabel/BarTender symbology catalog and circular/postal variants.
2. Thermal **printer-native** barcode commands (ZPL `^BC`, etc.) vs app graphic.
3. Physical barcode **verifier hardware** grading workflows.
4. BarTender-style multi-data-source HRI visibility and VB transforms.
5. Full GS1 AI wizard / Barcode Syntax Resource complete registry (subset only today).
6. Bearer bars, UPC split digits, start/stop in HRI as first-class UI.
7. Optional polish of the continuous “actual X mils/dots on selected printer” readout; the shared effective-module line is already shipped in Properties and preflight.
8. Automatic width mutation remains out of the default behavior: explicit `SizedFromX` width is shipped, while legacy `FrameOwned` continues to own the drawn frame when X is zero.

### Industrial policy (shipped software slice — 2026-08)

| Rule | Value | Where enforced |
| --- | --- | --- |
| Authored 1D module | `BarcodeModuleWidthMm` (mm); `0` = legacy estimate from frame ÷ module columns | Model / Properties / clone / document snapshot |
| Quantize | whole printer dots at **min(printDpiX, printDpiY)** via `LinearBarcodeModuleContract.Resolve` | Preflight + designer `BarcodeModuleSizeWarningText` |
| Minimum module dots | **2** (aligned with fixed matrix module preflight) | Same contract; fail-closed preflight issue |
| Industrial X floor | **~0.19 mm** (~7.5 mil) on **effective** quantized module | Same contract (warn/fail after quantize) |
| Recommended default (docs) | **~0.33 mm** (~13 mil) when operator sets an explicit X | Not auto-written onto legacy objects |
| Not claimed | Printer-native barcode commands, ISO verifier grade, full GS1 AI wizard | Still open |

---

## 8. Recommended product sequencing

**Authoritative ordered phases + acceptance gates:** [`INDUSTRIAL_BARCODE_EXECUTION_PLAN.md`](INDUSTRIAL_BARCODE_EXECUTION_PLAN.md) (P0–P2 software slices closed; P3 next).
**P1 closure record:** [`P1_LINEAR_GEOMETRY_NEXT_SLICE.md`](P1_LINEAR_GEOMETRY_NEXT_SLICE.md) — retained for the logical-module and legacy-safety rationale; it is no longer an unstarted coding slice.

Research-level summary (same order):

1. ~~**1D X-dimension mm + device-dot quantize at print DPI**~~ **done (software slice M2/M6 / P0)**.
2. ~~**P1** — auto frame width from X×modules + effective mils readout~~ **done as an opt-in software slice** (M2/M6; legacy `FrameOwned` preserved).
3. ~~**P2** — HRI placement enum None/Below/Above on shared `BarcodeHriLayoutContract`~~ **done as a software slice** (M9).
4. **P3** — Check-digit policy for Code 39/ITF + HRI show/hide check digit (M7/M8); review the [`P3_BARCODE_CHECK_DIGIT_UI_SPEC.md`](P3_BARCODE_CHECK_DIGIT_UI_SPEC.md) before coding.
5. **P4+** — ratio, quiet-zone mm, DM UI, GS1 growth, native path, hardware verifier — review [`P4_BARCODE_RATIO_QUIET_ZONE_UI_HANDOFF.md`](P4_BARCODE_RATIO_QUIET_ZONE_UI_HANDOFF.md) and its spec before coding.

---

## 9. Relation to prior ANLAbel docs

- **`docs/INDUSTRIAL_BARCODE_EXECUTION_PLAN.md`** — **long-horizon execution spine** (ordered phases P0–P8, gates, non-claims). Use that file to implement multi-session barcode industrial work; this research file remains the competitive gap matrix (M1–M22).
- `docs/barcode-notes.md` — implementation history (ZXing, DPI rules); this file is the **competitive object-model** research.
- `docs/reinvention/01-competitive-benchmark.md` — platform-level benchmark; barcode object depth is expanded **here**.

---

## 10. Verification self-check

| Criterion | Evidence |
| --- | --- |
| NL barcode mechanics | §2 + Loftware/NiceLabel URLs in §1 |
| BT barcode mechanics | §3 + Seagull Symbology/Size + Human Readable URLs in §1 |
| Gap matrix | §6 with Have/Partial/Missing |
| Code-grounded status | §5 path list |
| Open items not greenwashed | §7 |
