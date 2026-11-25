#!/bin/bash
# 墨守 MoShou · macOS 一键启动脚本（支持任意中英文路径）
DIR="$(cd "$(dirname "$0")" && pwd)"
cd "$DIR" || exit 1

if ! command -v python3 >/dev/null 2>&1; then
  osascript -e 'display dialog "未检测到 Python 3，请先安装 Command Line Tools（xcode-select --install）。" buttons {"好"} default button 1'
  exit 1
fi

nohup python3 "$DIR/moshou_mac.py" >/dev/null 2>&1 &
exit 0
