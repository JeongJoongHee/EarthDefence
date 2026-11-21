# -*- coding: utf-8 -*-
"""11-21 전투 특수 능력 단추 아이콘 2장 (요청: "대장 카피의 특수 능력 // 돔 특수 능력을 발동시킬 수 있는 버튼")
  Icon/skill_burn.png   대장 카피 버닝 — 불꽃
  Icon/skill_frost.png  돔 얼음 파동 — 눈 결정
게임 아이콘과 같은 낙서풍 · 같은 입체감(earth_export.pop3d) · 224px (2배)
사용 (PowerShell — Edge 렌더): $env:PYTHONUTF8='1'; python tools/skill_icons.py
"""
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import earth_art as A
from earth_art import C, G, L, P, blur
import earth_export as EX


def burn():
    """불꽃 (로컬 112)"""
    outer = 'M56 10 C66 30 88 40 88 66 C88 86 74 100 56 100 C38 100 24 86 24 66 C24 50 34 42 38 30 C42 42 48 46 50 50 C50 36 52 22 56 10 Z'
    inner = 'M57 48 C62 60 72 66 72 78 C72 90 64 96 56 96 C48 96 40 90 40 80 C40 70 48 66 50 58 C52 64 54 66 55 68 C54 60 55 54 57 48 Z'
    core = 'M56 72 C60 78 64 82 64 88 C64 93 60 96 56 96 C52 96 48 93 48 88 C48 82 53 79 56 72 Z'
    return (blur(C(56, 70, 40, '#ffb347', 0), 6, .6)
            + P(outer, G('#ff7a3a', 'v', .82, .3), 5) + P(inner, G('#ffc04a', 'v', .86, .3), 3.6)
            + P(core, '#fff2b0', 0) + C(88, 28, 4, '#ffd36b', 2.4) + C(22, 40, 3, '#ffd36b', 2.2))


def frost():
    """눈 결정 (로컬 112)"""
    cx, cy = 56, 56
    s = blur(C(cx, cy, 42, '#9fe8ff', 0), 6, .6) + C(cx, cy, 40, G('#7fd0f0', 'r', .82, .35), 5)
    arms = ''
    for a in range(0, 360, 60):
        r = math.radians(a)
        x1, y1 = cx + 30 * math.cos(r), cy + 30 * math.sin(r)
        arms += f'M{cx} {cy} L{x1:.1f} {y1:.1f} '
        for t, k in ((0.55, 10), (0.8, 7)):
            bx, by = cx + 30 * t * math.cos(r), cy + 30 * t * math.sin(r)
            for d in (-55, 55):
                rr = math.radians(a + d)
                arms += f'M{bx:.1f} {by:.1f} L{bx + k * math.cos(rr):.1f} {by + k * math.sin(rr):.1f} '
    s += L(arms, 8.5) + L(arms, 4.2, '#ffffff')
    return s + C(cx, cy, 6, '#ffffff', 3)


if __name__ == '__main__':
    for n, fn in (('skill_burn', burn), ('skill_frost', frost)):
        im = EX.Rr(f'<g transform="scale(2)">{fn()}</g>', 224, 224, A.WOB['icon'])
        print(EX.out('Icon', n, EX.pop3d(im)))
