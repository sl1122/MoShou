#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
墨守 MoShou · macOS 桌面图标布局记忆与恢复后端
铁律：只读写 Finder 桌面图标坐标 (desktop position)，绝不移动、改名或删除任何文件。
零第三方依赖，仅用 macOS 自带 Python 3 + osascript (Finder AppleScript)。
"""

import http.server
import json
import os
import platform
import shutil
import socketserver
import subprocess
import sys
import threading
import time
import urllib.parse
import webbrowser
from datetime import datetime

IS_MAC = platform.system() == "Darwin"
APP_SUPPORT = os.path.expanduser("~/Library/Application Support/MoShou") if IS_MAC else os.path.join(os.path.dirname(os.path.abspath(__file__)), "_local_data")
CONFIG_PATH = os.path.join(APP_SUPPORT, "config.json")
BASE_DIR = os.path.dirname(os.path.abspath(__file__))

DOC_EXT = {".txt", ".doc", ".docx", ".pdf", ".xls", ".xlsx", ".ppt", ".pptx", ".md", ".csv", ".rtf", ".pages", ".numbers", ".key"}
ARC_EXT = {".zip", ".rar", ".7z", ".tar", ".gz", ".bz2", ".xz", ".iso", ".dmg", ".pkg", ".tgz"}
APP_EXT = {".app", ".command", ".sh", ".workflow", ".webloc", ".inetloc"}


def ensure_dir(path):
    os.makedirs(path, exist_ok=True)


def can_write(path):
    try:
        ensure_dir(path)
        probe = os.path.join(path, ".ms_probe.tmp")
        with open(probe, "w", encoding="utf-8") as f:
            f.write("ok")
        os.remove(probe)
        return True
    except Exception:
        return False


def load_config():
    cfg = {"dark": False, "dataDir": "", "seenGuide": False}
    try:
        if os.path.exists(CONFIG_PATH):
            with open(CONFIG_PATH, "r", encoding="utf-8") as f:
                raw = json.load(f)
            if isinstance(raw, dict):
                cfg.update(raw)
    except Exception:
        pass
    return cfg


def save_config(cfg):
    try:
        ensure_dir(os.path.dirname(CONFIG_PATH))
        with open(CONFIG_PATH, "w", encoding="utf-8") as f:
            json.dump(cfg, f, ensure_ascii=False, indent=2)
    except Exception:
        pass


def resolve_root(cfg):
    d = cfg.get("dataDir", "")
    if d and os.path.isdir(d) and can_write(d):
        return d
    local_data = os.path.join(BASE_DIR, "data")
    if can_write(local_data):
        return local_data
    ensure_dir(APP_SUPPORT)
    return APP_SUPPORT


def migrate_data(src, dst):
    if not src or not dst or os.path.abspath(src) == os.path.abspath(dst):
        return
    try:
        ensure_dir(dst)
        s_snap = os.path.join(src, "layout_snapshot.json")
        d_snap = os.path.join(dst, "layout_snapshot.json")
        if os.path.exists(s_snap) and not os.path.exists(d_snap):
            shutil.copy2(s_snap, d_snap)
        s_lay = os.path.join(src, "layouts")
        d_lay = os.path.join(dst, "layouts")
        if os.path.isdir(s_lay):
            ensure_dir(d_lay)
            for fn in os.listdir(s_lay):
                if fn.endswith(".json"):
                    sp = os.path.join(s_lay, fn)
                    dp = os.path.join(d_lay, fn)
                    if not os.path.exists(dp):
                        shutil.copy2(sp, dp)
    except Exception:
        pass


def classify_item(name, is_dir=False, kind_hint=""):
    low = name.lower()
    _, ext = os.path.splitext(low)
    if ext == ".app":
        return "app"
    if kind_hint in ("disk", "volume"):
        return "sys"
    if is_dir or kind_hint == "folder":
        return "dir"
    if ext in ARC_EXT:
        return "arc"
    if ext in DOC_EXT:
        return "doc"
    if ext in APP_EXT:
        return "app"
    # Fallback check on ~/Desktop
    desk = os.path.expanduser("~/Desktop")
    full = os.path.join(desk, name)
    if os.path.isdir(full):
        return "app" if low.endswith(".app") else "dir"
    if not os.path.exists(full):
        return "sys"
    return "other"


def run_osascript(script):
    proc = subprocess.run(
        ["osascript", "-e", script],
        capture_output=True,
        text=True,
        timeout=15,
    )
    if proc.returncode != 0:
        err = (proc.stderr or "").strip()
        if "-1743" in err or "Not authorized" in err:
            raise RuntimeError("未获得 Finder 自动化权限：请前往「系统设置 → 隐私与安全性 → 自动化」允许控制 Finder")
        raise RuntimeError(err or "AppleScript 执行失败")
    return proc.stdout.strip()


def get_screen_info():
    if not IS_MAC:
        return {"screenW": 1440, "screenH": 900, "scale": 2.0}
    try:
        out = run_osascript('tell application "Finder" to get bounds of window of desktop')
        parts = [int(x.strip()) for x in out.split(",")]
        if len(parts) == 4:
            return {"screenW": max(800, parts[2] - parts[0]), "screenH": max(600, parts[3] - parts[1]), "scale": 2.0}
    except Exception:
        pass
    return {"screenW": 1440, "screenH": 900, "scale": 2.0}


def scan_desktop():
    if not IS_MAC:
        # Non-macOS preview fallback (for syntax/UI testing only)
        return []
    script = r'''
    tell application "Finder"
        set outLines to {}
        set itemsList to every item of desktop
        repeat with itm in itemsList
            set nm to name of itm
            set pos to desktop position of itm
            set cls to class of itm as text
            set end of outLines to nm & tab & (item 1 of pos as text) & tab & (item 2 of pos as text) & tab & cls
        end repeat
        set AppleScript's text item delimiters to linefeed
        return outLines as text
    end tell
    '''
    raw = run_osascript(script)
    items = []
    if not raw:
        return items
    for idx, line in enumerate(raw.splitlines()):
        parts = line.split("\t")
        if len(parts) < 3:
            continue
        name = parts[0]
        try:
            x = int(float(parts[1]))
            y = int(float(parts[2]))
        except ValueError:
            continue
        cls = parts[3].lower() if len(parts) >= 4 else ""
        is_dir = ("folder" in cls)
        kind = "disk" if ("disk" in cls or "volume" in cls) else ("folder" if is_dir else "")
        ty = classify_item(name, is_dir=is_dir, kind_hint=kind)
        items.append({"i": idx, "n": name, "x": x, "y": y, "t": ty})
    return items


def apply_desktop_positions(items):
    """仅写 Finder desktop position，绝不移动或修改文件本身。"""
    if not IS_MAC:
        raise RuntimeError("当前不在 macOS 环境下，无法写入 Finder 桌面坐标")
    lines = ['tell application "Finder"']
    for it in items:
        nm = str(it.get("n", "")).replace("\\", "\\\\").replace('"', '\\"')
        x = int(round(it.get("x", 0)))
        y = int(round(it.get("y", 0)))
        lines.append(f'  try')
        lines.append(f'    set desktop position of item "{nm}" of desktop to {{{x}, {y}}}')
        lines.append(f'  end try')
    lines.append("end tell")
    run_osascript("\n".join(lines))
    return len(items)


def load_snapshot(root):
    p = os.path.join(root, "layout_snapshot.json")
    if not os.path.exists(p):
        return None
    try:
        with open(p, "r", encoding="utf-8-sig") as f:
            return json.load(f)
    except Exception:
        return None


def save_snapshot(root, snap, backup=True):
    ensure_dir(root)
    p = os.path.join(root, "layout_snapshot.json")
    with open(p, "w", encoding="utf-8") as f:
        json.dump(snap, f, ensure_ascii=False, indent=2)
    if backup:
        lay = os.path.join(root, "layouts")
        ensure_dir(lay)
        stamp = datetime.now().strftime("%Y%m%d_%H%M%S")
        bp = os.path.join(lay, f"layout_{stamp}.json")
        with open(bp, "w", encoding="utf-8") as f:
            json.dump(snap, f, ensure_ascii=False, indent=2)


def list_history(root):
    lay = os.path.join(root, "layouts")
    if not os.path.isdir(lay):
        return []
    files = sorted([f for f in os.listdir(lay) if f.startswith("layout_") and f.endswith(".json")], reverse=True)
    out = []
    for fn in files[:40]:
        fp = os.path.join(lay, fn)
        try:
            with open(fp, "r", encoding="utf-8-sig") as f:
                s = json.load(f)
            out.append({
                "file": fn,
                "savedAt": s.get("savedAt", fn),
                "count": len(s.get("items", [])),
                "note": s.get("note", ""),
                "snap": s,
            })
        except Exception:
            continue
    return out


def calendar_counts(root):
    counts = [0] * 30
    lay = os.path.join(root, "layouts")
    today = datetime.now().date()
    if os.path.isdir(lay):
        for fn in os.listdir(lay):
            if fn.startswith("layout_") and len(fn) >= 15 and fn.endswith(".json"):
                try:
                    dt = datetime.strptime(fn[7:15], "%Y%m%d").date()
                    back = (today - dt).days
                    if 0 <= back < 30:
                        counts[29 - back] += 1
                except Exception:
                    pass
    return counts


class MoShouHandler(http.server.BaseHTTPRequestHandler):
    def log_message(self, format, *args):
        return  # Keep terminal silent

    def send_json(self, obj, code=200):
        raw = json.dumps(obj, ensure_ascii=False).encode("utf-8")
        self.send_response(code)
        self.send_header("Content-Type", "application/json; charset=utf-8")
        self.send_header("Content-Length", str(len(raw)))
        self.send_header("Cache-Control", "no-store")
        self.end_headers()
        self.wfile.write(raw)

    def read_json(self):
        n = int(self.headers.get("Content-Length", "0") or "0")
        if n <= 0:
            return {}
        return json.loads(self.rfile.read(n).decode("utf-8"))

    def do_GET(self):
        path = urllib.parse.urlparse(self.path).path
        if path in ("/", "/index.html"):
            ui_path = os.path.join(BASE_DIR, "ui.html")
            with open(ui_path, "rb") as f:
                data = f.read()
            self.send_response(200)
            self.send_header("Content-Type", "text/html; charset=utf-8")
            self.send_header("Content-Length", str(len(data)))
            self.end_headers()
            self.wfile.write(data)
            return

        cfg = load_config()
        root = resolve_root(cfg)

        if path == "/api/state":
            snap = load_snapshot(root)
            sc = get_screen_info()
            first_run = snap is None
            if snap is None and IS_MAC:
                try:
                    live = scan_desktop()
                    snap = {
                        "savedAt": "",
                        "note": "尚未落笔",
                        "count": len(live),
                        "screenW": sc["screenW"],
                        "screenH": sc["screenH"],
                        "scale": sc["scale"],
                        "items": live,
                    }
                except Exception as ex:
                    snap = {"savedAt": "", "note": str(ex), "count": 0, "screenW": sc["screenW"], "screenH": sc["screenH"], "scale": sc["scale"], "items": []}
            elif snap is None:
                snap = {"savedAt": "", "note": "演示模式", "count": 0, "screenW": sc["screenW"], "screenH": sc["screenH"], "scale": sc["scale"], "items": []}

            # Ensure type field exists on legacy snapshots
            for it in snap.get("items", []):
                if not it.get("t"):
                    it["t"] = classify_item(it.get("n", ""))

            self.send_json({
                "ok": True,
                "isMac": IS_MAC,
                "root": root,
                "exeDir": BASE_DIR,
                "config": cfg,
                "firstRun": first_run,
                "snap": snap,
                "calendar": calendar_counts(root),
            })
            return

        if path == "/api/history":
            self.send_json({"ok": True, "root": root, "items": list_history(root)})
            return

        self.send_json({"ok": False, "error": "Not found"}, 404)

    def do_POST(self):
        path = urllib.parse.urlparse(self.path).path
        body = self.read_json()
        cfg = load_config()
        root = resolve_root(cfg)

        try:
            if path == "/api/save":
                sc = get_screen_info()
                live = scan_desktop()
                snap = {
                    "savedAt": datetime.now().strftime("%Y-%m-%d %H:%M:%S"),
                    "note": "落笔 " + datetime.now().strftime("%H:%M"),
                    "count": len(live),
                    "screenW": sc["screenW"],
                    "screenH": sc["screenH"],
                    "scale": sc["scale"],
                    "items": live,
                }
                save_snapshot(root, snap, backup=True)
                self.send_json({"ok": True, "snap": snap, "calendar": calendar_counts(root)})
                return

            if path == "/api/diff":
                snap = load_snapshot(root)
                if not snap or not snap.get("items"):
                    self.send_json({"ok": False, "error": "还没有记忆，请先点击「落笔成记」"})
                    return
                live = scan_desktop()
                by_name = {d["n"]: d for d in live}
                move_count = 0
                for s in snap["items"]:
                    cur = by_name.get(s["n"])
                    if cur and (abs(cur["x"] - s["x"]) > 2 or abs(cur["y"] - s["y"]) > 2):
                        move_count += 1
                self.send_json({"ok": True, "moveCount": move_count})
                return

            if path == "/api/restore":
                snap = load_snapshot(root)
                if not snap or not snap.get("items"):
                    self.send_json({"ok": False, "error": "还没有记忆，请先点击「落笔成记」"})
                    return
                moved = apply_desktop_positions(snap["items"])
                self.send_json({"ok": True, "moved": moved})
                return

            if path == "/api/apply_draft":
                items = body.get("items", [])
                absorbs = body.get("absorbs", [])
                if not items and not absorbs:
                    self.send_json({"ok": False, "error": "草稿为空"})
                    return
                # 应用前先自动备份当前真实桌面，确保 100% 可回溯
                sc = get_screen_info()
                absorbed_ok = 0
                if IS_MAC:
                    before_items = scan_desktop()
                    before_snap = {
                        "savedAt": datetime.now().strftime("%Y-%m-%d %H:%M:%S"),
                        "note": "布阵前 " + datetime.now().strftime("%H:%M"),
                        "count": len(before_items),
                        "screenW": sc["screenW"],
                        "screenH": sc["screenH"],
                        "scale": sc["scale"],
                        "items": before_items,
                    }
                    save_snapshot(root, before_snap, backup=True)

                    # 执行用户显式确认的「拖入文件夹收纳」（安全防覆盖编号，绝不删文件）
                    desk = os.path.expanduser("~/Desktop")
                    for ab in absorbs:
                        src_name = ab.get("src", "")
                        dst_folder = ab.get("folder", "")
                        src_p = os.path.join(desk, src_name)
                        dst_d = os.path.join(desk, dst_folder)
                        if not src_name or not dst_folder or src_p == dst_d:
                            continue
                        if os.path.exists(src_p) and os.path.isdir(dst_d):
                            target = os.path.join(dst_d, os.path.basename(src_p))
                            if os.path.exists(target):
                                stem, ext = os.path.splitext(os.path.basename(src_p))
                                for k in range(1, 100):
                                    cand = os.path.join(dst_d, f"{stem}_{k}{ext}")
                                    if not os.path.exists(cand):
                                        target = cand
                                        break
                            try:
                                shutil.move(src_p, target)
                                absorbed_ok += 1
                            except Exception:
                                pass
                    if absorbed_ok > 0:
                        time.sleep(0.25)
                    apply_desktop_positions(items)
                next_snap = {
                    "savedAt": datetime.now().strftime("%Y-%m-%d %H:%M:%S"),
                    "note": (f"布阵收纳({absorbed_ok}) " if absorbed_ok > 0 else "棋盘布阵 ") + datetime.now().strftime("%H:%M"),
                    "count": len(items),
                    "screenW": sc["screenW"],
                    "screenH": sc["screenH"],
                    "scale": sc["scale"],
                    "items": items,
                }
                save_snapshot(root, next_snap, backup=True)
                self.send_json({"ok": True, "snap": next_snap, "absorbed": absorbed_ok, "calendar": calendar_counts(root)})
                return

            if path == "/api/rollback":
                fn = body.get("file", "")
                fp = os.path.join(root, "layouts", os.path.basename(fn))
                if not os.path.exists(fp):
                    self.send_json({"ok": False, "error": "历史快照不存在"})
                    return
                cur = load_snapshot(root)
                if cur and cur.get("items"):
                    save_snapshot(root, cur, backup=True)
                with open(fp, "r", encoding="utf-8-sig") as f:
                    chosen = json.load(f)
                save_snapshot(root, chosen, backup=False)
                self.send_json({"ok": True, "snap": chosen, "calendar": calendar_counts(root)})
                return

            if path == "/api/config":
                for k in ("dark", "seenGuide"):
                    if k in body:
                        cfg[k] = bool(body[k])
                save_config(cfg)
                self.send_json({"ok": True, "config": cfg})
                return

            if path == "/api/open_dir":
                target = root if body.get("which") != "exe" else BASE_DIR
                ensure_dir(target)
                if IS_MAC:
                    subprocess.Popen(["open", target])
                self.send_json({"ok": True, "path": target})
                return

            if path == "/api/choose_dir":
                if not IS_MAC:
                    self.send_json({"ok": False, "error": "仅 macOS 支持原生文件夹选择"})
                    return
                chosen = run_osascript('POSIX path of (choose folder with prompt "选择「墨守」快照与历史存址（支持任意中英文路径）")')
                chosen = chosen.rstrip("/")
                if not chosen or not can_write(chosen):
                    self.send_json({"ok": False, "error": "所选目录不可写"})
                    return
                migrate_data(root, chosen)
                cfg["dataDir"] = chosen
                save_config(cfg)
                self.send_json({"ok": True, "root": chosen})
                return

            if path == "/api/quit":
                self.send_json({"ok": True})
                threading.Timer(0.25, lambda: os._exit(0)).start()
                return

            self.send_json({"ok": False, "error": "Unknown endpoint"}, 404)
        except Exception as ex:
            self.send_json({"ok": False, "error": str(ex)}, 200)


class ReusableTCPServer(socketserver.ThreadingMixIn, socketserver.TCPServer):
    allow_reuse_address = True
    daemon_threads = True


def main():
    port = 0
    if "--port" in sys.argv:
        idx = sys.argv.index("--port")
        if idx + 1 < len(sys.argv):
            port = int(sys.argv[idx + 1])

    no_open = "--no-open" in sys.argv
    srv = ReusableTCPServer(("127.0.0.1", port), MoShouHandler)
    actual_port = srv.server_address[1]
    url = f"http://127.0.0.1:{actual_port}/"
    print(f"MoShou macOS running at {url}", flush=True)
    if not no_open:
        threading.Timer(0.3, lambda: webbrowser.open(url)).start()
    try:
        srv.serve_forever()
    except KeyboardInterrupt:
        pass
    finally:
        srv.server_close()


if __name__ == "__main__":
    main()
