# -*- coding: utf-8 -*-
"""요청 사항_05 (요청 사항/요청 사항_05.pptx, 11-17) 표 고치기 — 여러 번 돌려도 같은 결과.
- Shop.xlsx Product: Kind 칸 (dia · noads) + 광고 제거 상품 [초안]
- Shop.xlsx Item: 강화 재료 상자(parts_box) · 터렛 조각 상자(shard_box) — 다이아 · 하루 10번 · 큰 골드 주머니
- 새 글자 (착용 완료 · 회로 착용 확인 · 터렛 정보 칸 이름 · 강화 재료 안내 · 광고 제거)
값은 처음 값 (밸런스는 마지막).
사용: PYTHONUTF8=1 python tools/fix5_tables.py
"""
import os, sys
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from phase2_tables import TB, add_rows, ensure_col
from openpyxl import load_workbook

p = os.path.join(TB, 'Shop.xlsx'); wb = load_workbook(p)
ws = wb['Product']
ki = ensure_col(ws, 'Kind')
for r in range(2, ws.max_row + 1):
    if ws.cell(r, 1).value and not ws.cell(r, ki).value:
        ws.cell(r, ki, 'dia')
print('Product +', add_rows(ws, 'Id', [dict(Id='p_noads', Name='광고 제거', Dia=0, Price_KRW=5900, First_Double=0, Product_Id='earth_noads', Kind='noads')]))
ws = wb['Item']
print('Item +', add_rows(ws, 'Id', [
    dict(Id='i_parts', Name='강화 재료 상자', Kind='parts_box', Cost_Dia=100, Value=50, Per_Day=10, Desc='장비 강화 재료(부품) 50개'),
    dict(Id='i_shard', Name='터렛 조각 상자', Kind='shard_box', Cost_Dia=100, Value=10, Per_Day=10, Desc='열린 터렛 조각 10개 (무작위)'),
    dict(Id='i_gold2', Name='큰 골드 주머니', Kind='gold', Cost_Dia=450, Value=20, Per_Day=0, Desc='순찰 20시간치 골드'),
]))
wb.save(p)

wb = load_workbook(os.path.join(TB, 'String.xlsx')); ws = wb['String']
print('String +', add_rows(ws, 'Key', [dict(Key=k, KO=v, EN='') for k, v in [
    ('equipped_done', '착용되었습니다.'), ('chip_confirm', '회로 착용'), ('chip_confirm_q', '{0} {1}번 칸에 착용할까요?'),
    ('chip_swap', '지금 끼운 회로 {0} 은(는) 가방으로 돌아가요'), ('cancel', '취소'),
    ('ti_stat', '능력'), ('ti_desc', '설명'), ('ti_break', '돌파'),
    ('parts_label', '강화 재료'), ('parts_info', '부품 = 장비 강화 재료 (분해 · 상점 상자로 얻어요)'),
    ('noads_name', '광고 제거'), ('noads_desc', '광고 없이 바로 보상'), ('noads_done', '구매 완료'), ('r_parts_box', '강화 재료'),
]]))
wb.save(os.path.join(TB, 'String.xlsx'))
print('ok')
