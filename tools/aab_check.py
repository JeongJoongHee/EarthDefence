# -*- coding: utf-8 -*-
"""AAB · APK 점검 (11-21 카치건 aab_check.py 를 가져옴) — 사용: PYTHONUTF8=1 python tools/aab_check.py [aab·apk 경로] [--prod]
패키지 이름 · 버전 코드 · 서명(jarsigner, 업로드 키인지) · 64비트 라이브러리 ·
광고 ID 권한(AD_ID) · 결제 권한(BILLING) · AdMob 앱 ID(실제 번호 — 구글 시험 번호면 실패) · 치트 상태
--prod = 프로덕션에 나갈 빌드 점검: 치트(Cheat_On)가 켜져 있으면 실패
(AAB 매니페스트 · 리소스는 protobuf, APK 매니페스트는 바이너리 XML(UTF-16) 이라 글자 조각을 찾아 확인)
AdMob 앱 ID 는 Earth_Defence/Assets/Scripts/Core/AdService.cs 의 AdIds.AppId 를 읽음 (tools/set_admob.py 가 넣음)"""
import csv, glob, os, re, subprocess, sys, zipfile

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
PROJ = os.path.join(ROOT, 'Earth_Defence')
JDK = 'C:/Program Files/Unity Hub/6000.0.84f1/Editor/Data/PlaybackEngines/AndroidPlayer/OpenJDK/bin'
PACKAGE = 'com.zkfpf.earthdefence'
TEST_APP_ID = 'ca-app-pub-3940256099942544~3347511713'
UPLOAD_SHA1 = '99:DA:70:F9:F1:4D:AE:DC:83:6E:29:B9:02:38:D8:0D:46:FA:F0:E9'   # 키 보관/upload.keystore (11-21 만듦)


def admob_app_id():
    s = open(os.path.join(PROJ, 'Assets', 'Scripts', 'Core', 'AdService.cs'), encoding='utf-8').read()
    return re.search(r'AppId = "([^"]+)"', s).group(1)


def cheat_on():
    for r in csv.reader(open(os.path.join(PROJ, 'Assets', 'Resources', 'Tables', 'Config.csv'), encoding='utf-8-sig')):
        if r and r[0] == 'Cheat_On': return r[1].strip() == '1'
    return False


def main():
    args = [a for a in sys.argv[1:] if not a.startswith('--')]
    prod = '--prod' in sys.argv
    p = args[0] if args else max(glob.glob(os.path.join(PROJ, 'Builds', '*.aab')), key=os.path.getmtime)
    apk = p.lower().endswith('.apk')
    z = zipfile.ZipFile(p)
    names = z.namelist()
    man = z.read('AndroidManifest.xml' if apk else 'base/manifest/AndroidManifest.xml')
    enc = 'utf-16-le' if apk else 'utf-8'
    has = lambda s, blob=man: s.encode(enc) in blob or s.encode('utf-8') in blob
    fails = []

    def line(name, good, extra=''):
        print(f'{name:<34}: {"통과" if good else "확인 필요"} {extra}'.rstrip())
        if not good: fails.append(name)

    app_id = admob_app_id()
    print('파일', os.path.basename(p), '%.1f MB' % (os.path.getsize(p) / 1048576))
    line('패키지 ' + PACKAGE, has(PACKAGE))
    line('광고 ID 권한 (AD_ID)', has('com.google.android.gms.permission.AD_ID'), '— Play Console 광고 ID 선언 = 예')
    line('결제 권한 (BILLING)', has('com.android.vending.BILLING'), '— 인앱 상품 메뉴가 열림')
    line('AdMob 앱 ID 가 실제 번호', app_id != TEST_APP_ID and has(app_id),
         app_id + (' (아직 구글 시험 번호 → python tools/set_admob.py)' if app_id == TEST_APP_ID else ''))
    text = man.decode(enc, errors='ignore') if apk else man.decode('latin-1')
    perms = sorted(set(re.findall(r'[a-z.]+\.permission\.[A-Z_]+', text)))
    print(f'{"권한 목록":<34}:', ', '.join(perms) or '없음')
    pre = 'lib/' if apk else 'base/lib/'
    libs = sorted({n[len(pre):].split('/')[0] for n in names if n.startswith(pre)})
    line('64비트 라이브러리만 (arm64-v8a)', libs == ['arm64-v8a'], ', '.join(libs))
    c = cheat_on()
    line('치트 (Cheat_On)', not (prod and c), ('켜짐 — 비공개 테스트용' if c else '꺼짐') + (' · --prod 점검이라 꺼야 통과' if prod and c else ''))
    r = subprocess.run([os.path.join(JDK, 'jarsigner.exe'), '-verify', '-verbose:summary', '-certs', p], capture_output=True, encoding='cp949', errors='replace')
    signed = 'jar verified' in r.stdout
    owner = [l.strip() for l in r.stdout.splitlines() if 'CN=' in l][:1]
    if apk:
        print(f'{"서명":<34}:', 'v1 서명 있음' if signed else 'v2 서명만(APK 정상)', owner[0] if owner else '')
    else:
        line('서명 (업로드 키 CN=Cachisi)', signed and bool(owner) and 'Cachisi' in owner[0], owner[0] if owner else '')
    print('\n결과:', '모두 통과' if not fails else '확인 필요 ' + str(len(fails)) + '개 — ' + ' · '.join(fails))
    sys.exit(1 if fails else 0)


if __name__ == '__main__':
    main()
