# -*- coding: utf-8 -*-
"""카피바라 지구를 지켜라! 기획 표 처음 만들기 (Earth_Defence/Table/*.xlsx).
게임은 Unity 의 [지구 방위대 → 표 변환](재생할 때 자동)이 csv 로 바꿔 읽는다.

※ 표를 손으로 고친 뒤 다시 돌리면 고친 값이 사라짐 — 처음 한 번(또는 원할 때 --force)만.
사용: PYTHONUTF8=1 python tools/make_tables.py [--force]

값 출처: 「Galaxy Defense 벤치마킹 게임 기획서.pdf」 4장 터렛 수치(피해 %ATK · 쿨다운 · 사거리)를 처음 값으로.
이름은 원작 이름을 쓰지 않고 새로 지음(제작_계획 ⑦). 밸런스는 맨 마지막.
"""
import os, sys
from openpyxl import Workbook
from openpyxl.styles import Font, PatternFill, Alignment

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), '..'))
OUT = os.path.join(ROOT, 'Earth_Defence', 'Table')
HEAD = PatternFill('solid', fgColor='F1E8D8')


def book(path, sheets):
    if os.path.exists(path) and '--force' not in sys.argv:
        print('있음 (건너뜀):', os.path.basename(path)); return
    wb = Workbook(); wb.remove(wb.active)
    for name, (cols, rows, note) in sheets.items():
        ws = wb.create_sheet(name)
        ws.append(cols)
        for c in ws[1]:
            c.font = Font(bold=True); c.fill = HEAD; c.alignment = Alignment(horizontal='center')
        for r in rows: ws.append(list(r))
        for i, col in enumerate(cols):
            ws.column_dimensions[ws.cell(1, i + 1).column_letter].width = max(10, min(44, len(str(col)) * 1.6 + 4))
        if note:
            ws.cell(1, len(cols) + 2, note).font = Font(italic=True, color='7A6A5C')
    wb.save(path); print('→', os.path.relpath(path, ROOT))


os.makedirs(OUT, exist_ok=True)

# ------------------------------------------------------------------ Config
CONFIG = [
    # 한 판
    ('Run_Time', 240, '한 판 길이(초) — 11-15 결정 "약 4분"'),
    ('Mid_Boss_Time', 120, '중간 보스(엘리트) 나오는 시각(초)'),
    ('Boss_Time', 210, '마지막 보스 나오는 시각(초). 보스를 잡으면 승리'),
    ('Wave_Len', 30, '웨이브 하나 길이(초) — 웨이브마다 몰려오기 1번'),
    ('Level_Max', 20, '판 안 레벨 상한 (카드 약 20장)'),
    ('Exp_Base', 8, 'Lv1 → 2 필요 경험치'), ('Exp_Growth', 1.17, '레벨마다 필요 경험치 배율'),
    # 카드
    ('Card_Choices', 3, '레벨업 카드 장수'),
    ('Card_Upgrade_Guarantee', 0, '1 = 강화 카드 1장 이상 보장 (11-15 결정: 원작대로 0)'),
    ('Refresh_Ad', 1, '판마다 광고 새로고침 횟수'), ('Refresh_Free', 0, '판마다 무료 새로고침 횟수 (원작대로 0)'),
    ('Stars_Per_Tier', 4, '별 몇 개면 돌파 카드가 열리나'), ('Tier_Max', 3, '터렛 단계 최대'),
    ('Turret_Slots', 4, '가디언 말고 터렛 칸 수'),
    ('Break_Weight_Mul', 4, '별이 다 차면 돌파 카드 가중치 배율'),
    ('Boss_Extra_Card', 1, '엘리트·보스를 잡으면 카드 1장 더 (1 = 켬)'),
    ('Multi_Penalty', 0.15, '탄 +1 카드의 발당 피해 감소'),
    # 전장 (월드 단위 = 화면 1080 기준 100px)
    ('Field_W', 10.8, '전장 폭'), ('Ground_Y', -7.52, '땅 높이'), ('Shield_Y', -4.6, '적이 멈추는 방어막 높이'),
    ('Spawn_Y', 10.6, '적이 나오는 높이'), ('Enemy_Hit_Interval', 1.0, '방어막에 붙은 적이 때리는 간격(초)'),
    ('Crash_Mul', 3, '운석이 방어막에 부딪치면 공격력 x'),
    ('Max_Alive', 90, '화면에 동시에 있는 적 최대'),
    ('Spawn_Rate_Start', 1.2, '처음 초당 적 수'), ('Spawn_Rate_End', 3.2, '마지막 초당 적 수'),
    ('Spawn_Rate_Per_Stage', 0.03, '스테이지마다 늘어나는 초당 적 수'),
    ('Swarm_Size', 8, '웨이브마다 한꺼번에 몰려오는 수'),
    # 적 세기
    ('Mon_HP_Base', 55, '스테이지 1 적 체력 (Hp_Mul 1 기준)'), ('Mon_HP_Growth', 1.16, '스테이지마다 체력 배율'),
    ('Mon_Atk_Base', 12, '스테이지 1 적 공격력'), ('Mon_Atk_Growth', 1.09, '스테이지마다 공격력 배율'),
    ('Elite_HP_Mul', 30, '엘리트 체력 = 일반 x'), ('Elite_Atk_Mul', 3, '엘리트 공격력 x'),
    ('Boss_HP_Mul', 140, '보스 체력 = 일반 x'), ('Boss_Atk_Mul', 6, '보스 공격력 x'),
    ('Boss_Summon_Gap', 4, '보스가 부하를 부르는 간격(초)'),
    # 피해 공식 (기획서 6-3)
    ('Range_Scale', 1.3, '터렛 사거리 배율 (표 Range x 이 값 = 월드 칸)'),
    ('Atk_Base', 100, '기본 공격력'), ('Crit_Rate', 0.05, '기본 치명 확률'), ('Crit_Dmg', 1.25, '기본 치명 배율'),
    ('Weak_Mul', 1.5, '약점 속성 배율'), ('Resist_Mul', 0.6, '저항 속성 배율'),
    ('Pos_Near', 1.1, '기지 근처 적 피해 배율'), ('Pos_Far', 0.9, '먼 적 피해 배율'),
    ('Pos_Near_Y', -1.6, '이 높이보다 아래 = 기지 근처'), ('Pos_Far_Y', 5.0, '이 높이보다 위 = 먼 적'),
    # 가디언
    ('Guardian_Move', 14, '끌 때 좌우 이동 속도'), ('Guardian_X_Max', 1.5, '좌우로 움직일 수 있는 범위(±) — 옆 터렛과 안 겹치게'),
    ('Charge_Need', 120, '충전 포인트 (적중 1번 = 1)'), ('Charge_Time', 5, '충전 상태 시간(초)'),
    ('Charge_Mul', 5, '충전탄 피해 배율'), ('Charge_Interval', 0.1, '충전탄 간격(초)'),
    # 상태이상
    ('Slow_Max', 0.5, '둔화 최대'), ('Para_Time', 1.0, '마비 시간(초)'), ('Burn_Time', 3, '화상 시간(초)'),
    ('Burn_Rate', 40, '화상 초당 피해 %ATK'), ('Vuln_Time', 4, '취약 시간(초)'),
    ('Explode_Rate', 60, '"폭발" 카드 폭발 피해 %ATK'), ('Explode_Radius', 0.9, '"폭발" 반경'),
    ('Mini_Bolt_Rate', 120, '작은 낙뢰 피해 %ATK'), ('Mini_Laser_Rate', 80, '작은 레이저 피해 %ATK'),
    ('Chain_Rate', 50, '연쇄 번개 피해 %ATK'), ('Chain_Range', 2.4, '연쇄 번개가 튀는 거리'),
    # 기지 (요새)
    ('Base_HP', 3000, 'Lv1 기지 체력'), ('Base_HP_Per_Lv', 120, '레벨마다 체력'), ('Base_Atk_Per_Lv', 4, '레벨마다 공격력'),
    ('Base_Cost_Base', 120, 'Lv1 → 2 골드'), ('Base_Cost_Growth', 1.11, '레벨마다 비용 배율'), ('Base_Max_Lv', 300, '기지 최대 레벨'),
    # 보상
    ('Win_Gold_Base', 300, '승리 골드 (스테이지 1)'), ('Win_Gold_Per_Stage', 60, '스테이지마다 더하는 골드'),
    ('Kill_Gold', 1, '적 1마리 골드 (스테이지 배율 곱함)'), ('Lose_Mul', 0.35, '지면 보상 x (버틴 시간 비율도 곱함)'),
    ('Win_Shards', 12, '승리 터렛 조각 (무작위 터렛에 나눠 줌)'), ('Win_Shards_Per_Stage', 1, '스테이지마다 더하는 조각'),
    ('First_Clear_Mul', 2, '처음 깬 스테이지 보상 배율'),
    ('Patrol_Gold_Hour', 400, '순찰 시간당 골드 (최고 스테이지 1)'), ('Patrol_Gold_Per_Stage', 80, '최고 스테이지마다 더하는 시간당 골드'),
    ('Patrol_Shards_Hour', 4, '순찰 시간당 조각'), ('Patrol_Max_Hours', 12, '순찰 최대 시간'), ('Patrol_Min_Sec', 60, '이보다 짧으면 받기 버튼 꺼짐'),
    ('Start_Gold', 0, '처음 골드'),
    ('Run_HP_Ramp', 1.5, '한 판 안에서 적 체력이 끝 무렵 몇 배 더 (0 = 그대로, 1.5 = 끝에 2.5배)'),
    ('Charge_Per_Hit', 1, '가디언 적중 1번당 충전 포인트'),
]

# ------------------------------------------------------------------ 터렛 (기획서 4-1 기본 수치)
TURRET_COLS = ['Id', 'Name', 'Elem', 'Art', 'Kind', 'Rate', 'Rate2', 'Tick', 'Cooldown', 'Range', 'Count', 'Hits', 'Duration', 'Radius', 'Pierce', 'Speed', 'Desc']
TURRETS = [
    ('guardian', '대장 카피', 'phys', 'guardian', 'gun', 13.25, 0, 0, 0.15, 15, 1, 0, 0, 0, 0, 22, '쌍권총 연사 · 충전 상태'),
    ('rail', '관통포', 'phys', 'rail', 'rail', 260, 0, 0, 4, 12, 1, 0, 0, 0, 2, 26, '가장 가까운 적에게 꿰뚫는 포탄'),
    ('laser', '레이저 포', 'light', 'laser', 'laser', 28.8, 0, 0.2, 6, 12, 1, 0, 2.0, 0.32, 99, 0, '일직선으로 꿰뚫는 레이저'),
    ('drone', '끈끈이 드론', 'grav', 'drone', 'field', 18.55, 0, 0.5, 10, 0, 1, 0, 10, 0.9, 0, 0, '가로 띠 장판 · 틱마다 둔화'),
    ('hive', '벌집', 'grav', 'hive', 'hive', 200, 0, 0, 12, 10, 3, 7, 0, 0, 0, 7, '꼬마 벌 로봇이 쫓아가 쏨'),
    ('missile', '유도 미사일', 'fire', 'missile', 'missile', 193.5, 154.8, 0, 8, 13, 1, 0, 0, 1.0, 0, 9, '날아가 폭발'),
    ('bolt', '벼락탑', 'bolt', 'bolt', 'bolt', 139.7, 0, 0.4, 9, 30, 1, 0, 2.0, 0.35, 0, 0, '세로줄 낙뢰'),
]

# ------------------------------------------------------------------ 카드
# Kind: new(새 터렛) · normal · break(돌파, 라인 입구) · chain(돌파 뒤) · combo(두 터렛을 가져야)
# Stat 키 (터렛 하나에 더해짐): dmg(%) · shots(+N, 발당 감소) · shots_free · pierce · cd(공격 속도 %) · dur(%) · area(%) · range(%)
#   count(벌·장판·미사일 수) · count_free · hits(벌 공격 횟수) · crit(%p) · slow(%p) · slow_max(%p) · tick(틱 속도 %)
#   explode(확률) · explode_dmg(%) · explode_area(%) · split(개수) · split_dmg(%) · burn(1=켬) · burn_amp(%) · fire_zone
#   paralyze(확률) · para_time(%) · vuln(%) · chain(개수) · chain_dmg(%) · beam_split · sweep · dmg_by_slow(%) · field_explode · pull
#   pulse · pulse_area · pulse_end · echo · echo_hits · charge · charge_speed(%) · charge_shots(%) · dmg_vs_para(%)
#   hit_laser(확률) · kill_bolt(확률) · kill_bee(확률) · kill_explode(확률) · fire_add(%) · hit_chain(확률)
CARD_COLS = ['Id', 'Turret', 'Kind', 'Line', 'Name', 'Desc', 'Max', 'Weight', 'Partner', 'Requires', 'Stat1', 'Val1', 'Stat2', 'Val2', 'Stat3', 'Val3']
C = []


def card(id, tur, kind, name, desc, s=(), mx=1, w=10, line='', partner='', req=''):
    s = list(s) + [('', '')] * (3 - len(s))
    desc = desc.replace(' · ', ', ')   # 주아 글꼴에 · 가 없음
    C.append([id, tur, kind, line, name, desc, mx, w, partner, req] + [x for p in s for x in p])


for tid, name, *_ in TURRETS[1:]:
    card('new_' + tid, tid, 'new', name, '새 터렛을 빈 칸에 놓아요', w=12)

# 대장 카피
card('g_dmg', 'guardian', 'normal', '탄환 강화', '총알 피해 +50%', [('dmg', 50)], 3)
card('g_shot', 'guardian', 'normal', '일제 사격', '탄 +1 · 발당 피해 -15%', [('shots', 1)], 2)
card('g_shot2', 'guardian', 'normal', '일제 사격+', '탄 +1 (피해 그대로)', [('shots_free', 1)], 1, 5)
card('g_rate', 'guardian', 'normal', '속사', '연사 속도 +20%', [('cd', 20)], 3)
card('g_pierce', 'guardian', 'normal', '관통탄', '총알이 적 1마리를 더 꿰뚫음', [('pierce', 1)], 3)
card('g_crit', 'guardian', 'normal', '정밀 조준', '치명 확률 +10%', [('crit', 10)], 2)
card('g_bA', 'guardian', 'break', '폭발탄', '맞히면 10% 확률로 작은 폭발', [('explode', 0.10)], line='A')
card('g_cA1', 'guardian', 'chain', '폭발 확장', '폭발 범위 +50%', [('explode_area', 50)], line='A', req='g_bA')
card('g_cA2', 'guardian', 'chain', '폭발 강화', '폭발 피해 +60% · 확률 +10%', [('explode_dmg', 60), ('explode', 0.10)], line='A', req='g_bA')
card('g_bB', 'guardian', 'break', '충전 사격', '맞힐 때마다 충전 → 가득 차면 5초 동안 충전탄', [('charge', 1)], line='B')
card('g_cB1', 'guardian', 'chain', '빠른 충전', '충전 속도 +100%', [('charge_speed', 100)], line='B', req='g_bB')
card('g_cB2', 'guardian', 'chain', '지속 충전', '충전 중 발사 수 2배', [('charge_shots', 100)], line='B', req='g_bB')
# 관통포
card('r_dmg', 'rail', 'normal', '탄약 강화', '포탄 피해 +50%', [('dmg', 50)], 3)
card('r_dmg2', 'rail', 'normal', '탄약 강화+', '포탄 피해 +100%', [('dmg', 100)], 1, 5)
card('r_shot', 'rail', 'normal', '포탄 일제 사격', '포탄 +1 · 발당 피해 -15%', [('shots', 1)], 2)
card('r_up', 'rail', 'normal', '관통포 개량', '피해 +30% · 관통 +1', [('dmg', 30), ('pierce', 1)], 2)
card('r_pierce', 'rail', 'normal', '파워 관통', '관통 +3', [('pierce', 3)], 1)
card('r_crit', 'rail', 'normal', '치명 포탄', '피해 +40% · 치명 확률 +30%', [('dmg', 40), ('crit', 30)], 1)
card('r_cd', 'rail', 'normal', '빠른 장전', '공격 속도 +25%', [('cd', 25)], 2)
card('r_bA', 'rail', 'break', '폭발 포탄', '포탄이 맞힐 때마다 폭발', [('explode', 1.0)], line='A')
card('r_cA1', 'rail', 'chain', '충격 확산', '폭발 범위 +60%', [('explode_area', 60)], line='A', req='r_bA')
card('r_cA2', 'rail', 'chain', '충격 강화', '폭발 피해 +100%', [('explode_dmg', 100)], line='A', req='r_bA')
card('r_bB', 'rail', 'break', '분열탄', '처음 맞히면 작은 포탄 2개로 갈라짐', [('split', 2)], line='B')
card('r_cB1', 'rail', 'chain', '분열 강화', '작은 포탄 +2', [('split', 2)], line='B', req='r_bB')
card('r_cB2', 'rail', 'chain', '분열 숙련', '작은 포탄 피해 +100%', [('split_dmg', 100)], line='B', req='r_bB')
# 레이저 포
card('l_dmg', 'laser', 'normal', '레이저 증폭', '레이저 피해 +50%', [('dmg', 50)], 3)
card('l_dur', 'laser', 'normal', '안정 에너지', '쏘는 시간 +40%', [('dur', 40)], 2)
card('l_multi', 'laser', 'normal', '멀티 스트라이크', '쏘는 시간 +100% · 공격 속도 +20%', [('dur', 100), ('cd', 20)], 1, 6)
card('l_width', 'laser', 'normal', '고에너지 펄스', '레이저 굵기 +100%', [('area', 100)], 1)
card('l_cd', 'laser', 'normal', '빠른 냉각', '공격 속도 +25%', [('cd', 25)], 2)
card('l_bA', 'laser', 'break', '갈래 레이저', '레이저가 두 갈래로 갈라져 나가요', [('beam_split', 2)], line='A')
card('l_cA1', 'laser', 'chain', '둔화 레이저', '맞은 적 둔화 +30%', [('slow', 30)], line='A', req='l_bA')
card('l_cA2', 'laser', 'chain', '갈래 추가', '갈래 레이저 +2', [('beam_split', 2)], line='A', req='l_bA')
card('l_bB', 'laser', 'break', '휘두르기 레이저', '쏘는 동안 좌우로 휘둘러요', [('sweep', 1)], line='B')
card('l_cB1', 'laser', 'chain', '약점 노출', '맞은 적이 받는 피해 +20%', [('vuln', 20)], line='B', req='l_bB')
card('l_cB2', 'laser', 'chain', '휘두르기 강화', '피해 +60% · 쏘는 시간 +30%', [('dmg', 60), ('dur', 30)], line='B', req='l_bB')
# 끈끈이 드론
card('d_dmg', 'drone', 'normal', '장판 증폭', '장판 피해 +50%', [('dmg', 50)], 2)
card('d_area', 'drone', 'normal', '장판 확장', '장판 두께 +30%', [('area', 30)], 2)
card('d_dur', 'drone', 'normal', '장판 지속', '장판 시간 +30%', [('dur', 30)], 2)
card('d_tick', 'drone', 'normal', '빠른 진동', '틱 간격 -30%', [('tick', 30)], 1)
card('d_slow', 'drone', 'normal', '정지장', '둔화 최대 +40%', [('slow_max', 40)], 1)
card('d_cd', 'drone', 'normal', '빠른 재배치', '공격 속도 +25%', [('cd', 25)], 2)
card('d_bA', 'drone', 'break', '장판 제압', '느린 적일수록 피해 최대 +200%', [('dmg_by_slow', 200)], line='A')
card('d_cA1', 'drone', 'chain', '마지막 폭발', '장판이 사라질 때 폭발', [('field_explode', 1)], line='A', req='d_bA')
card('d_cA2', 'drone', 'chain', '수렴 장판', '장판이 적을 가운데로 끌어당김', [('pull', 1)], line='A', req='d_bA')
card('d_bB', 'drone', 'break', '두 번째 장판', '장판을 2개 깔아요', [('count_free', 1)], line='B')
card('d_cB1', 'drone', 'chain', '넓은 판', '장판 두께 +50%', [('area', 50)], line='B', req='d_bB')
card('d_cB2', 'drone', 'chain', '진동 장판', '틱마다 5% 확률로 마비', [('paralyze', 0.05)], line='B', req='d_bB')
# 벌집
card('h_dmg', 'hive', 'normal', '군집 공습', '벌 피해 +50%', [('dmg', 50)], 3)
card('h_hits', 'hive', 'normal', '속사 포화', '벌 공격 횟수 +2', [('hits', 2)], 3)
card('h_count', 'hive', 'normal', '밀집 군집', '벌 +2 · 피해 -15%', [('count', 2)], 2)
card('h_count2', 'hive', 'normal', '밀집 군집+', '벌 +2 (피해 그대로)', [('count_free', 2)], 1, 5)
card('h_cd', 'hive', 'normal', '빠른 부화', '공격 속도 +25%', [('cd', 25)], 2)
card('h_bA', 'hive', 'break', '접촉 펄스', '벌이 처음 맞히면 둥근 펄스파', [('pulse', 1)], line='A')
card('h_cA1', 'hive', 'chain', '증폭 펄스', '펄스 범위 +30% · 둔화', [('pulse_area', 30), ('slow', 20)], line='A', req='h_bA')
card('h_cA2', 'hive', 'chain', '최후의 공명', '벌이 사라질 때도 펄스파', [('pulse_end', 1)], line='A', req='h_bA')
card('h_bB', 'hive', 'break', '메아리 벌', '적을 잡으면 메아리 벌이 나와요', [('echo', 1)], line='B')
card('h_cB1', 'hive', 'chain', '메아리 강화', '메아리 벌 공격 +3', [('echo_hits', 3)], line='B', req='h_bB')
card('h_cB2', 'hive', 'chain', '꿀벌 진화', '벌 피해 +80%', [('dmg', 80)], line='B', req='h_bB')
# 유도 미사일
card('m_dmg', 'missile', 'normal', '미사일 부스트', '미사일 피해 +50%', [('dmg', 50)], 3)
card('m_shot', 'missile', 'normal', '일제 미사일', '미사일 +1 · 발당 피해 -15%', [('shots', 1)], 2)
card('m_area', 'missile', 'normal', '폭발 확산', '폭발 범위 +30%', [('area', 30)], 2)
card('m_cd', 'missile', 'normal', '빠른 발사', '공격 속도 +25%', [('cd', 25)], 2)
card('m_impact', 'missile', 'normal', '강력 충격', '미사일 피해 +100%', [('dmg', 100)], 1, 5)
card('m_bA', 'missile', 'break', '소형 미사일', '맞힌 뒤 작은 미사일 3발', [('split', 3)], line='A')
card('m_cA1', 'missile', 'chain', '미사일 증폭', '작은 미사일 +3', [('split', 3)], line='A', req='m_bA')
card('m_cA2', 'missile', 'chain', '열 탄두', '작은 미사일 피해 +100%', [('split_dmg', 100)], line='A', req='m_bA')
card('m_bB', 'missile', 'break', '점화', '맞은 적에 화상', [('burn', 1)], line='B')
card('m_cB1', 'missile', 'chain', '점화 증폭', '화상 피해 +100%', [('burn_amp', 100)], line='B', req='m_bB')
card('m_cB2', 'missile', 'chain', '발화 지대', '떨어진 자리에 5초 불바다', [('fire_zone', 1)], line='B', req='m_bB')
# 벼락탑
card('b_dmg', 'bolt', 'normal', '번개 부스트', '낙뢰 피해 +50%', [('dmg', 50)], 3)
card('b_shot', 'bolt', 'normal', '연속 낙뢰', '낙뢰 +1 · 발당 피해 -15%', [('shots', 1)], 2)
card('b_width', 'bolt', 'normal', '번개 확장', '낙뢰 굵기 +100%', [('area', 100)], 1)
card('b_cd', 'bolt', 'normal', '신속 낙뢰', '공격 속도 +20% · 피해 +30%', [('cd', 20), ('dmg', 30)], 2)
card('b_dur', 'bolt', 'normal', '긴 낙뢰', '치는 시간 +50%', [('dur', 50)], 2)
card('b_bA', 'bolt', 'break', '번개 마비', '틱마다 30% 확률로 마비', [('paralyze', 0.30)], line='A')
card('b_cA1', 'bolt', 'chain', '마비 연장', '마비 시간 +100%', [('para_time', 100)], line='A', req='b_bA')
card('b_cA2', 'bolt', 'chain', '마비 충격', '마비된 적에게 피해 +100%', [('dmg_vs_para', 100)], line='A', req='b_bA')
card('b_bB', 'bolt', 'break', '연쇄 번개', '맞은 적에서 번개가 3번 튀어요', [('chain', 3)], line='B')
card('b_cB1', 'bolt', 'chain', '연쇄 확장', '튀는 번개 +4', [('chain', 4)], line='B', req='b_bB')
card('b_cB2', 'bolt', 'chain', '연쇄 강화', '튀는 번개 피해 +100%', [('chain_dmg', 100)], line='B', req='b_bB')
# 콤보 15 (두 터렛이 모두 있을 때만)
card('cb_g_m', 'guardian', 'combo', '불꽃탄', '총알에 불 피해 +30%', [('fire_add', 30)], partner='missile', w=8)
card('cb_g_b', 'guardian', 'combo', '전기탄', '처치하면 50% 확률로 작은 낙뢰', [('kill_bolt', 0.5)], partner='bolt', w=8)
card('cb_g_l', 'guardian', 'combo', '레이저 사격', '맞히면 10% 확률로 작은 레이저', [('hit_laser', 0.10)], partner='laser', w=8)
card('cb_g_d', 'guardian', 'combo', '끈끈탄', '맞은 적 둔화 +10%', [('slow', 10)], partner='drone', w=8)
card('cb_g_h', 'guardian', 'combo', '꿀벌 탄환', '처치하면 20% 확률로 벌 1마리', [('kill_bee', 0.2)], partner='hive', w=8)
card('cb_r_g', 'rail', 'combo', '약점 표식', '포탄에 맞은 적이 받는 피해 +15%', [('vuln', 15)], partner='guardian', w=8)
card('cb_r_b', 'rail', 'combo', '전류 관통', '처치하면 50% 확률로 작은 낙뢰', [('kill_bolt', 0.5)], partner='bolt', w=8)
card('cb_r_l', 'rail', 'combo', '빛 쪼개기', '맞히면 20% 확률로 작은 레이저', [('hit_laser', 0.2)], partner='laser', w=8)
card('cb_l_b', 'laser', 'combo', '에너지 마비', '틱마다 5% 확률로 마비', [('paralyze', 0.05)], partner='bolt', w=8)
card('cb_l_d', 'laser', 'combo', '끈끈 레이저', '맞은 적 둔화 +20%', [('slow', 20)], partner='drone', w=8)
card('cb_d_b', 'drone', 'combo', '방해 번개', '장판 안에서 죽으면 30% 확률로 작은 낙뢰', [('kill_bolt', 0.3)], partner='bolt', w=8)
card('cb_d_m', 'drone', 'combo', '불타는 장판', '장판에 맞은 적 화상', [('burn', 1)], partner='missile', w=8)
card('cb_h_b', 'hive', 'combo', '전기 벌', '벌이 맞히면 50% 확률로 번개가 튐', [('hit_chain', 0.5)], partner='bolt', w=8)
card('cb_h_g', 'hive', 'combo', '탄환 벌', '벌이 한 번에 2발씩', [('shots_free', 1)], partner='guardian', w=8)
card('cb_m_b', 'missile', 'combo', '폭뢰', '처치하면 30% 확률로 폭발', [('kill_explode', 0.3)], partner='bolt', w=8)

# ------------------------------------------------------------------ 적 (11-15 운석 7종 · 벌레 · 젤리 · 함선)
ENEMY_COLS = ['Id', 'Name', 'Rank', 'Family', 'Art', 'Move', 'Speed', 'Hp_Mul', 'Atk_Mul', 'Exp', 'Size', 'Pivot_Y', 'Weak', 'Resist', 'Armor', 'Crash', 'Split', 'Split_N', 'Summon']
ENEMIES = [
    ('pebble', '돌멩이 운석', 'normal', 'meteor', 'pebble', 'fall', 1.7, 0.45, 0.5, 1, 0.6, 0.32, 'phys', '', 0, 1, '', 0, ''),
    ('fire', '불꼬리 운석', 'normal', 'meteor', 'fire', 'fall', 1.15, 1.0, 1.0, 2, 0.85, 0.32, 'light', 'fire', 0, 1, '', 0, ''),
    ('ice', '얼음 혜성', 'normal', 'meteor', 'ice', 'fall', 0.8, 1.5, 1.0, 3, 0.85, 0.32, 'fire', '', 0, 1, '', 0, ''),
    ('iron', '쇠 운석', 'normal', 'meteor', 'iron', 'fall', 0.85, 1.6, 1.2, 3, 0.85, 0.32, 'bolt', 'phys', 0.3, 1, '', 0, ''),
    ('crack', '쪼개지는 운석', 'normal', 'meteor', 'crack', 'fall', 0.9, 1.3, 1.0, 3, 0.9, 0.32, 'phys', '', 0, 1, 'pebble', 3, ''),
    ('moth', '외계 나방', 'normal', 'bug', 'moth', 'zigzag', 1.0, 0.9, 1.0, 2, 0.9, 0.5, 'fire', '', 0, 0, '', 0, ''),
    ('beetle', '딱정벌레', 'normal', 'bug', 'beetle', 'walk', 0.55, 2.2, 1.2, 3, 0.85, 0.5, 'bolt', '', 0.4, 0, '', 0, ''),
    ('spider', '외계 거미', 'normal', 'bug', 'spider', 'fast', 1.5, 0.8, 0.9, 2, 0.8, 0.5, 'fire', '', 0, 0, '', 0, ''),
    ('jelly', '젤리 바이러스', 'normal', 'jelly', 'jelly', 'walk', 0.7, 1.6, 1.0, 3, 0.85, 0.5, 'light', 'grav', 0, 0, '', 0, ''),
    ('split', '분열 젤리', 'normal', 'jelly', 'split', 'walk', 0.75, 1.2, 1.0, 3, 0.85, 0.5, 'light', '', 0, 0, 'jelly_mini', 2, ''),
    ('jelly_mini', '꼬마 젤리', 'normal', 'jelly', 'split', 'fast', 1.3, 0.35, 0.5, 1, 0.45, 0.5, 'light', '', 0, 0, '', 0, ''),
    ('ufo', '비행접시', 'normal', 'ship', 'ufo', 'hover', 0.8, 1.3, 1.0, 3, 0.9, 0.5, 'bolt', '', 0, 0, '', 0, ''),
    ('sdrone', '정찰 드론', 'normal', 'ship', 'sdrone', 'zigzag', 1.3, 0.7, 0.8, 2, 0.75, 0.5, 'light', '', 0, 0, '', 0, ''),
    # 엘리트 4 (중간 보스)
    ('big', '큰 소행성', 'elite', 'meteor', 'big', 'fall', 0.45, 1.0, 1.0, 20, 1.35, 0.5, 'phys', '', 0.2, 1, 'pebble', 5, ''),
    ('moth_king', '왕나방', 'elite', 'bug', 'moth_king', 'zigzag', 0.55, 0.8, 1.0, 20, 1.6, 0.5, 'fire', '', 0, 0, '', 0, 'moth'),
    ('beetle_steel', '강철 딱정벌레', 'elite', 'bug', 'beetle_steel', 'walk', 0.35, 1.2, 1.0, 20, 1.5, 0.5, 'bolt', 'phys', 0.5, 0, '', 0, ''),
    ('mother', '모선', 'elite', 'ship', 'mother', 'hover', 0.4, 1.0, 1.0, 20, 1.7, 0.5, 'bolt', '', 0, 0, '', 0, 'sdrone'),
    # 보스 2
    ('planet', '떠돌이 행성', 'boss', 'meteor', 'planet', 'boss', 0.28, 1.0, 1.0, 40, 1.15, 0.5, 'bolt', 'phys', 0.2, 0, '', 0, 'pebble'),
    ('queen', '젤리 여왕', 'boss', 'jelly', 'queen', 'boss', 0.3, 1.0, 1.0, 40, 1.9, 0.5, 'fire', 'light', 0.1, 0, '', 0, 'jelly_mini'),
]

# ------------------------------------------------------------------ 스테이지 30
STAGE_COLS = ['Stage', 'Name', 'Enemies', 'Swarm', 'Elite', 'Boss', 'Hp_Mul']
POOL = [  # 스테이지가 오르며 섞이는 적 (테마형 — 기획서 11-1)
    ['pebble', 'fire'], ['pebble', 'fire', 'moth'], ['fire', 'moth', 'spider'], ['pebble', 'fire', 'ice', 'moth'],
    ['fire', 'moth', 'jelly'], ['pebble', 'ice', 'spider', 'jelly'], ['fire', 'iron', 'moth', 'beetle'], ['crack', 'moth', 'spider', 'ufo'],
    ['fire', 'ice', 'jelly', 'split'], ['iron', 'crack', 'beetle', 'sdrone'],
]
ELITES = ['big', 'moth_king', 'beetle_steel', 'mother']
SWARMS = ['pebble', 'moth', 'spider', 'sdrone', 'jelly_mini']
NAMES = ['운석 소나기', '나방 떼', '거미 언덕', '얼음 꼬리', '젤리 습격', '차가운 밤', '단단한 껍질', '하늘의 접시', '끈적한 비', '쇠 비']
stages = []
for i in range(30):
    pool = POOL[i % 10] if i < 10 else sorted(set(POOL[i % 10] + POOL[(i + 3) % 10]))
    stages.append((i + 1, f'{NAMES[i % 10]}' + (f' {i // 10 + 1}' if i >= 10 else ''), ','.join(pool), SWARMS[i % len(SWARMS)],
                   ELITES[i % 4], 'planet' if i % 2 == 0 else 'queen', 1))

# ------------------------------------------------------------------ 터렛 레벨 (기획서 4-1: Lv45 · 레벨당 피해 +8% · 쿨 Lv8·16·28·36 총 -30%)
LV_COLS = ['Lv', 'Dmg_Bonus', 'Cd_Mul', 'Shards', 'Gold']
lvs = []
for lv in range(1, 46):
    steps = sum(1 for x in (8, 16, 28, 36) if lv >= x)
    lvs.append((lv, round(0.08 * (lv - 1), 2), round(1 - 0.075 * steps, 3), 0 if lv == 45 else int(5 + 3 * (lv - 1) + 0.12 * (lv - 1) ** 2),
                0 if lv == 45 else int(100 * 1.13 ** (lv - 1))))

# ------------------------------------------------------------------ 글자 (String)
STRINGS = [
    ('title', '카피바라 지구를 지켜라!', ''), ('start', '전투 시작', ''), ('stage_n', '스테이지 {0}', ''),
    ('lv', 'Lv {0}', ''), ('level_up', '레벨 업!', ''), ('pick_card', '카드 1장을 골라 주세요', ''), ('my_turrets', '내 터렛', ''),
    ('empty', '빈 칸', ''), ('refresh', '새로고침', ''), ('refresh_ad', '광고 보고 새로고침, 남은 횟수 {0}/{1}', ''),
    ('refresh_draft', '[초안] 광고 없이 바로', ''),
    ('kind_new', '새 터렛', ''), ('kind_normal', '강화', ''), ('kind_break', '돌파', ''), ('kind_chain', '연계', ''), ('kind_combo', '콤보', ''),
    ('boss_warn', '보스 등장!', ''), ('elite_warn', '중간 보스!', ''), ('boss_card', '보스를 잡았어요! 카드 1장 더', ''),
    ('win', '지구를 지켰어요!', ''), ('lose', '기지가 무너졌어요', ''), ('to_lobby', '로비로', ''), ('retry', '다시 하기', ''),
    ('next', '다음 스테이지', ''), ('reward', '보상', ''), ('kills', '처치 {0}', ''), ('time_left', '{0}', ''),
    ('pause', '일시정지', ''), ('resume', '계속하기', ''), ('give_up', '포기하기', ''), ('cards_title', '고른 카드', ''),
    ('turret', '터렛', ''), ('base', '기지', ''), ('base_hp', '체력 {0}', ''), ('base_atk', '공격력 {0}', ''),
    ('upgrade', '강화', ''), ('max', '최대', ''), ('shards', '조각 {0}/{1}', ''),
    ('patrol', '순찰 보상', ''), ('patrol_desc', '{0} 동안 순찰 (최대 {1}시간)', ''), ('claim', '받기', ''),
    ('best', '최고 기록: 스테이지 {0}', ''), ('first_clear', '처음 클리어 보상 x{0}', ''), ('locked', '잠김', ''),
    ('weak', '약점 {0}', ''),
]

book(os.path.join(OUT, 'Config.xlsx'), {'Config': (['Key', 'Value', 'Note'], CONFIG, '')})
book(os.path.join(OUT, 'Turret.xlsx'), {
    'Turret': (TURRET_COLS, TURRETS, 'Rate = 피해 %ATK (기획서 4-1) · Name 을 바꾸면 게임 이름도 바뀜'),
    'Turret_Level': (LV_COLS, lvs, '레벨당 피해 +8% · 쿨 Lv8·16·28·36 에서 -7.5%씩 · Shards/Gold = 다음 레벨 비용'),
})
book(os.path.join(OUT, 'Card.xlsx'), {'Card': (CARD_COLS, C, 'Stat 키는 tools/make_tables.py 맨 위 설명 · Max = 최대 몇 번 · Weight = 뽑힐 가중치')})
book(os.path.join(OUT, 'Enemy.xlsx'), {'Enemy': (ENEMY_COLS, ENEMIES, 'Hp_Mul · Atk_Mul 은 스테이지 기본값에 곱함 · Weak 약점 x1.5 · Resist x0.6 · Armor 받는 피해 감소')})
book(os.path.join(OUT, 'Stage.xlsx'), {'Stage': (STAGE_COLS, stages, 'Enemies = 섞여 나오는 적 · Swarm = 웨이브마다 몰려오는 적 · Elite = 2분 중간 보스 · Boss = 3분 30초 마지막 보스')})
book(os.path.join(OUT, 'String.xlsx'), {'String': (['Key', 'KO', 'EN'], STRINGS, 'EN 은 맨 마지막에 채움 (11-15 결정)')})
print('카드', len(C), '장 · 적', len(ENEMIES), '종 · 스테이지', len(stages))
