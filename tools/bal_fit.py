# -*- coding: utf-8 -*-
"""밸런스 2차: 목표 날짜표에 맞게 스테이지별 적 체력 배율(Stage 표 Hp_Mul)을 거꾸로 계산.
한 번 잰 결과(bal_<label>.json)의 "날짜별 힘 P(d)" 와 "스테이지를 처음 깬 순간의 힘 need(s)" 로
  배율(s) = P(목표 날짜(s)) / (need(s) / 그때 쓴 배율(s))
→ 20 부터 10 스테이지마다 계단 모양으로 정리 (계단 안은 평평 — 그 안의 오름은 Mon_HP_Growth)
사용: PYTHONUTF8=1 python tools/bal_fit.py <label> <이전 배율 파일 또는 -> <출력 배율 파일> [잴 때 Chapter_Step_HP] [잴 때 Mon_HP_Growth] [다음 Mon_HP_Growth]
목표: D(s) = 360 x ((s-8)/92)^2.2  (s>8, 무과금 · 하루 18판)
"""
import json, math, os, sys

HERE = os.path.dirname(os.path.abspath(__file__))
BAL = os.path.join(HERE, '..', 'Earth_Defence', 'Logs', 'Bal')
N = 100


def target_day(s):
    return 0.6 if s <= 8 else 360 * ((s - 8) / 92) ** 2.2


def main():
    lb, prev, out = sys.argv[1], sys.argv[2], sys.argv[3]
    step_old = float(sys.argv[4]) if len(sys.argv) > 4 else 1.0
    g_old = float(sys.argv[5]) if len(sys.argv) > 5 else 1.0      # 잴 때 쓴 Mon_HP_Growth
    g_new = float(sys.argv[6]) if len(sys.argv) > 6 else g_old    # 다음에 쓸 Mon_HP_Growth (계단 안 오름)
    d = json.load(open(os.path.join(BAL, f'bal_{lb}.json'), encoding='utf8'))
    mul_old = [1.0] * N
    if prev != '-': mul_old = [float(x) for x in open(prev).read().split(',') if x.strip()]

    def steps(s): return (s - 20) // 10 + 1 if s >= 20 else 0
    days = sorted(d['days'], key=lambda x: x['day'])
    P = {x['day']: max(1.0, x['pw']) for x in days}
    last = days[-1]['day']
    # P(d) 늘이기: 마지막 60일의 log 기울기로
    xs = [x['day'] for x in days if x['day'] > last - 60]; ys = [math.log(P[x]) for x in xs]
    k = ((len(xs) * sum(a * b for a, b in zip(xs, ys)) - sum(xs) * sum(ys)) / max(1e-9, len(xs) * sum(a * a for a in xs) - sum(xs) ** 2)) if len(xs) > 2 else 0
    def Pd(day):
        day = max(1, day)
        if day <= last:
            lo = int(math.floor(day)); hi = min(last, lo + 1); t = day - lo
            return math.exp(math.log(P.get(lo, P[1])) * (1 - t) + math.log(P.get(hi, P[last])) * t)
        return P[last] * math.exp(max(k, 0.002) * (day - last))
    need = {}
    for f in d['fights']:
        if f['win'] and not f.get('farm') and f['stage'] not in need: need[f['stage']] = f['pw']
    # 단위 배율당 필요한 힘 (그때 쓴 배율로 나눔)
    unit = {s: need[s] / (mul_old[s - 1] * step_old ** steps(s)) * (g_new / g_old) ** (s - 1) for s in need}
    cl = sorted(unit)
    # 못 깬 스테이지는 마지막 20개의 log 기울기로 늘림
    tail = cl[-20:]
    if len(tail) > 2:
        xs = tail; ys = [math.log(unit[s]) for s in xs]
        kk = (len(xs) * sum(a * b for a, b in zip(xs, ys)) - sum(xs) * sum(ys)) / (len(xs) * sum(a * a for a in xs) - sum(xs) ** 2)
    else: kk = math.log(1.07)
    for s in range(1, N + 1):
        if s not in unit:
            base = max(c for c in cl if c < s) if any(c < s for c in cl) else cl[0]
            unit[s] = unit[base] * math.exp(kk * (s - base))
    raw = [Pd(target_day(s)) / unit[s] for s in range(1, N + 1)]
    # 계단: 20 부터 10 스테이지 묶음마다 평평하게 (묶음 안 기하 평균) · 20 아래는 그대로 (단, 1 이하는 1 이하로 두지 않음 — 튜토리얼은 Early_* 가 맡음)
    mul = raw[:]
    for c0 in range(20, N + 1, 10):
        grp = list(range(c0, min(N, c0 + 9) + 1))
        g = math.exp(sum(math.log(raw[s - 1]) for s in grp) / len(grp))
        for s in grp: mul[s - 1] = g
    # 계단은 뒤로 갈수록 반드시 오르게 (앞 계단의 1.1 배 이상)
    for c0 in range(30, N + 1, 10):
        if mul[c0 - 1] < mul[c0 - 11] * 1.1:
            for s in range(c0, min(N, c0 + 9) + 1): mul[s - 1] = mul[c0 - 11] * 1.1
    # 20 아래: 1~8 = 1 (튜토리얼은 Early_* 가 맡음), 9~19 = 1 → (20 계단 / 1.4) 로 곱으로 서서히 (20 에서 한 번 툭)
    top = mul[19] / 1.4
    for s in range(1, 20):
        mul[s - 1] = 1.0 if s <= 8 else math.exp(math.log(max(1.0, top)) * (s - 8) / 11)
    mul = [max(0.05, round(m, 4)) for m in mul]
    open(out, 'w').write(','.join(str(m) for m in mul))
    print('끝 날', last, '마지막 최고', days[-1]['best'])
    for s in (5, 10, 15, 19, 20, 30, 40, 50, 60, 70, 80, 90, 100):
        dd = next((x['day'] for x in days if x['best'] >= s), None)
        print(f'  {s:3}: 목표 {target_day(s):5.1f}일 · 이번 {dd if dd else "-":>4}일 · 배율 {mul_old[s - 1] * step_old ** steps(s):9.3f} → {mul[s - 1]:9.3f}')


if __name__ == '__main__':
    main()
