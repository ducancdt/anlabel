# P3 barcode check-digit / HRI UI handoff

**Status:** pre-implementation handoff; design review required
**Parent spine:** [`INDUSTRIAL_BARCODE_EXECUTION_PLAN.md`](INDUSTRIAL_BARCODE_EXECUTION_PLAN.md) §P3
**Competitive matrix:** [`BARCODE_NICELABEL_BARTENDER_RESEARCH.md`](BARCODE_NICELABEL_BARTENDER_RESEARCH.md) M7/M8
**Figma rule:** use [`figma-ui-handoff-template.md`](figma-ui-handoff-template.md) and an existing reference first; the current panels page has no dedicated barcode-Properties frame.

## 1. Operator task

An operator editing a Code 39 barcode must be able to answer two separate questions without changing the protected Text/TextBox contract:

1. Should the optional check digit be **None**, **Auto**, or **Verify**?
2. Should the human-readable interpretation show or hide the check-digit characters while the encoded symbol stays unchanged?

The first slice is intentionally Code 39 (or one explicitly chosen optional-check symbology). Do not imply that one implementation closes check-digit behavior for every barcode type.

## 2. Current source evidence

| Surface | Current evidence | Gap for P3 |
| --- | --- | --- |
| Barcode Properties | `MainWindow.xaml` currently exposes application profile, HRI placement, HRI size, X-dimension, effective-module readout and validation/warning text in the barcode card. | No first-class check-digit policy or HRI check-digit display control is present. |
| Model/persistence | `LabelObject` persists `BarcodeApplicationProfile`, `BarcodeHriPlacement`, `ShowBarcodeText`, `BarcodeTextFontSizePt`, `BarcodeModuleWidthMm` and `BarcodeWidthMode`; clone/snapshot paths carry those fields. | Add new fields only through an explicit Core contract, clone/save/load identity and migration tests. Do not overload `ShowBarcodeText` to mean check-digit policy. |
| GS1 validation | `BarcodeApplicationContract` validates the existing GS1/application-profile path. | Preserve GS1 behavior; P3 must not silently rewrite the FNC1 or full AI path. |
| Shared HRI geometry | `BarcodeHriLayoutContract` and `BarcodeHriTextLayout` already share None/Below/Above geometry. | HRI check-digit display changes text content only; it must not create a second strip/layout policy. |
| Existing regressions | The runner passes shared HRI, HRI Above, clone/save placement, X-dimension, `SizedFromX` and legacy `FrameOwned` gates. | Add P3 named gates without weakening those existing tests. |

## 3. Proposed contract boundary

The implementation owner must confirm these semantics before coding:

| Concern | Proposed boundary | Must not do |
| --- | --- | --- |
| Check-digit policy | A typed per-object policy for the selected optional symbology: `None`, `Auto`, `Verify`. Code 128 remains engine-mandatory; GS1 checks remain under `BarcodeApplicationContract`. | Do not infer policy from a generic `ShowBarcodeText` boolean or from HRI placement. |
| Encoded payload | `Auto` may derive the symbol check digit; `Verify` rejects an invalid supplied digit before render/preflight; `None` leaves the optional check-digit behavior disabled. | Do not let an HRI-only toggle alter the encoded module pattern. |
| HRI display | A separate display-only flag/mode controls whether the check-digit characters appear in HRI. | Do not mutate the payload, GS1 separators, or barcode vector when hiding HRI characters. |
| Invalid data | Verify failure is a named preflight issue and blocks print; the UI must show a repair action. | Do not render a “green” symbol with a failed Verify state or silently fall back to Auto. |
| Legacy files | Missing fields use the documented legacy-safe default for the selected symbology; existing `ShowBarcodeText` and HRI placement keep their meanings. | Do not rewrite authored barcode geometry or migrate all old objects to a new check-digit policy. |

## 4. UI states to design

The first UI proposal should be a compact section inside the existing barcode Properties card, not a new top-level tab:

| State | Visible controls | Evidence / safe action |
| --- | --- | --- |
| Optional-check symbology selected | `Check digit: None / Auto / Verify`; HRI display `Show / Hide check digit` enabled only when meaningful | Show the current policy and whether the payload is complete; changing HRI display must leave the symbol geometry/hash unchanged. |
| `Verify` + valid payload | Policy remains `Verify`; validation line is neutral/valid | Allow preview/print after the normal preflight path. |
| `Verify` + invalid payload | Policy remains `Verify`; danger message names the invalid check digit and points to the payload field | Block print; do not silently change policy or payload. |
| `Auto` | Policy remains `Auto`; indicate that the symbol owns the generated digit | Preview/print uses the generated digit; HRI visibility is independent. |
| Non-optional symbology (for example Code 128) | Hide or disable optional-check controls with a short reason | Keep the engine-mandatory behavior explicit; do not expose a no-op selector. |

The panel must retain the existing frequency-first order: summary → content/application → check-digit/HRI policy → X-dimension/effective readout → validation. Exact grouping and copy remain an owner decision after the Figma review.

## 5. Figma handoff requirement

Existing references are useful for spacing and progressive disclosure, but they do not answer the barcode-specific states:

- `18:69` / `13:2` can provide the selected-Properties shell and compact card language.
- Ribbon text layer `1:8` is only a navigation hint and must not be treated as a barcode Properties design.
- No dedicated barcode-Properties frame is currently recorded on panels Page `0:1`.

Before implementation, the owner must choose one of these paths in the handoff template:

1. **Reuse existing WPF/Properties language:** record the exact node used for layout reference, the new controls/states, and why no new Figma component is needed; or
2. **Create/locate a state-specific reference:** record the file/node, measured card/control dimensions, and state variants for None/Auto/Verify plus HRI show/hide.

Do not edit Figma merely to make a screenshot look complete. The runtime screenshot/measurement at target window and display scales is the acceptance artifact.

### Read-only Figma node scan (2026-08-13)

The panels file was checked at Page `0:1` after selecting P3 as the next UI slice. The page contains the compact ribbon (`1:2`, with the `Text TextBox Image Barcode` navigation text at `1:8`), frequency-first panels (`8:2`), selected Properties (`13:2`), tabbed Properties (`18:69`) and Excel verification (`22:82`). No barcode-specific Properties frame, check-digit control, HRI mask state, or optional-symbology state is present in that page metadata.

**Interim routing decision:** keep the existing selected-Properties language and spacing as the reference (`18:69` for the tabbed card, `13:2` for the compact utility pattern), and treat the P3 controls as an implementation-owned extension until an owner either approves that reuse explicitly or supplies a state-specific Figma node. This is a design-input decision, not a claim that the WPF UI is implemented. Do not create or edit a Figma frame solely to satisfy this handoff.

## 6. Regression and acceptance gates

Required software gates for this slice:

- unit coverage for at least one optional-check symbology's None/Auto/Verify transitions;
- Verify invalid payload fails closed in preflight;
- HRI hide/show changes only displayed HRI text, not the encoded vector/module pattern;
- save/load/clone/document snapshot preserve the new policy and HRI display mode;
- existing GS1/application, HRI placement, X-dimension, `SizedFromX`, legacy `FrameOwned`, Text and TextBox gates remain green;
- WPF runtime screenshot or UI Automation at `1024×600`, `100%`, `125%` and `150%` (or a documented environment exception), including valid, invalid and non-optional states;
- preview/print parity is checked for the barcode path; physical verifier/native-printer evidence remains explicitly open.

Suggested commands remain:

```powershell
dotnet build ANLAbel.slnx --no-restore --nologo -v quiet -p:UseSharedCompilation=false -nodeReuse:false
dotnet test src/ANLAbel.UnitTests/ANLAbel.UnitTests.csproj --no-build --nologo -v quiet
dotnet run --project src/ANLAbel.Tests/ANLAbel.Tests.csproj --no-build
```

## 7. Explicit non-goals

- full symbology-wide check-digit parity;
- full GS1 AI wizard or certification;
- bearer bars, UPC split digits, ratio/density controls or printer-native barcode commands;
- changing any Text/TextBox ownership, sizing, wrapping, clipping, padding, resize lifecycle or print contract;
- claiming a Figma frame or software preflight as physical verifier grade.

## 8. Open owner decisions

1. Confirm Code 39 as the first optional-check symbology, or name the alternative and its engine/check-digit rules.
2. Choose the operator-facing copy (`Check digit`, `HRI check digit`, or equivalent) and the default HRI display behavior.
3. Choose reuse-vs-new Figma reference and record the node/state evidence in [`figma-ui-handoff-template.md`](figma-ui-handoff-template.md).
4. Assign the runtime screenshot/UI Automation owner before marking P3 ready for implementation.
