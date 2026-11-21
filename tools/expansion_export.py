# -*- coding: utf-8 -*-
"""11-21 콘텐츠 확장 제안서 그림 → 게임 그림 (제안서 체크 · 의견: 몬스터 · 보스 "일단 이미지만 추가하여 스테이지에 배치" ·
하늘 "이대로 진행" · 행성 "모든 하단 표면은 지구로만 유지" · 타워 "일단 이미지만 추가")
그림 함수는 tools/expansion_art.py (시안 도구) 것을 그대로 씀.
  Enemy/<id>.png + <id>_w.png   일반 8 · 엘리트 4 (로컬 220) · 보스 4 (로컬 320) — 기존 적과 같은 1.1배
  Turret/<id>.png               새 타워 6 (로컬 200x222, 1.56배) — 터렛 탭 "곧 열리는 터렛" 칸에만
  Bg/sky_ch2 ~ sky_ch5          챕터 하늘 (1080x2400, Ch.1 = 지금 Bg/sky 그대로)
  Bg/planet_ch3 ~ planet_ch5    하늘의 행성 (360x360, 가운데 = 달 자리 · 원 반지름 78 = 달과 같은 크기) — Ch.1 · 2 는 달 + 달토끼
사용 (PowerShell — Edge 렌더): $env:PYTHONUTF8='1'; python tools/expansion_export.py [enemy|turret|bg …]   (없으면 전부)
"""
import os, sys, random
import numpy as np
from PIL import Image

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import earth_art as A
from earth_art import C, G, P, blur, puff
import expansion_art as X

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
ART = os.path.join(ROOT, 'Earth_Defence', 'Assets', 'Resources', 'Art')

NORMAL = [('shooter_ufo', X.e_shooter_ufo), ('shield_bot', X.e_shield_bot), ('heal_jelly', X.e_heal_jelly), ('frost_mite', X.e_frost_mite),
          ('bomb_rock', X.e_bomb_rock), ('mirror_cube', X.e_mirror_cube), ('blink_bug', X.e_blink_bug), ('lava_slime', X.e_lava_slime)]
ELITE = [('cryo_golem', X.el_cryo_golem), ('carrier_pod', X.el_carrier_pod), ('prism_core', X.el_prism_core), ('magma_titan', X.el_magma_titan)]
BOSS = [('frost_emperor', X.boss_frost_emperor), ('mecha_crab', X.boss_mecha_crab), ('ring_wyrm', X.boss_ring_wyrm), ('jelly_king', X.boss_jelly_king)]
TURRET = [('boomerang', X.nt_boomerang), ('frost_cannon', X.nt_frost), ('flamethrower', X.nt_flamer),
          ('plasma_saw', X.nt_saw), ('meteor_mortar', X.nt_mortar), ('prism_beacon', X.nt_prism)]
PLANET = {3: 'mars_gas', 4: 'saturn', 5: 'jelly_twin'}


def out(sub, name, im):
    p = os.path.join(ART, sub, name + '.png')
    os.makedirs(os.path.dirname(p), exist_ok=True)
    (im if isinstance(im, Image.Image) else Image.fromarray(im)).save(p)
    return p


def white(im):
    a = np.array(im); w = np.zeros_like(a); w[..., :3] = 255; w[..., 3] = a[..., 3]
    return Image.fromarray(w)


def export_enemy():
    k = 1.1   # earth_export.export_enemy 와 같음
    for eid, fn in NORMAL + ELITE:
        im = A.piece(fn(), 220, 220, k, A.WOB['enemy'], .018)
        out('Enemy', eid, im); out('Enemy', eid + '_w', white(im))
    for eid, fn in BOSS:
        im = A.piece(fn(), 320, 320, k, A.WOB['enemy'], .018)
        out('Enemy', eid, im); out('Enemy', eid + '_w', white(im))
    print('enemy', len(NORMAL + ELITE + BOSS))


def export_turret():
    for tid, fn in TURRET:
        out('Turret', tid, A.piece(fn(), 200, 222, 1.56, A.WOB['turret'], .018))
    print('turret', len(TURRET))


def sky_chapter(ch, w=1080, h=2400):
    """지금 하늘(earth_art.sky + 위 별 띠)과 같은 짜임에 챕터 색만 — 위 #c0 → 아래 #c2, 성운 얼룩 2색"""
    c0, c1, c2 = ch['sky']
    gid = f'skych_{ch["id"]}'
    A._DEFS[gid] = (f'<linearGradient id="{gid}" x1="0" y1="0" x2="0" y2="1"><stop offset="0" stop-color="{c0}"/>'
                    f'<stop offset=".62" stop-color="{c1}"/><stop offset="1" stop-color="{c2}"/></linearGradient>')
    rnd = random.Random(8)
    s = [f'<rect width="{w}" height="{h}" fill="url(#{gid})"/>']
    for x, y, r in ((180, 900, 260), (860, 1380, 300), (300, 1730, 240)):
        s.append(f'<g opacity=".42">{puff(x, y, r, ch["neb"][0], 0, 9, x)}</g>')
    for x, y, r in ((200, 860, 120), (820, 1420, 140)):
        s.append(f'<g opacity=".32">{puff(x, y, r, ch["neb"][1], 0, 7, y)}</g>')
    for _ in range(150):
        s.append(f'<circle cx="{rnd.randint(0, w)}" cy="{rnd.randint(0, h - 300)}" r="{rnd.uniform(1.6, 3.6):.1f}" fill="{ch["star"]}" opacity="{rnd.uniform(.4, .9):.2f}"/>')
    for _ in range(12):
        x, y, q = rnd.randint(40, w - 40), rnd.randint(80, h - 500), rnd.randint(9, 15)
        s.append(X.star4(x, y, q, ch['star'], 3))
    return ''.join(s)


def planet_svg(kind):
    """360x360 · 가운데 (180,180) · 반지름 78 (Bg/moon 원과 같은 크기)"""
    return X.draw_bg_planet(kind, 180, 180, 78 if kind != 'saturn' else 70)


def export_bg():
    for i in (2, 3, 4, 5):
        ch = X.CHAPTERS[i - 1]
        out('Bg', f'sky_ch{i}', A.R(sky_chapter(ch), 1080, 2400, A.WOB['bg'] * .5, .008))
    for i, kind in PLANET.items():
        out('Bg', f'planet_ch{i}', A.R(planet_svg(kind), 360, 360, A.WOB['char'], .02))
    print('bg sky 4 · planet', len(PLANET))


def sheet(dst):
    """확인용 모음 (게임 그림 그대로 붙임)"""
    tiles = [('Enemy', e[0]) for e in NORMAL + ELITE + BOSS] + [('Turret', t[0]) for t in TURRET] + [('Bg', f'planet_ch{i}') for i in PLANET]
    S, cols = 200, 8
    rows = (len(tiles) + cols - 1) // cols
    im = Image.new('RGB', (cols * S, rows * S), (46, 56, 102))
    for i, (sub, n) in enumerate(tiles):
        t = Image.open(os.path.join(ART, sub, n + '.png')).convert('RGBA')
        t.thumbnail((S - 16, S - 16))
        im.paste(t, ((i % cols) * S + (S - t.width) // 2, (i // cols) * S + (S - t.height) // 2), t)
    im.save(dst); print('모음', dst)


if __name__ == '__main__':
    want = set(a for a in sys.argv[1:] if not a.startswith('--'))
    if not want or 'enemy' in want: export_enemy()
    if not want or 'turret' in want: export_turret()
    if not want or 'bg' in want: export_bg()
    for a in sys.argv[1:]:
        if a.startswith('--sheet='): sheet(a[8:])
