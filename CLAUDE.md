# 카피바라 지구를 지켜라! (폴더·코드 이름 Earth_Defence)

## ▶ 재개 메모 (2026-11-17 밤 — 치트 · 코어 그림 · 요청 사항_05)
- 치트 = UI/Cheat.cs (설정 창 맨 아래 [치트] — 에디터 · 개발용 빌드 · Config Cheat_On 1). 장비(등급 · 부위) · 회로 · 터렛 조각 · 강화 부품. **스토어 빌드 전 Cheat_On 0 확인**.
- 코어 그림 = gear_tiers.core (세로 에너지 캡슐, 육각 부품과 구분). 빈 칸 g_core 도 같은 그림.
- 요청 = `요청 사항/요청 사항_05.pptx` (슬라이드 6장 · 그림에 글 상자로 지시). 보고 = `요청05_결과.html` (키 earthdef-req5-1117, 캡처 `캡쳐본 모음/요청05_캡처/`). 시험 120 + 13 + 78 오류 0. 표 = tools/fix5_tables.py.
- 바뀐 것: 토스트 글자만 · 아래 탭 판 안 · 적 약점 → [적 정보] 안(스테이지 칸 300) · 터렛 정보 칸(능력 · 설명 · 돌파, Lobby.Section) · 장비 능력치 판 아래 3x2 · 부품 "강화 재료" · 부위 창 다시 배치 · GearTab.Equipped(착용되었습니다) · ChipConfirm(확인/취소) · 상점 광고 제거(Product Kind noads, iapOnce "noads", Store.NoAds) · parts_box · shard_box(하루 10) · 큰 골드 · Lv 표시 UI/badge(돌파 단계 붉게) · 마지막 보석 gem_red_* · 돌파 카드 붉은 GlowPulse.

## (지난) 재개 메모 (2026-11-17 — 4차 수정안 21개 반영)
- 요청 = `요청 사항/4차 수정안.docx`. 보고 = `4차_수정안_결과.html` (키 earthdef-req4-1117, 캡처 `캡쳐본 모음/4차_수정안_캡처/`, 전 = before_*.png). 시험 120 + 13 + 67 오류 0. 결정 대기 r4-art.
- 표 = tools/fix4_tables.py (회로 이름 짧게 · 장비 이름 = 등급 이름 + 그림 이름 · Chip_Prefix 비움 · Config Attend_Auto 1 · Patrol_Ad_Mul 2). 아이콘 = tools/gear_tiers.py (등급마다 모양 늘어남, 224px, `TIERS_ONLY=1` 이면 등급 아이콘만).
- 코드: UIKit.SoftOutline(8방향 글자 테두리 — Unity Outline 은 4방향이라 찢겨 보였음) · Art.Soft 진하게 · 팝업 제목 띠 90 · Toast.Show(s, life) 는 로비 전투 탭이면 Lobby.ToastSpot(스테이지 칸 ↔ [전투 시작] 사이) · 탭 7개(Tabs 끝에 "looks", 보이는 순서 TabOrder) · Looks.Build(탭 화면) · 순찰은 [전투 시작] 아래 + [광고 2배] · 출석 자동 받기(Intro) · Lobby.LvTable · GearTab.GearUpPopup · ChipWindow 새 모양 · Gear.StatName 끝 "-" 뗌(StatVal reduce 가 "-6").
- 해석(보고 첫머리): 출석 = 자동 받고 토스트 · 순찰도 같은 자리 토스트 · 등급 통일 = 장비 이름 앞 말을 등급 이름으로.

## (지난) 재개 메모 (2026-11-17 — 3차 수정안 10개 반영)
- 요청 = `요청 사항/3차 수정안.docx`. 보고 = `3차_수정안_결과.html` (키 earthdef-req3-1117, 캡처 `캡쳐본 모음/3차_수정안_캡처/`, 전·뒤 = before_*.png). 시험 120 + 13 + 54 오류 0. 결정 대기 r3-art (아트 퀄업 더 할 화면).
- 표 = tools/fix3_tables.py (칩 → 회로 · 스킨 → 외형 · Gear.xlsx Gear_Name 부위x등급 이름 · Gear_Grade Chip_Prefix). 코드: GearItem.Name/Icon(g_<부위>_<등급>) · ChipItem.Name/Icon(chip_<등급>) · Art.TurretIcon(외형 끼우면 외형 그림) · GearTab.ChipPlace(착용 안내 창) · 작은 회로 칸 UI/minislot · Looks.Open(외형 메뉴, 로비 위 줄 붓 단추, 상점·터렛 탭에선 뺌) · GlowPulse(영웅 이상 획득 빛, UI/glow_frame).
- 그림: earth_export.py GRADE_PAL · grade_icon · gq_* (icon) · card() 명암 + 윤기 (ui). 고치기 전 UI 그림은 세션 스크래치 ui_before (지워질 수 있음).
- 시험 고침: 소용돌이포 필살기가 가끔 0번 → 시험에서 사거리 안에 적을 둠.

## (지난) 재개 메모 (2026-11-17 — 2차 수정안 17개 반영)
- 요청 = `요청 사항/2차 수정안.docx`. 보고 = `2차_수정안_결과.html` (키 earthdef-req2-1117, 캡처 `캡쳐본 모음/2차_수정안_캡처/`). 시험 120 + 13 + 47 오류 0.
- 표 고치기 tools/fix2_tables.py · 아이콘 입체감 = earth_export.py pop3d (FLAT_ICONS 는 뺌) · 장비 그림 gx_* · 달토끼 earth_art.moon_rabbit + World · 돌파 붉은색 = SpriteHue _SetHue (Config Break_Hue · Break_Sat) · 보스 크기 Config Elite_Size_Mul 3 · Boss_Size_Mul 3.5.
- 해석: "기지 끼움 – 변경" = 외형 단추 "변경 / 사용 중" · 돌파 붉은색이 스킨 색보다 먼저.

## (지난) 재개 메모 (2026-11-17 — 3차 구현 끝)
- 사용자 "동료 액티브 스킬 빼고 3차 (동료는 출시 뒤)". 끝낸 것: 다이아 · 기지 외형 6종(Frame.xlsx, 효과 부활 · 별똥별 · 얼림 · 반격 · 회복 · 골드, 별 1~5) · 터렛 스킨 11종(Skin.xlsx, 셰이더 SpriteHue · UI 그림 tools/skin_preview.py · 필살기 · 피해 +10%) · 도전 탭(무한 모드 · 엘리트 군단, 하루 입장) · 상점 탭(Shop.xlsx 다이아 [결제 초안] · 상자 확률 · 무료/광고 상자 · 스태미나 · 골드 · 스킨 · 외형) · 결과 창 광고 2배 [초안] · 설정 구글 계정 · 클라우드 [초안]. 표 = tools/phase3_tables.py. 코드 Game/Store.cs · UI/ShopTab.cs(+ChallengeTab).
- 시험: 기존 120줄 · 시험장 13줄 · 3차 37줄(`PlayTest.RunP3`) 모두 오류 0. 캡처 `3차_캡처/`.
- 보고 = `3차_구현_결과.html` (키 earthdef-3rd-1117, 캡처 `캡쳐본 모음/3차_캡처/`). 다음 추천 = 밸런스 시뮬레이터 → 영어 → 출시 준비 (결정 t-next 대기).
- 폴더: 사용자가 끝난 보고서는 `완료 사항 모음_HTML/`, 캡처는 `캡쳐본 모음/` 로 옮김 → 새 보고서는 루트, 캡처는 `캡쳐본 모음/<이름>/` 에. 옮긴 문서 링크는 ../ 로 고쳐 둠.

## (지난) 재개 메모 (2026-11-16 밤 — 2차 의견 반영 2번째 + 터렛 시험장)
- 보고 = `2차_반영2_결과.html` (키 earthdef-fb3-1116, 캡처 `2차_반영2_캡처/`). 소리는 사용자가 나중에 체크 (`소리_검토.html` 그대로). 시험 120줄 + 시험장 13줄 오류 0.
- 반영: 로비(약점 → 보통/엘리트 → 나오는 적 접기 · 순찰/전투 시작은 돔 꼭대기 Config Dome_Top_Y 위) · 설명 창 작게 · 속성 태그 글자 가운데 · 단계 표시 "Lv.N + 보석"(Icon gem_on/off) · 고른 카드 창 구분선.
- **터렛 시험장** = 씬 `Assets/Scenes/TurretLab.unity` (Game/Lab.cs 규칙 · Game/LabBoot.cs · UI/LabUI.cs, 메뉴 지구 방위대 › 터렛 시험장 열기). 시험 `bash tools/unity_batch.sh EarthDef.EditorTools.PlayTest.RunLab --play`. 씬에 붙는 MonoBehaviour 는 파일 이름 = 클래스 이름 (Lab.cs 안에 LabBoot 를 두었다가 "스크립트 없음"이 났음).
- Turret.xlsx: 사용자가 고친 관통포 Speed 18 · 찌릿 코일 Duration 1 은 유지, Unlock 만 5~25 로 되돌림 (사용자 "Unlock 부분만 원상 복구 · 나머지는 그대로").
- push 는 자동 승인에서 막힘 → 끝날 때 사용자에게 `! git push`.

## (지난) 재개 메모 (2026-11-16 저녁 — 2차 의견 반영 + 소리)
**사용자 "2차 결과 확인하고 소리 작업 — 카치건과 비슷한데 잔잔 · 약간 몽환"** → 의견 14개 반영 + 소리. 보고 = `2차_반영_결과.html` (키 earthdef-fb2-1116, 캡처 `2차_반영_캡처/`) + `소리_검토.html` (키 earthdef-sound-1116, wav `소리_검토/`, 고치기 전 `소리_검토/before/`). 시험 120줄 오류 0. 커밋 ffdc049.
- 소리 = `Core/Audio.cs` 코드 합성: 배경음 로비 68 · 전투 84 · 보스 96 BPM (패드 + 첼레스타 + 메아리 + 잔향, 두 바퀴로 이음새 없음) · 효과음 29. 음량 Config Bgm_Volume 0.7 · Sfx_Volume 0.75 x 설정. 뽑기 `bash tools/unity_batch.sh EarthDef.EditorTools.PlayTest.RunSound --play` → `PYTHONUTF8=1 python tools/sound_review.py` (측정 + 소리_검토.html 다시 만듦). 폰 스피커는 400Hz 아래 안 들림 → 낮은 소리는 한 옥타브 위를 섞음.
- 판단(보고 첫머리): Turret.xlsx Unlock 이 0 으로 바뀌어 있어 5~25 로 되돌림 · Stamina_Max 30 → Base 30 · Per_Stage 1 · Cap 80 · "열린다는 멘트 제거" = 전투 탭 "다음 터렛" 칸만 뺌.
- **GitHub**: origin = JeongJoongHee/EarthDefence · 첫 push 는 사용자가 함(11-16 저녁, master 추적). 내 push 는 자동 승인에서 막힘 → 끝날 때 사용자에게 `! git push` 부탁 (허용하려면 settings 에 git push 규칙).
- 다음 = 두 문서 의견 대기 → 그다음 추천 3차 (기지 외형 · 동료 스킬 · 스킨 · 무한 모드 · 상점 · 광고 · 결제 [초안] · 구글 로그인) → 밸런스.

## (지난) 재개 메모 (2026-11-16 오후 — 2차 구현 끝)
**사용자 "2차 작업 진행해줘" → 2차 끝.** 보고 = `2차_구현_결과.html` (키 earthdef-2nd-1116, 캡처 `2차_캡처/`, 대기 효과 GIF `2차_캡처/idle/`) — 의견 읽기 `PYTHONUTF8=1 python tools/todo_checks.py 2차_구현_결과.html`. 자동 시험 112줄 오류 0. 커밋 1fe21ee.
- **git 만들었음** (로컬, 커밋은 내가). 첫 커밋 b7a8ff4 = 2차 전 세이브 포인트. GitHub 은 결정 대기(p-github).
- 2차 = 터렛 5종(쾅쾅 대포 · 집중 빔 · 소용돌이포 · 불바퀴 · 찌릿 코일, 스테이지 5·10·15·20·25 로 열림) · 카드 89장 · 장비 6부위 7등급 · 칩 68 · 엘리트 난이도 · 스테이지 100 · 스태미나 · 출석 7일 · 일일 미션 7 · 설정 · 도움말 6 · 로비 탭 4개.
- 보고 첫머리 해석: 소리 아직 안 함(다음 추천) · 다이아는 3차 상점 때(칩 칸은 스테이지로 열림 · 합성은 같은 것 3개) · 장비 특수 스킬은 칩이 맡음 · 스태미나 60/5/8/6분 처음 값 · 출석은 첫날부터.
- 결정 대기: p-github · p-next(추천 ① 소리 → ② 3차 → 밸런스). 재개 첫 질문: "2차 보셨나요? 다음은 소리부터 할까요?"
- 2차 주의: 표는 `tools/phase2_tables.py`(없는 줄·칸만 더함) · 로비 Intro(출석 → 새 터렛 알림 → 도움말)는 시험에서 `Lobby.NoIntro` 로 끔 · `Popup.CloseAll` 은 닫기 뒤 할 일을 부르지 않음 · 쾅쾅 대포 · 소용돌이포는 사거리가 짧아 시험에서 사거리 안에 적을 둠.

## (지난) 재개 메모 (2026-11-16 마무리)
**오늘 끝낸 것 (전부 사용자 확인 "문제없음")**: 1차 수정안(아트 퀄업) 1~3차 + 터렛 대기 효과 6종. 마지막 자동 시험 오류 0.
- 대장 카피 = 카치건 몸 + 우주복 · 0.75배 · 총이 따로 돌아 적을 겨눔 · 무게감 이동 · 쏠 때 몸 안 튐
- 네온 탄 · 레이저 · 낙뢰(끝 가늘게) · 미사일 · 불꽃 튐 · 총구 섬광 / 관통탄 크고 주황 / 작은 레이저 · 소형 미사일 · 벌은 각 터렛에서 / 피해 숫자 카치건 방식 / 적은 위쪽 UI 아래에서
- 터렛 대기 효과 6종(벼락 치직 · 레이저 빛 고리 · 관통포 바퀴 0.5배 · 드론 프로펠러 · 벌집 둘레 벌 · 미사일 재장전) · 터렛 아래 빛 고리는 뺌(놓일 때 톡 커짐은 유지)

**다음 할 일 (사용자 답 대기 — `1차_구현_결과.html` 결정 칸이 아직 비어 있음)**
1. f-git: git 저장소 만들기 (추천: 로컬 git + 커밋은 내가, GitHub 비공개는 사용자 확인) — **아직 git 없음, 오늘 변경도 커밋 안 됨**
2. f-next: 다음 순서 추천 = ① 소리(배경음 · 효과음 · 보스 곡, 1차 범위였는데 못 함) → ② 2차(장비 · 칩 · 엘리트 난이도 · 스테이지 100 · 스태미나 · 나머지 터렛 6종 · 출석 · 설정 · 튜토리얼). 밸런스는 맨 마지막
3. 재개 첫 질문: "git 만들까요? 다음은 소리부터 할까요?" (+ `1차_구현_결과.html` 의 f-play · f-cards 등 확인 칸)

**오늘 만든 도구 · 주의**
- Unity 가 열려 있으면 배치가 막힘 → `bash tools/compile_check.sh`(컴파일만) · 기다릴 땐 "Unity 프로세스가 15초 동안 없음"으로 (창 제목은 스크립트 다시 읽는 중 바뀜)
- 빛 더하기 셰이더 `Resources/Shaders/SpriteAdd.shader` = UnitySprites.cginc SpriteVert (Unity 6 은 SpriteRenderer 색 · 뒤집기를 _RendererColor · _Flip 으로 넘김 — 꼭짓점 색만 읽으면 빛이 다 흰색). 빛 더하기는 밝은 바탕(풀밭 · 기지)에서 하얗게 타니 빛은 조각 뒤에 · 장판 · 터렛 고리는 보통 그리기
- 대기 효과 촬영 `PlayTest.RunIdle` → `PYTHONUTF8=1 python tools/idle_gif.py [출력] [터렛…]` · 그림 전·뒤 `tools/art_compare.py`
- 대장 카피 값: `earth_export.py char` 가 찍는 숫자 → Art.cs GuardianFoot/Hand · GunPivot/Muzzle
- Config 새 값: Hud_H · Spawn_Below_Hud · Guardian_Smooth · Idle_Fx

<details><summary>11-16 세부 (단계별)</summary>

###  (2026-11-16 밤 — 터렛 대기 효과 2단계 · 셰이더 색 고침)
- 보고 = `터렛_대기_효과_2단계.html` (키 earthdef-idle2-1116, GIF `대기효과_시안/`). 6종 모두 대기 효과(관통포 바퀴 · 드론 프로펠러/둥실/눈 · 벌집 둘레 벌 · 미사일 재장전) · 터렛 아래 빛 고리 뺌(놓일 때 톡 커짐은 유지).
- **셰이더 고침**: SpriteAdd 가 꼭짓점 색만 읽어 Unity 6 의 _RendererColor · _Flip(SpriteRenderer.color · flip)을 못 받았음 → 네온 빛이 전부 흰색 · 사라지기 없음이었음. UnitySprites.cginc SpriteVert 로 고침 + 시험 검사 줄. 확인받은 밝기(투명도 1)에 맞춰 낙뢰 · 레이저 투명도 다시 올림.
- 의견 반영: 관통포 바퀴 0.5배 · 드론 프로펠러 = 관통포와 같은 빠르기(TurretIdle.SpinDeg) · 벌집 · 미사일 · 색 네온은 문제없음 → 터렛 대기 효과 끝.
- 촬영 묶음: s0 = 레이저 · 벼락 · 관통포 · 드론 / s1 = 벌집 · 미사일 (`PlayTest.RunIdle` → `tools/idle_gif.py`).

###  (2026-11-16 밤 — 터렛 대기 효과 1단계 시안)
- 질문 "터렛이 작동하는 느낌" → `터렛_대기_효과_검토.html`(가능 · 방법 · 6종 안) → "진행해줘" → 1단계 시안 = 벼락탑 · 레이저 포. 보고 = `터렛_대기_효과_시안.html` (키 earthdef-idle1-1116, GIF `대기효과_시안/`). 다음 = s-next(2단계: 관통포 바퀴 · 드론 프로펠러 · 벌집 둘레 벌 · 미사일 재장전).
- 구조: 터렛 그림을 받침(Turret/<id>_base) + 조각(같은 캔버스 · 같은 피벗)으로 나눔(earth_art t_laser_base/crystal · laser_ring · t_bolt_base/orb · spark_arc) → `Game/TurretIdle.cs` 가 코드로 움직임 · 세기 = 쉬는 동안 0.4 → 충전(TurretUnit.Charge01)으로 0.85 → 쏠 때 1 · Config Idle_Fx(0 = 끄기).
- 촬영: `bash tools/unity_batch.sh EarthDef.EditorTools.PlayTest.RunIdle --play` → `PYTHONUTF8=1 python tools/idle_gif.py` (끔/켬 3초씩 GIF). 빛 더하기 빛은 조각 뒤에 둘 것(앞이면 하얗게 탐).
- Unity 가 열려 있는지 기다릴 땐 창 제목이 아니라 "프로세스가 15초 동안 없음"으로 (스크립트 다시 읽기 중 제목이 바뀌어 잘못 시작했었음).

###  (2026-11-16 저녁 — 1차 수정안 3차 반영 끝)
- 보고 = `1차_수정안_결과_3.html` (키 earthdef-fix3-1116, 캡처 `1차_수정3_캡처/`) — 의견 읽기 `PYTHONUTF8=1 python tools/todo_checks.py 1차_수정안_결과_3.html`.
- 3차: 대장 카피 0.75배(earth_export GK 2.4 → 1.8, Art.cs GuardianHand/GunMuzzle 도 같이) · 관통탄 1.4배 주황 창(SHOT_HEAD shell 176/232) · 벼락 양 끝 가늘게(lightning taper) · 벌은 벌집에서(Combat.HiveHead) · 터렛 아래 속성 색 빛 고리 Fx/pad(보통 그리기 — 빛 더하기는 풀밭에서 하얗게 탐) + 놓일 때 톡 커짐·링·불꽃(TurretUnit.MakePad/Update). 시험 오류 0.

###  (2026-11-16 오후 — 1차 수정안 의견 7개 반영 끝)
- 보고 = `1차_수정안_결과_2.html` (키 earthdef-fix2-1116, 캡처 `1차_수정2_캡처/`) — 의견은 `PYTHONUTF8=1 python tools/todo_checks.py 1차_수정안_결과_2.html`. 결정 대기: y-bee(벌도 벌집에서 나가게?).
- 대장 카피 = 카치건 몸(BODY_D) + 우주복 · 총은 따로(Char/guardian_gun, 피벗 = 뒤 손) → TurretUnit.AimStep 이 적 쪽으로 돌림 · 몸 뒤집기 · 높이 겨누면 뒤로 젖힘(최대 14°, 총은 몸 기준 62°까지 = 주둥이 안 가림) · 쏠 때 몸 반동 없음(총만) · 이동 SmoothDamp(Config Guardian_Smooth). 카드용 한 장 = Char/guardian_ui. 값은 `earth_export.py char` 가 찍는 숫자를 Art.cs Guardian*/Gun* 에.
- 작은 레이저 = 레이저 포에서(Combat hit_laser) · 소형 미사일 = 미사일 터렛에서 · 피해 숫자 = 카치건 방식(Fx.Number key 합치기, 치명 붉은 주황, "치명!" 글자 뺌) · 적 = 위쪽 UI 아래(World.PlayTop, Config Hud_H · Spawn_Below_Hud).
- Unity 가 열려 있으면 `bash tools/compile_check.sh` (카치건 것 복사) 로 컴파일만.

###  (2026-11-16 — 1차 수정안 아트 퀄업 끝)
- 요청 = `요청 사항/1차 수정안.docx` (대장 카피 자세 · 탄 네온 · 미사일 · 전체 마감). 보고 = `1차_수정안_결과.html` (키 earthdef-fix1-1116, 전·뒤 그림 `1차_수정_캡처/`) — 의견은 `PYTHONUTF8=1 python tools/todo_checks.py 1차_수정안_결과.html`.
- 대장 카피: 옆모습 + 레이저 소총 오른쪽 위(로컬 260x300 · 발 y 292 · 몸 x 108 · 총구 GUN_TIP 하나) → 적이 왼쪽이면 flipX (TurretUnit.faceLeft). Art.cs GuardianFoot/Muzzle 값은 earth_art 의 G_* 와 같이 바꿀 것.
- 네온: `Resources/Shaders/SpriteAdd.shader`(빛 더하기, Art.Additive) · Fx.Glow/Sparks/Muzzle · 탄 그림은 꼬리 포함 → 머리 피벗(Art.ShotPivot = earth_export SHOT_HEAD) · 장판은 빛 더하기 빼야 함(겹치면 하얗게 탐).
- 마감: earth_art `WOB`(흔들림 줄임) · `G()` 명암 그라데이션 · `blur()` 빛 번짐 · 그림 전·뒤 모음 `tools/art_compare.py`. 고치기 전 백업 = 세션 스크래치 before_1116 (지워질 수 있음).
- 시험 오류 0 (가디언 몸 돌리기 · 불꽃 · 탄 피벗 등 9줄 추가). 결정 대기: x-bright(빛 세기) · x-nums(피해 숫자 합치기). 여전히 남은 것: 소리 · git(f-git).

</details>

## (지난) 재개 메모 (2026-11-15 저녁 — 1차 구현 끝)
- **1차 끝**: 자동 시험 42줄 통과 · 오류 0 (`bash tools/unity_batch.sh EarthDef.EditorTools.PlayTest.Run --play`, 화면 Earth_Defence/Logs/PlayTest/ → 보고용 사본 `1차_캡처/`). 보고 = `1차_구현_결과.html` (키 earthdef-1st-1115) — 의견은 `PYTHONUTF8=1 python tools/todo_checks.py 1차_구현_결과.html`.
- 샘플 2판 = 대장 카피 앞모습 우주인(사용자 참고 링크 느낌, 그림은 새로 그림) — `샘플_화면.html` s-hero2 확인 대기.
- 해석(보고 첫머리): 카드 규칙 원작대로 · 별 4개면 돌파를 골라야 다음 강화 · 새 터렛 = 별 1 · 사거리 x1.3(Range_Scale) · 밸런스는 처음 값(스테이지 1 너무 쉬움).
- **못 한 것: 소리** (1차 범위에 적었음) → 다음 추천 ① 소리 ② 2차. git 은 사용자 확인 대기(f-git).

## 코드 구조
- `Earth_Defence/Assets/Scripts/` Core(Tables · SaveData · Art · **Items**(장비 · 칩 · Loadout 2차)) · Game(Boot(+Game 흐름 · TryStart 스태미나 · UIRoot · Ads[초안]) · World · Battle(+엘리트 난이도 · 떨어지는 장비/칩) · Run(카드 · 별 · 돌파 · 칩 효과) · TurretUnit(+가디언 총 · 2차 5종 쏘기) · TurretIdle(대기 효과 11종) · Attacks · **Attacks2**(2차: 기뢰 · 집중 빔 · 소용돌이 · 불바퀴 · 찌릿 번개) · Combat(피해 공식 · 조건 칩 · 효과) · Enemy(+밀어내기) · Fx · **Meta**(터렛/기지 강화 · 순찰 · 스태미나 · 미션 · 출석 · 터렛 열림)) · UI(UIKit(+Scroll) · Popup · Lobby(위 줄 + 탭 4개: 전투 · 터렛 · 장비 · 미션) · **GearTab** · **MissionTab**(+Settings · Toast · Tutorial) · BattleUI) · Editor(SceneBuilder · PlayTest(+Phase2) · TableConverter · XlsxReader · ArtImport)
- 2차 표: `tools/phase2_tables.py` 가 기존 xlsx 에 **없는 줄 · 칸만 더함**(손으로 고친 값 지킴) + Gear · Chip · Mission.xlsx 새로. 칩 키는 카드와 같은 키(scope turret · all)거나 board · cond · card 키 (PlayTest 가 모르는 키를 잡음)
- 씬엔 카메라 + Boot 만. 씬 다시 만들기 = `bash tools/unity_batch.sh EarthDef.EditorTools.SceneBuilder.BuildAll` (표 변환 포함).
- 표: `Earth_Defence/Table/*.xlsx` (처음 값 `tools/make_tables.py`, --force 는 손으로 고친 값을 지움) · 그림: `tools/earth_export.py` (PowerShell) → Resources/Art.
- 주아 글꼴에 · × → ◀ ▶ ∞ 없음 → 화면 글자에 쓰지 말 것 (PlayTest 가 잡음).

## 무엇
- 참고: `Galaxy Defense 벤치마킹 게임 기획서.pdf`(원작 Galaxy Defense: Fortress TD 를 옮긴 문서) + `참고영상.txt`(유튜브 4개).
- 세로 화면 로그라이크 타워 디펜스 + 방치 성장: 아래 돔 기지 · 위에서 적 · 레벨업 카드 3장 · 별 4개 돌파 · 콤보 카드.
- **원칙**: 시스템만 참고. 게임 이름 · 세계관 · 터렛/카드/적 이름 · 그림 · UI 세부 · 음악은 전부 새로(원작 이름 쓰지 않음, 이름안은 계획 ⑦). 기획서 숫자는 표의 처음 값, 밸런스는 마지막.
- 전투는 고정 시간 간격 + 월드 좌표(화면 크기 무관).

## 폴더
- Unity 프로젝트: `Earth_Defence/` (Unity 6000.0.84f1, URP 2D, 씬 Assets/Scenes/Main.unity) — 에디터 `C:\Program Files\Unity Hub\6000.0.84f1\Editor\Unity.exe`
- `tools/todo_checks.py` HTML 체크·의견 읽기 (카피바라 타워에서 복사)
- `tools/doodle_kit.py`(화풍 스킬 복사) · `tools/earth_art.py`(게임 그림, **PowerShell 에서** `$env:PYTHONUTF8='1'; python tools/earth_art.py sample`) · `tools/fonts/Jua-Regular.ttf`
- 영상에서 본 것: 터렛 칸 = 가디언 + 4 · 카드 3장 + 광고 새로고침 1/1 · 한 판 약 5분(레벨 20) · 별 4칸 + 단계 숫자.
