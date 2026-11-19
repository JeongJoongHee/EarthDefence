# -*- coding: utf-8 -*-
"""8차 수정안 보고서 (8차_수정안_결과.html) 만들기 — 캡처 사본 · GIF · 전/뒤 그림.
사용: PYTHONUTF8=1 python tools/report8.py <before 폴더> <기본 OK 수> <시험장> <3차> <7차> <8차>
캡처: Earth_Defence/Logs/PlayTest/r8_*.png (PlayTest.RunR8) → 캡쳐본 모음/8차_수정안_캡처/
전 그림: 캡쳐본 모음/7차_수정안_캡처/ (7차 = 8차 전) + <before 폴더>(11-18 밤 기본 시험 캡처 · 요청 문서 그림)
"""
import os, sys, shutil
from PIL import Image

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), '..'))
LOG = os.path.join(ROOT, 'Earth_Defence', 'Logs', 'PlayTest')
CAP = os.path.join(ROOT, '캡쳐본 모음')
OUT = os.path.join(CAP, '8차_수정안_캡처')
os.makedirs(OUT, exist_ok=True)
BEFORE = sys.argv[1]


def small(src, dst, w=540):
    im = Image.open(src).convert('RGB')
    im.resize((w, round(im.height * w / im.width)), Image.LANCZOS).save(os.path.join(OUT, dst), quality=88)


def gif(prefix, dst, box=None, w=360, dur=120):
    fr = sorted(f for f in os.listdir(LOG) if f.startswith(prefix) and f.endswith('.png'))
    ims = []
    for f in fr:
        im = Image.open(os.path.join(LOG, f)).convert('RGB')
        if box: im = im.crop(box)
        ims.append(im.resize((w, round(im.height * w / im.width)), Image.LANCZOS))
    if ims: ims[0].save(os.path.join(OUT, dst), save_all=True, append_images=ims[1:], duration=dur, loop=0)
    return len(ims)


for n in ('r8_01_title', 'r8_02_title_star', 'r8_03_lobby', 'r8_04_gear', 'r8_05_looks_turret', 'r8_06_looks_base', 'r8_07_settings',
          'r8_08_quit', 'r8_09_battle', 'r8_10a_field_old', 'r8_10_field', 'r8_11_result_win', 'r8_12_result_lose'):
    if os.path.exists(os.path.join(LOG, n + '.png')): small(os.path.join(LOG, n + '.png'), n + '.jpg')
gif('r8_title_', 'title.gif', None, 360, 100)
gif('r8_gear_', 'gear.gif', (0, 120, 1080, 780), 420, 150)
gif('r8_field_', 'field.gif', (0, 300, 1080, 2340), 360, 120)
C7 = os.path.join(CAP, '7차_수정안_캡처')
for n in ('r7_01_title', 'r7_05_lobby', 'r7_07b_battle', 'lobby.gif', 'gear.gif'):
    shutil.copy(os.path.join(C7, n + ('' if n.endswith('.gif') else '.jpg')), os.path.join(OUT, 'before_' + n + ('' if n.endswith('.gif') else '.jpg')))
for n in ('08_result_win', '10_result_lose', '87_looks_turret', '88_looks_base', '82_settings'):
    small(os.path.join(BEFORE, n + '.png'), 'before_' + n + '.jpg')
small(os.path.join(BEFORE, 'user_result.png'), 'before_user_result.jpg', 440)

n_main, n_lab, n_p3, n_r7, n_r8 = (sys.argv[2:7] + ['?'] * 5)[:5]
src = open(os.path.join(ROOT, '7차_수정안_결과.html'), encoding='utf-8').read()
head = src[:src.index('</head>')].replace('<title>7차 수정안 반영</title>', '<title>8차 수정안 반영</title>')
script = src[src.index('<script>'):].replace("earthdef-req7-1118", "earthdef-req8-1119")
D = '캡쳐본 모음/8차_수정안_캡처/'


def fig(s, cap):
    return f'<figure><img src="{D}{s}" alt=""><figcaption>{cap}</figcaption></figure>'


def pair(b, a, bc, ac):
    return f'<div class="pair">{fig(b, "<span class=b>전</span> " + bc)}{fig(a, "<span class=a>뒤</span> " + ac)}</div>'


ITEMS = [
    ('r8-1', '1', '타이틀 [게임 시작]을 아래로 · 0.7배', '640x170 → 448x119 · 화면 아래에서 230px (Config Title_Btn_Bottom · Title_Btn_Scale)'),
    ('r8-2', '2', '타이틀 배경 별똥별 천천히', '빠르기 9~13 → 4.5~6.5 칸/초 (약 절반) · 느려진 만큼 보이는 시간 0.7~1.1 → 1.3~2초 (Config Shoot_Star_Speed_* · Shoot_Star_Life_*)'),
    ('r8-3', '3', '로비 [전투 시작] · 순찰 보상을 아래로', '아래 탭 바로 위(24px 띄움)에 순찰, 그 위에 [전투 시작]. 지구 위를 걷는 대장 카피는 단추 위로 보임 (Config Lobby_Btn_Bottom 0 이면 7차 자리)'),
    ('r8-4', '4', '로비 별똥별도 천천히', '타이틀과 같은 별똥별이라 같은 값으로 같이 느려짐'),
    ('r8-5', '5', '전투 달토끼를 오른쪽 위로', '달 + 달토끼를 왼쪽 위 → 오른쪽 위(위 UI 바로 아래)로. 운석보다 뒤에 그려서 운석이 달 앞을 지나감 (Config Moon_X 4.3 · Moon_Below_Hud)'),
    ('r8-6', '6', '전투 바닥 = 지구 · 예전 지구 그림 → 일반 행성', '바닥을 큰 지구의 둥근 윗면(바다 · 대륙 · 구름 · 대기 빛)으로 — 사령탑 자리는 평평하고 오른쪽으로 갈수록 둥글게 내려감. 뒤에 떠오르던 지구는 살구색 줄무늬 행성으로 (고리는 하늘에 고리 행성이 이미 있어 뺌). 낙뢰 아래 끝도 둥근 지구 면에 맞춤'),
    ('r8-7', '7', '끈끈이 장판을 세로로', '적이 가장 많은 세로줄(x)에 땅 ~ 위 UI 아래까지 세로 띠. 적은 오른쪽 위에서 비스듬히 오므로 띠를 가로질러 지나감. 필살기(젤리 드론 3줄) · 두 번째 장판 카드도 세로 (Config Field_Vertical 0 이면 예전 가로). 터렛 설명 글도 "세로 띠"로'),
    ('r8-8', '8', '전투 위 줄에 적 약점', '"스테이지 10  약점 중력" — 약점 이름은 그 속성 색. 시계와 안 겹침 (스테이지 모드만 — 무한 · 엘리트 군단은 정해진 약점이 없음)'),
    ('r8-9', '9', '성공 · 실패 창 보상 칸 경계', '골드 · 부품(· 다이아) 줄 둘레에 흰 테두리 판. 떨어진 장비 목록은 판 아래로'),
    ('r8-10', '10', '장비 탭 대장 카피: 제자리에서 둥실둥실', '좌우로 떠다니며 뒤집히던 것 → 그 자리에서 위아래로만 둥실(느린 물결 + 작은 물결) + 아주 살짝 기울기. 몸은 늘 같은 쪽'),
    ('r8-11', '11', '외형 설명 짧게', '터렛 외형 = 이름 · "필살기: 은하 관통" · "보유 효과: 피해 +10%" 세 줄(필살기 긴 설명 뺌). 기지 외형 = "골드 +10%" · "40초마다 별똥별 비 (400%)" 처럼 한 줄 (Frame.xlsx Desc)'),
    ('r8-12', '12', '종료 기능', '설정 창 맨 아래 [게임 종료] → "게임을 종료할까요?" [취소] / [게임 종료](저장하고 끔). 폰 뒤로 가기: 창이 있으면 닫기 → 전투 중이면 일시정지 → 로비 다른 탭이면 전투 탭 → 전투 탭 · 타이틀이면 종료 확인'),
]

body = f'''<body><main>
<h1>8차 수정안 반영</h1>
<p class="note">2026-11-19 · <code>요청 사항/8차 수정안.docx</code> (12줄 + 결과 창 그림) · 자동 시험 <b>기본 {n_main}줄 + 시험장 {n_lab}줄 + 3차 {n_p3}줄 + 7차 {n_r7}줄 + 8차 {n_r8}줄 통과 · 오류 0</b></p>
<div class="bar"><span id="stat"></span><button id="copyNotes" type="button">의견 모아 복사</button></div>
<div class="tip blue"><b>먼저 알려 드릴 것 (제 해석)</b><ul>
<li><b>달토끼 "어느 정도 운석을 가려도 괜찮음"</b> = 오른쪽 위는 운석이 나오는 자리라 겹쳐도 된다는 뜻으로 봤습니다. 달은 <b>운석보다 뒤</b>에 그려서 운석이 달 앞을 지나갑니다 (운석이 달 뒤로 숨으면 못 보고 맞을 수 있어서). 달이 앞에 오길 원하시면 알려 주세요.</li>
<li><b>"지구로 보이는 그림은 일반 행성"</b> = 7차에서 키운 오른쪽 아래 지구를 <b>살구색 줄무늬 행성</b>으로 바꿨습니다. 하늘 그림에 고리 행성이 이미 있어 고리는 뺐습니다.</li>
<li><b>"종료 기능"</b> = 설정 창의 [게임 종료] + <b>폰 뒤로 가기</b>로 넣었습니다 (지금까지 뒤로 가기를 눌러도 아무 일이 없었음). 결과 창 · 카드 고르기처럼 닫기(X)가 없는 창은 뒤로 가기로 안 닫힙니다.</li>
<li><b>끈끈이 세로 띠</b>는 가로 띠보다 길어(높이 ~16칸 ↔ 폭 7.5칸) 한 번에 맞는 적 수가 달라질 수 있습니다. 밸런스는 밸런스 3차 재측정 때 같이 봅니다.</li>
<li>시험: 기본 시험을 처음 돌렸을 때 <b>"스테이지 1 보상 · 다음 스테이지 열림" 1줄이 한 번 실패</b>했고, 다시 돌리니 통과했습니다(원인 못 찾음 — 다음에 또 나오면 원인을 볼 수 있게 값을 찍게 해 둠).</li>
<li>로비 단추를 아래로 내려 7차의 "[적 정보]를 펼치면 스테이지 칸과 겹침" 걱정은 없어졌습니다.</li>
</ul></div>

<h2>1 · 2 타이틀</h2>
{pair("before_r7_01_title.jpg", "r8_02_title_star.jpg", "단추가 가운데 · 큼", "단추 아래 · 0.7배")}
<div class="pair">{fig("before_lobby.gif", "<span class=b>전</span> 별똥별 빠름 (7차 로비, 0.12초 간격)")}{fig("title.gif", "<span class=a>뒤</span> 별똥별 천천히 (0.1초 간격)")}</div>

<h2>3 · 4 로비</h2>
{pair("before_r7_05_lobby.jpg", "r8_03_lobby.jpg", "[전투 시작] · 순찰이 대장 카피 머리 위", "아래 탭 바로 위")}

<h2>5 · 6 · 8 전투 화면</h2>
{pair("before_r7_07b_battle.jpg", "r8_09_battle.jpg", "달 바닥 · 오른쪽 아래 지구 · 달토끼 왼쪽 위", "지구 바닥 · 일반 행성 · 달토끼 오른쪽 위 · 스테이지 옆 약점")}

<h2>7 끈끈이 장판</h2>
{pair("r8_10a_field_old.jpg", "r8_10_field.jpg", "가로 띠 (Field_Vertical 0)", "세로 띠")}
<div class="shots">{fig("field.gif", "세로 띠 움직임 (0.12초 간격)")}</div>

<h2>9 결과 창</h2>
<div class="shots">{fig("before_user_result.jpg", "<span class=b>전</span> 요청 문서 그림")}{fig("r8_11_result_win.jpg", "<span class=a>뒤</span> 성공")}{fig("r8_12_result_lose.jpg", "<span class=a>뒤</span> 실패")}</div>

<h2>10 장비 탭</h2>
<div class="pair">{fig("before_gear.gif", "<span class=b>전</span> 떠다니며 좌우로 뒤집힘")}{fig("gear.gif", "<span class=a>뒤</span> 제자리에서 위아래 둥실 (0.15초 간격)")}</div>

<h2>11 외형 설명</h2>
{pair("before_87_looks_turret.jpg", "r8_05_looks_turret.jpg", "터렛 외형: 긴 필살기 설명 + 보유 효과 두 줄", "이름 · 필살기 · 보유 효과")}
{pair("before_88_looks_base.jpg", "r8_06_looks_base.jpg", "기지 외형: 긴 설명", "짧은 한 줄")}

<h2>12 종료</h2>
<div class="shots">{fig("before_82_settings.jpg", "<span class=b>전</span> 설정")}{fig("r8_07_settings.jpg", "<span class=a>뒤</span> 설정 [게임 종료]")}{fig("r8_08_quit.jpg", "<span class=a>뒤</span> 종료 확인")}</div>

<h2>바뀐 곳 (다음 작업용 메모)</h2>
<table><tr><th>무엇</th><th>어디</th></tr>
<tr><td>그림: 지구 바닥 + 일반 행성 (Bg/ground_earth)</td><td><code>tools/tower_art.py earth_ground · planet</code> → <code>earth_export.py tower</code></td></tr>
<tr><td>지구 윗면 높이 (낙뢰 · 장판 아래 끝) · 달 자리 · 별똥별 빠르기</td><td><code>World.FloorY</code> (EG_CX · EG_R = 그림 EG_*) · <code>PlaceMoon</code> · <code>StarStep</code></td></tr>
<tr><td>끈끈이 세로 띠 · 적 무리 x</td><td><code>Attacks.Field</code> · <code>Battle.CrowdX</code> · <code>TurretUnit</code> (field · drone 필살기)</td></tr>
<tr><td>타이틀 · 로비 단추 · 약점 · 결과 창 · 장비 탭 · 외형</td><td><code>Title.Build</code> · <code>Lobby.PlaceOverDome</code> · <code>BattleUI.Build/ShowResult</code> · <code>Float2D.InPlace</code> · <code>ShopTab.SkinRow/FrameRow</code></td></tr>
<tr><td>종료 · 뒤로 가기</td><td><code>Quit</code> · <code>BackKey</code> (UI/MissionTab.cs, Boot 에 붙음) · <code>Popup.CanClose</code></td></tr>
<tr><td>표 (Config 11 · 글자 6 · 기지 외형 설명 7 · 드론 설명)</td><td><code>tools/fix8_tables.py</code></td></tr>
<tr><td>시험</td><td><code>bash tools/unity_batch.sh EarthDef.EditorTools.PlayTest.RunR8 --play</code> (Editor/PlayTest8.cs)</td></tr>
</table>

<h2>확인 칸</h2>
<ul class="todo">''' + ''.join(f'<li data-id="{i}"><label><input type="checkbox"><span><span class="tag">{t}</span>{n}<span class="why">{w}</span></span></label></li>' for i, t, n, w in ITEMS) + '''<li data-id="r8-play"><label><input type="checkbox"><span><span class="tag">폰</span>APK 로 직접 해 본 느낌<span class="why">메뉴 지구 방위대 › APK 빌드 로 폰에서 뒤로 가기 · 종료 · 별똥별 빠르기를 보시고 의견 주세요</span></span></label></li>
</ul>
'''
open(os.path.join(ROOT, '8차_수정안_결과.html'), 'w', encoding='utf-8').write(head + '</head>' + body + '</main>\n' + script)
print('ok', OUT)
