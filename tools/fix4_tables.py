# -*- coding: utf-8 -*-
"""4차 수정안(요청 사항/4차 수정안.docx, 11-17) 표 고치기 — 여러 번 돌려도 같은 결과.
- 회로 이름 줄이기 ("… 회로" 떼고, 터렛 이름도 짧게) · 회로 앞 등급 말(Chip_Prefix) 비움
- 등급 이름 통일: 장비 이름 앞 말 = 등급 이름 (일반 · 고급 · 희귀 · 영웅 · 전설 · 신화 · 궁극) — 이름은 그림 모양에 맞춤
- 새 글자 (외형 탭 · 적 정보 · 순찰 광고 2배 · 출석 토스트 · 회로 창 · 강화 창 · 레벨별 능력)
- Config: Attend_Auto (1 = 로비에 오면 출석 자동으로 받고 토스트, 0 = 예전 창) · Patrol_Ad_Mul (2)
사용: PYTHONUTF8=1 python tools/fix4_tables.py
"""
import os, sys
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from phase2_tables import TB, add_rows
from openpyxl import load_workbook


def col(ws, name):
    cs = [c.value for c in ws[1]]
    return cs.index(name) + 1


def set_by_key(book, sheet, key, field, values):
    p = os.path.join(TB, book); wb = load_workbook(p); ws = wb[sheet]
    kc, fc = col(ws, key), col(ws, field); n = 0
    for r in range(2, ws.max_row + 1):
        k = ws.cell(r, kc).value
        if k in values and ws.cell(r, fc).value != values[k]:
            ws.cell(r, fc).value = values[k]; n += 1
    wb.save(p)
    return n


# ---- 회로 이름 (짧게)
TSHORT = dict(guardian='카피', rail='관통포', laser='레이저', drone='드론', hive='벌집', missile='미사일', bolt='벼락탑',
              cannon='대포', focus='집중 빔', vortex='소용돌이', wheel='불바퀴', tesla='코일')
CH = dict(ch_atk='공격', ch_atk_w='무기 공격', ch_atk_pct='공격 증폭', ch_crit='치명', ch_crit_dmg='치명 피해', ch_hp='기지 체력',
          ch_reduce='장갑', ch_shield='보호막', ch_immune='반사막', ch_heal_elite='회복', ch_heal_break='돌파 회복', ch_crit_hp='처형',
          ch_extra='연사', ch_refresh='새로고침', ch_choice='첫 카드', ch_break='돌파', ch_dmg='피해', ch_fire='불 속성', ch_bolt='번개 속성',
          ch_grav='중력 속성', ch_light='빛 속성', ch_phys='물리 속성', ch_hp70='선제', ch_elite='보스 사냥', ch_early='기선', ch_low='역습',
          ch_debuff='약점 공략', ch_near='근접 방어', ch_boss_on='긴장', ch_per_turret='포대', ch_per_card='수집', ch_cd='가속', ch_dur='지속')
for t, sh in TSHORT.items():
    CH[f'tc_{t}_dmg'] = f'{sh} 피해'; CH[f'tc_{t}_star'] = f'{sh} 별'; CH[f'tc_{t}_sp'] = f'{sh} 특수'
print('Chip', set_by_key('Chip.xlsx', 'Chip', 'Id', 'Name', CH))

# ---- 장비 이름 = 등급 이름 + 그림에 맞춘 이름
NAMES = {
    'weapon': ['일반 레이저 소총', '고급 레이저 소총', '희귀 조준 소총', '영웅 칼날 소총', '전설 에너지 소총', '신화 불꽃 소총', '궁극 날개 소총'],
    'helmet': ['일반 헬멧', '고급 헬멧', '희귀 안테나 헬멧', '영웅 지느러미 헬멧', '전설 볏 헬멧', '신화 뿔 헬멧', '궁극 왕관 헬멧'],
    'suit': ['일반 우주복', '고급 우주복', '희귀 보석 우주복', '영웅 기사 우주복', '전설 황금 우주복', '신화 망토 우주복', '궁극 날개 우주복'],
    'shield': ['일반 방패', '고급 방패', '희귀 보석 방패', '영웅 가시 방패', '전설 별 방패', '신화 날개 방패', '궁극 왕관 방패'],
    'core': ['일반 코어', '고급 코어', '희귀 수정 코어', '영웅 고리 코어', '전설 쌍고리 코어', '신화 불꽃 코어', '궁극 별 코어'],
    'boots': ['일반 부츠', '고급 부츠', '희귀 보석 부츠', '영웅 지느러미 부츠', '전설 분사 부츠', '신화 불꽃 부츠', '궁극 날개 부츠'],
}
print('Gear_Name', set_by_key('Gear.xlsx', 'Gear_Name', 'Key', 'Name', {f'{p}_{g + 1}': n for p, ns in NAMES.items() for g, n in enumerate(ns)}))
print('Chip_Prefix', set_by_key('Gear.xlsx', 'Gear_Grade', 'Grade', 'Chip_Prefix', {g: None for g in range(1, 8)}))

# ---- 글자
S = {'enemies_show': '적 정보', 'enemies_hide': '적 정보', 'chip_fit': '착용 가능 부위', 'weak_hint': '칸 아래 색 = 그 적의 약점'}
print('String fix', set_by_key('String.xlsx', 'String', 'Key', 'KO', S))
wb = load_workbook(os.path.join(TB, 'String.xlsx')); ws = wb['String']
print('String +', add_rows(ws, 'Key', [dict(Key=k, KO=v, EN='') for k, v in [
    ('tab_looks', '외형'), ('attend_toast', '출석 {0}일차 보상 {1}'), ('patrol_got', '순찰 보상 {0}'), ('patrol_x2', '2배'),
    ('chip_ability', '능력'), ('chip_all_parts', '모든 부위'), ('gear_up_title', '장비 강화'), ('lv_table', '레벨별 능력'),
    ('lv_now', '지금'), ('up_cost', '필요'),
    # 11-17 치트 (시험용)
    ('cheat', '치트 (시험용)'), ('cheat_btn', '치트'), ('cheat_grade', '등급'), ('cheat_part', '장비 부위'), ('cheat_random', '무작위'),
    ('cheat_shards', '터렛 조각'), ('cheat_all_turrets', '열린 터렛 모두'),
    ('cheat_note', '시험용입니다. 에디터 · 개발용 빌드에서만 보이고, 스토어 빌드는 Config Cheat_On 0 이면 안 보여요.'),
]]))
wb.save(os.path.join(TB, 'String.xlsx'))

wb = load_workbook(os.path.join(TB, 'Config.xlsx')); ws = wb[wb.sheetnames[0]]
heads = [c.value for c in ws[1]]
print('Config +', add_rows(ws, heads[0], [{heads[0]: k, heads[1]: v, **({heads[2]: d} if len(heads) > 2 else {})} for k, v, d in [
    ('Attend_Auto', 1, '4차 수정안: 1 = 로비에 오면 출석 보상을 자동으로 받고 토스트 · 0 = 예전처럼 창'),
    ('Patrol_Ad_Mul', 2, '4차 수정안: 순찰 보상 광고 보면 x배'),
    ('Cheat_On', 0, '11-17 치트 창: 1 = 스토어 빌드에서도 보임 (에디터 · 개발용 빌드는 늘 보임) — 스토어 빌드 전 0 확인'),
]]))
wb.save(os.path.join(TB, 'Config.xlsx'))
print('ok')
