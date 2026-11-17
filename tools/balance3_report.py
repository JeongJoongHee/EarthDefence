# -*- coding: utf-8 -*-
"""밸런스 3차 보고서 (밸런스_3차.html) — Logs/Bal 의 측정 결과에서 만듦.
사용: PYTHONUTF8=1 python tools/balance3_report.py
"""
import csv, json, math, os, re, statistics as stt, sys
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from bal_fit import target_day

HERE = os.path.dirname(os.path.abspath(__file__)); ROOT = os.path.join(HERE, '..')
BAL = os.path.join(ROOT, 'Earth_Defence', 'Logs', 'Bal')
TB = os.path.join(ROOT, 'Earth_Defence', 'Assets', 'Resources', 'Tables')
F2P = [l for l in ('n9_1000', 'n9_5000') if os.path.exists(os.path.join(BAL, f'bal_{l}.json'))]
TIERS = [(l, n, w) for l, n, w in (('t_ads', '무과금 + 광고 봄', '0원'), ('t_s2', '소과금', ''), ('t_m2', '중과금', ''), ('t_l2', '대과금', ''))
         if os.path.exists(os.path.join(BAL, f'bal_{l}.json'))]
TIER_COST = json.load(open(os.path.join(BAL, 'tier_cost.json'), encoding='utf8')) if os.path.exists(os.path.join(BAL, 'tier_cost.json')) else {}
TIER_GOAL = {'t_s2': 300, 't_m2': 200, 't_l2': 100}
NAME = {r['Id']: r['Name'] for r in csv.DictReader(open(os.path.join(TB, 'Turret.csv'), encoding='utf-8-sig'))}


def load(lb): return json.load(open(os.path.join(BAL, f'bal_{lb}.json'), encoding='utf8'))
def reach(d, s):
    for x in d['days']:
        if x['best'] >= s: return x['day']
    return None
def fmt(v): return f'{v}일' if v else '<span class="mut">420일+</span>'


def band(d, lo, hi, step=None):
    first, hp, tries, seen, cleared = [], [], {}, set(), set()
    for f in d['fights']:
        if f.get('farm') or f.get('elite') or not lo <= f['stage'] <= hi: continue
        s = f['stage']
        if step is not None and ((s >= 20 and s % 10 == 0) != step): continue
        if s in cleared: continue
        tries[s] = tries.get(s, 0) + 1
        if s not in seen: seen.add(s); first.append(f['win'])
        if f['win']: cleared.add(s); hp.append(f['hp'])
    if not first: return None
    return (sum(first) / len(first), stt.median(hp) if hp else 0, sum(tries[s] for s in cleared) / max(1, len(cleared)))


def avg(labels, lo, hi, step=None):
    xs = [b for b in (band(load(l), lo, hi, step) for l in labels) if b]
    return tuple(sum(x[i] for x in xs) / len(xs) for i in range(3)) if xs else None


def stays(d):
    """한 스테이지에 가장 오래 머문 날 수 (상위 3)"""
    st = {}
    for x in d['days']:
        if x['best'] < 100: st[x['best']] = st.get(x['best'], 0) + 1     # 100 을 깬 뒤의 날은 빼고
    return sorted(st.items(), key=lambda kv: -kv[1])[:3]


# ---------------------------------------------------------------- 그래프
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
series = [('목표 (100 = 360일)', '#2a78d6', tgt)]
if F2P: series.append(('무과금 · 광고 안 봄', '#eb6834', [sum(c) / len(F2P) for c in zip(*[curve(l) for l in F2P])]))
for (lb, n, _), c in zip(TIERS, ('#1baf7a', '#eda100', '#e87ba4', '#6250d6')): series.append((n, c, curve(lb)))
W, H, L, R, T, B = 760, 330, 48, 170, 16, 34
def X(d): return L + (d - 1) / (DAYS - 1) * (W - L - R)
def Y(v): return T + (1 - v / 100) * (H - T - B)
svg = [f'<svg viewBox="0 0 {W} {H}" width="100%" role="img" aria-label="날짜별 최고 스테이지" id="curve">']
for v in (0, 25, 50, 75, 100):
    svg.append(f'<line x1="{L}" x2="{W - R}" y1="{Y(v):.1f}" y2="{Y(v):.1f}" stroke="{"#c3c2b7" if v == 0 else "#e1e0d9"}"/><text x="{L - 8}" y="{Y(v) + 4:.1f}" text-anchor="end" class="ax">{v}</text>')
for dd in (1, 60, 120, 180, 240, 300, 360, 420):
    svg.append(f'<text x="{X(dd):.1f}" y="{H - 12}" text-anchor="middle" class="ax">{dd}일</text>')
ends = []; lines_js = []
for name, col, ys in series:
    lines_js.append({'name': name, 'col': col, 'ys': [round(y, 1) for y in ys]})
    dash = ' stroke-dasharray="6 5"' if name.startswith('목표') else ''
    svg.append(f'<polyline points="{" ".join(f"{X(i + 1):.1f},{Y(y):.1f}" for i, y in enumerate(ys))}" fill="none" stroke="{col}" stroke-width="2"{dash} stroke-linejoin="round"/>')
    ends.append([Y(ys[-1]) + 4, name.split(' (')[0].split(' · ')[0]])
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
for l in F2P: reach_t += f'<tr><th>무과금 (씨앗 {l.split("_")[1]})</th>' + ''.join(f'<td>{fmt(reach(load(l), s))}</td>' for s in STG) + '</tr>'
reach_t += '</table>'

BANDS = ((1, 9, '1~9'), (11, 19, '11~19'), (20, 49, '20~49'), (50, 79, '50~79'), (80, 100, '80~100'))
rows = []
for lo, hi, nm in BANDS:
    a, b = avg(F2P, lo, hi, False), (avg(F2P, lo, hi, True) if hi >= 20 else None)
    def c(x): return '<td colspan="3" class="mut">-</td>' if not x else f'<td>{x[0]:.0%}</td><td>{x[1]:.0%}</td><td>{x[2]:.1f}번</td>'
    rows.append(f'<tr><th>{nm}</th>{c(a)}{c(b)}</tr>')
feel = ('<table><tr><th rowspan="2">스테이지</th><th colspan="3">보통 스테이지</th><th colspan="3">장 첫 스테이지 (20 · 30 · 40 …)</th></tr>'
        '<tr><th>처음 승률</th><th>남은 체력</th><th>도전</th><th>처음 승률</th><th>남은 체력</th><th>도전</th></tr>' + ''.join(rows) + '</table>')
stay_txt = ' · '.join(f'씨앗 {l.split("_")[1]}: ' + ', '.join(f'{s}에서 {n}일' for s, n in stays(load(l))) for l in F2P)
old_stay = ', '.join(f'{s}에서 {n}일' for s, n in stays(load('f6_1000'))) if os.path.exists(os.path.join(BAL, 'bal_f6_1000.json')) else ''

# ---------------------------------------------------------------- 터렛
def feat(lb):
    p = os.path.join(BAL, f'feature_{lb}.json')
    return {r['turret']: r['k50'] for r in json.load(open(p, encoding='utf8'))} if os.path.exists(p) else {}
fb, fa = feat('f1'), feat('f4')
tur = '<table><tr><th>터렛 (주력으로 키움)</th><th>전</th><th>뒤</th></tr>'
for t in fb:
    tur += f'<tr><td>{NAME.get(t, t)}</td><td>{fb[t]:.1f}</td><td><b>{fa.get(t, 0):.1f}</b></td></tr>'
if fb and fa:
    vb = [v for k, v in fb.items() if k != 'guardian']; va = [v for k, v in fa.items() if k != 'guardian']
    tur += f'<tr><th>가장 센 ÷ 가장 약한</th><th>{max(vb) / min(vb):.1f}배</th><th>{max(va) / min(va):.1f}배</th></tr>'
tur += '</table>'

# ---------------------------------------------------------------- 도전 모드 · 엘리트
mode_t = ''
if F2P:
    d = load(F2P[0]); ms = {}
    for r in d.get('modes', []): ms.setdefault(r['mode'], []).append(r)
    en = ms.get('endless', []); sv = ms.get('survival', [])
    et = sorted(r['time'] for r in en);
    last = d['days'][-1]
    el = [(x['day'], x['best'], x['eliteBest']) for x in d['days'] if x['day'] in (30, 90, 180, 270, 360)]
    mode_t = (f'<table><tr><th>모드</th><th>측정</th></tr>'
              f'<tr><td>무한 모드</td><td>{len(en)}판 · 버틴 시간 중간 {et[len(et) // 2] / 60 if et else 0:.1f}분 (짧게 {et[len(et) // 4] / 60 if et else 0:.1f} ~ 길게 {et[len(et) * 3 // 4] / 60 if et else 0:.1f}분)</td></tr>'
              f'<tr><td>엘리트 군단</td><td>{len(sv)}판 · 막아냄 {sum(r["win"] for r in sv)}판 ({sum(r["win"] for r in sv) / max(1, len(sv)):.0%})</td></tr>'
              f'<tr><td>엘리트 난이도</td><td>' + ' · '.join(f'{dd}일 보통 {b} · 엘리트 {e}' for dd, b, e in el) + '</td></tr></table>')

# ---------------------------------------------------------------- 장비 · 회로 (식으로)
C = {r['Key']: float(r['Value']) for r in csv.DictReader(open(os.path.join(TB, 'Config.csv'), encoding='utf-8-sig')) if r['Value'] not in ('', None)}
G = list(csv.DictReader(open(os.path.join(TB, 'Gear_Part.csv'), encoding='utf-8-sig')))
GR = {int(r['Grade']): float(r['Mul']) for r in csv.DictReader(open(os.path.join(TB, 'Gear_Grade.csv'), encoding='utf-8-sig'))}
def gear_atk(g, lv, grow):
    s = 0
    for r in G:
        for k in '12':
            if r['Stat' + k] == 'atk': s += (float(r['Base' + k]) + float(r['Per_Lv' + k]) * (lv - 1)) * GR[g] * grow ** (lv - 1)
    return s
def base_atk(lv): return C['Atk_Base'] * C['Base_Atk_Growth'] ** (lv - 1) + C['Base_Atk_Per_Lv'] * (lv - 1)
gear_t = '<table><tr><th>시점 (기지 Lv · 장비)</th><th>전: 장비 공격력 ÷ 기지</th><th>뒤</th></tr>'
for blv, g, lv in ((50, 2, 5), (90, 4, 10), (127, 6, 30), (150, 7, 70), (185, 7, 70)):
    gear_t += f'<tr><td>기지 Lv {blv} · {g}등급 Lv {lv}</td><td>{gear_atk(g, lv, 1) / base_atk(blv):.0%}</td><td><b>{gear_atk(g, lv, C.get("Gear_Lv_Growth", 1)) / base_atk(blv):.0%}</b></td></tr>'
gear_t += '</table>'

# ---------------------------------------------------------------- 과금
tier_t = '<table><tr><th>단계</th><th>사는 것 (한 달)</th><th>한 달</th><th>100 스테이지</th><th>목표</th></tr>'
f2p_days = [reach(load(l), 100) for l in F2P]
tier_t += f'<tr><td>무과금 · 광고 안 봄</td><td>-</td><td>0원</td><td>{" / ".join(fmt(x) for x in f2p_days)}</td><td>360일</td></tr>'
for lb, n, _ in TIERS:
    c = TIER_COST.get(lb, {})
    tier_t += f'<tr><td>{n}</td><td>{c.get("buy", "-")}</td><td>{c.get("won", "-")}</td><td><b>{fmt(reach(load(lb), 100))}</b></td><td>{str(TIER_GOAL[lb]) + "일" if lb in TIER_GOAL else "-"}</td></tr>'
tier_t += '</table>'
OLD = [(l, n) for l, n in (('t_s', '광고 제거 + 월 패키지'), ('t_m', '+ 골드 부스터 + 궤짝 보름마다'), ('t_l', '+ 골드 부스터 + 산더미 엿새마다')) if os.path.exists(os.path.join(BAL, f'bal_{l}.json'))]
tier_old = ' · '.join(f'{n} {fmt(reach(load(l), 100))}' for l, n in OLD)

# ---------------------------------------------------------------- 본문
review = [
    ('2차 진행 기준', '무과금 = "광고는 다 봄"으로 맞췄음', '의견대로 <b>광고 안 봄</b>으로 바꾸고 스테이지 배율을 처음부터 다시 맞춤'),
    ('장 첫 스테이지 (계단)', '계단 체력을 장 첫 스테이지에 몰아 70 에서 43일 · 93 에서 34일을 머묾', '장 오름의 절반만 첫 스테이지에서 "툭", 나머지는 장 안에서 고르게 (tools/bal_fit.py STEP 0.5)'),
    ('테마별 어려움', '같은 장 안에서 거미 언덕(3) · 하늘의 접시(8)가 유난히 오래 걸림', '테마마다 어려움을 재서 배율을 나눔 (보정 0.8 ~ 1.24)'),
    ('맞추기 도구', '1~8 스테이지 흐름을 후반 자료로 계산해 20 · 30대가 너무 세게 나올 수 있었음 (이번에 찾음)', '앞쪽은 앞쪽 자료로 · "내려가지 않게" 규칙은 적의 실제 세기 기준'),
    ('터렛 세기', '하나를 주력으로 키웠을 때 깨는 적 세기가 5배 차이 (대포 2.1 ↔ 집중 빔 11.3)', '피해율 · 재장전을 고쳐 1.3배 안으로 (tools/balance3_turrets.py)'),
    ('효과 없는 카드', '불바퀴 "지속 연장"은 효과 0 · "정밀 조준"은 +4%', '지속 시간 +30% · 치명 배율 1.25 → 1.5 · 치명 +20%'),
    ('장비', '기지 공격력은 곱으로 크는데 장비는 더하기라 후반 비중 8%', '장비 공격력 · 체력도 레벨마다 1.02배 곱'),
    ('회로', '"공격력 +70" · "기지 체력 +1400" · "보호막 2800" 이 고정 값이라 후반엔 의미 없음', '기지 값의 % 로 (공격력 +10% · 체력 +14% · 보호막 = 체력의 28%)'),
    ('받는 피해 (방어력)', '고정 값을 빼서 후반엔 1 이 안 됨', '방어력 → 비율 감소 (240 ÷ (240 + 방어력))'),
    ('엘리트 난이도', '체력 2.6배 고정 — 계단 표가 생기면서 앞쪽은 쉽고 뒤쪽은 못 깸', '엘리트 n = 보통 n+10 의 적 (Elite_Mode_Offset)'),
    ('무한 모드 · 엘리트 군단', '골드 식에 곱 성장이 빠져 후반에 아주 작음 · 엘리트 군단은 7/51 승', '승리 골드와 같은 식 · 시작 세기와 군단 체력 조정'),
    ('측정 도구', '공략 비교(세이브를 잠깐 저장 · 되돌림)를 많이 한 측정만 후반이 빨라짐 (100 스테이지 180일 ↔ 300일)', '최종 값은 과금 단계와 같은 방식(공략 비교 1번)으로 잼 · 원인은 다음에 찾음'),
    ('미션 · 출석 보상', '골드 1500 · 조각 10 같은 고정 값이라 중반부터 의미 없음', '진행에 맞춰 커짐 (골드 = 순찰 시간당 골드 비율 · 조각 · 부품 = 25 스테이지마다 +1배)'),
]
rev_t = '<table><tr><th>무엇</th><th>찾은 문제</th><th>고친 것</th></tr>' + ''.join(f'<tr><td>{a}</td><td>{b}</td><td>{c}</td></tr>' for a, b, c in review) + '</table>'

items = [
    ('b3-pace', '확인', '무과금(광고 안 봄) 진행', '위 그래프 · 표. 후반(70 이후)은 힘이 거의 안 올라 측정마다 수십 일씩 흔들립니다 — 후반 성장(새 장비 · 동료)이 들어오면 다시 맞춥니다.'),
    ('b3-tier', '결정', '과금 단계 정의 (사는 것 · 한 달 금액)', '위 과금 표의 단계 정의가 생각하신 것과 맞는지 — 다르면 단계마다 "무엇을 얼마나 사는지" 적어 주세요. 그 기준으로 다시 맞춥니다.'),
    ('b3-noads', '확인', '광고 제거 = 광고 보상(순찰 · 결과 2배)을 광고 없이 받음', '광고를 안 보는 무과금 대비 골드가 약 2배라, 광고 제거 하나만 사도 많이 빨라집니다. 그대로 둘지, 광고 제거는 "광고만 없앰"으로 할지.'),
    ('b3-turret', '확인', '터렛 세기 조정 (피해율 · 재장전)', '시험장(지구 방위대 › 터렛 시험장 열기)에서 손맛을 봐 주세요. 대포 · 관통포 · 미사일은 재장전이 짧아지고(2.6 ~ 4.4초), 집중 빔 · 찌릿 코일 · 벌집은 약해졌습니다.'),
    ('b3-later', '안내', '다음 (출시 뒤 업데이트)', '후반 성장(새 장비 · 동료) · 영어 · 출시 준비.'),
]
todo = '<ul class="todo">' + ''.join(f'<li data-id="{i}"><label><input type="checkbox"><span><span class="tag">{t}</span>{n}<span class="why">{w}</span></span></label></li>' for i, t, n, w in items) + '</ul>'

kpi_days = [x for x in f2p_days if x]
_gw = _rw = _n = 0
for l in ('n6_1000', 'n6_5000'):      # 공략 비교를 많이 한 측정 (같은 세이브 · 같은 씨앗으로 약점 노림 ↔ 아무거나)
    if not os.path.exists(os.path.join(BAL, f'bal_{l}.json')): continue
    for p in load(l)['probes']: _gw += p['goodWin']; _rw += p['randWin']; _n += p['n']
pg, pr = f'{_gw}/{_n}', f'{_rw}/{_n}'
body = f'''<body><main>
<h1>밸런스 3차</h1>
<p class="note">2026-11-18 · 밸런스_2차.html 의견 반영 + 지금까지 밸런스 검토 · 측정 = 실제 게임 코드 (하루 18판 · 순찰 16시간 · 도전 모드 · 엘리트 난이도 포함) · 자동 시험 오류 0</p>
<div class="bar"><span id="stat"></span><button id="copyNotes" type="button">의견 모아 복사</button></div>
<div class="tip blue"><b>의견 → 반영</b><ul>
<li><b>무과금 = 광고 안 봄</b> → 이 기준으로 스테이지 배율을 다시 맞춤 (아래 그래프).</li>
<li><b>성장 패스 제외 · 골드 부스터 30일 추가 · 골드 주머니 OK</b> → 다이아 패널에 <b>골드 부스터</b> [초안] (9,900원 · 30일 · 판 골드 2배, 다시 사면 이어 붙음), 재화 패널에 <b>골드 금고</b> (다이아 1,500 · 순찰 100시간치 · 하루 3번).</li>
<li><b>진행 속도 "제안대로"</b> → 360일 목표 그대로.</li>
<li><b>후반 성장 거리 "나중에"</b> → 이번엔 손대지 않음.</li>
<li><b>나머지 밸런스 "지금"</b> → 엘리트 난이도 · 무한 모드 · 엘리트 군단 · 장비 · 회로 · 카드 · 터렛 · 미션 · 출석 (아래).</li>
</ul></div>

<h2>지금까지 밸런스 검토</h2>
<p class="note">1차 · 2차를 다시 재 보며 찾은 문제와 이번에 고친 것.</p>
{rev_t}

<h2>무과금 진행 (광고 안 봄)</h2>
<div class="chartbox">{legend}{chart}<div id="tip"></div></div>
{reach_t}
<p class="note">가장 오래 머문 스테이지 — {stay_txt}{(" (2차: " + old_stay + ")") if old_stay else ""}</p>

<h2>판의 느낌</h2>
{feel}
<p class="note">장 첫 스테이지(계단)는 후반일수록 오래 걸립니다 — 후반은 힘이 기지 레벨로만 천천히 올라서. 공략 차이(그 스테이지 첫 도전 세이브로 같은 씨앗 4판씩): <b>약점 노림 {pg} · 아무거나 {pr}</b>.</p>

<h2>터렛 세기</h2>
<p class="note">그 터렛 하나를 주력으로 키운 판(같은 세이브, 그 터렛 카드 → 대장 카피 카드만)이 승률 50% 로 깨는 적 체력 배율 (클수록 셈, 대장 카피만 키우면 3.4). 측정 = <code>bash tools/bal_turret.sh &lt;이름&gt; "" feature</code>.</p>
{tur}

<h2>장비 · 회로</h2>
{gear_t}

<h2>도전 모드 · 엘리트 난이도</h2>
{mode_t}

<h2>과금 단계</h2>
<p class="note">다이아는 조각 상자(터렛이 최대가 아닐 때) → 골드 금고 · 큰 골드 주머니 → 장비 · 회로 10+1. 광고 제거를 사면 광고 보상(순찰 · 결과 2배 · 광고 상자)을 광고 없이 받음.</p>
{tier_t}
<div class="tip gray"><b>먼저 잰 정의</b> (소과금에 광고 제거를 넣었을 때): {tier_old} → 광고 제거 하나로 "광고 보는 무과금"이 되어 소과금이 너무 빨랐음 → 광고 제거를 중과금으로 옮김.<br>
<b>흔들림</b>: 같은 설정 · 같은 씨앗인 대과금이 한 번은 138일, 한 번은 86일 — 게임 안에 씨앗과 무관한 무작위가 조금 섞여 있고 후반은 힘이 거의 안 올라, 한 번 잰 값이 ±30% 쯤 흔들립니다.</div>
<div class="shots"><figure><img src="캡쳐본 모음/밸런스_3차/92_shop_boost.png" loading="lazy"><figcaption>다이아 패널: 골드 부스터 [초안] (부스터 남은 날)</figcaption></figure></div>

<h2>결정 · 확인</h2>
'''
base = open(os.path.join(ROOT, '밸런스_2차.html'), encoding='utf8').read()
head = base[:base.index('<body><main>')].replace('<title>밸런스 2차</title>', '<title>밸런스 3차</title>')
tail = base[base.index('</main>'):].replace('earthdef-bal2-1117', 'earthdef-bal3-1117')
tail = re.sub(r'var L=\[.*?\],svg=', 'var L=' + json.dumps(lines_js, ensure_ascii=False) + ',svg=', tail, flags=re.S)
tail = re.sub(r'var W=\d+,l=\d+,r=\d+,D=\d+;', f'var W={W},l={L},r={R},D={DAYS};', tail)
open(os.path.join(ROOT, '밸런스_3차.html'), 'w', encoding='utf8').write(head + body + todo + '\n' + tail)
print('ok', f2p_days, [(l, reach(load(l), 100)) for l, _, _ in TIERS])
