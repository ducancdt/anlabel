# ANLAbel — P3 barcode check-digit / HRI UI/UX specification

**Status:** documentation-only, pre-implementation contract proposal (2026-08-13)
**Execution spine:** [`INDUSTRIAL_BARCODE_EXECUTION_PLAN.md`](INDUSTRIAL_BARCODE_EXECUTION_PLAN.md) §P3
**Competitive gap:** [`BARCODE_NICELABEL_BARTENDER_RESEARCH.md`](BARCODE_NICELABEL_BARTENDER_RESEARCH.md) M7/M8
**Handoff:** [`P3_BARCODE_CHECK_DIGIT_UI_HANDOFF.md`](P3_BARCODE_CHECK_DIGIT_UI_HANDOFF.md)
**Owner decision packet:** [`P3_BARCODE_CHECK_DIGIT_DECISION_PACKET.md`](P3_BARCODE_CHECK_DIGIT_DECISION_PACKET.md)
**Figma boundary:** reuse selected-Properties language from `18:69` / `13:2` only after owner approval; no barcode-specific frame exists

This document turns the P3 handoff into a reviewable operator-facing UI contract. It does not add a check-digit model, change barcode rendering, edit Figma, or claim that P3 is implemented. The first proposed optional-check slice is Code 39; ITF and other symbologies require their own standards and regression coverage.

## 1. Operator outcome

An operator editing an optional-check Code 39 barcode should be able to answer, without reading an expression or changing HRI placement:

1. whether the object uses no optional check digit, derives one automatically, or verifies one supplied by the payload;
2. whether HRI displays the check-digit character while the encoded symbol remains unchanged;
3. why a Verify payload is blocked, which value was expected, and what safe repair is available;
4. whether the selected standard supports optional check digits at all.

The control is a compact extension of the existing barcode Properties card. It is not a new top-level tab and it does not reuse `ShowBarcodeText` as a check-digit policy.

## 2. Evidence and Figma routing (read-only)

The 2026-08-13 metadata scan of panels Page `0:1` found the existing selected-Properties references but no barcode-specific state frame:

| Reference | Existing evidence | Allowed use in P3 | Not evidence of |
| --- | --- | --- | --- |
| `18:69` | `300 × 700` tabbed Properties shell; Label/Layout/More tabs and `284 DIP` content cards | Spacing, grouping and selected-tab language after owner approval | Check-digit controls or valid/invalid variants |
| `13:2` | `300 × 700` selected-Properties shell; `276 DIP` content/behavior cards and collapsed utility rows | Dense status/control treatment after owner approval | Barcode payload or HRI semantics |
| `1:8` | Ribbon text `Text TextBox Image Barcode` | Navigation vocabulary only | Barcode Properties layout or policy |

**Routing decision:** use `18:69` and `13:2` as interim visual references, while the runtime WPF card owns the new controls until a state-specific Figma node is approved. Do not create or edit a Figma frame solely to make this documentation appear complete. A later implementation must close the choice with target-scale runtime screenshots or UI Automation evidence.

## 3. Proposed data contract

The names below are documentation-level proposals. The implementation owner must confirm the Core type and migration shape before coding.

| Concept | Proposed values/meaning | UI consequence |
| --- | --- | --- |
| Optional check-digit policy | `None`, `Auto`, `Verify` for the selected optional-check symbology | Show a single explicit selector; never infer it from HRI placement or `ShowBarcodeText`. |
| Authored payload | The value supplied by static text or binding expression | Keep the original expression visible; do not rewrite it when deriving or hiding HRI text. |
| Encoded payload | `None`: authored payload; `Auto`: base payload plus derived digit; `Verify`: authored payload after successful validation | Renderer, vector/module hash and print manifest use this value. |
| HRI payload | Encoded payload or the same value with only the optional check digit removed when display mode is `Hide` | HRI masking is display-only and does not affect modules, dimensions or preflight. |
| HRI check-digit display | `Show` / `Hide`, enabled only when the selected policy can produce an optional check digit | Keep independent from `BarcodeHriPlacement` (`None` / `Below` / `Above`). |
| Validation status | `NotApplicable`, `Valid`, `Generated`, `Invalid`, `MissingInput`, `Unsupported` | Status text and print gate must name the exact reason. |

### 3.1 Payload semantics for the first Code 39 slice

To keep the first implementation deterministic, the proposed contract is:

- `None`: the authored payload is complete; no optional digit is generated or verified.
- `Auto`: the authored payload is the base value; the engine derives one optional digit and exposes the resulting encoded value in diagnostics/HRI according to the display mode.
- `Verify`: the authored payload includes the supplied optional digit; the engine computes the expected digit and blocks when it differs.

If the selected barcode engine needs a different input convention, the owner must amend this contract and its fixtures before implementation. The UI must not silently switch between base and complete payload interpretations.

Legacy files with missing fields use `None` plus `Show` as a no-op-safe default. Existing HRI placement, payload expression, authored geometry and `ShowBarcodeText` meaning remain unchanged.

## 4. Host-neutral wireframe

The existing frequency-first barcode card remains the owner of layout order:

```text
[Barcode summary: standard | data source | current status]
[Content / application profile / payload expression]

[Check digit:  None | Auto | Verify]
[HRI check digit:  Show | Hide]   (enabled only when meaningful)
[HRI placement:  None | Below | Above]

[X-dimension / width mode / effective mm · mil · dots @ DPI]
[Validation or warning line with repair guidance]
```

For a non-optional standard such as Code 128, the check-digit group is collapsed or disabled with the explanation `This standard owns its check digit`. It must not expose a selector whose changes have no effect.

The HRI display control stays adjacent to check-digit policy but remains visibly separate from HRI placement. Changing placement reserves the existing shared strip; changing HRI display changes text content only.

## 5. State matrix and safe actions

| State | Visible controls/evidence | Safe action | Print rule |
| --- | --- | --- | --- |
| Optional standard + `None` | Policy `None`; HRI display shown as not applicable or disabled; payload status neutral | Edit payload or choose `Auto`/`Verify` explicitly | Continue through normal preflight if all other checks pass |
| `Auto` + base payload valid | Policy `Auto`; `Generated` status; optional HRI `Show`/`Hide` | Inspect generated value or change policy | Allow preview/print; manifest records encoded payload/fingerprint |
| `Verify` + correct supplied digit | Policy `Verify`; `Valid` status; HRI choice available | Edit payload or choose another explicit policy | Allow preview/print after normal preflight |
| `Verify` + wrong supplied digit | Policy `Verify`; `Invalid` status with expected-vs-supplied explanation | Repair payload or change policy deliberately | Block preview/print dispatch; no Auto fallback |
| `Auto`/`Verify` + empty payload | Policy visible; `MissingInput` status | Supply/repair the bound value | Block until a concrete payload exists |
| Non-optional standard | Check-digit group hidden/disabled with reason; engine status remains explicit | Change standard or edit content | Preserve engine-mandatory behavior |
| Binding unresolved/stale | Policy retained; source/binding error shown separately | Repair source/binding before judging digit validity | Block if the encoded payload is not deterministic |
| Legacy object with no P3 fields | Existing HRI/content/geometry preserved; effective policy `None` | Opt in explicitly | No migration-only visual change |

The UI must never show a green/ready state when Verify failed, and it must never report a physical verifier grade from a software-only check.

## 6. Proposed accessibility and automation contract

These IDs are proposals until a WPF host and runtime owner approve them:

| Region/control | Proposed `AutomationId` | Accessible name / announcement |
| --- | --- | --- |
| Barcode Properties root | `Barcode.Properties.Root` | `Barcode properties` |
| Policy selector | `Barcode.Properties.CheckDigitPolicy` | `Check digit policy` |
| HRI display selector | `Barcode.Properties.HriCheckDigitDisplay` | `HRI check digit display` |
| HRI display explanation | `Barcode.Properties.HriCheckDigitHelp` | `HRI check digit display help` |
| Computed/validation status | `Barcode.Properties.CheckDigitStatus` | `Check digit status` |
| Expected value readout | `Barcode.Properties.ExpectedCheckDigit` | `Expected check digit` |
| Repair/action hint | `Barcode.Properties.CheckDigitRepair` | `Check digit repair guidance` |

Keyboard order should follow summary → content → policy → HRI display → HRI placement → X-dimension/readout → validation. A disabled HRI control must still expose why it is disabled to screen readers.

## 7. Responsive behavior

| Target | Layout rule | Evidence required before implementation closure |
| --- | --- | --- |
| `1280 × 800` | Keep the compact Properties card; status text may use one additional line without changing the card owner | Screenshot or UI Automation for None, Auto and Verify-valid |
| `1024 × 600` | Stack policy and HRI controls; keep invalid reason and repair action visible without page-level horizontal scroll | Screenshot/UI Automation for Verify-invalid and non-optional standard |
| `100%`, `125%`, `150%` | Reflow within the existing Properties column; do not widen shell columns from Figma metadata alone | Record scale, DPI, window size and clipping/focus result |

No change to Text/TextBox sizing, wrapping, clipping, padding, selection handles or resize lifecycle is part of this slice.

## 8. Acceptance gates for a future implementation

The P3 phase can be called implemented only when all of the following are evidenced:

1. Code 39 (or an explicitly approved alternative) has unit coverage for `None`, `Auto` and `Verify`.
2. A bad Verify payload fails closed in preflight and cannot dispatch through preview or print.
3. HRI `Hide` changes only the displayed HRI string; encoded payload, vector/module pattern, dimensions, manifest and output hash remain unchanged.
4. Clone, save/load, legacy migration and document snapshot preserve the new policy/display fields without rewriting authored geometry.
5. Designer, preview and print use the same resolved payload/check-digit semantics.
6. Existing GS1/application-profile, HRI placement, X-dimension, `SizedFromX`, legacy `FrameOwned`, and protected Text/TextBox regression gates remain green.
7. Runtime screenshot/UI Automation evidence covers `1024 × 600`, `100%`, `125%` and `150%` (or a documented environment exception), including invalid and non-optional states.
8. Physical verifier/native-printer evidence remains explicitly open unless separately supplied; software PASS is not certification.

Suggested commands remain the repository barcode gates:

```powershell
dotnet build ANLAbel.slnx --no-restore --nologo -v quiet -p:UseSharedCompilation=false -nodeReuse:false
dotnet test src/ANLAbel.UnitTests/ANLAbel.UnitTests.csproj --no-build --nologo -v quiet
dotnet run --project src/ANLAbel.Tests/ANLAbel.Tests.csproj --no-build
```

## 9. Explicit non-goals

- full check-digit parity for every symbology;
- changing Code 128's engine-mandatory check digit;
- changing GS1 FNC1/AI parsing or claiming a complete AI wizard;
- UPC split digits, bearer bars, ratio/density controls, printer-native commands or verifier grade;
- a new top-level Properties tab or a new Figma file;
- any change to the protected Text/TextBox contract.

Until the owner approves the symbology, payload convention, Figma reuse decision and runtime evidence owner, this file remains a UI/UX specification and P3 remains open.
