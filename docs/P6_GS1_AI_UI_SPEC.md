# ANLAbel — P6 GS1 AI diagnostics UI/UX specification

**Status:** documentation-only, pre-implementation UI/UX contract (2026-08-13)
**Execution spine:** [`INDUSTRIAL_BARCODE_EXECUTION_PLAN.md`](INDUSTRIAL_BARCODE_EXECUTION_PLAN.md) §P6
**Handoff:** [`P6_GS1_AI_UI_HANDOFF.md`](P6_GS1_AI_UI_HANDOFF.md)
**Owner decision packet:** [`P6_GS1_AI_UI_DECISION_PACKET.md`](P6_GS1_AI_UI_DECISION_PACKET.md)
**Research gap:** [`BARCODE_NICELABEL_BARTENDER_RESEARCH.md`](BARCODE_NICELABEL_BARTENDER_RESEARCH.md) M15
**Figma boundary:** selected-Properties shell from `13:2` / `18:69`; no GS1-specific frame is present

This document defines a diagnostics-first GS1 surface around ANLAbel's existing strict parser, versioned registry and fail-closed preflight. It does not add a full AI wizard, change Core validation, edit Figma, or claim GS1/ISO certification.

## 1. Product outcome

The operator should be able to distinguish:

1. the selected application profile (`General`, `Industrial`, or `GS1`);
2. the human-readable `(AI)value` source notation;
3. the parsed AI/boundary result and any inserted FNC1 separator;
4. the data error, geometry error and renderer/preflight error that blocks printing.

The UI is an explanation layer over the Core contract. It is not a second parser and must not infer a valid GS1 element string from raw digits.

## 2. Existing behavior and evidence

| Area | Current contract | UI implication |
| --- | --- | --- |
| Profile | `BarcodeApplicationProfile.Gs1` is explicit and supported for Code 128, QR Code and Data Matrix | Show scope and supported standards next to the profile, not only the enum value |
| Notation | `BarcodeApplicationContract` accepts strict `(AI)value(AI)value` notation | Use this as the only editable GS1 notation contract; do not expose raw separator editing |
| Normalization | Variable fields are joined with ASCII group separators for the renderer; parentheses remain useful for HRI | Provide a separate read-only encoder/boundary view with visible `[FNC1]` markers |
| Registry | Curated subset plus deterministic bundled official snapshot; `Gs1RegistryVersion` is exposed in Core | Show version/source as provenance, while clearly naming subset scope |
| Validation | Check digits, dates, numeric lengths, printable bounds, AI support, quiet zone and HRI geometry are fail-closed at their owning layers | Separate AI diagnostics from geometry/preflight status; one red string should not hide the owner of a failure |
| Figma | Node `13:2` is a `300 × 700` selected-Properties shell with `276 DIP` card content; `18:69` is a similar tabbed shell | Reuse density only after approval; no GS1 state is proven by metadata |

## 3. Host-neutral information architecture

```text
Barcode
  Standard / Application profile
    [Code 128 | QR Code | Data Matrix]
    [General | Industrial | GS1]

GS1 diagnostics (only when profile = GS1)
  Registry: ANL-industrial-subset-2026.08 · bundled snapshot
  Input notation: (AI)value(AI)value
  Parsed elements: AI / value / boundary status
  Encoder boundary preview: [FNC1] markers, never raw control characters
  AI result: supported / unknown / invalid / stale
  Geometry result: quiet-zone / HRI / module checks
  Repair: exact AI + action
```

The group may be a disclosure inside the existing Barcode card. At `1024 × 600`, parsed elements and repair guidance must stack rather than clip. A bound source displays a resolved sample and source identity; it does not turn the binding into a second editor.

## 4. State contract

| State | Copy/data requirements | Severity | Action |
| --- | --- | --- | --- |
| Profile not GS1 | Hide or collapse GS1 diagnostics; preserve existing General/Industrial copy | Neutral | Select GS1 deliberately if required |
| GS1 empty | `Waiting for GS1 data`; registry provenance remains visible | Block row | Enter/refresh data |
| Valid | AI rows identify supported status and boundary; normalized preview shows `[FNC1]` only where required | Ready | Continue to shared preflight |
| Unknown AI | `AI #### is not in supported registry <version>` | Block | Replace AI or choose an approved profile |
| Malformed notation | Identify segment offset and expected `(AI)value` form | Block | Repair parentheses/field value |
| Invalid value | Identify AI and expected numeric/date/length/check-digit rule | Block | Correct value; no auto-correction |
| Geometry failure | Show profile, symbology, quiet zone/HRI issue separately from AI row status | Block/warn per existing contract | Repair geometry |
| Renderer unsupported | Name supported GS1 standards and selected standard | Block | Choose Code 128/QR/Data Matrix or General |
| Stale binding | Mark prior parse stale and show refresh/field action | Block | Refresh or repair source |
| Provenance failure | Show bundled snapshot/version or package-integrity error | Block | Repair package/registry ownership; no unversioned fallback |

## 5. Data/interaction rules

1. Profile changes are explicit and non-destructive. They never rewrite text, binding, frame, quiet zone or HRI fields.
2. All data states call the same `BarcodeApplicationContract.ValidateData`/normalization path used by preflight. UI grouping may add context but not a competing validity result.
3. Parsed AI status is derived on each source/preview-row change. Stale status is never presented as current success.
4. Fixed and variable boundaries are derived from the registry definition. `[FNC1]` is a diagnostic label, not an editable payload character.
5. Check-digit/date/length failures display the failing AI and expected rule; values remain authored data until the operator edits them.
6. HRI may use human-readable parentheses, but encoded GS separators never appear as invisible text or alter HRI semantics.
7. Diagnostics are not persisted as authoritative data. Save/load/clone preserve the existing profile/payload/binding/geometry; the panel re-evaluates on load.
8. Registry version/source is displayed read-only and changes only through an explicit packaged registry update.

## 6. Proposed controls and AutomationIds

| Control | Proposed `AutomationId` | Accessible name | Kind |
| --- | --- | --- | --- |
| Profile selector | `Barcode.GS1.Properties.Profile` | Barcode application profile | ComboBox |
| Diagnostics disclosure | `Barcode.GS1.Properties.Diagnostics` | GS1 diagnostics | Expander/card |
| Registry provenance | `Barcode.GS1.Properties.Registry` | GS1 registry version and source | Read-only status |
| Notation sample/editor | `Barcode.GS1.Properties.Notation` | GS1 AI notation | Text/value |
| Parsed element list | `Barcode.GS1.Properties.Elements` | Parsed GS1 application identifiers | List/status |
| Boundary preview | `Barcode.GS1.Properties.Boundaries` | GS1 field boundaries | Read-only status |
| AI result | `Barcode.GS1.Properties.AiStatus` | GS1 AI validation status | Status |
| Geometry result | `Barcode.GS1.Properties.GeometryStatus` | GS1 geometry and quiet-zone status | Status |
| Repair guidance | `Barcode.GS1.Properties.Repair` | GS1 repair guidance | Status/action |

Keyboard order should be profile → source/value → diagnostics disclosure → parsed elements → geometry/preflight → repair. Screen readers must announce the AI, result, boundary kind and next action; they must not silently discard a control character.

## 7. Runtime evidence gates

P6 may be marked implemented only when:

1. the approved AI subset has one valid and one invalid fixture per newly claimed class;
2. unknown AI, syntax, check-digit, date/length and variable-boundary states are visible and fail closed;
3. the same result appears in designer, preview and print-row preflight;
4. profile switching is non-destructive and legacy templates round-trip without geometry/payload migration;
5. registry version/source is visible and deterministic offline behavior is tested;
6. supported/unsupported symbology and geometry/QZ errors remain separate from AI diagnostics;
7. runtime evidence covers `1024 × 600`, `100%`, `125%`, `150%`, keyboard/focus order and wrapped repair copy;
8. P0–P5 barcode gates and protected Text/TextBox gates remain green;
9. no full GS1 registry, certification, verifier grade or native-printer claim is added without external evidence.

Suggested verification remains:

```powershell
dotnet build ANLAbel.slnx --no-restore --nologo -v quiet -p:UseSharedCompilation=false -nodeReuse:false
dotnet test src/ANLAbel.UnitTests/ANLAbel.UnitTests.csproj --no-build --nologo -v quiet
dotnet run --project src/ANLAbel.Tests/ANLAbel.Tests.csproj --no-build
```

## 8. Explicit non-goals

- full GS1 AI wizard or complete Barcode Syntax Resource coverage;
- arbitrary raw element-string parsing or manual FNC1 editing;
- automatic check-digit correction or silent data coercion;
- GS1/ISO verifier certification, physical grade or hardware claims;
- changing the existing profile, parser, renderer, HRI, quiet-zone or frame ownership contract in this docs-only slice;
- a new Figma frame solely to satisfy this specification;
- any Text/TextBox ownership, sizing, wrapping, clipping, padding, resize or print-contract change.

Until the owner approves the first AI classes, segment presentation, `[FNC1]` copy, registry update policy and runtime evidence owner, P6 remains a UI/UX specification.
