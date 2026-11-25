// LayoutKeeper - 桌面布局管家 · 逻辑核心
// 复用 LayoutCore 已验证的 LVM 消息方案（0x1010 读坐标 / 0x100F 写坐标）
// 仅操作图标坐标，绝不移动、改名或删除任何桌面文件
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;

namespace LayoutKeeper
{
    public class DeskIcon
    {
        public int Index { get; set; }
        public int X { get; set; }
        public int Y { get; set; }
        public string Name { get; set; }
    }

    public class ScreenState
    {
        public int PhysW { get; set; }
        public int PhysH { get; set; }
        public int LogicalW { get; set; }
        public int LogicalH { get; set; }
        public double Scale { get; set; }
        public int Dpi { get; set; }
        public string DpiSource { get; set; }
        public int Dx { get; set; }
        public int Dy { get; set; }

        public string ScaleText
        {
            get { return ((int)Math.Round(Scale * 100)) + "%"; }
        }
    }

    public static class Desk
    {
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        static extern IntPtr FindWindow(string cls, string win);
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        static extern IntPtr FindWindowEx(IntPtr parent, IntPtr child, string cls, string win);
        [DllImport("user32.dll")]
        static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);
        [DllImport("user32.dll")]
        static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint pid);
        [DllImport("kernel32.dll")]
        static extern IntPtr OpenProcess(uint access, bool inherit, uint pid);
        [DllImport("kernel32.dll")]
        static extern bool CloseHandle(IntPtr h);
        [DllImport("kernel32.dll")]
        static extern IntPtr VirtualAllocEx(IntPtr h, IntPtr addr, uint size, uint type, uint protect);
        [DllImport("kernel32.dll")]
        static extern bool VirtualFreeEx(IntPtr h, IntPtr addr, uint size, uint type);
        [DllImport("kernel32.dll")]
        static extern bool ReadProcessMemory(IntPtr h, IntPtr addr, byte[] buf, uint size, out uint read);
        [DllImport("kernel32.dll")]
        static extern bool WriteProcessMemory(IntPtr h, IntPtr addr, byte[] buf, uint size, out uint written);

        [StructLayout(LayoutKind.Sequential)]
        struct LVITEM
        {
            public uint mask;
            public int iItem;
            public int iSubItem;
            public uint state;
            public uint stateMask;
            public IntPtr pszText;
            public int cchTextMax;
            public int iImage;
            public IntPtr lParam;
            public int iIndent;
            public int iGroupId;
            public uint cColumns;
            public IntPtr puColumns;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        struct DEVMODE
        {
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)] public string dmDeviceName;
            public short dmSpecVersion; public short dmDriverVersion; public short dmSize; public short dmDriverExtra;
            public int dmFields;
            public int dmPositionX; public int dmPositionY; public int dmDisplayOrientation; public int dmDisplayFixedOutput;
            public short dmColor; public short dmDuplex; public short dmYResolution; public short dmTTOption; public short dmCollate;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)] public string dmFormName;
            public short dmLogPixels;
            public int dmBitsPerPel; public int dmPelsWidth; public int dmPelsHeight;
            public int dmDisplayFlags; public int dmDisplayFrequency;
            public int dmICMMethod; public int dmICMIntent; public int dmMediaType; public int dmDitherType;
            public int dmReserved1; public int dmReserved2; public int dmPanningWidth; public int dmPanningHeight;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        struct DISPLAY_DEVICE
        {
            public int cb;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)] public string DeviceName;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)] public string DeviceString;
            public int StateFlags;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)] public string DeviceID;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)] public string DeviceKey;
        }

        [StructLayout(LayoutKind.Sequential)]
        struct POINT { public int X; public int Y; }

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        static extern bool EnumDisplaySettings(string dev, int mode, ref DEVMODE dm);
        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        static extern bool EnumDisplayDevices(string dev, int num, ref DISPLAY_DEVICE dd, int flags);
        [DllImport("user32.dll")]
        static extern IntPtr MonitorFromPoint(POINT pt, int flags);
        [DllImport("shcore.dll")]
        static extern int GetDpiForMonitor(IntPtr monitor, int dpiType, out uint dpiX, out uint dpiY);
        [DllImport("user32.dll")]
        static extern bool SetProcessDpiAwarenessContext(IntPtr value);

        public static bool EnablePerMonitorDpi()
        {
            try { return SetProcessDpiAwarenessContext(new IntPtr(-4)); }
            catch { return false; }
        }

        public static IntPtr GetListView()
        {
            IntPtr progman = FindWindow("Progman", null);
            IntPtr view = FindWindowEx(progman, IntPtr.Zero, "SHELLDLL_DefView", null);
            if (view == IntPtr.Zero)
            {
                IntPtr worker = IntPtr.Zero;
                while ((worker = FindWindowEx(IntPtr.Zero, worker, "WorkerW", null)) != IntPtr.Zero)
                {
                    IntPtr inner = FindWindowEx(worker, IntPtr.Zero, "SHELLDLL_DefView", null);
                    if (inner != IntPtr.Zero) { view = inner; break; }
                }
            }
            if (view == IntPtr.Zero) return IntPtr.Zero;
            return FindWindowEx(view, IntPtr.Zero, "SysListView32", "FolderView");
        }

        public static List<DeskIcon> Scan()
        {
            List<DeskIcon> list = new List<DeskIcon>();
            IntPtr lv = GetListView();
            if (lv == IntPtr.Zero) throw new Exception("找不到桌面图标列表（explorer 可能未运行）");

            uint pid = 0;
            GetWindowThreadProcessId(lv, out pid);
            IntPtr hp = OpenProcess(0x38, false, pid);
            if (hp == IntPtr.Zero) throw new Exception("无法打开 explorer 进程（拒绝访问）");

            int itemSize = Marshal.SizeOf(typeof(LVITEM));
            IntPtr itemBuf = VirtualAllocEx(hp, IntPtr.Zero, (uint)(itemSize + 8), 0x3000, 0x04);
            IntPtr textBuf = VirtualAllocEx(hp, IntPtr.Zero, 1040, 0x3000, 0x04);
            IntPtr posBuf = VirtualAllocEx(hp, IntPtr.Zero, 16, 0x3000, 0x04);

            try
            {
                int count = (int)SendMessage(lv, 0x1004, IntPtr.Zero, IntPtr.Zero);
                byte[] nameBytes = new byte[1040];
                byte[] posBytes = new byte[16];
                uint written = 0, read = 0;

                for (int i = 0; i < count; i++)
                {
                    LVITEM it = new LVITEM();
                    it.mask = 0x1;
                    it.iSubItem = 0;
                    it.pszText = textBuf;
                    it.cchTextMax = 260;

                    byte[] structBytes = new byte[itemSize];
                    IntPtr tmp = Marshal.AllocHGlobal(itemSize);
                    Marshal.StructureToPtr(it, tmp, false);
                    Marshal.Copy(tmp, structBytes, 0, itemSize);
                    Marshal.FreeHGlobal(tmp);
                    WriteProcessMemory(hp, itemBuf, structBytes, (uint)itemSize, out written);

                    IntPtr lenRet = SendMessage(lv, 0x1073, (IntPtr)i, itemBuf);
                    Array.Clear(nameBytes, 0, nameBytes.Length);
                    ReadProcessMemory(hp, textBuf, nameBytes, (uint)nameBytes.Length, out read);
                    int nameChars = Math.Min(Math.Max((int)lenRet * 2, 0), nameBytes.Length);
                    string nm = Encoding.Unicode.GetString(nameBytes, 0, nameChars).TrimEnd('\0');

                    SendMessage(lv, 0x1010, (IntPtr)i, posBuf);
                    Array.Clear(posBytes, 0, posBytes.Length);
                    ReadProcessMemory(hp, posBuf, posBytes, 8, out read);

                    DeskIcon di = new DeskIcon();
                    di.Index = i;
                    di.X = BitConverter.ToInt32(posBytes, 0);
                    di.Y = BitConverter.ToInt32(posBytes, 4);
                    di.Name = nm;
                    list.Add(di);
                }
            }
            finally
            {
                VirtualFreeEx(hp, itemBuf, 0, 0x8000);
                VirtualFreeEx(hp, textBuf, 0, 0x8000);
                VirtualFreeEx(hp, posBuf, 0, 0x8000);
                CloseHandle(hp);
            }
            return list;
        }

        public static void SetPositions(int[] indices, int[] xs, int[] ys)
        {
            IntPtr lv = GetListView();
            if (lv == IntPtr.Zero) throw new Exception("找不到桌面图标列表");
            for (int k = 0; k < indices.Length; k++)
            {
                long xv = (long)(xs[k] & 0xFFFF);
                long yv = (long)(ys[k] & 0xFFFF);
                long lp = (yv << 16) | xv;
                SendMessage(lv, 0x100F, (IntPtr)indices[k], (IntPtr)lp);
            }
        }

        public static void GetSpacing(out int dx, out int dy)
        {
            dx = 95; dy = 122;
            IntPtr lv = GetListView();
            if (lv == IntPtr.Zero) return;
            long ret = (long)SendMessage(lv, 0x1033, IntPtr.Zero, IntPtr.Zero);
            int cx = (int)(ret & 0xFFFF);
            int cy = (int)((ret >> 16) & 0xFFFF);
            if (cx > 0) dx = cx;
            if (cy > 0) dy = cy;
        }

        public static ScreenState GetScreen()
        {
            ScreenState s = new ScreenState();

            int physW = 0, physH = 0, dmLog = 0;
            DISPLAY_DEVICE dd = new DISPLAY_DEVICE();
            dd.cb = Marshal.SizeOf(typeof(DISPLAY_DEVICE));
            string active = null;
            for (int i = 0; EnumDisplayDevices(null, i, ref dd, 0); i++)
            {
                if ((dd.StateFlags & 0x1) != 0) { active = dd.DeviceName; break; }
            }
            DEVMODE dm = new DEVMODE();
            dm.dmSize = (short)Marshal.SizeOf(typeof(DEVMODE));
            bool ok = false;
            if (active != null) ok = EnumDisplaySettings(active, -1, ref dm);
            if (!ok)
            {
                dm = new DEVMODE();
                dm.dmSize = (short)Marshal.SizeOf(typeof(DEVMODE));
                ok = EnumDisplaySettings(null, -1, ref dm);
            }
            if (ok)
            {
                physW = dm.dmPelsWidth;
                physH = dm.dmPelsHeight;
                dmLog = dm.dmLogPixels;
            }

            int dpi = 0;
            string src = "";
            try
            {
                IntPtr mon = MonitorFromPoint(new POINT { X = 0, Y = 0 }, 1);
                if (mon != IntPtr.Zero)
                {
                    uint dpiX = 0, dpiY = 0;
                    if (GetDpiForMonitor(mon, 0, out dpiX, out dpiY) == 0 && dpiX > 0)
                    {
                        dpi = (int)dpiX;
                        src = "GetDpiForMonitor";
                    }
                }
            }
            catch { }
            if (dpi <= 0 && dmLog >= 96) { dpi = dmLog; src = "DEVMODE"; }
            if (dpi <= 0) { dpi = 96; src = "default"; }

            double scale = Math.Round(dpi / 96.0, 3);
            if (scale <= 0) scale = 1;

            s.PhysW = physW;
            s.PhysH = physH;
            s.Dpi = dpi;
            s.DpiSource = src;
            s.Scale = scale;
            s.LogicalW = (int)Math.Round(physW / scale);
            s.LogicalH = (int)Math.Round(physH / scale);

            int dx = 0, dy2 = 0;
            GetSpacing(out dx, out dy2);
            s.Dx = dx;
            s.Dy = dy2;
            return s;
        }
    }

    public class Snapshot
    {
        public string SavedAt = "";
        public string Note = "";
        public int Count = 0;
        public int ScreenW = 0;
        public int ScreenH = 0;
        public double Scale = 0;
        public int LogicalW = 0;
        public int LogicalH = 0;
        public int GridX0 = 0;
        public int GridDx = 0;
        public int GridY0 = 0;
        public int GridDy = 0;
        public int GridRows = 0;
        public List<DeskIcon> Items = new List<DeskIcon>();
    }

    public static class JsonStore
    {
        static string Esc(string s)
        {
            if (s == null) return "";
            StringBuilder sb = new StringBuilder();
            foreach (char c in s)
            {
                switch (c)
                {
                    case '\\': sb.Append("\\\\"); break;
                    case '"': sb.Append("\\\""); break;
                    case '\n': sb.Append("\\n"); break;
                    case '\r': sb.Append("\\r"); break;
                    case '\t': sb.Append("\\t"); break;
                    default:
                        if (c < 0x20) sb.Append("\\u").Append(((int)c).ToString("x4"));
                        else sb.Append(c);
                        break;
                }
            }
            return sb.ToString();
        }

        public static void Save(string path, Snapshot s)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("{\r\n");
            sb.Append("  \"savedAt\": \"").Append(Esc(s.SavedAt)).Append("\",\r\n");
            sb.Append("  \"note\": \"").Append(Esc(s.Note)).Append("\",\r\n");
            sb.Append("  \"count\": ").Append(s.Count).Append(",\r\n");
            sb.Append("  \"screenW\": ").Append(s.ScreenW).Append(",\r\n");
            sb.Append("  \"screenH\": ").Append(s.ScreenH).Append(",\r\n");
            sb.Append("  \"scale\": ").Append(s.Scale.ToString(System.Globalization.CultureInfo.InvariantCulture)).Append(",\r\n");
            sb.Append("  \"logicalW\": ").Append(s.LogicalW).Append(",\r\n");
            sb.Append("  \"logicalH\": ").Append(s.LogicalH).Append(",\r\n");
            sb.Append("  \"grid\": { \"x0\": ").Append(s.GridX0)
              .Append(", \"dx\": ").Append(s.GridDx)
              .Append(", \"y0\": ").Append(s.GridY0)
              .Append(", \"dy\": ").Append(s.GridDy)
              .Append(", \"rows\": ").Append(s.GridRows).Append(" },\r\n");
            sb.Append("  \"items\": [\r\n");
            for (int i = 0; i < s.Items.Count; i++)
            {
                DeskIcon d = s.Items[i];
                sb.Append("    { \"n\": \"").Append(Esc(d.Name)).Append("\", \"x\": ").Append(d.X)
                  .Append(", \"y\": ").Append(d.Y).Append(" }");
                if (i < s.Items.Count - 1) sb.Append(",");
                sb.Append("\r\n");
            }
            sb.Append("  ]\r\n}\r\n");
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            File.WriteAllText(path, sb.ToString(), new UTF8Encoding(true));
        }

        static string ReadField(string text, string key)
        {
            Match m = Regex.Match(text, "\"" + key + "\"\\s*:\\s*\"((?:\\\\.|[^\"\\\\])*)\"");
            if (!m.Success) return null;
            return Unescape(m.Groups[1].Value);
        }

        static int ReadInt(string text, string key)
        {
            Match m = Regex.Match(text, "\"" + key + "\"\\s*:\\s*(-?\\d+)");
            if (!m.Success) return 0;
            return int.Parse(m.Groups[1].Value);
        }

        static double ReadDouble(string text, string key)
        {
            Match m = Regex.Match(text, "\"" + key + "\"\\s*:\\s*(-?\\d+(?:\\.\\d+)?)");
            if (!m.Success) return 0;
            return double.Parse(m.Groups[1].Value, System.Globalization.CultureInfo.InvariantCulture);
        }

        static string Unescape(string s)
        {
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < s.Length; i++)
            {
                char c = s[i];
                if (c == '\\' && i + 1 < s.Length)
                {
                    char n = s[i + 1];
                    i++;
                    switch (n)
                    {
                        case 'n': sb.Append('\n'); break;
                        case 'r': sb.Append('\r'); break;
                        case 't': sb.Append('\t'); break;
                        case '"': sb.Append('"'); break;
                        case '\\': sb.Append('\\'); break;
                        case 'u':
                            if (i + 4 < s.Length)
                            {
                                sb.Append((char)Convert.ToInt32(s.Substring(i + 1, 4), 16));
                                i += 4;
                            }
                            break;
                        default: sb.Append(n); break;
                    }
                }
                else sb.Append(c);
            }
            return sb.ToString();
        }

        public static Snapshot Load(string path)
        {
            if (!File.Exists(path)) return null;
            string text;
            using (StreamReader sr = new StreamReader(path, new UTF8Encoding(false)))
            {
                text = sr.ReadToEnd();
            }
            if (text.Length > 0 && text[0] == '\uFEFF') text = text.Substring(1);

            Snapshot s = new Snapshot();
            string v;
            if ((v = ReadField(text, "savedAt")) != null) s.SavedAt = v;
            if ((v = ReadField(text, "note")) != null) s.Note = v;
            s.Count = ReadInt(text, "count");
            s.ScreenW = ReadInt(text, "screenW");
            s.ScreenH = ReadInt(text, "screenH");
            s.Scale = ReadDouble(text, "scale");
            s.LogicalW = ReadInt(text, "logicalW");
            s.LogicalH = ReadInt(text, "logicalH");

            Match gm = Regex.Match(text, "\"grid\"\\s*:\\s*\\{([^}]*)\\}");
            if (gm.Success)
            {
                string g = gm.Groups[1].Value;
                s.GridX0 = ReadInt(g, "x0");
                s.GridDx = ReadInt(g, "dx");
                s.GridY0 = ReadInt(g, "y0");
                s.GridDy = ReadInt(g, "dy");
                s.GridRows = ReadInt(g, "rows");
            }

            Match im = Regex.Match(text, "\"items\"\\s*:\\s*\\[([\\s\\S]*)\\]");
            if (im.Success)
            {
                MatchCollection rows = Regex.Matches(im.Groups[1].Value, "\\{([^}]*)\\}");
                foreach (Match row in rows)
                {
                    string r = row.Groups[1].Value;
                    Match nm = Regex.Match(r, "\"n\"\\s*:\\s*\"((?:\\\\.|[^\"\\\\])*)\"");
                    int x = ReadInt(r, "x");
                    int y = ReadInt(r, "y");
                    if (!nm.Success) continue;
                    DeskIcon d = new DeskIcon();
                    d.Name = Unescape(nm.Groups[1].Value);
                    d.X = x;
                    d.Y = y;
                    d.Index = s.Items.Count;
                    s.Items.Add(d);
                }
            }
            return s;
        }
    }

    public class DiffResult
    {
        public List<DeskIcon> Target = new List<DeskIcon>();      // 目标（含新增补位）
        public List<int> MoveIndices = new List<int>();           // 需要移动的当前索引
        public List<DeskIcon> MoveTargets = new List<DeskIcon>();  // 对应的目标坐标
        public List<string> NewOnes = new List<string>();          // 快照里没有的新图标
        public List<string> Missing = new List<string>();          // 快照里有但已消失的
        public int MatchCount = 0;
        public int AlreadyOk = 0;
    }

    public static class Engine
    {
        public static string Today()
        {
            return DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        }

        public static Snapshot Capture(string note)
        {
            ScreenState sc = Desk.GetScreen();
            List<DeskIcon> items = Desk.Scan();

            Snapshot s = new Snapshot();
            s.SavedAt = Today();
            s.Note = note ?? "";
            s.Count = items.Count;
            s.ScreenW = sc.PhysW;
            s.ScreenH = sc.PhysH;
            s.Scale = sc.Scale;
            s.LogicalW = sc.LogicalW;
            s.LogicalH = sc.LogicalH;
            s.GridDx = sc.Dx;
            s.GridDy = sc.Dy;
            s.Items = items;

            // 推断网格原点与行数（取最小 x / y 作为原点，最大行数为行数）
            if (items.Count > 0)
            {
                int minX = int.MaxValue, minY = int.MaxValue, maxY = int.MinValue;
                foreach (DeskIcon d in items)
                {
                    if (d.X < minX) minX = d.X;
                    if (d.Y < minY) minY = d.Y;
                    if (d.Y > maxY) maxY = d.Y;
                }
                s.GridX0 = minX;
                s.GridY0 = minY;
                s.GridRows = sc.Dy > 0 ? (int)Math.Round((maxY - minY) / (double)sc.Dy) + 1 : 0;
            }
            return s;
        }

        public static void Backup(string root, Snapshot s)
        {
            string dir = Path.Combine(root, "layouts");
            Directory.CreateDirectory(dir);
            string stamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            string path = Path.Combine(dir, "layout_" + stamp + ".json");
            JsonStore.Save(path, s);

            // 只保留最近 12 份
            string[] files = Directory.GetFiles(dir, "layout_*.json");
            if (files.Length > 12)
            {
                Array.Sort(files, StringComparer.OrdinalIgnoreCase);
                for (int i = 0; i < files.Length - 12; i++)
                {
                    try { File.Delete(files[i]); } catch { }
                }
            }
        }

        public static Snapshot LoadSnapshot(string root)
        {
            return JsonStore.Load(Path.Combine(root, "layout_snapshot.json"));
        }

        public static DiffResult Diff(Snapshot snap, List<DeskIcon> current, ScreenState sc)
        {
            DiffResult r = new DiffResult();

            Dictionary<string, DeskIcon> byName = new Dictionary<string, DeskIcon>();
            foreach (DeskIcon d in current)
            {
                if (d.Name.Length == 0) continue;
                if (!byName.ContainsKey(d.Name)) byName[d.Name] = d;
            }

            bool needScale = snap.ScreenW > 0 && snap.ScreenH > 0
                && (snap.ScreenW != sc.PhysW || snap.ScreenH != sc.PhysH);

            List<DeskIcon> targets = new List<DeskIcon>();
            foreach (DeskIcon s in snap.Items)
            {
                DeskIcon t = new DeskIcon();
                t.Name = s.Name;
                if (needScale)
                {
                    t.X = (int)Math.Round(s.X * sc.PhysW / (double)snap.ScreenW);
                    t.Y = (int)Math.Round(s.Y * sc.PhysH / (double)snap.ScreenH);
                    if (t.X > sc.PhysW - 60) t.X = sc.PhysW - 60;
                    if (t.Y > sc.PhysH - 80) t.Y = sc.PhysH - 80;
                    if (t.X < 0) t.X = 0;
                    if (t.Y < 0) t.Y = 0;
                }
                else
                {
                    t.X = s.X;
                    t.Y = s.Y;
                }
                targets.Add(t);
            }

            HashSet<string> snapNames = new HashSet<string>(StringComparer.Ordinal);
            foreach (DeskIcon s in snap.Items) snapNames.Add(s.Name);

            // 新增图标：快照里没有的，补到当前最大列之后第一个空位之前的最近空位
            int dx = sc.Dx > 0 ? sc.Dx : 95;
            int dy = sc.Dy > 0 ? sc.Dy : 122;
            HashSet<string> used = new HashSet<string>(StringComparer.Ordinal);
            foreach (DeskIcon t in targets)
                used.Add(t.X + "," + t.Y);

            int colStart = 0;
            int colEnd = 0;
            int rowTop = 0;
            if (targets.Count > 0)
            {
                int minX = int.MaxValue, minY = int.MaxValue, maxX = int.MinValue, maxY = int.MinValue;
                foreach (DeskIcon t in targets)
                {
                    if (t.X < minX) minX = t.X;
                    if (t.Y < minY) minY = t.Y;
                    if (t.X > maxX) maxX = t.X;
                    if (t.Y > maxY) maxY = t.Y;
                }
                colStart = minX;
                colEnd = maxX;
                rowTop = minY;
            }

            foreach (DeskIcon d in current)
            {
                if (d.Name.Length == 0) continue;
                if (snapNames.Contains(d.Name)) continue;

                r.NewOnes.Add(d.Name);

                int px = 0, py = 0;
                bool placed = false;
                for (int c = colStart; c <= colEnd + dx && !placed; c += dx)
                {
                    for (int y = rowTop; y < sc.PhysH - 80 && !placed; y += dy)
                    {
                        if (!used.Contains(c + "," + y))
                        {
                            px = c; py = y; placed = true;
                        }
                    }
                }
                if (!placed) { px = colEnd + dx; py = rowTop; }
                used.Add(px + "," + py);

                DeskIcon t = new DeskIcon();
                t.Name = d.Name;
                t.X = px;
                t.Y = py;
                targets.Add(t);
            }

            // 快照里有但已消失
            HashSet<string> curNames = new HashSet<string>(StringComparer.Ordinal);
            foreach (DeskIcon d in current) curNames.Add(d.Name);
            foreach (DeskIcon s in snap.Items)
            {
                if (!curNames.Contains(s.Name)) r.Missing.Add(s.Name);
            }

            // 计算需要移动的
            foreach (DeskIcon t in targets)
            {
                DeskIcon c = byName.ContainsKey(t.Name) ? byName[t.Name] : null;
                if (c == null) continue;
                r.MatchCount++;
                if (c.X == t.X && c.Y == t.Y)
                {
                    r.AlreadyOk++;
                }
                else
                {
                    r.MoveIndices.Add(c.Index);
                    r.MoveTargets.Add(t);
                }
            }

            r.Target = targets;
            return r;
        }

        // 多轮迭代写入，抵消吸附网格造成的挤位（最多 6 轮）
        public static int Apply(Snapshot snap, int maxRounds)
        {
            int totalMoves = 0;
            for (int round = 0; round < maxRounds; round++)
            {
                ScreenState sc = Desk.GetScreen();
                List<DeskIcon> current = Desk.Scan();
                DiffResult diff = Diff(snap, current, sc);
                if (diff.MoveIndices.Count == 0) break;

                int[] xs = new int[diff.MoveTargets.Count];
                int[] ys = new int[diff.MoveTargets.Count];
                for (int i = 0; i < diff.MoveTargets.Count; i++)
                {
                    xs[i] = diff.MoveTargets[i].X;
                    ys[i] = diff.MoveTargets[i].Y;
                }
                Desk.SetPositions(diff.MoveIndices.ToArray(), xs, ys);
                totalMoves += diff.MoveIndices.Count;
                System.Threading.Thread.Sleep(120);
            }
            return totalMoves;
        }

        // 紧致补洞：按列优先重新紧凑排列，保持原有排序
        public static Snapshot Compact(Snapshot snap, ScreenState sc)
        {
            int dx = sc.Dx > 0 ? sc.Dx : 95;
            int dy = sc.Dy > 0 ? sc.Dy : 122;

            // 可用行数（留一格底部余量）
            int rows = Math.Max(1, (int)Math.Floor((sc.PhysH - 80) / (double)dy));
            int x0 = snap.GridX0 > 0 ? snap.GridX0 : 21;
            int y0 = snap.GridY0 >= 0 ? snap.GridY0 : 2;

            Snapshot result = new Snapshot();
            result.SavedAt = Today();
            result.Note = "紧致补洞";
            result.Count = snap.Items.Count;
            result.ScreenW = sc.PhysW;
            result.ScreenH = sc.PhysH;
            result.Scale = sc.Scale;
            result.LogicalW = sc.LogicalW;
            result.LogicalH = sc.LogicalH;
            result.GridX0 = x0;
            result.GridDx = dx;
            result.GridY0 = y0;
            result.GridDy = dy;
            result.GridRows = rows;

            // 保持原顺序：按原坐标的「列 → 行」排序
            List<DeskIcon> ordered = new List<DeskIcon>(snap.Items);
            ordered.Sort(delegate (DeskIcon a, DeskIcon b)
            {
                int ca = (a.X - x0) / dx;
                int cb = (b.X - x0) / dx;
                if (ca != cb) return ca.CompareTo(cb);
                if (a.Y != b.Y) return a.Y.CompareTo(b.Y);
                return a.X.CompareTo(b.X);
            });

            for (int i = 0; i < ordered.Count; i++)
            {
                DeskIcon d = ordered[i];
                int col = i / rows;
                int row = i % rows;
                DeskIcon t = new DeskIcon();
                t.Name = d.Name;
                t.X = x0 + col * dx;
                t.Y = y0 + row * dy;
                result.Items.Add(t);
            }
            return result;
        }
    }
}
