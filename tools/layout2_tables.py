# -*- coding: utf-8 -*-
"""11-18 전투 배치 2판 (전투배치_시안.html — ③ 우주 사령탑) 표 고치기 — 여러 번 돌려도 같은 결과.
- Config: 방어선 x · 적 속도 배율 · 적이 나오는 곳 · 내려오는 각도 · 대장 카피 고정 · 터렛 머리 각도 · 자리 고르기 · 칸 순서 · 로비 자리
- String: 자리 고르기 글자
그림은 tools/tower_art.py (earth_export.py tower) · 시안은 tools/layout_mock.py command.
사용: PYTHONUTF8=1 python tools/layout2_tables.py
"""
import os, sys
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from phase2_tables import TB, add_rows
from openpyxl import load_workbook


def upsert(ws, key_col, rows):
    """키가 있으면 그 칸들을 고치고, 없으면 줄을 더함 (balance3_tables.py 와 같음)"""
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
    ('Shield_X', -2.1, '11-18 사령탑: 방어선 x — 적은 이 선(사령탑 · 터렛 오른쪽)에 닿으면 멈춰 때리거나 부딪쳐 터짐'),
    ('Enemy_Speed_Mul', 0.72, '11-18 사령탑: 적 속도 x — 사령탑까지 오는 길(평균 10.5칸)이 예전(14.8칸)보다 짧아 오는 시간을 같게 (밸런스 3차에서 다시)'),
    ('Spawn_Top_Share', 0.65, '11-18: 적이 위 가장자리에서 나오는 비율 (나머지는 오른쪽 가장자리)'),
    ('Spawn_Top_X0', -0.5, '11-18: 위 가장자리에서 나오는 x 왼쪽 끝 (오른쪽 끝 = 화면 끝)'),
    ('Spawn_Right_Y0', 0, '11-18: 오른쪽 가장자리에서 나오는 높이 아래 끝'),
    ('Approach_Min', 25, '11-18: 적이 내려오는 각도 최소(도, 수평 = 0) — 사용자 "대각선 오른쪽 위에서 왼쪽 아래로 떨어지듯"'),
    ('Approach_Max', 70, '11-18: 적이 내려오는 각도 최대(도)'),
    ('Guardian_Fixed', 1, '11-18: 1 = 대장 카피는 가운데 사령실에 고정(끌기 없음) · 0 = 예전처럼 좌우로 끌기'),
    ('Head_Rest_Deg', 45, '11-18: 터렛 머리(포신 · 미사일 · 수정)가 쉴 때 보는 각도(도) — 사용자 "45도 · 머리만"'),
    ('Head_Turn', 240, '11-18: 터렛 머리가 적 쪽으로 도는 빠르기(도/초)'),
    ('Slot_Pick', 1, '11-18: 1 = 새 터렛 카드를 고르면 놓을 자리를 고름 (빈 칸이 2개 이상일 때) · 0 = 정해진 순서로 바로'),
    ('Slot_Order', 2314, '11-18: 자리를 안 고르면 이 순서로 (칸 1 = 맨 위 · 2 = 사령실 위 · 3 = 사령실 아래 · 4 = 달 바닥)'),
    ('Lobby_Btn_X', 1.55, '11-18: 로비 [전투 시작] · 순찰 단추 가운데 x (월드, 사령탑 오른쪽 하늘)'),
    ('Lobby_Walk_X', 0.8, '11-18: 로비에서 대장 카피가 걷는 곳 가운데 x (달 바닥)'),
    ('Hp_Plate_X', -3.6, '11-18: 전투 기지 체력판 x (사령탑 앞 달 바닥)'),
]
p = os.path.join(TB, 'Config.xlsx'); wb = load_workbook(p); ws = wb.active
print('Config 고침/더함', upsert(ws, 'Key', [dict(Key=k, Value=v, Note=n) for k, v, n in CONFIG])); wb.save(p)

p = os.path.join(TB, 'String.xlsx'); wb = load_workbook(p); ws = wb['String']
print('String', upsert(ws, 'Key', [dict(Key=k, KO=v) for k, v in [
    ('slot_pick', '{0} 놓을 자리를 골라 주세요'), ('slot_back', '카드 다시 고르기'),
]])); wb.save(p)
print('ok')

# 기지 외형 이름: 돔 → 사령탑 (사용자 "사령탑으로 변경")
p = os.path.join(TB, 'Frame.xlsx'); wb = load_workbook(p); ws = wb[wb.sheetnames[0]]
print('Frame', upsert(ws, 'Id', [dict(Id=k, Name=v) for k, v in [
    ('basic', '기본 사령탑'), ('prism', '무지개 사령탑'), ('frost', '얼음 사령탑'), ('thorn', '가시 사령탑'), ('bloom', '꽃 사령탑'), ('gold', '황금 사령탑'),
]])); wb.save(p)
p = os.path.join(TB, 'Config.xlsx'); wb = load_workbook(p); ws = wb.active
upsert(ws, 'Key', [dict(Key='Dome_Top_Y', Note='로비: 순찰 단추 아래 끝 높이 (월드) — 11-18 돔이 없어져 이름만 예전 그대로')]); wb.save(p)
print('ok')
