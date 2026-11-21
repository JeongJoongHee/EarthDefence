# -*- coding: utf-8 -*-
"""플레이 스토어 기본 스토어 등록정보용 그림 (11-21, 카치건 store_graphics.py 를 가져옴)

  PYTHONUTF8=1 python tools/store_graphics.py

  결과 = 스토어 등록/
    앱아이콘_512.png           — Earth_Defence/Assets/Art/AppIcon/app_icon.png 복사 (tools/app_icon.py)
    그래픽이미지_1024x500.png  — Ch.2 전투 화면 띠 + 대장 카피 · 보스 + 게임 글꼴 제목
    스크린샷_01~08.png         — Logs/PlayTest/store 캡처(1080x2340)를 1080x1920 에 문구와 함께 (구글: 긴 변 ≤ 짧은 변 x2)
  캡처 다시 찍기 = bash tools/unity_batch.sh EarthDef.EditorTools.PlayTest.RunStore --play (Unity 닫고)
  주아 글꼴에 가운뎃점(·) · 말줄임(…) 이 없음 → 문구에 쓰지 말 것
"""
import os, shutil
from PIL import Image, ImageDraw, ImageFont, ImageFilter

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
GAME = os.path.join(ROOT, 'Earth_Defence')
SHOT_DIR = os.path.join(GAME, 'Logs', 'PlayTest', 'store')
ART = os.path.join(GAME, 'Assets', 'Resources', 'Art')
FONT = os.path.join(ROOT, 'tools', 'fonts', 'Jua-Regular.ttf')
OUT = os.path.join(ROOT, '스토어 등록')

SHOTS = [
    ('01_battle', '운석과 외계 벌레가 쏟아진다!', '돔 기지를 지키는 타워 디펜스'),
    ('02_boss', '행성마다 다른 거대 보스', '5개 챕터 100 스테이지'),
    ('03_cards', '카드 3장 중 1장, 나만의 빌드', '같은 터렛을 모으면 별이 쌓여요'),
    ('04_skill', '대장 카피 버닝!', '특수 능력으로 위기를 뒤집자'),
    ('05_gear', '장비 6부위와 회로', '모을수록 강해지는 대장 카피'),
    ('06_looks', '코스튬 27종, 탈것 16종', '나만의 대장 카피 꾸미기'),
    ('07_turret', '터렛 12종을 키워요', '레이저 포부터 소용돌이포까지'),
    ('08_story', '달토끼와 함께하는 우주 모험', '보스가 말을 거는 스토리 102편'),
]
CREAM = (255, 244, 214)
GOLD = (255, 205, 80)
SKY = (150, 220, 255)
INK = (40, 26, 18)


def font(size):
    return ImageFont.truetype(FONT, size)


def text_outline(d, xy, s, f, fill, outline=INK, w=6, anchor='mm'):
    x, y = xy
    for dx in range(-w, w + 1, 2):
        for dy in range(-w, w + 1, 2):
            if dx * dx + dy * dy <= w * w: d.text((x + dx, y + dy), s, font=f, fill=outline, anchor=anchor)
    d.text((x, y), s, font=f, fill=fill, anchor=anchor)


def screenshot(name, l1, l2):
    """1080x1920: 위 문구 띠 + 아래 게임 화면(가운데), 양옆은 같은 화면을 흐리게"""
    src = Image.open(os.path.join(SHOT_DIR, name + '.png')).convert('RGB')
    W, H, TOP = 1080, 1920, 300
    bg = src.resize((W, int(src.height * W / src.width))).crop((0, 0, W, H)).filter(ImageFilter.GaussianBlur(28))
    bg = Image.blend(bg, Image.new('RGB', bg.size, (22, 24, 50)), 0.55)
    gh = H - TOP - 30
    g = src.resize((int(src.width * gh / src.height), gh), Image.LANCZOS)
    canvas = bg.copy()
    gx = (W - g.width) // 2
    sh = Image.new('RGBA', (g.width + 40, g.height + 40), (0, 0, 0, 0))
    ImageDraw.Draw(sh).rounded_rectangle([20, 20, g.width + 20, g.height + 20], 26, fill=(0, 0, 0, 170))
    canvas.paste(sh.filter(ImageFilter.GaussianBlur(12)), (gx - 20, TOP - 6), sh.filter(ImageFilter.GaussianBlur(12)))
    m = Image.new('L', g.size, 0)
    ImageDraw.Draw(m).rounded_rectangle([0, 0, g.width - 1, g.height - 1], 24, fill=255)
    canvas.paste(g, (gx, TOP), m)
    d = ImageDraw.Draw(canvas)
    text_outline(d, (W // 2, 112), l1, font(80), GOLD, w=7)
    text_outline(d, (W // 2, 214), l2, font(54), CREAM, w=5)
    return canvas


def trim(im):
    return im.crop(im.getbbox())


def feature_graphic():
    W, H = 1024, 500
    src = Image.open(os.path.join(SHOT_DIR, '01_battle.png')).convert('RGB')
    band = src.resize((W, int(src.height * W / src.width)), Image.LANCZOS)
    y0 = int(band.height * 0.30)
    canvas = band.crop((0, y0, W, y0 + H)).filter(ImageFilter.GaussianBlur(3))
    canvas = Image.blend(canvas, Image.new('RGB', canvas.size, (18, 20, 44)), 0.45).convert('RGBA')
    shade = Image.new('L', (W, H), 0)
    for x in range(W):
        a = int(max(0, min(1, 1 - (x - 80) / 520)) * 150)
        ImageDraw.Draw(shade).line([(x, 0), (x, H)], fill=a)
    canvas.alpha_composite(Image.merge('RGBA', (*Image.new('RGB', (W, H), (0, 0, 0)).split(), shade)))
    sc = lambda im, hh: im.resize((int(im.width * hh / im.height), hh), Image.LANCZOS)
    boss = sc(trim(Image.open(os.path.join(ART, 'Enemy', 'frost_emperor.png')).convert('RGBA')), 300)
    canvas.alpha_composite(boss, (W - boss.width - 20, 18))
    for k, (eid, x, y, h) in enumerate((('fire', 600, 40, 120), ('moth', 690, 250, 95), ('jelly', 560, 300, 90))):
        e = sc(trim(Image.open(os.path.join(ART, 'Enemy', eid + '.png')).convert('RGBA')), h)
        canvas.alpha_composite(e, (x, y))
    capy = sc(trim(Image.open(os.path.join(ART, 'Char', 'guardian_ui.png')).convert('RGBA')), 300)
    canvas.alpha_composite(capy, (470, H - capy.height - 10))
    d = ImageDraw.Draw(canvas)
    text_outline(d, (255, 160), '카피바라', font(92), GOLD, w=8)
    text_outline(d, (255, 265), '지구를 지켜라', font(76), SKY, w=8)
    text_outline(d, (255, 365), '우주 타워 디펜스', font(42), CREAM, w=5)
    return canvas.convert('RGB')


def main():
    os.makedirs(OUT, exist_ok=True)
    shutil.copy(os.path.join(GAME, 'Assets', 'Art', 'AppIcon', 'app_icon.png'), os.path.join(OUT, '앱아이콘_512.png'))
    feature_graphic().save(os.path.join(OUT, '그래픽이미지_1024x500.png'))
    n = 0
    for i, (name, l1, l2) in enumerate(SHOTS, 1):
        p = os.path.join(SHOT_DIR, name + '.png')
        if not os.path.exists(p):
            print('캡처 없음 — 건너뜀:', name); continue
        screenshot(name, l1, l2).save(os.path.join(OUT, '스크린샷_%02d.png' % i)); n += 1
    print('완료 — 아이콘 · 그래픽 이미지 · 스크린샷', n, '장 →', OUT)


if __name__ == '__main__':
    main()
