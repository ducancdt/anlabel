# ANLAbel roadmap

The roadmap prioritizes reliable physical output on industrial label printers. It is directional rather than a promise of delivery dates.

## Documentation map (2026-08-13)

This file stays intentionally high-level. Use the following documents for the evidence and decisions behind each direction:

- [Master plan](MASTER_PLAN.md) — product history and release narrative.
- [Detailed plan](PLAN.md) — chronological implementation notes and named regression gates.
- [Continuation handoff](docs/reinvention/10-continuation-handoff-2026-08-13.md) — current Markdown reconciliation queue and ownership boundary.
- [Database plan](docs/database-plan.md), [print-preview reliability plan](docs/print-preview-reliability-plan.md), [properties panel plan](docs/properties-panel-plan.md), and [designer stability plan](docs/designer-stability-plan.md) — domain-specific historical plans.

Roadmap bullets are not release evidence. A shipped claim must be attached to a verified build/test/runtime checkpoint, and physical-printer, driver, verifier, and hardware claims remain open until external evidence exists. Figma references are visual inputs for a named UI/UX slice; a Figma frame is not runtime acceptance evidence by itself.

## Now

- Collect verified printer reports for Zebra, TSC, Godex, SATO, Argox, Honeywell, Intermec, Citizen, and Toshiba TEC drivers.
- Improve first-run guidance and diagnostic information for paper size, orientation, DPI, calibration, and print offsets.
- Expand regression coverage for Excel import, template persistence, barcode sizing, print geometry, and batch workflows.
- Improve documentation and generic example templates without embedding customer data.

## Next

- Add a structured printer-compatibility matrix based on physical test results.
- Improve accessibility, keyboard navigation, and localization coverage.
- Make issue diagnostics easier to export with sensitive data removed.
- Improve release packaging, checksums, and installation trust signals.

## Later

- Evaluate additional data-source formats requested by real users.
- Explore repeatable hardware-in-the-loop test procedures with maintainers who have physical industrial printers.
- Extend automation only where it preserves deterministic label output and operator review.

## Contributing to the roadmap

Open a feature request describing the real workflow, printer model, driver, DPI, label dimensions, expected behavior, and current workaround. Physical-printer evidence is more useful than a vendor name alone.
