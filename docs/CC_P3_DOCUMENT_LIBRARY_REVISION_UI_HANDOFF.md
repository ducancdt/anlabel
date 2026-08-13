# ANLAbel — CC-P3 Document Library + Revision UI handoff

**Status:** roadmap/design review; documentation-only checkpoint (2026-08-13)
**Owning roadmap:** [`MASTER_PLAN.md`](../MASTER_PLAN.md), section `2. Documents — storage, versioning, workflow`
**Related continuation:** [`reinvention/10-continuation-handoff-2026-08-13.md`](reinvention/10-continuation-handoff-2026-08-13.md)
**Protected contract:** [`AGENTS.md`](../AGENTS.md) remains authoritative for Text/TextBox behavior.

This handoff turns the CC-P3 “local document library → revision everywhere” milestone into one reviewable WPF slice. It records current source evidence and a Figma route; it does not claim that local folders, search, check-out, workflow states, or a unified browser have shipped.

## 1. Product boundary

NiceLabel’s reference workflow combines document storage, folders, search, preview, revision history, compare, check-out/check-in, permissions, and approval. ANLAbel’s current product is filesystem-first and desktop-only:

- `TemplateLibraryWindow` presents embedded `.anlabel` resources as a gallery and materializes a fresh editable copy.
- `TemplateRevisionWindow` is available for a saved current file and already inspects the primary, managed `.bak`, and bounded `.revisions` archive.
- `ProjectRevisionService` validates candidate bytes before exposing them as restorable and compares semantic document snapshots; `ProjectRevisionArchive` keeps hash-addressed local snapshots and JSONL audit events.
- There is no shared multi-user storage, folder ACL, lock server, approval enum/action surface, or first-class visual compare of two production revisions.

CC-P3 therefore means **local document library plus revision access**, not a web Document Storage clone. CC-P4 (workflow enum/gate) follows only after its policy owner decides which state is allowed to print and where the durable transition record lives. Multi-user locking, sync servers, and RemoteApp/browser extensions stay out of scope.

## 2. Existing WPF/source evidence

| Surface/evidence | Current behavior | CC-P3 implication |
| --- | --- | --- |
| [`TemplateLibraryWindow.xaml`](../src/ANLAbel.App/TemplateLibraryWindow.xaml) | `1000 × 700` window, `720 × 460` minimum; `190 DIP` filter rail; `WrapPanel` gallery; `216 DIP` cards with `200 × 140` thumbnails; `UseButton` is disabled until selection. | Keep the compact gallery as a reusable shell, but add a configured local root, folder scope, search, and explicit selected-item metadata before calling it a document library. |
| [`TemplateLibraryWindow.xaml.cs`](../src/ANLAbel.App/TemplateLibraryWindow.xaml.cs) | Filters are generated from embedded template group/type; cards select/double-click; `ChosenTemplate` is materialized through `TemplateLibraryService`. | Preserve embedded templates as a fallback/catalog source. Do not pretend embedded resources have filesystem identity, revision lineage, or check-out state. |
| [`TemplateLibraryService.cs`](../src/ANLAbel.App/TemplateLibrary/TemplateLibraryService.cs) | Reads manifest resources, skips malformed entries, describes group/type, and deserializes a fresh editable copy. | The first local-library adapter should be additive and use the same validation/materialization boundary. Invalid files must remain visible as diagnostics or be explicitly excluded with a reason. |
| [`LibraryThumbnailRenderer.cs`](../src/ANLAbel.App/TemplateLibrary/LibraryThumbnailRenderer.cs) | Renders label geometry, text, lines, rectangles, representative barcode stripes and QR/DataMatrix glyphs into a frozen bitmap; binding tokens are shown as field-name placeholders. | Reuse the renderer/preview pipeline for local files. Record thumbnail failure separately from document-load failure; do not make a thumbnail a print-validity claim. |
| [`TemplateRevisionWindow.xaml`](../src/ANLAbel.App/TemplateRevisionWindow.xaml) | `980 × 520` window (`760 × 420` minimum); read-only revision grid; Refresh, Restore selected revision, Close; status/diff area. | Keep revision inspection dense and keyboard reachable. A future library selection should open the same revision owner, not a second recovery implementation. |
| [`TemplateRevisionWindow.xaml.cs`](../src/ANLAbel.App/TemplateRevisionWindow.xaml.cs) | Calls `ListAllAsync`, `CompareAsync`, and `ListAuditAsync`; restore is enabled only for a valid backup/archive; confirmation warns that unsaved edits are discarded; reloads the open document after commit. | The restore confirmation and post-commit reload are acceptance gates. Any library entry point must preserve them and report the new primary/backup/archive evidence. |
| [`ProjectRevisionService.cs`](../src/ANLAbel.Project/SaveLoad/ProjectRevisionService.cs) | `Primary`, `Backup`, `Archive` entries expose existence, validity, size, timestamp, template name, hash prefix, status and diagnostic. Semantic diff covers size/DPI/printer/media/transform/objects/guides/data; restore allows only managed backup or local archive and validates before commit. | Revision UI can be reused for every saved local path. “Exists” is not “safe”; invalid/unsupported revisions remain non-restorable and must show diagnostics. |
| [`ProjectRevisionArchive.cs`](../src/ANLAbel.Project/SaveLoad/ProjectRevisionArchive.cs) | Hashes and durably writes bounded `.revisions` snapshots, appends JSONL audit entries, tolerates a torn final audit line, and trims local history. | Show local lineage/audit evidence without implying server-side retention, user identity, or tamper-proof multi-user governance. |
| [`MainWindow.xaml.cs`](../src/ANLAbel.App/MainWindow.xaml.cs) | Template Library opens from the existing entry point and loads the selected materialized template; Revision History currently requires a saved current path. | Decide whether CC-P3 extends these entry points or introduces one `DocumentLibraryWindow`; do not strand revision access behind only the current-document menu. |

## 3. Figma reference and routing

Use the existing Control Center research file [ANLAbel Control Center](https://www.figma.com/design/asnGsLMxceJWb3HlfaE3q4) as design input only. Read-only metadata was checked on 2026-08-13; no Figma node was edited or duplicated.

### Documents / storage shell

Frame `3:2` (`CC / Documents — Storage`, `1280 × 800`) is the closest reference for local browsing:

| Node | Measured structure | WPF question |
| --- | --- | --- |
| `3:16` | Toolbar at `(16,104)`, `1248 × 40`; Browse, Edit, Files & Folders, Revision History, Workflow, Deleted Items, Preview, and search copy. | Which actions are actually local-safe in M1? Revision and workflow must not be enabled just because the label exists in research. |
| `3:19` | Folder rail at `(16,156)`, `240 × 620`; Root, Approved, Staging, Production, Archive, Templates, Images, Fonts. | Owner must choose configured-root semantics and whether these are real folders, virtual filters, or a seeded example tree. |
| `3:29` | Files pane at `(272,156)`, `980 × 620`; icon/list-style card region. | Map to a single intentional scroll owner and provide selected-file details/keyboard focus. |
| `3:31`–`3:84` | Sample 100 × 110 cards for `label_1.nlbl` … `label_18.nlbl`. | Treat labels as visual density guidance only; they are not repository files or runtime content. |

The storage frame has no selected-file detail, invalid-file, revision-diff, restore-confirmation, or unsaved-edit state. Those states require WPF/source evidence and acceptance screenshots; do not invent a Figma edit solely to fill them.

### Workflow research, deferred to CC-P4

Frame `7:2` (`CC / Documents — Workflow`) is a state-specific research reference, not current ANLAbel behavior:

| Node | Reference state/action | ANLAbel boundary |
| --- | --- | --- |
| `7:23` | `220 DIP` sidebar with Root, Approved, Drafts, In Review, Published, Rejected. | No persisted workflow enum exists in the current envelope; these are deferred folders/statuses. |
| `7:38`–`7:53` | Draft → Request approval → Approved → Published flow. | Do not add buttons or print gates until the owner approves policy, transition authority, and durable audit shape. |
| `7:54`–`7:58` | Rejected and Scheduled for publishing alternates. | Scheduling is explicitly later than a durable local workflow store. |
| `7:59`–`7:67` | Approve, Reject, Request changes, Publish now actions. | Research vocabulary only; no command service or permission model is shipped. |
| `7:68`–`7:87` | Step history table: When, User, From, To, Comment. | Local revision JSONL has timestamps/reasons but no authenticated user/role identity; do not present it as approval history. |

Route implementation/design questions through [`figma-ui-handoff-template.md`](figma-ui-handoff-template.md). A new Figma frame is unnecessary for the M1 browse/revision decision unless the owner requires a concrete selected-file or invalid-state design; if created later, keep it state-specific and link the node here.

## 4. Proposed vertical sequence

### M1 — Browse local documents

1. Choose a configured local library root and define whether missing roots are created, selected, or reported.
2. Enumerate supported `.anlabel` files under the root with deterministic folder/file ordering; keep embedded templates in a separate “Built-in” source.
3. Add search and folder/filter state without nested scroll owners; preserve `200 × 140` thumbnail guidance unless runtime measurements show a target-scale problem.
4. Show selected-file metadata: path relative to root, size, last write, load status, template name, label dimensions, and thumbnail status.
5. Open the validated file through the existing project-load path. Never silently replace a selected file with a materialized embedded copy.

### M2 — Revision everywhere

1. Make revision history reachable from every saved local-library document, not only the current MainWindow path.
2. Show primary, managed backup, local archived snapshots, validity, diagnostic, hash prefix, timestamp, and audit reason.
3. Permit compare only when both physical inputs validate; keep “diff unavailable” explicit when either side is invalid/unsupported.
4. Require a confirmation that names the source revision, warns about unsaved edits, and explains that the current primary is preserved as backup/archive.
5. Reload the open document only after the atomic restore completes; report the resulting primary and retained previous-primary evidence.

### M3 — CC-P4 workflow gate (owner decision required)

Only after M1/M2 acceptance and a policy decision: add Draft/InReview/Approved/Published/Rejected to the template envelope, append transition audit events, and define whether non-Published print is blocked or warned. Scheduled publishing, role/identity, multi-user locking, folder ACLs, and sync remain later decisions.

## 5. State and failure matrix

| State | Required visible evidence | Safe action | Fail-closed rule |
| --- | --- | --- | --- |
| Missing/unconfigured root | Root path, reason, Configure/Choose action | Choose or create root only after explicit confirmation | Do not show an empty root as a healthy library. |
| Loading | Busy indicator and scope/search text | Cancel or wait; preserve prior selection | Do not open stale selection as if it were current. |
| Empty folder / no search match | Folder/filter and count | Clear search or choose another folder | Keep source and root visible; no phantom cards. |
| Valid selected file | Thumbnail, relative path, metadata, validation status | Open, Preview, Revision History | Open only through validated project load. |
| Invalid/unsupported file | Card or diagnostic row with reason | Inspect/remove from view; repair externally | Disable Open/Use/Restore; never materialize invalid bytes. |
| Unsaved current document | Explicit dirty indicator | Save, discard with confirmation, or cancel | Revision restore cannot silently discard edits. |
| Primary / backup / archive valid | Kind, timestamp, size, hash prefix, reason | Compare; restore backup/archive | Restore only approved local source kinds. |
| Revision invalid/missing | Status and diagnostic | Refresh or remove stale row | Restore disabled; no “exists = safe” shortcut. |
| Diff not comparable | Explain which input failed validation | Refresh/revalidate | Do not claim semantic equality/difference. |
| Restore confirmation | Source path/name, unsaved-edit warning, retention behavior | Confirm or Cancel | No write before explicit confirmation. |
| Restore success/failure | New primary, retained previous primary, error detail | Reload, retry, or open audit | Atomic commit boundary must not be reported as canceled after success. |
| Workflow states (CC-P4 research) | State, actor, transition/comment | Owner-approved action only | Keep controls hidden/deferred while no policy/enum exists. |

## 6. WPF, accessibility, and acceptance gates

| Gate | Evidence required before implementation closure |
| --- | --- |
| Host decision | Owner chooses extension of `TemplateLibraryWindow` versus a new `DocumentLibraryWindow`; revision remains one service/window owner. |
| Target scale | Runtime screenshots or UI Automation at `1024 × 600`, `100%`, `125%`, and `150%`; no clipped path/status/action copy. |
| Keyboard | Folder/file focus order, search shortcut, Enter=open, Escape=cancel/close, and restore confirmation keyboard path recorded. |
| Automation | Stable names/AutomationIds for root chooser, search, folder tree, file card/list, metadata panel, Preview, Revision History, Compare, Restore, and confirmation actions. |
| Data safety | No silent overwrite/materialization; unsaved edits and invalid revisions remain explicit. |
| Print/preview | Opening/previewing must use existing validated load/preview paths; no Text/TextBox ownership, sizing, wrapping, clipping, padding, resize, or print-parity changes. |
| Regression | Add named application regression for browse/open, invalid-file handling, revision reachability, diff-unavailable, restore confirmation, and post-restore reload; add unit coverage for root enumeration/order and state policy. |
| Build/test | Rerun the required build, unit, and application gates after implementation scope is staged. This documentation checkpoint does not claim those gates for a future UI change. |
| Figma | Record exact node/state and measured dimensions; a Figma frame alone is not runtime proof. No new file/edit is required for this handoff. |

## 7. Owner decisions needed

1. **Root:** one configured root per workstation, multiple roots, or a project-relative root? What happens when it disappears?
2. **Host:** extend the current Template Library, add a new Document Library window, or expose both built-in and local sources in one shell?
3. **Preview:** selected-file metadata only, preview pane, or a separate preview window? Which action opens an editable document?
4. **Revision scope:** should every local file expose `.bak`/`.revisions` immediately, and is local retention count configurable or fixed at eight?
5. **Workflow policy:** is non-Published print blocked, warned, or allowed by default? Who can approve/reject/publish on a single-machine installation?
6. **Check-out:** is the first flag informational, edit-blocking, or only an audit marker? What is the recovery action for a stale flag?
7. **Automation names:** approve stable control names before XAML work so keyboard/UI Automation evidence is repeatable.

## 8. Decision

**Needs design/product review.** Figma `3:2` is sufficient to route the browse shell and `7:2` is useful for CC-P4 workflow vocabulary, but neither supplies the selected-file, invalid-file, diff, restore, or unsaved-edit states required for a safe local implementation. Current WPF/source evidence supports an additive M1/M2 plan; it does not authorize workflow controls, check-out locks, folder ACLs, or a second revision stack.
