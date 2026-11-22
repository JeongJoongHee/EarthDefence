# 카피바라 지구를 지켜라! (폴더·코드 이름 Earth_Defence)

## ▶ 재개 메모 (2026-11-22 마무리 — 타워 전체 정리 반영)
- 요청 = "타워_전체_정리.html 보고 게임 내 반영" (원본 `타워 추가 컨셉.xlsx` + 문서 ✎ 의견 6개, 키 earthdef-towers-1120). 보고 = **`타워_반영_결과.html`** (키 earthdef-tower12-1122, 캡처 `캡쳐본 모음/타워_반영_1122/`) — 확인 칸 7개(t-drone · t-rail · t-dome · t-crit · t-focus · t-play · t-balance).
- 바뀐 것: 해금 5/10/15/20 · 멀티샷 → "한 발 더"(followup · followup_mul, TurretUnit.Queue/PendStep) · 드론 감속선(Field line · Drone_Line_*) · 레이저 펄스 둔화 · 번개 확장 다시 치기(recast_prob) · 집중 빔 처치까지(Focus_Hold*) · 코일 기본 1번 + 튕김 70% · 신속 바퀴 · 밀집 군집 작은 벌 · 의견 6개(Resist 0.5 · 대장 카피 10%/0.3초/탄속 20/Lv45 0.15 · 치명 사격 · 폭발탄 5% · 관통포 관통 3 · tc_rail_star → cd_sec · 돔 빙결 보스 포함 2초 + 취약 30% + 한 판 1번).
- 새 타워 6종 (Kind boomerang · frost · flame · saw · meteor · prism): `Game/Attacks12.cs`(Boomerang · Frost.Blast · SawBlade · MeteorFall · MeteorVolley · DelayBlast) · `Game/Battle12.cs`(증폭탑 버프 PrismPulse · BuffCdPct · BuffCrit · 표식 markUntil) · 필살기 6 = TurretUnit.CastUlt. 카드 90 · 스킨 6 · 회로 18. Turret_Soon 시트 비움.
- 도구: 표 `tools/fix12_tables.py`(Id 기준 덮어씀) · 효과 그림 `tools/fix12_art.py`(PowerShell) · 스킨 그림 `tools/skin_preview.py`.
- 시험: 새 `PlayTest.RunR12`(Editor/PlayTest12.cs) 38 · 기본 137 · 시험장 13 · 3차 107 · 7차 24 · 8차 28 · 9차 52 · 10 33 · 11 11 · 확장 22 · Closed 24 — 오류 0. (3차 "월 1회 패키지"는 11-21 결제 규칙 바뀐 뒤 옛 검사였던 것 고침)
- 해석(보고 첫머리): 감속선 기본 피해 남김(Drone_Line_Base_Dmg 0 = 엑셀대로) · 관통 3 = 최대 4마리 · 돔 취약 +30% 는 제가 정한 숫자 · 치명 사격 +30% · 집중 빔 최대 8초 · 히든 타워 8종은 안 넣음 · 밸런스 안 잼.
- 커밋 안 한 것(다른 작업): `장비_회로_*.html` · Icon/chip·g_* 그림 변경 · `타워 추가 컨셉.xlsx` · `타워_전체_정리.html`.
- 상태(마무리): 커밋 a8138b6 + 이 메모 · **push 는 사용자 몫** (`! git push`, 11-21 치트 커밋 포함) · Unity 닫힘 · 하다 만 작업 없음.
- **재개 첫 순서**: ① `PYTHONUTF8=1 python tools/todo_checks.py 타워_반영_결과.html` → 반영 ② 밸런스 다시 재기(새 타워 · 대장 카피 약해짐) ③ 아래 11-21 마무리 메모의 비공개 테스트 순서 (2번 AAB 에는 이번 변경이 없음 — 업데이트로 3번).

## ▶ 재개 메모 (2026-11-21 마무리 — 비공개 테스트 B 단계 진행 중)
- **남은 일 = `남은_작업.html` 하나** (키 earthdef-todo-1121 · `PYTHONUTF8=1 python tools/todo_checks.py 남은_작업.html`). 끝난 문서는 모두 `완료 사항 모음_HTML/`.
- **비공개 테스트 진행 (사용자, Play Console = Cachisi 계정 `/u/1/`, 앱 id 4974878942316119318)**:
  - A 끝: AdMob 앱 ~4098932532 · 보상형 reward_main /1611780664 · 테스트 기기 등록 · 대행업체 = 예전 업체 그대로 · Play Console 앱 만들기(패키지 com.zkfpf.earthdefence).
  - B-1 앱 콘텐츠: 광고 · 개인정보처리방침 조치됨, 나머지 진행 중 (데이터 보안은 2단계 일부 임시 저장 상태 — 제가 화면을 읽다 [다음]을 눌러 생겼을 수 있음, 값은 맞음). 답 = `스토어 등록/앱_콘텐츠_답안.html` (11-21 실제 화면으로 고침: "로그인 세부정보" = 아니요 · 판매자 상태 예 + 서비스 약관 URL 선택 · 데이터 보안 5단계 5유형 · 진단 추가).
  - B-2 스토어 등록 = `스토어 등록/스토어_등록정보.html` + 스크린샷 8 · 그래픽 이미지 · 아이콘 (촬영 `PlayTest.RunStore` → `tools/store_graphics.py`).
  - B-3 내부 테스트: **1번(1.0.1) 올림 19:44** (테스터 목록 "결제테스트" = zkfpf1 체크됨 · 참여 링크 https://play.google.com/apps/internaltest/4701111733734787038 — zkfpf1 은 "You're a tester", 19:53 엔 스토어 아직 "찾을 수 없음" = 퍼지는 중). Play 경고 "필수 하드웨어 기능 screen.portrait" → **2번 AAB `earthdefence-1.0.2-2.aab` (코드 2) 구움 · 점검 통과 — 사용자가 내부 테스트에 새로 올릴 차례**, 비공개(B-5)도 2번.
  - 개인정보처리방침 배포 끝: https://survival-ranking.jeongsan-license.workers.dev/earthdefence-privacy (불도깨비 서버 D:\00 Survival\server, Version 9a1894eb, 그 저장소 커밋 d71285b — kachigun-privacy.html 의 남의 변경은 커밋 안 함).
- **오늘 게임에 넣은 것**: 코스튬 27 · 탈것 16 · 새 적 16(챕터 보스 4) · 챕터 하늘 · 곧 열리는 터렛 6(그림만) · 대장 카피 좌우 이동 · 버닝 · 얼음 파동 · 속도 x0.5~x2 · 스토리 개편안(102편 · 엔딩) · 실제 AdMob · 결제 코드(키 없음 = 꺼짐) · 업로드 키(`키 보관/`, 사용자 백업 필요) · 앱 아이콘 · AndroidManifestFix(portrait 선택사항) · 빌드 때만 앱 이름 "카피바라 지구를 지켜라".
- 시험: 기본 137 · 9차 52 · 10 31 · 확장(+스토리) 22 · R11 14 · Closed 17 · Store 촬영 — 오류 0 (기본 시험 "스테이지 1 보상" 가끔 실패 원인 = 골드를 보스 빨리 감기 뒤에 잼 → 고침).
- 상태: 커밋 74ccd2c + 이 메모 · **push 는 사용자** · Unity 닫힘 · 하다 만 작업 없음.
- **재개 첫 순서**: ① 사용자에게 "내부 테스트에 2번 올렸는지 · 폰에 앱이 보이는지 · 경고가 사라졌는지" 묻고 필요하면 Chrome 으로 확인 ② B-1 남은 선언 · B-2 · B-4(비공개 트랙 · 대행 명단) · B-5(검토 전송 — [저장]만 말고 [전송]) Chrome 으로 같이 ③ `남은_작업.html` 결정 12개 ④ 10차 수정안(프리셋 이름 · 프리셋 회로 보기 · 초기화 · 스테이지 넘기기 치트 · 돔 안 온천 카피바라) → 테스트 중 업데이트 1번째.

## ▶ 재개 메모 (2026-11-21 오후 4 — AdMob 확인 · 1번 AAB · 스토리 개편안 · 문서 정리)
- **남은 일은 `남은_작업.html` 하나** (키 earthdef-todo-1121) — 비공개 테스트 A-4 · B · C · D 상세 + 결정 대기 12 + 다음 개발 순서. 끝난 문서는 모두 `완료 사항 모음_HTML/` (스토리_개편안 폴더 · 소리_검토 폴더 포함, 링크 ../ 고침 · 깨진 링크 0). 맨 위엔 남은_작업 · 히든_타워_기획서(아직 적용 안 함)만.
- AdMob(크롬 확인): 앱 ca-app-pub-5299482082775448~4098932532 · 보상형 reward_main …/1611780664 · 테스트 기기 "테스트 계정". 상태 "검토 필요 · 스토어 추가 필요"는 출시 전이라 정상 → 출시 뒤 [스토어 추가]. set_admob.py 로 넣음.
- **1번 AAB** = `Earth_Defence/Builds/earthdefence-1.0.1-1.aab` (코드 1, 아직 안 올림) — aab_check 모두 통과 · 홈 화면 이름은 빌드 때만 "카피바라 지구를 지켜라"(BuildAab.AppName, 에디터 제품 이름은 "!" 그대로 — 에디터 세이브 때문).
- 스토리 개편안 적용: Story.xlsx = `완료 사항 모음_HTML/스토리_개편안/Story_New.xlsx` (String_Add 시트 뺌) · who_ 보스 이름 4 · 엔딩(로비, best ≥ 100, 한 번) · **스토리 원본 = 완료 사항 모음_HTML/스토리_개편안/generate_story_data.py** (fix9_tables.py 는 --old-story 때만 옛 대사). 시험 RunExpand 에 스토리 4줄.
- **재개 첫 순서**: ① `python tools/todo_checks.py 남은_작업.html` ② 스토어 등록 자료 · 앱 콘텐츠 답안([복사] HTML) · 방침 파일(배포는 허락 뒤) ③ 사용자 A-4 · B 단계 (Chrome 으로 확인) ④ 10차 수정안.

## ▶ 재개 메모 (2026-11-21 오후 3 — 전투 요청 3개)
- 요청(대화) = ① 대장 카피 좌우 이동 ② 대장 카피 · 돔 특수 능력 단추 ③ 속도 x0.5 · x2. 보고 = `전투_요청_결과.html` (키 earthdef-battle11-1121, 캡처 `캡쳐본 모음/전투_요청_1121/`).
- ① Config Guardian_Fixed 0 (±Guardian_X_Max 1.5) ② `Game/BattleSkills.cs` (Battle 은 이제 partial): 버닝(UseBurn · BurnCdMul → TurretUnit · BurnDmgMul → Combat.Calc) · 얼음 파동(UseDome: Paralyze · 보스는 AddSlow · Combat.Raw %) · BattleUI SkillButton(왼쪽 · 오른쪽, 바깥 터렛 위 Skill_Btn_X/Y) · 아이콘 `tools/skill_icons.py` ③ BattleUI.ToggleSpeed 0.5/1/1.5/2 · Battle.LastSpeed. 표 = `tools/fix11_tables.py`.
- 시험: `PlayTest.RunR11`(Editor/PlayTest11.cs) 14 · 기본 137 x2 · 9차 · 10 · 확장 오류 0. 기본 시험 "스테이지 1 보상" 가끔 실패 원인 = 골드를 보스 빨리 감기 뒤에 재서 → 앞에서 재게 고침.

## ▶ 재개 메모 (2026-11-21 오후 2 — 콘텐츠 확장 그림 적용)
- 요청 = "콘텐츠 확장 제언서 보고 타워 몬스터 배경 이미지 추가해서 배치" + `콘텐츠_확장_제안서.html` 의견(몬스터 · 보스 이미지만 배치 · 하늘 그대로 · 바닥은 지구만 · 타워 이미지만). 보고 = `콘텐츠_확장_적용_결과.html` (키 earthdef-expand2-1121, 캡처 `캡쳐본 모음/콘텐츠_확장_적용/`).
- 새 적 16(기믹 없음, 비슷한 기존 적 능력치) · 스테이지 21~100 배치(원래 값 Stage *_Pre 칸, `tools/expansion_tables.py --revert`) · 챕터 하늘 Bg/sky_ch2~5 · 하늘 행성 Bg/planet_ch3~5 (World.SetChapter, Config Chapter_Bg · Chapter_Len) · 타워 6 = Turret_Soon 시트 → 터렛 탭 맨 아래만 (Turret_Soon_Show). 그림 = `tools/expansion_export.py`(PowerShell, 시안 도구 expansion_art.py 함수 씀).
- 시험 RunExpand 18 · 기본 137 오류 0. 커밋 d096a1a. 남은 확인: 스토리 s31 · s40 대사가 새 보스와 어긋남(x-story) · 도전 탭 무한 · 엘리트 군단은 예전 적 그대로.

## ▶ 재개 메모 (2026-11-21 오후 — 비공개 테스트 준비 · 광고 · 결제 · AAB 도구)
- 요청 = "비공개 테스트 진행할 수 있게 준비 — AdMob 붙이는 법부터 모두 상세하게" + "UI 다듬기 · 밸런스 · 상품 추가 · 타워 시스템 개선을 비공개 테스트 때 업데이트로 해도 되나" → 된다(구글 answer/14151465 · 9845334 11-21 다시 읽음, 문서 맨 위 "질문 답").
- 문서 = `비공개_테스트_준비.html` 11-21 판 (키 **earthdef-closed-1121**, 옛 판 = 세션 스크래치 비공개_테스트_준비_before_1121.html). 확인 칸: h-cheat · h-name(빌드 때만 느낌표 빼기 추천 — 체크되면 BuildAab/BuildApk Setup 에서 productName 바꾸고 되돌리기) · h-login · h-list · h-real · A-1 ~ D-2.
- 코드: `Core/AdService.cs`(Ads · AdIds · MainThread · AdMob — 카치건 것, 에디터 Draft) · `Core/Iap.cs`(Unity IAP 5.4.4, Store.Grant 로 지급, 주문 번호 iapOnce "order:…", 광고 제거 되살림) · Store.AccountOn(폰 = 가짜 로그인 · 클라우드 숨김, SimulateDevice) · Iap.SimulateNoStore · 패키지 com.google.ads.mobile 11.5.0(OpenUPM) · com.unity.purchasing 5.4.4 · Plugins/Android gradle 템플릿 · GoogleMobileAds/Resources 설정 · `UnityPurchasing/generated/GooglePlayTangle.cs` = 자리만(키 없음).
- 도구: Editor/BuildAab.cs(메뉴 지구 방위대 › AAB 빌드, 배치 `EarthDef.EditorTools.BuildAab.Build`) · IapTools.cs(결제 키 넣기 · `CompileAndroidBatch` · 점검) · AppIconSetup.cs · BuildApk 는 늘 EARTH_TEST_ADS(시험 광고) · `tools/aab_check.py [--prod]` · `tools/set_admob.py 앱ID 단위ID` · `tools/app_icon.py`(PowerShell) · `tools/closed1_tables.py`(String 11 · Config Cheat_On 1).
- 업로드 키 = `키 보관/upload.keystore` + `비번.txt`(git 밖, 출력 금지) · 별칭 earthdefence · SHA-1 99:DA:70:F9:F1:4D:AE:DC:83:6E:29:B9:02:38:D8:0D:46:FA:F0:E9 — 사용자 백업 필요.
- 시험: 새 `PlayTest.RunClosed`(Editor/PlayTestClosed.cs) 17줄 · 기본 137(첫 돌림 "스테이지 1 보상" 1번 실패 → 다시 통과) · 7차 24 오류 0 · 안드로이드 컴파일(실제 · 시험 광고) 통과.
- **재개 첫 순서**: ① A-1 AdMob 번호 받으면 `set_admob.py` → `BuildAab.Build`(버전 1.0.1 · 코드 1) → `aab_check` 모두 통과 ② 확인 칸 읽기(h-name 체크면 빌드 이름 처리) ③ 스토어 글 · 스크린샷 · 그래픽 이미지 · 앱 콘텐츠 답안 · 출시 노트([복사] HTML, `스토어 등록/`) · 방침 파일(배포는 허락 뒤) ④ 코스튬 추가 결과 · 10차 수정안(아래 메모).

## ▶ 재개 메모 (2026-11-21 — 코스튬 · 탈것 추가)
- 요청 = 대화 "이미지를 수정하고 몇 개 추가 — 요청 사항에 넣음" = `요청 사항/costume_zoom · mount_zoom · bonus_costumes · bonus_mounts.png` (look_art.py 의 시안 그림). 고친 그림은 이미 내보내져 있었음(다시 내보내도 같음). 시안 코스튬 8 · 탈것 6 을 look_art.py 에서 COSTUMES · MOUNTS 에 합침 → 코스튬 27 · 탈것 16 + 없음.
- 보고 = `코스튬_탈것_추가_결과.html` (키 earthdef-look2-1121). 시험 코스튬 33 · 기본 137 오류 0 (기본 "스테이지 1 보상"은 첫 돌림 1번 실패 → 다시 통과). 둥실 검사는 6번 재서 폭으로 고침.
- 커밋 cd8f445 · push 는 사용자 몫(ahead 2). 커밋 안 한 것: `10차 수정안.docx` · 루트 `look_sheet.jpg` · 타워/회로 정리 · 히든 타워 (다른 작업).
- **재개 첫 순서**: ① 추가 결과 의견 ② 10차 수정안 진행 여부 (추천 바로) ③ 능력치 · 얻는 법 결정.

## ▶ 재개 메모 (2026-11-20 밤 — 코스튬 · 탈것 끝)
- 요청 = 대화로 "외형 메뉴에 코스튬 · 탈것 (착용하면 능력치 — 미정) · 탈것은 전투 · 장비 화면에 · 코스튬 + 탈것 = 최종 모습" + 참고 `요청 사항/코스튬.png · 탈것.png`. 보고 = `코스튬_탈것_결과.html` (키 earthdef-look-1120, 캡처 `캡쳐본 모음/코스튬_탈것_캡처/`).
- 시험: 기본 137 · 7차 24 · 9차 52 · **코스튬 31 (`PlayTest.RunR10`, Editor/PlayTest10.cs)** 오류 0. 기본 · 7차 시험의 장비 탭 검사는 LookView(자식 Body) 기준으로 고침.
- 구조: 코스튬 = 몸 그림 통째 (Char/costume/<id> · _float · _gun · _ui, 피벗 Art.CostumeFoot) · 탈것 = 뒤 층 + 앞 층(Char/mount/<id> · _f, 피벗 = 자리 Art.MountSeat) → `Game/LookRig.cs` MountRig(전투 TurretUnit.MakeMount · 로비 LobbyWalker — 뒤 4 < 몸 5 < 앞 6 < 총 7, 몸이 젖혀도 탈것 수평, Bob 둥실) · LookView(UI 겹치기, Art.DrawnRect = look_bounds.txt 로 여백 빼고 맞춤). 외형 탭 sub 2 · 3 + 미리 보기(Looks.previewCostume/Mount). 능력치 = Loadout.Build 에서 착용 중 Stat1/Val1 · Stat2/Val2. 세이브 costume · mount · costumes · mounts. Store.BuyLook(사면 바로 착용) · WearLook.
- 표 = `Earth_Defence/Table/Look.xlsx` (Costume · Mount, **능력치 칸 비움 = "능력치 준비 중"**, 가격 300/500 임시) · `tools/fix10_tables.py` (다시 돌려도 손으로 넣은 값 유지, Lift 만 새로) · Config Mount_Battle_Scale · Mount_Lobby_Scale 0.8 · Mount_Lift_Mul · Mount_Bob · Mount_Bob_Amp.
- 그림 = `tools/look_art.py` (PowerShell: `export [id…]` → 그림 + look_pivots.json(Lift) + look_bounds.txt · `sheet` → tools/_preview/look_sheet.png). 그림을 바꾸면 export → fix10_tables.py → 표 변환. 손 소품(지팡이 · 우산 · 칼 …)은 총을 들어서 뺌.
- 해석(보고 첫머리): 기본 우주복 = 코스튬 "우주 대원" · 탈것 그림 속 의상은 코스튬이 맡음 · 얻는 법 = 다이아(요청 밖) · 로비 · 터렛 카드 · 스토리 얼굴도 같은 모습.
- 손대지 않은 것: **`요청 사항/10차 수정안.docx`** (작업 중에 새로 들어옴 — 다음 요청일 것) · `회로_전체_정리.html` · `타워_전체_정리.html` · `히든_타워_기획서.*` (다른 작업).
- 상태(마무리): 커밋 fbf91a9 · 사용자가 push 끝 (이 메모 커밋만 ahead 1) · Unity 닫힘 · 하다 만 작업 없음. 사용자 "오늘 작업은 이만 마무리" — 10차 수정안은 아직 시작 안 함.
- **재개 첫 순서**: ① 코스튬_탈것_결과 의견 (능력치 · 얻는 법 결정) ② 10차 수정안.docx 읽기 ③ 9차 결과 · 스토리 대사 의견 (아래 메모).

## ▶ 재개 메모 (2026-11-20 새벽 — 9차 수정안 반영 끝)
- 요청 = `요청 사항/9차 수정안.docx` (9줄 + 대화창 그림). 사용자 "수정할 게 많아서 비공개 테스트는 조금 미룸". 보고 = `9차_수정안_결과.html` (키 earthdef-req9-1120, 캡처 `캡쳐본 모음/9차_수정안_캡처/`) + **`스토리_대사_검토.html`** (키 earthdef-story-1120, 101편 + 로비 말풍선 — 편마다 체크 · 의견). 의견 읽기 `PYTHONUTF8=1 python tools/todo_checks.py <문서>`.
- 시험: 기본 137 · 시험장 13 · 3차 107 · 7차 24 · 8차 28 · **9차 52 (`PlayTest.RunR9`, Editor/PlayTest9.cs)** 오류 0. **7차 · 8차 시험은 Battle_Vertical 0(사령탑)으로 돎** = 되돌리기 스위치 확인용. 기본 시험 "스테이지 1 보상" 흔들림은 이번엔 두 번 다 통과(첫 돌림 1번 실패 — 골드 차이 0, 원인 못 찾음).
- 바뀐 것: ① 전투 = 예전 돔 방식(아래 가운데 돔 · 터렛 4칸 땅 위 한 줄 · 적 위에서 곧게 · 머리 위 90° · 장판 가로 · 적 속도 1배) — `World.V` (Config **Battle_Vertical** 1 · 0 = 사령탑) · World.ApplyLayout (SetLobby 때 바뀌었으면 다시) · 바닥 Bg/ground_earth_v(8차 지구를 가운데로 + 기지 건물) · 외형 이름 "OO 돔" · 목록 아이콘 Icon/framed_<id> ② 스토리 `UI/Story.cs` StoryView (처음 시작 = Title.Enter · 스테이지 = Game.TryStart, 스태미나 먼저 · 스테이지는 처음 한 번 Story_Stage_Repeat) · 설정 "스토리 건너뛰기"(S.storySkip) · [건너뛰기] · 뒤로 가기 ③ 로비 말풍선 LobbyTalk (전투 탭 · 누르면 한마디) ④ 도감 `Core/Codex.cs` + `UI/CodexUI.cs` (세이브 codex) ⑤ 회로 프리셋 3개 ChipPreset (세이브 chipPresets, uid@자리).
- 표 = `tools/fix9_tables.py` (Story.xlsx 새로: Story · Lobby_Talk 시트 — **다시 돌리면 Story 시트를 새로 씀, 대사 고치면 이 파일에 옮길 것**) · 그림 `earth_export.py groundv story9` (PowerShell) · 보고서 `tools/report9.py <숫자 6개>`.
- 해석(보고 첫머리): 복구 = 돔 방식 + 8차 지구 바닥 · 달토끼 등 뒤 요청은 유지 · 대장 카피 고정 유지 · 스테이지 스토리 처음 한 번 · 세계관(젤리 여왕이 온천을 노림 · 달토끼 · 100에서 화해) · 도감은 이번부터 기록 · 대화창 [건너뛰기]는 요청 밖.
- 마무리 때: 사용자가 지난 보고서 14개(2차~8차 결과 · 밸런스 1~3차 · 요청05 · 전투UI_제안 · 전투배치 2개)를 `완료 사항 모음_HTML/` 로 옮겨 둔 것을 커밋 + 그 안의 그림 링크 191개를 `../` 로 고침(깨진 링크 0). **7차 · 8차 의견은 이제 `완료 사항 모음_HTML/7차_수정안_결과.html` 등에서 읽음** (체크 · 의견은 브라우저 저장이라 옮겨도 그대로).
- 건드리지 않은 것(커밋 안 함): `히든_타워_기획서.html · .md` (다른 작업에서 생김) · 루트의 `코스튬.png` 등 사용자가 새로 넣은 그림 (다음 요청 재료일 수 있음 — 재개 때 물어볼 것).
- 상태: 커밋함 · **push 는 사용자 몫** (`! git push`) · Unity 닫힘 · 하다 만 작업 없음.
- **재개 첫 순서**: ① 9차 결과 · 스토리 대사 의견 읽기 → 반영 (대사는 fix9_tables.py 에 넣고 다시 돌림 + 표 변환) ② 8차 · 7차 결과 의견 (`완료 사항 모음_HTML/`) ③ `비공개_테스트_준비.html` 결정 ④ 밸런스 3차 재측정(세로 전장으로 돌아옴 — 적 속도 1배).

## ▶ 재개 메모 (2026-11-19 밤 — 8차 수정안 반영 끝)
- 요청 = `요청 사항/8차 수정안.docx` (12줄 + 결과 창 그림). 보고 = `8차_수정안_결과.html` (키 earthdef-req8-1119, 캡처 `캡쳐본 모음/8차_수정안_캡처/`, 전 = before_*). 의견 읽기 `PYTHONUTF8=1 python tools/todo_checks.py 8차_수정안_결과.html`.
- 시험: 기본 137 · 시험장 13 · 3차 107 · 7차 24 · **8차 28 (`PlayTest.RunR8`, Editor/PlayTest8.cs)** 오류 0. 기본 시험 "스테이지 1 보상 · 다음 스테이지 열림" 이 한 번 실패 → 다시 돌리니 통과(원인 못 찾음, 메시지에 best · stage · 골드 · 첫 클리어 값을 찍게 해 둠). **시험 도는 중 스크립트를 고치면 Unity 가 다시 읽어 시험이 깨짐 — 기다릴 땐 파일 손대지 말 것.**
- 바뀐 것: 타이틀 [게임 시작] 아래 · 0.7배(Title_Btn_Bottom · Title_Btn_Scale) · 로비 [전투 시작] · 순찰 아래 탭 바로 위(Lobby_Btn_Bottom 1 · Lobby_Btn_Gap) · 별똥별 느리게(Shoot_Star_Speed_* 4.5~6.5 · Shoot_Star_Life_*) · 달토끼 오른쪽 위(Moon_X 4.3, 운석보다 뒤) · 전투 바닥 = 지구 윗면 Bg/ground_earth + 일반 행성(tower_art.earth_ground · planet, World.FloorY = EG_CX -3.4 · EG_R 32) · 끈끈이 장판 세로(Field_Vertical 1, Battle.CrowdX, 필살기도) · 전투 위 줄 "약점 X"(BattleUI Weak · WeakLabel) · 결과 창 RewardBox · 장비 탭 Float2D.InPlace(제자리 위아래) · 외형 설명 짧게(Frame Desc · skin_ult · skin_bonus) · 종료(설정 [게임 종료] → Quit.Ask · 폰 뒤로 가기 BackKey · Popup.CanClose).
- 표 = tools/fix8_tables.py (Config 11 · 글자 6 · Frame 설명 7 · 드론 설명) · 보고서 = tools/report8.py <before 폴더> <숫자 5개>.
- 해석(보고 첫머리): 달은 운석보다 뒤 · 지구 그림 → 살구색 줄무늬 행성(고리 없음) · 종료 = 설정 + 뒤로 가기 · 세로 장판 밸런스는 3차 재측정 때.
- 상태: 커밋함 · **push 는 사용자 몫** — 재개 때 `git status -sb` 로 확인 · Unity 닫힘.
- **재개 첫 순서**: ① 8차 결과 의견 읽기 → 반영 ② 7차 결과 의견(아직 비어 있음) ③ `비공개_테스트_준비.html` 결정(아래 11-19 메모) ④ 전투UI_제안 · 밸런스 3차 재측정.

## ▶ 재개 메모 (2026-11-19 밤 — 비공개 테스트 준비 문서 다시 씀)
- 사용자: 테스터 = **대행업체(설치 · 실행만, 플레이 안 함)** · 결정 9개 체크(Cachisi 계정 · 패키지 그대로 · 결제 코드 · 게스트 · 치트 "추가해도 무방" · 이름 "카피바라 지구를 지켜라" · 주기적 업데이트). ④ 광고만 남음.
- `비공개_테스트_준비.html` 새 판(키 **earthdef-closed-1119b**, 옛 판 백업 = 세션 스크래치 비공개_테스트_준비_before.html): 위험(구글 문서 "참여 부족"도 거절 사유) · 대책(본인 직접 플레이 메모 · 사전 출시 보고서 · vitals · 신청서는 사실대로) · 직접 할 일 A(AdMob 앱 · 테스트 기기 · 대행 문의 · 앱 만들기) B(앱 콘텐츠 · 스토어 · 내부 → 비공개 · 링크) C(점검 · 직접 플레이 · 업데이트 · Play 게임즈 · 결제 상품 8개) D(마지막 빌드 · 신청).
- 해석 확인 대기: h-cheat(테스트 판 켬 · 프로덕션 전 끔) · h-name(느낌표 없음) · h-list(명단은 옛 계정에 있음 → Cachisi 에 새 목록) · h-real(지인 2~3명) · 라이선스 테스터는 본인만.
- todo_checks.py: label 뒤에 단계 설명이 붙은 항목도 읽게 고침.

## ▶ 재개 메모 (2026-11-19 — 비공개 테스트 준비)
- 사용자 "지금 비공개 테스트 열고 테스트 기간에 UI · 밸런스 · 콘텐츠 해도 되나?" → 된다(구글 answer/14151465: 14일은 테스터 참여로 셈 · 업데이트 권함 · 신청서가 "바꾼 것"을 물음). 결정 문서 = `비공개_테스트_준비.html` (키 earthdef-closed-1119, 결정 10 + 직접 3).
- 찾은 것: 폰에서도 광고 [초안] = 바로 보상(Ads.Draft true) · 구글 연동 가짜("draft-user") · 결제는 폰에서 꺼짐 · 개발용 키 · 아이콘 기본 · AAB 도구 없음 · 광고/결제/GPGS 패키지 없음.
- 재개 첫 순서: 결정 읽기 → 추천대로면 업로드 키 · BuildAab/aab_check(카치건) · 광고 11.5.0 · IAP 5 · [초안] 숨김 · 아이콘 · 스토어 그림 · 방침(배포는 허락 뒤) · 답안 → 1번 AAB.

## ▶ 재개 메모 (2026-11-18 밤 — 7차 수정안 반영 끝)
- 요청 = `요청 사항/7차 수정안.docx` (13줄 — 앞 5줄 로비, "전투 입장에서" 아래 7줄 전투로 읽음). 보고 = `7차_수정안_결과.html` (키 earthdef-req7-1118, 캡처 `캡쳐본 모음/7차_수정안_캡처/`, 전 = before_*). 의견 읽기 `PYTHONUTF8=1 python tools/todo_checks.py 7차_수정안_결과.html`.
- 시험: 기본 137 · 시험장 13 · 3차 107 · **7차 25 (`PlayTest.RunR7`, Editor/PlayTest7.cs)** 오류 0. 다른 시험은 PlayTestRunner.Start 에서 타이틀을 바로 넘김.
- 바뀐 것: 타이틀(UI/Title.cs · Game.ToTitle · Config Title_On) → [게임 시작] → 구글 [초안] / 게스트(저장 안 됨 확인 창) · 로비 = World.SetLobby(사령탑 · 달 바닥 · 달 숨김 · Bg/sky_plain · 큰 지구 Bg/earth_big · 별똥별 StarStep) · LobbyWalker 는 지구 면(World.EarthSurf · EarthTilt) 위 · 로비 단추 가운데(Lobby_Btn_X 0 · Dome_Top_Y -2, 스테이지 칸과 안 겹치게 내려옴) · 장비 탭 Float2D(Char/guardian_float, 총 없음) · 전투: 사령탑 가늘게(SlotXPos -3.9 · 대장 카피 -2.9 발코니 — 탄도가 터렛 안 지남) · 달 Bg/moon 따로(왼쪽 위 PlaceMoon · Moon_X · Moon_Below_Hud) · 땅 1080x700 큰 지구 · Hud_H 300(= Lv 게이지 아래) · 적 Fade in(World.FadeTop · Enemy_Fade_Len · Spawn_Below_Hud 0.45) · 자리 고르기 안내 가운데 크게(Slot_Tip_X) · [카드 다시 고르기] 뺌(Battle.SlotBack 없앰).
- 표 = tools/fix7_tables.py (Config 14 · 글자 14) · 그림 = tools/tower_art.py earth()/lobby_earth/shooting_star/moon_disk · earth_art.sky(moon, plain) · guardian_float → `earth_export.py bg char tower`. 보고서 = tools/report7.py. 고치기 전 그림 = 세션 스크래치 art_before7 (지워질 수 있음, git 에도 있음).
- 해석(보고 첫머리): 전투 "떨어지는 별똥별" = 적(운석) · 게스트 = 이 폰엔 저장(계정엔 안 됨) · 외형 탭 목록 그림은 그대로 · 지지대는 폭만 줄이고 칸 높이 그대로(밸런스 그대로).
- 상태(마무리): 커밋 c97c922 + 이 메모 커밋 · **push 는 사용자 몫(ahead 2)** — 재개 때 `git status -sb` 로 확인, 남아 있으면 `! git push` 부탁 · 작업 트리 깨끗 · 하다 만 작업 없음 · Unity 닫힘.
- **재개 첫 순서**: ① 7차 결과 의견 읽기 → 반영 (비어 있으면 "7차_수정안_결과 보셨나요? 해석 4가지(전투 별똥별 = 운석 · 게스트 = 폰엔 저장 · 외형 목록 그림 유지 · 지지대 폭만) 괜찮을까요?" 하고 물음) ② 그 전 결정 대기(아래 오후 메모: a-play · a-slot1 · a-ui) ③ 전투UI_제안 사령탑 기준으로 다시 ④ 밸런스 3차 재측정 (Hud_H 300 으로 적 나오는 줄이 0.4칸 낮아짐 — 측정에 반영됨).

## ▶ 재개 메모 (2026-11-18 오후 — 전투 배치 적용 끝)
- 시안 2판 의견(키 earthdef-layout2-1118): 이대로 · 머리만 45° · 대장 카피 고정(+ "터렛을 고를 때 자리를 고르는 흐름") · 로비/외형 사령탑. → 게임에 옮김.
- 보고 = `전투배치_적용_결과.html` (키 earthdef-layout3-1118, 캡처 `캡쳐본 모음/전투배치_적용/`). 결정 대기: a-play(직접 해 본 느낌) · a-slot1(빈 칸 1개도 묻기? 추천 바로) · a-ui(전투UI_제안을 사령탑 기준으로 고쳐 다시 — 추천) → 그다음 밸런스 3차 새 배치로 다시 측정.
- 구조: World.SlotPos(1~4 위 2 · 아래 2) · GuardianPos · ShieldX(방어선 세로줄) · OnField · 사령탑 그림 Bg/tower_<외형>(피벗 왼쪽 아래 40px = 땅) · 달 바닥 Bg/ground_moon. Enemy = basePos 가 dir 로 + 옆 흔들림 · Aim(target) · Remain/VY(예전 높이 기준 = Shield_Y + 남은 거리 → Pos_Near/Far · 얼림) · Knock = 온 길로. Battle.SpawnPoint/AimTarget/Swarm/BigSpawn · St.Slot · PlaceAt · SlotBack · NoSlotPick(시험 · 측정은 true). Run.FreeSlotList(Slot_Order 2314). TurretIdle 머리(NewHead · SetAim · TipWorld, 대포 · 미사일 · 레이저 · 관통포) · TurretUnit.HeadStep. BattleUI 위 줄 = 방주 항로 막대 · 체력판 달 바닥 · ShowSlotPick.
- 표 tools/layout2_tables.py (Config 15 · 글자 2 · 외형 이름 "OO 사령탑") · 그림 tools/tower_art.py → `earth_export.py tower` (frames 도 tower 로 — 예전 돔 아이콘이 덮어쓰지 않게). 시안 tools/layout_mock.py command(K=.75).
- 밸런스: 새 배치 1회 측정(씨앗 1000, 120일) 30일 39 · 60일 50 · 90일 60 · 120일 71 (예전 39 · 47 · 59 · 69) — 진행 속도 거의 같음 · 처음 승률은 낮아짐(4~10 100→67%). Enemy_Speed_Mul 0.72. 시험 세이브로 스테이지 10 을 40초 못 버팀 → 별똥별 시험은 기지 체력 채우며.
- 시험: 기본 137 · 시험장 13 · 3차 107 오류 0.
- 상태: 커밋 ff702b5 · 사용자가 push 끝(origin/master 와 같음) · 작업 트리 깨끗 · 하다 만 작업 없음.
- **재개 첫 순서**: ① `PYTHONUTF8=1 python tools/todo_checks.py 전투배치_적용_결과.html` 로 a-play · a-slot1 · a-ui 읽기 (비어 있으면 "전투배치_적용_결과 보셨나요? 빈 칸 1개일 땐 바로 놓기 · 전투UI_제안 사령탑 기준으로 고치기 — 추천대로 갈까요?" 하고 물음) → ② a-slot1 이 추천대로면 Battle.Choose 에서 FreeSlotList.Count>=2 조건 그대로(이미 1칸이면 바로 놓음) · 다르면 Config 하나로 → ③ `전투UI_제안.html`(키 earthdef-battleui-1118, 체크 0) 의 돔 기준 항목을 사령탑 배치로 고쳐 다시 드림(고치기 전 스크래치 백업) → ④ 밸런스 3차 다시 측정(씨앗 2개 이상, `bash tools/bal_run.sh` — 처음 승률 4~10 · 31~40 이 낮아진 것 · 무한 첫 1분 8칸 · 별똥별 시험) + `밸런스_3차.html` 결정 b3-tier · b3-noads · b3-turret.

## ▶ 재개 메모 (2026-11-18 아침 — 전투 배치 2판)
- 1판 의견(키 earthdef-layout-1118): ③ 배치 + ② 방주 위 줄(항로 막대) · 등대 → **우주 사령탑** · 대장 카피 가운데 · 위 2 아래 2 · 터렛 45° · 운석 오른쪽 위 → 왼쪽 아래 · 순서 = 배치 → 전투 UI 제안 → 밸런스 3차.
- 2판 = `전투배치_시안.html` (키 earthdef-layout2-1118, 그림 `캡쳐본 모음/전투배치_시안/command.png` · `tilt.png`, `tools/layout_mock.py command tilt`). 결정 대기: l2-ok · l2-tilt(추천 B 머리 조각만 45°, 쏠 때 적 쪽으로) · l2-move(추천 가운데 고정) · l2-look(로비 · 외형 7종 사령탑으로).
- 해석(보고 첫머리): 운석은 모두 같은 방향이면 아래쪽이 바닥에 떨어져 사령탑 여러 높이를 겨눔 · 바닥 = 달 + 지평선 지구 · 체력 = 사령탑 아래 이름판(② 방식). 지난 판 문서 백업 = 세션 스크래치 전투배치_시안_before_2판.html.
- 사용자 "사령탑 크기 절반" → `command(K=.5)`: 탑 그림만 절반(터렛 · 대장 카피 · 적은 그대로), 4칸 "&gt;" 모양(위 칸 버팀대는 위에서 당김 · 맨 아래 칸은 달 바닥) · 체력판은 바닥으로. 전 = command_full.png. → 다시 "1.5배만 · 벽에 최대한 붙여" → **K .75 · BX = 14 + 88K(왼쪽 벽)** · 4칸 탑 옆 한 줄(SX = BX+150) · 공격 표적 다시 고름. 절반 판 = command_half.png.
- 다음: 승인되면 게임에 옮김(적 나오는 자리 · 방향 · 터렛 칸 · 머리 돌리기 · 그림 · 위 줄) → 시험 → 전투UI_제안 → 밸런스 3차 다시 측정.

## ▶ 재개 메모 (2026-11-18 — 밸런스 3차 · 지금까지 검토)
- 사용자 의견(밸런스_2차.html): 성장 패스 제외 · **골드 부스터 30일 추가** · 골드 주머니 OK · 진행 "제안대로" · 후반 성장(새 장비 · 동료)은 나중 · **무과금 = 광고 아예 안 봄** · 나머지 밸런스 지금.
- 보고 = `밸런스_3차.html` (키 earthdef-bal3-1117, 캡처 `캡쳐본 모음/밸런스_3차/`). 결정 대기: b3-tier(과금 단계 정의) · b3-noads(광고 제거 = 광고 보상 공짜 그대로?) · b3-turret(시험장 손맛).
- 검토에서 찾아 고친 것(보고서 표): 계단이 장 첫 스테이지에 몰려 70 에서 43일 → bal_fit STEP 0.5 + 70 이후 계단 높이 절반 · 테마별 어려움 보정 · bal_fit 이 1~8 흐름을 후반 자료로 계산하던 결함 · 터렛 5배 차이 → 1.3배(tools/balance3_turrets.py, 대포 · 관통포 · 미사일은 재장전 줄임) · 효과 없는 카드(불바퀴 지속 연장 · 정밀 조준) · 장비 후반 비중 8% → Gear_Lv_Growth 1.02 · 회로 고정 값 → 기지 값 %(base_atk · base_hp · shield_pct) · 방어력 = 비율(Reduce_K 240) · 엘리트 난이도 = 보통 n+10 의 적(Elite_Mode_Offset) · 무한 모드 · 엘리트 군단 골드 식 · 미션 · 출석 보상 진행에 맞춰(Reward_Scale).
- 새 상품: 골드 부스터 p_boost [초안] (Kind boost, iapOnce "goldboost:끝나는 시각", 다시 사면 이어 붙음, Battle.End 판 골드 x Gold_Boost_Mul) · 골드 금고 i_gold3 (다이아 1,500 · 순찰 100시간치 · 하루 3번).
- 표 도구: tools/balance3_tables.py(Config · 회로 · 카드 · 상점 · 글자) · tools/balance3_turrets.py · 스테이지 배율 tools/bal3_stage_mul.txt → tools/balance2_apply.py. 터렛 측정 `bash tools/bal_turret.sh <이름> "" feature`(주력 터렛 k50) · `… turret`(초당 피해 · 카드별).
- 측정 도구 3차: 광고 기본 안 봄(ads=1 이면 봄, 광고 제거 사면 자동) · 도전 모드 · 엘리트 난이도 도전/골드 벌기 · rate=/cd= 덮어쓰기 · stopAtTop=0. **한 번 잰 값이 ±30% 흔들림**(씨앗과 무관한 무작위 + 후반 힘 정체) → 씨앗 2개 이상으로 볼 것.
- 결과: 무과금(광고 안 봄) 100 = 305 · 402일(평균 353, 목표 360) — 단 70~80 에서 한 스테이지 46~67일 머묾(120일쯤 터렛 · 장비 최대 → 후반 성장 업데이트 때 다시 맞춤) · 광고 봄 224 · 소(월 패키지) 314 · 중(+광고 제거 + 부스터) 175 · 대(+산더미 엿새마다) 86~138일 (과금은 70 이후 계단 낮추기 전 배율로 잼).
- **측정 도구 결함(못 고침)**: 공략 비교(probe)를 많이 넣은 측정은 후반이 빨라짐(180일 ↔ 300일+) → 최종 값은 probe 1개로 잼. 원인 후보: 세이브 되돌리기 · 실제 시계(SaveData.Now) · Store rnd(TickCount).
- 시험: 기본 121 · 시험장 13 · 3차·6차·밸런스 107줄 오류 0.

## (지난) 재개 메모 (2026-11-17 저녁 — 밸런스 2차)
- 사용자 의견(밸런스_1차.html): 무과금 100 = 360일 · 하루 1시간 30분(18판) · 약점으로 구성 + 배율 상승 · 보스 추천대로 · "10 스테이지마다 툭툭 20 부터" · 과금 소 300 / 중 200 / 대 100일 · 장인이 약점 · 돌파로 빌드를 깎아 깨는 느낌.
- 보고 = `밸런스_2차.html` (키 earthdef-bal2-1117, 캡처 `캡쳐본 모음/밸런스_2차/`). 결정 대기: **b2-bm 과금 단계 맞추는 방법**(지금 상점으론 하루 3,000 다이아≈월 100만 원도 243일 — 성장 패스 · 골드 부스터 등 과금 전용 성장 상품 + 단계별 월 금액을 정해 달라고 함) · b2-late(120일쯤 터렛 45 · 장비 궁극 70 이 다 차 후반은 기지 레벨만 → 후반 성장 거리) · b2-ads(무과금 = 광고 다 봄 가정).
- 게임 쪽: 스테이지 약점 Stage 표 Weak(이 스테이지 적 모두 그 속성에 약함, Combat) · 적 구성 60% 이상 그 약점(tools/balance2_tables.py, 원래 = Enemies_Old 칸) · Weak_Mul 2 · 보스 분노(Boss_Rage_*, Enemy.cs) · 계단(Chapter_Step_From 20 · Chapter_Step_Atk 1.2, 체력 계단은 Stage 표 Hp_Mul) · 로비 [적 정보] 약점 안내 · 상점 골드 주머니 = 순찰과 같은 식(SaveData.PatrolGoldHour — 1차 때 곱 성장 빠뜨렸던 것).
- 맞춘 방법: 목표 날짜표 D(s)=360((s-8)/92)^2.2 → `tools/bal_fit.py` 가 측정의 P(날짜) · 필요한 힘으로 스테이지 배율을 거꾸로 계산(5번 되풀이 + 후반 0.9배) → `tools/bal2_stage_mul.txt` → `tools/balance2_apply.py`. 결과: 무과금 씨앗 1000 = 365일 · 5000 = 420일에 98 (후반은 힘이 거의 안 올라 운에 따라 흔들림).
- 측정 도구 2차: 하루 18판 · 광고 다 봄 · 지면 힘 +10% 까지 빠른 골드 벌기(전투 없이 지난 승리의 처치 · 시간으로 보상) · 다이아 쓰기 · 과금(buy=상품:며칠 · diaDay=N) · stageMulFile. 360일 한 번 ≈ 5~8분. 약점 노림 ↔ 아무거나 83 : 41 / 160 (1차 75 : 89).
- 시험: 기본 121 · 시험장 13 · 3차·6차 97줄 오류 0 (시험 쪽 고침: 약점 배율 비교 때 스테이지 약점 비움 · 필살기 스테이지 12 → 3 · 무한 모드 15 → 5 · 순찰 오차 비율).

## (지난) 재개 메모 (2026-11-17 낮 — 밸런스 1차)
- 사용자 "참고 영상 수준으로 1차 · 제안 · 쉽게 깨기보단 맞으면서 공략으로 깨게". 보고 = `밸런스_1차.html` (키 earthdef-bal1-1117, 영상 모음 `캡쳐본 모음/밸런스_1차/`). 결정 대기: b-pace(100 스테이지 기간 — 추천 1차 그대로 ~40일 + 엘리트 · 무한으로 늘리기) · b-day(하루 12판 가정) · 제안 b-theme(스테이지마다 주 약점 → 공략 축, 2차 추천) · b-boss.
- 영상(②4~6 · ③10~12, 15초마다 잘라 봄): 원작은 기지 체력이 한 번도 안 깎임 · 한 판 ~5분 · 5분 무렵 레벨 20.
- 측정 = `PlayTest.RunBal` (Editor/PlayTestBal.cs, 한 판 0.5초): 새 세이브로 하루 N판 다음 스테이지 도전(지면 골드 벌기 → 다시) · 판 사이 장비 · 합성 · 회로 · 가장 싼 강화 · 순찰 · 무료 상자 · 출석, 무과금. 공략 비교(probe) · 고르기 규칙 겨루기(tour). `bash tools/bal_run.sh <이름> <일수> "<Key:값;…>" [probe] [판] [씨앗]` → `tools/bal_summary.py` · `tools/bal_loss.py` · 보고서 `tools/balance_report.py`. 결과 json = Earth_Defence/Logs/Bal (git 밖).
- 1차 값 = `tools/balance1_apply.py` (Config 20개, 새 손잡이: Win_Gold_Growth · Patrol_Gold_Growth · Base_Atk_Growth · Base_HP_Growth · Early_Stage_N · Early_HP_Mul · Ten_Stage_HP_Mul — 1 이면 예전). 표 백업 = 세션 스크래치 table_before_bal1 (지워질 수 있음, git 에도 있음).
- 결과(씨앗 3개): 100 스테이지 37~43일(지금 = 90일에 39) · 1~3 처음에 깸 · 31 이후 이긴 판 체력 45~79% · 11~30 은 아직 조금 쉬움(90%). **카드를 규칙대로 골라도 아무거나와 비슷**(75 vs 89 / 168) → 공략 축은 b-theme 로.
- 주의: 실행마다 흔들림이 커서 씨앗 3개로 볼 것 · BATCH_TIMEOUT 늘림(unity_batch.sh) · bal 모드는 PlayTest 시간 제한 7000초.

## (지난) 재개 메모 (2026-11-17 아침 — 성능 점검 마무리 + 6차 수정안 15개)
- 어제 밤 끊긴 곳 = 성능 점검 · 결함 고침(시험 · 커밋 전) → 시험 1줄(Fx 물체 이름 "fx_" 붙음) 고쳐 커밋 8b4e9ce. 측정 `PlayTest.RunPerf` → `Earth_Defence/Logs/Perf/perf_<label.txt>.json` (before · after3). 시험 4개를 연달아 돌린 직후 재면 PC 부하로 탭 · 프레임 값이 1.5~2배 나옴 → 한 번 더 재서 확인.
- 요청 = `요청 사항/6차 수정안.docx`. 보고 = `6차_수정안_결과.html` (키 earthdef-req6-1117, 캡처 `캡쳐본 모음/6차_수정안_캡처/`, 전 = before_*.png). 시험 120 + 13 + 95 오류 0. 표 = tools/fix6_tables.py.
- 바뀐 것: Num.Res(100만부터 K · UIKit.Fit 로 칸 안) · 위 줄 숫자 오른쪽 · 순찰 = 600x150 단추 → Lobby.PatrolPopup(받기 / 광고 2배) · 설정 프레임 · 도움말 다시 보기 제거(프레임 늘 60) · 토스트 흐려짐(Toast_Fade) · 상점 3패널(ShopTab.sub 다이아 · 장비 · 재화) · 월 1회 패키지 p_month(Kind monthly, iapOnce "month:id:yyyyMM") · 상자 전설까지(Box_Grade_Max) · 천장 55(Box_Pity, SaveData pityGear · chipPity) · 무한 모드 Endless_Exp_Mul 2.5 · Endless_Exp_From 5 · Endless_Pick_Gap 8 · 터렛 외형 바뀐 그림만 · 기지 외형 돔 7종(earth_art.dome_variant → Bg/dome_<id> · Icon/frame_<id>, `earth_export.py frames`) · 장비 탭 Hover3D(둥실 + 몸 돌림 + 그늘 · 발밑 그림자).
- 해석(보고 첫머리): 월 1회 패키지는 참고영상.txt 메모를 보고 넣음(확인 대기) · K 대문자 · 천장은 다이아 상자만 종류별.
- 주의: 무한 모드 시험은 스테이지 15 (30 은 시험 세이브 전력으론 대장 카피 혼자 겨우 잡아 결과가 흔들림) · 주아에 '·' 없음 → 표 글자에 쓰지 말 것.

## (지난) 재개 메모 (2026-11-17 밤 — 치트 · 코어 그림 · 요청 사항_05)
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
- **APK (폰 시험용, 11-18)**: 메뉴 지구 방위대 › APK 빌드 (폰에 바로 설치)... 또는 `BATCH_TIMEOUT=2400 bash tools/unity_batch.sh EarthDef.EditorTools.BuildApk.Build`(개발용 빌드 = BuildDev) → `Earth_Defence/Builds/earthdefence-1.0.N[-dev].apk`. 개발용 키 서명 · 패키지 com.zkfpf.earthdefence 는 **임시**(첫 AAB 전에 사용자 결정) · 앱 아이콘 아직 없음(Unity 기본). 빌드 대상은 이제 Android.
- `tools/todo_checks.py` HTML 체크·의견 읽기 (카피바라 타워에서 복사)
- `tools/doodle_kit.py`(화풍 스킬 복사) · `tools/earth_art.py`(게임 그림, **PowerShell 에서** `$env:PYTHONUTF8='1'; python tools/earth_art.py sample`) · `tools/fonts/Jua-Regular.ttf`
- 영상에서 본 것: 터렛 칸 = 가디언 + 4 · 카드 3장 + 광고 새로고침 1/1 · 한 판 약 5분(레벨 20) · 별 4칸 + 단계 숫자.
