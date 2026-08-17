# ANLAbel product contract

One Windows desktop app. Offline. Design, check, and print industrial labels.

## What it does

1. Edit a template in millimeters.
2. Place Text, TextBox, barcode, image, shape, and line.
3. Bind local Excel/CSV and check every selected row.
4. Preview the same scene that print uses.
5. Preflight fonts, images, barcodes, media, DPI, and printable area.
6. Print to one named Windows queue. No silent fallback.
7. Keep local save, backup, job evidence, and recovery.

Activation, editions, cloud, login, and a second renderer are not the product.

Priority: correct label → deterministic behavior → no crash/data loss → then speed.

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

A slice is done when code, version, Fast (and Mutation for label-safety)
agree. See [`AUTOMATED_QUALITY_LOOP.md`](AUTOMATED_QUALITY_LOOP.md).
