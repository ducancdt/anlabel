# P7 Print Method UI/UX Specification

**Status:** review-ready, documentation-only; no runtime implementation claim
**Scope:** one Print & Output decision surface for graphic versus pilot-approved printer-native output
**Date:** 2026-08-13
**Related handoff:** [`P7_PRINT_METHOD_UI_HANDOFF.md`](P7_PRINT_METHOD_UI_HANDOFF.md)

## 1. User tasks

| Task | Success condition | Failure must be visible |
| --- | --- | --- |
| Choose output method | Operator can select `Graphic` or an explicitly available `Native` option. | Native is hidden/disabled with a concrete evidence reason when unavailable. |
| Verify the queue | Queue identity, printer family and driver scope are visible. | A capability record for another queue cannot appear valid. |
| Understand resolution | Requested and resolved paths are shown together. | Explicit Graphic fallback never looks like Native success. |
| Review preview parity | Operator knows whether preview is graphic or native-semantic verified. | Unknown parity blocks or warns according to the ADR; it is not implied by a green status. |
| Dispatch safely | Job is enabled only for a resolved, contract-valid path. | Method/queue/DPI/media drift blocks dispatch and explains the repair. |

## 2. Control contract

| Region/control | Content and behavior | Initial state | AutomationId |
| --- | --- | --- | --- |
| Printer | Saved queue name plus verified/mismatch badge; `Setup` opens existing printer setup. | Existing selected queue or `Not selected`. | `Print.Output.Printer` / `Print.Output.PrinterSetup` |
| Print method | `Graphic (app-rendered)` is the default. `Native (pilot-approved)` appears only when capability evidence is valid for the queue and barcode parameters. | Graphic selected. | `Print.Output.Method` |
| Resolved path | Read-only sentence: `Graphic`, `Native via <adapter>`, or `Blocked`. | Graphic. | `Print.Output.ResolvedPath` |
| Capability evidence | Compact summary of family, driver, adapter, evidence age and scope. `Details` opens a read-only evidence view. | `Graphic baseline; native not selected`. | `Print.Output.CapabilityEvidence` / `Print.Output.EvidenceDetails` |
| Native reason | Plain-language reason for unavailable, stale, unsupported or mismatched native path. | Hidden for Graphic; present for Native review/block. | `Print.Output.NativeReason` |
| Fallback policy | Explicit choices: `Block native request` or `Use Graphic with warning`. No implicit fallback. | `Block native request`. | `Print.Output.FallbackPolicy` |
| Output contract | DPI, media, imageable area, orientation and fingerprint/hash summary. | Read-only from resolved print plan. | `Print.Output.OutputContract` |
| Preview parity | `Graphic preview`, `Native semantic parity verified`, or `Native parity unknown`. | `Graphic preview`. | `Print.Output.PreviewParity` |
| Dispatch readiness | Status plus reason; the Print action consumes this state rather than recomputing silently. | `Ready` for Graphic when existing preflight passes. | `Print.Output.DispatchReadiness` |

The method selector is not a barcode-object property. It applies to the job/output plan so that all barcode objects, images and text share one explicit output contract.

## 3. Resolution state machine

```text
Requested Graphic
  -> existing graphic preflight -> Ready / Blocked by normal print validation

Requested Native
  -> capability record matches queue + driver + family + symbology + contract?
       no -> Blocked, or explicit Graphic fallback (record reason)
       yes
         -> pilot and parity evidence valid?
              no -> Review/Blocked (no silent dispatch)
              yes -> Native resolved -> final preflight -> Ready / Blocked
```

The state machine is deterministic for the same document, queue, capability record and output contract. A changed DPI, media, printable area, driver or barcode parameter invalidates the native resolution and requires a new review.

## 4. Copy and visual hierarchy

Use the following copy family so operators can distinguish path, evidence and readiness:

- section heading: `Print & Output`;
- method label: `Print method`;
- graphic option: `Graphic (app-rendered)`;
- native option: `Native (pilot-approved)`;
- resolved graphic: `Graphic path selected`;
- resolved native: `Native via <adapter>; capability evidence <id>`;
- unavailable: `Native unavailable for this queue`;
- explicit fallback: `Native unavailable; operator selected Graphic`;
- parity unknown: `Preview is graphic; native semantic parity is not verified`;
- ready: `Ready — output contract verified`;
- blocked: `Blocked — review the reason before dispatch`.

Do not use `Printer supports barcode`, `Optimized`, `Certified`, `Verified output` or `Printed successfully` unless the corresponding evidence contract exists. A green/neutral readiness chip is about dispatch validation, not physical output.

## 5. State matrix and actions

| Requested | Evidence | Resolved | Readiness | Primary action |
| --- | --- | --- | --- | --- |
| Graphic | Not required | Graphic | Existing preflight result | `Preview` / `Print` |
| Native | Missing/unknown | Blocked | Blocked | `Keep Graphic`, `Select verified queue`, `Cancel` |
| Native | Symbology/parameter unsupported | Blocked | Blocked | `Edit barcode`, `Keep Graphic`, `Cancel` |
| Native | Stale or mismatched | Blocked | Blocked | `Review evidence`, `Select verified queue` |
| Native | Valid adapter but parity unproven | Native review | ADR-dependent | `Review evidence`, `Keep Graphic`, `Cancel` |
| Native | Valid pilot and contract | Native | Final preflight | `Preview`, `Print`, `Cancel` |
| Native | Explicit Graphic fallback | Graphic | Ready if graphic preflight passes | `Preview`, `Print`, `Cancel`; warning retained |

The Print button must not mutate the requested method, queue, DPI, media or fallback policy. It dispatches only the resolved plan shown in the card.

## 6. Responsive and accessibility requirements

The existing shell reference is `1440 x 900`; use it for the first layout pass, then verify:

| Target | Required behavior |
| --- | --- |
| `1440 x 900` | Keep Printer, Method, Resolved path and Readiness in the first visible Print & Output group; evidence and output contract may be compact summaries. |
| `1024 x 600` | Stack method/evidence/readiness; warning text wraps without truncation; Print remains associated with the resolved state. |
| 100% / 125% / 150% Windows scale | No clipped method labels, badges or reason text; keyboard focus remains visible. |
| Narrow/long queue names | Wrap or ellipsize with accessible full name; never replace queue identity with a generic `Printer`. |
| Native blocked | The reason is status text adjacent to the selector and reachable by screen reader; color/icon is supplementary. |

Keyboard order: Printer -> Setup -> Method -> Fallback policy -> Evidence details -> Preview parity/read-only fields -> Dispatch readiness -> Preview -> Print -> Cancel. The selected method and resolved path must be announced when they change.

## 7. Evidence and persistence requirements

The UI may display a capability record only when it can bind it to the selected queue and current output contract. At dispatch, persist the requested/resolved method, `nativeCommandsUsed`, adapter/family/driver evidence, fallback decision/reason and output-contract hash. The evidence panel should link to the job/manifest identifier without exposing secrets or driver internals that are not part of the contract.

If the app cannot persist a field, the control remains review-only and the native path stays open. A visual badge is not a substitute for a manifest field.

## 8. Figma connection boundary

Use the existing shell recreation node `2:2` only as a placement reference. Its `Print & Output` ribbon `2:39` has Setup `2:41`, Preview `2:44` and Print `2:47`; the printer status `2:19` currently says `Printer not selected`. There is no native/graphic state, capability card or fallback warning in the read-only node metadata.

Before any Figma write, record one of these owner decisions:

1. reuse the shell and add a state-specific frame for `Native unavailable`; or
2. create a minimal state frame after a pilot supplies real adapter/evidence labels.

Until then, this specification is the source of truth for copy, states and AutomationIds; it is not a Figma implementation request.

## 9. Acceptance gates

- [ ] ADR approved for requested/resolved method, adapter boundary and fallback policy.
- [ ] One real printer-family/driver pilot with evidence ID and supported symbology.
- [ ] Graphic golden path unchanged with `Graphic` selected.
- [ ] Native unavailable, unsupported, stale/mismatch, parity-unknown and explicit-fallback states covered by regression checks.
- [ ] Manifest/job log contains method and native-command evidence fields.
- [ ] Runtime click-through at shell and target scale verifies wrapping, focus, AutomationIds and no silent mutation.
- [ ] No Text/TextBox contract change is bundled with this slice.

## 10. Explicit non-goals

This document does not implement a native command emitter, claim ZPL/EPL/TSPL support, certify a printer, prove physical readability, add a verifier, or change barcode geometry. It also does not authorize a hidden `Auto` policy, queue switching or fallback without operator-visible evidence.
