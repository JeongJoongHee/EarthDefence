# -*- coding: utf-8 -*-
"""9차 수정안 보고서 (9차_수정안_결과.html) + 스토리 대사 검토 (스토리_대사_검토.html) 만들기.
사용: PYTHONUTF8=1 python tools/report9.py <기본 OK 수> <시험장> <3차> <7차> <8차> <9차>
캡처: Earth_Defence/Logs/PlayTest/r9_*.png (PlayTest.RunR9) → 캡쳐본 모음/9차_수정안_캡처/
전 그림: 캡쳐본 모음/8차_수정안_캡처/ (8차 = 9차 전) · 4차_수정안_캡처/03_levelup.png (11-18 전 돔 전투)
대사: Earth_Defence/Table/Story.xlsx (tools/fix9_tables.py)
"""
import os, sys, html
from PIL import Image
from openpyxl import load_workbook

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), '..'))
LOG = os.path.join(ROOT, 'Earth_Defence', 'Logs', 'PlayTest')
CAP = os.path.join(ROOT, '캡쳐본 모음')
OUT = os.path.join(CAP, '9차_수정안_캡처')
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


for f in sorted(os.listdir(LOG)):
    if f.startswith('r9_') and f[3:5].isdigit() and f.endswith('.png'): small(os.path.join(LOG, f), f[:-4] + '.jpg')
if os.path.exists(os.path.join(LOG, '88_looks_base.png')): small(os.path.join(LOG, '88_looks_base.png'), 'r9_looks_base.jpg')   # 3차 시험(RunP3)이 찍는 외형 탭
gif('r9_talk_', 'talk.gif', (0, 900, 1080, 1900), 420, 120)
gif('r9_battle_', 'battle.gif', None, 360, 120)
C8 = os.path.join(CAP, '8차_수정안_캡처')
for n in ('r8_09_battle', 'r8_03_lobby', 'r8_04_gear', 'r8_07_settings', 'r8_06_looks_base'):
    p = os.path.join(C8, n + '.jpg')
    if os.path.exists(p): Image.open(p).save(os.path.join(OUT, 'before_' + n + '.jpg'), quality=90)
old = os.path.join(CAP, '4차_수정안_캡처', '03_levelup.png')
if os.path.exists(old): small(old, 'before_dome_1117.jpg')

n_main, n_lab, n_p3, n_r7, n_r8, n_r9 = (sys.argv[1:7] + ['?'] * 6)[:6]
TPL = os.path.join(ROOT, '완료 사항 모음_HTML', '8차_수정안_결과.html')
src = open(TPL, encoding='utf-8').read()
head = src[:src.index('</head>')].replace('<title>8차 수정안 반영</title>', '<title>9차 수정안 반영</title>')
script = src[src.index('<script>'):]
D = '캡쳐본 모음/9차_수정안_캡처/'


def fig(s, cap):
    return f'<figure><img src="{D}{s}" alt=""><figcaption>{cap}</figcaption></figure>'


def pair(b, a, bc, ac):
    return f'<div class="pair">{fig(b, "<span class=b>전</span> " + bc)}{fig(a, "<span class=a>뒤</span> " + ac)}</div>'


def todo(items):
    return ''.join(f'<li data-id="{i}"><label><input type="checkbox"><span><span class="tag">{t}</span>{n}<span class="why">{w}</span></span></label></li>' for i, t, n, w in items)


ITEMS = [
    ('r9-1', '1', '설정에서 스토리 스킵', '설정 창에 "스토리 건너뛰기" 켬/끔 한 줄 (켬 = 처음 시작 · 스테이지 스토리 모두 안 뜸). 대화창 오른쪽 위 [건너뛰기] · 폰 뒤로 가기도 건너뛰기'),
    ('r9-2', '2', '처음 시작 스토리 (왜 대장 카피가 지구를 지키나)', '타이틀 [게임 시작] → 10줄 스토리 → 로비. 요청 그림대로 왼쪽 동그라미 얼굴 · 오른쪽 대화창 · [다음]. 글자는 한 자씩(누르면 한 번에) · 화면 아무 데나 눌러도 다음 · 한 번만'),
    ('r9-3', '3', '[전투 시작] → 스테이지 소개 스토리 → 전투', '위에 스테이지 카드(번호 · 이름 · 약점 · 보스 그림) + 달토끼 안내 · 약점 터렛 추천 · 대장 카피 각오. 마지막 단추 = "전투 시작!" (스태미나는 먼저 냄)'),
    ('r9-4', '4', '전투를 아래 → 위 방식으로 복구', '11-18 전처럼 아래 가운데 돔 + 터렛 4칸 땅 위 한 줄 + 적은 위에서 곧게 내려와 방어막에 닿음 · 터렛 머리는 위를 봄 · 끈끈이 장판 가로 띠 · 적 빠르기 예전 그대로. 바닥은 8차 지구를 가운데로 · 달토끼 오른쪽 위 그대로. Config Battle_Vertical 0 이면 사령탑으로 되돌림'),
    ('r9-5', '5', '로비 대장 카피 말풍선', '6~12초마다 머리 위 말풍선(3.4초) · 대장 카피를 누르면 바로 한마디 · 창이 떠 있으면 쉼 · "다음은 스테이지 13!" 처럼 진행에 맞춘 말. 대사 26개 = Story.xlsx Lobby_Talk 시트 (Weight = 뽑힐 비율)'),
    ('r9-6', '6', '장비 · 회로 도감', '장비 탭 [도감] → 장비(부위 6 x 등급 7 = 42칸, 못 얻은 칸은 흐리게 + 자물쇠, 누르면 능력치 Lv.1 → 최대 Lv) · 회로(68종, 얻어 본 최고 등급과 효과, 누르면 등급별 값 · 끼우는 곳). 합성 · 분해로 없어져도 도감엔 남음'),
    ('r9-7', '7', '회로 프리셋 3개', '장비 탭 [회로 프리셋] → 프리셋 1~3 마다 [저장](지금 끼운 회로 · 자리) · [불러오기](그대로 다시 끼움). 합성 · 분해로 없어진 회로는 빼고 끼우고 몇 개 빠졌는지 알림 (Config Chip_Presets 3)'),
    ('r9-8', '8', '스테이지별 스토리 (100개)', '장(10스테이지)마다 이야기 흐름 + 스테이지마다 다른 테마 안내 · 약점 터렛 추천 · 대장 카피 각오 — 대사는 <a href="스토리_대사_검토.html">스토리_대사_검토.html</a> 에서 한 편씩 보고 의견을 주세요'),
]

body = f'''<body><main>
<h1>9차 수정안 반영</h1>
<p class="note">2026-11-20 · <code>요청 사항/9차 수정안.docx</code> (9줄 + 대화창 그림) · 자동 시험 <b>기본 {n_main}줄 + 시험장 {n_lab}줄 + 3차 {n_p3}줄 + 7차 {n_r7}줄 + 8차 {n_r8}줄 + 9차 {n_r9}줄</b></p>
<div class="bar"><span id="stat"></span><button id="copyNotes" type="button">의견 모아 복사</button></div>
<div class="tip blue"><b>먼저 알려 드릴 것 (제 해석)</b><ul>
<li><b>"하단에서 상단으로 공격하는 방식으로 복구 (이전 버전)"</b> = 11-18 사령탑 전의 <b>돔 방식</b>으로 되돌렸습니다 (돔 외형 7종 · 이름도 "OO 돔"으로). 그 뒤에 요청하신 것 중 <b>8차 지구 바닥 · 달토끼 오른쪽 위 · 적 Fade in · 자리 고르기 · 약점 표시</b>는 그대로 두었습니다 (지구 바닥은 돔이 가운데 서게 지구 꼭대기를 가운데로 옮김). 사령탑 코드는 지우지 않고 <b>Config Battle_Vertical 0</b> 하나로 되돌릴 수 있게 남겼습니다.</li>
<li>대장 카피는 <b>가운데 고정</b>(11-18 결정) 그대로입니다. 예전처럼 끌어서 좌우로 옮기려면 Config Guardian_Fixed 0.</li>
<li>적 빠르기는 예전 길이로 돌아가 <b>예전 값(1배)</b> — 사령탑 때 길이가 짧아 0.72배로 줄였던 것. 밸런스 3차 측정은 대부분 돔 방식 때 잰 것이라 그 값에 다시 가까워집니다(재측정은 밸런스 차례에).</li>
<li><b>스테이지 스토리는 그 스테이지를 처음 시작할 때만</b> 나옵니다 (지면 다시 도전할 때마다 보면 번거로워서). 매번 보려면 Config Story_Stage_Repeat 1. 설정 "스토리 건너뛰기"를 켜면 둘 다 안 뜹니다.</li>
<li><b>스토리 세계관</b>(제가 정함): 카피바라들이 온천하며 사는 지구를 <b>젤리 여왕</b>이 온천을 차지하려고 운석 · 벌레 군단으로 침공 → 달에서 방아 찧던 <b>달토끼</b>가 발견해 알려 줌 → 대장 카피가 지구 방위대 결성. 장마다 사건(벌레 · 젤리 비 · 떠돌이 행성 · 강철 군단 · 모선 · 얼음 혜성 …) → 100 스테이지에서 여왕이 "우리 별이 추워서"라고 털어놓고 같이 온천에 가자는 귀여운 결말. 얼굴 그림은 있는 것(대장 카피 · 방아 찧는 달토끼 · 젤리 여왕 보스)을 썼습니다.</li>
<li><b>도감</b>은 이번 판부터 기록합니다. 지금 가진 장비 · 회로는 켤 때 자동으로 채우지만, 예전에 합성 · 분해해서 없어진 것은 알 수 없어 빠져 있습니다 (출시 전이라 실제 영향 없음).</li>
<li>대화창엔 요청에 없던 <b>[건너뛰기]</b>를 같이 넣었습니다 (설정까지 가지 않아도 넘길 수 있게).</li>
<li>폴더에 다른 작업에서 생긴 <code>히든_타워_기획서.html · .md</code> 가 있어 건드리지 않았습니다 (커밋에도 안 넣음).</li>
</ul></div>

<h2>4 전투: 아래(돔) → 위(적) 방식 복구</h2>
{pair("before_r8_09_battle.jpg", "r9_09_battle.jpg", "8차: 왼쪽 벽 사령탑 · 적은 오른쪽 위에서 비스듬히", "9차: 아래 가운데 돔 · 터렛 땅 위 한 줄 · 적은 위에서 곧게")}
<div class="pair">{fig("before_dome_1117.jpg", "<span class=b>참고</span> 11-17 예전 돔 (풀밭 바닥)")}{fig("r9_10_battle_more.jpg", "<span class=a>뒤</span> 터렛 4개 · 30초 뒤")}</div>
{pair("before_r8_06_looks_base.jpg", "r9_looks_base.jpg", "기지 외형 = 사령탑 7종", "기지 외형 = 돔 7종 (이름도 OO 돔)")}
<div class="shots">{fig("battle.gif", "9차 전투 움직임 (0.12초 간격)")}{fig("r9_11_battle_tower_switch.jpg", "Battle_Vertical 0 = 사령탑 (되돌리기 스위치 확인)")}</div>

<h2>2 처음 시작 스토리</h2>
<div class="shots">{fig("r9_01_prologue_narr.jpg", "해설 (얼굴 없이 가운데)")}{fig("r9_02_prologue_rabbit.jpg", "달토끼")}{fig("r9_03_prologue_queen.jpg", "젤리 여왕")}{fig("r9_04_prologue_capy.jpg", "대장 카피")}</div>

<h2>3 · 8 스테이지 스토리</h2>
<div class="shots">{fig("r9_07_stage_story.jpg", "[전투 시작] → 스테이지 카드 + 안내")}{fig("r9_08_stage_story_last.jpg", "마지막 줄 = [전투 시작!]")}</div>

<h2>1 설정 · 5 로비 말풍선</h2>
<div class="shots">{fig("before_r8_07_settings.jpg", "<span class=b>전</span> 설정")}{fig("r9_06_settings.jpg", "<span class=a>뒤</span> 스토리 건너뛰기")}{fig("r9_05_lobby_talk.jpg", "<span class=a>뒤</span> 로비 말풍선")}{fig("talk.gif", "말풍선 뿅 (0.12초 간격)")}</div>

<h2>6 도감 · 7 회로 프리셋</h2>
{pair("before_r8_04_gear.jpg", "r9_12_gear_tab.jpg", "장비 탭", "[도감] · [회로 프리셋] 단추")}
<div class="shots">{fig("r9_13_codex_gear.jpg", "장비 도감")}{fig("r9_14_codex_gear_info.jpg", "장비 정보")}{fig("r9_15_codex_chip.jpg", "회로 도감")}{fig("r9_16_codex_chip_info.jpg", "회로 등급별 값")}{fig("r9_17_presets.jpg", "회로 프리셋 3개")}</div>

<h2>바뀐 곳 (다음 작업용 메모)</h2>
<table><tr><th>무엇</th><th>어디</th></tr>
<tr><td>세로 전장 스위치 (돔 · 칸 · 적 나오는 곳 · 방어막 · 머리 · 장판 · 낙뢰)</td><td><code>World.V</code> (Config Battle_Vertical) · <code>World.ApplyLayout · SlotPos · GuardianPos · FieldX0 · FieldLo · BaseDir · HeadRest · FieldVert · StrikeBottom</code> · <code>Enemy.AtLine/Remain</code> · <code>Battle.SpawnPoint/BigSpawn/Swarm/AimTarget</code></td></tr>
<tr><td>세로 바닥 · 외형 목록 돔 아이콘</td><td><code>tools/tower_art.py earth_ground_v</code> → <code>earth_export.py groundv</code> (Bg/ground_earth_v · Icon/framed_&lt;id&gt;)</td></tr>
<tr><td>스토리 대화창 · 로비 말풍선</td><td><code>UI/Story.cs</code> (StoryView · LobbyTalk) · 그림 <code>earth_export.py story9</code> (UI/portrait · UI/bubble · Icon/book)</td></tr>
<tr><td>도감 · 프리셋</td><td><code>Core/Codex.cs</code> (Codex · ChipPreset) · <code>UI/CodexUI.cs</code> · 세이브 codex · chipPresets · storySkip</td></tr>
<tr><td>표 (Story.xlsx 새로 · Config 12 · 글자 31 · 외형 이름 · 드론 설명)</td><td><code>tools/fix9_tables.py</code> — 대사를 고치면 이 파일에 옮겨 둘 것 (다시 돌리면 Story 시트를 새로 씀)</td></tr>
<tr><td>시험</td><td><code>bash tools/unity_batch.sh EarthDef.EditorTools.PlayTest.RunR9 --play</code> (Editor/PlayTest9.cs) · 7차 · 8차 시험은 Battle_Vertical 0(사령탑)으로 돎</td></tr>
</table>

<h2>확인 칸</h2>
<ul class="todo">''' + todo(ITEMS) + '''<li data-id="r9-play"><label><input type="checkbox"><span><span class="tag">폰</span>APK 로 직접 해 본 느낌<span class="why">메뉴 지구 방위대 › APK 빌드 로 폰에서 처음 시작 스토리 · 전투 · 말풍선을 보시고 의견 주세요</span></span></label></li>
</ul>
'''
open(os.path.join(ROOT, '9차_수정안_결과.html'), 'w', encoding='utf-8').write(head + '</head>' + body + '</main>\n' + script.replace('earthdef-req8-1119', 'earthdef-req9-1120'))

# ------------------------------------------------------------------ 스토리 대사 검토 (편마다 체크 · 의견)
WHO = {'capy': '대장 카피', 'rabbit': '달토끼', 'queen': '젤리 여왕', 'planet': '떠돌이 행성', 'narr': '해설'}
COL = {'capy': '#f7a24a', 'rabbit': '#c9a8f0', 'queen': '#ff9fb8', 'planet': '#e8b9a2', 'narr': '#9aa3b8'}
wb = load_workbook(os.path.join(ROOT, 'Earth_Defence', 'Table', 'Story.xlsx'))
ws = wb['Story']
seq = {}
for r in ws.iter_rows(min_row=2, values_only=True):
    if not r[0] or not r[3]: continue
    seq.setdefault(r[0], []).append((r[2], r[3]))
import csv
stages = {int(x['Stage']): x for x in csv.DictReader(open(os.path.join(ROOT, 'Earth_Defence', 'Assets', 'Resources', 'Tables', 'Stage.csv'), encoding='utf-8-sig'))}
ENAME = {x['Id']: x['Name'] for x in csv.DictReader(open(os.path.join(ROOT, 'Earth_Defence', 'Assets', 'Resources', 'Tables', 'Enemy.csv'), encoding='utf-8-sig'))}
EL = {'phys': '물리', 'light': '빛', 'grav': '중력', 'fire': '불', 'bolt': '번개'}


def lines_html(ls):
    return ''.join(f'<div class="ln"><b style="background:{COL.get(w, "#ccc")}">{WHO.get(w, w)}</b><span>{html.escape(t)}</span></div>' for w, t in ls)


items = []
items.append(('st-pro', '처음', '처음 시작 스토리 (타이틀 → 로비)', lines_html(seq.get('prologue', []))))
for n in range(1, 101):
    st = stages.get(n, {})
    title = f'스테이지 {n} · {st.get("Name", "")} <small>약점 {EL.get(st.get("Weak", ""), "-")} · 보스 {ENAME.get(st.get("Boss", ""), st.get("Boss", ""))}</small>'
    items.append((f'st-{n}', f'{(n - 1) // 10 + 1}장', title, lines_html(seq.get(f's{n}', []))))
talk = wb['Lobby_Talk']
tl = [(r[1], r[2]) for r in talk.iter_rows(min_row=2, values_only=True) if r[1]]
items.append(('st-talk', '로비', f'로비 대장 카피 말풍선 {len(tl)}개', ''.join(f'<div class="ln"><b style="background:#f7a24a">x{w}</b><span>{html.escape(t)}</span></div>' for t, w in tl)))
lis = ''.join(f'<li data-id="{i}"><label><input type="checkbox"><span><span class="tag">{t}</span>{n}</span></label><div class="steps">{b}</div></li>' for i, t, n, b in items)
style = '''<style>.steps .ln{display:flex;gap:10px;align-items:flex-start;margin:6px 0 6px 34px}.steps .ln b{flex:0 0 auto;font-size:13px;padding:2px 8px;border-radius:10px;color:#3b2a20}
.steps .ln span{font-size:15px;line-height:1.5}small{color:#8a7a6a;font-weight:normal;margin-left:6px}</style>'''
body2 = f'''<body><main>
<h1>스토리 대사 검토</h1>
<p class="note">2026-11-20 · 9차 수정안 "처음 시작 스토리 · 스테이지별 스토리" — 대사 원본 <code>Earth_Defence/Table/Story.xlsx</code> (도구 <code>tools/fix9_tables.py</code>)</p>
<div class="bar"><span id="stat"></span><button id="copyNotes" type="button">의견 모아 복사</button></div>
<div class="tip blue"><b>보는 법</b><ul>
<li>마음에 들면 체크 · 바꾸고 싶은 줄은 의견 칸에 "몇째 줄 → 이렇게" 로 적어 주세요. 표(Story.xlsx)를 직접 고치셔도 됩니다.</li>
<li>스테이지 스토리 짜임: <b>장 첫 스테이지</b>(1 · 11 · 21 …) = 장 이야기 2줄 · <b>장 마지막</b>(10 · 20 …) = 보스 이야기 · 나머지 = 테마 안내 → 약점 · 추천 터렛(그 스테이지에서 열려 있는 것만) → 대장 카피 각오.</li>
<li>세계관: 젤리 여왕이 지구 온천을 노림 → 달토끼가 알려 줌 → 대장 카피가 지구 방위대 → 100 스테이지에서 화해(같이 온천).</li>
</ul></div>
<ul class="todo">{lis}</ul>
'''
open(os.path.join(ROOT, '스토리_대사_검토.html'), 'w', encoding='utf-8').write(head.replace('<title>9차 수정안 반영</title>', '<title>스토리 대사 검토</title>') + style + '</head>' + body2 + '</main>\n' + script.replace('earthdef-req8-1119', 'earthdef-story-1120'))
print('ok', OUT, len(items), '편')
