#!/usr/bin/env python3
# -*- coding: utf-8 -*-
import glob, re

english_words = ['Please', 'Select', 'Cancel', 'failed', 'blocked', 'unavailable', 'recovered', 'Print', 'Save', 'Open', 'Import', 'Export']
found_any = False
for f in sorted(glob.glob('src/ANLAbel.App/**/*.cs', recursive=True)):
    with open(f, 'r', encoding='utf-8') as fp:
        txt = fp.read()
    for m in re.finditer(r'MessageBox\.Show\s*\((.*?)\);', txt, re.DOTALL):
        snippet = m.group(0)
        literals = re.findall(r'"([^"]*)"', snippet)
        combined = ' '.join(literals)
        # ignore technical symbols/tokens
        found = [w for w in english_words if re.search(r'\b' + w + r'\b', combined, re.IGNORECASE)]
        if found:
            found_any = True
            print(f + ' : ' + str(found))
            print('  Literals: ' + repr(combined[:120]))
if not found_any:
    print('ALL MessageBox.Show strings in ANLAbel.App are 100% Vietnamese!')
