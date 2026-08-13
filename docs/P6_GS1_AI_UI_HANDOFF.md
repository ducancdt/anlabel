# P6 GS1 AI diagnostics UI handoff

**Status:** pre-implementation handoff; design and contract review required (2026-08-13)
**Parent spine:** [`INDUSTRIAL_BARCODE_EXECUTION_PLAN.md`](INDUSTRIAL_BARCODE_EXECUTION_PLAN.md) §P6
**Competitive matrix:** [`BARCODE_NICELABEL_BARTENDER_RESEARCH.md`](BARCODE_NICELABEL_BARTENDER_RESEARCH.md) M15
**UI/UX specification:** [`P6_GS1_AI_UI_SPEC.md`](P6_GS1_AI_UI_SPEC.md)
**Figma rule:** use the selected-Properties shell in [`figma-ui-handoff-template.md`](figma-ui-handoff-template.md); no GS1-specific frame is recorded.

## 1. Operator task

An operator preparing a logistics barcode must be able to:

1. opt into the explicit `GS1` application profile without changing the authored payload behind the scenes;
2. enter or bind the supported human-readable notation `(AI)value(AI)value`;
3. see which AIs are recognized, whether each value has the expected boundary/format, and whether a check digit or FNC1 separator is required;
4. repair an unknown AI, malformed segment, invalid check digit, date/value length, quiet-zone or renderer error before preview/print.

This is a diagnostics-first surface for the curated industrial subset. It is not a full GS1 AI wizard, a complete Barcode Syntax Resource implementation, or a certification workflow.

## 2. Current source evidence

| Surface | Current evidence | P6 gap |
| --- | --- | --- |
| Profile control | [`MainWindow.xaml`](../src/ANLAbel.App/MainWindow.xaml#L1888) exposes `General`, `Industrial` and `Gs1` through `Application profile`; its tooltip already describes fail-closed production preflight. | The profile needs explicit GS1 help and a visible scope/provenance status, not just an enum label. |
| Current validation | [`MainWindow.xaml`](../src/ANLAbel.App/MainWindow.xaml#L1930) binds one `BarcodeApplicationValidationMessage`; [`MainViewModel.cs`](../src/ANLAbel.App/ViewModels/MainViewModel.cs#L3628) combines geometry and data errors. | Errors are actionable strings, but there is no segment-level state, supported-AI count, boundary explanation or separate encoder/HRI preview. |
| Authoring contract | [`BarcodeApplicationContract.cs`](../src/ANLAbel.Core/Barcode/BarcodeApplicationContract.cs#L11) requires strict parenthesized notation, validates supported symbologies, normalizes variable fields with ASCII GS separators and checks fixed identifiers/dates/values. | Keep this contract as the single parser/validator authority; the UI must not guess boundaries or duplicate validation rules. |
| Registry | [`Gs1AiRegistry.cs`](../src/ANLAbel.Core/Barcode/Gs1AiRegistry.cs) exposes a versioned curated subset; [`Gs1OfficialRegistryBundle.cs`](../src/ANLAbel.Core/Barcode/Gs1OfficialRegistryBundle.cs) loads a deterministic offline official snapshot. | Surface registry provenance/version and supported/unknown state without implying the app covers every GS1 AI. |
| Print path | [`PrintPreflightValidator.cs`](../src/ANLAbel.Printing/PrinterProfiles/PrintPreflightValidator.cs) validates the application profile before renderer validation and uses `IsGs1` for Code 128, QR and Data Matrix. | Keep preflight fail-closed and align designer/preview/print wording; never turn a UI green state into verifier grade. |
| Figma | Read-only metadata for panels file `kqyNBI0DgRHnPzJTDBIui5`, node `13:2`, reports a `300 × 700` Properties shell, `276 DIP` content card and compact status/utility rows. No GS1 or barcode state is present. | Use shell density/status language only; a Figma frame cannot prove parser, FNC1 or physical-label behavior. |

## 3. Proposed contract boundary

| Concern | Proposed boundary | Must not do |
| --- | --- | --- |
| Profile opt-in | `GS1` is an explicit application profile. General remains permissive legacy behavior; Industrial remains its existing linear-only geometry policy. | Do not auto-switch profiles from a symbology, data prefix or guessed parentheses. |
| Input notation | One strict display/authoring form: `(AI)value(AI)value`. Binding values are evaluated first, then the same parser is used. | Do not accept raw concatenated element strings by guessing fixed/variable boundaries. |
| AI registry | Show `Supported`, `Unknown`, and `Registry error` with the version/source used by the parser. | Do not claim complete GS1 coverage because the official snapshot is available. |
| Boundary/FNC1 | Show a read-only separator marker between variable-length fields when normalization inserts ASCII GS (`[FNC1]` in UI copy). | Do not let operators type or delete hidden separators as if they were ordinary payload characters. |
| Value validation | Preserve Core rules for fixed lengths, numeric/date patterns, printable bounds, and check digits. | Do not duplicate regex/check-digit math in XAML or silently coerce invalid values. |
| HRI vs encoded data | Keep human-readable parentheses and the normalized encoder payload separate; HRI display must not expose control characters. | Do not show ASCII GS as an invisible successful value or alter encoded modules to satisfy HRI copy. |
| Geometry/preflight | Keep quiet-zone, HRI, module/DPI and renderer checks in existing shared contracts. | Do not make AI diagnostics a replacement for print-plan preflight or physical verification. |
| Legacy templates | Missing profile/registry-era data keeps existing geometry and payload; adding a diagnostics surface must be read-only until the operator edits. | Do not rewrite old payloads, insert separators into saved text, or resize frames on profile selection. |
| Certification | Label all evidence as software parser/preflight evidence. | Do not claim GS1 certified, ISO verifier grade or full NiceLabel/BarTender AI wizard parity. |

## 4. State matrix

| State | Visible evidence | Safe next action | Print rule |
| --- | --- | --- | --- |
| General profile | Profile scope says legacy/permissive; GS1 diagnostics are not active | Keep General or opt into GS1 deliberately | Existing General behavior applies |
| GS1 selected, empty source | `Waiting for GS1 data`; registry version shown; no stale token result | Enter data or repair binding | Block the affected row; do not print an empty GS1 symbol |
| Valid GS1 payload | Parsed AI chips/rows, supported count, boundary markers, check-digit status and normalized preview | Inspect or continue to normal geometry/preflight | Continue if all shared checks pass |
| Unknown AI | Exact AI, registry version/source and `not in supported subset` reason | Replace AI, update approved registry, or choose General deliberately | Block; never pass unknown AI through as raw data |
| Syntax/parenthesis error | Segment position and expected `(AI)value` form | Repair the segment or binding | Block before renderer invocation |
| Invalid fixed value/check digit | AI, expected shape/length and repair hint; no partial green status | Correct value/check digit | Block affected row |
| Variable boundary | Read-only `[FNC1]` marker between the variable field and following AI | Edit the parenthesized values, not the control character | Renderer receives normalized GS1 data only after validation |
| Geometry/QZ/HRI error | Separate application-profile geometry message from AI parse status | Increase QZ, change HRI or repair the profile-specific setting | Existing fail-closed severity remains authoritative |
| Unsupported symbology | `GS1 supports Code 128, QR Code, and Data Matrix in this release` | Choose a supported standard or General | Block GS1 print |
| Bound row stale/unknown | Source/refresh state shown; prior valid parse marked stale | Refresh or repair binding | Do not retain a stale green parse |
| Registry provenance unavailable | Deterministic bundled snapshot/version and load diagnostic shown | Stop and investigate package integrity | Fail closed; no silent fallback to an unversioned list |

## 5. First-pass host-neutral wireframe

```text
[Barcode application]
Application profile       [GS1 ▼]
Scope                     [Code 128 · QR Code · Data Matrix]
Registry                  [ANL-industrial-subset-2026.08 · bundled]

GS1 data notation         [(01)09506000134352(10)LOT-42(17)260630]
Parsed elements            [01 ✓] [10 ✓ variable] [17 ✓ date]
Boundaries / encoder view  [01]... [10]LOT-42 [FNC1] [17]260630

AI diagnostics             [3 supported · 0 unknown · 0 invalid]
Geometry / print preflight [Ready]
Repair guidance            [No action]
```

When data is bound, the notation field is a read-only resolved sample and the source/binding owner remains elsewhere in the Properties card. The encoder view is a diagnostic representation; it must never be copied back into the human-readable value field.

## 6. Interaction and persistence rules

1. Selecting `GS1` reveals the diagnostics group and explains the strict notation before the operator edits data. It does not mutate the payload or geometry.
2. Source, binding or preview-row changes invalidate the current segment result until the same Core contract re-evaluates it.
3. The parser returns the exact AI/boundary/value diagnostics used by designer, preview and print. The UI may group or order errors, but it may not invent a different validity result.
4. Registry version/source is read-only metadata. A future registry update is a deliberate product/data change, not an automatic network fetch during authoring or print.
5. Check-digit and date failures remain blocking; the UI shows the failing AI and repair action without auto-correcting the bound value.
6. Save/load/clone/document snapshot preserve profile, payload, binding and geometry exactly. Diagnostics are derived, not persisted as a second truth.
7. Switching away from GS1 hides or de-emphasizes diagnostics but does not erase the authored notation. Returning to GS1 re-evaluates it.

## 7. Proposed AutomationIds and accessibility

These IDs are proposals until the owner selects the host and runtime implementation:

| Region/control | Proposed `AutomationId` | Accessible name |
| --- | --- | --- |
| Application profile | `Barcode.GS1.Properties.Profile` | Barcode application profile |
| GS1 diagnostics card | `Barcode.GS1.Properties.Diagnostics` | GS1 diagnostics |
| Registry provenance | `Barcode.GS1.Properties.Registry` | GS1 registry version and source |
| Notation/value | `Barcode.GS1.Properties.Notation` | GS1 AI notation |
| Parsed AI list | `Barcode.GS1.Properties.Elements` | Parsed GS1 application identifiers |
| Boundary/FNC1 view | `Barcode.GS1.Properties.Boundaries` | GS1 field boundaries |
| AI status | `Barcode.GS1.Properties.AiStatus` | GS1 AI validation status |
| Geometry status | `Barcode.GS1.Properties.GeometryStatus` | GS1 geometry and quiet-zone status |
| Repair guidance | `Barcode.GS1.Properties.Repair` | GS1 repair guidance |

Screen readers must announce the AI number, value status, whether a separator is required, and the next repair action. Control characters are represented by the spoken label `FNC1 separator`, never as an invisible character.

## 8. Figma routing and runtime evidence

The read-only metadata check for panels node `13:2` is sufficient for this documentation slice: `300 × 700` Properties shell, `276 DIP` content card, compact selected-object summary and utility rows. It contains no GS1/AI state. Reuse it only for grouping/status density after owner approval; request a state-specific node if the existing shell cannot answer the diagnostics task.

Before P6 implementation closure, runtime evidence must cover `1024 × 600`, `100%`, `125%` and `150%` for:

- valid multi-AI payload with a variable-field `[FNC1]` boundary;
- unknown AI and syntax error;
- invalid fixed value/check digit/date;
- empty/stale binding;
- unsupported symbology and geometry/QZ failure;
- registry version/source and repair copy.

Evidence must show that designer status, preview/preflight and print-row blocking agree. Figma metadata is visual input, not parser, certification or physical-output proof.

## 9. Owner decisions before coding

1. Confirm the first demanded AI families for the P6 industrial subset and their operator-facing labels.
2. Approve whether parsed AI rows are inline chips, a compact list, or a separate disclosure in the existing card.
3. Approve the visible `[FNC1]` representation and the human-readable-versus-encoder copy.
4. Confirm the registry provenance/version wording and update policy; no runtime network fetch is assumed.
5. Approve reuse of Figma `13:2`/`18:69`, or provide a GS1-specific state node.
6. Assign runtime screenshot/UI Automation and parser/preflight regression owners.

Until these decisions and named gates are recorded, P6 remains a diagnostics UI/UX handoff and not an implemented GS1 feature or certification claim.
