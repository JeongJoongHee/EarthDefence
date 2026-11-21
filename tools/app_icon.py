# -*- coding: utf-8 -*-
"""앱 아이콘 (11-21 비공개 테스트 준비 — 카치건 app_icon_gen.py 와 같은 4장, 그림은 게임 낙서풍 그대로)
  밤하늘 + 아래 큰 지구 + 지구 위에 선 대장 카피(기본 우주 대원, 총을 위로) + 떨어지는 불꼬리 운석
  ※ 방사형 햇살 줄무늬 안 씀 (카치건 때 사용자 요청 — 욱일기처럼 보임)
결과 (Unity 연결은 Editor/AppIconSetup.cs — 빌드 때 자동):
  Earth_Defence/Assets/Art/AppIcon/app_icon.png        512 정사각형 (플레이 스토어 아이콘 · 옛 안드로이드)
  Earth_Defence/Assets/Art/AppIcon/app_icon_round.png  512 동그라미
  Earth_Defence/Assets/Art/AppIcon/app_icon_bg.png     432 적응형 바탕 (하늘 + 지구)
  Earth_Defence/Assets/Art/AppIcon/app_icon_fg.png     432 적응형 앞 그림 (대장 카피 + 운석, 투명 — 가운데 66/108 안)
사용 (PowerShell — Edge 렌더): $env:PYTHONUTF8='1'; python tools/app_icon.py [미리보기 폴더]
"""
import os, sys, random
from PIL import Image, ImageDraw

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import earth_art as A
from earth_art import C, F, G, blur
import tower_art as TA

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
DST = os.path.join(ROOT, 'Earth_Defence', 'Assets', 'Art', 'AppIcon')
STORE, ADAPT = 512, 432


def sky(n):
    """밤하늘 (가운데 위 밝은 성운 → 가장자리 짙게) + 별"""
    s = f'<rect width="{n}" height="{n}" fill="{A.NIGHT}"/>'
    s += blur(C(n * .5, n * .32, n * .42, A.NEBULA, 0), n / 9, .85)
    s += blur(C(n * .78, n * .18, n * .16, '#6a5fa8', 0), n / 14, .55)
    rnd = random.Random(7)
    for _ in range(26):
        x, y, r = rnd.uniform(0, n), rnd.uniform(0, n * .62), rnd.uniform(n * .004, n * .009)
        s += f'<circle cx="{x:.1f}" cy="{y:.1f}" r="{r:.1f}" fill="#fff6d8" opacity="{rnd.uniform(.5, 1):.2f}"/>'
    for x, y, r in ((.14, .16, .028), (.86, .44, .022), (.24, .5, .018)):   # 반짝 별 (4갈래 — 줄무늬 아님)
        cx, cy, q = n * x, n * y, n * r
        s += F(f'M{cx} {cy - q} Q{cx + q * .18} {cy - q * .18} {cx + q} {cy} Q{cx + q * .18} {cy + q * .18} {cx} {cy + q} '
               f'Q{cx - q * .18} {cy + q * .18} {cx - q} {cy} Q{cx - q * .18} {cy - q * .18} {cx} {cy - q} Z', '#fff3c4')
    return s


def ground(n):
    """아래 큰 지구 (윗면이 n*0.66 쯤)"""
    return TA.earth(n * .5, n * 1.32, n * .68, sw=n / 70, seed=4)


def capy(n, h, foot_y, x=0.5):
    """대장 카피 (총을 위로 -55°) — 높이 h px, 발 = (n*x, foot_y)"""
    k = h / A.G_H
    return (f'<g transform="translate({n * x - A.G_CX * k:.1f},{foot_y - A.G_FOOT * k:.1f}) scale({k:.4f})">'
            f'{A.guardian_full_svg(-55)}</g>')


def meteor(n, cx, cy, size):
    """떨어지는 불꼬리 운석 (오른쪽 위 → 왼쪽 아래)"""
    k = size / 220
    return f'<g transform="translate({cx:.1f},{cy:.1f}) rotate(35) scale({k:.4f}) translate(-110,-110)">{A.m_fire()}</g>'


def full_svg(n):
    return sky(n) + ground(n) + meteor(n, n * .84, n * .14, n * .22) + capy(n, n * .52, n * .69, .46)


def render(svg, n):
    return A.R(svg, n, n, A.WOB['char']).convert('RGBA')


def circle_mask(size, rate=0.5):
    m = Image.new('L', (size * 4, size * 4), 0)
    r = size * 4 * rate
    ImageDraw.Draw(m).ellipse([size * 2 - r, size * 2 - r, size * 2 + r, size * 2 + r], fill=255)
    return m.resize((size, size), Image.LANCZOS)


def rounded_mask(size, rate=0.22):
    m = Image.new('L', (size * 4, size * 4), 0)
    ImageDraw.Draw(m).rounded_rectangle([0, 0, size * 4 - 1, size * 4 - 1], radius=int(size * 4 * rate), fill=255)
    return m.resize((size, size), Image.LANCZOS)


def main():
    os.makedirs(DST, exist_ok=True)
    full = render(full_svg(STORE), STORE)
    full.convert('RGB').save(os.path.join(DST, 'app_icon.png'))
    rnd = full.copy(); rnd.putalpha(circle_mask(STORE))
    rnd.save(os.path.join(DST, 'app_icon_round.png'))
    n = ADAPT
    bg = render(sky(n) + ground(n), n)
    bg.convert('RGB').save(os.path.join(DST, 'app_icon_bg.png'))
    # 적응형 앞 그림: 108dp 중 가운데 66dp(= 0.61) 원 안에 — 대장 카피 높이 0.4 · 발은 지구 윗면 근처
    fg = A.R(meteor(n, n * .66, n * .31, n * .11) + capy(n, n * .4, n * .7, .47), n, n, A.WOB['char']).convert('RGBA')
    fg.save(os.path.join(DST, 'app_icon_fg.png'))
    print('완료', DST)
    if len(sys.argv) > 1:
        out = sys.argv[1]; os.makedirs(out, exist_ok=True)
        S = 300
        sheet = Image.new('RGB', (S * 4 + 50 * 5, S + 260), (236, 236, 240))
        a = full.resize((S, S), Image.LANCZOS)
        r1 = a.copy(); r1.putalpha(rounded_mask(S))
        ad = bg.copy(); ad.alpha_composite(fg)
        vis = ad.crop((n // 6, n // 6, n * 5 // 6, n * 5 // 6)).resize((S, S), Image.LANCZOS)
        c1 = vis.copy(); c1.putalpha(circle_mask(S))
        sq = vis.copy(); sq.putalpha(rounded_mask(S, 0.3))
        for i, im in enumerate([a, r1, c1, sq]):
            sheet.paste(im, (50 + i * (S + 50), 30), im)
        x = 50
        for s in (144, 96, 72, 48):
            sm = c1.resize((s, s), Image.LANCZOS)
            sheet.paste(sm, (x, S + 60 + (144 - s)), sm)
            x += s + 30
        p = os.path.join(out, 'app_icon_preview.png'); sheet.save(p); print('미리보기', p)


if __name__ == '__main__':
    main()
