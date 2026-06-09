ANLAbel Standard Icon Pack V2 - Match Mockup
===========================================

Bộ icon này được vẽ lại theo đúng style trong mockup ANLAbel:
- SVG line icon 32x32.
- Nét mảnh, bo tròn, tối giản.
- Toolbar icon dùng nét xanh #0F62D0.
- Import Excel dùng xanh lá #22A05A giống ảnh.
- Delete Selection dùng đỏ #EF4444 giống ảnh.
- Có 2 thư mục:
  1. svg/       : dùng currentColor, phù hợp import vào code để đổi màu theo state.
  2. svg_color/ : màu cố định, dùng trực tiếp trong app hoặc preview.
- Có preview.html để xem toàn bộ icon thật bằng trình duyệt.
- Nếu môi trường có cairosvg, có thêm png_64/.

Cách dùng khuyến nghị:
- Ribbon toolbar: 24px
- ToolTile bên trái: 30–32px
- Tree view: 16–18px
- Empty state: 56–64px
- Không mix icon từ nhiều bộ khác nhau.

Danh sách icon:
- new
- open
- save
- undo
- redo
- import_excel
- printer_setup
- preview
- print_current
- print_all_rows
- test_print
- panels
- delete_selection
- zoom_minus
- zoom_plus
- help
- settings
- static_text
- text_box
- barcode
- qr_code
- data_matrix
- line
- rectangle
- ellipse
- folder
- database
- table
- cursor_select
- collapse_chevron
- expand_chevron

Prompt cho Codex:
Tôi đã đính kèm ANLAbel Standard Icon Pack V2. Hãy dùng đúng icon trong thư mục svg/ hoặc svg_color/, không tự vẽ lại và không dùng icon ngẫu nhiên. Tạo một Icon component trung tâm, ví dụ AnlabelIcon(name, size, state). Ánh xạ các nút toolbar và tools panel đúng theo tên icon. Icon phải giữ phong cách line icon giống mockup: nét mảnh, bo tròn, màu xanh #0F62D0, riêng Import Excel màu xanh lá và Delete Selection màu đỏ. Không hard-code SVG rải rác trong từng màn hình.
