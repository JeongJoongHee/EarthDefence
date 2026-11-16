# -*- coding: utf-8 -*-
"""터렛 대기 효과 촬영 → GIF (11-16 밤 "터렛이 작동하는 느낌" 시안).

순서: bash tools/unity_batch.sh EarthDef.EditorTools.PlayTest.RunIdle --play
      → Earth_Defence/Logs/PlayTest/idle/s{묶음}p{0 끔 · 1 켬}_000.png ~, 1/30초 간격 90장씩
      PYTHONUTF8=1 python tools/idle_gif.py <출력 폴더>
      → <출력>/<터렛>_p0_x2.gif · <터렛>_p1_x2.gif (2배 확대) + base<묶음>_p*.gif (기지 전체, 실제 크기 0.6배)
월드 → 화면(1080x2340): x = (wx + 5.4) * 100 · 위에서 y = (13.8 - wy) * 100 (카메라 아래 끝 -9.6, 크기 11.7)
촬영 묶음 s0 = 레이저 포(칸 0, x -2.72) · 벼락탑(칸 1, +2.72) · 관통포(칸 2, -4.3) · 드론(칸 3, +4.3) / s1 = 벌집(-2.72) · 미사일(+2.72) · 바닥 Ground_Y -7.52
          s2 = 쾅쾅 대포 · 집중 빔 · 소용돌이포 · 불바퀴 / s3 = 찌릿 코일 (2차)
2차 터렛만: PYTHONUTF8=1 python tools/idle_gif.py 2차_캡처/idle cannon focus vortex wheel tesla
파일 이름: s{묶음}p{0 끔 · 1 켬}_000.png
"""
import os, sys, glob
from PIL import Image

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), '..'))
SRC = os.path.join(ROOT, 'Earth_Defence', 'Logs', 'PlayTest', 'idle')
TOP, GROUND = 13.8, -7.52


def px(wx, wy):
    return int((wx + 5.4) * 100), int((TOP - wy) * 100)


def crop_box(wx, w=280, up=250, down=40):
    x, y = px(wx, GROUND)
    x0 = max(0, min(1080 - w, x - w // 2))
    return (x0, y - up, x0 + w, y + down)


def make(out, name, box, frames, scale=1.0):
    ims = []
    for f in frames:
        im = Image.open(f).convert('RGB').crop(box)
        if scale != 1:
            im = im.resize((int(im.width * scale), int(im.height * scale)), Image.LANCZOS)
        ims.append(im.convert('P', palette=Image.ADAPTIVE, colors=200))
    ims[0].save(os.path.join(out, name), save_all=True, append_images=ims[1:], duration=33, loop=0, disposal=2)


if __name__ == '__main__':
    out = sys.argv[1] if len(sys.argv) > 1 else os.path.join(ROOT, '대기효과_시안')
    os.makedirs(out, exist_ok=True)
    SETS = {0: {'laser': -2.72, 'bolt': 2.72, 'rail': -4.3, 'drone': 4.3}, 1: {'hive': -2.72, 'missile': 2.72},
            # 2차 (11-16): s2 = 쾅쾅 대포 · 집중 빔 · 소용돌이포 · 불바퀴 / s3 = 찌릿 코일
            2: {'cannon': -2.72, 'focus': 2.72, 'vortex': -4.3, 'wheel': 4.3}, 3: {'tesla': -2.72}}
    want = set(sys.argv[2:]) or {n for t in SETS.values() for n in t}
    for st, tur in SETS.items():
        if not any(n in want for n in tur):
            continue
        for p in (0, 1):
            fr = sorted(glob.glob(os.path.join(SRC, f's{st}p{p}_*.png')))
            if not fr:
                sys.exit(f'촬영 그림 없음: s{st}p{p}')
            for name, wx in tur.items():
                if name in want:
                    make(out, f'{name}_p{p}_x2.gif', crop_box(wx), fr, 2)
            make(out, f'base{st}_p{p}.gif', (0, 1800, 1080, 2200), fr, .6)
    print('→', out)
