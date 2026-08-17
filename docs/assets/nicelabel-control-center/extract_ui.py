import fitz
from pathlib import Path

pdf_path = Path(r"H:\00_REPOS_PROJECTS\ANLABEL\ug-NiceLabel_Control_Center-en.pdf")
img_dir = Path(r"H:\00_REPOS_PROJECTS\ANLABEL\docs\assets\nicelabel-control-center\ui-screens")
img_dir.mkdir(parents=True, exist_ok=True)
doc = fitz.open(pdf_path)
print("pages", doc.page_count)

# Key UI pages from TOC (1-based PDF pages for screenshots of product UI)
# Prefer middle content pages that typically have screenshots
key_pages = sorted(set([
    1, 8, 9, 15, 18, 24, 25, 28, 30, 31, 38, 40, 43, 46, 54, 55, 56, 57, 58,
    62, 65, 66, 71, 72, 75, 79, 80, 81, 87, 90, 101, 103, 104, 105, 112, 114,
    119, 120, 123, 125, 127, 128, 130, 134, 136, 137, 139, 141, 146, 148
]))

# Render each key page at 1.5x for readable UI
saved = []
for pno in key_pages:
    if pno < 1 or pno > doc.page_count:
        continue
    page = doc[pno - 1]
    mat = fitz.Matrix(1.6, 1.6)
    pix = page.get_pixmap(matrix=mat, alpha=False)
    out = img_dir / f"page-{pno:03d}.png"
    pix.save(str(out))
    saved.append((pno, out.name, out.stat().st_size))

# Also extract large embedded images (screenshots) across all pages
emb = 0
for pno in range(doc.page_count):
    for img in doc.get_page_images(pno, full=True):
        xref = img[0]
        w, h = img[2], img[3]
        if w < 200 or h < 100:
            continue
        try:
            pix = fitz.Pixmap(doc, xref)
            if pix.n - pix.alpha >= 4:  # CMYK
                pix = fitz.Pixmap(fitz.csRGB, pix)
            out = img_dir / f"embed-p{pno+1:03d}-x{xref}-{w}x{h}.png"
            pix.save(str(out))
            emb += 1
        except Exception as e:
            pass

print("key_pages_rendered", len(saved))
print("embedded_large", emb)
print("total_files", len(list(img_dir.glob('*.png'))))
# top 15 largest
files = sorted(img_dir.glob('*.png'), key=lambda p: p.stat().st_size, reverse=True)[:15]
for f in files:
    print(f"{f.name}\t{f.stat().st_size}")
