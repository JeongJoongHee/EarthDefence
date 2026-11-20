# -*- coding: utf-8 -*-
"""11-20 코스튬 · 탈것 보고서 (코스튬_탈것_결과.html)
사용: PYTHONUTF8=1 python tools/report10.py <기본 OK 수> <7차> <9차> <코스튬 시험>
캡처: Earth_Defence/Logs/PlayTest/r10_*.png (PlayTest.RunR10) → 캡쳐본 모음/코스튬_탈것_캡처/
그림 모음: tools/_preview/look_sheet.png (tools/look_art.py sheet)
"""
import os, sys, shutil
from PIL import Image

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), '..'))
LOG = os.path.join(ROOT, 'Earth_Defence', 'Logs', 'PlayTest')
CAP = os.path.join(ROOT, '캡쳐본 모음')
OUT = os.path.join(CAP, '코스튬_탈것_캡처')
os.makedirs(OUT, exist_ok=True)
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from look_art import COSTUMES, MOUNTS


def small(src, dst, w=540):
    im = Image.open(src).convert('RGB')
    im.resize((w, round(im.height * w / im.width)), Image.LANCZOS).save(os.path.join(OUT, dst), quality=88)


for f in sorted(os.listdir(LOG)):
    if f.startswith('r10_') and f.endswith('.png'): small(os.path.join(LOG, f), f[:-4] + '.jpg')
# 전투 장면은 대장 카피 둘레만 크게 잘라 한 장 더 (화면 아래 가운데)
for f in sorted(os.listdir(LOG)):
    if f.startswith('r10_') and '_battle_' in f and f.endswith('.png'):
        im = Image.open(os.path.join(LOG, f)).convert('RGB')
        W, H = im.size
        im.crop((W // 2 - 300, int(H * .74), W // 2 + 300, int(H * .95))).resize((440, round(440 * (H * .21) / 600)), Image.LANCZOS).save(os.path.join(OUT, 'zoom_' + f[:-4] + '.jpg'), quality=90)
sheet = os.path.join(ROOT, 'tools', '_preview', 'look_sheet.png')
if os.path.exists(sheet): Image.open(sheet).convert('RGB').save(os.path.join(OUT, 'look_sheet.jpg'), quality=90)
for n, src in (('ref_costume.jpg', '코스튬.png'), ('ref_mount.jpg', '탈것.png')):
    p = os.path.join(ROOT, '요청 사항', src)
    if os.path.exists(p):
        im = Image.open(p).convert('RGB'); im.resize((1100, round(im.height * 1100 / im.width)), Image.LANCZOS).save(os.path.join(OUT, n), quality=85)

n_main, n_r7, n_r9, n_r10 = (sys.argv[1:5] + ['?'] * 4)[:4]
TPL = os.path.join(ROOT, '9차_수정안_결과.html')
if not os.path.exists(TPL): TPL = os.path.join(ROOT, '완료 사항 모음_HTML', '9차_수정안_결과.html')
src = open(TPL, encoding='utf-8').read()
head = src[:src.index('</head>')]
head = head[:head.index('<title>')] + '<title>코스튬 · 탈것</title>' + head[head.index('</title>') + 8:]
script = src[src.index('<script>'):]
D = '캡쳐본 모음/코스튬_탈것_캡처/'


def fig(s, cap):
    return f'<figure><img src="{D}{s}" alt=""><figcaption>{cap}</figcaption></figure>'


def todo(items):
    return ''.join(f'<li data-id="{i}"><label><input type="checkbox"><span><span class="tag">{t}</span>{n}<span class="why">{w}</span></span></label></li>' for i, t, n, w in items)


battles = sorted(f for f in os.listdir(OUT) if f.startswith('zoom_r10_'))
def bname(f):
    p = f[:-4].split('_battle_')[1].split('_')
    cn = dict((c[0], c[1]) for c in COSTUMES); mn = dict((m[0], m[1]) for m in MOUNTS)
    return f'{cn.get(p[0], p[0])} + {mn.get(p[1], p[1])}'
bshots = ''.join(fig(f, bname(f)) for f in battles)

clist = ' · '.join(c[1] for c in COSTUMES[1:])
mlist = ' · '.join(m[1] for m in MOUNTS[1:])

ITEMS = [
    ('l-look', '그림', '코스튬 18종 · 탈것 10종 그림', '참고 그림을 그대로 넣지 않고 게임 화풍(낙서풍 · 옆모습 · 총을 드는 자세)으로 새로 그렸습니다. 고칠 것은 이름과 함께 의견 칸에 (예: "닌자 복면 더 크게")'),
    ('l-props', '그림', '손에 드는 소품은 뺌 (지팡이 · 우산 · 거품기 · 돋보기 · 기타 · 삽 · 밧줄 · 칼)', '대장 카피는 두 손으로 총을 들고 돌려 겨눠서 소품을 같이 들 자리가 없음 → 옷 · 모자 · 꼬리 · 날개로 알아보게. 소품이 꼭 필요하면 등에 메는 식으로 넣을 수 있어요'),
    ('l-stat', '능력치', '능력치 칸 = 비워 둠 (미정)', 'Look.xlsx 의 Stat1/Val1 · Stat2/Val2 에 키 · 값만 넣으면 착용 중일 때 더해집니다 (장비와 같은 키: atk · atk_pct · hp · base_atk · base_hp · shield_pct · reduce · crit · crit_dmg · cd). 정해 주시면 넣어 드려요'),
    ('l-price', '얻는 법', '얻는 법 = 다이아로 사기 (코스튬 300 · 탈것 500 — 임시)', '기지 · 터렛 외형과 같은 방식. 사면 바로 착용. 상자 · 패키지 · 이벤트 보상으로 줄지, 등급(일반~전설)을 둘지 정해 주세요'),
    ('l-battle', '전투', '전투 화면: 탈것 높이만큼 대장 카피가 올라탐', '떠다니는 탈것(서프보드 · 양탄자 · 비행기 · 별똥별 · 해마 · 호버바이크 · 빗자루)은 위아래 둥실. 크기는 Config Mount_Battle_Scale (지금 1) 로 줄일 수 있어요'),
    ('l-gear', '장비', '장비 탭: 코스튬 + 탈것을 겹친 모습이 둥실', '탈것이 크면 칸 사이 빈 곳에 맞춰 조금 작아짐'),
]

body = f'''<body><main>
<h1>코스튬 · 탈것</h1>
<p class="note">2026-11-20 · 요청: 외형 메뉴에 코스튬 · 탈것 (착용하면 능력치 — 무엇인지는 미정) · 탈것은 전투 · 장비 화면에 · 코스튬 외형 + 탈것 외형 = 최종 모습 · 참고 <code>요청 사항/코스튬.png · 탈것.png</code> · 자동 시험 <b>코스튬 · 탈것 {n_r10}줄 + 기본 {n_main}줄 + 7차 {n_r7}줄 + 9차 {n_r9}줄</b></p>
<div class="bar"><span id="stat"></span><button id="copyNotes" type="button">의견 모아 복사</button></div>
<div class="tip blue"><b>먼저 알려 드릴 것 (제 해석)</b><ul>
<li><b>코스튬 = 대장 카피 몸 그림을 통째로 바꿈</b> (기본 = 지금 우주복 "우주 대원"). <b>탈것 = 발밑에 까는 그림</b> — 게임이 코스튬 몸 위에 겹쳐 그려서 어떤 코스튬이든 어떤 탈것에나 탈 수 있습니다 (19 x 11 = 209가지 조합). 비행기 · 해마 · 유니콘 · 호버바이크 · 티라노는 몸 앞에 한 층(동체 · 안장)을 더 그려 "올라탄" 느낌.</li>
<li>참고 그림 1장(코스튬)의 첫 번째 우주인은 지금 기본 모습이라 <b>코스튬 18종 + 기본</b>, 탈것 그림 10종 + <b>"탈것 없음"</b>. 탈것 그림 속 캐릭터 의상(터번 · 비행 모자 · 잎 옷 …)은 탈것에 넣지 않았습니다 — 의상은 코스튬이 맡아서.</li>
<li><b>능력치는 표에 칸만 만들고 비워 둠</b> → 화면엔 "능력치 준비 중". 정해지면 표에 값만 넣으면 바로 적용됩니다 (시험에서 공격력 % · 체력 · 치명을 넣어 더해지는 것까지 확인).</li>
<li><b>얻는 법은 요청에 없어</b> 기지 · 터렛 외형처럼 다이아로 사게 했습니다 (가격 임시, 사면 바로 착용). 외형 탭에서 목록을 누르면 사기 전에 <b>미리 보기</b> (위 칸에 지금 코스튬 x 그 탈것).</li>
<li>요청엔 전투 · 장비 화면이었지만 <b>로비에서 걸어 다니는 대장 카피 · 터렛 탭 대장 카피 카드 · 스토리 얼굴</b>도 같은 모습으로 바꿨습니다 (화면마다 모습이 다르면 어색해서). 로비 탈것은 0.8배.</li>
<li>세이브에 칸 4개(착용 코스튬 · 탈것 · 가진 것 목록)를 더했고 옛 세이브는 기본 모습으로 열립니다.</li>
</ul></div>

<h2>외형 탭: [코스튬] · [탈것]</h2>
<div class="shots">{fig("r10_01_looks_costume.jpg", "코스튬 칸 · 위 = 지금 모습 (시험용으로 능력치를 넣은 상태)")}{fig("r10_02_looks_preview.jpg", "목록을 누르면 미리 보기 (해적)")}{fig("r10_03_looks_mount.jpg", "탈것 칸 · 목록 그림 = 지금 코스튬을 태운 모습")}{fig("r10_04_looks_mount_nostat.jpg", "지금 표 (능력치 비어 있음 = 준비 중)")}</div>

<h2>장비 탭</h2>
<div class="shots">{fig("r10_06_gear_basic.jpg", "기본 (탈것 없음)")}{fig("r10_05_gear.jpg", "별빛 마법사 + 숲속 유니콘")}</div>

<h2>전투 화면 (대장 카피 둘레 확대)</h2>
<div class="shots">{bshots}</div>
<div class="shots">{fig("r10_10_battle_wizard_unicorn.jpg", "전체 화면: 마법사 + 유니콘")}{fig("r10_30_lobby.jpg", "로비: 공룡 잠옷 + 아기 티라노")}</div>

<h2>그림 모음 (참고 그림 → 게임 화풍)</h2>
<p>코스튬 18: {clist}<br>탈것 10: {mlist}</p>
<div class="shots">{fig("ref_costume.jpg", "참고: 코스튬.png")}{fig("ref_mount.jpg", "참고: 탈것.png")}</div>
<figure class="wide"><img src="{D}look_sheet.jpg" alt=""><figcaption>게임 그림 전부 + 조합 예시 (tools/look_art.py sheet)</figcaption></figure>

<h2>바뀐 곳 (다음 작업용 메모)</h2>
<table><tr><th>무엇</th><th>어디</th></tr>
<tr><td>표 (Look.xlsx 새로: Costume · Mount 시트 · Config 5 · 글자 9)</td><td><code>tools/fix10_tables.py</code> — 다시 돌려도 손으로 넣은 이름 · 가격 · 능력치는 그대로 (없는 줄만 더하고 Lift 만 새로)</td></tr>
<tr><td>그림 (코스튬 몸 · 둥실 · 총 · 카드 / 탈것 뒤 · 앞 층)</td><td><code>tools/look_art.py export</code> (PowerShell) → Resources/Art/Char/costume · Char/mount + <code>tools/look_pivots.json</code>(탈것 높이) → fix10_tables.py</td></tr>
<tr><td>전투 · 로비 겹치기</td><td><code>Game/LookRig.cs</code> MountRig (뒤 층 &lt; 몸 &lt; 앞 층 &lt; 총 · 몸을 젖혀도 탈것은 수평 · 둥실) · <code>TurretUnit.MakeMount</code> · <code>LobbyWalker</code></td></tr>
<tr><td>UI 겹치기 (장비 탭 · 외형 탭)</td><td><code>LookView.Build</code> (발 = 탈것 자리를 같은 점에 두고 상자에 맞춤)</td></tr>
<tr><td>외형 탭 · 능력치 · 사기</td><td><code>UI/ShopTab.cs Looks</code> (sub 2 · 3 · 미리 보기) · <code>Loadout.Build</code> (착용 중 능력치) · <code>Store.BuyLook / WearLook</code> · 세이브 costume · mount · costumes · mounts</td></tr>
<tr><td>시험</td><td><code>bash tools/unity_batch.sh EarthDef.EditorTools.PlayTest.RunR10 --play</code> (Editor/PlayTest10.cs)</td></tr>
</table>

<h2>확인 칸</h2>
<ul class="todo">''' + todo(ITEMS) + '''<li data-id="l-stats-plan"><label><input type="checkbox"><span><span class="tag">결정</span>능력치 정할 때 제안 (추천)<span class="why">코스튬 = 공격 쪽(공격력 % · 치명) · 탈것 = 버티기 쪽(기지 체력 % · 방어력)으로 나누고, 가진 것만으로도 조금(보유 효과) + 착용하면 더 — 터렛 외형의 "보유 효과"와 같은 방식. 원하시면 의견 칸에 적어 주세요</span></span></label></li>
<li data-id="l-play"><label><input type="checkbox"><span><span class="tag">폰</span>APK 로 직접 해 본 느낌<span class="why">탈것 크기 · 높이가 전투에서 거슬리지 않는지</span></span></label></li>
</ul>
'''
open(os.path.join(ROOT, '코스튬_탈것_결과.html'), 'w', encoding='utf-8').write(head + '<style>figure.wide img{max-width:100%}</style></head>' + body + '</main>\n' + script.replace('earthdef-req9-1120', 'earthdef-look-1120').replace('earthdef-req8-1119', 'earthdef-look-1120'))
print('ok', OUT, len(battles))
