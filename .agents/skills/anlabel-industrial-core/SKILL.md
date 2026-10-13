---
name: anlabel-industrial-core
description: Review ANLAbel product scope, agent rules or cross-cutting reliability changes against its industrial label workflow; use for competitor-derived proposals and recurring operational failures.
---

# ANLAbel industrial core

Use this skill within the ANLAbel repository. Resolve paths below from its root.
Read `AGENTS.md`, `docs/LOCAL_LABEL_PRODUCT_CONTRACT.md` and the relevant section
of `docs/CORE_SOFTWARE_STANDARD.md`. For competitor-derived work also read
`docs/REFERENCE_POLICY.md`. The live plan is `docs/reinvention/07-execution-plan.md`.

## Decide from the operator's task

Identify the intended workflow and observable failure or requested benefit.
Prioritize correct labels, preserved data, predictable operation and a simple
recovery path. Feature count, competitor parity and extra validation layers
are not acceptance criteria.

For diagnosis, inspect current build identity, selected template/data/printer,
logs and the relevant code. Separate confirmed causes from hypotheses. Do not
send physical print jobs or mutate printer settings solely to collect evidence
unless the user's task authorizes that operation.

For implementation, use the existing document/data/preview/print path. Add an
abstraction only for a concrete responsibility or test seam. Protect Text/TextBox
and authored geometry. A reference manual never authorizes changing these.

## Evaluate errors and usability

Distinguish invalid output or stale input, uncertain dispatch requiring
reconciliation, and nonblocking diagnostics. Missing optional driver metadata
does not by itself prove unsafe output. Do not invent capability, weaken a
protected gate or automatically retry a job whose outcome is unknown.

Show the operator the problem, affected row/object/job and next useful action.
Group duplicates and put hashes/technical evidence in details. Keep advanced
settings away from the normal path without hiding meaningful choices.

## Select references and verify

Adopt a NiceLabel idea only when it solves an in-scope ANLAbel need. Record the
selected behavior and its acceptance check; do not import an entire feature
portfolio or architecture. Older plans and unchecked research lists are not
assignments.

Run checks appropriate to changed behavior and retain required safety gates.
For documentation-only edits, check conflicting instructions, local links,
scope and preservation of approved contracts. Do not bump a runtime version.
For code, follow `docs/AUTOMATED_QUALITY_LOOP.md` and version rules. Software,
driver observations and physical output are separate evidence levels.

Report the result, evidence and remaining limitations. Update a decision record
only when a live decision changes; routine fixes do not require a new framework,
audit program or delegated agent.
