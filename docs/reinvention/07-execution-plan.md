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
- UI changes are built directly in XAML/code and verified by build plus
  structural regression gates (owner direction 2026-10-27: no Figma step).

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

## Now (0.370)

Public version `0.370`. Printer/paper stock, authored DPI, document queue
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

14. **PDF417 error-correction level and truncated symbols (core only,
    2026-10-19).** Continuing the NiceLabel 5 reference review
    ([`NICELABEL_5_DEEP_REVERSE_ENGINEERING.md`](../NICELABEL_5_DEEP_REVERSE_ENGINEERING.md) §13–15), PDF417 objects gain
    `Pdf417ErrorCorrectionLevel` (-1 Auto, 0–8) and `Pdf417Truncated`, both
    stored in the label file and off by default. Auto passes no encoder
    hint, so existing templates render the identical symbol and keep their
    document hash; data that no longer fits at the chosen level fails
    closed in preflight. The owner was asked to pick the area, scope and
    REA depth and did not answer in time; the recommended options were
    used, so explicit owner approval is still outstanding. There is no
    Properties UI at the time; a Properties UI followed on 2026-10-27
    (§47 of the RE doc): the barcode card shows a PDF417 error-correction
    combo (Auto, levels 0-8) and a truncated checkbox only when the
    selected symbology is PDF417. Still no physical scan evidence.

    Evidence: `Pdf417ContractTests` (decode round trips at levels 0/2/5/8,
    truncated decode, capacity failure at level 8, clone/snapshot/save-load
    and hash behaviour); full build, unit tests, regression suite and the
    Fast loop pass. Not yet released; no version bump.

15. **Serial counter character sequences (core only, 2026-10-19).** The
    reference counter counts in data formats from `formats.def` (Hex,
    Letters, Digits&Capitals, a custom Sequence). `{#SERIAL}` gains the
    same choice: `Alphabet` (Numeric by default, Hexadecimal, Letters,
    DigitsAndLetters, Custom) plus `CustomAlphabet`. Reservations stay
    numeric, so the append-only store, reprint reuse, shared-folder
    sequence and preview identity are unchanged; only the written form
    of each reserved value changes (positional notation over the chosen
    characters, padded with the first character). A repeating or
    whitespace custom sequence and a negative range fail validation in
    plain Vietnamese at the UI boundary. Same approval status as item
    14: recommended defaults used after the question timed out, explicit
    owner approval outstanding; no UI at the time, but the serial counter
    panel gained an alphabet combo plus a custom-alphabet box on
    2026-10-27 (§47 of the RE doc), built directly in XAML. No physical scan
    evidence. Details in
    [`NICELABEL_5_DEEP_REVERSE_ENGINEERING.md`](../NICELABEL_5_DEEP_REVERSE_ENGINEERING.md) §16–18.

    Evidence: `SerialCounterTests` additions (positional rendering,
    disjoint reservations across the carry boundary, broken-sequence
    validation, save/load, document hash); full build, unit tests,
    regression suite and the Fast loop pass. Not yet released; no
    version bump.

16. **EAN/UPC height advice from decompiled reference check (2026-10-27).**
    Ghidra 12.1.4 was already installed on this machine, so REA decompiled
    `TBarcodeBase::IsBarcodeInStandard` in `bareng.dll` (Evidence
    `ev_a26da947430bd52f83580b6266f1305bd36321a0ffd36f895714d1ac9767b90a`):
    the reference engine compares both symbol width and height against the
    `barcode.def` windows and only returns a flag. Item 10 covered the width
    half through X; preflight now also advises, without blocking, when an
    EAN-13/EAN-8/UPC-A/UPC-E object's authored height is outside the same
    table (19.60-49.00 / 15.90-39.76 / 11.20-28.00 mm), with a Vietnamese
    operator rule. No geometry, template data or print path changes.
    `GetWhiteBits` proved to be a white-bit counter, not the Space
    correction insertion site, so that non-adoption stands; `CalcBarWidth`
    rounding remains unreadable through the x87 division and ANLAbel does
    not depend on it. Details in
    [`NICELABEL_5_DEEP_REVERSE_ENGINEERING.md`](../NICELABEL_5_DEEP_REVERSE_ENGINEERING.md) §22-23.

    Evidence: extended `RetailMagnificationContractTests` (boundary values
    equal to the `barcode.def` table), regression gate 7b (short EAN-13 gets
    non-blocking advice with a Vietnamese rule, nominal height gets none);
    full build, 1390 unit tests, regression suite and Fast loop pass. No
    physical scan evidence; not yet released, no version bump.

17. **NiceLabel 5 .lbl header reader (foundation, 2026-10-27).** The
    TD32 breakthrough in the reference review (decompiled
    `TLabel::LoadObjects`, the per-category streams, the `TObject::Load`
    read order, the full TIO vtable map and two sample record alignments)
    supports a first fail-closed foundation reader in
    `NiceLabel5LblReader`: it opens the OLE compound file, verifies the
    `NicePower` magic and format version 29, reads the label size in
    1/100 mm and lists embedded streams. A later structural pass (§46)
    decoded the full header — three stored strings (name, description,
    author) followed by six 1/100 mm fields whose third and fourth values
    are the label size — so the reader now walks that structure instead
    of a fixed offset; this also corrected the size read for files the
    fixed-offset reader had accepted (it was one field late). Unverified
    layouts are still rejected with a named reason instead of being
    guessed. Object conversion has since landed in slices (§53-81):
    barcode objects whose records fully cross-check (long rectangle,
    inch doubles, known symbology) convert with literal data or with a
    prompt-variable link resolved inside the same file, and fixed text
    objects convert with literal or single-mask variable content plus
    the stored font face, point size and bold. A later pass (§70) found
    the byte in front of the face name is the LOGFONT pitch-and-family
    byte, so non-Arial faces (Courier New, Times New Roman) convert
    too, multi-line CRLF captions convert, the geometry cross-check
    accepts the label's own unit (inch/cm/mm doubles), UPC-A records
    convert, and paragraph records (type 0x12E) whose content is a literal
    string in Contents convert to frame-owned TextBox objects. A
    barcode-only relaxed geometry path (§81) also converts records
    whose size doubles are a stale design size (MIL129L2).
    Paragraph records with RTF payloads, graphics and records
    linked to variables outside the file are not converted.
    Evidence: all 65
    installed sample labels swept (65 accepted with plausible sizes,
    cross-checked against size-bearing sample file names), regression gate
    `niceLabel 5 lbl header reader accepts verified layout and rejects variants`
    over a synthetic compound file; full build, 1406 unit tests and Fast
    loop pass. Details in
    [`NICELABEL_5_DEEP_REVERSE_ENGINEERING.md`](../NICELABEL_5_DEEP_REVERSE_ENGINEERING.md) §36-44.
    Not yet released, no version bump.

18. **NiceLabel 10 .nlbl import (2026-10-27).** The installed NiceLabel
    10 is managed .NET; decompiling its `Core.dll`/`NetCommon.dll` loader
    chain recovered the `.nlbl` package layout (WinZip-AES-256 ZIP with an
    XML `Formats/` entry and a `.nsln` variable table, geometry in
    1/1000 mm) and the fixed package password its own loader derives.
    ANLAbel now opens `.nlbl` like `.lbl`: a new unsaved template at the
    stored size/name converting position/framed texts (Text/TextBox),
    in-package variable bindings, linear barcodes with their authored
    module width, QR/DataMatrix codes through ANLAbel's 2D auto-size
    contract (vendor sizing enums recovered from
    `Barcodes.Interfaces.dll`), PDF417 (frame seeded from the production
    encoder's module grid), lines and rectangles. Follow-up 2026-10-29:
    the solution entry inside vendor packages is `.slnx` (the reader had
    looked for `.nsln`, so no real-file variable ever resolved); every
    object is now placed through the recovered anchoring-point/rotation
    contract (`NiceLabel10Placement`, 205 rotated and 615 anchored corpus
    objects; the app supplies its text layout for position texts);
    human-readable placement follows the vendor enum (0 = off); embedded
    PNG and WMF graphics become images (WMF played by GDI+ at the
    authored frame); variables keep prompt text, default, length and
    digit facts; current-date variables become `{#NOW:format}`;
    concatenations, mixed contents and database fields become mixed
    binding expressions; `ScaleFactor` below 100 becomes a FixedFrame
    Text at the scaled width and framed-text `BestFit` becomes
    ShrinkFont/ScaleWidth with the stored bounds (vendor `BestFitType`
    from `FormatDocument.dll`); rich-text items carry their recovered
    plain text; GS1-128 compositions become `(AI)value` expressions
    through the vendor AI table recovered from `AdvancedFunctions.dll`,
    ANSI MH10.8.2 data identifiers and the ISO/IEC 15434 format 06
    envelope become prefixed/separated expressions, and date arithmetic
    on the current date becomes an offset `#NOW` field; GS1 parts with a
    computed check digit and substring functions become formulas through
    the new `GS1CHECKDIGIT` and `MID` formula functions (prompt/date names
    inside formulas now reach the job-variable contract). Objects bound
    to script/HIBC/delimited-subset functions, GS1 DataBar, payload-less
    graphics and report items stay unconverted and are counted in the
    status line; a graphic without an embedded payload is read from its
    file when that file sits beside the label. Evidence: production sweep
    over all 207 installed sample labels (207/207 opened, 6,233 of 6,467
    objects converted honestly —
    the earlier 6,298 included bound objects silently fixed to their
    sample text), the regression gate
    `opening a niceLabel 10 nlbl imports its label as a new template` and
    unit tests `NiceLabel10PlacementTests`/`NiceLabel10ImportMappingTests`.
    Printer and paper (2026-10-29): `Media/PaperFormat` is the Windows
    `DMPAPER` id and `DevModeBuffer` a DEVMODEW; labels on label stock get
    manual physical media and the authored DPI, driver-settings labels keep
    driver-automatic paper; since the owner's 2026-10-29 request office
    sheets and multi-label sheets are imported as NiceLabel prints them
    (`LabelStockContract` allows office sheets, `SheetLayoutContract` +
    the print service lay several labels on one sheet), text follows
    NiceLabel `Font Scaling` and VariableHeight, and the authoring printer
    is named in the import status, never selected.
    Details in
    [`NICELABEL_10_REVERSE_ENGINEERING.md`](../NICELABEL_10_REVERSE_ENGINEERING.md).
    Not yet released, no version bump.

**Reference spike (2026-10-19, no product code):** a stdlib-only CFBF
reader parses all 65 sample `.lbl` files; label size (mm/100 integer pair
plus display-unit doubles), title, printer strings, variable names,
fixed texts, font and barcode-type strings are recoverable from
`Contents`. Object-record layout is not yet decoded confidently (mixed
string conventions, version-delta streams), so no `.lbl` import was
added; a header-only import would silently drop objects. Next evidence
routes need an owner decision: differential saves with the installed
NiceLabel 5, or Ghidra decompilation. See
[`NICELABEL_5_DEEP_REVERSE_ENGINEERING.md`](../NICELABEL_5_DEEP_REVERSE_ENGINEERING.md) §19–21.

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
