# -*- coding: utf-8 -*-
"""밸런스 측정의 진 판 · 이긴 판 모양 (구간마다 진 때 · 이긴 판 남은 체력 중간값 · 하위 25%). 사용: python tools/bal_loss.py <label>"""
import json, os, sys
from collections import Counter
d = json.load(open(os.path.join(os.path.dirname(__file__), '..', 'Earth_Defence', 'Logs', 'Bal', f'bal_{sys.argv[1]}.json'), encoding='utf8'))
for lo, hi in ((1, 3), (4, 10), (11, 30), (31, 50), (51, 70), (71, 100)):
    L = [f for f in d['fights'] if lo <= f['stage'] <= hi and not f.get('farm')]
    if not L: continue
    lose = [f for f in L if not f['win']]; win = [f for f in L if f['win']]
    tc = Counter('시간초과' if f['time'] >= 479 else ('보스 전' if f['time'] < 210 else '보스 중') for f in lose)
    hp = sorted(round(f['hp'], 2) for f in win)
    print(f'{lo:3}-{hi:<3} 판 {len(L):3} 패 {len(lose):3} {dict(tc)} | 이긴 판 체력 중간 {hp[len(hp) // 2] if hp else "-"} · 하위25% {hp[len(hp) // 4] if hp else "-"}')
