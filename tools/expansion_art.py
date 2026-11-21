# -*- coding: utf-8 -*-
"""2026-11-21 콘텐츠 확장 제안서용 아트 시안 생성기
- 신규 일반 몬스터 8종 (monsters_normal.png)
- 신규 엘리트 4종 · 신규 최종 보스 4종 (monsters_elite_boss.png)
- 챕터별 하늘 배경 색감 5종 (sky_chapters.png)
- 챕터별 행성 & 전투 바닥 변화 5종 (planet_chapters.png)
- 신규 타워 6종 (new_turrets6.png)

실행: python -X utf8 tools/expansion_art.py
출력: 캡쳐본 모음/콘텐츠_확장_시안/*.png
"""
import os, sys, math, random
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import earth_art as A
from earth_art import *          # noqa
import tower_art as TA

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), '..'))
OUT = os.path.join(ROOT, '캡쳐본 모음', '콘텐츠_확장_시안')
os.makedirs(OUT, exist_ok=True)


def star5(cx, cy, r, fill, sw=2.6, rot=-90):
    pts = []
    for i in range(10):
        rad = r if i % 2 == 0 else r * .46
        a = math.radians(rot + i * 36)
        pts.append(f'{cx + rad * math.cos(a):.1f} {cy + rad * math.sin(a):.1f}')
    return P('M' + ' L'.join(pts) + ' Z', fill, sw)


def star4(x, y, q, fill='#fff4dc', sw=2.4):
    return P(f'M{x} {y - q} Q{x + 2} {y - 2} {x + q} {y} Q{x + 2} {y + 2} {x} {y + q} Q{x - 2} {y + 2} {x - q} {y} Q{x - 2} {y - 2} {x} {y - q} Z', fill, sw)


# ==================================================================
# 1. 신규 일반 몬스터 8종 (로컬 220x220, 아래(기지)를 바라봄)
# ==================================================================
def e_shooter_ufo():
    """레이저 정찰선 (원거리 사격형)"""
    s = (P('M40 94 L18 124 L56 116 Z', G('#7c8ca6'), 5) + P('M180 94 L202 124 L164 116 Z', G('#7c8ca6'), 5)
         + rr(98, 128, 24, 34, 8, G(METAL2), 5)
         + blur(C(110, 168, 18, '#ff7878', 0), 5, .9) + C(110, 166, 10, '#ff9088', 3.4) + C(110, 166, 4.5, '#ffffff', 0)
         + P('M62 86 Q68 34 110 32 Q152 34 158 86 Z', G('#aee8ff', 'r', .85, .5), 5.5)
         + C(110, 68, 22, '#8dd47e', 4.2) + face(110, 67, 38, angry=True, mouth='frown')
         + E(110, 104, 86, 26, G('#6b7a94', 'v', .82, .28), 5.5)
         + ''.join(neon_dot(x, 108, 5.5, '#ff8e72') for x in (56, 84, 136, 164))
         + F('M80 44 Q92 36 108 38 Q94 44 88 54 Z', '#ffffff', .75))
    return s


def e_shield_bot():
    """보호막 위성 (주변 적 보호막 서포터)"""
    s = (blur(C(110, 110, 86, '#7ce8ff', 0), 8, .35)
         + f'<circle cx="110" cy="110" r="84" fill="#9fe8ff" opacity=".14" stroke="#7ce8ff" stroke-width="5" stroke-dasharray="16 10"/>'
         + L('M110 34 L110 56 M34 110 L56 110 M164 110 L186 110', 6)
         + rr(24, 92, 18, 36, 6, G('#79c8e8'), 4.5) + rr(178, 92, 18, 36, 6, G('#79c8e8'), 4.5)
         + neon_dot(110, 30, 8, '#7ce8ff')
         + P('M110 54 L158 76 L150 134 L110 162 L70 134 L62 76 Z', G('#9cd8f5', 'r', .82, .35), 5.5)
         + P('M110 68 L142 84 L136 126 L110 146 L84 126 L78 84 Z', G('#e4f7ff', 'v', .9, .2), 4)
         + face(110, 106, 54, angry=False, mouth='frown'))
    return s


def e_heal_jelly():
    """회복 젤리 (주변 적 회복 힐러)"""
    s = (blur(C(110, 118, 76, '#96f0b0', 0), 7, .4)
         + P(blob(110, 122, 68, 52, 10, .06), G('#8be0a4', 'r', .82, .36), 5.5)
         + F('M68 94 Q80 68 106 64 Q88 78 84 102 Z', '#ffffff', .6)
         # 머리 위 연두 십자 잎
         + rr(101, 32, 18, 38, 6, G('#fff4dc'), 4.5) + rr(91, 42, 38, 18, 6, G('#fff4dc'), 4.5)
         + rr(104, 36, 12, 30, 4, '#5ec47a', 0) + rr(95, 45, 30, 12, 4, '#5ec47a', 0)
         + face(110, 122, 74, angry=False, mouth='frown')
         + C(68, 134, 8, '#ff9eb5', 0).replace('<circle', '<circle opacity=".55"')
         + C(152, 134, 8, '#ff9eb5', 0).replace('<circle', '<circle opacity=".55"'))
    return s


def e_frost_mite():
    """서리 진드기 (터렛 동결 방해형)"""
    legs = ''.join(L(f'M{110 + sx * 36} {96 + i * 16} Q{110 + sx * 76} {78 + i * 18} {110 + sx * 88} {118 + i * 22}', 6)
                   for sx in (-1, 1) for i in range(3))
    crys = (P('M76 62 L58 24 L94 52 Z', G('#d8f6ff'), 4.5)
            + P('M110 54 L110 14 L126 54 Z', G('#9fe4ff'), 4.5)
            + P('M144 62 L162 24 L126 52 Z', G('#d8f6ff'), 4.5))
    return (legs + crys
            + E(110, 114, 62, 52, G('#8ed8f8', 'r', .80, .38), 5.5)
            + F('M66 92 Q80 68 108 66 Q88 80 82 98 Z', '#ffffff', .55)
            + face(110, 118, 72, angry=True, mouth='teeth'))


def e_bomb_rock():
    """화산 폭탄 운석 (고위험 자폭 · 주변 연쇄 폭발)"""
    fuse = (L('M110 66 Q120 40 140 34', 6)
            + blur(burst(146, 30, 22, 7, '#ff9b38', 0, .5), 4, .9)
            + burst(146, 30, 16, 7, '#ffe066', 3, .5))
    cracks = 'M68 112 L94 128 L86 154 M152 106 L130 126 L142 152 M100 176 L112 156 L124 176'
    return (flame(110, 136, 56, 108, '#ff7a42', '#ffd966') + fuse
            + P(blob(110, 134, 62, 19, 10, .1), G('#5c4542', 'r', .78, .22), 5.5)
            + blur(L(cracks, 9, '#ff6e38'), 3, .9) + L(cracks, 4.2, '#ffe066')
            + face(110, 132, 68, angry=True, mouth='teeth'))


def e_mirror_cube():
    """거울 프리즘 (빛/불 반사 · 물리/중력 약점)"""
    s = (blur(P('M110 26 L180 110 L110 194 L40 110 Z', '#e4d4ff', 0), 8, .6)
         + P('M110 28 L178 110 L110 192 L42 110 Z', G('#d2c4f5', 'r', .82, .45), 5.5)
         + P('M110 28 L178 110 L110 110 Z', '#ffffff', 0).replace('fill="#ffffff"', 'fill="#ffffff" opacity=".45"')
         + P('M42 110 L110 110 L110 192 Z', '#8f7cc4', 0).replace('fill="#8f7cc4"', 'fill="#8f7cc4" opacity=".32"')
         + L('M110 28 L110 192 M42 110 L178 110', 3.4, '#ffffff')
         + P('M110 28 L178 110 L110 192 L42 110 Z', 'none', 5.5)
         + face(110, 114, 62, angry=True, mouth='frown'))
    return s


def e_blink_bug():
    """점멸 반딧불 (순간이동 회피형)"""
    s = (blur(C(110, 148, 36, '#ffe66d', 0), 8, .85)
         + E(58, 92, 46, 22, '#e4f8ff', 4.5, -26).replace('<ellipse', '<ellipse opacity=".86"')
         + E(162, 92, 46, 22, '#e4f8ff', 4.5, 26).replace('<ellipse', '<ellipse opacity=".86"')
         + L('M96 58 Q84 32 72 34 M124 58 Q136 32 148 34', 4.4) + C(70, 34, 5, '#ffe066', 2.4) + C(150, 34, 5, '#ffe066', 2.4)
         + E(110, 142, 34, 34, G('#ffe878', 'r', .9, .5), 5.5)
         + E(110, 92, 34, 34, G('#5a4e7c', 'r', .82, .28), 5.5)
         + face(110, 94, 54, angry=True, mouth='frown')
         + star4(34, 152, 11, '#fff29c', 2.2) + star4(186, 144, 9, '#fff29c', 2.2))
    return s


def e_lava_slime():
    """용암 젤리 (체력 50% 이하 광폭화 가속)"""
    s = (blur(P(blob(110, 128, 72, 61, 10, .08), '#ff7842', 0), 7, .55)
         + P('M64 82 Q72 40 94 64 Q110 24 128 62 Q148 38 156 82 Z', G('#ff9f42', 'v', .9, .3), 4.5)
         + P(blob(110, 128, 66, 61, 10, .08), G('#f06444', 'r', .8, .35), 5.5)
         + C(72, 102, 10, '#ffe078', 0).replace('<circle', '<circle opacity=".7"')
         + C(148, 144, 8, '#ffe078', 0).replace('<circle', '<circle opacity=".7"')
         + face(110, 128, 76, angry=True, mouth='teeth'))
    return s


# ==================================================================
# 2. 신규 엘리트 4종 (220x220) + 신규 최종 보스 4종 (320x320)
# ==================================================================
def el_cryo_golem():
    """엘리트: 빙하 골렘"""
    s = (P('M44 106 L22 146 L52 156 L68 122 Z', G('#8cd4f2'), 5.5)
         + P('M176 106 L198 146 L168 156 L152 122 Z', G('#8cd4f2'), 5.5)
         + P('M64 60 L54 22 L88 48 L110 12 L132 48 L166 22 L156 60 Z', G('#d8f7ff', 'v', .88, .4), 5.5)
         + P('M54 64 L166 64 L178 134 L144 186 L76 186 L42 134 Z', G('#7ec8eb', 'r', .8, .35), 6)
         + L('M64 96 L156 96 M84 156 L136 156', 3.4, '#d8f7ff')
         + face(110, 118, 84, angry=True, mouth='teeth'))
    return s


def el_carrier_pod():
    """엘리트: 포자 모체"""
    s = (''.join(L(f'M{110 + sx * 54} {110 + dy} Q{110 + sx * 90} {96 + dy} {110 + sx * 96} {134 + dy}', 7)
                 for sx in (-1, 1) for dy in (-24, 16))
         + E(110, 114, 76, 72, G('#7f9c5e', 'r', .78, .3), 6)
         + C(72, 78, 16, G('#d4f08a', 'r'), 4.5) + C(148, 78, 16, G('#d4f08a', 'r'), 4.5)
         + C(110, 56, 18, G('#ffe078', 'r'), 4.5)
         + face(110, 128, 78, angry=True, mouth='teeth'))
    return s


def el_prism_core():
    """엘리트: 거울 코어"""
    s = (blur(C(110, 110, 92, '#c8a6ff', 0), 9, .5)
         + ''.join(P(f'M{110 + 92 * math.cos(math.radians(a)):.1f} {110 + 92 * math.sin(math.radians(a)):.1f} '
                     f'L{110 + 60 * math.cos(math.radians(a - 16)):.1f} {110 + 60 * math.sin(math.radians(a - 16)):.1f} '
                     f'L{110 + 60 * math.cos(math.radians(a + 16)):.1f} {110 + 60 * math.sin(math.radians(a + 16)):.1f} Z',
                     G('#b398e8'), 4.5) for a in range(0, 360, 60))
         + C(110, 110, 62, G('#e6dbff', 'r', .84, .45), 6)
         + face(110, 112, 74, angry=True, mouth='frown')
         + neon_dot(110, 68, 7, '#8fd6ff'))
    return s


def el_magma_titan():
    """엘리트: 용암 거석"""
    cracks = 'M58 98 L94 118 L80 156 M162 94 L132 122 L150 158 M104 60 L114 90'
    return (flame(110, 126, 76, 108, '#ff6e38', '#ffe066')
            + P(blob(110, 126, 78, 29, 10, .12), G('#634640', 'r', .75, .22), 6)
            + blur(L(cracks, 11, '#ff6a2b'), 4, .9) + L(cracks, 4.8, '#ffe270')
            + face(110, 128, 86, angry=True, mouth='teeth'))


def boss_frost_emperor():
    """보스 1: 서리 수정왕 (로컬 320x320)"""
    s = (blur(C(160, 168, 126, '#9fe8ff', 0), 14, .48)
         # 거대한 등 얼음 결정 날개
         + P('M60 126 L16 64 L96 96 Z', G('#bfefff'), 6) + P('M260 126 L304 64 L224 96 Z', G('#bfefff'), 6)
         + P('M54 186 L18 228 L88 214 Z', G('#8fd6f5'), 6) + P('M266 186 L302 228 L232 214 Z', G('#8fd6f5'), 6)
         # 얼음 왕관
         + P('M96 86 L84 26 L128 62 L160 14 L192 62 L236 26 L224 86 Z', G('#e4f9ff', 'v', .86, .45), 6)
         + neon_dot(160, 56, 9, '#7ce8ff')
         # 육각 얼음 본체
         + P('M84 82 L236 82 L262 176 L216 266 L104 266 L58 176 Z', G('#86d4f5', 'r', .78, .38), 6.5)
         + P('M84 82 L160 82 L134 266 L104 266 L58 176 Z', '#ffffff', 0).replace('fill="#ffffff"', 'fill="#ffffff" opacity=".24"')
         + L('M92 224 L228 224', 4.5, '#dff8ff')
         + face(160, 164, 118, angry=True, mouth='teeth'))
    return s


def boss_mecha_crab():
    """보스 2: 집게 요새 게 (로컬 320x320)"""
    # 양옆 거대 집게발
    claws = (L('M72 170 Q34 150 38 112', 18) + L('M72 170 Q34 150 38 112', 10, '#d95848')
             + P('M38 116 C10 100 10 52 46 42 L46 78 L66 64 C84 82 74 114 38 116 Z', G('#e86854', 'r'), 6)
             + L('M248 170 Q286 150 282 112', 18) + L('M248 170 Q286 150 282 112', 10, '#d95848')
             + P('M282 116 C310 100 310 52 274 42 L274 78 L254 64 C236 82 246 114 282 116 Z', G('#e86854', 'r'), 6))
    legs = ''.join(L(f'M{160 + sx * 76} {214 + i * 16} L{160 + sx * 124} {236 + i * 24}', 9)
                   for sx in (-1, 1) for i in range(2))
    return (legs + claws
            + rr(134, 62, 52, 40, 9, G(METAL), 5.5) + neon_dot(160, 76, 8, '#ffd54f')
            + E(160, 174, 108, 86, G('#e05b4c', 'r', .80, .30), 6.5)
            + P('M84 134 Q160 104 236 134 L228 154 Q160 126 92 154 Z', G(METAL), 4.5)
            + F('M88 142 Q112 108 156 106 Q124 126 114 156 Z', '#ffffff', .35)
            + face(160, 184, 114, angry=True, mouth='teeth'))


def boss_ring_wyrm():
    """보스 3: 토성 고리룡 (로컬 320x320)"""
    ring_b = f'<g transform="rotate(-10 160 202)">' + L('M26 202 A134 36 0 0 1 294 202', 22) + L('M26 202 A134 36 0 0 1 294 202', 12, '#ffd878') + '</g>'
    ring_f = f'<g transform="rotate(-10 160 202)">' + L('M26 202 A134 36 0 0 0 294 202', 22) + L('M26 202 A134 36 0 0 0 294 202', 12, '#ffd878') + '</g>'
    wings = (P('M86 146 Q24 88 20 144 Q42 178 86 186 Z', G('#7ec8c0'), 5.5)
             + P('M234 146 Q296 88 300 144 Q278 178 234 186 Z', G('#7ec8c0'), 5.5))
    horns = P('M114 82 Q92 34 114 26 Q126 48 134 74 Z', G('#ffd54f'), 5.5) + P('M206 82 Q228 34 206 26 Q194 48 186 74 Z', G('#ffd54f'), 5.5)
    body = (E(160, 174, 86, 86, G('#58a8a8', 'r', .80, .32), 6.5)
            + E(160, 212, 54, 42, G('#ffeab0', 'v', .9, .2), 4.5)
            + L('M124 208 Q160 218 196 208 M130 228 Q160 236 190 228', 3.4, '#d9b468')
            + face(160, 144, 108, angry=True, mouth='teeth'))
    return blur(C(160, 174, 126, '#8ee8dc', 0), 12, .35) + ring_b + wings + horns + body + ring_f


def boss_jelly_king():
    """보스 4: 대왕 젤리 황제 (로컬 320x320)"""
    s = (blur(C(160, 176, 128, '#ff9ec8', 0), 14, .45)
         # 왕관 + 보석
         + P('M102 92 L92 32 L130 64 L160 20 L190 64 L228 32 L218 92 Z', G('#ffd54f', 'v', .86, .3), 6)
         + C(160, 56, 10, '#ff6a8a', 3.4) + C(124, 68, 7, '#7ce8ff', 2.8) + C(196, 68, 7, '#7ce8ff', 2.8)
         # 거대 말랑 무지개 젤리 몸통
         + P('M46 210 C42 108 92 78 160 78 C228 78 278 108 274 210 C272 264 230 278 160 278 C90 278 48 264 46 210 Z',
             G('#f28ab8', 'r', .80, .38), 6.5)
         + F('M72 136 Q94 94 144 92 Q112 116 102 154 Z', '#ffffff', .55)
         + C(82, 224, 14, '#ffe6f2', 0).replace('<circle', '<circle opacity=".55"')
         + C(238, 206, 11, '#d8f4ff', 0).replace('<circle', '<circle opacity=".55"')
         + face(160, 178, 126, angry=True, mouth='teeth')
         + C(94, 202, 13, '#ff688c', 0).replace('<circle', '<circle opacity=".5"')
         + C(226, 202, 13, '#ff688c', 0).replace('<circle', '<circle opacity=".5"'))
    return s


# ==================================================================
# 3. 신규 타워 6종 (로컬 200x222 — 기존 earth_art 터렛과 동일 규격!)
# ==================================================================
def nt_boomerang():
    """별빛 부메랑 (물리 · St.30)"""
    c = ELEM['phys']
    base = pedestal(c) + rr(84, 112, 32, 52, 8, G(METAL), 5.5) + E(100, 110, 38, 14, G(METAL2), 5)
    # 3날 네온 황금 부메랑
    blades = ''
    for a in (-90, 30, 150):
        blades += f'<g transform="rotate({a} 100 68)">' + P('M100 68 Q120 40 144 26 Q132 58 106 78 Z', G('#ffd978', 'v', .82, .35), 4.8) + '</g>'
    return (base + blur(C(100, 68, 42, '#ffe082', 0), 7, .55) + blades
            + C(100, 68, 16, G(c, 'r'), 5) + neon_dot(100, 68, 6, '#fff4b8'))


def nt_frost():
    """서리 눈꽃포 (빛 · St.40)"""
    c = ELEM['light']
    s = (pedestal(c)
         + P('M60 162 L72 96 L128 96 L140 162 Z', G('#c6e6f5', 'v', .84, .32), 5.5)
         + rr(76, 30, 48, 72, 14, G('#8fd6ff', 'v', .80, .42), 5.5)
         + rr(68, 22, 64, 22, 9, G('#e6f8ff', 'v', .88, .3), 5.5)
         + F('M84 44 L92 44 L92 94 L84 94 Z', '#ffffff', .65))
    # 중앙 빛나는 눈꽃 결정
    cx, cy, r = 100, 128, 18
    flake = ''.join(f'<g transform="rotate({a} {cx} {cy})">' + L(f'M{cx - r} {cy} L{cx + r} {cy}', 4.2, '#ffffff') + '</g>' for a in (0, 60, 120))
    return s + blur(C(cx, cy, 22, '#9fe8ff', 0), 6, .85) + flake + C(cx, cy, 6, '#e6fbff', 2.4)


def nt_flamer():
    """용암 화염방사기 (불 · St.50)"""
    c = ELEM['fire']
    tanks = (rr(42, 98, 32, 62, 12, G('#e06850'), 5) + rr(126, 98, 32, 62, 12, G('#e06850'), 5)
             + L('M44 128 L72 128 M128 128 L156 128', 4, GOLD))
    nozzle = (rr(76, 62, 48, 98, 12, G(METAL), 5.5)
              + P('M70 62 L130 62 L122 40 L78 40 Z', G('#d9924c'), 5.5)
              + neon_dot(100, 118, 7, '#ffb070'))
    pilot = (blur(P('M76 42 Q76 14 90 22 Q100 -10 112 20 Q126 12 124 42 Z', '#ff8030', 0), 7, .92)
             + P('M80 42 Q80 18 92 26 Q100 -2 110 24 Q122 18 120 42 Z', G('#ffb84d', 'v', .9, .32), 4.0)
             + P('M90 42 Q100 14 110 42 Z', '#fff7cc', 0))
    return pedestal(c) + tanks + pilot + nozzle


def nt_saw():
    """자기장 톱날 (번개 · St.60)"""
    c = ELEM['bolt']
    arms = (P('M52 162 L44 76 L62 72 L70 162 Z', G(METAL), 5.5)
            + P('M148 162 L156 76 L138 72 L130 162 Z', G(METAL), 5.5)
            + neon_dot(54, 82, 6, '#ffe066') + neon_dot(146, 82, 6, '#ffe066'))
    # 공중에 뜬 전기 톱날 원반
    pts = []
    for i in range(16):
        a = math.radians(i * 22.5)
        rad = 38 if i % 2 == 0 else 28
        pts.append(f'{100 + rad * math.cos(a):.1f} {78 + rad * math.sin(a):.1f}')
    saw_d = 'M' + ' L'.join(pts) + ' Z'
    arcs = blur(L('M58 80 L76 74 M142 80 L124 74', 6, '#ffe878'), 3, .9) + L('M58 80 L76 74 M142 80 L124 74', 2.4, '#ffffff')
    return (pedestal(c) + arms + blur(C(100, 78, 36, '#ffe066', 0), 7, .65)
            + P(saw_d, G('#dfe6ee', 'r', .84, .35), 4.8)
            + C(100, 78, 16, G(c, 'r'), 4.5) + C(100, 78, 6, '#ffffff', 0) + arcs)


def nt_mortar():
    """유성 박격포 (불 · St.70)"""
    c = ELEM['fire']
    body = (pedestal(c)
            + P('M54 162 Q48 82 100 76 Q152 82 146 162 Z', G('#726b78', 'r', .78, .28), 5.5)
            + E(100, 82, 48, 18, G(METAL), 5.5) + E(100, 82, 34, 11, '#3b281c', 0)
            + rr(62, 120, 76, 18, 6, G(c), 4.5) + neon_dot(100, 129, 6, '#ffd278'))
    orb = (blur(C(100, 48, 30, '#ff8842', 0), 8, .85)
           + P(blob(100, 48, 24, 9, 9, .1), G('#ff9c54', 'r', .82, .35), 4.8)
           + L('M90 42 L102 52 L112 44', 3, '#fff0a8'))
    return body + orb


def nt_prism():
    """별빛 증폭탑 (중력 · St.80 · 인접 버프 서포터)"""
    c = ELEM['grav']
    pillar = (pedestal(c)
              + P('M78 162 L88 86 L112 86 L122 162 Z', G('#dcd4f5', 'v', .86, .28), 5.5)
              + neon_dot(100, 128, 6, '#e2d4ff'))
    rings = (E(100, 58, 48, 14, 'none', 5, -24).replace(f'stroke="{INK}"', 'stroke="#ffd86b"')
             + E(100, 58, 48, 14, 'none', 5, 24).replace(f'stroke="{INK}"', 'stroke="#d4b8ff"'))
    return (pillar + blur(C(100, 58, 36, '#d4b8ff', 0), 9, .75)
            + rings + star5(100, 58, 22, G('#ffe888', 'r', .86, .45), 4.2)
            + C(100, 58, 7, '#ffffff', 0))


# ==================================================================
# 4. 챕터별 하늘 배경 5종 & 행성/바닥 변화 5종
# ==================================================================
CHAPTERS = [
    dict(id='ch1', st='St. 1~20', name='Ch.1 푸른 지구 밤하늘',
         sky=('#2e3866', '#36427a', '#4a5796'), neb=('#3f4c8c', '#5866ab'), star='#fff4dc',
         planet='moon', ground='earth', desc='지구 대륙 바닥 · 달토끼 달'),
    dict(id='ch2', st='St. 21~40', name='Ch.2 달빛 보라 궤도',
         sky=('#241d4f', '#3d3078', '#624da3'), neb=('#4e3c96', '#7b62c4'), star='#f0e8ff',
         planet='earth', ground='moon', desc='연보라 달 바닥 · 떠오르는 푸른 지구'),
    dict(id='ch3', st='St. 41~60', name='Ch.3 화성 석양 성운',
         sky=('#3d1c33', '#662d45', '#9c4c58'), neb=('#7d3550', '#bf6468'), star='#ffe8c2',
         planet='mars_gas', ground='mars', desc='테라코타 화성 바닥 · 살구 가스 행성'),
    dict(id='ch4', st='St. 61~80', name='Ch.4 토성 청록 오로라',
         sky=('#162e42', '#224d66', '#38788a'), neb=('#2a627a', '#4fa3ad'), star='#e4fcff',
         planet='saturn', ground='ice', desc='서리 얼음 바닥 · 거대 황금 고리 토성'),
    dict(id='ch5', st='St. 81~100', name='Ch.5 젤리 은하 중심',
         sky=('#301a4a', '#5a2b6e', '#944787'), neb=('#74368a', '#c968a6'), star='#ffe6f5',
         planet='jelly_twin', ground='jelly', desc='분홍 젤리 행성 바닥 · 쌍둥이 성운 행성'),
]


def draw_sky_panel(ch, w, h, seed=1):
    rnd = random.Random(seed)
    c0, c1, c2 = ch['sky']
    gid = f'sk_{ch["id"]}_{w}'
    A._DEFS[gid] = f'<linearGradient id="{gid}" x1="0" y1="0" x2="0" y2="1"><stop offset="0" stop-color="{c0}"/><stop offset=".6" stop-color="{c1}"/><stop offset="1" stop-color="{c2}"/></linearGradient>'
    s = [f'<rect width="{w}" height="{h}" fill="url(#{gid})"/>']
    for x, y, r in ((w * .24, h * .28, w * .34), (w * .78, h * .58, w * .38)):
        s.append(f'<g opacity=".36">{puff(x, y, r, ch["neb"][0], 0, 7, int(x + seed))}</g>')
    s.append(f'<g opacity=".28">{puff(w * .52, h * .42, w * .24, ch["neb"][1], 0, 6, seed + 11)}</g>')
    for _ in range(38):
        s.append(f'<circle cx="{rnd.uniform(8, w - 8):.1f}" cy="{rnd.uniform(8, h * .82):.1f}" r="{rnd.uniform(1.2, 2.8):.1f}" fill="{ch["star"]}" opacity="{rnd.uniform(.45, .95):.2f}"/>')
    for _ in range(4):
        s.append(star4(rnd.uniform(22, w - 22), rnd.uniform(24, h * .65), rnd.uniform(6, 10), ch['star'], 1.6))
    return ''.join(s)


def draw_bg_planet(kind, cx, cy, r):
    if kind == 'moon':
        return blur(C(cx, cy, r * 1.25, '#f3e3c3', 0), 8, .38) + C(cx, cy, r, G('#f3e3c3', 'r', .88, .4), 3.6) + craters(cx, cy, r, '#e1cda3', 5, 4)
    if kind == 'earth':
        return TA.earth(cx, cy, r, sw=3.6, seed=4)
    if kind == 'mars_gas':
        return TA.planet(cx, cy, r, sw=3.6)
    if kind == 'saturn':
        rb = f'<g transform="rotate(-16 {cx} {cy})">' + L(f'M{cx - r * 1.85:.1f} {cy} A{r * 1.85:.1f} {r * .44:.1f} 0 0 1 {cx + r * 1.85:.1f} {cy}', 14) + L(f'M{cx - r * 1.85:.1f} {cy} A{r * 1.85:.1f} {r * .44:.1f} 0 0 1 {cx + r * 1.85:.1f} {cy}', 8, '#f5d996') + '</g>'
        rf = f'<g transform="rotate(-16 {cx} {cy})">' + L(f'M{cx - r * 1.85:.1f} {cy} A{r * 1.85:.1f} {r * .44:.1f} 0 0 0 {cx + r * 1.85:.1f} {cy}', 14) + L(f'M{cx - r * 1.85:.1f} {cy} A{r * 1.85:.1f} {r * .44:.1f} 0 0 0 {cx + r * 1.85:.1f} {cy}', 8, '#f5d996') + '</g>'
        bd = blur(C(cx, cy, r * 1.25, '#ffe6a8', 0), 8, .35) + C(cx, cy, r, G('#e8c47e', 'r', .82, .32), 3.6)
        return rb + bd + rf
    # jelly_twin
    p1 = blur(C(cx - 10, cy, r * 1.25, '#ffb3da', 0), 8, .4) + C(cx - 10, cy, r * .92, G('#f294c2', 'r', .82, .38), 3.6) + C(cx - 22, cy - 10, r * .2, '#ffffff', 0).replace('<circle', '<circle opacity=".45"')
    p2 = blur(C(cx + r * 1.05, cy + r * .55, r * .6, '#9fe8ff', 0), 6, .4) + C(cx + r * 1.05, cy + r * .55, r * .46, G('#90d8f8', 'r', .84, .38), 3.2)
    return p1 + p2


def draw_ground_horizon(kind, w, h):
    y0 = h - 76
    pal = {
        'earth': ('#7fc0e8', '#a9d68c', '#d9f3ff'),
        'moon':  ('#cfc8dc', '#b3abc6', '#efeaf8'),
        'mars':  ('#d9845c', '#b86242', '#ffcdb2'),
        'ice':   ('#94d8ee', '#c8f2ff', '#eafcff'),
        'jelly': ('#e894c4', '#c46ca8', '#ffd8ef'),
    }[kind]
    s = (blur(f'<path d="M-20 {y0} Q{w / 2} {y0 - 26} {w + 20} {y0} L{w + 20} {h + 10} L-20 {h + 10} Z" fill="{pal[2]}"/>', 8, .55)
         + P(f'M-20 {y0} Q{w / 2} {y0 - 24} {w + 20} {y0} L{w + 20} {h + 10} L-20 {h + 10} Z', G(pal[0], 'v', .82, .20), 4.2))
    # 행성 표면 특징 (대륙/크레이터/협곡/결정/젤리방울)
    if kind == 'earth':
        s += E(w * .24, y0 + 26, 46, 20, pal[1], 3) + E(w * .78, y0 + 32, 42, 18, pal[1], 3)
    elif kind == 'moon':
        s += E(w * .22, y0 + 30, 26, 9, pal[1], 2.6) + E(w * .76, y0 + 38, 22, 8, pal[1], 2.6) + E(w * .48, y0 + 50, 16, 6, pal[1], 2.4)
    elif kind == 'mars':
        s += P(f'M{w * .1} {y0 + 18} L{w * .24} {y0 - 4} L{w * .36} {y0 + 18} Z', pal[1], 2.6) + E(w * .76, y0 + 32, 30, 10, pal[1], 2.6)
    elif kind == 'ice':
        s += P(f'M{w * .14} {y0 + 16} L{w * .20} {y0 - 16} L{w * .28} {y0 + 16} Z', pal[1], 2.6) + P(f'M{w * .74} {y0 + 18} L{w * .82} {y0 - 12} L{w * .88} {y0 + 18} Z', pal[1], 2.6)
    elif kind == 'jelly':
        s += C(w * .20, y0 + 28, 16, pal[1], 2.6) + C(w * .80, y0 + 34, 14, pal[1], 2.6) + C(w * .20 - 4, y0 + 24, 4, '#ffffff', 0)
    # 중앙 미니 돔 기지 실루엣 (실제 전투 화면 느낌!)
    cx = w / 2
    s += f'<path d="M{cx - 48} {y0 - 12} A48 42 0 0 1 {cx + 48} {y0 - 12} Z" fill="#cfeeff" opacity=".24" stroke="{INK}" stroke-width="3.6"/>'
    s += rr(cx - 26, y0 - 34, 20, 22, 4, '#fff4dc', 2.4) + rr(cx + 6, y0 - 30, 20, 18, 4, '#fff4dc', 2.4)
    s += C(cx - 16, y0 - 24, 3.5, GOLD, 0) + C(cx + 16, y0 - 22, 3.5, GOLD, 0)
    return s


# ==================================================================
# 5. 시트 렌더링 5장
# ==================================================================
def save_sheet(svg, w, h, name, wob=1.2):
    im = A.R(svg, w, h, wob)
    p = os.path.join(OUT, name)
    im.save(p)
    print('saved', p)


def render_monsters_normal():
    items = [
        ('레이저 정찰선', '원거리 사격 · 약점 번개', e_shooter_ufo),
        ('보호막 위성', '주변 보호막 · 약점 번개', e_shield_bot),
        ('회복 젤리', '주변 체력 회복 · 약점 빛', e_heal_jelly),
        ('서리 진드기', '터렛 동결 방해 · 약점 불', e_frost_mite),
        ('화산 폭탄 운석', '자폭 연쇄폭발 · 약점 물리', e_bomb_rock),
        ('거울 프리즘', '빛/불 반사 · 약점 중력', e_mirror_cube),
        ('점멸 반딧불', '라인 순간이동 · 약점 불', e_blink_bug),
        ('용암 젤리', '반피 광폭화 · 약점 빛', e_lava_slime),
    ]
    cw, ch, cols = 280, 300, 4
    rows = 2
    svg = f'<rect width="{cols * cw}" height="{rows * ch}" fill="#2b355e"/>' + A.font_css()
    for i, (nm, sub, fn) in enumerate(items):
        x, y = (i % cols) * cw, (i // cols) * ch
        svg += f'<rect x="{x + 8}" y="{y + 8}" width="{cw - 16}" height="{ch - 16}" rx="18" fill="#364378" stroke="#5363a3" stroke-width="2.5"/>'
        svg += f'<g transform="translate({x + 30},{y + 18}) scale(1.0)">{fn()}</g>'
        svg += A.T(x + cw / 2, y + ch - 44, nm, 22, '#ffffff', outline=True, sw=4)
        svg += A.T(x + cw / 2, y + ch - 18, sub, 15, '#d8e2ff', outline=False)
    save_sheet(svg, cols * cw, rows * ch, 'monsters_normal.png', wob=1.4)


def render_monsters_elite_boss():
    elites = [
        ('빙하 골렘', '엘리트 · 방어/쿨 방해', el_cryo_golem),
        ('포자 모체', '엘리트 · 벌레 대량 소환', el_carrier_pod),
        ('거울 코어', '엘리트 · 전체 보호막 지원', el_prism_core),
        ('용암 거석', '엘리트 · 폭탄 운석 분열', el_magma_titan),
    ]
    bosses = [
        ('서리 수정왕', 'Ch.2 보스 · 터렛 동결 파동', boss_frost_emperor),
        ('집게 요새 게', 'Ch.3 보스 · 정면 집게 방패', boss_mecha_crab),
        ('토성 고리룡', 'Ch.4 보스 · 운석 비 3줄 폭격', boss_ring_wyrm),
        ('대왕 젤리 황제', 'Ch.5 최종 보스 · 분열/보호막', boss_jelly_king),
    ]
    cw, ch1, ch2, cols = 300, 300, 360, 4
    W, H = cols * cw, ch1 + ch2
    svg = f'<rect width="{W}" height="{H}" fill="#252c52"/>' + A.font_css()
    for i, (nm, sub, fn) in enumerate(elites):
        x, y = i * cw, 0
        svg += f'<rect x="{x + 8}" y="{y + 8}" width="{cw - 16}" height="{ch1 - 16}" rx="18" fill="#343f70" stroke="#7684c4" stroke-width="2.5"/>'
        svg += f'<g transform="translate({x + 40},{y + 16})">{fn()}</g>'
        svg += A.T(x + cw / 2, y + ch1 - 44, nm, 22, '#ffe082', outline=True, sw=4)
        svg += A.T(x + cw / 2, y + ch1 - 18, sub, 15, '#d8e2ff', outline=False)
    for i, (nm, sub, fn) in enumerate(bosses):
        x, y = i * cw, ch1
        svg += f'<rect x="{x + 8}" y="{y + 8}" width="{cw - 16}" height="{ch2 - 16}" rx="18" fill="#3d2b5c" stroke="#c878a8" stroke-width="3"/>'
        svg += f'<g transform="translate({x + 14},{y + 12}) scale(.85)">{fn()}</g>'
        svg += A.T(x + cw / 2, y + ch2 - 46, nm, 23, '#ff9eb8', outline=True, sw=4.5)
        svg += A.T(x + cw / 2, y + ch2 - 20, sub, 15, '#f2dcf5', outline=False)
    save_sheet(svg, W, H, 'monsters_elite_boss.png', wob=1.4)


def render_sky_chapters():
    cw, ch_h = 260, 420
    W, H = cw * 5, ch_h
    svg = f'<rect width="{W}" height="{H}" fill="#f7f1e3"/>' + A.font_css()
    for i, c in enumerate(CHAPTERS):
        x = i * cw
        cid = f'sc_{i}'
        svg += f'<clipPath id="{cid}"><rect x="{x + 8}" y="8" width="{cw - 16}" height="{ch_h - 108}" rx="16"/></clipPath>'
        svg += f'<g clip-path="url(#{cid})"><g transform="translate({x + 8},8)">{draw_sky_panel(c, cw - 16, ch_h - 108, i * 17 + 5)}</g></g>'
        svg += f'<rect x="{x + 8}" y="8" width="{cw - 16}" height="{ch_h - 108}" rx="16" fill="none" stroke="{INK}" stroke-width="4"/>'
        # 컬러 스와치 3개
        for j, hx in enumerate(c['sky']):
            svg += f'<rect x="{x + 22 + j * 74}" y="{ch_h - 88}" width="66" height="22" rx="6" fill="{hx}" stroke="{INK}" stroke-width="2"/>'
            svg += A.T(x + 55 + j * 74, ch_h - 72, hx, 11, '#ffffff', outline=False)
        svg += A.T(x + cw / 2, ch_h - 40, c['name'], 18, INK, outline=False)
        svg += A.T(x + cw / 2, ch_h - 16, c['st'], 15, '#7a6a5c', outline=False)
    save_sheet(svg, W, H, 'sky_chapters.png', wob=0.8)


def render_planet_chapters():
    cw, ch_h = 260, 460
    W, H = cw * 5, ch_h
    svg = f'<rect width="{W}" height="{H}" fill="#f7f1e3"/>' + A.font_css()
    for i, c in enumerate(CHAPTERS):
        x = i * cw
        pw, ph = cw - 16, ch_h - 80
        cid = f'pc_{i}'
        inner = (draw_sky_panel(c, pw, ph, i * 29 + 3)
                 + draw_bg_planet(c['planet'], pw * .68, ph * .28, 36 if c['planet'] != 'saturn' else 30)
                 + draw_ground_horizon(c['ground'], pw, ph))
        svg += f'<clipPath id="{cid}"><rect x="{x + 8}" y="8" width="{pw}" height="{ph}" rx="16"/></clipPath>'
        svg += f'<g clip-path="url(#{cid})"><g transform="translate({x + 8},8)">{inner}</g></g>'
        svg += f'<rect x="{x + 8}" y="8" width="{pw}" height="{ph}" rx="16" fill="none" stroke="{INK}" stroke-width="4"/>'
        svg += A.T(x + cw / 2, ch_h - 44, c['name'], 17, INK, outline=False)
        svg += A.T(x + cw / 2, ch_h - 20, c['desc'], 13, '#7a6a5c', outline=False)
    save_sheet(svg, W, H, 'planet_chapters.png', wob=0.9)


def render_new_turrets():
    items = [
        ('별빛 부메랑', '물리 · St.30 · 왕복 관통', 'phys', nt_boomerang),
        ('서리 눈꽃포', '빛 · St.40 · 광역 빙결/취약', 'light', nt_frost),
        ('용암 화염방사기', '불 · St.50 · 근거리 화염벽', 'fire', nt_flamer),
        ('자기장 톱날', '번개 · St.60 · 장갑 관통 점사', 'bolt', nt_saw),
        ('유성 박격포', '불 · St.70 · 초광역 유성 낙하', 'fire', nt_mortar),
        ('별빛 증폭탑', '중력 · St.80 · 인접 버프+표식', 'grav', nt_prism),
    ]
    cw, ch, cols = 320, 320, 3
    rows = 2
    svg = f'<rect width="{cols * cw}" height="{rows * ch}" fill="#2b355e"/>' + A.font_css()
    for i, (nm, sub, el, fn) in enumerate(items):
        x, y = (i % cols) * cw, (i // cols) * ch
        svg += f'<rect x="{x + 10}" y="{y + 10}" width="{cw - 20}" height="{ch - 20}" rx="20" fill="#354275" stroke="{ELEM[el]}" stroke-width="3.5"/>'
        svg += f'<g transform="translate({x + 60},{y + 18})">{fn()}</g>'
        svg += A.T(x + cw / 2, y + ch - 46, nm, 24, '#ffffff', outline=True, sw=4.5)
        svg += A.T(x + cw / 2, y + ch - 20, sub, 16, ELEM[el], outline=False)
    save_sheet(svg, cols * cw, rows * ch, 'new_turrets6.png', wob=1.0)


if __name__ == '__main__':
    render_monsters_normal()
    render_monsters_elite_boss()
    render_sky_chapters()
    render_planet_chapters()
    render_new_turrets()
    print('ALL DONE')
