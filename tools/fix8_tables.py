# -*- coding: utf-8 -*-
"""11-19 8차 수정안 (요청 사항/8차 수정안.docx) 표 고치기 — 여러 번 돌려도 같은 결과.
- Config: 타이틀 단추 아래 · 0.7배 · 로비 단추 아래 · 별똥별 느리게 · 달토끼 오른쪽 위 · 끈끈이 장판 세로
- String: 전투 약점 · 게임 종료 · 외형 설명 짧게
- Frame: 기지 외형 설명 짧게 (Desc)
그림은 tools/tower_art.py earth_ground() (earth_export.py tower).
사용: PYTHONUTF8=1 python tools/fix8_tables.py
"""
import os, sys
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from phase2_tables import TB, add_rows
from openpyxl import load_workbook


def upsert(ws, key_col, rows):
    """키가 있으면 그 칸들을 고치고, 없으면 줄을 더함 (fix7_tables.py 와 같음 — import 하면 7차 값을 다시 넣어서 복사)"""
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
    ('Title_Btn_Bottom', 230, '8차: 타이틀 [게임 시작] 아래 끝 높이 (화면 아래에서 UI 픽셀) — "게임 시작 버튼 하단에 배치"'),
    ('Title_Btn_Scale', 0.7, '8차: 타이틀 [게임 시작] 크기 배율 (예전 1 = 640x170)'),
    ('Lobby_Btn_Bottom', 1, '8차: 1 = 로비 [전투 시작] · 순찰을 아래 탭 바로 위에 · 0 = 7차처럼 지구 위 대장 카피 머리 위(Dome_Top_Y)'),
    ('Lobby_Btn_Gap', 24, '8차: 로비 순찰 단추 아래 끝 ↔ 아래 탭 사이 (UI 픽셀)'),
    ('Shoot_Star_Speed_Min', 4.5, '8차: 별똥별 빠르기 최소 (칸/초, 예전 9) — "조금 더 천천히"'),
    ('Shoot_Star_Speed_Max', 6.5, '8차: 별똥별 빠르기 최대 (칸/초, 예전 13)'),
    ('Shoot_Star_Life_Min', 1.3, '8차: 별똥별 보이는 시간 최소 (초, 예전 0.7) — 느려진 만큼 길게'),
    ('Shoot_Star_Life_Max', 2.0, '8차: 별똥별 보이는 시간 최대 (초, 예전 1.1)'),
    ('Moon_X', 4.3, '8차: 전투 달토끼 x — 오른쪽 위 ("어느 정도 운석을 가려도 괜찮음", 운석보다 뒤에 그림) · 7차 = -4.45 (왼쪽 위)'),
    ('Moon_Below_Hud', 1.05, '전투 달토끼 가운데 = 위 UI 아래 끝에서 이만큼 아래'),
    ('Field_Vertical', 1, '8차: 1 = 끈끈이 장판을 세로 띠로 (적 무리가 있는 x 에 땅~위 UI) · 0 = 예전 가로 띠'),
]
p = os.path.join(TB, 'Config.xlsx'); wb = load_workbook(p); ws = wb.active
print('Config 고침/더함', upsert(ws, 'Key', [dict(Key=k, Value=v, Note=n) for k, v, n in CONFIG])); wb.save(p)

p = os.path.join(TB, 'String.xlsx'); wb = load_workbook(p); ws = wb['String']
print('String', upsert(ws, 'Key', [dict(Key=k, KO=v) for k, v in [
    ('battle_weak', '약점'),
    ('quit_title', '게임 종료'),
    ('quit_ask', '게임을 종료할까요?'),
    ('quit_btn', '게임 종료'),
    ('skin_bonus', '보유 효과: 피해 +{1}%'),
    ('skin_ult', '필살기: {0}'),
]])); wb.save(p)

# 외형 설명 짧게 ("외형에서 추가 설명을 간소화 — 짧고 간결하게")
p = os.path.join(TB, 'Frame.xlsx'); wb = load_workbook(p); ws = wb.active
print('Frame', upsert(ws, 'Id', [dict(Id=k, Desc=v) for k, v in [
    ('basic', '효과 없음'),
    ('prism', '한 번 버티기 (체력 {0}%)'),
    ('starsea', '{1}초마다 별똥별 비 ({0}%)'),
    ('frost', '{1}초마다 적 {0}초 얼림'),
    ('thorn', '때린 적에게 반격 ({0}%)'),
    ('bloom', '{1}초마다 체력 {0}% 회복'),
    ('gold', '골드 +{0}%'),
]])); wb.save(p)
# 끈끈이 드론 설명 (가로 → 세로 띠)
p = os.path.join(TB, 'Turret.xlsx'); wb = load_workbook(p); ws = wb.active
print('Turret', upsert(ws, 'Id', [dict(Id='drone', Desc='세로 띠 장판 · 틱마다 둔화',
                                       Info='적이 가장 많이 모인 곳에 땅에서 하늘까지 세로 띠 장판을 깔아요. 장판 안의 적은 틱마다 피해를 받고 점점 느려져요.')])); wb.save(p)
print('ok')
