import Cocoa
import WebKit

class MoShouNSWindow: NSWindow {
    override var canBecomeKey: Bool { return true }
    override var canBecomeMain: Bool { return true }
}

class AppDelegate: NSObject, NSApplicationDelegate, WKScriptMessageHandler, WKUIDelegate, NSWindowDelegate {
    var window: MoShouNSWindow!
    var webView: WKWebView!
    var pyProc: Process?
    var loadedUrl = false

    func applicationDidFinishLaunching(_ notification: Notification) {
        setupMainMenu()

        let initW: CGFloat = 740
        let initH: CGFloat = 778
        let rect = NSRect(x: 0, y: 0, width: initW, height: initH)
        window = MoShouNSWindow(
            contentRect: rect,
            styleMask: [.borderless, .resizable, .miniaturizable],
            backing: .buffered,
            defer: false
        )
        window.title = "墨守"
        window.isOpaque = false
        window.backgroundColor = .clear
        window.hasShadow = true
        window.aspectRatio = NSSize(width: 462, height: 486)
        window.minSize = NSSize(width: 462, height: 486)
        window.isMovableByWindowBackground = true
        window.delegate = self
        window.center()

        let cfg = WKWebViewConfiguration()
        cfg.userContentController.add(self, name: "moshouHost")
        webView = WKWebView(frame: rect, configuration: cfg)
        webView.autoresizingMask = [.width, .height]
        webView.setValue(false, forKey: "drawsBackground")
        webView.uiDelegate = self
        webView.wantsLayer = true
        webView.layer?.backgroundColor = NSColor.clear.cgColor

        window.contentView = webView
        window.makeKeyAndOrderFront(nil)
        NSApp.activate(ignoringOtherApps: true)

        startBackend()
    }

    func setupMainMenu() {
        let mainMenu = NSMenu()
        let appItem = NSMenuItem()
        mainMenu.addItem(appItem)
        let appMenu = NSMenu(title: "墨守")
        appMenu.addItem(withTitle: "隐藏墨守", action: #selector(NSApplication.hide(_:)), keyEquivalent: "h")
        appMenu.addItem(NSMenuItem.separator())
        appMenu.addItem(withTitle: "退出墨守", action: #selector(NSApplication.terminate(_:)), keyEquivalent: "q")
        appItem.submenu = appMenu

        let editItem = NSMenuItem()
        mainMenu.addItem(editItem)
        let editMenu = NSMenu(title: "编辑")
        editMenu.addItem(withTitle: "撤销", action: Selector(("undo:")), keyEquivalent: "z")
        editMenu.addItem(withTitle: "复制", action: #selector(NSText.copy(_:)), keyEquivalent: "c")
        editMenu.addItem(withTitle: "粘贴", action: #selector(NSText.paste(_:)), keyEquivalent: "v")
        editMenu.addItem(withTitle: "全选", action: #selector(NSText.selectAll(_:)), keyEquivalent: "a")
        editItem.submenu = editMenu
        NSApp.mainMenu = mainMenu
    }

    func findPython3() -> String {
        let candidates = ["/usr/bin/python3", "/opt/homebrew/bin/python3", "/usr/local/bin/python3"]
        for p in candidates {
            if FileManager.default.isExecutableFile(atPath: p) { return p }
        }
        return "/usr/bin/env"
    }

    func startBackend() {
        guard let resDir = Bundle.main.resourcePath else { return }
        let scriptPath = (resDir as NSString).appendingPathComponent("moshou_mac.py")
        let py = findPython3()

        let proc = Process()
        proc.executableURL = URL(fileURLWithPath: py)
        if py == "/usr/bin/env" {
            proc.arguments = ["python3", "-u", scriptPath, "--no-open", "--port", "0"]
        } else {
            proc.arguments = ["-u", scriptPath, "--no-open", "--port", "0"]
        }
        var env = ProcessInfo.processInfo.environment
        env["PYTHONUNBUFFERED"] = "1"
        proc.environment = env

        let pipe = Pipe()
        proc.standardOutput = pipe
        proc.standardError = pipe

        pipe.fileHandleForReading.readabilityHandler = { [weak self] fh in
            let data = fh.availableData
            guard !data.isEmpty, let text = String(data: data, encoding: .utf8) else { return }
            guard let self = self, !self.loadedUrl else { return }
            if let range = text.range(of: "http://127.0.0.1:[0-9]+/", options: .regularExpression) {
                let urlStr = String(text[range])
                self.loadedUrl = true
                DispatchQueue.main.async {
                    if let url = URL(string: urlStr) {
                        self.webView.load(URLRequest(url: url))
                    }
                }
            }
        }

        do {
            try proc.run()
            self.pyProc = proc
        } catch {
            DispatchQueue.main.async {
                let alert = NSAlert()
                alert.messageText = "启动墨守后端失败"
                alert.informativeText = error.localizedDescription
                alert.runModal()
                NSApp.terminate(nil)
            }
        }
    }

    func userContentController(_ userContentController: WKUserContentController, didReceive message: WKScriptMessage) {
        guard let cmd = message.body as? String else { return }
        if cmd == "close" {
            NSApp.terminate(nil)
        } else if cmd == "minimize" {
            window.miniaturize(nil)
        } else if cmd == "drag" {
            if let ev = NSApp.currentEvent {
                window.performDrag(with: ev)
            }
        } else if cmd == "toggleSize" {
            let cur = window.frame
            let targetW: CGFloat = cur.width > 640 ? 540 : 740
            let targetH: CGFloat = round(targetW * 486.0 / 462.0)
            let newX = cur.midX - targetW * 0.5
            let newY = cur.midY - targetH * 0.5
            window.setFrame(NSRect(x: newX, y: newY, width: targetW, height: targetH), display: true, animate: true)
        }
    }

    func webView(_ webView: WKWebView, runJavaScriptConfirmPanelWithMessage message: String,
                 initiatedByFrame frame: WKFrameInfo, completionHandler: @escaping (Bool) -> Void) {
        let alert = NSAlert()
        alert.messageText = "墨守"
        alert.informativeText = message
        alert.addButton(withTitle: "确认")
        alert.addButton(withTitle: "取消")
        let res = alert.runModal()
        completionHandler(res == .alertFirstButtonReturn)
    }

    func webView(_ webView: WKWebView, runJavaScriptAlertPanelWithMessage message: String,
                 initiatedByFrame frame: WKFrameInfo, completionHandler: @escaping () -> Void) {
        let alert = NSAlert()
        alert.messageText = "墨守"
        alert.informativeText = message
        alert.addButton(withTitle: "好")
        alert.runModal()
        completionHandler()
    }

    func windowWillClose(_ notification: Notification) {
        NSApp.terminate(nil)
    }

    func applicationShouldTerminateAfterLastWindowClosed(_ sender: NSApplication) -> Bool {
        return true
    }

    func applicationWillTerminate(_ notification: Notification) {
        if let proc = pyProc, proc.isRunning {
            proc.terminate()
        }
    }
}

let app = NSApplication.shared
let delegate = AppDelegate()
app.delegate = delegate
app.setActivationPolicy(.regular)
app.run()
