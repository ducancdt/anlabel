# UX System và Information Architecture

## 1. UX objective

Giao diện mới phải giảm ba loại lỗi:

- lỗi thiết kế: object nhảy, chọn nhầm, property khó tìm, canvas thiếu không gian;
- lỗi dữ liệu: bind sai, stale/missing field, không biết record nào đang preview;
- lỗi sản xuất: chọn sai profile/media/record/quantity, in trùng, không hiểu trạng thái job.

Thiết kế đẹp là hệ quả của hierarchy, state và workflow đúng; không phải thêm card, border hoặc icon.

## 2. Global information architecture

```text
Home
Design
Data
Print
Automate      (capability/edition có thể ẩn)
Library
Monitor
Settings/Admin
```

Không phải mọi mục đều là một app riêng. Đây là destinations trong một shell; backend service có thể tách process khi cần.

### Home

- recent/pinned documents;
- create from template;
- recent print jobs;
- printer health summary;
- onboarding/checklist;
- recovery/autosave drafts.

### Design

- document tabs;
- artboard/scene;
- insert/layers/components/data explorer;
- context inspector;
- problems/preflight.

### Data

- connections;
- schema/typed variables;
- transforms;
- sample records;
- mappings/lineage;
- validation results.

### Print

- published documents/jobs;
- record picker/prompts;
- printer/profile/media;
- preview/preflight;
- quantity/serial policy;
- job status/reprint.

### Automate

- pipeline list;
- visual/text pipeline editor;
- test payload;
- deploy/run state;
- execution logs/dead-letter.

### Library/Monitor/Admin

- document/resources/revisions/publish;
- print/data/automation events;
- printer profiles/calibration/compatibility;
- roles/settings/diagnostics.

## 3. Design workspace shell

```text
┌ Title/document tabs ───────── profile/media ─── Save · Preview · Print ┐
├ Context command bar: Insert / Arrange / Data / View / selection actions ┤
│ Left rail             │ Viewport                         │ Inspector     │
│ Insert                │                                  │ Layout        │
│ Layers                │      physical artboard           │ Appearance    │
│ Components            │      content + overlays          │ Data          │
│ Data                  │                                  │ Rules         │
├ Problems / Data Preview / History / Job Console (collapsible) ──────────┤
└ profile · media · DPI · record · zoom · selection · preflight status ───┘
```

### Top bar

Chỉ chứa document identity, unsaved state, profile context và primary actions. Không chứa mọi CRUD/layer/data command.

### Context command bar

Nhóm lệnh thay đổi theo mode/selection:

- no selection: insert, paste, document setup;
- text: font/size/alignment/overflow;
- shape: fill/stroke;
- barcode: symbology/data/quiet zone/module warning;
- multi-select: align/distribute/group;
- data mode: connection/map/test.

Lệnh thứ cấp vào overflow hoặc command palette.

### Left rail

Một panel, nhiều tab; không hiển thị toolbox, object tree, data source và binding issues đồng thời trong các card lồng nhau.

### Inspector

Một scroll owner, section theo ngữ cảnh, section advanced mặc định đóng. Primary physical fields X/Y/W/H luôn dễ thấy. Mixed value rõ cho multi-select.

### Bottom panel

Problems là tab mặc định khi có lỗi; Data Preview hiện record/table; History hiện command/revision; Job Console hiện print/automation. Panel có min/max và remember state theo workspace.

## 4. Responsive strategy

Ứng dụng là desktop công nghiệp; không cần giả vờ tối ưu phone. Tuy nhiên phải hoạt động ở effective pixels thấp do DPI scaling.

### Wide — ≥ 1440 px

- left panel 280–320;
- inspector 300–360;
- bottom panel optional;
- command labels + icons;
- viewport tối đa diện tích còn lại.

### Standard — 1180–1439 px

- left/inspector 240–300;
- icon + concise labels;
- bottom panel overlay hoặc thấp hơn;
- rail có thể collapse từng bên.

### Compact — 1024–1179 px

- chỉ một side panel mở tại một thời điểm;
- rail icons giữ cố định, panel overlay/drawer;
- inspector có property search;
- secondary command vào overflow;
- status bar rút gọn nhưng profile/DPI/preflight vẫn thấy;
- dialogs thành sheet/page có scroll owner rõ.

### Height states

- ≥ 760: normal density.
- 650–759: compact command/status, bottom panel auto-collapse.
- 600–649: single side panel, no fixed-height dynamic form, footer action sticky.

Không dùng “hide Properties vĩnh viễn” như lời giải responsive; phải có affordance mở nhanh và focus return.

## 5. Visual system

### Tokens

Centralize:

- neutral/background/surface/border/text/status colors;
- spacing 4/8/12/16/24/32;
- corner 4/6/8;
- typography roles;
- control heights/density;
- icon sizes 16/20/24;
- focus/error/warning/success states.

Dùng DynamicResource/theme dictionaries trong WPF. Light, dark và high contrast không hard-code riêng từng Window.

### Surfaces

- không “card quanh card”;
- border chỉ khi phân tách chức năng thật;
- typography + spacing tạo hierarchy;
- problems/status dùng color + icon + text, không dựa chỉ màu;
- badge nhỏ cho revision/binding/status, tránh pill trang trí tràn lan.

### Iconography

- một bộ vector/path hoặc high-quality asset có cùng stroke/weight;
- không dùng emoji/ký tự mojibake;
- icon-only luôn có tooltip và accessible name;
- active/disabled/error state vẫn đọc được high contrast.

### Motion

- subtle pane/selection transitions;
- disable/reduce motion theo system setting;
- không animate geometry của label khi state model không đổi;
- progress cho operation thật, không fake spinner vô hạn.

## 6. Core workflows

### 6.1 Create document

1. Chọn template hoặc blank.
2. Chọn media preset/custom dimensions.
3. Chọn printer profile optional; profile không âm thầm đổi geometry.
4. Summary hiển thị size/orientation/DPI/verification.
5. Create draft.

Wizard nhớ lựa chọn gần đây nhưng luôn cho quay lại. Ở compact height, mỗi step là một page, không nhồi hai bảng song song.

### 6.2 Bind data

1. Add connection.
2. Test/discover schema async.
3. Chọn fields/alias/type.
4. Preview sample records.
5. Drag variable vào object hoặc chọn trong inspector.
6. Problems cập nhật lineage/missing/transform error.

User không cần hiểu formula syntax cho common transforms. Advanced expression editor có autocomplete/type errors/test fixture.

### 6.3 Design and validate

- command palette hoặc Insert rail;
- drag/keyboard position;
- align/distribute/group/layer;
- record navigator trên status bar;
- Problems luôn aggregate theo severity;
- “Validate all sample records” là command riêng có progress/cancel.

### 6.4 Print Center

```text
Document/revision
  → Data/prompts/record picker
  → Printer profile/media
  → Quantity/serialization
  → Preview + preflight
  → Confirm
  → Live job status
```

Primary print button chỉ enabled khi required input hợp lệ. Override lỗi cần quyền/lý do, không chỉ OK/Cancel.

### 6.5 Reprint

1. Search job/item.
2. Xem preview và manifest gốc.
3. Chọn printer/quantity được phép.
4. Nhập reason.
5. Tạo child job, không sửa job gốc.

## 7. Dialog and form rules

- Form dữ liệu động dùng `Grid` rows + một scroll owner.
- Primary/footer actions sticky; không bị đẩy khỏi viewport.
- DPI/printer/media high-risk fields nằm vùng cố định hoặc ở step riêng.
- Combo/list dài có search/filter.
- Validation inline, summary ở top/problems; không spam MessageBox.
- File/printer/data discovery có busy state, cancel và retry.
- Error message nói: việc gì thất bại, tại sao biết, ảnh hưởng gì, bước xử lý tiếp.

## 8. Keyboard and scanner model

### Global

- `Ctrl+N/O/S/Shift+S/P/Z/Y` chuẩn;
- `Ctrl+K` command palette;
- `F6` chuyển vùng shell;
- `F4` inspector;
- `Tab/Shift+Tab` trong form; không nhảy vào decorative element;
- `Esc` cancel gesture/dialog/pane theo cấp;
- shortcut overlay có thể search.

### Designer

- arrow nudge; Shift coarse; modifier precise/configurable;
- Tab cycle nodes; Enter focus inspector; Delete; Ctrl+G/U group/ungroup;
- align/distribute có shortcut tùy cấu hình;
- focus state phân biệt viewport và inspector.

### Operator/scanner

- scanner input không phụ thuộc mouse;
- autofocus và optional auto-submit;
- Enter/Function key chuyển bước/in theo policy;
- validation giữ focus ở field lỗi và đọc được bởi screen reader;
- debounce/terminator cấu hình theo station.

## 9. Accessibility

Acceptance minimum:

- accessible name/help/state cho mọi command và editor node;
- logical tab/focus order;
- visible focus trong mọi theme;
- keyboard hoàn tất create/basic design/import/print;
- Narrator đọc selection, bounds, lock/visibility, problems và print confirmation;
- 200% text scale không che primary actions;
- high contrast giữ selection/error/guide distinguishable;
- không dùng color-only state;
- user-facing strings localizable;
- content editor hỗ trợ Unicode/bidi, label language độc lập UI language.

## 10. Onboarding

- first-run chọn use case: design + print / operator station / integration;
- interactive checklist, không overlay tour dài;
- sample generic, không link data khách hàng;
- empty states có một primary action;
- contextual Teaching Tip chỉ khi user chạm feature phức tạp;
- “Why disabled?” giải thích precondition thay vì nút mờ im lặng.

## 11. Error and status language

Mẫu:

```text
[Severity] [Object/Record/Profile]
What happened
Why it matters for physical output
Suggested fix / Jump to / Retry / View details
```

Ví dụ:

`Error · QR LotCode · Record 128 — module chỉ 1,4 dot ở profile TSC-300DPI. Tăng kích thước QR hoặc giảm version/data; job chưa được gửi.`

## 12. UX quality gates

- Wide/Standard/Compact screenshot or UI test fixtures.
- 100/125/150/200% scale.
- Light/Dark/High Contrast.
- Vietnamese/English + pseudo-localized strings +40% length.
- Keyboard-only smoke.
- Narrator/UI Automation smoke.
- 4 GB target machine responsiveness for import/large document.
- Dialogs/forms with longest content and no printers/50 printers.
- Empty/error/loading/success/stale/offline states.

