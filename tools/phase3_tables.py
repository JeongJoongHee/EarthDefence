# -*- coding: utf-8 -*-
"""3차 표 더하기 (2026-11-16 밤, 사용자 "동료 액티브 스킬을 제외하고 3차") — phase2_tables.py 처럼 **없는 줄 · 칸만 더함**.

새 표
  Frame.xlsx : 기지 외형 6종 (별 1~5 · 효과 값 = Base + Per_Star x (별-1))
  Skin.xlsx  : 터렛 스킨 11종 (색 돌리기 Hue · 필살기 Ult · 필살기까지 쏘는 횟수 Ult_Charge · 피해 보너스)
  Shop.xlsx  : Product(다이아 결제 [초안] · 원화) · Item(상자 · 스태미나 · 골드 · 무료 · 광고) · Box_Rate(상자 등급 확률)
더하는 것: Config(다이아 · 도전 모드 · 필살기 · 결과 광고) · String
사용: PYTHONUTF8=1 python tools/phase3_tables.py
"""
import os, sys
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from phase2_tables import TB, add_rows, new_book
from openpyxl import load_workbook

CONFIG = [
    # 다이아 (3차 상점 · 유료 재화) — 얻는 곳
    ('Dia_First_Clear', 5, '보통 스테이지를 처음 깰 때 다이아'), ('Dia_First_Clear_10', 50, '10의 배수 스테이지를 처음 깰 때 더'),
    ('Dia_Elite_First', 10, '엘리트 난이도를 처음 깰 때 다이아'), ('Start_Dia', 100, '처음 다이아'),
    # 도전: 무한 모드
    ('Endless_Tickets', 3, '무한 모드 하루 입장'), ('Endless_Growth', 1.55, '무한 모드: 1분마다 적 체력 x'),
    ('Endless_Boss_Gap', 150, '무한 모드: 보스 간격(초) · 그 절반에 중간 보스'), ('Endless_Level_Max', 30, '무한 모드 레벨 상한'),
    ('Endless_Gold_Min', 600, '무한 모드: 버틴 1분마다 골드 (스테이지 배율 곱함)'), ('Endless_Dia_Min', 4, '무한 모드: 버틴 1분마다 다이아'),
    ('Endless_Spawn_Growth', 0.35, '무한 모드: 1분마다 초당 적 +'),
    # 도전: 엘리트 군단 (3분 버티기)
    ('Survival_Tickets', 2, '엘리트 군단 하루 입장'), ('Survival_Time', 180, '엘리트 군단: 버틸 시간(초)'),
    ('Survival_Elite_Gap', 12, '엘리트 군단: 중간 보스가 나오는 간격(초)'), ('Survival_HP', 1.4, '엘리트 군단: 적 체력 x (최고 스테이지 기준)'),
    ('Survival_Dia', 30, '엘리트 군단 승리 다이아'), ('Survival_Drops', 3, '엘리트 군단 승리 장비 · 칩'),
    # 필살기 · 스킨
    ('Ult_On', 1, '1 = 스킨을 끼우면 필살기 (0 = 끔)'),
    # 광고 [초안]
    ('Ad_Reward_Mul', 2, '결과 창 "광고 보고 2배"'), ('Ad_Box_Per_Day', 1, '상점 광고 상자 하루 횟수'), ('Free_Box_Per_Day', 1, '상점 무료 상자 하루 횟수'),
    ('Frame_Up_Gold', 20000, '기지 외형 별 올리기 골드 (별 1 → 2) · 다음은 x3'),
]

FRAME_COLS = ['Id', 'Name', 'Effect', 'Base', 'Per_Star', 'Cooldown', 'Price_Dia', 'Tint', 'Glow', 'Desc']
FRAMES = [  # Desc 의 {0} = 효과 값 · {1} = 쿨다운
    ('basic', '기본 돔', 'none', 0, 0, 0, 0, '#ffffff', '#cfeeff', '효과 없음'),
    ('prism', '무지개 돔', 'revive', 20, 10, 0, 600, '#f3dcff', '#d9b8ff', '기지가 처음 무너지면 체력 {0}% 로 버티고 3초 무적 (한 판 1번)'),
    ('starsea', '별바다 장막', 'starfall', 400, 200, 40, 500, '#d8e0ff', '#9fb2ff', '{1}초마다 별똥별 비 — 모든 적에게 공격력 {0}% 피해'),
    ('frost', '얼음 돔', 'freeze', 1.5, 0.5, 30, 500, '#dff6ff', '#9fe8ff', '{1}초마다 기지 근처 적을 {0}초 얼림 (보스 빼고)'),
    ('thorn', '가시 돔', 'thorn', 150, 75, 0, 400, '#e8f4d8', '#b8e08f', '방어막을 때린 적에게 공격력 {0}% 반격'),
    ('bloom', '꽃 돔', 'regen', 0.5, 0.25, 5, 400, '#ffe4ec', '#ffb3c8', '{1}초마다 기지 체력 {0}% 회복'),
    ('gold', '황금 돔', 'gold', 10, 5, 0, 300, '#fff2c8', '#ffd34d', '판에서 얻는 골드 +{0}%'),
]

SKIN_COLS = ['Id', 'Turret', 'Name', 'Hue', 'Sat', 'Ult', 'Ult_Name', 'Ult_Desc', 'Ult_Charge', 'Ult_Rate', 'Dmg', 'Price_Dia']
SKINS = [  # Hue = 색을 몇 도 돌리나 · Dmg = 스킨이 있으면 그 터렛 후반 피해 +% · Ult_Rate = 필살기 %ATK
    ('sk_rail', 'rail', '은하 관통포', 200, 1.15, 'rail', '은하 관통', '거대한 관통 광선이 일직선의 모든 적을 꿰뚫어요', 6, 1500, 10, 300),
    ('sk_laser', 'laser', '오로라 레이저', 120, 1.2, 'laser', '오로라 쓸기', '레이저 3줄이 화면을 쓸어요', 4, 60, 10, 300),
    ('sk_drone', 'drone', '젤리 드론', 230, 1.6, 'drone', '끈끈 대폭발', '장판 3개를 한꺼번에 깔아요', 3, 30, 10, 300),
    ('sk_hive', 'hive', '꿀벌 왕국', 300, 1.1, 'hive', '벌 떼', '꼬마 벌 15마리가 한꺼번에 나가요', 3, 220, 10, 300),
    ('sk_missile', 'missile', '불꽃놀이 미사일', 250, 1.2, 'missile', '미사일 비', '하늘에서 미사일 12발이 떨어져요', 4, 300, 10, 300),
    ('sk_bolt', 'bolt', '보랏빛 벼락', 230, 1.2, 'bolt', '천둥 7줄', '낙뢰 7줄이 동시에 떨어져요', 4, 200, 10, 300),
    ('sk_cannon', 'cannon', '바다 대포', 180, 1.9, 'cannon', '거대 포탄', '아주 큰 포탄이 넓게 터지며 밀어내요', 4, 2000, 10, 400),
    ('sk_focus', 'focus', '별빛 빔', 205, 1.3, 'focus', '세 갈래 집중', '빔 3줄을 동시에 붙여요', 3, 0, 10, 400),
    ('sk_vortex', 'vortex', '초록 소용돌이', 230, 1.5, 'vortex', '큰 블랙홀', '3초 동안 화면 가운데에 큰 블랙홀', 4, 150, 10, 400),
    ('sk_wheel', 'wheel', '파란 불바퀴', 190, 1.2, 'wheel', '불바퀴 떼', '불바퀴 6개를 한꺼번에 굴려요', 3, 0, 10, 400),
    ('sk_tesla', 'tesla', '분홍 코일', 300, 1.15, 'tesla', '번개 폭풍', '번개 3줄이 12번씩 튀어요', 4, 0, 10, 400),
]

PRODUCT_COLS = ['Id', 'Name', 'Dia', 'Price_KRW', 'First_Double', 'Product_Id']
PRODUCTS = [  # [초안] — 실제 상품 ID 는 출시 때 Play Console 에서 (Product_Id 칸)
    ('p_dia1', '다이아 한 줌', 80, 1200, 1, 'earth_dia_80'),
    ('p_dia2', '다이아 주머니', 420, 5900, 1, 'earth_dia_420'),
    ('p_dia3', '다이아 상자', 900, 12000, 1, 'earth_dia_900'),
    ('p_dia4', '다이아 궤짝', 1900, 25000, 1, 'earth_dia_1900'),
    ('p_dia5', '다이아 산더미', 5000, 59000, 1, 'earth_dia_5000'),
]
ITEM_COLS = ['Id', 'Name', 'Kind', 'Cost_Dia', 'Value', 'Per_Day', 'Desc']
ITEMS = [  # Kind: gear_box · chip_box · stamina · gold(Value = 순찰 몇 시간치) · free_box · ad_box
    ('i_free', '오늘의 무료 상자', 'free_box', 0, 1, 1, '장비 또는 칩 1개'),
    ('i_ad', '광고 상자', 'ad_box', 0, 1, 1, '광고 보고 장비 또는 칩 1개'),
    ('i_gear', '장비 상자', 'gear_box', 150, 1, 0, '장비 1개 (등급 확률 보기)'),
    ('i_gear10', '장비 상자 10+1', 'gear_box', 1500, 11, 0, '장비 11개'),
    ('i_chip', '칩 상자', 'chip_box', 150, 1, 0, '칩 1개 (등급 확률 보기)'),
    ('i_chip10', '칩 상자 10+1', 'chip_box', 1500, 11, 0, '칩 11개'),
    ('i_stamina', '스태미나 30', 'stamina', 50, 30, 3, '하루 3번'),
    ('i_gold', '골드 주머니', 'gold', 100, 4, 0, '순찰 4시간치 골드'),
]
BOX_COLS = ['Grade', 'Rate']   # 상자 등급 확률 (%) — 화면 [확률] 에 그대로 보임 (확률형 아이템 표시)
BOX_RATES = [(1, 40), (2, 30), (3, 18), (4, 8), (5, 3.2), (6, 0.7), (7, 0.1)]

STRINGS = [
    ('tab_challenge', '도전'), ('tab_shop', '상점'), ('dia', '다이아'),
    ('ch_endless', '무한 모드'), ('ch_endless_desc', '끝없이 몰려오는 적을 얼마나 오래 버틸까요? 버틴 시간만큼 골드 · 다이아'),
    ('ch_survival', '엘리트 군단'), ('ch_survival_desc', '중간 보스가 계속 나와요. 3분 버티면 다이아 · 장비 · 칩'),
    ('ch_tickets', '오늘 {0}/{1}번'), ('ch_best', '최고 {0}'), ('ch_start', '도전'), ('ch_no_ticket', '오늘 입장 횟수를 다 썼어요'),
    ('survive_left', '버티기 {0}'), ('endless_time', '버틴 시간 {0}'), ('survival_win', '군단을 막아냈어요!'),
    ('frame', '기지 외형'), ('frame_star', '별 {0}'), ('frame_up', '별 올리기'), ('owned', '가짐'), ('equipped', '끼움'),
    ('buy', '사기'), ('skin', '스킨'), ('skins', '터렛 스킨'), ('ult', '필살기'), ('ult_ready', '필살기!'),
    ('skin_bonus', '가지고 있으면 {0} 피해 +{1}% · 끼우면 필살기'), ('no_dia', '다이아가 모자라요'),
    ('shop_dia', '다이아'), ('shop_box', '상자'), ('shop_etc', '스태미나 · 골드'), ('rates', '확률'), ('rate_title', '상자 등급 확률'),
    ('first_double', '첫 구매 2배'), ('iap_draft', '[초안] 결제 없이 바로 지급 (출시 때 실제 결제)'), ('iap_off', '결제는 출시 때 열려요'),
    ('per_day', '오늘 {0}/{1}'), ('ad_double', '광고 보고 보상 x{0}'), ('got_double', '보상 2배 받았어요!'),
    ('account', '구글 계정'), ('account_link', '연결하기'), ('account_linked', '연결됨'), ('cloud_save', '클라우드에 저장'),
    ('cloud_load', '클라우드에서 불러오기'), ('cloud_draft', '[초안] 실제 구글 로그인 · 클라우드는 출시 때'), ('cloud_none', '저장된 것이 없어요'),
    ('cloud_saved', '저장했어요'), ('cloud_cmp', '어느 쪽을 쓸까요?'), ('cloud_local', '이 기기'), ('cloud_remote', '클라우드'),
    ('frame_eff_revive', '부활!'), ('frame_eff_starfall', '별똥별 비!'), ('frame_eff_freeze', '얼음!'),
    ('mode_endless', '무한 모드'), ('mode_survival', '엘리트 군단'),
]


def main():
    wb = load_workbook(os.path.join(TB, 'Config.xlsx')); ws = wb.active
    print('Config +', add_rows(ws, 'Key', [dict(Key=k, Value=v, Note=n) for k, v, n in CONFIG]))
    wb.save(os.path.join(TB, 'Config.xlsx'))
    wb = load_workbook(os.path.join(TB, 'String.xlsx')); ws = wb['String']
    print('String +', add_rows(ws, 'Key', [dict(Key=k, KO=v.replace(' · ', ', '), EN='') for k, v in STRINGS]))
    wb.save(os.path.join(TB, 'String.xlsx'))
    new_book(os.path.join(TB, 'Frame.xlsx'), {'Frame': (FRAME_COLS, 'Id', FRAMES, '효과 값 = Base + Per_Star x (별-1) · Effect: none · revive · starfall · freeze · thorn · regen · gold · Tint = 돔 색 · Glow = 둘레 빛')})
    new_book(os.path.join(TB, 'Skin.xlsx'), {'Skin': (SKIN_COLS, 'Id', SKINS, 'Hue = 색을 돌리는 도(0~360) · Sat = 채도 x · Ult_Charge = 몇 번 쏘면 필살기 · Ult_Rate = 필살기 %ATK · Dmg = 가지고 있으면 그 터렛 후반 피해 +%')})
    new_book(os.path.join(TB, 'Shop.xlsx'), {
        'Product': (PRODUCT_COLS, 'Id', PRODUCTS, '[초안] 결제 — 에디터는 바로 지급, 폰은 출시 때까지 꺼짐 · Product_Id 는 Play Console 상품 ID'),
        'Item': (ITEM_COLS, 'Id', ITEMS, 'Kind: free_box · ad_box · gear_box · chip_box · stamina · gold(Value = 순찰 몇 시간치) · Per_Day 0 = 제한 없음'),
        'Box_Rate': (BOX_COLS, 'Grade', BOX_RATES, '상자 등급 확률(%) — 상점 [확률] 창에 그대로 보임. 합 100'),
    })


if __name__ == '__main__':
    main()
