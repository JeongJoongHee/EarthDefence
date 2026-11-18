# -*- coding: utf-8 -*-
"""카피바라 지구를 지켜라! — 낙서풍 그림 (카치건 화풍 도구 doodle_kit 위에).

사용 (PowerShell 에서 — Bash 샌드박스에선 Edge 가 바로 끝나 그림이 안 나옴):
  $env:PYTHONUTF8='1'; python tools/earth_art.py sample
    → tools/_preview/sample_battle.png  (전투 화면 1080x1920)
    → tools/_preview/sample_levelup.png (레벨업 창)
    → tools/_preview/sample_parts.png   (가디언 · 터렛 6 · 적 모음)

2026-11-15 결정(제작_계획.html): 밤하늘 낙서풍 · 가디언 = 카피바라 대원(우주 헬멧, 귀는 헬멧 위로, 쌍권총)
· 적 = 운석(작은 돌 ~ 거대 행성까지 여러 종) · 외계 벌레 · 젤리 바이러스 · 외계 함선
· 1차 터렛 6 = 관통포 · 레이저 포 · 끈끈이 드론 · 벌집 · 유도 미사일 · 벼락탑
"""
import os, sys, math, random
from PIL import Image

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from doodle_kit import *          # noqa: F401,F403
import doodle_kit as dk

HERE = os.path.dirname(os.path.abspath(__file__))
PREV = os.path.join(HERE, '_preview')
FONT_URL = 'file:///' + os.path.join(HERE, 'fonts', 'Jua-Regular.ttf').replace('\\', '/')

# ------------------------------------------------------------------ 색
NIGHT, NIGHT2, NEBULA = '#2e3866', '#36427a', '#4a5796'
CAPY_BODY, CAPY_DARK, CAPY_BELLY, CAPY_FAR = '#c49a6e', '#a97f58', '#d9b88f', '#b9906a'
CAPY_INK, CAPY_SW = '#1c110a', 4.4
SUIT, SUIT2, SUIT_BELLY = '#86b7dd', '#6f9fc6', '#b3d6ef'
HELM, HELM2 = '#eef2f4', '#cfd8de'
STONE, STONE2, STONE_TOP = '#c9c1b3', '#b0a796', '#dcd5c8'
METAL, METAL2 = '#a9b1bb', '#8a929c'
# 속성 5 (파스텔) — 물리 · 빛 · 중력 · 불 · 번개
ELEM = dict(phys='#d9c19a', light='#8fd6ff', grav='#b9a6e0', fire='#f09a7e', bolt='#ffd966')
ELEM_NAME = dict(phys='물리', light='빛', grav='중력', fire='불', bolt='번개')
PURPLE, PURPLE2 = '#a68bd6', '#8a70bd'


def shade(hexc, k):
    h = hexc.lstrip('#'); r, g, b = (int(h[i:i + 2], 16) for i in (0, 2, 4))
    return '#%02x%02x%02x' % tuple(max(0, min(255, int(v * k))) for v in (r, g, b))


# ------------------------------------------------------------------ 글자 (주아)
def font_css():
    return f'<style>@font-face{{font-family:Jua;src:url("{FONT_URL}")}}</style>'


def T(x, y, s, size, fill='#ffffff', anchor='middle', outline=True, sw=None):
    size = size * 1.07
    sw = sw if sw is not None else max(3, size * .16)
    base = f'x="{x}" y="{y}" font-family="Jua" font-size="{size:.1f}" text-anchor="{anchor}"'
    if not outline:
        return f'<text {base} fill="{fill}">{s}</text>'
    sh = f'<text x="{x}" y="{y + size * .07:.1f}" font-family="Jua" font-size="{size:.1f}" text-anchor="{anchor}" fill="{INK}" stroke="{INK}" stroke-width="{sw}" stroke-linejoin="round">{s}</text>'
    return sh + f'<text {base} fill="{fill}" stroke="{INK}" stroke-width="{sw}" stroke-linejoin="round" paint-order="stroke">{s}</text>'


# ------------------------------------------------------------------ 11-16 1차 수정안(아트 퀄업): 명암 그라데이션 · 네온 빛 번짐
# 흔들림(wob)은 전체적으로 줄임 — 사용자 "투박함 · 이음새·마감을 매끄럽고 깔끔하게"
WOB = dict(char=0.5, turret=1.0, enemy=1.6, ui=1.0, bg=3.0, icon=0.6, fx=0.0)
_DEFS = {}


def mix(a, b, t):
    a, b = a.lstrip('#'), b.lstrip('#')
    ca = [int(a[i:i + 2], 16) for i in (0, 2, 4)]; cb = [int(b[i:i + 2], 16) for i in (0, 2, 4)]
    return '#%02x%02x%02x' % tuple(int(x + (y - x) * t) for x, y in zip(ca, cb))


def G(c, kind='v', lo=.84, hi=.28):
    """명암 칠: v = 위 밝게 → 아래 어둡게 · r = 왼쪽 위가 밝은 공 모양. fill="..." 에 그대로 넣음"""
    gid = f'g{kind}{c.lstrip("#")}{int(lo * 100)}{int(hi * 100)}'
    if gid not in _DEFS:
        top, bot = mix(c, '#ffffff', hi), shade(c, lo)
        if kind == 'r':
            _DEFS[gid] = (f'<radialGradient id="{gid}" cx=".36" cy=".3" r=".8"><stop offset="0" stop-color="{top}"/>'
                          f'<stop offset=".55" stop-color="{c}"/><stop offset="1" stop-color="{bot}"/></radialGradient>')
        else:
            _DEFS[gid] = (f'<linearGradient id="{gid}" x1="0" y1="0" x2="0" y2="1"><stop offset="0" stop-color="{top}"/>'
                          f'<stop offset=".5" stop-color="{c}"/><stop offset="1" stop-color="{bot}"/></linearGradient>')
    return f'url(#{gid})'


def glow(svg, std=6, op=1.0):
    """빛 번짐: 같은 모양을 흐리게 뒤에 깔고 위에 원래 모양"""
    fid = f'blur{int(std * 10)}'
    _DEFS.setdefault(fid, f'<filter id="{fid}" x="-100%" y="-100%" width="300%" height="300%"><feGaussianBlur stdDeviation="{std}"/></filter>')
    return f'<g filter="url(#{fid})" opacity="{op}">{svg}</g>'


def blur(svg, std=6, op=1.0):
    """흐리기만 (번짐 층)"""
    return glow(svg, std, op)


def D(svg):
    """그림에 쓰인 그라데이션·필터 정의를 앞에 붙임"""
    return '<defs>' + ''.join(_DEFS.values()) + '</defs>' + svg


def R(svg, w, h, wob, freq=0.022):
    return Image.fromarray(render(D(svg), w, h, wob=wob, freq=freq))


def piece(svg, w, h, k, wob, freq=0.022, pad=0):
    """로컬 w x h 그림을 k 배로 (SVG 안에서 키워 그림 → 흐리지 않음). pad = 위쪽 여유(로컬)"""
    return R(f'<g transform="scale({k}) translate(0,{pad})">{svg}</g>', int(w * k), int((h + pad) * k), wob, freq)


# ------------------------------------------------------------------ 가디언: 카피바라 대원 — 몸 + 따로 도는 레이저 소총
# 11-15 첫 판: 앞모습 2등신 우주인 · 11-16 오전: 옆모습 + 긴 팔로 소총을 위로
# 11-16 오후 사용자 의견 "길쭉한 팔이 부자연스러움 · 팔이 짧아도 되니 귀엽게 · 카치건과 비슷한 형태로"
#   → 카치건 카피바라 몸(BODY_D · SNOUT_D · Y자 코, 사용자 자기 게임 그림)을 그대로 쓰고 우주복 잠옷 + 유리 헬멧 + 산소통.
#   총은 카치건처럼 따로 그림(guardian_gun) — 뒤 손(=회전 중심)을 축으로 적 쪽으로 돌아감. 짧은 팔은 몸 그림에, 손은 총 그림에.
# 몸 로컬: 카치건 좌표 그대로 (발 가운데 x 72 · 발바닥 y 168.5) · 뒤 손 = (118, 112)
SUIT_W, SUIT_W2, SUIT_OR, SUIT_GR, SUIT_GR2 = '#f6f7f8', '#dfe4e8', '#f0a06a', '#9aa3ad', '#7f8892'
NEON = '#7ff0ff'          # 우주복 작은 등
GUN_NEON = '#ffd75e'      # 소총 에너지 빛 = 대장 탄 색(금빛 네온)
BODY_D = ('M38 156 C20 144 18 112 26 92 C30 74 32 52 42 36 C52 22 70 14 90 15 C108 16 121 24 127 38 C129.7 39.0 132.3 42.7 133.0 44.0 '
          'C134.6 48.0 136.2 56.0 136.2 60.0 C136.0 66.0 135.5 69.3 134.6 72.0 C133.0 76.0 130.1 80.0 125.1 84.0 C121.7 86.5 118.4 88.7 118.0 89.0 '
          'C125 108 122 138 106 154 C92 166 54 166 38 156 Z')
SNOUT_D = 'M116 43 C108 44.5 104.5 54 105.5 64 C106.5 74 110.5 80 119 80.5 C128 80.5 134 74 135 64 C136 52 128 42 116 43 Z'
G_W, G_H = 180, 176                 # 몸 그림 로컬 크기 (왼쪽 산소통까지)
G_OX = 14                           # 카치건 좌표 → 그림 좌표 x 이동 (산소통 자리)
G_CX, G_FOOT = 72 + G_OX, 168.5     # 발 가운데 · 발바닥
HAND = (118 + G_OX, 112)            # 뒤 손 = 총 회전 중심
# 총 그림 로컬: 회전 중심(뒤 손) = (0,0), 오른쪽 = 앞. 그림 범위 x -34~110, y -40~20 · 총구 (100,-10)
GUN_BOX = (-34, -40, 144, 60)
GUN_MUZ = (100, -10)
GUN_TIP = (HAND[0] + 70, HAND[1] - 70)   # 미리보기(전투 샘플)용: 45° 위로 겨눌 때 대략 총구


def capy_face_hole(sw):
    """잠옷 얼굴 구멍 (카치건 onesie 방식): 몸 모양 안으로 자른 얼굴 + 짙은 주둥이 + Y자 코·ㅅ 입 + 점 눈"""
    snout = F(SNOUT_D, CAPY_DARK)
    marks = L('M117 50.7 L122.8 56 L129.4 50.7 M122.8 56 L122.8 69.1', 2.4) + L('M118.1 73.6 L122.8 69.1 L126.7 73', 2.6)
    return (f'<clipPath id="gBodyClip"><path d="{BODY_D}"/></clipPath><g clip-path="url(#gBodyClip)">'
            f'<ellipse cx="104" cy="56" rx="35" ry="32" fill="{CAPY_BODY}" stroke="{INK}" stroke-width="{sw}"/>' + snout + '</g>'
            + marks + f'<circle cx="91.3" cy="40.2" r="2.9" fill="{INK}"/><circle cx="92.3" cy="39.2" r=".9" fill="#ffffff"/>')


def capy_guardian(sw=CAPY_SW):
    """몸 (총 빼고). 짧은 팔은 어깨 → 뒤 손(HAND)까지"""
    s = []
    # 등 산소통 (몸 뒤)
    s.append(rr(-6, 70, 28, 62, 10, G(SUIT_W2), sw) + rr(-2, 62, 20, 14, 6, G(SUIT_OR), sw * .9)
             + blur(C(8, 118, 5, NEON, 0), 2.5) + C(8, 118, 3.6, NEON, sw * .5))
    # 발 (장화)
    s.append(E(90, 161, 15, 8, G(SUIT_GR), sw) + E(54, 160, 15, 8, G(SUIT_GR2), sw))
    # 몸 = 하얀 우주복 + 허리 주황 띠 + 가슴 조종판
    s.append(P(BODY_D, G(SUIT_W, 'v', .86, .2), sw))
    s.append(f'<clipPath id="gBeltClip"><path d="{BODY_D}"/></clipPath><g clip-path="url(#gBeltClip)">'
             + F('M0 138 L160 138 L160 147 L0 147 Z', SUIT_OR) + L('M0 138 L160 138 M0 147 L160 147', 2.8) + '</g>')
    s.append(rr(46, 104, 30, 20, 6, G(SUIT_GR), sw * .8) + C(55, 114, 3.6, NEON, 2.2) + C(67, 114, 3.6, RED, 2.2))
    # 얼굴 구멍 + 귀 (사용자 규칙: 모자를 써도 귀는 위로 보이게)
    s.append(capy_face_hole(sw))
    s.append(P('M50 32 Q44 18 54 16 Q61 18 60 28 Z', G(CAPY_DARK, 'r'), sw) + P('M78 18 Q77 4 88 5 Q94 8 91 17 Z', G(CAPY_DARK, 'r'), sw))
    # 짧은 팔 (어깨 → 뒤 손) — 손은 총 그림에서 덮음
    s.append(limb('M92 100 Q104 112 118 112', 15, SUIT_W, sw) + L('M101 104 L104 116', 6) + L('M101 104 L104 116', 2.6, SUIT_OR))
    body = ''.join(s)
    # 유리 헬멧 (머리 둘레) + 목 테 + 안테나
    hx, hy, r = 92, 52, 50
    helm = (f'<circle cx="{hx}" cy="{hy}" r="{r}" fill="#cfeeff" opacity=".12"/>'
            + f'<path d="M{hx - 42} {hy + 14} Q{hx - 48} {hy - 8} {hx - 38} {hy - 24}" fill="none" stroke="#ffffff" stroke-width="5.5" stroke-linecap="round" opacity=".75"/>'
            + f'<circle cx="{hx - 8}" cy="{hy - 42}" r="3.4" fill="#ffffff" opacity=".85"/>'
            + f'<circle cx="{hx}" cy="{hy}" r="{r}" fill="none" stroke="{INK}" stroke-width="{sw}"/>'
            + f'<path d="M{hx - 44} {hy + 26} Q{hx} {hy + 46} {hx + 44} {hy + 26}" fill="none" stroke="{INK}" stroke-width="{sw + 6}" stroke-linecap="round"/>'
            + f'<path d="M{hx - 44} {hy + 26} Q{hx} {hy + 46} {hx + 44} {hy + 26}" fill="none" stroke="{SUIT_W2}" stroke-width="6" stroke-linecap="round"/>'
            + L(f'M{hx - 24} {hy - 44} L{hx - 34} {hy - 62}', sw) + blur(C(hx - 35, hy - 64, 6, '#ff9a8a', 0), 2.5) + C(hx - 35, hy - 64, 4.6, RED, sw * .7))
    return f'<g transform="translate({G_OX},0)">{body}{helm}</g>'.replace(INK, CAPY_INK)


def guardian_gun(sw=CAPY_SW):
    """레이저 소총 + 두 손 (회전 중심 = 뒤 손 (0,0), 오른쪽이 앞)"""
    s = P('M-30 -18 Q-34 -18 -34 -12 L-34 -4 Q-34 2 -28 2 L-4 0 L-4 -18 Z', G(SUIT_GR), sw)               # 개머리
    s += P('M-4 -4 L-6 10 Q-6 14 -1 14 L5 14 Q9 14 9 10 L10 -4 Z', G(SUIT_GR2), sw * .9)                 # 손잡이
    s += rr(-8, -23, 72, 25, 10, G(SUIT_W), sw)                                                         # 몸통
    s += rr(10, -36, 26, 11, 5, G(SUIT_GR), sw * .8) + C(36, -30.5, 3.4, NEON, sw * .5)                 # 조준경
    s += blur(rr(2, -17, 50, 12, 6, GUN_NEON, 0), 3, .9) + rr(2, -17, 50, 12, 6, GUN_NEON, sw * .65)    # 에너지 칸
    s += F('M8 -14 L46 -14 L46 -11.5 L8 -11.5 Z', '#ffffff', .95) + F('M-2 -20 L58 -20 L58 -17.5 L-2 -17.5 Z', '#ffffff', .5)
    s += rr(62, -17, 28, 14, 5, G(METAL), sw) + L('M71 -17 L71 -3 M80 -17 L80 -3', sw * .55)            # 총열
    s += rr(87, -21, 10, 22, 4, G(SUIT_OR), sw)                                                         # 총구 테
    s += blur(C(100, -10, 8, GUN_NEON, 0), 4, .95) + C(99, -10, 4.6, '#ffffff', sw * .45)               # 총구 빛
    s += C(52, 4, 9, G(SUIT_GR, 'r'), sw)                                                               # 앞 손 (총열 아래)
    s += C(0, 2, 10, G(SUIT_GR, 'r'), sw) + L('M-4 -4 Q2 -8 7 -3', 2.4)                                 # 뒤 손 (회전 중심)
    return s.replace(INK, CAPY_INK)


def guardian_img(k):
    return piece(capy_guardian(), G_W, G_H, k, WOB['char'])


def guardian_gun_img(k):
    x0, y0, w, h = GUN_BOX
    return piece(f'<g transform="translate({-x0},{-y0})">{guardian_gun()}</g>', w, h, k, WOB['char'])


def guardian_full_svg(ang=-50):
    """몸 + 총(ang 도 위로) 한 장 — 카드·로비 아이콘용"""
    return capy_guardian() + f'<g transform="translate({HAND[0]},{HAND[1]}) rotate({ang})">{guardian_gun()}</g>'


def guardian_float_svg():
    """7차 수정안 "장비 탭 카피바라는 2D 로 둥실둥실 · 총 없이 우주복": 몸 + 두 손(총 대신) + 산소통 아래 작은 분사 빛"""
    hx, hy = HAND
    s = blur(E(G_OX + 10, 146, 9, 14, NEON, 0), 4, .8) + E(G_OX + 10, 142, 5, 9, '#e9fdff', 0)     # 산소통 분사
    s += capy_guardian()
    s += (C(hx, hy, 10, G(SUIT_GR, 'r'), CAPY_SW) + L(f'M{hx - 4} {hy - 6} Q{hx + 2} {hy - 10} {hx + 7} {hy - 5}', 2.4)).replace(INK, CAPY_INK)
    return s


def guardian_float_img(k):
    return piece(f'<g transform="translate(10,30)">{guardian_float_svg()}</g>', G_W + 20, G_H + 40, k, WOB['char'])


def guardian_ui_img(k, ang=-50):
    return piece(f'<g transform="translate(0,40)">{guardian_full_svg(ang)}</g>', G_W + 40, G_H + 40, k, WOB['char'])


# ------------------------------------------------------------------ 터렛 (로컬 200x220, 가운데 x 100, 바닥 y 212)
def neon_dot(x, y, r, c):
    """빛나는 작은 등 (번짐 + 흰 가운데)"""
    return blur(C(x, y, r * 1.7, c, 0), r * .7, .9) + C(x, y, r, c, max(2.4, r * .45)) + C(x - r * .25, y - r * .25, r * .38, '#ffffff', 0)


def pedestal(col):
    """11-16: 받침 명암 + 속성 색 빛 띠(가운데 빛 점)"""
    return (f'<ellipse cx="100" cy="214" rx="70" ry="9" fill="#000" opacity=".2"/>'
            + P('M38 212 L54 162 L146 162 L162 212 Z', G(STONE, 'v', .8, .2), 5.5)
            + blur(F('M47 186 L153 186 L157 198 L43 198 Z', col), 3, .8)
            + F('M47 186 L153 186 L157 198 L43 198 Z', mix(col, '#ffffff', .25)) + L('M47 186 L153 186 M43 198 L157 198', 3.6)
            + F('M50 190 L150 190 L151 192 L49 192 Z', '#ffffff', .7)
            + E(100, 162, 50, 13, G(STONE_TOP, 'v', .9, .3), 5.5) + F('M62 158 Q100 150 138 158 Q100 154 62 158 Z', '#ffffff', .55))


def t_rail_base():
    c = ELEM['phys']
    return (pedestal(c) + rr(86, 34, 28, 108, 9, G(METAL), 5.5) + rr(80, 26, 40, 20, 7, G(METAL2), 5.5)
            + F('M92 50 L97 50 L97 128 L92 128 Z', '#ffffff', .5) + L('M86 70 L114 70 M86 96 L114 96', 3.2))


RAIL_WHEEL = (100, 138, 34)          # 도는 바퀴 cx, cy, r


def t_rail_wheel():
    """도는 바퀴 (돌아가는 게 보이게 바퀴살 4 + 홈 4)"""
    c = ELEM['phys']; cx, cy, r = RAIL_WHEEL
    s = C(cx, cy, r, G(c, 'r'), 5.5)
    for i in range(4):
        a = math.radians(45 + i * 90); a2 = math.radians(i * 90)
        s += L(f'M{cx + 11 * math.cos(a):.1f} {cy + 11 * math.sin(a):.1f} L{cx + 27 * math.cos(a):.1f} {cy + 27 * math.sin(a):.1f}', 3.4, shade(c, .7))
        s += C(cx + 25 * math.cos(a2), cy + 25 * math.sin(a2), 3.6, shade(c, .72), 0)
    return s + C(cx, cy, 9, G(METAL2, 'r'), 3.6)


def t_rail_hl():
    """바퀴 위 반짝 (돌지 않음)"""
    cx, cy, r = RAIL_WHEEL
    return F(f'M{cx - 26} {cy - 6} Q{cx - 20} {cy - 26} {cx - 2} {cy - 30} Q{cx - 16} {cy - 18} {cx - 20} {cy - 2} Z', '#ffffff', .45)


def t_rail():
    return t_rail_base() + blur(C(100, 34, 9, '#fff1cf', 0), 4, .9) + t_rail_wheel() + t_rail_hl()


# 11-16 밤 "터렛이 작동하는 느낌" — 움직일 조각을 나눔: 받침(_base) + 조각(같은 캔버스 · 같은 피벗이라 겹치면 원래 그림)
LASER_CRYSTAL_C = (100, 62)          # 수정 가운데 (고리도 여기)
LASER_RING = (100, 66, 46, 12)       # 빛 고리 cx, cy, rx, ry


def t_laser_base():
    c = ELEM['light']
    return (pedestal(c) + rr(88, 112, 24, 50, 7, G(METAL), 5.5)
            + E(100, 104, 44, 26, G(HELM), 5.5) + E(100, 98, 30, 14, G(c, 'v', .8, .4), 4.5))


def t_laser_crystal():
    c = ELEM['light']
    crystal = 'M100 24 L119 66 L100 98 L81 66 Z'
    return (blur(P(crystal, c, 0), 9, .85)
            + P(crystal, G(c, 'v', .78, .55), 5.5) + L('M81 66 L119 66 M100 24 L100 98', 3)
            + F('M92 50 L99 34 L97 62 Z', '#ffffff', .85) + neon_dot(100, 66, 5, '#e6fbff'))


def laser_ring(front):
    """수정 둘레 빛 고리 반쪽 (흰색 → 코드에서 색 · 빛 더하기). front = 아래 반쪽(수정 앞)"""
    cx, cy, rx, ry = LASER_RING
    d = f'M{cx - rx} {cy} A{rx} {ry} 0 0 {0 if front else 1} {cx + rx} {cy}'
    return blur(f'<path d="{d}" fill="none" stroke="#ffffff" stroke-width="7"/>', 3, .8) + f'<path d="{d}" fill="none" stroke="#ffffff" stroke-width="2.4"/>'


def t_laser():
    return t_laser_base() + t_laser_crystal()


DRONE_PROP = ((36, 58), (164, 58))   # 프로펠러 가운데
DRONE_EYE = (100, 86)


def t_drone_base():
    c = ELEM['grav']
    return pedestal(c) + E(100, 154, 30, 7, shade(c, .85), 4)


def t_drone_body():
    """둥실 움직이는 몸 (프로펠러 · 눈동자 빼고)"""
    c = ELEM['grav']
    return (L('M60 82 L36 62 M140 82 L164 62', 6)
            + P('M58 92 Q58 60 100 58 Q142 60 142 92 Q142 112 100 114 Q58 112 58 92 Z', G(c, 'r'), 5.5)
            + E(100, 84, 20, 14, '#fff4dc', 4.5)
            + P('M84 112 Q84 128 90 128 Q96 128 94 112 Z', G(c), 4) + P('M110 112 Q110 136 116 136 Q122 136 120 112 Z', G(c), 4)
            + blur(L('M100 122 L100 150', 5, '#d7b8ff'), 3) + L('M100 122 L100 148', 2.6, '#ffffff'))


def t_drone_prop(x=36, y=58):
    return E(x, y, 28, 8, G(HELM2), 4.5)


def t_drone_pupil(x=100, y=88):
    return blur(C(x, y, 9, '#d7b8ff', 0), 3, .9) + f'<circle cx="{x}" cy="{y}" r="6" fill="{INK}"/>' + C(x - 2, y - 3, 2, '#ffffff', 0)


def t_drone_lid():
    """눈 깜빡임 (눈 흰자를 몸 색으로 덮고 감은 눈 줄)"""
    c = ELEM['grav']
    return E(100, 84, 20, 14, G(c, 'r'), 4.5) + L('M84 86 Q100 94 116 86', 3.2)


def t_drone():
    return t_drone_base() + t_drone_body() + t_drone_prop(*DRONE_PROP[0]) + t_drone_prop(*DRONE_PROP[1]) + t_drone_pupil()


def bee(x, y, k=1.0):
    return (f'<g transform="translate({x},{y}) scale({k})">' + E(-4, -12, 9, 7, '#ffffff', 3) + E(6, -12, 9, 7, '#ffffff', 3)
            + E(0, 0, 15, 11, G(GOLD, 'r'), 3.6) + L('M-4 -10 L-4 10 M5 -10 L5 10', 3.6) + C(13, 0, 6, INK, 0) + '</g>')


HIVE_DOOR = (100, 150)


def t_hive_base():
    c = GOLD
    return (pedestal(ELEM['grav']) + P('M46 162 Q42 64 100 58 Q158 64 154 162 Z', G(c, 'r', .82, .3), 5.5)
            + L('M50 128 Q100 116 150 128 M47 96 Q100 82 153 96', 4.4, GOLD2) + L('M70 66 Q100 58 130 66', 4.4, GOLD2)
            + P('M84 162 L84 140 Q100 124 116 140 L116 162 Z', TRACK, 4.5))


def t_hive():
    return (t_hive_base() + blur(P('M84 162 L84 140 Q100 124 116 140 L116 162 Z', '#ffcf6b', 0), 5, .8)
            + P('M84 162 L84 140 Q100 124 116 140 L116 162 Z', TRACK, 4.5)
            + bee(42, 48, .9) + bee(156, 34, .8) + bee(168, 100, .7))


def missile(x, y, k=1.0, rot=0):
    return (f'<g transform="translate({x},{y}) rotate({rot}) scale({k})">'
            + P('M-12 10 L-24 30 L-12 26 Z', G(RED), 3.6) + P('M12 10 L24 30 L12 26 Z', G(RED), 3.6)
            + rr(-12, -34, 24, 64, 11, G('#fff4dc'), 4.4) + P('M-12 -26 Q0 -54 12 -26 Z', G(RED), 4.4)
            + F('M-7 -20 L-3 -20 L-3 18 L-7 18 Z', '#ffffff', .7) + L('M-12 6 L12 6', 3.6) + '</g>')


MISSILE_DOTS = ((76, 136), (124, 136))


def t_missile_base():
    c = ELEM['fire']
    return (pedestal(c) + rr(50, 104, 100, 56, 12, G(c), 5.5) + F('M58 112 L142 112 L142 117 L58 117 Z', '#ffffff', .45)
            + neon_dot(76, 136, 7, '#ffb08a') + neon_dot(124, 136, 7, '#ffb08a'))


def t_missile():
    return missile(78, 84) + missile(122, 76) + t_missile_base()


BOLT_ORB = (100, 52, 26)             # 벼락 구슬 cx, cy, r


def t_bolt_base():
    c = ELEM['bolt']
    return (pedestal(c) + rr(88, 66, 24, 98, 8, G(STONE2), 5.5) + F('M93 72 L97 72 L97 158 L93 158 Z', '#ffffff', .4)
            + E(100, 138, 34, 9, G(c), 4.5) + E(100, 114, 30, 8, G(c), 4.5) + E(100, 90, 26, 7, G(c), 4.5))


def t_bolt_orb():
    return (blur(C(100, 52, 34, '#fff2b8', 0), 9, .85)
            + C(100, 52, 26, G('#fff2b8', 'r', .9, .7), 5.5) + F('M86 44 Q92 32 104 32 Q94 38 92 50 Z', '#ffffff', .9))


def t_bolt():
    c = ELEM['bolt']
    zl, zr = 'M128 30 L140 40 L134 44 L148 56', 'M72 28 L62 40 L68 44 L54 54'
    return (t_bolt_base() + t_bolt_orb()
            + blur(L(zl, 6, c) + L(zr, 6, c), 3) + L(zl, 4.4) + L(zl, 2.2, '#fff6c8') + L(zr, 4.4) + L(zr, 2.2, '#fff6c8'))


def spark_arc(seed, w=64, h=28):
    """구슬에서 튀는 작은 번개 (왼쪽 가운데 = 시작점, 오른쪽으로 뻗음). 끝으로 갈수록 가늘게"""
    rnd = random.Random(seed); pts = [(3, h / 2)]; x = 3
    while x < w - 6:
        x = min(w - 4, x + rnd.uniform(9, 15)); pts.append((x, h / 2 + rnd.uniform(-9, 9)))
    s_glow, s_core = [], []
    for i in range(len(pts) - 1):
        k = 1 - i / (len(pts) - 1) * .7
        d = f'M{pts[i][0]:.1f} {pts[i][1]:.1f} L{pts[i + 1][0]:.1f} {pts[i + 1][1]:.1f}'
        s_glow.append(L(d, 7 * k, '#ffffff')); s_core.append(L(d, 2.4 * k, '#ffffff'))
    return blur(''.join(s_glow), 2.2, .7) + ''.join(s_core)


# ------------------------------------------------------------------ 2차 터렛 5종 (11-16) — 1차와 같은 받침 · 같은 캔버스 200x222
# 쾅쾅 대포: 둥근 몸 + 굵은 포신(쏘면 뒤로 밀림) · 집중 빔: 받침대 + 도는 렌즈 고리 · 소용돌이포: 그릇 + 도는 소용돌이 원반
# 불바퀴: 아치 틀 + 도는 불바퀴 · 찌릿 코일: 구리 코일 기둥 + 고리 + 위 구슬(작은 번개)
CANNON_MUZ = (100, 34)


def t_cannon_base():
    c = ELEM['phys']
    return (pedestal(c) + C(100, 128, 42, G(METAL, 'r'), 5.5) + rr(64, 132, 72, 26, 10, G(METAL2), 5)
            + C(78, 145, 4, shade(METAL2, .7), 0) + C(122, 145, 4, shade(METAL2, .7), 0)
            + F('M74 104 Q86 92 102 92 Q88 100 82 112 Z', '#ffffff', .55) + neon_dot(100, 122, 6, '#ffd9a0'))


def t_cannon_barrel():
    c = ELEM['phys']
    return (rr(76, 44, 48, 62, 12, G(c, 'v', .78, .3), 5.5) + L('M76 64 L124 64 M76 84 L124 84', 3.2)
            + rr(68, 26, 64, 24, 10, G(METAL), 5.5) + E(100, 34, 20, 7, INK, 0) + F('M84 50 L90 50 L90 100 L84 100 Z', '#ffffff', .45))


def t_cannon():
    return t_cannon_barrel() + t_cannon_base()


FOCUS_C = (100, 70)


def t_focus_base():
    c = ELEM['light']
    return (pedestal(c) + rr(90, 98, 20, 66, 7, G(METAL), 5.5) + P('M70 104 Q100 120 130 104 L126 96 Q100 108 74 96 Z', G(METAL2), 4.5)
            + L('M74 96 L66 70 M126 96 L134 70', 6) + C(66, 70, 6, G(METAL2, 'r'), 4) + C(134, 70, 6, G(METAL2, 'r'), 4))


def t_focus_spin():
    """도는 렌즈: 바깥 고리(살 3개) + 가운데 빛나는 렌즈"""
    c = ELEM['light']; cx, cy = FOCUS_C
    s = C(cx, cy, 34, 'none', 6) + C(cx, cy, 34, 'none', 0)
    s = f'<circle cx="{cx}" cy="{cy}" r="34" fill="none" stroke="{INK}" stroke-width="13"/><circle cx="{cx}" cy="{cy}" r="34" fill="none" stroke="{mix(c, "#ffffff", .2)}" stroke-width="6"/>'
    for i in range(3):
        a = math.radians(-90 + i * 120)
        s += C(cx + 34 * math.cos(a), cy + 34 * math.sin(a), 7, G(METAL2, 'r'), 4)
    return (s + blur(C(cx, cy, 20, '#c8f6ff', 0), 6, .9) + C(cx, cy, 18, G('#c8f6ff', 'r', .85, .7), 5)
            + F(f'M{cx - 10} {cy - 6} Q{cx - 6} {cy - 14} {cx + 2} {cy - 14} Q{cx - 4} {cy - 9} {cx - 6} {cy - 2} Z', '#ffffff', .9))


def t_focus():
    return t_focus_base() + t_focus_spin()


VORTEX_C = (100, 74)


def t_vortex_base():
    c = ELEM['grav']
    return (pedestal(c) + P('M48 104 Q52 160 100 160 Q148 160 152 104 Z', G(c, 'v', .8, .3), 5.5)
            + E(100, 104, 52, 14, G(shade(c, .75), 'v', .8, .4), 5) + neon_dot(76, 136, 5, '#e0d4ff') + neon_dot(124, 136, 5, '#e0d4ff'))


def t_vortex_spin():
    """도는 소용돌이 원반 (팔 4개)"""
    c = ELEM['grav']; cx, cy = VORTEX_C
    s = C(cx, cy, 40, G(mix(c, '#ffffff', .35), 'r', .85, .5), 5.5)
    for i in range(4):
        a = i * 90
        s += f'<path d="M{cx} {cy} Q{cx + 30} {cy - 6} {cx + 34} {cy - 30}" transform="rotate({a} {cx} {cy})" fill="none" stroke="{shade(c, .6)}" stroke-width="6" stroke-linecap="round"/>'
    return s + blur(C(cx, cy, 12, '#f2ecff', 0), 4, .9) + C(cx, cy, 9, '#f6f2ff', 3.6)


def t_vortex():
    return t_vortex_base() + t_vortex_spin()


WHEEL_C = (100, 76)


def t_wheel_base():
    c = ELEM['fire']
    return (pedestal(c) + P('M46 162 L46 92 Q46 34 100 34 Q154 34 154 92 L154 162 L136 162 L136 92 Q136 52 100 52 Q64 52 64 92 L64 162 Z', G(shade(c, .9), 'v', .82, .3), 5.5)
            + neon_dot(55, 120, 5, '#ffc0a0') + neon_dot(145, 120, 5, '#ffc0a0'))


def t_wheel_spin():
    """도는 불바퀴: 바퀴 + 살 + 불꽃 끝 6개"""
    c = ELEM['fire']; cx, cy = WHEEL_C
    s = ''
    for i in range(6):
        a = math.radians(i * 60)
        x, y = cx + 34 * math.cos(a), cy + 34 * math.sin(a)
        s += f'<path d="M{x:.1f} {y:.1f} q{8 * math.cos(a + 1.2):.1f} {8 * math.sin(a + 1.2):.1f} {14 * math.cos(a + .5):.1f} {14 * math.sin(a + .5):.1f}" fill="none" stroke="#ffb35c" stroke-width="9" stroke-linecap="round"/>'
    s = blur(s, 2.5, .9) + s
    s += C(cx, cy, 30, G('#ffb36b', 'r', .8, .45), 5.5)
    for i in range(3):
        s += L(f'M{cx - 26 * math.cos(math.radians(i * 60)):.1f} {cy - 26 * math.sin(math.radians(i * 60)):.1f} L{cx + 26 * math.cos(math.radians(i * 60)):.1f} {cy + 26 * math.sin(math.radians(i * 60)):.1f}', 4.4, shade('#ffb36b', .6))
    return s + C(cx, cy, 9, G('#fff0c0', 'r'), 4)


def t_wheel():
    return t_wheel_base() + t_wheel_spin()


def t_tesla_base():
    c = ELEM['bolt']; cu = '#e0a060'
    s = pedestal(c) + rr(84, 72, 32, 92, 10, G(cu, 'v', .8, .3), 5.5)
    for y in range(80, 160, 13):
        s += L(f'M86 {y} L114 {y + 7}', 3.2, shade(cu, .55))
    s += E(100, 74, 38, 11, G(METAL, 'v', .85, .35), 5) + E(100, 74, 22, 5, shade(METAL2, .75), 0)
    return s


def t_tesla_orb():
    return (blur(C(100, 46, 28, '#fff2b8', 0), 8, .85) + C(100, 46, 21, G('#fff2b8', 'r', .9, .7), 5)
            + L('M92 38 L100 48 L96 50 L106 58', 3, shade(GOLD2, .8)) + F('M88 40 Q92 30 102 30 Q94 34 92 44 Z', '#ffffff', .9))


def t_tesla():
    return t_tesla_base() + t_tesla_orb()


TURRETS = [  # id, 이름, 속성, 그림
    ('rail', '관통포', 'phys', t_rail), ('laser', '레이저 포', 'light', t_laser), ('drone', '끈끈이 드론', 'grav', t_drone),
    ('hive', '벌집', 'grav', t_hive), ('missile', '유도 미사일', 'fire', t_missile), ('bolt', '벼락탑', 'bolt', t_bolt),
    # 2차 (11-16)
    ('cannon', '쾅쾅 대포', 'phys', t_cannon), ('focus', '집중 빔', 'light', t_focus), ('vortex', '소용돌이포', 'grav', t_vortex),
    ('wheel', '불바퀴', 'fire', t_wheel), ('tesla', '찌릿 코일', 'bolt', t_tesla)]
TUR = {t[0]: t for t in TURRETS}


def turret_img(tid, k):
    return piece(TUR[tid][3](), 200, 222, k, WOB['turret'], .018)


# ------------------------------------------------------------------ 적 (로컬 220x220, 가운데 110,110 — 앞을 봄, 아래(기지)로 내려옴)
def blob(cx, cy, r, seed=1, n=9, j=.14):
    """울퉁불퉁한 돌 모양 (닫힌 매끈한 곡선)"""
    rnd = random.Random(seed); pts = []
    for i in range(n):
        a = 2 * math.pi * i / n + rnd.uniform(-.15, .15); q = r * (1 + rnd.uniform(-j, j))
        pts.append((cx + q * math.cos(a), cy + q * math.sin(a)))
    mid = [((pts[i][0] + pts[(i + 1) % n][0]) / 2, (pts[i][1] + pts[(i + 1) % n][1]) / 2) for i in range(n)]
    d = f'M{mid[-1][0]:.1f} {mid[-1][1]:.1f} ' + ' '.join(f'Q{pts[i][0]:.1f} {pts[i][1]:.1f} {mid[i][0]:.1f} {mid[i][1]:.1f}' for i in range(n)) + ' Z'
    return d


def face(cx, cy, s, angry=True, mouth='frown'):
    """동그란 흰 눈 2 (눈동자는 아래 = 기지 쪽) + 화난 눈썹 + 입"""
    r = s * .2; dx = s * .3
    out = C(cx - dx, cy, r, '#fffbe8', max(3, s * .045)) + C(cx + dx, cy, r, '#fffbe8', max(3, s * .045))
    out += f'<circle cx="{cx - dx}" cy="{cy + r * .3}" r="{r * .45}" fill="{INK}"/><circle cx="{cx + dx}" cy="{cy + r * .3}" r="{r * .45}" fill="{INK}"/>'
    if angry:
        out += L(f'M{cx - dx - r} {cy - r * 1.5} L{cx - dx + r * .9} {cy - r * .9} M{cx + dx + r} {cy - r * 1.5} L{cx + dx - r * .9} {cy - r * .9}', max(3, s * .05))
    my = cy + s * .36
    if mouth == 'frown':
        out += L(f'M{cx - s * .12} {my} Q{cx} {my - s * .08} {cx + s * .12} {my}', max(2.6, s * .04))
    elif mouth == 'teeth':
        out += P(f'M{cx - s * .22} {my - s * .04} Q{cx} {my + s * .16} {cx + s * .22} {my - s * .04} Z', '#5a2b2b', max(3, s * .045))
        out += F(f'M{cx - s * .14} {my - s * .02} L{cx - s * .08} {my + s * .06} L{cx - s * .02} {my} Z M{cx + s * .14} {my - s * .02} L{cx + s * .08} {my + s * .06} L{cx + s * .02} {my} Z', '#fffbe8')
    return out


def craters(cx, cy, r, col, seed=3, n=4):
    rnd = random.Random(seed); s = []
    for _ in range(n):
        a = rnd.uniform(0, 2 * math.pi); d = rnd.uniform(.35, .7) * r; q = rnd.uniform(.1, .2) * r
        s.append(f'<ellipse cx="{cx + d * math.cos(a):.1f}" cy="{cy + d * math.sin(a):.1f}" rx="{q:.1f}" ry="{q * .75:.1f}" fill="{col}"/>')
    return ''.join(s)


def flame(cx, cy, r, length, c1='#ffb36b', c2='#ffe08a'):
    """떨어지는 운석의 꼬리 (위쪽으로)"""
    top = cy - length
    outer = f'M{cx - r * .95} {cy} Q{cx - r * .9} {cy - length * .45} {cx - r * .25} {top + length * .15} Q{cx} {top} {cx + r * .1} {top + length * .1} Q{cx + r * .9} {cy - length * .5} {cx + r * .95} {cy} Z'
    return (blur(F(outer, c1), 7, .75) + P(outer, G(c1, 'v', .95, .45), 4.5)
            + F(f'M{cx - r * .55} {cy} Q{cx - r * .45} {cy - length * .4} {cx} {top + length * .35} Q{cx + r * .45} {cy - length * .4} {cx + r * .55} {cy} Z', c2))


def rock(r, col, seed, crater=True, face_s=None, extra='', cx=110, cy=110):
    s = P(blob(cx, cy, r, seed), G(col, 'r', .78, .3), 5.5)
    s += F(blob(cx + r * .12, cy + r * .14, r * .8, seed + 9), shade(col, .9), .3)
    if crater:
        s += craters(cx, cy, r, shade(col, .82), seed)
    s += F(f'M{cx - r * .6} {cy - r * .25} Q{cx - r * .5} {cy - r * .7} {cx - r * .05} {cy - r * .72} Q{cx - r * .4} {cy - r * .5} {cx - r * .45} {cy - r * .2} Z', '#ffffff', .35)
    s += extra
    s += face(cx, cy + r * .05, face_s or r * 1.0)
    return s


MY = 150   # 운석 몸 가운데 y (위로 꼬리 자리)


def m_pebble():          # 1 돌멩이 운석 — 작고 빠름
    return flame(110, MY + 10, 30, 150) + rock(34, '#c9b29a', 2, cy=MY)


def m_fire():            # 2 불꼬리 운석 — 기본
    return flame(110, MY + 14, 52, 150) + rock(52, '#a98568', 5, cy=MY)


def m_ice():             # 3 얼음 혜성 — 느리게, 맞으면 주변 둔화(안)
    return flame(110, MY + 12, 50, 152, '#cfeeff', '#ffffff') + rock(48, '#9fd3ef', 7, cy=MY, extra=F(f'M84 {MY - 28} L94 {MY - 44} L98 {MY - 22} Z M126 {MY + 22} L140 {MY + 12} L136 {MY + 32} Z', '#ffffff', .8))


def m_iron():            # 4 쇠 운석 — 단단함(물리 저항)
    plates = F(f'M66 {MY + 6} L84 {MY + 30} L70 {MY + 40} Z M140 {MY - 30} L156 {MY - 6} L146 {MY - 2} Z', METAL2) + F(f'M92 {MY - 40} L128 {MY - 38} L124 {MY - 30} L96 {MY - 32} Z', '#ffffff', .75)
    return flame(110, MY + 12, 50, 148) + rock(50, METAL, 11, crater=False, cy=MY, extra=plates)


def m_crack():           # 5 쪼개지는 운석 — 부서지면 돌멩이 3개
    crack = f'M58 {MY - 6} L72 {MY + 2} L68 {MY + 16} M150 {MY - 22} L140 {MY - 8} L156 {MY + 4} M96 {MY + 52} L104 {MY + 38} L120 {MY + 50}'
    return flame(110, MY + 14, 54, 150) + rock(54, '#b48a6e', 13, cy=MY, extra=L(crack, 8) + L(crack, 3.6, '#ffd08a'))


def m_big():             # 엘리트: 큰 소행성 — 주위에 작은 돌이 돔
    s = P(blob(28, 40, 12, 3), '#c9b29a', 5) + P(blob(196, 176, 10, 4), '#c9b29a', 5)
    return s + rock(86, '#9c8c80', 17, face_s=80)


def m_planet():          # 보스: 떠돌이 행성 — 로컬 440x440
    cx = cy = 220; r = 150
    back = f'<g transform="rotate(-14 {cx} {cy})">' + L(f'M{cx - 215} {cy} A215 50 0 0 1 {cx + 215} {cy}', 30) + L(f'M{cx - 215} {cy} A215 50 0 0 1 {cx + 215} {cy}', 18, '#f3d7a0') + '</g>'
    front = f'<g transform="rotate(-14 {cx} {cy})">' + L(f'M{cx - 215} {cy} A215 50 0 0 0 {cx + 215} {cy}', 30) + L(f'M{cx - 215} {cy} A215 50 0 0 0 {cx + 215} {cy}', 18, '#f3d7a0') + '</g>'
    col = '#c3a0dc'
    body = (C(cx, cy, r, G(col, 'r', .78, .3), 6.5)
            + f'<clipPath id="plc"><circle cx="{cx}" cy="{cy}" r="{r}"/></clipPath><g clip-path="url(#plc)">'
            + F(f'M{cx - r} {cy - 70} Q{cx} {cy - 100} {cx + r} {cy - 60} L{cx + r} {cy - 34} Q{cx} {cy - 74} {cx - r} {cy - 44} Z', shade(col, .88))
            + F(f'M{cx - r} {cy + 64} Q{cx} {cy + 30} {cx + r} {cy + 74} L{cx + r} {cy + 104} Q{cx} {cy + 60} {cx - r} {cy + 96} Z', shade(col, .88))
            + craters(cx, cy, r, shade(col, .8), 21, 5) + '</g>'
            + F(f'M{cx - 110} {cy - 40} Q{cx - 100} {cy - 110} {cx - 30} {cy - 128} Q{cx - 80} {cy - 90} {cx - 92} {cy - 34} Z', '#ffffff', .4)
            + face(cx, cy - 36, 140, mouth='teeth'))
    return back + body + front


def b_moth(w='#cdb6e6'):            # 외계 나방 — 흔들리며 내려옴
    return (L('M100 64 Q88 34 74 36 M120 64 Q132 34 146 36', 4.4)
            + E(62, 92, 52, 34, G(w, 'r'), 5.5, -28) + E(158, 92, 52, 34, G(w, 'r'), 5.5, 28)
            + E(70, 140, 34, 24, G(shade(w, .92), 'r'), 5.5, 30) + E(150, 140, 34, 24, G(shade(w, .92), 'r'), 5.5, -30)
            + F('M44 84 a10 10 0 1 0 0.1 0 Z M176 84 a10 10 0 1 0 0.1 0 Z', '#b49ad6')
            + E(110, 112, 22, 44, G('#9a7f6a', 'r'), 5.5) + L('M92 100 L128 100 M92 116 L128 116', 4, '#7d6553')
            + C(110, 154, 24, G('#9a7f6a', 'r'), 5.5) + face(110, 152, 46, mouth='frown'))


def b_beetle(c='#8fbf6a'):          # 딱정벌레 — 단단한 껍질(보호막)
    legs = L('M62 80 L34 70 M58 110 L28 112 M64 140 L36 156 M158 80 L186 70 M162 110 L192 112 M156 140 L184 156', 6)
    return (legs + P('M110 30 Q170 34 170 100 Q170 150 110 156 Q50 150 50 100 Q50 34 110 30 Z', G(c, 'r', .78, .3), 5.5)
            + L('M110 32 L110 154', 5) + F('M70 60 Q80 40 100 38 Q86 52 80 76 Z', '#ffffff', .4)
            + C(110, 160, 30, G('#5f7f4a', 'r'), 5.5) + L('M110 132 L110 112', 5.5) + face(110, 162, 54))


def b_spider():          # 외계 거미 — 빠르게 기어 내려옴
    c = '#8a7bb0'
    legs = ''.join(L(f'M{110 + sx * 30} {100 + i * 14} Q{110 + sx * 76} {70 + i * 22} {110 + sx * 92} {120 + i * 26}', 6) for sx in (-1, 1) for i in range(4))
    return (legs + E(110, 76, 46, 40, G(shade(c, .9), 'r'), 5.5) + F('M90 60 Q110 50 130 60 Q110 70 90 60 Z', '#e8735f', .7)
            + C(110, 128, 40, G(c, 'r'), 5.5) + face(110, 126, 66) + C(92, 104, 5, '#fffbe8', 2.6) + C(128, 104, 5, '#fffbe8', 2.6))


def v_jelly(c='#b7dc8f'):           # 젤리 바이러스 — 가시 끝 혹
    s = []
    for i in range(9):
        a = 2 * math.pi * i / 9 - math.pi / 2; x1, y1 = 110 + 62 * math.cos(a), 110 + 62 * math.sin(a); x2, y2 = 110 + 86 * math.cos(a), 110 + 86 * math.sin(a)
        s.append(L(f'M{x1:.1f} {y1:.1f} L{x2:.1f} {y2:.1f}', 6) + C(x2, y2, 9, '#9cc874', 4.5))
    return ''.join(s) + P(blob(110, 110, 66, 31, 10, .06), G(c, 'r', .8, .35), 5.5) + F('M70 86 Q78 62 102 56 Q86 72 84 92 Z', '#ffffff', .55) + face(110, 112, 76)


def v_split():           # 분열 젤리 — 죽으면 작은 젤리 2
    c = '#f4a9b8'
    return (P(blob(76, 116, 52, 41, 9, .05), G(c, 'r', .8, .35), 5.5) + P(blob(146, 104, 56, 43, 9, .05), G(shade(c, 1.03), 'r', .8, .35), 5.5)
            + F('M126 72 Q136 60 152 60 Q140 68 136 80 Z', '#ffffff', .6)
            + face(70, 116, 52) + face(148, 104, 58, mouth='teeth'))


def s_ufo():             # 비행접시 — 공중 (일부 터렛만 맞힘 — 안)
    return (P('M70 92 Q72 46 110 44 Q148 46 150 92 Z', G('#cfeeff', 'r', .85, .5), 5.5) + C(110, 80, 22, '#a8d672', 4.5)
            + C(102, 78, 6, '#fffbe8', 2.6) + C(118, 78, 6, '#fffbe8', 2.6) + f'<circle cx="102" cy="80" r="2.6" fill="{INK}"/><circle cx="118" cy="80" r="2.6" fill="{INK}"/>'
            + E(110, 112, 92, 28, G(METAL), 5.5) + E(110, 104, 70, 12, HELM2, 0)
            + ''.join(neon_dot(x, 116, 6, GOLD) for x in (54, 82, 110, 138, 166))
            + F('M84 54 Q92 46 106 46 Q94 52 92 62 Z', '#ffffff', .8))


def s_drone():           # 정찰 드론 — 외눈
    return (L('M64 92 L30 70 M156 92 L190 70', 7) + E(30, 64, 30, 8, HELM2, 5) + E(190, 64, 30, 8, HELM2, 5)
            + C(110, 108, 52, G(STONE, 'r'), 5.5) + F('M70 92 Q80 64 106 60 Q88 78 86 98 Z', '#ffffff', .4)
            + C(110, 112, 26, '#fffbe8', 5) + C(110, 118, 12, INK, 0) + L('M84 82 L136 82', 5)
            + L('M110 160 L110 176', 5.5) + neon_dot(110, 180, 6, RED))


ENEMIES = [  # id, 이름, 계열, 그림, 크기(로컬)
    ('pebble', '돌멩이 운석', '운석', m_pebble, 220), ('fire', '불꼬리 운석', '운석', m_fire, 220), ('ice', '얼음 혜성', '운석', m_ice, 220),
    ('iron', '쇠 운석', '운석', m_iron, 220), ('crack', '쪼개지는 운석', '운석', m_crack, 220), ('big', '큰 소행성 (엘리트)', '운석', m_big, 220),
    ('planet', '떠돌이 행성 (보스)', '운석', m_planet, 440),
    ('moth', '외계 나방', '벌레', b_moth, 220), ('beetle', '딱정벌레', '벌레', b_beetle, 220), ('spider', '외계 거미', '벌레', b_spider, 220),
    ('jelly', '젤리 바이러스', '젤리', v_jelly, 220), ('split', '분열 젤리', '젤리', v_split, 220),
    ('ufo', '비행접시', '함선', s_ufo, 220), ('sdrone', '정찰 드론', '함선', s_drone, 220)]
ENE = {e[0]: e for e in ENEMIES}


def enemy_img(eid, k):
    e = ENE[eid]
    return piece(e[3](), e[4], e[4], k, WOB['enemy'], .018)


# ------------------------------------------------------------------ 배경 · 기지
W_, H_ = 1080, 1920
GROUND_Y = 1712


def sky(moon=True, plain=False):
    """moon = 하늘에 달 그리기 (7차: 전투 달은 따로 Bg/moon) · plain = 로비용 심플 (은하수 얼룩 · 고리 행성 없음)"""
    rnd = random.Random(8)
    _DEFS.setdefault('skyv', f'<linearGradient id="skyv" x1="0" y1="0" x2="0" y2="1"><stop offset="0" stop-color="{NIGHT}"/><stop offset=".72" stop-color="#34407a"/><stop offset="1" stop-color="#46538f"/></linearGradient>')
    s = [f'<rect width="{W_}" height="{H_}" fill="url(#skyv)"/>']
    for x, y, r in (() if plain else ((180, 420, 260), (860, 900, 300), (300, 1250, 240))):          # 은하수 얼룩 (테두리 없음)
        s.append(f'<g opacity=".45">{puff(x, y, r, NIGHT2, 0, 9, x)}</g>')
    for x, y, r in (() if plain else ((200, 380, 120), (820, 940, 140))):
        s.append(f'<g opacity=".35">{puff(x, y, r, NEBULA, 0, 7, y)}</g>')
    for _ in range(110):
        s.append(f'<circle cx="{rnd.randint(0, W_)}" cy="{rnd.randint(0, 1600)}" r="{rnd.uniform(1.6, 3.6):.1f}" fill="#fff4dc" opacity="{rnd.uniform(.4, .9):.2f}"/>')
    for _ in range(9):
        x, y, q = rnd.randint(40, W_ - 40), rnd.randint(80, 1450), rnd.randint(9, 15)
        s.append(P(f'M{x} {y - q} Q{x + 2} {y - 2} {x + q} {y} Q{x + 2} {y + 2} {x} {y + q} Q{x - 2} {y + 2} {x - q} {y} Q{x - 2} {y - 2} {x} {y - q} Z', '#fff4dc', 3))
    # 달 · 작은 고리 행성
    if plain: return ''.join(s)
    if moon: s.append(blur(C(915, 300, 96, '#f3e3c3', 0), 18, .35) + C(915, 300, 78, G('#f3e3c3', 'r', .88, .4), 5.5) + craters(915, 300, 78, '#e1cda3', 5, 4))
    if moon: s.append(f'<g transform="rotate(-18 110 1460)">' + L('M60 1460 A50 12 0 0 1 160 1460', 12) + L('M60 1460 A50 12 0 0 1 160 1460', 6, '#f3d7a0') + '</g>'
             + C(110, 1460, 30, G('#f4a9b8', 'r'), 5)
             + f'<g transform="rotate(-18 110 1460)">' + L('M60 1460 A50 12 0 0 0 160 1460', 12) + L('M60 1460 A50 12 0 0 0 160 1460', 6, '#f3d7a0') + '</g>')
    return ''.join(s)


def moon_rabbit(down):
    """달토끼 (2차 수정안 11-17): 달(지름 156px) 위에 앉아 절구에 방아를 찧음. down = 공이가 내려온 그림 (번갈아 → 찧는 모습)
    로컬 160x160 · 달 가운데 = (80, 80). 달 무늬처럼 보이게 달보다 조금 진한 색 + 얇은 선"""
    ink, body, dark = '#7a6248', '#cdb88a', '#b59f72'
    sw = 3.2
    s = ''
    # 절구 (왼쪽 아래)
    s += P('M30 112 L70 112 L64 138 L36 138 Z', dark, sw, ink=ink) + E(50, 112, 20, 6, '#a48f63', sw, ink=ink)
    # 귀 (뒤로 살짝 젖힘)
    s += E(104, 34, 7, 20, body, sw, -14, ink=ink) + E(118, 36, 7, 20, body, sw, 10, ink=ink)
    s += E(104, 36, 3, 13, '#e8c9b0', 0, -14) + E(118, 38, 3, 13, '#e8c9b0', 0, 10)
    # 몸 · 꼬리 · 머리
    s += C(132, 118, 9, body, sw, ink=ink)
    s += E(110, 112, 25, 28, body, sw, ink=ink)
    s += C(108, 70, 19, body, sw, ink=ink) + C(99, 68, 2.6, ink, 0) + E(92, 76, 3, 2, '#e8a9a0', 0)
    # 공이 (팔 + 자루 + 머리)
    if down:
        s += L('M96 98 L66 104', 6, ink) + L('M96 98 L66 104', 3.4, '#a48f63')
        s += rr(52, 94, 22, 18, 6, '#a48f63', sw, ) .replace(INK, ink)
        s += E(92, 98, 7, 6, body, sw, ink=ink)
    else:
        s += L('M98 92 L72 60', 6, ink) + L('M98 92 L72 60', 3.4, '#a48f63')
        s += f'<g transform="rotate(-38 66 54)">' + rr(54, 44, 24, 18, 6, '#a48f63', sw).replace(INK, ink) + '</g>'
        s += E(96, 92, 7, 6, body, sw, ink=ink)
    return s


def base_back():
    """돔 안 기지 건물 + 돔 안쪽 칠"""
    cx = W_ / 2
    return (f'<path d="M{cx - 330} {GROUND_Y} A330 290 0 0 1 {cx + 330} {GROUND_Y} Z" fill="{SKY}" opacity=".16"/>'
            + rr(cx - 170, GROUND_Y - 150, 120, 150, 14, G('#fff4dc', 'v', .86, .3), 5.5) + rr(cx + 50, GROUND_Y - 130, 120, 130, 14, G('#fff4dc', 'v', .86, .3), 5.5)
            + P(f'M{cx - 182} {GROUND_Y - 146} L{cx - 110} {GROUND_Y - 196} L{cx - 38} {GROUND_Y - 146} Z', G(ORANGE), 5.5)
            + P(f'M{cx + 38} {GROUND_Y - 126} L{cx + 110} {GROUND_Y - 172} L{cx + 182} {GROUND_Y - 126} Z', G(ORANGE), 5.5)
            + blur(rr(cx - 150, GROUND_Y - 118, 82, 42, 10, '#ffe28a', 0) + rr(cx + 70, GROUND_Y - 102, 82, 42, 10, '#ffe28a', 0), 8, .7)
            + rr(cx - 144, GROUND_Y - 112, 30, 30, 6, G(GOLD, 'v', .9, .5), 4.2) + rr(cx - 104, GROUND_Y - 112, 30, 30, 6, G(GOLD, 'v', .9, .5), 4.2)
            + rr(cx + 76, GROUND_Y - 96, 30, 30, 6, G(GOLD, 'v', .9, .5), 4.2) + rr(cx + 116, GROUND_Y - 96, 30, 30, 6, G(GOLD, 'v', .9, .5), 4.2)
            + L(f'M{cx} {GROUND_Y - 30} L{cx} {GROUND_Y - 236}', 6) + E(cx, GROUND_Y - 236, 36, 12, G(HELM), 4.5)
            + blur(C(cx, GROUND_Y - 254, 16, '#ff9a8a', 0), 6, .8) + C(cx, GROUND_Y - 254, 9, RED, 3.6))


def dome_front():
    cx = W_ / 2
    d = f'M{cx - 330} {GROUND_Y} A330 290 0 0 1 {cx + 330} {GROUND_Y}'
    return (blur(L(d, 18, '#9fe0ff'), 7, .55) + L(d, 11) + L(d, 5.5, '#d4f1ff')
            + f'<path d="M{cx - 270} {GROUND_Y - 110} A270 230 0 0 1 {cx - 120} {GROUND_Y - 248}" fill="none" stroke="#ffffff" stroke-width="10" stroke-linecap="round" opacity=".45"/>')



# ------------------------------------------------------------------ 6차 수정안 "기지 외형 이미지 수정": 외형마다 다른 돔 (예전 = 같은 돔에 색만)
# 돔 테두리 = 가로 반지름 330 · 세로 290 반원. 장식은 테두리를 따라 놓음. 유리 안쪽 칠은 아주 옅게(안의 대장 카피가 가려지지 않게).
DOME_RX, DOME_RY = 330, 290


def _arc_pt(t, k=1.0):
    """돔 테두리 위 점: t = 0(오른쪽 끝) ~ 180(왼쪽 끝) 도 · k = 반지름 배율"""
    a = math.radians(t)
    return W_ / 2 + DOME_RX * k * math.cos(a), GROUND_Y - DOME_RY * k * math.sin(a)


def _arc_d(k=1.0):
    cx = W_ / 2
    return f'M{cx - DOME_RX * k} {GROUND_Y} A{DOME_RX * k} {DOME_RY * k} 0 0 1 {cx + DOME_RX * k} {GROUND_Y}'


def _flower(x, y, r, petal, core=GOLD):
    s = ''
    for i in range(5):
        a = math.radians(i * 72 - 90)
        s += C(x + math.cos(a) * r * .62, y + math.sin(a) * r * .62, r * .5, petal, 3.4)
    return s + C(x, y, r * .36, core, 3.4)


def _star(x, y, r, fill=GOLD, sw=3.6, rot=0):
    pts = []
    for i in range(10):
        a = math.radians(rot - 90 + i * 36); rr_ = r if i % 2 == 0 else r * .45
        pts.append(f'{x + math.cos(a) * rr_:.1f} {y + math.sin(a) * rr_:.1f}')
    return P('M' + ' L'.join(pts) + ' Z', fill, sw)


def dome_variant(fid):
    """외형별 돔 앞 그림 (dome_front 와 같은 캔버스 · 같은 자리)"""
    cx = W_ / 2
    d = _arc_d()
    hl = f'<path d="M{cx - 270} {GROUND_Y - 110} A270 230 0 0 1 {cx - 120} {GROUND_Y - 248}" fill="none" stroke="#ffffff" stroke-width="10" stroke-linecap="round" opacity=".45"/>'

    def glass(c, op=.10):
        return f'<path d="{d} Z" fill="{c}" opacity="{op}"/>'

    def rim(glow, line, w=11):
        return blur(L(d, w + 8, glow), 7, .6) + L(d, w) + L(d, w * .5, line)

    if fid == 'prism':     # 무지개 돔 (부활): 테두리 안쪽 무지개 띠 3줄 + 꼭대기 깃털 한 쌍
        s = glass('#f3dcff', .12) + rim('#d9b8ff', '#f6e8ff', 12)
        for k, c in ((.955, '#ffb3c8'), (.92, '#ffe08a'), (.885, '#9fe0ff')):
            s += f'<path d="{_arc_d(k)}" fill="none" stroke="{c}" stroke-width="9" stroke-linecap="round" opacity=".85"/>'
        tx, ty = _arc_pt(90)
        for sgn in (-1, 1):
            s += P(f'M{tx} {ty - 4} Q{tx + sgn * 40} {ty - 70} {tx + sgn * 92} {ty - 52} Q{tx + sgn * 60} {ty - 30} {tx + sgn * 70} {ty - 14} Q{tx + sgn * 34} {ty - 14} {tx} {ty - 4} Z', G('#ffd0e6', 'v', .86, .3), 4.4)
            s += L(f'M{tx + sgn * 8} {ty - 10} Q{tx + sgn * 40} {ty - 40} {tx + sgn * 76} {ty - 46}', 3, '#e8a2c8')
        s += blur(C(tx, ty - 4, 16, '#ffd0ff', 0), 6, .8) + C(tx, ty - 4, 14, G('#fff4dc', 'r'), 4.4)
        return s + hl
    if fid == 'starsea':   # 별바다 장막 (별똥별): 남색 유리 + 안쪽 별 · 테두리 별 장식
        s = glass('#5868b8', .13) + rim('#9fb2ff', '#dfe5ff', 12)
        rnd = random.Random(31)
        for _ in range(16):
            t = rnd.uniform(18, 162); k = rnd.uniform(.3, .86)
            x, y = _arc_pt(t, k)
            s += f'<circle cx="{x:.1f}" cy="{y:.1f}" r="{rnd.uniform(2.4, 4.6):.1f}" fill="#fff4dc" opacity="{rnd.uniform(.5, .95):.2f}"/>'
        for t in (30, 62, 90, 118, 150):
            x, y = _arc_pt(t)
            s += blur(C(x, y, 16, '#cfd8ff', 0), 6, .8) + _star(x, y, 20 if t == 90 else 15, '#fff2a8', 3.6, t)
        x0, y0 = _arc_pt(140, .7)
        s += L(f'M{x0} {y0} l90 46', 5, '#fff4dc') + _star(x0 + 92, y0 + 47, 11, '#fff2a8', 3)
        return s + hl
    if fid == 'frost':     # 얼음 돔 (얼림): 두꺼운 얼음 테두리 + 고드름 + 눈 결정
        s = glass('#dff6ff', .14) + rim('#9fe8ff', '#eafcff', 16)
        for t in range(14, 170, 13):
            x, y = _arc_pt(t, .985); ln = 26 + (t * 7) % 22
            s += P(f'M{x - 9} {y} L{x + 9} {y} L{x} {y + ln} Z', G('#e6fbff', 'v', .9, .4), 3.4)
        for t, k, r in ((34, .74, 18), (146, .74, 18), (64, .9, 13), (116, .9, 13)):
            x, y = _arc_pt(t, k)
            for a in (0, 60, 120):
                s += f'<g transform="rotate({a} {x} {y})">' + L(f'M{x - r} {y} L{x + r} {y} M{x - r * .55} {y - 7} L{x - r * .3} {y} L{x - r * .55} {y + 7} M{x + r * .55} {y - 7} L{x + r * .3} {y} L{x + r * .55} {y + 7}', 3.4, '#ffffff') + '</g>'
        return s + hl
    if fid == 'thorn':     # 가시 돔 (반격): 초록 덩굴 테두리 + 바깥으로 뻗은 가시
        s = glass('#e8f4d8', .10)
        for t in range(10, 175, 11):
            x, y = _arc_pt(t); xo, yo = _arc_pt(t, 1.13)
            a = math.radians(t); nx, ny = -math.sin(a) * 12, -math.cos(a) * 12
            s += P(f'M{x + nx} {y + ny} L{xo} {yo} L{x - nx} {y - ny} Z', G('#c9e6a6', 'v', .86, .3), 3.8)
        s += rim('#b8e08f', '#d9efbf', 13)
        for t in range(16, 170, 22):
            x, y = _arc_pt(t, .975)
            s += E(x, y + 10, 14, 7, '#9dcf7a', 3, rot=-t + 45)
        return s + hl
    if fid == 'bloom':     # 꽃 돔 (회복): 분홍 테두리 + 꽃 · 잎이 테두리를 따라
        s = glass('#ffe4ec', .12) + rim('#ffb3c8', '#ffe1ea', 12)
        for i, t in enumerate(range(14, 175, 16)):
            x, y = _arc_pt(t)
            s += E(x + 14, y + 6, 13, 7, '#a9d68c', 3, rot=-t + 30)
            s += _flower(x, y, 20 if i % 2 == 0 else 15, '#ffc2d4' if i % 3 else '#fff4dc', GOLD if i % 3 else '#ffb3c8')
        return s + hl
    if fid == 'gold':      # 황금 돔 (골드): 굵은 금 테두리 + 리벳 + 꼭대기 왕관 보석
        s = glass('#fff2c8', .12) + blur(L(d, 26, '#ffd34d'), 8, .6) + L(d, 18) + L(d, 11, '#f7c948') + L(d, 4, '#fff1b0')
        for t in range(8, 176, 14):
            x, y = _arc_pt(t)
            s += C(x, y, 6.5, G('#fff4dc', 'r'), 3)
        tx, ty = _arc_pt(90)
        s += P(f'M{tx - 46} {ty - 6} L{tx - 50} {ty - 58} L{tx - 22} {ty - 30} L{tx} {ty - 66} L{tx + 22} {ty - 30} L{tx + 50} {ty - 58} L{tx + 46} {ty - 6} Z', G('#ffd34d', 'v', .86, .3), 4.6)
        s += C(tx, ty - 24, 9, '#ff9a8a', 3.4) + C(tx - 50, ty - 60, 6, '#8fd6ff', 3) + C(tx + 50, ty - 60, 6, '#8fd6ff', 3)
        return s + hl
    return dome_front()


def ground():
    rnd = random.Random(4)
    s = [P(f'M-10 {GROUND_Y - 6} Q270 {GROUND_Y - 34} 540 {GROUND_Y - 4} Q810 {GROUND_Y + 22} 1090 {GROUND_Y - 20} L1090 1930 L-10 1930 Z', G('#7da36d', 'v', .82, .12), 5.5),
         F(f'M-10 {GROUND_Y + 4} Q270 {GROUND_Y - 24} 540 {GROUND_Y + 6} Q810 {GROUND_Y + 32} 1090 {GROUND_Y - 10} L1090 {GROUND_Y + 2} Q810 {GROUND_Y + 40} 540 {GROUND_Y + 14} Q270 {GROUND_Y - 14} -10 {GROUND_Y + 14} Z', '#a9cf86', .55),
         F(f'M-10 {GROUND_Y + 60} Q300 {GROUND_Y + 40} 560 {GROUND_Y + 70} Q820 {GROUND_Y + 96} 1090 {GROUND_Y + 60} L1090 1930 L-10 1930 Z', '#6e9460')]
    for _ in range(16):
        x, y = rnd.randint(20, W_ - 20), rnd.randint(GROUND_Y + 30, 1890)
        s.append(L(f'M{x} {y} l7 -18 M{x + 10} {y} l0 -22 M{x + 20} {y} l-7 -18', 5, '#5f8a5c'))
    for _ in range(7):
        s.append(E(rnd.randint(30, W_ - 30), rnd.randint(GROUND_Y + 50, 1880), rnd.randint(12, 22), rnd.randint(8, 13), STONE, 4.5))
    return ''.join(s)


SLOT_X = (110, 268, 812, 970)


def slot_empty(x):
    return (f'<ellipse cx="{x}" cy="{GROUND_Y + 2}" rx="62" ry="15" fill="#000" opacity=".18"/>'
            + f'<ellipse cx="{x}" cy="{GROUND_Y - 4}" rx="58" ry="16" fill="#fff4dc" opacity=".25" stroke="{INK}" stroke-width="5" stroke-dasharray="14 10"/>'
            + L(f'M{x - 12} {GROUND_Y - 4} L{x + 12} {GROUND_Y - 4} M{x} {GROUND_Y - 16} L{x} {GROUND_Y + 8}', 6, '#fff4dc'))


# ------------------------------------------------------------------ 효과
def bullet(x, y, ang):
    return (f'<g transform="translate({x},{y}) rotate({ang})">' + '<path d="M-6 -6 L-56 0 L-6 6 Z" fill="#fff4dc" opacity=".6"/>'
            + rr(-10, -8, 26, 16, 8, GOLD, 4) + '</g>')


def beam(x0, y0, x1, y1, c, w=12):
    d = f'M{x0} {y0} L{x1} {y1}'
    return f'<g opacity=".35">{L(d, w * 2.4, c)}</g>' + L(d, w + 8) + L(d, w, c) + L(d, w * .35, '#ffffff')


def hit(x, y, r=34):
    return burst(x, y, r, 8, '#fff2b0', 5, .5) + burst(x, y, r * .45, 6, '#ffffff', 0, .5)


# ------------------------------------------------------------------ HUD
def hud_svg():
    s = [f'<g transform="translate(26,34)">{button(100, 100, ORANGE, ORANGE2, 22)}<g transform="translate(26,22)">{dk.ic_pause(48, 50, CREAM, 5)}</g></g>',
         f'<g transform="translate(140,46)">{button(134, 78, CREAM, CREAM2, 26)}</g>',
         rr(296, 30, 488, 108, 30, CREAM, 6),
         f'<g transform="translate(954,34)">{button(100, 100, ORANGE, ORANGE2, 22)}'
         + rr(30, 22, 36, 48, 7, CREAM2, 4.5) + rr(40, 30, 36, 48, 7, CREAM, 4.5) + '</g>',
         rr(30, 162, 1020, 40, 20, TRACK, 6), rrf(36, 168, 1008 * .62, 28, 14, '#8fd6ff'), rrf(44, 172, 1008 * .62 - 20, 8, 4, '#ffffff', .45)]
    # 기지 체력 (돔 위)
    s.append(f'<g transform="translate(410,1344)">{dk.ic_heart(52, 48, RED, 5)}</g>')
    # 가디언 충전 게이지
    s.append(rr(466, 1768, 148, 22, 11, TRACK, 5) + rrf(470, 1772, 140 * .7, 14, 7, GOLD))
    return ''.join(s)


def hud_text():
    return (T(207, 98, '×1.5', 38, fill=INK, outline=False)
            + T(540, 82, '스테이지 7', 42, fill=INK, outline=False) + T(540, 124, '02 : 14', 30, fill='#7a6a5c', outline=False)
            + T(540, 194, 'Lv 9', 32) + T(560, 1384, '2,840', 46))


# ------------------------------------------------------------------ 화면
def battle(with_hud=True):
    scr = Image.new('RGBA', (W_, H_))
    scr.alpha_composite(R(sky(), W_, H_, 6.0, .008))
    # 적
    foes = [('big', 540, 470, .95), ('fire', 190, 330, .62), ('fire', 790, 640, .55), ('pebble', 400, 690, .62), ('pebble', 660, 300, .55),
            ('pebble', 960, 470, .5), ('ice', 985, 760, .55), ('ufo', 170, 700, .62), ('moth', 230, 980, .6), ('moth', 610, 940, .55),
            ('beetle', 830, 1140, .6), ('jelly', 390, 1170, .55), ('spider', 720, 1330, .55), ('crack', 470, 1000, .5), ('split', 120, 1300, .5)]
    for eid, x, y, k in foes:
        im = enemy_img(eid, k)
        scr.alpha_composite(im, (int(x - im.width / 2), int(y - im.height / 2)))
    # 큰 소행성 체력 바
    hp = rr(440, 340, 200, 22, 11, TRACK, 5) + rrf(444, 344, 192 * .55, 14, 7, RED)
    # 끈끈이 드론 장판 (가로 띠)
    band = f'<rect x="-10" y="1030" width="1100" height="96" rx="30" fill="{ELEM["grav"]}" opacity=".32"/>' \
           + f'<rect x="-10" y="1030" width="1100" height="96" rx="30" fill="none" stroke="{ELEM["grav"]}" stroke-width="6" stroke-dasharray="26 14"/>'
    scr.alpha_composite(R(band + hp, W_, H_, 2.0, .04))
    # 땅 · 기지
    scr.alpha_composite(R(ground() + base_back(), W_, H_, 5.0, .01))
    scr.alpha_composite(R(dome_front() + slot_empty(SLOT_X[3]), W_, H_, 3.0, .02))
    for tid, x in (('rail', SLOT_X[0]), ('laser', SLOT_X[1]), ('drone', SLOT_X[2])):
        im = turret_img(tid, .78)
        scr.alpha_composite(im, (int(x - 100 * .78), int(GROUND_Y + 6 - 214 * .78)))
    GK = .92
    g = guardian_img(GK)
    gx0, gy0 = 540 - G_CX * GK, GROUND_Y + 4 - G_FOOT * GK
    scr.alpha_composite(g, (int(gx0), int(gy0)))
    # 효과: 레이저(관통) · 관통포 탄 · 가디언 연사 · 맞음
    fx = beam(268, 1560, 268 + (390 - 268) * 1260 / 390, 300, ELEM['light'], 12)
    for (lx, ly), (tx, ty) in zip((GUN_TIP, GUN_TIP), ((470, 1000), (610, 940))):     # 소총 연사
        gx, gy = gx0 + lx * GK, gy0 + ly * GK
        ang = math.degrees(math.atan2(ty - gy, tx - gx))
        for i in range(1, 5):
            t = i / 5
            fx += bullet(gx + (tx - gx) * t, gy + (ty - gy) * t, ang)
    fx += (f'<g transform="translate(150,980) rotate(-84)">' + '<path d="M-10 -10 L-150 0 L-10 10 Z" fill="#fff4dc" opacity=".55"/>'
           + rr(-24, -11, 56, 22, 10, ELEM['phys'], 5) + P('M30 -11 L48 0 L30 11 Z', METAL, 4) + '</g>')
    fx += hit(390, 1170, 40) + hit(612, 952, 30) + hit(170, 740, 36) + burst(540, 470, 46, 9, '#ffe08a', 5, .5)
    scr.alpha_composite(R(fx, W_, H_, 1.6))
    nums = (T(392, 1100, '128', 44) + T(640, 880, '36', 36) + T(612, 840, '41', 36) + T(186, 650, '치명! 512', 46, fill=GOLD)
            + T(540, 326, '중간 보스', 34, fill='#ffd0c4'))
    scr.alpha_composite(R(font_css() + nums, W_, H_, 0.0))
    if with_hud:
        scr.alpha_composite(R(hud_svg(), W_, H_, 2.0, .04))
        scr.alpha_composite(R(font_css() + hud_text(), W_, H_, 0.0))
    return scr


def stars_row(x, y, n, on, size=24, gap=6):
    s = ''
    for i in range(n):
        cx = x + i * (size + gap)
        s += P(f'M{cx} {y - size / 2} L{cx + size / 2} {y} L{cx} {y + size / 2} L{cx - size / 2} {y} Z', GOLD if i < on else TRACK, 3.6)
    return s


def tier_badge(x, y, n, r=20, col=CREAM):
    return C(x, y, r, col, 4.5), T(x, y + r * .42, str(n), r * 1.15, fill=INK, outline=False)


CARDS = [  # 종류, 띠 글자, 띠 색, 터렛, 카드 이름, 설명 2줄, 단계, 별(고른 뒤), 별 원래
    ('강화', ORANGE, 'rail', '관통탄 강화', ('탄이 적 1마리를', '더 꿰뚫어요'), 1, 3, 2),
    ('새 터렛', GREEN, 'bolt', '벼락탑', ('세로줄로 벼락을', '떨어뜨려 마비시켜요'), 1, 1, 0),
    ('돌파', PURPLE, 'laser', '갈래 레이저', ('레이저가 두 갈래로', '갈라져 나가요'), 2, 0, 4),
]


def levelup():
    scr = battle(with_hud=True)
    scr.alpha_composite(Image.new('RGBA', (W_, H_), (20, 18, 40, 150)))
    X0, Y0, WW, HH = 40, 330, 1000, 1360
    shapes, texts, pics = [], [], []
    shapes.append(rr(X0, Y0, WW, HH, 30, CREAM, 7))
    shapes.append(f'<path d="M{X0 + 3} {Y0 + 33} Q{X0 + 3} {Y0 + 3} {X0 + 33} {Y0 + 3} L{X0 + WW - 33} {Y0 + 3} Q{X0 + WW - 3} {Y0 + 3} {X0 + WW - 3} {Y0 + 33} L{X0 + WW - 3} {Y0 + 96} L{X0 + 3} {Y0 + 96} Z" fill="{ORANGE}"/>' + L(f'M{X0 + 3} {Y0 + 96} L{X0 + WW - 3} {Y0 + 96}', 6))
    texts.append(T(540, Y0 + 70, '레벨 업!', 58))
    texts.append(T(540, Y0 + 160, '카드 1장을 골라 주세요', 36, fill='#7a6a5c', outline=False))
    # 내 터렛 5칸
    slots = [('g', 1, 2), ('rail', 1, 2), ('laser', 1, 4), ('drone', 1, 1), (None, 0, 0)]
    sx0, sy = 105, Y0 + 200
    for i, (tid, tier, st) in enumerate(slots):
        x = sx0 + i * 180
        if tid is None:
            shapes.append(f'<rect x="{x}" y="{sy}" width="150" height="150" rx="24" fill="#f3e3c3" stroke="{INK}" stroke-width="5" stroke-dasharray="16 10"/>' + L(f'M{x + 55} {sy + 75} L{x + 95} {sy + 75} M{x + 75} {sy + 55} L{x + 75} {sy + 95}', 7, '#c9b48e'))
            texts.append(T(x + 75, sy + 198, '빈 칸', 28, fill='#7a6a5c', outline=False))
            continue
        shapes.append(rr(x, sy, 150, 150, 24, CREAM2, 6))
        pics.append((tid, x, sy))
        b, t = tier_badge(x + 22, sy + 176, tier)
        shapes.append(b + stars_row(x + 60, sy + 176, 4, st, 20, 4)); texts.append(t)
    texts.append(T(105, sy - 14, '내 터렛', 30, fill=INK, anchor='start', outline=False))
    # 카드 3장
    cy0 = Y0 + 450
    for i, (kind, col, tid, name, desc, tier, st_after, st_before) in enumerate(CARDS):
        x = 75 + i * 320; w, h = 290, 690
        shapes.append(rr(x, cy0, w, h, 26, '#fffaf0', 7))
        shapes.append(f'<path d="M{x + 4} {cy0 + 30} Q{x + 4} {cy0 + 4} {x + 30} {cy0 + 4} L{x + w - 30} {cy0 + 4} Q{x + w - 4} {cy0 + 4} {x + w - 4} {cy0 + 30} L{x + w - 4} {cy0 + 92} L{x + 4} {cy0 + 92} Z" fill="{ELEM[TUR[tid][2]]}"/>' + L(f'M{x + 4} {cy0 + 92} L{x + w - 4} {cy0 + 92}', 6))
        texts.append(T(x + w / 2, cy0 + 64, name, 40))
        shapes.append(rr(x + 22, cy0 + 112, w - 44, 250, 20, '#f6e9cc', 5))
        pics.append((tid + '_big', x + w / 2, cy0 + 112))
        shapes.append(rr(x + w / 2 - 74, cy0 - 26, 148, 50, 25, col, 5))
        texts.append(T(x + w / 2, cy0 + 10, kind, 30))
        texts.append(T(x + w / 2, cy0 + 404, TUR[tid][1] + ' · ' + ELEM_NAME[TUR[tid][2]], 28, fill='#7a6a5c', outline=False))
        for j, ln in enumerate(desc):
            texts.append(T(x + w / 2, cy0 + 470 + j * 44, ln, 31, fill=INK, outline=False))
        if kind == '돌파':
            b1, t1 = tier_badge(x + 92, cy0 + 600, 1, 24); b2, t2 = tier_badge(x + 198, cy0 + 600, 2, 24, '#e4d6f6')
            shapes.append(b1 + b2 + L(f'M{x + 124} {cy0 + 600} L{x + 164} {cy0 + 600} M{x + 152} {cy0 + 588} L{x + 166} {cy0 + 600} L{x + 152} {cy0 + 612}', 6, PURPLE2))
            texts += [t1, t2]
        else:
            b, t = tier_badge(x + 62, cy0 + 600, tier, 24)
            shapes.append(b + stars_row(x + 112, cy0 + 600, 4, st_after, 26, 8)); texts.append(t)
            if st_after > st_before:
                xi = x + 112 + (st_after - 1) * 34
                shapes.append(f'<circle cx="{xi}" cy="{cy0 + 600}" r="22" fill="none" stroke="{GREEN2}" stroke-width="5"/>')
    # 새로고침 (원작대로: 광고만, 판마다 1번)
    by = Y0 + HH - 190
    shapes.append(f'<g transform="translate(330,{by})">{button(420, 110, ORANGE, ORANGE2, 28, 6, 10)}'
                  + rr(44, 30, 60, 44, 9, CREAM, 4.5) + P('M66 40 L86 52 L66 64 Z', ORANGE2, 3.4) + '</g>')
    texts.append(T(570, by + 70, '새로고침', 44))
    texts.append(T(540, by + 150, '광고 보고 새로고침 · 남은 횟수 1/1', 28, fill='#7a6a5c', outline=False))
    scr.alpha_composite(R(''.join(shapes), W_, H_, 2.0, .04))
    for tid, x, y in pics:
        if tid == 'g':
            im = guardian_img(.44); scr.alpha_composite(im, (int(x + 75 - G_CX * .44), int(y + 144 - G_FOOT * .44)))
        elif tid.endswith('_big'):
            im = turret_img(tid[:-4], 1.0); scr.alpha_composite(im, (int(x - 100), int(y + 16)))
        else:
            im = turret_img(tid, .62); scr.alpha_composite(im, (int(x + 75 - 62), int(y + 144 - 214 * .62)))
    scr.alpha_composite(R(font_css() + ''.join(texts), W_, H_, 0.0))
    return scr


def parts():
    W, H = 1600, 1640
    sheet = Image.new('RGBA', (W, H))
    bg = f'<rect width="{W}" height="{H}" fill="{NIGHT}"/>' + rr(20, 20, W - 40, 560, 28, NIGHT2, 0) + rr(20, 600, W - 40, 1020, 28, NIGHT2, 0)
    sheet.alpha_composite(R(bg, W, H, 0.0))
    labels = [T(60, 74, '가디언 · 터렛 6종 (1단계 모양)', 40, anchor='start'), T(60, 654, '적 — 운석 7종(작은 돌 ~ 거대 행성) · 벌레 3 · 젤리 2 · 함선 2', 40, anchor='start')]
    g = guardian_img(1.6); sheet.alpha_composite(g, (54, 100))
    labels.append(T(230, 556, '대장 카피 (가디언)', 34))
    for i, (tid, name, el, _) in enumerate(TURRETS):
        x = 480 + (i % 3) * 360; y = 96 + (i // 3) * 240
        im = turret_img(tid, .86); sheet.alpha_composite(im, (x, y))
        labels.append(T(x + 270, y + 120, name, 34) + T(x + 270, y + 164, ELEM_NAME[el], 28, fill=ELEM[el]))
    pos = [('pebble', 110, 790, .9), ('fire', 330, 790, .9), ('ice', 560, 790, .9), ('iron', 790, 790, .9), ('crack', 1020, 790, .9),
           ('big', 1310, 800, 1.15), ('planet', 1300, 1310, .95),
           ('moth', 130, 1100, .9), ('beetle', 370, 1100, .9), ('spider', 610, 1100, .9),
           ('jelly', 130, 1390, .9), ('split', 370, 1390, .9), ('ufo', 610, 1390, .9), ('sdrone', 850, 1390, .9)]
    for eid, x, y, k in pos:
        im = enemy_img(eid, k); sheet.alpha_composite(im, (int(x - im.width / 2), int(y - im.height / 2)))
        ly = y + im.height / 2 + 10 if eid != 'planet' else y + 230
        labels.append(T(x, ly, ENE[eid][1], 30))
    sheet.alpha_composite(R(font_css() + ''.join(labels), W, H, 0.0))
    return sheet


def sample():
    os.makedirs(PREV, exist_ok=True)
    b = battle(); b.convert('RGB').save(os.path.join(PREV, 'sample_battle.png')); print('→ sample_battle.png')
    l = levelup(); l.convert('RGB').save(os.path.join(PREV, 'sample_levelup.png')); print('→ sample_levelup.png')
    p = parts(); p.convert('RGB').save(os.path.join(PREV, 'sample_parts.png')); print('→ sample_parts.png')


if __name__ == '__main__':
    if len(sys.argv) > 1 and sys.argv[1] == 'sample':
        sample()
    elif len(sys.argv) > 1 and sys.argv[1] == 'one':      # 조각 하나 미리보기: one guardian | one turret:laser | one enemy:fire
        os.makedirs(PREV, exist_ok=True)
        what = sys.argv[2]
        im = guardian_ui_img(2.2) if what == 'guardian' else turret_img(what.split(':')[1], 1.6) if what.startswith('turret') else enemy_img(what.split(':')[1], 1.6)
        bg = Image.new('RGBA', im.size, (46, 56, 102, 255)); bg.alpha_composite(im)
        bg.convert('RGB').save(os.path.join(PREV, 'one_' + what.replace(':', '_') + '.png')); print('→ one_' + what.replace(':', '_') + '.png')
    elif len(sys.argv) > 1 and sys.argv[1] == 'parts':
        os.makedirs(PREV, exist_ok=True); parts().convert('RGB').save(os.path.join(PREV, 'sample_parts.png'))
    else:
        print(__doc__)
