# P6 GS1 AI diagnostics owner decision packet

**Status:** documentation-only owner gate; no parser, renderer, WPF or Figma change is authorized by this packet (2026-08-13)
**Execution spine:** [`INDUSTRIAL_BARCODE_EXECUTION_PLAN.md`](INDUSTRIAL_BARCODE_EXECUTION_PLAN.md) §P6
**Handoff:** [`P6_GS1_AI_UI_HANDOFF.md`](P6_GS1_AI_UI_HANDOFF.md)
**Specification:** [`P6_GS1_AI_UI_SPEC.md`](P6_GS1_AI_UI_SPEC.md)
**Program index:** [`BARCODE_UI_UX_PROGRAM_INDEX.md`](BARCODE_UI_UX_PROGRAM_INDEX.md)

## Purpose and decision boundary

P6 is the next open barcode authoring slice after the P5 2D parity review. This packet turns the existing diagnostics-first handoff into an owner-reviewable decision record. It does not claim that P6 is implemented, that every GS1 Application Identifier is supported, or that ANLAbel has GS1/ISO verifier certification.

The proposed surface explains the existing Core contract:

```text
human-readable `(AI)value(AI)value`
        -> strict parser + versioned registry
        -> AI/value/boundary diagnostics
        -> normalized encoder payload with derived FNC1 separators
        -> existing symbology, geometry, HRI and print preflight
```

The UI must not become a second parser, a raw FNC1 editor, a registry download client, or a replacement for geometry/preflight. The protected Text/TextBox contract is outside this slice.

## Decision summary

| Decision | Evidence-backed recommendation | Owner choice required |
| --- | --- | --- |
| D1. First AI demand | Start with the existing curated industrial families (`00`, `01`, `02`, logistics dates `11/12/13/15/16/17`, variable `10/21/30/37`, logistics locations `410`–`417`, measures `31xx`–`36xx`, and existing extended families). Keep official-snapshot fallback explicit rather than presenting it as curated UX coverage. | Approve the first labelled families and the release-by-release expansion rule. |
| D2. Notation and source | Keep strict parenthesized `(AI)value(AI)value` notation as the only authoring contract. Bound/preview values are resolved through the same Core validator; do not infer boundaries from raw concatenated digits. | Approve editable-vs-read-only treatment for direct data and bindings. |
| D3. Boundary/FNC1 copy | Show a read-only `[FNC1]` marker only where the registry says a variable field needs a separator before the next AI. Keep HRI parentheses and normalized encoder payload separate; never expose ASCII GS as invisible editable text. | Approve `[FNC1]` wording, tooltip and screen-reader label `FNC1 separator`. |
| D4. Registry provenance | Show curated version `ANL-industrial-subset-2026.08` and, when fallback is used, the bundled official snapshot version/source/last-modified/hash. Precedence stays curated first, official bundled fallback second; no runtime network fetch or unversioned fallback. | Approve visible provenance fields and packaged update/rollback policy. |
| D5. Validation ownership | AI syntax, registry, value, check-digit, date and boundary diagnostics remain distinct from symbology support, quiet-zone/module/HRI geometry and renderer preflight. All blocking decisions remain fail-closed at the existing Core/Printing owners. | Approve error severity and the cross-surface wording owner. |
| D6. Layout/state | Reuse the selected Properties shell only for grouping/status density. Recommend a compact disclosure with rows/chips, an encoder boundary preview and a separate geometry/preflight row; request a GS1-specific Figma node only if the shell cannot express these states. | Approve compact disclosure vs inline chips/list and the WPF host. |
| D7. Runtime closure | Require parser/unit fixtures, designer/preview/print-row parity, offline registry provenance evidence, keyboard/accessibility evidence and target-scale screenshots before P6 closure. | Name product, WPF/UI Automation, Core and print/preflight owners. |

## Source evidence and implications

| Evidence | What it proves | What it does not prove |
| --- | --- | --- |
| [`MainWindow.xaml`](../src/ANLAbel.App/MainWindow.xaml#L1888) `Application profile` control and [`#L1930`](../src/ANLAbel.App/MainWindow.xaml#L1930) `BarcodeApplicationValidationMessage` | The current WPF surface has an explicit profile selector and one combined validation text block. | It does not provide AI rows, boundary/FNC1 detail, registry provenance or a GS1-specific state. |
| [`MainViewModel.cs`](../src/ANLAbel.App/ViewModels/MainViewModel.cs#L3628) | Current application validation combines data and geometry/HRI messages for the selected object. | It does not establish that a new UI may duplicate or reinterpret the parser. |
| [`BarcodeApplicationContract.cs`](../src/ANLAbel.Core/Barcode/BarcodeApplicationContract.cs#L11) | Core owns supported GS1 symbologies, strict notation, normalization, separator insertion, value rules, check digits and required quiet-zone policy. | It is not a UI model or a complete GS1 wizard contract. |
| [`Gs1AiRegistry.cs`](../src/ANLAbel.Core/Barcode/Gs1AiRegistry.cs) | A versioned curated registry supplies boundary kinds and value patterns, with curated-first lookup. | The curated subset is not a complete GS1 registry. |
| [`Gs1OfficialRegistrySnapshot.cs`](../src/ANLAbel.Core/Barcode/Gs1OfficialRegistrySnapshot.cs) and [`Gs1OfficialRegistryBundle.cs`](../src/ANLAbel.Core/Barcode/Gs1OfficialRegistryBundle.cs) | An immutable, hashed, deterministic offline official snapshot supplies provenance and fallback definitions. | Bundling the snapshot is not permission to imply certification or automatic online updates. |
| [`PrintPreflightValidator.cs`](../src/ANLAbel.Printing/PrinterProfiles/PrintPreflightValidator.cs) | Application-profile checks are part of print preflight and must remain fail-closed. | A green AI row alone is not a printable/physically verified result. |
| [`BarcodeApplicationContractTests.cs`](../src/ANLAbel.UnitTests/BarcodeApplicationContractTests.cs) | Existing tests cover unknown-AI rejection, curated boundary decisions, official snapshot provenance, offline fallback and normalization. | They do not yet close the proposed UI/runtime gates or prove full registry coverage. |
| Read-only Figma panels file [`kqyNBI0DgRHnPzJTDBIui5`](https://www.figma.com/design/kqyNBI0DgRHnPzJTDBIui5), node `13:2` | The selected Properties shell is `300 × 700` with a `276 DIP` content card and compact utility/status density. | No GS1, AI-row, `[FNC1]`, registry or failure-state node exists in the inspected metadata. |

## Recommended ownership model

| Layer | Single authority | UI responsibility |
| --- | --- | --- |
| Source/binding resolution | Existing selected-object/binding pipeline | Display the resolved sample and stale/refresh state; do not persist a second parse result. |
| AI parsing and normalization | `BarcodeApplicationContract` plus `Gs1AiRegistry`/official snapshot | Render the returned segment, boundary and repair diagnostics. |
| Registry provenance | `Gs1AiRegistry` and immutable bundled snapshot | Show version/source/hash and whether a definition came from curated or official fallback data. |
| Geometry/HRI/renderer | Existing Core/Printing contracts and preflight | Show a separate status row and preserve current severity/blocking behavior. |
| Figma | Read-only metadata until a state-specific node is approved | Borrow grouping and density language only; never infer behavior from sample copy. |
| Runtime evidence | Product owner + WPF/UI Automation + Core/Printing test owners | Close fixtures, parity, accessibility and scale gates; record failures as open decisions. |

## State matrix for owner approval

| State | Required visible facts | Blocking rule | Repair/action |
| --- | --- | --- | --- |
| `General` or `Industrial` profile | GS1 diagnostics hidden/collapsed; existing profile scope remains visible | Existing profile contract applies | Select GS1 deliberately if the label requires AI notation. |
| `GS1` + empty/stale binding | Empty or stale source is explicit; registry provenance is still deterministic | Block affected row; never retain a previous green result | Enter, refresh or repair the source. |
| Valid curated AI payload | AI rows, value status, fixed/variable boundary, supported count and normalized preview are visible | Continue only after shared geometry/preflight passes | Continue or inspect the source. |
| Valid official-fallback AI | Exact AI, `official bundled fallback`, snapshot version and hash/source are visible | Continue only under the approved fallback policy | Prefer a curated definition in a later approved release; do not silently relabel provenance. |
| Unknown AI | Exact AI and registry version/source explain `not in supported subset` | Block; do not pass raw data through | Replace AI, choose an approved profile, or request a registry update. |
| Malformed notation | Segment position and expected `(AI)value` shape are explicit | Block before renderer invocation | Repair parentheses/AI/value in the source. |
| Invalid fixed value/check digit/date/length | AI, expected rule and actual failure are visible | Block that data result | Correct authored value; no auto-correction or coercion. |
| Variable boundary | `[FNC1]` appears only at the derived boundary before the next AI; raw control characters never appear | Block if the boundary cannot be determined | Edit parenthesized fields, not the separator marker. |
| Unsupported GS1 symbology | Selected standard and supported set (`Code 128`, `QRCode`, `DataMatrix`) are named | Existing fail-closed profile validation blocks print | Choose a supported standard or deliberately leave GS1. |
| Geometry/QZ/HRI failure | AI result remains visible while geometry/preflight gets its own severity and repair copy | Existing geometry/preflight rule remains authoritative | Repair quiet zone, module/DPI, HRI or frame settings. |
| Registry load/provenance failure | Package integrity/version problem is explicit; no silent unversioned list | Fail closed | Repair package/registry ownership before printing. |

## Fixture and regression packet

The owner should approve fixture names and expected outcomes before implementation. The following are proposed fixtures, not new tests in this docs-only change.

| Fixture | Expected diagnostic | Required parity/evidence |
| --- | --- | --- |
| Curated fixed identifiers: `(00)...`, `(01)...`, `(02)...` with valid check digits | Supported; fixed length; check digit passes | Core normalization, designer, preview and print-row status agree. |
| Curated logistics dates: `(11)`, `(12)`, `(13)`, `(15)`, `(16)`, `(17)` | Supported; six-digit date rule | Valid and invalid date fixtures fail closed with the AI named. |
| Variable fields: `(10)LOT-42(17)260630` and `(21)SERIAL(10)LOT` | Variable boundary shown with `[FNC1]` only where required | Normalized payload contains the derived group separator; HRI remains human-readable. |
| Curated logistics/location/measure classes: `30`, `37`, `410`–`417`, `31xx`–`36xx` | Supported with numeric/length/boundary copy | One valid and one invalid fixture for each newly claimed class. |
| Official fallback: `(253)...` outside the curated subset | Supported only with official bundled provenance/version/hash | Offline load, normalization and save/load/clone are deterministic. |
| Unknown AI: `(23)...` | Unknown in the approved registry/version | Block consistently; error names the registry version. |
| Invalid identifier/check digit | Exact AI and check-digit repair guidance | No auto-correction; encoded modules are not generated. |
| Invalid date/length/printable value | Exact AI and expected format/length | Designer, preview and print-row preflight use the same result. |
| Unsupported symbology | Separate application-profile error | AI rows do not hide or override the profile error. |
| Geometry/QZ/HRI failure with valid AI data | AI diagnostics remain valid; geometry row blocks as appropriate | Confirms data and geometry ownership are not collapsed into one unexplained string. |
| Empty/stale binding and preview-row change | Prior result becomes stale/empty until reevaluated | No stale green state, payload rewrite or frame mutation. |
| Offline registry provenance | Curated version and official snapshot source/version/hash are deterministic | No network request during authoring or print; package integrity failure is visible. |

## UI/Figma decision details

### Recommended initial layout

Use the existing selected-Properties shell (`13:2`) as a host-neutral density reference, with one compact GS1 disclosure:

```text
Application profile       [GS1]
Registry                  [curated version | bundled official fallback]
Notation                  [(AI)value(AI)value]
Parsed elements           [AI 01 ✓] [AI 10 ✓ variable] [AI 17 ✓ date]
Encoder boundary preview  [01]... [10]LOT-42 [FNC1] [17]260630
AI diagnostics            [3 supported · 0 unknown · 0 invalid]
Geometry / print          [Ready / separate blocking reason]
Repair                    [exact next action]
```

At `1024 × 600`, rows must stack or disclose; they must not clip. A bound value is a resolved sample, not a second editor. The `[FNC1]` marker is a read-only diagnostic token and must have a screen-reader label such as `FNC1 separator`.

### Figma reuse decision

The current read-only metadata is sufficient for a docs-only packet. It shows grouping, selected-object summary and compact cards, but no GS1 state. The recommended choice is **reuse shell density only, defer Figma write**, and create or request a GS1-specific state node only after D1–D7 are approved. A future node must include at least valid, unknown-AI, invalid-value, variable-boundary, geometry-failure and provenance states; a generic shell or sample barcode is not enough.

## No-go list

- Do not add a second parser, boundary guesser or UI-only check-digit/date implementation.
- Do not expose raw ASCII GS or permit manual FNC1 editing.
- Do not fetch the registry over the network during authoring or print, and do not fall back to an unversioned list.
- Do not present the bundled official snapshot as complete certification or as a claim of full GS1 coverage.
- Do not merge AI diagnostics with geometry, HRI, renderer or physical-verifier ownership.
- Do not rewrite payloads, bindings, frames or legacy profile data merely to display diagnostics.
- Do not alter Text/TextBox ownership, sizing, wrapping, clipping, padding, selection or print behavior.
- Do not edit Figma or treat metadata/sample values as runtime or physical-output evidence.

## Owner sign-off record

Record one owner, date and decision for each row. Blank rows keep P6 open.

| Decision | Owner | Date | Approved option / notes |
| --- | --- | --- | --- |
| D1. First AI families and labels | `TBD` | `TBD` | `TBD` |
| D2. Direct vs bound notation behavior | `TBD` | `TBD` | `TBD` |
| D3. `[FNC1]` copy and accessibility | `TBD` | `TBD` | `TBD` |
| D4. Registry precedence/provenance/update policy | `TBD` | `TBD` | `TBD` |
| D5. Diagnostics vs geometry/preflight ownership | `TBD` | `TBD` | `TBD` |
| D6. Host/layout and Figma reuse/state node | `TBD` | `TBD` | `TBD` |
| D7. Runtime, UI Automation and regression owners | `TBD` | `TBD` | `TBD` |

**Closure rule:** P6 may move from design/contract review to implementation only after D1–D7 are filled, the fixture packet is converted into named Core/UI/preflight gates, and target-scale runtime evidence is assigned. Until then, P6 remains an open diagnostics-first plan.
