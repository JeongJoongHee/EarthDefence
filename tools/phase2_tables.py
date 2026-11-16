# -*- coding: utf-8 -*-
"""2차 표 더하기 (2026-11-16) — 기존 xlsx 는 손으로 고친 값을 지키려고 **없는 줄 · 없는 칸만 더함** (여러 번 돌려도 같음).

더하는 것
  Config  : 스태미나 · 엘리트 난이도 · 장비/칩 · 떨어지는 보상 · 튜토리얼 · 출석
  Turret  : 2차 터렛 5종(쾅쾅 대포 · 집중 빔 · 소용돌이포 · 불바퀴 · 찌릿 코일) + Unlock(몇 스테이지를 깨면 열림) · Knock(밀어내기) 칸
  Card    : 2차 터렛 카드 + 새 콤보
  Stage   : 31 ~ 100
  String  : 2차 화면 글자
새 표
  Gear.xlsx    : Gear_Part(부위 6) · Gear_Grade(등급 7)
  Chip.xlsx    : Chip(범용 · 터렛 전용)
  Mission.xlsx : Mission(일일 미션) · Attend(출석 7일)

사용: PYTHONUTF8=1 python tools/phase2_tables.py
값은 처음 값 — 밸런스는 맨 마지막 (11-15 결정).
"""
import os
from openpyxl import Workbook, load_workbook
from openpyxl.styles import Font, PatternFill, Alignment

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), '..'))
TB = os.path.join(ROOT, 'Earth_Defence', 'Table')
HEAD = PatternFill('solid', fgColor='F1E8D8')
NEW = PatternFill('solid', fgColor='E4F2DA')     # 2차에 더한 줄 (연한 초록)


def head_style(ws):
    for c in ws[1]:
        if c.value is not None:
            c.font = Font(bold=True); c.fill = HEAD; c.alignment = Alignment(horizontal='center')


def cols(ws):
    return [c.value for c in ws[1]]


def ensure_col(ws, name, default=None):
    """칸이 없으면 마지막 이름 있는 칸 뒤에 만듦 (메모 칸이 있으면 그 앞에 끼움)"""
    cs = cols(ws)
    if name in cs:
        return cs.index(name) + 1
    last = max(i for i, v in enumerate(cs) if v is not None and not str(v).startswith(('Rate =', 'Stat 키', 'Enemies =', 'EN 은', 'Hp_Mul', '레벨당')))
    pos = last + 2
    ws.insert_cols(pos)
    ws.cell(1, pos, name)
    if default is not None:
        for r in range(2, ws.max_row + 1):
            if ws.cell(r, 1).value not in (None, ''):
                ws.cell(r, pos, default)
    head_style(ws)
    return pos


def add_rows(ws, key_col, rows, by_name=None):
    """rows = dict 목록 (칸 이름 → 값). key_col 값이 이미 있으면 건너뜀. 돌려줌: 더한 수"""
    cs = cols(ws)
    ki = cs.index(key_col)
    have = {str(ws.cell(r, ki + 1).value) for r in range(2, ws.max_row + 1)}
    n = 0
    # 맨 끝 빈 줄 다음
    r = ws.max_row + 1
    while r > 2 and ws.cell(r - 1, 1).value in (None, ''):
        r -= 1
    for row in rows:
        if str(row[key_col]) in have:
            continue
        for k, v in row.items():
            if k in cs:
                c = ws.cell(r, cs.index(k) + 1, v)
                c.fill = NEW
        have.add(str(row[key_col])); r += 1; n += 1
    return n


def new_book(path, sheets):
    """새 표: 없을 때만 만듦 (있으면 줄만 더함)"""
    if os.path.exists(path):
        wb = load_workbook(path)
    else:
        wb = Workbook(); wb.remove(wb.active)
    for name, (columns, key, rows, note) in sheets.items():
        if name not in wb.sheetnames:
            ws = wb.create_sheet(name); ws.append(columns)
            if note:
                ws.cell(1, len(columns) + 2, note).font = Font(italic=True, color='7A6A5C')
            head_style(ws)
            for i, col in enumerate(columns):
                ws.column_dimensions[ws.cell(1, i + 1).column_letter].width = max(10, min(40, len(str(col)) * 1.6 + 6))
        ws = wb[name]
        print(f'  {os.path.basename(path)}:{name} +{add_rows(ws, key, [dict(zip(columns, r)) for r in rows])}')
    wb.save(path)


# ================================================================== Config
CONFIG = [
    # 스태미나 (기획서 2장 · 16장 — 숫자는 영상·문서에 없어 처음 값)
    ('Stamina_On', 1, '1 = 전투마다 스태미나를 씀 (0 = 끔)'),
    ('Stamina_Max', 60, '(11-16 저녁부터 안 씀 — Stamina_Base · Per_Stage · Cap)'),
    ('Stamina_Base', 30, '스태미나 최대 시작 값 (11-16 저녁 사용자 "시작 30")'),
    ('Stamina_Per_Stage', 1, '보통 스테이지를 처음 깰 때마다 최대 +'), ('Stamina_Cap', 80, '스태미나 최대의 끝 (30 + 50스테이지)'),
    ('Stamina_Cost', 5, '보통 난이도 한 판'), ('Stamina_Cost_Elite', 8, '엘리트 난이도 한 판'),
    ('Stamina_Regen_Sec', 360, '스태미나 1 차는 시간(초) — 6분'),
    ('Stamina_Ad', 20, '[초안] 광고 보고 받는 스태미나'), ('Stamina_Ad_Per_Day', 3, '하루 광고 스태미나 횟수'),
    # 엘리트 난이도 (기획서 11-1: 적이 더 많고 강하고 보상이 더 좋음)
    ('Elite_Mode_HP', 2.6, '엘리트 난이도 적 체력 x'), ('Elite_Mode_Atk', 1.6, '엘리트 난이도 적 공격력 x'),
    ('Elite_Mode_Spawn', 1.25, '엘리트 난이도 나오는 적 수 x'), ('Elite_Mode_Reward', 2, '엘리트 난이도 골드 · 조각 x'),
    ('Elite_Mode_Grade', 1, '엘리트 난이도 떨어지는 장비 · 칩 등급 +'),
    # 떨어지는 장비 · 칩
    ('Drop_Win_N', 2, '이기면 장비 · 칩 몇 개'), ('Drop_Elite_N', 1, '엘리트 난이도면 더'), ('Drop_First_N', 1, '처음 깨면 더'),
    ('Drop_Gear_Chance', 0.4, '떨어지는 것이 장비일 확률 (나머지 = 칩)'),
    ('Drop_Grade_Step', 12, '스테이지 몇 개마다 기본 등급 +1'), ('Drop_Grade_Up', 0.2, '한 등급 위로 나올 확률'),
    ('Drop_Grade_Down', 0.25, '한 등급 아래로 나올 확률'),
    ('Inv_Max', 200, '장비 · 칩 가방 최대 (넘으면 가장 낮은 등급을 부품으로)'),
    # 장비
    ('Gear_Up_Gold', 150, '장비 Lv1 → 2 골드 (등급 배율 곱함)'), ('Gear_Up_Gold_Growth', 1.1, '장비 레벨마다 골드 배율'),
    ('Gear_Up_Parts', 1, '장비 강화 부품 (레벨 5마다 +1)'), ('Gear_Scrap_Parts', 2, '장비 분해 부품 = 이 값 x 등급 + 레벨/2'),
    ('Merge_N', 3, '같은 것 몇 개를 합쳐 한 등급 위로 (장비 = 같은 부위 · 같은 등급, 칩 = 같은 칩 · 같은 등급)'),
    ('Chip_Slot_1', 0, '칩 칸 1 열림 (최고 스테이지)'), ('Chip_Slot_2', 0, '칩 칸 2'), ('Chip_Slot_3', 3, '칩 칸 3'),
    ('Chip_Slot_4', 8, '칩 칸 4'), ('Chip_Slot_5', 15, '칩 칸 5'), ('Chip_Slot_6', 25, '칩 칸 6'),
    ('Chip_Scrap_Parts', 1, '칩 분해 부품 = 이 값 x 등급'),
    ('Crit_Hp_Cap', 4, '"치명 때 최대 체력 % 추가" 상한 = 원래 피해 x'),
    ('Extra_Shot_Max', 0.5, '"추가 발사 확률" 상한'),
    # 넉백 · 2차 터렛
    ('Knock_Base', 0.6, '밀어내기 1 = 이만큼 위로 (월드 칸)'),
    ('Mine_Rate', 120, '부유 기뢰 폭발 %ATK'), ('Mine_Life', 8, '부유 기뢰 남는 시간(초)'),
    ('Scatter_Rate', 60, '산탄 1발 %ATK'),
    ('Focus_Ramp_Sec', 3, '집중 빔: 이 시간(초) 붙어 있으면 누적 최대'),
    ('Refract_Rate', 0.5, '굴절 빔 피해 = 빔 피해 x'),
    ('Blackhole_Time', 5, '블랙홀 시간(초)'), ('Vortex_Burst_Rate', 300, '중력 폭발 %ATK'),
    ('Wheel_Trail_Rate', 25, '불바퀴 자국 틱 %ATK'), ('Mini_Wheel_Gap', 3, '미니 불바퀴 나오는 간격(초)'),
    ('Trap_Rate', 40, '전기 덫 틱 %ATK'), ('Trap_Time', 2, '전기 덫 시간(초)'),
    # 튜토리얼 · 출석
    ('Tutorial', 1, '1 = 처음 한 번씩 도움말 (0 = 끔)'),
    # 소리 (11-16 저녁)
    ('Bgm_Volume', 0.7, '배경음 기본 음량 (설정 배경음 % 를 곱함)'), ('Sfx_Volume', 0.75, '효과음 기본 음량 (설정 효과음 % 를 곱함)'),
    ('Attend_Days', 7, '출석 며칠 묶음 (다 받으면 1일부터 다시)'),
]

# ================================================================== 2차 터렛 5종 (기획서 4-1 기본 수치 · 4-4 · 4-5 · 4-8 · 4-10 · 4-12)
# Kind: cannon 포탄 + 폭발 + 밀어냄 · focus 한 적에 붙는 빔 · vortex 느리게 나는 소용돌이(끌어당김) · wheel 벽에 튕기는 불바퀴 · tesla 충전 → 튀는 번개 폭발
# 터렛 탭에서 그림을 누르면 나오는 설명 (11-16 저녁 사용자 "어떠한 방식으로 공격하는지 정보 팝업")
TURRET_INFO = dict(
    guardian='레이저 소총으로 가장 가까운 적을 빠르게 연사해요. 화면을 끌면 좌우로 움직이고, 충전 사격을 배우면 충전탄을 쏟아내요.',
    rail='가장 가까운 적에게 꿰뚫는 포탄을 쏴요. 일렬로 선 적을 한꺼번에 뚫고, 돌파하면 폭발하거나 작은 포탄으로 갈라져요.',
    laser='일직선 레이저를 2초 동안 쏴서 선 위의 모든 적을 지져요. 돌파하면 갈래 레이저나 휘두르기 레이저가 돼요.',
    drone='적이 가장 많이 모인 높이에 화면 폭만 한 가로 띠 장판을 깔아요. 장판 안의 적은 틱마다 피해를 받고 점점 느려져요.',
    hive='꼬마 벌 로봇 3마리를 내보내요. 벌은 적을 쫓아가 7번씩 쏘아요.',
    missile='적을 쫓아가는 미사일을 쏴요. 맞으면 둥글게 폭발하고, 돌파하면 작은 미사일이나 화상이 더해져요.',
    bolt='적이 있는 세로줄에 낙뢰를 떨어뜨려 그 줄의 모든 적을 맞혀요. 사거리가 가장 길어요.',
    cannon='사거리가 짧은 큰 포탄을 쏴요. 맞으면 크게 폭발하고 둘레 적을 위로 밀어내요 (보스는 안 밀려요).',
    focus='한 적에게 빔을 붙여 4초 동안 지져요. 오래 붙을수록 강해지는 보스 사냥용이에요.',
    vortex='천천히 나는 소용돌이를 쏴서 둘레 적을 가운데로 끌어모으고 피해를 줘요. 폭발하는 터렛과 잘 맞아요.',
    wheel='벽과 방어막에 튕기며 굴러다니는 불바퀴를 굴려요. 적을 75번 맞히면 사라져요.',
    tesla='0.6초 충전한 번개를 쏴요. 번개는 적 사이를 튀며 맞은 자리마다 폭발해요.',
)

TURRET_NEW = [
    dict(Id='cannon', Name='쾅쾅 대포', Elem='phys', Art='cannon', Kind='cannon', Rate=421.1, Rate2=294.8, Tick=0, Cooldown=7, Range=9,
         Count=1, Hits=0, Duration=0, Radius=1.0, Pierce=0, Speed=13, Desc='포탄이 터지며 적을 밀어냄', Unlock=5, Knock=1.0),
    dict(Id='focus', Name='집중 빔', Elem='light', Art='focus', Kind='focus', Rate=145, Rate2=0, Tick=0.2, Cooldown=10, Range=14,
         Count=1, Hits=0, Duration=4, Radius=0.22, Pierce=0, Speed=0, Desc='한 적에 붙어 오래 쏘는 빔 (보스용)', Unlock=10, Knock=0),
    dict(Id='vortex', Name='소용돌이포', Elem='grav', Art='vortex', Kind='vortex', Rate=112.5, Rate2=0, Tick=0.2, Cooldown=6, Range=8,
         Count=1, Hits=0, Duration=2.0, Radius=1.0, Pierce=0, Speed=2.2, Desc='천천히 나는 소용돌이가 적을 모음', Unlock=15, Knock=0),
    dict(Id='wheel', Name='불바퀴', Elem='fire', Art='wheel', Kind='wheel', Rate=51.8, Rate2=0, Tick=0.2, Cooldown=10, Range=9,
         Count=1, Hits=75, Duration=15, Radius=0.45, Pierce=0, Speed=4.5, Desc='벽에 튕기며 굴러다니는 불바퀴', Unlock=20, Knock=0),
    dict(Id='tesla', Name='찌릿 코일', Elem='bolt', Art='tesla', Kind='tesla', Rate=625.3, Rate2=0, Tick=0, Cooldown=6, Range=10,
         Count=1, Hits=3, Duration=0.6, Radius=0.85, Pierce=0, Speed=0, Desc='충전한 번개가 적 사이를 튀며 폭발', Unlock=25, Knock=0),
]

# ================================================================== 카드 (make_tables.py 와 같은 꼴)
C = []


def card(id, tur, kind, name, desc, s=(), mx=1, w=10, line='', partner='', req=''):
    s = list(s) + [('', '')] * (3 - len(s))
    desc = desc.replace(' · ', ', ')
    row = dict(Id=id, Turret=tur, Kind=kind, Line=line, Name=name, Desc=desc, Max=mx, Weight=w, Partner=partner, Requires=req)
    for i, (k, v) in enumerate(s, 1):
        row[f'Stat{i}'] = k; row[f'Val{i}'] = v
    C.append(row)


for t in TURRET_NEW:
    card('new_' + t['Id'], t['Id'], 'new', t['Name'], '새 터렛을 빈 칸에 놓아요', w=12)
# 쾅쾅 대포
card('c_dmg', 'cannon', 'normal', '강력 폭발', '포탄 피해 +50%', [('dmg', 50)], 3)
card('c_dmg2', 'cannon', 'normal', '강력 폭발+', '포탄 피해 +100%', [('dmg', 100)], 1, 5)
card('c_shot', 'cannon', 'normal', '다중 포격', '포탄 +1 · 발당 피해 -15%', [('shots', 1)], 2)
card('c_knock', 'cannon', 'normal', '충격 강화', '밀어내기 +50% · 피해 +30%', [('knock', 50), ('dmg', 30)], 2)
card('c_area', 'cannon', 'normal', '거대 폭발', '폭발 범위 +70%', [('area', 70)], 1)
card('c_range', 'cannon', 'normal', '사거리 확장', '사거리 +70%', [('range', 70)], 1)
card('c_cd', 'cannon', 'normal', '빠른 장전', '공격 속도 +25%', [('cd', 25)], 2)
card('c_bA', 'cannon', 'break', '산탄 포화', '쏜 뒤 꿰뚫는 산탄 5발', [('scatter', 5)], line='A')
card('c_cA1', 'cannon', 'chain', '추가 장전', '산탄 +3', [('scatter', 3)], line='A', req='c_bA')
card('c_cA2', 'cannon', 'chain', '산탄 강화', '산탄 피해 +100%', [('scatter_dmg', 100)], line='A', req='c_bA')
card('c_bB', 'cannon', 'break', '부유 기뢰', '포탄마다 떠 있는 기뢰 2개', [('mines', 2)], line='B')
card('c_cB1', 'cannon', 'chain', '기뢰 지대', '기뢰 +2 · 기뢰 피해 +25%', [('mines', 2), ('mine_dmg', 25)], line='B', req='c_bB')
card('c_cB2', 'cannon', 'chain', '기뢰 충격파', '기뢰 +2 · 기뢰가 적을 밀어냄', [('mines', 2), ('mine_knock', 1)], line='B', req='c_bB')
# 집중 빔
card('f_dmg', 'focus', 'normal', '빔 강화', '빔 피해 +50%', [('dmg', 50)], 3)
card('f_dmg2', 'focus', 'normal', '빔 강화+', '빔 피해 +100%', [('dmg', 100)], 1, 5)
card('f_cd', 'focus', 'normal', '고주파 빔', '공격 속도 +30%', [('cd', 30)], 2)
card('f_dur', 'focus', 'normal', '효율 빔', '쏘는 시간 +40%', [('dur', 40)], 2)
card('f_tick', 'focus', 'normal', '촘촘한 빔', '틱 간격 -40%', [('tick', 66)], 1)
card('f_extra', 'focus', 'normal', '추가 빔', '다른 적에게 빔 1줄 더', [('beams', 1)], 1, 6)
card('f_vuln', 'focus', 'normal', '빔 관통', '맞은 적이 받는 피해 +15%', [('vuln', 15)], 1)
card('f_bA', 'focus', 'break', '집중 누적', '한 적에 오래 붙을수록 피해 최대 +200%', [('focus_ramp', 200)], line='A')
card('f_cA1', 'focus', 'chain', '누적 유지', '대상을 바꿔도 누적이 안 사라짐', [('focus_keep', 1)], line='A', req='f_bA')
card('f_cA2', 'focus', 'chain', '극한 집중', '누적 최대 +200% 더 (최대 +400%)', [('focus_ramp', 200)], line='A', req='f_bA')
card('f_bB', 'focus', 'break', '빔 굴절', '맞은 적에서 굴절 빔 2줄', [('refract', 2)], line='B')
card('f_cB1', 'focus', 'chain', '굴절 추가', '굴절 빔 +2', [('refract', 2)], line='B', req='f_bB')
card('f_cB2', 'focus', 'chain', '굴절 증폭', '굴절 빔 피해 +100%', [('refract_dmg', 100)], line='B', req='f_bB')
# 소용돌이포
card('v_dmg', 'vortex', 'normal', '고출력 장', '소용돌이 피해 +50%', [('dmg', 50)], 3)
card('v_dmg2', 'vortex', 'normal', '고출력 장+', '소용돌이 피해 +100%', [('dmg', 100)], 1, 5)
card('v_area', 'vortex', 'normal', '광역 장', '소용돌이 범위 +25%', [('area', 25)], 2)
card('v_dur', 'vortex', 'normal', '연장 장', '나는 시간 +30%', [('dur', 30)], 2)
card('v_pull', 'vortex', 'normal', '장 증폭', '끌어당김 +100% · 피해 +30%', [('pull_pow', 100), ('dmg', 30)], 1)
card('v_shot', 'vortex', 'normal', '추가 장', '소용돌이 +1 · 발당 피해 -15%', [('shots', 1)], 1)
card('v_cd', 'vortex', 'normal', '빠른 회전', '공격 속도 +25%', [('cd', 25)], 2)
card('v_bA', 'vortex', 'break', '작은 블랙홀', '사라진 자리에 5초 블랙홀', [('blackhole', 1)], line='A')
card('v_cA1', 'vortex', 'chain', '블랙홀 확장', '블랙홀 범위 · 끌어당김 +50%', [('bh_area', 50)], line='A', req='v_bA')
card('v_cA2', 'vortex', 'chain', '파괴 블랙홀', '블랙홀 피해 +250%', [('bh_dmg', 250)], line='A', req='v_bA')
card('v_bB', 'vortex', 'break', '중력 폭발', '사라질 때 모은 적에게 큰 폭발', [('vortex_burst', 1)], line='B')
card('v_cB1', 'vortex', 'chain', '폭발 확장', '중력 폭발 범위 +60%', [('burst_area', 60)], line='B', req='v_bB')
card('v_cB2', 'vortex', 'chain', '폭발 강화', '중력 폭발 피해 +100%', [('burst_dmg', 100)], line='B', req='v_bB')
# 불바퀴
card('w_dmg', 'wheel', 'normal', '불바퀴 강화', '불바퀴 피해 +50%', [('dmg', 50)], 3)
card('w_dmg2', 'wheel', 'normal', '불바퀴 강화+', '불바퀴 피해 +100%', [('dmg', 100)], 1, 5)
card('w_size', 'wheel', 'normal', '크기 강화', '불바퀴 크기 +25%', [('area', 25)], 2)
card('w_hits', 'wheel', 'normal', '지속 연장', '맞히는 횟수 +30%', [('hits_pct', 30)], 2)
card('w_speed', 'wheel', 'normal', '스마트 가속', '속도 +50% · 피해 +20%', [('speed', 50), ('dmg', 20)], 1)
card('w_shot', 'wheel', 'normal', '쌍바퀴', '불바퀴 +1 · 발당 피해 -15%', [('shots', 1)], 1)
card('w_cd', 'wheel', 'normal', '빠른 점화', '공격 속도 +25%', [('cd', 25)], 2)
card('w_bA', 'wheel', 'break', '불꽃 자국', '지나간 자리에 2초 불길', [('wheel_trail', 1)], line='A')
card('w_cA1', 'wheel', 'chain', '타오르는 자국', '불길 피해 +60%', [('trail_dmg', 60)], line='A', req='w_bA')
card('w_cA2', 'wheel', 'chain', '자국 연장', '불길 시간 +100%', [('trail_dur', 100)], line='A', req='w_bA')
card('w_bB', 'wheel', 'break', '꼬마 바퀴', '3초마다 꼬마 불바퀴 2개', [('mini_wheel', 2)], line='B')
card('w_cB1', 'wheel', 'chain', '꼬마 바퀴 떼', '꼬마 불바퀴 +2', [('mini_wheel', 2)], line='B', req='w_bB')
card('w_cB2', 'wheel', 'chain', '꼬마 바퀴 강화', '꼬마 불바퀴 피해 +50%', [('mini_dmg', 50)], line='B', req='w_bB')
# 찌릿 코일
card('t_dmg', 'tesla', 'normal', '전기 부스트', '번개 폭발 피해 +50%', [('dmg', 50)], 3)
card('t_dmg2', 'tesla', 'normal', '전기 부스트+', '번개 폭발 피해 +100%', [('dmg', 100)], 1, 5)
card('t_bounce', 'tesla', 'normal', '전기 체인', '튀기 +1 · 발당 피해 -15%', [('bounce', 1)], 2)
card('t_shot', 'tesla', 'normal', '전기 체인+', '번개 +1 (피해 그대로)', [('shots_free', 1)], 1, 5)
card('t_cd', 'tesla', 'normal', '전기 가속', '공격 속도 +30%', [('cd', 30)], 2)
card('t_area', 'tesla', 'normal', '전기 간섭', '폭발 범위 +30% · 10% 확률로 마비', [('area', 30), ('paralyze', 0.1)], 1)
card('t_wave', 'tesla', 'normal', '확장 충격파', '폭발 바깥에 약한 충격파', [('shockwave', 1)], 1)
card('t_bA', 'tesla', 'break', '전기 덫', '폭발 자리에 2초 덫 (둔화)', [('trap', 1)], line='A')
card('t_cA1', 'tesla', 'chain', '강화 덫', '덫 피해 +100% · 덫 시간 +50%', [('trap_dmg', 100), ('trap_dur', 50)], line='A', req='t_bA')
card('t_cA2', 'tesla', 'chain', '덫 낙뢰', '덫 위에 작은 낙뢰', [('trap_bolt', 1)], line='A', req='t_bA')
card('t_bB', 'tesla', 'break', '과충전', '충전 +0.5초 · 폭발 범위 +40%', [('overcharge', 1)], line='B')
card('t_cB1', 'tesla', 'chain', '과충전 파괴', '폭발 피해 +100%', [('dmg', 100)], line='B', req='t_bB')
card('t_cB2', 'tesla', 'chain', '연쇄 서지', '튀기 +3 (피해 그대로)', [('bounce_free', 3)], line='B', req='t_bB')
# 2차 콤보 (두 터렛이 모두 있을 때만)
card('cb_g_t', 'guardian', 'combo', '전류 탄환', '맞히면 5% 확률로 번개가 튐', [('hit_chain', 0.05)], partner='tesla', w=8)
card('cb_g_c', 'guardian', 'combo', '쾅쾅 탄환', '처치하면 15% 확률로 폭발', [('kill_explode', 0.15)], partner='cannon', w=8)
card('cb_g_v', 'guardian', 'combo', '중력탄', '맞은 적이 받는 피해 +10%', [('vuln', 10)], partner='vortex', w=8)
card('cb_r_w', 'rail', 'combo', '불꽃 관통', '관통 +1 · 포탄 피해 +20%', [('pierce', 1), ('dmg', 20)], partner='wheel', w=8)
card('cb_r_t', 'rail', 'combo', '서지 관통', '처치하면 50% 확률로 번개가 튐', [('kill_chain', 0.5)], partner='tesla', w=8)
card('cb_c_r', 'cannon', 'combo', '관통 포격', '포탄이 적 1마리를 꿰뚫음', [('pierce', 1)], partner='rail', w=8)
card('cb_c_v', 'cannon', 'combo', '중력 폭탄', '포탄 피해 +60%', [('dmg', 60)], partner='vortex', w=8)
card('cb_c_w', 'cannon', 'combo', '불씨 포탄', '포탄에 맞은 적 화상', [('burn', 1)], partner='wheel', w=8)
card('cb_f_b', 'focus', 'combo', '전기 빔', '틱마다 3% 확률로 마비', [('paralyze', 0.03)], partner='bolt', w=8)
card('cb_f_d', 'focus', 'combo', '끈끈 빔', '맞은 적 둔화 +20%', [('slow', 20)], partner='drone', w=8)
card('cb_l_f', 'laser', 'combo', '빛 공명', '레이저 피해 +40%', [('dmg', 40)], partner='focus', w=8)
card('cb_v_m', 'vortex', 'combo', '불 소용돌이', '소용돌이에 맞은 적 화상', [('burn', 1)], partner='missile', w=8)
card('cb_v_b', 'vortex', 'combo', '번개 소용돌이', '처치하면 30% 확률로 작은 낙뢰', [('kill_bolt', 0.3)], partner='bolt', w=8)
card('cb_w_d', 'wheel', 'combo', '끈끈 바퀴', '맞은 적 둔화 +15%', [('slow', 15)], partner='drone', w=8)
card('cb_w_c', 'wheel', 'combo', '폭발 바퀴', '처치하면 25% 확률로 폭발', [('kill_explode', 0.25)], partner='cannon', w=8)
card('cb_t_b', 'tesla', 'combo', '벼락 코일', '마비된 적에게 피해 +80%', [('dmg_vs_para', 80)], partner='bolt', w=8)
card('cb_t_l', 'tesla', 'combo', '빛 코일', '맞히면 15% 확률로 작은 레이저', [('hit_laser', 0.15)], partner='laser', w=8)
card('cb_m_c', 'missile', 'combo', '충격 미사일', '미사일이 적을 밀어냄', [('knock', 40)], partner='cannon', w=8)
card('cb_h_v', 'hive', 'combo', '벌떼 소용돌이', '벌 피해 +40%', [('dmg', 40)], partner='vortex', w=8)

# ================================================================== 스테이지 31 ~ 100 (make_tables.py 방식 이어서)
POOL = [
    ['pebble', 'fire'], ['pebble', 'fire', 'moth'], ['fire', 'moth', 'spider'], ['pebble', 'fire', 'ice', 'moth'],
    ['fire', 'moth', 'jelly'], ['pebble', 'ice', 'spider', 'jelly'], ['fire', 'iron', 'moth', 'beetle'], ['crack', 'moth', 'spider', 'ufo'],
    ['fire', 'ice', 'jelly', 'split'], ['iron', 'crack', 'beetle', 'sdrone'],
]
ELITES = ['big', 'moth_king', 'beetle_steel', 'mother']
SWARMS = ['pebble', 'moth', 'spider', 'sdrone', 'jelly_mini']
NAMES = ['운석 소나기', '나방 떼', '거미 언덕', '얼음 꼬리', '젤리 습격', '차가운 밤', '단단한 껍질', '하늘의 접시', '끈적한 비', '쇠 비']
STAGES = []
for i in range(30, 100):
    pool = set(POOL[i % 10] + POOL[(i + 3) % 10])
    if i >= 50:
        pool |= set(POOL[(i + 6) % 10])        # 51 부터는 세 묶음이 섞임
    STAGES.append(dict(Stage=i + 1, Name=f'{NAMES[i % 10]} {i // 10 + 1}', Enemies=','.join(sorted(pool)), Swarm=SWARMS[i % len(SWARMS)],
                       Elite=ELITES[i % 4], Boss='planet' if i % 2 == 0 else 'queen', Hp_Mul=1))

# ================================================================== 장비 (기획서 9-1: 6부위 · 등급 · 강화 · 분해)
GEAR_PART = ['Part', 'Name', 'Icon', 'Stat1', 'Base1', 'Per_Lv1', 'Stat2', 'Base2', 'Per_Lv2']
GEAR_PARTS = [  # 값 = (Base + Per_Lv x (Lv-1)) x 등급 배율
    ('weapon', '레이저 소총', 'g_weapon', 'atk', 30, 6, 'crit_dmg', 3, 0.3),
    ('helmet', '우주 헬멧', 'g_helmet', 'hp', 300, 60, 'atk', 8, 1.6),
    ('suit', '우주복', 'g_suit', 'hp', 500, 100, 'reduce', 1, 0.25),
    ('shield', '에너지 방패', 'g_shield', 'hp', 400, 80, 'reduce', 1.5, 0.3),
    ('core', '에너지 코어', 'g_core', 'atk', 20, 4, 'crit', 0.5, 0.05),
    ('boots', '점프 부츠', 'g_boots', 'atk', 20, 4, 'cd', 0.5, 0.08),
]
GEAR_GRADE = ['Grade', 'Name', 'Color', 'Mul', 'Lv_Max']
GEAR_GRADES = [(1, '일반', '#c9c1b3', 1, 10), (2, '고급', '#a9d68c', 1.5, 20), (3, '희귀', '#8fc8f0', 2.2, 30), (4, '영웅', '#bba3e6', 3.2, 40),
               (5, '전설', '#f2b46b', 4.6, 50), (6, '신화', '#ee8f7c', 6.5, 60), (7, '궁극', '#ffd34d', 9, 70)]

# ================================================================== 칩 (기획서 9-2 ~ 9-4) — Max = 궁극(7등급) 값, 등급 n = Max x n/7
# Scope: board(보드 전체 스탯) · cond(조건 피해, 일반 브래킷) · turret(그 터렛 — 카드와 같은 키) · all(모든 터렛 — 카드와 같은 키) · card(카드 지급)
# Parts: 끼울 수 있는 부위 (all = 모두)
CHIP_COLS = ['Id', 'Name', 'Desc', 'Scope', 'Turret', 'Stat', 'Max', 'Parts', 'Weight']
P_ALL, P_BC, P_BCW, P_AHS = 'all', 'boots,core', 'boots,core,weapon', 'suit,helmet,shield'
CHIPS = [
    # 보드 전체 스탯
    ('ch_atk', '공격 칩', '공격력 +{0}', 'board', '', 'atk', 70, P_ALL, 10),
    ('ch_atk_w', '무기 공격 칩', '공격력 +{0}', 'board', '', 'atk', 140, 'weapon', 8),
    ('ch_atk_pct', '공격 증폭 칩', '장비 · 칩 공격력 +{0}%', 'board', '', 'atk_pct', 100, 'suit', 6),
    ('ch_crit', '치명 칩', '치명 확률 +{0}%', 'board', '', 'crit', 7, P_ALL, 10),
    ('ch_crit_dmg', '치명 피해 칩', '치명 피해 +{0}%', 'board', '', 'crit_dmg', 70, P_BC, 8),
    ('ch_hp', '기지 칩', '기지 체력 +{0}', 'board', '', 'hp', 1400, P_ALL, 10),
    ('ch_reduce', '장갑 칩', '받는 피해 -{0}', 'board', '', 'reduce', 14, P_ALL, 8),
    ('ch_shield', '보호막 칩', '시작할 때 보호막 {0}', 'board', '', 'shield', 2800, P_AHS, 6),
    ('ch_immune', '반사막 칩', '처음 {0}번 맞아도 피해 없음', 'board', '', 'immune', 7, P_ALL, 5),
    ('ch_heal_elite', '회복 칩', '중간 보스 · 보스를 잡으면 기지 체력 {0}% 회복', 'board', '', 'heal_elite', 14, 'shield', 5),
    ('ch_heal_break', '돌파 회복 칩', '돌파를 고르면 기지 체력 {0}% 회복', 'board', '', 'heal_break', 12, P_AHS, 5),
    ('ch_crit_hp', '처형 칩', '치명 때 적 최대 체력 {0}% 피해 더', 'board', '', 'crit_hp', 8, P_BCW, 4),
    ('ch_extra', '연사 칩', '{0}% 확률로 한 번 더 쏨 (대장 카피 빼고)', 'board', '', 'extra_shot', 6, 'boots', 5),
    # 카드
    ('ch_refresh', '새로고침 칩', '카드 새로고침 +{0}번', 'card', '', 'refresh', 6, 'shield', 5),
    ('ch_choice', '첫 카드 칩', '첫 레벨업 카드 {0}장 더', 'card', '', 'choices', 1, 'shield', 4),
    ('ch_break', '돌파 칩', '돌파 카드가 나올 확률 +{0}%', 'card', '', 'break_rate', 200, 'core', 5),
    # 조건 피해 (일반 브래킷)
    ('ch_dmg', '피해 칩', '피해 +{0}%', 'cond', '', 'dmg', 14, P_BC, 10),
    ('ch_fire', '불 칩', '불 속성 피해 +{0}%', 'cond', '', 'el_fire', 28, P_ALL, 7),
    ('ch_bolt', '번개 칩', '번개 속성 피해 +{0}%', 'cond', '', 'el_bolt', 28, P_ALL, 7),
    ('ch_grav', '중력 칩', '중력 속성 피해 +{0}%', 'cond', '', 'el_grav', 28, P_ALL, 7),
    ('ch_light', '빛 칩', '빛 속성 피해 +{0}%', 'cond', '', 'el_light', 28, P_ALL, 7),
    ('ch_phys', '물리 칩', '물리 속성 피해 +{0}%', 'cond', '', 'el_phys', 21, P_ALL, 7),
    ('ch_hp70', '선제 칩', '체력 70% 넘는 적에게 피해 +{0}%', 'cond', '', 'hp70', 70, P_ALL, 6),
    ('ch_elite', '보스 사냥 칩', '중간 보스 · 보스에게 피해 +{0}%', 'cond', '', 'elite_dmg', 140, P_BCW, 6),
    ('ch_early', '기선 칩', '처음 2분 동안 피해 +{0}%', 'cond', '', 'early', 84, P_AHS, 6),
    ('ch_low', '역습 칩', '기지 체력 30% 아래면 피해 +{0}%', 'cond', '', 'low_hp', 110, P_BCW, 5),
    ('ch_debuff', '약점 공략 칩', '상태이상 걸린 적에게 피해 +{0}%', 'cond', '', 'debuff', 42, P_BCW, 6),
    ('ch_near', '근접 방어 칩', '기지 근처 적에게 피해 +{0}%', 'cond', '', 'near', 42, 'suit,boots,core', 6),
    ('ch_boss_on', '긴장 칩', '중간 보스 · 보스가 있으면 피해 +{0}%', 'cond', '', 'boss_on', 50, 'boots', 5),
    ('ch_per_turret', '포대 칩', '터렛 1기마다 피해 +{0}%', 'cond', '', 'per_turret', 4.5, 'helmet', 5),
    ('ch_per_card', '수집 칩', '고른 카드 1장마다 피해 +{0}%', 'cond', '', 'per_card', 1, 'suit', 5),
    # 모든 터렛 (카드와 같은 키)
    ('ch_cd', '가속 칩', '모든 터렛 공격 속도 +{0}%', 'all', '', 'cd', 7, P_AHS, 7),
    ('ch_dur', '지속 칩', '모든 터렛 지속 시간 +{0}%', 'all', '', 'dur', 21, P_ALL, 5),
]
TURRET_IDS = ['guardian', 'rail', 'laser', 'drone', 'hive', 'missile', 'bolt', 'cannon', 'focus', 'vortex', 'wheel', 'tesla']
TNAME = dict(guardian='대장 카피', rail='관통포', laser='레이저 포', drone='끈끈이 드론', hive='벌집', missile='유도 미사일', bolt='벼락탑',
             cannon='쾅쾅 대포', focus='집중 빔', vortex='소용돌이포', wheel='불바퀴', tesla='찌릿 코일')
# 터렛 전용 (기획서 9-4 전용 칩 목록에서 하나씩 — 카드와 같은 키라 효과가 그대로 들어감)
SPECIAL = dict(
    guardian=('pierce', 2, '총알 관통 +{0}', 'shield,boots'), rail=('pierce', 4, '관통 +{0}', 'shield'),
    laser=('dur', 50, '쏘는 시간 +{0}%', 'core,weapon'), drone=('tick', 50, '틱 간격 빠르게 +{0}%', 'helmet'),
    hive=('hits', 5, '벌 공격 횟수 +{0}', 'helmet'), missile=('area', 40, '폭발 범위 +{0}%', 'suit,shield'),
    bolt=('paralyze', 0.15, '틱마다 마비 확률 +{0}', 'weapon'), cannon=('knock', 80, '밀어내기 +{0}%', 'weapon'),
    focus=('dur', 30, '쏘는 시간 +{0}%', 'core'), vortex=('pull_pow', 32, '끌어당김 +{0}%', 'suit,shield'),
    wheel=('hits_pct', 50, '맞히는 횟수 +{0}%', 'helmet'), tesla=('bounce_free', 2, '튀기 +{0}', 'core'),
)
for tid in TURRET_IDS:
    n = TNAME[tid]
    CHIPS.append((f'tc_{tid}_dmg', f'{n} 칩', f'{n} 피해 +{{0}}%', 'turret', tid, 'dmg', 42, 'all', 4))
    if tid != 'guardian':
        CHIPS.append((f'tc_{tid}_star', f'{n} 별 칩', f'{n}을(를) 놓으면 별 +{{0}}', 'turret', tid, 'start_stars', 2, 'suit,boots', 3))
    k, mx, d, parts = SPECIAL[tid]
    CHIPS.append((f'tc_{tid}_sp', f'{n} 특수 칩', f'{n} {d}', 'turret', tid, k, mx, parts, 3))

# ================================================================== 일일 미션 · 출석 7일
MISSION_COLS = ['Id', 'Kind', 'Target', 'Name', 'Reward', 'Value']
MISSIONS = [  # Kind: play 판 · win 승리 · kill 처치 · up 강화(터렛 · 기지 · 장비) · patrol 순찰 받기 · elite 엘리트 난이도 판 · all 오늘 미션 다 끝냄
    ('m_play', 'play', 3, '전투 {0}판 하기', 'gold', 1500),
    ('m_win', 'win', 1, '스테이지 {0}번 이기기', 'shards', 10),
    ('m_kill', 'kill', 500, '적 {0}마리 처치', 'gold', 2000),
    ('m_up', 'up', 3, '강화 {0}번 하기', 'parts', 5),
    ('m_patrol', 'patrol', 1, '순찰 보상 {0}번 받기', 'stamina', 10),
    ('m_elite', 'elite', 1, '엘리트 난이도 {0}판 하기', 'chip', 2),
    ('m_all', 'all', 6, '오늘 미션 모두 끝내기', 'gear', 3),
]
ATTEND_COLS = ['Day', 'Reward', 'Value']   # chip/gear 의 Value = 등급
ATTEND = [(1, 'gold', 1000), (2, 'stamina', 30), (3, 'shards', 20), (4, 'parts', 10), (5, 'chip', 2), (6, 'gold', 5000), (7, 'gear', 3)]

# ================================================================== 글자
STRINGS = [
    ('tab_battle', '전투'), ('tab_turret', '터렛'), ('tab_gear', '장비'), ('tab_mission', '미션'),
    ('normal', '보통'), ('elite_mode', '엘리트'), ('elite_locked', '보통을 깨면 열려요'), ('elite_desc', '적이 강하고 보상이 2배'),
    ('stamina', '스태미나'), ('stamina_lack', '스태미나가 부족해요'), ('stamina_ad', '광고 보고 +{0}'), ('stamina_ad_left', '오늘 {0}/{1}번'),
    ('stamina_next', '{0} 뒤 +1'), ('stamina_full', '가득'), ('close', '닫기'), ('ok', '확인'),
    ('unlock_at', '스테이지 {0}을(를) 깨면 열려요'), ('unlocked', '새 터렛이 열렸어요!'),
    ('gear_title', '장비'), ('chip', '칩'), ('gear_list', '장비 {0}'), ('chip_list', '칩 {0}'), ('parts', '부품'),
    ('equip', '끼우기'), ('unequip', '빼기'), ('scrap', '분해'), ('merge', '합성'), ('merge_all', '모두 합성'), ('enhance', '강화'),
    ('chip_slots', '칩 칸'), ('chip_locked', '스테이지 {0}'), ('no_chip', '끼울 수 있는 칩이 없어요'), ('empty_part', '비어 있음'),
    ('stats_title', '능력치'), ('st_atk', '공격력'), ('st_hp', '기지 체력'), ('st_reduce', '받는 피해 -'), ('st_crit', '치명 확률'),
    ('st_crit_dmg', '치명 피해'), ('st_cd', '공격 속도'), ('gear_lv', 'Lv {0}/{1}'),
    ('drops', '얻은 장비 · 칩'), ('mission_title', '일일 미션'), ('attend_title', '출석 보상'), ('attend_day', '{0}일'),
    ('attend_claim', '오늘 출석 받기'), ('attend_done', '내일 또 만나요'), ('claimed', '받음'), ('mission_reset', '매일 밤 12시에 새로'),
    ('settings', '설정'), ('set_bgm', '배경음'), ('set_sfx', '효과음'), ('set_vib', '진동'), ('set_nums', '피해 숫자'),
    ('set_fps', '프레임 60'), ('set_tutorial', '도움말 다시 보기'), ('on', '켬'), ('off', '끔'),
    ('sound_soon', '소리는 다음 작업에서 들어가요 (값만 저장)'),
    ('tut_move', '화면을 끌면 대장 카피가 좌우로 움직여요'), ('tut_card', '카드 1장을 골라요. 같은 터렛을 고를수록 별이 쌓여요'),
    ('tut_star', '별 4개! 돌파 카드로 터렛이 다음 단계가 돼요'), ('tut_turret', '전투에서 얻은 조각으로 터렛을 강화해요'),
    ('tut_gear', '장비를 얻었어요! 장비 칸에서 끼워 보세요'), ('tut_elite', '엘리트 난이도가 열렸어요. 더 강한 적, 2배 보상'),
    ('tap_close', '눌러서 닫기'), ('reward_get', '받았어요!'), ('grade_n', '{0}등급'),
    ('r_gold', '골드'), ('r_shards', '조각'), ('r_stamina', '스태미나'), ('r_parts', '부품'), ('r_chip', '칩'), ('r_gear', '장비'),
    ('enemy_weak', '적 약점: {0}'), ('cost_n', '{0}'),
    # 11-16 저녁 (2차 의견 반영)
    ('weak_title', '적 약점'), ('enemies_title', '나오는 적'), ('weak_hint', '칸 아래 색 = 그 적의 약점'),
    ('elite_title', '중간 보스 (2분)'), ('boss_title', '보스 (3분 30초)'),
    ('tinfo_stat', '피해 {0}% (공격력 기준), 쿨다운 {1}초, 사거리 {2}'), ('tinfo_stat_field', '틱마다 피해 {0}%, 쿨다운 {1}초, 사거리 화면 전체'),
]


def main():
    # ---- Config
    wb = load_workbook(os.path.join(TB, 'Config.xlsx')); ws = wb.active
    for i, c in enumerate(CHIPS):
        CHIPS[i] = c[:2] + (c[2].replace(' · ', ', '),) + c[3:]
    print('Config +', add_rows(ws, 'Key', [dict(Key=k, Value=v, Note=n) for k, v, n in CONFIG]))
    wb.save(os.path.join(TB, 'Config.xlsx'))
    # ---- Turret
    wb = load_workbook(os.path.join(TB, 'Turret.xlsx')); ws = wb['Turret']
    ensure_col(ws, 'Unlock', 0); ensure_col(ws, 'Knock', 0)
    print('Turret +', add_rows(ws, 'Id', TURRET_NEW))
    ci = ensure_col(ws, 'Info'); cs = cols(ws); ii = cs.index('Id') + 1
    for r in range(2, ws.max_row + 1):
        tid = ws.cell(r, ii).value
        if tid in TURRET_INFO and not ws.cell(r, ci).value:
            ws.cell(r, ci, TURRET_INFO[tid])
    wb.save(os.path.join(TB, 'Turret.xlsx'))
    # ---- Card
    wb = load_workbook(os.path.join(TB, 'Card.xlsx')); ws = wb['Card']
    print('Card +', add_rows(ws, 'Id', C))
    wb.save(os.path.join(TB, 'Card.xlsx'))
    # ---- Stage
    wb = load_workbook(os.path.join(TB, 'Stage.xlsx')); ws = wb['Stage']
    print('Stage +', add_rows(ws, 'Stage', STAGES))
    wb.save(os.path.join(TB, 'Stage.xlsx'))
    # ---- String
    wb = load_workbook(os.path.join(TB, 'String.xlsx')); ws = wb['String']
    print('String +', add_rows(ws, 'Key', [dict(Key=k, KO=v.replace(' · ', ', '), EN='') for k, v in STRINGS]))
    wb.save(os.path.join(TB, 'String.xlsx'))
    # ---- 새 표
    new_book(os.path.join(TB, 'Gear.xlsx'), {
        'Gear_Part': (GEAR_PART, 'Part', GEAR_PARTS, '값 = (Base + Per_Lv x (Lv-1)) x 등급 Mul · Stat: atk 공격력 · hp 기지 체력 · reduce 받는 피해 - · crit 치명 % · crit_dmg 치명 피해 % · cd 공격 속도 %'),
        'Gear_Grade': (GEAR_GRADE, 'Grade', GEAR_GRADES, 'Mul = 장비 값 배율 · Lv_Max = 강화 최대 · 칩 등급 n 의 값 = 칩 Max x n/7'),
    })
    new_book(os.path.join(TB, 'Chip.xlsx'), {
        'Chip': (CHIP_COLS, 'Id', CHIPS, 'Max = 7등급(궁극) 값 · Scope: board 보드 · cond 조건 피해 · turret 그 터렛 · all 모든 터렛 · card 카드 · Parts = 끼울 수 있는 부위'),
    })
    new_book(os.path.join(TB, 'Mission.xlsx'), {
        'Mission': (MISSION_COLS, 'Id', MISSIONS, 'Kind: play · win · kill · up · patrol · elite · all · Reward: gold · shards · stamina · parts · chip(값 = 등급) · gear(값 = 등급)'),
        'Attend': (ATTEND_COLS, 'Day', ATTEND, '7일을 다 받으면 1일부터 다시'),
    })
    print('카드 2차', len(C), '· 칩', len(CHIPS), '· 스테이지 +', len(STAGES))


if __name__ == '__main__':
    main()
