# -*- coding: utf-8 -*-
"""밸런스 2차 (11-17, 밸런스_1차.html 의견) 표 고치기 — 여러 번 돌려도 같은 결과.
- Stage.xlsx: Weak 칸 (스테이지 약점 — 이 스테이지 적은 모두 이 속성에 약함, 보스 포함) + 적 구성을 그 약점 위주로(60% 이상)
  테마(10 스테이지마다 되풀이): 운석 소나기 물리 · 나방 떼 불 · 거미 언덕 중력 · 얼음 꼬리 불 · 젤리 습격 빛 · 차가운 밤 물리 ·
  단단한 껍질 중력 · 하늘의 접시 번개 · 끈적한 비 빛 · 쇠 비 번개  (속성마다 두 번)
  중력이 약점인 적은 없어서 중력 테마는 적 구성을 그대로 둠 (스테이지 약점으로 모두 중력에 약함)
- Config: Weak_Mul 1.5 → 2 · 보스 분노(Boss_Rage_*) · 10 스테이지 계단(Chapter_Step_*)
- String: stage_weak_hint · boss_rage
사용: PYTHONUTF8=1 python tools/balance2_tables.py   (원래 적 구성은 Stage.xlsx 의 Enemies_Old 칸에 남겨 두고, 그걸 기준으로 다시 만듦)
"""
import math, os, sys
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from phase2_tables import TB, add_rows, ensure_col
from openpyxl import load_workbook
import csv

THEME_WEAK = ['phys', 'fire', 'grav', 'fire', 'light', 'phys', 'grav', 'bolt', 'light', 'bolt']
POOL = {'phys': ['pebble', 'crack'], 'fire': ['moth', 'spider', 'ice'], 'light': ['jelly', 'split', 'fire', 'sdrone'], 'bolt': ['iron', 'beetle', 'ufo']}
SWARM = {'phys': 'pebble', 'fire': 'moth', 'light': 'jelly_mini', 'bolt': 'ufo'}
ELITE = {'phys': 'big', 'fire': 'moth_king', 'bolt': 'beetle_steel'}

enemy_weak = {r['Id']: r['Weak'] for r in csv.DictReader(open(os.path.join(TB, '..', 'Assets', 'Resources', 'Tables', 'Enemy.csv'), encoding='utf-8-sig'))}

p = os.path.join(TB, 'Stage.xlsx'); wb = load_workbook(p); ws = wb[wb.sheetnames[0]]
wi = ensure_col(ws, 'Weak'); oi = ensure_col(ws, 'Enemies_Old')
head = [c.value for c in ws[1]]
si, ei, swi, eli = head.index('Stage') + 1, head.index('Enemies') + 1, head.index('Swarm') + 1, head.index('Elite') + 1
changed = 0
for r in range(2, ws.max_row + 1):
    n = ws.cell(r, si).value
    if not n: continue
    n = int(n)
    old = ws.cell(r, oi).value or ws.cell(r, ei).value
    ws.cell(r, oi, old)
    W = THEME_WEAK[(n - 1) % 10]
    ws.cell(r, wi, W)
    orig = [x.strip() for x in str(old).split(',') if x.strip()]
    if W in POOL:
        match = [e for e in orig if enemy_weak.get(e) == W]
        for e in POOL[W]:
            if e not in match and len(match) < max(2, len([x for x in orig if True]) * 6 // 10): match.append(e)
        if len(match) < 2: match += [e for e in POOL[W] if e not in match][:2 - len(match)]
        others = [e for e in orig if enemy_weak.get(e) != W]
        keep = int(math.floor(len(match) * 0.4 / 0.6 + 1e-9))
        new = match + others[:keep]
        ws.cell(r, ei, ','.join(new))
        if enemy_weak.get(ws.cell(r, swi).value) != W: ws.cell(r, swi, SWARM[W])
        if W in ELITE and enemy_weak.get(ws.cell(r, eli).value) != W: ws.cell(r, eli, ELITE[W])
    else:
        ws.cell(r, ei, ','.join(orig))
    changed += 1
wb.save(p)
print('Stage', changed)

wb = load_workbook(os.path.join(TB, 'Config.xlsx')); ws = wb.active
head = [c.value for c in ws[1]]; ki, vi = head.index('Key') + 1, head.index('Value') + 1
rows = {ws.cell(r, ki).value: r for r in range(2, ws.max_row + 1)}
if 'Weak_Mul' in rows: ws.cell(rows['Weak_Mul'], vi, 2)
print('Config +', add_rows(ws, 'Key', [dict(Key=k, Value=v, Note=n) for k, v, n in [
    ('Boss_Rage_At', 0.5, '밸런스 2차 보스 패턴: 체력이 이 비율 아래로 처음 내려가면 화냄 (0 = 끔)'),
    ('Boss_Rage_Time', 4, '보스가 화나서 돌진하는 시간(초)'),
    ('Boss_Rage_Summon', 6, '화날 때 한꺼번에 부르는 부하 수'),
    ('Boss_Rage_Speed', 2.5, '돌진 속도 x'),
    ('Chapter_Step_From', 20, '밸런스 2차 "10 스테이지마다 툭툭 · 20 부터": 이 스테이지부터 10 스테이지마다 한 계단 (0 = 끔)'),
    ('Chapter_Step_HP', 1, '계단 하나마다 적 체력 x (그 안의 스테이지는 Mon_HP_Growth 로 완만하게)'),
    ('Chapter_Step_Atk', 1, '계단 하나마다 적 공격력 x'),
]]))
wb.save(os.path.join(TB, 'Config.xlsx'))

wb = load_workbook(os.path.join(TB, 'String.xlsx')); ws = wb['String']
print('String +', add_rows(ws, 'Key', [dict(Key=k, KO=v, EN='') for k, v in [
    ('stage_weak_hint', '이 스테이지 적은 모두 {0} 공격에 약해요 (피해 {1}배)'),
    ('boss_rage', '보스가 화났어요! 부하가 몰려와요'),
]]))
wb.save(os.path.join(TB, 'String.xlsx'))
print('ok')
