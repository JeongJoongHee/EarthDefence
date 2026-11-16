# -*- coding: utf-8 -*-
"""소리 검토 (11-16 저녁) — 귀로 못 들으니 재서 판단하고, 사용자가 들을 수 있는 HTML 을 만든다.

순서: bash tools/unity_batch.sh EarthDef.EditorTools.PlayTest.RunSound --play   (→ Earth_Defence/Logs/Sound/*.wav · events_*.json)
      PYTHONUTF8=1 python tools/sound_review.py [출력 폴더 = 소리_검토]
재는 것: 체감 크기(A 가중 RMS · 효과음은 가장 큰 50ms) · 꼭대기 · 폰 스피커 비율(400Hz 위 에너지) · 길이 · 반복 이음새 · 잔향 꼬리
장면: 로비 30초 · 전투 30초 · 보스 20초 = 게임과 같은 음량(배경음 Bgm_Volume x 설정 80% · 효과음은 기록된 음량)으로
      효과음은 게임처럼 전투 채널 10개만 (꽉 차면 건너뜀) · 높낮이 바꿈 · 배경음은 처음부터.
"""
import os, sys, json, wave, html, shutil
import numpy as np

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), '..'))
SRC = os.path.join(ROOT, 'Earth_Defence', 'Logs', 'Sound')
OUT = os.path.join(ROOT, sys.argv[1] if len(sys.argv) > 1 else '소리_검토')
SR = 44100
BGM_VOL, SFX_VOL, SET = 0.7, 0.75, 0.8          # Config Bgm_Volume x 설정 기본 80%
COMBAT = {'Gun', 'Rail', 'Laser', 'Field', 'Bee', 'Missile', 'Boom', 'BigBoom', 'Bolt', 'Focus', 'Vortex', 'Wheel', 'Tesla', 'Zap', 'Hit', 'Kill', 'BaseHit'}
KO = dict(UiClick='버튼', Gun='대장 카피 소총', Rail='관통포', Laser='레이저 포', Field='끈끈이 드론 장판', Bee='벌집 벌', Missile='유도 미사일',
          Boom='폭발', BigBoom='쾅쾅 대포 · 보스 처치', Bolt='벼락탑', Focus='집중 빔 (지글지글)', Vortex='소용돌이포', Wheel='불바퀴', Tesla='찌릿 코일 충전',
          Zap='번개 튐 · 작은 낙뢰', Hit='맞음', Kill='적 처치', BaseHit='기지가 맞음', LevelUp='레벨 업', CardPick='카드 고름', Break='돌파',
          Place='새 터렛 놓임', BossWarn='보스 등장', EliteWarn='중간 보스 등장', Victory='승리', Defeat='패배', Coin='보상 받기', Upgrade='강화',
          Merge='합성')


def read(p):
    with wave.open(p) as w:
        return np.frombuffer(w.readframes(w.getnframes()), np.int16).astype(np.float64) / 32767


def write(p, d):
    d = np.clip(d, -1, 1)
    with wave.open(p, 'wb') as w:
        w.setnchannels(1); w.setsampwidth(2); w.setframerate(SR)
        w.writeframes((d * 32767).astype(np.int16).tobytes())


def a_weight(f):
    f = np.maximum(f, 1e-3); f2 = f * f
    ra = (12194 ** 2 * f2 * f2) / ((f2 + 20.6 ** 2) * np.sqrt((f2 + 107.7 ** 2) * (f2 + 737.9 ** 2)) * (f2 + 12194 ** 2))
    return ra / 0.7943  # 1kHz = 1


def aw(d):
    """A 가중 신호 (FFT 로)"""
    n = len(d); F = np.fft.rfft(d); f = np.fft.rfftfreq(n, 1 / SR)
    return np.fft.irfft(F * a_weight(f), n)


def db(x):
    return 20 * np.log10(max(x, 1e-9))


def loud(d, short=False):
    a = aw(d)
    if not short:
        return db(np.sqrt(np.mean(a * a)))
    w = int(SR * 0.05); best = 0
    for i in range(0, max(1, len(a) - w), w // 2):
        best = max(best, np.sqrt(np.mean(a[i:i + w] ** 2)))
    return db(best)


def hi_ratio(d):
    P = np.abs(np.fft.rfft(d)) ** 2; f = np.fft.rfftfreq(len(d), 1 / SR)
    return P[f >= 400].sum() / max(P.sum(), 1e-12)


def hp400(d):
    """폰 스피커가 내는 부분만 (400Hz 아래를 뺌)"""
    F = np.fft.rfft(d); f = np.fft.rfftfreq(len(d), 1 / SR)
    return np.fft.irfft(F * (f >= 400), len(d))


def tail(d):
    """마지막으로 꼭대기의 -40dB 보다 큰 곳까지 (잔향 꼬리 포함 길이)"""
    pk = np.abs(d).max(); idx = np.where(np.abs(d) > pk * 0.01)[0]
    return (idx[-1] + 1) / SR if len(idx) else 0


def seam(d):
    """반복 이음새: 끝 → 처음 이어 붙인 곳의 샘플 차이를 곡 안의 보통 차이와 비교 (1 근처면 매끈)"""
    diffs = np.abs(np.diff(d)); typical = np.percentile(diffs, 99) + 1e-9
    return abs(d[0] - d[-1]) / typical


def mix_scene(bgm, events, sfx, sec):
    n = int(SR * sec); out = np.zeros(n); fx = np.zeros(n)
    b = np.tile(bgm, int(np.ceil(n / len(bgm))))[:n] * BGM_VOL * SET
    ends = []   # 전투 채널 끝나는 시각
    used = skipped = 0
    for e in events:
        t = e['t']
        if t < 0 or t >= sec:
            continue
        s = sfx.get(e['s'])
        if s is None:
            continue
        if e['s'] in COMBAT:
            ends = [x for x in ends if x > t]
            if len(ends) >= 10:
                skipped += 1; continue
        p = e['p']
        if abs(p - 1) > 1e-3:
            idx = np.arange(0, len(s) - 1, p); s = np.interp(idx, np.arange(len(s)), s)
        if e['s'] in COMBAT:
            ends.append(t + len(s) / SR)
        i = int(t * SR); j = min(n, i + len(s))
        fx[i:j] += s[:j - i] * e['v']; used += 1
    out = b + fx
    return out, b, fx, used, skipped


def main():
    os.makedirs(OUT, exist_ok=True)
    sfx = {}; rows = []
    for f in sorted(os.listdir(SRC)):
        if f.startswith('sfx_') and f.endswith('.wav'):
            name = f[4:-4]; d = read(os.path.join(SRC, f)); sfx[name] = d
            shutil.copy(os.path.join(SRC, f), os.path.join(OUT, f))
    order = list(KO.keys())
    vol = SFX_VOL * SET
    for n in order:
        if n not in sfx:
            continue
        d = sfx[n]
        rows.append(dict(name=n, ko=KO[n], len=len(d) / SR, tail=tail(d), loud=loud(d * vol, True), phone=loud(hp400(d) * vol, True), peak=db(np.abs(d).max() * vol), hi=hi_ratio(d)))
    bgm = {}
    brows = []
    for n, ko in (('lobby', '로비 "별빛 기지"'), ('battle', '전투 "꿈속 순찰"'), ('boss', '보스 "검은 행성"')):
        d = read(os.path.join(SRC, f'bgm_{n}.wav')); bgm[n] = d
        shutil.copy(os.path.join(SRC, f'bgm_{n}.wav'), os.path.join(OUT, f'bgm_{n}.wav'))
        g = d * BGM_VOL * SET
        brows.append(dict(name=n, ko=ko, len=len(d) / SR, loud=loud(g), phone=loud(hp400(g)), peak=db(np.abs(g).max()), hi=hi_ratio(d), seam=seam(d)))
    scenes = []
    for n, ev, sec, ko in (('battle', 'events_battle.json', 30, '전투 30초 (스테이지 8 · 대장 카피 + 관통포 · 레이저 · 벌집 · 쾅쾅 대포)'),
                           ('boss', 'events_boss.json', 20, '보스 20초 (보스 등장 → 보스전)')):
        events = json.load(open(os.path.join(SRC, ev), encoding='utf-8'))
        out, b, fx, used, skipped = mix_scene(bgm[n], events, sfx, sec)
        pk = np.abs(out).max()
        if pk > 1:
            out = out / pk * 0.98
        write(os.path.join(OUT, f'scene_{n}.wav'), out)
        scenes.append(dict(name=n, ko=ko, bgm=loud(b), fx=loud(fx), all=loud(out), peak=db(pk), used=used, skipped=skipped, n=len(events)))
    write(os.path.join(OUT, 'scene_lobby.wav'), np.tile(bgm['lobby'], 1)[:SR * 30] * BGM_VOL * SET)
    json.dump(dict(sfx=rows, bgm=brows, scenes=scenes), open(os.path.join(OUT, 'measure.json'), 'w', encoding='utf-8'), ensure_ascii=False, indent=1)
    for r in brows:
        print(f"배경음 {r['name']:7s} {r['len']:5.1f}초  체감 {r['loud']:6.1f}dB 폰 {r['phone']:6.1f}  꼭대기 {r['peak']:6.1f}  폰 {r['hi'] * 100:4.0f}%  이음새 {r['seam']:.2f}")
    for s in scenes:
        print(f"장면 {s['name']:7s} 배경음 {s['bgm']:6.1f}  효과음 {s['fx']:6.1f}  전체 {s['all']:6.1f}  꼭대기 {s['peak']:5.1f}  효과음 {s['used']}/{s['n']} (채널 꽉 차 건너뜀 {s['skipped']})")
    for r in rows:
        print(f"  {r['name']:9s} {r['len']:4.2f}초 꼬리 {r['tail']:4.2f}  체감 {r['loud']:6.1f} 폰 {r['phone']:6.1f}  꼭대기 {r['peak']:6.1f}  폰 {r['hi'] * 100:4.0f}%")
    page(rows, brows, scenes)
    print('→', OUT)


BEFORE_NOTE = {
    'Boom': '낮은 "쿵"만 있어 폰에서 거의 안 들림 → 가운데 소리를 섞음', 'BigBoom': '같은 이유로 가운데 소리를 섞음',
    'Field': '220Hz 웅 → 한 옥타브 위가 주인공', 'BaseHit': '먹먹한 "퉁"에 가운데 소리를 섞음',
    'BossWarn': '낮은 화음 → 한 옥타브 위가 주인공 (중간 보스 등장보다 작던 것)', 'EliteWarn': '한 옥타브 위로',
    'Missile': '쉬익을 조금 밝게', 'Wheel': '불길을 조금 크고 밝게', 'Defeat': '아래 화음을 한 옥타브 위로',
}


def page(rows, brows, scenes):
    """소리_검토.html (1차_수정안_결과_3.html 과 같은 틀 · 체크/의견 키 earthdef-sound-1116)"""
    tmpl = open(os.path.join(ROOT, '1차_수정안_결과_3.html'), encoding='utf-8').read()
    head = tmpl[:tmpl.index('</head>')].replace('<title>1차 수정안 3차 반영</title>', '<title>소리 검토</title>')
    script = tmpl[tmpl.index('<script>'):].replace('earthdef-fix3-1116', 'earthdef-sound-1116')
    d = os.path.basename(OUT)
    au = lambda f: f'<audio controls preload="none" src="{d}/{f}" style="height:32px;width:100%;max-width:330px"></audio>'
    E = html.escape
    sc = {x['name']: x for x in scenes}
    b = f'''<body><main>
<h1>소리 검토 — 잔잔하고 몽환적으로</h1>
<p class="note">2026-11-16 · 사용자 "카치건과 비슷한데 잔잔함에 약간 몽환적인 느낌" · 소리는 전부 코드 합성(<code>Core/Audio.cs</code>) · 아래 wav 는 게임이 실제로 만든 소리를 뽑은 것(<code>PlayTest.RunSound</code>) · 숫자는 <code>tools/sound_review.py</code> 가 잰 값</p>
<div class="bar"><span id="stat"></span><button id="copyNotes" type="button">의견 모아 복사</button></div>
<div class="tip blue"><b>어떻게 만들었나</b><ul>
<li><b>몽환</b> = 종소리 멜로디에 <b>메아리</b>(점4분 · 여러 번 되울림) + 곡 전체에 <b>큰 잔향</b> · 패드는 살짝 어긋난 두 음이 천천히 일렁임 · 반복 이음새는 두 바퀴 돌려 매끈하게 (이음새 값 0.03~0.15 = 끊김 없음)</li>
<li><b>잔잔</b> = 로비 68 BPM 드럼 없음 · 전투 84 BPM 아주 약한 킥 · 쉐이커 · 보스 96 BPM 단조 낮은 맥박 (카치건 전투 곡 126 BPM 보다 느림)</li>
<li><b>효과음</b> = 사인 · 삼각파로 둥글게, 톱니 · 거친 잡음은 줄임 · 레벨 업 · 돌파 · 승리는 종소리 + 잔향</li>
<li>카치건에서 배운 것: <b>폰 스피커는 400Hz 아래가 거의 안 나와서</b> 폭발 · 보스 등장처럼 낮은 소리는 한 옥타브 위 소리를 섞음 (아래 "고치기 전 → 뒤") · 전투 소리는 채널 10개까지 · 같은 소리 간격 제한</li>
<li>기본 음량: 배경음 <code>Bgm_Volume</code> 0.7 · 효과음 <code>Sfx_Volume</code> 0.75 (Config) x 설정 창 80%. 전투 장면에서 배경음과 효과음 체감 크기가 비슷({sc['battle']['bgm']:.1f} / {sc['battle']['fx']:.1f} dB)하게 맞춤</li>
</ul></div>
<h2>① 장면으로 듣기 <small>게임과 같은 음량 · 같은 순서</small></h2>
<table><tr><th>장면</th><th>듣기</th><th>배경음 / 효과음 체감</th></tr>
<tr><td>로비 30초</td><td>{au('scene_lobby.wav')}</td><td>배경음만</td></tr>
<tr><td>{E(sc['battle']['ko'])}</td><td>{au('scene_battle.wav')}</td><td>{sc['battle']['bgm']:.1f} / {sc['battle']['fx']:.1f} dB · 효과음 {sc['battle']['used']}번</td></tr>
<tr><td>{E(sc['boss']['ko'])}</td><td>{au('scene_boss.wav')}</td><td>{sc['boss']['bgm']:.1f} / {sc['boss']['fx']:.1f} dB · 효과음 {sc['boss']['used']}번</td></tr>
<tr><td>(고치기 전) 전투 30초</td><td>{au('before/scene_battle.wav')}</td><td>폭발 · 장판 소리가 폰에서 안 들리던 판 · 음량도 3dB 작음</td></tr>
</table>
<h2>② 배경음 3곡</h2>
<table><tr><th>곡</th><th>듣기</th><th>길이</th><th>체감 · 폰</th><th>이음새</th></tr>'''
    for r in brows:
        b += f"<tr><td>{E(r['ko'])}</td><td>{au('bgm_' + r['name'] + '.wav')}</td><td>{r['len']:.0f}초 반복</td><td>{r['loud']:.1f} · {r['phone']:.1f} dB</td><td>{r['seam']:.2f}</td></tr>"
    b += '</table><p class="note">로비 = D장조 Dmaj7 · Bm7 · Gmaj7 · Asus (드럼 없음) / 전투 = Bm · G · D · A + Em9 · F#m 구간 (유리 같은 분산화음 · 약한 킥) / 보스 = Bm · G · Em · F# 단조 (낮은 8분 맥박 · 북)</p>'
    b += '<h2>③ 효과음 29개</h2><table><tr><th>소리</th><th>어디서</th><th>듣기</th><th>길이(꼬리)</th><th>체감 · 폰</th><th>고치기 전</th></tr>'
    for r in rows:
        bf = os.path.join(OUT, 'before', f"sfx_{r['name']}.wav")
        bcell = (au('before/sfx_' + r['name'] + '.wav') + f"<br><small>{E(BEFORE_NOTE.get(r['name'], ''))}</small>") if os.path.exists(bf) else ''
        b += f"<tr><td>{r['name']}</td><td>{E(r['ko'])}</td><td>{au('sfx_' + r['name'] + '.wav')}</td><td>{r['len']:.2f}초 ({r['tail']:.2f})</td><td>{r['loud']:.1f} · {r['phone']:.1f}</td><td>{bcell}</td></tr>"
    b += '</table>'
    items = [
        ('s-feel', '확인', '<b>전체 느낌</b> — 잔잔하고 약간 몽환적인가 (장면 ① 부터)'),
        ('s-lobby', '확인', '<b>로비 곡</b> "별빛 기지" — 드럼 없는 느린 곡'),
        ('s-battle', '확인', '<b>전투 곡</b> "꿈속 순찰" — 너무 처지거나 졸리지 않은지'),
        ('s-boss', '확인', '<b>보스 곡</b> "검은 행성" — 긴장감이 충분한지 (단조 · 낮은 맥박)'),
        ('s-sfx', '확인', '<b>효과음</b> — 거슬리거나 너무 큰/작은 소리 (이름을 적어 주세요)'),
        ('s-vol', '확인', '<b>배경음 · 효과음 크기 비율</b> — 지금은 전투에서 비슷하게. 설정 창에서 각각 10% 씩 바꿀 수 있음'),
    ]
    b += '<h2>④ 들어 보시고 <small style="font-size:14px;color:#7a6a5c;font-weight:normal">✎ 의견 칸</small></h2><ul class="todo">'
    for i, t, body in items:
        b += f'<li data-id="{i}"><label><input type="checkbox"><span><span class="tag">{t}</span>{body}</span></label></li>'
    b += '</ul></main>\n'
    open(os.path.join(ROOT, '소리_검토.html'), 'w', encoding='utf-8').write(head + '</head>' + b + script)


if __name__ == '__main__':
    main()
