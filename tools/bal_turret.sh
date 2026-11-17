#!/usr/bin/env bash
# 터렛 · 카드 세기 측정: bash tools/bal_turret.sh <label> ["Config 덮어쓰기"] [turret | feature]
#   turret  = 시험장 규칙으로 초당 피해 (카드 없음 / A / B / 모두 / 강화 카드 하나씩)  → Logs/Bal/turret_<label>.json
#   feature = 터렛 하나를 주력으로 키운 판이 깨는 적 체력 배율 k50                  → Logs/Bal/feature_<label>.json
#   추가 설정은 BAL_EXTRA (예: BAL_EXTRA="only=missile,laser")  → PYTHONUTF8=1 python tools/bal_turret.py <label>
cd "$(dirname "$0")/.."
mkdir -p Earth_Defence/Logs/Bal
printf "label=%s\nmode=%s\nset=%s\n%s\n" "$1" "${3:-turret}" "$2" "$BAL_EXTRA" > Earth_Defence/Logs/Bal/args.txt
BATCH_TIMEOUT=3000 BATCH_LINES=5 bash tools/unity_batch.sh EarthDef.EditorTools.PlayTest.RunBal --play > /dev/null 2>&1
echo "exit=$?"
