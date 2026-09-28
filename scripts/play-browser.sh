#!/usr/bin/env bash
set -euo pipefail
project_dir="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")/.." && pwd)"
if [[ ! -f "$project_dir/Builds/Web/index.html" ]]; then
  printf 'Browser-Build fehlt. In Unity: Clash of Cities > Build Browser Player\n' >&2
  exit 1
fi
printf 'Spiel im Browser: http://localhost:8765\nZum Beenden des Servers Strg+C drücken.\n'
exec python3 -m http.server 8765 --bind 127.0.0.1 --directory "$project_dir/Builds/Web"
