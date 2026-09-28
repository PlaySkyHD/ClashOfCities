#!/usr/bin/env bash
set -euo pipefail
project_dir="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")/.." && pwd)"
editor="${UNITY_EDITOR:-/home/nic/Unity/Hub/Editor/2022.3.62f3/Editor/Unity}"
platform="${1:-all}"
case "$platform" in
  linux) targets=(Linux) ;;
  windows) targets=(Windows) ;;
  macos) targets=(MacOS) ;;
  all) targets=(Linux Windows MacOS) ;;
  *) printf 'Usage: %s [linux|windows|macos|all]\n' "$0" >&2; exit 2 ;;
esac
mkdir -p "$project_dir/Builds"
for target in "${targets[@]}"; do
    case "$target" in
      Linux) unity_target=Linux64 ;;
      Windows) unity_target=Win64 ;;
      MacOS) unity_target=OSXUniversal ;;
    esac
    env SSL_CERT_DIR=/etc/ssl/certs "$editor" -batchmode -nographics -quit \
      -buildTarget "$unity_target" -projectPath "$project_dir" \
      -executeMethod "ClashOfCities.Editor.BuildTools.Build$target" \
      -logFile "$project_dir/Builds/native-$target.log"
done
