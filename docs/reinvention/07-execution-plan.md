# ANLAbel execution plan

**Status:** live  
**Product:** [`../LOCAL_LABEL_PRODUCT_CONTRACT.md`](../LOCAL_LABEL_PRODUCT_CONTRACT.md)  
**Gates:** [`../AUTOMATED_QUALITY_LOOP.md`](../AUTOMATED_QUALITY_LOOP.md)

Keep the app compact, basic, and stable. History lives in `PLAN.md`. It is not
the backlog.

## Boundary

- One offline Windows desktop app.
- No cloud, login, web, second renderer, or silent default-printer fallback.
- Render/preview/refresh never mutates authored geometry.
- Text/TextBox stays on the protected contract.
- UI changes go through Figma first.

## What already works (keep it)

```text
L0  save, version, tests
L1  designer + one compiled scene
L2  local Excel/CSV binding
L3  preview, preflight, named-queue print, recovery
L4  local file-drop on the same spine
```

Do not add a parallel path. Fail closed on missing queue, stale data, or
ambiguous print outcome.

## Now (0.264)

Public version $Version"
Replace-Required 'docs/reinvention/MEMORY.md' 'Version: \d+\.\d+' . Fast loop is the everyday gate. Mutation stays at 90
on the existing label-safety list. Header/1024, Control Center, and C: clones
are closed.

**Next increment:** protect compactness, basics, and stability. Do not open
new product surfaces. If label-safety code changes, add at most one existing
contract to mutation after the combined score stays ≥ 90.

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/Invoke-ANLAbelQualityLoop.ps1 -Mode Fast
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/Invoke-ANLAbelQualityLoop.ps1 -Mode Mutation
```

## History

Old checkpoints are in `PLAN.md`. Headings below exist only so old links resolve.

### Implementation checkpoint v0.100 (2026-08-09)

### Latest audit addendum v0.145 — verification work, not a hardware claim

### Industrial reliability research addendum v0.127 (2026-08-10)

### Audit-driven plan addendum (research review v0.120)
