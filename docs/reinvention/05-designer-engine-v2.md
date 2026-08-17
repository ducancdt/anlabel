# Designer Engine V2

## 1. Quyết định cốt lõi

Không bỏ artboard vật lý. Bỏ việc để WPF `Canvas` và control tree đồng thời làm layout engine, renderer, hit-test index, gesture state và cầu nối mutation.

V2 gồm:

```text
DocumentSnapshot
  → SceneCompiler
  → ResolvedScene
  → WpfScenePresenter
  + EditorOverlay
```

Document là nguồn sự thật. Presenter chỉ đọc scene. Overlay giữ selection/guides/handles. Mọi thay đổi đi qua editor command.

## 2. Đánh giá lựa chọn renderer

| Lựa chọn | Ưu điểm | Rủi ro | Quyết định |
| --- | --- | --- | --- |
| Tiếp tục control-per-object trên WPF Canvas | ít thay đổi | visual tree nặng, coupling, khó parity | không dùng làm kiến trúc đích |
| WPF `DrawingVisual` retained scene | phù hợp WPF/text/printing, nhẹ, incremental | phải tự hit-test/overlay/accessibility bridge | **chọn cho V2 đầu tiên** |
| SkiaSharp viewport | hiệu năng/cross-platform, control rendering | font/text/print parity và dependency mới | prototype sau compiler, không phải R1 |
| WinUI 3 custom viewport | shell hiện đại | rewrite + print/text migration cùng lúc | xem lại sau khi engine độc lập |
| Avalonia | cross-platform | scope lớn, printer industrial Windows vẫn là chính | không ưu tiên hiện tại |
| Web/canvas | dễ remote collaboration | font/physical print/gateway phức tạp | operator portal về sau, không thay desktop authoring ngay |

Điểm quan trọng: `SceneCompiler` làm renderer adapter có thể thay được. Nếu chọn Skia/WinUI sau này, document/editor semantics không đổi.

## 3. Viewport layers

Tách rõ:

1. workspace background;
2. artboard/media surface;
3. compiled content visuals;
4. non-printing annotations;
5. selection/transform overlay;
6. guides/snap/alignment overlay;
7. problems/hover/accessibility overlay.

Chỉ layer 2–3 ảnh hưởng print scene. Layer 4–7 không persisted trừ guide được user tạo rõ ràng.

## 4. Scene primitives

Resolved primitives tối thiểu:

- text run/frame;
- vector path/shape/line;
- image resource;
- 1D barcode vector bars + human-readable text;
- 2D module matrix;
- clip/group/transform;
- repeat/table/layout container.

Mỗi primitive có source node ID, bounds, transform, clip, paint, z-order, hit shape và diagnostic refs.

## 5. Interaction architecture

### Hit testing

- Transform pointer từ viewport DIP → document physical coordinates.
- Query spatial index theo tolerance phụ thuộc zoom/input mode.
- Hit-test từ z-order cao xuống.
- Dùng geometry thật: line stroke tolerance, transparent rectangle interior policy, text/frame bounds, barcode bounds.
- `Alt` cycle candidates hoặc bypass snap; click-through semantics phải test được.

R1 dùng linear index nếu node count nhỏ; interface cho phép chuyển uniform grid/R-tree khi benchmark vượt threshold.

### Selection

- Selection là tập stable node IDs trong `EditorSession`.
- Primary selection quyết định inspector anchor.
- Group/layer lock và hidden state được kiểm tra trước gesture.
- Marquee hỗ trợ contain/intersect mode.
- Tab/Shift+Tab cycle theo scene/z-order cho keyboard.

### Transform gesture

```text
PointerDown → BeginTransformSession
PointerMove → compute preview delta + snap guides
PointerUp   → Commit TransformNodesCommand once
Esc/LostCapture → Cancel, document unchanged
```

Preview delta nằm trong editor overlay, không spam model property.

### Snap

Snap service nhận geometry snapshot và trả:

- snapped delta;
- matched anchors/edges/centers;
- guide visuals;
- explanation/priority.

Các nguồn snap: grid, guide, artboard, margin/safe area, object edge/center, baseline, container track. `Alt` bypass tạm thời.

Contract chi tiết nằm tại [Designer Precision và Industrial Reliability](09-designer-precision-and-industrial-reliability.md): tolerance đo ở screen-space theo zoom, acquire/release hysteresis, semantic priority, stable tie-break, target lock từng trục và cùng engine cho single/group/resize/draw. Selection phải tách selected/primary/key bằng stable IDs.

## 6. Layout model

### AbsoluteLayer

Tương thích hoàn toàn canvas hiện tại. Node có transform/bounds tuyệt đối.

### Group

Transform cục bộ, children và optional clipping. Resize policy explicit: scale content, resize container only hoặc reflow.

### Stack/Grid/Table

Phù hợp text/data động, tránh tự sửa Width/Height trong render. Layout engine measure/arrange giống concept UI layout nhưng output là physical scene, không là WPF layout.

### RepeatRegion

Lặp item template theo records, hỗ trợ header/footer/group và page break. Đây là nền cho table/multi-label/multi-up nâng cao.

### Constraints

R2 chỉ cần constraint hữu ích:

- anchor left/right/top/bottom/center;
- fixed/fill/hug-content;
- min/max;
- aspect lock;
- relative alignment.

Không xây general-purpose solver trước khi có use case.

## 7. Text engine

Text là vùng parity khó nhất. Contract cần:

- font family/weight/style/stretch;
- fallback chain và missing-font diagnostic;
- point size, line height, letter spacing;
- alignment, wrapping, overflow policy;
- bidi/RTL/culture;
- shaped glyph runs hoặc deterministic measurement result;
- `fit`, `clip`, `ellipsis`, `error`, `shrink-to-fit` là policy explicit.

Không tự sửa node bounds khi render. “Hug content” là layout property được compiler resolve; nếu user chọn “Apply measured size”, đó là command explicit.

Frame alignment, content alignment và baseline alignment là ba semantics riêng. `TextLayoutResult` phải expose layout bounds, ink bounds, first/last baseline và overflow diagnostics; sizing/overflow/direction/horizontal/vertical alignment là policy persisted explicit. Chi tiết và matrix test nằm trong [plan precision/reliability](09-designer-precision-and-industrial-reliability.md).

## 8. Barcode engine

- Binding/transform resolve data trước barcode.
- Symbology validator trả typed diagnostics.
- Barcode layout tạo vector bars/modules và quiet zone.
- Module physical dots được tính theo target printer profile.
- Human-readable text là sub-layout có font/fallback riêng.
- Designer có thể preview ở screen DPI nhưng problems phải dựa trên print DPI.
- GS1/AI wizard là feature riêng trên structured barcode data, không ghép string mù.

## 9. Inspector

Inspector không bind trực tiếp hàng trăm TextBox vào mutable `LabelObject`.

Flow:

`Selection → PropertySchema → Editor values/mixed state → Commit command`

Property schema khai báo:

- group/order/label/unit;
- applicable node types;
- editor type/range/validation;
- multi-selection merge behavior;
- live preview policy;
- command factory.

Nhờ đó inspector context-aware, test được và không cần một XAML 100 KB.

## 10. Undo/redo và history

- Command delta, không JSON snapshot cho mỗi change.
- Coalesce typing/nudging theo thời gian và property.
- Transaction cho multi-node action.
- History entry có label, affected IDs, timestamp, optional before/after summary.
- Document revision history khác editor undo history.
- Autosave lưu draft snapshot/operation checkpoint; không ghi đè Published.

## 11. Multi-artboard, layer và component

Document có thể có nhiều artboard/template về sau, nhưng R1 giữ một artboard để giảm scope.

Layer hỗ trợ:

- design visibility;
- print visibility/condition;
- lock;
- non-printing;
- clip;
- name/color metadata.

Component gồm definition và instances. Override được whitelist; detach là command. Component library dùng resource/document reference có version.

## 12. Performance budget

Mục tiêu đầu:

- 500 nodes: pan/zoom/drag giữ tương tác mượt trên máy mục tiêu;
- incremental compile chỉ rebuild affected subtree;
- không tạo FrameworkElement cho mỗi primitive;
- text/barcode/resource cache theo content key;
- compile có cancellation/version token, bỏ kết quả stale;
- UI thread chỉ swap visual/result và xử lý input.

Đo bằng ETW/WPR hoặc timing instrumentation trước khi tối ưu sâu.

## 13. Accessibility bridge

DrawingVisual không tự có automation tree đầy đủ. Presenter cần virtual peers hoặc companion accessible item list:

- mỗi selectable node có accessible name/type/bounds/state;
- keyboard cycle/select/move/resize;
- focus outline luôn thấy;
- inspector là đường chỉnh property đầy đủ bằng keyboard;
- problems panel link về node;
- high contrast không dựa chỉ vào màu.

Đây là deliverable kiến trúc, không để cuối.

## 14. Parity và acceptance gates

### R1 parity fixtures

- Text, TextBox, Rectangle, Ellipse, Line, Image;
- Code128, QR, DataMatrix;
- rotation 0/90/180/270;
- bindings/formulas hiện có;
- 203/300/600 DPI;
- edge clipping/overflow;
- Vietnamese/Unicode.

### Gate

- source node geometry không đổi sau compile/render;
- scene bounds nằm trong tolerance đã định với renderer cũ;
- designer/preview/print cùng scene hash trước target adapter;
- undo/redo round-trip document hash;
- cancel/lost capture không đổi document;
- no stale compile replaces newer result;
- keyboard selection/transform hoạt động;
- profiler không thấy file/printer/data I/O trên UI thread.

## 15. Rollout

1. compiler chạy shadow mode, không render UI;
2. diagnostics/parity report cho fixtures;
3. viewport V2 feature flag theo document/object subset;
4. read-only V2 preview;
5. selection/navigation;
6. move/resize/insert;
7. inspector/undo;
8. preview dùng scene;
9. print dùng scene;
10. retire old paths sau hardware/golden gate.
