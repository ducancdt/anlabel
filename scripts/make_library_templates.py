import json
import os
import uuid

out_dir = os.path.join(os.path.dirname(__file__), "..", "src", "ANLAbel.App", "TemplateLibrary")


def gid():
    return uuid.uuid4().hex


def style(size=8, bold=False, align="Left", border=0.0, outline="None"):
    return {
        "FontFamily": "Arial",
        "FontSizePt": size,
        "Bold": bold,
        "Italic": False,
        "Underline": False,
        "Alignment": align,
        "BorderThicknessMm": border,
        "OutlineStyle": outline,
        "FillStyle": "None",
        "CornerRadiusMm": 0,
        "FillColor": "#00FFFFFF",
        "StrokeColor": "#FF000000",
    }


def obj(
    typ,
    name,
    x,
    y,
    w,
    h,
    text="",
    bind="",
    z=5,
    size=8,
    bold=False,
    align="Left",
    border=0.0,
    outline="None",
    line_ex=0,
    line_ey=0,
    show_hri=False,
    bc="Code128",
):
    return {
        "Id": gid(),
        "Type": typ,
        "Name": name,
        "XMm": round(x, 2),
        "YMm": round(y, 2),
        "WidthMm": round(w, 2),
        "HeightMm": round(h, 2),
        "LineEndXMm": round(line_ex, 2),
        "LineEndYMm": round(line_ey, 2),
        "Rotation": 0,
        "ZIndex": z,
        "IsLocked": False,
        "IsVisible": True,
        "BindingExpression": bind,
        "Text": text,
        "BarcodeSymbology": bc,
        "QrSizingMode": "AutoSizeByData",
        "QrErrorCorrection": "M",
        "QrFixedVersion": 1,
        "QrModuleSizePx": 3 if typ == "QRCode" else 6,
        "QrQuietZoneModules": 2 if typ == "QRCode" else 4,
        "QrDpi": 203 if typ == "QRCode" else 300,
        "Style": style(size, bold, align, border, outline),
        "ShowBarcodeText": show_hri,
        "BarcodeTextFontSizePt": 7,
    }


def line(name, x1, y1, x2, y2, z=1):
    return obj(
        "Line",
        name,
        min(x1, x2),
        min(y1, y2),
        abs(x2 - x1) or 0.5,
        abs(y2 - y1) or 0.5,
        z=z,
        border=0.25,
        outline="Solid",
        line_ex=x2,
        line_ey=y2,
    )


def rect(name, x, y, w, h, z=1):
    return obj("Rectangle", name, x, y, w, h, z=z, border=0.25, outline="Solid")


def txt(name, x, y, w, h, text, bind="", size=8, bold=False, align="Left", z=5):
    return obj("Text", name, x, y, w, h, text=text, bind=bind, size=size, bold=bold, align=align, z=z)


def tbox(name, x, y, w, h, text, bind="", size=8, bold=False):
    return obj("TextBox", name, x, y, w, h, text=text, bind=bind, size=size, bold=bold, z=5)


def bc128(name, x, y, w, h, bind, text="000000"):
    return obj("BarcodeCode128", name, x, y, w, h, text=text, bind=bind, z=6, show_hri=False, bc="Code128")


def qr(name, x, y, s, bind, text="QRData"):
    return obj("QRCode", name, x, y, s, s, text=text, bind=bind, z=6, bc="QRCode")


def template(name, w, h, orient, objects, key="PartNo"):
    return {
        "Id": gid(),
        "Name": name,
        "WidthMm": w,
        "HeightMm": h,
        "Dpi": 203,
        "GapMm": 0,
        "MarginMm": 1,
        "Orientation": orient,
        "Objects": objects,
        "PrinterProfile": {
            "PrinterName": "",
            "PaperName": f"{w} × {h} mm",
            "SettingsSource": "Label",
            "PaperSizeSource": "DriverAutomatic",
            "MediaType": "Gap",
            "FeedDirection": "TopToBottom",
            "Rotated180": False,
            "Dpi": 203,
            "LabelWidthMm": w,
            "LabelHeightMm": h,
            "GapMm": 0,
            "OffsetXMm": 0,
            "OffsetYMm": 0,
            "ScaleX": 1,
            "ScaleY": 1,
            "PhysicalWidthMm": w,
            "PhysicalHeightMm": h,
        },
        "DatabaseConfig": {"FilePath": "", "SheetName": "", "KeyField": key},
    }


def save(fname, data):
    path = os.path.join(out_dir, fname)
    with open(path, "w", encoding="utf-8", newline="\n") as handle:
        json.dump(data, handle, ensure_ascii=False, indent=2)
        handle.write("\n")
    print("wrote", fname, "objects", len(data["Objects"]), "size", data["WidthMm"], "x", data["HeightMm"])


save(
    "25_box_75x100_parts.anlabel",
    template(
        "Parts Box Label 75×100",
        75,
        100,
        "Portrait",
        [
            rect("border", 1.5, 1.5, 72, 97),
            txt("hdr: Company", 3, 2.2, 46, 7, "COMPANY NAME", size=14, bold=True),
            line("div-header", 1.5, 10.5, 73.5, 10.5),
            txt("lbl: PN", 3, 11.2, 18, 4, "P/N:", size=9, bold=True),
            txt("val: PartNo", 20, 11.2, 30, 4, "000000", "{PartNo}", size=9, bold=True),
            txt("lbl: Qty", 52, 11.2, 10, 4, "Q'ty:", size=9, bold=True),
            txt("val: Qty", 62, 11.2, 11, 8, "0", "{Qty}", size=16, bold=True, align="Center"),
            bc128("bc: PartNo", 3, 16, 46, 10, "{PartNo}"),
            line("div-pn", 1.5, 27.5, 73.5, 27.5),
            line("v-qty", 51, 10.5, 51, 27.5),
            txt("lbl: Name", 3, 28.5, 16, 4, "P/Name:", size=8, bold=True),
            tbox("val: PartName", 3, 32.5, 69, 14, "PART NAME", "{PartName}", size=8),
            line("div-name", 1.5, 48, 73.5, 48),
            txt("lbl: PO", 3, 49, 10, 4, "PO:", size=8, bold=True),
            txt("val: PO", 14, 49, 56, 4, "000000", "{PO}", size=9, bold=True),
            line("div-po", 1.5, 55, 73.5, 55),
            txt("lbl: Lot", 3, 56, 10, 4, "Lot:", size=8, bold=True),
            txt("val: Lot", 14, 56, 56, 4, "000000", "{Lot}", size=9, bold=True),
            line("div-lot", 1.5, 62, 73.5, 62),
            txt("lbl: Ship", 3, 63, 18, 4, "Ship Date:", size=8, bold=True),
            txt("val: ShipDate", 22, 63, 28, 4, "0000-00-00", "{ShipDate}", size=9, bold=True),
            qr("qr: QRData", 50, 70, 22, "{QRData}"),
        ],
    ),
)

save(
    "26_carton_100x75_trace.anlabel",
    template(
        "Carton Trace Label 100×75",
        100,
        75,
        "Landscape",
        [
            rect("border", 1.5, 1.5, 97, 72),
            txt("lbl: Part", 3, 2.5, 14, 4, "Part", size=9, bold=True),
            txt("val: PartNo", 18, 2.5, 50, 4, "000000", "{PartNo}", size=9, bold=True),
            bc128("bc: PartNo", 18, 7, 50, 6, "{PartNo}"),
            line("r1", 1.5, 14.5, 98.5, 14.5),
            txt("lbl: Qty", 3, 15.5, 14, 4, "Q'ty", size=9, bold=True),
            txt("val: Qty", 18, 15.5, 30, 4, "0", "{Qty}", size=9, bold=True),
            bc128("bc: Qty", 18, 20, 36, 6, "{Qty}"),
            line("r2", 1.5, 27.5, 98.5, 27.5),
            txt("lbl: PO", 3, 28.5, 14, 4, "PO", size=9, bold=True),
            txt("val: PO", 18, 28.5, 50, 4, "000000", "{PO}", size=9, bold=True),
            bc128("bc: PO", 18, 33, 50, 6, "{PO}"),
            line("r3", 1.5, 40.5, 98.5, 40.5),
            txt("lbl: Lot", 3, 41.5, 14, 4, "Lot", size=9, bold=True),
            txt("val: Lot", 18, 41.5, 40, 4, "000000", "{Lot}", size=9, bold=True),
            txt("lbl: Mfg", 62, 41.5, 10, 4, "Mfg", size=8, bold=True),
            txt("val: Mfg", 72, 41.5, 24, 4, "SUPPLIER NAME", "{SupplierName}", size=8),
            bc128("bc: Lot", 18, 46, 40, 6, "{Lot}"),
            line("r4", 1.5, 53.5, 98.5, 53.5),
            txt("lbl: MPN", 3, 54.5, 14, 4, "MPN", size=9, bold=True),
            txt("val: MPN", 18, 54.5, 50, 4, "000000", "{MPN}", size=9, bold=True),
            bc128("bc: MPN", 18, 59, 44, 6, "{MPN}"),
            txt("lbl: Date", 68, 54.5, 14, 4, "Date Code", size=8, bold=True),
            txt("val: DateCode", 68, 59, 28, 4, "0000", "{DateCode}", size=9, bold=True),
            txt("lbl: Origin", 68, 64, 14, 4, "Origin", size=8, bold=True),
            txt("val: Origin", 68, 68, 28, 4, "COUNTRY", "{Origin}", size=8),
        ],
    ),
)

save(
    "27_bag_75x85_trace.anlabel",
    template(
        "Bag Trace Label 75×85",
        75,
        85,
        "Portrait",
        [
            rect("border", 1.5, 1.5, 72, 82),
            txt("lbl: Part", 3, 3, 14, 4, "Part", size=9, bold=True),
            txt("val: PartNo", 18, 3, 54, 4, "000000", "{PartNo}", size=9, bold=True),
            bc128("bc: PartNo", 3, 8, 52, 8, "{PartNo}"),
            line("r1", 1.5, 18, 73.5, 18),
            txt("lbl: Qty", 3, 19.5, 14, 4, "Q'ty", size=9, bold=True),
            txt("val: Qty", 18, 19.5, 30, 4, "0", "{Qty}", size=10, bold=True),
            bc128("bc: Qty", 3, 24.5, 40, 7, "{Qty}"),
            line("r2", 1.5, 33, 73.5, 33),
            txt("lbl: Lot", 3, 34.5, 14, 4, "Lot", size=9, bold=True),
            txt("val: Lot", 18, 34.5, 52, 4, "000000", "{Lot}", size=9, bold=True),
            bc128("bc: Lot", 3, 39.5, 40, 7, "{Lot}"),
            line("r3", 1.5, 48, 73.5, 48),
            txt("lbl: MPN", 3, 49.5, 14, 4, "MPN", size=9, bold=True),
            txt("val: MPN", 18, 49.5, 52, 4, "000000", "{MPN}", size=9, bold=True),
            bc128("bc: MPN", 3, 54.5, 44, 7, "{MPN}"),
            line("r4", 1.5, 63, 73.5, 63),
            txt("lbl: Mfg", 3, 64.5, 14, 4, "Mfg", size=9, bold=True),
            txt("val: Mfg", 18, 64.5, 52, 4, "SUPPLIER NAME", "{SupplierName}", size=8),
            txt("lbl: Date", 3, 72, 18, 4, "Date Code", size=8, bold=True),
            txt("val: DateCode", 22, 72, 20, 4, "0000", "{DateCode}", size=9, bold=True),
            txt("lbl: RoHS", 58, 72, 14, 8, "RoHS", size=9, bold=True, align="Center"),
        ],
    ),
)

save(
    "28_box_150x100_trace.anlabel",
    template(
        "Shipping Box Label 150×100",
        150,
        100,
        "Landscape",
        [
            rect("border", 2, 2, 146, 96),
            txt("hdr: Company", 4, 3, 90, 7, "COMPANY NAME", size=16, bold=True),
            txt("lbl: Origin", 110, 3, 16, 4, "Origin", size=8, bold=True),
            txt("val: Origin", 110, 7, 34, 5, "COUNTRY", "{Origin}", size=10, bold=True),
            line("h1", 2, 12, 148, 12),
            txt("lbl: Part", 4, 13.5, 16, 4, "Part", size=9, bold=True),
            txt("val: PartNo", 22, 13.5, 70, 4, "000000", "{PartNo}", size=11, bold=True),
            bc128("bc: PartNo", 22, 18.5, 70, 10, "{PartNo}"),
            txt("lbl: Qty", 100, 13.5, 14, 4, "Q'ty", size=9, bold=True),
            txt("val: Qty", 114, 13.5, 30, 8, "0", "{Qty}", size=16, bold=True),
            bc128("bc: Qty", 100, 23, 44, 8, "{Qty}"),
            line("h2", 2, 33, 148, 33),
            txt("lbl: PO", 4, 34.5, 16, 4, "PO", size=9, bold=True),
            txt("val: PO", 22, 34.5, 70, 4, "000000", "{PO}", size=10, bold=True),
            bc128("bc: PO", 22, 39.5, 70, 8, "{PO}"),
            txt("lbl: Lot", 100, 34.5, 16, 4, "Lot", size=9, bold=True),
            txt("val: Lot", 114, 34.5, 32, 4, "000000", "{Lot}", size=10, bold=True),
            bc128("bc: Lot", 100, 39.5, 44, 8, "{Lot}"),
            line("h3", 2, 50, 148, 50),
            txt("lbl: MPN", 4, 51.5, 16, 4, "MPN", size=9, bold=True),
            txt("val: MPN", 22, 51.5, 70, 4, "000000", "{MPN}", size=10, bold=True),
            bc128("bc: MPN", 22, 56.5, 70, 8, "{MPN}"),
            txt("lbl: Date", 100, 51.5, 20, 4, "Date Code", size=8, bold=True),
            txt("val: DateCode", 100, 56.5, 24, 4, "0000", "{DateCode}", size=10, bold=True),
            line("h4", 2, 67, 148, 67),
            txt("lbl: Name", 4, 68.5, 20, 4, "P/Name:", size=8, bold=True),
            tbox("val: PartName", 4, 73, 90, 22, "PART NAME", "{PartName}", size=9),
            qr("qr: QRData", 118, 70, 26, "{QRData}"),
        ],
    ),
)

save(
    "29_shipping_100x150_typed.anlabel",
    template(
        "Typed Shipping Label 100×150",
        100,
        150,
        "Portrait",
        [
            txt("hdr: Company", 3, 2, 48, 7, "COMPANY NAME", size=14, bold=True),
            txt("hdr: Company2", 3, 9, 48, 10, "SUPPLIER NAME", "{SupplierName}", size=7),
            txt("lbl: Type", 54, 2, 28, 4, "LABEL TYPE:", size=9, bold=True),
            txt("val: Type", 84, 2, 14, 12, "S", "{LabelType}", size=28, bold=True, align="Center"),
            txt("lbl: ShipTo", 54, 16, 28, 3.5, "Delivery Address:", size=7, bold=True),
            tbox("val: ShipTo", 54, 19.5, 43, 12, "CUSTOMER NAME", "{ShipTo}", size=7),
            line("h1", 2, 33, 98, 33),
            txt("lbl: PO", 3, 34, 28, 3.5, "PO Number:", size=8, bold=True),
            bc128("bc: PO", 48, 34, 48, 7, "{PO}"),
            txt("val: PO", 48, 41.2, 48, 3.5, "000000", "{PO}", size=8, align="Center"),
            txt("lbl: SupplierNo", 3, 45.5, 32, 3.5, "Supplier number:", size=8, bold=True),
            txt("lbl: Serial", 52, 45.5, 28, 3.5, "Serial number:", size=8, bold=True),
            bc128("bc: SupplierNo", 3, 49.5, 44, 7, "{SupplierNo}"),
            bc128("bc: Serial", 52, 49.5, 44, 7, "{Serial}"),
            txt("val: SupplierNo", 3, 56.8, 44, 3.5, "000000", "{SupplierNo}", size=8, align="Center"),
            txt("val: Serial", 52, 56.8, 44, 3.5, "000000", "{Serial}", size=8, align="Center"),
            txt("lbl: PartNo", 3, 61, 36, 3.5, "Part Number:", size=8, bold=True),
            txt("lbl: Qty", 52, 61, 20, 3.5, "Q'ty:", size=8, bold=True),
            bc128("bc: PartNo", 3, 65, 44, 7, "{PartNo}"),
            bc128("bc: Qty", 52, 65, 28, 7, "{Qty}"),
            txt("val: PartNo", 3, 72.2, 44, 3.5, "000000", "{PartNo}", size=8, align="Center"),
            txt("val: Qty", 52, 72.2, 20, 3.5, "0", "{Qty}", size=8, align="Center"),
            txt("val: Uom", 74, 72.2, 12, 3.5, "PCS", "{Uom}", size=8, bold=True),
            txt("lbl: MPN", 3, 76.5, 40, 3.5, "Supplier part number:", size=8, bold=True),
            txt("lbl: Lot", 52, 76.5, 28, 3.5, "LOT number:", size=8, bold=True),
            bc128("bc: MPN", 3, 80.5, 44, 7, "{MPN}"),
            bc128("bc: Lot", 52, 80.5, 44, 7, "{Lot}"),
            txt("val: MPN", 3, 87.8, 44, 3.5, "000000", "{MPN}", size=8, align="Center"),
            txt("val: Lot", 52, 87.8, 44, 3.5, "000000", "{Lot}", size=8, align="Center"),
            txt("lbl: Desc", 3, 92, 28, 3.5, "Description:", size=8, bold=True),
            tbox("val: Desc", 3, 95.5, 94, 8, "PART NAME", "{Description}", size=8),
            txt("lbl: DN", 3, 104.5, 40, 3.5, "Delivery Note Number:", size=8, bold=True),
            bc128("bc: DN", 3, 108.5, 58, 8, "{DeliveryNote}"),
            txt("val: DN", 3, 116.8, 58, 3.5, "000000", "{DeliveryNote}", size=8, align="Center"),
            qr("qr: QRData", 68, 104.5, 28, "{QRData}"),
            txt("lbl: MfgDate", 3, 121, 22, 3.5, "MFG Date:", size=8, bold=True),
            bc128("bc: MfgDate", 3, 125, 40, 7, "{MfgDate}"),
            txt("val: MfgDate", 3, 132.2, 40, 3.5, "0000-00-00", "{MfgDate}", size=8, align="Center"),
            txt("lbl: Net", 46, 121, 18, 3.5, "Net w:", size=8, bold=True),
            txt("val: Net", 46, 125, 18, 5, "0", "{NetWeight}", size=12, bold=True),
            txt("lbl: Gross", 46, 131, 18, 3.5, "Gross w:", size=8, bold=True),
            txt("val: Gross", 46, 135, 18, 5, "0", "{GrossWeight}", size=12, bold=True),
        ],
        key="Serial",
    ),
)
