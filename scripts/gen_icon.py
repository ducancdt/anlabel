import struct, zlib, os

width, height = 64, 64
pixels = []
for y in range(height):
    for x in range(width):
        radius = 10
        in_rect = True
        if x < radius and y < radius:
            in_rect = (x - radius)**2 + (y - radius)**2 <= radius**2
        elif x >= width-radius and y < radius:
            in_rect = (x - width + radius + 1)**2 + (y - radius)**2 <= radius**2
        elif x < radius and y >= height-radius:
            in_rect = (x - radius)**2 + (y - height + radius + 1)**2 <= radius**2
        elif x >= width-radius and y >= height-radius:
            in_rect = (x - width + radius + 1)**2 + (y - height + radius + 1)**2 <= radius**2
        
        if not in_rect:
            pixels.extend([0, 0, 0, 0])
        else:
            is_a = False
            if 14 <= x <= 32 and 8 <= y <= 44:
                expected_x = 14 + (32-14) * (44-y) / (44-8)
                if abs(x - expected_x) < 3.5:
                    is_a = True
            if 32 <= x <= 50 and 8 <= y <= 44:
                expected_x = 50 - (50-34) * (44-y) / (44-8)
                if abs(x - expected_x) < 3.5:
                    is_a = True
            if 20 <= y <= 24 and 19 <= x <= 45:
                is_a = True
            
            if is_a:
                pixels.extend([255, 255, 255, 255])
            else:
                pixels.extend([20, 100, 210, 255])

def create_png(w, h, rgba_data):
    def chunk(ctype, data):
        c = ctype + data
        crc = struct.pack('>I', zlib.crc32(c) & 0xffffffff)
        return struct.pack('>I', len(data)) + c + crc
    raw = b''
    for y in range(h):
        raw += b'\x00'
        row_start = y * w * 4
        raw += rgba_data[row_start:row_start + w * 4]
    compressed = zlib.compress(raw)
    png = b'\x89PNG\r\n\x1a\n'
    png += chunk(b'IHDR', struct.pack('>IIBBBBB', w, h, 8, 6, 0, 0, 0))
    png += chunk(b'IDAT', compressed)
    png += chunk(b'IEND', b'')
    return png

png_data = create_png(width, height, bytes(pixels))
ico = struct.pack('<HHH', 0, 1, 1)
png_size = len(png_data)
ico += struct.pack('<BBBBHHII', width, height, 0, 0, 1, 32, png_size, 22)
ico += png_data

out_path = os.path.join(os.path.dirname(__file__), '..', 'src', 'ANLAbel.App', 'anlabel.ico')
out_path = os.path.normpath(out_path)
with open(out_path, 'wb') as f:
    f.write(ico)
print(f'Created {out_path} ({len(ico)} bytes), exists={os.path.exists(out_path)}')