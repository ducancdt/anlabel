# ANLAbel rules

`agent.md` / `AGENTS.md` win if this file disagrees.

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
