# -*- coding: utf-8 -*-
"""AdMob 번호 넣기 (11-21) — 사용자가 AdMob 에서 앱 · 보상형 광고 단위를 만든 뒤:
    PYTHONUTF8=1 python tools/set_admob.py ca-app-pub-XXXX~YYYY ca-app-pub-XXXX/ZZZZ
고치는 곳 2군데 (같은 값이어야 함):
  1) Earth_Defence/Assets/Scripts/Core/AdService.cs   AdIds.AppId · AdIds.Rewarded
  2) Earth_Defence/Assets/GoogleMobileAds/Resources/GoogleMobileAdsSettings.asset   adMobAndroidAppId (AAB 매니페스트에 들어감)
tools/aab_check.py 는 AdService.cs 를 읽어 확인함. 번호는 비밀번호가 아님(앱 안에 그대로 들어감)."""
import os, re, sys

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
CS = os.path.join(ROOT, 'Earth_Defence', 'Assets', 'Scripts', 'Core', 'AdService.cs')
ASSET = os.path.join(ROOT, 'Earth_Defence', 'Assets', 'GoogleMobileAds', 'Resources', 'GoogleMobileAdsSettings.asset')


def main():
    if len(sys.argv) < 3: print(__doc__); sys.exit(1)
    app, unit = sys.argv[1].strip(), sys.argv[2].strip()
    if not re.fullmatch(r'ca-app-pub-\d{16}~\d{10}', app): sys.exit('앱 ID 모양이 아님 (ca-app-pub-16자리~10자리): ' + app)
    if not re.fullmatch(r'ca-app-pub-\d{16}/\d{10}', unit): sys.exit('광고 단위 ID 모양이 아님 (ca-app-pub-16자리/10자리): ' + unit)
    if app.split('~')[0] != unit.split('/')[0]: sys.exit('앱 ID 와 광고 단위 ID 의 게시자 번호(pub-…)가 다름')
    s = open(CS, encoding='utf-8').read()
    s = re.sub(r'public const string AppId = "[^"]+";', f'public const string AppId = "{app}";', s, count=1)
    s = re.sub(r'public const string Rewarded = "[^"]+";', f'public const string Rewarded = "{unit}";', s, count=1)
    open(CS, 'w', encoding='utf-8').write(s)
    a = open(ASSET, encoding='utf-8').read()
    a = re.sub(r'adMobAndroidAppId: .*', 'adMobAndroidAppId: ' + app, a, count=1)
    open(ASSET, 'w', encoding='utf-8').write(a)
    print('넣음 — 앱', app, '· 보상형', unit)


if __name__ == '__main__':
    main()
