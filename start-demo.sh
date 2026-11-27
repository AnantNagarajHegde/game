#!/usr/bin/env bash
# Start everything the Mac demo needs (levels 1 to 3). Run from the game folder:
#   conda activate paridhwani
#   ./start-demo.sh          # start the backend (curriculum + voice) on http://127.0.0.1:8767
#   ./start-demo.sh --warm   # first record any voice lines that are not cached yet, then start
set -euo pipefail
cd "$(dirname "$0")/village_backend"
PY="${PARIDHWANI_PYTHON:-python}"
if ! "$PY" -c "import pandas" >/dev/null 2>&1; then
  echo "Python with pandas not found. Run: conda activate paridhwani"; exit 1
fi
export PARIDHWANI_TTS_PROVIDER=sarvam
if [ -z "${SARVAM_API_KEY:-}" ]; then
  echo "Note: SARVAM_API_KEY is not set. Cached voice lines still play; live replies and uncached lines are off."
fi
if [ "${1:-}" = "--warm" ]; then
  "$PY" warm_tts_cache.py
fi
if "$PY" -m unittest -q test_curriculum >/dev/null 2>&1; then
  echo "Curriculum checks: OK"
else
  echo "WARNING: curriculum checks failed. Run: python -m unittest test_curriculum -v"
fi
if lsof -i :8767 >/dev/null 2>&1; then
  echo "Port 8767 is already in use (an old backend?). Stop it first: lsof -ti :8767 | xargs kill"; exit 1
fi
echo "Backend running on http://127.0.0.1:8767  (Ctrl+C to stop). Now press Play in Unity."
exec "$PY" server.py --provider ollama --model preview --port 8767
