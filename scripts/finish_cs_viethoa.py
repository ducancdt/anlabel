#!/usr/bin/env python3
# -*- coding: utf-8 -*-
import io, re

def update_print_preview():
    path = "src/ANLAbel.App/PrintPreviewWindow.xaml.cs"
    with io.open(path, "r", encoding="utf-8") as f:
        c = f.read()

    pairs = [
        (
            'The current label dimensions and printer settings were saved to the open label. Use the editor Save command to write them to disk.',
            'Kích thước nhãn và cài đặt máy in hiện tại đã được lưu vào mẫu tem đang mở. Sử dụng lệnh Lưu trong trình thiết kế để ghi vào tệp trên đĩa.'
        ),
        (
            '"Print settings saved to label"',
            '"Đã lưu cài đặt in vào mẫu tem"'
        ),
        (
            'Print preparation stopped because the selected printer contract could not be validated.',
            'Quá trình chuẩn bị in đã dừng vì hợp đồng máy in đã chọn không thể kiểm chứng.'
        ),
        (
            '"Printer contract unavailable"',
            '"Hợp đồng máy in không khả dụng"'
        ),
        (
            'were already printed earlier in this session',
            'đã được in trước đó trong phiên này'
        ),
        (
            'Printing again may create duplicate labels. Continue?',
            'In lại có thể tạo nhãn trùng lặp. Tiếp tục?'
        ),
        (
            '"Rows already printed this session"',
            '"Các dòng đã in trong phiên này"'
        ),
        (
            'row(s) have preflight issues and will be skipped:',
            'dòng gặp lỗi tiền kiểm và sẽ bị bỏ qua:'
        ),
        (
            'Print the remaining {remainingCount} row(s) instead?',
            'Tiếp tục in {remainingCount} dòng còn lại?'
        ),
        (
            '"Some rows blocked"',
            '"Một số dòng bị chặn"'
        ),
        (
            '"Print blocked"',
            '"Lệnh in bị chặn"'
        ),
        (
            'Reprint blocked: the selected template, printer, DPI or data no longer matches the approved manifest.',
            'Lệnh in lại bị chặn: mẫu tem, máy in, DPI hoặc dữ liệu đã chọn không còn khớp với bản kê khai (manifest) đã phê duyệt.'
        ),
        (
            '"Manifest mismatch"',
            '"Sai lệch kê khai"'
        ),
        (
            'The linked Excel file has changed since this preview was opened. Print preparation is blocked because the displayed rows may be outdated.\\n\\nClose this window, click Update Excel, then open Print Preview again.',
            'Tệp Excel được liên kết đã thay đổi kể từ khi mở xem trước. Chuẩn bị in bị chặn vì các dòng hiển thị có thể đã lỗi thời.\\n\\nĐóng cửa sổ này, nhấn Cập nhật Excel, rồi mở lại Xem trước khi in.'
        ),
        (
            '"Excel data changed"',
            '"Dữ liệu Excel đã thay đổi"'
        )
    ]

    for old, new in pairs:
        assert old in c, f"Not found in PrintPreview: {old}"
        c = c.replace(old, new, 1)

    with io.open(path, "w", encoding="utf-8") as f:
        f.write(c)
    print("PrintPreviewWindow.xaml.cs updated successfully.")

def update_main_window():
    path = "src/ANLAbel.App/MainWindow.xaml.cs"
    with io.open(path, "r", encoding="utf-8") as f:
        c = f.read()

    pairs = [
        (
            'The approved job has no valid manifest and cannot open a guarded preview.',
            'Lệnh in đã duyệt không có bản kê khai (manifest) hợp lệ nên không thể mở xem trước bảo vệ.'
        ),
        (
            'A safe printer/job identity is available for {queueCandidate.JobId}. Query the queue now?\\n\\nThis only reads spooler status and never retries the print.',
            'Đã có thông tin định danh máy in / lệnh in an toàn cho {queueCandidate.JobId}. Bạn có muốn truy vấn hàng đợi ngay bây giờ?\\n\\nThao tác này chỉ đọc trạng thái bộ đệm in và không bao giờ in lại.'
        ),
        (
            '"Query print queue"',
            '"Truy vấn hàng đợi in"'
        ),
        (
            '"Queue reconciliation"',
            '"Đối soát hàng đợi"'
        ),
        (
            'No complete job record is available.',
            'Không có bản ghi lệnh in hoàn chỉnh nào.'
        ),
        (
            'Diagnostics:',
            'Chẩn đoán:'
        ),
        (
            'Do not retry automatically until the queue/job identity has been reconciled.',
            'Không thử lại tự động cho đến khi định danh hàng đợi/lệnh in được đối soát.'
        ),
        (
            '"Print recovery review"',
            '"Xem xét phục hồi in ấn"'
        ),
        (
            'Acknowledge {candidate.JobId} as reviewed?\\n\\nThis records an operator decision only; it does not mark the label printed.',
            'Xác nhận {candidate.JobId} đã được xem xét?\\n\\nThao tác này chỉ ghi nhận quyết định của người vận hành, không đánh dấu nhãn đã in.'
        ),
        (
            '"Acknowledge print job"',
            '"Xác nhận lệnh in"'
        ),
        (
            'Void {candidate.JobId} in the durable history?\\n\\nNo printer command is sent, and the event history is retained.',
            'Hủy bỏ {candidate.JobId} trong lịch sử bền vững?\\n\\nKhông có lệnh máy in nào được gửi, và lịch sử sự kiện vẫn được giữ lại.'
        ),
        (
            '"Void uncertain print job"',
            '"Hủy lệnh in không chắc chắn"'
        ),
        (
            'Create a linked reprint request for {candidate.JobId}?\\n\\nA new Created job is recorded for review, but it is not prepared or dispatched automatically.',
            'Tạo yêu cầu in lại liên kết cho {candidate.JobId}?\\n\\nMột lệnh mới sẽ được ghi nhận để xem xét, nhưng không tự động chuẩn bị hoặc phát lệnh.'
        ),
        (
            '"Request linked reprint"',
            '"Yêu cầu in lại liên kết"'
        ),
        (
            'Review manifest {manifest.Fingerprint} for the linked child?\\n\\nApproval only records explicit operator consent. It does not print until the current template/data are matched and dispatched separately.',
            'Xem xét bản kê khai {manifest.Fingerprint} cho lệnh in liên kết?\\n\\nPhê duyệt chỉ ghi nhận sự đồng thuận rõ ràng của người vận hành. Lệnh sẽ không in cho đến khi mẫu tem/dữ liệu hiện tại khớp và được phát lệnh riêng.'
        ),
        (
            '"Approve linked reprint"',
            '"Phê duyệt in lại liên kết"'
        ),
        (
            'Open Print Preview to select the exact source rows for this approved child? The manifest guard will block any changed template/data.',
            'Mở Xem trước khi in để chọn chính xác các dòng nguồn cho lệnh in đã duyệt này? Khung bảo vệ kê khai sẽ chặn nếu mẫu tem hoặc dữ liệu có thay đổi.'
        ),
        (
            '"Choose reprint rows"',
            '"Chọn dòng in lại"'
        ),
        (
            '"Open ANLAbel template"',
            '"Mở mẫu tem ANLAbel"'
        ),
        (
            '"ANLAbel Template (*.anlabel)|*.anlabel|JSON (*.json)|*.json|All files (*.*)|*.*"',
            '"Mẫu tem ANLAbel (*.anlabel)|*.anlabel|Tệp JSON (*.json)|*.json|Tất cả tệp (*.*)|*.*"'
        ),
        (
            'The selected template could not be read, so ANLAbel opened its last known-good backup.\\n\\nBackup: {backupPath}\\n\\nThe original file was not overwritten. Use Save As to create a new template after reviewing it.',
            'Không thể đọc mẫu tem đã chọn, ANLAbel đã mở bản sao lưu tốt gần nhất.\\n\\nBản sao lưu: {backupPath}\\n\\nTệp gốc không bị ghi đè. Dùng lệnh Lưu thành để tạo mẫu tem mới sau khi xem xét.'
        ),
        (
            '"Template recovered"',
            '"Đã khôi phục mẫu tem"'
        ),
        (
            'Save the current template first. Revision history is available for committed files only.',
            'Vui lòng lưu mẫu tem hiện tại trước. Lịch sử sửa đổi chỉ khả dụng cho các tệp đã lưu trên đĩa.'
        ),
        (
            '"Revision history"',
            '"Lịch sử sửa đổi"'
        ),
        (
            '"Open failed"',
            '"Mở thất bại"'
        ),
        (
            '"Export print history to Excel"',
            '"Xuất lịch sử in ra Excel"'
        ),
        (
            '"Excel Workbook (*.xlsx)|*.xlsx"',
            '"Sổ làm việc Excel (*.xlsx)|*.xlsx"'
        ),
        (
            'Remove the Excel link from this template?\\n\\nObjects keep their field bindings but will show placeholders until you import data again.',
            'Hủy liên kết Excel khỏi mẫu tem này?\\n\\nCác đối tượng vẫn giữ trường liên kết nhưng sẽ hiển thị nội dung mẫu cho đến khi bạn nhập dữ liệu lại.'
        ),
        (
            'Print Preview could not be opened. {ex.Message}',
            'Không thể mở Xem trước khi in. {ex.Message}'
        ),
        (
            '"Preview unavailable"',
            '"Xem trước không khả dụng"'
        ),
        (
            '{_viewModel.PrinterQueueStatusMessage}\\n\\nOpen Printer Setup now to choose a verified queue?',
            '{_viewModel.PrinterQueueStatusMessage}\\n\\nMở Cài đặt máy in ngay bây giờ để chọn hàng đợi đã xác thực?'
        ),
        (
            '"Printer queue unavailable"',
            '"Hàng đợi máy in không khả dụng"'
        )
    ]

    for old, new in pairs:
        assert old in c, f"Not found in MainWindow: {old}"
        c = c.replace(old, new, 1)

    # Also replace any remaining "Print recovery" titles
    c = c.replace('"Print recovery"', '"Phục hồi in ấn"')

    with io.open(path, "w", encoding="utf-8") as f:
        f.write(c)
    print("MainWindow.xaml.cs updated successfully.")

if __name__ == "__main__":
    update_print_preview()
    update_main_window()
