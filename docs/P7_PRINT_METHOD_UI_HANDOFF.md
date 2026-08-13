# P7 Print Method UI/UX Handoff

**Status:** documentation-only review contract; implementation and printer pilot are still open
**Date:** 2026-08-13
**Owner:** barcode/printing product review
**Related phase:** P7 in [`INDUSTRIAL_BARCODE_EXECUTION_PLAN.md`](INDUSTRIAL_BARCODE_EXECUTION_PLAN.md)
**Owner decision packet:** [`P7_PRINT_METHOD_DECISION_PACKET.md`](P7_PRINT_METHOD_DECISION_PACKET.md)

## 1. Purpose and decision boundary

P7 makes the output path visible to an operator: ANLAbel's app-owned graphic barcode path remains the default, while a future printer-native command path may be selected only when a concrete adapter, printer-family capability record and pilot evidence exist. The surface must explain what was requested, what was resolved, and why a native path is unavailable. It must never silently change the queue or fall back from native to graphic.

This handoff is an ADR-first contract. It does not claim that ANLAbel emits ZPL, EPL, TSPL, printer fonts or native barcode commands today, and it does not turn a Figma shell into printer evidence.

## 2. Current source evidence

| Evidence | Current finding | Consequence for UI |
| --- | --- | --- |
| [`PrintService.cs`](../src/ANLAbel.Printing/PrinterProfiles/PrintService.cs) | Builds a WPF `PrintDocument` and routes labels through the app render path. | Graphic is the only currently evidenced resolved method. |
| [`LabelVisualRenderer.cs`](../src/ANLAbel.Printing/RenderPipeline/LabelVisualRenderer.cs) | Renders label visuals in the application pipeline. | Designer/preview parity starts from the same graphic path. |
| [`PrintRenderPlan.cs`](../src/ANLAbel.Printing/RenderPipeline/PrintRenderPlan.cs) | Carries DPI, media, printable-area, scene/text/image fingerprints and output-contract evidence. | A future method decision must be part of the output contract, not an untracked UI toggle. |
| [`PrintJobResult.cs`](../src/ANLAbel.Printing/PrinterProfiles/PrintJobResult.cs) | Records outcome, queue identity, fingerprints and support evidence, but no print-method field. | A manifest/job-log schema extension is required before a native pilot can close P7. |
| Barcode renderers under [`src/ANLAbel.Barcode`](../src/ANLAbel.Barcode/) | Produce app-owned raster/vector barcode output; no native command adapter is present. | Do not expose a working Native option as if it were implemented. |

## 3. Operator problem and intended outcome

An operator needs to answer these questions before dispatch:

1. Will the job use the app-rendered graphic barcode or a printer-native command?
2. Which printer, driver and capability evidence justify that choice?
3. If the requested native path cannot be used, will the job block or explicitly use graphic output?
4. Is the preview a faithful representation of the resolved path, or is semantic native parity still unproven?

The intended outcome is a deterministic, reviewable decision. `Graphic` is safe and visible. `Native` is an explicit, pilot-gated choice. An implicit `Auto` mode is not part of the first contract; if a later design introduces it, the resolution policy must be deterministic, visible, persisted and tested.

## 4. Proposed contract boundary

### Requested and resolved method

- `Graphic` is the persisted default and must preserve existing designer, preview and print golden behavior.
- `Native` is selectable only when a registered adapter declares the printer family, driver/firmware scope, supported symbology and pilot evidence.
- The UI displays both `Requested method` and `Resolved path`. They may differ only through an explicit operator-approved policy, with the reason persisted in the job evidence.
- A queue change is a separate decision. Resolving a method must never silently select another printer.

### Capability evidence

The capability record is keyed by the concrete queue identity plus printer family, driver version and firmware/feature scope. A generic claim such as “printer supports barcodes” is insufficient. The record must identify:

- adapter and version;
- printer family/model and driver identity;
- supported symbologies and command/parameter limits;
- evidence timestamp, pilot/fixture identifier and owner;
- output-contract assumptions (DPI, media, imageable area and orientation).

Unknown, stale or mismatched evidence is an unavailable state, not an optimistic success.

### Preview and parity

Designer and preview remain graphic-first. A future native adapter may show a semantic overlay or parity warning, but it may not claim pixel parity without evidence. The dispatch card must distinguish `Graphic preview` from `Native semantic parity verified` and `Native parity unknown`.

### Job and manifest evidence

The eventual manifest/job-log record should include at least:

| Field | Meaning |
| --- | --- |
| `requestedPrintMethod` | Operator/document request: `Graphic` or `Native`. |
| `resolvedPrintMethod` | Actual emitted path, or no path when blocked. |
| `nativeCommandsUsed` | Explicit boolean; never inferred from a queue name. |
| `adapterId` / `adapterVersion` | Native adapter identity when applicable. |
| `printerFamily` / `driverIdentity` | Capability scope used for resolution. |
| `capabilityEvidenceId` | Pilot/capability record reference and timestamp. |
| `fallbackPolicy` / `fallbackReason` | Block or explicit graphic fallback with operator-visible reason. |
| `outputContractHash` | DPI/media/printable-area and method-sensitive output contract. |

No field should imply physical completion, barcode grade or verifier evidence.

## 5. Host-neutral wireframe

The method belongs in the Print/Output setup surface, close to printer selection and output-contract evidence. It does not belong in the barcode Properties card as a symbology-specific toggle.

```text
Print & Output
  Printer              [Verified queue                          ] [Setup]
  Print method         [Graphic (app-rendered)                 v]
  Resolved path        Graphic — preview and dispatch use app render
  Capability evidence  Graphic baseline; native evidence not selected
  Fallback policy      [Block native request | Explicitly use Graphic]
  Output contract      300 dpi · 100 x 50 mm · imageable area · hash …
  Preview parity       Graphic preview; native semantic parity not claimed
  Dispatch readiness   [Ready]  Graphic path selected
```

When `Native` is requested, the same card must expose the adapter/capability record, pilot age and unsupported reason before enabling dispatch. If the operator chooses explicit graphic fallback, the resolved path and warning remain visible until the job is submitted.

## 6. State and action matrix

| State | Visible message | Allowed action | Dispatch |
| --- | --- | --- | --- |
| Graphic ready | `Graphic path selected` | Preview, print, inspect output contract | Enabled |
| Native capability unknown | `No verified adapter/capability evidence for this queue` | Keep Graphic, choose a verified queue, or cancel | Blocked for Native |
| Native symbology unsupported | `Adapter does not support this symbology/parameter set` | Keep Graphic or edit barcode; do not auto-change symbology | Blocked for Native |
| Native pilot missing/stale | `Pilot evidence is required or expired` | Open evidence/setup, keep Graphic, or cancel | Blocked for Native |
| Native parity unknown | `Native output may differ from graphic preview` | Review evidence; explicit owner approval if policy allows | Review/blocked until ADR says otherwise |
| Explicit Native -> Graphic fallback | `Native unavailable; operator selected Graphic` | Preview, print, or cancel | Enabled as Graphic; warning is persisted |
| Printer/driver mismatch | `Capability record does not match the selected queue` | Select verified queue, keep Graphic, or cancel | Blocked for Native |
| Output-contract drift | `DPI/media/imageable area changed after method resolution` | Re-resolve and review | Blocked until revalidated |

The primary safe actions are always `Keep Graphic`, `Select verified queue`, `Review evidence`, and `Cancel`. There is no hidden retry that changes the method.

## 7. Figma routing and evidence boundary

Read-only metadata was checked on 2026-08-13 in the existing shell recreation file [ANLAbel — NiceLabel Shell Recreation](https://www.figma.com/design/zdN71qfzrYV6pPt1b2FRRc/ANLAbel-%E2%80%94-NiceLabel-Shell-Recreation), node `2:2`:

- full shell: `1440 x 900`;
- Quick Access: `2:3`, `1440 x 52`;
- printer status: `2:19`, `Printer not selected`;
- paper status: `2:21`, `Paper: 100 x 50 mm`;
- Print & Output ribbon: `2:39`, `147 x 58`, with Setup `2:41`, Preview `2:44` and Print `2:47`;
- body geometry: left rail `268`, design surface `880`, Properties `292`;
- status bar: `Ready · industrial designer shell`, `Printer: —`, zoom.

The shell has the correct grouping language but no Print Method, capability evidence, resolved path or fallback state. Reuse it only for placement and density. Do not create or edit a Figma frame until the owner supplies a concrete native pilot and chooses the host surface; runtime screenshot/measurement remains the closure gate.

## 8. Proposed AutomationIds

| Control | AutomationId |
| --- | --- |
| Print/output region | `Print.Output.Region` |
| Printer selector | `Print.Output.Printer` |
| Printer setup action | `Print.Output.PrinterSetup` |
| Requested method selector | `Print.Output.Method` |
| Resolved path readout | `Print.Output.ResolvedPath` |
| Capability evidence summary | `Print.Output.CapabilityEvidence` |
| Native reason/details | `Print.Output.NativeReason` |
| Fallback policy selector | `Print.Output.FallbackPolicy` |
| Output contract summary | `Print.Output.OutputContract` |
| Preview parity state | `Print.Output.PreviewParity` |
| Dispatch readiness | `Print.Output.DispatchReadiness` |
| Evidence/details action | `Print.Output.EvidenceDetails` |

Warnings must be announced as status text and remain keyboard reachable; color alone cannot communicate a blocked native path.

## 9. Acceptance and stop gates

P7 remains open until all of the following are true:

1. An architecture ADR names the method enum, adapter boundary, capability-record schema and fallback policy.
2. At least one real printer family/driver pilot records evidence for one supported symbology and output contract.
3. Job/manifest evidence records requested method, resolved method, native-command usage and fallback reason.
4. `Graphic` preserves the existing render golden/regression path.
5. Native unsupported, stale, mismatch, parity-unknown and explicit-fallback states have regression coverage.
6. A target-scale runtime click-through records the selected queue, warning wrapping, keyboard order and AutomationIds.

Stop and leave the phase open when device access, driver documentation, pilot evidence or a clear owner decision is missing. Do not mark a native path complete from Figma metadata, a queue name or a code seam alone.

## 10. Non-goals and protected behavior

- No native command emitter, driver SDK, printer-family adapter or hardware pilot is introduced by this document.
- No `Auto` mode or silent queue/method fallback is introduced.
- No claim of physical print completion, verifier grade, certification or barcode readability is made.
- Barcode geometry, HRI and GS1 contracts remain separate from method selection.
- Text/TextBox ownership, sizing, wrapping, clipping, padding, resize lifecycle and Properties presets are untouched.

The companion control-level contract is [`P7_PRINT_METHOD_UI_SPEC.md`](P7_PRINT_METHOD_UI_SPEC.md).
