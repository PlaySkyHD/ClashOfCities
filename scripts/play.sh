#!/usr/bin/env bash
set -euo pipefail
project_dir="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")/.." && pwd)"
player="$project_dir/Builds/Linux/ClashOfCities.x86_64"
if [[ -x "$player" ]]; then
    exec "$player" -screen-fullscreen 0 -screen-width 1280 -screen-height 800 "$@"
fi
editor="${UNITY_EDITOR:-/home/nic/Unity/Hub/Editor/2022.3.62f3/Editor/Unity}"
if [[ ! -x "$editor" ]]; then
    printf 'Unity Editor nicht gefunden: %s\n' "$editor" >&2
    exit 1
fi
exec "$editor" -projectPath "$project_dir" -executeMethod ClashOfCities.Editor.BuildTools.PlayGame "$@"
