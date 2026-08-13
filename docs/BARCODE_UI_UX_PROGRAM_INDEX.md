# ANLAbel — Barcode UI/UX program index

**Status:** documentation coordination index; no barcode UI implementation or Figma edit is authorized
**Date:** 2026-08-13
**Roadmap source:** [`INDUSTRIAL_BARCODE_EXECUTION_PLAN.md`](INDUSTRIAL_BARCODE_EXECUTION_PLAN.md)
**Competitive source:** [`BARCODE_NICELABEL_BARTENDER_RESEARCH.md`](BARCODE_NICELABEL_BARTENDER_RESEARCH.md)
**Figma routing template:** [`figma-ui-handoff-template.md`](figma-ui-handoff-template.md)
**P3 owner decision packet:** [`P3_BARCODE_CHECK_DIGIT_DECISION_PACKET.md`](P3_BARCODE_CHECK_DIGIT_DECISION_PACKET.md)
**P4 owner decision packet:** [`P4_BARCODE_RATIO_QUIET_ZONE_DECISION_PACKET.md`](P4_BARCODE_RATIO_QUIET_ZONE_DECISION_PACKET.md)
**Continuation checkpoint:** [`reinvention/10-continuation-handoff-2026-08-13.md`](reinvention/10-continuation-handoff-2026-08-13.md)
**Verification checkpoint:** [`reinvention/11-verification-checkpoint-2026-08-13.md`](reinvention/11-verification-checkpoint-2026-08-13.md)

This index coordinates the barcode UI/UX handoffs for P3–P8. The individual handoff and spec remain authoritative for each slice's state matrix and acceptance details. This file keeps phase order, source/action ownership, Figma references and non-claims aligned so that a research shell is not mistaken for a shipped WPF state.

## 1. Program status at a glance

| Slice | Operator task | Current source owner | Figma route | Status / next gate |
| --- | --- | --- | --- | --- |
| P1/P2 baseline | Author linear width/HRI geometry without breaking legacy objects | Core module/HRI contracts, `PrintPreflightValidator`, `PrintService`, MainWindow Properties | Panels `18:69` / `13:2` as density references; no dedicated barcode frame | Software gates are recorded as passed; do not reopen as greenfield. |
| P3 | Choose check-digit policy and HRI display without changing encoded modules | Barcode validation contracts and future MainWindow Properties policy surface | Panels file `18:69` / `13:2`; no barcode-specific state | [`P3_BARCODE_CHECK_DIGIT_UI_HANDOFF.md`](P3_BARCODE_CHECK_DIGIT_UI_HANDOFF.md) · approve Code 39-first policy, HRI copy, owner and runtime evidence. |
| P4 | Review ratio, derived density and physical quiet-zone measurement | Linear module/layout/preflight contracts and future Properties readout | Panels file `18:69` / `13:2`; no ratio/QZ state | [`P4_BARCODE_RATIO_QUIET_ZONE_UI_HANDOFF.md`](P4_BARCODE_RATIO_QUIET_ZONE_UI_HANDOFF.md) · approve first symbology and QZ convention before fields. |
| P5 | Distinguish QR version/ECC from Data Matrix size/EC semantics | Renderer capability, matrix geometry and standard-aware preflight | Panels file `18:69`; no 2D state | [`P5_2D_BARCODE_PARITY_UI_HANDOFF.md`](P5_2D_BARCODE_PARITY_UI_HANDOFF.md) · confirm renderer semantics and unavailable states. |
| P6 | Explain GS1 AI parsing, boundaries/FNC1 and registry provenance | `BarcodeApplicationContract`, `Gs1AiRegistry`, preflight and diagnostics | Panels file `13:2`; no GS1 state | [`P6_GS1_AI_UI_HANDOFF.md`](P6_GS1_AI_UI_HANDOFF.md) · confirm demanded AI classes, registry policy and runtime evidence. |
| P7 | Know whether dispatch is graphic or pilot-approved native output | `PrintService`, `PrintRenderPlan`, `PrintJobResult` and future method/adapter ADR | Shell `2:2`, Print & Output `2:39`; no method state | [`P7_PRINT_METHOD_UI_HANDOFF.md`](P7_PRINT_METHOD_UI_HANDOFF.md) · ADR, capability record and real printer-family pilot required. |
| P8 | Distinguish queue/preflight from physical verifier evidence and grade | `PhysicalOutputVerificationEvidence`, adapter/coordinator, manifest/state store and future Print Center/History detail | Shell `2:2`; Control Center History `3:85`; no verifier state | [`P8_PHYSICAL_VERIFIER_UI_HANDOFF.md`](P8_PHYSICAL_VERIFIER_UI_HANDOFF.md) · fixture, adapter, evidence and hardware gates required. |

All rows are design/review evidence, not release approval. `Current source owner` names where the authoritative policy or action lives today; it does not imply that the proposed control is implemented.

## 2. Dependency and sequencing rule

```text
P1/P2 shared geometry + HRI + print-plan evidence
          │
          ├── authoring diagnostics: P3 → P4 → P5 → P6
          │       (each preserves prior fields and legacy data)
          │
          └── dispatch/evidence: P7 → P8
                  (graphic baseline remains valid throughout)
```

The arrows are review dependencies, not claims that every phase must ship before another can be designed. Before a downstream slice changes a surface:

1. reuse the upstream policy/math/action owner instead of copying it into XAML or a second window;
2. keep authored geometry and existing Text/TextBox behavior unchanged unless the user explicitly reopens those contracts;
3. preserve `Graphic` as the parity baseline while P7 native output is open;
4. keep P8 physical claims separate from queue, thermal-golden, preflight and visual-audit evidence;
5. leave a slice open when standards, renderer capability, printer pilot or verifier hardware is missing.

P3–P6 are authoring/diagnostic slices and must not silently mutate existing barcode objects. P7 and P8 are job/output slices and must not become barcode Properties toggles.

## 3. Surface and action ownership

| Concern | Current/future owner | Reused by | Never duplicate |
| --- | --- | --- | --- |
| Linear module/X and HRI geometry | Core contracts plus `PrintPreflightValidator`/`LabelVisualRenderer` | P3, P4, P7, P8 evidence summaries | A second ratio, width or HRI math path in UI code. |
| Barcode authoring policy | MainWindow Properties + view-model bindings after an approved slice | P3–P6 | Print Center or verifier panel authoring controls. |
| Print method resolution | `PrintService` / future output-method adapter and manifest/job evidence | P7, P8 | A barcode-specific method toggle or silent queue/method fallback. |
| Physical verification | `PhysicalOutputVerifierCoordinator` / state store; future Print Center or History details | P8, support export, lifecycle completion | Queue monitor, preflight or operator action claiming physical output. |
| Figma visual reference | Existing shell/panels/History files, read-only until an owner names a state | All slices as appropriate | New file/frame created only to make a missing state look complete. |

The first host decision for P3–P6 is the existing WPF Properties surface. P7 belongs with printer/output setup. P8 belongs with selected-job evidence details. These are proposals until runtime owner and AutomationId evidence are approved.

## 4. Figma node map and missing-state rule

Metadata below was checked read-only on 2026-08-13. No node was edited, duplicated or treated as runtime proof.

| File / node | Measured evidence | Suitable use | Missing states |
| --- | --- | --- | --- |
| Panels file [`kqyNBI0DgRHnPzJTDBIui5`](https://www.figma.com/design/kqyNBI0DgRHnPzJTDBIui5), `18:69` | Selected Properties shell `300 × 700`, content cards around `284 DIP` | P3–P5 grouping, compact density and status placement | Barcode-specific check-digit/HRI, ratio/QZ, QR/DM and disabled/error variants. |
| Same panels file, `13:2` | Selected Properties shell `300 × 700`, content card `276 DIP` | P6 diagnostics grouping and compact utility rows | GS1 AI chips, boundary/FNC1 detail, registry provenance and preflight split. |
| Shell recreation [`zdN71qfzrYV6pPt1b2FRRc`](https://www.figma.com/design/zdN71qfzrYV6pPt1b2FRRc/ANLAbel-%E2%80%94-NiceLabel-Shell-Recreation), `2:2` | Full shell `1440 × 900`; Print & Output `2:39`; status bar `2:170` | P7 placement, printer/output grouping and status density | Method selector, capability evidence, resolved path, fallback, verifier device/grade. |
| Control Center file [`asnGsLMxceJWb3HlfaE3q4`](https://www.figma.com/design/asnGsLMxceJWb3HlfaE3q4), `3:85` | History shell `1280 × 800`; filters `3:99`; activity frame `3:101` | P8 row/detail hierarchy and provenance vocabulary | Real ANLAbel job rows, evidence details, verifier states and physical claims. |

If an existing node cannot answer a concrete state question, record either an owner-approved WPF reuse decision or the smallest state-specific reference needed. Do not call `get_design_context`, create a Figma file or write to an existing file for documentation-only work. A Figma frame is visual input; runtime screenshot/UI Automation and source-backed state are the acceptance evidence.

## 5. Shared acceptance gates

| Gate | Required evidence | Explicit non-claim |
| --- | --- | --- |
| Source truth | Named Core/App/Printing service, field provenance, stale/invalid/unsupported behavior and owner | Sample Figma copy is not product data. |
| One action owner | One path for Properties policy, print method, verifier, lifecycle and support export | No second dispatch, reprint, queue, grade or geometry authority. |
| Runtime | Screenshot/UI Automation at `1024 × 600`, `100%`, `125%`, `150%` (or documented environment exception) | Figma dimensions do not prove WPF reachability or clipping. |
| Accessibility | Stable AutomationIds/names, keyboard order, focus, disabled/error copy and scroll owner | Color/icon alone cannot communicate a blocked or unverified state. |
| Data safety | Legacy preservation, redaction, invalid/future-schema/cancel/error paths and no raw payload leakage | A green chip is not evidence of physical output. |
| Print parity | Preview, preflight, manifest and dispatch share one effective output contract | No native/physical/certification claim without external evidence. |
| Regression | Named application + unit/contract gates for each approved policy slice | Green software tests do not prove a printer pilot or verifier grade. |
| Documentation | Execution plan, research matrix, handoff/spec, Figma template and checkpoint agree | A committed Markdown package does not close an open hardware/ADR gate. |

## 6. Phase-specific close criteria

| Phase | Must be true before implementation closure |
| --- | --- |
| P3 | Check-digit policy is explicit, encoded modules stay unchanged by HRI display, invalid states fail closed and runtime copy is measured. |
| P4 | First symbology, ratio convention, density readout and physical quiet-zone convention are approved; legacy geometry is preserved. |
| P5 | QR/Data Matrix vocabulary is renderer-backed; unsupported states are explicit; fixed-module/DPI preflight remains authoritative. |
| P6 | Demanded AI classes, parser/boundary/FNC1 diagnostics and registry provenance are named; geometry/preflight remains separate. |
| P7 | ADR, capability record, requested/resolved method, explicit fallback and real printer-family pilot exist; graphic golden path is unchanged. |
| P8 | Real fixture/adapter evidence binds content and manifest, grade policy passes, timeout/busy/cancel paths are covered and any signed-evidence requirement is resolved. |

## 7. Open owner decisions

For the first open authoring slice, record D1-D5 in [`P3_BARCODE_CHECK_DIGIT_DECISION_PACKET.md`](P3_BARCODE_CHECK_DIGIT_DECISION_PACKET.md) before treating a Code 39-first policy, HRI copy/default, persistence shape or WPF/Figma/runtime owner as approved.

For the next geometry/diagnostics slice, record D1-D6 in [`P4_BARCODE_RATIO_QUIET_ZONE_DECISION_PACKET.md`](P4_BARCODE_RATIO_QUIET_ZONE_DECISION_PACKET.md) before treating a ratio legal set, density formula, quiet-zone convention, threshold or runtime owner as approved.

1. Select the first implementation slice (P3–P6 authoring or P7/P8 job evidence) and name its WPF owner.
2. Approve reuse of the existing Properties/shell/History references or identify the smallest missing Figma state.
3. Decide whether P7 native output is a product option or remains consciously deferred with an ADR.
4. Decide whether P8 requires signed device evidence in addition to the current hash-only Core contract.
5. Attach target-scale runtime evidence and named regression gates before changing any UI.

Until these decisions are recorded, the barcode UI/UX program is mapped but open. The next useful action is to select one slice and close its owner/runtime gate; broad Figma redesign or phase-wide implementation is not authorized by this index.

### P3 decision route

P3 is the first open authoring slice in this index. The source audit shows Code 39 and ITF renderer support but no check-digit policy or HRI masking field; the panels metadata shows no barcode-specific state. Use the [`P3_BARCODE_CHECK_DIGIT_DECISION_PACKET.md`](P3_BARCODE_CHECK_DIGIT_DECISION_PACKET.md) to capture the symbology, payload semantics, copy/default, persistence and WPF/Figma/runtime ownership before any implementation. Its Code 39-first option is a recommendation, not a product decision.

### P4 decision route

P4 is the next geometry/diagnostics slice. The source has logical quiet-zone modules, profile-level requirements and shared X quantization, but no ratio field, ratio renderer hint, density contract or physical-QZ result. Use the [`P4_BARCODE_RATIO_QUIET_ZONE_DECISION_PACKET.md`](P4_BARCODE_RATIO_QUIET_ZONE_DECISION_PACKET.md) to capture the legal symbology, ratio representation, density/QZ conventions, legacy-X behavior and WPF/Figma/runtime ownership before implementation. Its per-side Code 39 option is a recommendation pending a renderer/standards probe.
