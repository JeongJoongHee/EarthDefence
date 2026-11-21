# -*- coding: utf-8 -*-
"""11-21 비공개 테스트 1번 빌드 준비 — 실제 광고(AdMob) · 결제(Unity IAP) 글자 (String 표)
- 광고: 불러오는 중 · 못 띄움
- 결제: 지급 · 되살림 · 연결 중 · 승인 대기 · 확인 실패 · 취소 · 앞 결제 처리 중 · 실패 · 준비 중(iap_off 고침)
사용: PYTHONUTF8=1 python tools/closed1_tables.py   (그다음 표 → csv 는 Unity 배치 TableConverter)
주아 글꼴에 '…' '·' 없음 → 안 씀"""
import os, sys
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from phase2_tables import TB
from fix9_tables import upsert
from openpyxl import load_workbook

STRINGS = [
    ('ad_loading', '광고를 불러오는 중이에요. 잠시 뒤 다시 눌러 주세요'),
    ('ad_fail', '광고를 띄우지 못했어요. 잠시 뒤 다시 해 주세요'),
    ('iap_done', '{0} 구매 완료!'),
    ('iap_restored', '{0} 구매 기록을 되살렸어요'),
    ('iap_connecting', '스토어에 연결하는 중이에요. 잠시 뒤 다시 눌러 주세요'),
    ('iap_deferred', '결제 승인을 기다리는 중이에요'),
    ('iap_invalid', '결제를 확인하지 못했어요'),
    ('iap_cancel', '결제를 취소했어요'),
    ('iap_pending', '앞의 결제를 처리하는 중이에요'),
    ('iap_fail', '결제하지 못했어요'),
]
# 치트: 테스트 빌드는 켬 (사용자 의견 "추가해도 무방" → h-cheat) · 프로덕션 신청 전 마지막 빌드에서 0 (aab_check --prod 가 막음)
CONFIG = [('Cheat_On', 1, '11-17 치트 창: 1 = 스토어 빌드에서도 보임 (에디터 · 개발용 빌드는 늘 보임) — 11-21 비공개 테스트 빌드는 1 · 프로덕션에 나갈 마지막 빌드 전 0 (aab_check --prod 가 확인)')]
FORCE = {'iap_off': '결제는 준비 중이에요'}   # 예전 글("출시 때 열림" 등)을 덮어씀

if __name__ == '__main__':
    p = os.path.join(TB, 'String.xlsx'); wb = load_workbook(p); ws = wb['String']
    print('String', upsert(ws, 'Key', [dict(Key=k, KO=v) for k, v in STRINGS]))
    head = [c.value for c in ws[1]]
    ki, ko = head.index('Key') + 1, head.index('KO') + 1
    for r in range(2, ws.max_row + 1):
        k = ws.cell(r, ki).value
        if k in FORCE and ws.cell(r, ko).value != FORCE[k]:
            print('고침', k, ws.cell(r, ko).value, '→', FORCE[k]); ws.cell(r, ko, FORCE[k])
    wb.save(p)
    p = os.path.join(TB, 'Config.xlsx'); wb = load_workbook(p); ws = wb.active
    print('Config', upsert(ws, 'Key', [dict(Key=k, Value=v, Note=n) for k, v, n in CONFIG])); wb.save(p)
    print('ok')
