# -*- coding: utf-8 -*-
"""7차 수정안 보고서 (7차_수정안_결과.html) 만들기 — 캡처 사본 · GIF · 전/뒤 그림.
사용: PYTHONUTF8=1 python tools/report7.py <기본 시험 OK 수> <시험장> <3차> <7차>
캡처: Earth_Defence/Logs/PlayTest/r7_*.png (PlayTest.RunR7) → 캡쳐본 모음/7차_수정안_캡처/
전 그림: 캡쳐본 모음/전투배치_적용/ (11-18 오후 · 7차 전) · 6차_수정안_캡처/90_gear_capy_a.png
"""
import os, sys, shutil
from PIL import Image

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), '..'))
LOG = os.path.join(ROOT, 'Earth_Defence', 'Logs', 'PlayTest')
CAP = os.path.join(ROOT, '캡쳐본 모음')
OUT = os.path.join(CAP, '7차_수정안_캡처')
os.makedirs(OUT, exist_ok=True)


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


for n in ('r7_01_title', 'r7_02_title_star', 'r7_03_enter', 'r7_04_guest_warn', 'r7_05_lobby', 'r7_06_gear', 'r7_07_battle', 'r7_07b_battle', 'r7_09_slot_pick'):
    small(os.path.join(LOG, n + '.png'), n + '.jpg')
gif('r7_lobby_', 'lobby.gif', None, 360, 120)
gif('r7_gear_', 'gear.gif', (0, 120, 1080, 780), 420, 250)
gif('r7_fade_', 'fade.gif', (0, 0, 1080, 1100), 420, 100)
B = os.path.join(CAP, '전투배치_적용')
for n in ('18c_lobby_walk', '04b_battle_full', '03b_slot_pick'):
    small(os.path.join(B, n + '.png'), 'before_' + n + '.jpg')
small(os.path.join(CAP, '6차_수정안_캡처', '90_gear_capy_a.png'), 'before_gear.jpg')

n_main, n_lab, n_p3, n_r7 = (sys.argv[1:5] + ['?'] * 4)[:4]
src6 = open(os.path.join(ROOT, '6차_수정안_결과.html'), encoding='utf-8').read()
head = src6[:src6.index('</head>')].replace('<title>6차 수정안 반영</title>', '<title>7차 수정안 반영</title>')
script = src6[src6.index('<script>'):].replace("earthdef-req6-1117", "earthdef-req7-1118")
D = '캡쳐본 모음/7차_수정안_캡처/'


def fig(src, cap, cls=''):
    return f'<figure><img src="{D}{src}" alt=""><figcaption>{cap}</figcaption></figure>'


def pair(b, a, bc, ac):
    return f'<div class="pair">{fig(b, "<span class=b>전</span> " + bc)}{fig(a, "<span class=a>뒤</span> " + ac)}</div>'


ITEMS = [
    ('r7-1', '1', '로비 배경에 별똥별 애니메이션', '하늘 위쪽에서 왼쪽 아래로 1.2~3.2초마다 하나씩 빠르게 긋고 지구 뒤로 사라짐 (Config Shoot_Star_Gap_Min · Max). 로비 · 타이틀에서만'),
    ('r7-2', '2', '로비: 사령탑 지우고 심플한 배경 + 큰 지구', '은하수 얼룩 · 달 · 고리 행성을 뺀 별 하늘 + 화면 아래를 덮는 큰 지구(반지름 7.8칸). 대장 카피가 지구 둥근 면을 따라 걸음 · [전투 시작] · 순찰은 가운데로'),
    ('r7-3', '3', '타이틀 → [게임 시작] → 구글 연동 / 게스트', '게스트를 고르면 "데이터가 저장되지 않아요" 확인 창 한 번 더. 연동돼 있으면 [게임 시작]만으로 바로 로비. 구글 연동은 아직 [초안](실제 로그인은 출시 단계)'),
    ('r7-4', '4', '장비 탭 카피바라: 2D 로 둥실둥실 · 총 없이 우주복', '총 대신 두 손 + 산소통 분사 빛을 그린 새 그림. 칸 사이 빈 곳을 천천히 떠다니며 가는 쪽을 봄 (6차의 3D 몸 돌림 · 빛 · 그늘 · 발밑 그림자는 뺌)'),
    ('r7-5', '5', '기지 외형은 게임(전투) 안에서만', '로비 · 타이틀엔 사령탑이 안 나옴. 외형을 끼우면 전투 사령탑만 바뀜 (외형 탭 목록 그림은 그대로)'),
    ('r7-6', '6', '전투: 달토끼가 다른 그림과 안 겹침', '하늘 그림에서 달을 빼 따로 — 사령탑 위 왼쪽 위(위 UI 바로 아래). 적 · 탄은 방어선 오른쪽으로만 다녀 이 자리는 비어 있음 (시험: 사령탑 · 터렛 · 대장 카피 · 방어선과 안 겹침)'),
    ('r7-7', '7', '전투: 떨어지는 운석은 Lv 게이지 밑부터 Fade in', '위 UI 높이를 Lv 게이지 아래 끝(300)으로 바로잡음 (예전 210 은 스테이지 판까지만이라 운석이 게이지에 겹쳐 나왔음). 운석 그림이 게이지 아래를 지나며 1.2칸 동안 서서히 나타남 (중간 보스 · 보스는 예전대로)'),
    ('r7-8', '8', '전투: 대장 카피 탄도가 위 터렛과 안 겹침', '대장 카피를 터렛 줄보다 오른쪽(-3.9 → 터렛 오른쪽 끝 -3.35 · 대장 카피 -2.9)으로 내민 발코니에 세움 → 위로 쏘는 탄이 터렛을 지나지 않음 (시험: 30초 동안 겨눈 120번 중 겹침 0)'),
    ('r7-9', '9', '전투: 타워 지지대 최대한 컴팩트', '몸통 폭 절반 · 터렛 칸을 몸통 바로 옆 짧은 선반으로 · 긴 버팀대 없앰 · 안테나 짧게 · 받침 날개 작게. 칸 높이는 그대로(밸런스 그대로)'),
    ('r7-10', '10', '전투: 아래 지구를 더 크게', '지평선 위 지구 반지름 160 → 330 · 더 위로 올라옴 (땅 그림 1080x460 → 1080x700)'),
    ('r7-11', '11', '자리 고르기 안내를 가운데 크게', '화면 가운데에 큰 판(터렛 그림 + 이름 + "놓을 자리를 골라 주세요") · 금빛 테두리 반짝 · 칸 쪽 화살표 톡톡. 칸 단추와 안 겹치게 오른쪽으로 조금 (Config Slot_Tip_X)'),
    ('r7-12', '12', '자리 고르기에서 [카드 다시 고르기] 뺌', '카드 창에 새로고침이 있으므로 — 새 터렛 카드를 고르면 꼭 자리를 골라 놓음'),
]

body = f'''<body><main>
<h1>7차 수정안 반영</h1>
<p class="note">2026-11-18 · <code>요청 사항/7차 수정안.docx</code> (13줄) · 자동 시험 <b>기본 {n_main}줄 + 시험장 {n_lab}줄 + 3차 {n_p3}줄 + 7차 {n_r7}줄 통과 · 오류 0</b></p>
<div class="bar"><span id="stat"></span><button id="copyNotes" type="button">의견 모아 복사</button></div>
<div class="tip blue"><b>먼저 알려 드릴 것 (제 해석)</b><ul>
<li><b>문서 앞 5줄 = 로비, "전투 입장에서" 아래 7줄 = 전투</b>로 읽었습니다. 그래서 별똥별 애니메이션 · 심플한 배경 · 큰 지구는 <b>로비(와 타이틀)</b>에, 달토끼 · 운석 Fade in · 탄도 · 지지대 · 큰 지구 · 자리 고르기는 <b>전투</b>에 넣었습니다.</li>
<li><b>"떨어지는 별똥별"(전투)</b> = 하늘에서 비스듬히 떨어지는 <b>적(운석)</b>으로 봤습니다. 로비 배경의 별똥별은 장식이라 전투엔 넣지 않았습니다 (적과 헷갈림).</li>
<li><b>게스트 "데이터 저장이 없다"</b> = 계정(클라우드)에 저장되지 않는다는 뜻으로 봤습니다. 방치형이라 이 폰 안에는 그대로 저장되고, 앱을 지우거나 폰을 바꾸면 사라진다고 안내합니다. 정말 아무것도 저장하지 않게 하려면 알려 주세요.</li>
<li><b>"기지 외형은 게임 안에서만"</b> = 로비엔 사령탑을 안 보이고 전투에서만 보이게 했습니다. 외형 탭의 목록 그림(작은 사령탑)은 고를 때 필요해서 그대로 두었습니다.</li>
<li><b>지지대 컴팩트</b>는 폭(가로)만 줄였습니다. 칸 높이까지 줄이면 적이 노리는 높이 · 터렛 사거리가 바뀌어 밸런스를 다시 재야 해서 그대로 뒀습니다 — 세로도 줄이길 원하시면 의견 주세요.</li>
<li>타이틀이 생겨 앱을 켤 때마다 [게임 시작]을 누릅니다. 연동돼 있으면 바로 로비, 아니면 입장 방법 창 (Config Title_On 0 이면 예전처럼 바로 로비).</li>
</ul></div>

<h2>1 · 3 타이틀 · 입장</h2>
<div class="shots">{fig("r7_02_title_star.jpg", "타이틀 (별똥별 · 지구 위 대장 카피)")}{fig("r7_03_enter.jpg", "[게임 시작] → 구글 / 게스트")}{fig("r7_04_guest_warn.jpg", "게스트 → 저장되지 않아요 확인")}</div>

<h2>1 · 2 · 5 로비</h2>
{pair("before_18c_lobby_walk.jpg", "r7_05_lobby.jpg", "사령탑 · 달 · 구름 얼룩", "심플한 하늘 + 큰 지구 · 단추 가운데 · 사령탑 없음")}
<div class="shots">{fig("lobby.gif", "움직임 (별똥별 · 지구 위를 걷는 대장 카피)")}</div>

<h2>4 장비 탭 카피바라</h2>
<div class="pair">{fig("before_gear.jpg", "<span class=b>전</span> 총 든 3D 느낌 (몸 돌림 · 빛 · 그늘)")}{fig("gear.gif", "<span class=a>뒤</span> 총 없이 우주복 · 2D 로 둥실둥실 (0.25초 간격)")}</div>

<h2>6~10 전투 배치</h2>
{pair("before_04b_battle_full.jpg", "r7_07b_battle.jpg", "넓은 사령탑 · 긴 버팀대 · 달이 하늘 가운데 · 작은 지구", "가는 사령탑 · 짧은 선반 · 대장 카피는 터렛 줄 오른쪽 · 달토끼 왼쪽 위 · 큰 지구")}
<div class="shots">{fig("fade.gif", "운석이 Lv 게이지 아래부터 서서히 (0.1초 간격, 위쪽만 잘라 봄)")}</div>

<h2>11 · 12 자리 고르기</h2>
{pair("before_03b_slot_pick.jpg", "r7_09_slot_pick.jpg", "위에 작은 안내 · [카드 다시 고르기]", "가운데 큰 안내 + 화살표 · 다시 고르기 없음")}

<h2>바뀐 곳 (다음 작업용 메모)</h2>
<table><tr><th>무엇</th><th>어디</th></tr>
<tr><td>그림: 가는 사령탑 · 큰 지구 · 로비 지구 · 별똥별 · 따로 뗀 달 · 하늘 심플판 · 떠다니는 대장 카피</td><td><code>tools/tower_art.py</code> · <code>earth_art.py</code> → <code>earth_export.py bg char tower</code></td></tr>
<tr><td>로비 모드 (사령탑 · 달 바닥 숨김 · 큰 지구 · 별똥별) · 달 자리 · Fade 높이</td><td><code>World.SetLobby</code> · <code>PlaceMoon</code> · <code>StarStep</code> · <code>FadeTop</code></td></tr>
<tr><td>타이틀 · 입장</td><td><code>UI/Title.cs</code> · <code>Game.ToTitle</code></td></tr>
<tr><td>장비 탭 떠다니기</td><td><code>GearTab.Float2D</code> (그림 Char/guardian_float)</td></tr>
<tr><td>표 (Config 14 · 글자 14)</td><td><code>tools/fix7_tables.py</code></td></tr>
<tr><td>시험</td><td><code>bash tools/unity_batch.sh EarthDef.EditorTools.PlayTest.RunR7 --play</code> (Editor/PlayTest7.cs)</td></tr>
</table>

<h2>확인 칸</h2>
<ul class="todo">''' + ''.join(f'<li data-id="{i}"><label><input type="checkbox"><span><span class="tag">{t}</span>{n}<span class="why">{w}</span></span></label></li>' for i, t, n, w in ITEMS) + '''<li data-id="r7-play"><label><input type="checkbox"><span><span class="tag">폰</span>APK 로 직접 해 본 느낌<span class="why">메뉴 지구 방위대 › APK 빌드 로 폰에서 타이틀 → 로비 → 전투 흐름을 보시고 의견 주세요</span></span></label></li>
</ul>
'''
open(os.path.join(ROOT, '7차_수정안_결과.html'), 'w', encoding='utf-8').write(head + '</head>' + body + '</main>\n' + script)
print('ok', OUT)
