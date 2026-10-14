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

## Measured limits

Measured on the development PC, not yet on the 4 GB operator PC: import on
2026-10-13, Print Preview again on 2026-10-14 on an idle machine (three runs;
the range is shown). Source: regression gates `import and preview cost is
measured for 1, 1000 and 10000 rows` and `design stays responsive with 200
objects` (MEASURE lines). The gate opens the preview for 10,000 rows only when
`ANLABEL_MEASURE_LARGE_PREVIEW=1` is set.

| Operation | 1 row | 1,000 rows | 10,000 rows |
| --- | --- | --- | --- |
| Import XLSX, 6 columns | 6 ms | 50 ms, +0.3 MB | 0.45 s, +3.5 MB |
| Import CSV, 6 columns | 1 ms | 4 ms, +0.1 MB | 0.04 s, +1.3 MB |
| Open Print Preview, every row checked (one text and one EAN-13 per label) | 0.05 s; 0.55–0.74 s when it is the first window the process opens | 1.0–1.3 s | 8.6–9.6 s |

- Import runs off the UI thread, with Cancel. It stops after 45 s for a local
  file and 30 s for a network file.
- Print Preview shows progress and can be cancelled. Its time grows about
  linearly with rows × barcodes.
- With 200 objects, a drag costs about 6 ms per frame. With the largest real
  template (49 objects), it costs under 1 ms.

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
