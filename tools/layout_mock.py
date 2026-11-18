# 전투 배치 시안 (원작과 다른 느낌) — 실제 게임 그림(Resources/Art) + 새로 그린 기지(낙서풍 벡터)를 한 화면 SVG 로 합쳐 1080x2340 PNG 로 찍음.
# PowerShell 에서: $env:PYTHONUTF8='1'; python tools/layout_mock.py [orbit|ark|tower ...]
# 출력: 캡쳐본 모음/전투배치_시안/<이름>.png
import math, os, random, shutil, subprocess, sys, tempfile
from PIL import Image

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
ART = os.path.join(ROOT, 'Earth_Defence', 'Assets', 'Resources', 'Art')
OUT = os.path.join(ROOT, '캡쳐본 모음', '전투배치_시안')
FONT = os.path.join(ROOT, 'tools', 'fonts', 'Jua-Regular.ttf')
EDGE = r'C:\Program Files (x86)\Microsoft\Edge\Application\msedge.exe'
W, H = 1080, 2340
INK = '#3b281c'
CREAM, CREAM2 = '#fff4dc', '#f3e3c3'
ORANGE, ORANGE2 = '#f2ad55', '#d98f3c'
GREEN, GREEN2 = '#8ccb6e', '#6fae55'
RED, RED2 = '#e8735f', '#c95544'
GOLD = '#ffd34d'
SKY = '#8fd6ff'
PURPLE = '#b9a3ea'
NAVY = '#1d2448'


def url(p):
    return 'file:///' + p.replace('\\', '/').replace(' ', '%20')


_size = {}


def size(name):
    if name not in _size:
        _size[name] = Image.open(os.path.join(ART, name + '.png')).size
    return _size[name]


def spr(name, cx, cy, w, rot=0, op=1, flip=False, bottom=False):
    """게임 그림 한 장. cx, cy = 가운데(bottom=True 면 발 밑 가운데)"""
    sw, sh = size(name); h = w * sh / sw
    y0 = cy - h if bottom else cy - h / 2
    ccy = y0 + h / 2
    tr = f'rotate({rot:.1f} {cx:.1f} {ccy:.1f})' if rot else ''
    if flip: tr += f' translate({2 * cx:.1f} 0) scale(-1 1)'
    return f'<image href="{url(os.path.join(ART, name + ".png"))}" x="{cx - w / 2:.1f}" y="{y0:.1f}" width="{w:.1f}" height="{h:.1f}" opacity="{op}" transform="{tr}"/>'


def T(x, y, s, size, fill='#fff', anchor='middle', sw=None, ink=INK, extra=''):
    sw = size * 0.16 if sw is None else sw
    return (f'<text x="{x}" y="{y}" font-family="Jua" font-size="{size}" fill="{fill}" text-anchor="{anchor}" '
            f'stroke="{ink}" stroke-width="{sw:.1f}" stroke-linejoin="round" paint-order="stroke" {extra}>{s}</text>')


def P(d, fill, sw=6, extra=''):
    return f'<path d="{d}" fill="{fill}" stroke="{INK}" stroke-width="{sw}" stroke-linejoin="round" stroke-linecap="round" {extra}/>'


def L(d, sw=6, color=INK, extra=''):
    return f'<path d="{d}" fill="none" stroke="{color}" stroke-width="{sw}" stroke-linejoin="round" stroke-linecap="round" {extra}/>'


def C(cx, cy, r, fill, sw=6, extra=''):
    return f'<circle cx="{cx:.1f}" cy="{cy:.1f}" r="{r:.1f}" fill="{fill}" stroke="{INK}" stroke-width="{sw}" {extra}/>'


def rr(x, y, w, h, r, fill, sw=6, extra=''):
    return f'<rect x="{x:.1f}" y="{y:.1f}" width="{w:.1f}" height="{h:.1f}" rx="{r}" fill="{fill}" stroke="{INK}" stroke-width="{sw}" {extra}/>'


def F(d, fill, op=1):
    return f'<path d="{d}" fill="{fill}" opacity="{op}"/>'


def pol(cx, cy, r, deg):
    """deg = 위에서 시계 방향"""
    a = math.radians(deg)
    return cx + r * math.sin(a), cy - r * math.cos(a)


def arc(cx, cy, r, a0, a1):
    x0, y0 = pol(cx, cy, r, a0); x1, y1 = pol(cx, cy, r, a1)
    large = 1 if (a1 - a0) % 360 > 180 else 0
    return f'M{x0:.1f} {y0:.1f} A{r} {r} 0 {large} 1 {x1:.1f} {y1:.1f}'


def wob(body, k=3, freq=.02, seed=7):
    fid = f'w{random.randint(0, 10**8)}'
    return (f'<filter id="{fid}" filterUnits="userSpaceOnUse" x="-50" y="-50" width="{W + 100}" height="{H + 100}">'
            f'<feTurbulence type="fractalNoise" baseFrequency="{freq}" numOctaves="2" seed="{seed}"/>'
            f'<feDisplacementMap in="SourceGraphic" scale="{k}"/></filter><g filter="url(#{fid})">{body}</g>')


# ------------------------------------------------------------------ 공통 조각
def btn_round(cx, cy, r, inner, fill=CREAM, lip=CREAM2):
    return (C(cx, cy + 7, r, lip, 6) + C(cx, cy, r, fill, 6) + F(f'M{cx - r * .7} {cy - r * .35} A{r} {r} 0 0 1 {cx + r * .7} {cy - r * .35} Z', '#fff', .35) + inner)


def pause_icon(cx, cy, s=1, col=INK):
    return f'<rect x="{cx - 17 * s}" y="{cy - 22 * s}" width="{12 * s}" height="{44 * s}" rx="{5 * s}" fill="{col}"/><rect x="{cx + 5 * s}" y="{cy - 22 * s}" width="{12 * s}" height="{44 * s}" rx="{5 * s}" fill="{col}"/>'


def fast_icon(cx, cy, s=1, col=INK):
    return (f'<path d="M{cx - 24 * s} {cy - 18 * s} L{cx} {cy} L{cx - 24 * s} {cy + 18 * s} Z" fill="{col}" stroke="{col}" stroke-width="5" stroke-linejoin="round"/>'
            f'<path d="M{cx + 2 * s} {cy - 18 * s} L{cx + 26 * s} {cy} L{cx + 2 * s} {cy + 18 * s} Z" fill="{col}" stroke="{col}" stroke-width="5" stroke-linejoin="round"/>')


def heart(cx, cy, s, fill=RED):
    w = h = s
    x, y = cx - w / 2, cy - h / 2
    d = (f'M{x + w * .5} {y + h * .9} C{x + w * .1} {y + h * .62} {x + w * .02} {y + h * .38} {x + w * .18} {y + h * .2} '
         f'C{x + w * .32} {y + h * .06} {x + w * .48} {y + h * .16} {x + w * .5} {y + h * .3} C{x + w * .52} {y + h * .16} {x + w * .68} {y + h * .06} {x + w * .82} {y + h * .2} '
         f'C{x + w * .98} {y + h * .38} {x + w * .9} {y + h * .62} {x + w * .5} {y + h * .9} Z')
    return P(d, fill, 5)


def skull(cx, cy, s):
    x, y, w, h = cx - s / 2, cy - s / 2, s, s
    return (P(f'M{x + w * .5} {y + h * .08} Q{x + w * .92} {y + h * .08} {x + w * .92} {y + h * .46} Q{x + w * .92} {y + h * .66} {x + w * .74} {y + h * .72} L{x + w * .74} {y + h * .9} L{x + w * .26} {y + h * .9} L{x + w * .26} {y + h * .72} Q{x + w * .08} {y + h * .66} {x + w * .08} {y + h * .46} Q{x + w * .08} {y + h * .08} {x + w * .5} {y + h * .08} Z', CREAM, 4)
            + f'<circle cx="{x + w * .34}" cy="{y + h * .46}" r="{w * .1}" fill="{INK}"/><circle cx="{x + w * .66}" cy="{y + h * .46}" r="{w * .1}" fill="{INK}"/>')


def flame_icon(cx, cy, s):
    return C(cx, cy, s, RED, 5) + F(f'M{cx} {cy - s * .62} Q{cx + s * .55} {cy + s * .05} {cx} {cy + s * .6} Q{cx - s * .5} {cy + s * .1} {cx} {cy - s * .62} Z', '#fde39a')


def bar(x, y, w, h, k, fill, bg=NAVY, sw=5):
    r = h / 2
    return rr(x, y, w, h, r, bg, sw) + f'<rect x="{x + sw / 2:.1f}" y="{y + sw / 2:.1f}" width="{max(h - sw, (w - sw) * k):.1f}" height="{h - sw:.1f}" rx="{r - sw / 2:.1f}" fill="{fill}"/>' \
        + f'<rect x="{x + sw / 2 + 6:.1f}" y="{y + sw / 2 + 3:.1f}" width="{max(0, (w - sw) * k - 12):.1f}" height="{(h - sw) * .3:.1f}" rx="3" fill="#fff" opacity=".35"/>'


def lv_badge(cx, cy, r, n):
    return C(cx, cy + 6, r, '#9b84d6', 6) + C(cx, cy, r, PURPLE, 6) + T(cx, cy - r * .1, 'Lv', r * .36, '#fff', sw=r * .1) + T(cx, cy + r * .56, str(n), r * .6, '#fff', sw=r * .12)


def dmg(x, y, s, size=46, fill='#fff', op=1):
    return T(x, y, s, size, fill, sw=size * .2, extra=f'opacity="{op}"')


def beam(x0, y0, x1, y1, col=SKY, w=14):
    d = f'M{x0:.1f} {y0:.1f} L{x1:.1f} {y1:.1f}'
    return L(d, w * 2.4, col, 'opacity=".22"') + L(d, w, col, 'opacity=".9"') + L(d, w * .35, '#fff')


def zap(x0, y0, x1, y1, seed=3, col=GOLD):
    rnd = random.Random(seed); n = 7
    pts = []
    for i in range(n + 1):
        t = i / n; x = x0 + (x1 - x0) * t; y = y0 + (y1 - y0) * t
        if 0 < i < n:
            nx, ny = -(y1 - y0), (x1 - x0); l = math.hypot(nx, ny); off = rnd.uniform(-26, 26)
            x += nx / l * off; y += ny / l * off
        pts.append(f'{x:.1f} {y:.1f}')
    d = 'M' + ' L'.join(pts)
    return L(d, 22, col, 'opacity=".25"') + L(d, 9, col) + L(d, 3, '#fff')


def shot(name, x, y, deg, w):
    """deg = 날아가는 방향(오른쪽 0, 아래 90)"""
    return spr(name, x, y, w, rot=deg)


def boom(x, y, s):
    return spr('Fx/glow', x, y, s * 1.6, op=.7) + spr('Fx/burst', x, y, s)


def stars_bg():
    return spr('Bg/sky', W / 2, 1200, W)


def meteor(name, x, y, w, tx, ty):
    """불꽃 꼬리 운석: (tx,ty) 쪽으로 날아오게 돌림 (그림은 아래로 떨어지는 모양)"""
    rot = math.degrees(math.atan2(ty - y, tx - x)) - 90
    return spr(name, x, y, w, rot=rot)


# ------------------------------------------------------------------ ① 꼬마 지구 360°
def orbit():
    random.seed(11)
    cx, cy, R = 540, 1250, 205
    s = stars_bg()
    s += spr('Bg/moon_rabbit_0', 914, 778, 140)
    # 궤도 고리 · 줄
    ORB = 405
    s += f'<circle cx="{cx}" cy="{cy}" r="{ORB}" fill="none" stroke="{CREAM}" stroke-width="5" stroke-dasharray="4 18" stroke-linecap="round" opacity=".55"/>'
    # 대기(방패) + 체력 고리
    s += f'<circle cx="{cx}" cy="{cy}" r="{R + 46}" fill="{SKY}" opacity=".13"/>'
    v = ''
    v += L(arc(cx, cy, R + 58, 0.01, 359.99), 30, INK)
    v += L(arc(cx, cy, R + 58, 0.01, 359.99), 20, NAVY)
    v += L(arc(cx, cy, R + 58, -20, 238), 20, GREEN)
    v += L(arc(cx, cy, R + 58, -20, 238), 6, '#fff', 'opacity=".35"')
    v += L(arc(cx, cy, R + 58, -20, 30), 8, SKY)
    # 행성
    clip = f'<clipPath id="pl"><circle cx="{cx}" cy="{cy}" r="{R}"/></clipPath>'
    land = ''.join(F(d, '#a9cf86') for d in [
        f'M{cx - 190} {cy - 70} Q{cx - 120} {cy - 150} {cx - 40} {cy - 110} Q{cx + 10} {cy - 60} {cx - 50} {cy - 10} Q{cx - 120} {cy + 30} {cx - 150} {cy + 90} Q{cx - 210} {cy + 40} {cx - 190} {cy - 70} Z',
        f'M{cx + 60} {cy + 20} Q{cx + 150} {cy - 30} {cx + 200} {cy + 40} Q{cx + 190} {cy + 140} {cx + 110} {cy + 160} Q{cx + 40} {cy + 120} {cx + 60} {cy + 20} Z',
        f'M{cx - 60} {cy + 150} Q{cx} {cy + 120} {cx + 30} {cy + 180} Q{cx} {cy + 230} {cx - 70} {cy + 210} Z',
        f'M{cx + 70} {cy - 180} Q{cx + 140} {cy - 170} {cx + 150} {cy - 110} Q{cx + 100} {cy - 90} {cx + 60} {cy - 130} Z'])
    landL = ''.join(L(d, 5) for d in [
        f'M{cx - 190} {cy - 70} Q{cx - 120} {cy - 150} {cx - 40} {cy - 110} Q{cx + 10} {cy - 60} {cx - 50} {cy - 10} Q{cx - 120} {cy + 30} {cx - 150} {cy + 90} Q{cx - 210} {cy + 40} {cx - 190} {cy - 70} Z',
        f'M{cx + 60} {cy + 20} Q{cx + 150} {cy - 30} {cx + 200} {cy + 40} Q{cx + 190} {cy + 140} {cx + 110} {cy + 160} Q{cx + 40} {cy + 120} {cx + 60} {cy + 20} Z',
        f'M{cx - 60} {cy + 150} Q{cx} {cy + 120} {cx + 30} {cy + 180} Q{cx} {cy + 230} {cx - 70} {cy + 210} Z',
        f'M{cx + 70} {cy - 180} Q{cx + 140} {cy - 170} {cx + 150} {cy - 110} Q{cx + 100} {cy - 90} {cx + 60} {cy - 130} Z'])
    v += clip + C(cx, cy, R, '#8cc6e6', 7)
    v += f'<g clip-path="url(#pl)">{land}{landL}'
    v += F(f'M{cx + R} {cy - 40} A{R} {R} 0 0 1 {cx - 60} {cy + R} Q{cx + 150} {cy + 120} {cx + R} {cy - 40} Z', '#3b6d9a', .22)
    v += ''.join(f'<ellipse cx="{x}" cy="{y}" rx="{rx}" ry="{ry}" fill="#fff" opacity=".75"/>' for x, y, rx, ry in [(cx - 90, cy + 60, 46, 14), (cx + 120, cy - 60, 38, 12), (cx + 20, cy + 110, 30, 10)])
    v += '</g>' + f'<circle cx="{cx}" cy="{cy}" r="{R}" fill="none" stroke="{INK}" stroke-width="7"/>'
    # 집 두 채 (행성 위 · 표면에 맞춰 기울임)
    for ang, sc in ((-27, 1), (27, .9)):
        hx, hy = pol(cx, cy, R - 6, ang)
        hw, hh = 78 * sc, 70 * sc
        house = (rr(-hw / 2, -hh, hw, hh, 6, CREAM, 5) + P(f'M{-hw / 2 - 12} {-hh + 4} L0 {-hh - 46 * sc} L{hw / 2 + 12} {-hh + 4} Z', ORANGE, 5)
                 + rr(-hw * .32, -hh * .7, hw * .26, hh * .3, 4, GOLD, 4) + rr(hw * .06, -hh * .7, hw * .26, hh * .3, 4, GOLD, 4))
        v += f'<g transform="translate({hx:.1f} {hy:.1f}) rotate({ang})">{house}</g>'
    # 궤도 터렛 받침 (떠 있는 원판 + 행성과 잇는 빛줄)
    slots = [(45, 'Turret/laser'), (135, 'Turret/missile'), (225, 'Turret/hive'), (315, 'Turret/bolt')]
    pads = {}
    for ang, t in slots:
        px, py = pol(cx, cy, ORB, ang); pads[ang] = (px, py)
        ix, iy = pol(cx, cy, R + 76, ang)
        s += L(f'M{ix:.1f} {iy:.1f} L{px:.1f} {py:.1f}', 4, CREAM, 'stroke-dasharray="2 14" opacity=".45"')
    s += wob(v, 3, .02)
    pad = ''
    for ang, t in slots:
        px, py = pads[ang]
        if t is None:
            pad += f'<ellipse cx="{px:.1f}" cy="{py + 40:.1f}" rx="70" ry="24" fill="none" stroke="{CREAM}" stroke-width="5" stroke-dasharray="10 9" opacity=".7"/>'
            pad += T(px, py + 54, '+', 44, CREAM, sw=0, extra='opacity=".75"')
            continue
        pad += f'<ellipse cx="{px:.1f}" cy="{py + 68:.1f}" rx="44" ry="14" fill="{SKY}" opacity=".35"/>'
        pad += f'<ellipse cx="{px:.1f}" cy="{py + 50:.1f}" rx="78" ry="26" fill="{CREAM2}" stroke="{INK}" stroke-width="5"/><ellipse cx="{px:.1f}" cy="{py + 42:.1f}" rx="78" ry="24" fill="{CREAM}" stroke="{INK}" stroke-width="5"/>'
        pad += f'<ellipse cx="{px - 40:.1f}" cy="{py + 40:.1f}" rx="7" ry="4" fill="{SKY}"/><ellipse cx="{px + 40:.1f}" cy="{py + 40:.1f}" rx="7" ry="4" fill="{SKY}"/>'
    s += wob(pad, 2, .03)
    for ang, t in slots:
        if t: px, py = pads[ang]; s += spr(t, px, py + 48, 150, bottom=True)
    # 대장 카피 (북극)
    s += spr('Char/guardian', cx, cy - R + 4, 150, bottom=True)
    # 적: 사방에서
    en = [('Enemy/fire', 130, 420, 110), ('Enemy/ice', 330, 360, 100), ('Enemy/iron', 560, 430, 104), ('Enemy/crack', 760, 400, 106),
          ('Enemy/pebble', 470, 560, 70), ('Enemy/fire', 1010, 930, 100), ('Enemy/ice', 70, 850, 96), ('Enemy/iron', 1010, 1140, 100),
          ('Enemy/crack', 50, 1500, 104), ('Enemy/fire', 1000, 1760, 100), ('Enemy/pebble', 900, 2010, 74), ('Enemy/ice', 240, 2030, 98),
          ('Enemy/fire', 560, 2110, 104), ('Enemy/crack', 760, 2150, 92), ('Enemy/pebble', 120, 1880, 70)]
    for n, x, y, w in en: s += meteor(n, x, y, w, cx, cy)
    for n, x, y, w in [('Enemy/moth', 250, 650, 130), ('Enemy/ufo', 880, 610, 140), ('Enemy/beetle', 960, 1430, 120), ('Enemy/jelly', 90, 990, 116),
                       ('Enemy/sdrone', 400, 1980, 130), ('Enemy/spider', 960, 2180 - 40, 120)]:
        s += spr(n, x, y, w)
    # 엘리트 (나방 왕) + 작은 체력 막대
    s += spr('Enemy/moth_king', 170, 1720, 170) + bar(95, 1615, 150, 22, .58, RED, sw=4)
    # 공격
    lx, ly = pads[45]; s += beam(lx + 2, ly - 110, 880, 640, SKY, 12)
    bx, by = pads[315]; s += zap(bx, by - 120, 262, 676, 5)
    s += shot('Fx/missile', 935, 1290, -70, 120) + shot('Fx/mini_missile', 950, 1720, 40, 90)
    for x, y in [(300, 1650), (275, 1712), (330, 1730)]: s += spr('Fx/bee', x, y, 48)
    s += shot('Fx/bullet', 560, 760, -82, 110) + shot('Fx/bullet', 572, 610, -84, 110)
    s += boom(560, 438, 150) + boom(880, 612, 120) + spr('Fx/hit', 250, 650, 90) + spr('Fx/hit', 960, 1430, 80)
    # 피해 숫자 (정리된 모양)
    s += dmg(560, 385, '1.2K', 60, '#ffb36b') + dmg(880, 548, '486', 44) + dmg(250, 580, '312', 42, '#ffd34d') + T(250, 535, '약점', 26, '#fde39a', sw=5) + dmg(960, 1370, '205', 38)
    s += dmg(1000, 1080, '97', 36, '#fff', .7) + dmg(420, 1920, '84', 34, '#fff', .6)
    # 체력 숫자 꼬리표 (행성 아래)
    tag = rr(cx - 110, cy + R + 92, 220, 64, 32, NAVY, 5) + heart(cx - 62, cy + R + 124, 40) + T(cx + 22, cy + R + 140, '2,140', 40, '#fff')
    tag += P(f'M{cx + 112} {cy + R + 92} l26 9 v18 q0 20 -26 30 q-26 -10 -26 -30 v-18 z', SKY, 4) + T(cx + 112, cy + R + 128, '300', 18, NAVY, sw=0)
    s += wob(tag, 1.5, .04)
    # 위 HUD: 둥근 단추 · 하늘색 리본 · 시계 판
    hud = btn_round(92, 112, 56, pause_icon(92, 112, 1.05)) + btn_round(988, 112, 56, '')
    rib = (P('M268 70 L218 92 L248 124 L218 156 L280 156 Z', '#79b4d8', 6) + P('M812 70 L862 92 L832 124 L862 156 L800 156 Z', '#79b4d8', 6)
           + P('M280 54 L800 54 L800 150 Q540 172 280 150 Z', '#9fd0ea', 6) + F('M292 64 L788 64 L788 84 L292 84 Z', '#fff', .3))
    hud += rib + T(540, 128, '스테이지 12', 58, '#fff')
    hud += rr(350, 176, 380, 64, 32, CREAM, 6) + flame_icon(392, 208, 22) + T(474, 224, '03:46', 36, INK, sw=0)
    hud += bar(556, 196, 130, 24, .62, ORANGE, '#e9dcc4', 4) + skull(706, 208, 34)
    s += wob(hud, 2, .04) + spr('Icon/cards', 988, 108, 70)
    # 아래: 레벨 배지 + 경험치 + 배속
    low = bar(170, 2200, 700, 50, .64, SKY, NAVY, 6) + lv_badge(118, 2222, 64, 12) + btn_round(978, 2222, 52, fast_icon(980, 2222, .95))
    s += wob(low, 2, .04)
    # 화면 밖 보스 화살표 (왼쪽 아래)
    s += wob(P('M1050 1610 L992 1572 L992 1648 Z', RED, 5) + C(952, 1610, 34, RED, 5) + skull(952, 1608, 40), 1.5, .04)
    return s


# ------------------------------------------------------------------ ② 카피 방주
def ark():
    random.seed(21)
    s = stars_bg()
    # 앞으로 날아가는 느낌: 세로 빛줄
    streak = ''
    for i in range(70):
        x = random.uniform(10, W - 10); y = random.uniform(250, 1900); l = random.uniform(60, 240)
        streak += f'<line x1="{x:.0f}" y1="{y:.0f}" x2="{x:.0f}" y2="{y + l:.0f}" stroke="#fff" stroke-width="{random.choice([2, 3, 4])}" stroke-linecap="round" opacity="{random.uniform(.1, .35):.2f}"/>'
    s += streak
    s += spr('Enemy/big', 120, 1250, 180, op=.35) + spr('Enemy/big', 980, 980, 120, op=.25)
    # ---- 배 ----
    v = ''
    # 엔진 불꽃
    for ex in (360, 720):
        v += F(f'M{ex - 52} 2230 Q{ex} 2420 {ex + 52} 2230 Z', '#ffb36b', .9) + F(f'M{ex - 28} 2230 Q{ex} 2350 {ex + 28} 2230 Z', '#fde39a')
    v += rr(310, 2180, 100, 70, 14, '#b9c3cf', 6) + rr(670, 2180, 100, 70, 14, '#b9c3cf', 6)
    # 날개 (양옆 프로펠러 받침)
    v += P('M80 1990 Q20 2030 40 2100 L190 2080 L200 1990 Z', '#c99260', 6) + P('M1000 1990 Q1060 2030 1040 2100 L890 2080 L880 1990 Z', '#c99260', 6)
    v += f'<ellipse cx="44" cy="1960" rx="64" ry="12" fill="#e7ecf2" stroke="{INK}" stroke-width="5"/><ellipse cx="1036" cy="1960" rx="64" ry="12" fill="#e7ecf2" stroke="{INK}" stroke-width="5"/>'
    v += L('M44 1960 L60 2000 M1036 1960 L1020 2000', 6)
    # 선체
    v += P('M70 1880 L1010 1880 Q1000 2010 900 2120 Q780 2240 540 2240 Q300 2240 180 2120 Q80 2010 70 1880 Z', '#c99260', 7)
    for y in (1930, 1985, 2040, 2095, 2150):
        x0 = 80 + (y - 1880) * .55 if y > 2000 else 76 + (y - 1880) * .2
        v += L(f'M{x0:.0f} {y} Q540 {y + 18} {W - x0:.0f} {y}', 4, '#a97a4c')
    v += F('M120 2050 Q540 2120 960 2050 Q900 2150 760 2210 Q540 2250 320 2210 Q180 2150 120 2050 Z', '#a97a4c', .45)
    for px in (190, 300, 780, 890):
        v += C(px, 1975, 30, SKY, 6) + F(f'M{px - 14} {1960} A20 20 0 0 1 {px + 10} {1958} L{px} 1972 Z', '#fff', .6)
    # 갑판 앞 띠 + 난간
    v += rr(56, 1852, 968, 44, 14, CREAM, 7) + F('M70 1860 L1010 1860 L1010 1870 L70 1870 Z', '#fff', .4)
    # 선실 (2층)
    v += rr(300, 1660, 480, 200, 22, ORANGE, 7) + F('M308 1800 L772 1800 L772 1852 L308 1852 Z', ORANGE2, .6)
    for wx in (350, 470, 610):
        v += rr(wx, 1700, 100, 70, 14, GOLD, 5) + L(f'M{wx + 50} 1702 L{wx + 50} 1768', 4)
    v += rr(286, 1628, 508, 48, 16, CREAM, 7)
    # 깃발 · 줄 전구
    v += L('M300 1640 L300 1500', 7) + P('M300 1505 L390 1530 L300 1560 Z', RED, 5)
    v += L('M780 1640 L780 1520', 7) + C(780, 1514, 10, GOLD, 4)
    v += L('M300 1530 Q540 1600 780 1520', 3, CREAM, 'opacity=".8"')
    for t in (.15, .3, .45, .6, .75, .9):
        x = 300 + 480 * t; y = 1530 + math.sin(math.pi * t) * 52 - 8 * t
        v += C(x, y + 6, 9, random.choice([GOLD, SKY, '#f6a2b6', GREEN]), 3)
    # 선체 이름판 = 기지 체력
    v += rr(395, 1938, 290, 96, 20, CREAM, 6) + heart(440, 1972, 40) + bar(470, 1990, 196, 30, .72, GREEN, '#e9dcc4', 4)
    s += wob(v, 3, .018)
    s += T(568, 1980, '2,140', 36, INK, sw=0)
    # 터렛 (아래층 2 · 위층 2) + 대장 카피
    s += spr('Turret/cannon', 150, 1870, 150, bottom=True) + spr('Turret/rail', 930, 1870, 150, bottom=True)
    s += spr('Turret/laser', 380, 1638, 140, bottom=True) + spr('Turret/tesla', 700, 1638, 140, bottom=True)
    s += spr('Char/guardian', 540, 1640, 150, bottom=True)
    # 적 (위에서)
    for n, x, y, w in [('Enemy/fire', 160, 470, 108), ('Enemy/ice', 420, 420, 100), ('Enemy/iron', 700, 380, 104), ('Enemy/crack', 930, 470, 104),
                       ('Enemy/fire', 300, 720, 104), ('Enemy/pebble', 560, 640, 72), ('Enemy/ice', 820, 760, 100), ('Enemy/crack', 120, 980, 100),
                       ('Enemy/iron', 480, 1010, 100), ('Enemy/fire', 760, 1110, 100), ('Enemy/pebble', 960, 1300, 70), ('Enemy/fire', 280, 1330, 96)]:
        s += spr(n, x, y, w)
    for n, x, y, w in [('Enemy/ufo', 600, 860, 150), ('Enemy/moth', 900, 620, 130), ('Enemy/jelly', 160, 1160, 120), ('Enemy/beetle', 640, 1340, 118)]:
        s += spr(n, x, y, w)
    # 큰 적 (엘리트 어미 비행선) + 막대
    s += spr('Enemy/mother', 300, 560, 230) + bar(210, 470, 180, 24, .4, RED, sw=4)
    # 공격
    s += beam(380, 1520, 470, 1050, SKY, 12)
    s += zap(700, 1520, 760, 1150, 9)
    s += shot('Fx/cannonball', 190, 1500, -80, 54) + shot('Fx/cannonball', 230, 1290, -80, 48)
    s += beam(930, 1700, 955, 1330, '#f2c48d', 9) + spr('Fx/hit', 960, 1300, 80)
    s += shot('Fx/bullet', 545, 1380, -90, 110) + shot('Fx/bullet', 548, 1190, -90, 110)
    s += boom(480, 1010, 140) + boom(760, 1120, 110) + spr('Fx/hit', 640, 1340, 90)
    s += dmg(480, 950, '1.4K', 60, '#ffb36b') + dmg(770, 1060, '520', 44, '#ffd34d') + T(770, 1018, '약점', 26, '#fde39a', sw=5) + dmg(640, 1280, '210', 40) + dmg(300, 1280, '96', 34, '#fff', .65)
    # 위 HUD: 항로 막대 (시간 대신 "목적지까지")
    hud = rr(28, 40, 104, 104, 26, CREAM2, 6) + rr(28, 34, 104, 104, 26, CREAM, 6) + pause_icon(80, 86)
    hud += rr(948, 40, 104, 104, 26, CREAM2, 6) + rr(948, 34, 104, 104, 26, CREAM, 6)
    hud += rr(150, 30, 780, 160, 30, '#2a3262', 6, 'opacity=".92"')
    hud += L('M205 140 L865 140', 8, '#56609a') + L('M205 140 L615 140', 8, ORANGE)
    for x in (325, 475, 625, 775): hud += C(x, 140, 10, CREAM if x < 615 else '#56609a', 4)
    hud += P('M598 112 L640 140 L598 168 L606 140 Z', CREAM, 5)
    hud += C(880, 140, 28, '#f6a2b6', 5) + L('M845 150 Q880 128 915 142', 5) + skull(880, 96, 30)
    s += wob(hud, 2, .04) + spr('Icon/cards', 1000, 84, 66)
    s += T(205, 98, '스테이지 12', 44, '#fff', 'start') + T(855, 98, '03:46', 34, '#ffd34d', 'end', sw=0)
    low = bar(150, 214, 780, 40, .58, SKY, NAVY, 6) + lv_badge(96, 232, 54, 12)
    low += btn_round(988, 236, 44, fast_icon(990, 236, .8))
    s += wob(low, 2, .04)
    return s


# ------------------------------------------------------------------ ③ 카피 등대 (옆에서 오는 전투)
def tower():
    random.seed(31)
    s = stars_bg()
    s += spr('Bg/moon_rabbit_0', 914, 778, 140)
    top, bot, SEA = 640, 2070, 2165
    floors = [1770, 1420, 1070]
    v = ''
    # 바다
    v += P(f'M-10 {SEA} Q140 {SEA - 26} 300 {SEA} T620 {SEA} T940 {SEA} T1260 {SEA} L1090 2360 L-10 2360 Z', '#7fb6d6', 6)
    for y in (SEA + 60, SEA + 130):
        for x0 in range(-40, 1100, 220):
            v += L(f'M{x0 + (y % 3) * 40} {y} q30 -18 60 0 t60 0', 5, '#cfe8f5', 'opacity=".8"')
    # 벼랑
    v += P(f'M-20 {bot} Q120 {bot - 40} 400 {bot - 6} Q450 {bot + 80} 420 {bot + 170} Q380 2300 300 2360 L-20 2360 Z', '#c9c1b3', 7)
    v += F(f'M250 {bot + 60} Q330 {bot + 110} 310 {bot + 200} Q280 2300 220 2350 L160 2350 Q230 2250 250 {bot + 60} Z', '#a89f90', .6)
    v += P(f'M-20 {bot} Q120 {bot - 50} 400 {bot - 6} Q410 {bot + 14} 390 {bot + 28} Q120 {bot + 8} -20 {bot + 46} Z', '#a9cf86', 6)
    # 등대 몸통 (위로 가늘어짐) + 줄무늬
    xl0, xr0, xl1, xr1 = 90, 330, 118, 302
    def xl(y): return xl0 + (xl1 - xl0) * (bot - y) / (bot - top)
    def xr(y): return xr0 + (xr1 - xr0) * (bot - y) / (bot - top)
    v += P(f'M{xl0} {bot} L{xl1} {top} L{xr1} {top} L{xr0} {bot} Z', CREAM, 7)
    for y0 in (1880, 1530, 1180, 830):
        y1 = y0 + 110
        v += F(f'M{xl(y0):.0f} {y0} L{xr(y0):.0f} {y0} L{xr(y1):.0f} {y1} L{xl(y1):.0f} {y1} Z', '#f0a68f')
    v += P(f'M{xl0} {bot} L{xl1} {top} L{xr1} {top} L{xr0} {bot} Z', 'none', 7)
    v += F(f'M{xl0 + 10} {bot - 4} L{xl1 + 8} {top + 4} L{xl1 + 40} {top + 4} L{xl0 + 50} {bot - 4} Z', '#fff', .25)
    v += rr(180, bot - 100, 62, 98, 28, '#9a6a44', 5)
    for y in (1640, 1290, 940): v += C(210, y, 24, GOLD, 5)
    # 층마다 발코니 (터렛 자리)
    for y in floors:
        v += P(f'M260 {y} L475 {y} L475 {y + 26} L260 {y + 26} Z', '#c99260', 6)
        v += P(f'M300 {y + 26} L330 {y + 72} L346 {y + 26} Z', '#a97a4c', 5)
        v += L(f'M262 {y - 54} L473 {y - 54}', 6) + ''.join(L(f'M{x} {y - 54} L{x} {y}', 5) for x in (300, 350, 425, 471))
    # 꼭대기: 등불 방(왼쪽) + 둘레 난간(대장 카피 자리)
    v += P(f'M70 {top} L380 {top} L380 {top + 28} L70 {top + 28} Z', '#c99260', 6)
    v += L(f'M72 {top - 50} L378 {top - 50}', 6) + ''.join(L(f'M{x} {top - 50} L{x} {top}', 5) for x in (72, 120, 330, 378))
    v += rr(110, top - 140, 130, 140, 18, '#cfe8f5', 6) + C(175, top - 72, 32, GOLD, 5)
    v += P(f'M92 {top - 132} Q175 {top - 232} 258 {top - 132} Z', RED, 6) + C(175, top - 214, 12, GOLD, 4)
    # 바닥 터렛 자리 (벼랑 위)
    v += f'<ellipse cx="455" cy="{bot - 2}" rx="74" ry="20" fill="{CREAM2}" stroke="{INK}" stroke-width="5"/>'
    # 탑 체력 = 왼쪽 세로 막대
    hy0, hh = 760, 1080
    v += rr(22, hy0, 50, hh, 25, NAVY, 6) + f'<rect x="27" y="{hy0 + hh * .28:.0f}" width="40" height="{hh * .72 - 6:.0f}" rx="20" fill="{GREEN}"/>'
    v += f'<rect x="33" y="{hy0 + hh * .28 + 10:.0f}" width="10" height="{hh * .72 - 30:.0f}" rx="5" fill="#fff" opacity=".35"/>'
    v += f'<rect x="27" y="{hy0 + hh * .28:.0f}" width="40" height="110" rx="20" fill="{SKY}"/>'
    s += spr('Fx/glow', 175, top - 72, 330, op=.55)
    s += wob(v, 3, .018)
    s += heart(47, hy0 - 40, 56) + rr(6, hy0 + hh + 18, 132, 50, 25, NAVY, 5) + T(72, hy0 + hh + 55, '2,140', 32, '#fff', sw=0)
    # 터렛 · 대장 카피
    s += spr('Char/guardian', 300, top + 2, 150, bottom=True)
    s += spr('Turret/missile', 405, floors[2], 140, bottom=True) + spr('Turret/focus', 405, floors[1], 140, bottom=True)
    s += spr('Turret/hive', 405, floors[0], 140, bottom=True) + spr('Turret/wheel', 455, bot, 140, bottom=True)
    # 적: 오른쪽에서 왼쪽으로 (운석은 꼬리가 오른쪽)
    for n, x, y, w in [('Enemy/fire', 760, 700, 104), ('Enemy/ice', 990, 880, 100), ('Enemy/iron', 700, 1140, 104), ('Enemy/crack', 930, 1240, 100),
                       ('Enemy/fire', 1010, 1530, 100), ('Enemy/pebble', 820, 1580, 70), ('Enemy/ice', 660, 1880, 96), ('Enemy/fire', 960, 1960, 100),
                       ('Enemy/crack', 1010, 600, 96), ('Enemy/pebble', 620, 960, 66), ('Enemy/iron', 800, 2060, 96)]:
        s += meteor(n, x, y, w, -500, y)
    for n, x, y, w in [('Enemy/moth', 840, 1010, 130), ('Enemy/ufo', 1000, 1350, 140), ('Enemy/jelly', 760, 1720, 120), ('Enemy/beetle', 1010, 2090, 116),
                       ('Enemy/sdrone', 640, 520, 120)]:
        s += spr(n, x, y, w)
    s += spr('Enemy/planet', 1040, 1770, 300) + bar(900, 1600, 170, 24, .74, RED, sw=4)
    # 공격 (오른쪽으로)
    s += shot('Fx/missile', 570, 1005, 0, 120) + shot('Fx/mini_missile', 700, 1060, -8, 90)
    s += beam(475, 1335, 690, 1142, SKY, 14)
    for x, y in ((540, 1700), (610, 1730), (690, 1690)): s += spr('Fx/bee', x, y, 46)
    s += shot('Fx/bullet', 440, 560, -8, 110) + shot('Fx/bullet', 570, 542, -8, 110)
    s += beam(510, 2000, 650, 1890, '#ffb36b', 10)
    s += boom(700, 1140, 140) + boom(640, 520, 110) + spr('Fx/hit', 760, 1720, 90)
    s += dmg(700, 1070, '1.1K', 58, '#ffb36b') + dmg(640, 460, '380', 42) + dmg(770, 1660, '264', 40, '#ffd34d') + T(770, 1618, '약점', 26, '#fde39a', sw=5)
    s += dmg(930, 1180, '88', 34, '#fff', .65)
    # 위 HUD: 크림 판 하나에 모음
    hud = rr(24, 36, 1032, 178, 40, CREAM2, 6) + rr(24, 28, 1032, 178, 40, CREAM, 6)
    hud += btn_round(104, 117, 52, pause_icon(104, 117), ORANGE, ORANGE2) + btn_round(976, 117, 52, '', ORANGE, ORANGE2)
    hud += flame_icon(214, 92, 26)
    hud += bar(190, 150, 700, 36, .64, SKY, '#e9dcc4', 5) + lv_badge(196, 168, 36, 12)
    s += wob(hud, 2, .04) + spr('Icon/cards', 976, 112, 64)
    s += T(260, 108, '스테이지 12', 48, INK, 'start', sw=0) + T(880, 108, '03:46', 40, '#c4741f', 'end', sw=0)
    s += wob(btn_round(978, 2262, 50, fast_icon(980, 2262, .9)), 2, .04)
    return s


# ------------------------------------------------------------------ 2판: ③ 우주 사령탑 (사용자 의견 11-18)
# 대장 카피 가운데 · 위 2칸 아래 2칸 · 터렛 45° · 운석은 오른쪽 위 → 왼쪽 아래 · 위 줄은 ② 방주의 항로 막대
STEEL, STEEL2 = '#dfe4ee', '#b4bdcf'


def hud_route(stage='스테이지 12', clock='03:46', k=.58):
    """② 방주의 위 줄: 일시정지 · 항로 막대(웨이브 점 → 배 위치 → 보스 행성) · 카드 / 레벨 배지 · 경험치 · 배속"""
    hud = rr(28, 40, 104, 104, 26, CREAM2, 6) + rr(28, 34, 104, 104, 26, CREAM, 6) + pause_icon(80, 86)
    hud += rr(948, 40, 104, 104, 26, CREAM2, 6) + rr(948, 34, 104, 104, 26, CREAM, 6)
    hud += rr(150, 30, 780, 160, 30, '#2a3262', 6, 'opacity=".92"')
    hud += L('M205 140 L865 140', 8, '#56609a') + L('M205 140 L615 140', 8, ORANGE)
    for x in (325, 475, 625, 775): hud += C(x, 140, 10, CREAM if x < 615 else '#56609a', 4)
    hud += P('M598 112 L640 140 L598 168 L606 140 Z', CREAM, 5)
    hud += C(880, 140, 28, '#f6a2b6', 5) + L('M845 150 Q880 128 915 142', 5) + skull(880, 96, 30)
    s = wob(hud, 2, .04) + spr('Icon/cards', 1000, 84, 66)
    s += T(205, 98, stage, 44, '#fff', 'start') + T(855, 98, clock, 34, '#ffd34d', 'end', sw=0)
    low = bar(150, 214, 780, 40, k, SKY, NAVY, 6) + lv_badge(96, 232, 54, 12)
    low += btn_round(988, 236, 44, fast_icon(990, 236, .8))
    return s + wob(low, 2, .04)


def tilted(name, mx, my, w=140, deg=45):
    """터렛을 통째로 기울인 받침 위에: (mx,my) = 받침 가운데. 끝(쏘는 곳) 좌표도 돌려줌"""
    g = (f'<ellipse cx="0" cy="10" rx="{w * .55:.0f}" ry="{w * .16:.0f}" fill="{CREAM2}" stroke="{INK}" stroke-width="5"/>'
         f'<ellipse cx="0" cy="2" rx="{w * .55:.0f}" ry="{w * .15:.0f}" fill="{CREAM}" stroke="{INK}" stroke-width="5"/>'
         + spr(name, 0, 8, w, bottom=True))
    sw, sh = size(name); d = w * sh / sw * .88
    a = math.radians(deg)
    return f'<g transform="translate({mx:.1f} {my:.1f}) rotate({deg})">{g}</g>', (mx + d * math.sin(a), my - d * math.cos(a))


HEADS = {'cannon': ['cannon_barrel'], 'missile': ['missile_l', 'missile_r'], 'laser': ['laser_crystal']}


def turret_b(t, x, by, w=150, deg=45, pad=True):
    """B 방식: 받침은 그대로, 쏘는 머리 조각만 deg 만큼 (머리 조각이 없는 터렛은 그대로). (svg, 끝 좌표)"""
    sw_, sh_ = size(f'Turret/{t}'); sc = w / sw_; h = sh_ * sc; x0, y0 = x - w / 2, by - h
    s = ''
    if pad:
        s += f'<ellipse cx="{x}" cy="{by - 2}" rx="{w * .55:.0f}" ry="{w * .15:.0f}" fill="{CREAM}" stroke="{INK}" stroke-width="5"/>'
    heads = HEADS.get(t)
    if not heads:
        return s + spr(f'Turret/{t}', x, by, w, bottom=True), (x, y0 + h * .12)
    s += spr(f'Turret/{t}_base', x, by, w, bottom=True)
    a = math.radians(deg); tip = None
    for hn in heads:
        im = Image.open(os.path.join(ART, 'Turret', hn + '.png')).getchannel('A')
        bx0, by0, bx1, by1 = im.point(lambda v: 255 if v > 20 else 0).getbbox()
        px, py = x0 + (bx0 + bx1) / 2 * sc, y0 + (by1 - 12) * sc
        s += f'<g transform="rotate({deg} {px:.1f} {py:.1f})">' + spr(f'Turret/{hn}', x, by, w, bottom=True) + '</g>'
        ln = (by1 - 12 - by0) * sc
        tip = (px + ln * math.sin(a), py - ln * math.cos(a))
    return s, tip


def command(K=.75):
    """우주 사령탑. K = 탑 크기 배율(11-18 사용자 "절반으로" → .5 → "1.5배만 키워줘 · 벽에 붙여" → .75). 선 굵기 · 터렛 · 대장 카피 크기는 그대로"""
    random.seed(41)
    s = stars_bg()
    # 비스듬히 떨어지는 빛줄 (오른쪽 위 → 왼쪽 아래)
    for i in range(46):
        x = random.uniform(380, W + 200); y = random.uniform(280, 1900); l = random.uniform(70, 220)
        s += (f'<line x1="{x:.0f}" y1="{y:.0f}" x2="{x - l * .62:.0f}" y2="{y + l * .78:.0f}" stroke="#fff" '
              f'stroke-width="{random.choice([2, 3])}" stroke-linecap="round" opacity="{random.uniform(.08, .26):.2f}"/>')
    G = 2140
    # 지평선 위로 떠오르는 지구 (지키는 것)
    ex, ey, er = 860, 2205, 160
    e = C(ex, ey, er, '#8cc6e6', 7)
    for d in (f'M{ex - 130} {ey - 60} Q{ex - 70} {ey - 140} {ex + 10} {ey - 110} Q{ex + 40} {ey - 60} {ex - 20} {ey - 30} Q{ex - 90} {ey} {ex - 120} {ey + 20} Q{ex - 160} {ey - 10} {ex - 130} {ey - 60} Z',
              f'M{ex + 50} {ey - 120} Q{ex + 120} {ey - 120} {ex + 140} {ey - 60} Q{ex + 90} {ey - 40} {ex + 50} {ey - 80} Z'):
        e += F(d, '#a9cf86') + L(d, 5)
    e += f'<ellipse cx="{ex - 40}" cy="{ey - 70}" rx="40" ry="12" fill="#fff" opacity=".7"/>'
    s += spr('Fx/glow', ex, ey - 20, 520, op=.35) + wob(e, 2.5, .02)
    # 달 바닥
    gnd = P(f'M-20 {G} Q200 {G - 30} 420 {G - 4} T860 {G + 6} T1100 {G - 10} L1100 2360 L-20 2360 Z', '#cfc8dc', 7)
    for cx_, cy_, rx, ry in ((560, 2230, 74, 20), (780, 2305, 50, 14), (990, 2250, 62, 18), (420, 2320, 40, 12)):
        gnd += f'<ellipse cx="{cx_}" cy="{cy_}" rx="{rx}" ry="{ry}" fill="#b3abc6" stroke="{INK}" stroke-width="4"/>'
        gnd += f'<ellipse cx="{cx_}" cy="{cy_ - ry * .35:.0f}" rx="{rx * .8:.0f}" ry="{ry * .45:.0f}" fill="#a39bb8"/>'
    s += wob(gnd, 2.5, .02)
    # ---- 사령탑 (BX = 몸통 가운데) ----
    BX = 14 + 88 * K                              # 왼쪽 벽에 바짝 (몸통 왼쪽 끝 = 14)
    top = G - 1580 * K
    hb, ht = 88 * K, 62 * K                       # 아래 · 위 반폭
    xl0, xr0, xl1, xr1 = BX - hb, BX + hb, BX - ht, BX + ht
    def xl(y): return xl0 + (xl1 - xl0) * (G - y) / (G - top)
    def xr(y): return xr0 + (xr1 - xr0) * (G - y) / (G - top)
    v = ''
    # 안테나 · 레이더 (오른쪽 위를 봄)
    ah = 230 * K
    v += L(f'M{BX} {top - 40 * K} L{BX} {top - ah}', 7) + L(f'M{BX} {top - ah * .65:.0f} L{BX + 30 * K + 10:.0f} {top - ah:.0f}', 4)
    dx, dy, dr = BX + 50 * K + 6, top - 115 * K, 60 * K + 8
    v += L(f'M{BX + 12} {top - 40 * K} L{dx - 4:.0f} {dy + 12:.0f}', 6)
    v += f'<g transform="rotate(45 {dx:.1f} {dy:.1f})">' + P(f'M{dx - dr:.1f} {dy:.1f} Q{dx:.1f} {dy + dr * .8:.1f} {dx + dr:.1f} {dy:.1f} Z', STEEL, 5) + L(f'M{dx:.1f} {dy + 12:.1f} L{dx:.1f} {dy - dr * .7:.1f}', 5) + C(dx, dy - dr * .7 - 6, 7, ORANGE, 3) + '</g>'
    # 꼭대기 유리 지붕
    v += P(f'M{xl1 - 10:.1f} {top - 8:.1f} Q{BX} {top - 70 * K - 30:.1f} {xr1 + 10:.1f} {top - 8:.1f} Z', '#9fd8f5', 5)
    # 몸통 · 빛 · 그늘 · 판 이음줄 · 창
    v += P(f'M{xl0:.1f} {G} L{xl1:.1f} {top:.1f} L{xr1:.1f} {top:.1f} L{xr0:.1f} {G} Z', STEEL, 6)
    v += F(f'M{xr0 - 22:.1f} {G - 4} L{xr1 - 16:.1f} {top + 6:.1f} L{xr1 - 3:.1f} {top + 6:.1f} L{xr0 - 4:.1f} {G - 4} Z', STEEL2, .6)
    v += F(f'M{xl0 + 7:.1f} {G - 4} L{xl1 + 6:.1f} {top + 6:.1f} L{xl1 + 18:.1f} {top + 6:.1f} L{xl0 + 21:.1f} {G - 4} Z', '#fff', .45)
    v += rr(xl1 - 8, top - 12, xr1 - xl1 + 16, 24, 10, ORANGE, 5)
    # 가운데 사령실 (대장 카피 자리)
    DY = G - 790 * K * 1.0 + 0          # 몸통 가운데쯤
    DY = top + (G - top) * .5 + 20
    dl, drr, dh = BX - 72, BX + 150, 64
    v += P(f'M{dl} {DY} L{drr} {DY} Q{drr + 22} {DY + dh * .45:.1f} {drr - 10} {DY + dh} L{dl + 10} {DY + dh} Q{dl - 22} {DY + dh * .45:.1f} {dl} {DY} Z', ORANGE, 6)
    v += rr(dl + 18, DY + 14, drr - dl - 36, 28, 13, '#9fd8f5', 4)
    for x in (dl + 80, dl + 150): v += L(f'M{x} {DY + 15} L{x} {DY + 41}', 3)
    v += P(f'M{BX - 30} {DY + dh} L{BX + 30} {DY + dh} L{BX + 18} {DY + dh + 24} L{BX - 18} {DY + dh + 24} Z', ORANGE2, 5)
    v += rr(dl - 8, DY - 12, drr - dl + 16, 20, 9, CREAM, 5)
    v += L(f'M{dl} {DY - 48} L{drr} {DY - 48}', 4) + ''.join(L(f'M{x} {DY - 48} L{x} {DY - 10}', 4) for x in (dl, dl + 60, drr))
    for x in (dl + 6, drr - 6): v += C(x, DY + dh - 12, 6, GOLD, 3)
    # 동그란 창 (칸 사이)
    for y in ((top + 95 + DY - 200) / 2 + 40, (DY + dh + 180 + G) / 2 - 20):
        v += C(BX, y, 13 + 6 * K, SKY, 5) + F(f'M{BX - 8} {y - 6} A10 10 0 0 1 {BX + 6} {y - 8} L{BX} {y} Z', '#fff', .7)
    # 아래 받침 (발 날개 둘)
    v += P(f'M{xl0 + 3:.1f} {G - 80} L{xl0 - 46:.1f} {G - 6} L{xl0 + 6:.1f} {G - 6} Z', ORANGE, 5) + P(f'M{xr0 - 3:.1f} {G - 80} L{xr0 + 46:.1f} {G - 6} L{xr0 - 6:.1f} {G - 6} Z', ORANGE, 5)
    v += rr(xl0 - 16, G - 20, xr0 - xl0 + 32, 26, 10, STEEL2, 5)
    # 터렛 자리: 위 2 · 아래 2 (가운데 사령실을 향해 ">" 모양 — 가까운 칸 / 먼 칸)
    SX = BX + 150                                 # 터렛 4칸은 탑 바로 옆 한 줄 (탑이 높아져 위아래로 쌓을 자리가 생김)
    slots = [(top + 95, 'laser', SX), (DY - 200, 'missile', SX), (DY + dh + 180, 'cannon', SX), (G, 'tesla', SX)]
    for y, t, mx in slots:
        if y >= G: continue                       # 맨 아래 칸은 달 바닥 위
        v += rr(xl(y) - 7, y - 26, xr(y) - xl(y) + 14, 18, 8, ORANGE, 5)
        by_ = y - 80 if y < DY else y + 70          # 위 칸은 위에서 당기는 버팀대(대장 카피를 안 가리게), 아래 칸은 밑에서 받침
        v += L(f'M{xr(by_) - 2:.0f} {by_} L{mx + 40} {y + 4}', 20) + L(f'M{xr(by_) - 2:.0f} {by_} L{mx + 40} {y + 4}', 10, STEEL2)
        v += rr(xr(y) - 10, y - 6, mx + 100 - xr(y), 28, 12, STEEL, 5) + F(f'M{xr(y):.0f} {y + 1} L{mx + 82} {y + 1} L{mx + 82} {y + 7} L{xr(y):.0f} {y + 7} Z', '#fff', .6)
        for lx in (mx - 66, mx + 66): v += C(lx, y + 8, 5, GOLD, 3)
    s += spr('Fx/glow', BX, top - ah, 100, op=.8)
    s += wob(v, 2.2, .02)
    # 체력 이름판 (사령탑 앞 달 바닥)
    PX, PY = 34, G + 44
    hp = rr(PX, PY, 290, 92, 20, CREAM, 6) + heart(PX + 44, PY + 32, 40) + bar(PX + 76, PY + 48, 196, 28, .72, GREEN, '#e9dcc4', 4)
    hp += f'<rect x="{PX + 78}" y="{PY + 50}" width="40" height="24" rx="12" fill="{SKY}"/>' + L(f'M{PX + 145} {PY} L{PX + 145} {G - 4}', 6)
    s += wob(hp, 1.5, .04) + T(PX + 174, PY + 36, '2,140', 32, INK, sw=0)
    tips = {}
    for y, t, mx in slots:
        g, tip = turret_b(t, mx, y, 150); s += g; tips[t] = tip
    s += spr('Char/guardian', BX + 70, DY - 8, 150, bottom=True)
    gx, gy = BX + 130, DY - 80                     # 대장 카피 총구 근처
    # 적: 오른쪽 위에서 사령탑 쪽으로 비스듬히 (운석 꼬리는 오른쪽 위)
    def aim(x, y):
        return BX + 40, min(G - 40, max(top, y + (x - BX) * .8))
    for n, x, y, w in [('Enemy/fire', 600, 370, 96), ('Enemy/ice', 760, 430, 92), ('Enemy/pebble', 470, 520, 64), ('Enemy/crack', 330, 760, 92),
                       ('Enemy/iron', 640, 1180, 104), ('Enemy/crack', 1020, 880, 100), ('Enemy/fire', 880, 1270, 100), ('Enemy/ice', 1000, 1120, 96),
                       ('Enemy/fire', 760, 960, 104), ('Enemy/pebble', 520, 980, 68), ('Enemy/iron', 980, 1560, 96), ('Enemy/fire', 760, 1880, 100), ('Enemy/fire', 700, 1500, 100),
                       ('Enemy/ice', 900, 1760, 92), ('Enemy/crack', 660, 2010, 92), ('Enemy/pebble', 1040, 1940, 64), ('Enemy/fire', 420, 1180, 88)]:
        tx, ty = aim(x, y); s += meteor(n, x, y, w, tx, ty)
    for n, x, y, w, fl in [('Enemy/moth', 640, 1640, 120, True), ('Enemy/jelly', 960, 1380, 110, False), ('Enemy/ufo', 560, 780, 130, False),
                           ('Enemy/beetle', 980, 2060, 110, True), ('Enemy/sdrone', 230, 470, 110, True)]:
        s += spr(n, x, y, w, flip=fl)
    # 보스 (오른쪽 위 모서리에 반쯤)
    s += spr('Enemy/planet', 960, 560, 280) + bar(860, 384, 180, 24, .66, RED, sw=4)
    # 공격 (오른쪽 위로)
    # 탑이 커지고 벽에 붙어서(11-18) 공격 표적을 터렛 오른쪽 위로 다시 고름
    lx, ly = tips['laser']; s += beam(lx, ly, 560, 780, SKY, 12)
    mx_, my_ = tips['missile']; s += shot('Fx/missile', mx_ + 150, my_ - 20, -10, 110) + shot('Fx/mini_missile', mx_ + 270, my_ - 40, -8, 80)
    cx_, cy_ = tips['cannon']; s += shot('Fx/cannonball', cx_ + 120, cy_ - 57, -25, 48) + shot('Fx/cannonball', cx_ + 250, cy_ - 118, -25, 44)
    tx_, ty_ = tips['tesla']; s += zap(tx_, ty_, 650, 2010, 7)
    gx, gy = BX + 130, DY - 80
    s += shot('Fx/bullet', gx + 120, gy + 42, 19, 100) + shot('Fx/bullet', gx + 240, gy + 84, 19, 100)
    s += boom(640, 1180, 150) + boom(560, 780, 110) + boom(700, 1500, 120) + spr('Fx/hit', 640, 1640, 90) + spr('Fx/hit', 650, 2010, 80)
    s += dmg(640, 1110, '1.4K', 60, '#ffb36b') + dmg(560, 720, '486', 44) + dmg(700, 1440, '520', 44, '#ffd34d') + T(700, 1398, '약점', 26, '#fde39a', sw=5)
    s += dmg(640, 1580, '210', 40) + dmg(650, 1960, '96', 34, '#fff', .7) + dmg(1000, 1060, '88', 32, '#fff', .6)
    s += hud_route()
    return s


def tilt():
    """45° 만드는 두 방법 비교: 지금(위로) / A 통째로 45° / B 받침 그대로 머리만 45°"""
    TW, TH = 1080, 1260
    s = f'<rect width="{TW}" height="{TH}" fill="#2d3768"/>'
    cols = [(190, '지금 (위로 쏨)'), (540, 'A 통째로 45°'), (890, 'B 머리만 45°')]
    for x, t in cols:
        s += T(x, 90, t, 46, '#fff')
    s += L('M365 130 L365 1230 M715 130 L715 1230', 3, '#56609a')
    rows = [(430, 'cannon', [('cannon_barrel', None)]), (800, 'missile', [('missile_l', None), ('missile_r', None)]), (1170, 'laser', [('laser_crystal', None)])]
    w = 210
    for by, t, heads in rows:
        # 지금
        x = 190
        s += f'<ellipse cx="{x}" cy="{by + 6}" rx="{w * .55:.0f}" ry="{w * .15:.0f}" fill="{CREAM}" stroke="{INK}" stroke-width="5"/>'
        s += spr(f'Turret/{t}', x, by + 8, w, bottom=True)
        s += L(f'M{x} {by - 250} L{x} {by - 300}', 6, '#ffd34d', 'stroke-dasharray="2 12"') + P(f'M{x - 14} {by - 300} L{x} {by - 324} L{x + 14} {by - 300} Z', '#ffd34d', 3)
        # A
        x = 540
        s += L(f'M{x - 175} {by + 90} L{x - 75} {by + 20}', 26) + L(f'M{x - 175} {by + 90} L{x - 75} {by + 20}', 14, STEEL2)
        g, (ax, ay) = tilted(f'Turret/{t}', x - 60, by + 10, w)
        s += g + L(f'M{ax + 18:.0f} {ay - 18:.0f} L{ax + 60:.0f} {ay - 60:.0f}', 6, '#ffd34d', 'stroke-dasharray="2 12"')
        s += f'<g transform="translate({ax + 72:.0f} {ay - 72:.0f}) rotate(45)">' + P('M-14 0 L0 -24 L14 0 Z', '#ffd34d', 3) + '</g>'
        # B
        x = 890
        sw_, sh_ = size(f'Turret/{t}'); sc = w / sw_; h = sh_ * sc; x0, y0 = x - w / 2, by + 8 - h
        s += f'<ellipse cx="{x}" cy="{by + 6}" rx="{w * .55:.0f}" ry="{w * .15:.0f}" fill="{CREAM}" stroke="{INK}" stroke-width="5"/>'
        s += spr(f'Turret/{t}_base', x, by + 8, w, bottom=True)
        for hn, _ in heads:
            im = Image.open(os.path.join(ART, 'Turret', hn + '.png')).getchannel('A')
            bx0, by0, bx1, by1 = im.point(lambda a: 255 if a > 20 else 0).getbbox()
            px, py = x0 + (bx0 + bx1) / 2 * sc, y0 + (by1 - 12) * sc
            s += f'<g transform="rotate(45 {px:.1f} {py:.1f})">' + spr(f'Turret/{hn}', x, by + 8, w, bottom=True) + '</g>'
        hx, hy = x + 70, by - 170
        s += L(f'M{hx} {hy} L{hx + 40} {hy - 40}', 6, '#ffd34d', 'stroke-dasharray="2 12"') + f'<g transform="translate({hx + 52} {hy - 52}) rotate(45)">' + P('M-14 0 L0 -24 L14 0 Z', '#ffd34d', 3) + '</g>'
    return s, TW, TH


def page(body, w=W, h=H):
    font = url(FONT)
    svg = f'<svg xmlns="http://www.w3.org/2000/svg" width="{w}" height="{h}" viewBox="0 0 {w} {h}"><rect width="{w}" height="{h}" fill="#2d3768"/>{body}</svg>'
    return (f'<!doctype html><html><head><meta charset="utf-8"><style>@font-face{{font-family:Jua;src:url("{font}")}}'
            f'html,body{{margin:0;background:#2d3768;overflow:hidden}}</style></head><body>{svg}</body></html>')


def shoot(name, body, scale=2, w=W, h=H):
    os.makedirs(OUT, exist_ok=True)
    tmp = tempfile.mkdtemp(prefix='lay_')
    try:
        hp = os.path.join(tmp, 'a.html'); open(hp, 'w', encoding='utf-8').write(page(body, w, h))
        out = os.path.join(tmp, 'a.png')
        subprocess.run([EDGE, '--headless=new', '--disable-gpu', '--hide-scrollbars', '--allow-file-access-from-files',
                        f'--window-size={w},{h}', f'--force-device-scale-factor={scale}', '--virtual-time-budget=4000',
                        f'--user-data-dir={os.path.join(tmp, "prof")}', f'--screenshot={out}', url(hp)],
                       stdout=subprocess.DEVNULL, stderr=subprocess.DEVNULL, timeout=180)
        im = Image.open(out).convert('RGB').crop((0, 0, w * scale, h * scale)).resize((w, h), Image.LANCZOS)
        im.save(os.path.join(OUT, name + '.png'))
        print('ok', name)
    finally:
        shutil.rmtree(tmp, ignore_errors=True)


if __name__ == '__main__':
    todo = sys.argv[1:] or ['orbit', 'ark', 'tower', 'command', 'tilt']
    for n in todo:
        r = globals()[n]()
        if isinstance(r, tuple): shoot(n, r[0], w=r[1], h=r[2])
        else: shoot(n, r)
