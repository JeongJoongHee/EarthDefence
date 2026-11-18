# -*- coding: utf-8 -*-
"""11-18 전투 배치 2판: 우주 사령탑 (전투배치_시안.html — 사용자 "③ 우주 사령탑 · 0.75배 · 왼쪽 벽에 붙여").

그림 캔버스 = 화면 픽셀 그대로 (1 px = 월드 0.01칸, PPU 100)
  · 사령탑: 420 x 1440 · 캔버스 x 0 = 화면 왼쪽 끝(월드 -5.4) · 캔버스 y 1400 = 땅(월드 Ground_Y)
    → Unity 에서 피벗 (0, 40/1440) 으로 (-5.4, Ground_Y) 에 놓음
  · 달 바닥: 1080 x 460 · 월드 y -9.6 ~ -5.0 · 캔버스 y 252 = 땅(월드 -7.52)
게임 코드 World.cs 의 칸 좌표(SlotPos · GuardianPos · Shield_X)는 아래 상수에서 나옴 — 바꾸면 같이 바꿀 것.
시안 도구 tools/layout_mock.py command(K=.75) 와 같은 모양.
"""
import math, random
import earth_art as A
from earth_art import P, L, C, E, F, rr, G, blur, shade, mix, INK, CREAM, CREAM2, ORANGE, ORANGE2, GOLD, SKY, RED, GREEN

TW, TH = 420, 1440
GY = 1400                      # 땅
K = .75                        # 시안 0.75배
BX = 14 + 88 * K               # 몸통 가운데 x (= 80, 왼쪽 벽에 바짝)
TOP = GY - 1580 * K            # 몸통 꼭대기 (= 215)
HB, HT = 88 * K, 62 * K        # 아래 · 위 반폭
DY = TOP + (GY - TOP) * .5 + 20  # 사령실 윗면 (대장 카피 발)
DL, DR, DH = BX - 72, BX + 150, 64
SX = BX + 150                  # 터렛 칸 x (= 230)
SLOTS = (TOP + 95, DY - 200, DY + DH + 180, GY)   # 터렛 칸 1~4 의 발판 윗면 (위 2 · 아래 2)
STEEL, STEEL2 = '#dfe4ee', '#b4bdcf'


def world(cx, cy, ground=-7.52):
    """캔버스 좌표 → 월드 (게임 코드에 넣을 값)"""
    return round(cx / 100 - 5.4, 3), round(ground + (GY - cy) / 100, 3)


# 외형 7종 (기지 외형 = 6차 수정안 돔 7종을 사령탑으로): 몸통 · 띠(사령실) · 유리 색 + 장식
LOOKS = {
    'basic':   dict(body=STEEL, body2=STEEL2, band=ORANGE, band2=ORANGE2, glass='#9fd8f5'),
    'prism':   dict(body='#f1e8fb', body2='#cdbbe6', band='#c9a8f0', band2='#a585d6', glass='#f3dcff'),
    'starsea': dict(body='#bfc8ec', body2='#8e9bd0', band='#5868b8', band2='#44529a', glass='#9fb2ff'),
    'frost':   dict(body='#eafaff', body2='#b9e2f0', band='#8fd6ff', band2='#66b6e6', glass='#dff6ff'),
    'thorn':   dict(body='#e4efd6', body2='#b7cf9c', band='#8ccb6e', band2='#6fae55', glass='#d4f0c0'),
    'bloom':   dict(body='#fff0f4', body2='#efc6d2', band='#ffb3c8', band2='#e891aa', glass='#ffe4ec'),
    'gold':    dict(body='#fff4d6', body2='#e9cf8a', band='#f7c948', band2='#d9a72a', glass='#fff2c8'),
}


def _star(x, y, r, fill=GOLD, sw=3.2, rot=0):
    pts = []
    for i in range(10):
        a = math.radians(rot - 90 + i * 36); q = r if i % 2 == 0 else r * .45
        pts.append(f'{x + math.cos(a) * q:.1f} {y + math.sin(a) * q:.1f}')
    return P('M' + ' L'.join(pts) + ' Z', fill, sw)


def _flower(x, y, r, petal, core=GOLD):
    s = ''
    for i in range(5):
        a = math.radians(i * 72 - 90)
        s += C(x + math.cos(a) * r * .62, y + math.sin(a) * r * .62, r * .5, petal, 3)
    return s + C(x, y, r * .36, core, 3)


def tower(fid='basic', slots=True):
    """사령탑 한 장 (캔버스 420 x 1440). slots = 터렛 발판까지 (외형 아이콘에서도 씀)"""
    lk = LOOKS.get(fid, LOOKS['basic'])
    body, body2, band, band2, glass = lk['body'], lk['body2'], lk['band'], lk['band2'], lk['glass']
    xl0, xr0, xl1, xr1 = BX - HB, BX + HB, BX - HT, BX + HT

    def xl(y): return xl0 + (xl1 - xl0) * (GY - y) / (GY - TOP)
    def xr(y): return xr0 + (xr1 - xr0) * (GY - y) / (GY - TOP)

    v = ''
    # 안테나 · 레이더 (적이 오는 오른쪽 위를 봄) · 깜빡이 등
    ah = 230 * K
    v += L(f'M{BX} {TOP - 40 * K:.1f} L{BX} {TOP - ah:.1f}', 7) + L(f'M{BX} {TOP - ah * .65:.1f} L{BX + 30 * K + 10:.1f} {TOP - ah:.1f}', 4)
    dx, dy, dr = BX + 50 * K + 6, TOP - 115 * K, 60 * K + 8
    v += L(f'M{BX + 12} {TOP - 40 * K:.1f} L{dx - 4:.1f} {dy + 12:.1f}', 6)
    v += (f'<g transform="rotate(45 {dx:.1f} {dy:.1f})">' + P(f'M{dx - dr:.1f} {dy:.1f} Q{dx:.1f} {dy + dr * .8:.1f} {dx + dr:.1f} {dy:.1f} Z', G(body), 5)
          + L(f'M{dx:.1f} {dy + 12:.1f} L{dx:.1f} {dy - dr * .7:.1f}', 5) + C(dx, dy - dr * .7 - 6, 7, band, 3) + '</g>')
    v += blur(C(BX, TOP - ah, 18, '#ff9a8a', 0), 8, .9) + C(BX, TOP - ah, 8, RED, 3.4)
    # 꼭대기 유리 관제실
    v += P(f'M{xl1 - 10:.1f} {TOP - 8:.1f} Q{BX} {TOP - 70 * K - 30:.1f} {xr1 + 10:.1f} {TOP - 8:.1f} Z', G(glass, 'v', .9, .5), 5)
    v += F(f'M{xl1 + 2:.1f} {TOP - 16:.1f} Q{BX - 14} {TOP - 66:.1f} {BX + 4} {TOP - 70:.1f} Q{BX - 10} {TOP - 52:.1f} {xl1 + 14:.1f} {TOP - 16:.1f} Z', '#ffffff', .6)
    # 몸통 (위로 가늘어짐) · 그늘 · 빛 · 이음줄
    v += P(f'M{xl0:.1f} {GY} L{xl1:.1f} {TOP:.1f} L{xr1:.1f} {TOP:.1f} L{xr0:.1f} {GY} Z', G(body, 'v', .92, .2), 6)
    v += F(f'M{xr0 - 24:.1f} {GY - 4} L{xr1 - 17:.1f} {TOP + 6:.1f} L{xr1 - 3:.1f} {TOP + 6:.1f} L{xr0 - 4:.1f} {GY - 4} Z', body2, .7)
    v += F(f'M{xl0 + 7:.1f} {GY - 4} L{xl1 + 6:.1f} {TOP + 6:.1f} L{xl1 + 19:.1f} {TOP + 6:.1f} L{xl0 + 22:.1f} {GY - 4} Z', '#ffffff', .5)
    for y in (TOP + 210, DY - 70, DY + 190, GY - 230):
        v += L(f'M{xl(y) + 4:.1f} {y:.1f} L{xr(y) - 4:.1f} {y:.1f}', 3.4, shade(body2, .8))
    v += rr(xl1 - 8, TOP - 12, xr1 - xl1 + 16, 24, 10, G(band), 5)
    # 동그란 창 (칸 사이)
    for y in ((SLOTS[0] + SLOTS[1]) / 2 + 40, (SLOTS[2] + GY) / 2 - 20):
        v += blur(C(BX, y, 22, '#bfe9ff', 0), 6, .6) + C(BX, y, 17.5, G(SKY, 'r'), 5) + F(f'M{BX - 9} {y - 7} A11 11 0 0 1 {BX + 6} {y - 10} L{BX} {y} Z', '#ffffff', .75)
    # 가운데 사령실 (대장 카피 자리)
    v += P(f'M{DL} {DY} L{DR} {DY} Q{DR + 22} {DY + DH * .45:.1f} {DR - 10} {DY + DH} L{DL + 10} {DY + DH} Q{DL - 22} {DY + DH * .45:.1f} {DL} {DY} Z', G(band, 'v', .86, .2), 6)
    v += rr(DL + 18, DY + 14, DR - DL - 36, 28, 13, G(glass, 'v', .9, .45), 4)
    v += blur(rr(DL + 24, DY + 18, DR - DL - 48, 20, 10, '#e9f9ff', 0), 4, .5)
    for x in (DL + 80, DL + 150):
        v += L(f'M{x} {DY + 15} L{x} {DY + 41}', 3)
    v += P(f'M{BX - 30} {DY + DH} L{BX + 30} {DY + DH} L{BX + 18} {DY + DH + 24} L{BX - 18} {DY + DH + 24} Z', G(band2), 5)
    v += rr(DL - 8, DY - 12, DR - DL + 16, 20, 9, G(CREAM), 5)
    v += L(f'M{DL} {DY - 48} L{DR} {DY - 48}', 4) + ''.join(L(f'M{x} {DY - 48} L{x} {DY - 10}', 4) for x in (DL, DL + 60, DR))
    for x in (DL + 6, DR - 6):
        v += blur(C(x, DY + DH - 12, 10, '#fff2a8', 0), 4, .8) + C(x, DY + DH - 12, 6, GOLD, 3)
    # 아래 받침 (발 날개 둘)
    v += P(f'M{xl0 + 3:.1f} {GY - 80} L{xl0 - 46:.1f} {GY - 6} L{xl0 + 6:.1f} {GY - 6} Z', G(band), 5) + P(f'M{xr0 - 3:.1f} {GY - 80} L{xr0 + 46:.1f} {GY - 6} L{xr0 - 6:.1f} {GY - 6} Z', G(band), 5)
    v += rr(xl0 - 16, GY - 20, xr0 - xl0 + 32, 26, 10, G(body2), 5)
    # 터렛 발판 (몸통에서 나온 판 · 위 칸은 위에서 당기는 버팀대 · 아래 칸은 밑에서 받침 · 맨 아래 칸은 달 바닥)
    if slots:
        for y in SLOTS[:3]:
            by = y - 80 if y < DY else y + 70
            v += rr(xl(y) - 7, y - 26, xr(y) - xl(y) + 14, 18, 8, G(band), 5)
            v += L(f'M{xr(by) - 2:.1f} {by:.1f} L{SX + 40} {y + 4:.1f}', 20) + L(f'M{xr(by) - 2:.1f} {by:.1f} L{SX + 40} {y + 4:.1f}', 10, body2)
            v += rr(xr(y) - 10, y - 6, SX + 100 - xr(y), 28, 12, G(body), 5) + F(f'M{xr(y):.1f} {y + 1:.1f} L{SX + 82} {y + 1:.1f} L{SX + 82} {y + 6:.1f} L{xr(y):.1f} {y + 6:.1f} Z', '#ffffff', .6)
            for lx in (SX - 66, SX + 66):
                v += C(lx, y + 8, 5, GOLD, 3)
    v += _deco(fid, xl, xr, lk)
    return v


def _deco(fid, xl, xr, lk):
    """외형 장식 (효과와 같은 느낌): 무지개 띠 · 별 · 고드름 · 덩굴 · 꽃 · 금 리벳"""
    s = ''
    rnd = random.Random(7)
    if fid == 'prism':
        for i, c in enumerate(('#ffb3c8', '#ffe08a', '#9fe0ff')):
            y = TOP + 40 + i * 12
            s += L(f'M{xl(y) + 6:.1f} {y:.1f} L{xr(y) - 6:.1f} {y:.1f}', 7, c)
        for y in (DY + DH + 60, GY - 120):
            for i, c in enumerate(('#ffb3c8', '#ffe08a', '#9fe0ff')):
                yy = y + i * 10
                s += L(f'M{xl(yy) + 6:.1f} {yy:.1f} L{xr(yy) - 6:.1f} {yy:.1f}', 6, c)
    elif fid == 'starsea':
        for _ in range(14):
            y = rnd.uniform(TOP + 30, GY - 60)
            x = rnd.uniform(xl(y) + 14, xr(y) - 14)
            s += f'<circle cx="{x:.1f}" cy="{y:.1f}" r="{rnd.uniform(2.2, 4.2):.1f}" fill="#fff4dc" opacity="{rnd.uniform(.5, .95):.2f}"/>'
        s += blur(C(BX, TOP - 26, 14, '#cfd8ff', 0), 5, .8) + _star(BX, TOP - 26, 15, '#fff2a8')
        s += _star(DR - 26, DY - 66, 12, '#fff2a8', 3, 20)
    elif fid == 'frost':
        for x in range(int(DL) + 14, int(DR) - 6, 22):
            ln = 18 + (x * 7) % 16
            s += P(f'M{x - 7} {DY + DH - 2} L{x + 7} {DY + DH - 2} L{x} {DY + DH + ln} Z', G('#e6fbff', 'v', .9, .4), 3)
        y = TOP - 8
        for x in (BX - 30, BX - 10, BX + 12, BX + 32):
            s += P(f'M{x - 6} {y} L{x + 6} {y} L{x} {y + 18 + (x % 3) * 5} Z', G('#e6fbff', 'v', .9, .4), 3)
        for (x, y) in ((BX, DY - 120), (BX, GY - 330)):
            for a in (0, 60, 120):
                s += f'<g transform="rotate({a} {x} {y})">' + L(f'M{x - 15} {y} L{x + 15} {y}', 3.4, '#ffffff') + '</g>'
    elif fid == 'thorn':
        y0, y1 = TOP + 20, GY - 30
        pts = []
        for i in range(26):
            y = y0 + (y1 - y0) * i / 25
            x = BX + math.sin(i * .9) * (xr(y) - BX - 10)
            pts.append((x, y))
        d = 'M' + ' L'.join(f'{x:.1f} {y:.1f}' for x, y in pts)
        s += L(d, 9) + L(d, 5, '#8ccb6e')
        for i, (x, y) in enumerate(pts[1::2]):
            sg = 1 if i % 2 else -1
            s += E(x + sg * 12, y - 4, 11, 6, '#9dcf7a', 3, rot=sg * 30)
        for y in (TOP + 120, DY - 140, DY + 220, GY - 200):
            s += P(f'M{xr(y) - 2:.1f} {y - 9:.1f} L{xr(y) + 16:.1f} {y:.1f} L{xr(y) - 2:.1f} {y + 9:.1f} Z', G('#c9e6a6'), 3)
    elif fid == 'bloom':
        for i, y in enumerate((TOP + 60, TOP + 300, DY - 90, DY + 200, GY - 300, GY - 120)):
            x = BX + (16 if i % 2 else -16)
            s += E(x + 12, y + 10, 11, 6, '#a9d68c', 3, rot=30) + _flower(x, y, 16 if i % 2 else 13, '#ffc2d4' if i % 3 else '#fff4dc', GOLD if i % 3 else '#ffb3c8')
        s += _flower(DR - 30, DY - 64, 13, '#ffc2d4')
    elif fid == 'gold':
        for y in (TOP + 40, DY - 30, DY + DH + 40, GY - 40):
            for x in (xl(y) + 12, BX, xr(y) - 12):
                s += C(x, y, 5.5, G('#fff4dc', 'r'), 2.6)
        s += P(f'M{BX - 30} {TOP - 34} L{BX - 34} {TOP - 70} L{BX - 14} {TOP - 50} L{BX} {TOP - 80} L{BX + 14} {TOP - 50} L{BX + 34} {TOP - 70} L{BX + 30} {TOP - 34} Z', G('#ffd34d', 'v', .86, .3), 4)
        s += C(BX, TOP - 50, 6, '#ff9a8a', 3)
    return s


def moon_ground():
    """달 바닥 + 지평선 위로 떠오르는 지구 (캔버스 1080 x 460, 땅 = y 252)"""
    g = 252
    ex, ey, er = 860, g + 65, 160
    s = blur(C(ex, ey - 20, 230, '#9fd8ff', 0), 30, .35)
    land = (f'M{ex - 130} {ey - 60} Q{ex - 70} {ey - 140} {ex + 10} {ey - 110} Q{ex + 40} {ey - 60} {ex - 20} {ey - 30} Q{ex - 90} {ey} {ex - 120} {ey + 20} Q{ex - 160} {ey - 10} {ex - 130} {ey - 60} Z',
            f'M{ex + 50} {ey - 120} Q{ex + 120} {ey - 120} {ex + 140} {ey - 60} Q{ex + 90} {ey - 40} {ex + 50} {ey - 80} Z')
    s += C(ex, ey, er, G('#8cc6e6', 'r', .8, .25), 7)
    for d in land:
        s += F(d, '#a9cf86') + L(d, 5)
    s += f'<ellipse cx="{ex - 40}" cy="{ey - 70}" rx="40" ry="12" fill="#fff" opacity=".7"/>'
    s += P(f'M-20 {g} Q200 {g - 30} 420 {g - 4} T860 {g + 6} T1100 {g - 10} L1100 480 L-20 480 Z', G('#cfc8dc', 'v', .86, .15), 7)
    s += F(f'M-20 {g + 8} Q200 {g - 22} 420 {g + 4} T860 {g + 14} T1100 {g - 2} L1100 {g + 16} T860 {g + 26} T420 {g + 16} Q200 {g - 8} -20 {g + 22} Z', '#ffffff', .25)
    for cx_, cy_, rx, ry in ((560, g + 90, 74, 20), (780, g + 165, 50, 14), (990, g + 110, 62, 18), (420, g + 180, 40, 12), (120, g + 140, 46, 13)):
        s += E(cx_, cy_, rx, ry, '#b3abc6', 4) + f'<ellipse cx="{cx_}" cy="{cy_ - ry * .35:.0f}" rx="{rx * .8:.0f}" ry="{ry * .45:.0f}" fill="#a39bb8"/>'
    rnd = random.Random(9)
    for _ in range(18):
        x, y = rnd.randint(20, 1060), rnd.randint(g + 30, 450)
        s += f'<circle cx="{x}" cy="{y}" r="{rnd.uniform(3, 7):.1f}" fill="#bdb5cf"/>'
    return s


def frame_icon(fid):
    """외형 목록 아이콘 (380 x 210): 사령탑 윗부분(안테나 ~ 사령실, 0.23배) + 뒤에 지구 · 별"""
    k = .23
    s = '<rect width="380" height="210" rx="24" fill="#2e3866"/>'
    rnd = random.Random(3)
    s += ''.join(f'<circle cx="{rnd.randint(10, 370)}" cy="{rnd.randint(8, 200)}" r="{rnd.uniform(1.4, 3):.1f}" fill="#fff4dc" opacity=".8"/>' for _ in range(18))
    s += blur(C(290, 150, 90, '#9fd8ff', 0), 14, .35) + C(290, 150, 64, G('#8cc6e6', 'r', .8, .25), 5)
    s += F('M240 130 Q262 100 292 112 Q300 132 278 140 Q254 150 246 162 Q230 150 240 130 Z', '#a9cf86') + L('M240 130 Q262 100 292 112 Q300 132 278 140 Q254 150 246 162 Q230 150 240 130 Z', 3.6)
    s += f'<g transform="translate({120 - BX * k},{8 - 42 * k}) scale({k})">{tower(fid, slots=False)}</g>'
    return s


# ------------------------------------------------------------------ 관통포: 받침 + 포신(머리, 45° 로 돎) 나눔 (예전 rail_base = 받침 + 포신 한 장)
def t_rail_stand():
    return A.pedestal(A.ELEM['phys'])


def t_rail_barrel():
    return (rr(86, 34, 28, 108, 9, G(A.METAL), 5.5) + rr(80, 26, 40, 20, 7, G(A.METAL2), 5.5)
            + F('M92 50 L97 50 L97 128 L92 128 Z', '#ffffff', .5) + L('M86 70 L114 70 M86 96 L114 96', 3.2))
