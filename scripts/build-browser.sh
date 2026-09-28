#!/usr/bin/env bash
set -euo pipefail
project_dir="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")/.." && pwd)"
editor="${UNITY_EDITOR:-/home/nic/Unity/Hub/Editor/2022.3.62f3/Editor/Unity}"
if [[ -x "$project_dir/.tools/bin/ffmpeg" ]]; then export PATH="$project_dir/.tools/bin:$PATH"; fi
env SSL_CERT_DIR=/etc/ssl/certs "$editor" -batchmode -nographics -quit -buildTarget WebGL -projectPath "$project_dir" -executeMethod ClashOfCities.Editor.BuildTools.BuildWeb -logFile "$project_dir/Builds/browser-build.log"

python3 "$project_dir/scripts/version-browser-assets.py" "${CLASH_WEB_OUTPUT:-$project_dir/Builds/Web}"
python3 "$project_dir/scripts/prepare-pages.py"
