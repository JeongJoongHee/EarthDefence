# 카피바라 지구를 지켜라! (폴더·코드 이름 Earth_Defence)

## ▶ 재개 메모 (2026-11-16 마무리 — 다음에 여기부터)
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
- `Earth_Defence/Assets/Scripts/` Core(Tables · SaveData · Art) · Game(Boot(+Game 흐름 · UIRoot · Ads[초안]) · World · Battle · Run(카드 · 별 · 돌파) · TurretUnit(+가디언 총) · TurretIdle(대기 효과) · Attacks · Combat(피해 공식 · 효과) · Enemy · Fx) · UI(UIKit · Popup · Lobby(+Meta 강화 · 순찰) · BattleUI) · Editor(SceneBuilder · PlayTest · TableConverter · XlsxReader · ArtImport)
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
