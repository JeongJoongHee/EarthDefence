# -*- coding: utf-8 -*-
"""밝은 낙서풍 그림 도구 (카치건 2026-11-05 화풍을 게임에 묶이지 않게 뽑은 것).

SVG 로 그린 뒤 Edge 헤드리스로 투명 PNG 를 뽑는다. 새 프로젝트에선 이 파일을 tools/ 에 복사해서
`from doodle_kit import *` 로 쓰고, 게임별 그림(캐릭터·적·UI)은 따로 tools/<게임>_art.py 에 만든다.

사용: PYTHONUTF8=1 python doodle_kit.py demo <출력폴더>     → 화풍 견본 한 장(doodle_demo.png)
      ※ PowerShell 에서 실행 (Bash 샌드박스에선 Edge 가 바로 끝나 그림이 안 나옴)

화풍 규칙 요약 (자세히는 SKILL.md):
  선 = 진한 갈색 INK #3b281c, 둥근 끝·둥근 이음 / 굵은 선 + 단색 칠 + 파스텔(쨍한 색 금지)
  살짝 흔들리는 손그림 선(feTurbulence) — 세기는 절반(WOB_K .5): 사용자 "테두리를 조금 매끈하게"
  주인공 선은 더 진하고(#1c110a) 덜 흔들리게(wob 1.0) / 2배로 그려 줄여 가장자리 매끈(SS 2)
"""
import os, sys, math, random, subprocess, tempfile, shutil
import numpy as np
from PIL import Image

EDGE = r'C:\Program Files (x86)\Microsoft\Edge\Application\msedge.exe'

# ------------------------------------------------------------------ 색 (카치건에서 쓴 값 그대로)
INK = '#3b281c'          # 모든 선
CHAR_INK = '#1c110a'     # 주인공 선 (더 진하게)
CREAM, CREAM2, TAN = '#fff4dc', '#f3e3c3', '#e9d3a8'           # 판·창 바탕
ORANGE, ORANGE2 = '#f2ad55', '#d98f3c'                         # 기본 버튼 (윗면, 아래 턱)
GREEN, GREEN2, GREEN_D = '#8ccb6e', '#6fae55', '#3f7d3a'       # 확인·강화 버튼, 값 띠
RED, RED2 = '#e8735f', '#c95544'                               # 경고·닫기
GOLD, GOLD2 = '#ffd34d', '#e0a722'                             # 금화·별
TEAL, TRACK = '#6cc4c0', '#5b4433'                             # 게이지, 게이지 바탕
SKY = '#8fd6ff'                                                # 보석·레이저
STEEL, STEEL2 = '#8f97a3', '#5d6470'                           # 총·쇠
WOOD = '#9a6a44'                                               # 나무 손잡이·나무 줄기
ICON_B, ICON_W = '#8a5e3c', CREAM                              # 갈색 아이콘 / 흰 아이콘
# 배경 (밝은 숲길)
GRASS, DIRT, DIRT2, ROCK = '#a9cf86', '#ecdcb8', '#e1cda3', '#c9c1b3'
LEAVES = ('#79b05a', '#86ba62', '#8fc06c', '#9ccb78')
# 동물 마스코트 (카피바라 갈색)
FUR, FUR_DARK, FUR_BELLY = '#c49a6e', '#a97f58', '#d9b88f'
# 좀비·적 피부
ZSKIN = '#8cc56a'

WOB_K = 0.5      # 흔들림 전체 배율 (1 이면 처음 샘플, .5 가 최종 선택)
SS = 2           # 몇 배로 그려 줄일지


# ------------------------------------------------------------------ SVG → 투명 PNG
def render(svg_body, w, h, wob=3.0, freq=0.022, ss=None):
    """svg_body 를 w x h 투명 PNG(np RGBA)로. wob = 손그림 흔들림 세기(px) · freq = 흔들림 결(작을수록 큰 물결)
    권장: 캐릭터 wob 1.0 · 적 4.0/freq .018 · UI 2.0/freq .04 · 아이콘 1.2 · 배경 6.0/freq .008"""
    ss = ss or SS
    wob = wob * WOB_K
    tmp = tempfile.mkdtemp(prefix='doodle_')
    try:
        svg = f'''<svg xmlns="http://www.w3.org/2000/svg" width="{w}" height="{h}" viewBox="0 0 {w} {h}">
<defs><filter id="wob" filterUnits="userSpaceOnUse" x="0" y="0" width="{w}" height="{h}">
<feTurbulence type="fractalNoise" baseFrequency="{freq}" numOctaves="2" seed="7"/>
<feDisplacementMap in="SourceGraphic" scale="{wob}"/></filter></defs>
<g filter="url(#wob)">{svg_body}</g></svg>'''
        html = f'<!doctype html><html><head><meta charset="utf-8"><style>html,body{{margin:0;background:transparent;overflow:hidden}}</style></head><body>{svg}</body></html>'
        hp = os.path.join(tmp, 'a.html'); open(hp, 'w', encoding='utf-8').write(html)
        out = os.path.join(tmp, 'a.png')
        url = 'file:///' + hp.replace('\\', '/')
        subprocess.run([EDGE, '--headless=new', '--disable-gpu', '--hide-scrollbars', '--default-background-color=00000000',
                        f'--window-size={w},{h}', f'--force-device-scale-factor={ss}', '--virtual-time-budget=2000',
                        f'--user-data-dir={os.path.join(tmp, "prof")}', f'--screenshot={out}', url],
                       stdout=subprocess.DEVNULL, stderr=subprocess.DEVNULL, timeout=120)
        if not os.path.exists(out):
            raise RuntimeError('Edge 가 그림을 못 만듦 — PowerShell 에서 실행했는지 확인 (Bash 샌드박스에선 안 됨)')
        big = Image.open(out).convert('RGBA').crop((0, 0, w * ss, h * ss))
        if ss != 1:   # 미리 곱한 알파로 줄여야 가장자리에 검은 테가 안 생김
            big = big.convert('RGBa').resize((w, h), Image.LANCZOS).convert('RGBA')
        return np.array(big)
    finally:
        shutil.rmtree(tmp, ignore_errors=True)


def save(arr, path):
    os.makedirs(os.path.dirname(path) or '.', exist_ok=True)
    Image.fromarray(arr).save(path)


# ------------------------------------------------------------------ 그리기 조각
def P(d, fill, sw=6.0, extra='', ink=INK):
    """테두리 있는 칠 도형"""
    return f'<path d="{d}" fill="{fill}" stroke="{ink}" stroke-width="{sw}" stroke-linejoin="round" stroke-linecap="round" {extra}/>'


def L(d, sw=6.0, color=INK):
    """선만"""
    return f'<path d="{d}" fill="none" stroke="{color}" stroke-width="{sw}" stroke-linejoin="round" stroke-linecap="round"/>'


def C(cx, cy, r, fill, sw=6.0, ink=INK):
    return f'<circle cx="{cx}" cy="{cy}" r="{r}" fill="{fill}" stroke="{ink}" stroke-width="{sw}"/>'


def E(cx, cy, rx, ry, fill, sw=6.0, rot=0, ink=INK):
    t = f' transform="rotate({rot} {cx} {cy})"' if rot else ''
    return f'<ellipse cx="{cx}" cy="{cy}" rx="{rx}" ry="{ry}" fill="{fill}" stroke="{ink}" stroke-width="{sw}"{t}/>'


def F(d, fill, op=1):
    """테두리 없는 칠 (그림자·배 무늬·얼룩)"""
    return f'<path d="{d}" fill="{fill}" stroke="none" opacity="{op}"/>'


def limb(d, w, fill, sw=6.0):
    """굵은 막대(팔·다리·총구 띠): 굵은 선 위에 칠 선"""
    return (f'<path d="{d}" fill="none" stroke="{INK}" stroke-width="{w + 2 * sw}" stroke-linecap="round" stroke-linejoin="round"/>'
            f'<path d="{d}" fill="none" stroke="{fill}" stroke-width="{w}" stroke-linecap="round" stroke-linejoin="round"/>')


def zig(x0, x1, y, n, amp):
    """찢어진 옷단: x1 → x0 지그재그 (path 이어 쓰기용)"""
    return ' '.join(f'L{x1 - (x1 - x0) * i / n:.1f} {y + (amp if i % 2 else 0):.1f}' for i in range(n + 1))


def rr(x, y, w, h, r, fill, sw=6, extra=''):
    return f'<rect x="{x}" y="{y}" width="{w}" height="{h}" rx="{r}" fill="{fill}" stroke="{INK}" stroke-width="{sw}" {extra}/>'


def rrf(x, y, w, h, r, fill, op=1):
    return f'<rect x="{x}" y="{y}" width="{w}" height="{h}" rx="{r}" fill="{fill}" opacity="{op}"/>'


def button(w, h, fill, lip, r=18, sw=6, lipH=8):
    """아래 턱이 있는 둥근 버튼/판: 테두리+턱색 → 윗면 → 위쪽 하이라이트(흰 35%)"""
    m = sw / 2 + 1
    return rr(m, m, w - 2 * m, h - 2 * m, r, lip, sw) + rrf(m + sw / 2, m + sw / 2, w - 2 * m - sw, h - 2 * m - sw - lipH, r - 4, fill) \
        + rrf(m + sw / 2 + 6, m + sw / 2 + 4, w - 2 * m - sw - 12, 6, 3, '#ffffff', .35)


def coin(cx, cy, r, sw=None):
    """둥근 동전 (사용자: 금화 더미보다 둥근 동전이 알아보기 쉬움)"""
    sw = sw or max(3, r * .16)
    return C(cx, cy, r, GOLD, sw) + f'<circle cx="{cx}" cy="{cy}" r="{r * .6}" fill="none" stroke="{GOLD2}" stroke-width="{r * .2}"/>'


def burst(cx, cy, r, n, fill, sw, k=.55, rot=0):
    """뾰족 별 터짐 (폭발·총구 불꽃·공격 아이콘)"""
    pts = []
    for i in range(n * 2):
        a = math.radians(rot) + i * math.pi / n; q = r if i % 2 == 0 else r * k
        pts.append(f'{cx + q * math.cos(a):.1f} {cy + q * math.sin(a):.1f}')
    return P('M' + ' L'.join(pts) + ' Z', fill, sw)


def puff(cx, cy, r, fill, sw, n=7, seed=1):
    """뭉게 연기·구름 (원 n 개 + 가운데 메움)"""
    rnd = random.Random(seed); out = []
    for i in range(n):
        a = i * 2 * math.pi / n; d = r * .55
        out.append(f'<circle cx="{cx + d * math.cos(a):.1f}" cy="{cy + d * math.sin(a):.1f}" r="{r * rnd.uniform(.42, .55):.1f}" fill="{fill}" stroke="{INK}" stroke-width="{sw}"/>')
    out.append(f'<circle cx="{cx}" cy="{cy}" r="{r * .6}" fill="{fill}"/>')
    return ''.join(out)


def trail(x0, x1, y, h, c):
    """총알 꼬리 (반투명 삼각형)"""
    return f'<path d="M{x0} {y} L{x1} {y - h / 2} L{x1} {y + h / 2} Z" fill="{c}" opacity=".75"/>'


# ------------------------------------------------------------------ 아이콘 (w x h 안에, f = 칠 색, s = 선 굵기)
def ic_check(w, h, f, s): d = f'M{w*.24} {h*.54} L{w*.42} {h*.72} L{w*.78} {h*.28}'; return L(d, w * .26) + L(d, w * .26 - 2 * s, f)
def ic_x(w, h, f, s): d = f'M{w*.3} {h*.3} L{w*.7} {h*.7} M{w*.7} {h*.3} L{w*.3} {h*.7}'; return L(d, w * .24) + L(d, w * .24 - 2 * s, f)
def ic_chest(w, h, f, s): return P(f'M{w*.1} {h*.42} L{w*.9} {h*.42} L{w*.9} {h*.9} L{w*.1} {h*.9} Z', f, s) + P(f'M{w*.1} {h*.42} Q{w*.1} {h*.1} {w*.5} {h*.1} Q{w*.9} {h*.1} {w*.9} {h*.42} Z', f, s) + rr(w * .42, h * .38, w * .16, h * .2, 3, GOLD, s * .8)
def ic_diamond(w, h, f, s): return P(f'M{w*.5} {h*.06} L{w*.92} {h*.4} L{w*.5} {h*.94} L{w*.08} {h*.4} Z', f, s) + L(f'M{w*.08} {h*.4} L{w*.92} {h*.4} M{w*.3} {h*.4} L{w*.5} {h*.94} L{w*.7} {h*.4}', s * .6)
def ic_down(w, h, f, s): return P(f'M{w*.35} {h*.08} L{w*.65} {h*.08} L{w*.65} {h*.5} L{w*.9} {h*.5} L{w*.5} {h*.92} L{w*.1} {h*.5} L{w*.35} {h*.5} Z', f, s)
def ic_play(w, h, f, s): return P(f'M{w*.2} {h*.1} L{w*.88} {h*.5} L{w*.2} {h*.9} Z', f, s)
def ic_pause(w, h, f, s): return rr(w * .12, h * .1, w * .28, h * .8, w * .1, f, s) + rr(w * .6, h * .1, w * .28, h * .8, w * .1, f, s)
def ic_heart(w, h, f, s): return P(f'M{w*.5} {h*.9} C{w*.1} {h*.62} {w*.02} {h*.38} {w*.18} {h*.2} C{w*.32} {h*.06} {w*.48} {h*.16} {w*.5} {h*.3} C{w*.52} {h*.16} {w*.68} {h*.06} {w*.82} {h*.2} C{w*.98} {h*.38} {w*.9} {h*.62} {w*.5} {h*.9} Z', f, s)
def ic_info(w, h, f, s): return C(w * .5, h * .14, w * .3, f, s) + rr(w * .22, h * .32, w * .56, h * .6, w * .25, f, s)
def ic_lock(w, h, f, s): return L(f'M{w*.28} {h*.48} L{w*.28} {h*.3} Q{w*.28} {h*.08} {w*.5} {h*.08} Q{w*.72} {h*.08} {w*.72} {h*.3} L{w*.72} {h*.48}', w * .14) + rr(w * .1, h * .44, w * .8, h * .5, w * .12, f, s) + C(w * .5, h * .64, w * .08, INK, 0) + rrf(w * .46, h * .66, w * .08, h * .14, 1, INK)
def ic_person(w, h, f, s): return C(w * .5, h * .3, w * .22, f, s) + P(f'M{w*.12} {h*.92} Q{w*.12} {h*.56} {w*.5} {h*.56} Q{w*.88} {h*.56} {w*.88} {h*.92} Z', f, s)
def ic_question(w, h, f, s): d = f'M{w*.26} {h*.3} Q{w*.26} {h*.08} {w*.52} {h*.08} Q{w*.8} {h*.1} {w*.78} {h*.32} Q{w*.76} {h*.48} {w*.52} {h*.56} L{w*.52} {h*.66}'; return L(d, w * .26) + L(d, w * .26 - 2 * s, f) + C(w * .52, h * .86, w * .12, f, s)
def ic_shield(w, h, f, s): return P(f'M{w*.5} {h*.06} L{w*.9} {h*.2} Q{w*.92} {h*.66} {w*.5} {h*.94} Q{w*.08} {h*.66} {w*.1} {h*.2} Z', f, s)
def ic_skull(w, h, f, s): return P(f'M{w*.5} {h*.08} Q{w*.92} {h*.08} {w*.92} {h*.46} Q{w*.92} {h*.66} {w*.74} {h*.72} L{w*.74} {h*.9} L{w*.26} {h*.9} L{w*.26} {h*.72} Q{w*.08} {h*.66} {w*.08} {h*.46} Q{w*.08} {h*.08} {w*.5} {h*.08} Z', f, s) + C(w * .34, h * .46, w * .1, INK, 0) + C(w * .66, h * .46, w * .1, INK, 0)
def ic_sound(w, h, f, s): return P(f'M{w*.08} {h*.36} L{w*.26} {h*.36} L{w*.5} {h*.12} L{w*.5} {h*.88} L{w*.26} {h*.64} L{w*.08} {h*.64} Z', f, s) + L(f'M{w*.64} {h*.32} Q{w*.74} {h*.5} {w*.64} {h*.68} M{w*.76} {h*.18} Q{w*.94} {h*.5} {w*.76} {h*.82}', s * 1.4)
def ic_star(w, h, f, s):
    pts = []
    for i in range(10):
        a = -math.pi / 2 + i * math.pi / 5; r = (.46 if i % 2 == 0 else .2) * min(w, h)
        pts.append(f'{w * .5 + r * math.cos(a):.1f} {h * .54 + r * math.sin(a):.1f}')
    return P('M' + ' L'.join(pts) + ' Z', f, s)
def ic_sword(w, h, f, s): return P(f'M{w*.86} {h*.1} L{w*.9} {h*.14} L{w*.4} {h*.64} L{w*.34} {h*.58} Z', '#e6e9ee', s) + L(f'M{w*.24} {h*.5} L{w*.5} {h*.76}', s * 1.6) + L(f'M{w*.24} {h*.5} L{w*.5} {h*.76}', s * .7, f) + L(f'M{w*.36} {h*.64} L{w*.12} {h*.88}', s * 2.2) + L(f'M{w*.36} {h*.64} L{w*.12} {h*.88}', s * 1.1, f)
def ic_trophy(w, h, f, s): return P(f'M{w*.24} {h*.1} L{w*.76} {h*.1} L{w*.72} {h*.42} Q{w*.5} {h*.6} {w*.28} {h*.42} Z', f, s) + L(f'M{w*.24} {h*.18} Q{w*.06} {h*.18} {w*.12} {h*.34} Q{w*.18} {h*.42} {w*.28} {h*.4} M{w*.76} {h*.18} Q{w*.94} {h*.18} {w*.88} {h*.34} Q{w*.82} {h*.42} {w*.72} {h*.4}', s) + rr(w * .42, h * .56, w * .16, h * .2, 2, f, s) + rr(w * .26, h * .76, w * .48, h * .14, 4, f, s)
def ic_music(w, h, f, s): return L(f'M{w*.34} {h*.76} L{w*.34} {h*.16} L{w*.84} {h*.08} L{w*.84} {h*.68}', s * 1.6) + E(w * .24, h * .78, w * .16, h * .12, f, s) + E(w * .74, h * .7, w * .16, h * .12, f, s) + L(f'M{w*.34} {h*.16} L{w*.84} {h*.08}', s * 1.0, f)
def ic_gear(w, h, f, s):
    out = [f'<rect x="{w*.42}" y="{h*.04}" width="{w*.16}" height="{h*.2}" rx="3" fill="{f}" stroke="{INK}" stroke-width="{s}" transform="rotate({i * 45} {w*.5} {h*.5})"/>' for i in range(8)]
    return ''.join(out) + C(w * .5, h * .5, w * .32, f, s) + C(w * .5, h * .5, w * .12, CREAM if f != CREAM else ICON_B, s)
def ic_bolt(w, h, f, s): return P(f'M{w*.62} {h*.06} L{w*.16} {h*.58} L{w*.48} {h*.58} L{w*.36} {h*.94} L{w*.86} {h*.38} L{w*.54} {h*.38} Z', f, s)
def ic_shop(w, h, f, s): return rr(w * .14, h * .42, w * .72, h * .49, 8, CREAM, s) + P(f'M{w*.08} {h*.42} L{w*.2} {h*.12} L{w*.8} {h*.12} L{w*.92} {h*.42} Z', RED, s) + rr(w * .22, h * .56, w * .21, h * .35, 4, '#c99260', s) + rr(w * .53, h * .54, w * .24, h * .2, 4, '#9fd6ff', s)

ICONS = dict(check=ic_check, x=ic_x, chest=ic_chest, diamond=ic_diamond, down=ic_down, play=ic_play, pause=ic_pause, heart=ic_heart,
             info=ic_info, lock=ic_lock, person=ic_person, question=ic_question, shield=ic_shield, skull=ic_skull, sound=ic_sound,
             star=ic_star, sword=ic_sword, trophy=ic_trophy, music=ic_music, gear=ic_gear, bolt=ic_bolt, shop=ic_shop)


def icon_tile(name, w=120, h=120, fill=CREAM, bg=ORANGE, lip=ORANGE2):
    """로비 버튼 모양: 주황 둥근 칸 + 크림색 아이콘 (카치건 왼쪽·오른쪽 세로 버튼)"""
    k = .62
    return button(w, h, bg, lip, 22) + f'<g transform="translate({w*(1-k)/2:.1f},{h*(1-k)/2 - 4:.1f})">{ICONS[name](w*k, h*k, fill, 5)}</g>'


# ------------------------------------------------------------------ 판·카드
def window(w, h, title_band=True):
    """크림색 창 + 주황 제목 띠 + 오른쪽 위 X 동그라미"""
    s = rr(4, 4, w - 8, h - 8, 26, CREAM)
    if title_band:
        s += f'<path d="M7 30 Q7 7 30 7 L{w-30} 7 Q{w-7} 7 {w-7} 30 L{w-7} 74 L7 74 Z" fill="{ORANGE}"/>' + L(f'M7 74 L{w-7} 74', 6)
        s += C(w - 40, 40, 22, CREAM, 5) + L(f'M{w-49} 31 L{w-31} 49 M{w-31} 31 L{w-49} 49', 6)
    return s


def leaf(x, y, rot=0, s=1.0):
    """판 모서리 장식 잎 (카치건 강화 카드)"""
    return f'<g transform="translate({x},{y}) rotate({rot}) scale({s})">' + P('M0 0 Q14 -24 38 -18 Q30 4 4 4 Z', '#8fc06c', 5) + '</g>'


def upgrade_card(w=497, h=372, icon_bg='#ff9a6a', icon_svg=''):
    """강화 카드: 크림 판 + 왼쪽 위 아이콘 칸 + 연두 값 칸 + 진녹 가격 띠 + 동전 + 잎 장식"""
    return (leaf(346, 34, -10) + leaf(380, 44, 10)
            + rr(12, 46, w - 49, h - 88, 26, CREAM, 7) + rr(26, h - 168, w - 77, 116, 22, '#bfe3a3', 6)
            + rr(100, h - 136, w - 215, 54, 27, GREEN_D, 6) + coin(132, h - 110, 30, 6)
            + rr(6, 28, 116, 116, 22, icon_bg, 7) + icon_svg + leaf(452, h - 114, 20))


# ------------------------------------------------------------------ 동물 마스코트 견본 (카치건 카피바라의 짜임)
def mascot(fur=FUR, dark=FUR_DARK, belly=FUR_BELLY, ink=CHAR_INK, sw=4.4):
    """옆을 보는 통통한 동물 (로컬 약 170x175). 짜임 = 발 타원 2 → 큰 둥근 몸(머리와 한 덩어리) → 배 타원(테두리 없음)
    → 주둥이 짙은 색(몸 안으로 잘림) → 작은 귀 2 → 점 눈 → Y자 코 + ㅅ 입. 볼터치 없음."""
    body = 'M38 156 C20 144 18 112 26 92 C30 74 32 52 42 36 C52 22 70 14 90 15 C108 16 121 24 127 38 C134 46 137 58 136 66 C135 78 128 84 118 89 C125 108 122 138 106 154 C92 166 54 166 38 156 Z'
    snout = 'M116 43 C108 44.5 104.5 54 105.5 64 C106.5 74 110.5 80 119 80.5 C128 80.5 134 74 135 64 C136 52 128 42 116 43 Z'
    return ''.join([
        E(90, 161, 14, 7.5, dark, sw, ink=ink), E(54, 160, 14, 7.5, dark, sw, ink=ink),
        P(body, fur, sw, ink=ink),
        f'<ellipse cx="72" cy="126" rx="27" ry="29" fill="{belly}"/>',
        f'<clipPath id="sn"><path d="{body}"/></clipPath><g clip-path="url(#sn)">{F(snout, dark)}</g>',
        P('M50 32 Q44 20 54 18 Q60 20 59 28', dark, sw, ink=ink), P('M78 18 Q78 6 88 7 Q93 10 90 17', dark, sw, ink=ink),
        f'<circle cx="83.3" cy="40.2" r="2.7" fill="{ink}"/>',
        L('M117 50.7 L122.8 56 L129.4 50.7 M122.8 56 L122.8 69.1', 2.4, ink) + L('M118.1 73.6 L122.8 69.1 L126.7 73', 2.6, ink),
        # 앞팔 (몸 위에 칠 + 바깥 선만)
        F('M36 88 Q40 104 48 111 Q55 117 63 116 Q75 115 79 109 L79 97 Q76 91 69 90 Q62 88 57 82 Z', fur)
        + L('M36 88 Q40 104 48 111 Q55 117 63 116 Q75 115 79 109', sw, ink) + L('M57 82 Q59 88 65 90 Q69 91 70 94', sw, ink),
    ])


def zombie(skin=ZSKIN, shirt='#c98a5a', pants='#5a6ea6', shoes='#4a3628'):
    """서 있는 적 견본 (400x380 칸, 발바닥 y 376) — 큰 둥근 머리 · 동그란 흰 눈 2 · 앞으로 뻗은 팔 · 짧은 다리"""
    sw = 6
    sk2 = '#79ad59'
    return ''.join([
        limb('M186 300 L178 360', 26, pants), limb('M222 300 L230 360', 26, pants),
        P('M150 352 Q146 376 168 376 L196 376 Q198 362 190 354 Z', shoes), P('M214 354 Q212 376 232 376 L262 376 Q264 362 252 354 Z', shoes),
        limb('M190 230 Q240 236 286 236', 22, sk2),
        P('M160 216 Q156 260 160 306 L248 306 Q252 260 244 216 Q204 200 160 216 Z', shirt), F('M160 306 ' + zig(160, 248, 306, 6, 10) + ' L248 296 L160 296 Z', shirt),
        L('M160 306 ' + zig(160, 248, 306, 6, 10), sw),
        C(204, 158, 56, skin), C(212, 150, 15, '#fffbe8', 4.8), C(242, 148, 13, '#fffbe8', 4.8),
        f'<circle cx="215" cy="152" r="5" fill="{INK}"/><circle cx="245" cy="150" r="4.5" fill="{INK}"/>',
        P('M210 182 Q228 194 246 180 Q228 186 210 182 Z', '#5a2b2b', 4.8),
        limb('M214 240 Q256 246 296 246', 22, skin), C(302, 246, 13, skin),
    ])


def enemy_sheet(cell):
    """적 한 장(PIL RGBA, 400x380, 발바닥 y 376) → 7칸 시트 2800x380: 걷기 0~3(같은 그림, 걸음은 코드/뼈대로) · 4 맞기(6도 젖힘)
    · 5 죽기(뒤로 벌러덩: 90도 돌려 좌우 뒤집기 → 얼굴·팔이 하늘) · 6 흰 실루엣(맞을 때 번쩍)"""
    a = cell
    sheet = Image.new('RGBA', (2800, 380), (0, 0, 0, 0))
    for i in range(4):
        sheet.alpha_composite(a, (i * 400, 0))
    sheet.alpha_composite(a.rotate(6, resample=Image.BICUBIC, center=(200, 376)), (1600, 0))
    ly = a.rotate(90, resample=Image.BICUBIC, expand=True).transpose(Image.FLIP_LEFT_RIGHT)
    arr = np.array(ly); ys, xs = np.nonzero(arr[..., 3] > 0)
    ly = ly.crop((xs.min(), ys.min(), xs.max() + 1, ys.max() + 1))
    if ly.width > 396:
        k = 396 / ly.width; ly = ly.resize((396, int(ly.height * k)), Image.BICUBIC)
    sheet.alpha_composite(ly, (2000 + (400 - ly.width) // 2, 376 - ly.height))
    im = np.array(a); wh = np.zeros_like(im); wh[..., :3] = 255; wh[..., 3] = im[..., 3]
    sheet.alpha_composite(Image.fromarray(wh), (2400, 0))
    return sheet


# ------------------------------------------------------------------ 배경
def field_map(W=1080, H=1200, seed=5):
    """밝은 숲길: 위·아래 풀밭 + 나무(갈색 줄기 + 둥근 잎) + 덤불 줄 + 물결 경계 흙길 + 흙 얼룩 + 돌 + 풀 세 가닥"""
    rnd = random.Random(seed)
    top, bot = int(H * .3), int(H * .78)
    s = [f'<rect width="{W}" height="{H}" fill="{GRASS}"/>']
    for i in range(W // 120 + 2):
        x = i * 120 + rnd.randint(-25, 25); y = top - 140 + rnd.randint(-80, 40); r = rnd.randint(60, 90)
        s.append(f'<rect x="{x - 12}" y="{y}" width="24" height="{r + 40}" fill="{WOOD}" stroke="{INK}" stroke-width="6"/>')
        s.append(C(x, y, r, rnd.choice(LEAVES[:3]), 6))
    for i in range(W // 100 + 2):
        s.append(C(i * 100 + rnd.randint(-15, 15), top + rnd.randint(-10, 10), rnd.randint(55, 80), rnd.choice(LEAVES[2:]), 6))
    s.append(f'<path d="M0 {top+30} Q{W*.2} {top} {W*.4} {top+25} Q{W*.65} {top+50} {W*.85} {top+20} Q{W*.95} {top+10} {W} {top+30} '
             f'L{W} {bot} Q{W*.7} {bot-25} {W*.5} {bot} Q{W*.25} {bot+20} 0 {bot-10} Z" fill="{DIRT}" stroke="{INK}" stroke-width="6"/>')
    for _ in range(6):
        s.append(f'<ellipse cx="{rnd.randint(80, W-80)}" cy="{rnd.randint(top+90, bot-60)}" rx="{rnd.randint(100, 220)}" ry="{rnd.randint(28, 55)}" fill="{DIRT2}"/>')
    for _ in range(14):
        s.append(E(rnd.randint(30, W-30), rnd.randint(top+70, bot-30), rnd.randint(10, 22), rnd.randint(7, 13), ROCK, 4.5))
    for _ in range(12):
        x = rnd.randint(30, W-30); y = rnd.randint(top+70, bot-30)
        s.append(L(f'M{x} {y} l8 -20 M{x+11} {y} l0 -24 M{x+22} {y} l-8 -20', 6, '#7fae5c'))
    for i in range(W // 100 + 2):
        s.append(C(i * 100 + rnd.randint(-15, 15), bot + 40 + rnd.randint(-10, 20), rnd.randint(55, 80), rnd.choice(LEAVES[2:]), 6))
    return ''.join(s)


# ------------------------------------------------------------------ 견본 한 장
def demo(out_dir):
    os.makedirs(out_dir, exist_ok=True)
    W, H = 1080, 1200
    bg = Image.fromarray(render(field_map(W, H), W, H, wob=6.0, freq=0.008)).convert('RGBA')
    # 크게 쓸 그림은 SVG 안에서 키워 그린다 (작게 그려 PNG 를 늘리면 흐려짐)
    hero = Image.fromarray(render(f'<g transform="scale(2)">{mascot()}</g>', 400, 400, wob=1.0))
    bg.alpha_composite(hero, (90, 470))
    zc = Image.fromarray(render(zombie(), 400, 380, wob=4.0, freq=0.018))
    zc = zc.transpose(Image.FLIP_LEFT_RIGHT).resize((300, 285), Image.LANCZOS)
    bg.alpha_composite(zc, (700, 560))
    # UI: 버튼 3 · 아이콘 칸 4 · 강화 카드
    ui = [(button(240, 80, ORANGE, ORANGE2, 20, lipH=8), 240, 80, (40, 40)),
          (button(240, 80, GREEN, GREEN2, 20, lipH=8), 240, 80, (300, 40)),
          (button(240, 80, RED, RED2, 20, lipH=8), 240, 80, (560, 40))]
    for i, n in enumerate(('gear', 'shop', 'trophy', 'chest')):
        ui.append((icon_tile(n), 120, 120, (40 + i * 140, 150)))
    ui.append((upgrade_card(icon_svg=burst(64, 86, 40, 7, GOLD, 5, .5, -90)), 497, 372, (570, 820)))
    ui.append((coin(40, 40, 32), 80, 80, (860, 40)))
    ui.append((ic_diamond(70, 80, SKY, 5), 70, 80, (960, 40)))
    for svg, w, h, pos in ui:
        bg.alpha_composite(Image.fromarray(render(svg, w, h, wob=2.0, freq=0.04)), pos)
    p = os.path.join(out_dir, 'doodle_demo.png')
    bg.convert('RGB').save(p)
    print('→', p)


if __name__ == '__main__':
    if len(sys.argv) >= 2 and sys.argv[1] == 'demo':
        demo(sys.argv[2] if len(sys.argv) > 2 else '.')
    else:
        print(__doc__)
