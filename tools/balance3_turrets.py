# -*- coding: utf-8 -*-
"""밸런스 3차 — 터렛 세기 맞추기 (Turret.xlsx 의 Rate · Rate2 · Cooldown 만 고침, 나머지 칸은 그대로).
측정 = bash tools/bal_turret.sh <이름> "" feature  (터렛 하나를 주력으로 키운 판이 깨는 적 체력 배율 k50)
  조정 전: 쾅쾅 대포 2.1 · 유도 미사일 2.6 · 관통포 3.0 ↔ 벌집 8.3 · 찌릿 코일 10.3 · 집중 빔 11.3 (5배 차이, 대장 카피만 키우면 3.4)
  한 발이 세고 재장전이 긴 터렛(관통포 · 미사일 · 대포)은 피해율을 올려도 그대로 → 재장전을 줄임
원래 값(ORIG)에서 다시 계산하므로 여러 번 돌려도 같은 결과. 배율을 바꾸려면 MUL 을 고치고 다시 돌림.
사용: PYTHONUTF8=1 python tools/balance3_turrets.py
"""
import os, sys
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from phase2_tables import TB
from openpyxl import load_workbook

# 조정 전 값 (Rate, Rate2, Cooldown) — 2026-11-17 Turret.xlsx
ORIG = dict(rail=(260, 0, 4), laser=(28.8, 0, 6), drone=(18.55, 0, 10), hive=(200, 0, 12), missile=(193.5, 154.8, 8), bolt=(139.7, 0, 9),
            cannon=(421.1, 294.8, 7), focus=(145, 0, 10), vortex=(112.5, 0, 6), wheel=(51.8, 0, 10), tesla=(625.3, 0, 6))
# (피해율 배율, 재장전 배율)
MUL = dict(rail=(1.5, 0.65), laser=(1.9, 1), drone=(0.71, 1), hive=(0.46, 1), missile=(2.0, 0.55), bolt=(1.0, 1),
           cannon=(1.8, 0.5), focus=(0.33, 1), vortex=(1.8, 0.85), wheel=(1.7, 1), tesla=(0.32, 1))

p = os.path.join(TB, 'Turret.xlsx'); wb = load_workbook(p); ws = wb[wb.sheetnames[0]]
head = [c.value for c in ws[1]]; ci = {k: head.index(k) + 1 for k in ('Id', 'Rate', 'Rate2', 'Cooldown')}
for r in range(2, ws.max_row + 1):
    tid = ws.cell(r, ci['Id']).value
    if tid not in ORIG: continue
    (r1, r2, cd), (mr, mc) = ORIG[tid], MUL[tid]
    new = (round(r1 * mr, 2), round(r2 * mr, 2), round(cd * mc, 2))
    ws.cell(r, ci['Rate'], new[0]); ws.cell(r, ci['Rate2'], new[1]); ws.cell(r, ci['Cooldown'], new[2])
    print(f'{tid:8} 피해율 {r1:>7} → {new[0]:>7}' + (f' · 폭발 {r2} → {new[1]}' if r2 else '') + (f' · 재장전 {cd} → {new[2]}초' if mc != 1 else ''))
wb.save(p)
print('ok')
