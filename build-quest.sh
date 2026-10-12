#!/usr/bin/env bash
set -euo pipefail

UNITY_PATH="/Applications/Unity/Hub/Editor/6000.6.0f1/Unity.app/Contents/MacOS/Unity"
PROJECT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
BUILD_DIR="${PROJECT_DIR}/Builds"
APK_PATH="${BUILD_DIR}/TheHouseIsAlive.apk"
ADB="/Applications/Unity/Hub/Editor/6000.6.0f1/PlaybackEngines/AndroidPlayer/SDK/platform-tools/adb"

echo "=== The House Is Alive: Meta Quest 3 Build Pipeline ==="

mkdir -p "${BUILD_DIR}"

case "${1:-build}" in
  "sim"|"simulator")
    echo "Starting Meta XR Simulator..."
    open "/Applications/MetaXRSimulator.app"
    ;;
  "devices")
    "${ADB}" devices -l
    ;;
  "build")
    echo "Compiling Quest 3 Android APK via Unity 6..."
    "${UNITY_PATH}" -batchmode -quit \
      -projectPath "${PROJECT_DIR}" \
      -buildTarget Android \
      -logFile "${BUILD_DIR}/build.log"
    echo "Build completed. See ${BUILD_DIR}/build.log for details."
    ;;
  "install"|"sideload")
    if [ ! -f "${APK_PATH}" ]; then
      echo "Error: ${APK_PATH} not found. Run ./build-quest.sh build first."
      exit 1
    fi
    echo "Installing ${APK_PATH} onto connected Quest 3..."
    "${ADB}" install -r "${APK_PATH}"
    echo "Launching app..."
    "${ADB}" shell am start -n "com.LoveLogicAI.TheHouseIsAlive/com.unity3d.player.UnityPlayerActivity"
    ;;
  *)
    echo "Usage: ./build-quest.sh [sim|devices|build|install]"
    ;;
esac
