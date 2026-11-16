# -*- coding: utf-8 -*-
"""2차 수정안(요청 사항/2차 수정안.docx, 11-17) 표 고치기 — 여러 번 돌려도 같은 값.
- 글자: 끼우기 → 착용 · 끼움 → 착용 중 · 기지 외형 단추 = 변경 / 사용 중
- 운석(보통 적)을 조금 크게 x1.25 (처음 값 기준으로 고정 값을 씀)
- 등급 색: 희귀 = 파랑 · 영웅 = 보라
- Config: 중간 보스 크기 x3 · 보스 x3.5 · 돌파한 터렛 색(붉게)
사용: PYTHONUTF8=1 python tools/fix2_tables.py
"""
import os, sys
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from phase2_tables import TB, add_rows
from openpyxl import load_workbook


def set_cells(book, sheet, key_col, col, values):
    p = os.path.join(TB, book); wb = load_workbook(p); ws = wb[sheet]
    cs = [c.value for c in ws[1]]; k = cs.index(key_col) + 1; c = cs.index(col) + 1
    for r in range(2, ws.max_row + 1):
        v = ws.cell(r, k).value
        if v in values: ws.cell(r, c).value = values[v]
    wb.save(p)


set_cells('String.xlsx', 'String', 'Key', 'KO', {'equip': '착용', 'equipped': '착용 중'})
set_cells('Enemy.xlsx', 'Enemy', 'Id', 'Size', {'pebble': 0.75, 'fire': 1.06, 'ice': 1.06, 'iron': 1.06, 'crack': 1.13})
set_cells('Gear.xlsx', 'Gear_Grade', 'Grade', 'Color', {3: '#62a8ec', 4: '#9b74dc'})
wb = load_workbook(os.path.join(TB, 'String.xlsx')); ws = wb['String']
print('String +', add_rows(ws, 'Key', [dict(Key=k, KO=v, EN='') for k, v in [
    ('frame_change', '변경'), ('frame_using', '사용 중'), ('merge_title', '일괄 합성'), ('merge_gear', '장비 합성'), ('merge_chip', '칩 합성'),
    ('merge_none', '합성할 것이 없어요'), ('merge_can', '{0}번 합성할 수 있어요'), ('up_title', '강화'), ('up_dmg', '피해'), ('up_cd', '공격 속도'),
    ('up_next_cd', 'Lv {0} 에서 공격 속도가 빨라져요'), ('up_hp', '기지 체력'), ('up_atk', '공격력'), ('picked_cards', '고른 카드 {0}장'),
    ('tap_turret_cards', '터렛 그림을 누르면 고른 카드 목록'), ('kind_times', 'x{0}'),
]]))
wb.save(os.path.join(TB, 'String.xlsx'))
wb = load_workbook(os.path.join(TB, 'Config.xlsx')); ws = wb.active
print('Config +', add_rows(ws, 'Key', [dict(Key=k, Value=v, Note=n) for k, v, n in [
    ('Elite_Size_Mul', 3, '중간 보스 크기 x (2차 수정안 "3배")'), ('Boss_Size_Mul', 3.5, '보스 크기 x (2차 수정안 "매우매우 크게")'),
    ('Break_Hue', 0.0, '돌파한 터렛 색 (0 = 빨강 · 0~1 색상환) — 2차 수정안 "돌파하면 붉은색"'), ('Break_Sat', 1.25, '돌파한 터렛 채도 x'),
]]))
wb.save(os.path.join(TB, 'Config.xlsx'))
print('ok')
