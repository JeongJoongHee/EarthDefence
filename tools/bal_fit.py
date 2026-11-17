# -*- coding: utf-8 -*-
"""목표 날짜표에 맞게 스테이지별 적 체력 배율(Stage 표 Hp_Mul)을 거꾸로 계산.
측정 결과(bal_<label>.json, 씨앗 여러 개면 쉼표로)의 "날짜별 힘 P(d)" 와 "스테이지를 처음 깬 순간의 힘 need(s)" 로
  배율(s) = P(목표 날짜(s)) / (need(s) / 그때 쓴 배율(s))
밸런스 3차에서 바꾼 것:
  - 계단: 20 부터 10 스테이지마다 "툭" — 그 장 오름의 STEP 비율(기본 0.5)만 장 첫 스테이지에서, 나머지는 장 안에서 고르게
    (2차는 전부 첫 스테이지에 몰려 70 스테이지에서 43일을 머물렀음)
  - 테마 보정: 같은 테마(스테이지 번호 끝자리)가 유난히 어렵거나 쉬우면 그만큼 배율을 나눔 (2차: 거미 언덕 33 · 43 · 83 · 93 에서 오래 머묾)
사용: PYTHONUTF8=1 python tools/bal_fit.py <label[,label…]> <이전 배율 파일 또는 -> <출력 배율 파일> [잴 때 Mon_HP_Growth] [다음 Mon_HP_Growth]
목표: D(s) = TOTAL x ((s-8)/92)^2.2  (s>8) — TOTAL 기본 360 (환경 변수 BAL_TOTAL)
"""
import json, math, os, sys

HERE = os.path.dirname(os.path.abspath(__file__))
BAL = os.path.join(HERE, '..', 'Earth_Defence', 'Logs', 'Bal')
N = 100
TOTAL = float(os.environ.get('BAL_TOTAL', 360))
STEP = float(os.environ.get('BAL_STEP', 0.5))


def target_day(s):
    return 0.6 if s <= 8 else TOTAL * ((s - 8) / 92) ** 2.2


def gmean(xs): return math.exp(sum(math.log(max(1e-12, x)) for x in xs) / len(xs))


def linfit(xs, ys):
    n = len(xs); sx, sy = sum(xs), sum(ys)
    den = n * sum(x * x for x in xs) - sx * sx
    k = (n * sum(x * y for x, y in zip(xs, ys)) - sx * sy) / den if abs(den) > 1e-9 else 0
    return k, (sy - k * sx) / n


def main():
    labels, prev, out = sys.argv[1].split(','), sys.argv[2], sys.argv[3]
    g_old = float(sys.argv[4]) if len(sys.argv) > 4 else 1.0
    g_new = float(sys.argv[5]) if len(sys.argv) > 5 else g_old
    mul_old = [1.0] * N
    if prev != '-': mul_old = [float(x) for x in open(prev).read().split(',') if x.strip()]
    runs = [json.load(open(os.path.join(BAL, f'bal_{lb}.json'), encoding='utf8')) for lb in labels]

    # 날짜별 힘 (씨앗 기하 평균) + 끝난 뒤는 마지막 60일 기울기로 늘림
    last = max(max(x['day'] for x in d['days']) for d in runs)
    def p_of(d):
        m = {x['day']: max(1.0, x['pw']) for x in d['days']}; lastd = max(m)
        xs = [x for x in m if x > lastd - 60]; k, _ = linfit(xs, [math.log(m[x]) for x in xs]) if len(xs) > 2 else (0, 0)
        return lambda day: m[min(lastd, max(1, int(day)))] if day <= lastd else m[lastd] * math.exp(max(k, 0.002) * (day - lastd))
    pfs = [p_of(d) for d in runs]
    def Pd(day):
        lo = max(1, math.floor(day)); t = max(0.0, day - lo)
        return math.exp(sum((1 - t) * math.log(pf(lo)) + t * math.log(pf(lo + 1)) for pf in pfs) / len(pfs))

    # 스테이지를 처음 깬 순간의 힘 → 단위 배율당 필요한 힘
    units = {}
    for d in runs:
        need = {}
        for f in d['fights']:
            if f['win'] and not f.get('farm') and not f.get('elite') and f['stage'] not in need: need[f['stage']] = f['pw']
        for s, v in need.items(): units.setdefault(s, []).append(v / mul_old[s - 1] * (g_new / g_old) ** (s - 1))
    unit = {s: gmean(v) for s, v in units.items()}
    cl = sorted(unit)
    # 매끈한 흐름: 앞뒤 7 스테이지 log 직선
    def trend(s):
        xs = [c for c in cl if abs(c - s) <= 7 and c >= 9]
        if len(xs) < 3: xs = [c for c in cl if c >= 9][:12] if s < 50 else cl[-12:]     # 앞쪽은 앞쪽 자료로 (예전엔 후반 자료로 늘려 1 스테이지 값이 터무니없이 컸음)
        k, b = linfit(xs, [math.log(unit[c]) for c in xs])
        return math.exp(k * s + b)
    tr = {s: trend(s) for s in range(1, N + 1)}
    # 테마 보정 (11 스테이지부터 · 0.6 ~ 1.6)
    tf = {}
    for t in range(10):
        xs = [unit[s] / tr[s] for s in cl if s >= 11 and (s - 1) % 10 == t]
        tf[t] = min(1.6, max(0.6, gmean(xs))) if len(xs) >= 2 else 1.0
    g = gmean(list(tf.values())); tf = {t: v / g for t, v in tf.items()}
    raw = [Pd(target_day(s)) / tr[s] for s in range(1, N + 1)]
    # 적의 실제 세기(배율 x Mon_HP_Growth^(s-1))가 뒤로 갈수록 내려가지 않게 — 9 스테이지부터 (1~8 은 Early_* 가 맡음)
    for i in range(9, N): raw[i] = max(raw[i], raw[i - 1] / g_new)
    mul = raw[:]
    for c0 in range(20, N + 1, 10):
        grp = list(range(c0, min(N, c0 + 9) + 1)); flat = gmean([raw[s - 1] for s in grp])
        for s in grp: mul[s - 1] = math.exp(STEP * math.log(flat) + (1 - STEP) * math.log(raw[s - 1]))
    # 20 아래: 1~8 = 1 (튜토리얼은 Early_* 가 맡음), 9~19 = 1 → (20 의 값 / 1.25) 로 곱으로 서서히
    top = max(1.0, mul[19] / 1.25)
    for s in range(1, 20): mul[s - 1] = 1.0 if s <= 8 else math.exp(math.log(top) * (s - 8) / 11)
    mul = [max(0.05, round(m / tf[(s - 1) % 10] if s >= 11 else m, 4)) for s, m in enumerate(mul, 1)]
    open(out, 'w').write(','.join(str(m) for m in mul))
    print('측정', labels, '끝 날', last, '· 테마 보정', ' '.join(f'{t + 1}:{v:.2f}' for t, v in tf.items()))
    for s in (10, 15, 19, 20, 21, 29, 30, 40, 50, 60, 70, 80, 90, 100):
        dd = [next((x['day'] for x in d['days'] if x['best'] >= s), None) for d in runs]
        print(f'  {s:3}: 목표 {target_day(s):5.1f}일 · 이번 {"/".join(str(x) if x else "-" for x in dd):>9}일 · 배율 {mul_old[s - 1]:9.3f} → {mul[s - 1]:9.3f}')


if __name__ == '__main__':
    main()
