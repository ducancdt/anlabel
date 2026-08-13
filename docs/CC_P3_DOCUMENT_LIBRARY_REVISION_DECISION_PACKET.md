# CC-P3 document library and revision owner decision packet

**Status:** documentation-only owner gate; no document browser, workflow enum, check-out lock, ACL, new Figma frame or Text/TextBox change is authorized by this packet (2026-08-13)
**Program index:** [`CC_UI_UX_PROGRAM_INDEX.md`](CC_UI_UX_PROGRAM_INDEX.md)
**Handoff:** [`CC_P3_DOCUMENT_LIBRARY_REVISION_UI_HANDOFF.md`](CC_P3_DOCUMENT_LIBRARY_REVISION_UI_HANDOFF.md)
**Specification:** [`CC_P3_DOCUMENT_LIBRARY_REVISION_UI_SPEC.md`](CC_P3_DOCUMENT_LIBRARY_REVISION_UI_SPEC.md)
**Host packet:** [`CC_P1_P2_P5_HOST_DECISION_PACKET.md`](CC_P1_P2_P5_HOST_DECISION_PACKET.md)
**Predecessor:** [`CC_P5_HISTORY_REPRINT_UI_DECISION_PACKET.md`](CC_P5_HISTORY_REPRINT_UI_DECISION_PACKET.md)
**Successor:** [`CC_P4_APPROVAL_WORKFLOW_DECISION_PACKET.md`](CC_P4_APPROVAL_WORKFLOW_DECISION_PACKET.md)

## Purpose and decision boundary

CC-P3 is the local filesystem-first document-library and revision-access slice after the P5 history/reprint read model. It must make a selected local document, its validation state and its managed revisions understandable without pretending that ANLAbel has NiceLabel's server storage, folder ACLs, check-out, approval workflow or multi-user identity.

```text
configured local root + Built-in catalog
        -> deterministic file enumeration and validation
        -> selected-file metadata / preview / open
        -> one ProjectRevisionService owner
        -> compare valid revisions
        -> explicit dirty-edit confirmation + atomic restore
        -> post-commit reload and audit evidence
```

The packet does not authorize a second revision/archive stack, silent materialization, workflow controls, remote sync, server retention or changes to protected Text/TextBox behavior.

**Current dependency note:** the documentation-only P5 History/read-model owner boundary is now
explicit: History remains read-only and controlled reprint returns to Print Center. CC-P3 is the
next local filesystem/revision gate; after its D1-D8 decisions, CC-P4 owns document workflow,
actor/audit and policy-on print questions. This packet does not promote either downstream slice to
runtime implementation.

## Decision summary

| Decision | Evidence-backed recommendation | Owner choice required |
| --- | --- | --- |
| D1. Local root | Start with one explicitly configured local root per workstation, with a visible missing/unconfigured state. Keep project-relative/multiple-root semantics out of M1 until a product owner supplies migration and precedence rules. | Approve root cardinality, missing-root behavior, creation permission and path-redaction policy. |
| D2. Source split/order | Keep `Built-in` embedded templates separate from local `.anlabel` files. Enumerate supported local files deterministically by relative path; never give an embedded materialized copy a fake filesystem/revision identity. | Approve source tabs/filters and ordering/search semantics. |
| D3. Host/action owner | Prefer extending the existing `TemplateLibraryWindow`/entry point for browse and opening the existing `TemplateRevisionWindow`/`ProjectRevisionService` for revisions, unless the shared host packet selects a dedicated local document window. | Choose host, deep-link path and one owner for Open/Preview/Revision History. |
| D4. Validation/open/preview | A file card exposes path-relative-to-root, metadata, load status and thumbnail status. Open/Preview uses the existing validated project-load path; invalid/unsupported files remain diagnosable and cannot be materialized or opened as valid. | Approve whether invalid files stay in results with a disabled action or are filtered with a reason. |
| D5. Revision lineage/retention | Treat primary, managed `.bak` and bounded local `.revisions` as distinct kinds. Recommend the existing fixed retention count (`8`) until a configurable policy is approved; show validity, hash prefix, timestamp, diagnostic and audit reason. | Approve retention count/configurability and whether archive inspection is enabled for every local file in M1. |
| D6. Compare/restore | Compare only validated inputs. Restore only managed backup/local archive after explicit confirmation naming source and unsaved-edit risk; preserve the current primary, commit atomically, then reload and report resulting lineage. | Approve restore source kinds, confirmation wording and recovery behavior after commit. |
| D7. Workflow boundary | Keep `Approved`, `Staging`, `Production`, workflow actions, check-out, ACL and user/actor fields as CC-P4 research vocabulary until a durable policy/store exists. | Approve the P4 dependency and whether any informational state may be shown without actions. |
| D8. Runtime/Figma/data safety | Require target-scale browse/open/revision/restore evidence, stable AutomationIds, path/payload redaction and regression gates. Reuse Figma `3:2` only for density; request a state-specific node only if a concrete missing state needs design review. | Name product, WPF host, project/revision, UI Automation and QA owners. |

## Source evidence and implications

| Evidence | What it proves | What it does not prove |
| --- | --- | --- |
| [`TemplateLibraryWindow.xaml`](../src/ANLAbel.App/TemplateLibraryWindow.xaml#L1-L75) and [`TemplateLibraryWindow.xaml.cs`](../src/ANLAbel.App/TemplateLibraryWindow.xaml.cs#L28-L43) | Current WPF has a gallery/filter/selection surface for embedded resources and materializes a fresh editable copy through the existing service. | Embedded resources are not a local folder, path, revision lineage or multi-user document library. |
| [`TemplateLibraryService.cs`](../src/ANLAbel.App/TemplateLibrary/TemplateLibraryService.cs#L23-L86) | Manifest resources are enumerated in deterministic name order, malformed entries can be skipped, and valid resources are deserialized/materialized through one boundary. | It does not enumerate a configured local root or define root precedence. |
| [`TemplateRevisionWindow.xaml`](../src/ANLAbel.App/TemplateRevisionWindow.xaml#L1-L55) and [`TemplateRevisionWindow.xaml.cs`](../src/ANLAbel.App/TemplateRevisionWindow.xaml.cs#L29-L71) | Existing revision UI is read-only inspection with Refresh, Compare/status and guarded Restore; confirmation warns about unsaved edits and reload follows a successful commit. | It is not a library browser and does not provide workflow/check-out/ACL semantics. |
| [`ProjectRevisionService.cs`](../src/ANLAbel.Project/SaveLoad/ProjectRevisionService.cs#L89-L165) | Primary, managed backup and local archive entries expose validity/diagnostics and audit evidence. | “File exists” is not safe; no server, user identity or policy-on workflow is present. |
| [`ProjectRevisionService.cs`](../src/ANLAbel.Project/SaveLoad/ProjectRevisionService.cs#L167-L218) | Compare is semantic and returns an explicit unavailable result when inputs are invalid; differences cover document/layout/data evidence. | A diff is not a workflow approval or print authorization. |
| [`ProjectRevisionService.cs`](../src/ANLAbel.Project/SaveLoad/ProjectRevisionService.cs#L221-L330) | Restore validates source bytes, restricts allowed paths, archives the previous primary and commits/reloads through a guarded boundary. | Restore does not establish a Published state or multi-user audit. |
| [`ProjectRevisionArchive.cs`](../src/ANLAbel.Project/SaveLoad/ProjectRevisionArchive.cs#L13-L24) and [`ProjectRevisionArchive.cs`](../src/ANLAbel.Project/SaveLoad/ProjectRevisionArchive.cs#L195-L224) | Local `.revisions` snapshots are bounded by the explicit default retention count (`8`) and cleanup is restricted to the derived archive directory. | Retention is local policy, not server retention or immutable governance. |
| [`ProjectRevisionArchive.cs`](../src/ANLAbel.Project/SaveLoad/ProjectRevisionArchive.cs#L37-L193) | Archive bytes are durably written, hash-addressed, audited in JSONL and a torn final audit line does not hide valid earlier events. | Local JSONL is not authenticated multi-user audit or immutable server history. |
| [`ProjectFileService.cs`](../src/ANLAbel.Project/SaveLoad/ProjectFileService.cs) and project-load contracts | Existing load/validation is the source of truth for supported schema and safe materialization. | A thumbnail or card label cannot bypass load validation. |
| [`MainWindow.xaml.cs`](../src/ANLAbel.App/MainWindow.xaml.cs#L701-L716) and [`MainWindow.xaml.cs`](../src/ANLAbel.App/MainWindow.xaml.cs#L788-L801) | Current entry points expose Revision History only for a saved current path and open the embedded Template Library through a separate materialization flow. | Current reachability does not prove that every local-library file can open the same revision owner. |
| Read-only Control Center Documents [`asnGsLMxceJWb3HlfaE3q4`](https://www.figma.com/design/asnGsLMxceJWb3HlfaE3q4), node `3:2` | Metadata gives `1280 × 800`, toolbar `3:16` (`1248 × 40`), folder rail `3:19` (`240 × 620`), file pane `3:29` (`980 × 620`) and sample cards `3:31`–`3:84`. | No selected detail, invalid-file, diff, restore, dirty-edit, local-root or real repository state exists in the node. |
| Read-only Workflow node `7:2` in the same file | Candidate Draft/Approved/Published vocabulary and action/history density for later CC-P4 review. | It does not authorize a workflow enum, actor identity, ACL, check-out or print gate. |

## Recommended ownership model

| Layer | Single authority | UI responsibility |
| --- | --- | --- |
| Source/catalog | `TemplateLibraryService` for Built-in; one future local-root enumerator for files | Show source kind and deterministic scope; never merge identities or silently materialize. |
| Load/validation | Existing project file service/load result | Gate Open/Preview/Use and show invalid/unsupported diagnostics. |
| Thumbnail | Existing `LibraryThumbnailRenderer`/preview path | Report thumbnail failure separately; never call a thumbnail print-valid. |
| Revision lineage | `ProjectRevisionService` + `ProjectRevisionArchive` | Show primary/backup/archive evidence, compare and guarded restore through one owner. |
| Dirty-edit/restore | Existing MainWindow/TemplateRevisionWindow confirmation and reload path | Preserve unsaved-edit warning and post-commit state; no silent overwrite. |
| Workflow/permissions | Future CC-P4 policy/store | Keep research labels deferred or informational only; no actions before policy. |
| Figma | Read-only Control Center metadata | Borrow browse density/scroll hierarchy only; missing states require WPF/runtime evidence. |

## State matrix for owner approval

| State | Required visible facts | Safe action | Fail-closed rule |
| --- | --- | --- | --- |
| Root unconfigured/missing | Exact redacted root state and reason | Choose/configure or cancel | Do not show an empty root as a healthy library. |
| Loading/enumerating | Root, folder/search scope and busy/cancel state | Wait/cancel/refresh | Do not open stale selection as current. |
| Empty/no match | Root/source, folder/filter and count | Clear search or choose another folder/source | No phantom cards or fallback to Built-in without saying so. |
| Built-in selected | Embedded source/name/group/type and materialization notice | Use/open a fresh editable copy | Do not claim local path or revision lineage. |
| Valid local selected | Relative path, size, timestamp, template/label metadata, load/thumbnail status | Open, Preview, Revision History | Open only through validated load. |
| Invalid/unsupported local file | Path-relative identifier, diagnostic and schema status | Inspect/refresh/repair externally | Disable Open/Use/Restore; do not materialize invalid bytes. |
| Dirty current document | Explicit dirty marker and selected file identity | Save, discard with confirmation or cancel | Revision restore cannot silently discard edits. |
| Valid primary/backup/archive | Kind, path redaction, timestamp, size, hash prefix, reason | Compare or restore allowed source | Existence alone never enables restore. |
| Invalid/missing revision | Diagnostic and source kind | Refresh or remove stale row | Restore/compare disabled. |
| Diff unavailable | Which input failed validation/compatibility | Refresh/revalidate | Never report semantic equality/difference from partial data. |
| Restore confirmation | Source revision, unsaved-edit warning, retention/backup behavior | Confirm or Cancel | No write before explicit confirmation. |
| Restore committed | New primary, retained previous primary/backup/archive and reload result | Open/review audit | Do not report cancellation after atomic commit succeeds. |
| Workflow/check-out vocabulary | Deferred badge or no control with explanation | Follow CC-P4 owner decision | No approval, lock, ACL or Published print gate from Figma labels. |

## Proposed host-neutral layout

```text
Documents
  Source              [Built-in | Local root: C:\Labels\Production]
  Folder              [Root / subfolder]     Search [____________]
  Files               [valid card/list] [invalid row with reason]
  Selected file       name · relative path · size · modified · load/thumbnail status
  Actions             [Open] [Preview] [Revision History] [Refresh]

Revision History (same owner)
  Primary / Backup / Archive · validity · timestamp · hash prefix · reason
  [Compare] [Restore selected]
  Confirmation: restore <source>; unsaved edits will be discarded; current primary is retained.
```

At `1024 × 600`, file scope/search and selected metadata must stack with one intentional scroll owner; path/status/revision diagnostics may wrap but cannot be replaced by generic `Ready`. Long paths use a redacted relative display with accessible full path where policy permits.

## Fixture and regression packet

The following are proposed fixture names and expected outcomes, not tests added by this documentation-only change.

| Fixture | Expected result | Required evidence |
| --- | --- | --- |
| Missing/unconfigured root | Explicit configuration state | No healthy-empty library; no destructive directory creation without consent. |
| Deterministic local enumeration | Same relative-path order across runs | Built-in source remains separate; unsupported extensions are handled explicitly. |
| Valid local `.anlabel` | Metadata/card/load/preview path succeeds | Existing project-load and thumbnail owner are reused. |
| Invalid JSON/unsupported schema | Diagnostic row and disabled actions | No materialization, open, preview or restore. |
| Built-in embedded template | Fresh editable copy with Built-in identity | No fake path/revision lineage; no overwrite of local file. |
| Dirty document + restore request | Confirmation appears and cancel changes nothing | Unsaved edits are not silently discarded. |
| Valid backup/archive restore | Atomic commit, previous primary retained, reload succeeds | Audit event and resulting primary/backup/archive facts visible. |
| Invalid/missing revision | Compare/restore blocked with source diagnostic | No `exists = safe` shortcut. |
| Semantic diff with changed objects/data | Field-level differences and hashes shown | No claim when either side fails validation. |
| Torn final audit JSONL line | Earlier valid audit entries remain visible | Local tolerance is stated; no tamper-proof claim. |
| Retention boundary | Existing default `8` archives retained unless policy changes | Trim is bounded to the file's `.revisions` directory. |
| Figma sample card | Used only for density | Sample `.nlbl` names/content never become runtime fixtures. |
| Workflow research label | No action/print gate | CC-P4 policy/store remains the owner. |

## No-go list

- Do not present embedded templates as local files or silently materialize them over a selected path.
- Do not enumerate or create roots without explicit owner policy; do not hide missing roots as empty healthy folders.
- Do not create a second revision/archive/restore implementation or bypass validated project load.
- Do not restore invalid/unsupported bytes, discard dirty edits without confirmation, or report a successful commit as canceled.
- Do not infer workflow, check-out, ACL, actor identity, approval or Published print policy from Figma labels.
- Do not treat local JSONL audit, timestamps or hashes as authenticated multi-user governance.
- Do not add a browser/server/sync/multi-tenant claim, or use a Figma sample file as product data.
- Do not change Text/TextBox ownership, sizing, wrapping, clipping, padding, resize lifecycle or print parity.

## Owner sign-off record

Record one owner, date and decision for each row. Blank rows keep CC-P3 open.

| Decision | Owner | Date | Approved option / notes |
| --- | --- | --- | --- |
| D1. Root cardinality/missing behavior | `TBD` | `TBD` | `TBD` |
| D2. Built-in/local source split and ordering | `TBD` | `TBD` | `TBD` |
| D3. Host and Open/Preview/Revision ownership | `TBD` | `TBD` | `TBD` |
| D4. Validation/thumbnail/metadata state | `TBD` | `TBD` | `TBD` |
| D5. Revision kinds/retention | `TBD` | `TBD` | `TBD` |
| D6. Compare/restore/dirty-edit policy | `TBD` | `TBD` | `TBD` |
| D7. CC-P4 workflow/check-out boundary | `TBD` | `TBD` | `TBD` |
| D8. Runtime, AutomationIds, Figma and regression owners | `TBD` | `TBD` | `TBD` |

**Closure rule:** CC-P3 may move from design review to implementation only after D1–D8 are filled, the shared host choice is recorded, local-root and revision policies are named, and browse/open/diff/restore fixtures are converted into runtime and regression gates. Until then, CC-P3 remains a local-library/revision plan and not a shipped Control Center browser.
