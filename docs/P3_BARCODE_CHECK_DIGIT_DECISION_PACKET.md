# P3 barcode check-digit / HRI owner decision packet

**Status:** documentation-only decision packet; owner sign-off required before implementation
**Date:** 2026-08-13
**Execution spine:** [`INDUSTRIAL_BARCODE_EXECUTION_PLAN.md`](INDUSTRIAL_BARCODE_EXECUTION_PLAN.md) §P3
**UI/UX handoff:** [`P3_BARCODE_CHECK_DIGIT_UI_HANDOFF.md`](P3_BARCODE_CHECK_DIGIT_UI_HANDOFF.md)
**UI/UX specification:** [`P3_BARCODE_CHECK_DIGIT_UI_SPEC.md`](P3_BARCODE_CHECK_DIGIT_UI_SPEC.md)
**Program index:** [`BARCODE_UI_UX_PROGRAM_INDEX.md`](BARCODE_UI_UX_PROGRAM_INDEX.md)

This packet turns the open P3 questions into a bounded owner decision. It records what the current source and read-only Figma metadata actually support, recommends the smallest first slice, and lists the evidence required before a code change can be called ready. It does not add a model field, change barcode rendering, edit Figma, or close P3.

## 1. Decision requested

Approve or amend these five decisions before implementation:

1. first optional-check symbology;
2. authored-payload and `None` / `Auto` / `Verify` semantics;
3. operator-facing HRI copy and legacy-safe default;
4. persistence/migration shape;
5. WPF/Figma/runtime evidence ownership.

The recommended bounded option is **Code 39 first**, with a typed per-object policy and a separate HRI display mode. The recommendation is not an approval; the owner must record the selected option in the sign-off table at the end of this packet.

## 2. Source and design evidence

| Evidence | What is true today | Consequence for P3 |
| --- | --- | --- |
| Symbology catalog | [`BarcodeSymbology`](../src/ANLAbel.Core/Enums/BarcodeSymbology.cs) contains `Code39` and `ITF`; the App groups both in the Standard ComboBox. | Either can be selected as the first optional-check slice, but the existing catalog is not evidence that check digits are implemented. |
| Renderer validation | [`ZxingBarcodeRenderer`](../src/ANLAbel.Barcode/Renderers/ZxingBarcodeRenderer.cs) validates Code 39 characters, enforces even numeric ITF input, normalizes Code 39/Codabar case and maps both to ZXing formats. | Current validation is character/length/engine validation. It does not expose a typed optional check-digit policy or a resolved payload seam. |
| Core application checks | [`BarcodeApplicationContract`](../src/ANLAbel.Core/Barcode/BarcodeApplicationContract.cs) has GS1/AI check-digit validation for its existing application-profile path. | Preserve GS1 behavior; do not reuse the GS1 validator as a general Code 39/ITF contract without standards-backed fixtures. |
| Object contract | [`LabelObject`](../src/ANLAbel.Core/Models/LabelObject.cs) persists `BarcodeSymbology`, application profile, HRI placement, legacy `ShowBarcodeText`, HRI size, X-dimension and width mode. | There is no check-digit policy or HRI check-digit display field. Do not overload `ShowBarcodeText`; it remains an HRI placement compatibility flag. |
| Properties card | [`MainWindow.xaml`](../src/ANLAbel.App/MainWindow.xaml) exposes Standard, QR mode, EC level, application profile, quiet zone, HRI placement, HRI size, X-dimension, width mode and validation/readouts. | P3 should be a compact extension of this card, with policy/status before geometry diagnostics. |
| Validation path | [`MainViewModel`](../src/ANLAbel.App/ViewModels/MainViewModel.cs) calls renderer validation, shared HRI layout and application-profile validation. | A future P3 implementation must resolve the encoded payload once and feed designer, preview, preflight and print from that result. |
| Figma panels | Read-only metadata for panels file `kqyNBI0DgRHnPzJTDBIui5` shows selected Properties `13:2` and tabbed Properties `18:69`, both `300 × 700`; no barcode/check-digit state is present. | Reuse may be approved for spacing and grouping only. A Figma node is not runtime proof and no write is needed for this decision packet. |

## 3. Decision matrix

### D1 — First optional-check symbology

| Option | Benefits | Risks / required evidence | Recommendation |
| --- | --- | --- | --- |
| **Code 39** | Already named in the P3 research gap; renderer and Properties catalog support it; a compact `None`/`Auto`/`Verify` demonstration is easy to isolate from GS1 and Code 128. | The implementation owner must attach the exact check-digit algorithm, input convention and valid/invalid fixtures before coding. | **Recommended first slice.** |
| ITF | Already renderer-supported and relevant to industrial labels. | Existing validation only checks numeric/even length; ITF check-digit conventions and bearer/ratio interactions must be resolved separately. | Defer until a separate fixture/standards decision. |
| Code 128 / GS1 | Existing production paths and checks. | Code 128's check digit is engine/standard-mandatory; GS1 has its own AI/FNC1 contract. An optional selector would be misleading. | Explicitly non-optional for P3. |

**Owner record:** `Selected symbology: ____________________`  **Owner/date:** ____________________

### D2 — Payload and policy semantics

| Policy | Authored input | Encoded payload | Verify/preflight rule | Recommended UI status |
| --- | --- | --- | --- | --- |
| `None` | Complete authored value | Authored value unchanged | No optional digit operation | `Not applicable` / neutral |
| `Auto` | Base value without the optional digit | Base value plus the standards-derived digit | Fail closed if the base value cannot be resolved or the selected engine cannot derive it | `Generated` plus resolved-value diagnostics |
| `Verify` | Complete value including the supplied digit | Authored value after successful verification | Block preview/print when supplied and expected digits differ; never fall back to `Auto` | `Valid` or `Invalid` with expected/supplied values |

The policy must be typed and per object. HRI placement (`None` / `Below` / `Above`) and the legacy `ShowBarcodeText` mapping remain independent. The exact Code 39 algorithm, whether an explicit start/stop character participates in the authored value, and the engine input convention are **standards/implementation decisions still requiring evidence**; do not encode them by inference in XAML.

**Owner record:** `Policy semantics approved / amended: ____________________`  **Owner/date:** ____________________

### D3 — HRI copy and default

Recommended copy is **Check digit** for the policy selector and **HRI check digit** for the display-only selector. Use `Show` as the legacy-safe default when a new field is absent, so existing HRI placement and visible content do not unexpectedly change. For `None`, the HRI selector is disabled with `Not applicable`; for `Auto` and `Verify`, `Show`/`Hide` changes only the displayed HRI text.

**Owner record:** `Copy/default approved or amended: ____________________`  **Owner/date:** ____________________

### D4 — Persistence and migration

The implementation proposal is two explicit per-object fields (names are illustrative until Core review):

- `BarcodeCheckDigitPolicy`: `None` / `Auto` / `Verify`;
- `BarcodeHriCheckDigitDisplay`: `Show` / `Hide`.

Missing fields in legacy JSON/document snapshots resolve to `None` + `Show`. Existing `BarcodeHriPlacement`, `ShowBarcodeText`, payload expressions, authored geometry, X-dimension and width mode must round-trip byte-for-byte in the existing compatibility scope. Clone, save/load, document snapshot and any future migration test must prove the new fields without rewriting old objects.

**Owner record:** `Core field names/migration approved or amended: ____________________`  **Owner/date:** ____________________

### D5 — UI, Figma and runtime ownership

| Concern | Bounded recommendation | Required owner evidence |
| --- | --- | --- |
| WPF surface | Extend the existing Barcode Properties card; order summary/content → policy → HRI display/placement → geometry/readout → validation. | Named App/MainWindow owner and target-scale screenshot/UI Automation owner. |
| Figma reference | Reuse `18:69` (tabbed selected-Properties shell) and `13:2` (compact selected-object card) as interim visual references only. | Explicit reuse approval, or a smallest state-specific node with dimensions for None/Auto/Verify. |
| Figma write | None for this packet. Do not create/duplicate a frame just to fill a documentation gap. | A later write requires an explicit UI/UX task and `figma-use` review; it is not implied by P3. |
| Runtime evidence | Capture `1024 × 600` at `100%`, `125%`, `150%` (or document the environment exception), including valid, invalid and non-optional states. | Named runtime/UIA owner, focus/keyboard/scroll result and artifact paths. |
| Physical claim | Software status remains separate from verifier grade and native-printer evidence. | No P3 approval may claim physical verification. |

**Owner record:** `WPF owner: __________  Figma route: reuse / new node  Runtime owner: __________  Date: __________`

## 4. Implementation-ready fixture matrix

The following fixtures are names and assertions, not a license to implement before D1–D5 are signed:

| Fixture | Policy/state | Required assertion |
| --- | --- | --- |
| `Code39_None_preserves_authored_payload` | `None` | Encoded value, vector/module fingerprint and HRI value match the existing path. |
| `Code39_Auto_resolves_digit` | `Auto` + valid base payload | Resolved digit is standards-backed and is used consistently by designer, preview, preflight and print. |
| `Code39_Verify_accepts_expected_digit` | `Verify` + valid complete payload | Status is valid and dispatch remains allowed after normal gates. |
| `Code39_Verify_rejects_wrong_digit` | `Verify` + invalid complete payload | Status names expected/supplied values and preview/print dispatch is blocked. |
| `Code39_HriHide_does_not_change_symbol` | `Auto` or `Verify` + HRI `Hide` | Only HRI text changes; encoded payload, module/vector fingerprint, dimensions and manifest identity do not. |
| `Code39_non_optional_controls_are_not_actionable` | Code 128, QR or Data Matrix | Optional-check controls are hidden/disabled with a reason; existing engine/application behavior is unchanged. |
| `BarcodeCheckDigit_clone_save_legacy_round_trip` | New and legacy objects | New fields round-trip; missing fields use `None` + `Show`; authored HRI/geometry remain unchanged. |

## 5. Ready / not-ready gates

P3 is **ready for implementation** only after all of the following are recorded:

- D1–D5 have an owner/date and any amendment is reflected in the handoff/spec;
- the selected symbology's algorithm, payload convention and standards-backed fixture source are attached;
- one encoded-payload resolver is named as the authority for designer, preview, preflight and print;
- the HRI masking contract explicitly excludes module/layout mutation;
- AutomationIds and keyboard order are accepted against the existing Properties card;
- runtime screenshot/UIA ownership and target-scale artifacts are named;
- the existing barcode, GS1, X-dimension, HRI and protected Text/TextBox gates remain in scope;
- no Figma or physical-printer claim is used as a substitute for runtime/software evidence.

P3 remains **not ready** when the owner has not chosen Code 39 versus ITF, when Verify can silently become Auto, when HRI masking changes encoded modules, when legacy fields are rewritten, or when a Figma shell is treated as a shipped state.

## 6. Sign-off

| Decision | Owner | Date | Approved / amended | Evidence link |
| --- | --- | --- | --- | --- |
| D1 first symbology |  |  |  |  |
| D2 payload/policy semantics |  |  |  |  |
| D3 HRI copy/default |  |  |  |  |
| D4 persistence/migration |  |  |  |  |
| D5 UI/Figma/runtime ownership |  |  |  |  |

Until this table is completed, this packet is a review aid only. The next safe action is to obtain the missing owner decisions and standards-backed fixtures; no Text/TextBox contract change is involved.
