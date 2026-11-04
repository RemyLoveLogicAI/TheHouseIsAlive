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
    set +e
    "${UNITY_PATH}" -batchmode -quit \
      -projectPath "${PROJECT_DIR}" \
      -buildTarget Android \
      -executeMethod TheHouseIsAlive.Editor.BuildScript.BuildQuestAPK \
      -logFile "${BUILD_DIR}/build.log"
    BUILD_EXIT=$?
    set -e
    echo "exit=${BUILD_EXIT}" > "${BUILD_DIR}/build.exit"

    if grep -q "Quest 3 build SUCCEEDED" "${BUILD_DIR}/build.log"; then
      echo "Quest 3 build SUCCEEDED. APK: ${APK_PATH}"
    elif [ ${BUILD_EXIT} -eq 0 ]; then
      echo "Build completed. See ${BUILD_DIR}/build.log for details."
    else
      echo "Build failed with exit code ${BUILD_EXIT}. See ${BUILD_DIR}/build.log."
      exit ${BUILD_EXIT}
    fi
    ;;
  "scene")
    echo "Generating the main MR scene via Unity 6..."
    set +e
    "${UNITY_PATH}" -batchmode -quit \
      -projectPath "${PROJECT_DIR}" \
      -buildTarget Android \
      -executeMethod TheHouseIsAlive.Editor.BuildScript.GenerateMainScene \
      -logFile "${BUILD_DIR}/scene.log"
    SCENE_EXIT=$?
    set -e
    if grep -q "Main MR Scene saved successfully" "${BUILD_DIR}/scene.log"; then
      echo "Scene generation completed successfully. See ${BUILD_DIR}/scene.log for details."
    elif [ ${SCENE_EXIT} -eq 0 ]; then
      echo "Scene generation completed. See ${BUILD_DIR}/scene.log for details."
    else
      echo "Scene generation failed with exit code ${SCENE_EXIT}. See ${BUILD_DIR}/scene.log."
      exit ${SCENE_EXIT}
    fi
    ;;
  "install"|"sideload")
    if [ ! -f "${APK_PATH}" ]; then
      echo "Error: ${APK_PATH} not found. Run ./build-quest.sh build first."
      exit 1
    fi
    echo "Installing ${APK_PATH} onto connected Quest 3..."
    "${ADB}" install -r "${APK_PATH}"
    echo "Launching app..."
    "${ADB}" shell am start -n "com.LoveLogicAI.TheHouseIsAlive/com.unity3d.player.UnityPlayerGameActivity"
    ;;
  *)
    echo "Usage: ./build-quest.sh [sim|devices|build|scene|install]"
    ;;
esac
