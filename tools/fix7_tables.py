# -*- coding: utf-8 -*-
"""11-18 7차 수정안 (요청 사항/7차 수정안.docx) 표 고치기 — 여러 번 돌려도 같은 결과.
- Config: 로비 큰 지구 · 단추 자리 · 대장 카피 걷는 곳 · 별똥별 · 적 Fade in · 위 UI 높이(Lv 게이지 아래 끝) · 달 자리 · 타이틀 · 자리 고르기 안내
- String: 타이틀 · 입장 방법(구글 / 게스트) · 자리 고르기 안내 두 줄
그림은 tools/tower_art.py (earth_export.py bg char tower).
사용: PYTHONUTF8=1 python tools/fix7_tables.py
"""
import os, sys
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from phase2_tables import TB, add_rows
from openpyxl import load_workbook


def upsert(ws, key_col, rows):
    """키가 있으면 그 칸들을 고치고, 없으면 줄을 더함 (layout2_tables.py 와 같음)"""
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
    ('Lobby_Earth_Top', -3.4, '7차: 로비 큰 지구 꼭대기 높이 (월드) — 대장 카피가 그 위를 걸음 (그림 Bg/earth_big, 반지름 7.8)'),
    ('Lobby_Btn_X', 0, '7차: 로비 [전투 시작] · 순찰 단추 가운데 x (월드) — 사령탑을 빼서 가운데'),
    ('Dome_Top_Y', -2.0, '7차: 로비 순찰 단추 아래 끝 높이 (월드) = 지구 위 대장 카피 머리 위 (타이틀 [게임 시작]도 이 높이)'),
    ('Lobby_Walk_X', 0, '7차: 로비에서 대장 카피가 걷는 곳 가운데 x (지구 꼭대기)'),
    ('Lobby_Walk_Range', 1.8, '7차: 로비 대장 카피가 가운데에서 좌우로 걷는 거리 (월드)'),
    ('Shoot_Star_Gap_Min', 1.2, '7차: 로비 별똥별 사이 시간 최소(초)'),
    ('Shoot_Star_Gap_Max', 3.2, '7차: 로비 별똥별 사이 시간 최대(초)'),
    ('Hud_H', 300, '위쪽 UI 높이(1080 폭 기준 픽셀) = Lv 게이지 아래 끝 — 적은 이 아래에서 나옴 (7차 "Lv 게이지 밑부터", 예전 210 은 스테이지 판까지만이라 Lv 게이지와 겹쳤음)'),
    ('Enemy_Fade_Len', 1.2, '7차: 적(운석)이 Lv 게이지 아래를 지나며 이 거리(칸) 동안 서서히 나타남 (중간 보스 · 보스 빼고)'),
    ('Moon_X', -4.45, '7차: 전투 달토끼 x (사령탑 위 왼쪽 위 — 적 · 탄은 방어선 오른쪽에만 다녀 겹치지 않음)'),
    ('Moon_Below_Hud', 1.15, '7차: 전투 달토끼 가운데 = 위 UI 아래 끝에서 이만큼 아래 (안테나보다는 위)'),
    ('Spawn_Below_Hud', 0.45, '위 UI 아래 끝에서 이만큼 아래가 적이 나오는 줄 — 7차: 운석 그림 윗끝이 Lv 게이지 아래에 딱 닿게(처음엔 투명) 0.6 → 0.45'),
    ('Title_On', 1, '7차: 1 = 켤 때 타이틀 → [게임 시작] → 구글 연동 / 게스트 · 0 = 바로 로비'),
    ('Slot_Tip_X', 110, '7차: 자리 고르기 안내 판 가운데 x (UI 픽셀, 화면 가운데 기준 — 왼쪽 칸 줄과 안 겹치게 오른쪽으로)'),
]
p = os.path.join(TB, 'Config.xlsx'); wb = load_workbook(p); ws = wb.active
print('Config 고침/더함', upsert(ws, 'Key', [dict(Key=k, Value=v, Note=n) for k, v, n in CONFIG])); wb.save(p)

p = os.path.join(TB, 'String.xlsx'); wb = load_workbook(p); ws = wb['String']
print('String', upsert(ws, 'Key', [dict(Key=k, KO=v) for k, v in [
    ('title_line1', '카피바라'),
    ('title_line2', '지구를 지켜라!'),
    ('title_sub', '우주에서 날아오는 운석을 막아 주세요'),
    ('title_start', '게임 시작'),
    ('title_enter', '입장하기'),
    ('title_enter_desc', '구글 계정을 연동하면 게임 기록이 저장돼요'),
    ('title_google', '구글 계정으로 시작'),
    ('title_guest', '게스트로 시작'),
    ('title_draft', '[초안] 구글 연동은 출시 단계에서 실제 로그인으로 바뀝니다'),
    ('title_google_ok', '구글 계정이 연동되었습니다'),
    ('title_guest_warn', '데이터가 저장되지 않아요!'),
    ('title_guest_desc', '게스트는 게임 기록이 계정에 저장되지 않아\n앱을 지우거나 폰을 바꾸면 기록이 사라집니다.\n설정에서 언제든 구글 계정을 연동할 수 있어요.'),
    ('title_guest_go', '게스트로 입장'),
    ('slot_pick2', '놓을 자리를 골라 주세요'),
]])); wb.save(p)
print('ok')
