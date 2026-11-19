# -*- coding: utf-8 -*-
"""11-20 9차 수정안 (요청 사항/9차 수정안.docx) 표 고치기 — 여러 번 돌려도 같은 결과.
- Story.xlsx (새 표, 시트 Story · Lobby_Talk): 처음 시작 스토리(prologue) · 스테이지 1~100 진입 스토리 · 로비 대장 카피 혼잣말
    Story 칸: Id (prologue / s<스테이지>) · Seq (순서) · Who (capy 대장 카피 · rabbit 달토끼 · queen 젤리 여왕 · planet 떠돌이 행성 · narr 해설) · Text
    → 표에서 바로 고쳐도 됨. 단 이 도구를 다시 돌리면 Story 시트를 아래 내용으로 다시 씀 (고친 대사는 이 파일에 옮길 것)
- Config: 세로 전장 복구(Battle_Vertical) · 스토리 · 로비 말풍선 · 회로 프리셋
- String: 스토리 · 도감 · 프리셋 · 설정 글자
- Frame: 외형 이름 "OO 사령탑" → "OO 돔" (세로 전장 = 돔)
- Turret: 끈끈이 드론 설명 (세로 띠 → 가로 띠, 세로 전장 기준)
사용: PYTHONUTF8=1 python tools/fix9_tables.py   (그다음 표 → csv 는 Unity 배치 TableConverter)
"""
import os, sys, csv
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from phase2_tables import TB, add_rows
from openpyxl import load_workbook, Workbook
from openpyxl.styles import Font, PatternFill, Alignment

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
CSV = os.path.join(ROOT, 'Earth_Defence', 'Assets', 'Resources', 'Tables')


def upsert(ws, key_col, rows):
    head = [c.value for c in ws[1]]; ki = head.index(key_col) + 1
    at = {ws.cell(r, ki).value: r for r in range(2, ws.max_row + 1) if ws.cell(r, ki).value}
    new = []
    for row in rows:
        if row[key_col] in at:
            for k, v in row.items():
                if k in head: ws.cell(at[row[key_col]], head.index(k) + 1, v)
        else: new.append(row)
    if new: add_rows(ws, key_col, new)
    return len(rows) - len(new), len(new)


# ------------------------------------------------------------------ 스토리
PROLOGUE = [
    ('narr', '여기는 푸른 별 지구. 카피바라들이 따끈한 온천에 몸을 담그고 느긋하게 사는 곳이에요.'),
    ('narr', '그런데 어느 밤, 까만 하늘에서 반짝이는 것들이 우르르 쏟아지기 시작했어요.'),
    ('rabbit', '카피! 카피! 큰일 났어! 달에서 방아 찧다가 망원경으로 다 봤어!'),
    ('capy', '음... 달토끼? 나 지금 온천 중인데...'),
    ('rabbit', '우주 저편의 젤리 여왕이 운석이랑 벌레 군단을 끌고 지구로 오고 있어!'),
    ('queen', '말랑말랑~ 이 따끈따끈한 별은 이제 내 거야! 온천은 몽땅 내 차지!'),
    ('capy', '온천을...? 그건 절대 안 돼.'),
    ('rabbit', '지구엔 방어 돔 기지가 하나 있어. 터렛을 세우고 지구를 지킬 수 있는 건 너뿐이야!'),
    ('capy', '좋아. 오늘부터 나는 지구 방위대 대장 카피다!'),
    ('rabbit', '적이 오면 내가 달에서 알려 줄게. 우리 같이 지구를 지키자!'),
]

# 장(10스테이지)마다 첫 스테이지 이야기
CHAPTER = [
    [('rabbit', '첫 출동이야! 돔 위로 떨어지는 건 전부 쏴서 부숴 줘!'), ('capy', '긴장되지만... 온천을 위해서라면!')],
    [('queen', '흥, 운석 몇 개 막았다고 좋아하긴! 이번엔 내 귀여운 벌레들도 같이 보낼 거야!'), ('rabbit', '2장 시작이야! 운석에 벌레까지... 날개 달린 녀석들은 빨라, 조심해!')],
    [('rabbit', '젤리 여왕이 하늘에 끈적한 젤리 구름을 퍼뜨렸어!'), ('capy', '끈적한 건 질색이야. 깨끗하게 치워 줄게.')],
    [('queen', '말랑... 이번엔 내 떠돌이 행성을 지구로 굴려 보낼 거야!'), ('rabbit', '행성이 통째로 굴러온다니! 그래도 카피라면 할 수 있어!')],
    [('rabbit', '벌써 절반이나 왔어! 지구 카피바라들이 응원 편지를 보냈대.'), ('capy', '편지에 귤 그림이 있네. 힘이 난다!')],
    [('queen', '강철 껍질 딱정벌레들아, 저 돔을 꽉 깨물어 버려!'), ('rabbit', '단단한 껍질은 그냥 쏘면 잘 안 들어가. 약점을 노려!')],
    [('rabbit', '저 멀리 커다란 모선들이 줄지어 오고 있어...!'), ('capy', '배가 크면 과녁도 크지. 하나도 안 놓칠게.')],
    [('rabbit', '추워... 얼음 혜성이 몰려와서 달까지 꽁꽁 얼었어.'), ('capy', '다 끝나면 온천에서 같이 몸 녹이자, 달토끼.')],
    [('queen', '말랑말랑! 이제 내 군단을 전부 보낼 거야! 각오해!'), ('rabbit', '여왕이 진심이야. 남은 힘을 모두 모으자!')],
    [('rabbit', '마지막 장이야. 여왕의 성이 바로 저기 보여!'), ('capy', '지구도, 온천도, 친구들도. 전부 지킨다!')],
]

# 장 마지막(10번째) 스테이지 = 장 보스 이야기 (Who 가 queen/planet 이면 그 스테이지 보스와 같게 맞춤)
FINALE = [
    [('rabbit', '첫 장의 마지막이야! 하늘 끝에서 커다란 그림자가 와!'), ('capy', '첫 보스구나. 떨리지만 물러서지 않아.')],
    [('queen', '내 벌레들을 다 쫓아내다니! 이번엔 내가 직접 간다!'), ('capy', '여왕님, 온천엔 줄을 서서 들어오세요.')],
    [('rabbit', '젤리 구름 한가운데에서 커다란 게 꿈틀거려!'), ('capy', '끈적이 대장이네. 말끔하게 정리하자.')],
    [('queen', '떠돌이 행성이 막혔다고? 그럼 내가 직접 굴러간다! 말랑!'), ('rabbit', '여왕이 직접 왔어! 돔에 닿기 전에 막아야 해!')],
    [('queen', '말랑! 반이나 왔다고 좋아하긴 일러!'), ('capy', '반이나 왔으니까 반만 더 하면 되지.')],
    [('rabbit', '강철 군단의 우두머리가 나타났어!'), ('capy', '아무리 단단해도 약점은 있어.')],
    [('rabbit', '모선 함대의 기함이야! 엄청 커!'), ('capy', '큰 배일수록 천천히 가라앉지. 끝까지 쏘자.')],
    [('queen', '얼음처럼 차갑게 얼려 줄게! 말랑말랑~'), ('capy', '얼면 녹이면 돼. 우리 온천은 따끈하거든.')],
    [('queen', '이럴 수가... 내 군단이 거의 다 사라졌어!'), ('rabbit', '조금만 더! 여왕의 마지막 방어선이야!')],
    [('queen', '...사실 우리 별은 너무 추웠어. 나도 따끈한 데 있고 싶었단 말이야.'),
     ('capy', '그럼 처음부터 같이 들어가자고 하지. 온천은 넓어.'),
     ('rabbit', '마지막 싸움이야! 이기고 나서 다 같이 온천에 가자!')],
]

# 스테이지 테마(이름 순서와 같음: 운석 소나기 · 나방 떼 · 거미 언덕 · 얼음 꼬리 · 젤리 습격 · 차가운 밤 · 단단한 껍질 · 하늘의 접시 · 끈적한 비 · 쇠 비)
#   테마마다 3가지 (장이 바뀌면 돌아가며)
THEME = [
    ['돌멩이 운석이 소나기처럼 쏟아져! 하나도 돔에 닿게 하면 안 돼.', '또 운석 비야! 이번엔 쪼개지는 녀석도 섞였어.', '하늘이 돌멩이로 가득해! 우산 대신 터렛이다!'],
    ['반짝반짝 외계 나방 떼야. 불빛을 보고 몰려오나 봐!', '나방들이 무리 지어 날아와. 한꺼번에 쓸어 버리자!', '날갯짓 소리가 달까지 들려. 엄청 많아!'],
    ['거미 언덕에서 외계 거미들이 줄을 타고 내려와!', '거미줄이 하늘에 잔뜩! 걸리지 말고 쏘자.', '거미들이 다리를 바쁘게 움직여. 빨리 끝내자!'],
    ['꼬리가 긴 얼음 혜성이 날아와. 차가우니까 조심!', '얼음 꼬리가 반짝반짝... 예쁘지만 적이야!', '얼음 혜성 무리야. 돔이 얼지 않게 막아 줘!'],
    ['말랑한 젤리 바이러스가 습격했어!', '젤리들이 통통 튀며 내려와. 쪼개지면 더 많아져!', '하늘이 젤리로 출렁출렁해!'],
    ['조용하고 차가운 밤이야. 그런데 운석이 숨어 있어.', '별이 예쁜 밤... 이 아니라 운석이 많은 밤이야!', '깜깜한 밤하늘에서 얼음이 반짝여. 집중!'],
    ['껍질이 단단한 녀석들이 몰려와. 약점으로 뚫어야 해!', '딱딱한 쇠 운석이랑 딱정벌레야. 이빨 조심!', '단단하기로는 우주 최고래. 그래도 우리가 더 세지!'],
    ['하늘에 비행접시가 떴어! 외계인 구경은 나중에!', '접시들이 빙글빙글 돌면서 와. 어지러워!', '비행접시 편대야! 하나씩 떨어뜨리자!'],
    ['끈적한 비가 내려. 젤리랑 드론이 섞여 있어!', '하늘에서 끈적끈적한 게 뚝뚝 떨어져!', '끈적한 비구름이 돔 위에 걸렸어. 걷어 내자!'],
    ['쇠로 된 비가 쏟아져! 단단하니까 끝까지 쏴야 해.', '쇳덩이 비야! 소리만 들어도 무거워.', '쇠 비가 내리는 날이야. 돔아, 버텨 줘!'],
]

ELEM = {'phys': '물리', 'light': '빛', 'grav': '중력', 'fire': '불', 'bolt': '번개'}
REC = {'phys': [('관통포', 0), ('쾅쾅 대포', 5)], 'light': [('레이저 포', 0), ('집중 빔', 10)],
       'grav': [('끈끈이 드론', 0), ('벌집', 0), ('소용돌이포', 15)], 'fire': [('유도 미사일', 0), ('불바퀴', 20)],
       'bolt': [('벼락탑', 0), ('찌릿 코일', 25)]}


def josa(word, a, b):
    """받침 있으면 a, 없으면 b (이야/야 · 이/가 ...)"""
    ch = word[-1]
    if '가' <= ch <= '힣': return a if (ord(ch) - 0xAC00) % 28 else b
    return b



RESOLVE = [
    '좋아, 온천 물 식기 전에 끝내자!', '귤 하나 먹었으니 힘이 넘쳐!', '대장 카피, 출동!', '돔은 내가 지킨다!',
    '천천히, 하지만 정확하게.', '오늘도 느긋하게... 는 아니고, 빠르게!', '다들 걱정 마. 금방 돌아올게.', '하나도 안 놓칠 거야.',
    '준비 완료. 터렛들아, 부탁해!', '이번 판 끝나면 낮잠 잘 거야.', '물러서지 않아. 한 발짝도!', '내 털이 쭈뼛 섰어. 진심 모드!',
    '달토끼, 잘 보고 있어!', '지구 카피바라들의 응원이 들려.', '숨 한 번 크게 쉬고... 간다!', '귀여운 얼굴로 봐주지 않아.',
    '조준은 침착하게, 마음은 뜨겁게!', '오늘 저녁은 승리의 수박이다!', '이 정도쯤이야!', '돔에 흠집 하나 안 낼 거야.',
    '터렛 정비 끝! 반짝반짝해.', '우리 기지는 생각보다 튼튼하다고.', '다 같이 힘내자!', '한 판 더! 아니, 이번 판부터 잘하자.',
    '꼬리... 는 없지만 각오는 있어!', '눈 크게 뜨고, 귀 쫑긋!', '하늘이 시끄럽네. 조용히 시켜 줄게.', '온천 물 데워 놔, 금방 갈게!',
    '지난번보다 더 세졌어. 보여 줄게.', '무서워도 괜찮아. 대장이니까 앞에 설게.', '빙글빙글 도는 건 나도 잘해.', '끝까지 집중!',
    '오늘의 목표: 기지 체력 가득 지키기!', '달토끼 떡 먹으려면 이겨야지.', '자, 시작해 볼까?', '숨은 약점, 다 찾아낼 거야.',
    '카피바라는 원래 느긋한데... 오늘은 예외!', '한 마리도 못 지나가!', '같이 싸워 주는 터렛들이 있어서 든든해.', '좋아, 각오 단단히!',
    '배 든든, 마음 든든!', '내 조준경엔 다 보여.', '이번엔 더 멋지게 이길게.', '지구는 내가 지킨다, 온천도 내가 지킨다!',
    '반짝이는 건 별만으로 충분해.', '이 하늘은 우리 거야.', '거의 다 왔어. 힘내자!', '한 걸음씩, 한 판씩.',
    '모두 무사히 돌아가자!', '자, 대장 카피의 실력을 보여 줄 시간!',
]


def stage_lines(rows):
    out = []
    for r in rows:
        n = int(r['Stage']); c, t = (n - 1) // 10, (n - 1) % 10
        weak, boss = r['Weak'], r['Boss']
        L = []
        if t == 0: L += CHAPTER[min(c, 9)]
        if t == 9:
            L += FINALE[min(c, 9)]      # 장 마지막 스테이지(10 · 20 ...)의 보스는 모두 젤리 여왕 (Stage 표) — 표가 바뀌면 대사도 확인
        else:
            L.append(('rabbit', THEME[t][c % 3]))
        if weak in ELEM:
            recs = [nm for nm, un in REC[weak] if un < n] or [REC[weak][0][0]]
            tl, last, e = ', '.join(recs), recs[-1], ELEM[weak]
            k = (n + c) % 3
            tip = (f'이번 적들은 {e} 공격에 약해! 추천 터렛은 {tl}{josa(last, "이야", "야")}!' if k == 0 else
                   f'{e} 속성이 잘 통할 거야. {tl}, 데려가 볼래?' if k == 1 else
                   f'힌트! 적들은 {e}에 약해. {tl}{josa(last, "이", "가")} 활약할 차례야.')
            L.append(('rabbit', tip))
        L.append(('capy', RESOLVE[(n * 7 + c * 3) % len(RESOLVE)]))
        out.append(('s%d' % n, L))
    return out


LOBBY_TALK = [
    ('하암... 온천 생각난다.', 1), ('오늘도 지구는 평화... 아직은.', 1), ('달토끼는 지금도 방아를 찧고 있겠지?', 1),
    ('터렛들 닦아 줘야겠다. 반짝반짝!', 1), ('귤 하나만 더 먹을까...', 1), ('다음은 스테이지 {stage}! 준비됐어.', 3),
    ('젤리 여왕은 왜 그렇게 온천을 좋아할까?', 1), ('장비를 강화하면 더 든든해져.', 2), ('회로를 끼우면 터렛이 더 세져!', 2),
    ('순찰 보상 받는 거 잊지 마!', 2), ('도감을 채우는 재미도 쏠쏠해.', 1), ('졸려... 아니, 안 졸려! 대장이니까!', 1),
    ('지구 별빛은 오늘도 예쁘네.', 1), ('같은 장비 세 개면 합성할 수 있어.', 2), ('약점 속성을 노리면 훨씬 쉬워져.', 3),
    ('우주복이 조금 답답해... 그래도 멋지지?', 1), ('저 별똥별... 설마 운석은 아니겠지?', 1), ('오늘은 몇 판 할까?', 1),
    ('기지 레벨을 올리면 돔이 튼튼해져.', 2), ('달토끼 떡, 진짜 맛있대.', 1), ('후... 심호흡. 오늘도 힘내자!', 1),
    ('어디 보자, 터렛 조각이 얼마나 모였지?', 1), ('회로 프리셋을 쓰면 갈아 끼우기 편해!', 1), ('나 좀 귀엽지 않아?', 1),
    ('카피바라는 원래 느긋한 동물이야. 원래는.', 1), ('지구를 지키는 건 생각보다 바빠.', 1),
]


def write_story():
    rows = list(csv.DictReader(open(os.path.join(CSV, 'Stage.csv'), encoding='utf-8-sig')))
    p = os.path.join(TB, 'Story.xlsx')
    wb = Workbook(); ws = wb.active; ws.title = 'Story'
    ws.append(['Id', 'Seq', 'Who', 'Text', '', '메모: Who = capy 대장 카피 · rabbit 달토끼 · queen 젤리 여왕 · planet 떠돌이 행성 · narr 해설(얼굴 없음). Id = prologue(처음 시작) · s<스테이지>(전투 시작 때)'])
    seqs = [('prologue', PROLOGUE)] + stage_lines(rows)
    n = 0
    for sid, lines in seqs:
        for i, (w, t) in enumerate(lines):
            ws.append([sid, i + 1, w, t]); n += 1
    hf = PatternFill('solid', fgColor='FFE9C9')
    for c in ws[1]: c.font = Font(bold=True); c.fill = hf
    ws.column_dimensions['A'].width = 10; ws.column_dimensions['C'].width = 9; ws.column_dimensions['D'].width = 90
    for r in ws.iter_rows(min_row=2):
        r[3].alignment = Alignment(wrap_text=True)
    ws2 = wb.create_sheet('Lobby_Talk')
    ws2.append(['Id', 'Text', 'Weight', '', '메모: 로비 대장 카피 말풍선. {stage} = 다음 스테이지 번호. Weight = 뽑힐 비율'])
    for i, (t, w) in enumerate(LOBBY_TALK): ws2.append(['t%02d' % (i + 1), t, w])
    for c in ws2[1]: c.font = Font(bold=True); c.fill = hf
    ws2.column_dimensions['B'].width = 50
    wb.save(p)
    print('Story', n, '줄 ·', len(seqs), '편 · 로비 말풍선', len(LOBBY_TALK))


CONFIG = [
    ('Battle_Vertical', 1, '9차: 1 = 전투를 예전처럼 아래(돔) → 위(적) 방식 (돔 + 터렛 칸 땅 위 한 줄 · 적은 위에서 곧게) · 0 = 11-18 우주 사령탑(왼쪽 벽 · 적은 오른쪽 위에서 비스듬히)'),
    ('Enemy_Speed_Mul_V', 1, '9차: 세로 전장의 적 속도 x (예전 길이 그대로라 1 = 사령탑 전 빠르기) · 사령탑은 Enemy_Speed_Mul'),
    ('Head_Rest_Deg_V', 90, '9차: 세로 전장에서 터렛 머리가 쉴 때 보는 각도 (90 = 위)'),
    ('Slot_Order_V', 1234, '9차: 세로 전장 터렛 칸 순서 (1 왼쪽 안 · 2 오른쪽 안 · 3 왼쪽 밖 · 4 오른쪽 밖)'),
    ('Story_On', 1, '9차: 1 = 스토리 켬 (처음 시작 · 스테이지 진입) · 0 = 끔 (설정 "스토리 건너뛰기" 는 사람마다)'),
    ('Story_Stage_Repeat', 0, '9차: 0 = 스테이지 스토리는 그 스테이지를 처음 시작할 때만 · 1 = 전투 시작마다'),
    ('Story_Type_Speed', 38, '9차: 대사 글자가 나오는 빠르기 (글자/초, 0 = 한 번에)'),
    ('Lobby_Talk_On', 1, '9차: 1 = 로비에서 대장 카피가 가끔 말풍선 (누르면 바로)'),
    ('Lobby_Talk_Gap_Min', 6, '9차: 로비 말풍선 사이 최소 (초)'),
    ('Lobby_Talk_Gap_Max', 12, '9차: 로비 말풍선 사이 최대 (초)'),
    ('Lobby_Talk_Life', 3.4, '9차: 로비 말풍선 보이는 시간 (초)'),
    ('Chip_Presets', 3, '9차: 장비 탭 회로 프리셋 개수'),
]

STRINGS = [
    ('who_capy', '대장 카피'), ('who_rabbit', '달토끼'), ('who_queen', '젤리 여왕'), ('who_planet', '떠돌이 행성'),
    ('story_next', '다음'), ('story_start', '전투 시작!'), ('story_skip_btn', '건너뛰기'), ('set_story_skip', '스토리 건너뛰기'),
    ('story_stage_head', '스테이지 {0}'), ('story_tap', '화면을 누르면 다음'),
    ('codex', '도감'), ('codex_gear', '장비'), ('codex_chip', '회로'), ('codex_got', '모은 것 {0} / {1}'),
    ('codex_unknown', '아직 못 얻음'), ('codex_best', '얻은 최고 등급: {0}'), ('codex_stat', '{0}  Lv.1 {1}  >  최대 Lv.{2} {3}'),
    ('codex_parts', '끼우는 곳: {0}'), ('codex_range', '등급별 값'), ('codex_all_parts', '모든 부위'),
    ('preset_title', '회로 프리셋'), ('preset_save', '저장'), ('preset_load', '불러오기'), ('preset_n', '프리셋 {0}'),
    ('preset_empty', '비어 있음'), ('preset_count', '회로 {0}개'), ('preset_saved', '프리셋 {0}에 지금 회로를 저장했어요'),
    ('preset_loaded', '프리셋 {0}을 불러왔어요'), ('preset_missing', '프리셋 {0}을 불러왔어요 (없어진 회로 {1}개는 빼고)'),
    ('preset_nothing', '저장된 회로가 없어요'), ('preset_tip', '저장 = 지금 끼운 회로를 기억 / 불러오기 = 그대로 다시 끼움'),
]


if __name__ == '__main__':
    write_story()
    p = os.path.join(TB, 'Config.xlsx'); wb = load_workbook(p); ws = wb.active
    print('Config', upsert(ws, 'Key', [dict(Key=k, Value=v, Note=n) for k, v, n in CONFIG])); wb.save(p)
    p = os.path.join(TB, 'String.xlsx'); wb = load_workbook(p); ws = wb['String']
    print('String', upsert(ws, 'Key', [dict(Key=k, KO=v) for k, v in STRINGS])); wb.save(p)
    p = os.path.join(TB, 'Frame.xlsx'); wb = load_workbook(p); ws = wb.active
    head = [c.value for c in ws[1]]; ni = head.index('Name') + 1; k = 0
    for r in range(2, ws.max_row + 1):
        v = ws.cell(r, ni).value
        if isinstance(v, str) and v.endswith('사령탑'): ws.cell(r, ni, v[:-3] + '돔'); k += 1
    wb.save(p); print('Frame 이름', k)
    p = os.path.join(TB, 'Turret.xlsx'); wb = load_workbook(p); ws = wb.active
    print('Turret', upsert(ws, 'Id', [dict(Id='drone', Desc='띠 장판 · 틱마다 둔화',
                                           Info='적이 가장 많이 모인 곳에 띠 장판을 깔아요. 장판 안의 적은 틱마다 피해를 받고 점점 느려져요.')])); wb.save(p)
    print('ok')
