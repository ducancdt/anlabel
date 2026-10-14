# ANLAbel execution plan

**Status:** live  
**Product:** [`../LOCAL_LABEL_PRODUCT_CONTRACT.md`](../LOCAL_LABEL_PRODUCT_CONTRACT.md)  
**Gates:** [`../AUTOMATED_QUALITY_LOOP.md`](../AUTOMATED_QUALITY_LOOP.md)

Keep the app compact, basic, and stable. History lives in `PLAN.md`. It is not
the backlog.

## Boundary

Priority: correct labels and preserved data, reliable operation, then simple
operator workflows. Use [reference policy](../REFERENCE_POLICY.md) to select
core ideas; competitor feature lists do not create tasks.

For each reported fault: establish the affected build/input and reproduce it,
fix the smallest coherent cause through the existing path, then check the
operator workflow and relevant failure/recovery cases. Investigate avoidable
print stops as well as unsafe output. Do not relax safety gates to hide errors.

Apply [`../CORE_SOFTWARE_STANDARD.md`](../CORE_SOFTWARE_STANDARD.md) for
requirement IDs and acceptance. Next work follows the restructuring program
[`13-ke-hoach-cai-to-loi.md`](13-ke-hoach-cai-to-loi.md): baseline lock and
evidence mapping, out-of-core surface reduction, file lifecycle (no lost
edits), then Designer and Excel/CSV reliability gaps, barcode verification and
preview/dispatch integration. SQL/ODBC and full NiceLabel JOB expansion are
outside core scope.

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

## Now (0.312)

Public version `0.312`. Printer/paper stock, authored DPI, document queue
identity, and print scale fail closed
([`PRINT_PRINTER_PAPER_FAIL_CLOSED.md`](../PRINT_PRINTER_PAPER_FAIL_CLOSED.md)).
Fast loop is the everyday gate. Mutation stays at 90 on the existing
label-safety list. Header/1024, Control Center, and C: clones are closed.

**Current program (accepted and in execution, 2026-10-13):**
[`13-ke-hoach-cai-to-loi.md`](13-ke-hoach-cai-to-loi.md), the core-stability
restructuring. Its §9 checklist is the only work queue. Owner decisions in
its §8 were approved on 2026-10-13 (recommended options). Order:

1. Lock the baseline: commit, release to the desktop, one work source, evidence
   mapping.
2. Remove unreachable and out-of-core surface.
3. Fix the file lifecycle: no lost edits, no silent failures, crash recovery.
4. Designer and Excel/CSV reliability.
5. Barcode and preview/dispatch.
6. Acceptance on real templates and printers.

A reproduced fault that stops an operator's print run is handled immediately at
any stage.

Excel DB04 formula-without-cache remains open: ExcelDataReader 3.7.0 cannot
distinguish a missing cache from a blank cell or a formula that cached an empty
string; see [`../EXCEL_NUMERIC_ERROR_GAP_AUDIT.md`](../EXCEL_NUMERIC_ERROR_GAP_AUDIT.md).

Do not open new product surfaces. If label-safety code changes, add at most one
existing contract to mutation after the combined score stays ≥ 90.

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
