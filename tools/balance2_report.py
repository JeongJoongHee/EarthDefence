# -*- coding: utf-8 -*-
"""밸런스 2차 보고서 (밸런스_2차.html) — Logs/Bal/bal_*.json 에서 만듦.
사용: PYTHONUTF8=1 python tools/balance2_report.py
"""
import csv, json, math, os, statistics as stt, sys
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from bal_fit import target_day

HERE = os.path.dirname(os.path.abspath(__file__)); ROOT = os.path.join(HERE, '..')
BAL = os.path.join(ROOT, 'Earth_Defence', 'Logs', 'Bal')
F2P = ['f6_1000', 'f6_5000']
DIA = [('dia_333', 333), ('dia_1000', 1000), ('dia_3000', 3000)]
EL = dict(phys='물리', light='빛', grav='중력', fire='불', bolt='번개')
ELC = dict(phys='#d9c19a', light='#8fd6ff', grav='#b9a6e0', fire='#f09a7e', bolt='#ffd966')


def load(lb): return json.load(open(os.path.join(BAL, f'bal_{lb}.json'), encoding='utf8'))
def reach(d, s):
    for x in d['days']:
        if x['best'] >= s: return x['day']
    return None


def band(d, lo, hi):
    first, hp, tries, seen, cleared = [], [], {}, set(), set()
    for f in d['fights']:
        if f.get('farm') or not lo <= f['stage'] <= hi: continue
        s = f['stage']
        if s in cleared: continue
        tries[s] = tries.get(s, 0) + 1
        if s not in seen: seen.add(s); first.append(f['win'])
        if f['win']: cleared.add(s); hp.append(f['hp'])
    if not first: return None
    return (sum(first) / len(first), stt.median(hp) if hp else 0, sum(tries[s] for s in cleared) / max(1, len(cleared)))


def avg(labels, lo, hi, step=None):
    xs = []
    for l in labels:
        d = load(l)
        if step is None: b = band(d, lo, hi)
        else:
            # 계단 첫 스테이지만 / 그 밖만
            sub = dict(d); sub['fights'] = [f for f in d['fights'] if ((f['stage'] >= 20 and f['stage'] % 10 == 0) == step)]
            b = band(sub, lo, hi)
        if b: xs.append(b)
    return tuple(sum(x[i] for x in xs) / len(xs) for i in range(3)) if xs else None


# ---------------------------------------------------------------- 그래프: 날짜 → 최고 스테이지 (목표 · 무과금 2개 평균 · 과금)
DAYS = 420
def curve(lb):
    d = load(lb); m = {x['day']: x['best'] for x in d['days']}; out = []; last = 0
    for dd in range(1, DAYS + 1):
        if dd in m: last = m[dd]
        out.append(last)
    return out
tgt = []
for dd in range(1, DAYS + 1):
    s = 1
    while s < 100 and target_day(s + 1) <= dd: s += 1
    tgt.append(s)
f2p = [sum(c) / len(F2P) for c in zip(*[curve(l) for l in F2P])]
series = [('목표 (100 = 360일)', '#2a78d6', tgt), ('무과금 (씨앗 2개 평균)', '#eb6834', f2p)]
dia_ok = [(lb, n) for lb, n in DIA if os.path.exists(os.path.join(BAL, f'bal_{lb}.json'))]
cols = ['#1baf7a', '#eda100', '#e87ba4']
for (lb, n), c in zip(dia_ok, cols): series.append((f'하루 다이아 {n:,}', c, curve(lb)))
W, H, L, R, T, B = 760, 330, 48, 170, 16, 34
def X(d): return L + (d - 1) / (DAYS - 1) * (W - L - R)
def Y(v): return T + (1 - v / 100) * (H - T - B)
svg = [f'<svg viewBox="0 0 {W} {H}" width="100%" role="img" aria-label="날짜별 최고 스테이지" id="curve">']
for v in (0, 25, 50, 75, 100):
    svg.append(f'<line x1="{L}" x2="{W - R}" y1="{Y(v):.1f}" y2="{Y(v):.1f}" stroke="{"#c3c2b7" if v == 0 else "#e1e0d9"}"/><text x="{L - 8}" y="{Y(v) + 4:.1f}" text-anchor="end" class="ax">{v}</text>')
for dd in (1, 30, 90, 180, 270, 360, 420):
    svg.append(f'<text x="{X(dd):.1f}" y="{H - 12}" text-anchor="middle" class="ax">{dd}일</text>')
ends = []; lines_js = []
for name, col, ys in series:
    lines_js.append({'name': name, 'col': col, 'ys': [round(y, 1) for y in ys]})
    dash = ' stroke-dasharray="6 5"' if name.startswith('목표') else ''
    svg.append(f'<polyline points="{" ".join(f"{X(i + 1):.1f},{Y(y):.1f}" for i, y in enumerate(ys))}" fill="none" stroke="{col}" stroke-width="2"{dash} stroke-linejoin="round"/>')
    ends.append([Y(ys[-1]) + 4, name.split(' (')[0]])
ends.sort()
for i in range(1, len(ends)):
    if ends[i][0] - ends[i - 1][0] < 16: ends[i][0] = ends[i - 1][0] + 16
for y, t in ends: svg.append(f'<text x="{W - R + 8}" y="{y:.1f}" class="lab">{t}</text>')
svg.append(f'<line id="cx" y1="{T}" y2="{H - B}" stroke="#7a6a5c" visibility="hidden"/><rect x="{L}" y="{T}" width="{W - L - R}" height="{H - T - B}" fill="transparent" id="hit"/></svg>')
chart = '\n'.join(svg)
legend = '<div class="legend">' + ''.join(f'<span><i style="background:{c}"></i>{n}</span>' for n, c, _ in series) + '</div>'

STG = (20, 30, 40, 50, 60, 70, 80, 90, 100)
reach_t = '<table><tr><th>스테이지</th>' + ''.join(f'<th>{s}</th>' for s in STG) + '</tr>'
reach_t += '<tr><th>목표</th>' + ''.join(f'<td>{target_day(s):.0f}일</td>' for s in STG) + '</tr>'
for l in F2P:
    reach_t += f'<tr><th>무과금 (씨앗 {l.split("_")[1]})</th>' + ''.join(f'<td>{reach(load(l), s) or "-"}{"일" if reach(load(l), s) else ""}</td>' for s in STG) + '</tr>'
for lb, n in dia_ok:
    reach_t += f'<tr><th>하루 다이아 {n:,}</th>' + ''.join(f'<td>{reach(load(lb), s) or "-"}{"일" if reach(load(lb), s) else ""}</td>' for s in STG) + '</tr>'
reach_t += '</table>'

BANDS = ((1, 9, '1~9 (튜토리얼 · 1장)'), (11, 19, '11~19'), (20, 49, '20~49'), (50, 79, '50~79'), (80, 100, '80~100'))
rows = []
for lo, hi, nm in BANDS:
    a, b = avg(F2P, lo, hi, False), (avg(F2P, lo, hi, True) if hi >= 20 else None)
    def c(x): return '<td colspan="3" class="mut">-</td>' if not x else f'<td>{x[0]:.0%}</td><td>{x[1]:.0%}</td><td>{x[2]:.1f}번</td>'
    rows.append(f'<tr><th>{nm}</th>{c(a)}{c(b)}</tr>')
feel = ('<table><tr><th rowspan="2">스테이지</th><th colspan="3">보통 스테이지</th><th colspan="3">계단 첫 스테이지 (20 · 30 · 40 …)</th></tr>'
        '<tr><th>처음 승률</th><th>남은 체력</th><th>도전</th><th>처음 승률</th><th>남은 체력</th><th>도전</th></tr>' + ''.join(rows) + '</table>')

pr = {}
for l in ['it4', 'f2p_a', 'f6_1000', 'f6_5000']:
    if not os.path.exists(os.path.join(BAL, f'bal_{l}.json')): continue
    for p in load(l)['probes']:
        q = pr.setdefault(p['stage'], [0, 0, 0, [], []]); q[0] += p['goodWin']; q[1] += p['randWin']; q[2] += p['n']
        if p['goodWin']: q[3].append(p['goodHp'])
        if p['randWin']: q[4].append(p['randHp'])
gw = sum(v[0] for v in pr.values()); rw = sum(v[1] for v in pr.values()); nn = sum(v[2] for v in pr.values())
probe = '<table><tr><th>스테이지</th>' + ''.join(f'<th>{s}</th>' for s in sorted(pr)) + '<th>합</th></tr>'
probe += '<tr><th>약점 노림</th>' + ''.join(f'<td>{pr[s][0]}/{pr[s][2]}</td>' for s in sorted(pr)) + f'<td><b>{gw}/{nn}</b></td></tr>'
probe += '<tr><th>아무거나</th>' + ''.join(f'<td>{pr[s][1]}/{pr[s][2]}</td>' for s in sorted(pr)) + f'<td><b>{rw}/{nn}</b></td></tr></table>'

# 스테이지 약점 표 (1~10 테마)
stages = list(csv.DictReader(open(os.path.join(ROOT, 'Earth_Defence', 'Assets', 'Resources', 'Tables', 'Stage.csv'), encoding='utf-8-sig')))
theme = '<table><tr><th>테마 (10 스테이지마다 되풀이)</th><th>스테이지 약점</th><th>나오는 적 (스테이지 1~10)</th></tr>'
for r in stages[:10]:
    w = r.get('Weak', '')
    theme += f'<tr><td>{r["Name"]}</td><td><span class="tag2" style="background:{ELC.get(w, "#ddd")}">{EL.get(w, w)}</span></td><td class="mut">{r["Enemies"]} · 몰려옴 {r["Swarm"]} · 중간 보스 {r["Elite"]} · 보스 {r["Boss"]}</td></tr>'
theme += '</table>'

# 과금
dia_t = '<table><tr><th>하루 다이아</th><th>한 달 (원, 다이아 산더미 기준)</th><th>100 스테이지</th><th>무과금 대비</th></tr>'
f2p_days = [reach(load(l), 100) or 420 for l in F2P]; fd = sum(f2p_days) / len(f2p_days)
dia_t += f'<tr><td>0 (무과금)</td><td>0원</td><td>{fd:.0f}일</td><td>1배</td></tr>'
pts = []
for lb, n in dia_ok:
    dd = reach(load(lb), 100) or 420
    pts.append((n, dd))
    dia_t += f'<tr><td>{n:,}</td><td>약 {n * 30 / 5000 * 59000 / 10000:,.0f}만 원</td><td>{dd}일{"+" if not reach(load(lb), 100) else ""}</td><td>{fd / dd:.1f}배 빠름</td></tr>'
dia_t += '</table>'

body = f'''<body><main>
<h1>밸런스 2차</h1>
<p class="note">2026-11-17 · 밸런스_1차.html 의견 반영 · 실제 게임 코드로 진행 흉내 (하루 18판 = 1시간 30분 · 순찰 16시간 · 광고 다 봄 · 지면 힘이 10% 오를 때까지 깬 스테이지에서 골드를 벌고 다시 도전) · 자동 시험 오류 0</p>
<div class="bar"><span id="stat"></span><button id="copyNotes" type="button">의견 모아 복사</button></div>
<div class="kpi"><div><b>{min(f2p_days)}~{max(f2p_days)}일{"+" if any(reach(load(l), 100) is None for l in F2P) else ""}</b><span>무과금 100 스테이지 (목표 360일 · 후반은 운에 따라 흔들림)</span></div>
<div><b>{gw}/{nn} : {rw}/{nn}</b><span>약점 노림 : 아무거나 승리 (1차는 75 : 89 / 168)</span></div>
<div><b>20 · 30 · 40 …</b><span>10 스테이지마다 툭 (계단 첫 스테이지 = 벽)</span></div>
<div><b>과금</b><span>대과금 100일은 아직 — 아래 과금 절</span></div></div>
<div class="tip blue"><b>의견 → 반영</b><ul>
<li><b>b-pace 무과금 360일 · b-day 1시간 30분</b> → 하루 18판 기준으로, 목표 날짜표(20 → 4일 · 30 → 16일 · 50 → 64일 · 70 → 151일 · 90 → 280일 · 100 → 360일)에서 <b>스테이지별 적 체력 배율을 거꾸로 계산</b>(tools/bal_fit.py, 5번 되풀이)해 Stage 표 Hp_Mul 에 넣었습니다.</li>
<li><b>b-ten "10 스테이지마다 툭툭 · 20부터"</b> → 20 · 30 · 40 … 에서 적이 한 계단 세지고(체력 = Hp_Mul 계단 · 공격력 1.2배), 계단 안은 완만하게(스테이지마다 1.02배). 계단 첫 스테이지가 그 장의 벽입니다.</li>
<li><b>b-theme 약점으로 구성 · 피해 배율 상승</b> → 스테이지마다 약점 1개(Stage 표 Weak) — <b>이 스테이지 적은 모두(보스 포함) 그 속성에 약함</b>, 적 구성도 그 약점 위주(60% 이상), 약점 배율 1.5 → 2배. 로비 [적 정보]에 "이 스테이지 적은 모두 불 공격에 약해요 (피해 2배)".</li>
<li><b>b-boss</b> → 보스 체력이 절반 아래로 처음 내려가면 화냄: 부하 6마리를 한꺼번에 부르고 4초 돌진 ("보스가 화났어요!").</li>
<li><b>b-tut</b> → 1차 그대로 (1~9 약하게).</li>
<li><b>과금 (b-later)</b> → 측정해 보니 지금 상점으로는 <b>대과금 100일이 안 나옵니다</b> (아래). 진행은 거의 골드 수입에 반비례하는데, 다이아 → 골드(큰 골드 주머니) 가치가 그만큼 크지 않음. 결정 항목 b-bm 에 방법을 올렸습니다.</li>
<li>같이 고친 것: 상점 골드 주머니가 1차의 "순찰 골드 스테이지 곱 성장"을 빠뜨려 후반에 아주 작았음 → 순찰과 같은 식. 장비 떨어지는 등급은 12 → 25 스테이지마다 +1 (무과금은 100 에서 전설, 신화부터는 합성 · 상자).</li>
</ul></div>

<h2>날짜별 최고 스테이지</h2>
<div class="chartbox">{legend}{chart}<div id="tip"></div></div>
{reach_t}

<h2>판의 느낌 (무과금, 씨앗 2개 평균)</h2>
<p class="note">처음 승률 = 그 스테이지 첫 도전에서 이긴 비율 · 남은 체력 = 이긴 판 기지 체력 중간값 · 도전 = 깰 때까지 (하루 첫 판은 늘 도전, 그 뒤엔 힘이 10% 오를 때까지 골드 벌기). 후반 도전 수가 많은 건 "날마다 한 번씩 도전"하기 때문.</p>
{feel}

<h2>공략 차이 (약점 노림 ↔ 아무거나)</h2>
<p class="note">그 스테이지를 처음 도전하는 순간의 세이브로 같은 씨앗 4판씩: 스테이지 약점 속성 터렛을 먼저 놓고 그 터렛에 카드 · 돌파를 몰아주는 사람 ↔ 아무거나 고르는 사람.</p>
{probe}

<h2>스테이지 약점</h2>
<div class="shots"><figure><img src="캡쳐본 모음/밸런스_2차/91_stage_weak.png" loading="lazy"><figcaption>로비 [적 정보] — 스테이지 약점 안내</figcaption></figure></div>
{theme}

<h2>과금 (하루 다이아 N 개를 쓰는 플레이어)</h2>
<p class="note">(과금 측정은 후반 0.9배 조정 바로 전 배율 — 무과금 377~420일 판 기준) 다이아는 조각 상자(터렛이 최대가 아닐 때) → 큰 골드 주머니(450 다이아 = 순찰 20시간치) → 장비 · 회로 10+1. 월 금액은 다이아 산더미(59,000원 = 5,000) 기준 (월 패키지 9,900원 = 10,000 다이아는 하루 333).</p>
{dia_t}

<h2>결정 · 제안</h2>
'''
items = [
    ('b2-pace', '확인', '무과금 진행 (100 = 약 360일)', '위 그래프 · 표. 계단 첫 스테이지에서 며칠씩 머무는 모양입니다. 더 고르게(계단 줄이기) 또는 더 길게/짧게 원하시면 적어 주세요 — 날짜표만 바꾸고 tools/bal_fit.py 로 다시 맞춥니다.'),
    ('b2-bm', '결정', '과금 단계 맞추는 방법 (소 300 · 중 200 · 대 100일)',
     '추천 = <b>과금 전용 "성장" 상품을 따로</b>: ① 성장 패스(월 · 레벨마다 골드 · 조각) ② 골드 부스터(30일 · 판 골드 +100%) ③ 큰 골드 주머니 가치 상향 + 하루 구매 제한. 지금 상점만으로는 다이아 → 골드가 정비례라 대과금 100일에 월 수백만 원이 필요(위 표). 과금 단계별 월 금액(예: 소 1만 · 중 10만 · 대 50만)을 정해 주시면 그 금액으로 300 · 200 · 100일이 되게 상품 값을 맞추겠습니다.'),
    ('b2-late', '제안', '후반 성장 거리 (터렛 레벨 45 · 장비 궁극 Lv 70 이 120일쯤 다 참)',
     '그 뒤 360일까지는 기지 레벨만 천천히 오름 → 후반이 "기다리기"만 됨. 추천 = 터렛 레벨 상한 45 → 60~80 (돌파 레벨 · 별 장식) 또는 장비 "초월" 같은 후반 성장 하나 (3차 시스템).'),
    ('b2-ads', '확인', '무과금 = 광고는 다 봄 (결과 2배 · 순찰 2배 · 광고 상자)', '광고를 안 보는 무과금이면 약 2배 느려집니다(골드가 절반). 기준을 "광고 안 봄"으로 바꾸려면 적어 주세요.'),
    ('b2-modes', '안내', '아직 안 맞춘 것', '엘리트 난이도 · 무한 모드 · 엘리트 군단, 장비 · 회로 · 카드 하나하나, 미션 · 출석 보상 크기 (지금은 진행에 비해 작음).'),
]
todo = '<ul class="todo">' + ''.join(f'<li data-id="{i}"><label><input type="checkbox"><span><span class="tag">{t}</span>{n}<span class="why">{w}</span></span></label></li>' for i, t, n, w in items) + '</ul>'

base = open(os.path.join(ROOT, '밸런스_1차.html'), encoding='utf8').read()
head = base[:base.index('<body><main>')].replace('<title>밸런스 1차</title>', '<title>밸런스 2차</title>').replace('</head>', '<style>.tag2{display:inline-block;border-radius:6px;padding:0 8px;font-weight:bold;color:#3b281c}</style></head>')
tail = base[base.index('</main>'):].replace('earthdef-bal1-1117', 'earthdef-bal2-1117')
import re
tail = re.sub(r'var L=\[.*?\],svg=', 'var L=' + json.dumps(lines_js, ensure_ascii=False) + ',svg=', tail, flags=re.S)
tail = re.sub(r'var W=\d+,l=\d+,r=\d+,D=\d+;', f'var W={W},l={L},r={R},D={DAYS};', tail)
open(os.path.join(ROOT, '밸런스_2차.html'), 'w', encoding='utf8').write(head + body + todo + '\n' + tail)
print('ok', f2p_days, gw, rw, nn, pts)
