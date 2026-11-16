#!/usr/bin/env bash
# 밸런스 측정 한 번: bash tools/bal_run.sh <label> <days> "<Config 덮어쓰기 Key:값;Key:값>" [probe 목록] [battles]
#   예) bash tools/bal_run.sh try1 60 "Mon_HP_Growth:1.12;Win_Gold_Growth:1.08"
# 결과 Earth_Defence/Logs/Bal/bal_<label>.json → PYTHONUTF8=1 python tools/bal_summary.py <label>
cd "$(dirname "$0")/.."
LABEL="$1"; DAYS="${2:-30}"; SET="$3"; PROBE="${4:-3,5,8,10,15,20,25,30,40,50,60,70,80,90,100}"; BATTLES="${5:-12}"; SEED="${6:-1000}"
mkdir -p Earth_Defence/Logs/Bal
printf "label=%s\ndays=%s\nbattles=%s\npatrolH=16\nprobe=%s\nprobeN=4\npolicy=6\nseed=%s\nset=%s\n" "$LABEL" "$DAYS" "$BATTLES" "$PROBE" "$SEED" "$SET" > Earth_Defence/Logs/Bal/args.txt
BATCH_TIMEOUT=3000 BATCH_LINES=5 bash tools/unity_batch.sh EarthDef.EditorTools.PlayTest.RunBal --play > /dev/null 2>&1
echo "exit=$?"
PYTHONUTF8=1 python tools/bal_summary.py "$LABEL"
