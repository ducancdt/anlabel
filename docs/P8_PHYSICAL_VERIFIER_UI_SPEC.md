# P8 Physical Verifier / Grade UI/UX Specification

**Status:** review-ready, documentation-only; hardware and adapter remain open
**Scope:** one job-level evidence surface for physical scanner/verifier results
**Date:** 2026-08-13
**Related handoff:** [`P8_PHYSICAL_VERIFIER_UI_HANDOFF.md`](P8_PHYSICAL_VERIFIER_UI_HANDOFF.md)

## 1. User tasks

| Task | Success condition | Failure must be visible |
| --- | --- | --- |
| Separate queue from physical evidence | Operator sees queue/spool status and physical-verifier status as different facts. | Copy never calls queue completion a verified label. |
| Start a safe observation | A valid manifest/request starts one bounded adapter observation. | Invalid manifest, missing device, thermal mismatch or busy adapter blocks the start. |
| Review grade | Scale, minimum and observed grade are side-by-side. | Invalid, missing or below-threshold grade blocks completion. |
| Review content identity | Expected/observed fingerprints and match state are visible without raw payload. | Mismatch names the failure and prevents completion. |
| Complete the lifecycle | Only accepted evidence bound to the manifest enables physical completion. | Visual inspection, preflight, golden or spool evidence cannot enable it. |
| Export support evidence | Operator can export redacted, deterministic evidence. | Raw label values, images and secrets never appear in the export. |

## 2. Proposed host contract

The first implementation candidate is a selected-job verification detail region in `PrintCenterWindow`; a History detail drawer may later consume the same read model. This specification does not authorize either host or add controls to the barcode Properties panel.

| Region/control | Content and behavior | Initial state | AutomationId |
| --- | --- | --- | --- |
| Job/manifest | Job ID, manifest fingerprint, validity and output-contract hash. | Selected job or `No job selected`. | `Print.Verification.Manifest` |
| Queue evidence | Printer, spool ID, queue state, timestamp and explicit `not physical verification` copy. | Existing durable evidence. | `Print.Verification.QueueEvidence` |
| Method | `Scanner` or `Barcode verifier`; visual inspection is audit-only and not a completion option. | Not requested. | `Print.Verification.Method` |
| Grade policy | Scale and minimum grade from the immutable expectation. | `Not configured` until a barcode-verifier request exists. | `Print.Verification.GradePolicy` |
| Device/adapter | Device, adapter/version and connection/availability state. | `Not available`. | `Print.Verification.Device` |
| Correlation | Current observation token and match state. | `—`. | `Print.Verification.Correlation` |
| Content identity | Expected/observed SHA-256 fingerprints, match status and hash-only notice. | Expected may be present; observed `—`. | `Print.Verification.ExpectedDigest` / `Print.Verification.ObservedDigest` |
| Thermal context | Golden bound, unbound or mismatch state; separate from grade. | `Not required` when no golden is in manifest. | `Print.Verification.ThermalContext` |
| Status | Human copy plus diagnostic code; status is never inferred from color alone. | `Not verified`. | `Print.Verification.Status` |
| Completion eligibility | `Eligible`, `Blocked` or `Unknown`; derives from the same validation used by lifecycle state. | `Blocked — no accepted evidence`. | `Print.Verification.CompletionEligibility` |
| Start | Starts one explicit adapter observation, never a print. | Disabled without valid request/device. | `Print.Verification.Start` |
| Cancel | Cancels the observation only; it does not cancel the print job. | Disabled unless waiting/verifying. | `Print.Verification.Cancel` |
| Details | Read-only evidence metadata and validation reason. | Enabled when an event exists. | `Print.Verification.EvidenceDetails` |
| Export | Calls the redacted support-evidence contract. | Enabled for durable job evidence. | `Print.Verification.ExportSupport` |

## 3. Grade and evidence rules

- ANSI uses letter grades `A`, `B`, `C`, `D`, `F`; ISO15415 and ISO15416 use numeric grades `0`–`4`.
- The UI must show the grade scale with both minimum and observed values. It must not translate `ANSI:A` into an ISO number or compare scales by visual order.
- A grade without a valid expected/observed content fingerprint is rejected.
- A matching fingerprint without an accepted grade remains unverified.
- A thermal golden fingerprint proves a reviewed raster/driver/media context only; it is not a device observation.
- A queue state or print-result outcome by itself is not enough to display `Physically verified` unless the evidence-bound lifecycle transition has been accepted; operator visual inspection is always audit-only.
- Display digests as hash-only, with a copy action or full value only in an evidence-details view governed by the redaction policy. Never display raw production payloads or captured images in the durable job view.

## 4. State machine

```text
Durable job selected
  -> valid manifest + request + device?
       no -> Blocked (reason)
       yes -> Ready
  -> Start observation
       -> Waiting / Verifying
       -> Busy | Timed out | Cancelled | Adapter error | No evidence
       -> Evidence validation
            -> identity/content/thermal/grade failure -> Rejected
            -> accepted -> Physically verified / completion eligible
```

The state machine is job-local and evidence-bound. It has no edge to print dispatch, automatic reprint, queue selection or P7 method resolution.

## 5. State copy matrix

| Diagnostic code/state | Primary copy | Secondary guidance |
| --- | --- | --- |
| `not-requested` | `No physical verification requested` | Review manifest or connect an approved verifier. |
| `queue-completed-unverified` | `Queue completed — physical output is not verified` | Start an observation; do not mark the label complete. |
| `request-invalid` | `Verification request is invalid` | Check manifest, job identity, expected digest and method. |
| `adapter-busy` | `Verifier is busy with a previous observation` | Wait until idle; do not overlap reads. |
| `adapter-timeout` | `Verifier timed out` | Inspect device and retry explicitly with a new correlation. |
| `no-evidence` / `adapter-error` | `No accepted verifier evidence returned` | Export support evidence; output remains unverified. |
| `manifest-mismatch` / `identity-mismatch` | `Evidence does not belong to this job` | Discard observation and rebind to the reviewed manifest. |
| `payload-mismatch` / `content-mismatch` | `Observed content does not match` | Hold/reject and investigate data or print path. |
| `grade-invalid` | `Grade is invalid for the selected scale` | Review adapter mapping and grade policy. |
| `grade-below-threshold` | `Grade is below the required minimum` | Hold/reject; review media, darkness and speed. |
| `visual-only` | `Visual inspection is audit-only` | Run an approved scanner/verifier for completion evidence. |
| `accepted` | `Physical output verified against manifest` | Show device, grade, timestamp and evidence fingerprint. |

## 6. Responsive and accessibility requirements

The shell reference is `1440 x 900`; the verification detail must also be checked at `1180 x 720` (current Print Center), `1024 x 600`, and 100%/125%/150% Windows scale.

- At `1440 x 900`, keep Job, Queue evidence, Status and Completion eligibility visible without opening Details.
- At `1180 x 720`, stack Device, Correlation, Content identity and Grade policy; keep the Start/Cancel action group visible.
- At `1024 x 600`, allow long diagnostics to wrap; never truncate the reason to a color-only badge.
- Keyboard order: job selection -> method -> grade policy (read-only) -> device/details -> Start -> Cancel -> evidence details -> export.
- Screen readers announce `Queue completed — not physical verification`, `Observed grade`, `Minimum grade`, `Content match`, and `Completion eligibility` as separate status values.
- Focus remains visible while `Verifying`; Cancel is reachable without changing the selected job.

## 7. Persistence and privacy

At acceptance, persist the evidence fields already defined by Core: job ID, manifest fingerprint, method, outcome, expected/observed content fingerprints, device identity, grade, timestamp and evidence fingerprint. If a future signature is added, persist signature status and verification key identity separately; never replace the existing fingerprint with an opaque green badge.

Support export uses [`PrintSupportEvidenceContract`](../src/ANLAbel.Core/Printing/PrintSupportEvidenceContract.cs) and must remain redacted. The UI should expose the export fingerprint and file path, not raw payloads, scanner images or credentials.

## 8. Figma connection boundary

Use shell node `2:2` for Print & Output/status placement and History node `3:85` for row/detail hierarchy only. Neither has P8-specific states. A future Figma write requires a named first state (`Adapter unavailable`, `Grade below threshold` or `Accepted`) plus real adapter labels and target-scale runtime evidence. Until that decision, metadata and this spec are the only design evidence.

## 9. Acceptance gates

- [ ] One real verifier/fixture and media correlation procedure are named.
- [ ] Adapter timeout, busy, cancellation, no-evidence and mapping-failure states are covered.
- [ ] At least one real barcode observation validates content fingerprint and grade threshold.
- [ ] Manifest/output-contract and thermal-golden mismatch states fail closed.
- [ ] `Completed` remains impossible without accepted scanner/verifier evidence.
- [ ] Redacted support export contains no raw payload or image.
- [ ] Runtime click-through verifies state copy, keyboard order, AutomationIds and responsive wrapping.
- [ ] Any signed-evidence requirement is approved as an ADR/schema change before a signed badge appears.
- [ ] Text/TextBox protected behavior and P7 print-method policy are unchanged.

## 10. Non-goals

No device SDK, vendor selection, automatic retry/reprint, printer-native output, barcode geometry change, verifier certification, ISO/ANSI grade claim, or Figma edit is included in this documentation slice.
