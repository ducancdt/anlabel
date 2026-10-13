# ANLAbel product contract

One Windows desktop app. Offline. Design, check, and print industrial labels.

The normative development and acceptance framework is
[`CORE_SOFTWARE_STANDARD.md`](CORE_SOFTWARE_STANDARD.md). Label data sources
are Excel/CSV only: no SQL, SQLite, ODBC, or server database connectors.
Designer and data reliability take priority over automation expansion.

## What it does

1. Edit a template in millimeters.
2. Place Text, TextBox, barcode, image, shape, and line.
3. Bind local Excel/CSV and check every selected row.
4. Preview the same scene that print uses.
5. Preflight fonts, images, barcodes, media, DPI, and printable area.
6. Print to one named Windows queue. No silent fallback.
7. Keep local save, backup, job evidence, and recovery.

Activation, editions, cloud, login, and a second renderer are not the product.

Priority: correct label and preserved data → reliable, predictable operation →
simple and understandable workflow → speed → additional features.

NiceLabel manuals, screenshots and recreations are references, not a product
specification or a parity backlog. Adopt only the core ideas selected in
[`REFERENCE_POLICY.md`](REFERENCE_POLICY.md). Reliability includes avoiding
unnecessary stops and giving an actionable recovery path, while preserving
the safety contracts in the core standard.

## What it does not do

Web, cloud, login, sync, remote print gateways, hosted control planes,
alerts, collaboration, or a rewrite to another UI framework for looks.

## Text / TextBox

Text is content-owned. TextBox is frame-owned. Do not merge them. See
[`NICELABEL_TEXTBOX_RESEARCH.md`](NICELABEL_TEXTBOX_RESEARCH.md) and `AGENTS.md`.

## UI

User-visible UI needs a Figma file URL and node ID first
([`figma-ui-handoff-template.md`](figma-ui-handoff-template.md)).
Domain, save, parse, and tests do not.

## Done

A code slice is done when behavior, version, Fast (and Mutation for label-safety)
agree. Documentation-only work checks scope, consistency and links without
bumping the application version. See [`AUTOMATED_QUALITY_LOOP.md`](AUTOMATED_QUALITY_LOOP.md).
