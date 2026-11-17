# -*- coding: utf-8 -*-
"""밸런스 3차 (11-17, 밸런스_2차.html 의견) 표 고치기 — 여러 번 돌려도 같은 결과.
- Config: 장비 레벨 곱 성장 · 방어력 비율 · 엘리트 난이도 = 몇 스테이지 뒤의 적 · 무한 모드 시작 배율 · 골드 부스터 · 미션/출석 보상 커짐
- Chip.xlsx: 고정 값 회로(공격력 +70 · 기지 체력 +1400 · 보호막 2800)를 기지 값의 % 로 (기지가 곱으로 크면서 의미가 없어졌던 것)
- Shop.xlsx: 골드 부스터 30일 상품 [초안] · 골드 금고(다이아 → 순찰 100시간치 골드, 하루 3번)
- String: 방어력 · 골드 부스터 글자
터렛 피해율은 tools/balance3_turrets.py · 스테이지 배율은 tools/balance2_apply.py (bal_fit 결과).
사용: PYTHONUTF8=1 python tools/balance3_tables.py
"""
import os, sys
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from phase2_tables import TB, add_rows
from openpyxl import load_workbook


def upsert(ws, key_col, rows):
    """키가 있으면 그 칸들을 고치고, 없으면 줄을 더함"""
    head = [c.value for c in ws[1]]; ki = head.index(key_col) + 1
    at = {ws.cell(r, ki).value: r for r in range(2, ws.max_row + 1) if ws.cell(r, ki).value}
    new = []
    for row in rows:
        if row[key_col] in at:
            for k, v in row.items():
                if k in head: ws.cell(at[row[key_col]], head.index(k) + 1, v)
        else: new.append(row)
    if new: add_rows(ws, key_col, new)
    return len(rows) - len(new), len(new)


CONFIG = [
    ('Gear_Lv_Growth', 1.02, '밸런스 3차: 장비 공격력 · 기지 체력이 장비 레벨마다 곱으로도 커짐 (1 = 예전)'),
    ('Reduce_K', 240, '밸런스 3차: 방어력 → 받는 피해 x K/(K+방어력) (0 = 예전처럼 고정 값을 뺌)'),
    ('Elite_Mode_Offset', 10, '밸런스 3차: 엘리트 난이도 = 이만큼 뒤 스테이지의 적 (엘리트 5 = 보통 15 의 적)'),
    ('Elite_Mode_HP', 1, '엘리트 난이도 적 체력 x — 밸런스 3차: 2.6 → 1 (Elite_Mode_Offset 이 맡음)'),
    ('Elite_Mode_Atk', 1, '엘리트 난이도 적 공격력 x — 밸런스 3차: 1.6 → 1'),
    ('Chapter_Over_HP', 1.3, '밸런스 3차: 스테이지 표 끝을 넘는 엘리트(91~100)는 10 스테이지마다 적 체력 x'),
    ('Endless_Start_Mul', 0.45, '밸런스 3차: 무한 모드 처음 적 체력 = 깬 최고 스테이지 적 x (1분마다 Endless_Growth 배) — 0.3 이면 5~14분을 버팀 → 4~6분쯤'),
    ('Survival_HP', 0.75, '엘리트 군단 적 체력 x — 밸런스 3차: 1.4 → 0.75 (51판 중 7승 → 0.6 은 84% 로 쉬움 → 60~70% 쯤)'),
    ('Crit_Dmg', 1.5, '기본 치명 배율 — 밸런스 3차: 1.25 → 1.5 (치명 카드 · 회로가 거의 의미 없었음)'),
    ('Survival_Elite_Gap', 15, '엘리트 군단 중간 보스 간격(초) — 밸런스 3차: 12 → 15'),
    ('Endless_Atk_Growth', 1.12, '밸런스 3차: 무한 모드 1분마다 적 공격력 x'),
    ('Endless_Gold_Rate', 0.25, '밸런스 3차: 무한 모드 1분 버티면 그 스테이지 승리 골드의 이 배율'),
    ('Gold_Boost_Days', 30, '밸런스 3차: 골드 부스터 기간(일)'),
    ('Gold_Boost_Mul', 2, '밸런스 3차: 골드 부스터 판 골드 x'),
    ('Reward_Scale', 1, '밸런스 3차: 1 = 미션 · 출석 보상이 진행에 맞춰 커짐 (표 값 = 스테이지 1 기준)'),
    ('Reward_Res_Step', 25, '밸런스 3차: 미션 · 출석의 조각 · 부품은 최고 스테이지 이만큼마다 +1배'),
]
p = os.path.join(TB, 'Config.xlsx'); wb = load_workbook(p); ws = wb.active
print('Config 고침/더함', upsert(ws, 'Key', [dict(Key=k, Value=v, Note=n) for k, v, n in CONFIG])); wb.save(p)

p = os.path.join(TB, 'Chip.xlsx'); wb = load_workbook(p); ws = wb[wb.sheetnames[0]]
print('Chip', upsert(ws, 'Id', [
    dict(Id='ch_atk', Desc='기지 공격력 +{0}%', Stat='base_atk', Max=10),
    dict(Id='ch_atk_w', Desc='기지 공격력 +{0}%', Stat='base_atk', Max=20),
    dict(Id='ch_atk_pct', Desc='장비 공격력 +{0}%'),
    dict(Id='ch_hp', Desc='기지 체력 +{0}%', Stat='base_hp', Max=14),
    dict(Id='ch_shield', Desc='시작할 때 기지 체력 {0}% 만큼 보호막', Stat='shield_pct', Max=28),
    dict(Id='ch_reduce', Desc='방어력 +{0}'),
])); wb.save(p)

p = os.path.join(TB, 'Card.xlsx'); wb = load_workbook(p); ws = wb[wb.sheetnames[0]]
# 측정(tools/bal_turret.sh turret)에서 효과가 0 이던 카드: 불바퀴는 맞히는 횟수보다 시간(15초)이 먼저 끝남 → 지속 시간으로
print('Card', upsert(ws, 'Id', [
    dict(Id='w_hits', Desc='불바퀴가 굴러다니는 시간 +30%', Stat1='dur', Val1=30),
    dict(Id='g_crit', Desc='대장 카피 치명 확률 +20%', Val1=20),
])); wb.save(p)

p = os.path.join(TB, 'Shop.xlsx'); wb = load_workbook(p)
print('Product', upsert(wb['Product'], 'Id', [dict(Id='p_boost', Name='골드 부스터', Dia=0, Price_KRW=9900, First_Double=0, Product_Id='earth_gold_boost', Kind='boost')]))
print('Item', upsert(wb['Item'], 'Id', [dict(Id='i_gold3', Name='골드 금고', Kind='gold', Cost_Dia=1500, Value=100, Per_Day=3, Desc='순찰 100시간치 골드')]))
wb.save(p)

p = os.path.join(TB, 'String.xlsx'); wb = load_workbook(p); ws = wb['String']
print('String', upsert(ws, 'Key', [dict(Key=k, KO=v) for k, v in [
    ('st_reduce', '방어력'), ('st_reduce_total', '받는 피해'),
    ('boost_desc', '{0}일 동안 판 골드 {1}배'), ('boost_left', '부스터 {0}일 남음'),
]])); wb.save(p)
print('ok')
