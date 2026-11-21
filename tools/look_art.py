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


def face_bare(cid, blush=False, brow=None):
    """맨 얼굴 (몸 = 털) — 주둥이 · Y자 코 · 점 눈 (+ 선택적 볼터치 · 눈썹)"""
    s = body_clip('f' + cid, F(SNOUT_D, A.CAPY_DARK))
    if blush:
        s += C(93, 52, 5.2, '#ff9898', 0).replace('<circle', '<circle opacity=".45"')
    s += (L('M117 50.7 L122.8 56 L129.4 50.7 M122.8 56 L122.8 69.1', 2.4)
          + L('M118.1 73.6 L122.8 69.1 L126.7 73', 2.6)
          + '<circle cx="91.3" cy="40.2" r="2.9" fill="#1c110a"/><circle cx="92.3" cy="39.2" r=".9" fill="#ffffff"/>')
    if brow:
        s += L(brow, 2.5)
    return s


def hat_shadow(cid, d='M30 32 Q84 22 136 34 L136 45 Q84 33 30 43 Z', op=.16):
    """모자 챙 아래 이마 그림자 (모자가 머리에 자연스럽게 얹힌 느낌)"""
    return body_clip('hs' + cid, F(d, '#3b2010', op))


def ears(col=None):
    col = col or A.CAPY_DARK
    return P(BACK_EAR, G(col, 'r'), SW) + P(FRONT_EAR, G(col, 'r'), SW)


def feet(col, col2=None, trim=None):
    s = E(90, 161, 15, 8, G(col), SW) + E(54, 160, 15, 8, G(col2 or shade(col, .86)), SW)
    if trim:
        s += L('M79 156 Q90 153 101 156', 2.6, trim) + L('M43 155 Q54 152 65 155', 2.6, trim)
    return s


def arm(col, cuff=None):
    s = limb(ARM, 15, col, SW)
    if cuff: s += L('M109 104 L112 118', 6.2) + L('M109 104 L112 118', 3.0, cuff)
    return s


def bare(cid, fur=None):
    """맨몸 카피바라 (옷 입히기 바탕 + 은은한 머리 입체감)"""
    fur = fur or A.CAPY_BODY
    return (P(BODY_D, G(fur, 'v', .86, .20), SW)
            + body_clip('hl' + cid, E(64, 48, 22, 16, '#ffffff', 0).replace('<ellipse', '<ellipse opacity=".14"')))


def onesie(cid, col, belly=None):
    """잠옷형: 몸 전체가 옷 + 얼굴 구멍 (기본 우주복과 같은 방식)"""
    s = P(BODY_D, G(col, 'v', .86, .22), SW)
    if belly: s += body_clip('b' + cid, E(104, 130, 26, 30, belly, 0))
    return s + A.capy_face_hole(SW).replace('gBodyClip', 'fh' + cid)


# ------------------------------------------------------------------ 코스튬 19 (id, 이름, 손 색, 그림)
def c_wizard(cid):
    pur, pur2, pur3, gold = '#8b6cd4', '#6d50b6', '#563c96', '#ffd54f'
    s = feet(pur3, '#442e7a') + bare(cid)
    # 살짝 퍼지는 마법사 로브 (몸선을 감싸며 아래로 자연스럽게 펼쳐짐)
    robe_d = 'M24 94 Q68 82 124 88 C130 112 128 144 114 162 Q74 170 26 162 C12 146 14 116 24 94 Z'
    robe_in = (F('M0 70 L160 70 L160 180 L0 180 Z', G(pur, 'v', .80, .24))
               + F('M16 148 Q74 156 122 146 L122 156 Q74 166 16 158 Z', gold)
               + L('M16 148 Q74 156 122 146', 2.2, '#b8861c')
               + L('M50 116 Q44 138 46 156 M76 116 Q76 138 78 158', 2.4, pur2)
               + star(36, 128, 7.2, gold, 1.8) + star(66, 138, 6.2, gold, 1.8)
               + star(96, 124, 5.4, gold, 1.6) + C(52, 118, 2.2, '#fff2a8', 0) + C(84, 144, 2.0, '#fff2a8', 0))
    s += clip('wr' + cid, robe_d, robe_in) + P(robe_d, 'none', SW)
    # 뾰족 망토 깃(케이프릿) + 금빛 브로치 단추 (팔에 안 가리게 중앙 앞쪽 배치)
    collar = 'M22 92 Q70 80 124 88 Q126 102 108 108 L94 101 L80 114 L62 102 L44 114 Q24 108 22 92 Z'
    s += P(collar, G(pur2, 'v', .84, .22), SW * .85) + C(80, 104, 5.6, G(gold, 'r'), 2.4)
    s += face_bare(cid) + hat_shadow(cid, 'M30 35 Q82 24 134 34 L134 45 Q82 35 30 45 Z')
    s += arm(pur, gold)
    # 뾰족 마법사 모자 (머리 위에 포근히 안착 · 귀 겹침 없음)
    s += P('M24 40 Q82 16 140 30 Q146 38 136 41 Q84 28 30 48 Q20 47 24 40 Z', G(pur3), SW)
    hat_d = 'M38 32 Q54 -12 68 -42 Q74 -58 50 -68 Q84 -66 92 -32 Q102 -4 128 26 Q84 18 38 32 Z'
    s += P(hat_d, G(pur, 'v', .80, .26), SW)
    s += clip('wh' + cid, hat_d, F('M30 18 Q84 6 136 16 L136 30 Q84 20 30 34 Z', gold)
              + L('M30 18 Q84 6 136 16', 2.4)
              + F('M54 -32 Q68 -12 60 14 Q46 14 46 -8 Z', '#ffffff', .18))
    s += P(hat_d, 'none', SW)
    s += P('M24 40 Q82 18 140 30 Q144 37 135 40 Q84 28 32 47 Q22 46 24 40 Z', G(pur2, 'v', .86, .18), SW)
    s += star(76, -8, 7.2, gold, 1.8) + star(96, 8, 5.0, gold, 1.6) + star(56, 10, 4.6, gold, 1.6)
    return s


def c_rain(cid):
    yel, yel2, yel3 = '#ffd84d', '#ebb826', '#d49d18'
    s = feet(yel2, yel3, '#ffffff') + bare(cid)
    # 뒤로 접힌 우비 후드
    s += P('M22 86 Q10 92 14 110 Q24 118 40 108 Q36 92 22 86 Z', G(yel2, 'v', .84, .2), SW * .9)
    # A라인 노란 우비 코트
    coat_d = 'M24 94 Q70 82 122 88 C128 112 126 142 114 158 Q72 166 24 158 C14 142 16 114 24 94 Z'
    coat_in = (F('M0 70 L160 70 L160 180 L0 180 Z', G(yel, 'v', .84, .24))
               + F('M22 104 Q32 106 30 154 L14 154 L14 104 Z', '#ffffff', .22)
               + L('M96 90 Q98 124 95 160', 2.6, '#b58416')
               + C(90, 112, 3.6, '#fffdf4', 2.0) + C(90, 130, 3.6, '#fffdf4', 2.0)
               + rr(46, 126, 28, 18, 5, G(yel2), 2.4) + L('M46 131 Q60 135 74 131', 2.2))
    s += clip('rc' + cid, coat_d, coat_in) + P(coat_d, 'none', SW)
    # 접힌 우비 깃(카라)
    s += P('M24 92 Q70 80 122 88 Q122 100 104 104 Q72 96 36 106 Q22 102 24 92 Z', G(yel, 'v', .88, .28), SW * .85)
    s += face_bare(cid) + hat_shadow(cid, 'M30 35 Q82 24 134 34 L134 45 Q82 35 30 45 Z')
    s += arm(yel, yel2)
    # 둥근 우비 모자 (뒤 챙이 머리 뒤를 부드럽게 감쌈)
    s += P('M38 30 Q46 0 86 -2 Q124 0 128 28 Z', G(yel, 'v', .86, .28), SW)
    s += L('M84 -1 Q82 12 82 24', 2.2, yel3) + F('M54 8 Q70 1 80 6 Q68 12 52 18 Z', '#ffffff', .32)
    s += P('M20 50 Q24 32 42 26 Q88 18 140 30 Q145 36 135 38 Q90 30 50 40 Q38 48 26 52 Q19 54 20 50 Z', G(yel2, 'v', .88, .18), SW)
    return s


def c_gnome(cid):
    grn, grn2, red, red2, beard = '#6aad4f', '#4f8c39', '#e05044', '#bf3a30', '#e87f3a'
    # 등 위 반투명 연두 요정 날개 (어깨 위쪽에서 우아하게)
    wings = (P('M32 88 Q-12 58 -10 92 Q0 112 32 104 Z', G('#c6eda2', 'r'), SW * .78)
             + L('M26 94 Q6 86 -2 90', 1.8, '#8cc268')
             + P('M30 106 Q-4 108 0 128 Q14 140 32 122 Z', G('#b5e48d', 'r'), SW * .78)
             + L('M26 114 Q10 116 6 122', 1.8, '#8cc268')).replace('<path', '<path opacity=".93"')
    s = wings + feet('#784e30', '#5c3b22') + bare(cid)
    # 초록 튜닉 + 갈색 벨트 + 가운데 금 버클
    tunic_d = 'M24 94 Q70 82 122 88 C128 112 126 142 114 158 Q72 166 26 158 C14 142 16 114 24 94 Z'
    tunic_in = (F('M0 70 L160 70 L160 180 L0 180 Z', G(grn, 'v', .82, .22))
                + F('M0 128 L160 126 L160 140 L0 142 Z', '#734a2d')
                + L('M0 128 L160 126 M0 142 L160 140', 2.4)
                + L('M52 142 L48 160 M92 141 L94 159', 2.2, grn2))
    s += clip('gt' + cid, tunic_d, tunic_in) + P(tunic_d, 'none', SW)
    s += rr(76, 125, 20, 18, 4, G('#ffd54f', 'r'), 2.6) + rr(82, 130, 8, 8, 2, '#734a2d', 1.8)
    s += P('M24 94 Q70 82 122 88 Q118 100 72 98 Q32 102 24 94 Z', G(grn2), SW * .8)
    s += face_bare(cid) + hat_shadow(cid, 'M32 34 Q82 22 132 32 L132 42 Q82 32 32 43 Z')
    # 풍성하고 자연스럽게 흘러내리는 주황 요정 수염 (볼 ~ 턱 ~ 가슴 위를 하나로 감쌈)
    beard_d = ('M76 74 Q74 96 88 106 Q100 116 114 114 Q128 110 134 94 Q138 80 132 72 '
               'Q122 80 110 78 Q94 82 76 74 Z')
    s += P(beard_d, G(beard, 'v', .84, .22), SW * .85)
    s += clip('gb' + cid, beard_d, F('M92 75 Q110 74 130 75 Q126 86 110 85 Q94 86 92 75 Z', '#f7a462', .75)
              + L('M94 88 Q98 98 96 104 M112 88 Q114 98 112 105 M124 86 Q126 94 124 98', 2.0, '#b5561e'))
    s += P(beard_d, 'none', SW * .85)
    s += arm(grn, '#734a2d')
    # 모자 테두리 아래에서 살짝 위로 삐죽 나온 귀여운 요정 귀
    s += P('M54 36 Q38 24 32 32 Q36 44 54 44 Z', G(A.CAPY_BODY, 'r'), SW * .78) + L('M38 35 Q46 37 52 39', 1.8, A.CAPY_DARK)
    # 끝이 살짝 뒤로 처진 둥근 빨간 요정 모자
    s += P('M34 34 Q42 -16 74 -46 Q92 -56 102 -36 Q100 -24 108 -12 Q122 6 130 28 Q84 18 34 34 Z', G(red, 'v', .80, .26), SW)
    s += F('M52 -8 Q64 -32 78 -40 Q68 -18 62 8 Z', '#ffffff', .20)
    s += L('M34 34 Q84 18 130 28', SW) + L('M88 -26 Q98 -16 96 -2', 2.4, red2)
    return s


def c_pirate(cid):
    red, red2, blu, pant = '#e04e42', '#bc3a30', '#487ec7', '#44485c'
    s = feet('#4a3426', '#38261b') + bare(cid)
    stripes = ''.join(F(f'M-20 {y} L160 {y} L160 {y + 7.5} L-20 {y + 7.5} Z', blu) for y in range(98, 134, 15))
    s += cloth(cid, F('M-20 80 L160 80 L160 200 L-20 200 Z', '#f8f7f2') + stripes
               + F('M-20 138 L160 138 L160 200 L-20 200 Z', G(pant, 'v', .82, .18))
               + L('M-20 138 L160 138', 2.8))
    # 깔끔한 셔츠 목선 + 붉은 해적 허리 띠(새시)
    s += neck(cid)
    s += body_clip('ps' + cid, F('M0 128 Q68 134 140 126 L140 140 Q68 148 0 140 Z', G(red, 'v', .84, .2))
                   + L('M0 128 Q68 134 140 126 M0 140 Q68 148 140 140', 2.6))
    s += P('M20 132 Q10 146 12 158 Q22 154 26 142 Z', G(red2), SW * .75)
    s += face_bare(cid) + hat_shadow(cid, 'M32 34 Q82 22 132 30 L132 40 Q82 32 32 42 Z')
    # 안대 끈은 BODY_D 안으로 깔끔하게 자르고 안대는 눈 위치에 정확히!
    s += body_clip('pe' + cid, L('M54 28 Q78 36 130 37', 3.0))
    s += E(91.5, 41, 8.0, 6.8, '#262220', 2.4) + C(89, 38.5, 1.5, '#ffffff', 0)
    s += arm('#f8f7f2') + L('M99 104 L103 117', 6.5, blu) + L('M109 104 L112 118', 2.6)
    # 빨간 해적 두건 + 매듭 + 나부끼는 끈
    s += P('M36 36 Q18 38 4 26 Q10 42 22 46 Q8 56 4 68 Q24 58 40 46 Z', G(red2), SW * .85)
    band_d = 'M34 38 Q40 8 84 6 Q124 8 132 30 Q86 20 34 38 Z'
    s += P(band_d, G(red, 'v', .82, .26), SW)
    s += clip('pb' + cid, band_d, F('M48 14 Q74 8 92 14 Q74 18 48 22 Z', '#ffffff', .22)
              + C(64, 22, 3.0, '#fff5f2', 0) + C(94, 15, 3.0, '#fff5f2', 0) + C(114, 24, 2.8, '#fff5f2', 0)
              + L('M34 33 Q84 16 132 27', 2.4, red2))
    s += P(band_d, 'none', SW) + C(36, 40, 5.5, G(red, 'r'), 2.6)
    return s


def c_lion(cid):
    gold, gold2, mane, mane2 = '#f3ba56', '#d99e3e', '#cd7a2d', '#a85c1b'
    # 통통하고 귀여운 사자 꼬리 + 복슬 끝털
    tail = (L('M24 140 Q-8 148 -18 122', 9.5) + L('M24 140 Q-8 148 -18 122', 4.5, gold)
            + P('M-18 122 Q-34 112 -24 98 Q-12 96 -8 106 Q-4 116 -14 124 Z', G(mane, 'r'), SW * .8))
    s = tail + feet(gold, gold2)
    # 사자 잠옷 몸통 + 크림 배
    s += P(BODY_D, G(gold, 'v', .86, .22), SW)
    s += body_clip('lb' + cid, E(102, 132, 26, 29, '#ffebc0', 0))
    # 갈기 위 둥근 사자 귀 2개 (선명하고 귀엽게!)
    s += P('M46 24 Q36 -2 56 -5 Q72 -2 68 18 Z', G(gold, 'r'), SW * .9) + C(56, 10, 5.5, mane, 0)
    s += P('M94 14 Q96 -10 114 -6 Q124 4 112 20 Z', G(gold, 'r'), SW * .9) + C(107, 7, 5.2, mane, 0)
    # 하나로 이어진 복슬복슬 구름 갈기 (코 앞으로 안 튀어나오게 오른쪽 정리)
    mane_d = ('M46 82 Q30 74 32 56 Q24 40 38 26 Q38 10 56 8 Q68 -6 88 0 Q106 -6 118 10 '
              'Q132 16 132 32 Q136 46 134 62 Q136 78 124 88 Q114 102 98 100 Q82 108 66 98 Q48 100 46 82 Z')
    s += P(mane_d, G(mane, 'r', .82, .24), SW)
    s += L('M46 56 Q52 46 48 36 M62 18 Q72 12 80 16 M64 90 Q74 96 86 92', 2.4, mane2)
    s += A.capy_face_hole(SW).replace('gBodyClip', 'fh' + cid)
    s += arm(gold, '#ffe6b0')
    return s


def c_dino(cid):
    grn, grn2, belly = '#7ac466', '#5ea64c', '#c9ea9c'
    # 등 따라 부드럽게 이어진 둥근 공룡 등판
    plates = ''.join(P(f'M{x - 4} {y + 10} Q{x - 16} {y - 2} {x - 4} {y - 10} Q{x + 8} {y - 8} {x + 8} {y + 4} Z', G(grn2, 'r'), SW * .75)
                     for x, y in [(54, 24), (38, 44), (28, 68), (22, 92), (20, 116)])
    # 통통하게 휘어진 공룡 꼬리 + 작은 꼬리 돌기
    tail = (P('M38 128 Q6 144 -28 158 Q-34 166 -20 168 Q12 168 46 156 Z', G(grn, 'v', .86, .20), SW)
            + P('M10 142 Q0 132 -6 144 Z', grn2, SW * .65) + P('M-8 150 Q-18 142 -20 153 Z', grn2, SW * .6))
    s = plates + tail + feet(grn, grn2)
    s += onesie(cid, grn, belly)
    s += body_clip('dr' + cid, L('M86 118 Q106 115 124 120 M82 134 Q104 131 124 136 M86 148 Q104 146 120 150', 2.2, '#96cc6c'))
    s += C(93, 52, 4.8, '#ff9898', 0).replace('<circle', '<circle opacity=".42"')
    s += arm(grn, '#b8e288')
    # 정수리에 얹힌 귀여운 나뭇잎 (참고 이미지처럼 부드럽게 눕힘)
    s += P('M66 18 Q68 0 92 -2 Q108 0 112 10 Q96 18 66 18 Z', G('#8fd462', 'r'), SW * .82)
    s += L('M70 15 Q90 8 108 10', 2.2, '#4f8c39')
    return s


def c_chef(cid):
    wht, wht2, red = '#fbfaf5', '#e4dfd3', '#e05044'
    s = feet('#3f3c3a', '#2e2b29') + bare(cid)
    # 셰프 재킷 + 깔끔한 앞치마
    coat_d = 'M24 94 Q70 82 122 88 C126 112 124 140 110 158 Q72 166 28 158 C16 142 18 114 24 94 Z'
    coat_in = (F('M0 70 L160 70 L160 180 L0 180 Z', G(wht, 'v', .90, .12))
               + C(72, 108, 2.8, '#8a8278', 1.4) + C(88, 107, 2.8, '#8a8278', 1.4)
               + F('M16 120 Q72 114 130 120 L130 128 Q72 122 16 128 Z', '#d6cfc2')
               + P('M36 122 Q74 118 114 120 L118 166 L32 166 Z', '#ffffff', 2.8)
               + L('M16 122 Q72 116 130 122', 2.6))
    s += clip('cc' + cid, coat_d, coat_in) + P(coat_d, 'none', SW)
    # 빨간 목 스카프 (목 전체를 자연스럽게 감싸고 앞 매듭)
    s += P('M24 92 Q72 82 122 88 Q122 98 112 102 Q72 94 30 102 Q20 98 24 92 Z', G(red, 'v', .84, .22), SW * .85)
    s += P('M84 96 L98 96 L94 112 L84 108 Z', G('#c93e34'), SW * .75) + C(90, 98, 4.5, G(red, 'r'), 2.2)
    s += face_bare(cid) + hat_shadow(cid, 'M44 26 Q84 18 122 26 L122 35 Q84 26 44 35 Z', .14)
    s += arm(wht, wht2)
    # 몽실한 요리사 모자 (정수리에 깔끔하게 안착 · 이마 위 어색한 귀 혹 제거)
    s += P('M46 32 Q38 18 48 15 Q56 18 55 29 Z', G(A.CAPY_DARK, 'r'), SW)     # 뒤쪽 귀만 모자 옆으로 자연스럽게
    s += P('M50 24 Q48 2 62 0 L110 -4 Q120 0 116 22 Q84 16 50 24 Z', G(wht, 'v', .90, .10), SW)
    s += P('M50 0 Q32 -16 46 -32 Q52 -52 78 -46 Q94 -62 112 -44 Q134 -42 126 -20 Q136 -4 114 0 Q84 -4 50 0 Z', G('#ffffff', 'v', .92, .05), SW)
    s += L('M72 -8 Q74 -22 68 -32 M96 -8 Q98 -22 94 -34', 2.2, '#cfc8bc')
    return s


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
    met, met2, met3, red = '#d2d9e2', '#aab4c0', '#848f9c', '#e04e42'
    s = feet(met2, met3)
    # 붉은 투구 볏 (투구 정수리에서 뒤로 우아하게)
    s += P('M64 18 Q54 -14 76 -26 Q98 -32 106 -14 Q94 -16 88 -4 Q82 6 64 18 Z', G(red, 'v', .80, .26), SW)
    s += L('M74 -12 Q84 -18 94 -14', 2.2, '#b8362c')
    # 갑옷 몸통 (깔끔한 실루엣 + 금속 광택 · 흉갑 분할)
    s += onesie(cid, met)
    s += body_clip('kp' + cid,
                   F('M30 22 Q48 16 54 146 L38 148 Q26 90 30 22 Z', '#ffffff', .36)
                   + L('M16 98 Q72 88 130 94', 3.0)
                   + F('M16 124 Q72 118 130 122 L130 132 Q72 128 16 134 Z', met2)
                   + L('M16 124 Q72 118 130 122 M16 134 Q72 128 130 132', 2.6)
                   + L('M52 134 L48 162 M86 132 L86 162', 2.4, met3)
                   + C(68, 110, 3.2, '#ffd54f', 1.8))
    # 얼굴 구멍 위를 딱 맞게 감싸는 아치형 투구 바이저 (BODY_D 로 깔끔히 자름)
    visor = 'M68 42 Q76 16 106 16 Q132 18 138 42 Q122 28 102 28 Q82 30 68 42 Z'
    s += body_clip('kv' + cid, P(visor, G(met2, 'v', .84, .24), 3.0)
                   + L('M92 20 L90 29 M104 18 L104 27 M116 20 L118 29', 2.2, '#4a525c'))
    s += C(68, 56, 4.0, G(met, 'r'), 2.2)                                      # 바이저 경첩 리벳
    # 어깨 갑옷 견갑(폴드론) + 팔
    s += arm(met, met2)
    s += P('M78 96 Q90 86 106 94 Q106 108 90 110 Q78 106 78 96 Z', G(met, 'r', .84, .28), SW * .8)
    s += F('M84 94 Q92 90 99 95 Q92 98 84 98 Z', '#ffffff', .5)
    s += P(BODY_D, 'none', SW)
    return s


def c_detective(cid):
    tw, tw2, tw3 = '#ba9266', '#967048', '#755434'
    s = feet('#5c3e2c', '#452e20') + bare(cid)
    # 클래식 트렌치 코트 + 어깨 망토(인버네스 케이프)
    coat_d = 'M24 94 Q70 82 122 88 C128 112 126 142 114 158 Q72 166 24 158 C14 142 16 114 24 94 Z'
    chk = (''.join(L(f'M{x} 80 L{x} 180', 1.4, tw2) for x in range(12, 140, 14))
           + ''.join(L(f'M0 {y} L150 {y}', 1.4, tw2) for y in range(94, 170, 14)))
    coat_in = (F('M0 70 L160 70 L160 180 L0 180 Z', G(tw, 'v', .86, .20)) + chk
               + L('M88 124 L86 160', 2.4, tw3) + C(80, 134, 3.0, '#4a3220', 1.4) + C(80, 148, 3.0, '#4a3220', 1.4))
    s += clip('dc' + cid, coat_d, coat_in) + P(coat_d, 'none', SW)
    # 크림 셔츠 · 버건디 넥타이 + 어깨 케이프릿
    s += P('M94 88 L112 88 L106 116 L96 114 Z', '#fbf7ee', 2.2) + P('M100 90 L108 90 L105 112 L100 108 Z', '#a83c32', 1.8)
    cape_d = 'M20 94 Q70 80 120 88 Q112 104 94 114 Q72 124 38 122 Q18 118 20 94 Z'
    s += clip('dk' + cid, cape_d, F('M0 70 L140 70 L140 130 L0 130 Z', G(tw, 'v', .88, .24)) + chk) + P(cape_d, 'none', SW * .88)
    s += face_bare(cid) + hat_shadow(cid, 'M32 34 Q82 22 134 32 L134 42 Q82 32 32 42 Z')
    s += arm(tw, tw2)
    # 체크 사냥 모자 (균형 잡힌 앞뒤 챙)
    s += P('M46 30 Q26 32 22 42 Q36 46 50 38 Z', G(tw2), SW * .88)
    s += P('M114 25 Q134 22 142 32 Q130 37 116 32 Z', G(tw2), SW * .88)
    hat_d = 'M38 32 Q42 -2 84 -4 Q124 -2 128 28 Q84 18 38 32 Z'
    h_in = (F('M20 -20 L150 -20 L150 50 L20 50 Z', G(tw, 'v', .86, .24))
            + ''.join(L(f'M{x} -20 L{x + 18} 40', 1.4, tw2) for x in range(24, 140, 12))
            + ''.join(L(f'M{x} -20 L{x - 18} 40', 1.4, tw2) for x in range(44, 150, 14)))
    s += clip('dh' + cid, hat_d, h_in) + P(hat_d, 'none', SW)
    s += P('M72 22 Q78 8 88 20', G(tw2), 2.2) + C(84, -7, 4.2, G(tw2, 'r'), 2.4)
    return s


def c_hero(cid):
    blu, blu2, red, red2, yel = '#4982db', '#3466b8', '#e04c40', '#ba362c', '#ffd54f'
    # 부드럽게 휘날리는 빨간 망토
    cape = (P('M46 92 Q4 114 -24 156 Q-12 172 16 164 Q36 170 46 158 Q50 132 58 96 Z', G(red, 'v', .82, .22), SW)
            + F('M42 104 Q12 126 -10 156 Q4 162 22 154 Q34 134 48 104 Z', red2, .45))
    s = cape + feet(red, red2, yel) + bare(cid)
    # 파란 히어로 수트 + 노란 벨트 + 빨간 팬츠
    s += cloth(cid, F('M-20 80 L160 80 L160 200 L-20 200 Z', G(blu, 'v', .84, .22))
               + F('M-20 140 L160 140 L160 200 L-20 200 Z', G(red, 'v', .84, .18))
               + F('M-20 130 L160 130 L160 140 L-20 140 Z', yel)
               + L('M-20 130 L160 130 M-20 140 L160 140', 2.4))
    # 목을 감싸는 빨간 망토 깃
    s += P('M24 94 Q72 82 122 88 Q122 98 108 102 Q72 94 34 104 Q20 100 24 94 Z', G(red, 'v', .86, .22), SW * .85)
    # 가슴 방패 엠블럼 (총을 들어도 온전히 보이도록 중앙 배치) + 벨트 버클
    s += P('M62 104 L88 104 L84 120 L75 128 L66 120 Z', yel, 2.4) + star(75, 114, 5.8, red, 1.6)
    s += rr(68, 128, 18, 13, 3, yel, 2.2)
    s += face_bare(cid, brow='M84 33 L99 36') + arm(blu, red)
    return s + ears()


def c_bunny(cid):
    pink, pink2, inner, fluff = '#f7b8ca', '#e399af', '#ffdbe6', '#fff5f8'
    # 복슬복슬 흰 솜방울 꼬리
    tail = (C(20, 140, 13, G('#ffffff', 'r', .92, .08), SW * .85)
            + C(14, 134, 6.5, '#ffffff', 0) + C(26, 134, 6.5, '#ffffff', 0) + C(18, 146, 6.5, '#ffffff', 0))
    s = tail + feet(pink, pink2)
    # 긴 토끼 귀 2개만 (카피바라 갈색 귀와 4귀 겹침 제거)
    s += P('M54 24 Q34 -28 46 -58 Q62 -64 68 -28 Q70 -2 68 18 Z', G(pink, 'v', .88, .20), SW)
    s += F('M55 14 Q44 -26 50 -48 Q59 -50 62 -26 Q64 -4 63 12 Z', inner)
    s += P('M78 16 Q78 -34 94 -58 Q112 -60 112 -34 Q108 -18 96 -6 Q92 4 88 16 Z', G(pink, 'v', .88, .20), SW)
    s += F('M84 8 Q84 -30 95 -48 Q104 -46 102 -30 Q98 -16 90 6 Z', inner)
    # 분홍 토끼 잠옷 + 크림 배 + 얼굴 구멍 바깥쪽 보송보송 흰 털 테두리 (깔끔한 2겹)
    s += P(BODY_D, G(pink, 'v', .86, .22), SW)
    s += body_clip('bb' + cid, E(104, 130, 26, 30, fluff, 0))
    # 솜털 테두리를 얼굴 구멍 바깥쪽에 먼저 그린 뒤 깔끔한 얼굴 구멍으로 덮음!
    puffs_out = ''.join(C(104 + 38 * math.cos(math.radians(a)), 56 + 35 * math.sin(math.radians(a)), 6.2, fluff, 2.2)
                        for a in range(95, 275, 20))
    puffs_in = ''.join(C(104 + 38 * math.cos(math.radians(a)), 56 + 35 * math.sin(math.radians(a)), 6.2, fluff, 0)
                       for a in range(95, 275, 20))
    s += body_clip('bf' + cid, puffs_out + puffs_in)
    s += A.capy_face_hole(SW).replace('gBodyClip', 'fh' + cid)
    s += P(BODY_D, 'none', SW)
    s += C(93, 52, 5.0, '#ff9898', 0).replace('<circle', '<circle opacity=".45"')
    s += arm(pink, fluff)
    return s


def c_ninja(cid):
    red, red2, blk = '#d9483b', '#b33429', '#382c26'
    # 나부끼는 닌자 머리띠 끈
    tails = P('M40 30 Q14 28 -4 14 Q6 32 20 38 Q2 48 -8 62 Q18 56 42 42 Z', G(red2), SW * .88)
    s = tails + feet(blk, '#261d19')
    # 닌자 두건 일체형 뾰족 귀
    s += P('M46 32 Q42 14 56 16 Q62 20 60 28 Z', G(red2, 'r'), SW * .9)
    s += P('M78 18 Q80 2 92 6 Q96 12 92 18 Z', G(red, 'r'), SW * .9)
    # 닌자 복장 몸통 + V자 도복 깃 + 어두운 허리띠
    s += P(BODY_D, G(red, 'v', .84, .22), SW)
    s += body_clip('ng' + cid,
                   L('M60 92 L92 126 M116 90 L86 122', 3.4, red2)
                   + F('M-20 128 L160 128 L160 142 L-20 142 Z', blk)
                   + L('M-20 128 L160 128 M-20 142 L160 142', 2.4)
                   + P('M40 140 L32 158 L44 156 L50 140 Z', blk, 2.0))
    # 깔끔하고 여유 있는 눈 구멍 창 + 늠름한 눈 + 아래 주둥이를 완전히 덮는 붉은 복면
    eye_win = 'M72 44 Q74 28 96 27 Q118 28 122 44 Q114 56 96 56 Q76 56 72 44 Z'
    s += body_clip('ne' + cid, P(eye_win, A.CAPY_BODY, SW * .85))
    s += '<circle cx="91.3" cy="41.5" r="2.9" fill="#1c110a"/><circle cx="92.3" cy="40.5" r=".9" fill="#ffffff"/>'
    s += L('M84 35 L99 38', 2.6)
    # 복면 주름선 + 이마 띠(BODY_D 안으로 깔끔하게!) + 이마 각도에 맞춘 은빛 서클릿 판
    plate = ('<g transform="rotate(8 97 22)">'
             + rr(86, 16, 22, 11, 3, G('#d4dce4', 'v', .86, .28), 2.2)
             + C(90, 21.5, 1.3, '#6b737e', 0) + C(104, 21.5, 1.3, '#6b737e', 0) + '</g>')
    s += body_clip('nm' + cid,
                   L('M72 62 Q100 56 134 60', 2.4, red2) + L('M78 74 Q104 68 132 72', 2.0, red2)
                   + F('M28 18 Q84 10 134 22 L134 32 Q84 20 28 28 Z', blk)
                   + L('M28 18 Q84 10 134 22 M28 28 Q84 20 134 32', 2.4) + plate)
    s += P(BODY_D, 'none', SW)
    s += arm(red, blk)
    return s


def c_shark(cid):
    blu, blu2, belly = '#80a7cc', '#638ab0', '#f2f6fa'
    # 갈라진 상어 꼬리 지느러미 + 매끄럽게 뒤로 젖혀진 등 지느러미
    s = P('M32 130 Q-4 134 -28 110 Q-22 138 -32 166 Q4 150 38 154 Z', G(blu, 'v', .86, .20), SW)
    s += P('M46 28 Q36 -10 56 -30 Q68 -10 86 14 Z', G(blu, 'v', .86, .24), SW)
    s += feet(blu, blu2)
    s += onesie(cid, blu, belly)
    # 이빨은 BODY_D 안으로 깔끔하게 잘라서 코 앞으로 절대 튀어나오지 않게!
    teeth = ''.join(P(f'M{104 + 36 * math.cos(math.radians(a - 9)):.1f} {56 + 33 * math.sin(math.radians(a - 9)):.1f} '
                      f'L{104 + 27 * math.cos(math.radians(a)):.1f} {56 + 25 * math.sin(math.radians(a)):.1f} '
                      f'L{104 + 36 * math.cos(math.radians(a + 9)):.1f} {56 + 33 * math.sin(math.radians(a + 9)):.1f} Z', '#ffffff', 1.8)
                    for a in (-158, -138, -118, -98, -78, 162, 182, 202))
    s += body_clip('st' + cid, teeth)
    s += P(BODY_D, 'none', SW)
    # 상어 후드 옆면 귀여운 까만 단추 눈
    s += C(60, 34, 4.4, '#1c110a', 0) + C(61.5, 32.5, 1.4, '#ffffff', 0)
    # 옆 지느러미 + 팔
    s += P('M66 106 Q46 116 42 132 Q60 128 74 114 Z', G(blu, 'v', .86, .20), SW * .8)
    s += arm(blu, '#cfe0f0')
    return s


def c_farmer(cid):
    yel, den, den2, straw = '#ffdc6c', '#5683bf', '#3f68a1', '#f0cc76'
    s = feet('#784e30', '#5c3b22') + bare(cid)
    # 노란 셔츠 + 파란 멜빵 청바지
    s += cloth(cid, F('M-20 80 L160 80 L160 200 L-20 200 Z', G(yel, 'v', .86, .22))
               + F('M-20 124 L160 122 L160 200 L-20 200 Z', G(den, 'v', .84, .20))
               + L('M-20 124 L160 122', 2.6)
               + L('M64 106 L52 90 M102 104 L102 88', 7.5) + L('M64 106 L52 90 M102 104 L102 88', 4.2, den)
               + rr(60, 102, 46, 26, 4, G(den, 'v', .86, .18), 2.6)
               + rr(72, 112, 22, 12, 3, den2, 2.0)
               + C(66, 108, 3.0, '#ffd54f', 1.6) + C(100, 107, 3.0, '#ffd54f', 1.6))
    # 접힌 노란 셔츠 깃
    s += P('M26 94 Q70 82 122 88 Q116 98 96 98 L82 92 L62 100 Q34 100 26 94 Z', G('#ffe58a'), SW * .8)
    s += face_bare(cid) + hat_shadow(cid, 'M30 35 Q82 24 134 34 L134 44 Q82 34 30 44 Z')
    s += arm(yel, '#e6bd48')
    # 넓은 밀짚모자 + 빨간 띠 (머리에 포근히 안착)
    s += P('M12 36 Q84 12 156 28 Q160 36 148 39 Q84 24 20 46 Q6 46 12 36 Z', G('#d8b054'), SW)
    hat_d = 'M46 28 Q48 -4 84 -6 Q122 -4 124 26 Q84 18 46 28 Z'
    s += P(hat_d, G(straw, 'v', .86, .26), SW)
    s += clip('fh' + cid, hat_d, F('M40 14 Q84 6 130 12 L130 24 Q84 18 40 26 Z', '#e05044')
              + L('M40 14 Q84 6 130 12 M40 24 Q84 18 130 22', 2.2)
              + L('M56 4 Q84 0 114 4', 1.6, '#cca246'))
    s += P(hat_d, 'none', SW)
    s += P('M12 36 Q84 14 156 28 Q158 35 148 38 Q84 24 20 45 Q8 45 12 36 Z', G(straw, 'v', .88, .20), SW)
    s += L('M30 36 Q84 20 138 30', 1.8, '#c49a40')
    return s


def c_pumpkin(cid):
    org, org2, grn, grn2 = '#f58e36', '#d46b1e', '#6aad4f', '#4e8738'
    s = feet('#4f3320', '#3b2416') + bare(cid)
    # 통통한 호박 수트 (배 정면에 귀여운 잭오랜턴 표정)
    p_d = 'M14 132 Q12 94 58 92 Q76 86 94 92 Q136 94 134 132 Q130 168 74 168 Q18 168 14 132 Z'
    s += P(p_d, G(org, 'r', .84, .26), SW)
    s += clip('pk' + cid, p_d,
              F('M28 104 Q44 96 52 146 Q32 146 28 104 Z', '#ffffff', .18)
              + L('M44 94 Q30 130 46 166 M74 90 L74 168 M104 94 Q118 130 102 166', 2.6, org2))
    s += P(p_d, 'none', SW)
    s += P('M46 118 L56 108 L63 120 Z', '#3e210d', 2.0) + P('M84 118 L94 108 L101 120 Z', '#3e210d', 2.0)
    s += P('M44 136 L54 144 L64 136 L74 146 L84 136 L94 144 L104 135 Q94 158 74 158 Q54 158 44 136 Z', '#3e210d', 2.0)
    # 목둘레 잎사귀 깃
    s += P('M34 94 Q48 84 62 94 Q74 82 88 94 Q102 84 118 92 Q112 104 96 100 Q84 108 72 98 Q58 108 46 100 Q34 104 34 94 Z', G(grn, 'v', .86, .22), SW * .8)
    s += face_bare(cid) + hat_shadow(cid, 'M48 24 Q82 16 116 24 L116 32 Q82 24 48 32 Z', .14)
    s += arm(A.CAPY_BODY, grn)
    # 뒤쪽 자연 귀 + 정수리 미니 호박 베레모 · 꼭지 · 잎 (이마 위 어색한 귀 혹 제거)
    s += P('M46 32 Q38 18 48 15 Q56 18 55 29 Z', G(A.CAPY_DARK, 'r'), SW)
    s += P('M52 22 Q50 2 82 0 Q114 2 114 22 Q82 15 52 22 Z', G(org, 'r', .84, .26), SW * .92)
    s += L('M70 2 Q68 11 70 18 M94 2 Q96 11 94 18', 2.0, org2)
    s += L('M82 2 Q82 -10 90 -14', 6.0) + L('M82 2 Q82 -10 90 -14', 2.8, grn2)
    s += P('M88 -10 Q102 -20 112 -8 Q100 0 88 -10 Z', G(grn), SW * .75)
    return s


def c_cowboy(cid):
    red, red2, den, hat, hat2 = '#dc554a', '#a8382f', '#527ab3', '#9e6c44', '#7a502e'
    plaid = (''.join(F(f'M{x} 80 L{x + 6} 80 L{x + 6} 150 L{x} 150 Z', red2, .65) for x in range(4, 150, 18))
             + ''.join(F(f'M-20 {y} L160 {y} L160 {y + 5.5} L-20 {y + 5.5} Z', red2, .65) for y in range(96, 138, 16)))
    s = feet('#7a4c2a', '#5c381e') + bare(cid)
    s += cloth(cid, F('M-20 80 L160 80 L160 200 L-20 200 Z', G(red, 'v', .86, .20)) + plaid
               + F('M-20 138 L160 136 L160 200 L-20 200 Z', G(den, 'v', .84, .20))
               + F('M-20 130 L160 128 L160 138 L-20 140 Z', '#5a3a22')
               + L('M-20 130 L160 128 M-20 140 L160 138', 2.4))
    # 가운데 금빛 카우보이 벨트 버클 + 빨간 카우보이 반다나 스카프
    s += rr(74, 128, 20, 14, 4, G('#ffd54f', 'r'), 2.4) + rr(80, 132, 8, 6, 2, '#5a3a22', 1.4)
    s += P('M24 94 Q72 82 122 88 Q122 98 106 102 L94 114 L78 100 Q38 102 24 94 Z', G('#e64b3e', 'v', .84, .22), SW * .85)
    s += face_bare(cid) + hat_shadow(cid, 'M30 34 Q82 22 134 32 L134 42 Q82 32 30 42 Z')
    s += arm(red, red2)
    # 클래식 카우보이 모자 (깔끔하게 다듬은 챙)
    crown = 'M48 24 Q46 -12 68 -14 Q84 -4 100 -14 Q124 -12 122 22 Q86 14 48 24 Z'
    s += P(crown, G(hat, 'v', .86, .26), SW)
    s += clip('ch' + cid, crown, F('M44 12 Q84 4 126 10 L126 20 Q84 14 44 22 Z', '#52331b')
              + L('M44 12 Q84 4 126 10', 2.2) + star(84, 13, 4.4, '#ffd54f', 1.2))
    s += P(crown, 'none', SW)
    s += P('M10 24 Q24 38 86 24 Q142 15 154 16 Q158 27 142 34 Q86 41 24 43 Q6 38 10 24 Z', G(hat, 'v', .86, .22), SW)
    s += L('M28 36 Q86 26 138 26', 2.0, hat2)
    return s


def c_winter(cid):
    tea, tea2, red, red2, crm = '#5aa999', '#428a7c', '#e05044', '#bd3a30', '#f4f8f5'
    s = feet('#70482e', '#563621', '#f6f2ea') + bare(cid)
    # 포근한 청록 노르딕 니트 스웨터
    knit = (''.join(P(f'M{x} 118 L{x + 6} 112 L{x + 12} 118 L{x + 6} 124 Z', crm, 0) for x in range(16, 132, 16))
            + ''.join(C(x + 6, 118, 2.0, red, 0) for x in range(16, 132, 16))
            + L('M10 108 L140 108 M10 128 L140 128', 2.0, crm)
            + F('M10 150 L140 150 L140 166 L10 166 Z', tea2)
            + ''.join(L(f'M{x} 150 L{x} 166', 1.6, tea) for x in range(20, 130, 8)))
    s += cloth(cid, F('M-20 80 L160 80 L160 200 L-20 200 Z', G(tea, 'v', .86, .20)) + knit)
    # 포근한 빨간 목도리 (목 감싸기 + 둥글게 내려오는 끝자락 · 수술)
    s += P('M46 98 Q42 126 42 146 L60 148 Q62 126 62 100 Z', G(red2, 'v', .86, .20), SW * .85)
    s += L('M43 138 L60 140 M44 128 L61 130', 2.4, crm)
    s += L('M45 148 L44 154 M51 148 L51 155 M57 148 L58 154', 2.4, red2)
    s += P('M24 90 Q74 78 124 86 Q128 98 118 104 Q74 94 32 106 Q20 100 24 90 Z', G(red, 'v', .86, .24), SW)
    s += L('M52 88 Q56 96 54 102 M82 86 Q84 94 82 99', 2.0, red2)
    s += face_bare(cid) + hat_shadow(cid, 'M34 32 Q82 22 132 30 L132 40 Q82 32 34 40 Z')
    s += arm(tea, crm)
    # 빨간 니트 비니 + 하얀 방울 + 보송보송 접힌 테두리 (귀 겹침 제거!)
    s += C(84, -15, 12.5, G('#ffffff', 'r', .90, .08), SW * .88)
    s += P('M42 26 Q44 -8 84 -10 Q124 -8 126 24 Z', G(red, 'v', .82, .26), SW)
    s += L('M64 -6 Q60 6 60 18 M84 -9 Q84 4 84 16 M104 -6 Q106 6 106 18', 2.0, red2)
    s += P('M36 28 Q36 16 48 16 Q84 10 120 16 Q132 16 130 28 Q128 34 118 32 Q84 26 48 34 Q36 36 36 28 Z', G('#f8f6f0', 'v', .92, .06), SW)
    s += L('M54 18 L53 31 M72 15 L72 28 M92 15 L92 28 M110 17 L111 30', 1.8, '#d4cec2')
    return s


def c_basic(cid):
    return None     # 기본 = earth_art.capy_guardian (우주복 · 유리 헬멧 · 산소통)


COSTUMES = [   # id, 이름, 손(장갑) 색
    ('basic', '우주 대원', A.SUIT_GR),
    ('wizard', '별빛 마법사', PAW), ('rain', '노란 우비', PAW), ('gnome', '숲 요정', PAW), ('pirate', '해적 선장', PAW),
    ('lion', '사자 잠옷', '#f3ba56'), ('dino', '공룡 잠옷', '#7ac466'), ('chef', '꼬마 요리사', PAW), ('bee', '꿀벌 잠옷', '#ffd23f'),
    ('knight', '은빛 기사', '#cdd4dc'), ('detective', '명탐정', PAW), ('hero', '슈퍼 카피', PAW), ('bunny', '토끼 잠옷', '#f7b8ca'),
    ('ninja', '닌자', '#382c26'), ('shark', '상어 잠옷', '#80a7cc'), ('farmer', '농부', PAW), ('pumpkin', '호박 유령', PAW),
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
    sil, sil2, sil3, neo = '#eef4fa', '#c5d1de', '#8e9eb0', '#6ff2ff'
    # 입체적인 제트 불꽃 + 별똥별 스파크
    flame = (blur(P('M-112 6 L-176 -8 L-156 12 L-188 28 L-144 26 L-164 46 L-108 22 Z', '#ff9f38', 0), 5, .85)
             + P('M-112 8 L-164 -2 L-148 12 L-174 26 L-138 23 L-152 40 L-108 20 Z', '#ffd24c', 3.2)
             + P('M-112 11 L-140 8 L-130 16 L-144 25 L-110 19 Z', '#fff7d6', 0)
             + star(-166, -12, 5.5, '#ffe880', 1.4) + star(-176, 42, 4.2, '#ffe880', 1.2))
    # 하단 꼬리 지느러미(핀) + 분사 노즐
    fin = P('M-84 22 L-104 38 L-68 24 Z', G(sil3), SW * .8) + rr(-122, 4, 16, 18, 4, G('#5a6472'), SW * .75)
    # 3/4 원근감 있는 은빛 우주 서프보드 (두께 옆면 + 윗면 데크 + 네온 라인)
    deck = 'M-114 4 Q-92 -10 20 -10 Q116 -10 156 -2 Q132 22 40 24 L-90 24 Q-116 22 -114 4 Z'
    side = 'M-114 10 Q-112 28 -88 30 L42 30 Q130 28 156 -2 Q132 18 40 20 L-90 20 Q-112 18 -114 10 Z'
    board = P(deck, G(sil, 'v', .86, .26), SW) + P(side, G(sil3, 'v', .82, .18), SW * .9)
    board += clip('sf', deck,
                  F('M-94 -2 Q20 -6 138 0 L138 6 Q20 2 -94 6 Z', '#ffffff', .65)
                  + L('M-92 10 Q20 12 128 4', 3.4, neo) + L('M-92 10 Q20 12 128 4', 1.4, '#e6feff')
                  + L('M-50 -10 L-58 24 M52 -10 L44 24', 1.8, sil2))
    board += P(deck, 'none', SW)
    board += blur(C(-68, 22, 4.5, neo, 0), 2.5, .85) + C(-68, 22, 2.8, '#e6feff', 1.5)
    board += blur(C(56, 22, 4.5, neo, 0), 2.5, .85) + C(56, 22, 2.8, '#e6feff', 1.5)
    return flame + fin + board, ''


def m_carpet():
    red, red2, blu, gold = '#cf404c', '#a12d38', '#3c5aa8', '#ffd54f'
    # 몽환적인 보라빛 마법 반짝이 꼬리
    trail = (blur(P('M-126 10 Q-162 2 -188 14 Q-160 28 -126 24 Z', '#c9b4ff', 0), 6, .75)
             + ''.join(star(x, y, r, '#ffe885', 1.3) for x, y, r in ((-156, 0, 5.2), (-178, 18, 4.0), (-148, 28, 3.8))))
    # 3/4 원근감으로 윗면 무늬가 잘 보이는 물결 양탄자
    top = 'M-118 -6 Q-56 -16 14 -10 Q84 -2 134 -12 Q140 8 118 24 Q60 32 -8 24 Q-78 18 -132 22 Q-130 6 -118 -6 Z'
    edge = 'M-132 22 Q-78 18 -8 24 Q60 32 118 24 L118 30 Q60 38 -8 30 Q-78 24 -132 28 Z'
    # 4개 모서리 금빛 수술
    tassels = ''
    for x, y, dx in ((-120, -2, -14), (-132, 24, -14), (134, -8, 14), (118, 26, 14)):
        tassels += L(f'M{x} {y} Q{x + dx * .6} {y + 6} {x + dx} {y + 12}', 3.4)
        tassels += L(f'M{x} {y} Q{x + dx * .6} {y + 6} {x + dx} {y + 12}', 1.6, gold)
        tassels += P(f'M{x + dx} {y + 10} L{x + dx - 5} {y + 20} L{x + dx + 5} {y + 20} Z', G(gold), 2.2)
    s = trail + tassels + P(edge, G(red2), SW * .85) + P(top, G(red, 'v', .84, .22), SW)
    s += clip('cp', top,
              P('M-104 0 Q-50 -8 14 -2 Q78 6 118 -2 Q122 10 106 18 Q54 24 -8 17 Q-70 12 -114 15 Z', G(blu, 'v', .86, .20), 2.8)
              + L('M-112 4 Q-52 -4 14 2 Q80 10 124 2', 3.4, gold)
              + P('M-16 8 L0 0 L16 8 L0 16 Z', gold, 1.6)
              + star(-62, 6, 4.8, gold, 1.3) + star(64, 10, 4.8, gold, 1.3)
              + C(-90, 9, 2.5, gold, 0) + C(94, 8, 2.5, gold, 0))
    s += P(top, 'none', SW)
    return s, ''


def m_plane():
    red, red2, red3, met, gold = '#e05044', '#bc3b30', '#962c24', '#cfd7e0', '#f0bd42'
    # 뒤쪽 부드러운 구름 배기 연기 + 꼬리 수직/수평 날개 + 반대편 위 날개
    smoke = (blur(C(-154, -26, 12, '#e6eef5', 0), 3, .75) + blur(C(-174, -34, 16, '#e6eef5', 0), 4, .55)
             + C(-150, -25, 8.5, '#ffffff', 0).replace('<circle', '<circle opacity=".82"')
             + C(-168, -33, 11, '#ffffff', 0).replace('<circle', '<circle opacity=".68"'))
    back = smoke
    back += P('M-118 -38 L-148 -92 Q-136 -100 -116 -92 L-88 -44 Z', G(red2, 'v', .84, .22), SW)
    back += L('M-126 -86 L-106 -48', 2.2, '#ffffff')
    back += P('M-22 -58 L84 -62 Q98 -54 84 -44 L-22 -42 Z', G(red2, 'v', .84, .20), SW)     # 반대쪽 날개
    # 앞쪽 동체 (조종석 턱 + 앞유리는 조종사 앞쪽(x=34..68)에 정확히 배치!)
    body = 'M-136 -42 Q-76 -52 -24 -48 Q12 -46 44 -48 Q98 -48 112 -24 Q116 4 98 20 Q42 34 -56 22 Q-118 10 -136 -28 Z'
    front = P(body, G(red, 'v', .84, .24), SW)
    front += clip('pb', body,
                  F('M-160 -2 L160 -8 L160 6 L-160 12 Z', '#fffaf0', .92)
                  + F('M-160 5 L160 -1 L160 4 L-160 10 Z', gold)
                  + F('M86 -56 L124 -56 L124 30 L86 30 Z', G(met, 'v', .84, .24))
                  + L('M86 -56 L86 30', 3.0)
                  + C(95, -24, 2.2, '#6b7582', 0) + C(95, -6, 2.2, '#6b7582', 0) + C(95, 10, 2.2, '#6b7582', 0))
    front += P(body, 'none', SW)
    # 조종석 갈색 가죽 테두리 + 앞쪽 방풍 유리(x=32..68)
    front += P('M-34 -48 Q0 -36 36 -48 Q38 -38 0 -30 Q-36 -38 -34 -48 Z', G('#7c4e2e'), SW * .8)
    front += P('M34 -48 Q46 -72 68 -66 Q66 -54 58 -46 Z', '#bfeaff', 3.2).replace('fill="#bfeaff"', 'fill="#bfeaff" fill-opacity=".75"')
    front += L('M44 -60 L54 -62', 2.4, '#ffffff')
    # 스팀펑크 작은 톱니 장식 + 아래 날개 + 지주대
    front += C(54, -10, 9.5, G(gold, 'r'), 2.4) + C(54, -10, 3.6, red2, 1.8)
    front += P('M-64 0 L72 2 Q90 12 72 20 L-64 18 Z', G(red2, 'v', .86, .22), SW)
    front += F('M-54 5 L64 7 L64 10 L-54 8 Z', '#ffffff', .35)
    # 튼튼한 바퀴 다리 + 바퀴
    front += L('M-22 18 L-28 46 M38 20 L44 46', 5.4) + L('M-22 18 L-28 46 M38 20 L44 46', 2.2, '#7d8794')
    front += C(-28, 48, 12.5, G('#3a3a42', 'r'), SW * .85) + C(44, 48, 12.5, G('#3a3a42', 'r'), SW * .85)
    front += C(-28, 48, 5.2, G(met, 'r'), 1.8) + C(44, 48, 5.2, G(met, 'r'), 1.8)
    # 프로펠러 허브 + 회전 잔상 + 나무 날개
    front += C(114, -14, 14, G(met, 'r', .84, .30), SW * .85)
    front += blur(E(130, -14, 10, 52, '#ffffff', 0), 3, .55) + E(130, -14, 8, 48, 'none', 2.2).replace(f'stroke="{A.INK}"', 'stroke="#9aa4b0" stroke-dasharray="7 6"')
    front += P('M127 -60 Q140 -36 130 -14 Q120 -36 127 -60 Z', G('#a66e42'), 2.8) + P('M130 -14 Q142 10 132 34 Q122 10 130 -14 Z', G('#a66e42'), 2.8)
    front += C(129, -14, 5.5, G(gold, 'r'), 2.2)
    return back, front


def m_ladybug():
    red, red2, blk = '#eb4b40', '#c4332a', '#343030'
    # 귀엽고 통통한 둥근 발 6개 (가는 막대기 다리 제거!)
    legs = ''.join(P(f'M{x - 8} 48 Q{x - 10 + d} 68 {x + d} 78 Q{x + 12 + d} 80 {x + 10 + d} 68 L{x + 6} 48 Z', G(blk), SW * .8)
                   for x, d in ((-72, -10), (-24, -4), (24, 4), (72, 10)))
    # 통통하고 사랑스러운 무당벌레 머리 (큰 반짝 눈 + 볼터치 + 미소 + 더듬이)
    head = (L('M108 10 Q100 -14 86 -20', 4.2) + C(84, -22, 5.8, blk, 2.2)
            + L('M126 10 Q132 -14 148 -18', 4.2) + C(150, -20, 5.8, blk, 2.2)
            + E(118, 36, 36, 32, G(blk, 'r', .84, .20), SW)
            + C(124, 29, 11.5, '#ffffff', 2.8) + C(143, 30, 9.5, '#ffffff', 2.8)
            + C(127, 30, 5.8, '#1c110a', 0) + C(145, 31, 4.8, '#1c110a', 0)
            + C(129, 27, 2.2, '#ffffff', 0) + C(146.5, 28.5, 1.8, '#ffffff', 0)
            + C(116, 44, 5.5, '#ff8e96', 0).replace('<circle', '<circle opacity=".55"')
            + L('M128 48 Q136 53 144 47', 2.6, '#f6d8cc'))
    # 높고 통통하게 부푼 광택 빨간 등딱지
    shell = 'M-112 50 C-116 -12 -52 -24 2 -24 C60 -24 112 -8 108 50 Q0 58 -112 50 Z'
    s = legs + head + P(shell, G(red, 'r', .82, .26), SW)
    s += clip('lb', shell,
              ''.join(C(x, y, r, '#292525', 0) for x, y, r in ((-68, 22, 16), (-18, 34, 14), (36, 12, 15), (76, 32, 13), (-92, 42, 11), (-10, -8, 11)))
              + L('M2 -24 Q0 14 2 54', 3.4)
              + P('M-84 6 Q-58 -16 -16 -16 Q-54 -4 -74 16 Z', '#ffffff', 0).replace('fill="#ffffff"', 'fill="#ffffff" opacity=".48"'))
    s += P(shell, 'none', SW)
    # 아래 검은 배 테두리
    s += P('M-114 48 Q0 56 110 48 L106 58 Q0 66 -110 58 Z', G(blk), SW * .82)
    return s, ''


def m_star():
    yel, yel2, glowc = '#ffe052', '#f2b824', '#ffd042'
    # 부드럽게 빛나는 다층 유성 꼬리 (뾰족 검은 바늘 제거!)
    tail = (blur(P('M-18 28 Q-108 -10 -186 -54 Q-122 14 -36 68 Z', '#ffb838', 0), 8, .75)
            + P('M-20 32 Q-104 -2 -172 -42 Q-112 16 -36 62 Z', '#ffe478', 0).replace('fill="#ffe478"', 'fill="#ffe478" opacity=".88"')
            + P('M-24 38 Q-86 14 -138 -16 Q-88 26 -38 56 Z', '#fff7d4', 0)
            + L('M-46 28 Q-108 0 -164 -34', 3.0, '#fffdf0')
            + star(-146, -42, 7.0, '#ffe87c', 1.6) + star(-168, 8, 5.2, '#ffe87c', 1.4) + star(-118, 32, 4.4, '#fff2b0', 1.2))
    # 둥글고 통통한 5각 별 (부드러운 둥근 꼭짓점 — 발이 위쪽 골짜기에 자연스럽게 안착)
    pts = []
    for i in range(10):
        rad = 78 if i % 2 == 0 else 42
        a = math.radians(90 + i * 36)
        pts.append((rad * math.cos(a), 42 + rad * math.sin(a)))
    # 베지에로 모서리를 부드럽게 라운딩
    segs = []
    n = len(pts)
    for i in range(n):
        p0, p1, p2 = pts[(i - 1) % n], pts[i], pts[(i + 1) % n]
        a_x, a_y = p1[0] + (p0[0] - p1[0]) * .22, p1[1] + (p0[1] - p1[1]) * .22
        b_x, b_y = p1[0] + (p2[0] - p1[0]) * .22, p1[1] + (p2[1] - p1[1]) * .22
        segs.append((f'M{a_x:.1f} {a_y:.1f}' if i == 0 else f'L{a_x:.1f} {a_y:.1f}') + f' Q{p1[0]:.1f} {p1[1]:.1f} {b_x:.1f} {b_y:.1f}')
    star_d = ' '.join(segs) + ' Z'
    s = tail + blur(P(star_d, glowc, 0), 6, .55) + P(star_d, G(yel, 'r', .84, .28), SW)
    s += clip('st', star_d, F('M-40 4 Q0 -12 36 4 Q0 10 -40 16 Z', '#ffffff', .35))
    s += P(star_d, 'none', SW)
    # 초롱초롱 귀여운 별 표정
    s += C(-16, 46, 5.0, '#1c110a', 0) + C(16, 46, 5.0, '#1c110a', 0)
    s += C(-14.5, 44, 1.8, '#ffffff', 0) + C(17.5, 44, 1.8, '#ffffff', 0)
    s += L('M-7 56 Q0 64 7 56', 3.0)
    s += C(-28, 56, 6.5, '#ff9898', 0).replace('<circle', '<circle opacity=".6"')
    s += C(28, 56, 6.5, '#ff9898', 0).replace('<circle', '<circle opacity=".6"')
    return s, ''


def m_seahorse():
    t1, t2, belly, fin = '#56cbb8', '#8f82d8', '#ffd07a', '#f58eb8'
    # 물방울 장식 + 등 지느러미 + 왕관 지느러미
    bubbles = (C(-64, -52, 7.0, '#d8f6ff', 2.2) + C(-62, -54, 2.0, '#ffffff', 0)
               + C(148, -36, 5.5, '#d8f6ff', 2.0) + C(-86, 40, 6.0, '#d8f6ff', 2.0))
    dorsal = P('M-34 8 Q-76 -6 -82 -44 Q-52 -36 -40 -26 Q-64 -62 -46 -84 Q-20 -52 -6 -28 Z', G(fin, 'r', .84, .24), SW * .9)
    dorsal += L('M-34 -12 L-70 -34 M-24 -24 L-48 -68', 2.0, '#d46492')
    crest = P('M60 -116 Q56 -144 74 -144 Q80 -132 84 -146 Q100 -144 98 -124 Q112 -136 118 -118 Q94 -116 60 -116 Z', G(fin, 'v', .86, .22), SW * .85)
    # 해마의 등/허리가 카피바라 자리(0,0) 바로 아래를 든든히 받치도록 S자 곡선 재설계!
    body = ('M-38 -2 Q-46 38 -22 72 Q6 108 50 108 Q86 106 88 76 Q88 54 64 54 Q46 56 50 74 '
            'Q60 68 66 76 Q64 88 46 86 Q20 82 22 46 Q26 24 62 12 Q96 -4 102 -50 '
            'Q106 -76 130 -80 L158 -78 Q166 -86 158 -96 L128 -100 Q114 -120 84 -120 '
            'Q52 -120 46 -88 Q42 -56 18 -24 Q-12 -10 -38 -2 Z')
    s = bubbles + dorsal + crest + P(body, G(t1, 'v', .82, .22), SW)
    # 부드러운 무지개(민트 → 라벤더) + 따뜻한 산호 노랑 배 주름 (배 안쪽으로 깔끔히 클리핑)
    sh_bel = 'M62 -56 Q96 -42 92 2 Q82 32 42 52 Q26 44 26 24 Q58 6 62 -56 Z'
    s += clip('shb', body,
              F('M-80 28 Q20 10 140 24 L140 130 L-80 130 Z', t2, .52)
              + P(sh_bel, G(belly, 'v', .88, .20), 2.4)
              + clip('shbr', sh_bel, ''.join(L(f'M40 {-22 + i * 16} Q74 {-16 + i * 16} 98 {-24 + i * 16}', 2.2, '#d99a44') for i in range(4)))
              + P(sh_bel, 'none', 2.4)
              + C(58, -86, 4.5, '#a8f0e4', 0) + C(44, -64, 3.8, '#a8f0e4', 0))
    s += P(body, 'none', SW)
    # 초롱초롱 귀여운 해마 눈 + 미소
    s += C(104, -98, 7.2, '#1c110a', 0) + C(106.5, -100.5, 2.4, '#ffffff', 0) + C(101.5, -95.5, 1.1, '#ffffff', 0)
    s += C(114, -86, 5.2, '#ff98a2', 0).replace('<circle', '<circle opacity=".55"')
    # 앞 층: 해마 등에 딱 밀착된 가죽 안장 + 황금 링 벨트 (절대 공중에 안 뜸!)
    front = P('M-34 -14 Q0 -26 38 -12 Q46 8 34 16 Q0 6 -34 14 Q-44 2 -34 -14 Z', G('#8f5e38', 'v', .86, .24), SW)
    front += L('M-26 -4 Q0 -12 30 -2', 2.4, '#ffd54f')
    front += L('M18 10 Q42 18 68 8', 5.2) + L('M18 10 Q42 18 68 8', 2.6, '#704626')
    front += C(54, 14, 7.2, G('#ffd54f', 'r'), 2.4) + C(54, 14, 2.8, '#8f5e38', 1.4)
    return s, front


def m_unicorn():
    wht, wht2, mane, mane2, gold, grn = '#fbf8ff', '#e4dcef', '#bca4e6', '#9b82cc', '#ffd54f', '#6ab850'
    # 풍성하게 흐르는 연보라 꼬리
    tail = (P('M-80 -8 Q-132 -16 -144 36 Q-122 30 -114 56 Q-102 32 -90 22 Q-82 14 -74 10 Z', G(mane, 'v', .84, .24), SW)
            + L('M-94 4 Q-120 6 -130 30 M-88 12 Q-108 20 -112 40', 2.2, '#e2d4f8'))
    # 뒤쪽 다리 2개 (살짝 어두운 입체감 + 둥근 발굽)
    far_legs = (P('M-56 26 Q-64 60 -62 96 Q-50 100 -42 96 Q-40 62 -36 28 Z', G(wht2), SW)
                + P('M-63 86 Q-52 82 -42 86 L-41 97 Q-52 102 -63 97 Z', '#a79bb8', SW * .8)
                + P('M36 26 Q42 60 44 96 Q56 100 64 96 Q62 60 56 26 Z', G(wht2), SW)
                + P('M43 86 Q54 82 64 86 L65 97 Q54 102 43 97 Z', '#a79bb8', SW * .8))
    # 몸통 + 우아하게 이어진 목 + 귀여운 머리를 하나의 매끄러운 외곽선으로! (목 절단선 제거!)
    body_all = ('M-84 4 C-86 -18 -64 -26 -24 -24 L38 -24 C56 -48 68 -80 86 -100 '
                'C100 -114 122 -110 134 -96 L154 -72 C160 -60 150 -48 134 -50 L114 -54 '
                'C104 -28 100 10 92 28 C82 46 52 48 18 46 L-36 46 C-76 46 -84 26 -84 4 Z')
    # 앞쪽 다리 2개 (윗부분 닫는 선 없이 배 색과 매끄럽게 이어지게!)
    leg_c = '#f1ecf8'
    near_legs = (F('M-40 24 Q-44 60 -40 100 Q-26 104 -18 100 Q-18 62 -16 26 Z', G(leg_c, 'v', .92, .0))
                 + L('M-40 26 Q-44 60 -40 100 Q-26 104 -18 100 Q-18 62 -16 32', SW)
                 + P('M-41 89 Q-29 85 -18 89 L-17 101 Q-29 106 -41 101 Z', '#b5a8c9', SW * .8)
                 + F('M54 22 Q64 58 72 98 Q86 100 92 94 Q84 56 76 20 Z', G(leg_c, 'v', .92, .0))
                 + L('M55 30 Q64 58 72 98 Q86 100 92 94 Q84 56 76 24', SW)
                 + P('M70 87 Q82 82 91 85 L93 96 Q82 102 71 98 Z', '#b5a8c9', SW * .8))
    # 갈기 + 금빛 나선 뿔 + 귀
    horn = P('M112 -100 L128 -154 L130 -96 Z', G(gold, 'v', .86, .28), SW * .82) + L('M116 -112 L128 -116 M119 -126 L128 -130 M122 -138 L128 -141', 2.0, '#c48f18')
    ear = P('M102 -102 Q96 -124 110 -122 Q118 -116 114 -102 Z', G(wht), SW * .8) + F('M104 -105 Q102 -117 109 -116 Q112 -112 110 -105 Z', '#ffd4e2')
    mane_f = (P('M96 -104 Q64 -94 52 -56 Q60 -58 64 -48 Q50 -36 48 -18 Q70 -24 80 -48 Q90 -72 104 -92 Z', G(mane, 'v', .86, .22), SW * .9)
              + L('M88 -90 Q70 -76 62 -52', 2.2, '#e2d4f8'))
    # 숲속 꽃·덩굴 목걸이 (참고 이미지 포인트!)
    garland = (L('M52 -24 Q74 -12 98 -22', 4.4, grn)
               + C(64, -18, 5.2, '#ff9eb8', 1.8) + C(84, -17, 5.2, '#ffe066', 1.8) + C(74, -15, 2.2, '#ffffff', 0))
    # 귀여운 눈 · 볼터치 · 미소
    face = (C(126, -84, 5.8, '#5a3f94', 0) + C(128, -86, 2.0, '#ffffff', 0)
            + C(135, -74, 5.0, '#ff9eb8', 0).replace('<circle', '<circle opacity=".52"')
            + L('M138 -60 Q145 -58 150 -62', 2.2))
    s = tail + far_legs + P(body_all, G(wht, 'v', .92, .08), SW) + near_legs + horn + ear + mane_f + garland + face
    # 앞 층: 숲속 나뭇잎 안장 + 꽃 장식 (막대기 끈 대신 깔끔한 잎 띠)
    front = P('M-34 -26 Q0 -36 36 -26 Q44 -6 32 4 Q0 -4 -34 4 Q-44 -10 -34 -26 Z', G(grn, 'v', .86, .24), SW)
    front += L('M-22 -10 Q0 -16 24 -10', 2.0, '#4c8e36')
    front += P('M-4 0 Q2 22 -4 44 L4 44 Q10 22 4 0 Z', G('#54943e'), 2.2)
    front += C(-12, -24, 5.5, '#ff9eb8', 2.0) + C(14, -24, 4.8, '#ffd54f', 2.0) + C(-12, -24, 2.0, '#fff4b8', 0)
    return s, front


def m_hoverbike():
    blk, blk2, stl, neo = '#30394a', '#1e2532', '#58667c', '#58f2ff'
    # 강렬한 플라즈마 제트 불꽃 + 배기 머플러
    jet = (blur(P('M-126 -6 L-184 -20 L-164 -2 L-192 14 L-126 10 Z', '#ff9b38', 0), 5, .88)
           + P('M-126 -6 L-170 -16 L-154 -2 L-178 10 L-126 7 Z', '#ffd24c', 3.0)
           + P('M-126 -2 L-148 -6 L-140 2 L-152 6 L-126 4 Z', '#fff7d6', 0))
    muffler = P('M-132 -10 L-96 -16 L-94 8 L-130 12 Z', G(stl), SW * .85)
    # 네온 호버 휠(앞·뒤 공중부양 발광 링!)
    wheels = ''
    for cx in (-76, 82):
        wheels += (blur(E(cx, 48, 34, 8, neo, 0), 6, .9)
                   + C(cx, 24, 25, G(blk2, 'r'), SW * .9)
                   + C(cx, 24, 15, 'none', 4.5).replace(f'stroke="{A.INK}"', f'stroke="{neo}"')
                   + C(cx, 24, 15, 'none', 1.8).replace(f'stroke="{A.INK}"', 'stroke="#e8feff"')
                   + C(cx, 24, 7, G(stl, 'r'), 2.0))
    back = jet + muffler + wheels
    # 미래형 스포츠 호버바이크 차체 (앞 카울 · 네온 스트립 · 홀로그램 방풍창)
    body = ('M-122 -22 Q-86 -42 -24 -36 L34 -36 Q66 -62 114 -48 Q138 -36 138 -12 '
            'Q136 14 102 22 L-64 24 Q-114 22 -122 -22 Z')
    front = P(body, G(blk, 'v', .82, .26), SW)
    front += clip('hb', body,
                  P('M-108 -20 Q-30 -28 42 -26 L124 -36 L124 -12 L-108 0 Z', G(stl, 'v', .86, .20), 2.4)
                  + blur(L('M-112 6 Q-10 16 128 -18', 7, neo), 3, .95)
                  + L('M-112 6 Q-10 16 128 -18', 2.8, '#ebfeff')
                  + L('M46 -32 L78 -10', 3.0, neo))
    front += P(body, 'none', SW)
    # 홀로그램 시안 윈드실드 + LED 헤드라이트 + 안장
    front += P('M56 -48 Q84 -88 122 -74 Q114 -50 96 -40 Z', '#8ff6ff', 3.2).replace('fill="#8ff6ff"', 'fill="#8ff6ff" fill-opacity=".62"')
    front += L('M74 -68 Q94 -76 110 -68', 2.4, '#ffffff')
    front += blur(P('M112 -28 L136 -26 L132 -14 L110 -18 Z', neo, 0), 3, .9) + P('M114 -26 L134 -24 L130 -16 L112 -19 Z', '#ebfeff', 1.8)
    front += P('M-44 -42 Q-4 -48 34 -40 Q38 -30 28 -28 L-38 -30 Q-48 -34 -44 -42 Z', G('#4c586c'), SW * .8)
    return back, front


def m_trex():
    br, br2, br3, belly = '#de944e', '#ba7232', '#945620', '#f7cf96'
    # 뒤쪽 통통한 다리 + 발톱
    far_leg = (P('M-42 22 Q-58 58 -52 90 Q-34 94 -22 88 Q-20 58 -16 26 Z', G(br2), SW)
               + P('M-56 84 Q-40 78 -22 86 Q-20 96 -54 96 Q-62 94 -56 84 Z', G(br2), SW * .85))
    # 하나의 매끄러운 아기 티라노 실루엣 (꼬리 + 몸통 + 목 + 통통한 머리 — 목 절단선 완전 제거!)
    dino_d = ('M-174 14 C-136 -8 -90 -16 -58 -22 C-34 -34 10 -34 46 -34 '
              'C66 -44 74 -70 84 -92 C94 -112 128 -116 154 -106 C174 -98 180 -80 174 -64 '
              'C168 -50 148 -46 122 -48 C108 -34 106 -6 96 16 C80 42 40 50 -10 46 '
              'C-58 42 -118 32 -174 14 Z')
    # 앞쪽 통통한 뒷다리 (허벅지가 몸에서 매끄럽게 이어지게 윗선 생략!) + 하얀 발톱 2개
    near_leg = (F('M12 12 Q-4 46 8 88 Q28 94 42 86 Q44 52 46 20 Z', G(br, 'v', .86, .20))
                + L('M10 18 Q-4 46 8 88 Q28 94 42 86 Q44 54 45 28', SW)
                + P('M6 82 Q24 76 48 84 Q56 96 14 96 Q4 94 6 82 Z', G(br, 'v', .86, .20), SW * .9)
                + P('M46 86 L55 92 L46 95 Z', '#fff8eb', 1.8) + P('M38 89 L46 95 L38 96 Z', '#fff8eb', 1.8))
    s = far_leg + P(dino_d, G(br, 'v', .84, .22), SW)
    # 크림색 배 + 줄무늬 반점
    s += clip('tb', dino_d,
              P('M-34 38 Q32 54 94 12 Q104 -14 94 -38 Q78 -16 66 8 Q26 24 -34 20 Z', G(belly, 'v', .90, .14), 2.4)
              + ''.join(L(f'M{x} {y} Q{x + 14} {y + 6} {x + 28} {y - 2}', 2.0, '#d6a468') for x, y in ((24, 24), (48, 12), (66, -4)))
              + ''.join(E(x, y, rx, ry, br2, 0, rot) for x, y, rx, ry, rot in ((-108, 6, 12, 6, -12), (-74, -4, 14, 7, -8), (-42, -16, 12, 6, 0), (114, -98, 10, 5, 10))))
    s += P(dino_d, 'none', SW) + near_leg
    # 귀여운 짧은 티라노 앞발 + 반짝 눈 + 환한 이빨 미소!
    s += P('M86 -10 Q106 -14 110 -2 Q104 8 86 2 Z', G(br, 'v', .86, .20), SW * .8) + L('M106 -6 L112 -4 M105 0 L110 3', 2.0)
    s += L('M114 -64 Q146 -62 170 -70', 3.0)
    s += ''.join(P(f'M{x} -65 L{x + 4.5} -56 L{x + 9} -66 Z', '#ffffff', 1.8) for x in (122, 136, 150))
    s += C(132, -88, 6.2, '#1c110a', 0) + C(134, -90, 2.2, '#ffffff', 0) + C(162, -84, 2.2, br3, 0)
    s += C(140, -76, 5.2, '#ff9898', 0).replace('<circle', '<circle opacity=".45"')
    # 앞 층: 곡선에 딱 밀착된 원시 표범무늬 가죽 안장 + 배 끈
    sad_d = 'M-34 -34 Q0 -44 36 -34 Q44 -12 32 -2 Q0 -10 -34 -2 Q-44 -16 -34 -34 Z'
    front = P(sad_d, G('#efb954', 'v', .86, .24), SW)
    front += clip('ts', sad_d, ''.join(C(x, y, r, '#7c481c', 0) for x, y, r in ((-18, -24, 4.5), (2, -16, 4.0), (20, -26, 4.5), (22, -10, 3.6), (-10, -10, 3.6))))
    front += P(sad_d, 'none', SW) + P('M-4 -4 Q0 20 -2 42 L6 42 Q8 20 4 -4 Z', G('#7c481c'), 2.2)
    return s, front


def m_broom():
    wood, wood2, straw, straw2, red = '#9c683e', '#754b2a', '#edd074', '#c9a042', '#d94848'
    # 반짝반짝 마법 별가루
    sparks = ''.join(star(x, y, r, '#ffe880', 1.6) for x, y, r in ((-174, 22, 7.2), (-154, 52, 5.4), (-190, 46, 4.2), (-134, 66, 4.2), (-166, -12, 4.5)))
    # 풍성한 마법 빗자루 솔 (결선 + 명암)
    bristle = 'M-46 -4 Q-108 -22 -156 -4 Q-166 20 -152 46 Q-108 50 -46 20 Z'
    s = sparks + P(bristle, G(straw, 'v', .84, .24), SW)
    s += clip('br', bristle,
              L('M-54 0 Q-104 -6 -152 4 M-54 8 Q-104 12 -154 22 M-54 14 Q-102 28 -148 38', 2.4, straw2)
              + F('M-150 -8 Q-100 -18 -54 -4 L-54 4 Q-100 -8 -150 2 Z', '#ffffff', .28))
    s += P(bristle, 'none', SW)
    # 붉은 마법 리본 매듭
    s += P('M-58 -6 Q-46 -12 -38 -6 L-38 22 Q-46 28 -58 22 Z', G(red, 'v', .86, .22), SW * .85)
    s += P('M-50 22 L-62 42 L-50 38 L-44 22 Z', G('#b83434'), 2.4) + L('M-58 4 L-38 4 M-58 14 L-38 14', 2.0, '#ffd54f')
    # 살짝 휘어진 원목 마법 자루
    s += limb('M-40 8 Q56 0 152 -8', 13, wood, SW) + L('M-26 5 Q56 -2 142 -9', 2.2, '#c89c70')
    # 앞 끝 따뜻한 금빛 마법 랜턴 (은은한 광채!)
    s += L('M150 -8 L156 12', 3.4) + blur(C(156, 32, 18, '#ffd54f', 0), 6, .75)
    s += rr(144, 14, 24, 34, 6, G('#fff1a8', 'r', .88, .26), SW * .82)
    s += L('M156 14 L156 48 M144 31 L168 31', 2.0, '#785228')
    s += P('M140 14 Q156 2 172 14 Z', G('#5a3c24'), 2.8) + rr(142, 46, 28, 6, 3, '#5a3c24', 2.6)
    return s, ''


def m_none():
    return '', ''


MOUNTS = [   # id, 이름, 떠다님(1 = 둥실)
    ('none', '탈것 없음', 0),
    ('surf', '은빛 서프보드', 1), ('carpet', '하늘 양탄자', 1), ('plane', '빨간 비행기', 1), ('ladybug', '무당벌레', 0), ('star', '별똥별', 1),
    ('seahorse', '무지개 해마', 1), ('unicorn', '숲속 유니콘', 0), ('hoverbike', '호버바이크', 1), ('trex', '아기 티라노', 0), ('broom', '마법 빗자루', 1),
]
MFN = {m[0]: globals()['m_' + m[0]] for m in MOUNTS}


# ------------------------------------------------------------------ 추천 신규 탈것 시안 6종 (게임 반영 전 미리보기용)
def bm_tub():
    wd, wd2, wd3, wat = '#dfa464', '#bf8242', '#945f2a', '#8ce6f5'
    # 등 뒤 몽글몽글 온천 김 + 뒤쪽 나무 욕조 벽
    steam = (L('M-96 -36 Q-108 -56 -96 -74 Q-84 -92 -96 -108', 4.0, '#bfe6f2')
             + L('M-74 -44 Q-84 -60 -74 -76', 3.2, '#bfe6f2')).replace('<path', '<path opacity=".72"')
    back = steam + E(0, -10, 108, 30, G(wd2), SW) + E(0, -8, 96, 22, G(wat, 'r', .90, .24), 2.8)
    # 욕조 물 위에 동동 뜬 유자 2알 + 작은 오리 인형!
    back += E(-68, -14, 13, 10, G('#ffa62b', 'r', .84, .28), 2.6) + P('M-64 -24 Q-54 -30 -48 -22 Q-56 -18 -64 -24 Z', '#6ab84c', 2.0)
    back += E(72, -16, 14, 11, G('#ffe05c', 'r'), 2.4) + C(82, -26, 9, G('#ffe05c', 'r'), 2.4) + P('M88 -26 L98 -23 L88 -20 Z', '#ff9436', 1.8) + C(84, -27, 1.8, '#1c110a', 0)
    # 앞 층: 히노키 나무통 앞면 + 금빛 테두리 띠 + 물결 반짝임
    tub_f = 'M-108 -10 Q0 18 108 -10 L96 66 Q0 88 -96 66 Z'
    front = P(tub_f, G(wd, 'v', .84, .22), SW)
    front += clip('tb_f', tub_f,
                  ''.join(L(f'M{x} -20 L{x * .9:.0f} 90', 2.2, wd2) for x in (-72, -36, 0, 36, 72))
                  + P('M-114 14 Q0 38 114 14 L114 26 Q0 50 -114 26 Z', G('#ffd54f', 'v', .86, .22), 2.4)
                  + P('M-108 46 Q0 70 108 46 L108 58 Q0 82 -108 58 Z', G('#ffd54f', 'v', .86, .22), 2.4))
    front += P(tub_f, 'none', SW)
    front += L('M-84 0 Q-42 12 0 10 Q42 12 84 0', 3.4, '#d8faff')
    return back, front


def bm_cloud():
    wht, sky = '#ffffff', '#e4f2ff'
    # 뒤쪽 파스텔 무지개 아치 + 반짝 별
    rb = (L('M-136 8 Q-84 -96 14 -44', 12, '#ff9eb5') + L('M-128 14 Q-80 -82 10 -34', 11, '#ffe078')
          + L('M-120 20 Q-76 -68 6 -24', 10, '#8fe2b8') + L('M-112 26 Q-72 -54 2 -14', 9, '#8fd0ff'))
    sparks = star(-138, 36, 6.2, '#ffe880', 1.6) + star(130, -24, 5.2, '#ffe880', 1.4) + star(-96, 62, 4.5, '#ffbed0', 1.4)
    # 뭉게뭉게 구름 본체 (뒤 + 앞 폭신 쿠션)
    back = rb + sparks + P('M-118 20 C-138 -6 -108 -36 -74 -26 C-62 -56 -16 -58 4 -34 C32 -58 84 -48 92 -16 C126 -16 142 18 116 42 C94 64 -92 64 -118 20 Z', G(wht, 'v', .92, .08), SW)
    front = P('M-106 16 C-122 36 -96 66 -54 58 C-28 74 28 74 54 58 C96 66 122 36 106 14 C86 24 62 12 36 16 C14 2 -14 2 -36 16 C-62 12 -86 24 -106 16 Z', G(wht, 'v', .92, .08), SW)
    front += F('M-94 46 Q0 66 94 46 Q68 64 0 64 Q-68 64 -94 46 Z', sky, .85)
    # 행복한 구름 얼굴 (웃는 눈 ^ ^ + 볼터치)
    front += L('M-26 36 Q-19 29 -12 36 M12 36 Q19 29 26 36', 3.4) + L('M-6 44 Q0 50 6 44', 3.0)
    front += C(-34, 44, 6.5, '#ff9eb5', 0).replace('<circle', '<circle opacity=".6"') + C(34, 44, 6.5, '#ff9eb5', 0).replace('<circle', '<circle opacity=".6"')
    return back, front


def bm_ufo():
    mnt, mnt2, sil, neo = '#86e2d0', '#58bfa9', '#dfe7f0', '#6ff2ff'
    # 아래 신비로운 시안 광선 빔
    beam = (blur(P('M-46 28 L-86 84 Q0 96 86 84 L46 28 Z', neo, 0), 7, .48)
            + P('M-34 28 L-62 78 Q0 88 62 78 L34 28 Z', '#d4feff', 0).replace('fill="#d4feff"', 'fill="#d4feff" opacity=".45"'))
    # 오픈형 미니 UFO 조종석 (키 큰 모자 코스튬도 걸리지 않게 깔끔한 링 + 뒤 등받이!)
    back = (beam
            + E(0, -8, 128, 34, G(sil, 'v', .86, .24), SW)
            + E(0, -10, 74, 18, G('#3c4b66', 'v', .82, .20), 2.8)
            + P('M-62 -16 Q-58 -52 -32 -48 Q-38 -28 -36 -10 Z', G(mnt2), SW * .8))
    # 앞 층: 파스텔 민트 UFO 원반 앞면 + 알록달록 점멸등 + 앞쪽 작은 계기판 방풍창
    front = P('M-130 -8 Q0 28 130 -8 Q122 28 0 38 Q-122 28 -130 -8 Z', G(mnt, 'v', .84, .24), SW)
    front += P('M-84 22 Q0 42 84 22 Q64 40 0 46 Q-64 40 -84 22 Z', G(mnt2), SW * .85)
    front += P('M42 -12 Q58 -40 82 -32 Q78 -16 66 -8 Z', '#b8f6ff', 2.8).replace('fill="#b8f6ff"', 'fill="#b8f6ff" fill-opacity=".68"')
    for x, y, c in ((-92, 6, '#ff8aa6'), (-48, 18, '#ffe066'), (0, 22, '#6ff2ff'), (48, 18, '#ffe066'), (92, 6, '#ff8aa6')):
        front += blur(C(x, y, 7.5, c, 0), 2.5, .8) + C(x, y, 5.5, c, 2.2) + C(x - 1.5, y - 1.5, 1.8, '#ffffff', 0)
    front += L('M-96 -2 Q0 20 96 -2', 2.4, '#ffffff')
    return back, front


def bm_duckboat():
    yel, yel2, org, wat = '#ffe05c', '#ebc02c', '#ff9436', '#8ce2ff'
    # 물결 링
    rip = E(0, 62, 126, 18, '#d4f4ff', 2.6).replace('<ellipse', '<ellipse opacity=".75"')
    # 통통한 대형 오리 보트 (꼬리 + 몸통 + 둥근 오리 머리 + 주황 부리)
    body = ('M-116 20 Q-132 -4 -110 -12 Q-92 -4 -74 -14 L50 -14 C64 -44 74 -84 106 -86 '
            'C138 -88 152 -62 146 -34 C142 -12 126 4 124 22 C122 52 72 68 0 68 C-76 68 -114 48 -116 20 Z')
    beak = P('M140 -58 Q174 -58 176 -42 Q166 -28 138 -36 Z', G(org, 'v', .86, .26), SW * .9)
    tuft = P('M96 -84 Q92 -106 106 -104 Q112 -94 110 -84 Z', G(yel), SW * .8)
    back = rip + tuft + beak + P(body, G(yel, 'v', .86, .24), SW)
    back += C(124, -58, 6.2, '#1c110a', 0) + C(126, -60, 2.2, '#ffffff', 0)
    back += C(116, -42, 6.5, '#ff9898', 0).replace('<circle', '<circle opacity=".55"')
    # 앞 층: 오리 보트 옆 날개 + 시트 테두리
    front = P('M-52 -8 Q-12 -16 32 -8 Q46 18 28 32 Q-12 38 -52 24 Q-64 8 -52 -8 Z', G(yel2, 'v', .88, .22), SW)
    front += P('M-34 8 Q-4 2 24 12 Q8 30 -28 24 Q-42 18 -34 8 Z', G('#fff4b8'), SW * .75)
    return back, front


def bm_whale():
    blu, blu2, bel, wat = '#7ec2f0', '#589fd4', '#eef8ff', '#b8f0ff'
    # 머리 위 반짝 물줄기 분수!
    spout = (P('M102 -64 Q94 -102 68 -110 Q84 -92 94 -64 Z', G(wat), 2.4)
             + P('M106 -64 Q118 -102 144 -106 Q126 -88 112 -64 Z', G(wat), 2.4)
             + star(66, -114, 5.2, '#ffe880', 1.4) + star(146, -110, 4.8, '#ffe880', 1.4))
    # 꼬리 지느러미 + 통통한 하늘 아기 고래 몸통
    tail = P('M-112 10 Q-148 -28 -174 -16 Q-154 6 -136 16 Q-166 28 -164 52 Q-132 40 -106 24 Z', G(blu2, 'v', .86, .20), SW)
    body = ('M-114 14 C-102 -30 -44 -56 30 -58 C104 -60 156 -30 158 14 '
            'C160 52 106 66 28 66 C-48 66 -98 44 -114 14 Z')
    back = spout + tail + P(body, G(blu, 'v', .84, .24), SW)
    back += clip('wh', body,
                 P('M-60 30 Q36 22 162 24 L162 80 L-60 80 Z', G(bel, 'v', .92, .08), 2.6)
                 + ''.join(L(f'M{x} 30 L{x - 6} 66', 2.0, '#c8dff0') for x in (20, 52, 84, 116))
                 + F('M-50 -40 Q30 -56 106 -38 Q30 -40 -50 -24 Z', '#ffffff', .30))
    back += P(body, 'none', SW)
    # 고래 미소 + 반짝 눈 + 볼터치 + 옆 지느러미
    back += C(120, 2, 6.2, '#1c110a', 0) + C(122, 0, 2.2, '#ffffff', 0)
    back += C(108, 16, 6.2, '#ff9eb5', 0).replace('<circle', '<circle opacity=".52"')
    back += L('M118 26 Q142 30 154 18', 3.0)
    back += P('M30 34 Q10 66 26 76 Q52 68 60 36 Z', G(blu2, 'v', .86, .22), SW * .88)
    # 앞 층: 빨간 금테 쿠션 안장
    front = P('M-34 -30 Q0 -40 36 -30 Q42 -12 30 -4 Q0 -12 -32 -4 Q-44 -16 -34 -30 Z', G('#e05044', 'v', .86, .22), SW)
    front += L('M-26 -18 Q0 -24 28 -18', 2.6, '#ffd54f')
    return back, front


def bm_corgi():
    tan, tan2, wht, red = '#eba452', '#cc8334', '#fffaf2', '#e05044'
    # 통통한 코기 복숭아 엉덩이 꼬리 + 뒤쪽 짧은 다리
    butt = C(-96, 6, 14, G(wht, 'r'), SW * .85)
    far_legs = (P('M-64 32 Q-68 62 -64 82 Q-50 86 -42 80 Q-40 60 -40 32 Z', G('#e8dfd2'), SW)
                + P('M34 32 Q36 62 42 82 Q56 86 64 80 Q62 58 58 32 Z', G('#e8dfd2'), SW))
    # 하나의 매끄러운 웰시코기 몸 + 목 + 동그란 코기 머리!
    body = ('M-94 8 C-94 -22 -64 -28 -16 -26 L44 -26 C58 -52 68 -86 102 -88 '
            'C134 -90 154 -66 154 -44 C162 -34 158 -16 140 -14 L118 -14 '
            'C116 16 100 44 64 46 L-44 46 C-80 46 -94 32 -94 8 Z')
    # 쫑긋 큰 코기 귀 2개
    ears_c = (P('M80 -80 Q66 -122 86 -124 Q102 -112 98 -82 Z', G(tan, 'v', .86, .24), SW * .9)
              + P('M84 -86 Q76 -112 88 -114 Q95 -106 94 -86 Z', '#ffccd8', 0)
              + P('M112 -82 Q122 -122 140 -116 Q146 -98 128 -78 Z', G(tan, 'v', .86, .24), SW * .9)
              + P('M116 -84 Q124 -110 134 -106 Q138 -96 125 -82 Z', '#ffccd8', 0))
    # 앞쪽 짧은 통통 다리 (윗선 생략해 몸에서 자연스럽게 이어지게!)
    near_legs = (F('M-48 28 Q-50 64 -44 86 Q-28 90 -20 84 Q-20 62 -22 28 Z', G(wht, 'v', .92, .08))
                 + L('M-48 34 Q-50 64 -44 86 Q-28 90 -20 84 Q-20 62 -22 38', SW)
                 + F('M54 26 Q60 62 68 84 Q84 88 90 80 Q84 58 78 26 Z', G(wht, 'v', .92, .08))
                 + L('M56 36 Q60 62 68 84 Q84 88 90 80 Q84 58 78 30', SW))
    back = butt + far_legs + ears_c + P(body, G(tan, 'v', .86, .22), SW)
    back += clip('cg', body,
                 P('M76 -26 Q116 -24 126 14 Q110 52 54 52 Q66 12 76 -26 Z', G(wht, 'v', .94, .06), 0)
                 + P('M-80 28 Q0 40 70 30 L70 58 L-80 58 Z', wht, 0)
                 + P('M124 -48 Q144 -54 164 -38 L164 -10 L118 -10 Q114 -34 124 -48 Z', wht, 0))
    back += P(body, 'none', SW) + near_legs
    # 행복한 코기 얼굴 (반짝 눈 + 까만 코 + 메롱 핑크 혀!)
    back += (C(122, -56, 5.8, '#1c110a', 0) + C(124, -58, 2.0, '#ffffff', 0)
             + E(153, -36, 5.5, 4.2, '#241c1a', 0)
             + P('M136 -22 Q144 -10 152 -22 Z', '#ff8aa6', 2.2)
             + L('M128 -26 Q138 -20 150 -26', 2.6)
             + C(116, -42, 5.5, '#ff9898', 0).replace('<circle', '<circle opacity=".5"'))
    # 앞 층: 빨간 체크 안장 + 벨트
    sad = 'M-34 -32 Q0 -42 36 -32 Q44 -10 32 0 Q0 -8 -34 0 Q-44 -14 -34 -32 Z'
    front = P(sad, G(red, 'v', .86, .22), SW)
    front += clip('cgs', sad, L('M-20 -44 L-20 8 M0 -44 L0 8 M20 -44 L20 8 M-44 -18 L44 -18', 2.4, '#b02e28'))
    front += P(sad, 'none', SW) + P('M-4 -2 L-4 44 L4 44 L4 -2 Z', G('#7c4e2e'), 2.2)
    return back, front


BONUS_MOUNTS = [
    ('tub', '온천 나무 욕조', 1, 'onsen'),
    ('cloud', '무지개 솜구름', 1, 'angel'),
    ('ufo', '미니 UFO', 1, 'basic'),
    ('duckboat', '오리 보트', 1, 'duck'),
    ('whale', '하늘 아기 고래', 1, 'shark'),
    ('corgi', '통통 웰시코기', 0, 'winter'),
]
BMFN = {m[0]: globals()['bm_' + m[0]] for m in BONUS_MOUNTS}


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
    cap_jpg = os.path.join(ROOT, '캡쳐본 모음', '코스튬_탈것_캡처', 'look_sheet.jpg')
    if os.path.isdir(os.path.dirname(cap_jpg)):
        im.convert('RGB').save(cap_jpg, quality=90); print(cap_jpg)


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


def costume_sheet():
    """코스튬 19종 크게 보기 (총 든 모습 + 총 없는 _float 모습 나란히)"""
    cw, ch, cols = 420, 280, 4
    rows = (len(COSTUMES) + cols - 1) // cols
    svg = f'<rect width="{cols * cw}" height="{rows * ch}" fill="#f7f1e3"/>' + A.font_css()
    for i, (cid, name, _) in enumerate(COSTUMES):
        x, y = (i % cols) * cw, (i // cols) * ch
        svg += f'<rect x="{x + 6}" y="{y + 6}" width="{cw - 12}" height="{ch - 12}" rx="16" fill="#ffffff" opacity=".7"/>'
        svg += f'<g transform="translate({x + 28},{y + 78}) scale(.95)">{costume_float(cid)}</g>'
        svg += f'<g transform="translate({x + 216},{y + 78}) scale(.95)">{costume_full(cid)}</g>'
        svg += A.T(x + cw / 2, y + ch - 14, f'{cid} ({name})', 20, '#5e4f42', outline=False)
    im = A.R(svg, cols * cw, rows * ch, A.WOB['char'])
    p = os.path.join(A.PREV, 'costume_zoom.png'); im.save(p); print(p)


# ------------------------------------------------------------------ 추천 신규 코스튬 시안 8종 (게임 반영 전 미리보기용)
def b_onsen(cid):
    sky, sky2, obi, obi2 = '#82c4d6', '#5ea6bc', '#f28b76', '#d66c58'
    # 따뜻한 온천 김 (밝은 UI · 밤하늘 전투 어디서나 은은하게 보이게)
    steam = (L('M4 88 Q-4 76 4 64 Q12 52 4 40', 3.4, '#bfe2ee')
             + L('M16 96 Q10 86 16 76 Q22 66 16 58', 2.8, '#bfe2ee')).replace('<path', '<path opacity=".72"')
    s = steam + feet('#a6784e', '#875e3a', '#f28b76') + bare(cid)
    # 하늘색 유카타 + 물결 무늬 + 산호빛 오비 띠
    robe_d = 'M24 94 Q70 82 122 88 C128 112 126 142 114 160 Q72 168 24 160 C14 142 16 114 24 94 Z'
    robe_in = (F('M0 70 L160 70 L160 180 L0 180 Z', G(sky, 'v', .86, .22))
               + L('M50 88 L92 128 M112 88 L82 122', 3.4, '#fbfaf5')
               + L('M30 144 Q42 138 54 144 Q66 150 78 144 Q90 138 102 144', 2.0, '#d8f2f8')
               + F('M0 124 L160 122 L160 138 L0 140 Z', G(obi, 'v', .86, .20))
               + L('M0 124 L160 122 M0 140 L160 138', 2.4)
               + L('M0 132 L160 130', 2.6, '#ffe5a8'))
    s += clip('on' + cid, robe_d, robe_in) + P(robe_d, 'none', SW)
    # 뒤쪽 오비 리본 매듭
    s += P('M20 124 Q4 114 4 132 Q4 146 20 138 Z', G(obi2), SW * .8)
    s += face_bare(cid, blush=True) + hat_shadow(cid, 'M46 24 Q82 16 118 24 L118 32 Q82 24 46 32 Z', .14)
    s += arm(sky, '#fbfaf5')
    # 뒤쪽 귀 + 접은 흰 온천 수건 + 머리 위 통통한 유자(귤)와 잎!
    s += P('M46 32 Q38 18 48 15 Q56 18 55 29 Z', G(A.CAPY_DARK, 'r'), SW)
    s += P('M50 22 Q50 10 62 10 L104 10 Q116 10 116 22 Q83 18 50 22 Z', G('#fbfaf5', 'v', .92, .08), SW * .9)
    s += L('M56 15 Q83 13 110 15', 1.8, '#dcd6ca')
    s += E(83, -2, 16, 13, G('#ffa62b', 'r', .84, .28), SW * .88)
    s += F('M74 -7 Q80 -12 87 -9 Q80 -5 74 -7 Z', '#ffffff', .45)
    s += C(84, -13.5, 2.8, '#4e8a38', 1.4)
    s += P('M86 -14 Q98 -24 106 -14 Q96 -6 86 -14 Z', G('#6ab84c'), SW * .72)
    return s


def b_duck(cid):
    yel, yel2, org, org2 = '#ffe05c', '#ebc02c', '#ff9436', '#de721a'
    # 뒤쪽 귀여운 오리 꽁지 깃털 + 주황 물갈퀴 발
    tail = P('M30 136 Q8 134 2 122 Q12 146 28 152 Z', G(yel, 'v', .86, .20), SW * .9)
    s = tail + feet(org, org2)
    # 정수리 삐죽 오리 솜털 2가닥
    s += P('M64 18 Q54 -2 68 -6 Q74 4 74 16 Z', G(yel), SW * .82) + P('M74 16 Q74 -6 88 -6 Q88 6 84 16 Z', G(yel), SW * .82)
    # 노란 오리 잠옷 + 크림 배
    s += onesie(cid, yel, '#fff7cc')
    # 후드 이마에 도톰하고 둥근 주황 오리 부리 + 까만 단추 눈
    s += P('M102 18 Q126 10 138 22 Q132 32 106 29 Z', G(org, 'v', .86, .26), SW * .85)
    s += F('M112 18 Q122 17 129 21 Q120 23 112 21 Z', '#ffffff', .42)
    s += C(74, 24, 3.8, '#1c110a', 0) + C(75.3, 22.7, 1.2, '#ffffff', 0)
    # 옆면 작은 오리 날개 + 볼터치 + 팔
    s += P('M66 106 Q42 112 38 130 Q58 132 74 116 Z', G(yel2, 'v', .88, .22), SW * .82)
    s += C(93, 52, 4.8, '#ff9898', 0).replace('<circle', '<circle opacity=".45"')
    s += arm(yel, '#fff4b8')
    return s


def b_frog(cid):
    grn, grn2, belly, pink = '#7ad474', '#5bb355', '#eef5c4', '#ff9eb5'
    s = feet(grn, grn2)
    # 후드 위 톡 튀어나온 큰 개구리 눈 2개 + 귀여운 분홍 연꽃잎 포인트
    s += C(64, 14, 15, G(grn, 'r', .86, .24), SW) + C(66, 12, 9.5, '#ffffff', 2.4) + C(68.5, 11.5, 5.2, '#1c110a', 0) + C(70, 9.5, 1.8, '#ffffff', 0)
    s += C(98, 12, 15, G(grn, 'r', .86, .24), SW) + C(101, 10, 9.5, '#ffffff', 2.4) + C(103.5, 9.5, 5.2, '#1c110a', 0) + C(105, 7.5, 1.8, '#ffffff', 0)
    # 연두 개구리 잠옷 + 깔끔한 크림 배 (꿀벌 · 오리처럼 깔끔한 실루엣!)
    s += onesie(cid, grn, belly)
    # 후드 볼점 + 왼쪽 귀 옆 작은 연꽃잎 장식 + 볼터치
    s += C(54, 42, 5.5, pink, 0).replace('<circle', '<circle opacity=".65"')
    s += P('M46 28 Q34 18 42 12 Q52 14 54 24 Z', G('#ff9eb5', 'r'), SW * .7) + P('M50 24 Q44 10 54 8 Q58 16 56 24 Z', G('#ffc2d1', 'r'), SW * .65)
    s += C(93, 52, 4.8, '#ff9898', 0).replace('<circle', '<circle opacity=".45"')
    s += arm(grn, belly)
    return s


def b_panda(cid):
    wht, blk, blk2, bam = '#fcfbf7', '#332e2c', '#221e1d', '#6eb854'
    # 둥근 까만 판다 꼬리 + 까만 발
    s = C(22, 142, 11, G(blk, 'r', .85, .18), SW * .85) + feet(blk, blk2)
    # 둥근 까만 판다 귀 2개 + 귀여운 대나무 잎
    s += C(54, 22, 12.5, G(blk, 'r', .84, .20), SW) + C(90, 12, 12.5, G(blk, 'r', .84, .20), SW)
    s += P('M60 12 Q66 -8 82 -10 Q78 8 60 12 Z', G(bam), SW * .75) + L('M62 10 Q72 0 80 -8', 1.8, '#468232')
    # 하얀 판다 잠옷 + 어깨 까만 띠 + 하체 까만 바지 느낌
    s += P(BODY_D, G(wht, 'v', .92, .08), SW)
    s += body_clip('pd' + cid,
                   F('M0 88 Q70 80 140 88 L140 116 Q70 108 0 118 Z', G(blk, 'v', .86, .16))
                   + F('M0 144 Q70 150 140 144 L140 176 L0 176 Z', G(blk, 'v', .86, .16))
                   + L('M0 88 Q70 80 140 88 M0 118 Q70 108 140 116', 2.4))
    s += A.capy_face_hole(SW).replace('gBodyClip', 'fh' + cid)
    s += P(BODY_D, 'none', SW)
    s += C(93, 52, 4.8, '#ff9898', 0).replace('<circle', '<circle opacity=".48"')
    s += arm(blk, '#524b48')
    return s


def b_strawberry(cid):
    red, red2, grn, grn2, crm = '#f26878', '#d64b5c', '#6ab850', '#4c8e36', '#fff8ee'
    s = feet(red2, '#b83c4b')
    # 잘 익은 딸기 잠옷 몸통 + 노란 씨앗 + 하얀 생크림 배
    s += P(BODY_D, G(red, 'v', .84, .24), SW)
    seeds = ''.join(E(x, y, 2.4, 3.8, '#ffe699', 0, rot)
                    for x, y, rot in ((44, 52, -15), (56, 76, 10), (38, 108, -10), (60, 118, 12),
                                      (44, 140, 8), (72, 146, -12), (98, 148, 14)))
    s += body_clip('sb' + cid, E(104, 132, 24, 28, '#ffd9df', 0) + seeds)
    s += A.capy_face_hole(SW).replace('gBodyClip', 'fh' + cid)
    s += P(BODY_D, 'none', SW)
    # 폭신한 생크림 웨이브 깃 (목둘레에 몽실하게!)
    s += P('M24 94 Q34 108 48 98 Q62 112 78 98 Q94 110 112 96 Q120 92 118 86 Q72 82 26 88 Q20 90 24 94 Z', G(crm, 'v', .92, .06), SW * .82)
    # 머리 위 풍성한 초록 딸기 꼭지(잎 4갈래) + 동그란 꼭지 줄기 (딸기 본연의 귀여운 실루엣!)
    s += L('M80 14 Q82 -6 94 -12', 6.4) + L('M80 14 Q82 -6 94 -12', 3.0, grn2)
    s += P('M40 28 Q52 10 72 16 Q78 2 92 14 Q112 8 122 24 Q104 22 94 32 Q82 20 68 32 Q56 22 40 28 Z', G(grn, 'v', .86, .22), SW * .85)
    s += C(93, 52, 4.8, '#ff9898', 0).replace('<circle', '<circle opacity=".48"')
    s += arm(red, crm)
    return s


def b_sleepy(cid):
    blu, blu2, crm, gold = '#8ac2f0', '#6aa4d8', '#fff8eb', '#ffe066'
    s = feet(crm, '#e6dcc8') + bare(cid)
    # 하늘색 스트라이프 파자마 + 단추 + 앞주머니
    pj_d = 'M24 94 Q70 82 122 88 C128 112 126 142 114 158 Q72 166 24 158 C14 142 16 114 24 94 Z'
    stripes = ''.join(F(f'M{x} 70 L{x + 7} 70 L{x + 7} 180 L{x} 180 Z', '#cfe8fc', .85) for x in range(18, 130, 18))
    pj_in = (F('M0 70 L160 70 L160 180 L0 180 Z', G(blu, 'v', .88, .20)) + stripes
             + L('M88 94 L86 160', 2.4, blu2) + C(82, 114, 3.0, crm, 1.8) + C(82, 132, 3.0, crm, 1.8)
             + rr(44, 122, 26, 22, 5, G(blu, 'v', .86, .20), 2.4) + star(57, 133, 5.2, gold, 1.4))
    s += clip('pj' + cid, pj_d, pj_in) + P(pj_d, 'none', SW)
    # 둥근 파자마 카라
    s += P('M24 94 Q70 80 122 88 Q116 102 96 102 L84 94 L64 104 Q32 104 24 94 Z', G(crm, 'v', .92, .08), SW * .85)
    s += face_bare(cid, blush=True) + hat_shadow(cid, 'M32 34 Q82 22 132 32 L132 42 Q82 32 32 42 Z')
    s += arm(blu, crm)
    # 뒤로 길게 늘어진 별밤 수면 모자 + 하얀 방울 + 이마에 얹은 수면 안대!
    cap_d = 'M36 32 Q42 -12 86 -14 Q122 -12 128 26 Q84 18 36 32 Z'
    tail_d = 'M48 -2 Q12 -10 -10 18 Q4 26 24 14 Q36 16 48 16 Z'
    s += P(tail_d, G(blu2, 'v', .86, .20), SW) + C(-12, 22, 10.5, G('#ffffff', 'r', .92, .08), SW * .85)
    s += P(cap_d, G(blu, 'v', .86, .24), SW)
    s += clip('sc' + cid, cap_d, star(66, 2, 5.5, gold, 1.4) + star(102, 4, 4.5, gold, 1.4) + C(84, -4, 2.2, '#fff5b8', 0))
    s += P(cap_d, 'none', SW)
    # 모자 테두리에 살짝 올린 수면 안대 (감은 눈 자수 u u)
    s += P('M58 22 Q86 12 118 20 Q122 30 112 32 Q86 24 62 32 Q54 30 58 22 Z', G('#6f69b8', 'v', .86, .20), SW * .8)
    s += L('M72 25 Q77 29 82 25 M94 24 Q99 28 104 24', 2.0, '#ffe999')
    return s


def b_angel(cid):
    wht, wht2, gold, sky = '#fffdf7', '#ebe6da', '#ffd84d', '#d8f0ff'
    # 복슬복슬 하얀 천사 날개 (등 위쪽에서 포근하게)
    wings = (P('M34 94 Q-16 62 -16 96 Q-14 110 2 110 Q-12 124 6 130 Q20 132 34 116 Z', G('#ffffff', 'v', .92, .08), SW * .85)
             + L('M24 102 Q4 96 -6 100 M24 112 Q8 114 2 120', 2.0, '#c8d8e8'))
    s = wings + feet('#f2d6a2', '#d9ba84') + bare(cid)
    # 순백 천사 로브 + 금빛 허리띠 + 하늘빛 아래단
    robe_d = 'M24 94 Q70 82 122 88 C128 112 126 142 114 162 Q72 170 24 162 C14 144 16 114 24 94 Z'
    robe_in = (F('M0 70 L160 70 L160 180 L0 180 Z', G(wht, 'v', .92, .08))
               + F('M14 148 Q72 156 124 146 L124 164 L14 164 Z', '#e4f3ff')
               + L('M14 148 Q72 156 124 146', 2.6, gold)
               + L('M48 112 Q44 136 46 156 M76 112 Q76 136 78 158', 2.0, wht2)
               + F('M0 116 Q72 122 140 114 L140 124 Q72 132 0 126 Z', gold)
               + L('M0 116 Q72 122 140 114 M0 126 Q72 132 140 124', 2.0, '#c49418'))
    s += clip('ag' + cid, robe_d, robe_in) + P(robe_d, 'none', SW)
    s += P('M24 92 Q70 80 122 88 Q116 100 72 98 Q32 102 24 92 Z', G(wht, 'v', .94, .06), SW * .82)
    s += star(76, 122, 6.0, '#fff2a6', 1.8)
    s += face_bare(cid, blush=True) + arm(wht, gold) + ears()
    # 머리 위 반짝이는 금빛 천사 링(후광)
    s += blur(E(82, -14, 28, 8.5, 'none', 7.5).replace(f'stroke="{A.INK}"', f'stroke="{gold}"'), 4, .85)
    s += E(82, -14, 28, 8.5, 'none', 7.2) + E(82, -14, 28, 8.5, 'none', 3.6).replace(f'stroke="{A.INK}"', f'stroke="{gold}"')
    s += L('M64 -18 Q82 -22 98 -18', 1.8, '#ffffff')
    return s


def b_king(cid):
    red, red2, nav, gold, fur = '#dc4448', '#b52e34', '#3f538c', '#ffd54f', '#fbf8f0'
    # 등 뒤 우아하게 드리운 붉은 왕실 망토
    cape = (P('M44 92 Q4 114 -18 158 Q6 172 36 164 L52 96 Z', G(red2, 'v', .82, .20), SW)
            + F('M-14 152 Q8 164 36 156 L36 164 Q6 172 -18 158 Z', fur))
    s = cape + feet('#5a3926', '#42291a', gold) + bare(cid)
    # 네이비 왕실 정복 + 금빛 벨트 + 붉은 망토 앞자락
    s += cloth(cid, F('M-20 80 L160 80 L160 200 L-20 200 Z', G(nav, 'v', .84, .22))
               + F('M10 86 L46 86 L38 170 L10 170 Z', G(red, 'v', .84, .20))
               + L('M46 86 L38 170', 2.6, gold)
               + F('M-20 130 L160 128 L160 138 L-20 140 Z', gold)
               + L('M-20 130 L160 128 M-20 140 L160 138', 2.2)
               + C(76, 114, 3.4, gold, 1.6) + C(76, 134, 4.2, '#e04842', 1.8))
    # 풍성한 흰 담비털(어민) 망토 칼라 + 까만 털 점
    fur_d = 'M20 92 Q72 78 124 88 Q126 102 108 106 Q72 98 34 108 Q18 104 20 92 Z'
    s += P(fur_d, G(fur, 'v', .92, .08), SW * .88)
    s += C(42, 98, 2.2, '#2b221e', 0) + C(64, 93, 2.2, '#2b221e', 0) + C(88, 94, 2.2, '#2b221e', 0) + C(108, 97, 2.2, '#2b221e', 0)
    s += face_bare(cid) + hat_shadow(cid, 'M46 22 Q82 14 118 22 L118 30 Q82 22 46 30 Z', .14)
    s += arm(red, gold)
    # 뒤쪽 귀 + 보석 박힌 반짝 금빛 왕관
    s += P('M46 32 Q38 18 48 15 Q56 18 55 29 Z', G(A.CAPY_DARK, 'r'), SW)
    crown_d = 'M52 22 L46 -10 L68 4 L83 -18 L98 4 L120 -10 L114 20 Q83 14 52 22 Z'
    s += P(crown_d, G(gold, 'v', .84, .28), SW * .92)
    s += C(46, -11, 4.2, gold, 2.2) + C(83, -19, 4.8, gold, 2.2) + C(120, -11, 4.2, gold, 2.2)
    s += C(83, 6, 4.2, '#e84848', 1.8) + C(64, 10, 3.0, '#58b4f0', 1.6) + C(102, 9, 3.0, '#58b4f0', 1.6)
    s += F('M54 14 Q83 8 112 13 L113 18 Q83 13 53 19 Z', red)
    return s


BONUS_COSTUMES = [
    ('onsen', '온천 유자', PAW),
    ('duck', '아기 오리 잠옷', '#ffe05c'),
    ('frog', '개구리 잠옷', '#7ad474'),
    ('panda', '판다 잠옷', '#332e2c'),
    ('strawberry', '딸기 생크림', '#f26878'),
    ('sleepy', '별밤 파자마', PAW),
    ('angel', '아기 천사', PAW),
    ('king', '카피 대왕', PAW),
]
BFN = {c[0]: globals()['b_' + c[0]] for c in BONUS_COSTUMES}
BHANDC = {c[0]: c[2] for c in BONUS_COSTUMES}

# 11-21: 시안 코스튬 8종 · 탈것 6종을 게임 목록에 합침 (export · fix10_tables.py 가 COSTUMES · MOUNTS 를 읽음 → 표 줄 · 그림이 같이 생김)
COSTUMES += [c for c in BONUS_COSTUMES if c[0] not in CFN]
CFN.update(BFN); HANDC.update(BHANDC)
MOUNTS += [(m[0], m[1], m[2]) for m in BONUS_MOUNTS if m[0] not in MFN]
MFN.update(BMFN)


def bonus_sheet():
    """추천 신규 코스튬 8종 크게 보기 (총 없음 _float + 총 장착 나란히)"""
    cw, ch, cols = 420, 280, 4
    rows = (len(BONUS_COSTUMES) + cols - 1) // cols
    svg = f'<rect width="{cols * cw}" height="{rows * ch}" fill="#f7f1e3"/>' + A.font_css()
    for i, (cid, name, hc) in enumerate(BONUS_COSTUMES):
        x, y = (i % cols) * cw, (i // cols) * ch
        body = f'<g transform="translate({A.G_OX},0)">{BFN[cid](cid)}</g>'.replace(A.INK, A.CAPY_INK)
        hx, hy = A.HAND
        h_svg = (C(hx, hy, 10, G(hc, 'r'), SW) + L(f'M{hx - 4} {hy - 6} Q{hx + 2} {hy - 10} {hx + 7} {hy - 5}', 2.4)).replace(A.INK, A.CAPY_INK)
        g_svg = A.guardian_gun()
        if hc != A.SUIT_GR: g_svg = g_svg.replace(G(A.SUIT_GR, 'r'), G(hc, 'r'))
        fl = body + h_svg
        fu = body + f'<g transform="translate({hx},{hy}) rotate(-50)">{g_svg}</g>'
        svg += f'<rect x="{x + 6}" y="{y + 6}" width="{cw - 12}" height="{ch - 12}" rx="16" fill="#ffffff" opacity=".75"/>'
        svg += f'<g transform="translate({x + 28},{y + 78}) scale(.95)">{fl}</g>'
        svg += f'<g transform="translate({x + 216},{y + 78}) scale(.95)">{fu}</g>'
        svg += A.T(x + cw / 2, y + ch - 14, f'{cid} ({name})', 20, '#5e4f42', outline=False)
    im = A.R(svg, cols * cw, rows * ch, A.WOB['char'])
    p = os.path.join(A.PREV, 'bonus_costumes.png'); im.save(p); print(p)


def any_costume(cid, gun=True):
    """COSTUMES 또는 BONUS_COSTUMES 중 어떤 코스튬이든 그려줌"""
    if cid in CFN: return costume_full(cid) if gun else costume_float(cid)
    hc = BHANDC[cid]
    body = f'<g transform="translate({A.G_OX},0)">{BFN[cid](cid)}</g>'.replace(A.INK, A.CAPY_INK)
    hx, hy = A.HAND
    if not gun:
        h_svg = (C(hx, hy, 10, G(hc, 'r'), SW) + L(f'M{hx - 4} {hy - 6} Q{hx + 2} {hy - 10} {hx + 7} {hy - 5}', 2.4)).replace(A.INK, A.CAPY_INK)
        return body + h_svg
    g_svg = A.guardian_gun()
    if hc != A.SUIT_GR: g_svg = g_svg.replace(G(A.SUIT_GR, 'r'), G(hc, 'r'))
    return body + f'<g transform="translate({hx},{hy}) rotate(-50)">{g_svg}</g>'


def mount_sheet():
    """기존 탈것 10종 크게 보기 (어울리는 코스튬 탑승 · 총 없음 vs 총 장착)"""
    pairs = [
        ('surf', '은빛 서프보드', 'basic'), ('carpet', '하늘 양탄자', 'wizard'),
        ('plane', '빨간 비행기', 'knight'), ('ladybug', '무당벌레', 'gnome'),
        ('star', '별똥별', 'hero'), ('seahorse', '무지개 해마', 'shark'),
        ('unicorn', '숲속 유니콘', 'wizard'), ('hoverbike', '호버바이크', 'ninja'),
        ('trex', '아기 티라노', 'dino'), ('broom', '마법 빗자루', 'bee'),
    ]
    cw, ch, cols = 520, 300, 2
    rows = (len(pairs) + cols - 1) // cols
    svg = f'<rect width="{cols * cw}" height="{rows * ch}" fill="#f7f1e3"/>' + A.font_css()
    for i, (mid, name, cid) in enumerate(pairs):
        x, y = (i % cols) * cw, (i // cols) * ch
        back, front = MFN[mid]()
        svg += f'<rect x="{x + 6}" y="{y + 6}" width="{cw - 12}" height="{ch - 12}" rx="16" fill="#ffffff" opacity=".75"/>'
        fl = (back + f'<g transform="translate({-A.G_CX},{-A.G_FOOT})">{any_costume(cid, False)}</g>' + front).replace(A.INK, A.CAPY_INK)
        fu = (back + f'<g transform="translate({-A.G_CX},{-A.G_FOOT})">{any_costume(cid, True)}</g>' + front).replace(A.INK, A.CAPY_INK)
        svg += f'<g transform="translate({x + 135},{y + 178}) scale(.66)">{fl}</g>'
        svg += f'<g transform="translate({x + 385},{y + 178}) scale(.66)">{fu}</g>'
        svg += A.T(x + cw / 2, y + ch - 14, f'{mid} ({name})', 20, '#5e4f42', outline=False)
    im = A.R(svg, cols * cw, rows * ch, A.WOB['char'])
    p = os.path.join(A.PREV, 'mount_zoom.png'); im.save(p); print(p)


def bonus_mount_sheet():
    """추천 신규 탈것 6종 크게 보기 (어울리는 코스튬 탑승 · 총 없음 vs 총 장착)"""
    cw, ch, cols = 520, 300, 2
    rows = (len(BONUS_MOUNTS) + cols - 1) // cols
    svg = f'<rect width="{cols * cw}" height="{rows * ch}" fill="#f7f1e3"/>' + A.font_css()
    for i, (mid, name, _, cid) in enumerate(BONUS_MOUNTS):
        x, y = (i % cols) * cw, (i // cols) * ch
        back, front = BMFN[mid]()
        svg += f'<rect x="{x + 6}" y="{y + 6}" width="{cw - 12}" height="{ch - 12}" rx="16" fill="#ffffff" opacity=".75"/>'
        fl = (back + f'<g transform="translate({-A.G_CX},{-A.G_FOOT})">{any_costume(cid, False)}</g>' + front).replace(A.INK, A.CAPY_INK)
        fu = (back + f'<g transform="translate({-A.G_CX},{-A.G_FOOT})">{any_costume(cid, True)}</g>' + front).replace(A.INK, A.CAPY_INK)
        svg += f'<g transform="translate({x + 135},{y + 178}) scale(.66)">{fl}</g>'
        svg += f'<g transform="translate({x + 385},{y + 178}) scale(.66)">{fu}</g>'
        svg += A.T(x + cw / 2, y + ch - 14, f'{mid} ({name})', 20, '#5e4f42', outline=False)
    im = A.R(svg, cols * cw, rows * ch, A.WOB['char'])
    p = os.path.join(A.PREV, 'bonus_mounts.png'); im.save(p); print(p)


if __name__ == '__main__':
    cmd = sys.argv[1] if len(sys.argv) > 1 else 'sheet'
    if cmd == 'sheet': sheet()
    elif cmd == 'zoom': costume_sheet()
    elif cmd == 'bonus': bonus_sheet()
    elif cmd == 'mzoom': mount_sheet()
    elif cmd == 'mbonus': bonus_mount_sheet()
    elif cmd == 'mpreview': mount_sheet(); bonus_mount_sheet(); sheet()
    elif cmd == 'preview': sheet(); costume_sheet()
    elif cmd == 'export': export(set(sys.argv[2:]) or None); bounds()
    elif cmd == 'bounds': bounds()
