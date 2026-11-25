#!/bin/bash
# 在任意 macOS 上运行此脚本：编译原生 Swift 独立桌面客户端（通用二进制 arm64+x86_64）并打包出 墨守-macOS.dmg
set -euo pipefail
DIR="$(cd "$(dirname "$0")" && pwd)"
APP="$DIR/墨守.app"
RES="$APP/Contents/Resources"
BIN="$APP/Contents/MacOS/moshou_launcher"
STAGING="/tmp/moshou_dmg_staging"
OUT="$DIR/墨守-macOS.dmg"

cp "$DIR/moshou_mac.py" "$RES/moshou_mac.py"
cp "$DIR/ui.html" "$RES/ui.html"

# 编译原生 macOS 独立窗口壳（Swift + Cocoa + WKWebView），彻底告别浏览器标签页
if [ -f "$DIR/MoShouHost.swift" ] && command -v swiftc >/dev/null 2>&1; then
  echo "正在编译原生 macOS 独立应用（arm64 + x86_64 Universal）..."
  if swiftc -O -target arm64-apple-macos11.0 -framework Cocoa -framework WebKit "$DIR/MoShouHost.swift" -o "/tmp/moshou_arm64" 2>/dev/null \
     && swiftc -O -target x86_64-apple-macos11.0 -framework Cocoa -framework WebKit "$DIR/MoShouHost.swift" -o "/tmp/moshou_x86_64" 2>/dev/null; then
    lipo -create "/tmp/moshou_arm64" "/tmp/moshou_x86_64" -output "$BIN"
    rm -f "/tmp/moshou_arm64" "/tmp/moshou_x86_64"
  else
    swiftc -O -framework Cocoa -framework WebKit "$DIR/MoShouHost.swift" -o "$BIN"
  fi
fi
chmod +x "$BIN"

# Apple Silicon 原生二进制需做本地 ad-hoc 签名
if command -v codesign >/dev/null 2>&1; then
  codesign --force --deep --sign - "$APP" || true
fi

rm -rf "$STAGING" "$OUT"
mkdir -p "$STAGING"
cp -R "$APP" "$STAGING/墨守.app"
ln -s /Applications "$STAGING/Applications"

hdiutil create -volname "墨守 MoShou" -srcfolder "$STAGING" -ov -format UDZO "$OUT"
rm -rf "$STAGING"
echo "已生成原生独立软件镜像: $OUT"
