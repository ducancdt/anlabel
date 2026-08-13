# ANLAbel — Industrial barcode long-horizon execution plan

**Status:** Active execution spine (multi-session / multi-agent)
**Created:** 2026-08-12
**Repo of record:** `H:\00_REPOS_PROJECTS\ANLABEL` (keep Grok worktree in parity when both are used)
**Audience:** Implementers and agents executing barcode reliability work for industrial thermal label ops

This file is the **ordered backlog + “done when” gates** for barcode/ma vạch industrial reliability.
It does **not** replace:

| Doc | Role |
| --- | --- |
| [`BARCODE_UI_UX_PROGRAM_INDEX.md`](BARCODE_UI_UX_PROGRAM_INDEX.md) | Barcode P3-P8 UI/UX sequence, source/action ownership, read-only Figma routing and shared gates |
| [`BARCODE_NICELABEL_BARTENDER_RESEARCH.md`](BARCODE_NICELABEL_BARTENDER_RESEARCH.md) | Competitive gap matrix (M1–M22) and official NL/BT mechanics |
| [`barcode-notes.md`](barcode-notes.md) | Implementation history (engine, DPI quirks) |
| [`reinvention/07-execution-plan.md`](reinvention/07-execution-plan.md) | Whole-product reinvention phases |
| [`reinvention/09-designer-precision-and-industrial-reliability.md`](reinvention/09-designer-precision-and-industrial-reliability.md) | Designer precision + industrial reliability framing |
| `MASTER_PLAN.md` / `PLAN.md` | Product evolution history (do not rewrite Phase 1 history) |
| `Agents.md` / `agent.md` | Mandatory agent rules (including protected Text/TextBox) |

When research matrix status and this plan disagree, **update both in the same change** (matrix = competitive truth; this file = sequence + gates).

---

## 0. How to use this plan

1. Pick **one phase** (or one vertical slice inside a phase). Do not parallelize phases that share the HRI or X-dim model without an ADR.
2. Read **Baseline (already shipped)** and **Hard non-claims** before coding.
3. Implement only **In scope** for that phase; leave **Out of phase** alone.
4. Add/extend **named** unit + `ANLAbel.Tests` gates that drive the **shipped** entry points (`LinearBarcodeModuleContract`, `PrintPreflightValidator`, `BarcodeHriLayoutContract`, `PrintService.ValidateRows`, designer warning properties).
5. Update this plan’s phase status checkbox and the research matrix row(s) when a gate truly lands.
6. **Stop** when phase acceptance is met or a hard external blocker (hardware, vendor SDK license) is hit—document blocker under that phase, do not greenwash.

### Agent stop rules (every barcode session)

- Do **not** alter protected Text/TextBox industrial contracts (`Agents.md`) unless the user explicitly requests that contract change.
- Do **not** claim GS1 certification, ISO verifier grade, or full NL/BT catalog parity.
- Do **not** silently migrate legacy templates (e.g. force `BarcodeModuleWidthMm` onto old labels).
- Prefer pure Core contracts + preflight/render wiring; keep WPF free of re-implemented policy math.
- Prefer fail-closed industrial risk over silent stretch of sub-dot modules.

#### Current verified software checkpoint (2026-08-13)

The P1/P2 software status below was rechecked against the current checkout before this documentation reconciliation. The worktree is still dirty, so this is implementation evidence rather than a release approval:

| Gate | Result | Scope boundary |
| --- | --- | --- |
| `dotnet build ANLAbel.slnx --no-restore --nologo -v quiet -p:UseSharedCompilation=false -nodeReuse:false` | PASS · 0 warnings · 0 errors | Compile evidence for the current checkout. |
| `dotnet test src/ANLAbel.UnitTests/ANLAbel.UnitTests.csproj --no-build --nologo -v quiet` | PASS · 356/356 | Unit/contract evidence; not hardware evidence. |
| `dotnet run --project src/ANLAbel.Tests/ANLAbel.Tests.csproj --no-build` | PASS · exit 0 | Named P1/P2 barcode gates pass, including logical module count, `SizedFromX`, legacy `FrameOwned`, and HRI `Above`/`Below`/`None` geometry. |

The open program-level boundaries remain physical verifier/grade, printer-native command output, full GS1/catalog parity, and clean ownership of the dirty implementation wave.

---

## 1. Baseline — already shipped (do not re-open as greenfield)

### 1.1 1D X-dimension software slice (M2 / M6 partial → Have)

| Piece | Location / behavior |
| --- | --- |
| Authored module | `LabelObject.BarcodeModuleWidthMm` (mm); **0 = legacy** frame ÷ module columns |
| Quantize policy | `LinearBarcodeModuleContract` → whole printer dots at **print-plan DPI** |
| Floors | **≥ 2 module dots**; industrial floor **~0.19 mm** (~7.5 mil) on **effective** quantized width |
| Preflight | `PrintPreflightValidator.ValidateLinearBarcodeModuleAtPrintDpi` (fail-closed issue) |
| Designer warning | `MainViewModel.BarcodeModuleSizeWarningText` (linear path; uses `PrinterProfile.Dpi` first) |
| UI | Properties **X-dim (mm)** on linear barcodes |
| Persist | Clone + `DocumentSnapshot` |
| Matrix fixed module | Unchanged: sub-2-dot at print DPI for `FixedVersionAndModuleSize` QR/DM |
| HRI geometry | Shared `BarcodeHriLayout` / print `BarcodeHriTextLayout` (below strip) |
| GS1 subset | Application profile + AI registry subset + quiet-zone rules (not full wizard) |
| DPI parity | Plan DPI drives barcode render; tests forbid print-only object QrDpi for final output |

**Remaining partial/polish after the P0–P2 software slices (explicit):**

- Legacy **FrameOwned** rendering still fills the authored object frame; explicit **SizedFromX** production rendering uses effective X × logical modules without silently mutating legacy objects.
- The shared effective mm/mils/dots readout is shipped in Properties and preflight; optional continuous readout chrome remains polish.
- No dedicated mil unit editor field; authored X remains mm-only.

### 1.2 Named regression gates that must stay green

When touching barcode industrial code, re-run at least:

```text
dotnet build ANLAbel.slnx --no-restore
dotnet test src/ANLAbel.UnitTests/ANLAbel.UnitTests.csproj --no-build
dotnet run --project src/ANLAbel.Tests/ANLAbel.Tests.csproj --no-build
```

**Must continue to PASS** (names as in `ANLAbel.Tests` / unit suite):

| Gate | Why |
| --- | --- |
| `LinearBarcodeModuleContractTests` (unit) | X-dim quantize / reconstruct / DPI |
| `linear barcode X-dim warning flags sub-2-dot modules` | Designer warning |
| `print preflight blocks undersized linear X-dim at print dpi` | Fail-closed preflight |
| `print preflight accepts comfortable linear X-dim` | No false positive |
| `preflight warns when barcode module too small at real print dpi` | Matrix path |
| `barcode module size warning uses same dpi as real preflight` | DPI source parity |
| `barcode HRI reserves a shared symbol layout` | HRI strip contract |
| `barcode application profile preflight` | GS1 quiet zone + profile |
| `gs1 industrial AI subset validates weight and variable fields` | AI subset |
| `print barcode uses plan (real print) dpi` | Plan DPI render |

Protected Text/TextBox named gates (never collateral damage):

- `Text stays free while TextBox stays bounded`
- `text box does not resize object from text content`
- `text box reflows to fit frame when user resizes`
- `normal resize capture release does not cancel gesture`
- `new text box uses compact label-aware frame`
- `designer preview row keeps object geometry`

---

## 2. Hard non-claims (forever open unless a dedicated program lands)

These are **not** “done” and must not be marked complete in matrix or release notes without real evidence:

1. **Physical barcode verifier / ISO grade on live devices** (M20 hardware).
2. **Full GS1 AI wizard / complete AI registry / certification** (M15 beyond industrial subset).
3. **Full NiceLabel/BarTender symbology catalog** (circular, postal, etc.).
4. **Printer-native barcode command emission as default print path** (ZPL `^BC`, EPL, TSPL) without an explicit print-method product decision (M19).
5. **BarTender multi-data-source HRI + VB transforms** (M13).
6. Changing **Text/TextBox protected industrial contracts** as part of “barcode work.”

---

## 3. Gap → phase map (execution view)

| Phase | Primary matrix IDs | Theme | Depends on |
| --- | --- | --- | --- |
| **P0** | M2/M6 slice | **DONE** — X-dim mm + quantize + preflight | — |
| **P1** | M2 partial, M5, M6 chrome | **DONE** — physical geometry fidelity (opt-in auto-width, mils readout, bar-height clarity) | P0 |
| **P2** | M9, M10–M12 subset | HRI placement & layout (None / Below / Above) — **DONE** | P0; prefer before heavy HRI chrome |
| **P3** | M7, M8 | Check-digit policy + HRI show/hide check digit | P2 recommended |
| **P4** | M3, M4, M14 | Ratio / density presentation + quiet zone as physical mm | P1 (needs trustworthy X) |
| **P5** | M16 polish, M17 | 2D industrial parity (DM UI, naming) | Independent of P3 |
| **P6** | M15 growth | GS1 AI registry expansion (still not “certified”) | Keep fail-closed tests |
| **P7** | M19 | Printer-native vs graphic print method | Architecture ADR; after P1–P2 stable |
| **P8** | M20 | Hardware verifier adapters (optional program) | Vendor SDK + device lab |
| **Px** | M1, M18, M21 | Catalog growth, bearer bars, engine swap | Only when vertical demand is real |

---

## 4. Phases (ordered)

Phase status legend: `[ ]` not started · `[~]` partial / in progress · `[x]` phase acceptance met in product + tests.

---

### P0 — 1D X-dim + print-DPI quantize + preflight — `[x]` DONE

**Outcome:** Operators can author a physical module width; print path quantizes to whole dots; unscannable modules fail closed.

**In scope (shipped):** See §1.1.

**Out of scope (then and now):** Auto frame width, mil widget, native ZPL, hardware grade.

**Acceptance (met):** Unit quantize tests; preflight undersized/comfortable linear X; matrix gates unchanged; research industrial policy table present.

---

### P1 — Physical geometry fidelity (close remaining M2/M5/M6 software gaps) — `[x]` DONE (2026-08-12)

> **Completed implementation-slice record (2026-08-12 timed research):**
> [`P1_LINEAR_GEOMETRY_NEXT_SLICE.md`](P1_LINEAR_GEOMETRY_NEXT_SLICE.md)
> Research note: the timed scratch material is summarized in the closure record below; no standalone scratch file is required for the repository checkpoint.

> This linked file is retained as acceptance history. It is no longer the next coding slice; the next open barcode phase is P3.

**Outcome:** Operators can (1) trust a **logical module count** independent of frame stretch, (2) optionally size symbol **width from quantized X × modules**, (3) see **effective mm / mils / dots @ print DPI** from the same math as preflight, (4) understand bar height vs HRI strip—without silently resizing legacy labels.

**Why P1 is harder than the stub implied:**
`ZxingBarcodeRenderer.RenderBarcodeVector` returns `WidthModules = BitMatrix pixel width` after scaling into the target mm×DPI frame. Probe evidence shows `WidthModules ≈ round(widthMm/25.4*dpi)` and frame-derived `width/WidthModules ≈ one printer dot`. **That value is not a logical module count.** Auto-width and legacy frame-estimates must **not** use it as `totalModules` without a pure-encode seam.

#### Dependency order (implement in this order)

| Order | Slice | Gap | Blocking? |
| --- | --- | --- | --- |
| 1 | **P1.0** Logical module count API | foundation | **Shipped** — `CountLinearModules` pure encode |
| 2 | **P1.0b** Legacy preflight estimate uses logical count (or skips false 1-dot) | M2 honesty | **Shipped** |
| 3 | **P1.b** Effective module readout (mm / mils / dots / DPI) | M6 chrome | **Shipped** — Properties readout |
| 4 | **P1.a** SizedFromX width = effMm × logicalModules | M2 remaining | **Shipped** — `BarcodeWidthMode` + production width helper |
| 5 | **P1.c** Bar-height UI/docs only | M5 | **Shipped** tooltip |

#### In scope (detail)

| Slice | Work | Design rule |
| --- | --- | --- |
| **P1.0** | Ship `LogicalModuleCount(payload, symbology, quietZoneModules)` (Core or renderer) via **minimum/pure** encode—not frame-scaled `RenderBarcodeVector`. Unit: same payload → same count at 20 mm vs 60 mm “frame”. | Quiet-zone modules must match `BarcodeRenderOptions.QuietZoneModules` / object field used at print. |
| **P1.0b** | When `BarcodeModuleWidthMm == 0`, estimate X as `frameWidth/logicalCount` then `Resolve`; **never** `frameWidth/vector.WidthModules` for industrial risk. | Fail-closed only on true sub-2-dot / sub-0.19 mm modules. |
| **P1.a** | Width modes: **FrameOwned** (default; legacy) vs **SizedFromX** (requires X>0). Production width = `Resolve(X, planDpi).EffectiveModuleWidthMm * LogicalModuleCount`. Designer + `LabelVisualRenderer` use that width for 1D symbolRect when SizedFromX. | DPI source = preflight (`PrinterProfile.Dpi` then template). Bound payload changes recompute width under undo rules. |
| **P1.b** | Read-only Properties: eff mm, mils (`effMm/25.4*1000`), dots, DPI—from **one** `LinearBarcodeModuleResolution` shared with warning/preflight. | No second quantize formula in ViewModel. |
| **P1.c** | Tooltips/copy: bar height = object height − HRI strip (current `BarcodeHriLayoutContract` Below geometry). | **No** new height model field in P1. |

#### Out of phase for the original P1 slice (historical; do not expand that slice)

- HRI Above / placement enum (**P2**)—auto-width only touches **width**; current Below strip is safe.
- Check-digit / hide CD in HRI (**P3**).
- Wide/narrow ratio or Density as independent control (**P4** / M3–M4)—BT Density is inverse of X; present later as readout only if needed.
- Quiet-zone mm productization beyond using QZ in logical count (**P4.c**).
- Printer-native commands (**P7**), hardware verifier (**P8**).
- Mutating Text/TextBox contracts.

#### HRI interaction note (design, not implementation)

Print already sets `symbolRect.Width = object width` and reduces **height** for HRI Below. SizedFromX must not invent a second width channel in WPF-only code. P2 Above will move the strip; width formula stays `eff × modules`.

#### Acceptance outcomes (checkable — named gates)

| # | Gate (proposed name) | Pass condition |
| --- | --- | --- |
| 1 | `logical module count independent of frame width` | Unit: payload fixed; count(20 mm context) == count(60 mm context); ≠ scaled pixel width |
| 2 | `linear barcode width follows quantized X-dim` | SizedFromX: production width ≈ `EffectiveModuleWidthMm * logicalCount` within **one printer-dot mm** at plan DPI |
| 3 | `effective module readout matches preflight dots` | App: VM readout dots/mm match `PrintPreflightValidator` / `Resolve` for same object+DPI |
| 4 | `legacy zero X keeps frame-owned width on open` | Load template with `BarcodeModuleWidthMm==0`; WidthMm unchanged; no auto-shrink |
| 5 | P0 suite | All existing linear X + matrix module + HRI + GS1 gates still PASS |

#### Suggested verification

```powershell
dotnet build ANLAbel.slnx --no-restore
dotnet test src/ANLAbel.UnitTests/ANLAbel.UnitTests.csproj --no-build --filter "FullyQualifiedName~LinearBarcode|LogicalModule"
dotnet run --project src/ANLAbel.Tests/ANLAbel.Tests.csproj --no-build
# Required green: P0 gates + new P1 gates above
```

#### Historical stop conditions (before P1 landed)

- Stop when **P1.0 + P1.a + P1.b** gates pass and research matrix M2/M6 rows updated.
- P1.c copy-only may ship in the same PR.
- Do not start P2 enum work or P7 native path inside P1.
- If pure logical encode cannot be obtained from ZXing without engine change, **stop and ADR** before faking counts from scaled matrices.

#### P1 closure evidence (2026-08-13)

The current app regression run reports PASS for `linear barcode width follows quantized X-dim when SizedFromX`, `compiled scene print uses SizedFromX production width`, and `legacy frame-owned width not auto-sized when X is zero`. The same run also reports PASS for the shared HRI layout, HRI Above top-strip geometry, and clone/save placement gates listed under P2.

#### Competitor alignment (research)

| Source | Takeaway for P1 |
| --- | --- |
| Loftware X dimension + “actual properties based on selected printer” | X physical; printer-actual readout = P1.b |
| BarTender X Dimension (mils) + Density | Ship mils as display; density not a third size driver |
| GS1 nominal X 0.33 mm; thermal practical ~0.19 mm / 7.5 mil | Already in P0 floors; keep |

---

### P2 — HRI placement (None / Below / Above) — `[x]` DONE (2026-08-12)

**Outcome:** Human-readable text can be off, under, or above the symbol via one shared layout contract used by designer, preview, and print.

#### In scope

| Slice | Gap | Work |
| --- | --- | --- |
| P2.a | M9 | Replace bool-only mental model with placement enum (`None` / `Below` / `Above`); map legacy `ShowBarcodeText` on load. |
| P2.b | M9 | Extend `BarcodeHriLayoutContract` so Above reserves strip at top; Below keeps current geometry; None uses full frame for symbol. |
| P2.c | M10 | Keep font size pt path; ensure Above/Below do not break clip or print DPI. |

#### Out of phase

- Hide check digit in HRI (P3).
- Auto font scaling with frame (M11) unless trivial.
- UPC split digits / retail chrome (M12 deep).
- Per-data-source HRI visibility (M13).

#### Acceptance outcomes

1. App gate: HRI **Above** reduces symbol height from the top; **Below** matches current shared reservation; **None** restores full frame for bars.
2. Designer / preview / print use the **same** contract (no WPF-only strip math).
3. Existing `barcode HRI reserves a shared symbol layout` updated or extended; still PASS.
4. Save/load/clone preserves placement.

#### Suggested verification

```powershell
dotnet test src/ANLAbel.UnitTests --filter "FullyQualifiedName~BarcodeHri"
dotnet run --project src/ANLAbel.Tests/ANLAbel.Tests.csproj --no-build
# Expect: barcode HRI above reserves top strip (or equivalent named gate)
```

#### Stop conditions

- Stop when enum + shared layout + named gates pass.
- Do not invent a second HRI layout in the designer canvas.

---

### P3 — Check-digit policy + HRI check-digit display — `[ ]`

**Outcome:** Symbologies that allow optional check digits expose an explicit policy; HRI can show or hide the check digit without changing encoded symbol rules.

**Owner decision packet:** [`P3_BARCODE_CHECK_DIGIT_DECISION_PACKET.md`](P3_BARCODE_CHECK_DIGIT_DECISION_PACKET.md). Complete its D1-D5 sign-off (symbology, payload semantics, HRI copy/default, persistence and UI/Figma/runtime ownership) before coding; the packet is documentation-only.

**UI/UX handoff:** [`P3_BARCODE_CHECK_DIGIT_UI_HANDOFF.md`](P3_BARCODE_CHECK_DIGIT_UI_HANDOFF.md) · [`P3_BARCODE_CHECK_DIGIT_UI_SPEC.md`](P3_BARCODE_CHECK_DIGIT_UI_SPEC.md). These are pre-implementation design/ownership artifacts; no barcode Properties UI is claimed complete by these links.

#### In scope

| Slice | Gap | Work |
| --- | --- | --- |
| P3.a | M7 | Policy enum for Code 39 / ITF (None / Auto / Verify) as standards allow; Code 128 remains engine-mandatory. |
| P3.b | M7 | Preflight fail-closed on Verify failure; GS1 GTIN check remains via `BarcodeApplicationContract`. |
| P3.c | M8 | HRI display flag: include/exclude check digit characters in human-readable string only. |

#### Out of phase

- Full AI wizard (P6/P8).
- Bearer bars (Px).
- Changing GS1 FNC1 encode path except where check-digit display requires formatting.

#### Acceptance outcomes

1. Unit tests: policy transitions for at least one optional-check symbology.
2. Preflight blocks bad Verify payloads.
3. HRI-only hide does **not** change vector module pattern of the symbol.
4. GS1 application profile gates still PASS.

#### Suggested verification

```powershell
dotnet test src/ANLAbel.UnitTests --filter "FullyQualifiedName~BarcodeApplication|CheckDigit|Hri"
dotnet run --project src/ANLAbel.Tests --no-build
# Expect: check digit verify fail-closed; HRI hide check digit does not alter modules
```

#### Stop conditions

- Stop when Code 39 (or chosen first symbology) policy + HRI flag are tested end-to-end.
- Do not mark “all symbologies complete” without per-type coverage.

---

### P4 — Ratio, density presentation, physical quiet zone — `[ ]`

**Outcome:** Linear codes that support wide/narrow ratio expose it safely; quiet zones are reportable as **physical mm** at current X (not only module counts).

**UI/UX handoff/spec:** [`P4_BARCODE_RATIO_QUIET_ZONE_UI_HANDOFF.md`](P4_BARCODE_RATIO_QUIET_ZONE_UI_HANDOFF.md) · [`P4_BARCODE_RATIO_QUIET_ZONE_UI_SPEC.md`](P4_BARCODE_RATIO_QUIET_ZONE_UI_SPEC.md). These are pre-implementation design/contract artifacts; P4 is not claimed complete by these links.

#### In scope

| Slice | Gap | Work |
| --- | --- | --- |
| P4.a | M3 | Authored ratio where symbology allows (e.g. Code 39); clamp to legal set; wire into render options. |
| P4.b | M4 | Optional **display** of implied density from X (not a third independent size driver). |
| P4.c | M14 | Preflight: quiet zone width mm = QZ modules × effective module mm; warn/fail under industrial minimums for GS1 profiles. |

#### Out of phase

- Native printer QZ commands (P7).
- Catalog expansion (Px).

#### Acceptance outcomes

1. Changing ratio changes encoded geometry for a controlled fixture (test via vector/module pattern or width).
2. Quiet-zone preflight message mentions **mm** and/or modules and uses shared X resolution.
3. P0/P1 linear module gates still PASS.

#### Suggested verification

```powershell
dotnet run --project src/ANLAbel.Tests --no-build
# Expect: linear quiet zone physical mm preflight; ratio fixture gate
```

---

### P5 — 2D industrial parity (QR polish + Data Matrix UI) — `[ ]`

**Outcome:** Data Matrix gets first-class sizing/ECC controls analogous to QR where the engine supports them; QR UI copy aligns with industrial language (Symbol Version / Module).

**UI/UX handoff/spec:** [`P5_2D_BARCODE_PARITY_UI_HANDOFF.md`](P5_2D_BARCODE_PARITY_UI_HANDOFF.md) · [`P5_2D_BARCODE_PARITY_UI_SPEC.md`](P5_2D_BARCODE_PARITY_UI_SPEC.md). These are pre-implementation design/contract artifacts; P5 is not claimed complete by these links.

#### In scope

| Slice | Gap | Work |
| --- | --- | --- |
| P5.a | M16 | UI copy alignment; keep capacity preflight exact (already Have). |
| P5.b | M17 | DM size/EC controls + preflight for undersized modules at plan DPI (reuse matrix module policy). |

#### Out of phase

- Full 2D catalog (Aztec, MaxiCode, etc.) unless product priority changes.
- Native 2D printer commands (P7).

#### Acceptance outcomes

1. DM fixed-module path reports sub-2-dot risk at print DPI (same policy as QR).
2. Save/load/clone for new DM fields.
3. Existing QR capacity/undersized gates still PASS.

---

### P6 — GS1 AI registry growth (still not certification) — `[ ]`

**Outcome:** Broader industrial AI subset for common logistics labels; preflight remains fail-closed on invalid AI structure.

**UI/UX handoff/spec:** [`P6_GS1_AI_UI_HANDOFF.md`](P6_GS1_AI_UI_HANDOFF.md) · [`P6_GS1_AI_UI_SPEC.md`](P6_GS1_AI_UI_SPEC.md). These are diagnostics-first, pre-implementation artifacts; P6 is not a full GS1 wizard or certification claim.

#### In scope

- Grow `Gs1AiRegistry` / official snapshot coverage for demanded AIs.
- Keep FNC1 normalization and application-profile preflight.
- Tests for each newly claimed AI class.

#### Out of phase / non-claims

- “GS1 certified,” full Barcode Syntax Resource, BT AI wizard UX.
- Retail UPC split HRI.

#### Acceptance outcomes

1. New AIs accepted only with unit tests + one app preflight path.
2. Invalid AI / check digit still fails closed.
3. Matrix M15 stays **Partial** until a deliberate certification program (not this phase).

---

### P7 — Print method: graphic vs printer-native (thermal) — `[ ]`

**Outcome:** Explicit product choice between app-owned graphic barcode and optional vendor/native command path, without silent fallback.

#### In scope

- ADR: when native is allowed, which printers, which symbologies, evidence in job log.
- Emit path behind a **Print method** setting; default remains graphic for parity with designer.
- Preflight: native path unavailable → fail closed or force graphic with explicit operator message.

#### Out of phase

- Claiming native path as always better than graphic.
- Hardware grade (P8).

#### Acceptance outcomes

1. Job/manifest records print method + whether native commands were used.
2. No silent switch of queue or method.
3. Golden/regression for graphic path unchanged when method = graphic.

#### Stop conditions

- Requires architecture ADR + at least one real printer family pilot.
- External blocker (no device / no driver docs) → leave phase open; do not fake PASS.

#### UI/UX review package

- [`P7_PRINT_METHOD_UI_HANDOFF.md`](P7_PRINT_METHOD_UI_HANDOFF.md) records the ADR-first boundary, source evidence, Figma shell routing and native/non-native non-claims.
- [`P7_PRINT_METHOD_UI_SPEC.md`](P7_PRINT_METHOD_UI_SPEC.md) defines the Print & Output controls, state matrix, explicit fallback policy, AutomationIds and target-scale acceptance gates.

---

### P8 — Physical verifier / grade program — `[ ]` OPEN HARDWARE

**Outcome:** Optional adapter from print job evidence to a hardware verifier reading; never claim grade without device evidence.

#### In scope

- Keep/extend `BarcodeVerificationContract` / `PhysicalVerifierAdapter` seams.
- Hash-only evidence; timeout; busy guard (already partially shipped).
- Lab procedure doc + fixture correlation IDs.

#### Non-claims until devices exist

- ISO/ANSI grade on customer media.
- “Production certified” badges in UI.

#### Acceptance outcomes

1. Adapter timeout/cancel tests stay green.
2. End-to-end grade only when a real adapter returns signed evidence.
3. Software preflight **never** advertised as verifier grade (Help copy already warns).

#### UI/UX review package

- [`P8_PHYSICAL_VERIFIER_UI_HANDOFF.md`](P8_PHYSICAL_VERIFIER_UI_HANDOFF.md) records the hash-only evidence boundary, current Core/App source, Figma references and hardware non-claims.
- [`P8_PHYSICAL_VERIFIER_UI_SPEC.md`](P8_PHYSICAL_VERIFIER_UI_SPEC.md) defines the job-level state matrix, grade/manifest rules, redacted evidence surface, AutomationIds and target-scale gates.

---

### Px — Demand-driven backlog (not scheduled)

| ID | Item | Trigger to promote |
| --- | --- | --- |
| M1 | Symbology catalog growth | Customer vertical + engine validation |
| M11 | HRI auto font scale | Operator complaints on small frames |
| M12 | HRI offsets / UPC split | Retail vertical |
| M13 | Multi-source HRI | Multi-field barcode composition product |
| M18 | Bearer / guard bars | ITF-14 retail |
| M21 | Zint or second engine | ZXing hard limits on required symbology |

---

## 5. Cross-cutting quality rules

1. **One policy math:** module/X, quiet zone mm, and HRI strip geometry live in Core (or shared Printing contracts), not copy-pasted in XAML code-behind.
2. **Print-plan DPI** is the production authority; designer warnings must follow `PrinterProfile.Dpi` then template DPI (already required for module warnings).
3. **Fail closed** on industrial risk that would print unscannable symbols; do not stretch sub-dot modules silently.
4. **Legacy safe:** new fields default so old `.anlabel` files open with same visual size.
5. **Evidence:** every phase lands named tests + research matrix row update in the **same** change.
6. **Text/TextBox:** out of bounds for barcode goals unless user explicitly opens that contract.

---

## 6. Suggested multi-goal sequence (for future harness goals)

Use this order when spinning new implementation goals:

1. **P3** — check digit + HRI hide check digit.
2. **P4** — ratio + quiet zone mm.
3. **P5** — Data Matrix UI parity.
4. **P6** — GS1 AI growth (incremental).
5. **P7** — native print method (ADR-first).
6. **P8** — hardware verifier (lab-first).

P1 and P2 are closed software slices. Reopen them only for a regression or a separately approved product-scope change; do not treat their historical implementation plans as new work.

Each harness goal should implement **one phase or one slice**, point verification at this file’s acceptance list, and flip the phase checkbox only when gates pass.

---

## 7. File / ownership map (where to implement)

| Concern | Primary paths |
| --- | --- |
| X-dim / module policy | `src/ANLAbel.Core/Barcode/LinearBarcodeModuleContract.cs` |
| Model fields | `src/ANLAbel.Core/Models/LabelObject.cs`, cloner, `DocumentSnapshot` |
| HRI layout | `src/ANLAbel.Core/Barcode/BarcodeHriLayout.cs`, print `BarcodeHriTextLayout.cs` |
| GS1 / application | `BarcodeApplicationContract.cs`, `Gs1AiRegistry.cs` |
| Device bars | `DeviceBarcodeLayout.cs`, `DeviceDotQuantizer.cs` |
| Preflight | `PrintPreflightValidator.cs` |
| Render | `LabelVisualRenderer.cs`, `ZxingBarcodeRenderer.cs` |
| Designer warning / props | `MainViewModel.cs`, `MainWindow.xaml` |
| Unit tests | `src/ANLAbel.UnitTests/*Barcode*` |
| App gates | `src/ANLAbel.Tests/Program.cs` |
| Competitive truth | `docs/BARCODE_NICELABEL_BARTENDER_RESEARCH.md` |
| This spine | `docs/INDUSTRIAL_BARCODE_EXECUTION_PLAN.md` |

---

## 8. Definition of “industrial barcode program complete” (program-level)

The **program** (not a single PR) may be called industrially credible when:

- [x] Authored X-dim + print-DPI quantize + fail-closed preflight (P0).
- [x] Frame/width consistent with quantized X when operator opts in (P1 software slice; `FrameOwned` remains the legacy default).
- [x] HRI placement industrial triad None/Below/Above (P2 software slice; optional retail offsets remain open).
- [ ] Check-digit policy for primary linear optional types + HRI display policy (P3).
- [ ] Quiet zone expressible as physical mm at X (P4.c).
- [ ] DM industrial controls at parity with QR for target verticals (P5).
- [ ] GS1 subset covers agreed logistics AI set with tests (P6) — still not “certified.”
- [ ] Optional native path is explicit and evidenced (P7) **or** consciously deferred with ADR.
- [ ] Hardware grade either integrated with lab evidence (P8) **or** explicitly out of product scope.

Until then, market language must stay: **software preflight + graphic thermal path**, not “verifier certified.”

---

## 9. Change log (plan document only)

| Date | Change |
| --- | --- |
| 2026-08-12 | Initial long-horizon spine; P0 marked done after X-dim software slice. |
| 2026-08-12 | Timed research (≥30 min): expanded P1 with P1.0 logical-module foundation, legacy estimate hazard, ordered slices, named gates; added `P1_LINEAR_GEOMETRY_NEXT_SLICE.md`. |
