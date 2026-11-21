# -*- coding: utf-8 -*-
"""11-21 사용자 "처음부터 시작하는 초기화 치트" — 치트 창 [처음부터 다시] 글자 (String 표)
사용: PYTHONUTF8=1 python tools/cheat_reset_tables.py   (그다음 표 → csv 는 Unity 배치 TableConverter)
주아 글꼴에 '…' '·' 없음 → 안 씀"""
import os, sys
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from phase2_tables import TB
from fix9_tables import upsert
from openpyxl import load_workbook

STRINGS = [
    ('cheat_reset', '처음부터 다시 (초기화)'),
    ('cheat_reset_ask', '모든 진행(스테이지, 장비, 재화, 외형, 설정)을 지우고 타이틀부터 다시 시작해요.\n되돌릴 수 없어요.'),
    ('cheat_reset_go', '초기화'),
    ('cheat_reset_done', '처음부터 다시 시작해요'),
]

if __name__ == '__main__':
    p = os.path.join(TB, 'String.xlsx'); wb = load_workbook(p); ws = wb['String']
    print('String', upsert(ws, 'Key', [dict(Key=k, KO=v) for k, v in STRINGS]))
    wb.save(p)
    print('ok')
