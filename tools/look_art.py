# -*- coding: utf-8 -*-
"""11-20 코스튬 · 탈것 (요청 사항/코스튬.png · 탈것.png 를 참고해 낙서풍으로 새로 그림 — 그림을 그대로 넣지 않음)

코스튬 = 대장 카피 몸 그림을 통째로 바꿈 (기본 = 우주복 · earth_art.capy_guardian)
탈것   = 대장 카피 발밑에 까는 그림 (뒤 층 + 앞 층) — 게임이 코스튬 몸 위에 겹쳐 그려서 "코스튬 x 탈것" 이 최종 모습

사용 (PowerShell 에서 — Bash 샌드박스에선 Edge 가 안 뜸):
  $env:PYTHONUTF8='1'; python tools/look_art.py sheet            → tools/_preview/look_sheet.png (모두 한 장 미리보기)
  $env:PYTHONUTF8='1'; python tools/look_art.py export [id …]   → Resources/Art/Char/costume/<id>[_float|_gun|_ui].png
                                                                  · Resources/Art/Char/mount/<id>[_f].png · tools/look_pivots.json
  $env:PYTHONUTF8='1'; python tools/look_art.py bounds           → Resources/Art/Char/look_bounds.txt (export 끝에 저절로)
  (그다음 표는 tools/fix10_tables.py 가 look_pivots.json 의 Lift 를 Mount 표에 넣음)

좌표
  · 코스튬: 카치건 몸 좌표(발 가운데 x 72 · 발바닥 y 168.5) → earth_art 와 같이 x 를 G_OX 만큼 옮김.
    캔버스 = G 좌표 x -40~200 · y -80~180 (240 x 260) → 피벗(발) = (126/240, 11.5/260) — Art.cs CostumeFoot 과 같게
  · 탈것: 자리(발이 닿는 곳) = (0,0) · 캔버스 400 x 330 에서 자리 (200,170) → 피벗 (0.5, 160/330) — Art.cs MountSeat 과 같게
  · 둘 다 배율 GK 1.8 (PPU 200) — 대장 카피 몸과 같은 크기
"""
import os, sys, json, math
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import earth_art as A
from earth_art import *          # noqa
import numpy as np

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), '..'))
ART = os.path.join(ROOT, 'Earth_Defence', 'Assets', 'Resources', 'Art')
GK = 1.8
SW = A.CAPY_SW

# ------------------------------------------------------------------ 캔버스
CX0, CY0, CW, CH = -40, -80, 240, 260           # 코스튬 (G 좌표)
MW, MH, MSX, MSY = 400, 330, 200, 170           # 탈것 (자리 = MSX, MSY)
NECK = 'M0 100 Q60 86 140 90'
CLOTH = NECK + ' L160 200 L-20 200 L-20 100 Z'    # 목 아래 (몸 모양으로 자름)
BACK_EAR = 'M50 32 Q44 18 54 16 Q61 18 60 28 Z'
FRONT_EAR = 'M78 18 Q77 4 88 5 Q94 8 91 17 Z'
ARM = 'M92 100 Q104 112 118 112'
PAW = A.CAPY_DARK


def star(cx, cy, r, fill, sw=2.6, rot=-90):
    pts = []
    for i in range(10):
        rad = r if i % 2 == 0 else r * .45
        a = math.radians(rot + i * 36)
        pts.append(f'{cx + rad * math.cos(a):.1f} {cy + rad * math.sin(a):.1f}')
    return P('M' + ' L'.join(pts) + ' Z', fill, sw)


def clip(cid, d, inner):
    return f'<clipPath id="{cid}"><path d="{d}"/></clipPath><g clip-path="url(#{cid})">{inner}</g>'


def body_clip(cid, inner):
    return clip('cb_' + cid, BODY_D, inner)


def cloth(cid, inner):
    """몸 모양 안 + 목 아래만"""
    return body_clip(cid, clip('cc_' + cid, CLOTH, inner))


def neck(cid):
    """옷 목둘레 선 (몸 밖으로 안 나가게 자름)"""
    return body_clip('n' + cid, L(NECK, SW + 1))


def face_bare(cid):
    """맨 얼굴 (몸 = 털) — 주둥이 · Y자 코 · 점 눈"""
    return (body_clip('f' + cid, F(SNOUT_D, A.CAPY_DARK))
            + L('M117 50.7 L122.8 56 L129.4 50.7 M122.8 56 L122.8 69.1', 2.4) + L('M118.1 73.6 L122.8 69.1 L126.7 73', 2.6)
            + '<circle cx="91.3" cy="40.2" r="2.9" fill="#1c110a"/><circle cx="92.3" cy="39.2" r=".9" fill="#ffffff"/>')


def ears(col=None):
    col = col or A.CAPY_DARK
    return P(BACK_EAR, G(col, 'r'), SW) + P(FRONT_EAR, G(col, 'r'), SW)


def feet(col, col2=None):
    return E(90, 161, 15, 8, G(col), SW) + E(54, 160, 15, 8, G(col2 or shade(col, .86)), SW)


def arm(col, cuff=None):
    s = limb(ARM, 15, col, SW)
    if cuff: s += L('M109 104 L112 118', 6) + L('M109 104 L112 118', 2.8, cuff)
    return s


def bare(cid, fur=None):
    """맨몸 카피바라 (옷 입히기 바탕)"""
    fur = fur or A.CAPY_BODY
    return P(BODY_D, G(fur, 'v', .86, .22), SW)


def onesie(cid, col, belly=None):
    """잠옷형: 몸 전체가 옷 + 얼굴 구멍 (기본 우주복과 같은 방식)"""
    s = P(BODY_D, G(col, 'v', .86, .22), SW)
    if belly: s += body_clip('b' + cid, E(104, 130, 26, 30, belly, 0))
    return s + A.capy_face_hole(SW).replace('gBodyClip', 'fh' + cid)


# ------------------------------------------------------------------ 코스튬 19 (id, 이름, 손 색, 그림)
def c_wizard(cid):
    pur, pur2, gold = '#8f72d4', '#6f55b4', '#ffd34d'
    s = feet('#7a5a8a') + bare(cid)
    s += cloth(cid, F('M-20 80 L160 80 L160 200 L-20 200 Z', G(pur, 'v', .8, .2))
               + star(40, 120, 7, gold, 2) + star(76, 146, 6, gold, 2) + star(104, 112, 5, gold, 2) + star(30, 150, 5, gold, 2))
    s += neck(cid) + body_clip('wg' + cid, L('M14 104 Q60 92 128 94', 5, gold))
    s += face_bare(cid) + arm(pur, gold)
    # 뾰족 모자 (끝이 뒤로 꺾임) + 별 + 금 띠 — 귀는 모자 위로
    s += P('M36 34 Q60 -10 70 -40 Q74 -58 52 -70 Q84 -66 90 -34 Q100 -4 134 26 Q90 20 36 34 Z', G(pur, 'v', .8, .25), SW)
    s += P('M30 36 Q86 14 140 28 Q144 36 134 38 Q86 28 36 44 Q26 44 30 36 Z', G(pur2), SW) + L('M44 30 Q86 16 124 24', 5, gold)
    s += star(76, -8, 7, gold, 2) + star(96, 8, 5, gold, 2) + star(56, 12, 5, gold, 2)
    return s + ears()


def c_rain(cid):
    yel, yel2 = '#ffd54a', '#e8b52c'
    s = feet(yel, yel2) + bare(cid)
    s += cloth(cid, F('M-20 80 L160 80 L160 200 L-20 200 Z', G(yel, 'v', .84, .2)) + L('M106 96 L104 165', 3)
               + C(112, 112, 3.6, '#ffffff', 2) + C(111, 132, 3.6, '#ffffff', 2) + rr(56, 128, 26, 16, 5, yel2, 2.6))
    s += neck(cid) + face_bare(cid) + arm(yel, yel2)
    # 비옷 모자 (뒤가 길게 늘어진 챙)
    s += P('M42 30 Q52 2 88 0 Q122 2 128 28 Z', G(yel, 'v', .84, .25), SW)
    s += P('M20 52 Q24 30 44 26 Q90 20 140 28 Q146 34 136 36 Q92 32 52 40 Q40 52 20 52 Z', G(yel2), SW)
    return s + ears()


def c_gnome(cid):
    grn, red, beard = '#6fae55', '#e0564a', '#e8803e'
    s = P('M30 100 Q-6 82 -4 112 Q4 126 30 118 Z', '#bde39a', SW * .8) + P('M28 120 Q0 124 4 142 Q14 150 30 134 Z', '#bde39a', SW * .8)   # 잎 날개
    s += feet('#7a5034') + bare(cid)
    s += cloth(cid, F('M-20 80 L160 80 L160 200 L-20 200 Z', G(grn, 'v', .82, .2)) + F('M-20 134 L160 134 L160 146 L-20 146 Z', '#7a5034')
               + L('M-20 134 L160 134 M-20 146 L160 146', 2.6))
    s += rr(96, 130, 18, 20, 3, '#ffd34d', 2.6) + neck(cid)
    s += face_bare(cid)
    # 수염 (주둥이 아래 · 볼까지)
    s += P('M96 66 Q90 92 104 104 Q112 116 122 104 Q136 108 138 92 Q144 76 136 72 Q128 82 116 78 Q104 76 96 66 Z', G(beard, 'v', .85, .2), SW)
    s += L('M108 88 Q112 96 110 102 M124 88 Q126 96 130 98', 2.4)
    s += arm(grn, '#7a5034')
    # 빨간 뾰족 모자
    s += P('M38 36 Q48 -20 84 -54 Q96 -30 108 -10 Q120 10 132 28 Q86 20 38 36 Z', G(red, 'v', .82, .25), SW)
    s += L('M84 -54 Q78 -20 92 8', 3, shade(red, .8))
    return s + ears()


def c_pirate(cid):
    s = feet('#4a3628') + bare(cid)
    stripes = ''.join(F(f'M-20 {y} L160 {y} L160 {y + 8} L-20 {y + 8} Z', '#4f7fc6') for y in range(96, 140, 16))
    s += cloth(cid, F('M-20 80 L160 80 L160 200 L-20 200 Z', '#f7f7f4') + stripes
               + F('M-20 138 L160 138 L160 200 L-20 200 Z', G('#4b4b5e')) + L('M-20 138 L160 138', 3))
    s += neck(cid) + face_bare(cid)
    s += L('M70 30 Q90 40 128 40', 3.4) + E(93, 42, 9, 8, '#2b2b2b', 2.6)     # 안대
    s += arm('#f7f7f4') + L('M100 105 L104 117', 7, '#4f7fc6')
    # 빨간 두건 + 매듭 끈
    s += P('M36 40 Q42 8 84 6 Q122 8 130 30 Q88 22 36 40 Z', G('#e0564a', 'v', .82, .25), SW)
    s += P('M38 38 Q22 40 8 30 Q12 44 24 46 Q12 56 6 66 Q26 58 40 46 Z', G('#cc4a3e'), SW * .9)
    s += C(68, 22, 3, '#ffffff', 0) + C(98, 16, 3, '#ffffff', 0) + C(112, 26, 3, '#ffffff', 0)
    return s + ears()


def c_lion(cid):
    gold, mane = '#f3b856', '#c9772e'
    tail = L('M24 140 Q-6 150 -16 124', 9) + L('M24 140 Q-6 150 -16 124', 4, gold) + P('M-16 124 Q-30 112 -22 102 Q-8 106 -10 120 Z', G(mane), SW * .8)
    s = tail + feet(gold, '#d9a046')
    # 갈기 (얼굴 둘레 동그란 털)
    puffs = ''.join(C(104 + 46 * math.cos(math.radians(a)), 56 + 42 * math.sin(math.radians(a)), 15, G(mane, 'r'), SW * .8)
                    for a in range(-170, 120, 26))
    s += P(BODY_D, G(gold, 'v', .86, .22), SW) + body_clip('lb' + cid, E(106, 132, 24, 28, '#ffe2a8', 0))
    s += puffs
    s += A.capy_face_hole(SW).replace('gBodyClip', 'fh' + cid)
    s += arm(gold)
    s += P('M60 26 Q52 6 68 4 Q80 8 74 24 Z', G(gold, 'r'), SW) + P('M92 14 Q94 -4 108 0 Q114 10 104 20 Z', G(gold, 'r'), SW)
    return s


def c_dino(cid):
    grn, belly = '#7cc46a', '#c3e69a'
    spikes = ''.join(P(f'M{x - 7} {y + 4} L{x - 12} {y - 10} L{x + 4} {y - 4} Z', '#5e9e4e', SW * .7)
                     for x, y in [(62, 18), (44, 34), (32, 56), (24, 80), (20, 104), (24, 128)])
    tail = P('M36 132 Q0 150 -30 166 Q10 170 44 156 Z', G(grn), SW)
    s = spikes + tail + feet(grn, '#62a654')
    s += onesie(cid, grn, belly)
    s += C(108, 70, 6, '#ff9e9e', 0).replace('<circle', '<circle opacity=".55"')
    s += arm(grn)
    s += P('M80 10 Q86 -12 106 -8 Q100 10 80 10 Z', G('#8fd06a'), SW * .8) + L('M82 8 Q92 -2 104 -6', 2)
    return s + ears(shade(grn, .82))


def c_chef(cid):
    s = feet('#3b3b3b') + bare(cid)
    s += cloth(cid, F('M-20 80 L160 80 L160 200 L-20 200 Z', G('#f6f4ee', 'v', .9, .1))
               + P('M44 116 L124 112 L126 170 L42 170 Z', '#ffffff', 2.8)
               + C(94, 102, 2.8, '#c9c1b3', 1.6) + C(110, 102, 2.8, '#c9c1b3', 1.6))
    s += neck(cid) + P('M96 90 Q108 98 120 90 L116 102 Q108 98 100 102 Z', '#e0564a', 2.6)    # 목 스카프
    s += face_bare(cid) + arm('#f6f4ee', '#c9c1b3')
    # 요리사 모자 (몽실한 윗부분 + 띠)
    s += rr(52, 2, 70, 26, 6, '#f8f8f8', SW)
    s += P('M50 6 Q30 -14 50 -30 Q56 -52 80 -46 Q94 -64 112 -46 Q136 -46 130 -22 Q144 -6 124 6 Z', G('#ffffff', 'v', .92, 0), SW)
    s += L('M72 -6 Q76 -20 70 -30 M98 -4 Q100 -20 96 -34', 2.4, '#c9c1b3')
    return s + ears()


def c_bee(cid):
    yel, blk = '#ffd23f', '#3b2a20'
    wings = (E(12, 92, 30, 18, '#e3f4ff', SW * .8, -30) + E(22, 116, 24, 14, '#d4eeff', SW * .8, 10)).replace('<ellipse', '<ellipse opacity=".92"')
    s = wings + feet(blk, '#2a1d16')
    s += onesie(cid, yel)
    s += body_clip('bs' + cid, ''.join(F(f'M-20 {y} L160 {y - 4} L160 {y + 10} L-20 {y + 14} Z', blk) for y in (104, 130, 154)))
    s += P('M34 160 L22 172 L40 166 Z', blk, SW * .6)
    s += arm(yel)
    s += L('M70 14 Q62 -8 54 -16', 3.6) + C(53, -18, 6, blk, 2.6) + L('M92 8 Q98 -12 106 -18', 3.6) + C(108, -20, 6, blk, 2.6)
    return s + ears(shade(yel, .8))


def c_knight(cid):
    met, met2 = '#cdd4dc', '#a7b0ba'
    s = feet(met2, '#8f98a2')
    s += onesie(cid, met)
    s += body_clip('kp' + cid, L('M10 104 Q70 92 130 96', 3) + L('M10 128 Q70 120 128 124', 3) + L('M14 150 Q70 146 120 150', 3)
                   + F('M10 104 Q70 92 130 96 L130 100 Q70 96 10 108 Z', '#ffffff', .4))
    # 투구 테두리 (얼굴 구멍 둘레) + 위 볏
    s += E(104, 56, 37, 34, 'none', SW + 2).replace(f'stroke="{A.INK}"', f'stroke="{met2}"') + E(104, 56, 40, 37, 'none', 2.6)
    s += P('M66 14 Q60 -12 80 -24 Q98 -30 104 -14 Q94 -16 90 -4 Q82 4 66 14 Z', G('#e0564a', 'v', .82, .25), SW)
    s += arm(met, met2)
    return s + ears(met2)


def c_detective(cid):
    tw, tw2 = '#b08a5e', '#8f6c46'
    chk = ''.join(L(f'M{x} 80 L{x} 200', 1.6, tw2) for x in range(0, 160, 14)) + ''.join(L(f'M-20 {y} L160 {y}', 1.6, tw2) for y in range(96, 200, 14))
    s = feet('#5a4030') + bare(cid)
    s += cloth(cid, F('M-20 80 L160 80 L160 200 L-20 200 Z', G(tw, 'v', .86, .2)) + chk
               + P('M100 92 L112 92 L108 122 Z', '#3b3b5a', 2.4) + L('M98 92 L80 150', 3))
    s += neck(cid) + face_bare(cid) + arm(tw, tw2)
    # 사냥 모자 (앞뒤 챙 + 귀덮개)
    hat = F('M-60 -60 L200 -60 L200 60 L-60 60 Z', G(tw, 'v', .86, .25)) + ''.join(L(f'M{x} -40 L{x + 20} 40', 1.6, tw2) for x in range(30, 140, 12))
    s += clip('dh' + cid, 'M40 34 Q44 -2 86 -4 Q126 0 130 30 Q88 22 40 34 Z', hat) + P('M40 34 Q44 -2 86 -4 Q126 0 130 30 Q88 22 40 34 Z', 'none', SW)
    s += P('M120 26 Q140 22 148 32 Q136 36 124 32 Z', G(tw2), SW * .9) + P('M44 32 Q28 34 24 44 Q36 46 48 38 Z', G(tw2), SW * .9)
    s += L('M84 -4 L84 -10', 4) + C(84, -12, 3.4, tw2, 2)
    return s + ears()


def c_hero(cid):
    blu, red, yel = '#4f84d8', '#e0564a', '#ffd34d'
    cape = P('M40 96 Q0 120 -20 176 Q20 168 40 176 Q50 150 60 98 Z', G(red, 'v', .8, .2), SW)
    s = cape + feet(red, '#c84a3e') + bare(cid)
    s += cloth(cid, F('M-20 80 L160 80 L160 200 L-20 200 Z', G(blu, 'v', .84, .22)) + F('M-20 136 L160 136 L160 146 L-20 146 Z', yel)
               + L('M-20 136 L160 136 M-20 146 L160 146', 2.4))
    s += neck(cid)
    s += P('M98 102 L122 102 L118 120 L110 128 L102 120 Z', yel, 2.6) + star(110, 112, 6, red, 1.8)    # 가슴 방패 + 별
    s += face_bare(cid) + arm(blu, red)
    s += P('M60 96 Q50 90 46 100 L56 104 Z', red, 2.4)
    return s + ears()


def c_bunny(cid):
    pink, pink2, inner = '#f7b9cb', '#e89cb2', '#ffdbe6'
    s = C(22, 140, 14, G('#ffffff', 'r'), SW * .9) + feet(pink, pink2)
    s += onesie(cid, pink, '#ffe3ec')
    s += arm(pink)
    # 긴 토끼 귀 (모자 귀) — 카피 귀도 그대로 보이게 옆에
    s += P('M56 24 Q34 -30 46 -60 Q64 -66 68 -30 Q70 0 70 18 Z', G(pink), SW) + F('M56 14 Q44 -28 50 -50 Q60 -52 62 -28 Q64 -4 64 12 Z', inner)
    s += P('M78 14 Q76 -40 96 -64 Q112 -60 104 -30 Q98 -4 90 14 Z', G(pink), SW) + F('M84 8 Q84 -34 96 -52 Q104 -48 98 -28 Q94 -6 88 8 Z', inner)
    s += C(108, 70, 6, '#ff9e9e', 0).replace('<circle', '<circle opacity=".5"')
    return s + ears(A.CAPY_DARK)


def c_ninja(cid):
    red, red2 = '#d9483b', '#b5392e'
    tails = P('M44 30 Q16 30 0 16 Q8 34 22 40 Q4 48 -6 62 Q20 56 44 42 Z', G(red2), SW * .9)
    s = tails + feet('#3b2a20') + onesie(cid, red)
    s += body_clip('nb' + cid, F('M-20 134 L160 134 L160 146 L-20 146 Z', '#3b2a20'))
    s += L('M-20 134 L160 134', 0)
    # 복면 (주둥이 덮개) + 머리띠
    s += clip('nm' + cid, BODY_D, P('M66 62 Q100 54 150 58 L150 110 L66 110 Z', G(red, 'v', .86, .2), SW))
    s += L('M66 62 Q100 54 150 58', SW) + L('M76 66 Q100 62 132 64', 2, red2)
    s += L('M60 26 Q100 16 136 30', 9) + L('M60 26 Q100 16 136 30', 4.6, '#3b2a20')
    s += arm(red, '#3b2a20')
    s += L('M84 36 L100 40', 3)     # 눈썹 (진지)
    return s + ears(red2)


def c_shark(cid):
    blu, belly = '#86a9cc', '#f1f5f8'
    s = P('M30 130 Q-6 136 -28 112 Q-24 140 -32 166 Q4 150 36 154 Z', G(blu), SW)            # 꼬리 지느러미
    s += P('M52 22 Q40 -16 66 -34 Q70 -6 84 14 Z', G(blu), SW)                               # 등 지느러미
    s += feet(blu, '#6f92b6')
    s += onesie(cid, blu, belly)
    # 이빨 (얼굴 구멍 둘레)
    teeth = ''.join(P(f'M{104 + 35 * math.cos(math.radians(a - 8)):.1f} {56 + 32 * math.sin(math.radians(a - 8)):.1f} '
                      f'L{104 + 26 * math.cos(math.radians(a)):.1f} {56 + 24 * math.sin(math.radians(a)):.1f} '
                      f'L{104 + 35 * math.cos(math.radians(a + 8)):.1f} {56 + 32 * math.sin(math.radians(a + 8)):.1f} Z', '#ffffff', 2)
                    for a in range(-160, 40, 22))
    s += teeth + P('M70 104 Q56 112 52 126 Q66 122 78 112 Z', G(blu), SW * .8)
    s += arm(blu)
    return s


def c_farmer(cid):
    yel, den, straw = '#ffd96a', '#5b86c2', '#efcc7a'
    s = feet('#7a5034') + bare(cid)
    s += cloth(cid, F('M-20 80 L160 80 L160 200 L-20 200 Z', G(yel, 'v', .86, .2))
               + F('M-20 124 L160 120 L160 200 L-20 200 Z', G(den, 'v', .86, .2)) + rr(80, 104, 40, 30, 4, G(den), 2.6)
               + L('M-20 124 L160 120', 2.6) + rr(88, 116, 16, 10, 2, shade(den, .9), 2) + L('M84 104 L70 94 M118 104 L120 92', 6, den))
    s += C(86, 110, 3, '#ffd34d', 1.8) + C(116, 108, 3, '#ffd34d', 1.8)
    s += neck(cid) + face_bare(cid) + arm(yel)
    # 밀짚모자 (넓은 챙 + 빨간 띠)
    s += P('M48 26 Q50 -6 86 -8 Q122 -6 124 24 Z', G(straw, 'v', .86, .25), SW) + F('M49 14 Q86 6 123 12 L124 22 Q86 16 48 24 Z', '#e0564a') + L('M49 14 Q86 6 123 12', 2.4)
    s += P('M14 34 Q86 8 156 26 Q160 34 148 36 Q86 24 22 44 Q8 44 14 34 Z', G(straw), SW)
    s += L('M34 34 Q86 18 140 28', 1.8, '#c9a250')
    return s + ears()


def c_pumpkin(cid):
    org, org2 = '#f28c3c', '#d9722a'
    s = feet('#7a5034') + bare(cid)
    s += P('M18 132 Q14 96 60 94 Q78 88 96 94 Q138 96 136 134 Q132 168 78 168 Q22 168 18 132 Z', G(org, 'r'), SW)
    s += L('M52 98 Q40 132 56 166 M78 94 L78 168 M104 98 Q118 132 102 166', 2.6, org2)
    s += P('M48 116 L58 108 L64 120 Z', '#4a2b14', 2) + P('M90 116 L100 108 L106 120 Z', '#4a2b14', 2)
    s += P('M46 136 L56 144 L66 136 L76 146 L86 136 L96 144 L108 134 Q96 158 76 158 Q54 158 46 136 Z', '#4a2b14', 2)
    s += P('M56 94 Q60 84 78 88 Q98 82 104 94 Q80 100 56 94 Z', '#6fae55', SW * .8)          # 목 잎 깃
    s += face_bare(cid) + arm(org, org2)
    s += L('M84 10 Q84 -4 92 -12', 7, '#5e8a3e') + P('M90 -6 Q106 -18 116 -4 Q102 2 90 -6 Z', '#6fae55', SW * .8)
    return s + ears()


def c_cowboy(cid):
    red, den, hat = '#d9574c', '#5b7fb6', '#9a6a44'
    plaid = (''.join(F(f'M{x} 80 L{x + 7} 80 L{x + 7} 200 L{x} 200 Z', '#a83c34', .7) for x in range(0, 160, 20))
             + ''.join(F(f'M-20 {y} L160 {y} L160 {y + 6} L-20 {y + 6} Z', '#a83c34', .7) for y in range(94, 140, 18)))
    s = feet('#7a5034') + bare(cid)
    s += cloth(cid, F('M-20 80 L160 80 L160 200 L-20 200 Z', G(red, 'v', .86, .2)) + plaid
               + F('M-20 140 L160 138 L160 200 L-20 200 Z', G(den, 'v', .86, .2)) + F('M-20 136 L160 134 L160 142 L-20 144 Z', '#5a3a22'))
    s += rr(96, 132, 18, 13, 3, '#ffd34d', 2.4) + neck(cid) + face_bare(cid) + arm(red, '#a83c34')
    s += P('M50 26 Q48 -10 70 -14 Q86 -4 102 -14 Q124 -10 122 24 Z', G(hat, 'v', .86, .25), SW) + F('M51 14 Q86 6 121 12 L122 22 Q86 16 50 24 Z', '#5a3a22')
    s += P('M8 18 Q22 40 86 26 Q150 14 160 0 Q166 22 150 30 Q90 42 22 46 Q2 40 8 18 Z', G(hat), SW)
    return s + ears()


def c_winter(cid):
    tea, red = '#5fae9e', '#e0564a'
    knit = ''.join(L(f'M-20 {y} ' + ' '.join(f'L{x} {y + (5 if (x // 10) % 2 else 0)}' for x in range(-10, 170, 10)), 2.2, '#e9f6f2') for y in (110, 148))
    s = feet('#7a5034') + bare(cid)
    s += cloth(cid, F('M-20 80 L160 80 L160 200 L-20 200 Z', G(tea, 'v', .86, .2)) + knit)
    s += P('M36 92 Q78 80 128 88 Q130 100 124 104 Q80 96 40 106 Q32 100 36 92 Z', G(red), SW)              # 목도리
    s += P('M50 100 L44 150 L62 150 L64 102 Z', G(red), SW * .9) + L('M46 140 L62 140 M48 128 L62 128', 2, '#ffffff')
    s += face_bare(cid) + arm(tea, '#e9f6f2')
    s += P('M46 30 Q46 -6 86 -8 Q126 -6 128 26 Z', G(red, 'v', .82, .25), SW) + rr(40, 18, 92, 18, 9, G('#ffffff', 'v', .9, 0), SW)
    s += C(84, -14, 12, G('#ffffff', 'r'), SW * .9)
    return s + ears()


def c_basic(cid):
    return None     # 기본 = earth_art.capy_guardian (우주복 · 유리 헬멧 · 산소통)


COSTUMES = [   # id, 이름, 손(장갑) 색
    ('basic', '우주 대원', A.SUIT_GR),
    ('wizard', '별빛 마법사', PAW), ('rain', '노란 우비', '#ffd54a'), ('gnome', '숲 요정', PAW), ('pirate', '해적 선장', PAW),
    ('lion', '사자 잠옷', '#f3b856'), ('dino', '공룡 잠옷', '#7cc46a'), ('chef', '꼬마 요리사', '#f8f8f8'), ('bee', '꿀벌 잠옷', '#ffd23f'),
    ('knight', '은빛 기사', '#cdd4dc'), ('detective', '명탐정', PAW), ('hero', '슈퍼 카피', PAW), ('bunny', '토끼 잠옷', '#f7b9cb'),
    ('ninja', '닌자', '#d9483b'), ('shark', '상어 잠옷', '#86a9cc'), ('farmer', '농부', PAW), ('pumpkin', '호박 유령', PAW),
    ('cowboy', '카우보이', PAW), ('winter', '겨울 니트', PAW),
]
CFN = {c[0]: globals()['c_' + c[0]] for c in COSTUMES}
HANDC = {c[0]: c[2] for c in COSTUMES}


def costume_body(cid):
    """G 좌표 몸 그림 (총 · 손 빼고)"""
    if cid == 'basic': return A.capy_guardian()
    return f'<g transform="translate({A.G_OX},0)">{CFN[cid](cid)}</g>'.replace(A.INK, A.CAPY_INK)


def costume_gun(cid):
    """총 + 두 손 (손 색만 코스튬에 맞춤)"""
    g = A.guardian_gun()
    hc = HANDC[cid]
    if hc != A.SUIT_GR: g = g.replace(G(A.SUIT_GR, 'r'), G(hc, 'r'))
    return g


def hand(cid):
    hx, hy = A.HAND
    return (C(hx, hy, 10, G(HANDC[cid], 'r'), SW) + L(f'M{hx - 4} {hy - 6} Q{hx + 2} {hy - 10} {hx + 7} {hy - 5}', 2.4)).replace(A.INK, A.CAPY_INK)


def costume_float(cid):
    """장비 탭 · 외형 목록: 총 없이 두 손 (기본은 산소통 분사 빛)"""
    s = ''
    if cid == 'basic': s += blur(E(A.G_OX + 10, 146, 9, 14, A.NEON, 0), 4, .8) + E(A.G_OX + 10, 142, 5, 9, '#e9fdff', 0)
    return s + costume_body(cid) + hand(cid)


def costume_full(cid, ang=-50):
    return costume_body(cid) + f'<g transform="translate({A.HAND[0]},{A.HAND[1]}) rotate({ang})">{costume_gun(cid)}</g>'


def in_canvas(svg):
    return f'<g transform="translate({-CX0},{-CY0})">{svg}</g>'


# ------------------------------------------------------------------ 탈것 (자리 = (0,0), 오른쪽이 앞) — back / front 두 층
def m_surf():
    sil, sil2 = '#dfe6ee', '#aab6c4'
    flame = blur(P('M-108 10 L-170 -6 L-150 12 L-184 26 L-140 24 L-160 44 L-104 22 Z', '#ffb648', 0), 5, .8) \
        + P('M-108 10 L-160 -2 L-144 12 L-172 24 L-136 22 L-150 38 L-104 20 Z', '#ffcf5a', 3.4) + P('M-108 12 L-136 8 L-126 16 L-140 24 L-106 18 Z', '#fff4c8', 0)
    board = P('M-116 4 Q-120 22 -96 24 L96 18 Q140 14 150 -6 Q120 -10 96 -4 L-96 -2 Q-114 -2 -116 4 Z', G(sil, 'v', .8, .4), SW)
    board += L('M-90 10 L100 6', 2.6, sil2) + F('M-80 2 L80 0 L80 3 L-80 5 Z', '#ffffff', .8) + C(-60, 14, 3, '#7ff0ff', 1.6) + C(60, 10, 3, '#7ff0ff', 1.6)
    return flame + board, ''


def m_carpet():
    red, blu, gold = '#c8434f', '#4867b8', '#ffd34d'
    trail = ''.join(blur(C(-150 - i * 16, 20 - i * 10, 8 - i, '#c9b6ff', 0), 3, .7) for i in range(5))
    top = 'M-120 -2 Q-60 8 0 -2 Q60 -10 124 0 L132 18 Q66 10 0 22 Q-66 30 -128 18 Z'
    s = trail + P(top, G(red, 'v', .82, .2), SW)
    s += clip('cp', top, L('M-110 6 Q-60 16 0 6 Q60 -2 118 8', 5, gold) + L('M-104 12 Q-60 20 0 12 Q60 4 112 14', 7, blu)
              + ''.join(star(x, 10 - x * .02, 4, gold, 1.4) for x in (-70, 0, 70)))
    for x, y in ((-126, 14), (-122, 22), (128, 6), (132, 16)):
        s += L(f'M{x} {y} L{x + (-14 if x < 0 else 14)} {y + 10}', 3.4) + C(x + (-16 if x < 0 else 16), y + 12, 4, gold, 2)
    return s, ''


def m_plane():
    red, red2, met = '#e0564a', '#b8433a', '#c9d0d8'
    back = P('M-120 -40 L-150 -96 L-122 -96 L-92 -46 Z', G(red2), SW)            # 꼬리 날개 (세로)
    back += P('M-30 -54 L90 -58 Q100 -50 90 -44 L-30 -42 Z', G(red2), SW)         # 뒤 날개 (반대쪽)
    body = 'M-140 -44 Q-60 -54 40 -50 Q100 -48 112 -26 Q116 0 100 16 Q40 30 -60 18 Q-120 8 -140 -30 Z'
    front = P(body, G(red, 'v', .84, .25), SW)
    front += clip('pb', body, F('M-200 -2 L200 -10 L200 4 L-200 12 Z', '#ffffff', .85) + F('M-200 4 L200 -4 L200 0 L-200 8 Z', '#e8b52c'))
    front += L('M-40 -50 Q-34 -66 -2 -70 Q10 -60 12 -50', 4) + F('M-34 -52 Q-28 -64 -4 -66 Q6 -58 8 -52 Z', '#bfeaff', .7)   # 앞 유리
    front += P('M-60 0 L80 2 Q96 14 80 20 L-60 18 Z', G(red2), SW)                 # 아래 날개
    front += L('M-20 16 L-26 42 M40 18 L46 42', 5) + C(-26, 46, 11, '#3b3b3b', SW * .8) + C(46, 46, 11, '#3b3b3b', SW * .8)
    front += C(-26, 46, 4, met, 1.6) + C(46, 46, 4, met, 1.6)
    front += C(116, -16, 13, G(met, 'r'), SW * .8)
    front += blur(E(130, -16, 8, 50, '#ffffff', 0), 2, .5) + E(130, -16, 7, 46, 'none', 2, 0).replace(f'stroke="{A.INK}"', 'stroke="#8a929c" stroke-dasharray="6 6"')
    front += P('M126 -60 Q138 -38 130 -16 Q122 -38 126 -60 Z', G('#9a6a44'), 3) + P('M130 -16 Q140 8 132 30 Q124 8 130 -16 Z', G('#9a6a44'), 3)
    front += ''.join(blur(C(-150 - i * 18, -30 - i * 6, 9 + i * 3, '#d8d8d8', 0), 3, .55) for i in range(3))
    return back, front


def m_ladybug():
    red = '#e8483e'
    legs = ''.join(L(f'M{x} 48 Q{x + d} 70 {x + d * 2} 82', 6) for x, d in ((-70, -10), (-20, -4), (30, 6), (80, 10)))
    head = C(126, 36, 34, G('#3b3b3b', 'r'), SW) + C(132, 28, 13, '#ffffff', 3) + C(150, 30, 11, '#ffffff', 3)
    head += C(136, 30, 6, '#1c110a', 0) + C(152, 32, 5, '#1c110a', 0) + C(138, 27, 2, '#ffffff', 0) + C(153, 29, 1.8, '#ffffff', 0)
    head += L('M120 6 Q114 -16 100 -22', 4) + C(98, -24, 5, '#3b3b3b', 2) + L('M136 6 Q140 -18 154 -24', 4) + C(156, -26, 5, '#3b3b3b', 2)
    shell = 'M-120 50 Q-120 -6 0 -6 Q118 -6 120 50 Z'
    s = legs + head + P(shell, G(red, 'r'), SW)
    s += clip('lb', shell, ''.join(C(x, y, r, '#2b2b2b', 0) for x, y, r in ((-70, 24, 14), (-20, 34, 12), (40, 16, 14), (84, 34, 11), (-96, 44, 9), (10, 6, 8))))
    s += L('M0 -6 L0 50', 3.4) + F('M-80 10 Q-60 -2 -30 -2 Q-60 6 -70 18 Z', '#ffffff', .6)
    s += P('M-122 48 L122 48 L118 58 L-118 58 Z', '#3b3b3b', SW * .8)
    return s, ''


def m_star():
    yel = '#ffd84a'
    tail = blur(P('M-30 30 Q-120 -10 -190 -50 Q-120 10 -40 64 Z', '#ffcf5a', 0), 8, .7) + P('M-30 34 Q-110 0 -170 -36 Q-110 14 -40 58 Z', '#ffe680', 3)
    tail += P('M-34 40 Q-90 20 -130 -6 Q-90 26 -40 52 Z', '#fff6c8', 0)
    pts = []
    for i in range(10):
        rad = 80 if i % 2 == 0 else 38
        a = math.radians(90 + i * 36)            # 점이 아래로 = 위에 뿔 두 개 · 가운데 골짜기(y 4)에 발
        pts.append(f'{rad * math.cos(a):.1f} {42 + rad * math.sin(a):.1f}')
    star_d = 'M' + ' L'.join(pts) + ' Z'
    s = tail + P(star_d, G(yel, 'r'), SW).replace('stroke-linejoin="round"', 'stroke-linejoin="round" ')
    s += C(-14, 50, 4.4, '#1c110a', 0) + C(16, 50, 4.4, '#1c110a', 0) + L('M-6 62 Q1 68 8 62', 3) + C(-26, 62, 6, '#ff9e9e', 0).replace('<circle', '<circle opacity=".6"') + C(28, 62, 6, '#ff9e9e', 0).replace('<circle', '<circle opacity=".6"')
    return s, ''


def m_seahorse():
    t1, t2, fin = '#5fc9b8', '#a68bd6', '#f39ac0'
    body = ('M20 -4 Q60 -40 70 -90 Q74 -120 98 -124 Q130 -122 150 -104 L174 -100 Q178 -88 168 -84 L138 -84 Q120 -70 110 -40 '
            'Q100 10 90 50 Q80 96 40 110 Q0 116 -4 90 Q-6 70 18 68 Q36 70 32 88 Q22 82 16 90 Q20 100 36 96 Q60 84 54 50 Q50 20 20 -4 Z')
    back = P('M10 -10 Q-30 -20 -36 -60 Q-6 -46 6 -36 Q-22 -70 -10 -96 Q14 -60 40 -40 Z', G(fin), SW)     # 등 지느러미
    s = back + P(body, G(t1, 'v', .8, .2), SW)
    s += clip('shb', body, F('M-100 -10 L200 -40 L200 200 L-100 200 Z', t2, .55) + ''.join(L(f'M{60 + i * 4} {-60 + i * 22} Q{90 + i * 2} {-50 + i * 22} {104 - i * 2} {-66 + i * 22}', 2.4, '#ffffff') for i in range(5)))
    s += C(122, -104, 5, '#1c110a', 0) + C(124, -106, 1.6, '#ffffff', 0) + P('M96 -126 Q100 -150 114 -140 Q110 -128 104 -122 Z', G(fin), SW * .8)
    front = P('M-30 -10 Q0 -22 34 -12 Q40 6 30 12 Q0 4 -28 12 Q-36 2 -30 -10 Z', G('#8a5e3c', 'v', .86, .25), SW)   # 안장
    front += L('M30 6 Q40 30 30 50', 3.4, '#5a3a22')
    return s, front


def m_unicorn():
    wht, mane, gold = '#f8f4fb', '#b9a6e0', '#ffd34d'
    legs = ''.join(limb(f'M{x} 24 L{x + d} 96', 18, wht if i % 2 == 0 else '#e6e0ee', SW) + E(x + d, 98, 12, 6, '#a7a0b4', SW * .8)
                   for i, (x, d) in enumerate(((-60, -6), (-36, 4), (54, 4), (76, 12))))
    tail = P('M-80 -6 Q-130 -10 -140 40 Q-120 30 -110 54 Q-104 20 -78 14 Z', G(mane), SW)
    body = 'M-86 -8 Q-90 40 -40 44 L60 44 Q100 40 96 0 Q90 -20 40 -20 L-40 -20 Q-84 -20 -86 -8 Z'
    neck = 'M50 -14 Q70 -60 92 -92 Q110 -110 132 -96 L152 -70 Q158 -58 146 -54 Q128 -58 116 -60 Q104 -30 96 4 Z'
    s = legs + tail + P(body, G(wht, 'v', .9, .1), SW) + P(neck, G(wht, 'v', .9, .1), SW)
    s += P('M112 -100 L126 -150 L128 -98 Z', G(gold), SW * .8) + L('M116 -112 L126 -116 M118 -124 L127 -128', 2)
    s += P('M84 -96 Q60 -80 52 -40 Q66 -50 70 -30 Q76 -60 96 -86 Z', G(mane), SW)
    s += C(128, -82, 4.4, '#6a4fa0', 0) + C(129, -84, 1.4, '#ffffff', 0) + P('M108 -104 L102 -122 L118 -108 Z', G(wht), SW * .7)
    front = P('M-30 -26 Q0 -34 34 -26 Q42 -6 32 2 Q0 -6 -30 2 Q-40 -10 -30 -26 Z', G('#6fae55', 'v', .86, .25), SW)   # 잎 안장
    front += L('M0 -2 Q2 20 -4 42', 4, '#5e8a3e') + C(-8, -24, 5, '#f39ac0', 2) + C(14, -26, 4, '#ffd34d', 2)
    return s, front


def m_hoverbike():
    blk, neo = '#3e4656', '#5ff0ff'
    jet = blur(P('M-124 -8 L-180 -20 L-160 -4 L-190 10 L-124 6 Z', '#ffb648', 0), 5, .8) + P('M-124 -8 L-168 -16 L-152 -4 L-176 6 L-124 4 Z', '#ffcf5a', 3)
    pads = ''.join(blur(E(x, 46, 30, 8, neo, 0), 5, .9) + E(x, 40, 26, 8, G('#566070'), SW * .8) for x in (-70, 70))
    back = jet + pads
    body = 'M-128 -20 Q-90 -40 -20 -36 L40 -40 Q90 -60 130 -30 Q144 -6 120 10 Q60 30 -60 28 Q-120 24 -128 -20 Z'
    front = P(body, G(blk, 'v', .8, .3), SW)
    front += clip('hb', body, blur(L('M-120 6 Q0 18 128 -8', 6, neo), 3, .9) + L('M-120 6 Q0 18 128 -8', 2.6, '#e8feff'))
    front += P('M60 -46 Q86 -86 118 -78 Q110 -54 96 -40 Z', '#9ff6ff', 3.4).replace('fill="#9ff6ff"', 'fill="#9ff6ff" fill-opacity=".55"')
    front += C(124, -18, 6, neo, 2.4) + rr(-40, -44, 70, 12, 6, G('#566070'), SW * .8)
    return back, front


def m_trex():
    br, br2, belly = '#d48c4c', '#b06e36', '#f2c48a'
    tail = P('M-60 -4 Q-130 -10 -176 20 Q-120 26 -60 30 Z', G(br), SW)
    legs = limb('M-30 30 L-40 86', 26, br2, SW) + limb('M40 30 L46 86', 26, br, SW) + E(-40, 90, 18, 7, br2, SW * .8) + E(48, 90, 18, 7, br2, SW * .8)
    body = 'M-70 -14 Q-60 -34 0 -30 Q60 -30 80 -50 Q100 -60 110 -40 Q104 20 60 40 Q0 56 -50 34 Q-80 20 -70 -14 Z'
    head = 'M74 -54 Q76 -110 126 -112 Q170 -110 178 -76 Q178 -56 160 -52 L100 -46 Q80 -40 74 -54 Z'
    s = tail + legs + P(body, G(br, 'v', .86, .2), SW) + P(head, G(br, 'v', .86, .2), SW)
    s += clip('tb', body, E(40, 30, 40, 24, belly, 0) + ''.join(C(x, y, 5, br2, 0) for x, y in ((-40, -10), (-20, 8), (10, -14), (-50, 14))))
    s += L('M100 -60 L172 -66', 3) + ''.join(P(f'M{x} -62 L{x + 4} -54 L{x + 8} -63 Z', '#ffffff', 1.6) for x in (110, 124, 138, 152))
    s += C(132, -90, 5, '#1c110a', 0) + C(133, -92, 1.6, '#ffffff', 0) + limb('M90 -10 L104 4', 9, br2, SW * .8)
    front = P('M-34 -34 Q0 -42 34 -34 Q42 -12 32 -4 Q0 -12 -32 -4 Q-44 -18 -34 -34 Z', G('#e8b552', 'v', .86, .25), SW)   # 표범 무늬 안장
    front += clip('ts', 'M-34 -34 Q0 -42 34 -34 Q42 -12 32 -4 Q0 -12 -32 -4 Q-44 -18 -34 -34 Z', ''.join(C(x, y, 4, '#7a4a20', 0) for x, y in ((-20, -26), (0, -18), (18, -28), (24, -12), (-10, -10))))
    return s, front


def m_broom():
    wood, straw = '#9a6a44', '#e8c46a'
    sparks = ''.join(star(x, y, r, '#ffe680', 1.6) for x, y, r in ((-170, 30, 7), (-150, 56, 5), (-188, 52, 4), (-136, 70, 4)))
    bristle = 'M-48 -2 Q-110 -18 -150 -4 Q-160 20 -150 44 Q-110 46 -48 18 Z'
    s = sparks + P(bristle, G(straw, 'v', .86, .2), SW) + L('M-60 2 L-140 6 M-60 10 L-146 24 M-60 14 L-140 38', 2.4, '#c9a250')
    s += rr(-58, -6, 18, 28, 4, '#8a5e3c', SW * .8)
    s += limb('M-44 8 L150 -6', 12, wood, SW) + L('M-30 6 L140 -6', 2, '#c49a6e')
    s += L('M150 -6 L156 14', 3) + rr(146, 14, 22, 30, 5, '#ffe9a0', SW * .8) + blur(C(157, 29, 12, '#ffd34d', 0), 5, .8) + rr(144, 10, 26, 7, 3, '#5a3a22', 3)
    return s, ''


def m_none():
    return '', ''


MOUNTS = [   # id, 이름, 떠다님(1 = 둥실)
    ('none', '탈것 없음', 0),
    ('surf', '은빛 서프보드', 1), ('carpet', '하늘 양탄자', 1), ('plane', '빨간 비행기', 1), ('ladybug', '무당벌레', 0), ('star', '별똥별', 1),
    ('seahorse', '무지개 해마', 1), ('unicorn', '숲속 유니콘', 0), ('hoverbike', '호버바이크', 1), ('trex', '아기 티라노', 0), ('broom', '마법 빗자루', 1),
]
MFN = {m[0]: globals()['m_' + m[0]] for m in MOUNTS}


def m_canvas(svg):
    return f'<g transform="translate({MSX},{MSY})">{svg}</g>'.replace(A.INK, A.CAPY_INK)


# ------------------------------------------------------------------ 미리보기 · 내보내기
def capy_on_mount(cid, mid, scale=1.0, gun=True):
    """탈것 자리(0,0)에 코스튬 발을 맞춘 한 장 (미리보기용 — 게임은 층을 따로 겹침)"""
    back, front = MFN[mid]()
    body = costume_full(cid) if gun else costume_float(cid)
    return (back + f'<g transform="translate({-A.G_CX},{-A.G_FOOT})">{body}</g>' + front).replace(A.INK, A.CAPY_INK)


def sheet():
    cell = 260
    cols = 7
    items = [('c', c[0], c[1]) for c in COSTUMES] + [('m', m[0], m[1]) for m in MOUNTS] + [('x', 'wizard', 'unicorn'), ('x', 'dino', 'trex'), ('x', 'knight', 'plane'), ('x', 'ninja', 'hoverbike'), ('x', 'bee', 'broom'), ('x', 'shark', 'seahorse')]
    rows = (len(items) + cols - 1) // cols
    svg = f'<rect width="{cols * cell}" height="{rows * cell}" fill="#f7f1e3"/>' + A.font_css()
    for i, (k, a, b) in enumerate(items):
        x, y = (i % cols) * cell, (i // cols) * cell
        if k == 'c': g = f'<g transform="translate({x + 40},{y + 70}) scale(.9)">{costume_full(a)}</g>'
        elif k == 'm': g = f'<g transform="translate({x + cell / 2},{y + 150}) scale(.62)">{capy_on_mount("basic", a)}</g>'
        else: g = f'<g transform="translate({x + cell / 2},{y + 150}) scale(.62)">{capy_on_mount(a, b)}</g>'
        svg += f'<rect x="{x + 4}" y="{y + 4}" width="{cell - 8}" height="{cell - 8}" rx="14" fill="#ffffff" opacity=".5"/>' + g
        svg += A.T(x + cell / 2, y + cell - 14, a if k != 'x' else f'{a}+{b}', 20, '#5e4f42', outline=False)
    im = A.R(svg, cols * cell, rows * cell, A.WOB['char'])
    p = os.path.join(A.PREV, 'look_sheet.png'); im.save(p); print(p)


def out(sub, name, im):
    p = os.path.join(ART, 'Char', sub, name + '.png')
    os.makedirs(os.path.dirname(p), exist_ok=True); im.save(p)
    return im


def lift_of(im):
    """자리(피벗)에서 그림 맨 아래까지 (월드 칸)"""
    a = np.array(im)[:, :, 3]
    rows = np.where(a.max(axis=1) > 40)[0]
    if len(rows) == 0: return 0.0
    bottom = rows[-1] / GK          # 로컬
    return round(max(0.0, bottom - MSY) * GK / 200, 3)


def export(want=None):
    pv_path = os.path.join(os.path.dirname(os.path.abspath(__file__)), 'look_pivots.json')
    pv = json.load(open(pv_path, encoding='utf-8')) if os.path.exists(pv_path) else {}
    pv['costume_pivot'] = [round((A.G_CX - CX0) / CW, 4), round((CY0 + CH - A.G_FOOT) / CH, 4)]
    pv['mount_pivot'] = [round(MSX / MW, 4), round((MH - MSY) / MH, 4)]
    pv.setdefault('lift', {})
    for cid, _, _ in COSTUMES:
        if want and cid not in want: continue
        out('costume', cid, A.piece(in_canvas(costume_body(cid)), CW, CH, GK, A.WOB['char']))
        out('costume', cid + '_float', A.piece(in_canvas(costume_float(cid)), CW, CH, GK, A.WOB['char']))
        x0, y0, w, h = A.GUN_BOX
        out('costume', cid + '_gun', A.piece(f'<g transform="translate({-x0},{-y0})">{costume_gun(cid).replace(A.INK, A.CAPY_INK)}</g>', w, h, GK, A.WOB['char']))
        out('costume', cid + '_ui', A.piece(f'<g transform="translate(40,80)">{costume_full(cid)}</g>', A.G_W + 80, A.G_H + 90, 1.6, A.WOB['char']))
        print('costume', cid)
    for mid, _, _ in MOUNTS:
        if mid == 'none' or (want and mid not in want): continue
        back, front = MFN[mid]()
        im = out('mount', mid, A.piece(m_canvas(back), MW, MH, GK, A.WOB['char']))
        lift = lift_of(im)
        if front:
            imf = out('mount', mid + '_f', A.piece(m_canvas(front), MW, MH, GK, A.WOB['char']))
            lift = max(lift, lift_of(imf))
        pv['lift'][mid] = lift
        print('mount', mid, 'lift', lift, 'front' if front else '')
    json.dump(pv, open(pv_path, 'w', encoding='utf-8'), ensure_ascii=False, indent=1)
    print('pivots', pv['costume_pivot'], pv['mount_pivot'])


def bounds():
    """그림마다 실제로 그려진 범위(px, 왼쪽 아래 기준) → Resources/Art/Char/look_bounds.txt — LookView 가 빈 여백 없이 상자에 맞출 때 씀"""
    from PIL import Image
    lines = []
    for sub in ('costume', 'mount'):
        d = os.path.join(ART, 'Char', sub)
        for f in sorted(os.listdir(d)):
            if not f.endswith('.png'): continue
            a = np.array(Image.open(os.path.join(d, f)).convert('RGBA'))[:, :, 3]
            ys, xs = np.where(a > 24)
            if len(xs) == 0: continue
            h = a.shape[0]
            lines.append(f'Char/{sub}/{f[:-4]} {xs.min()} {h - 1 - ys.max()} {xs.max() + 1} {h - ys.min()}')
    open(os.path.join(ART, 'Char', 'look_bounds.txt'), 'w', encoding='utf-8').write(chr(10).join(lines) + chr(10))
    print('bounds', len(lines))


if __name__ == '__main__':
    cmd = sys.argv[1] if len(sys.argv) > 1 else 'sheet'
    if cmd == 'sheet': sheet()
    elif cmd == 'export': export(set(sys.argv[2:]) or None); bounds()
    elif cmd == 'bounds': bounds()
