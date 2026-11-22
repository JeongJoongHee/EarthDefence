# -*- coding: utf-8 -*-
"""11-22 「오픈_스펙_기획서.html」(v0.2, 키 earthdef-launchspec-1122-v2) — 오픈 때 안 여는 것 숨기기 (표 쪽)
 ① 코스튬 10종만 보임 (나머지 17종 Enabled 0) · 탈것 10종만 (7종 0) · 기지 돔 기본 + 4종 (2종 0)
    돔 "희귀 2번(공격력)" 은 문서에서 미정(무지개 돔 중복) → 가시 돔(thorn) 을 열어 둠 — Frame.xlsx Enabled 한 칸으로 바꿈
 ② 등급 상한: 장비 6(신화)까지 · 회로 5(전설)까지 (Config Gear_Grade_Top · Chip_Grade_Top)
    상자 · 전투 드롭 · 보상 최대: 장비 5(전설) · 회로 4(영웅) — 그 위 한 등급은 합성으로만 (Box_Grade_Max · Chip_Box_Grade_Max)
 터렛 18종 · 스킨 17종 · 회로 86종은 그대로 다 엶 (문서 "그대로 유지").
 가격 · 보유/착용 능력치 · 액티브 스킬 · 무료 해금(출석 · 스테이지)은 문서 결정 칸이 비어 있어 아직 안 함.
사용: PYTHONUTF8=1 python tools/open_spec_tables.py   (다시 돌려도 같은 값) → 표 변환은 Unity 배치
되돌리기: Enabled 칸을 1(또는 비움) · Config 값을 7 / 5 로"""
import os, sys
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from phase2_tables import TB
from fix9_tables import upsert
from openpyxl import load_workbook

OPEN_COSTUME = ['basic', 'duck', 'sleepy', 'rain', 'dino', 'ninja', 'lion', 'panda', 'king', 'onsen']
OPEN_MOUNT = ['none', 'duckboat', 'star', 'ladybug', 'broom', 'seahorse', 'corgi', 'surf', 'unicorn', 'tub']
OPEN_FRAME = ['basic', 'prism', 'thorn', 'bloom', 'frost']

CONFIG = [
    ('Gear_Grade_Top', 6, '11-22 오픈 스펙: 장비 가장 높은 등급 (6 = 신화까지 · 7 궁극은 업데이트 때 · 7 = 예전)'),
    ('Chip_Grade_Top', 5, '11-22 오픈 스펙: 회로 가장 높은 등급 (5 = 전설까지 · 6~7 은 업데이트 때 · 7 = 예전)'),
    ('Box_Grade_Max', 5, '장비 상자 · 전투 드롭 · 보상에서 나오는 가장 높은 등급 (5 = 전설 · 신화는 합성으로만) — 11-22 부터 장비만'),
    ('Chip_Box_Grade_Max', 4, '11-22 오픈 스펙: 회로 상자 · 전투 드롭 · 보상 최대 등급 (4 = 영웅 · 전설은 합성으로만, 천장도 영웅 확정)'),
]

# 주아 글꼴에 '·' '…' 없음 → 쉼표로
STRINGS = [
    ('pity_left', '{1} 확정까지 {0}번'),
    ('pity_note', '{0}번 안에 확정: 장비 {1}, 회로 {2}'),
    ('box_max_note', '상자 최대: 장비 {0}, 회로 {1} (그 위는 합성으로만)'),
    ('rate_gear', '장비 상자'),
    ('rate_chip', '회로 상자'),
]


def enable(ws, open_ids):
    head = [c.value for c in ws[1]]
    if 'Enabled' not in head:
        # 메모 칸 앞의 빈 칸 자리에 새 칸을 끼움 (메모는 오른쪽으로 밀림)
        col = head.index(None) + 1 if None in head else len(head) + 1
        ws.insert_cols(col); ws.cell(1, col, 'Enabled'); head = [c.value for c in ws[1]]
    ci = head.index('Enabled') + 1
    on = off = 0
    for r in range(2, ws.max_row + 1):
        rid = ws.cell(r, 1).value
        if not rid: continue
        v = 1 if rid in open_ids else 0
        ws.cell(r, ci, v); on += v; off += 1 - v
    miss = [i for i in open_ids if i not in [ws.cell(r, 1).value for r in range(2, ws.max_row + 1)]]
    if miss: raise SystemExit(f'{ws.title}: 표에 없는 id {miss}')
    return on, off


def main():
    p = os.path.join(TB, 'Look.xlsx'); wb = load_workbook(p)
    print('Costume 보임/숨김', enable(wb['Costume'], OPEN_COSTUME))
    print('Mount 보임/숨김', enable(wb['Mount'], OPEN_MOUNT)); wb.save(p)
    p = os.path.join(TB, 'Frame.xlsx'); wb = load_workbook(p)
    print('Frame 보임/숨김', enable(wb.active, OPEN_FRAME)); wb.save(p)
    p = os.path.join(TB, 'Config.xlsx'); wb = load_workbook(p); ws = wb.active
    print('Config', upsert(ws, 'Key', [dict(Key=k, Value=v, Note=n) for k, v, n in CONFIG])); wb.save(p)
    p = os.path.join(TB, 'String.xlsx'); wb = load_workbook(p); ws = wb['String']
    print('String', upsert(ws, 'Key', [dict(Key=k, KO=v) for k, v in STRINGS])); wb.save(p)


if __name__ == '__main__':
    main()
