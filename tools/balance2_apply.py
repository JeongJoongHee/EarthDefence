# -*- coding: utf-8 -*-
"""밸런스 2차 (11-17) — 무과금 100 스테이지 ≈ 360일 (하루 1시간 30분 = 18판) 값 넣기. 여러 번 돌려도 같은 결과.
- Stage.xlsx Hp_Mul = 목표 날짜표에서 거꾸로 맞춘 스테이지별 적 체력 배율 (tools/bal_fit.py 결과 파일, 20 부터 10 스테이지 계단)
- Config: 계단 안 오름 · 계단 공격력 · 장비 떨어지는 등급 · 보상 성장
먼저 balance2_tables.py (스테이지 약점 · 보스 분노 · 계단 칸) 를 돌린 뒤에.
사용: PYTHONUTF8=1 python tools/balance2_apply.py [배율 파일 — 기본 tools/bal2_stage_mul.txt]
"""
import os, sys
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from phase2_tables import TB, add_rows
from openpyxl import load_workbook

HERE = os.path.dirname(os.path.abspath(__file__))
MUL = sys.argv[1] if len(sys.argv) > 1 else os.path.join(HERE, 'bal2_stage_mul.txt')
VALUES = [
    ('Mon_HP_Growth', 1.02, '스테이지마다 체력 배율 — 밸런스 2차: 1.073 → 1.02 (계단 안은 완만하게, 오름은 Stage 표 Hp_Mul 계단에서 "툭")'),
    ('Chapter_Step_HP', 1, '밸런스 2차: 계단 체력은 Stage 표 Hp_Mul 에 넣어 둠 (여기는 1)'),
    ('Chapter_Step_Atk', 1.2, '밸런스 2차: 계단 하나마다 적 공격력 x'),
    ('Drop_Grade_Step', 25, '스테이지 몇 개마다 기본 등급 +1 — 밸런스 2차: 12 → 25 (무과금은 100 에서 전설 · 신화부터는 합성 · 상자)'),
    ('Win_Gold_Growth', 1.05, '밸런스 2차: 1.065 → 1.05'),
    ('Patrol_Gold_Growth', 1.05, '밸런스 2차: 1.065 → 1.05'),
]
p = os.path.join(TB, 'Config.xlsx'); wb = load_workbook(p); ws = wb.active
head = [c.value for c in ws[1]]; ki, vi, ni = head.index('Key') + 1, head.index('Value') + 1, head.index('Note') + 1
rows = {ws.cell(r, ki).value: r for r in range(2, ws.max_row + 1) if ws.cell(r, ki).value}
for k, v, n in VALUES:
    if k in rows: ws.cell(rows[k], vi, v); ws.cell(rows[k], ni, n)
    else: add_rows(ws, 'Key', [dict(Key=k, Value=v, Note=n)])
wb.save(p)

mul = [float(x) for x in open(MUL).read().split(',') if x.strip()]
p = os.path.join(TB, 'Stage.xlsx'); wb = load_workbook(p); ws = wb[wb.sheetnames[0]]
head = [c.value for c in ws[1]]; si, hi = head.index('Stage') + 1, head.index('Hp_Mul') + 1
for r in range(2, ws.max_row + 1):
    n = ws.cell(r, si).value
    if n and 1 <= int(n) <= len(mul): ws.cell(r, hi, mul[int(n) - 1])
wb.save(p)
print('Config', len(VALUES), '· Stage Hp_Mul', len(mul), '(1:', mul[0], '20:', mul[19], '100:', mul[-1], ')')
