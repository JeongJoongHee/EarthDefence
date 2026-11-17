#!/usr/bin/env bash
# 밸런스 측정 한 번: bash tools/bal_run.sh <label> <days> "<Config 덮어쓰기 Key:값;Key:값>" [probe 목록] [battles] [seed]
#   추가 설정은 환경 변수 BAL_EXTRA 에 "이름=값" 줄로 (예: BAL_EXTRA=$'buy=p_month:30;p_dia5:5\nads=1')
#   예) bash tools/bal_run.sh f2p 360 "Mon_HP_Growth:1.08"
# 결과 Earth_Defence/Logs/Bal/bal_<label>.json → PYTHONUTF8=1 python tools/bal_summary.py <label>
cd "$(dirname "$0")/.."
LABEL="$1"; DAYS="${2:-30}"; SET="$3"; PROBE="${4:-3,5,8,10,15,20,25,30,40,50,60,70,80,90,100}"; BATTLES="${5:-18}"; SEED="${6:-1000}"
mkdir -p Earth_Defence/Logs/Bal
printf "label=%s\ndays=%s\nbattles=%s\npatrolH=16\nprobe=%s\nprobeN=4\npolicy=6\nseed=%s\nset=%s\n%s\n" "$LABEL" "$DAYS" "$BATTLES" "$PROBE" "$SEED" "$SET" "$BAL_EXTRA" > Earth_Defence/Logs/Bal/args.txt
BATCH_TIMEOUT=6500 BATCH_LINES=5 bash tools/unity_batch.sh EarthDef.EditorTools.PlayTest.RunBal --play > /dev/null 2>&1
echo "exit=$?"
PYTHONUTF8=1 python tools/bal_summary.py "$LABEL"
