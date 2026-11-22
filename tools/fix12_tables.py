# -*- coding: utf-8 -*-
"""11-22 「타워_전체_정리.html」(원본 = 타워 추가 컨셉.xlsx) 게임 반영 — 표 쪽
 ① 해금 다시: 쾅쾅 대포 · 집중 빔 · 소용돌이포 = 5 / 불바퀴 · 찌릿 코일 · 별빛 부메랑 = 10 / 서리 · 화염 · 톱날 = 15 / 유성 · 증폭탑 = 20
 ② 멀티샷 개편: "탄 +1, 발당 -15%" → "쏜 뒤 80%(관통포 50%) 피해로 한 발 더" (followup · followup_mul)
 ③ 기믹 개편: 끈끈이 드론 감속선 · 레이저 고에너지 펄스(둔화) · 벼락탑 번개 확장(다시 치기) · 집중 빔(처치까지 · 점점 세짐)
    · 불바퀴 신속 바퀴 · 찌릿 코일(기본 1번 튕김 · 전기 체인 70%) · 소용돌이 연장 장 50% · 벌집 밀집 군집(작은 벌)
 ④ 새 타워 6종: Turret 줄 · 카드(새 · 일반 6 · 돌파 2 · 연계 4 · 콤보 2) · 스킨 · 전용 회로 3 — 곧 열리는 터렛 시트는 비움
 ⑤ 문서 의견 칸(키 earthdef-towers-1120) 6개: 저항 0.5 · 멀티샷 70% · 돔 빙결 1회 · 대장 카피 10% / 0.3초 / 탄속 20 / 최대 0.15초
    · 일제 사격 → 치명 피해 · 폭발탄 5% · 관통포 기본 관통 3 · 관통포 별 회로 → 쿨다운 -0.5초
사용: PYTHONUTF8=1 python tools/fix12_tables.py   (다시 돌려도 같은 값 — 줄 Id 기준으로 덮어씀) → 표 변환은 Unity 배치
주아 글꼴에 '…' '·' '—' 없음 → 화면 글자(Name · Desc)에 안 씀"""
import os, sys
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from phase2_tables import TB
from fix9_tables import upsert
from openpyxl import load_workbook

CONFIG = [
    ('Resist_Mul', 0.5, '적 저항 속성 배율 (11-22 문서 의견 "적 저항 속성 시 0.5배", 예전 0.6)'),
    ('Multi_Penalty', 0.3, '탄 +1 카드의 발당 피해 감소 (11-22 의견 "멀티샷 패널티 70%" = 발당 70%, 예전 0.15) — 남은 카드: 없음(모두 한 발 더로 바뀜)'),
    ('Followup_Mul', 0.8, '11-22 "한 발 더" 카드 기본 피해 배율 (카드 Val2 followup_mul 이 있으면 그 값 — 관통포 0.5)'),
    ('Followup_Delay', 0.22, '11-22 한 발 더가 나가는 간격(초)'),
    ('Recast_Delay', 0.45, '11-22 벼락탑 번개 확장: 다시 치는 간격(초)'),
    ('Tesla_Bounce_Mul', 0.7, '11-22 찌릿 코일: 첫 타격 뒤 튕긴 번개 피해 배율 (엑셀 "2번째 타격 시 피해량 70%")'),
    ('Drone_Line_Base_Dmg', 1, '11-22 끈끈이 감속선 기본 피해 (1 = 예전 장판 피해 그대로 · 0 = 엑셀대로 피해 없음, 장판 공격 카드로만)'),
    ('Drone_Line_Slow', 0.35, '11-22 감속선에 닿으면 둔화 (엑셀 "지나가면 n초간 감속")'),
    ('Drone_Line_Slow_Time', 3, '11-22 감속선 둔화 시간(초)'),
    ('Drone_Fly_Time', 0.6, '11-22 드론이 선을 그으며 가로지르는 시간(초)'),
    ('Focus_Hold', 1, '11-22 집중 빔 개편: 1 = 적을 처치할 때까지 오래 쏨(최대 Focus_Hold_Max) · 0 = 예전 4초'),
    ('Focus_Hold_Max', 8, '11-22 집중 빔 최대 쏘는 시간(초, 쏘는 시간 카드 · 회로가 늘림)'),
    ('Focus_Base_Ramp', 0.15, '11-22 집중 빔: 한 적에 붙은 1초마다 피해 +15%'),
    ('Focus_Base_Ramp_Max', 1.5, '11-22 집중 빔 기본 증폭 최대 (+150%)'),
    ('Guardian_Cd_Lv_Scale', 1.6667, '11-22 의견 "쿨다운 0.3초 기준 최대 0.15초": 대장 카피만 터렛 레벨 쿨 단축 x 이 값 (Lv45 0.7 → 0.5)'),
    ('Dome_Freeze_Time', 2, '11-22 의견 "빙결 2초간 모두 이동 정지" (예전 3)'),
    ('Dome_Freeze_Boss', 1, '11-22 1 = 얼음 파동은 보스도 얼림 ("모두") · 0 = 예전(보스는 느려짐)'),
    ('Dome_Vuln', 30, '11-22 얼음 파동: 얼어 있는 동안 받는 피해 +%'),
    ('Dome_Skill_Uses', 1, '11-22 얼음 파동: 한 판에 쓸 수 있는 횟수 (의견 "1회성", 0 = 쿨타임마다)'),
    # 새 타워
    ('Boomerang_Apex_Time', 1.2, '11-22 별빛 부메랑 정점 회전 시간(초)'),
    ('Boomerang_Apex_Rate', 120, '11-22 정점 회전 틱 피해 %ATK (0.25초마다)'),
    ('Boomerang_Twin_Mul', 0.8, '11-22 쌍부메랑 보조 부메랑 피해 배율'),
    ('Frost_Slow', 0.3, '11-22 서리 눈꽃포 기본 둔화'),
    ('Freeze_Time', 1.2, '11-22 빙결(서리) 시간(초) — 보스는 안 얼음'),
    ('Frost_Zone_Rate', 50, '11-22 서리 지대 0.5초마다 %ATK'),
    ('Flame_Zone_Rate', 35, '11-22 용암 화염방사기 잔여 화상 지대 0.5초마다 %ATK'),
    ('Flame_Zone_Time', 3, '11-22 잔여 화상 지대 시간(초)'),
    ('Saw_Tick_Rate', 48, '11-22 자기장 톱날 원형 톱날 틱 피해 %ATK'),
    ('Saw_Armor_Pen', 30, '11-22 자기장 톱날 기본 장갑 무시 %'),
    ('Saw_Track_Speed', 2.2, '11-22 자석 추적 톱날 속도'),
    ('Meteor_Gap', 0.35, '11-22 유성 박격포 연발 간격(초)'),
    ('Prism_Lv_Mul', 0.4, '11-22 별빛 증폭탑 버프 = Rate x (1 + 터렛 레벨 피해 보너스 x 이 값) → Lv45 약 +60%'),
]

STRINGS = [
    ('tinfo_stat_prism', '아군 피해 +{0}% 버프, 쿨다운 {1}초, 화면 전체'),
    ('prism_buff', '별빛 증폭!'),
    ('dome_spent', '이번 판에 이미 썼어요'),
]

# ---------------------------------------------------------------- Turret
TURRET_EDIT = [
    dict(Id='guardian', Rate=10, Cooldown=0.3, Speed=20),
    dict(Id='rail', Pierce=3),
    dict(Id='cannon', Unlock=5), dict(Id='focus', Unlock=5, Info='한 적에게 빔을 붙여 처치할 때까지 지져요. 오래 붙을수록 점점 세지는 보스 사냥용이에요.'),
    dict(Id='vortex', Unlock=5), dict(Id='wheel', Unlock=10),
    dict(Id='tesla', Unlock=10, Hits=1, Desc='충전한 번개가 한 번 튕기며 폭발',
         Info='충전한 번개를 쏴요. 번개는 다른 적에게 한 번 튀고, 맞은 자리마다 폭발해요.'),
    dict(Id='drone', Desc='드론이 지나간 자리에 감속선', Info='드론이 적이 많은 곳을 가로지르며 감속선을 그어요. 선을 지나는 적은 피해를 받고 3초 동안 느려져요.'),
]
T_COLS = ['Id', 'Name', 'Elem', 'Art', 'Kind', 'Rate', 'Rate2', 'Tick', 'Cooldown', 'Range', 'Count', 'Hits', 'Duration', 'Radius', 'Pierce', 'Speed', 'Desc', 'Unlock', 'Knock', 'Info']
NEW_TURRETS = [
    ('boomerang', '별빛 부메랑', 'phys', 'boomerang', 'boomerang', 240, 0, 0, 3.6, 11, 1, 0, 0, 0.55, 99, 14, '꿰뚫고 되돌아오는 부메랑', 10, 0,
     '부메랑을 던져 지나는 길의 적을 모두 꿰뚫고, 타워로 되돌아오며 한 번 더 때려요.'),
    ('frost_cannon', '서리 눈꽃포', 'light', 'frost_cannon', 'frost', 260, 0, 0, 4.2, 11, 1, 0, 2.5, 1.35, 0, 12, '넓게 터지는 서리 포탄 + 감속', 15, 0,
     '넓게 터지는 서리 포탄을 쏴요. 맞은 적은 모두 피해를 받고 느려져요.'),
    ('flamethrower', '용암 화염방사기', 'fire', 'flamethrower', 'flame', 65, 0, 0.2, 5.2, 9.5, 1, 0, 1.6, 0.65, 99, 0, '일직선 화염 + 잔여 화상 지대', 15, 0,
     '적을 향해 일직선으로 불길을 뿜어 꿰뚫어요. 불길이 지나간 자리엔 화상 지대가 남아요.'),
    ('plasma_saw', '자기장 톱날', 'bolt', 'plasma_saw', 'saw', 180, 0, 0.25, 5.5, 10, 1, 0, 3, 0.95, 0, 12, '맞힌 자리에 도는 톱날을 놓음', 15, 0,
     '톱날을 쏴서 맞힌 뒤, 그 자리에 3초 동안 도는 자기장 톱날을 놓아요. 적 장갑을 30% 뚫어요.'),
    ('meteor_mortar', '유성 박격포', 'fire', 'meteor_mortar', 'meteor', 210, 0, 0, 5.8, 14, 3, 0, 0, 1.2, 0, 11, '유성 3발 연속 폭격', 20, 0,
     '적이 몰린 곳에 유성을 3번 연속으로 떨어뜨려 넓게 폭격해요.'),
    ('prism_beacon', '별빛 증폭탑', 'grav', 'prism_beacon', 'prism', 25, 0, 0, 9, 0, 1, 0, 5, 0, 0, 0, '아군 타워 전체 피해 증폭', 20, 0,
     '직접 공격하지 않아요. 9초마다 5초 동안 놓인 아군 타워(대장 카피 포함) 모두의 피해를 크게 올려요.'),
]

# ---------------------------------------------------------------- Card
C_COLS = ['Id', 'Turret', 'Kind', 'Line', 'Name', 'Desc', 'Max', 'Weight', 'Partner', 'Requires', 'Stat1', 'Val1', 'Stat2', 'Val2', 'Stat3', 'Val3']


def card(id, tur, kind, line, name, desc, mx, w, partner='', req='', *stats):
    d = dict(Id=id, Turret=tur, Kind=kind, Line=line, Name=name, Desc=desc, Max=mx, Weight=w, Partner=partner, Requires=req,
             Stat1=None, Val1=None, Stat2=None, Val2=None, Stat3=None, Val3=None)
    for i, (k, v) in enumerate(stats):
        d['Stat%d' % (i + 1)] = k; d['Val%d' % (i + 1)] = v
    return d


CARD_EDIT = [
    # 의견: 일제 사격 → 치명 피해 (최대 3회) · 폭발탄 5% 로 시작
    card('g_shot', 'guardian', 'normal', '', '치명 사격', '치명 피해 +30%', 3, 10, '', '', ('crit_dmg', 30)),
    card('g_bA', 'guardian', 'break', 'A', '폭발탄', '맞히면 5% 확률로 작은 폭발', 1, 10, '', '', ('explode', 0.05)),
    # 멀티샷 → 한 발 더
    card('r_shot', 'rail', 'normal', '', '포탄 일제 사격', '쏜 뒤 50% 피해 포탄 1발 더', 2, 10, '', '', ('followup', 1), ('followup_mul', 0.5)),
    card('m_shot', 'missile', 'normal', '', '일제 미사일', '작은 미사일 1발 더 (80% 피해)', 2, 10, '', '', ('followup', 1), ('followup_mul', 0.8)),
    card('b_shot', 'bolt', 'normal', '', '연속 낙뢰', '낙뢰 뒤 작은 낙뢰 1발 더 (80% 피해)', 2, 10, '', '', ('followup', 1), ('followup_mul', 0.8)),
    card('c_shot', 'cannon', 'normal', '', '다중 포격', '포격 뒤 포탄 1발 더 (80% 피해)', 2, 10, '', '', ('followup', 1), ('followup_mul', 0.8)),
    card('v_shot', 'vortex', 'normal', '', '추가 장', '소용돌이 1개 더 (80% 피해)', 1, 10, '', '', ('followup', 1), ('followup_mul', 0.8)),
    card('w_shot', 'wheel', 'normal', '', '쌍바퀴', '불바퀴 1개 더 (80% 피해)', 1, 10, '', '', ('followup', 1), ('followup_mul', 0.8)),
    card('h_count', 'hive', 'normal', '', '밀집 군집', '작은 벌 1마리 더 (80% 피해)', 1, 10, '', '', ('sub_bee', 1), ('sub_mul', 0.8)),
    card('h_count2', 'hive', 'normal', '', '밀집 군집+', '벌 +1', 1, 5, '', '', ('count_free', 1)),
    card('t_bounce', 'tesla', 'normal', '', '전기 체인', '튕기기 +1 (튕긴 번개는 70% 피해)', 2, 10, '', '', ('bounce', 1)),
    # 기믹 개편
    card('l_width', 'laser', 'normal', '', '고에너지 펄스', '레이저에 맞는 적 둔화 +20%', 2, 10, '', '', ('slow', 20)),
    card('d_dmg', 'drone', 'normal', '', '장판 증폭', '35% 확률로 감속선 1개 더', 2, 10, '', '', ('extra_line', 0.35)),
    card('d_line', 'drone', 'normal', '', '장판 공격', '감속선 피해 +25%', 2, 10, '', '', ('line_dmg', 25)),
    card('b_width', 'bolt', 'normal', '', '번개 확장', '30% 확률로 낙뢰를 한 번 더', 2, 10, '', '', ('recast_prob', 0.3)),
    card('v_dur', 'vortex', 'normal', '', '연장 장', '소용돌이 지속 시간 +50%', 2, 10, '', '', ('dur', 50)),
    card('w_fast', 'wheel', 'normal', '', '신속 바퀴', '불바퀴 이동 속도 +50%', 2, 10, '', '', ('speed', 50)),
]
CARD_DELETE = ['d_tick', 't_shot']   # 엑셀 "빠른 진동 제거" · "전기 체인+ 제거"

NEW_CARDS = [
    # ---- 별빛 부메랑
    card('new_boomerang', 'boomerang', 'new', '', '별빛 부메랑', '새 터렛을 빈 칸에 놓아요', 1, 12),
    card('bm_dmg', 'boomerang', 'normal', '', '별빛 날 연마', '부메랑 피해 +50%', 3, 10, '', '', ('dmg', 50)),
    card('bm_dmg2', 'boomerang', 'normal', '', '별빛 날 연마+', '부메랑 피해 +100%', 1, 5, '', '', ('dmg', 100)),
    card('bm_cd', 'boomerang', 'normal', '', '신속 회수', '공격 속도 +30%', 2, 10, '', '', ('cd', 30)),
    card('bm_recast', 'boomerang', 'normal', '', '되돌이 연사', '돌아오면 35% 확률로 바로 다시 던짐', 2, 8, '', '', ('instant_recast', 0.35)),
    card('bm_size', 'boomerang', 'normal', '', '거대 부메랑', '부메랑 크기 +30%', 2, 10, '', '', ('area', 30)),
    card('bm_speed', 'boomerang', 'normal', '', '가속 회전', '부메랑 이동 속도 +30%', 2, 10, '', '', ('speed', 30)),
    card('bm_bA', 'boomerang', 'break', 'A', '정점 회전', '되돌아갈 곳에서 1.2초 동안 돌며 여러 번 때림', 1, 10, '', '', ('hover_apex', 1)),
    card('bm_cA1', 'boomerang', 'chain', 'A', '회전 연장', '정점 회전 시간 +50%', 1, 10, '', 'bm_bA', ('apex_dur', 50)),
    card('bm_cA2', 'boomerang', 'chain', 'A', '회오리 날', '정점 회전 피해 +80%, 크기 +25%', 1, 10, '', 'bm_bA', ('apex_dmg', 80), ('area', 25)),
    card('bm_bB', 'boomerang', 'break', 'B', '쌍부메랑 교차', '좌우 곡선으로 부메랑 2개 (보조 80%)', 1, 10, '', '', ('twin_arc', 1)),
    card('bm_cB1', 'boomerang', 'chain', 'B', '별가루 궤적', '날아간 길에 별빛 파편이 터짐', 1, 10, '', 'bm_bB', ('trail_dmg', 80)),
    card('bm_cB2', 'boomerang', 'chain', 'B', '귀환 강타', '돌아올 때 피해 +100%', 1, 10, '', 'bm_bB', ('return_dmg', 100)),
    card('cb_bm_r', 'boomerang', 'combo', '', '관통 부메랑', '적 장갑 30% 무시, 피해 +25%', 1, 8, 'rail', '', ('armor_pen', 30), ('dmg', 25)),
    card('cb_bm_w', 'boomerang', 'combo', '', '불꽃 부메랑', '부메랑에 맞은 적 화상', 1, 8, 'wheel', '', ('burn', 1)),
    # ---- 서리 눈꽃포
    card('new_frost_cannon', 'frost_cannon', 'new', '', '서리 눈꽃포', '새 터렛을 빈 칸에 놓아요', 1, 12),
    card('fr_dmg', 'frost_cannon', 'normal', '', '서리 결정 강화', '서리 피해 +50%', 3, 10, '', '', ('dmg', 50)),
    card('fr_dmg2', 'frost_cannon', 'normal', '', '서리 결정 강화+', '서리 피해 +100%', 1, 5, '', '', ('dmg', 100)),
    card('fr_area', 'frost_cannon', 'normal', '', '눈보라 확산', '서리 폭발 범위 +30%', 2, 10, '', '', ('area', 30)),
    card('fr_slow', 'frost_cannon', 'normal', '', '혹한 냉기', '감속 +15%, 둔화 최대 +10%', 2, 10, '', '', ('slow', 15), ('slow_max', 10)),
    card('fr_cd', 'frost_cannon', 'normal', '', '급속 냉각', '공격 속도 +30%', 2, 10, '', '', ('cd', 30)),
    card('fr_follow', 'frost_cannon', 'normal', '', '이중 눈꽃 포격', '서리 포탄 1발 더 (80% 피해)', 2, 8, '', '', ('followup', 1), ('followup_mul', 0.8)),
    card('fr_bA', 'frost_cannon', 'break', 'A', '빙결 파쇄', '25% 확률로 빙결, 느린 적에게 피해 +60%', 1, 10, '', '', ('freeze', 0.25), ('dmg_vs_slow', 60)),
    card('fr_cA1', 'frost_cannon', 'chain', 'A', '얼음 파편', '터질 때 얼음 파편 4개가 튐', 1, 10, '', 'fr_bA', ('ice_shard', 4)),
    card('fr_cA2', 'frost_cannon', 'chain', 'A', '동상 취약', '서리에 맞은 적이 받는 피해 +20%', 1, 10, '', 'fr_bA', ('vuln', 20)),
    card('fr_bB', 'frost_cannon', 'break', 'B', '서리 지대', '떨어진 자리에 3초 서리 장판', 1, 10, '', '', ('frost_zone', 1)),
    card('fr_cB1', 'frost_cannon', 'chain', 'B', '영구 동토', '서리 지대 시간 +50%, 범위 +30%', 1, 10, '', 'fr_bB', ('zone_dur', 50), ('zone_area', 30)),
    card('fr_cB2', 'frost_cannon', 'chain', 'B', '서리 폭발', '서리 지대가 끝날 때 크게 터짐', 1, 10, '', 'fr_bB', ('zone_detonate', 1)),
    card('cb_fr_l', 'frost_cannon', 'combo', '', '빙결 공명', '느리거나 언 적에게 피해 +70%', 1, 8, 'laser', '', ('dmg_vs_slow', 70)),
    card('cb_fr_v', 'frost_cannon', 'combo', '', '얼음 소용돌이', '범위 +25%, 15% 확률로 빙결', 1, 8, 'vortex', '', ('area', 25), ('freeze', 0.15)),
    # ---- 용암 화염방사기
    card('new_flamethrower', 'flamethrower', 'new', '', '용암 화염방사기', '새 터렛을 빈 칸에 놓아요', 1, 12),
    card('fl_dmg', 'flamethrower', 'normal', '', '고열 화염', '화염 피해 +50%', 3, 10, '', '', ('dmg', 50)),
    card('fl_dmg2', 'flamethrower', 'normal', '', '고열 화염+', '화염 피해 +100%', 1, 5, '', '', ('dmg', 100)),
    card('fl_burn', 'flamethrower', 'normal', '', '잔불 점화', '화상 지대 피해 +50%', 2, 10, '', '', ('burn_dmg', 50)),
    card('fl_burndur', 'flamethrower', 'normal', '', '꺼지지 않는 불씨', '화상 지대 시간 +50%', 2, 10, '', '', ('burn_dur', 50)),
    card('fl_cd', 'flamethrower', 'normal', '', '연료 과급', '공격 속도 +30%', 2, 10, '', '', ('cd', 30)),
    card('fl_area', 'flamethrower', 'normal', '', '광폭 화염', '화염 굵기, 길이 +50%', 2, 10, '', '', ('area', 50)),
    card('fl_bA', 'flamethrower', 'break', 'A', '용암 웅덩이', '화상 지대가 용암으로 (피해 +60%, 둔화)', 1, 10, '', '', ('lava_pool', 1)),
    card('fl_cA1', 'flamethrower', 'chain', 'A', '열기 증폭', '화상 지대 위 적이 받는 피해 +25%', 1, 10, '', 'fl_bA', ('burn_vuln', 25)),
    card('fl_cA2', 'flamethrower', 'chain', 'A', '분화구 파열', '처치하면 35% 확률로 용암 폭발', 1, 10, '', 'fl_bA', ('kill_explode', 0.35)),
    card('fl_bB', 'flamethrower', 'break', 'B', '초고온 플라즈마', '장갑 50% 무시, 화염 피해 +50%', 1, 10, '', '', ('armor_pen', 50), ('dmg', 50)),
    card('fl_cB1', 'flamethrower', 'chain', 'B', '연속 분사', '뿜는 시간 +50%', 1, 10, '', 'fl_bB', ('dur', 50)),
    card('fl_cB2', 'flamethrower', 'chain', 'B', '열풍 밀치기', '화염에 맞은 적을 밀어냄', 1, 10, '', 'fl_bB', ('knock', 35)),
    card('cb_fl_fr', 'flamethrower', 'combo', '', '열기 융해', '느리거나 언 적에게 피해 +80%', 1, 8, 'frost_cannon', '', ('dmg_vs_slow', 80)),
    card('cb_fl_m', 'flamethrower', 'combo', '', '화염 폭풍', '화상 지대 +40%, 처치 때 25% 작은 미사일', 1, 8, 'missile', '', ('burn_dmg', 40), ('kill_mini', 0.25)),
    # ---- 자기장 톱날
    card('new_plasma_saw', 'plasma_saw', 'new', '', '자기장 톱날', '새 터렛을 빈 칸에 놓아요', 1, 12),
    card('sw_dmg', 'plasma_saw', 'normal', '', '자기장 증폭', '자기장 피해 +50%', 3, 10, '', '', ('dmg', 50)),
    card('sw_dmg2', 'plasma_saw', 'normal', '', '자기장 증폭+', '자기장 피해 +100%', 1, 5, '', '', ('dmg', 100)),
    card('sw_dur', 'plasma_saw', 'normal', '', '고속 베어링', '톱날 유지 시간 +40%', 2, 10, '', '', ('dur', 40)),
    card('sw_area', 'plasma_saw', 'normal', '', '대형 톱날', '톱날 범위 +35%', 2, 10, '', '', ('area', 35)),
    card('sw_tick', 'plasma_saw', 'normal', '', '과회전 날', '톱날 틱 피해 +40%', 2, 10, '', '', ('tick_dmg', 40)),
    card('sw_cd', 'plasma_saw', 'normal', '', '자기장 가속', '공격 속도 +30%', 2, 10, '', '', ('cd', 30)),
    card('sw_bA', 'plasma_saw', 'break', 'A', '자석 추적 톱날', '톱날이 엘리트, 보스를 천천히 쫓아감', 1, 10, '', '', ('saw_track', 1)),
    card('sw_cA1', 'plasma_saw', 'chain', 'A', '장갑 분쇄', '장갑 무시 +40%, 맞은 적 취약 +15%', 1, 10, '', 'sw_bA', ('armor_pen', 40), ('vuln', 15)),
    card('sw_cA2', 'plasma_saw', 'chain', 'A', '가속 회전', '톱날 틱 간격 -30%', 1, 10, '', 'sw_bA', ('tick', 30)),
    card('sw_bB', 'plasma_saw', 'break', 'B', '자기장 분열', '톱날이 끝날 때 폭발 + 작은 톱날 2개', 1, 10, '', '', ('saw_split', 1)),
    card('sw_cB1', 'plasma_saw', 'chain', 'B', '전자기 감전', '12% 확률로 마비', 1, 10, '', 'sw_bB', ('paralyze', 0.12)),
    card('sw_cB2', 'plasma_saw', 'chain', 'B', '이중 톱날', '톱날 1개 더 (75% 피해)', 1, 10, '', 'sw_bB', ('followup', 1), ('followup_mul', 0.75)),
    card('cb_sw_t', 'plasma_saw', 'combo', '', '고압 전류 날', '톱날 틱마다 15% 확률로 연쇄 번개', 1, 8, 'tesla', '', ('hit_chain', 0.15)),
    card('cb_sw_d', 'plasma_saw', 'combo', '', '중력 고정 날', '느린 적에게 톱날 피해 +60%', 1, 8, 'drone', '', ('dmg_vs_slow', 60)),
    # ---- 유성 박격포
    card('new_meteor_mortar', 'meteor_mortar', 'new', '', '유성 박격포', '새 터렛을 빈 칸에 놓아요', 1, 12),
    card('mt_dmg', 'meteor_mortar', 'normal', '', '운석 질량 강화', '유성 피해 +50%', 3, 10, '', '', ('dmg', 50)),
    card('mt_dmg2', 'meteor_mortar', 'normal', '', '운석 질량 강화+', '유성 피해 +100%', 1, 5, '', '', ('dmg', 100)),
    card('mt_crit', 'meteor_mortar', 'normal', '', '정밀 낙하', '치명 확률 +20%', 2, 10, '', '', ('crit', 20)),
    card('mt_cd', 'meteor_mortar', 'normal', '', '신속 장전', '공격 속도 +20%', 2, 10, '', '', ('cd', 20)),
    card('mt_speed', 'meteor_mortar', 'normal', '', '유성 가속 낙하', '떨어지는 속도 +40%', 2, 10, '', '', ('speed', 40)),
    card('mt_para', 'meteor_mortar', 'normal', '', '충격파 마비', '터질 때 20% 확률로 마비', 2, 8, '', '', ('paralyze', 0.2)),
    card('mt_bA', 'meteor_mortar', 'break', 'A', '거대 혜성', '3번째 유성이 큰 혜성 (범위 +60%, 피해 2배)', 1, 10, '', '', ('giant_meteor', 1)),
    card('mt_cA1', 'meteor_mortar', 'chain', 'A', '운석 충돌구', '혜성 자리에 3초 불 분화구', 1, 10, '', 'mt_bA', ('crater_burn', 1)),
    card('mt_cA2', 'meteor_mortar', 'chain', 'A', '치명 파열', '치명 피해 +80%', 1, 10, '', 'mt_bA', ('crit_dmg', 80)),
    card('mt_bB', 'meteor_mortar', 'break', 'B', '유성우 포화', '유성 +2발 (80% 피해)', 1, 10, '', '', ('meteor_extra', 2)),
    card('mt_cB1', 'meteor_mortar', 'chain', 'B', '파편 확산', '떨어질 때마다 작은 운석 3개', 1, 10, '', 'mt_bB', ('shrapnel', 3)),
    card('mt_cB2', 'meteor_mortar', 'chain', 'B', '충격 여진', '폭발 범위 +25%, 맞은 적 취약 +15%', 1, 10, '', 'mt_bB', ('area', 25), ('vuln', 15)),
    card('cb_mt_v', 'meteor_mortar', 'combo', '', '블랙홀 유성', '유성 폭발 범위 +35%, 피해 +35%', 1, 8, 'vortex', '', ('area', 35), ('dmg', 35)),
    card('cb_mt_b', 'meteor_mortar', 'combo', '', '벼락 운석', '떨어질 때 35% 확률로 작은 낙뢰', 1, 8, 'bolt', '', ('hit_bolt', 0.35)),
    # ---- 별빛 증폭탑
    card('new_prism_beacon', 'prism_beacon', 'new', '', '별빛 증폭탑', '새 터렛을 빈 칸에 놓아요', 1, 12),
    card('pr_dmg', 'prism_beacon', 'normal', '', '별빛 공명', '아군 타워 전체 피해 +10%', 3, 10, '', '', ('buff_dmg', 10)),
    card('pr_dmg2', 'prism_beacon', 'normal', '', '별빛 공명+', '아군 타워 전체 피해 +20%', 1, 5, '', '', ('buff_dmg', 20)),
    card('pr_crit', 'prism_beacon', 'normal', '', '정밀 조율', '아군 타워 치명 확률 +15%', 2, 10, '', '', ('buff_crit', 15)),
    card('pr_critdmg', 'prism_beacon', 'normal', '', '극대화 증폭', '아군 타워 치명 피해 +50%', 2, 10, '', '', ('buff_crit_dmg', 50)),
    card('pr_dur', 'prism_beacon', 'normal', '', '파동 유지', '버프 시간 +30%', 2, 10, '', '', ('dur', 30)),
    card('pr_cd', 'prism_beacon', 'normal', '', '고속 충전', '증폭탑 쿨다운 단축 (공격 속도 +25%)', 2, 10, '', '', ('cd', 25)),
    card('pr_bA', 'prism_beacon', 'break', 'A', '약점 파동', '버프 때 적 모두 받는 피해 +20%', 1, 10, '', '', ('aura_vuln', 20)),
    card('pr_cA1', 'prism_beacon', 'chain', 'A', '약점 심화', '약점 표식 +15%, 표식 적 둔화 15%', 1, 10, '', 'pr_bA', ('aura_vuln', 15), ('aura_slow', 15)),
    card('pr_cA2', 'prism_beacon', 'chain', 'A', '표식 파열', '표식된 적을 잡으면 별빛 폭발', 1, 10, '', 'pr_bA', ('mark_pop', 1)),
    card('pr_bB', 'prism_beacon', 'break', 'B', '초신성 과부하', '버프 중 아군 공격 속도 +25%', 1, 10, '', '', ('buff_cd', 25)),
    card('pr_cB1', 'prism_beacon', 'chain', 'B', '상시 가동', '버프 시간 +40%, 공격 속도 +15%', 1, 10, '', 'pr_bB', ('dur', 40), ('cd', 15)),
    card('pr_cB2', 'prism_beacon', 'chain', 'B', '대장 집중 증폭', '대장 카피 버프 효과 +40%', 1, 10, '', 'pr_bB', ('guardian_bonus', 40)),
    card('cb_pr_f', 'prism_beacon', 'combo', '', '빛의 프리즘', '버프 중 보스, 엘리트 피해 +30%', 1, 8, 'focus', '', ('buff_boss', 30)),
    card('cb_pr_h', 'prism_beacon', 'combo', '', '군집 지휘', '버프 때 아군 남은 쿨다운 15% 줄임', 1, 8, 'hive', '', ('buff_cdr_burst', 15)),
]

# ---------------------------------------------------------------- Skin
S_COLS = ['Id', 'Turret', 'Name', 'Hue', 'Sat', 'Ult', 'Ult_Name', 'Ult_Desc', 'Ult_Charge', 'Ult_Rate', 'Dmg', 'Price_Dia']
NEW_SKINS = [
    ('sk_boomerang', 'boomerang', '은하수 부메랑', 210, 1.2, 'boomerang', '쌍성 대부메랑', '큰 부메랑 2개가 X자로 화면을 휩쓸어요', 5, 420, 10, 300),
    ('sk_frost_cannon', 'frost_cannon', '빙하 눈꽃포', 40, 1.2, 'frost', '절대영도 폭격', '가운데에 큰 눈꽃탄을 떨어뜨려 넓게 얼려요', 4, 520, 10, 300),
    ('sk_flamethrower', 'flamethrower', '태양 용암포', 40, 1.3, 'flame', '마그마 대분출', '세 갈래 불길이 휩쓸고 용암 지대를 남겨요', 4, 90, 10, 300),
    ('sk_plasma_saw', 'plasma_saw', '오버클럭 톱날', 150, 1.3, 'saw', '기가 톱날 폭풍', '아주 큰 톱날이 적을 끌어당기며 갈아요', 4, 95, 10, 300),
    ('sk_meteor_mortar', 'meteor_mortar', '혜성 박격포', 200, 1.2, 'meteor', '유성우 대폭격', '적이 몰린 곳에 큰 유성 6발이 떨어져요', 4, 340, 10, 300),
    ('sk_prism_beacon', 'prism_beacon', '초신성 증폭탑', 300, 1.2, 'prism', '은하 과부하', '4초 동안 아군 공격 속도 +40%, 치명 +30%', 3, 40, 10, 300),
]

# ---------------------------------------------------------------- Chip
H_COLS = ['Id', 'Name', 'Desc', 'Scope', 'Turret', 'Stat', 'Max', 'Parts', 'Weight']
NEW_CHIPS = []
for tid, short, full, sp in [
    ('boomerang', '부메랑', '별빛 부메랑', ('area', 50, 'weapon,shield', '크기 +{0}%')),
    ('frost_cannon', '눈꽃포', '서리 눈꽃포', ('area', 40, 'shield,suit', '폭발 범위 +{0}%')),
    ('flamethrower', '화염방사기', '용암 화염방사기', ('burn_dur', 50, 'weapon,core', '화상 지대 시간 +{0}%')),
    ('plasma_saw', '톱날', '자기장 톱날', ('dur', 50, 'helmet,core', '톱날 유지 시간 +{0}%')),
    ('meteor_mortar', '유성', '유성 박격포', ('area', 40, 'shield,suit', '폭발 범위 +{0}%')),
    ('prism_beacon', '증폭탑', '별빛 증폭탑', ('dur', 50, 'core,helmet', '버프 시간 +{0}%')),
]:
    if tid == 'prism_beacon':
        NEW_CHIPS.append(dict(Id='tc_%s_dmg' % tid, Name='증폭탑 효율', Desc='증폭탑 버프 +{0}%', Scope='turret', Turret=tid, Stat='buff_dmg', Max=21, Parts='all', Weight=4))
    else:
        NEW_CHIPS.append(dict(Id='tc_%s_dmg' % tid, Name=short + ' 피해', Desc=full + ' 피해 +{0}%', Scope='turret', Turret=tid, Stat='dmg', Max=42, Parts='all', Weight=4))
    NEW_CHIPS.append(dict(Id='tc_%s_star' % tid, Name=short + ' 별', Desc=full + '을(를) 놓으면 별 +{0}', Scope='turret', Turret=tid, Stat='start_stars', Max=2, Parts='suit,boots', Weight=3))
    NEW_CHIPS.append(dict(Id='tc_%s_sp' % tid, Name=short + ' 특수', Desc=full + ' ' + sp[3], Scope='turret', Turret=tid, Stat=sp[0], Max=sp[1], Parts=sp[2], Weight=3))
# 의견 "관통포 별 삭제 - 공속 최대 0.5초 감소": 같은 Id 를 그대로 바꿈 (가진 회로가 사라지지 않게)
CHIP_EDIT = [dict(Id='tc_rail_star', Name='관통포 공속', Desc='관통포 쿨다운 -{0}초', Stat='cd_sec', Max=0.5, Parts='suit,boots')]


def rows_of(cols, tup): return [dict(zip(cols, t)) for t in tup]


def delete_rows(ws, key_col, ids):
    head = [c.value for c in ws[1]]; ki = head.index(key_col) + 1
    n = 0
    for r in range(ws.max_row, 1, -1):
        if ws.cell(r, ki).value in ids: ws.delete_rows(r); n += 1
    return n


if __name__ == '__main__':
    p = os.path.join(TB, 'Config.xlsx'); wb = load_workbook(p); ws = wb.active
    print('Config', upsert(ws, 'Key', [dict(Key=k, Value=v, Note=n) for k, v, n in CONFIG])); wb.save(p)
    p = os.path.join(TB, 'String.xlsx'); wb = load_workbook(p); ws = wb['String']
    print('String', upsert(ws, 'Key', [dict(Key=k, KO=v) for k, v in STRINGS])); wb.save(p)

    p = os.path.join(TB, 'Turret.xlsx'); wb = load_workbook(p)
    ws = wb['Turret']
    print('Turret edit', upsert(ws, 'Id', TURRET_EDIT))
    print('Turret new', upsert(ws, 'Id', rows_of(T_COLS, NEW_TURRETS)))
    ws = wb['Turret_Soon']
    print('Turret_Soon 지움', delete_rows(ws, 'Id', [t[0] for t in NEW_TURRETS]))
    wb.save(p)

    p = os.path.join(TB, 'Card.xlsx'); wb = load_workbook(p); ws = wb['Card']
    print('Card 지움', delete_rows(ws, 'Id', CARD_DELETE))
    print('Card edit', upsert(ws, 'Id', CARD_EDIT))
    print('Card new', upsert(ws, 'Id', NEW_CARDS))
    wb.save(p)

    p = os.path.join(TB, 'Skin.xlsx'); wb = load_workbook(p); ws = wb['Skin']
    print('Skin', upsert(ws, 'Id', rows_of(S_COLS, NEW_SKINS))); wb.save(p)

    p = os.path.join(TB, 'Chip.xlsx'); wb = load_workbook(p); ws = wb['Chip']
    print('Chip edit', upsert(ws, 'Id', CHIP_EDIT))
    print('Chip new', upsert(ws, 'Id', NEW_CHIPS)); wb.save(p)
    print('ok')
