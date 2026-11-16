# -*- coding: utf-8 -*-
"""터렛 스킨 UI 그림 (3차): Turret/<art>.png → Turret/<art>_skin.png — Skin.xlsx 의 Hue · Sat 로 색을 돌림.
셰이더 Resources/Shaders/SpriteHue.shader 와 같은 식 (감마 공간 HSV · 채도 낮거나 어두운 곳(선 · 흰색)은 그대로).
사용: PYTHONUTF8=1 python tools/skin_preview.py   (Skin.xlsx 를 바꾸면 다시)
"""
import os, colorsys
import numpy as np
from PIL import Image
import openpyxl

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), '..'))
ART = os.path.join(ROOT, 'Earth_Defence', 'Assets', 'Resources', 'Art', 'Turret')


def smooth(a, b, x):
    t = np.clip((x - a) / (b - a), 0, 1); return t * t * (3 - 2 * t)


def shift(im, deg, sat):
    a = np.asarray(im.convert('RGBA')).astype(np.float64) / 255
    rgb = a[..., :3]
    mx, mn = rgb.max(-1), rgb.min(-1); d = mx - mn
    v = mx; s = np.where(mx > 0, d / np.maximum(mx, 1e-9), 0)
    r, g, b = rgb[..., 0], rgb[..., 1], rgb[..., 2]
    h = np.zeros_like(mx)
    m = d > 1e-9
    rc = np.where(m, (mx - r) / np.maximum(d, 1e-9), 0); gc = np.where(m, (mx - g) / np.maximum(d, 1e-9), 0); bc = np.where(m, (mx - b) / np.maximum(d, 1e-9), 0)
    h = np.where(r == mx, bc - gc, np.where(g == mx, 2 + rc - bc, 4 + gc - rc)); h = (h / 6) % 1
    k = smooth(0.12, 0.3, s) * smooth(0.18, 0.38, v)
    h2 = (h + deg / 360) % 1; s2 = np.clip(s * sat, 0, 1)
    i = np.floor(h2 * 6); f = h2 * 6 - i; p = v * (1 - s2); q = v * (1 - s2 * f); t = v * (1 - s2 * (1 - f)); i = i.astype(int) % 6
    out = np.stack([np.choose(i, [v, q, p, p, t, v]), np.choose(i, [t, v, v, q, p, p]), np.choose(i, [p, p, t, v, v, q])], -1)
    res = rgb * (1 - k[..., None]) + out * k[..., None]
    a[..., :3] = res
    return Image.fromarray((a * 255).round().astype(np.uint8), 'RGBA')


if __name__ == '__main__':
    ws = openpyxl.load_workbook(os.path.join(ROOT, 'Earth_Defence', 'Table', 'Skin.xlsx'))['Skin']
    cols = [c.value for c in ws[1]]
    for row in ws.iter_rows(min_row=2, values_only=True):
        if not row[0]:
            continue
        r = dict(zip(cols, row))
        src = os.path.join(ART, r['Turret'] + '.png')
        shift(Image.open(src), float(r['Hue']), float(r['Sat'] or 1)).save(os.path.join(ART, r['Turret'] + '_skin.png'))
        print('→', r['Turret'] + '_skin.png')
