# Print calibration checklist

**Status:** current operational guide (2026-08-13) · physical-output evidence remains open

Calibration is a physical measurement workflow, not a substitute for print preflight or a claim that a printer has been certified. The architecture map is in [docs/architecture.md](architecture.md); the broader reliability plan is [print-preview-reliability-plan.md](print-preview-reliability-plan.md).

## What the current pipeline does

- PrintService prepares an effective PrintRenderPlan from the selected Windows queue and its PrintTicket. The plan carries label size, DPI X/Y, media type, gap, feed direction, rotation, offsets, scale, printable-area evidence, document/scene identities, and the output-contract fingerprint.
- LabelVisualRenderer.RenderCalibration draws the label border, 10 mm ruler marks, physical label dimensions, DPI, media, gap, feed, rotation, and printable-margin text. It is rendered through the same queue/plan path as calibration dispatch.
- Calibration and normal print use an explicit printer queue. A missing or implicit Windows default queue is rejected; the app must not silently print to another device.
- The dispatch path revalidates the effective output contract immediately before PrintDocument. A queue/ticket/DPI/media drift requires reopening preparation rather than submitting a stale plan.
- A successful Windows submission is reported as SpoolAccepted. That means the queue accepted the job; it does not mean the physical label was printed correctly or that a barcode was verified.

## Safe calibration procedure

### 1. Confirm media and queue

Before opening calibration, record:

- printer model, driver/version, and queue name;
- media mode: gap, black mark, or continuous;
- nominal label width and height;
- gap or mark pitch;
- printer DPI (203, 300, 600, or the actual non-square X/Y values);
- feed direction and whether the label is rotated 180 degrees.

Select the named queue explicitly. If the queue is missing, stop and repair the printer selection; do not choose the Windows default as a workaround.

### 2. Prepare the driver ticket

Set the driver and template to the same physical stock. Check orientation, media type, label size, gap/mark settings, darkness, speed, and any driver scaling option. ANLAbel should render from millimetres to printer units; Windows or the driver must not add an unreviewed “fit to page” scale.

Keep the following profile fields as measured corrections only:

- OffsetXMm
- OffsetYMm
- ScaleX
- ScaleY

First correct the stock and driver media definition. Use offsets or scale only after the physical measurement shows a repeatable error.

### 3. Print one calibration label

Run Test Print / Print Calibration and keep the result metadata with the job record. The ruler page is one label, not a production batch. Measure:

- outer label width and height;
- distance from the physical leading/left edges to the 0 mm marks;
- 10 mm intervals across both axes;
- gap/black-mark registration and feed direction;
- rotation and printable-margin behavior.

If the intervals are correct but the whole grid is shifted, adjust an offset. If the error grows across the label, investigate scale, DPI, stock size, or driver ticket before changing the design.

### 4. Confirm the design path

After calibration, open Print Preview with a synthetic, non-sensitive fixture:

1. one text object near each intended edge;
2. one TextBox with authored frame geometry;
3. one linear barcode and one matrix barcode;
4. a known row with no missing bindings.

Resolve every preflight issue. TextBox content must wrap/clip inside its authored frame; calibration must never be used to “fix” a TextBox by changing its width or height.

For linear barcodes, verify the effective X-dimension readout at the effective print DPI. FrameOwned is the legacy-safe default; Size width from X × modules is an explicit opt-in. For HRI, keep the selected None / Below / Above placement consistent with the available frame height.

### 5. Print and verify physically

Print exactly one synthetic label first. Measure the same edges and intervals again, then scan every barcode with the intended scanner. Record:

- printer/driver and media;
- template/document hash if shown in the print evidence;
- effective DPI X/Y and output-contract status;
- measured width/height/offset/scale;
- scanner result and any failed symbols;
- whether the queue exposed a spool identity.

Only after this single-label check should a batch be attempted. A green software test or SpoolAccepted result does not close the hardware/verifier gate.

## Evidence record

Attach the following to the owning release or support record:

- calibration label photo or measured worksheet;
- driver ticket/media settings;
- printer queue name and model;
- Print Preview screenshot with the effective plan/readout;
- preflight result and named regression output;
- physical scanner/verifier result;
- explicit open items when no hardware or verifier was available.

Keep production/customer data out of screenshots and examples. Use synthetic values.

## Named software gates

The current regression harness includes:

- print barcode uses plan (real print) dpi;
- vector barcode geometry uses independent device dots;
- preview and print render the same geometry, offset by the plan;
- preflight warns when barcode module too small at real print dpi;
- spool accepted does not claim physical completion;
- calibration dispatch honors pre-start cancellation;
- effective print-plan preparation honors pre-start cancellation;
- explicit print path fails closed without printer queue.

These gates prove software policy and queue safety. They do not replace the physical-printer checklist above.

## UI/UX note

The owner gate for the existing Preview/Printer Setup/Calibration workflow is [`PRINT_PREVIEW_CALIBRATION_UI_DECISION_PACKET.md`](PRINT_PREVIEW_CALIBRATION_UI_DECISION_PACKET.md). It keeps Figma shell nodes read-only and separates software queue acceptance from physical measurement.

The checked Figma references do not contain a dedicated calibration screen. If calibration UI changes, use the [Figma → WPF handoff template](figma-ui-handoff-template.md), record the exact state/node and target display scales, and attach a WPF runtime screenshot. Do not infer a new calibration workflow from the shell or barcode ribbon frames alone.
