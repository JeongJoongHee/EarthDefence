# -*- coding: utf-8 -*-
"""밸런스 측정 결과 요약 (Earth_Defence/Logs/Bal/bal_<label>.json — PlayTest.RunBal 이 만듦).
사용: PYTHONUTF8=1 python tools/bal_summary.py now try1 try2 ...
- 날짜별 최고 스테이지
- 구간별 "처음 도전" 승률 · 이겼을 때 남은 기지 체력 · 깨기까지 도전 횟수 (10 의 배수 스테이지는 따로)
- 공략 비교 (잘 고름 ↔ 아무거나)
"""
import json, os, sys

HERE = os.path.dirname(os.path.abspath(__file__))
BAL = os.path.join(HERE, '..', 'Earth_Defence', 'Logs', 'Bal')
DAYS = (1, 3, 7, 14, 30, 60, 90, 120, 180, 240, 300, 360, 420)
BANDS = ((1, 3), (4, 10), (11, 20), (21, 30), (31, 40), (41, 50), (51, 70), (71, 100))


def load(label):
    return json.load(open(os.path.join(BAL, f'bal_{label}.json'), encoding='utf8'))


def stage_stats(d):
    """스테이지마다: 처음 도전 결과 · 남은 체력 · 깰 때까지 도전 수 (골드 벌기 판은 뺌)"""
    st = {}
    for f in d['fights']:
        if f.get('farm') or f.get('elite'): continue
        s = st.setdefault(f['stage'], dict(tries=0, first=None, cleared=False, hpWin=None, day=f['day']))
        if s['cleared']: continue
        s['tries'] += 1
        if s['first'] is None: s['first'] = f
        if f['win']: s['cleared'] = True; s['hpWin'] = f['hp']
    return st


def best_by_day(d):
    m = {x['day']: x['best'] for x in d['days']}
    return {day: m.get(day, max(m.values()) if day > max(m) else None) for day in DAYS if day <= d['args']['days']}


def summary(label):
    d = load(label); st = stage_stats(d)
    out = [f'== {label}  set={d.get("set", {})}']
    bd = best_by_day(d)
    out.append('  최고: ' + ' · '.join(f'{k}일 {v}' for k, v in bd.items()))
    for a, b in BANDS:
        ss = [st[k] for k in range(a, b + 1) if k in st]
        if not ss: continue
        norm = [s for k, s in ((k, st[k]) for k in range(a, b + 1) if k in st) if k % 10 != 0]
        boss = [s for k, s in ((k, st[k]) for k in range(a, b + 1) if k in st) if k % 10 == 0]
        def line(xs, name):
            if not xs: return ''
            fw = sum(1 for s in xs if s['first']['win']) / len(xs)
            hp = [s['first']['hp'] for s in xs if s['first']['win']]
            tr = [s['tries'] for s in xs if s['cleared']]
            return f'{name} 처음 승률 {fw:4.0%} · 남은 체력 {sum(hp) / len(hp) if hp else 0:4.0%} · 도전 {sum(tr) / len(tr) if tr else 0:3.1f}번 ({len(xs)}개)'
        out.append(f'  {a:3}-{b:<3} ' + line(norm, '보통') + ('  | 10배수 ' + line(boss, '') if boss else ''))
    for p in d['probes']:
        out.append(f'  공략 {p["stage"]:3} ({p["day"]}일): 잘 고름 {p["goodWin"]}/{p["n"]} 체력 {p["goodHp"]:.0%} · 아무거나 {p["randWin"]}/{p["n"]} 체력 {p["randHp"]:.0%}  [기지 {p["baseLv"]} · 터렛 {p["tLv"]}]')
    last = d['days'][-1]
    out.append(f'  끝: {last["day"]}일 최고 {last["best"]} · 기지 Lv {last["baseLv"]} · 터렛 {last["tLv"]} · 장비 {last["gear"]} · 다이아 {last["dia"]}')
    return '\n'.join(out)


if __name__ == '__main__':
    for lb in sys.argv[1:] or ['now']:
        print(summary(lb))
