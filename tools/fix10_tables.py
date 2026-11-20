# -*- coding: utf-8 -*-
"""11-20 코스튬 · 탈것 (요청: 외형 메뉴에 코스튬 · 탈것 — 착용하면 능력치, 어떤 능력치인지는 아직 미정)
- Look.xlsx (새 표, 시트 Costume · Mount)
    Id · Name · Desc · Price_Dia · Stat1 · Val1 · Stat2 · Val2 (+ 탈것만 Bob · Lift · Scale)
    · 능력치 칸(Stat1~Val2)은 비워 둠 = 화면에 "능력치 준비 중". 정해지면 표에 키 · 값만 넣으면 착용할 때 더해짐
      키 = 장비와 같음: atk(공격력) · atk_pct(공격력 %) · hp(체력) · base_atk(기지 공격력 %) · base_hp(기지 체력 %)
            · shield_pct(보호막 %) · reduce(방어력) · crit(치명 확률 %p) · crit_dmg(치명 피해 %) · cd(재사용 %) · 그 밖의 회로 키
    · Price_Dia = 임시 값 (밸런스는 마지막)
    · Lift = 탈것 그림 맨 아래가 발보다 얼마나 아래인지(월드 칸) — tools/look_art.py export 가 tools/look_pivots.json 에 씀
    · Bob = 1 이면 떠다니는 탈것 (위아래 둥실) · Scale = 탈것 크기 x
  → 다시 돌려도 손으로 넣은 Name · Desc · Price_Dia · Stat · Val · Bob · Scale 은 그대로 (없는 줄만 더하고 Lift 만 새로 씀)
- Config: Mount_Battle_Scale · Mount_Lobby_Scale · Mount_Lift_Mul · Mount_Bob · Mount_Bob_Amp
- String: 외형 탭 코스튬 · 탈것 글자
사용: PYTHONUTF8=1 python tools/fix10_tables.py   (그다음 표 → csv 는 Unity 배치 TableConverter)
"""
import os, sys, json
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from phase2_tables import TB
from fix9_tables import upsert
from look_art import COSTUMES, MOUNTS
from openpyxl import load_workbook, Workbook
from openpyxl.styles import Font, PatternFill

HERE = os.path.dirname(os.path.abspath(__file__))
PRICE_COSTUME, PRICE_MOUNT = 300, 500        # 임시

C_HEAD = ['Id', 'Name', 'Desc', 'Price_Dia', 'Stat1', 'Val1', 'Stat2', 'Val2']
M_HEAD = C_HEAD + ['Bob', 'Lift', 'Scale']
NOTE = ('메모: 착용 중인 코스튬 · 탈것의 Stat1/Val1 · Stat2/Val2 가 능력치로 더해짐 (비우면 없음 = "능력치 준비 중"). '
        '키 = atk · atk_pct · hp · base_atk · base_hp · shield_pct · reduce · crit · crit_dmg · cd (장비와 같음). Price_Dia 는 임시 값')


def sheet(wb, name, head, rows):
    if name in wb.sheetnames:
        ws = wb[name]
        have = [c.value for c in ws[1]]
        for h in head:
            if h not in have: ws.cell(1, len([x for x in have if x is not None]) + 1, h); have = [c.value for c in ws[1]]
    else:
        ws = wb.create_sheet(name); ws.append(head + ['', NOTE])
        hf = PatternFill('solid', fgColor='FFE9C9')
        for c in ws[1]: c.font = Font(bold=True); c.fill = hf
        ws.column_dimensions['B'].width = 14; ws.column_dimensions['C'].width = 30
    head_now = [c.value for c in ws[1]]
    at = {ws.cell(r, 1).value: r for r in range(2, ws.max_row + 1) if ws.cell(r, 1).value}
    add = 0
    for row in rows:
        if row['Id'] in at:
            r = at[row['Id']]
            if 'Lift' in row: ws.cell(r, head_now.index('Lift') + 1, row['Lift'])     # 그림이 바뀌면 높이만 새로
        else:
            ws.append([row.get(h, '') for h in head_now]); add += 1
    return add


def write_look():
    p = os.path.join(TB, 'Look.xlsx')
    if os.path.exists(p): wb = load_workbook(p)
    else: wb = Workbook(); wb.remove(wb.active)
    lift = json.load(open(os.path.join(HERE, 'look_pivots.json'), encoding='utf-8'))['lift']
    crow = [dict(Id=i, Name=n, Desc='', Price_Dia=0 if i == 'basic' else PRICE_COSTUME) for i, n, _ in COSTUMES]
    mrow = [dict(Id=i, Name=n, Desc='', Price_Dia=0 if i == 'none' else PRICE_MOUNT, Bob=b, Lift=lift.get(i, 0), Scale=1) for i, n, b in MOUNTS]
    a = sheet(wb, 'Costume', C_HEAD, crow); b = sheet(wb, 'Mount', M_HEAD, mrow)
    wb.save(p); print('Look.xlsx 코스튬 +', a, '· 탈것 +', b)


CONFIG = [
    ('Mount_Battle_Scale', 1, '11-20: 전투에서 탈것 크기 x (대장 카피 몸은 그대로)'),
    ('Mount_Lobby_Scale', 0.8, '11-20: 로비에서 탈것 크기 x'),
    ('Mount_Lift_Mul', 1, '11-20: 탈것에 탈 때 대장 카피를 올리는 높이 x (Look 표 Lift x 이 값)'),
    ('Mount_Bob', 1, '11-20: 1 = 떠다니는 탈것(Look 표 Bob 1)은 위아래 둥실 · 0 = 끔'),
    ('Mount_Bob_Amp', 0.06, '11-20: 둥실 높이 (월드 칸)'),
]
STRINGS = [
    ('looks_costume', '코스튬'), ('looks_mount', '탈것'),
    ('look_now', '지금 모습'), ('look_preview', '미리 보기'), ('look_back_now', '지금 모습으로'),
    ('look_tap_tip', '목록을 누르면 미리 볼 수 있어요'), ('look_stat_none', '능력치 준비 중'),
    ('look_no_mount', '걸어서 싸워요'), ('look_got', '{0} 착용!'),
    ('st_atk_pct', '공격력'), ('st_base_atk', '기지 공격력'), ('st_base_hp', '기지 체력'), ('st_shield_pct', '시작 보호막'),
]


if __name__ == '__main__':
    write_look()
    p = os.path.join(TB, 'Config.xlsx'); wb = load_workbook(p); ws = wb.active
    print('Config', upsert(ws, 'Key', [dict(Key=k, Value=v, Note=n) for k, v, n in CONFIG])); wb.save(p)
    p = os.path.join(TB, 'String.xlsx'); wb = load_workbook(p); ws = wb['String']
    print('String', upsert(ws, 'Key', [dict(Key=k, KO=v) for k, v in STRINGS])); wb.save(p)
    print('ok')
