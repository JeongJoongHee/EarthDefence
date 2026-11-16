# -*- coding: utf-8 -*-
"""밸런스 1차 (11-17) — Config.xlsx 에 값 넣기 (있으면 고치고, 없으면 줄을 더함). 여러 번 돌려도 같은 결과.
측정 = PlayTest.RunBal (tools/bal_run.sh) · 요약 tools/bal_summary.py · 보고서 tools/balance_report.py
값을 바꾸면 여기 VALUES 를 고치고 다시 돌린 뒤, bal_run.sh 로 다시 잴 것.
사용: PYTHONUTF8=1 python tools/balance1_apply.py
"""
import os, sys
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from phase2_tables import TB, add_rows
from openpyxl import load_workbook

# (키, 값, 설명) — 지금 값과 바뀐 까닭은 밸런스_1차.html
VALUES = [
    # 적 (스테이지마다 커지는 힘)
    ('Mon_HP_Base', 1000, '스테이지 1 적 체력 (Hp_Mul 1 기준) — 밸런스 1차: 55 → 1000 (초반부터 맞게, 대신 성장은 완만하게)'),
    ('Mon_HP_Growth', 1.073, '스테이지마다 체력 배율 — 밸런스 1차: 1.16 → 1.073 (30 근처 벽을 없앰)'),
    ('Mon_Atk_Base', 30, '스테이지 1 적 공격력 — 밸런스 1차: 12 → 30'),
    ('Mon_Atk_Growth', 1.035, '스테이지마다 공격력 배율 — 밸런스 1차: 1.09 → 1.035 (후반 기지가 한 번에 녹지 않게)'),
    ('Run_HP_Ramp', 3, '한 판 안에서 적 체력이 끝 무렵 몇 배 더 — 밸런스 1차: 1.5 → 3 (압박을 판 뒤쪽 · 보스 무렵으로)'),
    ('Spawn_Rate_Per_Stage', 0.015, '스테이지마다 늘어나는 초당 적 수 — 밸런스 1차: 0.03 → 0.015 (후반 판 시작부터 몰려 무너지던 것)'),
    ('Boss_HP_Mul', 100, '보스 체력 = 일반 x — 밸런스 1차: 140 → 100 (보스 = 한 방 싸움이 아니라 깎이며 버티는 싸움)'),
    ('Boss_Atk_Mul', 4, '보스 공격력 x — 밸런스 1차: 6 → 4'),
    ('Elite_Atk_Mul', 2.5, '엘리트 공격력 x — 밸런스 1차: 3 → 2.5'),
    ('Early_Stage_N', 10, '밸런스 1차: 이 스테이지 아래는 적을 약하게 (튜토리얼 구간)'),
    ('Early_HP_Mul', 0.03, '밸런스 1차: 스테이지 1 적 체력 배율 → Early_Stage_N 에서 1 이 되게 곱으로 서서히 (공격력은 제곱근)'),
    ('Ten_Stage_HP_Mul', 0.8, '밸런스 1차: 10의 배수 스테이지(쇠 비 — 갑옷 · 운석 · 적 종류가 많음) 적 체력 x'),
    # 보상 · 성장
    ('Win_Gold_Base', 200, '승리 골드 (스테이지 1) — 밸런스 1차: 300 → 200'),
    ('Win_Gold_Per_Stage', 40, '스테이지마다 더하는 골드 — 밸런스 1차: 60 → 40'),
    ('Win_Gold_Growth', 1.065, '밸런스 1차: 승리 골드가 스테이지마다 곱으로도 커짐 (1 = 예전)'),
    ('Patrol_Gold_Growth', 1.065, '밸런스 1차: 순찰 골드가 최고 스테이지마다 곱으로도 커짐 (1 = 예전)'),
    ('Base_Cost_Base', 300, 'Lv1 → 2 골드 — 밸런스 1차: 120 → 300'),
    ('Base_Cost_Growth', 1.085, '레벨마다 비용 배율 — 밸런스 1차: 1.11 → 1.085'),
    ('Base_Atk_Growth', 1.04, '밸런스 1차: 기지 공격력이 레벨마다 곱으로도 커짐 (1 = 예전처럼 +4 만)'),
    ('Base_HP_Growth', 1.03, '밸런스 1차: 기지 체력이 레벨마다 곱으로도 커짐 (1 = 예전처럼 +120 만)'),
]

p = os.path.join(TB, 'Config.xlsx'); wb = load_workbook(p); ws = wb.active
head = [c.value for c in ws[1]]; ki, vi, ni = head.index('Key') + 1, head.index('Value') + 1, head.index('Note') + 1
rows = {ws.cell(r, ki).value: r for r in range(2, ws.max_row + 1) if ws.cell(r, ki).value}
changed, added = [], []
for k, v, n in VALUES:
    if k in rows:
        r = rows[k]
        if ws.cell(r, vi).value != v: changed.append(f'{k}: {ws.cell(r, vi).value} → {v}'); ws.cell(r, vi, v)
        ws.cell(r, ni, n)
    else:
        added.append(k)
add_rows(ws, 'Key', [dict(Key=k, Value=v, Note=n) for k, v, n in VALUES if k in added])
wb.save(p)
print('바꿈', len(changed)); [print('  ', c) for c in changed]
print('더함', added)
