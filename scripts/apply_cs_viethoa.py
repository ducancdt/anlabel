#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""Thực thi thay thế chuỗi hiển thị tiếng Anh sang tiếng Việt trong các tệp C#."""
from viethoa_cs import replace_in_file

replacements_excel_import = [
    (
        '            Title = "Import Excel or CSV data",\n            Filter = "Data files (*.xlsx;*.xlsm;*.csv)|*.xlsx;*.xlsm;*.csv|Excel Workbook (*.xlsx;*.xlsm)|*.xlsx;*.xlsm|CSV files (*.csv)|*.csv|All files (*.*)|*.*"',
        '            Title = "Nhập dữ liệu Excel hoặc CSV",\n            Filter = "Tệp dữ liệu (*.xlsx;*.xlsm;*.csv)|*.xlsx;*.xlsm;*.csv|Sổ làm việc Excel (*.xlsx;*.xlsm)|*.xlsx;*.xlsm|Tệp CSV (*.csv)|*.csv|Tất cả tệp (*.*)|*.*"'
    ),
    (
        'MessageBox.Show(this, $"Cannot read the Excel file. Close it in Excel and try again.\\n\\n{ex.Message}", "Import failed", MessageBoxButton.OK, MessageBoxImage.Warning);',
        'MessageBox.Show(this, $"Không thể đọc tệp Excel. Vui lòng đóng tệp trong Excel rồi thử lại.\\n\\n{ex.Message}", "Nhập dữ liệu thất bại", MessageBoxButton.OK, MessageBoxImage.Warning);'
    ),
    (
        '            MessageBox.Show(\n                this,\n                "Reading this Excel file is taking too long and was stopped automatically. This can happen on an older or busy computer. Try again, close other programs first, or try a smaller/simpler file.",\n                "Import timed out",\n                MessageBoxButton.OK,\n                MessageBoxImage.Warning);',
        '            MessageBox.Show(\n                this,\n                "Đọc tệp Excel quá lâu và đã tự động dừng. Hãy thử lại, đóng các chương trình khác hoặc thử với tệp nhỏ hơn.",\n                "Hết thời gian nhập dữ liệu",\n                MessageBoxButton.OK,\n                MessageBoxImage.Warning);'
    ),
    (
        'MessageBox.Show(this, ex.Message, "Import failed", MessageBoxButton.OK, MessageBoxImage.Error);',
        'MessageBox.Show(this, ex.Message, "Nhập dữ liệu thất bại", MessageBoxButton.OK, MessageBoxImage.Error);'
    )
]

replacements_db_manager = [
    (
        '        var usageNote = isUsedByCurrent\n            ? "\\n\\nThe template currently open in the designer uses this source — it will fall back to its own file path after removal."\n            : "\\n\\nAny template referencing this source will fall back to its own file path after removal.";\n\n        var confirmed = MessageBox.Show(\n            this,\n            $"Remove the shared data source \\"{source.DisplayName}\\"?{usageNote}",\n            "Remove Data Source",\n            MessageBoxButton.YesNo,\n            MessageBoxImage.Warning) == MessageBoxResult.Yes;',
        '        var usageNote = isUsedByCurrent\n            ? "\\n\\nMẫu tem đang mở trong bảng thiết kế đang dùng nguồn này — mẫu sẽ quay lại đường dẫn tệp riêng sau khi xóa."\n            : "\\n\\nMọi mẫu tem tham chiếu tới nguồn này sẽ quay lại đường dẫn tệp riêng sau khi xóa.";\n\n        var confirmed = MessageBox.Show(\n            this,\n            $"Bạn có chắc chắn muốn xóa nguồn dữ liệu dùng chung \\"{source.DisplayName}\\"?{usageNote}",\n            "Xóa nguồn dữ liệu",\n            MessageBoxButton.YesNo,\n            MessageBoxImage.Warning) == MessageBoxResult.Yes;'
    ),
    (
        'MessageBox.Show(this, "No orphaned data sources found (missing file, unused for 30+ days).", "Clean Up", MessageBoxButton.OK, MessageBoxImage.Information);',
        'MessageBox.Show(this, "Không tìm thấy nguồn dữ liệu mồ côi (tệp không tồn tại, không dùng trong 30+ ngày).", "Dọn dẹp nguồn dữ liệu", MessageBoxButton.OK, MessageBoxImage.Information);'
    )
]

replacements_ds_cleanup = [
    (
        'MessageBox.Show(this, "No sources are selected.", "Clean Up", MessageBoxButton.OK, MessageBoxImage.Information);',
        'MessageBox.Show(this, "Chưa chọn nguồn dữ liệu nào.", "Dọn dẹp nguồn dữ liệu", MessageBoxButton.OK, MessageBoxImage.Information);'
    ),
    (
        '        var confirmed = MessageBox.Show(\n            this,\n            $"Remove {selected.Length} data source(s) from the registry? This cannot be undone.",\n            "Clean Up",\n            MessageBoxButton.YesNo,\n            MessageBoxImage.Warning) == MessageBoxResult.Yes;',
        '        var confirmed = MessageBox.Show(\n            this,\n            $"Xóa {selected.Length} nguồn dữ liệu khỏi sổ đăng ký? Thao tác này không thể hoàn tác.",\n            "Dọn dẹp nguồn dữ liệu",\n            MessageBoxButton.YesNo,\n            MessageBoxImage.Warning) == MessageBoxResult.Yes;'
    )
]

replacements_new_template = [
    (
        'MessageBox.Show(this, "Please enter valid width, height and DPI.", "Invalid template", MessageBoxButton.OK, MessageBoxImage.Warning);',
        'MessageBox.Show(this, "Vui lòng nhập chiều rộng, chiều cao và DPI hợp lệ.", "Mẫu tem không hợp lệ", MessageBoxButton.OK, MessageBoxImage.Warning);'
    ),
    (
        'Request = new NewTemplateRequest(string.IsNullOrWhiteSpace(NameBox.Text) ? "Untitled Label" : NameBox.Text.Trim(), width, height, dpi);',
        'Request = new NewTemplateRequest(string.IsNullOrWhiteSpace(NameBox.Text) ? "Mẫu tem mới" : NameBox.Text.Trim(), width, height, dpi);'
    )
]

replacements_revision = [
    (
        '        var confirmation = MessageBox.Show(\n            this,\n            "Restore the validated revision as the current template? The current primary will be moved into the backup slot and archived, and the open document will be reloaded. Unsaved edits in the current document will be discarded.",\n            "Confirm template rollback",\n            MessageBoxButton.YesNo,\n            MessageBoxImage.Warning);',
        '        var confirmation = MessageBox.Show(\n            this,\n            "Khôi phục bản sửa đổi đã kiểm chứng này làm mẫu tem hiện tại? Bản chính hiện tại sẽ được lưu trữ sao lưu và tài liệu đang mở sẽ được tải lại. Các chỉnh sửa chưa lưu sẽ bị hủy.",\n            "Xác nhận khôi phục mẫu tem",\n            MessageBoxButton.YesNo,\n            MessageBoxImage.Warning);'
    ),
    (
        'StatusText.Text = $"Restored {result.TemplateName} from the validated revision. The previous primary is retained as backup and archive.";',
        'StatusText.Text = $"Đã khôi phục {result.TemplateName} từ bản sửa đổi đã kiểm chứng. Bản chính trước đó được lưu trữ sao lưu.";'
    ),
    (
        'StatusText.Text = $"Rollback failed: {exception.Message}";\n            MessageBox.Show(this, exception.Message, "Rollback failed", MessageBoxButton.OK, MessageBoxImage.Error);',
        'StatusText.Text = $"Khôi phục thất bại: {exception.Message}";\n            MessageBox.Show(this, exception.Message, "Khôi phục thất bại", MessageBoxButton.OK, MessageBoxImage.Error);'
    )
]

replacements_print_center = [
    (
        'MessageBox.Show(this, ex.Message, "Print Center", MessageBoxButton.OK, MessageBoxImage.Warning);',
        'MessageBox.Show(this, ex.Message, "Trung tâm in ấn", MessageBoxButton.OK, MessageBoxImage.Warning);'
    ),
    (
        '        var choice = MessageBox.Show(\n            this,\n            $"Void {candidate.JobId} in the durable history? No printer command is sent and the event chain is retained.",\n            "Void print job",\n            MessageBoxButton.YesNo,\n            MessageBoxImage.Warning);',
        '        var choice = MessageBox.Show(\n            this,\n            $"Hủy lệnh {candidate.JobId} trong lịch sử bền vững? Không có lệnh in nào được gửi và chuỗi sự kiện vẫn được giữ lại.",\n            "Hủy lệnh in",\n            MessageBoxButton.YesNo,\n            MessageBoxImage.Warning);'
    ),
    (
        'MessageBox.Show(this, message, "Print Center", MessageBoxButton.OK, image);',
        'MessageBox.Show(this, message, "Trung tâm in ấn", MessageBoxButton.OK, image);'
    )
]

replacements_printer_setup = [
    (
        'MessageBox.Show(this, "Select a printer queue first.", "Driver preferences", MessageBoxButton.OK, MessageBoxImage.Information);',
        'MessageBox.Show(this, "Vui lòng chọn một hàng đợi máy in trước.", "Tùy chọn trình điều khiển", MessageBoxButton.OK, MessageBoxImage.Information);'
    ),
    (
        'MessageBox.Show(this, ex.Message, "Driver preferences", MessageBoxButton.OK, MessageBoxImage.Warning);',
        'MessageBox.Show(this, ex.Message, "Tùy chọn trình điều khiển", MessageBoxButton.OK, MessageBoxImage.Warning);'
    ),
    (
        'MessageBox.Show(this, "Please enter valid label width and height in mm.", "Printer setup", MessageBoxButton.OK, MessageBoxImage.Warning);',
        'MessageBox.Show(this, "Vui lòng nhập chiều rộng và chiều cao nhãn hợp lệ (mm).", "Cài đặt máy in", MessageBoxButton.OK, MessageBoxImage.Warning);'
    ),
    (
        'MessageBox.Show(this, stock.Diagnostic, "Printer setup", MessageBoxButton.OK, MessageBoxImage.Warning);',
        'MessageBox.Show(this, stock.Diagnostic, "Cài đặt máy in", MessageBoxButton.OK, MessageBoxImage.Warning);'
    ),
    (
        'MessageBox.Show(this, dpiDecision.Diagnostic, "Printer setup", MessageBoxButton.OK, MessageBoxImage.Warning);',
        'MessageBox.Show(this, dpiDecision.Diagnostic, "Cài đặt máy in", MessageBoxButton.OK, MessageBoxImage.Warning);'
    ),
    (
        'MessageBox.Show(this, "Select an exact printer queue before applying this setup.", "Printer setup", MessageBoxButton.OK, MessageBoxImage.Warning);',
        'MessageBox.Show(this, "Vui lòng chọn chính xác hàng đợi máy in trước khi áp dụng thiết lập.", "Cài đặt máy in", MessageBoxButton.OK, MessageBoxImage.Warning);'
    ),
    (
        '            MessageBox.Show(\n                this,\n                "Printer capabilities have not been loaded yet. Please wait for the driver check to complete.",\n                "Printer media compatibility",\n                MessageBoxButton.OK,\n                MessageBoxImage.Warning);',
        '            MessageBox.Show(\n                this,\n                "Khả năng của máy in chưa được tải xong. Vui lòng đợi quá trình kiểm tra trình điều khiển hoàn tất.",\n                "Tương thích khổ in máy in",\n                MessageBoxButton.OK,\n                MessageBoxImage.Warning);'
    ),
    (
        '                var warning = string.IsNullOrWhiteSpace(SelectedMediaResolution.Message)\n                    ? "The selected media cannot be verified as driver-advertised physical media, "\n                      + "so it cannot authorize printing. Choose a driver-advertised size or enable "\n                      + "driver-automatic paper selection."\n                    : SelectedMediaResolution.Message;\n                MessageBox.Show(\n                    this,\n                    warning,\n                    "Printer media compatibility",\n                    MessageBoxButton.OK,\n                    MessageBoxImage.Warning);',
        '                var warning = string.IsNullOrWhiteSpace(SelectedMediaResolution.Message)\n                    ? "Khổ giấy đã chọn không thể xác thực là khổ giấy vật lý do trình điều khiển công bố, "\n                      + "nên không thể cho phép in. Vui lòng chọn khổ do trình điều khiển hỗ trợ hoặc bật "\n                      + "tùy chọn khổ giấy tự động từ trình điều khiển."\n                    : SelectedMediaResolution.Message;\n                MessageBox.Show(\n                    this,\n                    warning,\n                    "Tương thích khổ in máy in",\n                    MessageBoxButton.OK,\n                    MessageBoxImage.Warning);'
    )
]

replacements_print_preview = [
    (
        'MessageBox.Show(this, "No Windows printers were found.", "Printer setup", MessageBoxButton.OK, MessageBoxImage.Information);',
        'MessageBox.Show(this, "Không tìm thấy máy in Windows nào.", "Cài đặt máy in", MessageBoxButton.OK, MessageBoxImage.Information);'
    ),
    (
        'MessageBox.Show(this, ex.Message, "Printer setup", MessageBoxButton.OK, MessageBoxImage.Warning);',
        'MessageBox.Show(this, ex.Message, "Cài đặt máy in", MessageBoxButton.OK, MessageBoxImage.Warning);'
    ),
    (
        '        MessageBox.Show(\n            this,\n            "The current label dimensions and printer settings were saved to the open label. Use the editor Save command to write them to disk.",\n            "Print settings saved to label",\n            MessageBoxButton.OK,\n            MessageBoxImage.Information);',
        '        MessageBox.Show(\n            this,\n            "Kích thước nhãn và cài đặt máy in hiện tại đã được lưu vào mẫu tem đang mở. Sử dụng lệnh Lưu trong trình thiết kế để ghi vào tệp trên đĩa.",\n            "Đã lưu cài đặt in vào mẫu tem",\n            MessageBoxButton.OK,\n            MessageBoxImage.Information);'
    ),
    (
        'MessageBox.Show(this, "No rows selected for printing. Use checkboxes to select rows.", "Print", MessageBoxButton.OK, MessageBoxImage.Information);',
        'MessageBox.Show(this, "Chưa chọn dòng nào để in. Sử dụng các hộp kiểm để chọn dòng.", "In", MessageBoxButton.OK, MessageBoxImage.Information);'
    ),
    (
        '                MessageBox.Show(\n                    this,\n                    $"Print preparation stopped because the selected printer contract could not be validated.\\n\\n{ex.Message}",\n                    "Printer contract unavailable",\n                    MessageBoxButton.OK,\n                    MessageBoxImage.Warning);',
        '                MessageBox.Show(\n                    this,\n                    $"Quá trình chuẩn bị in đã dừng vì hợp đồng máy in đã chọn không thể kiểm chứng.\\n\\n{ex.Message}",\n                    "Hợp đồng máy in không khả dụng",\n                    MessageBoxButton.OK,\n                    MessageBoxImage.Warning);'
    ),
    (
        '                var choice = MessageBox.Show(\n                    this,\n                    $"{alreadyPrintedRows.Length} selected row(s) were already printed earlier in this session (Row {string.Join(", ", alreadyPrintedRows)}).\\n\\nPrinting again may create duplicate labels. Continue?",\n                    "Rows already printed this session",\n                    MessageBoxButton.YesNo,\n                    MessageBoxImage.Warning);',
        '                var choice = MessageBox.Show(\n                    this,\n                    $"{alreadyPrintedRows.Length} dòng được chọn đã được in trước đó trong phiên này (Dòng {string.Join(", ", alreadyPrintedRows)}).\\n\\nIn lại có thể tạo nhãn trùng lặp. Tiếp tục?",\n                    "Các dòng đã in trong phiên này",\n                    MessageBoxButton.YesNo,\n                    MessageBoxImage.Warning);'
    ),
    (
        'MessageBox.Show(this, preflight.ToUserMessage(), "Print blocked", MessageBoxButton.OK, MessageBoxImage.Warning);',
        'MessageBox.Show(this, preflight.ToUserMessage(), "Lệnh in bị chặn", MessageBoxButton.OK, MessageBoxImage.Warning);'
    ),
    (
        '                var choice = MessageBox.Show(\n                    this,\n                    $"{badSourceRows.Length} row(s) have preflight issues and will be skipped:\\nRow {string.Join(", ", badSourceRows)}\\n\\n{preflight.ToUserMessage(5)}\\n\\nPrint the remaining {remainingCount} row(s) instead?",\n                    "Some rows blocked",\n                    MessageBoxButton.YesNoCancel,\n                    MessageBoxImage.Warning);',
        '                var choice = MessageBox.Show(\n                    this,\n                    $"{badSourceRows.Length} dòng gặp lỗi tiền kiểm và sẽ bị bỏ qua:\\nDòng {string.Join(", ", badSourceRows)}\\n\\n{preflight.ToUserMessage(5)}\\n\\nTiếp tục in {remainingCount} dòng còn lại?",\n                    "Một số dòng bị chặn",\n                    MessageBoxButton.YesNoCancel,\n                    MessageBoxImage.Warning);'
    ),
    (
        'MessageBox.Show(this, "No rows left to print after skipping the blocked ones.", "Print", MessageBoxButton.OK, MessageBoxImage.Information);',
        'MessageBox.Show(this, "Không còn dòng nào để in sau khi bỏ qua các dòng lỗi.", "In", MessageBoxButton.OK, MessageBoxImage.Information);'
    ),
    (
        '                    MessageBox.Show(\n                        this,\n                        "Reprint blocked: the selected template, printer, DPI or data no longer matches the approved manifest.",\n                        "Manifest mismatch",\n                        MessageBoxButton.OK,\n                        MessageBoxImage.Warning);',
        '                    MessageBox.Show(\n                        this,\n                        "Lệnh in lại bị chặn: mẫu tem, máy in, DPI hoặc dữ liệu đã chọn không còn khớp với bản kê khai (manifest) đã phê duyệt.",\n                        "Sai lệch kê khai",\n                        MessageBoxButton.OK,\n                        MessageBoxImage.Warning);'
    ),
    (
        'MessageBox.Show(this, printResult.UserFacingStatus, "Print not completed", MessageBoxButton.OK, MessageBoxImage.Warning);',
        'MessageBox.Show(this, printResult.UserFacingStatus, "Lệnh in chưa hoàn tất", MessageBoxButton.OK, MessageBoxImage.Warning);'
    ),
    (
        'MessageBox.Show(this, ex.Message, "Print failed", MessageBoxButton.OK, MessageBoxImage.Error);',
        'MessageBox.Show(this, ex.Message, "In thất bại", MessageBoxButton.OK, MessageBoxImage.Error);'
    ),
    (
        '        MessageBox.Show(\n            this,\n            "The linked Excel file has changed since this preview was opened. Print preparation is blocked because the displayed rows may be outdated.\\n\\nClose this window, click Update Excel, then open Print Preview again.",\n            "Excel data changed",\n            MessageBoxButton.OK,\n            MessageBoxImage.Warning);',
        '        MessageBox.Show(\n            this,\n            "Tệp Excel được liên kết đã thay đổi kể từ khi mở xem trước. Chuẩn bị in bị chặn vì các dòng hiển thị có thể đã lỗi thời.\\n\\nĐóng cửa sổ này, nhấn Cập nhật Excel, rồi mở lại Xem trước khi in.",\n            "Dữ liệu Excel đã thay đổi",\n            MessageBoxButton.OK,\n            MessageBoxImage.Warning);'
    ),
    (
        'MessageBox.Show(this, result.UserFacingStatus, "Calibration not completed", MessageBoxButton.OK, MessageBoxImage.Warning);',
        'MessageBox.Show(this, result.UserFacingStatus, "Hiệu chỉnh chưa hoàn tất", MessageBoxButton.OK, MessageBoxImage.Warning);'
    ),
    (
        'MessageBox.Show(this, ex.Message, "Calibration print failed", MessageBoxButton.OK, MessageBoxImage.Error);',
        'MessageBox.Show(this, ex.Message, "In hiệu chỉnh thất bại", MessageBoxButton.OK, MessageBoxImage.Error);'
    ),
    (
        'MessageBox.Show(this, stock.Diagnostic, "Label setup", MessageBoxButton.OK, MessageBoxImage.Warning);',
        'MessageBox.Show(this, stock.Diagnostic, "Thiết lập nhãn", MessageBoxButton.OK, MessageBoxImage.Warning);'
    )
]

replacements_main_window = [
    (
        '            MessageBox.Show(\n                this,\n                "The approved job has no valid manifest and cannot open a guarded preview.",\n                "Print Center",\n                MessageBoxButton.OK,\n                MessageBoxImage.Warning);',
        '            MessageBox.Show(\n                this,\n                "Lệnh in đã duyệt không có bản kê khai (manifest) hợp lệ nên không thể mở xem trước bảo vệ.",\n                "Trung tâm in ấn",\n                MessageBoxButton.OK,\n                MessageBoxImage.Warning);'
    ),
    (
        'MessageBox.Show(this, report.UserFacingSummary, "Print recovery", MessageBoxButton.OK, MessageBoxImage.Information);',
        'MessageBox.Show(this, report.UserFacingSummary, "Phục hồi in ấn", MessageBoxButton.OK, MessageBoxImage.Information);'
    ),
    (
        '                var queryChoice = MessageBox.Show(\n                    this,\n                    $"A safe printer/job identity is available for {queueCandidate.JobId}. Query the queue now?\\n\\nThis only reads spooler status and never retries the print.",\n                    "Query print queue",\n                    MessageBoxButton.YesNo,\n                    MessageBoxImage.Question);',
        '                var queryChoice = MessageBox.Show(\n                    this,\n                    $"Đã có thông tin định danh máy in / lệnh in an toàn cho {queueCandidate.JobId}. Bạn có muốn truy vấn hàng đợi ngay bây giờ?\\n\\nThao tác này chỉ đọc trạng thái bộ đệm in và không bao giờ in lại.",\n                    "Truy vấn hàng đợi in",\n                    MessageBoxButton.YesNo,\n                    MessageBoxImage.Question);'
    ),
    (
        '                    MessageBox.Show(\n                        this,\n                        reconciliation.Summary,\n                        "Queue reconciliation",\n                        MessageBoxButton.OK,\n                        reconciliation.Outcome == ANLAbel.Data.PrintLogs.PrintJobReconciliationOutcome.QueueObserved\n                            ? MessageBoxImage.Information\n                            : MessageBoxImage.Warning);',
        '                    MessageBox.Show(\n                        this,\n                        reconciliation.Summary,\n                        "Đối soát hàng đợi",\n                        MessageBoxButton.OK,\n                        reconciliation.Outcome == ANLAbel.Data.PrintLogs.PrintJobReconciliationOutcome.QueueObserved\n                            ? MessageBoxImage.Information\n                            : MessageBoxImage.Warning);'
    ),
    (
        '            var details = report.Candidates.Count == 0\n                ? "No complete job record is available."\n                : string.Join(\n                    Environment.NewLine,\n                    report.Candidates.Select(candidate =>\n                        $"• {candidate.JobId} — {candidate.State} — {candidate.Action}\\n  {candidate.Reason}"));\n            var message = $"{report.UserFacingSummary}\\n\\n{details}";\n            if (report.StoreDiagnostics.Count > 0)\n            {\n                message += $"\\n\\nDiagnostics:\\n{string.Join(Environment.NewLine, report.StoreDiagnostics)}";\n            }\n\n            MessageBox.Show(\n                this,\n                message + "\\n\\nDo not retry automatically until the queue/job identity has been reconciled.",\n                "Print recovery review",\n                MessageBoxButton.OK,\n                MessageBoxImage.Warning);',
        '            var details = report.Candidates.Count == 0\n                ? "Không có bản ghi lệnh in hoàn chỉnh nào."\n                : string.Join(\n                    Environment.NewLine,\n                    report.Candidates.Select(candidate =>\n                        $"• {candidate.JobId} — {candidate.State} — {candidate.Action}\\n  {candidate.Reason}"));\n            var message = $"{report.UserFacingSummary}\\n\\n{details}";\n            if (report.StoreDiagnostics.Count > 0)\n            {\n                message += $"\\n\\nChẩn đoán:\\n{string.Join(Environment.NewLine, report.StoreDiagnostics)}";\n            }\n\n            MessageBox.Show(\n                this,\n                message + "\\n\\nKhông thử lại tự động cho đến khi định danh hàng đợi/lệnh in được đối soát.",\n                "Xem xét phục hồi in ấn",\n                MessageBoxButton.OK,\n                MessageBoxImage.Warning);'
    ),
    (
        '        var acknowledge = MessageBox.Show(\n            this,\n            $"Acknowledge {candidate.JobId} as reviewed?\\n\\nThis records an operator decision only; it does not mark the label printed.",\n            "Acknowledge print job",\n            MessageBoxButton.YesNo,\n            MessageBoxImage.Question);',
        '        var acknowledge = MessageBox.Show(\n            this,\n            $"Xác nhận {candidate.JobId} đã được xem xét?\\n\\nThao tác này chỉ ghi nhận quyết định của người vận hành, không đánh dấu nhãn đã in.",\n            "Xác nhận lệnh in",\n            MessageBoxButton.YesNo,\n            MessageBoxImage.Question);'
    ),
    (
        'MessageBox.Show(this, result.Summary, "Print recovery", MessageBoxButton.OK, MessageBoxImage.Information);',
        'MessageBox.Show(this, result.Summary, "Phục hồi in ấn", MessageBoxButton.OK, MessageBoxImage.Information);'
    ),
    (
        '        var voidChoice = MessageBox.Show(\n            this,\n            $"Void {candidate.JobId} in the durable history?\\n\\nNo printer command is sent, and the event history is retained.",\n            "Void uncertain print job",\n            MessageBoxButton.YesNo,\n            MessageBoxImage.Warning);',
        '        var voidChoice = MessageBox.Show(\n            this,\n            $"Hủy bỏ {candidate.JobId} trong lịch sử bền vững?\\n\\nKhông có lệnh máy in nào được gửi, và lịch sử sự kiện vẫn được giữ lại.",\n            "Hủy lệnh in không chắc chắn",\n            MessageBoxButton.YesNo,\n            MessageBoxImage.Warning);'
    ),
    (
        '        var reprintChoice = MessageBox.Show(\n            this,\n            $"Create a linked reprint request for {candidate.JobId}?\\n\\nA new Created job is recorded for review, but it is not prepared or dispatched automatically.",\n            "Request linked reprint",\n            MessageBoxButton.YesNo,\n            MessageBoxImage.Question);',
        '        var reprintChoice = MessageBox.Show(\n            this,\n            $"Tạo yêu cầu in lại liên kết cho {candidate.JobId}?\\n\\nMột lệnh mới sẽ được ghi nhận để xem xét, nhưng không tự động chuẩn bị hoặc phát lệnh.",\n            "Yêu cầu in lại liên kết",\n            MessageBoxButton.YesNo,\n            MessageBoxImage.Question);'
    ),
    (
        '                var approveChoice = MessageBox.Show(\n                    this,\n                    $"Review manifest {manifest.Fingerprint} for the linked child?\\n\\nApproval only records explicit operator consent. It does not print until the current template/data are matched and dispatched separately.",\n                    "Approve linked reprint",\n                    MessageBoxButton.YesNo,\n                    MessageBoxImage.Question);',
        '                var approveChoice = MessageBox.Show(\n                    this,\n                    $"Xem xét bản kê khai {manifest.Fingerprint} cho lệnh in liên kết?\\n\\nPhê duyệt chỉ ghi nhận sự đồng thuận rõ ràng của người vận hành. Lệnh sẽ không in cho đến khi mẫu tem/dữ liệu hiện tại khớp và được phát lệnh riêng.",\n                    "Phê duyệt in lại liên kết",\n                    MessageBoxButton.YesNo,\n                    MessageBoxImage.Question);'
    ),
    (
        '                        var openChoice = MessageBox.Show(\n                            this,\n                            "Open Print Preview to select the exact source rows for this approved child? The manifest guard will block any changed template/data.",\n                            "Choose reprint rows",\n                            MessageBoxButton.YesNo,\n                            MessageBoxImage.Question);',
        '                        var openChoice = MessageBox.Show(\n                            this,\n                            "Mở Xem trước khi in để chọn chính xác các dòng nguồn cho lệnh in đã duyệt này? Khung bảo vệ kê khai sẽ chặn nếu mẫu tem hoặc dữ liệu có thay đổi.",\n                            "Chọn dòng in lại",\n                            MessageBoxButton.YesNo,\n                            MessageBoxImage.Question);'
    ),
    (
        'MessageBox.Show(this, ex.Message, "Save failed", MessageBoxButton.OK, MessageBoxImage.Error);',
        'MessageBox.Show(this, ex.Message, "Lưu thất bại", MessageBoxButton.OK, MessageBoxImage.Error);'
    ),
    (
        '        var dialog = new OpenFileDialog\n        {\n            Title = "Open ANLAbel template",\n            Filter = "ANLAbel Template (*.anlabel)|*.anlabel|JSON (*.json)|*.json|All files (*.*)|*.*"\n        };',
        '        var dialog = new OpenFileDialog\n        {\n            Title = "Mở mẫu tem ANLAbel",\n            Filter = "Mẫu tem ANLAbel (*.anlabel)|*.anlabel|Tệp JSON (*.json)|*.json|Tất cả tệp (*.*)|*.*"\n        };'
    ),
    (
        '                MessageBox.Show(\n                    this,\n                    $"The selected template could not be read, so ANLAbel opened its last known-good backup.\\n\\nBackup: {backupPath}\\n\\nThe original file was not overwritten. Use Save As to create a new template after reviewing it.",\n                    "Template recovered",\n                    MessageBoxButton.OK,\n                    MessageBoxImage.Warning);',
        '                MessageBox.Show(\n                    this,\n                    $"Không thể đọc mẫu tem đã chọn, ANLAbel đã mở bản sao lưu tốt gần nhất.\\n\\nBản sao lưu: {backupPath}\\n\\nTệp gốc không bị ghi đè. Dùng lệnh Lưu thành để tạo mẫu tem mới sau khi xem xét.",\n                    "Đã khôi phục mẫu tem",\n                    MessageBoxButton.OK,\n                    MessageBoxImage.Warning);'
    ),
    (
        'MessageBox.Show(this, ex.Message, "Open failed", MessageBoxButton.OK, MessageBoxImage.Error);',
        'MessageBox.Show(this, ex.Message, "Mở thất bại", MessageBoxButton.OK, MessageBoxImage.Error);'
    ),
    (
        '            MessageBox.Show(\n                this,\n                "Save the current template first. Revision history is available for committed files only.",\n                "Revision history",\n                MessageBoxButton.OK,\n                MessageBoxImage.Information);',
        '            MessageBox.Show(\n                this,\n                "Vui lòng lưu mẫu tem hiện tại trước. Lịch sử sửa đổi chỉ khả dụng cho các tệp đã lưu trên đĩa.",\n                "Lịch sử sửa đổi",\n                MessageBoxButton.OK,\n                MessageBoxImage.Information);'
    ),
    (
        'MessageBox.Show(this, "Save the template first. Workflow audit is local to a committed file.", "Document Workflow", MessageBoxButton.OK, MessageBoxImage.Information);',
        'MessageBox.Show(this, "Vui lòng lưu mẫu tem trước. Nhật ký quy trình gắn liền với tệp đã lưu.", "Quy trình tài liệu", MessageBoxButton.OK, MessageBoxImage.Information);'
    ),
    (
        '        var dialog = new SaveFileDialog\n        {\n            Title = "Export print history to Excel",\n            Filter = "Excel Workbook (*.xlsx)|*.xlsx",\n            FileName = $"print-history-{DateTime.Now:yyyy-MM-dd}.xlsx"\n        };',
        '        var dialog = new SaveFileDialog\n        {\n            Title = "Xuất lịch sử in ra Excel",\n            Filter = "Sổ làm việc Excel (*.xlsx)|*.xlsx",\n            FileName = $"lich-su-in-{DateTime.Now:yyyy-MM-dd}.xlsx"\n        };'
    ),
    (
        '            var openReport = MessageBox.Show(\n                this,\n                $"Exported to {dialog.FileName}.\\n\\nOpen it now?",\n                "Export complete",\n                MessageBoxButton.YesNo,\n                MessageBoxImage.Information) == MessageBoxResult.Yes;',
        '            var openReport = MessageBox.Show(\n                this,\n                $"Đã xuất ra tệp {dialog.FileName}.\\n\\nBạn có muốn mở ngay bây giờ?",\n                "Xuất thành công",\n                MessageBoxButton.YesNo,\n                MessageBoxImage.Information) == MessageBoxResult.Yes;'
    ),
    (
        'MessageBox.Show(this, ex.Message, "Export failed", MessageBoxButton.OK, MessageBoxImage.Error);',
        'MessageBox.Show(this, ex.Message, "Xuất thất bại", MessageBoxButton.OK, MessageBoxImage.Error);'
    ),
    (
        'MessageBox.Show(this, ex.Message, "Template Library", MessageBoxButton.OK, MessageBoxImage.Error);',
        'MessageBox.Show(this, ex.Message, "Thư viện mẫu tem", MessageBoxButton.OK, MessageBoxImage.Error);'
    ),
    (
        '        var confirmed = MessageBox.Show(\n            this,\n            "Remove the Excel link from this template?\\n\\nObjects keep their field bindings but will show placeholders until you import data again.",\n            "Unlink Excel",\n            MessageBoxButton.YesNo,\n            MessageBoxImage.Question) == MessageBoxResult.Yes;',
        '        var confirmed = MessageBox.Show(\n            this,\n            "Hủy liên kết Excel khỏi mẫu tem này?\\n\\nCác đối tượng vẫn giữ trường liên kết nhưng sẽ hiển thị nội dung mẫu cho đến khi bạn nhập dữ liệu lại.",\n            "Hủy liên kết Excel",\n            MessageBoxButton.YesNo,\n            MessageBoxImage.Question) == MessageBoxResult.Yes;'
    ),
    (
        'MessageBox.Show(this, $"Print Preview is blocked: data transform error. {transformError}", "Preview blocked", MessageBoxButton.OK, MessageBoxImage.Warning);',
        'MessageBox.Show(this, $"Xem trước bị chặn: lỗi chuyển đổi dữ liệu. {transformError}", "Xem trước bị chặn", MessageBoxButton.OK, MessageBoxImage.Warning);'
    ),
    (
        '            MessageBox.Show(\n                this,\n                $"Print Preview could not be opened. {ex.Message}",\n                "Preview unavailable",\n                MessageBoxButton.OK,\n                MessageBoxImage.Error);',
        '            MessageBox.Show(\n                this,\n                $"Không thể mở Xem trước khi in. {ex.Message}",\n                "Xem trước không khả dụng",\n                MessageBoxButton.OK,\n                MessageBoxImage.Error);'
    ),
    (
        '        var choice = MessageBox.Show(\n            this,\n            $"{_viewModel.PrinterQueueStatusMessage}\\n\\nOpen Printer Setup now to choose a verified queue?",\n            "Printer queue unavailable",\n            MessageBoxButton.YesNo,\n            MessageBoxImage.Warning);',
        '        var choice = MessageBox.Show(\n            this,\n            $"{_viewModel.PrinterQueueStatusMessage}\\n\\nMở Cài đặt máy in ngay bây giờ để chọn hàng đợi đã xác thực?",\n            "Hàng đợi máy in không khả dụng",\n            MessageBoxButton.YesNo,\n            MessageBoxImage.Warning);'
    ),
    (
        'MessageBox.Show(this, "No Windows printers were found.", "Printer setup", MessageBoxButton.OK, MessageBoxImage.Information);',
        'MessageBox.Show(this, "Không tìm thấy máy in Windows nào.", "Cài đặt máy in", MessageBoxButton.OK, MessageBoxImage.Information);'
    ),
    (
        'MessageBox.Show(this, $"Cannot read installed printers.\\n\\n{ex.Message}", "Printer setup", MessageBoxButton.OK, MessageBoxImage.Warning);',
        'MessageBox.Show(this, $"Không thể đọc danh sách máy in đã cài đặt.\\n\\n{ex.Message}", "Cài đặt máy in", MessageBoxButton.OK, MessageBoxImage.Warning);'
    )
]

def main():
    import os
    base = os.path.join("src", "ANLAbel.App")
    tasks = [
        (os.path.join(base, "ExcelImportWindow.xaml.cs"), replacements_excel_import),
        (os.path.join(base, "DatabaseManagerWindow.xaml.cs"), replacements_db_manager),
        (os.path.join(base, "DataSourceCleanupWindow.xaml.cs"), replacements_ds_cleanup),
        (os.path.join(base, "NewTemplateWindow.xaml.cs"), replacements_new_template),
        (os.path.join(base, "TemplateRevisionWindow.xaml.cs"), replacements_revision),
        (os.path.join(base, "PrintCenterWindow.xaml.cs"), replacements_print_center),
        (os.path.join(base, "PrinterSetupWindow.xaml.cs"), replacements_printer_setup),
        (os.path.join(base, "PrintPreviewWindow.xaml.cs"), replacements_print_preview),
        (os.path.join(base, "MainWindow.xaml.cs"), replacements_main_window),
    ]

    all_ok = True
    for path, reps in tasks:
        ok = replace_in_file(path, reps)
        if not ok:
            all_ok = False
    return 0 if all_ok else 1

if __name__ == "__main__":
    import sys
    sys.exit(main())
