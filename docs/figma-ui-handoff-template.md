# ANLAbel mandatory Figma to WPF handoff

Use this artifact for every user-visible UI change. The design must be created
or updated with `@figma` before WPF implementation. Prose, competitor screenshots
and old research frames are not sufficient authority.

Pure domain, parser, persistence, compiler and test-infrastructure changes do
not require Figma.

## 1. Slice identity

| Field | Required value |
| --- | --- |
| Local label outcome | One concrete design/data/preview/print/recovery task |
| In scope | One vertical UI workflow |
| Out of scope | Adjacent local features and all online capabilities |
| Related active plan | Path + section in `reinvention/07-execution-plan.md` |
| Implementation date | `YYYY-MM-DD` |

## 2. Exact Figma authority

| Field | Required value |
| --- | --- |
| Figma file URL | Exact file, no secret-bearing URL |
| Node ID | Stable frame/component node ID |
| Node name | Human-readable frame/component name |
| Created or updated with `@figma` | `Yes` |
| Existing component/tokens reused | Component IDs and variable/style names |

If no suitable node exists, create the smallest state-complete frame in an
existing ANLAbel Figma file. A new file is used only when the existing design
system cannot own the slice.

## 3. Required states

Record every applicable state; use `N/A` only with a software reason.

| State | Figma node/variant | Runtime source | Primary action |
| --- | --- | --- | --- |
| Empty | | | |
| Loading/busy | | | |
| Ready/success | | | |
| Stale | | | |
| Blocked/error | | | |
| Disabled/read-only | | | |
| Large/long data | | | |

No sample row, count, printer, user or status from research becomes runtime data.

## 4. Layout contract

| Requirement | Decision/evidence |
| --- | --- |
| 1024 x 600 effective fit | |
| 1280 x 720 and 1920 x 1080 behavior | |
| 100/125/150/200% scale intent | |
| One explicit scroll owner | |
| Long text (+40%) wrapping/truncation | |
| Empty and maximum-data density | |
| Keyboard focus order | |
| Visible focus and high contrast | |

Fixed sizes are permitted only when the Figma node demonstrates fit at the
smallest target. Dynamic forms use one intentional scroll owner.

## 5. Figma to WPF mapping

| Figma node/component | WPF control/resource | AutomationId | Accessible name | Data/state source |
| --- | --- | --- | --- | --- |
| | | | | |

- Use existing WPF theme resources before hardcoded colors.
- Use the repository icon system; do not use emoji or Unicode glyphs as icons.
- Keep one action owner; links to History/Print Center do not duplicate dispatch.
- UI cannot infer success, data, queue state or completion without source evidence.

## 6. Runtime evidence

| Automated gate | Result/artifact |
| --- | --- |
| XAML compiles with zero warnings | |
| Stable AutomationIds present | |
| State transitions covered | |
| 1024 x 600 layout contract covered | |
| Keyboard/focus contract covered | |
| Long/empty/error data covered | |
| Relevant domain regressions pass | |
| Fast quality loop passes | |

Screenshots may document the result but do not replace assertions. User review,
external pending activities are not fields in this handoff.

## 7. Completion rule

A UI slice is complete only when the exact Figma node, WPF mapping, automated
state/layout evidence, version projection and active plan agree. Missing Figma
authority blocks UI implementation; it does not block unrelated domain work.
