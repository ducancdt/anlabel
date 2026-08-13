# P7 print-method owner decision packet

**Status:** documentation-only owner gate; no print-method enum, native adapter, manifest migration, WPF control or Figma write is authorized by this packet (2026-08-13)
**Execution spine:** [`INDUSTRIAL_BARCODE_EXECUTION_PLAN.md`](INDUSTRIAL_BARCODE_EXECUTION_PLAN.md) §P7
**Handoff:** [`P7_PRINT_METHOD_UI_HANDOFF.md`](P7_PRINT_METHOD_UI_HANDOFF.md)
**Specification:** [`P7_PRINT_METHOD_UI_SPEC.md`](P7_PRINT_METHOD_UI_SPEC.md)
**Program index:** [`BARCODE_UI_UX_PROGRAM_INDEX.md`](BARCODE_UI_UX_PROGRAM_INDEX.md)

## Purpose and decision boundary

P7 is the dispatch/output phase after the P3–P6 authoring diagnostics. It must make the effective output path understandable without turning a barcode Properties card into a printer command switch. The current, evidenced path is application-rendered graphic output. A printer-native path is only a future, pilot-gated option.

```text
requested method + selected queue
        -> capability record and pilot evidence
        -> effective output contract revalidation
        -> resolved Graphic / Native / Blocked path
        -> manifest + job evidence
        -> dispatch and separate spool/physical evidence
```

The packet does not claim ZPL, EPL, TSPL, printer-font or native barcode command support. It does not claim pixel parity, physical completion, barcode readability or verifier grade. The protected Text/TextBox contract remains outside this phase.

## Decision summary

| Decision | Evidence-backed recommendation | Owner choice required |
| --- | --- | --- |
| D1. Method model/default | Use explicit `Graphic` and `Native` values. Persist `Graphic` as the default and preserve the existing graphic golden path. Do not introduce `Auto` in the first contract. | Approve the method enum/vocabulary and whether Native is product-visible only after a pilot. |
| D2. Capability authority | Key native capability by canonical queue identity, printer family/model, driver/firmware scope, adapter version, supported symbology/parameters and output-contract assumptions. A queue name or generic “supports barcodes” claim is insufficient. | Approve the capability-record owner, required fields, evidence age/expiry and update process. |
| D3. Requested versus resolved | Show `Requested method`, `Resolved path`, and the reason for any difference. Method resolution must never silently change the queue. | Approve the resolution state machine and the owner of the final dispatch decision. |
| D4. Fallback policy | Support only explicit `Block native request` or operator-selected `Use Graphic with warning`. Never silently fall back from Native or retry against another queue. | Approve default policy, operator permission and persisted fallback wording. |
| D5. Output contract/parity | Reuse `EffectiveOutputContract` for DPI, media, printable area, orientation and ticket hashes. Graphic preview is the baseline; Native semantic/pixel parity is a separate evidence state. | Approve the method-sensitive contract fields and whether parity-unknown blocks dispatch. |
| D6. Durable evidence/migration | Current manifest v2 `PrintMode` is a workflow description (`Print Preview`, `Selected rows`, etc.), not a method. Add requested/resolved/native/fallback fields only through an approved schema/version migration; legacy graphic jobs remain readable. | Approve manifest/job-log version, legacy defaults and fail-closed behavior for incomplete Native evidence. |
| D7. Host/Figma state | Keep the method surface with Print & Output/printer setup, not barcode Properties. Reuse shell `2:2`/`2:39` only for grouping; request a state-specific frame after pilot evidence exists. | Approve WPF host, proposed AutomationIds and reuse-versus-new-state Figma choice. |
| D8. Runtime closure | Require ADR, real printer-family/driver pilot, graphic golden regression, method/fallback/mismatch fixtures, manifest/support-evidence assertions, UI Automation and target-scale screenshots. | Name product, printing, adapter, job-evidence and QA owners. |

## Source evidence and implications

| Evidence | What it proves | What it does not prove |
| --- | --- | --- |
| [`PrintService.cs`](../src/ANLAbel.Printing/PrinterProfiles/PrintService.cs#L23) and [`#L564`](../src/ANLAbel.Printing/PrinterProfiles/PrintService.cs#L564) | The shipped dispatch path resolves a WPF queue/ticket, renders through the app paginator and calls `PrintDocument`; queue selection and last-mile contract revalidation are explicit. | It does not provide a native command emitter, adapter registry or method selector. |
| [`PrintRenderPlan.cs`](../src/ANLAbel.Printing/RenderPipeline/PrintRenderPlan.cs#L7) | The plan carries DPI, media, printable-area, document/scene/resource fingerprints and an effective output contract. | It does not identify Graphic versus Native or prove physical output. |
| [`EffectiveOutputContract.cs`](../src/ANLAbel.Core/Printing/EffectiveOutputContract.cs#L15) | A stable fingerprint covers queue, ticket hashes, DPI, dimensions, imageable area, media, feed, offsets, scale and printable-area verification. | The fingerprint alone cannot establish a native capability or printer readability. |
| [`PrintJobResult.cs`](../src/ANLAbel.Printing/PrinterProfiles/PrintJobResult.cs#L19) | Job results carry outcome, queue, spool identity, output/document/scene hashes, manifest and redacted support evidence. | There is no requested/resolved method, adapter identity or fallback reason field today. |
| [`PrintJobManifest.cs`](../src/ANLAbel.Core/Printing/PrintJobManifest.cs#L12) | The v2 manifest is immutable/hash-checked and includes a `PrintMode` string. | `PrintMode` is a human workflow label, not a typed print method; current callers pass descriptions such as `Print Preview` or `Selected rows`. |
| [`MainViewModel.cs`](../src/ANLAbel.App/ViewModels/MainViewModel.cs#L4407) and [`PrintPreviewWindow.xaml.cs`](../src/ANLAbel.App/PrintPreviewWindow.xaml.cs#L412) | Current manifest creation binds the description to `PrintMode` while preserving document/row/output hashes. | It does not prove a method was requested or resolved. A native pilot cannot infer method semantics from this field. |
| [`PrintSupportEvidenceContract.cs`](../src/ANLAbel.Core/Printing/PrintSupportEvidenceContract.cs#L16) | Support export is redacted, fingerprinted and deliberately separate from physical verification. | Support evidence is not native capability proof or a physical label result. |
| [`PhysicalOutputVerification.cs`](../src/ANLAbel.Core/Printing/PhysicalOutputVerification.cs#L11) | Scanner/verifier evidence is a distinct manifest-bound contract; visual inspection cannot mark physical completion. | Queue acceptance, graphic/native selection or Figma metadata cannot substitute for a verifier. |
| [`P7_PRINT_METHOD_UI_HANDOFF.md`](P7_PRINT_METHOD_UI_HANDOFF.md) and [`P7_PRINT_METHOD_UI_SPEC.md`](P7_PRINT_METHOD_UI_SPEC.md) | Existing design docs already define requested/resolved copy, explicit fallback states, AutomationIds and stop gates. | They are review artifacts, not an ADR, implementation or printer pilot. |
| Read-only Figma shell [`zdN71qfzrYV6pPt1b2FRRc`](https://www.figma.com/design/zdN71qfzrYV6pPt1b2FRRc/ANLAbel-%E2%80%94-NiceLabel-Shell-Recreation), node `2:2` | Metadata currently shows `1440 × 900`, printer status `2:19`, `Print & Output` group `2:39` (`147 × 58`), Setup `2:41`, Preview `2:44`, Print `2:47`, and status bar `2:170`. | No method selector, capability record, resolved path, fallback warning or parity state exists in the inspected node. |

## Recommended ownership model

| Layer | Single authority | UI responsibility |
| --- | --- | --- |
| Method request | Future typed document/job output policy | Display and persist the operator/document request; do not derive it from queue name or barcode symbology. |
| Native capability | Future adapter/capability registry and pilot evidence | Show scope, age, adapter and unsupported reason; do not optimistically enable Native. |
| Output contract | `PrintService`, `PrintRenderPlan`, `EffectiveOutputContract` | Revalidate immediately before dispatch and expose the fingerprint/repair reason. |
| Resolution/fallback | Future method resolver owned by printing | Produce one resolved path; record explicit fallback or block; never switch queues silently. |
| Manifest/job lifecycle | `PrintJobManifest`, `PrintJobStateStore`, operation log | Carry method-sensitive evidence only after schema approval; preserve legacy hashes and redaction. |
| Physical evidence | `PhysicalOutputVerification` and verifier adapters | Keep spool/driver state, native semantics and physical scan/grade separate. |
| Figma | Read-only shell until a state owner/pilot exists | Borrow placement/density only; a Figma frame never closes dispatch or physical acceptance. |

## State matrix for owner approval

| State | Required visible facts | Dispatch rule | Safe action |
| --- | --- | --- | --- |
| Graphic ready | Requested `Graphic`, resolved `Graphic`, output-contract hash/ticket verified, graphic preview | Existing graphic preflight decides readiness | Preview, print, inspect contract or cancel. |
| Graphic output-contract drift | Queue/ticket/DPI/media/imageable area changed after preparation | Block until re-resolved; do not silently rebuild | Re-resolve and review. |
| Native requested, capability missing | Queue/family/driver scope and exact missing evidence are named | Block Native | Keep Graphic, choose verified queue, review evidence or cancel. |
| Native requested, symbology/parameter unsupported | Adapter scope and unsupported barcode parameter are visible | Block Native; do not mutate barcode | Edit barcode, keep Graphic or cancel. |
| Native requested, pilot stale/mismatched | Evidence ID, age and mismatch (queue/driver/firmware/contract) are visible | Block Native | Review/update capability or choose Graphic. |
| Native requested, valid pilot and parity verified | Adapter/version, evidence ID, contract hash and resolved Native path are visible | Final preflight must pass | Preview/dispatch under approved ADR. |
| Native requested, semantic parity unknown | Preview remains explicitly Graphic; Native difference risk is visible | ADR decides; default recommendation is block | Review evidence, keep Graphic or cancel. |
| Native unavailable, explicit Graphic fallback | Requested Native, resolved Graphic, operator action and reason are visible | Graphic preflight may enable dispatch; warning persists in evidence | Preview/print Graphic or cancel. |
| Queue changed after resolution | Previous capability no longer matches selected queue | Block until method is resolved again | Select verified queue or keep Graphic. |
| Spool accepted | Resolved method and spool identity/outcome are shown | Never call this physical completion | Monitor/reconcile queue; no automatic retry. |
| Physical verification pass/fail | Separate manifest-bound scanner/verifier result and device identity | Only verifier contract can mark physical completion | Open evidence; do not infer from method or spool status. |
| Cancelled/unknown/failed | Requested/resolved state and error are retained when available | No implicit retry or method change | Reconcile, repair or cancel. |

## Proposed method-sensitive evidence shape

This is a design proposal, not a schema change. The owner must decide whether to introduce a new manifest contract version or an extension record. A native-capable record should preserve the existing v2 fields and add explicit, normalized values such as:

| Field | Meaning and invariant |
| --- | --- |
| `requestedPrintMethod` | `Graphic` or `Native`; never inferred from `PrintMode`, queue name or renderer type. |
| `resolvedPrintMethod` | `Graphic`, `Native` or `Blocked`; empty/blocked is explicit when no dispatch occurred. |
| `nativeCommandsUsed` | Boolean emitted by the adapter/dispatch path; never assumed from a printer family. |
| `adapterId` / `adapterVersion` | Required only for Native; absent for Graphic. |
| `printerFamily` / `driverIdentity` / `firmwareScope` | Capability scope that was actually matched. |
| `capabilityEvidenceId` / `evidenceTimestamp` | Pilot/capability record and freshness evidence. |
| `fallbackPolicy` / `fallbackReason` / `operatorAction` | `Block` or explicit Graphic fallback with a visible reason. |
| `outputContractHash` | Existing effective contract fingerprint; Native resolution must bind to the same contract. |
| `previewParity` | `Graphic`, `NativeSemanticVerified` or `Unknown`; never a physical-grade value. |

Legacy v1/v2 manifests remain readable as historical records. A legacy record has no proof of Native and must be treated as historical/Graphic-unknown for display, not upgraded silently. A new Native dispatch must fail closed if method-sensitive fields cannot be persisted and fingerprinted.

## Fixture and regression packet

The following are proposed fixture names and expected outcomes, not tests added by this documentation-only change.

| Fixture | Expected result | Required evidence |
| --- | --- | --- |
| Existing Graphic dispatch with valid queue/ticket | Resolved `Graphic`; existing golden path unchanged | Graphic renderer/preflight/application regression remains green. |
| Requested Native with no adapter | Native unavailable; no dispatch | UI reason, no method mutation, no silent fallback. |
| Requested Native with unknown/stale capability | Blocked with queue/family/driver/evidence reason | Capability match and expiry are deterministic. |
| Requested Native with unsupported symbology/parameter | Blocked; authored barcode unchanged | No auto-change to symbology, X-dimension, HRI or GS1 data. |
| Capability record for another queue/driver | Mismatch; blocked | Canonical queue/driver identity is checked, not display name alone. |
| Output contract drift after preview | Revalidation blocks | DPI/media/imageable-area/ticket mismatch names the repair. |
| Explicit Native → Graphic fallback | Resolved Graphic, warning persisted, Graphic dispatch only | Requested/resolved/fallback fields and operator action round-trip. |
| Valid pilot + Native semantic parity | Native resolved only under approved ADR | Adapter, evidence ID, contract hash and native-command flag persist. |
| Legacy manifest v1/v2 read | Historical record remains fingerprint-valid | No silent method upgrade; controlled reprint rules stay intact. |
| Manifest cannot store method fields | Native dispatch blocked; Graphic remains available | No visual badge without durable evidence. |
| Support evidence export | Redacted queue/job/output fingerprints only | No raw payload, secret or physical-verifier claim leaks. |
| Spool accepted without device confirmation | Outcome remains spool/queue evidence | UI does not say printed, complete or readable. |
| Verifier pass/fail against manifest | Physical state comes only from separate verifier contract | Method and physical grade remain separate. |
| Method control placed in barcode Properties | Design review rejects the placement | Method belongs to Print & Output/job scope; barcode objects stay unchanged. |

## UI/Figma decision details

### Recommended initial layout

Use the existing shell recreation only as a density/placement reference and add the method state to the Print & Output/printer setup host after the owner chooses the runtime surface:

```text
Print & Output
  Printer              [Verified queue                         ] [Setup]
  Print method         [Graphic (app-rendered)                 v]
  Resolved path        Graphic — preview and dispatch use app render
  Capability evidence  Graphic baseline; native evidence not selected
  Fallback policy      [Block native request | Use Graphic warning]
  Output contract      300 dpi · 100 × 50 mm · imageable area · hash …
  Preview parity       Graphic preview; native semantic parity not claimed
  Dispatch readiness   [Ready] Graphic path selected
```

At `1024 × 600`, method, evidence and readiness must stack without truncating the reason. The selected queue must remain identifiable; do not replace a long queue name with a generic `Printer` without an accessible full name.

### Figma reuse decision

The read-only metadata check is sufficient for this packet: node `2:2` has the correct `Print & Output` grouping and dimensions, but no method/capability/fallback state. The recommendation is **reuse shell placement only, defer Figma write**. If a pilot is approved, request the smallest state-specific frame containing at least Graphic ready, Native unavailable, explicit fallback, output-contract drift and Native parity-unknown states. Do not create a new file merely to fill the current evidence gap.

## No-go list

- Do not add a native option that is enabled by queue name, driver brand, barcode type or a generic capability claim.
- Do not introduce silent `Auto`, queue switching, Native→Graphic fallback or retry behavior.
- Do not use `PrintMode` workflow labels as a substitute for requested/resolved method fields.
- Do not claim pixel parity, physical completion, barcode readability, verifier grade or certification from graphic/native selection, spool acceptance or Figma metadata.
- Do not mutate barcode symbology, X-dimension, HRI, GS1 payload, label geometry or Text/TextBox behavior to satisfy a print method.
- Do not persist a visual badge without method-sensitive manifest/job evidence and fingerprint coverage.
- Do not fetch or assume vendor capability data without a named adapter, scope, pilot and owner.
- Do not edit Figma until the host, pilot evidence and state-specific node decision are approved.

## Owner sign-off record

Record one owner, date and decision for each row. Blank rows keep P7 open.

| Decision | Owner | Date | Approved option / notes |
| --- | --- | --- | --- |
| D1. Method enum/default and no-Auto rule | `TBD` | `TBD` | `TBD` |
| D2. Capability record authority/scope/expiry | `TBD` | `TBD` | `TBD` |
| D3. Requested/resolved state machine | `TBD` | `TBD` | `TBD` |
| D4. Explicit fallback policy | `TBD` | `TBD` | `TBD` |
| D5. Output contract and parity severity | `TBD` | `TBD` | `TBD` |
| D6. Manifest/job schema and migration | `TBD` | `TBD` | `TBD` |
| D7. WPF host/AutomationIds/Figma state | `TBD` | `TBD` | `TBD` |
| D8. ADR, pilot and runtime/regression owners | `TBD` | `TBD` | `TBD` |

**Closure rule:** P7 may move from design/contract review to implementation only after D1–D8 are filled, the ADR and capability/pilot evidence owners are named, method-sensitive persistence is approved, and the graphic/native/fallback fixture packet is converted into named runtime and regression gates. Until then, P7 remains an open output-method plan.
