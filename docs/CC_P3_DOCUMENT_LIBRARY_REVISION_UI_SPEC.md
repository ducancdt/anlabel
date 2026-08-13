# ANLAbel — CC-P3 Document Library + Revision UI/UX spec

**Status:** design-only local browse/revision spec; host and workflow policy remain open (2026-08-13)
**Host decision:** [`CC_P1_P2_P5_HOST_DECISION_PACKET.md`](CC_P1_P2_P5_HOST_DECISION_PACKET.md)
**Handoff:** [`CC_P3_DOCUMENT_LIBRARY_REVISION_UI_HANDOFF.md`](CC_P3_DOCUMENT_LIBRARY_REVISION_UI_HANDOFF.md)
**Program route:** [`CC_UI_UX_PROGRAM_INDEX.md`](CC_UI_UX_PROGRAM_INDEX.md)
**Owner decision packet:** [`CC_P3_DOCUMENT_LIBRARY_REVISION_DECISION_PACKET.md`](CC_P3_DOCUMENT_LIBRARY_REVISION_DECISION_PACKET.md)
**Figma reference:** [NiceLabel Control Center research file](https://www.figma.com/design/asnGsLMxceJWb3HlfaE3q4), Page `0:1`, Documents `3:2`

This spec maps the Figma Documents / Storage shell to a local filesystem-first library, the existing embedded-template catalog and the existing revision/restore services. It does not create a browser, add check-out/ACL/workflow controls, or change protected Text/TextBox behavior.

## 1. Operator outcome

The first document surface should let an operator:

1. distinguish Built-in embedded templates from files in an explicitly configured local root;
2. browse folders, search supported files and see deterministic empty/invalid/loading states;
3. inspect selected-file path, metadata, validation and thumbnail status before opening it;
4. reach one revision owner for primary, managed backup and bounded local archives;
5. compare valid revisions and restore only after an explicit unsaved-edit confirmation;
6. understand that workflow, check-out, user identity and permissions are deferred rather than implied by Figma labels.

## 2. Figma node map (read-only)

Metadata for `3:2` was rechecked read-only on 2026-08-13. These bounds are visual guidance, not a WPF contract or evidence that the research folders exist locally.

| Figma node | Metadata name / bounds | ANLAbel role | Boundary |
| --- | --- | --- | --- |
| `3:2` | `CC / Documents — Storage`, `1280 x 800` | Browse/revision density reference | No web storage, server retention or workflow capability claim. |
| `3:3` | Top chrome, `1280 x 48` | Optional host chrome | No sign-out/help identity without a local owner. |
| `3:6` | Primary navigation, `1280 x 40` | Local module navigation reference | Show only modules that exist in the selected host. |
| `3:16` | Toolbar, `(16,104)`, `1248 x 40` | Browse/search/preview action region | Research labels Edit, Workflow and Deleted Items remain deferred until owners exist. |
| `3:19` | Folder rail, `(16,156)`, `240 x 620` | Configured-root folder/filter owner | Root, Approved, Staging and similar labels are not seeded folders unless local evidence defines them. |
| `3:29` | File pane, `(272,156)`, `980 x 620` | Card/list and selected-file region | One intentional scroll owner; sample cards are not repository files. |
| `3:31`–`3:84` | `100 x 110` sample cards, `label_1.nlbl` … `label_18.nlbl` | Thumbnail/card density reference | Never copy sample names or contents into runtime fixtures. |

The storage frame has no concrete selected-file, invalid-file, diff, restore or unsaved-edit state. No new Figma node is required for M1/M2; if a missing state needs design evidence, follow the [Figma escalation protocol](figma-ui-handoff-template.md#figma-escalation-protocol) and request the smallest state-specific reference.

## 3. Source-to-item contract

| Item field | Current source / owner | Display rule |
| --- | --- | --- |
| `SourceKind` | `TemplateLibraryService.Items` for embedded resources; future explicit local-root enumeration | Show `Built-in` and `Local file` as separate source kinds. Embedded materialization is not a filesystem revision. |
| `RootStatus` | Future configured local-root policy | Missing/unconfigured/permission-failed roots are explicit states; do not show an empty list as a healthy root. |
| `RelativePath` / `FileName` | Local enumeration under the chosen root | Show relative path as the primary identity; keep absolute path available for diagnostics without leaking it into sample data. |
| `FileSize` / `LastWrite` | Filesystem metadata | Show local timestamp basis and refresh time; stale metadata is not validation. |
| `LoadStatus` / `Diagnostic` | Existing project load/validation boundary | Valid, invalid, unsupported and read-failed remain distinct. Invalid bytes cannot be opened, used or restored. |
| `TemplateName` / dimensions / DPI | Validated `LabelTemplate` from `ProjectFileService` | Show only after validation; do not infer dimensions from a thumbnail. |
| `Thumbnail` / `ThumbnailStatus` | `LibraryThumbnailRenderer` | Thumbnail failure is separate from document-load failure and never proves print validity. |
| `RevisionKind` | `ProjectRevisionService`: `Primary`, `Backup`, `Archive` | Keep managed backup and `.revisions` archive distinct; existence is not safety. |
| `RevisionValidity` / hash / diagnostic | `ProjectRevisionEntry` inspection | Display status, size, timestamp, template name, hash prefix and reason; invalid/unsupported entries are not restorable. |
| `SemanticDiff` | `ProjectRevisionService.CompareAsync` / `ProjectRevisionDiff` | Compare only validated primary/backup inputs; expose unavailable and changed fields instead of claiming equality. |
| `RevisionAudit` | `ProjectRevisionArchive.ReadAuditAsync` | Show event, UTC timestamp, archive path/hash and reason; a torn final JSONL line does not erase valid prior events. |
| `RestoreResult` | `ProjectRevisionService.RestoreRevisionAsync` | Allow only managed backup or local archive; preserve previous primary as backup/archive and reload only after atomic commit. |
| `WorkflowState` / `CheckOut` | No current local source | Hide or mark deferred. Figma Approved/Staging/Published labels are not product state. |

## 4. Host-neutral wireframe

Keep this content order whether the host extends `TemplateLibraryWindow`, adds a `DocumentLibraryWindow` or exposes a view from the P1 host:

```text
[Library context: Local root | Built-in | Refresh | root status / last scan]

[Browse | Search | Preview | Revision History]

[Folder rail]  [Card/list file results]
               [Selected file: relative path | validation | size/time | dimensions/DPI | thumbnail status]

[Revision panel: Primary | Backup | Archive | Compare | Audit]
[Restore selected revision -> confirmation -> atomic commit -> reload]
```

Selection, filtering and preview are read-only until the existing validated open path is invoked. A selected Built-in item materializes a fresh editable copy only through `TemplateLibraryService`; it must not be presented as a local path or revision lineage.

## 5. State and failure matrix

| State | Visible evidence | Safe next action | Fail-closed rule |
| --- | --- | --- | --- |
| `RootUnconfigured` | Root path status, Choose/Configure action | Choose or create a root only after explicit confirmation | Do not render a healthy empty library. |
| `RootUnavailable` | Path and permission/missing diagnostic | Choose another root or retry | Do not silently fall back to the process directory. |
| `Loading` | Busy state, scope/search and prior selection | Wait/cancel if supported | Do not open a stale selection as current. |
| `Empty` / `FilterNoMatch` | Root/folder/filter and count | Clear search or choose another scope | No phantom cards and no implied missing document. |
| `BuiltInSelected` | Built-in badge, template type/size and thumbnail | Materialize through the existing catalog owner | No filesystem identity, revision or workflow claim. |
| `LocalValidSelected` | Relative path, metadata, validation and thumbnail status | Open, Preview or Revision History | Open only through validated project load. |
| `InvalidOrUnsupported` | Card/diagnostic row with reason | Inspect or remove from view | Disable Open/Use/Restore; never materialize invalid bytes. |
| `ThumbnailFailed` | Validity and thumbnail error shown separately | Open validated file or retry thumbnail | Thumbnail failure is not document failure or print proof. |
| `UnsavedCurrent` | Dirty indicator and current file identity | Save, discard with confirmation or cancel | Restore cannot silently discard edits. |
| `RevisionUnavailable` | No primary/backup/archive or source diagnostic | Save/refresh/support path | Do not present a revision button as actionable. |
| `RevisionInvalid` | Kind, status, hash/diagnostic and disabled restore | Refresh or inspect | `Exists` never means safe. |
| `DiffUnavailable` | Which input failed validation/availability | Revalidate or inspect | Do not claim semantic equality/difference. |
| `RestoreConfirmation` | Source revision, unsaved-edit warning and retention behavior | Confirm or Cancel | No bytes are committed before confirmation. |
| `RestoreSucceeded` / `RestoreFailed` | New primary, retained previous primary, audit/error detail | Reload, retry or open audit | Atomic success must not be reported as canceled. |
| `WorkflowDeferred` | Disabled/hidden workflow/check-out copy with owner link | Wait for CC-P4 policy decision | No Draft/InReview/Approved/Published action or print gate from this surface. |

## 6. M1/M2 behavior and responsive layout

M1 is local browse/search/preview with explicit root and source status. M2 makes revision inspection reachable from every saved local file, keeps one revision owner, and preserves confirmation plus post-restore reload. Workflow, ACL, check-out and multi-user identity are outside this UI contract.

| Target | Layout behavior | Scroll/focus rule |
| --- | --- | --- |
| `1280 x 800` | May preserve the `240 DIP` folder rail and `980 DIP` file pane proportions as a reference; cards may follow the existing `216 DIP` gallery shell. | File pane owns card/list scrolling; metadata/revision detail remains reachable. |
| `1024 x 600` | Collapse the folder rail into a drawer/narrow scope region; prioritize search, file identity, validation and revision actions; detail stacks below or opens as a bounded pane. | Keyboard order: root status → search → folder scope → file results → metadata → revision actions; selection survives refresh when identity remains. |
| `100%`, `125%`, `150%` | Reflow or clip only inside declared owners; no page-level horizontal scroll and no blind Figma scaling. | Capture screenshot/UI Automation at every scale and record environment exceptions. |

## 7. Proposed automation vocabulary

Proposals only; final IDs require the host decision and runtime UI Automation evidence.

| Region/control | Proposed AutomationId | Accessible name |
| --- | --- | --- |
| Root | `CC.P3.Library.Root` | `Document library` |
| Root status/chooser | `CC.P3.Library.RootStatus` | `Local library root status` |
| Refresh | `CC.P3.Library.Refresh` | `Refresh document library` |
| Search | `CC.P3.Library.Search` | `Search documents` |
| Folder scope | `CC.P3.Library.FolderTree` | `Document folders` |
| File results | `CC.P3.Library.FileResults` | `Documents in selected folder` |
| Selected metadata | `CC.P3.Library.SelectedDetails` | `Selected document details` |
| Preview | `CC.P3.Library.Preview` | `Preview selected document` |
| Revision panel | `CC.P3.Library.Revisions` | `Document revisions` |
| Compare | `CC.P3.Library.Compare` | `Compare revisions` |
| Restore | `CC.P3.Library.Restore` | `Restore selected revision` |
| Restore confirmation | `CC.P3.Library.RestoreConfirm` | `Confirm revision restore` |
| Audit | `CC.P3.Library.Audit` | `Revision audit` |

## 8. Acceptance gate

Before implementation review closes P3:

- the host decision chooses whether to extend `TemplateLibraryWindow` or add a local library host, while `ProjectRevisionService` remains the only revision/restore authority;
- fixtures cover unconfigured/missing/permission-failed roots, empty folders, search no-match, Built-in items, valid local files, invalid/unsupported files and thumbnail-only failure;
- selected-file identity, validation, metadata, source kind and refresh basis remain explicit;
- primary/backup/archive validity, semantic diff, audit reason/timestamp and restore eligibility are visible; invalid revisions cannot be restored;
- restore requires confirmation, atomically preserves the previous primary, reloads only after commit and reports success/failure honestly;
- workflow, check-out, ACL, user/role and Figma sample folder labels do not become runtime capabilities;
- runtime screenshot/UI Automation covers `1024 x 600`, `100%`, `125%`, `150%`, keyboard/focus and scroll ownership;
- no Text/TextBox ownership, sizing, wrapping, clipping, padding, resize lifecycle, overflow or designer/print parity changes are introduced.

Until these gates close, this file is a UI/UX specification, not a shipped Document Library or workflow implementation.
