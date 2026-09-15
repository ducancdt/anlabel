#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""Việt hóa chung — neo (số dòng, thuộc tính, giá trị cũ) → giá trị mới.
Dùng: python scripts/viethoa_apply.py <đường_dẫn_xaml> <đường_dẫn_map.py>
map.py phải định nghĩa R = [(line, attr, old, new), ...]."""
import sys, io, importlib.util

def load_map(mappath):
    spec = importlib.util.spec_from_file_location("vhmap", mappath)
    mod = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(mod)
    return mod.R

def main():
    path, mappath = sys.argv[1], sys.argv[2]
    R = load_map(mappath)
    with io.open(path, "r", encoding="utf-8", newline="") as f:
        lines = f.read().split("\n")
    misses = []
    for (ln, attr, old, new) in R:
        idx = ln - 1
        needle = f'{attr}="{old}"'
        repl   = f'{attr}="{new}"'
        applied = False
        for j in ([idx] + list(range(max(0, idx-3), min(len(lines), idx+4)))):
            if 0 <= j < len(lines) and needle in lines[j]:
                lines[j] = lines[j].replace(needle, repl, 1)
                applied = True
                break
        if not applied:
            misses.append((ln, attr, old))
    with io.open(path, "w", encoding="utf-8", newline="") as f:
        f.write("\n".join(lines))
    ok = len(R) - len(misses)
    with io.open("scripts/vh_result.txt", "w", encoding="utf-8") as f:
        f.write(f"applied {ok}/{len(R)}\n")
        for m in misses:
            f.write(f"MISS {m}\n")
    sys.exit(1 if misses else 0)

if __name__ == "__main__":
    main()
