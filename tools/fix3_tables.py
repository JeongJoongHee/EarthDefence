# -*- coding: utf-8 -*-
"""3차 수정안(요청 사항/3차 수정안.docx, 11-17) 표 고치기 — 여러 번 돌려도 같은 결과.
- "칩" → "회로" (글자 · 회로 이름 · 상점) · "스킨" → "외형"
- 등급별 장비 이름 (Gear.xlsx Gear_Name: 부위 x 등급) · 회로 이름 앞에 등급 말 (Gear_Grade Chip_Prefix)
- 외형 메뉴 · 회로 착용 안내 글자
사용: PYTHONUTF8=1 python tools/fix3_tables.py
"""
import os, sys
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from phase2_tables import TB, add_rows, new_book, ensure_col
from openpyxl import load_workbook


def replace_text(book, sheet, cols, pairs):
    p = os.path.join(TB, book); wb = load_workbook(p); ws = wb[sheet]
    cs = [c.value for c in ws[1]]; n = 0
    for col in cols:
        ci = cs.index(col) + 1
        for r in range(2, ws.max_row + 1):
            v = ws.cell(r, ci).value
            if isinstance(v, str):
                nv = v
                for a, b in pairs:
                    nv = nv.replace(a, b)
                if nv != v:
                    ws.cell(r, ci).value = nv; n += 1
    wb.save(p)
    return n


CHIP = [('칩', '회로')]
print('String', replace_text('String.xlsx', 'String', ['KO'], CHIP + [('터렛 스킨', '터렛 외형'), ('스킨', '외형'), ('끼우면 필살기', '착용하면 필살기')]))
print('Chip', replace_text('Chip.xlsx', 'Chip', ['Name', 'Desc'], CHIP))
print('Shop', replace_text('Shop.xlsx', 'Item', ['Name', 'Desc'], CHIP))

# 등급별 장비 이름 (3차 수정안 "등급별로 아이템 이름 다르게") — 등급 색과 같은 말 (푸른 = 희귀 · 보랏빛 = 영웅 · 노을빛 = 전설 · 붉은 = 신화 · 은하수 = 궁극)
NAMES = {
    'weapon': ['연습용 레이저 소총', '개량 레이저 소총', '푸른 섬광 소총', '보랏빛 플라즈마 소총', '노을빛 태양 소총', '붉은 혜성 소총', '은하수 소총'],
    'helmet': ['연습용 헬멧', '튼튼한 헬멧', '푸른 별빛 헬멧', '보랏빛 오로라 헬멧', '노을빛 왕관 헬멧', '붉은 성운 헬멧', '은하수 헬멧'],
    'suit': ['연습용 우주복', '개량 우주복', '푸른 은하 우주복', '보랏빛 성운 우주복', '노을빛 기사 우주복', '붉은 혜성 우주복', '은하수 우주복'],
    'shield': ['연습용 방패', '튼튼한 방패', '푸른 장막 방패', '보랏빛 결계 방패', '노을빛 수호 방패', '붉은 용 방패', '은하수 방패'],
    'core': ['연습용 코어', '개량 코어', '푸른 수정 코어', '보랏빛 별 코어', '노을빛 태양 코어', '붉은 초신성 코어', '은하수 코어'],
    'boots': ['연습용 부츠', '가벼운 부츠', '푸른 바람 부츠', '보랏빛 번개 부츠', '노을빛 날개 부츠', '붉은 불꽃 부츠', '은하수 부츠'],
}
rows = [(f'{p}_{g + 1}', p, g + 1, n) for p, ns in NAMES.items() for g, n in enumerate(ns)]
new_book(os.path.join(TB, 'Gear.xlsx'), {'Gear_Name': (['Key', 'Part', 'Grade', 'Name'], 'Key', rows, '등급별 장비 이름 (없으면 Gear_Part Name)')})
wb = load_workbook(os.path.join(TB, 'Gear.xlsx')); ws = wb['Gear_Grade']
ci = ensure_col(ws, 'Chip_Prefix')
pre = {1: '낡은', 2: '쓸만한', 3: '푸른', 4: '보랏빛', 5: '노을빛', 6: '붉은', 7: '은하수'}
for r in range(2, ws.max_row + 1):
    g = ws.cell(r, 1).value
    if g in pre and not ws.cell(r, ci).value:
        ws.cell(r, ci, pre[g])
wb.save(os.path.join(TB, 'Gear.xlsx'))

wb = load_workbook(os.path.join(TB, 'String.xlsx')); ws = wb['String']
print('String +', add_rows(ws, 'Key', [dict(Key=k, KO=v, EN='') for k, v in [
    ('looks', '외형'), ('looks_turret', '터렛 외형'), ('looks_base', '기지 외형'),
    ('chip_where', '어디에 착용할까요?'), ('chip_fit', '착용할 수 있는 부위'), ('chip_nofit', '이 회로는 착용할 수 없는 부위'),
    ('chip_tap_slot', '빈 칸을 누르면 착용, 회로가 있는 칸을 누르면 바꿔 끼워요'), ('chip_count', '회로 {0}/{1}'),
]]))
wb.save(os.path.join(TB, 'String.xlsx'))
print('ok')
