#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""Sửa từ ngữ chưa hợp lý / lệch ngôn ngữ. Thay chuỗi duy nhất (cả câu) nên an toàn."""
import io, sys, os

FIX = {
 "src/ANLAbel.App/MainWindow.xaml": [
   ("từ file Excel và trang tính đã liên kết", "từ tệp Excel và trang tính đã liên kết"),
   ("Dùng lại một file/trang Excel cho nhiều mẫu tem.", "Dùng lại một tệp/trang Excel cho nhiều mẫu tem."),
   ("Kiểm tra bản cập nhật mới từ GitHub Releases", "Kiểm tra bản cập nhật mới từ các bản phát hành trên GitHub"),
   (">docked<", ">cố định<"),
   ("Text=\"docked\"", "Text=\"cố định\""),
   ("Nhập cỡ theo point như Excel, hoặc chọn một cỡ chuẩn.", "Nhập cỡ theo pt như Excel, hoặc chọn một cỡ chuẩn."),
   ("hoặc nhập chiều cao hộp dòng tối thiểu theo point.", "hoặc nhập chiều cao hộp dòng tối thiểu theo pt."),
   ("giữ màu/alpha cho trình điều khiển máy in", "giữ màu/độ trong suốt cho trình điều khiển máy in"),
 ],
 "src/ANLAbel.App/DatabaseManagerWindow.xaml": [
   ("Dùng lại một file/trang Excel cho nhiều mẫu tem.", "Dùng lại một tệp/trang Excel cho nhiều mẫu tem."),
   ("có file Excel bị thiếu và không dùng quá 30 ngày", "có tệp Excel bị thiếu và không dùng quá 30 ngày"),
   ("Text=\"Excel file\"", "Text=\"Tệp Excel\""),  # phòng khi chưa dịch
   (">File Excel<", ">Tệp Excel<"),
   ("Text=\"File Excel\"", "Text=\"Tệp Excel\""),
 ],
 "src/ANLAbel.App/PrintCenterWindow.xaml": [
   ("hoặc tìm máy in, spool, hàng đợi, hoặc bản kê", "hoặc tìm máy in, bộ đệm in, hàng đợi, hoặc bản kê"),
   ("Header=\"Spool\"", "Header=\"Bộ đệm in\""),
 ],
 "src/ANLAbel.App/AutomationWindow.xaml": [
   ("Bắt đầu theo dõi chỉ-phát-hiện", "Bắt đầu theo dõi chỉ phát hiện"),
   ("hợp đồng năng lực, xác nhận, hết giờ và kết quả bền riêng", "hợp đồng riêng về năng lực, xác nhận, thời gian chờ và kết quả lưu bền"),
 ],
 "src/ANLAbel.App/PrintQueueConsoleWindow.xaml": [
   ("Chưa dùng được cho tới khi có hợp đồng năng lực, xác nhận, hết giờ và kết quả bền riêng.",
    "Chưa dùng được cho tới khi có hợp đồng riêng về năng lực, xác nhận, thời gian chờ và kết quả lưu bền."),
 ],
 "src/ANLAbel.App/AnalyticsWindow.xaml": [
   ("Lỗi / chưa chắc", "Lỗi / không chắc chắn"),
 ],
 "src/ANLAbel.App/TemplateRevisionWindow.xaml": [
   ("Header=\"Băm (tiền tố)\"", "Header=\"Mã băm (tiền tố)\""),
 ],
 "src/ANLAbel.App/OperationsOverviewWindow.xaml": [
   ("phục hồi in bền.", "phục hồi in lưu bền."),
 ],
 "src/ANLAbel.App/PrintHistoryWindow.xaml": [
   ("Nguồn gốc chỉ đọc theo vòng đời bền,", "Nguồn gốc chỉ đọc theo vòng đời lưu bền,"),
 ],
 "src/ANLAbel.App/PrintCenterWindow.xaml_2": [],  # placeholder
 "src/ANLAbel.App/UpdateWindow.xaml": [
   ("Kiểm tra phiên bản mới từ GitHub Release", "Kiểm tra phiên bản mới từ bản phát hành trên GitHub"),
   ("Nhật ký thay đổi (Release Notes):", "Nhật ký thay đổi:"),
 ],
}

total = 0
miss = []
for path, pairs in FIX.items():
    if path.endswith("_2"):
        continue
    if not os.path.exists(path):
        continue
    with io.open(path, "r", encoding="utf-8", newline="") as f:
        s = f.read()
    for old, new in pairs:
        if old in s:
            s = s.replace(old, new)
            total += 1
        else:
            miss.append(f"{os.path.basename(path)} :: {old}")
    with io.open(path, "w", encoding="utf-8", newline="") as f:
        f.write(s)
with io.open("scripts/vh_result.txt", "w", encoding="utf-8") as f:
    f.write(f"applied {total}\n")
    for m in miss:
        f.write("MISS " + m + "\n")
