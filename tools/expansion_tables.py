# -*- coding: utf-8 -*-
"""11-21 콘텐츠 확장 제안서 → 게임 표 (제안서 의견: 몬스터 · 보스 "일단 이미지만 추가하여 스테이지에 배치" · 타워 "일단 이미지만 추가")
- Enemy.xlsx  새 적 16줄 (일반 8 · 엘리트 4 · 보스 4) — 능력치는 비슷한 기존 적을 따라감. 제안서의 새 기믹(사격 · 보호막 · 회복 · 동결 …)은 아직 없음
              약점(Weak) · 장갑 · 분열 · 소환은 지금 게임에 있는 규칙만 씀
- Stage.xlsx  스테이지 21~100 (Ch.2~5, 20 스테이지 = 1 챕터):
              · Enemies: 그 챕터에서 나올 수 있는 새 일반 적 1종을 더함 (스테이지 약점과 같은 약점을 먼저 · 이미 6종이면 마지막 1종과 바꿈)
              · Elite: 짝수 스테이지는 그 챕터의 새 엘리트 (홀수는 그대로)
              · Boss: 그 챕터 전용 보스 (Ch.2 서리 수정왕 · Ch.3 집게 요새 게 · Ch.4 토성 고리룡 · Ch.5 대왕 젤리 황제) — Ch.1 은 그대로
              · 원래 값은 Enemies_Pre · Elite_Pre · Boss_Pre 칸에 한 번만 남김 → 다시 돌려도 원래 값에서 새로 계산 · --revert 로 되돌림
- Turret.xlsx 새 시트 Turret_Soon (6종 — 터렛 탭 맨 아래 "곧 열리는 터렛" 칸에만, 전투엔 안 나옴)
- Config: Chapter_Len 20 · Chapter_Bg 1 · Turret_Soon_Show 1 · String: 곧 열리는 터렛 글자
사용: PYTHONUTF8=1 python tools/expansion_tables.py [--revert]   (그다음 표 → csv 는 Unity 배치 TableConverter)
"""
import os, sys
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from phase2_tables import TB, add_rows, ensure_col, cols, new_book
from fix9_tables import upsert
from openpyxl import load_workbook

E_HEAD = ['Id', 'Name', 'Rank', 'Family', 'Art', 'Move', 'Speed', 'Hp_Mul', 'Atk_Mul', 'Exp', 'Size', 'Pivot_Y', 'Weak', 'Resist', 'Armor', 'Crash', 'Split', 'Split_N', 'Summon']
# id, 이름, 등급, 계열, 움직임, 빠르기, 체력x, 공격x, 경험치, 크기, 피벗Y, 약점, 장갑, 충돌, 분열, 분열 수, 소환   (비슷한 기존 적 = 메모)
ENEMIES = [
    ('shooter_ufo', '레이저 정찰선', 'normal', 'ship', 'hover', 0.8, 1.3, 1, 3, 0.9, 0.5, 'bolt', 0, 0, '', 0, ''),         # 비행접시
    ('shield_bot', '보호막 위성', 'normal', 'ship', 'zigzag', 0.9, 1.2, 0.9, 3, 0.9, 0.5, 'bolt', 0, 0, '', 0, ''),          # 정찰 드론보다 단단
    ('heal_jelly', '회복 젤리', 'normal', 'jelly', 'walk', 0.7, 1.4, 0.9, 3, 0.85, 0.5, 'light', 0, 0, '', 0, ''),           # 젤리 바이러스
    ('frost_mite', '서리 진드기', 'normal', 'bug', 'fast', 1.4, 0.8, 0.9, 2, 0.8, 0.5, 'fire', 0, 0, '', 0, ''),             # 외계 거미
    ('bomb_rock', '화산 폭탄 운석', 'normal', 'meteor', 'fall', 1.5, 0.6, 1.2, 2, 0.9, 0.43, 'phys', 0, 1, '', 0, ''),       # 돌멩이 운석보다 단단
    ('mirror_cube', '거울 프리즘', 'normal', 'crystal', 'hover', 0.7, 1.5, 1, 3, 0.9, 0.5, 'grav', 0, 0, '', 0, ''),         # 비행접시
    ('blink_bug', '점멸 반딧불', 'normal', 'bug', 'zigzag', 1.1, 0.9, 1, 2, 0.85, 0.5, 'fire', 0, 0, '', 0, ''),            # 외계 나방
    ('lava_slime', '용암 젤리', 'normal', 'jelly', 'walk', 0.75, 1.5, 1, 3, 0.9, 0.5, 'light', 0, 0, '', 0, ''),             # 젤리 바이러스
    ('cryo_golem', '빙하 골렘', 'elite', 'crystal', 'walk', 0.35, 1.2, 1, 20, 1.5, 0.5, 'fire', 0.4, 0, '', 0, ''),          # 강철 딱정벌레
    ('carrier_pod', '포자 모체', 'elite', 'bug', 'zigzag', 0.5, 0.9, 1, 20, 1.5, 0.5, 'fire', 0, 0, '', 0, 'spider'),        # 왕나방 (거미를 낳음)
    ('prism_core', '거울 코어', 'elite', 'crystal', 'hover', 0.4, 1, 1, 20, 1.6, 0.5, 'grav', 0, 0, '', 0, 'mirror_cube'),   # 모선 (프리즘을 낳음)
    ('magma_titan', '용암 거석', 'elite', 'meteor', 'fall', 0.45, 1, 1, 20, 1.35, 0.43, 'phys', 0.2, 1, 'bomb_rock', 4, ''),  # 큰 소행성 (폭탄 운석 4개로 깨짐)
    ('frost_emperor', '서리 수정왕', 'boss', 'crystal', 'boss', 0.3, 1, 1, 40, 1.45, 0.5, 'fire', 0.1, 0, '', 0, 'frost_mite'),
    ('mecha_crab', '집게 요새 게', 'boss', 'ship', 'boss', 0.28, 1, 1, 40, 1.45, 0.5, 'bolt', 0.2, 0, '', 0, 'shooter_ufo'),
    ('ring_wyrm', '토성 고리룡', 'boss', 'meteor', 'boss', 0.3, 1, 1, 40, 1.45, 0.5, 'phys', 0.1, 0, '', 0, 'pebble'),
    ('jelly_king', '대왕 젤리 황제', 'boss', 'jelly', 'boss', 0.3, 1, 1, 40, 1.5, 0.5, 'light', 0.1, 0, '', 0, 'split'),
]
WEAK = {e[0]: e[11] for e in ENEMIES}
CH_NORMAL = {2: ['shooter_ufo', 'shield_bot', 'heal_jelly', 'frost_mite']}
CH_NORMAL[3] = CH_NORMAL[2] + ['bomb_rock', 'mirror_cube', 'lava_slime']
CH_NORMAL[4] = CH_NORMAL[3] + ['blink_bug']
CH_NORMAL[5] = CH_NORMAL[4]
CH_ELITE = {2: ['cryo_golem', 'carrier_pod'], 3: ['carrier_pod', 'prism_core', 'magma_titan'], 4: ['cryo_golem', 'magma_titan'], 5: ['prism_core']}
CH_BOSS = {2: 'frost_emperor', 3: 'mecha_crab', 4: 'ring_wyrm', 5: 'jelly_king'}
CH_LEN = 20

SOON = [   # 제안서 4장 — Id 는 tools/expansion_export.py 의 Turret 그림 이름
    ('boomerang', '별빛 부메랑', 'phys', 30, '왕복 곡선 관통, 날아가 꿰뚫고 되돌아오며 두 번 때려요'),
    ('frost_cannon', '서리 눈꽃포', 'light', 40, '얼음 결정탄 — 맞으면 얼음 파편이 튀어 느려지고 더 아프게 맞아요'),
    ('flamethrower', '용암 화염방사기', 'fire', 50, '방어선 바로 앞을 불길로 휩쓸어 화상을 쌓아요'),
    ('plasma_saw', '자기장 톱날', 'bolt', 60, '장갑을 뚫는 전기 톱날이 단단한 적에게 달라붙어요'),
    ('meteor_mortar', '유성 박격포', 'fire', 70, '적이 몰린 곳에 거대한 유성을 떨어뜨려요'),
    ('prism_beacon', '별빛 증폭탑', 'grav', 80, '옆 칸 터렛을 빠르게 하고 적에게 약점 표식을 새겨요'),
]
CONFIG = [
    ('Chapter_Len', CH_LEN, '11-21 콘텐츠 확장: 한 챕터 = 스테이지 몇 개 (하늘 · 하늘 행성이 바뀌는 단위)'),
    ('Chapter_Bg', 1, '11-21 1 = 챕터마다 하늘 색(Bg/sky_ch2~5) · 하늘 행성(Ch.3~5 Bg/planet_ch*)이 바뀜 · 0 = 예전 하늘 하나'),
    ('Turret_Soon_Show', 1, '11-21 1 = 터렛 탭 맨 아래 "곧 열리는 터렛" 6종(Turret_Soon 시트) 보임 · 0 = 숨김'),
]
STRINGS = [('turret_soon_head', '곧 열리는 터렛'), ('turret_soon_at', '스테이지 {0} 해금 예정 (업데이트)')]


def ch_of(s): return (s - 1) // CH_LEN + 1


def stage_new(s, pre_en, pre_el, pre_boss, weak):
    ch = ch_of(s)
    if ch < 2: return pre_en, pre_el, pre_boss
    ch = min(ch, 5)
    en = [x for x in pre_en.split(',') if x]
    pool = CH_NORMAL[ch]
    match = [e for e in pool if WEAK[e] == weak] or pool
    pick = match[(s // 10 + (s % 10) // 2) % len(match)]   # 같은 약점 스테이지끼리도 번갈아 (약점이 2 스테이지씩 짝으로 돎)
    if pick not in en:
        if len(en) >= 6: en[-1] = pick
        else: en.append(pick)
    els = CH_ELITE[ch]
    el = els[(s // 2) % len(els)] if s % 2 == 0 else pre_el
    return ','.join(en), el, CH_BOSS[ch]


def main(revert=False):
    # ---- 적
    p = os.path.join(TB, 'Enemy.xlsx'); wb = load_workbook(p); ws = wb.active
    rows = [dict(zip(E_HEAD, (i, n, rk, fam, i, mv, sp, hp, atk, xp, sz, pv, wk, '', ar, cr, spl, spn, sm)))
            for i, n, rk, fam, mv, sp, hp, atk, xp, sz, pv, wk, ar, cr, spl, spn, sm in ENEMIES]
    print('Enemy +', add_rows(ws, 'Id', rows)); wb.save(p)

    # ---- 스테이지
    p = os.path.join(TB, 'Stage.xlsx'); wb = load_workbook(p); ws = wb.active
    for name in ('Enemies_Pre', 'Elite_Pre', 'Boss_Pre'): ensure_col(ws, name)
    cs = cols(ws); ci = {n: cs.index(n) + 1 for n in cs if n}
    changed = 0
    for r in range(2, ws.max_row + 1):
        s = ws.cell(r, 1).value
        if not isinstance(s, (int, float)): continue
        s = int(s)
        cur = {k: ws.cell(r, ci[k]).value or '' for k in ('Enemies', 'Elite', 'Boss', 'Weak')}
        for k in ('Enemies', 'Elite', 'Boss'):
            if not ws.cell(r, ci[k + '_Pre']).value: ws.cell(r, ci[k + '_Pre'], cur[k])
        pre = {k: ws.cell(r, ci[k + '_Pre']).value or '' for k in ('Enemies', 'Elite', 'Boss')}
        en, el, bo = (pre['Enemies'], pre['Elite'], pre['Boss']) if revert else stage_new(s, pre['Enemies'], pre['Elite'], pre['Boss'], cur['Weak'])
        if (en, el, bo) != (cur['Enemies'], cur['Elite'], cur['Boss']):
            ws.cell(r, ci['Enemies'], en); ws.cell(r, ci['Elite'], el); ws.cell(r, ci['Boss'], bo); changed += 1
    wb.save(p); print('Stage 바뀐 줄', changed, '(되돌림)' if revert else '')

    # ---- 곧 열리는 터렛
    new_book(os.path.join(TB, 'Turret.xlsx'), {'Turret_Soon': (['Id', 'Name', 'Elem', 'Unlock', 'Desc'], 'Id', [list(x) for x in SOON],
                                                               '11-21 콘텐츠 확장 제안서 새 타워 — 그림만 (터렛 탭 "곧 열리는 터렛" 칸). 전투 규칙은 아직 없음')})
    p = os.path.join(TB, 'Turret.xlsx'); wb = load_workbook(p); ws = wb['Turret_Soon']   # 다시 돌리면 이름 · 설명을 새로
    upsert(ws, 'Id', [dict(zip(['Id', 'Name', 'Elem', 'Unlock', 'Desc'], x)) for x in SOON]); wb.save(p)
    # ---- Config · String
    p = os.path.join(TB, 'Config.xlsx'); wb = load_workbook(p); ws = wb.active
    print('Config', upsert(ws, 'Key', [dict(Key=k, Value=v, Note=n) for k, v, n in CONFIG])); wb.save(p)
    p = os.path.join(TB, 'String.xlsx'); wb = load_workbook(p); ws = wb['String']
    print('String', upsert(ws, 'Key', [dict(Key=k, KO=v) for k, v in STRINGS])); wb.save(p)
    print('ok')


if __name__ == '__main__':
    main('--revert' in sys.argv)
