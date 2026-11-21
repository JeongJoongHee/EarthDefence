# -*- coding: utf-8 -*-
"""11-21 전투 요청 3개 (대화로 받음): ① 대장 카피 좌우 이동 ② 대장 카피 · 돔 특수 능력 단추 ③ 속도 x0.5 · x2 추가
- Config: Guardian_Fixed 0 (끌어서 좌우 이동 다시 켬 — 11-18 사령탑 때 고정했던 것) · 특수 능력 값 · 단추 자리
- String: 단추 이름 · 쓸 때 알림
사용: PYTHONUTF8=1 python tools/fix11_tables.py   (그다음 표 → csv 는 Unity 배치 TableConverter)
주아 글꼴에 '…' '·' '—' 없음 → 화면 글자에 안 씀"""
import os, sys
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from phase2_tables import TB
from fix9_tables import upsert
from openpyxl import load_workbook

CONFIG = [
    ('Guardian_Fixed', 0, '11-18: 1 = 대장 카피 가운데 고정 · 0 = 화면을 끌면 좌우로 이동 (11-21 요청 "대장 카피의 좌우 이동" → 0, 범위 Guardian_X_Max)'),
    ('Skill_On', 1, '11-21 전투 특수 능력 단추 (왼쪽 아래 대장 카피 버닝 · 오른쪽 아래 돔 얼음 파동) · 0 = 숨김'),
    ('Skill_First_Wait', 10, '11-21 전투 시작 몇 초 뒤부터 특수 능력을 쓸 수 있나'),
    ('Burn_Time', 8, '11-21 버닝 지속 시간(초)'),
    ('Burn_Cd', 45, '11-21 버닝 쿨타임(초, 쓴 순간부터)'),
    ('Burn_Rate_Mul', 2, '11-21 버닝 중 대장 카피 공격 속도 x'),
    ('Burn_Dmg_Mul', 1.5, '11-21 버닝 중 대장 카피 피해 x'),
    ('Dome_Skill_Cd', 60, '11-21 돔 얼음 파동 쿨타임(초)'),
    ('Dome_Freeze_Time', 3, '11-21 얼음 파동: 화면의 적 모두 얼리는 시간(초)'),
    ('Dome_Dmg_Pct', 10, '11-21 얼음 파동: 일반 적 체력 % 감소 (최대 체력 기준)'),
    ('Dome_Dmg_Boss_Pct', 3, '11-21 얼음 파동: 엘리트 · 보스 체력 % 감소'),
    ('Dome_Boss_Slow', 0.5, '11-21 얼음 파동: 보스는 얼지 않는 규칙(기획서 6-2)이라 대신 이만큼 느려짐 (얼리는 시간 동안)'),
    ('Skill_Btn_X', 4.4, '11-21 특수 능력 단추 가로 자리 (월드, 왼쪽은 -) — 바깥 터렛 칸(±4.3) 위'),
    ('Skill_Btn_Y', 2.7, '11-21 특수 능력 단추 높이 (땅 높이 기준, 월드) — 바깥 터렛 머리 위 (처음 -1.15 는 바깥 터렛을 가렸음)'),
]
STRINGS = [('skill_burn', '버닝'), ('skill_dome', '얼음 파동'), ('skill_burn_on', '대장 카피 버닝!'), ('skill_dome_on', '얼음 파동!')]

if __name__ == '__main__':
    p = os.path.join(TB, 'Config.xlsx'); wb = load_workbook(p); ws = wb.active
    print('Config', upsert(ws, 'Key', [dict(Key=k, Value=v, Note=n) for k, v, n in CONFIG])); wb.save(p)
    p = os.path.join(TB, 'String.xlsx'); wb = load_workbook(p); ws = wb['String']
    print('String', upsert(ws, 'Key', [dict(Key=k, KO=v) for k, v in STRINGS])); wb.save(p)
    print('ok')
