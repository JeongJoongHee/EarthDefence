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
# 11-18 7차 수정안 "타워 지지대는 최대한 컴팩트하게": 몸통을 가늘게 · 벽에 더 붙임 · 터렛 칸을 몸통 바로 옆 짧은 선반으로 · 긴 버팀대 없앰
#   · 대장 카피 사령실은 터렛 줄보다 오른쪽으로 내밀어 쏘는 탄이 위 터렛을 지나지 않게 ("중간 카피바라 탄도가 위쪽 터렛과 겹치지 않게")
BX = 50                        # 몸통 가운데 x (예전 80)
TOP = GY - 1580 * K            # 몸통 꼭대기 (= 215, 칸 높이는 예전 그대로 = 밸런스 그대로)
HB, HT = 34, 26                # 아래 · 위 반폭 (예전 66 · 46.5)
DY = TOP + (GY - TOP) * .5 + 20  # 사령실 윗면 (대장 카피 발)
GX = 250                       # 대장 카피 x (월드 -2.9) — 터렛 오른쪽 끝(SX+56)보다 오른쪽
DL, DR, DH = BX + 20, GX + 78, 56
SX = 150                       # 터렛 칸 x (= 월드 -3.9, 예전 230)
SLOTS = (TOP + 95, DY - 200, DY + 64 + 180, GY)   # 터렛 칸 1~4 의 발판 윗면 (위 2 · 아래 2)
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
    ah = 120                       # 7차: 짧게 (예전 172) — 왼쪽 위 달토끼 자리
    v += L(f'M{BX} {TOP - 40 * K:.1f} L{BX} {TOP - ah:.1f}', 7) + L(f'M{BX} {TOP - ah * .65:.1f} L{BX + 30 * K + 10:.1f} {TOP - ah:.1f}', 4)
    dx, dy, dr = BX + 34, TOP - 62, 30
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
        v += blur(C(BX, y, 17, '#bfe9ff', 0), 5, .6) + C(BX, y, 13, G(SKY, 'r'), 4.4) + F(f'M{BX - 7} {y - 5} A8 8 0 0 1 {BX + 4} {y - 8} L{BX} {y} Z', '#ffffff', .75)
    # 가운데 사령실 (대장 카피 자리) — 7차: 몸통에서 오른쪽으로 내민 짧은 발코니 (대장 카피가 터렛 줄보다 오른쪽에 서게)
    xd = xr(DY + DH)
    v += P(f'M{xd - 2:.1f} {DY + DH} L{xd + 70:.1f} {DY + DH} L{xd - 2:.1f} {DY + DH + 64} Z', G(band2), 5)          # 아래 받침 (작은 삼각)
    v += P(f'M{DL} {DY} L{DR} {DY} Q{DR + 18} {DY + DH * .45:.1f} {DR - 10} {DY + DH} L{DL + 6} {DY + DH} L{DL} {DY} Z', G(band, 'v', .86, .2), 6)
    v += rr(DL + 40, DY + 13, DR - DL - 62, 24, 11, G(glass, 'v', .9, .45), 4)
    v += blur(rr(DL + 46, DY + 17, DR - DL - 74, 16, 8, '#e9f9ff', 0), 4, .5)
    for x in (DL + 110, DL + 180):
        v += L(f'M{x} {DY + 14} L{x} {DY + 36}', 3)
    v += rr(DL - 6, DY - 12, DR - DL + 14, 20, 9, G(CREAM), 5)
    v += L(f'M{GX - 70} {DY - 44} L{DR} {DY - 44}', 4) + ''.join(L(f'M{x} {DY - 44} L{x} {DY - 10}', 4) for x in (GX - 70, DR))
    v += blur(C(DR - 6, DY + DH - 12, 10, '#fff2a8', 0), 4, .8) + C(DR - 6, DY + DH - 12, 6, GOLD, 3)
    # 아래 받침 (발 날개 둘, 작게)
    v += P(f'M{xl0 + 3:.1f} {GY - 56} L{xl0 - 24:.1f} {GY - 6} L{xl0 + 6:.1f} {GY - 6} Z', G(band), 4.4) + P(f'M{xr0 - 3:.1f} {GY - 56} L{xr0 + 26:.1f} {GY - 6} L{xr0 - 6:.1f} {GY - 6} Z', G(band), 4.4)
    v += rr(xl0 - 10, GY - 18, xr0 - xl0 + 20, 22, 9, G(body2), 4.6)
    # 터렛 발판 — 7차: 몸통 바로 옆 짧은 선반 + 밑에 작은 삼각 받침 (긴 버팀대 없앰) · 맨 아래 칸은 달 바닥
    if slots:
        for y in SLOTS[:3]:
            v += rr(xl(y) - 5, y - 22, xr(y) - xl(y) + 10, 16, 7, G(band), 4.6)
            v += P(f'M{xr(y) - 2:.1f} {y + 14} L{xr(y) + 42:.1f} {y + 14} L{xr(y) - 2:.1f} {y + 52} Z', G(body2), 4.4)
            v += rr(xr(y) - 8, y - 4, SX + 62 - xr(y), 24, 10, G(body), 5) + F(f'M{xr(y):.1f} {y + 2:.1f} L{SX + 50} {y + 2:.1f} L{SX + 50} {y + 6:.1f} L{xr(y):.1f} {y + 6:.1f} Z', '#ffffff', .6)
            v += C(SX + 44, y + 8, 4.5, GOLD, 2.8)
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


GM_H, GM_G = 700, 492            # 7차: 달 바닥 캔버스 1080 x 700 (월드 -9.6 ~ -2.6) · 땅 = y 492 (월드 -7.52)


def earth(ex, ey, er, sw=7, seed=4, clouds=True):
    """지구 한 덩이 (바다 · 대륙 · 구름 · 밤 그늘 · 대기 빛) — 원 밖은 안 그림 (clip)"""
    rnd = random.Random(seed)
    cid = f'eclip{seed}_{int(er)}'
    s = blur(C(ex, ey, er + 34, '#9fd8ff', 0), 22, .5)                 # 대기 빛
    s += C(ex, ey, er, G('#7fc0e8', 'r', .82, .3), 0)
    s += f'<clipPath id="{cid}"><circle cx="{ex}" cy="{ey}" r="{er}"/></clipPath><g clip-path="url(#{cid})">'
    # 대륙 (둥근 얼룩 몇 개) — 낙서풍: 연두 + 진한 선
    blobs = ((-.45, -.55, .42), (.25, -.7, .3), (.55, -.15, .34), (-.2, .05, .26), (-.7, .2, .22), (.15, .45, .3))
    for bx, by, br in blobs:
        cx_, cy_, r_ = ex + bx * er, ey + by * er, br * er
        pts = []
        for i in range(9):
            a = math.radians(i * 40 + rnd.uniform(-12, 12)); q = r_ * rnd.uniform(.72, 1.12)
            pts.append((cx_ + math.cos(a) * q, cy_ + math.sin(a) * q * .8))
        mid = [((pts[i][0] + pts[(i + 1) % 9][0]) / 2, (pts[i][1] + pts[(i + 1) % 9][1]) / 2) for i in range(9)]
        d = f'M{mid[0][0]:.1f} {mid[0][1]:.1f} ' + ' '.join(f'Q{pts[(i + 1) % 9][0]:.1f} {pts[(i + 1) % 9][1]:.1f} {mid[(i + 1) % 9][0]:.1f} {mid[(i + 1) % 9][1]:.1f}' for i in range(9)) + ' Z'
        s += P(d, G('#a9d68c', 'v', .86, .3), sw * .75)
        s += F(f'M{cx_ - r_ * .3:.1f} {cy_ - r_ * .25:.1f} Q{cx_:.1f} {cy_ - r_ * .45:.1f} {cx_ + r_ * .25:.1f} {cy_ - r_ * .2:.1f} Q{cx_:.1f} {cy_ - r_ * .3:.1f} {cx_ - r_ * .3:.1f} {cy_ - r_ * .25:.1f} Z', '#d7f0b8', .7)
    if clouds:   # 구름 띠 (흰 반투명)
        for i in range(9):
            cx_, cy_ = ex + rnd.uniform(-.75, .75) * er, ey + rnd.uniform(-.92, -.2) * er
            w_, h_ = er * rnd.uniform(.22, .4), er * rnd.uniform(.035, .06)
            s += f'<ellipse cx="{cx_:.1f}" cy="{cy_:.1f}" rx="{w_:.1f}" ry="{h_:.1f}" fill="#ffffff" opacity=".72" transform="rotate({rnd.uniform(-12, 8):.1f} {cx_:.1f} {cy_:.1f})"/>'
    # 밤 그늘 (오른쪽 아래) + 빛 (왼쪽 위)
    s += f'<circle cx="{ex + er * .55:.1f}" cy="{ey + er * .5:.1f}" r="{er * 1.05:.1f}" fill="#2c3a78" opacity=".22"/>'
    s += f'<circle cx="{ex - er * .45:.1f}" cy="{ey - er * .5:.1f}" r="{er * .55:.1f}" fill="#ffffff" opacity=".12"/>'
    s += '</g>'
    s += C(ex, ey, er, 'none', sw)
    s += f'<circle cx="{ex}" cy="{ey}" r="{er - sw * 1.3:.1f}" fill="none" stroke="#d9f3ff" stroke-width="{sw * .7:.1f}" opacity=".55"/>'
    return s


def moon_ground():
    """달 바닥 + 지평선 위로 떠오르는 지구 (캔버스 1080 x 700, 땅 = y 492)
    7차 수정안 "지구를 지킨다는 느낌으로 하단의 지구를 조금 더 크게": 반지름 160 → 330, 지평선 위로 더 올라옴"""
    g = GM_G
    s = earth(790, g + 70, 330, 7, seed=4)
    s += P(f'M-20 {g} Q200 {g - 30} 420 {g - 4} T860 {g + 6} T1100 {g - 10} L1100 720 L-20 720 Z', G('#cfc8dc', 'v', .86, .15), 7)
    s += F(f'M-20 {g + 8} Q200 {g - 22} 420 {g + 4} T860 {g + 14} T1100 {g - 2} L1100 {g + 16} T860 {g + 26} T420 {g + 16} Q200 {g - 8} -20 {g + 22} Z', '#ffffff', .25)
    for cx_, cy_, rx, ry in ((560, g + 90, 74, 20), (780, g + 165, 50, 14), (990, g + 110, 62, 18), (420, g + 180, 40, 12), (120, g + 140, 46, 13)):
        s += E(cx_, cy_, rx, ry, '#b3abc6', 4) + f'<ellipse cx="{cx_}" cy="{cy_ - ry * .35:.0f}" rx="{rx * .8:.0f}" ry="{ry * .45:.0f}" fill="#a39bb8"/>'
    rnd = random.Random(9)
    for _ in range(18):
        x, y = rnd.randint(20, 1060), rnd.randint(g + 30, GM_H - 10)
        s += f'<circle cx="{x}" cy="{y}" r="{rnd.uniform(3, 7):.1f}" fill="#bdb5cf"/>'
    return s


# ------------------------------------------------------------------ 7차 수정안: 로비 (사령탑 없이 · 심플한 하늘 · 큰 지구)
LE_W, LE_H, LE_TOP, LE_R = 1080, 760, 100, 780     # 로비 지구: 캔버스 1080 x 760 · 지구 꼭대기 y 100 · 반지름 780 (월드 7.8)


def lobby_earth():
    """로비 아래 큰 지구 (꼭대기 = 캔버스 y 100, 가운데 x 540) — 대장 카피가 그 위를 걸음"""
    return earth(LE_W / 2, LE_TOP + LE_R, LE_R, 8, seed=11)


def shooting_star():
    """별똥별 (360 x 64): 머리 = 오른쪽 (320, 32) · 꼬리는 왼쪽으로 가늘고 옅게"""
    hx, hy = 320, 32
    gid = 'ssTail'
    A._DEFS[gid] = (f'<linearGradient id="{gid}" x1="0" y1="0" x2="1" y2="0"><stop offset="0" stop-color="#fff4dc" stop-opacity="0"/>'
                    f'<stop offset=".7" stop-color="#ffe9a8" stop-opacity=".55"/><stop offset="1" stop-color="#ffffff" stop-opacity="1"/></linearGradient>')
    s = blur(f'<path d="M20 {hy} L{hx} {hy - 9} L{hx} {hy + 9} Z" fill="url(#{gid})"/>', 3, .9)
    s += f'<path d="M60 {hy} L{hx} {hy - 4} L{hx} {hy + 4} Z" fill="url(#{gid})"/>'
    s += blur(C(hx, hy, 20, '#fff2a8', 0), 7, .9) + C(hx, hy, 8, '#ffffff', 0)
    return s


def moon_disk():
    """전투 달 (260 x 260, 가운데 (130,130), 반지름 78) — 예전엔 하늘 그림에 그려져 있었음 → 따로 빼서 빈 곳(왼쪽 위)에 둠 (빛 번짐이 네모로 잘리지 않게 여백)"""
    return blur(C(130, 130, 92, '#f3e3c3', 0), 10, .35) + C(130, 130, 78, G('#f3e3c3', 'r', .88, .4), 5.5) + A.craters(130, 130, 78, '#e1cda3', 5, 4)


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
