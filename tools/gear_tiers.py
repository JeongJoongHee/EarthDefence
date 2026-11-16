# -*- coding: utf-8 -*-
"""4차 수정안 (11-17) "장비 아이콘 등급별로 다른 이미지로, 간단한 이미지에서 궁극까지 올라갈수록 더 멋있게" · "회로도 같은 방식으로".
등급(1~7)마다 모양이 한 단계씩 늘어남:
  1 일반 = 단순한 기본형 (무늬 없음, 흐린 색)   2 고급 = 띠 · 손잡이 같은 덧댐
  3 희귀 = 보석 · 조준경 같은 장치            4 영웅 = 날개깃 · 지느러미 · 어깨 보호대 (모양이 커짐)
  5 전설 = 금 장식 + 둘레 빛                 6 신화 = 뒤로 타오르는 기운(불꽃 · 망토)
  7 궁극 = 날개 · 왕관 · 별 반짝이 (금빛)
좌표는 0~1 (U() 가 크기를 곱함). earth_export.py export_icon 이 224px 로 그림 (예전 112px 의 2배 → 큰 창에서도 또렷).
※ 방사형 햇살 줄무늬는 쓰지 않음 (사용자 요청 — 기억 no-flag-like-imagery)
"""
import re
import earth_art as A
from earth_art import *   # noqa  (P L C E F INK …)

W = 96.0
PAL = {1: ('#c9c3b8', '#9a9488'), 2: ('#b9e09e', '#5fa64a'), 3: ('#a9d6f6', '#3f86cc'), 4: ('#d2b8f2', '#8458cc'),
       5: ('#ffd09a', '#e0862a'), 6: ('#f7aa9c', '#cc4a3e'), 7: ('#ffe48a', '#e0a020')}
GOLD = '#ffd34d'
NUM = re.compile(r'-?\d*\.\d+|-?\d+')


def U(d):
    """0~1 좌표 path → 픽셀"""
    return NUM.sub(lambda m: f'{float(m.group()) * W:.1f}', d)


def u(v): return v * W


S = 5.0   # 선 굵기


def halo(col, r=.5, cy=.52, op=.85): return A.blur(C(u(.5), u(cy), u(r), col, 0), 8, op)


def flames(col, s=S):
    """신화 이상: 둘레에 붉은 기운(두 겹 번짐) + 위로 떠오르는 불티 몇 개 (줄무늬 없음)"""
    out = A.blur(C(u(.5), u(.52), u(.44), col, 0), 9, .9) + A.blur(C(u(.5), u(.52), u(.44), 'none', u(.06), ink='#ffcf7a'), 4, .8)
    for x, y, r in ((.12, .3, .03), (.86, .22, .025), (.2, .82, .022), (.9, .64, .03), (.5, .04, .022)):
        out += A.blur(C(u(x), u(y), u(r * 1.8), col, 0), 2, 1) + C(u(x), u(y), u(r), '#fff0b8', 0)
    return out


def wings(cy=.5, col='#ffffff', s=S, k=1.0):
    """양쪽 날개 (깃 3단) — 오른쪽은 왼쪽을 뒤집어"""
    def one():
        d = U(f'M.36 {cy + .06} C.24 {cy - .2} .08 {cy - .3} .0 {cy - .3} C.02 {cy - .2} .05 {cy - .14} .08 {cy - .1} '
              f'C.02 {cy - .08} .0 {cy - .02} .02 {cy + .02} C.08 {cy + .02} .11 {cy + .04} .13 {cy + .07} '
              f'C.09 {cy + .1} .1 {cy + .15} .13 {cy + .17} C.22 {cy + .15} .3 {cy + .12} .38 {cy + .12} Z')
        lines = U(f'M.08 {cy - .1} Q.2 {cy - .08} .3 {cy} M.13 {cy + .07} Q.22 {cy + .06} .3 {cy + .08}')
        return P(d, A.G(col, 'v', .82, .5), s * .8) + L(lines, s * .45)
    sc = f'translate({u(.5)},{u(cy)}) scale({k}) translate({-u(.5)},{-u(cy)})'
    return f'<g transform="{sc}">{one()}<g transform="translate({W},0) scale(-1,1)">{one()}</g></g>'


def star4(cx, cy, r, col='#fff6c8', s=S):
    cx, cy, r = u(cx), u(cy), u(r)
    return P(f'M{cx} {cy - r} Q{cx + r * .15} {cy - r * .15} {cx + r} {cy} Q{cx + r * .15} {cy + r * .15} {cx} {cy + r} '
             f'Q{cx - r * .15} {cy + r * .15} {cx - r} {cy} Q{cx - r * .15} {cy - r * .15} {cx} {cy - r} Z', col, s * .4)


def star5(cx, cy, r, col=GOLD, s=S):
    import math
    pts = []
    for i in range(10):
        a = math.radians(-90 + i * 36); rr_ = r if i % 2 == 0 else r * .45
        pts.append(f'{cx + rr_ * math.cos(a):.3f} {cy + rr_ * math.sin(a):.3f}')
    return P(U('M' + ' L'.join(pts) + ' Z'), A.G(col, 'v', .8, .5), s * .5)


def gem(cx, cy, r, col, s=S):
    d = U(f'M{cx} {cy - r} L{cx + r * .85} {cy - r * .2} L{cx} {cy + r} L{cx - r * .85} {cy - r * .2} Z')
    top = U(f'M{cx - r * .85} {cy - r * .2} L{cx + r * .85} {cy - r * .2}')
    return A.blur(C(u(cx), u(cy), u(r * 1.1), col, 0), 3, .8) + P(d, A.G(col, 'v', .78, .55), s * .55) + L(top, s * .35) + C(u(cx - r * .25), u(cy - r * .45), u(r * .2), '#ffffff', 0)


def crown(cx, cy, k, s=S):
    d = U(f'M{cx - .2 * k} {cy} L{cx - .22 * k} {cy - .2 * k} L{cx - .1 * k} {cy - .1 * k} L{cx} {cy - .26 * k} L{cx + .1 * k} {cy - .1 * k} L{cx + .22 * k} {cy - .2 * k} L{cx + .2 * k} {cy} Z')
    return P(d, A.G(GOLD, 'v', .78, .5), s * .7) + C(u(cx), u(cy - .07 * k), u(.035 * k), '#ee6b5a', s * .35)


def sparkles(s=S):
    return star4(.1, .14, .09, s=s) + star4(.9, .86, .075, s=s) + star4(.92, .2, .05, s=s)


# ---------------------------------------------------------------- 부위별
def weapon(g):
    m, t = PAL[g]; G = A.G; o = ''
    if g >= 5: o += halo(m, .46)
    if g >= 6: o += flames(t)
    if g >= 7: o += wings(.44, '#fff6d8', k=.95)
    if g >= 4: o += P(U('M.14 .4 L.03 .24 L.26 .38 Z'), G(t, 'v'), S * .8)                          # 뒤 지느러미
    o += P(U('M.24 .54 L.44 .54 L.38 .88 Q.3 .92 .2 .86 Z'), G('#8a929c' if g < 5 else '#6b5a8a', 'v'), S)   # 손잡이
    blen = .2 + .04 * min(g, 5)
    if g >= 4: o += P(U(f'M.4 .55 L{.6 + blen} .55 L{.5 + blen} .67 L.48 .64 Z'), G(t, 'v', .8, .5), S * .8)    # 아래 칼날
    o += A.rr(u(.58), u(.41), u(blen + .04), u(.1), 4, G('#c4cbd3' if g < 7 else GOLD, 'v', .78, .5), S * .9)       # 총열
    o += A.rr(u(.08), u(.34), u(.56), u(.21), 8, G(m, 'v', .78, .45), S)                                # 몸통
    if g >= 2: o += A.rr(u(.14), u(.45), u(.42), u(.05), 2, t, 0)                                       # 띠
    if g >= 3:                                                                                          # 조준경
        o += A.rr(u(.2), u(.22), u(.28), u(.1), 4, G('#9aa3ad', 'v'), S * .8) + C(u(.48), u(.27), u(.045), '#7ff0ff', S * .5)
    if g >= 5:                                                                                          # 총열 빛 고리
        for i in range(3):
            x = .64 + i * (blen - .04) / 3
            o += A.blur(E(u(x), u(.46), u(.03), u(.08), t, 0), 2, 1) + E(u(x), u(.46), u(.018), u(.07), '#fff4c8', S * .4)
    tip = .62 + blen
    if g >= 6: o += A.blur(C(u(tip + .02), u(.46), u(.09), t, 0), 4, 1) + C(u(tip + .02), u(.46), u(.04), '#fffbe0', 0)
    if g >= 7: o += star4(tip + .02, .46, .1)
    if g == 1: o += L(U('M.16 .38 L.22 .48'), S * .45, '#7a7266')                                      # 흠집
    o += F(U('M.14 .37 L.56 .37 L.56 .4 L.14 .4 Z'), '#ffffff', .7)
    if g >= 7: o += sparkles()
    return o


def helmet(g):
    m, t = PAL[g]; G = A.G; o = ''
    if g >= 5: o += halo(m, .48)
    if g >= 6: o += flames(t)
    if g >= 7: o += wings(.56, '#fff6d8', k=1.0)
    if g >= 4:                                                                                          # 옆 지느러미
        o += P(U('M.14 .5 L.0 .3 L.06 .58 Z'), G(t, 'v'), S * .8) + P(U('M.86 .5 L1 .3 L.94 .58 Z'), G(t, 'v'), S * .8)
    if g >= 6:                                                                                          # 뿔
        o += P(U('M.3 .26 Q.18 .1 .2 .0 Q.3 .1 .38 .2 Z'), G('#fff0d8', 'v'), S * .7) + P(U('M.7 .26 Q.82 .1 .8 .0 Q.7 .1 .62 .2 Z'), G('#fff0d8', 'v'), S * .7)
    if g >= 3 and g < 5:                                                                                # 안테나 + 보석
        o += L(U('M.66 .2 L.76 .06'), S * .8) + gem(.77, .06, .06, t)
    if g == 5: o += P(U('M.3 .2 Q.5 -.04 .7 .2 Q.5 .12 .3 .2 Z'), G(GOLD, 'v', .8, .5), S * .7)       # 볏
    r = .36 if g == 1 else .41
    o += C(u(.5), u(.55), u(r), G(m, 'r', .82, .3), S)
    if g == 1: o += A.rr(u(.28), u(.44), u(.44), u(.2), 10, G('#4a6f99', 'v', .75, .3), S * .8)      # 작은 창
    else: o += P(U('M.2 .5 Q.5 .26 .8 .5 Q.8 .76 .5 .78 Q.2 .76 .2 .5 Z'), G('#ffd36b' if g >= 7 else '#5fb7ea', 'v', .7, .45), S * .8)
    o += F(U('M.3 .5 Q.36 .38 .5 .37 Q.38 .43 .35 .55 Z'), '#ffffff', .8)
    if g >= 2: o += A.rr(u(.14), u(.82), u(.72), u(.1), 5, G(t, 'v'), S * .7)
    if g >= 5: o += L(U('M.2 .86 L.8 .86'), S * .4, GOLD)
    if g >= 7: o += crown(.5, .2, .9) + sparkles()
    return o


def suit(g):
    m, t = PAL[g]; G = A.G; o = ''
    if g >= 5: o += halo(m, .48)
    if g >= 7: o += wings(.42, '#fff6d8', k=1.05)
    if g >= 6: o += P(U('M.26 .16 L.74 .16 L.94 .96 L.06 .96 Z'), G(t, 'v', .75, .4), S * .8)         # 망토
    body = 'M.3 .14 L.7 .14 L.9 .36 L.78 .48 L.76 .92 L.24 .92 L.22 .48 L.1 .36 Z' if g > 1 else 'M.32 .16 L.68 .16 L.84 .34 L.74 .44 L.72 .9 L.28 .9 L.26 .44 L.16 .34 Z'
    o += P(U(body), G(m, 'v', .8, .3), S)
    o += E(u(.5), u(.16), u(.2), u(.07), G('#cfd8de', 'v'), S * .8)
    if g >= 4: o += E(u(.14), u(.34), u(.12), u(.08), G(t, 'v'), S * .8, -30) + E(u(.86), u(.34), u(.12), u(.08), G(t, 'v'), S * .8, 30)   # 어깨
    if g >= 2: o += A.rr(u(.22), u(.66), u(.56), u(.09), 4, G(t, 'v'), S * .7)                       # 띠
    if g >= 5: o += L(U('M.5 .22 L.5 .64'), S * .45, GOLD) + L(U('M.32 .22 L.4 .64 M.68 .22 L.6 .64'), S * .35, GOLD)
    if g >= 7: o += star5(.5, .44, .13)
    elif g >= 3: o += gem(.5, .44, .1, t)
    if g == 1: o += L(U('M.6 .7 L.66 .78'), S * .45, '#8a8276')
    o += F(U('M.3 .2 L.37 .2 L.31 .6 L.25 .6 Z'), '#ffffff', .55)
    if g >= 7: o += sparkles()
    return o


def shield(g):
    m, t = PAL[g]; G = A.G; o = ''
    if g >= 5: o += halo(m, .48)
    if g >= 6: o += flames(t)
    if g >= 7: o += wings(.48, '#fff6d8', k=1.1)
    elif g >= 6: o += wings(.5, '#fff0e0', k=.8)
    if g == 1:                                                                                          # 둥근 작은 방패
        o += C(u(.5), u(.52), u(.4), G(m, 'r', .8, .3), S) + C(u(.5), u(.52), u(.13), G('#9aa3ad', 'r'), S * .8)
        o += L(U('M.24 .36 L.32 .42'), S * .45, '#7a7266')
        return o
    sh = 'M.5 .08 L.88 .2 Q.9 .66 .5 .94 Q.1 .66 .12 .2 Z'
    inn = 'M.5 .2 L.76 .29 Q.76 .62 .5 .8 Q.24 .62 .24 .29 Z'
    if g >= 4:                                                                                          # 테두리 가시
        for d in ('M.2 .2 L.2 .06 L.3 .16 Z', 'M.8 .2 L.8 .06 L.7 .16 Z', 'M.42 .1 L.5 -.02 L.58 .1 Z'):
            o += P(U(d), G('#e6e9ee', 'v'), S * .6)
    o += P(U(sh), G(t, 'v', .75, .4), S) + P(U(inn), G(m, 'v', .72, .5), S * .7)
    if g >= 4:
        for x, y in ((.2, .26), (.8, .26), (.17, .5), (.83, .5), (.32, .78), (.68, .78)):
            o += C(u(x), u(y), u(.025), '#f4efe6', S * .35)
    if g >= 5: o += star5(.5, .5, .17)
    elif g >= 3: o += gem(.5, .5, .12, t)
    else: o += L(U('M.5 .26 L.5 .74 M.32 .46 L.68 .46'), S * .6, t)
    o += F(U('M.2 .24 L.42 .15 L.3 .4 Z'), '#ffffff', .5)
    if g >= 7: o += crown(.5, .1, .7) + sparkles()
    return o


def core(g, plain=False):
    """에너지 코어 (11-17 사용자 "코어가 장비 강화 부품(육각 너트)과 비슷함" → 육각형 대신 세로 캡슐:
    유리관 안에 빛나는 구슬 + 위아래 금속 마개). plain = 빈 칸용 기본 그림"""
    m, t = PAL[g]; G = A.G; o = ''
    if plain: m, t = '#8fd6ff', '#a9b1bb'
    if g >= 5 and not plain: o += halo(m, .48)
    if g >= 6 and not plain: o += flames(t)
    if g >= 4 and not plain:                                                                            # 뒤쪽 고리 반
        o += f'<path d="{f"M{u(.04)} {u(.56)} A{u(.46)} {u(.14)} 0 0 1 {u(.96)} {u(.56)}"}" fill="none" stroke="{INK}" stroke-width="{S * 1.3}"/>'
        o += f'<path d="{f"M{u(.04)} {u(.56)} A{u(.46)} {u(.14)} 0 0 1 {u(.96)} {u(.56)}"}" fill="none" stroke="{t}" stroke-width="{S * .55}"/>'
    w0 = .26 if g == 1 else .22
    glass = '#c9d3d8' if g == 1 else A.mix(m, '#ffffff', .45)
    o += A.rr(u(w0), u(.16), u(1 - 2 * w0), u(.68), u(.12), G(glass, 'v', .85, .5), S)                  # 유리관
    orb_r = .12 if g == 1 else .16
    if g >= 2: o += A.blur(C(u(.5), u(.5), u(orb_r + .06), m, 0), 5, 1)
    o += C(u(.5), u(.5), u(orb_r), G(m if g > 1 else '#e0dbd2', 'r', .85, .6), S * .7)
    if g >= 7 and not plain: o += star5(.5, .5, .12, '#fff6c8')
    else: o += C(u(.46), u(.45), u(orb_r * .35), '#ffffff', 0)
    cap = GOLD if g >= 5 and not plain else ('#a9b1bb' if g < 4 else t)
    o += A.rr(u(w0 - .06), u(.06), u(1 - 2 * w0 + .12), u(.14), 6, G(cap, 'v', .78, .45), S * .9)     # 위 마개
    o += A.rr(u(w0 - .06), u(.8), u(1 - 2 * w0 + .12), u(.14), 6, G(cap, 'v', .78, .45), S * .9)      # 아래 마개
    if g >= 2: o += A.rr(u(w0 + .02), u(.68), u(1 - 2 * w0 - .04), u(.05), 2, t, 0)                     # 띠
    if g >= 3 and not plain:
        for x in (w0, 1 - w0):
            o += C(u(x), u(.13), u(.022), '#6b737d', 0) + C(u(x), u(.87), u(.022), '#6b737d', 0)
        o += gem(.5, .13, .05, t)
    if g >= 4 and not plain:                                                                            # 앞쪽 고리 반
        o += f'<path d="{f"M{u(.04)} {u(.56)} A{u(.46)} {u(.14)} 0 0 0 {u(.96)} {u(.56)}"}" fill="none" stroke="{INK}" stroke-width="{S * 1.3}"/>'
        o += f'<path d="{f"M{u(.04)} {u(.56)} A{u(.46)} {u(.14)} 0 0 0 {u(.96)} {u(.56)}"}" fill="none" stroke="{t}" stroke-width="{S * .55}"/>'
        o += C(u(.9), u(.62), u(.035), '#fffbe0', S * .4)
    if g >= 5 and not plain:
        o += E(u(.5), u(.4), u(.46), u(.12), 'none', S * .45, 20, ink=GOLD) + C(u(.08), u(.3), u(.03), '#fffbe0', S * .35)
    o += F(U(f'M{w0 + .06} .2 L{w0 + .12} .2 L{w0 + .12} .78 L{w0 + .06} .78 Z'), '#ffffff', .55)       # 유리 반짝임
    if g == 1: o += L(U('M.6 .3 L.66 .38'), S * .4, '#8a8276')
    if g >= 7 and not plain: o += sparkles()
    return o


def boots(g):
    m, t = PAL[g]; G = A.G; o = ''
    if g >= 5: o += halo(m, .46)
    if g >= 6: o += flames(t)
    if g >= 7: o += wings(.36, '#fff6d8', k=.85)
    if g >= 5:                                                                                          # 발 아래 분사
        o += A.blur(E(u(.36), u(.95), u(.16), u(.06), t, 0), 3, 1) + P(U('M.26 .9 Q.36 1.02 .46 .9 Z'), G('#ffd36b', 'v'), S * .5)
    if g >= 4: o += P(U('M.26 .5 L.08 .62 L.24 .7 Z'), G(t, 'v'), S * .8)                            # 뒤꿈치 지느러미
    body = 'M.3 .08 L.6 .08 L.6 .54 L.88 .64 Q.94 .84 .8 .86 L.24 .86 Q.18 .86 .2 .68 Z' if g > 1 else 'M.32 .14 L.58 .14 L.58 .56 L.82 .66 Q.86 .82 .76 .84 L.26 .84 Q.2 .84 .22 .7 Z'
    o += P(U(body), G(m, 'v', .8, .3), S)
    if g >= 2: o += A.rr(u(.18), u(.78), u(.74), u(.11), 5, G(t, 'v'), S * .7)                        # 밑창
    if g >= 5: o += P(U('M.66 .64 L.88 .64 Q.94 .78 .86 .8 L.66 .78 Z'), G(GOLD, 'v'), S * .6)       # 금 앞코
    o += A.rr(u(.28), u(.26), u(.34), u(.08), 3, G('#9aa3ad', 'v'), S * .6)
    if g >= 3: o += gem(.45, .3, .07, t)
    if g == 1: o += L(U('M.36 .5 L.42 .58'), S * .45, '#8a8276')
    o += F(U('M.34 .12 L.4 .12 L.38 .5 L.33 .5 Z'), '#ffffff', .6)
    if g >= 7: o += sparkles()
    return o


def chip(g):
    m, t = PAL[g]; G = A.G; o = ''
    if g >= 5: o += halo(m, .48)
    n = 2 if g == 1 else 3 if g <= 3 else 4
    xs = [.5 + (i - (n - 1) / 2) * (.42 / max(1, n - 1) if n > 1 else 0) for i in range(n)]
    b0, b1 = (.26, .74) if g == 1 else (.2, .8)
    pins = ''
    for x in xs:
        pins += f'M{x} {b0 - .12} L{x} {b0} M{x} {b1} L{x} {b1 + .12} M{b0 - .12} {x} L{b0} {x} M{b1} {x} L{b1 + .12} {x} '
    o += L(U(pins), S * .9) + L(U(pins), S * .35, GOLD if g >= 5 else '#c9c1b3')
    o += A.rr(u(b0), u(b0), u(b1 - b0), u(b1 - b0), 8, G(GOLD if g >= 7 else m, 'v', .8, .4), S)
    if g >= 6: o += A.rr(u(.28), u(.28), u(.44), u(.44), 6, 'none', S * .5).replace(f'stroke="{INK}"', f'stroke="{t}"')
    if g >= 3: o += L(U('M.26 .32 L.34 .4 M.74 .68 L.66 .6 M.26 .7 L.34 .62 M.74 .3 L.66 .38'), S * .45, '#ffe28a')
    if g >= 5: o += A.blur(C(u(.5), u(.5), u(.16), t, 0), 5, 1)
    o += A.rr(u(.36), u(.36), u(.28), u(.28), 4, G(t, 'v', .75, .4), S * .6)
    if g >= 7: o += star5(.5, .5, .12, '#fff6c8')
    elif g >= 5: o += C(u(.5), u(.5), u(.05), '#fffbe0', 0)
    if g >= 4:
        for x, y in ((.27, .27), (.73, .27), (.27, .73), (.73, .73)):
            o += C(u(x), u(y), u(.025), '#7ff0ff' if g < 6 else '#fff4c8', 0)
    if g == 1: o += L(U('M.62 .3 L.68 .36'), S * .4, '#7a7266')
    o += F(U('M.24 .24 L.5 .24 L.46 .28 L.24 .28 Z'), '#ffffff', .6)
    if g >= 7: o += sparkles()
    return o


PARTS = dict(weapon=weapon, helmet=helmet, suit=suit, shield=shield, core=core, boots=boots)
