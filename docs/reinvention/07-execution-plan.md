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

## Now (0.325)

Public version `0.325`. Printer/paper stock, authored DPI, document queue
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

Excel DB04 formula-without-cache is closed (2026-10-13, `1ed2367`):
ExcelDataReader 3.7.0 alone cannot tell a missing cache from a blank cell, so
`XlsxFormulaValueInspector` reads the sheet XML and stops the import at a
formula cell that has no `<v>`, naming the cell. A formula that cached an empty
string still imports. Background:
[`../EXCEL_NUMERIC_ERROR_GAP_AUDIT.md`](../EXCEL_NUMERIC_ERROR_GAP_AUDIT.md).

**Barcode and serial additions (owner-approved 2026-10-17).** A reference
review of NiceLabel 5 barcode behavior found, and this work fixed, five barcode
defects:

- Code 39 with an explicit ratio counted 1 module.
- Code 39 with an explicit ratio printed uneven bar widths. Bars are now whole
  dots with a realized ratio of 2.2–3.0:1, per ISO/IEC 16388.
- Code 39 lost its first bar at quiet zone 0.
- HRI in Auto check digit mode hid a real data character in 1 of 10 ITF values.
  Only Verify and Generate hide the check digit now (CORE B02).
- Frame bearer bars covered the outer bars. Preflight now blocks this.

The owner then approved four features. All of them are stored in the label file
and off by default:

1. **Check digit `Generate`.** The data is the body and the check digit is
   appended. Symbology validation runs on the encoded payload.
2. **Bar width reduction in whole dots.** It trims the trailing edge of each bar.
   Preflight blocks a reduction that is not smaller than the narrow bar.
3. **Custom check digit for numeric data.** The operator sets the modulus,
   cyclic weights, direction, sum of digits, complement and whether to keep all
   digits. It works on Code 39, ITF and Code 128. A broken definition blocks and
   never falls back to the standard rule.
4. **Template serial counter `{#SERIAL}`.**
   - The operator sets start, step, change every N labels, min/max, rollover,
     zero padding, prefix and suffix.
   - Values become part of each label's row, so the manifest hashes them.
   - Values are reserved in an append-only, mutex-guarded store before the
     manifest. They are never returned, so a failed or uncertain job leaves a
     gap and never a duplicate.
   - Preview refuses to print numbers other than the ones it showed.
   - A reprint reuses the original job's reservation.
   - The number never goes back without an explicit Reset.

Evidence: unit tests (`Code39RatioContractTests`, `BarcodeCheckDigitContractTests`,
`CustomCheckDigitAlgorithmTests`, `SerialCounterTests`, `DeviceBarcodeLayoutTests`)
and the regression gates `itf packaging linear barcode bearer bars preflight and
layout` and `serial numbers are reserved before the manifest`.

A second batch was owner-approved on the same day. All of it is off by default:

5. **HRI mask** (`(***) ***-****`). It needs exactly one position per HRI
   character. Otherwise the plain text prints and preflight blocks the row, so a
   mask can neither drop nor invent data.
6. **Code 39 intercharacter gap** of 1–5 X, and **bearer thickness in
   narrow-bar multiples**, resolved from the whole-dot X in renderer and
   preflight.
7. **Code 128 code set**: Auto, forced A/B/C (ZXing `FORCE_CODE_SET`), or
   Manual `<MODEA>`/`<MODEB>`/`<MODEC>`.
   - Manual uses an ISO/IEC 15417 encoder on ZXing's own writer base. Tests decode
     its output back with ZXing, and a single-set symbol matches ZXing bit for bit.
   - The markers are not printed in the HRI.
   - Encoder options now come from one `BarcodeRenderOptions.FromObject`. Two
     designer readouts and the size-from-X width used to drop the Code 39 ratio.
8. **Job values.**
   - `{#NOW[+n d|w|mo|y|h]:format}` dates and `{?Name}` values entered at print
     time. Optional definitions per template set the question, default,
     maximum length, digits only and required.
   - They are written into every label row and recorded per job, before the
     manifest, in an append-only store. A reprint repeats the original date and
     answers without asking again.
   - Preview refuses to print a date that changed since it was shown.

9. **Custom check digit value map and presets.**
   - `10=X;11=0`, plus "zero remainder gives the modulus".
   - Presets: GS1 mod 10, Luhn, Mod 11 with 10 = X (ISBN-10), and UPU S10.
   - Verified against the published vectors 030640615→2, 080442957→X and S10
     12345678→5, including the remainder-0 → 5 case.
   - A letter check digit is refused for ITF by the existing symbology check.

10. **EAN/UPC magnification advice.** The reference engine checks symbol size
    against a standard. ANLAbel now warns, without blocking, when EAN-13, EAN-8,
    UPC-A or UPC-E print outside the GS1 retail range of 80–200 % (X 0.264–0.660 mm,
    whole-dot X at plan DPI). In-house labels often shrink EAN codes on purpose.

11. **Hairline advice.** The designer draws lines and outlines at least one
    screen pixel wide, but print uses the real thickness. Preflight now warns,
    without blocking, when a line, rectangle or ellipse outline is thinner than
    one printer dot at the plan DPI.

Evidence (second batch): `BarcodeHriMaskContractTests`, `AdvancedLinearBarcodeTests`,
`JobVariableTests`, and the regression gate `job dates and entered values are
recorded before the manifest`.

12. **Virtual print gate and a tighter bar width reduction floor.**
    - The regression gate `virtual print scans serial, date, check digit and
      reduced bars` renders through the print renderer at 203 DPI and decodes
      with ZXing. It covers a reserved serial plus job date, an ITF with a
      generated check digit, and 0/1-dot reductions.
    - It found that a 3-dot narrow bar reduced by 2 dots no longer scans.
      Preflight now blocks any reduction that leaves the narrow bar below the
      existing 2-dot module floor.
13. **Shared serial counter, optional.** Set a shared folder on the counter
    (for example a network share) to keep its reservations there. Several PCs
    then use one sequence. Reservations are serialized by an exclusive lock file
    that works across machines, in addition to the per-PC mutex. Empty, the
    default, keeps the sequence per Windows user on one PC.

Still unverified, and needs real labels and printers:
- Bar width reduction values against a scanner or verifier on thermal stock.
  The virtual print proves the software output, not the ink gain.
- Serial printing on a physical queue.

The shared counter was checked over SMB (`\\localhost\H$` and `\\127.0.0.1\H$`)
with four separate processes reserving 25 jobs each. Two of them used a different
UNC spelling, so they had different per-PC mutexes and only the lock file
protected them, as between two real PCs. Result: 300 numbers, all distinct and
contiguous. A share served by another physical PC was not tested.

Files using these features need this version or later.

No other new product surfaces. If label-safety code changes, add at most one
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
