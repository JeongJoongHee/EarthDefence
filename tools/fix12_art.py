# -*- coding: utf-8 -*-
"""11-22 새 타워 6종 효과 그림 (타워_전체_정리 반영) → Resources/Art/Fx
  boomerang  220x220  별빛 부메랑 (3날 황금 부메랑 — 터렛 그림 expansion_art.nt_boomerang 과 같은 날)  · 코드가 빙글빙글 돌림
  frost_ball  96x96   서리 눈꽃포 포탄 (얼음 구슬 + 눈꽃)
  saw        380x380  자기장 톱날 (톱니 원반 + 노란 전기 — 반지름 0.95칸 = 190px @PPU200)
  meteor     176x96   유성 박격포 유성 (오른쪽 = 머리 · 왼쪽으로 불꼬리, 코드가 날아가는 쪽으로 돌림)
사용 (PowerShell — Edge 렌더): $env:PYTHONUTF8='1'; python tools/fix12_art.py
"""
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import earth_art as A
from earth_art import C, G, P, blur
import earth_export as E

W = A.WOB['fx']


def boomerang():
    k = 1.15
    blades = ''
    for a in (-90, 30, 150):
        blades += f'<g transform="rotate({a} 100 68)">' + P('M100 68 Q120 40 144 26 Q132 58 106 78 Z', G('#ffd978', 'v', .82, .35), 4.8) + '</g>'
    core = C(100, 68, 15, G('#c79a5a', 'r'), 4.5) + C(100, 68, 6, '#fff4b8', 0)
    body = blur(C(100, 68, 44, '#ffe082', 0), 8, .55) + blades + core
    return f'<g transform="translate({110 - 100 * k},{110 - 68 * k}) scale({k})">{body}</g>'


def frost_ball():
    cx = cy = 48
    flake = ''.join(f'<g transform="rotate({a} {cx} {cy})">' + E.L(f'M{cx - 17} {cy} L{cx + 17} {cy}', 3.4, '#ffffff') + '</g>' for a in (0, 60, 120))
    return (blur(C(cx, cy, 32, '#9fe8ff', 0), 9, .85) + C(cx, cy, 21, G('#bfefff', 'r', .9, .35), 4.2)
            + flake + C(cx, cy, 5, '#ffffff', 0))


def saw():
    cx = cy = 190
    pts = []
    for i in range(32):
        a = math.radians(i * 11.25)
        rad = 170 if i % 2 == 0 else 138
        pts.append(f'{cx + rad * math.cos(a):.1f} {cy + rad * math.sin(a):.1f}')
    d = 'M' + ' L'.join(pts) + ' Z'
    arcs = ''.join(E.L(f'M{cx + 60 * math.cos(math.radians(a)):.0f} {cy + 60 * math.sin(math.radians(a)):.0f} '
                       f'L{cx + 118 * math.cos(math.radians(a + 14)):.0f} {cy + 118 * math.sin(math.radians(a + 14)):.0f}', 7, '#ffe878')
                   for a in range(0, 360, 60))
    return (blur(C(cx, cy, 176, '#ffe066', 0), 14, .5) + P(d, G('#dfe6ee', 'r', .86, .35), 9)
            + C(cx, cy, 112, 'none', 6).replace('stroke="#3b281c"', 'stroke="#9aa3ad"')
            + blur(arcs, 4, .9) + arcs
            + C(cx, cy, 54, G('#ffd34d', 'r'), 8) + C(cx, cy, 20, '#ffffff', 0))


def meteor():
    hx, cy = 128, 48
    tail = (blur(E._cap(6, hx - 6, cy, 40, E._hgrad('#ff7a2a', 0, .95)), 8)
            + E._cap(28, hx - 10, cy, 24, E._hgrad('#ffb35c', 0, 1))
            + E._cap(56, hx - 14, cy, 12, E._hgrad('#fff1c4', 0, 1)))
    rock = (blur(C(hx, cy, 30, '#ff9a54', 0), 7, .9) + P(A.blob(hx, cy, 24, 7, 9, .12), G('#a0705a', 'r', .8, .35), 4.6)
            + C(hx - 6, cy - 6, 6, '#7a5040', 0) + C(hx + 8, cy + 6, 4, '#7a5040', 0)
            + P(f'M{hx + 6} {cy - 18} Q{hx + 20} {cy - 10} {hx + 22} {cy + 4}', 'none', 3).replace('stroke="#3b281c"', 'stroke="#ffe08a"'))
    return tail + rock


if __name__ == '__main__':
    for name, (svg, w, h) in {'boomerang': (boomerang(), 220, 220), 'frost_ball': (frost_ball(), 96, 96),
                              'saw': (saw(), 380, 380), 'meteor': (meteor(), 176, 96)}.items():
        print(E.out('Fx', name, E.Rr(svg, w, h, W)))
