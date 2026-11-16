# -*- coding: utf-8 -*-
"""그림 전·뒤 비교 모음 (11-16 1차 수정안 아트 퀄업 때 만듦).

사용: PYTHONUTF8=1 python tools/art_compare.py <예전 Art 폴더> <새 Art 폴더> <출력 폴더>
  → 출력 폴더에 units_before/after.png (가디언 + 터렛 6) · fx_before/after.png (탄 · 효과) · enemy_before/after.png
  예전 Art 폴더 = 고치기 전에 스크래치에 백업해 둔 Resources/Art
"""
import os, sys
from PIL import Image

BG = (46, 56, 102, 255)


def row(paths, scale=1.0, gap=16, h_min=0):
    ims = []
    for p in paths:
        if not os.path.exists(p):
            continue
        im = Image.open(p).convert('RGBA')
        if scale != 1:
            im = im.resize((int(im.width * scale), int(im.height * scale)), Image.LANCZOS)
        ims.append(im)
    W = sum(i.width for i in ims) + gap * (len(ims) + 1)
    H = max([i.height for i in ims] + [h_min]) + gap * 2
    s = Image.new('RGBA', (W, H), BG); x = gap
    for i in ims:
        s.alpha_composite(i, (x, H - gap - i.height)); x += i.width + gap
    return s


def sheet(art, out_dir, tag):
    T = ['rail', 'laser', 'drone', 'hive', 'missile', 'bolt']
    row([os.path.join(art, 'Char', 'guardian.png')] + [os.path.join(art, 'Turret', t + '.png') for t in T], .6).save(os.path.join(out_dir, f'units_{tag}.png'))
    F = ['bullet', 'bullet_charge', 'shell', 'missile', 'mini_missile', 'bee', 'hit', 'burst']
    row([os.path.join(art, 'Fx', f + '.png') for f in F], 1.6).save(os.path.join(out_dir, f'fx_{tag}.png'))
    E = ['pebble', 'fire', 'ice', 'iron', 'crack', 'moth', 'beetle', 'spider', 'jelly', 'split', 'ufo', 'sdrone']
    row([os.path.join(art, 'Enemy', e + '.png') for e in E], .5).save(os.path.join(out_dir, f'enemy_{tag}.png'))


if __name__ == '__main__':
    old, new, out = sys.argv[1:4]
    os.makedirs(out, exist_ok=True)
    sheet(old, out, 'before'); sheet(new, out, 'after')
    print('→', out)
