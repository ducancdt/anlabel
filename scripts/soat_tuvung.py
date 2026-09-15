#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""Soát từ vựng: liệt kê mọi từ Latin không-dấu còn sót trong chuỗi hiển thị của
các file XAML (hỗ trợ review thủ công, không phải cổng)."""
import io, re, glob, os

ALLOW = set(w.lower() for w in [
 "ANLAbel","GitHub","Excel","NiceLabel","Windows","USS","ISO","WPF",
 "CSV","QR","PDF","ZPL","GS1","DPI","ID","URL","RGB","HRI","ECC","ITF","FNC1","UTF",
 "mil","px","pt","mm","cm","kg","g","Ctrl","Alt","Shift","Del","Enter","Tab","Esc",
 "F1","F2","F3","F4","F5","F6","F7","F8","F9","F10","F11","F12",
 "x","y","b","u","i","l","r","t","x1","y1","x2","y2","GPL","cre","Code","Win32",
 "xlsx","xlsm","csv","JobId","Xprinter","XP",
])
MARKS="ăâđêôơưàảãáạằẳẵắặầẩẫấậèẻẽéẹềểễếệìỉĩíịòỏõóọồổỗốộờởỡớợùủũúụừửữứựỳỷỹýỵĂÂĐÊÔƠƯÀẢÃÁẠẰẲẴẮẶẦẨẪẤẬÈẺẼÉẸỀỂỄẾỆÌỈĨÍỊÒỎÕÓỌỒỔỖỐỘỜỞỠỚỢÙỦŨÚỤỪỬỮỨỰỲỶỸÝỴ"
ATTR=re.compile(r'(Content|Header|Text|ToolTip|Title|Watermark|Description|PlaceholderText)="([^"{}][^"{}]*)"')
found={}
for path in sorted(glob.glob("src/ANLAbel.App/*.xaml")):
    for i,line in enumerate(io.open(path,encoding="utf-8"),1):
        for m in ATTR.finditer(line):
            val=m.group(2)
            dec=val.replace("&amp;","&").replace("&gt;",">").replace("&lt;","<")
            for phrase in ["Data Matrix","DataMatrix","Print Schema","DriverManaged"]:
                dec=dec.replace(phrase," ")
            for w in re.findall(r"[A-Za-z]+",dec):
                if len(w)>=2 and w.lower() not in ALLOW and not any(c in MARKS for c in w):
                    found.setdefault(w,[]).append(f"{os.path.basename(path)}:{i}")
for w in sorted(found,key=lambda x:(-len(found[x]),x.lower())):
    print(f"{len(found[w]):3}  {w:16} {found[w][0]}")
