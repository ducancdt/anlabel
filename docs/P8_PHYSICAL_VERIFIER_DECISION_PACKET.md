# P8 physical-verifier owner decision packet

**Status:** documentation-only hardware/adapter owner gate; no device SDK, vendor selection, verifier UI, signed badge or Figma write is authorized by this packet (2026-08-13)
**Execution spine:** [`INDUSTRIAL_BARCODE_EXECUTION_PLAN.md`](INDUSTRIAL_BARCODE_EXECUTION_PLAN.md) §P8
**Handoff:** [`P8_PHYSICAL_VERIFIER_UI_HANDOFF.md`](P8_PHYSICAL_VERIFIER_UI_HANDOFF.md)
**Specification:** [`P8_PHYSICAL_VERIFIER_UI_SPEC.md`](P8_PHYSICAL_VERIFIER_UI_SPEC.md)
**Program index:** [`BARCODE_UI_UX_PROGRAM_INDEX.md`](BARCODE_UI_UX_PROGRAM_INDEX.md)

## Purpose and decision boundary

P8 is the final evidence phase after P7 output-method review. It defines how a future scanner or barcode-verifier observation can become a physical-output claim without confusing that claim with software preflight, a thermal raster golden, Windows spooler status or operator visual inspection.

```text
durable job + valid print manifest
        -> verifier request / expected content digest
        -> one bounded adapter observation
        -> correlation, identity, content, thermal and grade validation
        -> hash-only evidence
        -> eligible Completed transition (only on accepted evidence)
```

The packet does not claim ISO/ANSI certification, a vendor device, signed evidence, physical readability or any current physically verified job. It also does not alter barcode authoring, P7 method selection, queue dispatch or the protected Text/TextBox contract.

## Decision summary

| Decision | Evidence-backed recommendation | Owner choice required |
| --- | --- | --- |
| D1. Host and claim model | Keep verification as a selected-job/output detail concern. Use the existing Print Center as the first host candidate; let a future History detail surface consume the same evidence read model. Queue state remains `unverified` until accepted evidence. | Choose the host and the exact owner of the lifecycle transition. |
| D2. Methods and grade scales | Permit only `Scanner` or `BarcodeVerifier` for a physical claim. Keep `OperatorVisualInspection` audit-only. Display `ANSI`, `ISO15415` and `ISO15416` with their own minimum/observed values; never translate or compare scales. | Approve the initial method/scale set and any vendor-specific adapter policy. |
| D3. Request/manifest binding | Require a valid manifest, job identity, expected content fingerprint and, for barcode verification, a valid immutable expectation matching the digest/symbology/profile. | Approve request construction, rebind rules and whether an output-contract/thermal-golden context is mandatory per fixture. |
| D4. Adapter boundary | Keep vendor SDKs behind the hardware-neutral adapter. Require adapter ID/version, device ID, correlation token, normalized observed digest, grade and UTC timestamp. | Approve adapter registry, device identity, correlation format and supported fixture procedure. |
| D5. Timeout/busy/error policy | Preserve bounded timeout (maximum five minutes), single-flight busy guard, cancellation and explicit diagnostic codes. A late/non-cooperative SDK remains busy until its task finishes. | Approve default timeout, operator retry wording and device recovery owner. |
| D6. Completion/grade policy | Only accepted manifest-bound scanner/verifier evidence can set `Completed`; invalid/missing/below-threshold/mismatched evidence stays unverified or failed. Thermal golden proves context, not grade. | Approve minimum grade per scale, lifecycle policy and whether signed device evidence is required. |
| D7. Redaction and retention | Persist hashes/identities/metadata only; keep raw payloads, images and secrets out of durable lifecycle/support exports. Separate evidence fingerprint from any future signature status. | Approve redaction, export, retention and signature verification policy. |
| D8. Runtime/Figma/hardware closure | Require one real printer/media/verifier fixture, adapter evidence, state click-through, target-scale accessibility and named regression gates before P8 closure. Reuse Figma only for row/detail density. | Name lab, product, Core, adapter, WPF/UI Automation and QA owners. |

## Source evidence and implications

| Evidence | What it proves | What it does not prove |
| --- | --- | --- |
| [`PhysicalOutputVerification.cs`](../src/ANLAbel.Core/Printing/PhysicalOutputVerification.cs#L11) | Methods are `Scanner`, `BarcodeVerifier` and `OperatorVisualInspection`; evidence carries manifest/job identity, expected/observed fingerprints, device, grade and UTC timestamp; visual inspection is ineligible for completion. | It does not identify a vendor, signed device evidence or a completed hardware pilot. |
| [`PhysicalVerifierAdapterContract.cs`](../src/ANLAbel.Core/Printing/PhysicalVerifierAdapterContract.cs#L5) | SDK output is normalized to adapter/version, correlation, device, digest and grade; method/correlation/identity/content checks fail closed. | A neutral adapter interface is not a working device integration. |
| [`PhysicalVerifierAdapterContract.cs`](../src/ANLAbel.Core/Printing/PhysicalVerifierAdapterContract.cs#L40) | Timeout is finite and capped at five minutes; a non-cooperative in-flight observation remains guarded so overlapping reads are rejected as busy. | The default timeout or busy message is not a lab procedure or SLA. |
| [`PhysicalOutputVerifier.cs`](../src/ANLAbel.Core/Printing/PhysicalOutputVerifier.cs#L66) | The coordinator validates request/thermal context, maps adapter errors, validates evidence against the manifest and invokes barcode grade/content validation. | Coordinator tests do not prove a real verifier decodes a printed label. |
| [`BarcodeVerificationContract.cs`](../src/ANLAbel.Core/Barcode/BarcodeVerificationContract.cs#L7) | ANSI, ISO15415 and ISO15416 are explicit scales; expected content is a SHA-256-style digest and minimum grade is normalized per scale. | A software expectation is not an observed grade. |
| [`PrintJobState.cs`](../src/ANLAbel.Core/Printing/PrintJobState.cs#L74) and [`#L188`](../src/ANLAbel.Core/Printing/PrintJobState.cs#L188) | Lifecycle transitions separate `SpoolAccepted`/`QueueObserved` from `Completed`; `Completed` requires explicit physical verification and eligible evidence bound to the manifest. | A state-machine guard does not supply hardware evidence or a device identity. |
| [`PrintCenterWindow.xaml`](../src/ANLAbel.App/PrintCenterWindow.xaml#L20) | Current WPF Print Center lists durable recovery candidates, queue/spool/manifest facts and redacted support/recovery actions. | It has no verifier method, device, grade, correlation or evidence-detail controls today. |
| [`PrintSupportEvidenceContract.cs`](../src/ANLAbel.Core/Printing/PrintSupportEvidenceContract.cs#L16) | Support evidence is deterministic/redacted and carries an explicit `physicalOutputVerified` flag. | Exported support evidence is not a grade and does not authorize completion. |
| Existing unit fixtures [`PhysicalOutputVerificationTests.cs`](../src/ANLAbel.UnitTests/PhysicalOutputVerificationTests.cs), [`PhysicalOutputVerifierTests.cs`](../src/ANLAbel.UnitTests/PhysicalOutputVerifierTests.cs), [`PhysicalVerifierAdapterContractTests.cs`](../src/ANLAbel.UnitTests/PhysicalVerifierAdapterContractTests.cs) and [`BarcodeVerificationContractTests.cs`](../src/ANLAbel.UnitTests/BarcodeVerificationContractTests.cs) | Software coverage already exercises manifest mismatch, content/grade failures, adapter error/no evidence, timeout, busy and scale rules. | It does not close lab fixture, device, runtime UI or signed-evidence decisions. |
| Read-only Control Center History [`asnGsLMxceJWb3HlfaE3q4`](https://www.figma.com/design/asnGsLMxceJWb3HlfaE3q4), node `3:85` | Metadata shows `1280 × 800`, filter frame `3:99` (`1248 × 56`), activity frame `3:101` (`1248 × 600`), generic status rows and a details/reprint/errors note. | No ANLAbel manifest, verifier device, grade, correlation, rejected observation or physical-verification state exists in the inspected node. |
| Read-only shell [`zdN71qfzrYV6pPt1b2FRRc`](https://www.figma.com/design/zdN71qfzrYV6pPt1b2FRRc/ANLAbel-%E2%80%94-NiceLabel-Shell-Recreation), node `2:2` | Full shell, Print & Output and status density are available as placement references. | It is not a verifier surface and cannot prove physical output. |

## Recommended ownership model

| Layer | Single authority | UI responsibility |
| --- | --- | --- |
| Job/manifest identity | `PrintJobManifest`, `PrintJobStateStore` and lifecycle state machine | Show validity/fingerprint and block observation when identity is invalid or changed. |
| Expected content/grade policy | `BarcodeVerificationContract` and `PhysicalOutputVerificationRequest` | Show symbology/profile, scale, minimum grade and expected digest without raw payload. |
| Adapter observation | `IPhysicalVerifierPayloadAdapter` / `PhysicalVerifierAdapter` | Show waiting, verifying, busy, timeout, identity and correlation states; never persist SDK payloads. |
| Evidence validation | `PhysicalOutputVerifierCoordinator` plus `PhysicalOutputVerificationEvidence.Validate` | Show exact diagnostic code and eligibility; do not synthesize a green grade. |
| Lifecycle completion | `PrintJobStateMachine` and durable state store | Allow `Completed` only with accepted evidence; keep queue/spool states distinct. |
| Support/export | `PrintSupportEvidenceContract` | Export redacted metadata/fingerprints and explicit physical flag; never raw images/payloads. |
| Figma | Read-only shell/History metadata | Borrow row/detail density only; state-specific design remains owner/hardware-gated. |

## State matrix for owner approval

| State | Required visible facts | Lifecycle claim | Safe action |
| --- | --- | --- | --- |
| No verification requested | Job/manifest and queue facts; `Not verified` | Unverified | Review manifest or request an approved observation. |
| Queue completed — unverified | Queue/spool state plus explicit `not physical verification` copy | Unverified | Start verification, reconcile or export support evidence. |
| Manifest invalid/changed | Fingerprint mismatch/invalid reason | Blocked | Re-prepare the exact job; do not read a device. |
| Request invalid/unsupported | Missing job/digest, unsupported method or missing barcode expectation | Blocked | Repair request/profile; no adapter call. |
| Adapter unavailable | Device/adapter identity unavailable | Blocked | Connect/setup device or leave unverified. |
| Ready | Valid manifest/request, grade policy and thermal context | Pending | Start one observation. |
| Waiting/verifying | Correlation token, device and bounded timeout visible | Pending | Wait or cancel; no print/reprint action. |
| Adapter busy | Existing observation still in flight | Unverified | Wait until idle; do not overlap reads. |
| Timed out/cancelled | Explicit timeout/cancel diagnostic and correlation | Unverified | Inspect device or retry explicitly with a new correlation. |
| Identity/correlation mismatch | Adapter/device/request mismatch reason | Blocked | Discard observation and rebind. |
| Content mismatch | Expected vs observed digest mismatch | Failed/unverified | Hold/reject label and investigate print/data path. |
| Grade invalid/below threshold | Scale, minimum and observed grade; exact failure | Failed/unverified | Hold/reject label and review media/printer. |
| Visual audit only | `Operator visual inspection (audit only)` | Unverified | Retain audit or run an approved verifier. |
| Accepted | Manifest-bound evidence, device, timestamp, digest match and grade pass | Eligible for `Completed` | Append evidence-bound completion transition. |
| Signed evidence pending/invalid | Signature state separate from evidence validity | Policy-dependent, never implied | Follow ADR; do not show signed badge. |

No state silently retries, reprints, changes queue/method, downgrades grade policy or converts a software/visual signal into physical verification.

## Proposed evidence/display shape

This is a host-neutral proposal, not a WPF change:

```text
Physical verification
  Job / manifest       JOB-… · A1B2… [Valid]
  Queue evidence       Zebra … · spool #… · Queue completed — not physical verification
  Method               [Barcode verifier                         v]
  Grade policy         ANSI · minimum A
  Device / adapter     [Not connected] · Evidence: Not available
  Correlation          —
  Content identity     Expected 9F… · Observed — · hash-only
  Thermal context      Not bound / not required
  Readiness            [Blocked] Connect an approved adapter before starting
  [Start verification] [Cancel] [Export redacted evidence] [Details]
```

On accepted evidence, only the evidence and readiness fields change to show device/adapter identity, matching digest, scale/minimum/observed grade, UTC timestamp and evidence fingerprint. The lifecycle action remains separate: `Physically verified` is not the same text as `Completed` until the durable transition succeeds.

## Fixture and regression packet

The following are proposed fixture names and expected outcomes, not tests added by this documentation-only change.

| Fixture | Expected result | Required evidence |
| --- | --- | --- |
| Queue/spool accepted with no verifier | `Queue completed — physical output is not verified` | No physical flag, grade or completion transition. |
| Valid scanner request + accepted observation | Hash-only evidence accepted; lifecycle may complete under policy | Manifest/job identity, device, correlation and digest match. |
| Barcode verifier with ANSI minimum `A` | ANSI grade parsed and threshold enforced | `A` passes; `B`/lower or invalid scale fails without translation. |
| ISO15415/ISO15416 request | Numeric `0`–`4` grade stays on its named scale | No cross-scale comparison or UI normalization to ANSI. |
| Manifest mismatch/tamper | Request or evidence blocked | Adapter is not called when request is invalid; no completion. |
| Expected/observed content mismatch | `content-mismatch`/`payload-mismatch` | Raw payload remains absent from evidence and support JSON. |
| Invalid/below-threshold grade | `grade-invalid` or `grade-below-threshold` | Hold/reject guidance; no green state. |
| Correlation/method/device identity mismatch | Mapping/coordinator rejects observation | New explicit correlation required; old observation discarded. |
| Adapter timeout | `adapter-timeout`; output remains unverified | Bounded timeout, late task remains guarded and no overlap. |
| Adapter busy | `adapter-busy` | Second observation is rejected while first remains in flight. |
| Cancelled observation/no evidence/adapter exception | Unverified with exact code | No automatic retry/reprint; support evidence can be exported. |
| Thermal golden unbound/mismatch | Request blocked before device read | Golden context remains separate from grade. |
| Visual inspection | Audit evidence only | Cannot set `PhysicalOutputVerified` or `Completed`. |
| Signed evidence requirement | Policy/ADR state explicit | No signed badge until schema and verification policy exist. |
| Redacted support export | Deterministic hashes/metadata only | Raw label values, images, secrets and vendor payloads absent. |
| Figma sample row | Design-only placeholder | Never treated as a live job, manifest or verifier result. |

## UI/Figma decision details

### Recommended initial host

Use the selected-job details region of the existing `PrintCenterWindow` as the first host candidate, reusing the current recovery/evidence actions. A future Control Center History detail can consume the same read model only after a separate host/ownership decision. Do not add verifier controls to barcode Properties or Print & Output method selection.

### Figma reuse decision

The read-only History metadata (`3:85`, `1280 × 800`) is sufficient for row/detail hierarchy and filter density, but it has generic sample activities and no physical-verifier states. The recommendation is **reuse row/detail density only, defer Figma write**. If a lab fixture is approved, request the smallest state-specific frame containing queue-unverified, ready, verifying/busy/timeout, rejected content/grade, accepted evidence and redacted details. Do not create a new file merely to imply hardware readiness.

## No-go list

- Do not display `Physically verified`, `Completed`, `Production certified`, ISO/ANSI grade or `Verified output` from queue/spool status, preflight, raster golden or visual inspection.
- Do not accept a verifier observation without valid manifest, job identity, correlation, device identity, expected/observed digest and method/grade checks.
- Do not compare ANSI, ISO15415 and ISO15416 grades as one numeric or visual scale.
- Do not persist raw barcode payloads, captured images, SDK blobs, secrets or vendor credentials in lifecycle/support evidence.
- Do not overlap adapter observations, ignore timeout/busy state, retry silently or dispatch/reprint as part of verification.
- Do not let verifier UI mutate barcode data, geometry, Text/TextBox behavior, queue, P7 method or grade policy.
- Do not add a signed badge before the signature schema, verification policy and real device evidence are approved.
- Do not edit Figma or treat sample History rows as live ANLAbel evidence.

## Owner sign-off record

Record one owner, date and decision for each row. Blank rows keep P8 open.

| Decision | Owner | Date | Approved option / notes |
| --- | --- | --- | --- |
| D1. Host and lifecycle claim model | `TBD` | `TBD` | `TBD` |
| D2. Methods, grade scales and thresholds | `TBD` | `TBD` | `TBD` |
| D3. Request/manifest/content binding | `TBD` | `TBD` | `TBD` |
| D4. Adapter/device/correlation boundary | `TBD` | `TBD` | `TBD` |
| D5. Timeout/busy/cancel/error policy | `TBD` | `TBD` | `TBD` |
| D6. Completion, thermal and signed-evidence policy | `TBD` | `TBD` | `TBD` |
| D7. Redaction/export/retention | `TBD` | `TBD` | `TBD` |
| D8. Lab fixture, runtime, Figma and regression owners | `TBD` | `TBD` | `TBD` |

**Closure rule:** P8 may move from hardware-gated design review to implementation only after D1–D8 are filled, a real fixture/adapter owner and correlation procedure exist, signed-evidence policy is resolved, and the fixture packet is converted into named Core, adapter, lifecycle, WPF/UI Automation and lab gates. Until then, P8 remains an open non-certification plan.
