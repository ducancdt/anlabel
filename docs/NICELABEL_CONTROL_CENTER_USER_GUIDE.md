# NiceLabel Control Center 2019 — User Guide (Markdown)

**Source PDF:** `ug-NiceLabel_Control_Center-en.pdf` (On-premise Edition, Rev-2020-11) — typically at repo root or `H:\00_REPOS_PROJECTS\ANLABEL\`  
**Conversion:** Full text extract + UI page renders for research / Figma recreation  
**Asset folder:** `docs/assets/nicelabel-control-center/ui-screens/` (155 files: full-page renders + embedded UI crops)  
**Figma file:** [NiceLabel Control Center shells](https://www.figma.com/design/asnGsLMxceJWb3HlfaE3q4)

> This document is a research conversion of the official NiceLabel Control Center user guide for ANLAbel competitive UX study. Screenshots are rasterized from the PDF (not live product).

---

## How to use assets

| Asset pattern | Meaning |
| --- | --- |
| `assets/nicelabel-control-center/ui-screens/page-NNN.png` | Full PDF page render (UI-heavy pages prioritized) |
| `assets/nicelabel-control-center/ui-screens/embed-pNNN-*.png` | Large embedded images from that page |

---

## Table of contents (product map)

1. [Introduction](#1-introduction)
2. [Activation](#2-activation)
3. [Configuration and Administration](#3-configuration-and-administration)
4. [Using Control Center](#4-using-control-center)
   - Overview / Documents / Applications / Printers / History / Analytics
5. [Technical Support](#5-technical-support)
6. [Contact](#6-contact)

### UI surface map (for Figma)

| Module | Key UI | Sample screens |
| --- | --- | --- |
| Shell / Overview | Web app header + left nav + dashboard | page-054, page-055 |
| Documents | Document Storage browser, preview, revision, workflow | page-055–058, page-079–090 |
| Applications | Web Applications, Automation Manager | page-103–118 |
| Printers | Print Management, View by Printers, Custom Groups | page-119–126 |
| History | Filters, activity details, reprint, errors | page-127–135 |
| Analytics | Charts / workload optimization | page-136–146 |
| Administration | Auth, roles, users, groups, licenses, alerts | page-024–053, page-008–017 |

---

## 1. Introduction

NiceLabel LMS is the enterprise solution for client/server based label printing and centralized systems management. Control Center is an online application used to ensure label and brand consistency and to remotely monitor the label printing process in production.

### Main management features

- **Flexible licensing** — clients get licenses from Control Center
- **Central event logging** — all clients log printing activities
- **Printer monitoring** — printer statuses continuously reported
- **Managing print jobs** — pause, restart, priority, move to secondary printers
- **Proactive alerting** — software/printer problems
- **Web management** — concurrent browser access
- **Authentication** — permissions per user role
- **Revision control (versioning)** — track file changes
- **Workflows** — approval lifecycle on documents
- **Automation management** — remote start/stop triggers; events to History

### System requirements

See: https://www.nicelabel.com/products/specifications/system-requirements

![Title](assets/nicelabel-control-center/ui-screens/page-001.png)

---

## 2. Activation

Control Center acts as a **licensing server**. Clients sharing the same license key report events to the same Control Center.

**License model:** multi-user products license by **printer seats** (each unique printer counts as one seat). View seats under **Administration → Licenses**.

### 2.1 Managing licenses

| Action | Path |
| --- | --- |
| Activate new product | Administration → Licenses → Activate new product |
| Upgrade edition / printer count | Licenses → product → Upgrade |
| Version upgrade 2017→2019 | Install latest; Administration → Licenses → Automatic Activation |
| Clean-install upgrade | Activate with upgrade key + old license key |
| Add licenses | Upgrade → Add licenses |
| Offline activation | Activation Web Page → Activation Code → Finish |
| Deactivate | Administration → Licenses → Deactivate product |

### 2.2 Activating clients

Same LMS Enterprise/Pro key as Control Center. Incomplete activation → 30-day trial without Control Center connection.

### 2.3–2.4 Account & printer licensing mode

See PDF pages 17+ for account activation and printer licensing details.

![Licenses UI](assets/nicelabel-control-center/ui-screens/page-008.png)

---

## 3. Configuration and Administration

### 3.1 Configuration

- **UI language** — browser language settings (IE/Edge, Chrome, Firefox procedures in PDF)
- **Multitier landscape** — file synchronization between landscapes; identify document storage per tier

### 3.2 Administration

#### Authentication & privileges

- Configure authentication method
- Manage user privileges; multiple role membership
- Set up **access roles** and **role permissions**

#### Application users & groups

- Add application users; assign to groups; assign roles
- Share web applications with users/groups
- Password settings for application users
- Application groups management

#### Versioning and workflows

- Enable versioning on document storage
- Apply workflows (approval processes)

#### Database replacements & global variables

- Database replacement configuration
- Global variables + properties

#### Alerts

- E-mail (SMTP), Gmail-like providers
- RSS 2.0 feed
- SMS via Clickatell

#### Application server, synchronization, history log cleanup

- Application server options
- Synchronization rules, enabling sync, logging activity
- History log cleanup: archive method, schedule, Access DB archiving, recovery

![Admin roles](assets/nicelabel-control-center/ui-screens/page-025.png)
![Users](assets/nicelabel-control-center/ui-screens/page-031.png)
![Workflows admin](assets/nicelabel-control-center/ui-screens/page-040.png)

---

## 4. Using Control Center

### 4.1 Opening Control Center

Typical URL: http://server/epm (server = install host)

### 4.2 Overview

Dashboard / overview of system status and navigation into modules.

![Overview](assets/nicelabel-control-center/ui-screens/page-054.png)
![Overview / home](assets/nicelabel-control-center/ui-screens/page-055.png)

### 4.3 Documents

#### Document Storage

Central repository for label files and related assets.

**Capabilities:**

- Browse / work with document storage
- Search files and label data
- **Preview** label files
- Move files; file properties; access files; open directly
- Custom fonts

![Document Storage](assets/nicelabel-control-center/ui-screens/page-055.png)
![Search / storage](assets/nicelabel-control-center/ui-screens/page-056.png)
![Preview labels](assets/nicelabel-control-center/ui-screens/page-058.png)

#### File access control

- Access control rules
- Workflow-related access rules
- Folder permissions; specific permission options
- Removing published files

#### Label report

- Execute label report
- Report contents

#### Comparing label files

- Compare different files
- Compare revisions of same file

#### Revision control system

- Add files to storage
- Check out / check in (single and multiple)
- File revision history; work with revisions
- Request label revisions
- Restore deleted files

![Revisions](assets/nicelabel-control-center/ui-screens/page-079.png)
![Check out](assets/nicelabel-control-center/ui-screens/page-081.png)

#### Workflows

Approval processes:

1. Label Production Approval Process
2. Two-step Label Production Approval Process
3. Label Production Approval Process with Delayed Publishing

Also: enable workflows, assign steps to files, delayed publishing, limit approvers to folders, **custom workflows**.

![Workflow](assets/nicelabel-control-center/ui-screens/page-087.png)
![Workflow steps](assets/nicelabel-control-center/ui-screens/page-090.png)

#### Centralized Application Server

Application server options in Document Storage; technical background.

#### Browser extension

Browser integration for Control Center workflows.

### 4.4 Applications

#### Web Applications

- Create / share / configure web applications
- Restrict logins and printers
- Record printing actions
- Per-user/group settings
- Access shared web apps
- File database connections in web apps

![Web apps](assets/nicelabel-control-center/ui-screens/page-103.png)
![Share apps](assets/nicelabel-control-center/ui-screens/page-105.png)

#### Cloud integrations

Set up integrations.

#### Automation Manager

- Understanding Automation Manager in Control Center
- Trigger management permissions
- Access all triggers; start/stop triggers
- Add / reload / remove configurations
- Filter Automation logs

![Automation](assets/nicelabel-control-center/ui-screens/page-112.png)
![Triggers](assets/nicelabel-control-center/ui-screens/page-114.png)

### 4.5 Printers

#### Print Management

- Select displayed items
- Command buttons
- Connected printers and workstations
- Search and filtering
- Bottom row status

![Print management](assets/nicelabel-control-center/ui-screens/page-119.png)
![Printers list](assets/nicelabel-control-center/ui-screens/page-120.png)

#### View by Printers

- Licensed printers
- Viewing printers

#### Custom Printer Groups

- Add new group
- Existing groups

#### Printers tab access rights

Role-based access to Printers module.

![Printer groups](assets/nicelabel-control-center/ui-screens/page-125.png)

### 4.6 History

#### History views

- Data filtering
- Printing activities
- Activity details / additional details
- Reprint
- Errors
- System events
- Alerts
- All activities

#### Job statuses

- Print job statuses
- Label job statuses

#### Reprinting labels

- Prerequisites for reprint

![History](assets/nicelabel-control-center/ui-screens/page-127.png)
![Activity details](assets/nicelabel-control-center/ui-screens/page-130.png)

### 4.7 Analytics

- Introduce Analytics
- Optimize label usage
- Optimize printer / printer group / user / computer workloads
- Multiple filters
- Optimize printing processes

![Analytics](assets/nicelabel-control-center/ui-screens/page-136.png)
![Analytics charts](assets/nicelabel-control-center/ui-screens/page-137.png)
![Workload](assets/nicelabel-control-center/ui-screens/page-141.png)

---

## 5. Technical Support

- Problem solving; possible problems; resolution steps
- Online support resources

![Support](assets/nicelabel-control-center/ui-screens/page-148.png)

---

## 6. Contact

See PDF page 152 for NiceLabel contact information.

---

## ANLAbel mapping notes (research)

Large phased product plans from this research are recorded in [`MASTER_PLAN.md` — Control Center / LMS operations](../MASTER_PLAN.md#control-center--lms-operations--large-improvement-plans-2026-08-12).

| Control Center module | ANLAbel today | Figma / product follow-up |
| --- | --- | --- |
| Overview dashboard | Print Center / job state partial | Operator dashboard frame |
| Document Storage + versioning | ProjectRevisionService / template revisions | Document library + revision UI |
| Workflows / approval | Not first-class | Approval workflow board (future) |
| Print Management | Print preview + spool monitoring | Print queue console |
| History + reprint | PrintOperationLog + recovery | History / reprint screen |
| Analytics | Not shipped | Analytics charts (future) |
| Admin roles / users | License activation only | Admin shell (future) |
| Automation Manager | Not shipped | Automation triggers panel (future) |

---

## Full page-by-page text

The complete PDF text dump (all 152 pages) is preserved in:

- [assets/nicelabel-control-center/_raw_extract.md](assets/nicelabel-control-center/_raw_extract.md)

Use that file when a section needs verbatim procedure text.

---

## Figma deliverable checklist

**File:** https://www.figma.com/design/asnGsLMxceJWb3HlfaE3q4  
Page: `NiceLabel Control Center` · desktop web frames **1280×800**

| # | Frame | Status | PDF refs |
| --- | --- | --- | --- |
| 1 | **CC / Overview** | Done | page-054, page-055 |
| 2 | **CC / Documents — Storage** | Done | page-055–058 |
| 3 | **CC / Documents — Workflow** | Done | page-087–090 |
| 4 | **CC / Applications — Web Apps** | Done | page-103–114 |
| 5 | **CC / Printers — Print Management** | Done | page-119–125 |
| 6 | **CC / History** | Done | page-127–130 |
| 7 | **CC / Analytics** | Done | page-136–141 |
| 8 | **CC / Administration** | Done | page-024–031 |
| — | **REF / PDF …** | Reference bitmaps under shells | p54, p55, p120, p137 |

Shell pattern matched from PDF: green top brand bar · horizontal primary nav · left module sidebar · content pane · NiceLabel green accent.

