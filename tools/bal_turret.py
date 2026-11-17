# -*- coding: utf-8 -*-
"""주력 터렛 측정 결과 (Logs/Bal/feature_<label>.json) 요약 + 다음 Rate 배율 제안.
사용: PYTHONUTF8=1 python tools/bal_turret.py <label> [목표 k50]
k50 = 그 터렛을 주력으로 키운 판이 승률 50% 로 깨는 적 체력 배율 · share = 그 터렛의 피해 비중.
Rate 를 x 배 하면 k50 ≈ k50 x (1 - share + share x) 로 보고 x 를 구함 (0.6 ~ 2.5 로 자름).
"""
import json, os, sys
BAL = os.path.join(os.path.dirname(os.path.abspath(__file__)), '..', 'Earth_Defence', 'Logs', 'Bal')
LATE = dict(cannon=1.03, focus=1.06, vortex=1.09, wheel=1.12, tesla=1.15)   # 늦게 열리는 터렛은 조금 세게


def load(lb): return {r['turret']: r for r in json.load(open(os.path.join(BAL, f'feature_{lb}.json'), encoding='utf8'))}


def suggest(lb, target):
    out = {}
    for t, r in load(lb).items():
        if t == 'guardian': continue
        f = max(0.15, min(0.95, r.get('share', 0.5))); goal = target * LATE.get(t, 1.0)
        x = (goal / r['k50'] - 1 + f) / f
        out[t] = max(0.6, min(2.5, x))
    return out


if __name__ == '__main__':
    lb = sys.argv[1]; target = float(sys.argv[2]) if len(sys.argv) > 2 else 4.5
    sg = suggest(lb, target)
    for t, r in load(lb).items():
        print(f"{t:9} k50 {r['k50']:6.2f} · 피해 비중 {r.get('share', 0):4.0%} · 단계 {r['tier']} · 다음 Rate x{sg.get(t, 1):.2f}   {r['trace']}")
