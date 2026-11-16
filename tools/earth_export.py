# -*- coding: utf-8 -*-
"""카피바라 지구를 지켜라! — 게임 그림을 Unity 프로젝트로 뽑는다 (earth_art 의 그림 조각 사용).

사용 (PowerShell 에서 — Bash 샌드박스에선 Edge 가 안 뜸):
  $env:PYTHONUTF8='1'; python tools/earth_export.py [bg] [char] [turret] [enemy] [fx] [ui] [icon]   (없으면 전부)
  → Earth_Defence/Assets/Resources/Art/<Bg|Char|Turret|Enemy|Fx|UI|Icon>/*.png · 미리보기 tools/_preview/export_*.png

크기 규칙 (게임 코드 Art.cs 와 같게)
  · 월드 1칸 = 화면 1080 기준 100px. 게임 그림은 PPU 200 = 화면 크기의 2배로 그림 (배경은 PPU 100)
  · 가디언 k 1.84 → 화면 0.92배 (발 = 로컬 y 252) · 터렛 k 1.56 → 화면 0.78배 (바닥 = 로컬 y 214)
  · 적 k 1.1 (로컬 220 → 242px, PPU 200 → 1.21칸 x 표의 Size) · 운석은 돌 가운데가 로컬 (110,150) → 피벗 y .318
  · 배경: 하늘 1080x2400 (월드 y -9.6 ~ 14.4) · 땅 1080x560 (월드 y -9.6 ~ -4.0) · 돔 앞 1080x560
"""
import os, sys, math, random
import numpy as np
from PIL import Image

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import earth_art as A
from earth_art import *          # noqa

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), '..'))
ART = os.path.join(ROOT, 'Earth_Defence', 'Assets', 'Resources', 'Art')
PREV = A.PREV


def out(sub, name, im):
    p = os.path.join(ART, sub, name + '.png')
    os.makedirs(os.path.dirname(p), exist_ok=True)
    (im if isinstance(im, Image.Image) else Image.fromarray(im)).save(p)
    return p


def Rr(svg, w, h, wob, freq=0.022):
    return Image.fromarray(render(A.D(svg), int(w), int(h), wob=wob, freq=freq))


def white(im):
    a = np.array(im); w = np.zeros_like(a); w[..., :3] = 255; w[..., 3] = a[..., 3]
    return Image.fromarray(w)


# ------------------------------------------------------------------ 배경
def export_bg():
    old = A.H_
    rnd = random.Random(12)
    top = ''.join(f'<circle cx="{rnd.randint(0, 1080)}" cy="{rnd.randint(0, 480)}" r="{rnd.uniform(1.6, 3.4):.1f}" fill="#fff4dc" opacity="{rnd.uniform(.4, .9):.2f}"/>' for _ in range(40))
    svg = f'<rect width="1080" height="2400" fill="{A.NIGHT}"/>' + top + f'<g transform="translate(0,480)">{A.sky()}</g>'
    out('Bg', 'sky', Rr(svg, 1080, 2400, A.WOB['bg'], .008))
    y0 = 1360   # 1920 캔버스의 이 높이부터 아래(월드 -4.0 ~ -9.6)
    out('Bg', 'ground', Rr(f'<g transform="translate(0,{-y0})">{A.ground() + A.base_back()}</g>', 1080, 560, A.WOB['bg'], .01))
    out('Bg', 'dome', Rr(f'<g transform="translate(0,{-y0})">{A.dome_front()}</g>', 1080, 560, A.WOB['ui'], .02))
    out('Bg', 'slot', Rr(f'<g transform="translate(-{A.SLOT_X[3] - 80},{-(A.GROUND_Y - 40)})">{A.slot_empty(A.SLOT_X[3])}</g>', 160, 80, A.WOB['ui'], .02))
    print('bg')


# ------------------------------------------------------------------ 가디언 · 터렛
GK = 1.8   # 대장 카피 그림 배율 (PPU 200 → 화면 0.9배) — 11-16 "너무 큼, 0.7~0.8" → 2.4 의 0.75배 — 게임 Art.cs 의 Guardian* 값과 같게


def export_char():
    # 11-16 오후: 몸 · 총(따로 돎) · 카드/로비용 한 장
    out('Char', 'guardian', A.guardian_img(GK))
    out('Char', 'guardian_gun', A.guardian_gun_img(GK))
    out('Char', 'guardian_ui', A.guardian_ui_img(1.6))
    x0, y0, w, h = A.GUN_BOX
    print('char  body pivot', round(A.G_CX / A.G_W, 4), round((A.G_H - A.G_FOOT) / A.G_H, 4),
          ' hand(units)', round((A.HAND[0] - A.G_CX) * GK / 200, 4), round((A.G_FOOT - A.HAND[1]) * GK / 200, 4),
          ' gun pivot', round(-x0 / w, 4), round((h + y0) / h, 4), ' muzzle(units)', round(A.GUN_MUZ[0] * GK / 200, 4), round(-A.GUN_MUZ[1] * GK / 200, 4))


def export_turret():
    for tid, *_ in A.TURRETS:
        out('Turret', tid, A.turret_img(tid, 1.56))
    # 11-16 밤 터렛 대기 효과: 받침 + 움직일 조각 (같은 캔버스 200x222 · 같은 피벗) · 작은 번개 3장 (왼쪽 가운데 피벗)
    parts = {'laser_base': A.t_laser_base, 'laser_crystal': A.t_laser_crystal,
             'laser_ring_back': lambda: A.laser_ring(False), 'laser_ring_front': lambda: A.laser_ring(True),
             'bolt_base': A.t_bolt_base, 'bolt_orb': A.t_bolt_orb,
             # 2단계 (11-16 밤 "나머지도 진행")
             'rail_base': A.t_rail_base, 'rail_hl': A.t_rail_hl,
             'drone_base': A.t_drone_base, 'drone_body': A.t_drone_body,
             'hive_base': A.t_hive_base,
             'missile_base': A.t_missile_base, 'missile_l': lambda: A.missile(78, 84), 'missile_r': lambda: A.missile(122, 76),
             # 2차 5종 (11-16)
             'cannon_base': A.t_cannon_base, 'cannon_barrel': A.t_cannon_barrel, 'focus_base': A.t_focus_base,
             'vortex_base': A.t_vortex_base, 'wheel_base': A.t_wheel_base, 'tesla_base': A.t_tesla_base, 'tesla_orb': A.t_tesla_orb}
    for n, fn in parts.items():
        out('Turret', n, A.piece(fn(), 200, 222, 1.56, 0 if 'ring' in n else A.WOB['turret'], .018))
    for i in range(3):
        out('Turret', f'spark_arc_{i}', A.piece(A.spark_arc(11 + i * 7), 64, 28, 1.56, 0))
    # 가운데 피벗 조각 (돌거나 따로 움직임): 그림 좌표 (cx, cy) 를 가운데로 w x h 만 잘라 그림
    def box(svg, cx, cy, w, h):
        return A.piece(f'<g transform="translate({w / 2 - cx},{h / 2 - cy})">{svg}</g>', w, h, 1.56, A.WOB['turret'], .018)
    cx, cy, r = A.RAIL_WHEEL
    out('Turret', 'rail_wheel', box(A.t_rail_wheel(), cx, cy, 84, 84))
    out('Turret', 'drone_prop', box(A.t_drone_prop(), 36, 58, 70, 26))
    out('Turret', 'drone_pupil', box(A.t_drone_pupil(), 100, 88, 30, 30))
    out('Turret', 'drone_lid', box(A.t_drone_lid(), 100, 86, 50, 38))
    out('Turret', 'focus_spin', box(A.t_focus_spin(), *A.FOCUS_C, 92, 92))
    out('Turret', 'vortex_spin', box(A.t_vortex_spin(), *A.VORTEX_C, 100, 100))
    out('Turret', 'wheel_spin', box(A.t_wheel_spin(), *A.WHEEL_C, 104, 104))
    print('turret', len(A.TURRETS), '+ 대기 조각', len(parts) + 7)


# ------------------------------------------------------------------ 적 (+ 맞을 때 흰 실루엣)
def crown(x, y, k=1.0):
    return (f'<g transform="translate({x},{y}) scale({k})">' + P('M-30 10 L-34 -18 L-16 -4 L0 -26 L16 -4 L34 -18 L30 10 Z', GOLD, 5)
            + C(0, -26, 5, RED, 3) + C(-34, -18, 4, SKY, 3) + C(34, -18, 4, SKY, 3) + '</g>')


def mother_ship():
    s = A.s_ufo()
    return f'<g transform="translate(-22,-10) scale(1.2)">{s}</g>' + ''.join(C(x, 160, 6, RED, 3.5) for x in (60, 110, 160))


VARIANTS = {
    'moth_king': lambda: A.b_moth('#b48ad8') + crown(110, 128, .8),
    'beetle_steel': lambda: A.b_beetle(METAL) + L('M84 40 L70 14 M136 40 L150 14', 7) + L('M84 40 L70 14 M136 40 L150 14', 3, '#dfe4e8'),
    'mother': mother_ship,
    'queen': lambda: A.v_jelly('#a8d672') + crown(110, 46, 1.0),
}


def export_enemy():
    k = 1.1
    for eid, name, fam, fn, size in A.ENEMIES:
        im = A.piece(fn(), size, size, k, A.WOB['enemy'], .018)
        out('Enemy', eid, im); out('Enemy', eid + '_w', white(im))
    for eid, fn in VARIANTS.items():
        im = A.piece(fn(), 220, 220, k, A.WOB['enemy'], .018)
        out('Enemy', eid, im); out('Enemy', eid + '_w', white(im))
    print('enemy', len(A.ENEMIES) + len(VARIANTS))


def _hgrad(col, o0, o1, x0=0.0, x1=1.0):
    """가로 투명도 그라데이션 (꼬리 쪽 투명 → 머리 쪽 진하게)"""
    gid = f'h{col.lstrip("#")}{int(o0 * 100)}{int(o1 * 100)}{int(x0 * 100)}{int(x1 * 100)}'
    A._DEFS.setdefault(gid, f'<linearGradient id="{gid}" x1="{x0}" y1="0" x2="{x1}" y2="0"><stop offset="0" stop-color="{col}" stop-opacity="{o0}"/>'
                            f'<stop offset="1" stop-color="{col}" stop-opacity="{o1}"/></linearGradient>')
    return f'url(#{gid})'


def _rgrad(col, o0=1.0, o1=0.0, mid=None):
    gid = f'r{col.lstrip("#")}{int(o0 * 100)}{int(o1 * 100)}{int((mid or 0) * 100)}'
    stops = f'<stop offset="0" stop-color="{col}" stop-opacity="{o0}"/>'
    if mid:
        stops += f'<stop offset="{mid}" stop-color="{col}" stop-opacity="{o0 * .45}"/>'
    A._DEFS.setdefault(gid, f'<radialGradient id="{gid}" cx=".5" cy=".5" r=".5">{stops}<stop offset="1" stop-color="{col}" stop-opacity="{o1}"/></radialGradient>')
    return f'url(#{gid})'


def _cap(x0, x1, cy, h, fill):
    """머리 쪽이 둥근 꼬리 (x0 꼬리 끝은 뾰족, x1 머리)"""
    r = h / 2
    return f'<path d="M{x0} {cy} Q{(x0 + x1) / 2} {cy - r} {x1 - r} {cy - r} A{r} {r} 0 0 1 {x1 - r} {cy + r} Q{(x0 + x1) / 2} {cy + r} {x0} {cy} Z" fill="{fill}"/>'


def neon_bolt(w, h, head, col, mid, scale=1.0):
    """네온 탄: 바깥 번짐 · 색 띠 · 흰 심 · 머리 빛 (오른쪽이 앞, 머리 가운데 x = head)"""
    cy = h / 2; k = scale
    # 11-16 오후: 두께 약 60% · 머리 빛 작게 → 샤프하게
    s = blur(_cap(4, head + 10 * k, cy, 13 * k, _hgrad(col, 0, .85)), 3.5 * k)
    s += _cap(head - 74 * k, head + 7 * k, cy, 7.5 * k, _hgrad(mid, 0, 1))
    s += _cap(head - 46 * k, head + 5 * k, cy, 3.2 * k, _hgrad('#ffffff', .2, 1))
    s += blur(f'<circle cx="{head}" cy="{cy}" r="{5 * k}" fill="#ffffff"/>', 2 * k, .85)
    return s


def lightning(seed, w=160, h=600, pad=26):
    """11-16 오후 "벼락 끝이 잘린 느낌 → 하나의 벼락처럼": 마디마다 굵기를 바꿔 양 끝을 가늘게 모으고, 끝이 그림 밖으로 안 나가게 여백"""
    rnd = random.Random(seed); pts = [(w / 2, pad)]; y = pad
    while y < h - pad:
        y = min(h - pad, y + rnd.uniform(40, 80)); pts.append((w / 2 + (rnd.uniform(-30, 30) if y < h - pad else pts[-1][0] - w / 2 + rnd.uniform(-8, 8)), y))
    def taper(t):   # 0 = 위 끝, 1 = 아래 끝 → 끝은 가늘게
        return max(.12, min(1.0, 4 * min(t, 1 - t)) ** .7)
    glow_, mid_, core_ = [], [], []
    total = sum(math.dist(pts[i], pts[i + 1]) for i in range(len(pts) - 1)); run = 0.0
    for i in range(len(pts) - 1):            # 마디를 잘게 나눠 굵기가 부드럽게 변하게
        (x0, y0), (x1, y1) = pts[i], pts[i + 1]; seg = math.dist(pts[i], pts[i + 1]); m = 8
        for j in range(m):
            a0, a1 = j / m, (j + 1) / m
            k = taper((run + seg * (a0 + a1) / 2) / total)
            d = f'M{x0 + (x1 - x0) * a0:.1f} {y0 + (y1 - y0) * a0:.1f} L{x0 + (x1 - x0) * a1:.1f} {y0 + (y1 - y0) * a1:.1f}'
            glow_.append(L(d, 26 * k, '#ffd84a')); mid_.append(L(d, 11 * k, '#ffe98a')); core_.append(L(d, 4.5 * k, '#ffffff'))
        run += seg
    fork = ''
    for i in (2, 5):
        if i < len(pts) - 1:
            px, py = pts[i]; sx = rnd.choice((-1, 1))
            fork += f'M{px:.0f} {py:.0f} l{sx * 22} 28 l{-sx * 7} 22 l{sx * 14} 28 '
    return (blur(''.join(glow_) + L(fork, 9, '#ffd84a'), 10, .85) + ''.join(mid_) + L(fork, 4, '#ffe98a')
            + ''.join(core_) + L(fork, 1.8, '#ffffff'))


def sparkle(cx, cy, r, col='#ffffff', thin=.16):
    """네 갈래 반짝 (가운데 굵고 끝 뾰족)"""
    t = r * thin
    return (f'<path d="M{cx} {cy - r} Q{cx + t} {cy - t} {cx + r} {cy} Q{cx + t} {cy + t} {cx} {cy + r} Q{cx - t} {cy + t} {cx - r} {cy} '
            f'Q{cx - t} {cy - t} {cx} {cy - r} Z" fill="{col}"/>')


# ------------------------------------------------------------------ 효과 (오른쪽 = 앞)
# 11-16 1차 수정안: "탄환이 밋밋 · 미사일이 투박 → 네온 · 스파크" → 테두리 없는 네온 빛 그림으로 바꿈.
# 탄 그림은 꼬리가 들어 있어 머리 위치가 가운데가 아님 → 게임 Art.ShotPivot 과 같은 값(머리 x / 너비)
SHOT_HEAD = {'bullet': (92, 120), 'bullet_charge': (120, 160), 'shell': (176, 232), 'missile': (138, 184), 'mini_missile': (84, 112)}


def export_fx():
    W = A.WOB['fx']
    fx = {}
    fx['bullet'] = (neon_bolt(120, 40, 92, '#ffc94a', '#ffe28a'), 120, 40)
    fx['bullet_charge'] = (neon_bolt(160, 64, 120, '#ffb23e', '#ffe28a', 1.5)
                           + blur(f'<circle cx="120" cy="32" r="15" fill="none" stroke="#fff3c4" stroke-width="3"/>', 1.5, .7), 160, 64)
    # 관통포: 에너지 화살촉 + 속도 줄
    # 11-16 오후 "관통탄이 대장 탄과 거의 비슷함 → 조금 크고 눈에 띄게": 주황 에너지 창 + 뒤로 갈매기 고리 2개 + 긴 속도 줄
    cy = 40
    spear = f'M86 {cy} L130 {cy - 15} L170 {cy - 9} L198 {cy} L170 {cy + 9} L130 {cy + 15} Z'
    chev = lambda x, k: f'M{x} {cy - 17 * k} L{x + 12 * k} {cy} L{x} {cy + 17 * k}'
    fx['shell'] = (blur(_cap(8, 204, cy, 34, _hgrad('#ff9a48', 0, .9)), 7)
                   + L('M14 26 L96 26 M22 54 L102 54', 3, '#ffe0b8').replace('stroke=', 'opacity=".6" stroke=')
                   + blur(L(chev(92, 1) + chev(66, .8), 9, '#ffb36b'), 3, .9) + L(chev(92, 1) + chev(66, .8), 4, '#fff1dc')
                   + f'<path d="{spear}" fill="#ffc88f"/>' + f'<path d="M122 {cy} L136 {cy - 7} L172 {cy - 4} L188 {cy} L172 {cy + 4} L136 {cy + 7} Z" fill="#ffffff"/>'
                   + blur(f'<circle cx="176" cy="{cy}" r="10" fill="#ffffff"/>', 3, .9), 232, 80)
    # 미사일: 만화 미사일 + 빛나는 불꽃 꼬리
    def missile_fx(w, h, head, k):
        cy = h / 2
        flame = (blur(_cap(4, head - 28 * k, cy, 30 * k, _hgrad('#ff8a4a', 0, .95)), 6 * k)
                 + _cap(head - 96 * k, head - 28 * k, cy, 18 * k, _hgrad('#ffb35c', 0, 1))
                 + _cap(head - 74 * k, head - 30 * k, cy, 9 * k, _hgrad('#fff4c8', 0, 1)))
        body = f'<g transform="translate({head - 6 * k},{cy}) rotate(90) scale({.8 * k})">{A.missile(0, 0, 1.0)}</g>'
        tip = blur(f'<circle cx="{head + 22 * k}" cy="{cy}" r="{6 * k}" fill="#ffd7b0"/>', 3 * k, .7)
        return flame + body + tip
    fx['missile'] = (missile_fx(184, 64, 138, 1.0), 184, 64)
    fx['mini_missile'] = (missile_fx(112, 40, 84, .6), 112, 40)
    fx['bee'] = (blur('<circle cx="30" cy="30" r="20" fill="#ffd34d"/>', 7, .7) + f'<g transform="translate(30,32) scale(1.3)">{A.bee(0, 0)}</g>', 64, 60)
    fx['trail'] = ('<path d="M2 18 L98 6 L98 30 Z" fill="#fff4dc" opacity=".55"/>', 100, 36)
    for n, (svg, w, h) in fx.items():
        out('Fx', n, Rr(svg, w, h, W))
    # 레이저: 번짐 띠(색은 코드) + 흰 심 — 둘 다 9분할 가로
    out('Fx', 'beam', Rr(blur(A.rr(18, 18, 92, 28, 14, '#ffffff', 0), 8) + f'<rect x="20" y="20" width="88" height="24" rx="12" fill="#ffffff" opacity=".55"/>', 128, 64, 0))
    out('Fx', 'beam_core', Rr(blur('<rect x="10" y="10" width="108" height="12" rx="6" fill="#ffffff"/>', 1.5) + '<rect x="12" y="12" width="104" height="8" rx="4" fill="#ffffff"/>', 128, 32, 0))
    # 낙뢰 (세로, 위→아래) 2장 — 번갈아 보여 지직거림
    out('Fx', 'bolt', Rr(lightning(3), 160, 600, 0))
    out('Fx', 'bolt2', Rr(lightning(11), 160, 600, 0))
    # 불꽃 튐 · 빛 · 총구 · 맞음 · 폭발 · 링 (흰색 = 코드에서 색)
    out('Fx', 'spark', Rr(blur(_cap(2, 62, 8, 9, _hgrad('#ffffff', 0, .9)), 2) + _cap(14, 60, 8, 4, _hgrad('#ffffff', 0, 1)), 64, 16, 0))
    out('Fx', 'glow', Rr(f'<circle cx="64" cy="64" r="62" fill="{_rgrad("#ffffff", 1, 0, .35)}"/>', 128, 128, 0))
    out('Fx', 'muzzle', Rr(blur('<circle cx="48" cy="48" r="20" fill="#ffffff"/>', 8, .9) + sparkle(48, 48, 44, '#ffffff', .12)
                           + f'<circle cx="48" cy="48" r="9" fill="#ffffff"/>', 96, 96, 0))
    out('Fx', 'hit', Rr(blur('<circle cx="48" cy="48" r="18" fill="#ffffff"/>', 7, .8)
                        + '<circle cx="48" cy="48" r="30" fill="none" stroke="#ffffff" stroke-width="3.5" opacity=".8"/>'
                        + sparkle(48, 48, 40, '#ffffff', .1) + f'<g transform="rotate(45 48 48)">{sparkle(48, 48, 22, "#ffffff", .14)}</g>', 96, 96, 0))
    out('Fx', 'burst', Rr(blur('<circle cx="96" cy="96" r="70" fill="#ffb35c"/>', 16, .8)
                          + burst(96, 96, 80, 10, '#ffcf6b', 0, .5) + burst(96, 96, 54, 8, '#ffe9a6', 0, .55)
                          + blur('<circle cx="96" cy="96" r="30" fill="#ffffff"/>', 8) + '<circle cx="96" cy="96" r="20" fill="#ffffff"/>', 192, 192, 0))
    out('Fx', 'ring', Rr(blur('<circle cx="80" cy="80" r="66" fill="none" stroke="#ffffff" stroke-width="12"/>', 5, .8)
                         + '<circle cx="80" cy="80" r="66" fill="#ffffff" opacity=".12"/><circle cx="80" cy="80" r="66" fill="none" stroke="#ffffff" stroke-width="4"/>', 160, 160, 0))
    out('Fx', 'zone', Rr(f'<circle cx="80" cy="80" r="76" fill="{_rgrad("#ffffff", .55, .05)}"/>'
                         + blur('<circle cx="80" cy="80" r="70" fill="none" stroke="#ffffff" stroke-width="6"/>', 3, .7) + ''.join(
        P(f'M{80 + 46 * math.cos(a):.0f} {80 + 46 * math.sin(a) + 10:.0f} q-10 -14 0 -28 q10 14 0 28 Z', '#ffffff', 0) for a in [i * 1.2566 for i in range(5)]), 160, 160, 0))
    # 장판 띠 (9분할): 가운데 옅게 · 위아래 빛 줄 (점선 → 매끈한 빛)
    vid = 'fieldv'
    A._DEFS.setdefault(vid, f'<linearGradient id="{vid}" x1="0" y1="0" x2="0" y2="1"><stop offset="0" stop-color="#fff" stop-opacity=".5"/>'
                            f'<stop offset=".25" stop-color="#fff" stop-opacity=".22"/><stop offset=".75" stop-color="#fff" stop-opacity=".22"/>'
                            f'<stop offset="1" stop-color="#fff" stop-opacity=".5"/></linearGradient>')
    out('Fx', 'field', Rr(f'<rect x="4" y="10" width="112" height="60" rx="20" fill="url(#{vid})"/>'
                          + blur('<path d="M14 10 L106 10 M14 70 L106 70" stroke="#ffffff" stroke-width="6"/>', 3, .9)
                          + '<path d="M14 10 L106 10 M14 70 L106 70" stroke="#ffffff" stroke-width="3"/>', 120, 80, 0))
    # 11-16 오후 "터렛이 있는 느낌이 약함 → 생성된 터렛 하단에 강조 표시": 바닥 빛 고리 (흰색 → 코드에서 속성 색 · 빛 더하기)
    out('Fx', 'pad', Rr(f'<ellipse cx="160" cy="55" rx="150" ry="44" fill="{_rgrad("#ffffff", .55, 0)}"/>'
                        + blur('<ellipse cx="160" cy="55" rx="122" ry="30" fill="none" stroke="#ffffff" stroke-width="10"/>', 5, .9)
                        + '<ellipse cx="160" cy="55" rx="122" ry="30" fill="none" stroke="#ffffff" stroke-width="3.5"/>', 320, 110, 0))
    # 2차 (11-16): 쾅쾅 대포 포탄 · 부유 기뢰 · 소용돌이(흰색 → 코드에서 색) · 불바퀴
    out('Fx', 'cannonball', Rr(blur('<circle cx="48" cy="48" r="30" fill="#ffcf6b"/>', 9, .8)
                               + C(48, 48, 21, A.G('#7d6a5a', 'r', .7, .35), 4.5) + F('M36 40 Q40 30 50 30 Q42 34 40 44 Z', '#ffffff', .7)
                               + blur('<circle cx="48" cy="48" r="8" fill="#ffe9b0"/>', 3, .8), 96, 96, 0))
    spikes = ''.join(L(f'M{64 + 22 * math.cos(a):.1f} {64 + 22 * math.sin(a):.1f} L{64 + 34 * math.cos(a):.1f} {64 + 34 * math.sin(a):.1f}', 5) for a in [i * math.pi / 4 for i in range(8)])
    out('Fx', 'mine', Rr(blur('<circle cx="64" cy="64" r="34" fill="#ffb35c"/>', 10, .6) + spikes
                         + C(64, 64, 24, A.G('#9aa3ad', 'r', .7, .35), 4.5) + blur(C(64, 64, 8, '#ff7a5a', 0), 3, .9) + C(64, 64, 6, '#ffd0b8', 0), 128, 128, 0))
    # 11-16 저녁 "희미해 보여서 조금 더 선명하게": 팔을 굵고 진하게 · 바깥 테 · 가운데는 어두운 구멍 + 밝은 테 (색은 코드에서 곱함)
    arms = ''.join(f'<path d="M180 180 Q262 168 272 74" transform="rotate({i * 60} 180 180)" fill="none" stroke="#ffffff" stroke-width="{24 - i * 2}" stroke-linecap="round"/>' for i in range(6))
    out('Fx', 'vortex', Rr(f'<circle cx="180" cy="180" r="172" fill="{_rgrad("#ffffff", .75, .12, .5)}"/>'
                           + '<circle cx="180" cy="180" r="166" fill="none" stroke="#ffffff" stroke-width="7" opacity=".9"/>'
                           + blur(arms, 7, 1) + arms
                           + '<circle cx="180" cy="180" r="44" fill="#2a2140"/><circle cx="180" cy="180" r="44" fill="none" stroke="#ffffff" stroke-width="8"/>', 360, 360, 0))
    # 불바퀴 둘레 이글이글 불꽃 (흰 → 코드에서 주황 · 빛 더하기): 아래 둥근 · 위 뾰족
    out('Fx', 'flame', Rr(blur('<path d="M32 92 C10 92 6 66 18 50 C26 38 28 22 32 4 C38 24 46 36 50 50 C60 68 54 92 32 92 Z" fill="#ffffff"/>', 4, .8)
                          + '<path d="M32 88 C18 88 14 70 22 58 C28 48 30 36 32 22 C36 36 42 46 44 58 C50 72 46 88 32 88 Z" fill="#ffffff"/>', 64, 96, 0))
    out('Fx', 'wheel', Rr(f'<g transform="translate({100 - A.WHEEL_C[0] * 1.9},{100 - A.WHEEL_C[1] * 1.9}) scale(1.9)">{A.t_wheel_spin()}</g>', 200, 200, 0))
    out('Fx', 'puff', Rr(puff(50, 50, 40, '#ffffff', 5, 7, 3), 100, 100, 1.0))
    out('Fx', 'dot', Rr(f'<circle cx="16" cy="16" r="14" fill="#ffffff"/>', 32, 32, 0))
    print('fx')


# ------------------------------------------------------------------ UI 9분할 판
def export_ui():
    def card(fill, lip, w=120, h=120, r=24, sw=5.5, lipH=12):
        return A.rr(sw / 2 + 1, sw / 2 + 1, w - sw - 2, h - sw - 2, r, lip, sw) + rrf(sw + 1, sw + 1, w - 2 * sw - 2, h - 2 * sw - 2 - lipH, r - 4, fill) \
            + rrf(sw + 8, sw + 5, w - 2 * sw - 18, 6, 3, '#ffffff', .35)
    items = {
        'card': card(CREAM, TAN), 'card_white': card('#fffaf0', '#e9dcc2'), 'btn_green': card(GREEN, GREEN2), 'btn_gray': card('#cfc7b8', '#a8a092'),
        'btn_orange': card(ORANGE, ORANGE2), 'btn_red': card(RED, RED2), 'btn_cream': card(CREAM, TAN), 'btn_sky': card('#bfe6fb', '#8fc3e0'),
        'btn_purple': card(PURPLE, PURPLE2), 'slot': card(CREAM2, '#dcc8a2'), 'night': card(NIGHT2, NIGHT),
        'pill': A.rr(4, 4, 112, 72, 36, CREAM, 5), 'pill_dark': A.rr(4, 4, 112, 72, 36, TRACK, 4),
        'track': A.rr(3, 3, 74, 34, 17, TRACK, 4), 'fill': rrf(0, 0, 80, 40, 20, '#ffffff'),
        'white': rrf(0, 0, 80, 80, 24, '#ffffff'), 'window': A.rr(4, 4, 232, 232, 30, CREAM, 6),
        'slot_dash': f'<rect x="4" y="4" width="112" height="112" rx="22" fill="{CREAM2}" stroke="{INK}" stroke-width="5" stroke-dasharray="14 9"/>',
        'band': f'<path d="M3 30 Q3 3 30 3 L90 3 Q117 3 117 30 L117 117 L3 117 Z" fill="#ffffff"/>',
        # 11-16 저녁 사용자 "속성이 이쁘게 안 보임 — 타원이 아닌 네모(태그를 붙이는 느낌)": 흰 바탕(코드에서 속성 색) + 왼쪽 뾰족 + 구멍
        'tag': f'<path d="M28 5 L106 5 Q115 5 115 14 L115 58 Q115 67 106 67 L28 67 L6 36 Z" fill="#ffffff" stroke="{INK}" stroke-width="5" stroke-linejoin="round"/>'
               f'<circle cx="26" cy="36" r="6.5" fill="{INK}"/><path d="M34 13 L104 13" stroke="#ffffff" stroke-width="4" opacity=".5"/>',
    }
    sizes = {'tag': (120, 72), 'pill': (120, 80), 'pill_dark': (120, 80), 'track': (80, 40), 'fill': (80, 40), 'white': (80, 80), 'window': (240, 240)}
    for n, svg in items.items():
        w, h = sizes.get(n, (120, 120))
        out('UI', n, Rr(svg, w, h, A.WOB['ui'] if n not in ('fill', 'white', 'band', 'tag') else 0, .04))
    print('ui', len(items))


# ------------------------------------------------------------------ 아이콘 (112x112)
def ic_cards(w, h, f, s): return A.rr(w * .14, h * .18, w * .5, h * .66, 8, CREAM2, s) + A.rr(w * .36, h * .1, w * .5, h * .66, 8, CREAM, s)
def ic_coin(w, h, f, s): return coin(w / 2, h / 2, w * .42, s)
def ic_shard(w, h, f, s): return P(f'M{w*.5} {h*.06} L{w*.84} {h*.4} L{w*.62} {h*.94} L{w*.24} {h*.84} L{w*.14} {h*.36} Z', SKY, s) + L(f'M{w*.5} {h*.06} L{w*.46} {h*.54} L{w*.62} {h*.94} M{w*.14} {h*.36} L{w*.46} {h*.54} L{w*.84} {h*.4}', s * .6)
def ic_dia(col):
    return lambda w, h, f, s: P(f'M{w*.5} {h*.08} L{w*.9} {h*.5} L{w*.5} {h*.92} L{w*.1} {h*.5} Z', col, s)
def ic_ad(w, h, f, s): return A.rr(w * .08, h * .2, w * .84, h * .6, 12, CREAM, s) + P(f'M{w*.4} {h*.36} L{w*.66} {h*.5} L{w*.4} {h*.64} Z', ORANGE2, s * .8)
def ic_plus(w, h, f, s): d = f'M{w*.5} {h*.2} L{w*.5} {h*.8} M{w*.2} {h*.5} L{w*.8} {h*.5}'; return L(d, w * .24) + L(d, w * .24 - 2 * s, f)
def ic_home(w, h, f, s): return P(f'M{w*.1} {h*.5} L{w*.5} {h*.12} L{w*.9} {h*.5} L{w*.78} {h*.5} L{w*.78} {h*.88} L{w*.22} {h*.88} L{w*.22} {h*.5} Z', f, s) + A.rr(w * .42, h * .62, w * .16, h * .26, 3, ORANGE2, s * .7)
def ic_retry(w, h, f, s): d = f'M{w*.78} {h*.5} A{w*.3} {h*.3} 0 1 1 {w*.62} {h*.22}'; return L(d, w * .2) + L(d, w * .2 - 2 * s, f) + P(f'M{w*.56} {h*.06} L{w*.78} {h*.24} L{w*.52} {h*.36} Z', f, s)
def ic_base(w, h, f, s): return P(f'M{w*.08} {h*.86} A{w*.42} {h*.62} 0 0 1 {w*.92} {h*.86} Z', '#cfeeff', s) + A.rr(w * .3, h * .5, w * .4, h * .36, 6, CREAM, s) + P(f'M{w*.24} {h*.52} L{w*.5} {h*.32} L{w*.76} {h*.52} Z', ORANGE, s)
def ic_patrol(w, h, f, s): return C(w * .5, h * .54, w * .38, CREAM, s) + L(f'M{w*.5} {h*.3} L{w*.5} {h*.56} L{w*.68} {h*.66}', s * 1.3) + A.rr(w * .4, h * .04, w * .2, h * .12, 3, ORANGE, s * .8)


# 2차 (11-16): 장비 6부위 · 칩 · 스태미나 · 미션 · 출석 · 설정 · 부품 · 엘리트 · 손가락 · 탭
def ic_g_weapon(w, h, f, s): return (A.rr(w * .12, h * .42, w * .7, h * .2, 6, '#e6e9ee', s) + A.rr(w * .62, h * .36, w * .3, h * .14, 4, GOLD, s * .8)
                                     + P(f'M{w*.26} {h*.6} L{w*.42} {h*.6} L{w*.36} {h*.86} L{w*.22} {h*.86} Z', '#a9b1bb', s) + C(w * .88, h * .43, w * .05, '#fff3c4', 0))
def ic_g_helmet(w, h, f, s): return C(w * .5, h * .52, w * .4, '#eef2f4', s) + P(f'M{w*.22} {h*.5} Q{w*.5} {h*.3} {w*.78} {h*.5} Q{w*.78} {h*.72} {w*.5} {h*.74} Q{w*.22} {h*.72} {w*.22} {h*.5} Z', '#8fd6ff', s * .8) + F(f'M{w*.32} {h*.48} Q{w*.4} {h*.4} {w*.5} {h*.4} Q{w*.4} {h*.45} {w*.36} {h*.54} Z', '#ffffff', .8)
def ic_g_suit(w, h, f, s): return P(f'M{w*.3} {h*.12} L{w*.7} {h*.12} L{w*.9} {h*.36} L{w*.78} {h*.46} L{w*.74} {h*.9} L{w*.26} {h*.9} L{w*.22} {h*.46} L{w*.1} {h*.36} Z', '#f6f7f8', s) + A.rr(w * .38, h * .4, w * .24, h * .18, 4, '#f0a06a', s * .7) + C(w * .5, h * .2, w * .08, '#7ff0ff', s * .6)
def ic_g_shield(w, h, f, s): return P(f'M{w*.5} {h*.06} L{w*.9} {h*.2} Q{w*.92} {h*.66} {w*.5} {h*.94} Q{w*.08} {h*.66} {w*.1} {h*.2} Z', '#9fd6ff', s) + P(f'M{w*.5} {h*.2} L{w*.76} {h*.3} Q{w*.76} {h*.62} {w*.5} {h*.8} Z', '#d4efff', 0)
def ic_g_core(w, h, f, s): return P(f'M{w*.5} {h*.06} L{w*.88} {h*.28} L{w*.88} {h*.72} L{w*.5} {h*.94} L{w*.12} {h*.72} L{w*.12} {h*.28} Z', '#a9b1bb', s) + C(w * .5, h * .5, w * .22, '#ffd966', s * .8) + C(w * .44, h * .44, w * .07, '#ffffff', 0)
def ic_g_boots(w, h, f, s): return P(f'M{w*.3} {h*.08} L{w*.6} {h*.08} L{w*.6} {h*.56} L{w*.9} {h*.66} Q{w*.94} {h*.86} {w*.8} {h*.88} L{w*.24} {h*.88} Q{w*.18} {h*.88} {w*.2} {h*.7} Z', '#f6f7f8', s) + A.rr(w * .18, h * .78, w * .74, h * .12, 4, '#f0a06a', s * .7) + L(f'M{w*.3} {h*.3} L{w*.6} {h*.3}', s * .7)
def ic_chip(w, h, f, s):
    pins = ''.join(L(f'M{w*x} {h*.08} L{w*x} {h*.22} M{w*x} {h*.78} L{w*x} {h*.92} M{w*.08} {h*x} L{w*.22} {h*x} M{w*.78} {h*x} L{w*.92} {h*x}', s * .9) for x in (.35, .5, .65))
    return pins + A.rr(w * .2, h * .2, w * .6, h * .6, 8, '#8ccb6e', s) + A.rr(w * .34, h * .34, w * .32, h * .32, 4, '#3f7d3a', s * .6) + C(w * .3, h * .3, w * .04, '#fff4dc', 0)
def ic_energy(w, h, f, s): return C(w * .5, h * .5, w * .44, '#fff4dc', s) + P(f'M{w*.58} {h*.14} L{w*.26} {h*.56} L{w*.48} {h*.56} L{w*.4} {h*.86} L{w*.74} {h*.44} L{w*.52} {h*.44} Z', GOLD, s * .8)
def ic_mission(w, h, f, s): return (A.rr(w * .16, h * .12, w * .68, h * .8, 8, CREAM, s) + A.rr(w * .36, h * .04, w * .28, h * .14, 4, ORANGE, s * .8)
                                    + L(f'M{w*.28} {h*.42} L{w*.36} {h*.5} L{w*.48} {h*.36} M{w*.28} {h*.68} L{w*.36} {h*.76} L{w*.48} {h*.62}', s * .9, '#3f7d3a')
                                    + L(f'M{w*.56} {h*.44} L{w*.74} {h*.44} M{w*.56} {h*.7} L{w*.74} {h*.7}', s * .9))
def ic_calendar(w, h, f, s): return (A.rr(w * .1, h * .16, w * .8, h * .74, 10, CREAM, s) + P(f'M{w*.1} {h*.36} L{w*.1} {h*.26} Q{w*.1} {h*.16} {w*.2} {h*.16} L{w*.8} {h*.16} Q{w*.9} {h*.16} {w*.9} {h*.26} L{w*.9} {h*.36} Z', RED, s)
                                     + L(f'M{w*.3} {h*.08} L{w*.3} {h*.24} M{w*.7} {h*.08} L{w*.7} {h*.24}', s * 1.4) + L(f'M{w*.34} {h*.6} L{w*.46} {h*.72} L{w*.68} {h*.48}', s * 1.2, '#3f7d3a'))
def ic_parts(w, h, f, s): return P(' '.join(('M' if i == 0 else 'L') + f'{w*.5 + w*.42*math.cos(math.radians(30 + i*60)):.1f} {h*.5 + h*.42*math.sin(math.radians(30 + i*60)):.1f}' for i in range(6)) + ' Z', '#c9c1b3', s) + C(w * .5, h * .5, w * .16, '#5b4433', s * .8)
def ic_crown(w, h, f, s): return P(f'M{w*.1} {h*.3} L{w*.32} {h*.52} L{w*.5} {h*.2} L{w*.68} {h*.52} L{w*.9} {h*.3} L{w*.82} {h*.82} L{w*.18} {h*.82} Z', GOLD, s) + C(w * .5, h * .62, w * .07, RED, s * .6)
def ic_hand(w, h, f, s): return P(f'M{w*.36} {h*.5} L{w*.36} {h*.12} Q{w*.36} {h*.04} {w*.44} {h*.04} Q{w*.52} {h*.04} {w*.52} {h*.12} L{w*.52} {h*.4} L{w*.72} {h*.44} Q{w*.86} {h*.48} {w*.84} {h*.62} L{w*.78} {h*.86} L{w*.4} {h*.86} L{w*.2} {h*.62} Q{w*.16} {h*.52} {w*.26} {h*.5} Z', '#fff4dc', s)
def ic_tab_turret(w, h, f, s): return P(f'M{w*.18} {h*.9} L{w*.26} {h*.6} L{w*.74} {h*.6} L{w*.82} {h*.9} Z', '#c9c1b3', s) + C(w * .5, h * .5, w * .22, '#d9c19a', s) + A.rr(w * .44, h * .08, w * .12, h * .36, 4, '#a9b1bb', s)


EXTRA = dict(cards=ic_cards, coin=ic_coin, shard=ic_shard, star_on=ic_dia(GOLD), star_off=ic_dia(TRACK), ad=ic_ad, plus=ic_plus,
             home=ic_home, retry=ic_retry, base=ic_base, patrol=ic_patrol,
             g_weapon=ic_g_weapon, g_helmet=ic_g_helmet, g_suit=ic_g_suit, g_shield=ic_g_shield, g_core=ic_g_core, g_boots=ic_g_boots,
             chip=ic_chip, energy=ic_energy, mission=ic_mission, calendar=ic_calendar, parts=ic_parts, crown=ic_crown, hand=ic_hand, tab_turret=ic_tab_turret)
ICONS.update(EXTRA)
ICON_LIST = {'pause': CREAM, 'play': CREAM, 'cards': CREAM, 'heart': RED, 'coin': GOLD, 'shard': SKY, 'star_on': GOLD, 'star_off': TRACK,
             'ad': CREAM, 'plus': CREAM, 'x': CREAM, 'lock': CREAM, 'home': CREAM, 'retry': CREAM, 'check': CREAM, 'skull': CREAM,
             'base': CREAM, 'patrol': CREAM, 'trophy': GOLD, 'bolt': GOLD, 'star': GOLD,
             'g_weapon': CREAM, 'g_helmet': CREAM, 'g_suit': CREAM, 'g_shield': CREAM, 'g_core': CREAM, 'g_boots': CREAM,
             'chip': CREAM, 'energy': CREAM, 'mission': CREAM, 'calendar': CREAM, 'parts': CREAM, 'crown': GOLD, 'hand': CREAM,
             'tab_turret': CREAM, 'gear': CREAM, 'sword': GOLD, 'sound': CREAM, 'music': CREAM, 'chest': ORANGE}


def export_icon():
    for n, f in ICON_LIST.items():
        out('Icon', n, Rr(f'<g transform="translate(8,8)">{ICONS[n](96, 96, f, 5)}</g>', 112, 112, A.WOB['icon']))
    print('icon', len(ICON_LIST))


if __name__ == '__main__':
    want = set(sys.argv[1:]) or {'bg', 'char', 'turret', 'enemy', 'fx', 'ui', 'icon'}
    for n in ('bg', 'char', 'turret', 'enemy', 'fx', 'ui', 'icon'):
        if n in want:
            globals()['export_' + n]()
