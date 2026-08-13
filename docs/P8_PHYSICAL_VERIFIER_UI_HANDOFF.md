# P8 Physical Verifier / Grade UI/UX Handoff

**Status:** documentation-only, hardware-gated review contract
**Date:** 2026-08-13
**Owner:** printing / industrial verification product review
**Related phase:** P8 in [`INDUSTRIAL_BARCODE_EXECUTION_PLAN.md`](INDUSTRIAL_BARCODE_EXECUTION_PLAN.md)
**Owner decision packet:** [`P8_PHYSICAL_VERIFIER_DECISION_PACKET.md`](P8_PHYSICAL_VERIFIER_DECISION_PACKET.md)

## 1. Purpose and boundary

P8 defines how an operator will distinguish queue/spool evidence, software preflight, thermal golden metadata and a real physical-verifier observation. The UI must make a physical claim only when a real adapter returns accepted evidence bound to the reviewed print manifest. It must not turn a queue-completed row, a raster golden, a software preflight pass or an operator's visual inspection into a verifier grade.

This is a lab-first handoff. It does not add a device SDK, select a vendor, claim ISO/ANSI certification, or mark any current job as physically verified.

## 2. Current source evidence

| Evidence | Current finding | UI consequence |
| --- | --- | --- |
| [`PhysicalOutputVerification.cs`](../src/ANLAbel.Core/Printing/PhysicalOutputVerification.cs) | Methods are `Scanner`, `BarcodeVerifier` and `OperatorVisualInspection`; evidence is hash-only and carries manifest/job identity, expected/observed content fingerprints, device, grade and UTC timestamp. Visual inspection is never eligible for completion. | Show method and eligibility separately. Never label visual review or queue completion as a grade. |
| [`PhysicalVerifierAdapterContract.cs`](../src/ANLAbel.Core/Printing/PhysicalVerifierAdapterContract.cs) | Adapter observations require adapter identity/version, correlation, device identity and a canonical observed fingerprint. Timeout is bounded to five minutes and an in-flight adapter is busy-guarded. | Expose `Waiting`, `Verifying`, `Busy`, `Timed out` and identity/correlation failures as distinct states. |
| [`PhysicalOutputVerifier.cs`](../src/ANLAbel.Core/Printing/PhysicalOutputVerifier.cs) | Coordinator rejects invalid requests, missing evidence, adapter errors, thermal-golden mismatch and barcode payload/grade failures before lifecycle completion. | The readiness card is fail-closed and must preserve the returned diagnostic code. |
| [`BarcodeVerificationContract.cs`](../src/ANLAbel.Core/Barcode/BarcodeVerificationContract.cs) | Supported grade scales are ANSI, ISO15415 and ISO15416; the expectation carries a minimum grade and content fingerprint. | Always display scale, observed grade and minimum grade together; never compare grades across scales. |
| [`PrintJobManifest.cs`](../src/ANLAbel.Core/Printing/PrintJobManifest.cs) | Manifest fingerprints bind document, scene, rows, output contract, image and optional thermal golden metadata. | Verification cannot start against an invalid or changed manifest. |
| [`PrintJobState.cs`](../src/ANLAbel.Core/Printing/PrintJobState.cs) | `Completed` requires accepted scanner/verifier evidence bound to a valid manifest; operator actions and queue states cannot assert physical completion. | `Queue completed — unverified` is a first-class state, not a successful grade. |
| [`PrintCenterWindow.xaml`](../src/ANLAbel.App/PrintCenterWindow.xaml) | Current WPF Print Center lists recovery candidates and exports redacted support evidence; it has no verifier/device controls, and its recovery candidate does not expose verification evidence details. | The likely future host is a job-level verification detail region, but no implementation or host decision is closed by this document. |
| [`PrintSupportEvidenceContract.cs`](../src/ANLAbel.Core/Printing/PrintSupportEvidenceContract.cs) | Support export is deterministic and redacted; `physicalOutputVerified` is explicit. | Offer export for diagnostics without exposing raw payloads or implying physical proof. |

The current normalized adapter evidence has fingerprints but no vendor signature field. If signed device evidence is required for P8 closure, the ADR must define that schema and verification policy before the UI can display a signed badge.

## 3. Operator problem and intended outcome

For one print job, an operator needs to know:

1. whether the queue merely accepted or completed the job;
2. whether a verifier request is valid for the exact manifest and output contract;
3. which device/adapter observation was used and whether it is still in progress;
4. whether decoded content matches the reviewed digest and the grade meets the configured scale/threshold;
5. whether the result is eligible to mark the lifecycle `Completed`.

The intended outcome is a short, auditable decision: `Not verified`, `Verifying`, `Rejected`, or `Physically verified`. Every positive state links to evidence identity and timestamp; every negative state names a safe next action.

## 4. Ownership and placement

Verification is a job/output concern, not a barcode-object Properties toggle. The first host candidate is the existing Print Center's selected-job details, with a future History detail surface as a read-only destination. A verifier panel must remain separate from:

- barcode symbology, X-dimension, HRI and GS1 authoring controls;
- Text/TextBox content, frame and overflow behavior;
- printer-native method selection (P7);
- automatic retry or reprint dispatch.

Starting a verification observation may read a device, but it must not submit, duplicate, cancel or alter the print job. A retry creates a new correlation token and records an operator action; it never re-dispatches the label.

## 5. Evidence contract exposed to the UI

The detail surface should show these fields, with raw production values excluded:

| Field | Display rule |
| --- | --- |
| Job / manifest | Job ID, manifest fingerprint (full on details, shortened in list), validity badge. |
| Queue evidence | Printer, spool ID, queue state and timestamp; copy must say `not physical verification`. |
| Verification method | `Scanner`, `Barcode verifier` or `Operator visual inspection (audit only)`. |
| Request correlation | Correlation token/status; a mismatch is a blocked result. |
| Adapter/device | Adapter ID/version and device ID when returned; `Not connected` otherwise. |
| Content identity | Expected and observed SHA-256 fingerprints or redacted prefixes; never raw payload/image bytes. |
| Grade policy | Scale (`ANSI`, `ISO15415`, `ISO15416`), minimum grade and observed grade. |
| Thermal context | Bound/missing/mismatched thermal golden identity, separate from grade. |
| Evidence | Verification timestamp and evidence fingerprint; a future signature status is separate and explicit. |
| Completion eligibility | `Eligible`, `Not eligible` or `Unknown`, with the exact fail-closed reason. |

## 6. Host-neutral wireframe

```text
Physical verification
  Job                JOB-… · manifest A1B2… [Valid]
  Queue evidence     Zebra … · spool #… · Queue completed — not physical verification
  Method             [Barcode verifier                         v]
  Grade policy       ANSI · minimum A
  Device / adapter   [Not connected]  Evidence: Not available
  Correlation        —
  Content identity   Expected 9F… · Observed — · hash-only
  Thermal context    Not bound / not required
  Readiness          [Blocked] Connect a verified adapter before starting
  [Start verification] [Cancel] [Export redacted evidence] [Details]
```

When an observation is accepted, replace only the evidence fields and readiness:

```text
Readiness          [Physically verified] Eligible to complete lifecycle
Device / adapter   verifier-01 · Vendor.Verifier@2.0
Content identity   Expected 9F… · Observed 9F… · match
Grade policy       ANSI · minimum A · observed A
Evidence           2026-08-13 10:04 UTC · fingerprint 4C…
```

The button label `Completed` is reserved for a lifecycle transition that has passed the same evidence validation. A green card may say `Physically verified` only after that validation, not after a visual scan or queue update.

## 7. State and action matrix

| State | Operator-visible message | Safe action | Lifecycle claim |
| --- | --- | --- | --- |
| Not requested | `No physical verification requested` | Review manifest, keep queue evidence | Unverified |
| Queue completed — unverified | `Queue reports completion; physical output is not verified` | Start verification, acknowledge/reconcile, export evidence | Unverified |
| Manifest invalid/changed | `The reviewed manifest is invalid or no longer matches` | Re-open/re-prepare job; do not start device read | Blocked |
| Adapter unavailable | `No approved verifier adapter/device is available` | Connect/setup device or leave unverified | Blocked |
| Ready | `Manifest and verification request are valid` | Start verification | Pending |
| Waiting / verifying | `Waiting for device observation…` | Cancel; wait for bounded timeout | Pending |
| Adapter busy | `The verifier is still completing a previous observation` | Wait; retry only after idle | Unverified |
| Timed out | `Adapter did not respond within the configured timeout` | Inspect device, retry with a new correlation, or leave unverified | Unverified |
| Cancelled | `Verification cancelled; no physical claim made` | Start a new explicit observation | Unverified |
| No evidence / adapter error | `No accepted evidence returned` plus code | Inspect adapter/export support evidence | Unverified |
| Correlation/identity mismatch | `Observation does not belong to this job/device request` | Discard observation; start a new request | Blocked |
| Content mismatch | `Observed content does not match the reviewed digest` | Hold/reject label, investigate data/print path | Failed |
| Grade invalid/below threshold | `Observed grade is invalid/below <scale> <minimum>` | Hold/reject label; review media/printer settings | Failed |
| Visual audit only | `Visual inspection recorded for audit; not a verifier grade` | Keep audit record or run approved verifier | Unverified |
| Accepted | `Physical output verified against manifest` | Append evidence-bound completion | Eligible |

No state silently retries, reprints, changes print method, selects another queue or downgrades the grade policy.

## 8. Figma routing and evidence boundary

Read-only metadata was checked on 2026-08-13:

- Shell recreation [ANLAbel — NiceLabel Shell Recreation](https://www.figma.com/design/zdN71qfzrYV6pPt1b2FRRc/ANLAbel-%E2%80%94-NiceLabel-Shell-Recreation), node `2:2` (`1440 x 900`), supplies Print & Output `2:39`, status bar `2:170`, printer status `2:19` and paper status `2:21`.
- Control Center History [research shell](https://www.figma.com/design/asnGsLMxceJWb3HlfaE3q4), node `3:85` (`1280 x 800`), supplies filters `3:99`, activity frame `3:101`, and candidate details/reprint/errors language. Its sample rows are not ANLAbel job evidence.

Neither reference contains a verifier device, grade, correlation, rejected-observation or physically-verified state. Reuse them only for placement, row/detail hierarchy and status density. Do not create or edit a Figma frame until a real adapter/pilot supplies the first concrete state and the owner chooses whether Print Center or History owns the detail surface.

## 9. Proposed AutomationIds

| Control | AutomationId |
| --- | --- |
| Verification region | `Print.Verification.Region` |
| Job/manifest summary | `Print.Verification.Manifest` |
| Queue evidence | `Print.Verification.QueueEvidence` |
| Method selector | `Print.Verification.Method` |
| Grade policy | `Print.Verification.GradePolicy` |
| Device/adapter summary | `Print.Verification.Device` |
| Correlation status | `Print.Verification.Correlation` |
| Expected digest | `Print.Verification.ExpectedDigest` |
| Observed digest | `Print.Verification.ObservedDigest` |
| Thermal context | `Print.Verification.ThermalContext` |
| Verification status | `Print.Verification.Status` |
| Completion eligibility | `Print.Verification.CompletionEligibility` |
| Start observation | `Print.Verification.Start` |
| Cancel observation | `Print.Verification.Cancel` |
| Evidence details | `Print.Verification.EvidenceDetails` |
| Export support evidence | `Print.Verification.ExportSupport` |

## 10. Stop gates and non-claims

P8 stays open until:

1. a real printer/media/verifier fixture is identified and its correlation procedure is documented;
2. an approved adapter returns accepted evidence for at least one supported barcode and grade scale;
3. the evidence is bound to a valid manifest and output contract, with timeout/busy/cancel/error coverage;
4. the product ADR decides whether signed device evidence is required and defines its schema if so;
5. `Completed` remains impossible without accepted scanner/verifier evidence;
6. runtime UI evidence verifies state copy, keyboard order and redaction at target scales.

Until then, do not show ISO/ANSI grade badges, `Production certified`, `Verified output`, or `Physically completed` as product claims. Software preflight, thermal golden, spool acceptance and operator visual inspection remain explicitly non-equivalent.

The companion control-level contract is [`P8_PHYSICAL_VERIFIER_UI_SPEC.md`](P8_PHYSICAL_VERIFIER_UI_SPEC.md).
