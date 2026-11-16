# -*- coding: utf-8 -*-
"""밸런스 1차 보고서 (밸런스_1차.html) — Logs/Bal/bal_*.json (PlayTest.RunBal) 에서 만듦. 다시 재면 다시 돌리면 됨.
사용: PYTHONUTF8=1 python tools/balance_report.py
"""
import ast, json, os, re, statistics as stt

HERE = os.path.dirname(os.path.abspath(__file__)); ROOT = os.path.join(HERE, '..')
BAL = os.path.join(ROOT, 'Earth_Defence', 'Logs', 'Bal')
NOW, FIN, SLOW = ['now'], ['bal1_1000', 'bal1_5000', 'bal1_9000'], ['slow_1000']
BANDS = ((1, 3, '1~3 (튜토리얼)'), (4, 10, '4~10'), (11, 30, '11~30'), (31, 50, '31~50'), (51, 70, '51~70'), (71, 100, '71~100'))
TARGET = {1: '처음에 깸 · 거의 안 맞음', 4: '대부분 처음에 깸 · 조금 맞음', 11: '처음 승률 50~80% · 체력 50~80%', 31: '처음 승률 40~70% · 체력 40~70%',
          51: '처음 승률 20~40% · 체력 30~60% (벽)', 71: '처음 승률 20~40% · 체력 30~60% (벽)'}


def load(lb): return json.load(open(os.path.join(BAL, f'bal_{lb}.json'), encoding='utf8'))


def band(d, lo, hi):
    """구간: 처음 도전 승률 · 이긴 판 남은 체력 중간값 · 깰 때까지 도전 수 (골드 벌기 판 뺌)"""
    first, hp, tries, seen, cleared = [], [], {}, set(), set()
    for f in d['fights']:
        if f.get('farm') or not lo <= f['stage'] <= hi: continue
        s = f['stage']
        if s in cleared: continue
        tries[s] = tries.get(s, 0) + 1
        if s not in seen: seen.add(s); first.append(f['win'])
        if f['win']: cleared.add(s); hp.append(f['hp'])
    if not first: return None
    return (sum(first) / len(first), stt.median(hp) if hp else 0, sum(tries[s] for s in cleared) / max(1, len(cleared)), len(cleared))


def avg_band(labels, lo, hi):
    xs = [b for b in (band(load(l), lo, hi) for l in labels) if b]
    if not xs: return None
    return tuple(sum(x[i] for x in xs) / len(xs) for i in range(3)) + (min(x[3] for x in xs),)


def best_curve(lb, days):
    m = {x['day']: x['best'] for x in load(lb)['days']}
    last = 0; out = []
    for dd in range(1, days + 1): last = m.get(dd, last if dd > max(m) else last); out.append(m.get(dd, last)); last = out[-1]
    return out


def day_reach(lb, st):
    for x in load(lb)['days']:
        if x['best'] >= st: return x['day']
    return None


# ---------------------------------------------------------------- 그래프 (날짜 → 최고 스테이지, 선 3개)
DAYS = 90
series = [('지금 (밸런스 전)', '#2a78d6', [best_curve('now', DAYS)]),
          ('1차 적용 (씨앗 3개 평균)', '#eb6834', [best_curve(l, DAYS) for l in FIN]),
          ('비교: 느린 안 (골드 성장 1.055)', '#1baf7a', [best_curve('slow_1000', DAYS)])]
W, H, L, R, T, B = 760, 320, 48, 150, 16, 34
def X(d): return L + (d - 1) / (DAYS - 1) * (W - L - R)
def Y(v): return T + (1 - v / 100) * (H - T - B)
svg = [f'<svg viewBox="0 0 {W} {H}" width="100%" role="img" aria-label="날짜별 최고 스테이지" id="curve">']
for v in (0, 25, 50, 75, 100):
    svg.append(f'<line x1="{L}" x2="{W - R}" y1="{Y(v):.1f}" y2="{Y(v):.1f}" stroke="{"#c3c2b7" if v == 0 else "#e1e0d9"}" stroke-width="1"/><text x="{L - 8}" y="{Y(v) + 4:.1f}" text-anchor="end" class="ax">{v}</text>')
for dd in (1, 7, 14, 30, 45, 60, 90):
    svg.append(f'<text x="{X(dd):.1f}" y="{H - 12}" text-anchor="middle" class="ax">{dd}일</text>')
lines_js = []; ends = []
for name, col, curves in series:
    ys = [sum(c[i] for c in curves) / len(curves) for i in range(DAYS)]
    lines_js.append({'name': name, 'col': col, 'ys': [round(y, 1) for y in ys]})
    pts = ' '.join(f'{X(i + 1):.1f},{Y(y):.1f}' for i, y in enumerate(ys))
    svg.append(f'<polyline points="{pts}" fill="none" stroke="{col}" stroke-width="2" stroke-linejoin="round" stroke-linecap="round"/>')
    ends.append([Y(ys[-1]) + 4, f'{name.split(" (")[0]} {ys[-1]:.0f}'])
ends.sort()
for i in range(1, len(ends)):
    if ends[i][0] - ends[i - 1][0] < 17: ends[i][0] = ends[i - 1][0] + 17     # 이름표 겹침 벌리기
for y, t in ends: svg.append(f'<text x="{W - R + 8}" y="{y:.1f}" class="lab">{t}</text>')
svg.append(f'<line id="cx" x1="0" x2="0" y1="{T}" y2="{H - B}" stroke="#7a6a5c" stroke-width="1" visibility="hidden"/>')
svg.append(f'<rect x="{L}" y="{T}" width="{W - L - R}" height="{H - T - B}" fill="transparent" id="hit"/></svg>')
chart = '\n'.join(svg)

# ---------------------------------------------------------------- 표
rows = []
for lo, hi, nm in BANDS:
    a, b = avg_band(NOW, lo, hi), avg_band(FIN, lo, hi)
    def c(x): return '<td colspan="3" class="mut">못 감</td>' if not x else f'<td>{x[0]:.0%}</td><td>{x[1]:.0%}</td><td>{x[2]:.1f}번</td>'
    rows.append(f'<tr><th>{nm}</th>{c(a)}{c(b)}<td class="mut">{TARGET[lo]}</td></tr>')
band_table = ('<table><tr><th rowspan="2">스테이지</th><th colspan="3">지금 (밸런스 전)</th><th colspan="3">1차 적용 (씨앗 3개 평균)</th><th rowspan="2">1차 목표</th></tr>'
              '<tr><th>처음 승률</th><th>남은 체력</th><th>도전</th><th>처음 승률</th><th>남은 체력</th><th>도전</th></tr>' + ''.join(rows) + '</table>')
reach = '<table><tr><th>스테이지</th>' + ''.join(f'<th>{s}</th>' for s in (10, 20, 30, 50, 70, 100)) + '</tr>'
for nm, labels in (('지금', NOW), ('1차 적용', FIN), ('느린 안', SLOW)):
    cells = []
    for s in (10, 20, 30, 50, 70, 100):
        ds = [day_reach(l, s) for l in labels]
        cells.append('<td class="mut">못 감</td>' if any(x is None for x in ds) else f'<td>{min(ds)}~{max(ds)}일</td>' if min(ds) != max(ds) else f'<td>{ds[0]}일</td>')
    reach += f'<tr><th>{nm}</th>{"".join(cells)}</tr>'
reach += '</table>'

# 공략 비교 (잘 고름 ↔ 아무거나) — 씨앗 3개 합
pr = {}
for l in FIN:
    for p in load(l)['probes']:
        q = pr.setdefault(p['stage'], [0, 0, 0]); q[0] += p['goodWin']; q[1] += p['randWin']; q[2] += p['n']
gw = sum(v[0] for v in pr.values()); rw = sum(v[1] for v in pr.values()); nn = sum(v[2] for v in pr.values())
probe = '<table><tr><th>스테이지</th>' + ''.join(f'<th>{s}</th>' for s in sorted(pr)) + '<th>합</th></tr>'
probe += '<tr><th>규칙대로 고름</th>' + ''.join(f'<td>{pr[s][0]}/{pr[s][2]}</td>' for s in sorted(pr)) + f'<td><b>{gw}/{nn}</b></td></tr>'
probe += '<tr><th>아무거나</th>' + ''.join(f'<td>{pr[s][1]}/{pr[s][2]}</td>' for s in sorted(pr)) + f'<td><b>{rw}/{nn}</b></td></tr></table>'

# 바뀐 값 (balance1_apply.py VALUES)
src = open(os.path.join(HERE, 'balance1_apply.py'), encoding='utf8').read()
vals = ast.literal_eval(src[src.index('VALUES = [') + 9: src.index(']\n\np = ') + 1])
chg = '<table><tr><th>Config</th><th>값</th><th>설명</th></tr>' + ''.join(f'<tr><td><code>{k}</code></td><td>{v}</td><td>{n.replace("밸런스 1차: ", "").replace(" — 밸런스 1차:", " —")}</td></tr>' for k, v, n in vals) + '</table>'

last = [load(l)['days'][-1] for l in FIN]

items = [
    ('b-pace', '결정', '진행 속도: 100 스테이지를 얼마 만에?',
     '추천 = <b>1차 그대로 (무과금 · 하루 12판 기준 약 40일)</b> + 기간은 엘리트 난이도(같은 100 스테이지, 적 2.6배) · 무한 모드로 늘리기. 골드를 줄여 느리게 하면(느린 안 81일) 후반 스테이지마다 평균 11번 져서 "계속 지는" 느낌이 큼. 더 길게 원하시면 적어 주세요(예: 60일 · 90일).'),
    ('b-day', '결정', '하루 판 수 가정 (12판 ≈ 50분)',
     '측정은 하루 12판 · 순찰 16시간 · 무료 상자 · 출석 · 다이아는 안 씀(무과금)으로 했습니다. 생각하시는 하루 플레이가 다르면 알려 주세요 — 그 기준으로 다시 잽니다.'),
    ('b-theme', '제안', '공략 축: 스테이지마다 "주 약점" 하나',
     f'지금은 한 스테이지 적 약점이 4~5가지로 흩어지고 보스 약점도 달라서, 카드를 규칙대로 골라도 이기는 수가 아무거나와 비슷함 (규칙대로 {gw}/{nn} · 아무거나 {rw}/{nn}).' f' 추천 = 스테이지마다 적의 60% 이상 + 보스가 같은 약점 → 로비 [적 정보]의 약점을 보고 그 속성 터렛 · 카드를 고르면 확실히 쉬워지게 (2차, Stage 표만 바꿈). 약점 배율 1.5 → 1.8 같이.'),
    ('b-boss', '제안', '보스전 = 버티며 깎는 싸움',
     '1차에서 보스 체력 140 → 100배 · 공격력 6 → 4배로 "한 번에 무너짐"을 줄였습니다. 2차 추천 = 보스 체력의 절반쯤에서 패턴(부하 몰려옴 · 돌진)이 바뀌어, 그 순간 대장 카피를 옮겨 막는 손맛 (직접 조작 공략).'),
    ('b-ten', '확인', '10의 배수 = "쇠 비" 스테이지 (챕터 벽)',
     '갑옷 운석 · 딱정벌레 · 적 종류가 많아 다른 스테이지보다 훨씬 셌음 → 적 체력 0.8배 (Config Ten_Stage_HP_Mul). 지금은 가끔 2~5번 도전하는 정도의 벽. 더 세게/약하게 원하시면 값 하나로.'),
    ('b-tut', '확인', '처음 1~9 스테이지는 약하게 (튜토리얼)',
     '스테이지 1 = 0.03배에서 10 = 1배까지 곱으로 서서히 (Config Early_Stage_N · Early_HP_Mul). 씨앗 3개 모두 1~3을 처음에 깸.'),
    ('b-later', '안내', '아직 안 맞춘 것 (2차 밸런스)',
     '엘리트 난이도 · 무한 모드 · 엘리트 군단 수치, 터렛 · 카드 하나하나의 세기, 장비 · 회로 수치, 상점 값 · 미션 보상, 광고 2배 · 결제(다이아)가 진행에 주는 영향.'),
]
todo = '<ul class="todo">' + ''.join(f'<li data-id="{i}"><label><input type="checkbox"><span><span class="tag">{t}</span>{n}<span class="why">{w}</span></span></label></li>' for i, t, n, w in items) + '</ul>'

base = open(os.path.join(ROOT, '요청05_결과.html'), encoding='utf8').read()
head = base[:base.index('<body><main>')].replace('<title>요청 사항 05 반영</title>', '<title>밸런스 1차</title>')
tail = base[base.index('</main>'):].replace('earthdef-req5-1117', 'earthdef-bal1-1117')
css = '''<style>.ax{font-size:12px;fill:#7a6a5c}.lab{font-size:13px;fill:#3b281c}.mut{color:#7a6a5c}
.chartbox{background:#fcfcfb;border:1px solid #e3d8c6;border-radius:12px;padding:10px 10px 4px;position:relative}
.legend{display:flex;gap:16px;flex-wrap:wrap;font-size:14px;margin:4px 6px 8px}.legend i{display:inline-block;width:14px;height:3px;border-radius:2px;vertical-align:middle;margin-right:6px}
#tip{position:absolute;pointer-events:none;background:#fff;border:1px solid #e3d8c6;border-radius:8px;padding:6px 10px;font-size:13px;display:none;box-shadow:0 2px 6px rgba(0,0,0,.08)}
.kpi{display:grid;grid-template-columns:repeat(auto-fit,minmax(180px,1fr));gap:10px;margin:12px 0}.kpi div{background:#fff;border:1px solid #e3d8c6;border-radius:10px;padding:10px 12px}.kpi b{display:block;font-size:22px}.kpi span{color:#7a6a5c;font-size:13px}
.vid{display:grid;grid-template-columns:1fr 1fr;gap:12px}.vid img{width:100%;border:2px solid #3b281c;border-radius:10px}@media(max-width:700px){.vid{grid-template-columns:1fr}}</style>'''
legend = '<div class="legend">' + ''.join(f'<span><i style="background:{c}"></i>{n}</span>' for n, c, _ in series) + '</div>'
reach_days = [day_reach(l, 100) for l in FIN]
late = [avg_band(FIN, lo, hi) for lo, hi, _ in BANDS[3:]]
mid = avg_band(FIN, 11, 30)
body = f'''<body><main>
<h1>밸런스 1차</h1>
<p class="note">2026-11-17 · 실제 게임 코드로 진행을 흉내 낸 측정(<code>PlayTest.RunBal</code>, 한 판 0.5초) · 무과금 · 하루 12판 · 순찰 16시간 · 씨앗 3개 · 자동 시험 기존 120 + 시험장 13 + 3차·6차 95줄 오류 0</p>
<div class="bar"><span id="stat"></span><button id="copyNotes" type="button">의견 모아 복사</button></div>
<div class="kpi"><div><b>{min(reach_days)}~{max(reach_days)}일</b><span>100 스테이지까지 (지금: 90일에 39)</span></div>
<div><b>벽 없음</b><span>지금은 25~30에서 막혀 30일에 34</span></div>
<div><b>1~3 100%</b><span>튜토리얼은 처음에 깸 (씨앗 3개)</span></div>
<div><b>남은 체력 {min(x[1] for x in late):.0%}~{max(x[1] for x in late):.0%}</b><span>이긴 판 (31 이후 구간 중간값) · 11~30 은 {mid[1]:.0%} 로 아직 조금 쉬움</span></div></div>
<div class="tip blue"><b>먼저 알려 드릴 것</b><ul>
<li><b>참고 영상 수준</b> = 영상 ②(스테이지 4~6) · ③(10~12)을 15초마다 잘라 봤더니, <b>원작은 기지 체력이 한 번도 안 깎임</b> (3000 → 3800 → 4456 으로 오르기만, 방패 숫자만 스테이지 12에서 5 → 3). 한 판 약 5분 · 5분 무렵 레벨 20. 그래서 판 길이 · 레벨 흐름은 영상대로 두고, 난이도는 말씀대로 <b>"맞으면서 깨는"</b> 쪽으로 영상보다 조금 빡빡하게 잡았습니다.</li>
<li><b>지금 상태의 문제</b>: 1~20은 아무거나 골라도 안 맞고 이김 → 25~30에서 갑자기 벽 (적 체력은 스테이지마다 1.16배 곱인데, 보상 · 기지 공격력은 더하기라 힘이 못 따라감).</li>
<li><b>1차에서 한 것</b>: 적은 처음부터 세게 · 성장은 완만하게, 보상 · 기지 공격력은 곱으로 크게 (Config 7개 새로 · 13개 고침, 1 = 예전 그대로인 손잡이). 압박을 판 앞쪽이 아니라 보스 무렵으로 옮김. 처음 9스테이지는 약하게, 10의 배수(쇠 비)는 0.8배.</li>
<li><b>솔직한 결과</b>: 지금 카드 설계에선 <b>무엇을 고르느냐보다 어떤 카드가 나오느냐(운)가 큽니다</b> — 제가 만든 규칙(돌파 · 콤보 먼저 · 약점 · 몰아주기)이 {gw}/{nn}, 아무거나가 {rw}/{nn}로 오히려 비슷하거나 조금 높음. "공략을 잘해서 깬다"를 살리려면 고르는 방향이 결과를 바꾸게 해야 하고, 가장 쉬운 길이 스테이지마다 약점을 모으는 것(아래 제안 b-theme · 표만 바꿈)입니다 — 2차로 추천.</li>
<li>값 = <code>tools/balance1_apply.py</code> (표 고치기) · 측정 = <code>bash tools/bal_run.sh &lt;이름&gt; &lt;일수&gt; "&lt;Config 덮어쓰기&gt;"</code> · 요약 <code>tools/bal_summary.py</code> · 이 보고서 <code>tools/balance_report.py</code>.</li>
</ul></div>

<h2>날짜별 최고 스테이지</h2>
<div class="chartbox">{legend}{chart}<div id="tip"></div></div>
{reach}

<h2>판의 느낌 (구간별)</h2>
<p class="note">처음 승률 = 그 스테이지 첫 도전에서 이긴 비율 · 남은 체력 = 이긴 판의 기지 체력 중간값 · 도전 = 깰 때까지 평균 도전 수 (지면 깬 스테이지에서 골드를 벌고 다시 도전).</p>
{band_table}

<h2>공략 차이 (규칙대로 고름 ↔ 아무거나, 씨앗 3개 · 4판씩)</h2>
<p class="note">그 스테이지를 처음 도전하는 순간의 세이브로, 카드를 규칙대로 고르는 사람(돌파 · 콤보 먼저 · 약점 속성 · 터렛 3기에 몰아주기)과 아무거나 고르는 사람을 같은 씨앗으로 겨룸. 규칙 6가지를 먼저 겨뤄 가장 잘 이긴 규칙을 썼습니다.</p>
{probe}

<h2>참고 영상에서 본 것</h2>
<div class="vid"><figure><img src="캡쳐본 모음/밸런스_1차/video_stage4.png" loading="lazy"><figcaption>② 스테이지 4 — 15초마다 (시간 · 레벨 · 기지 체력 3000 그대로)</figcaption></figure>
<figure><img src="캡쳐본 모음/밸런스_1차/video_stage12.png" loading="lazy"><figcaption>③ 스테이지 11~12 — 기지 체력 4456 그대로 · 방패 5 → 3</figcaption></figure></div>

<h2>바뀐 값 (Config.xlsx)</h2>
{chg}

<h2>결정 · 제안</h2>
{todo}
'''
script = '''<script>
(function(){var L=''' + json.dumps(lines_js, ensure_ascii=False) + ''',svg=document.getElementById('curve'),hit=document.getElementById('hit'),cx=document.getElementById('cx'),tip=document.getElementById('tip');
var W=''' + str(W) + ''',l=''' + str(L) + ''',r=''' + str(R) + ''',D=''' + str(DAYS) + ''';
function at(e){var p=svg.createSVGPoint();p.x=e.clientX;p.y=e.clientY;var q=p.matrixTransform(svg.getScreenCTM().inverse());return Math.max(1,Math.min(D,Math.round((q.x-l)/(W-l-r)*(D-1))+1));}
function show(e){var d=at(e),x=l+(d-1)/(D-1)*(W-l-r);cx.setAttribute('x1',x);cx.setAttribute('x2',x);cx.setAttribute('visibility','visible');
tip.innerHTML='<b>'+d+'일</b><br>'+L.map(function(s){return '<span style="color:'+s.col+'">━</span> '+s.name+': '+Math.round(s.ys[d-1]);}).join('<br>');
var b=svg.parentNode.getBoundingClientRect();tip.style.display='block';tip.style.left=Math.min(e.clientX-b.left+12,b.width-260)+'px';tip.style.top=(e.clientY-b.top+12)+'px';}
hit.addEventListener('mousemove',show);hit.addEventListener('touchstart',function(e){show(e.touches[0]);},{passive:true});
hit.addEventListener('mouseleave',function(){tip.style.display='none';cx.setAttribute('visibility','hidden');});})();
</script>'''
html = head.replace('</head>', css + '</head>') + body + tail.replace('</body>', script + '</body>')
open(os.path.join(ROOT, '밸런스_1차.html'), 'w', encoding='utf8').write(html)
print('ok', gw, rw, nn, reach_days)
