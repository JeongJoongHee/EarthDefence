# -*- coding: utf-8 -*-
"""6차 수정안 (요청 사항/6차 수정안.docx, 11-17) 표 고치기 — 여러 번 돌려도 같은 결과.
- Shop.xlsx Box_Rate: 상자에서 신화 · 궁극은 안 나옴(확률 0) → 빠진 0.8% 는 일반에 (합 100)
- Shop.xlsx Product: 다이아 폭탄 패키지 (월 1회 · 9,900원 · 다이아 10,000 — 참고영상.txt 메모) Kind monthly
- Config: Box_Grade_Max 5 · Box_Pity 55 · Endless_Exp_Mul · Endless_Pick_Gap · Toast_Fade
- 새 글자 (상점 패널 3개 · 천장 · 순찰 안내 창 · 월 1회)
값은 처음 값 (밸런스는 마지막). Config 값 하나로 되돌릴 수 있음 (Box_Grade_Max 7 · Box_Pity 0 · Endless_Exp_Mul 1 · Endless_Pick_Gap 0).
사용: PYTHONUTF8=1 python tools/fix6_tables.py
"""
import os, sys
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from phase2_tables import TB, add_rows, cols
from openpyxl import load_workbook

p = os.path.join(TB, 'Shop.xlsx'); wb = load_workbook(p)
ws = wb['Box_Rate']
c = cols(ws); gi, ri = c.index('Grade') + 1, c.index('Rate') + 1
rates = {}
for r in range(2, ws.max_row + 1):
    g = ws.cell(r, gi).value
    if g is not None: rates[int(g)] = r
if float(ws.cell(rates[6], ri).value or 0) > 0 or float(ws.cell(rates[7], ri).value or 0) > 0:
    moved = float(ws.cell(rates[6], ri).value or 0) + float(ws.cell(rates[7], ri).value or 0)
    ws.cell(rates[6], ri, 0); ws.cell(rates[7], ri, 0)
    ws.cell(rates[1], ri, round(float(ws.cell(rates[1], ri).value) + moved, 4))
    print('Box_Rate: 신화 · 궁극 0, 일반 +', moved)
ws = wb['Product']
print('Product +', add_rows(ws, 'Id', [dict(Id='p_month', Name='다이아 폭탄 패키지', Dia=10000, Price_KRW=9900, First_Double=0, Product_Id='earth_dia_month', Kind='monthly')]))
wb.save(p)

wb = load_workbook(os.path.join(TB, 'Config.xlsx')); ws = wb.active
print('Config +', add_rows(ws, 'Key', [dict(Key=k, Value=v, Note=n) for k, v, n in [
    ('Box_Grade_Max', 5, '6차: 상자에서 나오는 가장 높은 등급 (5 = 전설 · 신화부터는 합성으로만, 7 = 예전)'),
    ('Box_Pity', 55, '6차: 장비 · 회로 상자 천장 — 이 횟수째에 전설 확정, 전설이 나오면 다시 셈 (0 = 끔)'),
    ('Endless_Exp_Mul', 2.5, '6차: 무한 모드 레벨업 필요 경험치 x (1 = 예전)'),
    ('Endless_Exp_From', 5, '6차: 무한 모드 — 이 레벨부터 필요 경험치 x Endless_Exp_Mul (처음 몇 레벨은 그대로)'),
    ('Endless_Pick_Gap', 8, '6차: 무한 모드 레벨업 창 사이 최소 간격(게임 초, 0 = 예전)'),
    ('Toast_Fade', 0.4, '6차: 토스트가 사라질 때 흐려지는 시간(초)'),
]]))
wb.save(os.path.join(TB, 'Config.xlsx'))

wb = load_workbook(os.path.join(TB, 'String.xlsx')); ws = wb['String']
print('String +', add_rows(ws, 'Key', [dict(Key=k, KO=v, EN='') for k, v in [
    ('shop_tab_dia', '다이아'), ('shop_tab_gear', '장비'), ('shop_tab_res', '재화'),
    ('month_limit', '월 1회'), ('month_done', '이번 달 구매 완료'),
    ('pity_left', '전설 확정까지 {0}번'), ('pity_note', '{0}번 안에 전설 1개 확정 (전설이 나오면 다시 셈)'),
    ('box_max_note', '상자에서는 전설까지, 신화부터는 합성으로만'),
    ('patrol_popup', '{0} 동안 순찰했어요'), ('patrol_max', '최대 {0}시간까지 쌓여요'), ('patrol_ad', '광고 보고 2배'), ('patrol_wait', '아직 순찰 중이에요 ({0} 뒤부터 받기)'),
]]))
wb.save(os.path.join(TB, 'String.xlsx'))
print('ok')
