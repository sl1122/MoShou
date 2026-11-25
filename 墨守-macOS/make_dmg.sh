#!/bin/bash
# 在任意 macOS 上运行此脚本，3 秒打包出标准拖拽安装镜像 墨守-macOS.dmg
set -euo pipefail
DIR="$(cd "$(dirname "$0")" && pwd)"
APP="$DIR/墨守.app"
RES="$APP/Contents/Resources"
STAGING="/tmp/moshou_dmg_staging"
OUT="$DIR/墨守-macOS.dmg"

cp "$DIR/moshou_mac.py" "$RES/moshou_mac.py"
cp "$DIR/ui.html" "$RES/ui.html"
chmod +x "$APP/Contents/MacOS/moshou_launcher"

rm -rf "$STAGING" "$OUT"
mkdir -p "$STAGING"
cp -R "$APP" "$STAGING/墨守.app"
ln -s /Applications "$STAGING/Applications"

hdiutil create -volname "墨守 MoShou" -srcfolder "$STAGING" -ov -format UDZO "$OUT"
rm -rf "$STAGING"
echo "已生成: $OUT"
