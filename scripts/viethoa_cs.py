#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""Thay thế chuỗi hiển thị tiếng Anh sang tiếng Việt trong mã nguồn C#."""
import sys, io

def replace_in_file(path, replacements):
    with io.open(path, "r", encoding="utf-8", newline="") as f:
        raw = f.read()

    is_crlf = "\r\n" in raw
    content = raw.replace("\r\n", "\n")

    misses = []
    for old, new in replacements:
        old_norm = old.replace("\r\n", "\n")
        new_norm = new.replace("\r\n", "\n")
        if old_norm not in content:
            misses.append(old_norm)
        else:
            content = content.replace(old_norm, new_norm, 1)

    if is_crlf:
        content = content.replace("\n", "\r\n")

    with io.open(path, "w", encoding="utf-8", newline="") as f:
        f.write(content)

    if misses:
        print(f"MISS in {path}:")
        for m in misses:
            print(f"  - {repr(m)}")
        return False
    print(f"OK: {path} ({len(replacements)} replacements)")
    return True
