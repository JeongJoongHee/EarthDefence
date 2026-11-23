# -*- coding: utf-8 -*-
"""11-23 오픈 스펙: 터렛은 찌릿 코일까지만 보임 — 별빛 부메랑 아래 6종은 Turret.xlsx Enabled 0
 숨긴 터렛: 터렛 탭 · 전투 카드 · 조각 · 회로 드롭 · 터렛 외형 · 회로 도감에서 빠짐 (표 줄 · 세이브는 그대로)
사용: PYTHONUTF8=1 python tools/open_turret_tables.py   (다시 돌려도 같은 값)
 Unity 가 열려 있어도 되게 Turret.csv 도 같이 고침 (Unity 표 변환 결과와 같은 모양)
되돌리기: Turret.xlsx Enabled 칸을 1(또는 비움) → 표 변환"""
import csv, io, os, re, sys
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from phase2_tables import TB
from open_spec_tables import enable
from openpyxl import load_workbook

OPEN_TURRET = ['guardian', 'rail', 'laser', 'drone', 'hive', 'missile', 'bolt',
               'cannon', 'focus', 'vortex', 'wheel', 'tesla']
CSV_PATH = os.path.join(os.path.dirname(TB), 'Assets', 'Resources', 'Tables', 'Turret.csv')
FIELD = re.compile(r'"(?:[^"]|"")*"|[^,]*')


def split_raw(line):
    """따옴표를 그대로 둔 채 칸 나누기 (다른 칸의 글자가 바뀌지 않게)"""
    out, i = [], 0
    while True:
        m = FIELD.match(line, i); out.append(m.group(0)); i = m.end()
        if i >= len(line): return out
        i += 1   # 쉼표
        if i == len(line): out.append(''); return out


def patch_csv():
    text = open(CSV_PATH, encoding='utf-8-sig', newline='').read()
    lines = text.split('\n')
    head = split_raw(lines[0])
    if 'Enabled' in head:
        ci = head.index('Enabled')
    else:
        ci = head.index('Info') + 1
        lines[0] = ','.join(head[:ci] + ['Enabled'] + head[ci:])
        for n in range(1, len(lines)):
            if lines[n]:
                f = split_raw(lines[n]); lines[n] = ','.join(f[:ci] + [''] + f[ci:])
    for n in range(1, len(lines)):
        if not lines[n]: continue
        f = split_raw(lines[n]); f[ci] = '1' if f[0] in OPEN_TURRET else '0'; lines[n] = ','.join(f)
    new = '\n'.join(lines)
    rows = list(csv.reader(io.StringIO(new)))
    if len({len(r) for r in rows if r}) != 1: raise SystemExit('csv 칸 수가 줄마다 다름')
    with open(CSV_PATH, 'w', encoding='utf-8-sig', newline='') as f: f.write(new)
    return [r[0] for r in rows[1:] if r and r[ci] == '0']


def main():
    p = os.path.join(TB, 'Turret.xlsx'); wb = load_workbook(p)
    print('Turret 보임/숨김', enable(wb['Turret'], OPEN_TURRET)); wb.save(p)
    print('csv 숨김', patch_csv())


if __name__ == '__main__':
    main()
