# ANLAbel rules

`agent.md` / `AGENTS.md` win if this file disagrees.

Product decisions prioritize reliable industrial labels and simple operation.
Follow [core standard](../CORE_SOFTWARE_STANDARD.md) and
[reference policy](../REFERENCE_POLICY.md). NiceLabel parity is not a gate.

## Stability

1. Document changes go through an explicit command or user edit. Render,
   preview, and refresh do not write geometry.
2. One gesture is one undo. Cancel restores the prior document hash.
3. Designer, preview, and print share one compiled scene.
4. Named printer queue only. Missing queue fails closed. Spool accept is not
   physical completion. No silent default-printer fallback.
5. Excel/CSV I/O stays off the UI thread.
6. Text stays content-owned. TextBox stays frame-owned.
7. UI needs a Figma node first.
8. Fast after each code slice. Mutation ≥ 90 for label-safety. One public
   version, projections in sync.

Changing a live rule needs an ADR in `DECISIONS.md`.

Keep work proportional: diagnose operational failures before adding structure;
group repeated errors and give one useful next action. Optional metadata gaps
are not automatically blocking defects. Preserve output safety and never
automatically retry an uncertain dispatch. Documentation-only slices check
consistency and links without changing the public application version.
