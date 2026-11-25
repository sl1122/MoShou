// 墨守 MoShou · 界面层
// 与 Core.cs 同一程序集编译：csc /out:墨守.exe Core.cs MoShou.cs
// 只读图标坐标，绝不移动/改名/删除文件
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using System.Windows.Shapes;
using System.Windows.Threading;
using LayoutKeeper;

namespace MoShou
{
    // ============ 色板：双材质，重建式切换 ============
    public static class Ink
    {
        public static bool Dark = false;

        public static Color C(byte r, byte g, byte b) { return Color.FromRgb(r, g, b); }
        public static Color A(byte a, byte r, byte g, byte b) { return Color.FromArgb(a, r, g, b); }

        public static Color Bg() { return Dark ? C(0x13, 0x13, 0x0E) : C(0xF4, 0xF0, 0xE6); }
        public static Color Card() { return Dark ? C(0x1A, 0x1A, 0x14) : C(0xEC, 0xE6, 0xD8); }
        public static Color BoardBg() { return Dark ? C(0x17, 0x17, 0x11) : C(0xEF, 0xE9, 0xDC); }
        public static Color Fg() { return Dark ? C(0xEF, 0xEA, 0xDC) : C(0x17, 0x16, 0x0F); }
        public static Color Fg2() { return Dark ? C(0xB8, 0xB2, 0xA2) : C(0x4B, 0x49, 0x3F); }
        public static Color Fg3() { return Dark ? C(0x7E, 0x79, 0x6C) : C(0x8A, 0x85, 0x77); }
        public static Color Hair() { return A(Dark ? (byte)0x26 : (byte)0x1E, Fg().R, Fg().G, Fg().B); }
        public static Color PrimaryBg() { return Dark ? C(0xE5, 0xDF, 0xD0) : C(0x17, 0x16, 0x0F); }
        public static Color PrimaryHoverBg() { return Dark ? C(0xEF, 0xEA, 0xDC) : C(0x23, 0x21, 0x18); }
        public static Color PrimaryFg() { return Dark ? C(0x13, 0x13, 0x0E) : C(0xF4, 0xF0, 0xE6); }
        public static Color PrimaryBorder() { return Dark ? C(0xC8, 0xBE, 0xAA) : C(0x17, 0x16, 0x0F); }
        public static Color Verm() { return Dark ? C(0xC9, 0x57, 0x3F) : C(0xA9, 0x3E, 0x30); }

        public static Color ByType(string t)
        {
            if (t == "app") return Dark ? C(0x3E, 0x6F, 0xC4) : C(0x27, 0x4C, 0x8C);
            if (t == "dir") return Dark ? C(0x6B, 0xA0, 0x77) : C(0x4E, 0x7A, 0x5C);
            if (t == "arc") return Dark ? C(0xA8, 0x78, 0xB0) : C(0x76, 0x4D, 0x7C);
            if (t == "doc") return Dark ? C(0xB5, 0x7A, 0x45) : C(0x8A, 0x5B, 0x33);
            if (t == "sys") return Dark ? C(0xD6, 0xCC, 0xB8) : C(0xC6, 0xBC, 0xA6);
            return Dark ? C(0xD6, 0xA2, 0x3C) : C(0xB8, 0x86, 0x2F);
        }

        public static SolidColorBrush Br(Color c) { return new SolidColorBrush(c); }

        // 墨点不是硬边的圆。中心实、边缘化开——硬边 Ellipse 平涂正是"机械感"的头号来源。
        public static Brush Soft(Color c)
        {
            RadialGradientBrush g = new RadialGradientBrush();
            g.Center = new Point(0.5, 0.5);
            g.GradientOrigin = new Point(0.5, 0.5);
            g.RadiusX = 0.5;
            g.RadiusY = 0.5;
            g.GradientStops.Add(new GradientStop(c, 0.0));
            g.GradientStops.Add(new GradientStop(c, 0.52));
            Color e = c;
            e.A = 0;
            g.GradientStops.Add(new GradientStop(e, 1.0));
            g.Freeze();
            return g;
        }

        // 宣纸洇墨渐变：浓芯柔边，用于按钮按压的水墨洇开（替代硬边实心圆）
        public static Brush InkWash(Color c, double coreStop)
        {
            RadialGradientBrush g = new RadialGradientBrush();
            g.Center = new Point(0.5, 0.5);
            g.GradientOrigin = new Point(0.5, 0.5);
            g.RadiusX = 0.5;
            g.RadiusY = 0.5;
            Color solid = Color.FromArgb(255, c.R, c.G, c.B);
            Color mid = Color.FromArgb(148, c.R, c.G, c.B);
            Color edge = Color.FromArgb(0, c.R, c.G, c.B);
            g.GradientStops.Add(new GradientStop(solid, 0.0));
            g.GradientStops.Add(new GradientStop(solid, coreStop));
            g.GradientStops.Add(new GradientStop(mid, coreStop + (1.0 - coreStop) * 0.48));
            g.GradientStops.Add(new GradientStop(edge, 1.0));
            g.Freeze();
            return g;
        }

        // 水痕墨边：边缘积墨、中空柔散，用于按钮外圈洇痕
        public static Brush InkRing(Color c)
        {
            RadialGradientBrush g = new RadialGradientBrush();
            g.Center = new Point(0.5, 0.5);
            g.GradientOrigin = new Point(0.5, 0.5);
            g.RadiusX = 0.5;
            g.RadiusY = 0.5;
            Color z = Color.FromArgb(0, c.R, c.G, c.B);
            Color m1 = Color.FromArgb(145, c.R, c.G, c.B);
            Color m2 = Color.FromArgb(80, c.R, c.G, c.B);
            g.GradientStops.Add(new GradientStop(z, 0.0));
            g.GradientStops.Add(new GradientStop(z, 0.60));
            g.GradientStops.Add(new GradientStop(m1, 0.78));
            g.GradientStops.Add(new GradientStop(m2, 0.89));
            g.GradientStops.Add(new GradientStop(z, 1.0));
            g.Freeze();
            return g;
        }

        // 湿痕：中空的一圈淡色，不是实心圆片
        public static Brush SoftRing(Color c)
        {
            RadialGradientBrush g = new RadialGradientBrush();
            g.Center = new Point(0.5, 0.5);
            g.GradientOrigin = new Point(0.5, 0.5);
            g.RadiusX = 0.5;
            g.RadiusY = 0.5;
            Color z = c;
            z.A = 0;
            Color m = c;
            m.A = 0x3C;
            g.GradientStops.Add(new GradientStop(z, 0.0));
            g.GradientStops.Add(new GradientStop(z, 0.50));
            g.GradientStops.Add(new GradientStop(m, 0.76));
            g.GradientStops.Add(new GradientStop(z, 1.0));
            g.Freeze();
            return g;
        }

        // Catmull-Rom 闭环三次贝塞尔有机墨团：C1 连续平滑，无折线棱角
        public static StreamGeometry OrganicBlobGeo(double dia, int seed, double amp1, double amp2, double amp3)
        {
            Random rr = new Random(seed);
            double r0 = dia * 0.5;
            int N = 24;
            double ph1 = rr.NextDouble() * 6.2832;
            double ph2 = rr.NextDouble() * 6.2832;
            double ph3 = rr.NextDouble() * 6.2832;

            Point[] pts = new Point[N];
            for (int i = 0; i < N; i++)
            {
                double a = i * 2.0 * Math.PI / N;
                double rad = r0 * (1.0
                    + amp1 * Math.Sin(3.0 * a + ph1)
                    + amp2 * Math.Sin(5.0 * a + ph2)
                    + amp3 * Math.Sin(8.0 * a + ph3));
                pts[i] = new Point(r0 + Math.Cos(a) * rad, r0 + Math.Sin(a) * rad);
            }

            StreamGeometry geo = new StreamGeometry();
            using (StreamGeometryContext ctx = geo.Open())
            {
                ctx.BeginFigure(pts[0], true, true);
                for (int i = 0; i < N; i++)
                {
                    Point p0 = pts[(i - 1 + N) % N];
                    Point p1 = pts[i];
                    Point p2 = pts[(i + 1) % N];
                    Point p3 = pts[(i + 2) % N];
                    Point c1 = new Point(p1.X + (p2.X - p0.X) / 6.0, p1.Y + (p2.Y - p0.Y) / 6.0);
                    Point c2 = new Point(p2.X - (p3.X - p1.X) / 6.0, p2.Y - (p3.Y - p1.Y) / 6.0);
                    ctx.BezierTo(c1, c2, p2, true, false);
                }
            }
            geo.Freeze();
            return geo;
        }

        public static FontFamily Serif = new FontFamily("Noto Serif SC,Source Han Serif SC,SimSun,serif");
        public static FontFamily Sans = new FontFamily("Microsoft YaHei UI,Microsoft YaHei");
        public static FontFamily Mono = new FontFamily("Consolas,Cascadia Mono");

        static readonly HashSet<string> DocExt = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        { ".txt",".doc",".docx",".pdf",".xls",".xlsx",".ppt",".pptx",".md",".csv",".rtf",".wps",".one" };
        static readonly HashSet<string> ArcExt = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        { ".zip",".rar",".7z",".tar",".gz",".bz2",".xz",".iso",".cab",".wim",".tgz" };
        static readonly HashSet<string> AppExt = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        { ".exe",".lnk",".bat",".cmd",".msi",".url",".appref-ms" };

        public static string DesktopDir()
        {
            try { return Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory); }
            catch { return ""; }
        }

        // 显示名 → 真实扩展名 的索引。
        // 资源管理器默认隐藏 .lnk 扩展名，LVM_GETITEMTEXT 取到的是显示名（"崩坏星穹铁道"），
        // 直接拼路径 File.Exists 必然为 false —— 那是"许多软件被判定成系统"的根因。
        static Dictionary<string, string> nameExt;
        static Dictionary<string, string> nameFullPath;
        static string indexedDesk;

        public static void ResetIndex() { nameExt = null; nameFullPath = null; indexedDesk = null; }

        static string CommonDesk()
        {
            try { return Environment.GetFolderPath(Environment.SpecialFolder.CommonDesktopDirectory); }
            catch { return ""; }
        }

        static void IndexDir(Dictionary<string, string> m, Dictionary<string, string> fp, string dir)
        {
            try
            {
                string[] fs = Directory.GetFiles(dir);
                for (int i = 0; i < fs.Length; i++)
                {
                    string full = System.IO.Path.GetFileName(fs[i]);
                    string bare = System.IO.Path.GetFileNameWithoutExtension(full);
                    string ext = System.IO.Path.GetExtension(full);
                    if (!m.ContainsKey(full)) { m[full] = ext; fp[full] = fs[i]; }
                    if (!m.ContainsKey(bare)) { m[bare] = ext; fp[bare] = fs[i]; }
                }
                string[] ds = Directory.GetDirectories(dir);
                for (int i = 0; i < ds.Length; i++)
                {
                    string bn = System.IO.Path.GetFileName(ds[i]);
                    if (!m.ContainsKey(bn)) { m[bn] = "<dir>"; fp[bn] = ds[i]; }
                }
            }
            catch { }
        }

        static void EnsureIndex(string desk)
        {
            string cd = CommonDesk();
            string key = desk + "|" + cd;
            if (nameExt != null && indexedDesk == key) return;

            Dictionary<string, string> m = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            Dictionary<string, string> fp = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            IndexDir(m, fp, desk);
            if (cd.Length > 0 && !cd.Equals(desk, StringComparison.OrdinalIgnoreCase)) IndexDir(m, fp, cd);

            nameExt = m;
            nameFullPath = fp;
            indexedDesk = key;
        }

        public static string ResolveRealPath(string name, string desk)
        {
            if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(desk)) return null;
            EnsureIndex(desk);
            if (nameFullPath != null && nameFullPath.ContainsKey(name)) return nameFullPath[name];
            string p = System.IO.Path.Combine(desk, name);
            if (File.Exists(p) || Directory.Exists(p)) return p;
            return null;
        }

        // 先按真实文件名匹配，再回退到显示名索引；两者都没有才是虚拟项
        public static string TypeOf(string name, string desk)
        {
            try
            {
                if (string.IsNullOrEmpty(desk)) return "other";
                EnsureIndex(desk);

                string ext = null;
                if (nameExt != null && nameExt.ContainsKey(name)) ext = nameExt[name];
                else
                {
                    string p = System.IO.Path.Combine(desk, name);
                    if (Directory.Exists(p)) return "dir";
                    if (File.Exists(p)) ext = System.IO.Path.GetExtension(name);
                }

                if (ext == "<dir>") return "dir";
                if (ext == null) return "sys";          // 真是虚拟项：此电脑 / 回收站 / 用户文件夹
                if (ArcExt.Contains(ext)) return "arc";
                if (DocExt.Contains(ext)) return "doc";
                if (AppExt.Contains(ext)) return "app";
                return "other";
            }
            catch { }
            return "other";
        }
    }

    // ============ 水墨按钮：文字严格居中，按压有机墨晕洇开 + 四角回纹生长 ============
    public class InkButton : Border
    {
        public TextBlock Label;
        Canvas ripHost;
        Canvas deco;
        ScaleTransform decoScale;
        System.Collections.Generic.List<System.Windows.Shapes.Shape> decoPaths;
        ScaleTransform scale;
        SolidColorBrush bgBrush;
        SolidColorBrush borderBrush;
        bool primary;
        bool isPressed = false;
        int pressSeq = 0;
        double decoDashLen = 120;
        public bool RetractMode = false;

        public InkButton(string text, bool isPrimary)
        {
            primary = isPrimary;
            Height = 52;
            CornerRadius = new CornerRadius(10);
            Cursor = Cursors.Hand;
            bgBrush = new SolidColorBrush(Colors.Transparent);
            borderBrush = new SolidColorBrush(Colors.Transparent);
            Background = bgBrush;
            BorderBrush = borderBrush;
            BorderThickness = new Thickness(1);
            RenderTransformOrigin = new Point(0.5, 0.5);
            scale = new ScaleTransform(1, 1);
            RenderTransform = scale;

            Grid g = new Grid();
            ripHost = new Canvas();
            ripHost.IsHitTestVisible = false;
            ripHost.ClipToBounds = true;
            ripHost.SizeChanged += delegate
            {
                ripHost.Clip = new RectangleGeometry(
                    new Rect(0, 0, ripHost.ActualWidth, ripHost.ActualHeight), 10, 10);
            };
            g.Children.Add(ripHost);

            // 四角回纹：只在按下时从内向外生长，抬起停一拍后轻收淡出；绝不常驻
            deco = new Canvas();
            deco.IsHitTestVisible = false;
            deco.ClipToBounds = false;
            deco.RenderTransformOrigin = new Point(0.5, 0.5);
            decoScale = new ScaleTransform(0.92, 0.92);
            deco.RenderTransform = decoScale;
            deco.SizeChanged += delegate { PaintDeco(); };
            g.Children.Add(deco);

            Label = new TextBlock();
            Label.Text = text;
            Label.FontFamily = Ink.Serif;
            Label.FontSize = 16;
            Label.FontWeight = FontWeights.Medium;
            Label.TextAlignment = TextAlignment.Center;
            Label.HorizontalAlignment = HorizontalAlignment.Center;
            Label.VerticalAlignment = VerticalAlignment.Center;
            Label.LineHeight = 22;
            Label.LineStackingStrategy = LineStackingStrategy.BlockLineHeight;
            // 显式居中：Pad/LineHeight 都给足，杜绝历史缺陷「文字偏上」
            Label.Padding = new Thickness(0, 1, 0, 0);
            g.Children.Add(Label);

            Child = g;
            ApplyVisual();

            MouseEnter += delegate { Hover(true); };
            MouseLeave += delegate
            {
                Hover(false);
                if (isPressed) { isPressed = false; Up(); }
            };
            PreviewMouseLeftButtonDown += delegate (object s, MouseButtonEventArgs e)
            {
                isPressed = true;
                Down(e.GetPosition(this));
            };
            PreviewMouseLeftButtonUp += delegate
            {
                if (isPressed) { isPressed = false; Up(); }
            };
        }

        Color RestBg()
        {
            if (primary) return Ink.PrimaryBg();
            return Ink.A(Ink.Dark ? (byte)0x38 : (byte)0x42, Ink.Card().R, Ink.Card().G, Ink.Card().B);
        }

        Color RestBorder()
        {
            if (primary) return Ink.PrimaryBorder();
            return Ink.Hair();
        }

        public void ApplyVisual()
        {
            bgBrush.BeginAnimation(SolidColorBrush.ColorProperty, null);
            borderBrush.BeginAnimation(SolidColorBrush.ColorProperty, null);
            bgBrush.Color = RestBg();
            borderBrush.Color = RestBorder();
            Label.Foreground = Ink.Br(primary ? Ink.PrimaryFg() : Ink.Fg());
            if (deco != null) PaintDeco();
        }

        Color DecoStrokeColor()
        {
            if (primary)
            {
                Color c = Ink.PrimaryFg();
                return Ink.A(Ink.Dark ? (byte)0x6E : (byte)0x78, c.R, c.G, c.B);
            }
            Color v = Ink.Verm();
            return Ink.A(Ink.Dark ? (byte)0x7C : (byte)0x6C, v.R, v.G, v.B);
        }

        // 同心流光内框：四段贴合圆角的纤毫笔触，从上下中点向四角顺滑绘出、抬起顺锋隐去；绝不常驻
        void PaintDeco()
        {
            deco.Children.Clear();
            decoPaths = new System.Collections.Generic.List<System.Windows.Shapes.Shape>();
            deco.BeginAnimation(UIElement.OpacityProperty, null);
            deco.Opacity = 0;
            decoScale.BeginAnimation(ScaleTransform.ScaleXProperty, null);
            decoScale.BeginAnimation(ScaleTransform.ScaleYProperty, null);
            decoScale.ScaleX = 1.0;
            decoScale.ScaleY = 1.0;

            double w = deco.ActualWidth, h = deco.ActualHeight;
            if (w < 70 || h < 32) return;

            // 外框圆角 10，内缩 3.5 -> 内圆角 6.5，数学同心贴边，彻底告别生硬悬浮拐角
            double m = 3.5;
            double r = 6.5;
            double th = 0.9;
            double midX = w * 0.5;
            double vTopEnd = Math.Max(m + r + 2.0, h * 0.42);
            double vBotEnd = Math.Min(h - m - r - 2.0, h * 0.58);

            double segLen = Math.Max(1.0, (midX - m - r)) + (Math.PI * 0.5 * r) + Math.Max(1.0, vTopEnd - (m + r));
            decoDashLen = segLen / th + 2.0;

            Brush strokeBr = Ink.Br(DecoStrokeColor());
            // 四段对称路径：上右、上左、下右、下左（均从上下中点起笔，顺同心圆角收锋于两侧腰线）
            Point[,] specs = new Point[4, 4]
            {
                { new Point(midX, m),     new Point(w - m - r, m),     new Point(w - m, m + r),     new Point(w - m, vTopEnd) },
                { new Point(midX, m),     new Point(m + r, m),         new Point(m, m + r),         new Point(m, vTopEnd) },
                { new Point(midX, h - m), new Point(w - m - r, h - m), new Point(w - m, h - m - r), new Point(w - m, vBotEnd) },
                { new Point(midX, h - m), new Point(m + r, h - m),     new Point(m, h - m - r),     new Point(m, vBotEnd) }
            };
            SweepDirection[] sweeps = new SweepDirection[]
            {
                SweepDirection.Clockwise,
                SweepDirection.Counterclockwise,
                SweepDirection.Counterclockwise,
                SweepDirection.Clockwise
            };

            for (int i = 0; i < 4; i++)
            {
                PathFigure pf = new PathFigure();
                pf.StartPoint = specs[i, 0];
                pf.Segments.Add(new LineSegment(specs[i, 1], true));
                pf.Segments.Add(new ArcSegment(specs[i, 2], new Size(r, r), 0, false, sweeps[i], true));
                pf.Segments.Add(new LineSegment(specs[i, 3], true));

                PathGeometry pg = new PathGeometry();
                pg.Figures.Add(pf);

                System.Windows.Shapes.Path p = new System.Windows.Shapes.Path();
                p.Data = pg;
                p.Stroke = strokeBr;
                p.StrokeThickness = th;
                p.StrokeStartLineCap = PenLineCap.Round;
                p.StrokeEndLineCap = PenLineCap.Round;
                p.StrokeLineJoin = PenLineJoin.Round;
                p.StrokeDashArray = new DoubleCollection(new double[] { decoDashLen, decoDashLen });
                p.StrokeDashOffset = decoDashLen;
                p.IsHitTestVisible = false;
                deco.Children.Add(p);
                decoPaths.Add(p);
            }
        }

        // 起笔：纤毫顺同心内框自中点向两侧流淌绘出（撤销模式则由外向内回锋收缩）
        void DecoIn()
        {
            if (decoPaths == null || decoPaths.Count == 0) return;
            IEasingFunction ez = new CubicEase { EasingMode = EasingMode.EaseOut };

            DoubleAnimation op = new DoubleAnimation(1.0, TimeSpan.FromMilliseconds(160));
            op.EasingFunction = ez;
            deco.BeginAnimation(UIElement.OpacityProperty, op);

            double fromOff = RetractMode ? 0.0 : decoDashLen;
            double toOff = RetractMode ? decoDashLen : 0.0;
            int ms = RetractMode ? 240 : 280;
            IEasingFunction dEz = RetractMode
                ? (IEasingFunction)new CubicEase { EasingMode = EasingMode.EaseIn }
                : (IEasingFunction)new CubicEase { EasingMode = EasingMode.EaseOut };

            for (int i = 0; i < decoPaths.Count; i++)
            {
                DoubleAnimation da = new DoubleAnimation(fromOff, toOff, TimeSpan.FromMilliseconds(ms));
                da.EasingFunction = dEz;
                decoPaths[i].BeginAnimation(System.Windows.Shapes.Shape.StrokeDashOffsetProperty, da);
            }
        }

        // 收笔：停一拍后笔尾顺势掠过消隐，干干净净不残留
        void DecoOut()
        {
            if (decoPaths == null || decoPaths.Count == 0) return;
            IEasingFunction ezInOut = new CubicEase { EasingMode = EasingMode.EaseInOut };
            IEasingFunction ezIn = new CubicEase { EasingMode = EasingMode.EaseIn };

            DoubleAnimation op = new DoubleAnimation(0.0, TimeSpan.FromMilliseconds(260));
            op.BeginTime = TimeSpan.FromMilliseconds(150);
            op.EasingFunction = ezIn;
            deco.BeginAnimation(UIElement.OpacityProperty, op);

            if (!RetractMode)
            {
                for (int i = 0; i < decoPaths.Count; i++)
                {
                    DoubleAnimation da = new DoubleAnimation(-decoDashLen, TimeSpan.FromMilliseconds(300));
                    da.BeginTime = TimeSpan.FromMilliseconds(130);
                    da.EasingFunction = ezInOut;
                    decoPaths[i].BeginAnimation(System.Windows.Shapes.Shape.StrokeDashOffsetProperty, da);
                }
            }
        }

        public void SetPrimary(bool on)
        {
            if (primary == on) return;
            primary = on;
            // 平滑过渡底色与边框，绝不在按压涟漪中途硬切清空角饰导致闪烁不协调
            AnimBrush(bgBrush, RestBg(), 220);
            AnimBrush(borderBrush, RestBorder(), 220);
            Label.Foreground = Ink.Br(primary ? Ink.PrimaryFg() : Ink.Fg());
            if (decoPaths != null)
            {
                Brush strokeBr = Ink.Br(DecoStrokeColor());
                for (int i = 0; i < decoPaths.Count; i++) decoPaths[i].Stroke = strokeBr;
            }
        }

        void AnimBrush(SolidColorBrush br, Color to, int ms)
        {
            ColorAnimation ca = new ColorAnimation(to, TimeSpan.FromMilliseconds(ms));
            ca.EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut };
            br.BeginAnimation(SolidColorBrush.ColorProperty, ca);
        }

        void Hover(bool on)
        {
            int ms = on ? 170 : 220;
            if (on)
            {
                if (primary)
                {
                    AnimBrush(bgBrush, Ink.PrimaryHoverBg(), ms);
                    AnimBrush(borderBrush, Ink.Verm(), ms);
                }
                else
                {
                    Color f = Ink.Fg();
                    AnimBrush(borderBrush, Ink.A(Ink.Dark ? (byte)0x78 : (byte)0x54, Ink.Verm().R, Ink.Verm().G, Ink.Verm().B), ms);
                    AnimBrush(bgBrush, Ink.A(Ink.Dark ? (byte)0x18 : (byte)0x12, f.R, f.G, f.B), ms);
                }
            }
            else
            {
                AnimBrush(bgBrush, RestBg(), ms);
                AnimBrush(borderBrush, RestBorder(), ms);
            }
        }

        // 供 --shot 抓帧验证按钮按压态
        public void SimulatePress()
        {
            double cx = ActualWidth > 10 ? ActualWidth * 0.48 : 96;
            double cy = ActualHeight > 10 ? ActualHeight * 0.52 : 26;
            Hover(true);
            Down(new Point(cx, cy));
        }

        public void SimulateRelease()
        {
            Up();
            Hover(false);
        }

        void Down(Point p)
        {
            pressSeq++;
            DoubleAnimation a = new DoubleAnimation(0.97, TimeSpan.FromMilliseconds(85));
            a.EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut };
            scale.BeginAnimation(ScaleTransform.ScaleXProperty, a);
            scale.BeginAnimation(ScaleTransform.ScaleYProperty, a);

            // 撤销模式：收笔回撤（从外向内聚拢收缩，与「落笔/归位」向外洇开形成鲜明区分）
            if (RetractMode)
            {
                InkRippleRetract(p, 176, 0.44, 340, 0, true, Ink.Verm(), 3700 + pressSeq * 7);
                InkRippleRetract(p, 118, 0.30, 280, 30, false, Ink.Fg2(), 3800 + pressSeq * 11);
                DecoIn();
                return;
            }

            // 有机水墨洇开：柔边低饱和水墨过渡，避免刺眼的亮红光斑感
            if (primary && !Ink.Dark)
            {
                // 深墨底上先用极柔宣纸白轻开，再渗出克制朱砂柔晕与外围淡墨边
                InkRipple(p, 152, 0.36, 420, 0, false, Ink.C(0xF4, 0xF0, 0xE6), 0.14, 3100 + pressSeq * 7);
                InkRipple(p, 114, 0.56, 360, 45, false, Ink.C(0xB8, 0x48, 0x36), 0.16, 3200 + pressSeq * 11);
                InkRipple(p, 218, 0.42, 480, 20, true, Ink.C(0xC9, 0x57, 0x3F), 0.0, 3300 + pressSeq * 13);
            }
            else
            {
                InkRipple(p, 136, 0.28, 380, 0, false, Ink.Verm(), 0.14, 3400 + pressSeq * 7);
                InkRipple(p, 90, 0.34, 310, 36, false, Ink.Verm(), 0.18, 3500 + pressSeq * 11);
                InkRipple(p, 204, 0.38, 460, 15, true, Ink.Verm(), 0.0, 3600 + pressSeq * 13);
            }

            DecoIn();
        }

        // 圆心永远钉在按下的那一点：用有机墨团 + 径向柔边模拟宣纸吃墨
        void InkRipple(Point p, double size, double opPeak, int ms, int delay, bool ring, Color col, double coreStop, int seed)
        {
            System.Windows.Shapes.Path e = new System.Windows.Shapes.Path();
            e.Width = size;
            e.Height = size;
            e.Data = Ink.OrganicBlobGeo(size, seed, ring ? 0.032 : 0.055, ring ? 0.018 : 0.032, 0.012);
            e.IsHitTestVisible = false;
            e.Fill = ring ? Ink.InkRing(col) : Ink.InkWash(col, coreStop);
            e.Opacity = 0;

            Canvas.SetLeft(e, p.X - size * 0.5);
            Canvas.SetTop(e, p.Y - size * 0.5);

            ScaleTransform st = new ScaleTransform(0.12, 0.12);
            TransformGroup tg = new TransformGroup();
            tg.Children.Add(st);
            tg.Children.Add(new RotateTransform((seed * 53) % 360));
            e.RenderTransformOrigin = new Point(0.5, 0.5);
            e.RenderTransform = tg;
            ripHost.Children.Add(e);

            IEasingFunction ez = new QuarticEase { EasingMode = EasingMode.EaseOut };

            DoubleAnimation sx = new DoubleAnimation(0.12, 1.12, TimeSpan.FromMilliseconds(ms));
            sx.BeginTime = TimeSpan.FromMilliseconds(delay);
            sx.EasingFunction = ez;

            DoubleAnimation sy = new DoubleAnimation(0.12, 1.12, TimeSpan.FromMilliseconds(ms));
            sy.BeginTime = TimeSpan.FromMilliseconds(delay);
            sy.EasingFunction = ez;

            DoubleAnimationUsingKeyFrames op = new DoubleAnimationUsingKeyFrames();
            op.BeginTime = TimeSpan.FromMilliseconds(delay);
            op.KeyFrames.Add(new LinearDoubleKeyFrame(opPeak,
                KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(Math.Min(65, ms * 0.2)))));
            op.KeyFrames.Add(new EasingDoubleKeyFrame(0.0,
                KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(ms)),
                new CubicEase { EasingMode = EasingMode.EaseOut }));

            sx.Completed += delegate { if (ripHost.Children.Contains(e)) ripHost.Children.Remove(e); };
            st.BeginAnimation(ScaleTransform.ScaleXProperty, sx);
            st.BeginAnimation(ScaleTransform.ScaleYProperty, sy);
            e.BeginAnimation(UIElement.OpacityProperty, op);
        }

        // 收笔回撤涟漪：从外围向指落点聚拢收缩，专用于「撤销一步」按钮
        void InkRippleRetract(Point p, double size, double opPeak, int ms, int delay, bool ring, Color col, int seed)
        {
            System.Windows.Shapes.Path e = new System.Windows.Shapes.Path();
            e.Width = size;
            e.Height = size;
            e.Data = Ink.OrganicBlobGeo(size, seed, 0.036, 0.020, 0.010);
            e.IsHitTestVisible = false;
            e.Fill = ring ? Ink.InkRing(col) : Ink.InkWash(col, 0.22);
            e.Opacity = 0;

            Canvas.SetLeft(e, p.X - size * 0.5);
            Canvas.SetTop(e, p.Y - size * 0.5);

            ScaleTransform st = new ScaleTransform(1.18, 1.18);
            TransformGroup tg = new TransformGroup();
            tg.Children.Add(st);
            tg.Children.Add(new RotateTransform((seed * 41) % 360));
            e.RenderTransformOrigin = new Point(0.5, 0.5);
            e.RenderTransform = tg;
            ripHost.Children.Add(e);

            IEasingFunction ez = new CubicEase { EasingMode = EasingMode.EaseIn };
            DoubleAnimation sx = new DoubleAnimation(1.18, 0.14, TimeSpan.FromMilliseconds(ms));
            sx.BeginTime = TimeSpan.FromMilliseconds(delay);
            sx.EasingFunction = ez;
            DoubleAnimation sy = new DoubleAnimation(1.18, 0.14, TimeSpan.FromMilliseconds(ms));
            sy.BeginTime = TimeSpan.FromMilliseconds(delay);
            sy.EasingFunction = ez;

            DoubleAnimationUsingKeyFrames op = new DoubleAnimationUsingKeyFrames();
            op.BeginTime = TimeSpan.FromMilliseconds(delay);
            op.KeyFrames.Add(new LinearDoubleKeyFrame(opPeak, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(55))));
            op.KeyFrames.Add(new EasingDoubleKeyFrame(0.0, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(ms)), ez));

            sx.Completed += delegate { if (ripHost.Children.Contains(e)) ripHost.Children.Remove(e); };
            st.BeginAnimation(ScaleTransform.ScaleXProperty, sx);
            st.BeginAnimation(ScaleTransform.ScaleYProperty, sy);
            e.BeginAnimation(UIElement.OpacityProperty, op);
        }

        void Up()
        {
            DoubleAnimation a = new DoubleAnimation(1.0, TimeSpan.FromMilliseconds(280));
            a.EasingFunction = new BackEase { Amplitude = 0.18, EasingMode = EasingMode.EaseOut };
            scale.BeginAnimation(ScaleTransform.ScaleXProperty, a);
            scale.BeginAnimation(ScaleTransform.ScaleYProperty, a);
            DecoOut();
        }
    }

    // ============ 主窗口 ============
    public class MoShouWindow : Window
    {
        // 462 × 486 是设计基准（所有内容按它排版），窗口等比缩放严丝合缝无死边
        const double WIN_W = 462;
        const double WIN_H = 486;
        const double ASPECT = WIN_W / WIN_H;
        const double START_W = 740;
        const double START_H = 778;

        string root;
        string savedDataDir = null;
        double savedWinW = 0;
        Border shell;
        ScaleTransform winScale;
        Canvas board, gridLayer, wetLayer, sweepLayer, dotLayer, previewLayer;
        Border dotPreviewCard;
        Ellipse dotPreviewPip;
        TextBlock dotPreviewName, dotPreviewSub;
        Ellipse dropTargetRing;
        // 根层常驻：内容层可整体换掉，遮罩层不动，换材质才不会闪
        Grid rootLayer, stageLayer;
        Canvas veilLayer;
        Border historyOverlay;
        StackPanel historyList;
        TextBlock historyPathTxt;
        Border dirOverlay;
        TextBlock dirDataTxt, dirExeTxt, dirMemTxt;
        Border guideOverlay;
        bool seenGuide = false;
        Snapshot previewSnap = null;
        string previewPath = null;
        UIElement headerHost, boardHostEl, calHostEl, actionBarEl, statusLineEl, closeBtnEl, reviewDotEl;
        ScaleTransform reviewDotScale;
        List<UIElement> bottomPips = new List<UIElement>();
        TextBlock tRes, tScale, tCount, tStatus, tCalDate, tCalMeta;
        InkButton btnSave, btnRestore;
        List<System.Windows.Shapes.Shape> gridLines = new List<System.Windows.Shapes.Shape>();
        List<Ellipse> dots = new List<Ellipse>();
        List<int> dotColRank = new List<int>();
        List<int> dotRowRank = new List<int>();
        List<string> dotTypes = new List<string>();
        List<int> dotGridC = new List<int>();
        List<int> dotGridR = new List<int>();
        List<int> dotAnimGen = new List<int>();
        Dictionary<string, TextBlock> legendCounts = new Dictionary<string, TextBlock>();
        Dictionary<string, Ellipse> legendDots = new Dictionary<string, Ellipse>();
        class DraftOp
        {
            public bool IsAbsorb;
            public bool IsBatch;
            public int IdxA, OldCA, OldRA, NewCA, NewRA;
            public int IdxB, OldCB, OldRB;
            public string SrcName, FolderName;
            public List<int> BatchOldC, BatchOldR;
        }

        List<DeskIcon> shown = new List<DeskIcon>();
        List<DeskIcon> draftBackup = null;
        Stack<DraftOp> draftStack = new Stack<DraftOp>();
        HashSet<int> absorbedIndices = new HashSet<int>();
        int absorbHoverFolder = -1;
        int dwellFolderIdx = -1;
        DateTime dwellStartUtc = DateTime.MinValue;
        bool absorbReady = false;
        bool draftDirty = false;
        int dragIdx = -1;
        bool dragMoved = false;
        Point dragStartPt;
        double dragOrigLeft, dragOrigTop;
        int dragTargetC, dragTargetR;
        double boardGx0 = 21, boardGdx = 115, boardGy0 = 2, boardGdy = 147;
        double boardOx = 0, boardOy = 0, boardK = 0.16;
        int boardCi0 = 0, boardCi1 = 21, boardRi0 = 0, boardRi1 = 8;
        Snapshot snap;
        bool armed = false;
        Snapshot pending = null;
        bool busy = false;

        double bw, bh;
        ScaleTransform fitScale;
        TranslateTransform fitTT;

        public MoShouWindow()
        {
            // 先读全局配置（记住用户自定义存址、材质与窗口尺寸），再决定当前数据根目录
            LoadSettings();
            if (MoShouApp.ShotDir != null) { Ink.Dark = false; savedWinW = 0; }
            root = ResolveRoot();

            Title = "墨守";
            double initW = (savedWinW >= WIN_W) ? ClampWinW(savedWinW) : START_W;
            Width = initW;
            Height = Math.Round(initW / ASPECT);
            MinWidth = WIN_W;
            MinHeight = WIN_H;
            WindowStyle = WindowStyle.None;
            // 启用 CanResize 以支持原生 Win32 SC_SIZE + WM_SIZING VSync 原子等比缩放（AllowsTransparency 下无任何系统边框）
            ResizeMode = ResizeMode.CanResize;
            AllowsTransparency = true;
            Background = Brushes.Transparent;
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
            ShowInTaskbar = true;
            FontFamily = Ink.Sans;

            rootLayer = new Grid();
            rootLayer.Background = Brushes.Transparent;
            stageLayer = new Grid();
            veilLayer = new Canvas();
            veilLayer.IsHitTestVisible = false;
            Panel.SetZIndex(veilLayer, 90);
            rootLayer.Children.Add(stageLayer);
            rootLayer.Children.Add(veilLayer);
            stageLayer.Children.Add(BuildUi());
            Content = rootLayer;
            SourceInitialized += delegate
            {
                System.Windows.Interop.HwndSource src =
                    System.Windows.Interop.HwndSource.FromVisual(this) as System.Windows.Interop.HwndSource;
                if (src != null) src.AddHook(WndProc);
            };
            Loaded += OnLoaded;
            SizeChanged += delegate { ApplyFit(); };
            StateChanged += delegate
            {
                if (WindowState == WindowState.Minimized) TrimMemory();
            };

            KeyDown += delegate (object s, KeyEventArgs e)
            {
                ModifierKeys mk = Keyboard.Modifiers;
                bool ctrl = (mk & ModifierKeys.Control) == ModifierKeys.Control;
                if (ctrl && e.Key == Key.S) { OnPrimaryBtn(); e.Handled = true; }
                else if (ctrl && e.Key == Key.R) { OnSecondaryBtn(); e.Handled = true; }
                else if (ctrl && e.Key == Key.Z && draftDirty) { UndoLastDraftStep(); e.Handled = true; }
                else if (e.Key == Key.Escape)
                {
                    if (guideOverlay != null && guideOverlay.Visibility == Visibility.Visible)
                    {
                        CloseGuide(true, true);
                    }
                    else if (historyOverlay != null && historyOverlay.Visibility == Visibility.Visible)
                    {
                        CloseHistory(true);
                    }
                    else if (dirOverlay != null && dirOverlay.Visibility == Visibility.Visible)
                    {
                        CloseDir();
                    }
                    else if (draftDirty)
                    {
                        CancelDraft();
                    }
                    else if (armed)
                    {
                        DisarmAll();
                        if (tStatus != null) tStatus.Text = "已取消操作 · 只动坐标 · 不动文件";
                    }
                    else
                    {
                        FadeClose();
                    }
                    e.Handled = true;
                }
            };
        }

        double MaxWinW()
        {
            try
            {
                double maxH = SystemParameters.WorkArea.Height * 0.92;
                double maxW = SystemParameters.WorkArea.Width * 0.90;
                double wByH = maxH * ASPECT;
                return Math.Max(WIN_W, Math.Min(maxW, wByH));
            }
            catch { return 960; }
        }

        double ClampWinW(double w)
        {
            double mx = MaxWinW();
            if (w < WIN_W) return WIN_W;
            if (w > mx) return mx;
            return w;
        }

        void SetAspectSize(double newW, bool save)
        {
            double w = Math.Round(ClampWinW(newW));
            double h = Math.Round(w / ASPECT);
            Width = w;
            Height = h;
            ApplyFitExplicit(w, h);
            if (save) { savedWinW = w; SaveSettings(); }
        }

        // 内容等比贴合窗口：只缩放，不改布局比例；缩放过程中由 ApplyFitExplicit 单次原子更新防抖
        void ApplyFit()
        {
            double w = ActualWidth > 0 ? ActualWidth : START_W;
            double h = ActualHeight > 0 ? ActualHeight : START_H;
            ApplyFitExplicit(w, h);
        }

        void ApplyFitExplicit(double w, double h)
        {
            if (fitScale == null) return;
            double k = Math.Min(w / WIN_W, h / WIN_H);
            if (k < 0.45) k = 0.45;
            fitScale.ScaleX = k;
            fitScale.ScaleY = k;
            if (fitTT != null)
            {
                fitTT.X = (w - WIN_W * k) / 2;
                fitTT.Y = (h - WIN_H * k) / 2;
            }
            // 遮罩跟着窗口圆角走，墨滴才不会从四角透明区溢出去
            if (veilLayer != null)
            {
                double ox = fitTT != null ? fitTT.X : 0;
                double oy = fitTT != null ? fitTT.Y : 0;
                veilLayer.Clip = new RectangleGeometry(
                    new Rect(ox, oy, WIN_W * k, WIN_H * k), 22 * k, 22 * k);
            }
        }

        [DllImport("kernel32.dll")]
        static extern bool SetProcessWorkingSetSize(IntPtr proc, IntPtr min, IntPtr max);

        [StructLayout(LayoutKind.Sequential)]
        struct WinPoint { public int X; public int Y; }

        [DllImport("user32.dll")]
        static extern bool GetCursorPos(out WinPoint pt);

        [DllImport("user32.dll")]
        static extern bool ReleaseCapture();

        [StructLayout(LayoutKind.Sequential)]
        struct WinRect { public int Left, Top, Right, Bottom; }

        const int WM_SIZING = 0x0214;
        const int WM_EXITSIZEMOVE = 0x0232;
        const int WM_SYSCOMMAND = 0x0112;
        const int SC_SIZE = 0xF000;

        // 借鉴成熟开源 WPF/Win32 应用（PowerToys/Chromium/MahApps）：在 WM_SIZING 阶段直接原子改写物理 RECT 锁定 462:486，
        // 交由系统 DWM VSync 单次原子提交，彻底根除逐属性赋 Left/Top/Width/Height 引发的四重重绘抖动
        IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (msg == WM_SIZING && lParam != IntPtr.Zero)
            {
                WinRect rc = (WinRect)Marshal.PtrToStructure(lParam, typeof(WinRect));
                int edge = wParam.ToInt32();
                double dpi = GetDpiScale();
                int minW = (int)Math.Round(WIN_W * dpi);
                int maxW = (int)Math.Round(ClampWinW(99999) * dpi);

                int curW = rc.Right - rc.Left;
                int curH = rc.Bottom - rc.Top;
                int nw;

                // 上/下边拖动以高度反推宽度；左/右边以宽度为准；四角取变化幅度更大的一轴
                if (edge == 3 || edge == 6)
                {
                    nw = (int)Math.Round(curH * ASPECT);
                }
                else if (edge == 1 || edge == 2)
                {
                    nw = curW;
                }
                else
                {
                    int wFromH = (int)Math.Round(curH * ASPECT);
                    nw = (Math.Abs(curW - wFromH) > 0 && curW > wFromH) ? curW : wFromH;
                }

                if (nw < minW) nw = minW;
                if (nw > maxW) nw = maxW;
                int nh = (int)Math.Round(nw / ASPECT);

                // 严格锚定对侧边/角，确保静止角纹丝不动
                switch (edge)
                {
                    case 1: // WMSZ_LEFT
                        rc.Left = rc.Right - nw;
                        rc.Bottom = rc.Top + nh;
                        break;
                    case 2: // WMSZ_RIGHT
                        rc.Right = rc.Left + nw;
                        rc.Bottom = rc.Top + nh;
                        break;
                    case 3: // WMSZ_TOP
                        rc.Top = rc.Bottom - nh;
                        rc.Right = rc.Left + nw;
                        break;
                    case 4: // WMSZ_TOPLEFT
                        rc.Left = rc.Right - nw;
                        rc.Top = rc.Bottom - nh;
                        break;
                    case 5: // WMSZ_TOPRIGHT
                        rc.Right = rc.Left + nw;
                        rc.Top = rc.Bottom - nh;
                        break;
                    case 6: // WMSZ_BOTTOM
                        rc.Bottom = rc.Top + nh;
                        rc.Right = rc.Left + nw;
                        break;
                    case 7: // WMSZ_BOTTOMLEFT
                        rc.Left = rc.Right - nw;
                        rc.Bottom = rc.Top + nh;
                        break;
                    case 8: // WMSZ_BOTTOMRIGHT
                        rc.Right = rc.Left + nw;
                        rc.Bottom = rc.Top + nh;
                        break;
                }

                Marshal.StructureToPtr(rc, lParam, true);
                handled = true;
                return new IntPtr(1);
            }
            else if (msg == WM_EXITSIZEMOVE)
            {
                savedWinW = ActualWidth;
                SaveSettings();
            }
            return IntPtr.Zero;
        }

        [DllImport("user32.dll")]
        static extern int GetWindowLong(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll")]
        static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);

        const int GWL_STYLE = -16;
        const int LVS_AUTOARRANGE = 0x0100;
        const int LVM_SETEXTENDEDLISTVIEWSTYLE = 0x1036;
        const int LVM_GETEXTENDEDLISTVIEWSTYLE = 0x1037;
        const int LVS_EX_SNAPTOGRID = 0x00080000;

        // 无碰撞精准写入桌面坐标：检测「自动排列图标」，并临时解除「对齐网格」挤位再还原，彻底杜绝空洞与 420 次死循环
        static int ApplyDesktopExact(Snapshot target, out bool autoArrangeBlocked, out int remainingDrift)
        {
            autoArrangeBlocked = false;
            remainingDrift = 0;
            IntPtr lv = Desk.GetListView();
            if (lv == IntPtr.Zero) return Engine.Apply(target, 4);

            int style = GetWindowLong(lv, GWL_STYLE);
            if ((style & LVS_AUTOARRANGE) != 0)
            {
                autoArrangeBlocked = true;
                return 0;
            }

            long exStyle = (long)SendMessage(lv, LVM_GETEXTENDEDLISTVIEWSTYLE, IntPtr.Zero, IntPtr.Zero);
            bool hadSnap = (exStyle & LVS_EX_SNAPTOGRID) != 0;
            int totalMoved = 0;

            try
            {
                if (hadSnap)
                    SendMessage(lv, LVM_SETEXTENDEDLISTVIEWSTYLE, (IntPtr)LVS_EX_SNAPTOGRID, IntPtr.Zero);

                // 关闭对齐网格后一次性无碰撞落位（最多 2 轮确认即可 100% 精确到位，不会相互挤出空洞）
                totalMoved = Engine.Apply(target, 2);
            }
            finally
            {
                if (hadSnap)
                    SendMessage(lv, LVM_SETEXTENDEDLISTVIEWSTYLE, (IntPtr)LVS_EX_SNAPTOGRID, (IntPtr)LVS_EX_SNAPTOGRID);
            }

            System.Threading.Thread.Sleep(110);
            try
            {
                List<DeskIcon> after = Desk.Scan();
                ScreenState sc = Desk.GetScreen();
                DiffResult check = Engine.Diff(target, after, sc);
                remainingDrift = check.MoveIndices.Count;
            }
            catch { }
            return totalMoved;
        }

        // 释放空闲托管堆与工作集内存，将常驻内存压到极低
        static void TrimMemory()
        {
            try
            {
                GC.Collect(2, GCCollectionMode.Optimized, false);
                GC.WaitForPendingFinalizers();
                SetProcessWorkingSetSize(
                    System.Diagnostics.Process.GetCurrentProcess().Handle,
                    (IntPtr)(-1), (IntPtr)(-1));
            }
            catch { }
        }

        static double GetWorkingSetMB()
        {
            try
            {
                return System.Diagnostics.Process.GetCurrentProcess().WorkingSet64 / (1024.0 * 1024.0);
            }
            catch { return 0; }
        }

        double GetDpiScale()
        {
            try
            {
                PresentationSource src = PresentationSource.FromVisual(this);
                if (src != null && src.CompositionTarget != null)
                {
                    double sx = src.CompositionTarget.TransformToDevice.M11;
                    if (sx >= 0.75 && sx <= 4.0) return sx;
                }
            }
            catch { }
            return 1.5;
        }

        static string ConfigPath()
        {
            string app = AppDir();
            return app.Length > 0 ? System.IO.Path.Combine(app, "config.json") : "";
        }

        static string JsonEsc(string s)
        {
            if (s == null) return "";
            return s.Replace("\\", "\\\\").Replace("\"", "\\\"");
        }

        static string JsonExtractString(string txt, string key)
        {
            string pat = "\"" + key + "\"";
            int idx = txt.IndexOf(pat, StringComparison.OrdinalIgnoreCase);
            if (idx < 0) return null;
            int col = txt.IndexOf(':', idx + pat.Length);
            if (col < 0) return null;
            int q1 = txt.IndexOf('"', col + 1);
            if (q1 < 0) return null;
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            for (int i = q1 + 1; i < txt.Length; i++)
            {
                char ch = txt[i];
                if (ch == '\\' && i + 1 < txt.Length) { sb.Append(txt[++i]); continue; }
                if (ch == '"') return sb.ToString();
                sb.Append(ch);
            }
            return null;
        }

        void LoadSettings()
        {
            try
            {
                string p = ConfigPath();
                if (p.Length == 0 || !System.IO.File.Exists(p)) return;
                string txt = System.IO.File.ReadAllText(p, System.Text.Encoding.UTF8);
                if (txt.IndexOf("\"dark\":true", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    txt.IndexOf("\"dark\": true", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    Ink.Dark = true;
                }
                string dir = JsonExtractString(txt, "dataDir");
                if (!string.IsNullOrEmpty(dir) && Directory.Exists(dir))
                    savedDataDir = dir;
                if (txt.IndexOf("\"seenGuide\":true", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    txt.IndexOf("\"seenGuide\": true", StringComparison.OrdinalIgnoreCase) >= 0)
                    seenGuide = true;
                int wi = txt.IndexOf("\"winW\"", StringComparison.OrdinalIgnoreCase);
                if (wi >= 0)
                {
                    int col = txt.IndexOf(':', wi + 6);
                    if (col >= 0)
                    {
                        int p0 = col + 1;
                        while (p0 < txt.Length && (txt[p0] == ' ' || txt[p0] == '\t')) p0++;
                        int p1 = p0;
                        while (p1 < txt.Length && ((txt[p1] >= '0' && txt[p1] <= '9') || txt[p1] == '.')) p1++;
                        double parsed;
                        if (p1 > p0 && double.TryParse(txt.Substring(p0, p1 - p0),
                            System.Globalization.NumberStyles.Any,
                            System.Globalization.CultureInfo.InvariantCulture, out parsed))
                            savedWinW = parsed;
                    }
                }
            }
            catch { }
        }

        void SaveSettings()
        {
            if (MoShouApp.ShotDir != null) return;
            try
            {
                string p = ConfigPath();
                if (p.Length == 0) return;
                string dirPart = !string.IsNullOrEmpty(savedDataDir)
                    ? ",\"dataDir\":\"" + JsonEsc(savedDataDir) + "\""
                    : "";
                string wPart = savedWinW >= WIN_W
                    ? ",\"winW\":" + ((int)Math.Round(savedWinW))
                    : "";
                string gPart = seenGuide ? ",\"seenGuide\":true" : "";
                string json = "{\"dark\":" + (Ink.Dark ? "true" : "false") + dirPart + wPart + gPart + "}\r\n";
                System.IO.File.WriteAllText(p, json, System.Text.Encoding.UTF8);
            }
            catch { }
        }

        void DisarmAll()
        {
            if (armed)
            {
                armed = false;
                pending = null;
                if (btnRestore != null)
                {
                    btnRestore.Label.Text = "归位";
                    btnRestore.SetPrimary(false);
                }
            }
        }

        // 从实际图标坐标反推真实格距（适配任意分辨率/DPI，不受 Core.cs 未缩放 GridDx 干扰）
        static double InferStep(List<int> vals, double fallback)
        {
            if (vals == null || vals.Count < 2) return fallback;
            vals.Sort();
            List<int> uniq = new List<int>();
            for (int i = 0; i < vals.Count; i++)
            {
                if (uniq.Count == 0 || vals[i] - uniq[uniq.Count - 1] >= 12)
                    uniq.Add(vals[i]);
            }
            if (uniq.Count < 2) return fallback;

            Dictionary<int, int> freq = new Dictionary<int, int>();
            for (int i = 1; i < uniq.Count; i++)
            {
                int diff = uniq[i] - uniq[i - 1];
                if (diff < 36 || diff > 400) continue;
                int bucket = (int)Math.Round(diff / 2.0) * 2;
                if (freq.ContainsKey(bucket)) freq[bucket]++;
                else freq[bucket] = 1;
            }
            int bestDiff = 0, bestCount = 0;
            foreach (KeyValuePair<int, int> kv in freq)
            {
                if (kv.Value > bestCount || (kv.Value == bestCount && kv.Key < bestDiff))
                {
                    bestCount = kv.Value;
                    bestDiff = kv.Key;
                }
            }
            return bestDiff >= 36 ? bestDiff : fallback;
        }

        // 动态读取桌面格距，适配任意分辨率/DPI；图标不足两列时按当前 DPI 缩放兜底
        void GetGridMetrics(out double gx0, out double gdx, out double gy0, out double gdy)
        {
            double dpi = GetDpiScale();
            double fbDx = (snap != null && snap.GridDx > 20)
                ? Math.Round(snap.GridDx * (snap.Scale > 0.5 ? snap.Scale : dpi) / 1.25)
                : Math.Round(77 * dpi);
            double fbDy = (snap != null && snap.GridDy > 20)
                ? Math.Round(snap.GridDy * (snap.Scale > 0.5 ? snap.Scale : dpi) / 1.25)
                : Math.Round(98 * dpi);
            if (fbDx < 64) fbDx = 76;
            if (fbDy < 76) fbDy = 96;
            gx0 = (snap != null) ? snap.GridX0 : 21;
            gy0 = (snap != null) ? snap.GridY0 : 2;

            if (shown != null && shown.Count >= 2)
            {
                List<int> xs = new List<int>(shown.Count);
                List<int> ys = new List<int>(shown.Count);
                int minX = int.MaxValue, minY = int.MaxValue;
                for (int i = 0; i < shown.Count; i++)
                {
                    xs.Add(shown[i].X);
                    ys.Add(shown[i].Y);
                    if (shown[i].X < minX) minX = shown[i].X;
                    if (shown[i].Y < minY) minY = shown[i].Y;
                }
                gx0 = minX;
                gy0 = minY;
                gdx = InferStep(xs, fbDx);
                gdy = InferStep(ys, fbDy);
                return;
            }

            int sdx = 0, sdy = 0;
            try { Desk.GetSpacing(out sdx, out sdy); } catch { }
            gdx = sdx > 20 ? sdx : fbDx;
            gdy = sdy > 20 ? sdy : fbDy;
        }

        // 记忆放在哪：优先用户记住的自定义存址 → 已有旧数据的便携目录 → 可写的程序安装目录 → 只读安装（如 Program Files）则落回 AppData
        string ResolveRoot()
        {
            if (!string.IsNullOrEmpty(savedDataDir) && Directory.Exists(savedDataDir) && CanWrite(savedDataDir))
                return savedDataDir;

            string exeDir = AppDomain.CurrentDomain.BaseDirectory.TrimEnd('\\');
            string parent = System.IO.Path.GetDirectoryName(exeDir);

            string[] cands = new string[] { exeDir, parent };
            for (int i = 0; i < cands.Length; i++)
            {
                string d = cands[i];
                if (string.IsNullOrEmpty(d) || !Directory.Exists(d)) continue;
                if (System.IO.File.Exists(System.IO.Path.Combine(d, "layout_snapshot.json")) && CanWrite(d))
                    return d;
            }

            // 便携/自定义中英文安装目录可写时，直接将数据存在程序身旁，同学拷走或自定义路径最直观
            if (!string.IsNullOrEmpty(exeDir) && Directory.Exists(exeDir) && CanWrite(exeDir))
                return exeDir;

            string app = AppDir();
            if (app.Length > 0)
            {
                Migrate(exeDir, app);
                if (parent != null && parent != exeDir) Migrate(parent, app);
                return app;
            }
            return exeDir;
        }

        static bool CanWrite(string dir)
        {
            try
            {
                string probe = System.IO.Path.Combine(dir, ".mw_" + System.Guid.NewGuid().ToString("N") + ".tmp");
                System.IO.File.WriteAllText(probe, "x");
                System.IO.File.Delete(probe);
                return true;
            }
            catch { return false; }
        }

        static string AppDir()
        {
            try
            {
                string d = System.IO.Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "MoShou");
                if (!Directory.Exists(d)) Directory.CreateDirectory(d);
                return d;
            }
            catch { return ""; }
        }

        // 首次在 AppData 开工时把旧位置的记忆带过去，免得上机第一眼是「尚无记忆」
        static void Migrate(string from, string to)
        {
            try
            {
                if (string.IsNullOrEmpty(from) || string.IsNullOrEmpty(to)) return;
                if (!Directory.Exists(from) || !Directory.Exists(to)) return;
                if (from.Equals(to, StringComparison.OrdinalIgnoreCase)) return;

                string srcJson = System.IO.Path.Combine(from, "layout_snapshot.json");
                string dstJson = System.IO.Path.Combine(to, "layout_snapshot.json");
                if (System.IO.File.Exists(srcJson) && !System.IO.File.Exists(dstJson))
                    System.IO.File.Copy(srcJson, dstJson, false);

                string srcLay = System.IO.Path.Combine(from, "layouts");
                string dstLay = System.IO.Path.Combine(to, "layouts");
                if (Directory.Exists(srcLay))
                {
                    if (!Directory.Exists(dstLay)) Directory.CreateDirectory(dstLay);
                    string[] fs = Directory.GetFiles(srcLay, "*.json");
                    for (int i = 0; i < fs.Length; i++)
                    {
                        string dst = System.IO.Path.Combine(dstLay, System.IO.Path.GetFileName(fs[i]));
                        if (!System.IO.File.Exists(dst)) System.IO.File.Copy(fs[i], dst, false);
                    }
                }
            }
            catch { }
        }

        static RowDefinition RowDef(double h)
        {
            RowDefinition r = new RowDefinition();
            r.Height = new GridLength(h);
            return r;
        }

        // ---------- 组装 ----------
        UIElement BuildUi()
        {
            shell = new Border();
            shell.CornerRadius = new CornerRadius(22);
            shell.Background = Ink.Br(Ink.Bg());
            shell.BorderBrush = Ink.Br(Ink.Hair());
            shell.BorderThickness = new Thickness(1);
            shell.Margin = new Thickness(0);
            shell.ClipToBounds = true;
            shell.RenderTransformOrigin = new Point(0.5, 0.5);
            winScale = new ScaleTransform(1, 1);
            shell.RenderTransform = winScale;
            // 全卡片空白与四周边距均可直接拖动窗口（不再局限于 Header 内缩区，贴近顶边也能顺手拖走，且绝不拦截按钮/棋子/入口点击）
            shell.MouseLeftButtonDown += delegate (object s, MouseButtonEventArgs e)
            {
                if (e.Handled) return;
                DependencyObject cur = e.OriginalSource as DependencyObject;
                while (cur != null && cur != shell)
                {
                    if (cur is InkButton || cur == board || cur is TextBox || cur is CheckBox) return;
                    FrameworkElement fe = cur as FrameworkElement;
                    if (fe != null && fe.Cursor == Cursors.Hand) return;
                    cur = VisualTreeHelper.GetParent(cur);
                }
                Point pt = e.GetPosition(shell);
                if (e.ClickCount == 2 && pt.Y <= 68)
                {
                    double targetW = (ActualWidth > 640) ? 540 : START_W;
                    SetAspectSize(targetW, true);
                    e.Handled = true;
                    return;
                }
                try { DragMove(); } catch { }
            };

            Grid stack = new Grid();
            stack.Children.Add(Halo());
            stack.Children.Add(PaperGrain());

            Grid inner = new Grid();
            inner.Margin = new Thickness(16, 14, 16, 12);
            // 50 + 226 + 92 + 60 + 32 = 460；加上下边距 26 = 486 设计基准
            inner.RowDefinitions.Add(RowDef(50));
            inner.RowDefinitions.Add(RowDef(226));
            inner.RowDefinitions.Add(RowDef(92));
            inner.RowDefinitions.Add(RowDef(60));
            inner.RowDefinitions.Add(RowDef(32));

            headerHost = Header();
            inner.Children.Add(headerHost);

            boardHostEl = BoardHost();
            Grid.SetRow(boardHostEl, 1);
            inner.Children.Add(boardHostEl);

            calHostEl = Calendar();
            Grid.SetRow(calHostEl, 2);
            inner.Children.Add(calHostEl);

            actionBarEl = ActionBar();
            Grid.SetRow(actionBarEl, 3);
            inner.Children.Add(actionBarEl);

            statusLineEl = StatusLine();
            Grid.SetRow(statusLineEl, 4);
            inner.Children.Add(statusLineEl);

            stack.Children.Add(inner);
            stack.Children.Add(HistoryDrawer());
            stack.Children.Add(DirDrawer());
            stack.Children.Add(GuideDrawer());
            stack.Children.Add(Corners());
            // 拉伸手柄收在圆角内侧，不再刺出窗角
            stack.Children.Add(Grip());
            // 最小化用素雅墨色、关闭用克制朱砂；手绘笔画，绝不用圆点
            stack.Children.Add(GlyphBtn("min", new Thickness(0, 6, 48, 0),
                delegate { WindowState = WindowState.Minimized; }));
            closeBtnEl = GlyphBtn("close", new Thickness(0, 6, 6, 0),
                delegate { FadeClose(); });
            stack.Children.Add(closeBtnEl);
            shell.Child = stack;

            // 内容按 462×486 设计稿排版，整体等比缩放贴合窗口——拉多大都不变形、都居中
            Grid fit = new Grid();
            fit.Width = WIN_W;
            fit.Height = WIN_H;
            fit.HorizontalAlignment = HorizontalAlignment.Left;
            fit.VerticalAlignment = VerticalAlignment.Top;
            fitScale = new ScaleTransform(1, 1);
            fitTT = new TranslateTransform(0, 0);
            TransformGroup tg = new TransformGroup();
            tg.Children.Add(fitScale);
            tg.Children.Add(fitTT);
            fit.RenderTransform = tg;
            fit.Children.Add(shell);

            Grid outer = new Grid();
            outer.Background = Brushes.Transparent;
            outer.Children.Add(fit);
            AddResizeHandles(outer);
            return outer;
        }

        // 八向等比缩放热区（四边 + 四角）：仅占最外侧 5px 细边与 14px 窗角，绝不侵占顶栏与边距的窗口拖动区
        void AddResizeHandles(Grid outer)
        {
            double edge = 5;
            double corner = 14;
            // 四边
            outer.Children.Add(MakeResizeZone(HorizontalAlignment.Right, VerticalAlignment.Stretch, edge, double.NaN, Cursors.SizeWE, 1, 0));
            outer.Children.Add(MakeResizeZone(HorizontalAlignment.Left, VerticalAlignment.Stretch, edge, double.NaN, Cursors.SizeWE, -1, 0));
            outer.Children.Add(MakeResizeZone(HorizontalAlignment.Stretch, VerticalAlignment.Bottom, double.NaN, edge, Cursors.SizeNS, 0, 1));
            outer.Children.Add(MakeResizeZone(HorizontalAlignment.Stretch, VerticalAlignment.Top, double.NaN, edge, Cursors.SizeNS, 0, -1));
            // 四角（层级在四边之上）
            outer.Children.Add(MakeResizeZone(HorizontalAlignment.Right, VerticalAlignment.Bottom, corner, corner, Cursors.SizeNWSE, 1, 1));
            outer.Children.Add(MakeResizeZone(HorizontalAlignment.Left, VerticalAlignment.Bottom, corner, corner, Cursors.SizeNESW, -1, 1));
            outer.Children.Add(MakeResizeZone(HorizontalAlignment.Right, VerticalAlignment.Top, corner, corner, Cursors.SizeNESW, 1, -1));
            outer.Children.Add(MakeResizeZone(HorizontalAlignment.Left, VerticalAlignment.Top, corner, corner, Cursors.SizeNWSE, -1, -1));
        }

        Border MakeResizeZone(HorizontalAlignment ha, VerticalAlignment va, double w, double h,
                              Cursor cur, int dxSign, int dySign)
        {
            Border b = new Border();
            b.HorizontalAlignment = ha;
            b.VerticalAlignment = va;
            if (!double.IsNaN(w)) b.Width = w;
            if (!double.IsNaN(h)) b.Height = h;
            b.Background = Brushes.Transparent;
            b.Cursor = cur;
            Panel.SetZIndex(b, (dxSign != 0 && dySign != 0) ? 85 : 80);

            // 映射到 Win32 WMSZ 方向码：1=左 2=右 3=上 4=左上 5=右上 6=下 7=左下 8=右下
            int wmsz = 8;
            if (dxSign < 0 && dySign == 0) wmsz = 1;
            else if (dxSign > 0 && dySign == 0) wmsz = 2;
            else if (dxSign == 0 && dySign < 0) wmsz = 3;
            else if (dxSign < 0 && dySign < 0) wmsz = 4;
            else if (dxSign > 0 && dySign < 0) wmsz = 5;
            else if (dxSign == 0 && dySign > 0) wmsz = 6;
            else if (dxSign < 0 && dySign > 0) wmsz = 7;
            else if (dxSign > 0 && dySign > 0) wmsz = 8;

            b.MouseLeftButtonDown += delegate (object s, MouseButtonEventArgs e)
            {
                e.Handled = true;
                IntPtr hwnd = new System.Windows.Interop.WindowInteropHelper(this).Handle;
                if (hwnd != IntPtr.Zero)
                {
                    ReleaseCapture();
                    SendMessage(hwnd, WM_SYSCOMMAND, (IntPtr)(SC_SIZE + wmsz), IntPtr.Zero);
                }
            };
            return b;
        }

        // 静态宣纸/夜墨纤维层：BitmapCache 栅格化，零帧开销，去掉纯平塑料感
        UIElement PaperGrain()
        {
            Canvas cv = new Canvas();
            cv.Width = WIN_W;
            cv.Height = WIN_H;
            cv.IsHitTestVisible = false;
            Random rnd = new Random(8811);
            int count = Ink.Dark ? 70 : 95;
            Color fc = Ink.Dark ? Ink.C(0xD6, 0xD0, 0xC0) : Ink.C(0xB8, 0xAC, 0x8F);
            for (int i = 0; i < count; i++)
            {
                double x = rnd.NextDouble() * WIN_W;
                double y = rnd.NextDouble() * WIN_H;
                double len = 5 + rnd.NextDouble() * 16;
                double dy = (rnd.NextDouble() - 0.5) * 3.0;
                System.Windows.Shapes.Line ln = new System.Windows.Shapes.Line();
                ln.X1 = x; ln.Y1 = y;
                ln.X2 = x + len; ln.Y2 = y + dy;
                ln.Stroke = Ink.Br(fc);
                ln.StrokeThickness = 0.65;
                ln.Opacity = Ink.Dark
                    ? (0.018 + rnd.NextDouble() * 0.028)
                    : (0.038 + rnd.NextDouble() * 0.052);
                cv.Children.Add(ln);
            }
            cv.CacheMode = new BitmapCache(GetDpiScale());
            return cv;
        }

        // 右下角拉伸视觉提示（实际缩放由外层八向等比热区接管）
        FrameworkElement Grip()
        {
            Border g = new Border();
            g.Width = 20;
            g.Height = 20;
            g.HorizontalAlignment = HorizontalAlignment.Right;
            g.VerticalAlignment = VerticalAlignment.Bottom;
            g.Margin = new Thickness(0, 0, 7, 7);
            g.Background = Brushes.Transparent;
            g.IsHitTestVisible = false;
            g.Opacity = 0.34;
            Panel.SetZIndex(g, 55);

            Canvas cv = new Canvas();
            cv.Width = 12;
            cv.Height = 12;
            cv.HorizontalAlignment = HorizontalAlignment.Right;
            cv.VerticalAlignment = VerticalAlignment.Bottom;
            cv.IsHitTestVisible = false;
            Rough.Pen(cv, 4.5, 11.5, 11.5, 4.5, Ink.Fg3(), 0.95, 0.85, 0.45, 0.30, 6101, null);
            Rough.Pen(cv, 8.2, 11.5, 11.5, 8.2, Ink.Fg3(), 0.95, 0.70, 0.45, 0.30, 6102, null);
            g.Child = cv;
            return g;
        }

        static UIElement Halo()
        {
            Grid halo = new Grid();
            halo.IsHitTestVisible = false;

            Ellipse e1 = new Ellipse();
            e1.Width = 380;
            e1.Height = 280;
            e1.HorizontalAlignment = HorizontalAlignment.Right;
            e1.VerticalAlignment = VerticalAlignment.Top;
            e1.Margin = new Thickness(0, -130, -150, 0);
            RadialGradientBrush rg1 = new RadialGradientBrush();
            rg1.GradientStops.Add(new GradientStop(Ink.A(Ink.Dark ? (byte)0x14 : (byte)0x18, 0xFF, 0xFB, 0xF0), 0));
            rg1.GradientStops.Add(new GradientStop(Ink.A(0x00, 0xFF, 0xFB, 0xF0), 1));
            e1.Fill = rg1;
            halo.Children.Add(e1);

            Ellipse e2 = new Ellipse();
            e2.Width = 340;
            e2.Height = 300;
            e2.HorizontalAlignment = HorizontalAlignment.Left;
            e2.VerticalAlignment = VerticalAlignment.Bottom;
            e2.Margin = new Thickness(-130, 0, 0, -150);
            RadialGradientBrush rg2 = new RadialGradientBrush();
            rg2.GradientStops.Add(new GradientStop(Ink.A(Ink.Dark ? (byte)0x20 : (byte)0x0C, 0x00, 0x00, 0x00), 0));
            rg2.GradientStops.Add(new GradientStop(Ink.A(0x00, 0x00, 0x00, 0x00), 1));
            e2.Fill = rg2;
            halo.Children.Add(e2);

            return halo;
        }

        // ---------- 四角断笔：四段 1/4 弧，把盒子"提"住 ----------
        static UIElement Corners()
        {
            Grid g = new Grid();
            g.IsHitTestVisible = false;
            double r = 26;
            for (int i = 0; i < 4; i++)
            {
                PathFigure fig = new PathFigure();
                fig.StartPoint = new Point(r, 0);
                fig.Segments.Add(new ArcSegment(new Point(0, r), new Size(r, r), 0, false, SweepDirection.Clockwise, false));
                PathGeometry pg = new PathGeometry();
                pg.Figures.Add(fig);

                System.Windows.Shapes.Path p = new System.Windows.Shapes.Path();
                p.Data = pg;
                p.Stroke = Ink.Br(Ink.A(Ink.Dark ? (byte)0x1A : (byte)0x24, Ink.Fg().R, Ink.Fg().G, Ink.Fg().B));
                p.StrokeThickness = 1;

                switch (i)
                {
                    case 0:
                        p.HorizontalAlignment = HorizontalAlignment.Left;
                        p.VerticalAlignment = VerticalAlignment.Top;
                        p.Margin = new Thickness(7, 7, 0, 0);
                        break;
                    case 1:
                        p.HorizontalAlignment = HorizontalAlignment.Right;
                        p.VerticalAlignment = VerticalAlignment.Top;
                        p.Margin = new Thickness(0, 7, 7, 0);
                        p.RenderTransformOrigin = new Point(0.5, 0.5);
                        p.RenderTransform = new RotateTransform(90);
                        break;
                    case 2:
                        p.HorizontalAlignment = HorizontalAlignment.Right;
                        p.VerticalAlignment = VerticalAlignment.Bottom;
                        p.Margin = new Thickness(0, 0, 7, 7);
                        p.RenderTransformOrigin = new Point(0.5, 0.5);
                        p.RenderTransform = new RotateTransform(180);
                        break;
                    default:
                        p.HorizontalAlignment = HorizontalAlignment.Left;
                        p.VerticalAlignment = VerticalAlignment.Bottom;
                        p.Margin = new Thickness(7, 0, 0, 7);
                        p.RenderTransformOrigin = new Point(0.5, 0.5);
                        p.RenderTransform = new RotateTransform(-90);
                        break;
                }
                g.Children.Add(p);
            }
            return g;
        }

        // ---------- 头部：竖排墨守 + 参数 + 无印章 ----------
        UIElement Header()
        {
            Grid h = new Grid();
            h.Margin = new Thickness(0, 0, 0, 0);
            h.Background = Brushes.Transparent;
            h.MouseLeftButtonDown += delegate (object s, MouseButtonEventArgs e)
            {
                if (e.ClickCount == 2)
                {
                    // 双击顶栏在常规尺寸 (740) 与紧凑尺寸 (540) 间快捷切换
                    double targetW = (ActualWidth > 640) ? 540 : START_W;
                    SetAspectSize(targetW, true);
                    e.Handled = true;
                    return;
                }
                e.Handled = true;
                try { DragMove(); } catch { }
            };

            StackPanel title = new StackPanel();
            title.Orientation = Orientation.Vertical;
            title.HorizontalAlignment = HorizontalAlignment.Left;
            title.VerticalAlignment = VerticalAlignment.Center;

            TextBlock t1 = new TextBlock();
            t1.Text = "墨";
            t1.FontFamily = Ink.Serif;
            t1.FontSize = 17.5;
            t1.LineHeight = 19.5;
            t1.LineStackingStrategy = LineStackingStrategy.BlockLineHeight;
            t1.Foreground = Ink.Br(Ink.Fg());

            TextBlock t2 = new TextBlock();
            t2.Text = "守";
            t2.FontFamily = Ink.Serif;
            t2.FontSize = 17.5;
            t2.LineHeight = 19.5;
            t2.LineStackingStrategy = LineStackingStrategy.BlockLineHeight;
            t2.Foreground = Ink.Br(Ink.Fg());

            title.Children.Add(t1);
            title.Children.Add(t2);
            h.Children.Add(title);

            // 专属回顾点（紧邻「墨守」右侧）：一枚素雅空心墨环 + 朱砂微芯，随时点开回顾使用指引与重播开场动画
            Grid reviewDot = new Grid();
            reviewDot.Width = 22;
            reviewDot.Height = 22;
            reviewDot.HorizontalAlignment = HorizontalAlignment.Left;
            reviewDot.VerticalAlignment = VerticalAlignment.Center;
            reviewDot.Margin = new Thickness(24, 0, 0, 0);
            reviewDot.Background = Brushes.Transparent;
            reviewDot.Cursor = Cursors.Hand;
            reviewDot.Opacity = 0.48;
            reviewDot.ToolTip = "使用指引与开场动画回顾";
            ScaleTransform rdSc = new ScaleTransform(1, 1);
            reviewDot.RenderTransformOrigin = new Point(0.5, 0.5);
            reviewDot.RenderTransform = rdSc;
            reviewDotEl = reviewDot;
            reviewDotScale = rdSc;

            Ellipse outerRing = new Ellipse();
            outerRing.Width = 10;
            outerRing.Height = 10;
            outerRing.Stroke = Ink.Br(Ink.Fg2());
            outerRing.StrokeThickness = 1.0;
            outerRing.Fill = Brushes.Transparent;
            reviewDot.Children.Add(outerRing);

            Ellipse innerCore = new Ellipse();
            innerCore.Width = 3.2;
            innerCore.Height = 3.2;
            innerCore.Fill = Ink.Br(Ink.Verm());
            reviewDot.Children.Add(innerCore);

            reviewDot.MouseEnter += delegate
            {
                reviewDot.BeginAnimation(UIElement.OpacityProperty, Fade(reviewDot.Opacity, 1.0, 150));
                ScaleDown(rdSc, 1.18, 150);
            };
            reviewDot.MouseLeave += delegate
            {
                reviewDot.BeginAnimation(UIElement.OpacityProperty, Fade(reviewDot.Opacity, 0.48, 180));
                ScaleDown(rdSc, 1.0, 180);
            };
            reviewDot.MouseLeftButtonDown += delegate (object s, MouseButtonEventArgs ev) { ev.Handled = true; };
            reviewDot.MouseLeftButtonUp += delegate (object s, MouseButtonEventArgs ev)
            {
                ev.Handled = true;
                OpenGuide();
            };
            h.Children.Add(reviewDot);

            StackPanel meta = new StackPanel();
            meta.HorizontalAlignment = HorizontalAlignment.Right;
            meta.VerticalAlignment = VerticalAlignment.Center;
            meta.Margin = new Thickness(0, 0, 96, 2);

            tRes = MetaLine();
            tScale = MetaLine();
            tCount = MetaLine();
            meta.Children.Add(tRes);
            meta.Children.Add(tScale);
            meta.Children.Add(tCount);
            h.Children.Add(meta);
            return h;
        }

        TextBlock MetaLine()
        {
            TextBlock t = new TextBlock();
            t.FontFamily = Ink.Mono;
            t.FontSize = 9;
            t.LineHeight = 13;
            t.LineStackingStrategy = LineStackingStrategy.BlockLineHeight;
            t.HorizontalAlignment = HorizontalAlignment.Right;
            t.Margin = new Thickness(0, 1, 0, 0);
            t.Foreground = Ink.Br(Ink.Fg3());
            return t;
        }

        // ---------- 最小化 / 关闭：手绘笔画，不用圆点 ----------
        // 最小化静默用墨色（克制），关闭静默用淡朱砂、悬停满朱砂；移出自动复位缩放
        UIElement GlyphBtn(string kind, Thickness margin, Action act)
        {
            bool isClose = (kind == "close");
            Grid g = new Grid();
            g.Width = 44;
            g.Height = 44;
            g.HorizontalAlignment = HorizontalAlignment.Right;
            g.VerticalAlignment = VerticalAlignment.Top;
            g.Margin = margin;
            g.Background = Brushes.Transparent;
            g.Cursor = Cursors.Hand;
            g.Opacity = isClose ? 0.50 : 0.56;
            g.ToolTip = isClose ? "关闭 (Esc)" : "最小化";
            Panel.SetZIndex(g, 50);

            Canvas cv = new Canvas();
            cv.Width = 18;
            cv.Height = 18;
            cv.HorizontalAlignment = HorizontalAlignment.Center;
            cv.VerticalAlignment = VerticalAlignment.Center;
            cv.IsHitTestVisible = false;
            g.Children.Add(cv);

            Color strokeCol = isClose ? Ink.Verm() : Ink.Fg2();

            if (isClose)
            {
                Rough.Pen(cv, 4.6, 4.6, 13.4, 13.4, strokeCol, 1.45, 1.0, 0.52, 0.34, 5501, null);
                Rough.Pen(cv, 13.4, 4.6, 4.6, 13.4, strokeCol, 1.45, 1.0, 0.52, 0.34, 5502, null);
            }
            else
            {
                Rough.Pen(cv, 4.2, 9.0, 13.8, 9.0, strokeCol, 1.45, 1.0, 0.52, 0.34, 5503, null);
            }

            ScaleTransform st = new ScaleTransform(1, 1);
            g.RenderTransformOrigin = new Point(0.5, 0.5);
            g.RenderTransform = st;
            double restOp = g.Opacity;

            g.MouseEnter += delegate
            {
                g.BeginAnimation(UIElement.OpacityProperty, Fade(g.Opacity, 1.0, 160));
            };
            g.MouseLeave += delegate
            {
                g.BeginAnimation(UIElement.OpacityProperty, Fade(g.Opacity, restOp, 200));
                ScaleDown(st, 1.0, 140);
            };
            g.MouseLeftButtonDown += delegate (object s, MouseButtonEventArgs e)
            {
                e.Handled = true;
                ScaleDown(st, 0.86, 80);
            };
            g.MouseLeftButtonUp += delegate (object s, MouseButtonEventArgs e)
            {
                e.Handled = true;
                ScaleDown(st, 1.0, 140);
                act();
            };
            return g;
        }

        static DoubleAnimation Fade(double from, double to, int ms)
        {
            DoubleAnimation a = new DoubleAnimation(from, to, TimeSpan.FromMilliseconds(ms));
            a.EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut };
            return a;
        }

        static void ScaleDown(ScaleTransform st, double to, int ms)
        {
            DoubleAnimation a = new DoubleAnimation(st.ScaleX, to, TimeSpan.FromMilliseconds(ms));
            a.EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut };
            st.BeginAnimation(ScaleTransform.ScaleXProperty, a);
            DoubleAnimation b = new DoubleAnimation(st.ScaleY, to, TimeSpan.FromMilliseconds(ms));
            b.EasingFunction = a.EasingFunction;
            st.BeginAnimation(ScaleTransform.ScaleYProperty, b);
        }

        void FadeClose()
        {
            DoubleAnimation a = new DoubleAnimation(Opacity, 0, TimeSpan.FromMilliseconds(210));
            a.EasingFunction = new CubicEase { EasingMode = EasingMode.EaseIn };
            a.Completed += delegate { Close(); };
            BeginAnimation(Window.OpacityProperty, a);

            if (winScale != null)
            {
                DoubleAnimation sx = new DoubleAnimation(1.0, 0.982, TimeSpan.FromMilliseconds(210));
                sx.EasingFunction = new CubicEase { EasingMode = EasingMode.EaseIn };
                winScale.BeginAnimation(ScaleTransform.ScaleXProperty, sx);
                winScale.BeginAnimation(ScaleTransform.ScaleYProperty, sx);
            }
        }

        // ---------- 棋盘 ----------
        UIElement BoardHost()
        {
            Border host = new Border();
            host.Background = Ink.Br(Ink.BoardBg());
            host.CornerRadius = new CornerRadius(6);
            host.BorderThickness = new Thickness(0, 1, 0, 1);
            host.BorderBrush = Ink.Br(Ink.Hair());
            host.ClipToBounds = true;

            board = new Canvas();
            board.Background = Brushes.Transparent;
            board.ClipToBounds = true;
            board.MouseLeftButtonUp += delegate
            {
                if (armed)
                {
                    DisarmAll();
                    if (tStatus != null) tStatus.Text = "已取消操作 · 只动坐标 · 不动文件";
                }
            };

            gridLayer = new Canvas();
            wetLayer = new Canvas();
            wetLayer.IsHitTestVisible = false;
            sweepLayer = new Canvas();
            sweepLayer.IsHitTestVisible = false;
            dotLayer = new Canvas();
            previewLayer = new Canvas();
            previewLayer.IsHitTestVisible = false;
            Panel.SetZIndex(previewLayer, 60);

            // 拖拽目标交点的朱砂预告环
            dropTargetRing = new Ellipse();
            dropTargetRing.Width = 16;
            dropTargetRing.Height = 16;
            dropTargetRing.Stroke = Ink.Br(Ink.Verm());
            dropTargetRing.StrokeThickness = 1.4;
            dropTargetRing.StrokeDashArray = new DoubleCollection(new double[] { 2.5, 2.0 });
            dropTargetRing.Fill = Ink.Br(Ink.A(0x1E, Ink.Verm().R, Ink.Verm().G, Ink.Verm().B));
            dropTargetRing.Visibility = Visibility.Collapsed;
            previewLayer.Children.Add(dropTargetRing);

            // 棋盘内水墨实时浮卡预览（替代丑陋的系统默认灰框 Tooltip）
            dotPreviewCard = new Border();
            dotPreviewCard.CornerRadius = new CornerRadius(7);
            dotPreviewCard.Background = Ink.Br(Ink.A(0xF5, Ink.Bg().R, Ink.Bg().G, Ink.Bg().B));
            dotPreviewCard.BorderBrush = Ink.Br(Ink.Hair());
            dotPreviewCard.BorderThickness = new Thickness(1);
            dotPreviewCard.Padding = new Thickness(8, 5, 9, 5);
            dotPreviewCard.Visibility = Visibility.Collapsed;
            dotPreviewCard.Opacity = 0;

            StackPanel pvStack = new StackPanel { Orientation = Orientation.Vertical };
            StackPanel pvTop = new StackPanel { Orientation = Orientation.Horizontal };
            dotPreviewPip = new Ellipse { Width = 6, Height = 6, VerticalAlignment = VerticalAlignment.Center };
            dotPreviewName = new TextBlock
            {
                FontFamily = Ink.Sans,
                FontSize = 10,
                FontWeight = FontWeights.Medium,
                Margin = new Thickness(5, 0, 0, 0),
                Foreground = Ink.Br(Ink.Fg()),
                MaxWidth = 156,
                TextTrimming = TextTrimming.CharacterEllipsis
            };
            pvTop.Children.Add(dotPreviewPip);
            pvTop.Children.Add(dotPreviewName);
            pvStack.Children.Add(pvTop);

            dotPreviewSub = new TextBlock
            {
                FontFamily = Ink.Mono,
                FontSize = 8.5,
                Margin = new Thickness(11, 2, 0, 0),
                Foreground = Ink.Br(Ink.Fg3())
            };
            pvStack.Children.Add(dotPreviewSub);
            dotPreviewCard.Child = pvStack;
            previewLayer.Children.Add(dotPreviewCard);

            board.Children.Add(gridLayer);
            board.Children.Add(wetLayer);
            board.Children.Add(sweepLayer);
            board.Children.Add(dotLayer);
            board.Children.Add(previewLayer);
            host.Child = board;
            return host;
        }

        void DrawBoard()
        {
            if (board == null) return;
            // 固定种子：同一份数据每次画出的墨点都一样，刷新时不会抖
            Random jt = new Random(4242);
            double W = board.ActualWidth;
            if (W <= 20) W = 430;
            // 顶部/底部留 16px，确保第一行棋子从 -14px 下落时绝不被棋盘顶边切平
            double padX = 12;
            double padY = 16;
            bh = 224;
            bw = W;
            double availH = bh - padY * 2;
            board.Height = bh;

            double gx0, gdx, gy0, gdy;
            GetGridMetrics(out gx0, out gdx, out gy0, out gdy);

            // 计算当前分辨率对应的完整桌面网格边界（保证四边贴边格位全覆盖、皆可拖放）
            double scrW = (snap != null && snap.ScreenW > 400) ? snap.ScreenW : SystemParameters.PrimaryScreenWidth * GetDpiScale();
            double scrH = (snap != null && snap.ScreenH > 300) ? snap.ScreenH : SystemParameters.PrimaryScreenHeight * GetDpiScale();
            double scrScale = (snap != null && snap.Scale > 0.5) ? snap.Scale : GetDpiScale();
            int fullMaxC = Math.Max(7, (int)Math.Floor((scrW - gx0 - gdx * 0.45) / gdx));
            int fullMaxR = Math.Max(5, (int)Math.Floor((scrH - 46 * scrScale - gy0 - gdy * 0.45) / gdy));
            if (fullMaxC > 34) fullMaxC = 34;
            if (fullMaxR > 18) fullMaxR = 18;

            int ci0 = 0, ci1 = fullMaxC;
            int ri0 = 0, ri1 = fullMaxR;
            for (int i = 0; i < shown.Count; i++)
            {
                int ci = (int)Math.Round((shown[i].X - gx0) / gdx);
                int ri = (int)Math.Round((shown[i].Y - gy0) / gdy);
                if (ci < ci0) ci0 = ci;
                if (ci > ci1) ci1 = ci;
                if (ri < ri0) ri0 = ri;
                if (ri > ri1) ri1 = ri;
            }

            // 按全屏网格范围撑满棋盘区：横纵同比例尺，四边留半格让贴边点完整落框
            double xmin = gx0 + ci0 * gdx - gdx * 0.5;
            double xmax = gx0 + ci1 * gdx + gdx * 0.5;
            double ymin = gy0 + ri0 * gdy - gdy * 0.5;
            double ymax = gy0 + ri1 * gdy + gdy * 0.5;

            double sx = (W - padX * 2) / Math.Max(1, xmax - xmin);
            double sy = availH / Math.Max(1, ymax - ymin);
            double kx = Math.Min(sx, sy);
            double ky = kx;

            double contentW = (xmax - xmin) * kx;
            double contentH = (ymax - ymin) * ky;
            double ox = (W - contentW) / 2 - xmin * kx;
            double oy = (bh - contentH) / 2 - ymin * ky;

            boardGx0 = gx0; boardGdx = gdx; boardGy0 = gy0; boardGdy = gdy;
            boardOx = ox; boardOy = oy; boardK = kx;

            // 网格线与棋子共用这一对函数，从根上杜绝两处各算一遍造成的错位
            Func<double, double> GX = delegate (double v) { return ox + v * kx; };
            Func<double, double> GY = delegate (double v) { return oy + v * ky; };

            gridLayer.Children.Clear();
            wetLayer.Children.Clear();
            sweepLayer.Children.Clear();
            dotLayer.Children.Clear();
            HideDotPreview();
            if (dropTargetRing != null) dropTargetRing.Visibility = Visibility.Collapsed;
            gridLines.Clear();
            dots.Clear();
            dotColRank.Clear();
            dotRowRank.Clear();
            dotTypes.Clear();
            dotGridC.Clear();
            dotGridR.Clear();
            dotAnimGen.Clear();

            // 先算出每枚棋子的吸附格位，网格线由这些格位生成——线必然穿过棋子
            double[] sxs = new double[shown.Count];
            double[] sys = new double[shown.Count];
            int[] cIdx = new int[shown.Count];
            int[] rIdx = new int[shown.Count];
            HashSet<double> colSet = new HashSet<double>();
            HashSet<double> rowSet = new HashSet<double>();
            SortedSet<int> activeCols = new SortedSet<int>();
            SortedSet<int> activeRows = new SortedSet<int>();

            for (int i = 0; i < shown.Count; i++)
            {
                int ci = (int)Math.Round((shown[i].X - gx0) / gdx);
                int ri = (int)Math.Round((shown[i].Y - gy0) / gdy);
                double sv = gx0 + ci * gdx;
                double rv = gy0 + ri * gdy;
                cIdx[i] = ci;
                rIdx[i] = ri;
                sxs[i] = sv;
                sys[i] = rv;
                colSet.Add(sv);
                rowSet.Add(rv);
                activeCols.Add(ci);
                activeRows.Add(ri);
            }

            if (colSet.Count == 0 || rowSet.Count == 0) return;

            // 压缩列序/行序（0..K-1）：消除主区与收纳列之间的空等，让右侧收纳列也保持先后节奏
            Dictionary<int, int> colRankMap = new Dictionary<int, int>();
            int rk = 0;
            foreach (int c in activeCols) { colRankMap[c] = rk++; }
            Dictionary<int, int> rowRankMap = new Dictionary<int, int>();
            rk = 0;
            foreach (int r in activeRows) { rowRankMap[r] = rk++; }

            Title = "墨守";

            boardCi0 = ci0; boardCi1 = ci1; boardRi0 = ri0; boardRi1 = ri1;
            double[] cols = new double[ci1 - ci0 + 1];
            double[] rows = new double[ri1 - ri0 + 1];
            for (int c = ci0; c <= ci1; c++) cols[c - ci0] = gx0 + c * gdx;
            for (int r = ri0; r <= ri1; r++) rows[r - ri0] = gy0 + r * gdy;

            double gyTop = GY(rows[0]);
            double gyBot = GY(rows[rows.Length - 1]);
            double gxL = GX(cols[0]);
            double gxR = GX(cols[cols.Length - 1]);

            // 单线控透明：用纯不透明 Ink.Fg()，浓度只由 Opacity 一处决定（有棋子 0.13 / 空档 0.04）
            Color gridCol = Ink.Fg();
            for (int c = 0; c < cols.Length; c++)
            {
                double x = GX(cols[c]);
                if (x < 0 || x > W) continue;
                bool has = colSet.Contains(cols[c]);
                if (has)
                    Rough.Pen(gridLayer, x, Math.Max(4, gyTop - 8), x, Math.Min(bh - 4, gyBot + 8),
                              gridCol, 0.95, 0.13, 0.40, 0.32, 7000 + c * 17, gridLines);
                else
                    Rough.Pen(gridLayer, x, Math.Max(4, gyTop - 8), x, Math.Min(bh - 4, gyBot + 8),
                              gridCol, 0.65, 0.04, 0.58, 0.44, 7000 + c * 17, gridLines);
            }

            for (int r = 0; r < rows.Length; r++)
            {
                double y = GY(rows[r]);
                if (y < 0 || y > bh) continue;
                bool has = rowSet.Contains(rows[r]);
                if (has)
                    Rough.Pen(gridLayer, Math.Max(4, gxL - 8), y, Math.Min(W - 4, gxR + 8), y,
                              gridCol, 0.95, 0.13, 0.40, 0.32, 7300 + r * 19, gridLines);
                else
                    Rough.Pen(gridLayer, Math.Max(4, gxL - 8), y, Math.Min(W - 4, gxR + 8), y,
                              gridCol, 0.65, 0.04, 0.58, 0.44, 7300 + r * 19, gridLines);
            }

            string deskDir = Ink.DesktopDir();
            // 棋径跟着格距走，永远留出缝隙
            double rad = 0.24 * Math.Min(gdx * kx, gdy * ky);
            if (rad < 3.0) rad = 3.0;

            Dictionary<string, int> used = new Dictionary<string, int>();
            Dictionary<string, int> typeCounts = new Dictionary<string, int>();
            typeCounts["app"] = 0; typeCounts["dir"] = 0; typeCounts["arc"] = 0;
            typeCounts["doc"] = 0; typeCounts["sys"] = 0; typeCounts["other"] = 0;

            for (int i = 0; i < shown.Count; i++)
            {
                DeskIcon d = shown[i];
                string key = sxs[i] + ":" + sys[i];
                int bump = 0;
                if (used.ContainsKey(key)) { bump = used[key]; used[key] = bump + 1; }
                else { used[key] = 1; }

                double cx = GX(sxs[i]) + bump * rad * 0.5;
                double cy = GY(sys[i]);
                string ty = Ink.TypeOf(d.Name, deskDir);
                if (typeCounts.ContainsKey(ty)) typeCounts[ty]++;

                // 棋子圆心严格落在网格交点上（0px 偏移，消除排列不齐感），仅保留柔边墨晕与微弱浓淡/椭圆变化
                double phase = cx * 0.017 + cy * 0.021;
                double jv = 1.0 + 0.022 * Math.Sin(phase) + 0.012 * Math.Sin(phase * 2.3 + 1.7);
                double ax = 0.985 + (jt.NextDouble() * 0.03);
                double ay = 0.985 + (jt.NextDouble() * 0.03);
                double ang = -4 + (jt.NextDouble() * 8);
                Color cc = Ink.ByType(ty);
                cc.A = (byte)(0xE6 + jt.Next(0x1A));

                double ew = rad * 2 * jv * ax;
                double eh = rad * 2 * jv * ay;

                Ellipse e = new Ellipse();
                e.Width = ew;
                e.Height = eh;
                e.Fill = Ink.Soft(cc);
                e.Tag = cc;
                e.Opacity = 1;
                e.Cursor = Cursors.Hand;
                Canvas.SetLeft(e, cx - ew * 0.5);
                Canvas.SetTop(e, cy - eh * 0.5);

                TranslateTransform tt = new TranslateTransform(0, 0);
                ScaleTransform sc = new ScaleTransform(1, 1);
                RotateTransform rt = new RotateTransform(ang);
                TransformGroup tg = new TransformGroup();
                tg.Children.Add(sc);
                tg.Children.Add(tt);
                tg.Children.Add(rt);
                e.RenderTransform = tg;
                e.RenderTransformOrigin = new Point(0.5, 0.5);

                int capturedIdx = i;
                Ellipse captured = e;
                e.MouseEnter += delegate
                {
                    if (dragIdx >= 0) return;
                    ScaleTo(captured, 1.28, 120);
                    ShowDotPreview(capturedIdx, null);
                };
                e.MouseLeave += delegate
                {
                    if (dragIdx == capturedIdx) return;
                    ScaleTo(captured, 1.0, 160);
                    if (dragIdx < 0) HideDotPreview();
                };
                e.MouseLeftButtonDown += delegate (object s, MouseButtonEventArgs ev)
                {
                    ev.Handled = true;
                    StartDotDrag(capturedIdx, ev.GetPosition(board));
                };
                e.MouseMove += delegate (object s, MouseEventArgs ev)
                {
                    if (dragIdx == capturedIdx)
                        MoveDotDrag(ev.GetPosition(board));
                };
                e.MouseLeftButtonUp += delegate (object s, MouseButtonEventArgs ev)
                {
                    ev.Handled = true;
                    if (dragIdx == capturedIdx) EndDotDrag();
                };

                dotLayer.Children.Add(e);
                dots.Add(e);
                dotColRank.Add(colRankMap[cIdx[i]]);
                dotRowRank.Add(rowRankMap[rIdx[i]]);
                dotTypes.Add(ty);
                dotGridC.Add(cIdx[i]);
                dotGridR.Add(rIdx[i]);
                dotAnimGen.Add(0);
            }

            // 同步更新六色图例的数量注脚
            foreach (KeyValuePair<string, TextBlock> kv in legendCounts)
            {
                int n = typeCounts.ContainsKey(kv.Key) ? typeCounts[kv.Key] : 0;
                kv.Value.Text = n > 0 ? " " + n : "";
            }
        }

        // ---------- 棋盘内实时水墨浮卡预览 + 拖拽草稿排兵布阵 ----------
        void ShowDotPreview(int idx, string extraLine)
        {
            if (dotPreviewCard == null || idx < 0 || idx >= shown.Count || idx >= dots.Count) return;
            DeskIcon d = shown[idx];
            string ty = idx < dotTypes.Count ? dotTypes[idx] : "other";
            Color pc = Ink.ByType(ty);

            dotPreviewPip.Fill = Ink.Br(pc);
            dotPreviewName.Text = d.Name;
            int colNo = (idx < dotGridC.Count ? dotGridC[idx] - boardCi0 + 1 : 1);
            int rowNo = (idx < dotGridR.Count ? dotGridR[idx] - boardRi0 + 1 : 1);
            string zTag = ZoneLabel(DetectZone(d.Name, ty));
            dotPreviewSub.Text = !string.IsNullOrEmpty(extraLine)
                ? extraLine
                : (TypeLabel(ty) + " · " + zTag + "区 · 第" + colNo + "列 第" + rowNo + "行");

            dotPreviewCard.Measure(new Size(220, 60));
            double cw = dotPreviewCard.DesiredSize.Width;
            double ch = dotPreviewCard.DesiredSize.Height;

            double px = Canvas.GetLeft(dots[idx]) + dots[idx].Width * 0.5;
            double py = Canvas.GetTop(dots[idx]) + dots[idx].Height * 0.5;
            double left = px + 14;
            if (left + cw > bw - 6) left = px - cw - 14;
            if (left < 6) left = 6;
            double top = py - ch - 10;
            if (top < 6) top = py + 14;
            if (top + ch > bh - 6) top = bh - ch - 6;

            Canvas.SetLeft(dotPreviewCard, left);
            Canvas.SetTop(dotPreviewCard, top);

            if (dotPreviewCard.Visibility != Visibility.Visible)
            {
                dotPreviewCard.Visibility = Visibility.Visible;
                DoubleAnimation op = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(110));
                op.EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut };
                dotPreviewCard.BeginAnimation(UIElement.OpacityProperty, op);
            }
            else
            {
                dotPreviewCard.Opacity = 1;
            }
        }

        void HideDotPreview()
        {
            if (dotPreviewCard == null || dotPreviewCard.Visibility != Visibility.Visible) return;
            dotPreviewCard.BeginAnimation(UIElement.OpacityProperty, null);
            dotPreviewCard.Opacity = 0;
            dotPreviewCard.Visibility = Visibility.Collapsed;
        }

        double CellToX(int ci) { return boardOx + (boardGx0 + ci * boardGdx) * boardK; }
        double CellToY(int ri) { return boardOy + (boardGy0 + ri * boardGdy) * boardK; }

        int FindDotAtCell(int ci, int ri, int excludeIdx)
        {
            for (int i = 0; i < dotGridC.Count; i++)
            {
                if (i == excludeIdx || absorbedIndices.Contains(i)) continue;
                if (dotGridC[i] == ci && dotGridR[i] == ri) return i;
            }
            return -1;
        }

        bool CanAbsorbInto(int srcIdx, int dstIdx)
        {
            if (srcIdx < 0 || dstIdx < 0 || srcIdx == dstIdx) return false;
            if (srcIdx >= dotTypes.Count || dstIdx >= dotTypes.Count) return false;
            // 系统虚拟项（此电脑/回收站）不可移入文件夹；目标必须是文件夹
            return dotTypes[dstIdx] == "dir" && dotTypes[srcIdx] != "sys";
        }

        void ClearAbsorbHover()
        {
            if (absorbHoverFolder >= 0 && absorbHoverFolder < dots.Count)
                ScaleTo(dots[absorbHoverFolder], 1.0, 140);
            absorbHoverFolder = -1;
            dwellFolderIdx = -1;
            absorbReady = false;
        }

        void StartDotDrag(int idx, Point pt)
        {
            if (busy || idx < 0 || idx >= dots.Count || absorbedIndices.Contains(idx)) return;
            // 预览历史快照期间禁止拖拽，先退出历史抽屉再布阵
            if (historyOverlay != null && historyOverlay.Visibility == Visibility.Visible) return;

            dragIdx = idx;
            dragMoved = false;
            dragStartPt = pt;
            dragOrigLeft = Canvas.GetLeft(dots[idx]);
            dragOrigTop = Canvas.GetTop(dots[idx]);
            dragTargetC = dotGridC[idx];
            dragTargetR = dotGridR[idx];
            ClearAbsorbHover();
            dots[idx].CaptureMouse();
        }

        void MoveDotDrag(Point pt)
        {
            if (dragIdx < 0 || dragIdx >= dots.Count) return;
            double dx = pt.X - dragStartPt.X;
            double dy = pt.Y - dragStartPt.Y;

            if (!dragMoved)
            {
                if (Math.Sqrt(dx * dx + dy * dy) < 3.5) return;
                dragMoved = true;
                Panel.SetZIndex(dots[dragIdx], 45);
                ScaleTo(dots[dragIdx], 1.35, 100);
                FadeGrid(1.0, 2.0, 150);
                if (dropTargetRing != null) dropTargetRing.Visibility = Visibility.Visible;
            }

            Ellipse e = dots[dragIdx];
            double nx = dragOrigLeft + dx;
            double ny = dragOrigTop + dy;
            Canvas.SetLeft(e, nx);
            Canvas.SetTop(e, ny);

            // 计算最近目标网格交点
            double cx = nx + e.Width * 0.5;
            double cy = ny + e.Height * 0.5;
            double deskX = (cx - boardOx) / boardK;
            double deskY = (cy - boardOy) / boardK;
            int tci = (int)Math.Round((deskX - boardGx0) / boardGdx);
            int tri = (int)Math.Round((deskY - boardGy0) / boardGdy);
            if (tci < boardCi0) tci = boardCi0;
            if (tci > boardCi1) tci = boardCi1;
            if (tri < boardRi0) tri = boardRi0;
            if (tri > boardRi1) tri = boardRi1;
            dragTargetC = tci;
            dragTargetR = tri;

            double tx = CellToX(tci);
            double ty = CellToY(tri);

            int occ = FindDotAtCell(tci, tri, dragIdx);
            bool isFolderTarget = CanAbsorbInto(dragIdx, occ);
            bool shiftDown = (Keyboard.Modifiers & ModifierKeys.Shift) == ModifierKeys.Shift;
            if (isFolderTarget)
            {
                if (dwellFolderIdx != occ)
                {
                    ClearAbsorbHover();
                    dwellFolderIdx = occ;
                    dwellStartUtc = DateTime.UtcNow;
                    absorbReady = shiftDown;
                }
                else if (!absorbReady && (shiftDown || (DateTime.UtcNow - dwellStartUtc).TotalMilliseconds >= 450))
                {
                    absorbReady = true;
                }
                if (absorbReady && absorbHoverFolder != occ)
                {
                    absorbHoverFolder = occ;
                    ScaleTo(dots[occ], 1.48, 120);
                }
            }
            else
            {
                ClearAbsorbHover();
            }

            bool absorb = isFolderTarget && absorbReady;
            if (dropTargetRing != null)
            {
                Color rc = absorb ? Ink.ByType("dir") : Ink.Verm();
                dropTargetRing.Stroke = Ink.Br(rc);
                dropTargetRing.Fill = Ink.Br(Ink.A(absorb ? (byte)0x30 : (byte)0x1E, rc.R, rc.G, rc.B));
                Canvas.SetLeft(dropTargetRing, tx - dropTargetRing.Width * 0.5);
                Canvas.SetTop(dropTargetRing, ty - dropTargetRing.Height * 0.5);
            }

            string hint;
            if (absorb && occ < shown.Count)
                hint = "松手收纳进文件夹「" + shown[occ].Name + "」";
            else if (isFolderTarget && occ < shown.Count)
                hint = "松手与「" + shown[occ].Name + "」互换（停顿/Shift 收纳）";
            else if (occ >= 0 && occ < shown.Count)
                hint = "松手与「" + shown[occ].Name + "」互换 · 第" + (tci - boardCi0 + 1) + "列 第" + (tri - boardRi0 + 1) + "行";
            else
                hint = "移至 第" + (tci - boardCi0 + 1) + "列 第" + (tri - boardRi0 + 1) + "行";
            ShowDotPreview(dragIdx, hint);
        }

        void EnsureDraftInitialized()
        {
            if (draftDirty) return;
            draftBackup = new List<DeskIcon>(shown.Count);
            for (int i = 0; i < shown.Count; i++)
            {
                draftBackup.Add(new DeskIcon
                {
                    Index = shown[i].Index,
                    Name = shown[i].Name,
                    X = shown[i].X,
                    Y = shown[i].Y
                });
            }
            draftDirty = true;
            draftStack.Clear();
            absorbedIndices.Clear();
            DisarmAll();
        }

        void EndDotDrag()
        {
            int idx = dragIdx;
            if (idx < 0 || idx >= dots.Count) return;
            Ellipse e = dots[idx];
            e.ReleaseMouseCapture();
            dragIdx = -1;
            bool doAbsorbNow = absorbReady && dwellFolderIdx >= 0;
            ClearAbsorbHover();

            if (!dragMoved)
            {
                DeskIcon d = shown[idx];
                string ty = idx < dotTypes.Count ? dotTypes[idx] : "other";
                if (tStatus != null)
                    tStatus.Text = d.Name + " · " + TypeLabel(ty) + " · (" + d.X + ", " + d.Y + ") · 按住拖动可排布或拖入文件夹收纳";
                return;
            }

            Panel.SetZIndex(e, 0);
            FadeGrid(2.0, 1.0, 220);
            if (dropTargetRing != null) dropTargetRing.Visibility = Visibility.Collapsed;

            int oldC = dotGridC[idx];
            int oldR = dotGridR[idx];
            int newC = dragTargetC;
            int newR = dragTargetR;

            if (newC != oldC || newR != oldR)
            {
                EnsureDraftInitialized();

                int occ = FindDotAtCell(newC, newR, idx);
                if (doAbsorbNow && CanAbsorbInto(idx, occ))
                {
                    // 收纳进文件夹：文件点收缩吸入文件夹中心，文件夹石绿水痕脉动
                    DraftOp op = new DraftOp
                    {
                        IsAbsorb = true,
                        IdxA = idx, OldCA = oldC, OldRA = oldR, NewCA = newC, NewRA = newR,
                        IdxB = occ, OldCB = newC, OldRB = newR,
                        SrcName = shown[idx].Name,
                        FolderName = shown[occ].Name
                    };
                    draftStack.Push(op);
                    absorbedIndices.Add(idx);
                    int myGen = ++dotAnimGen[idx];
                    e.IsHitTestVisible = false;

                    double fx = CellToX(newC), fy = CellToY(newR);
                    AnimateDotSnap(e, fx, fy);
                    ScaleTo(e, 0.15, 220);
                    e.BeginAnimation(UIElement.OpacityProperty, null);
                    e.Opacity = 0.0;
                    DoubleAnimation fadeOut = new DoubleAnimation(1.0, 0.0, TimeSpan.FromMilliseconds(220));
                    fadeOut.EasingFunction = new CubicEase { EasingMode = EasingMode.EaseIn };
                    fadeOut.FillBehavior = FillBehavior.Stop;
                    fadeOut.Completed += delegate
                    {
                        if (idx < dotAnimGen.Count && dotAnimGen[idx] == myGen && absorbedIndices.Contains(idx))
                            e.Visibility = Visibility.Collapsed;
                    };
                    e.BeginAnimation(UIElement.OpacityProperty, fadeOut);

                    ScaleTo(dots[occ], 1.0, 220);
                    SpawnWetRing(dots[occ], 80, 0.48);
                    UpdateDraftButtons();
                }
                else
                {
                    ScaleTo(e, 1.0, 160);
                    DraftOp op = new DraftOp
                    {
                        IsAbsorb = false,
                        IdxA = idx, OldCA = oldC, OldRA = oldR, NewCA = newC, NewRA = newR,
                        IdxB = occ, OldCB = occ >= 0 ? newC : -1, OldRB = occ >= 0 ? newR : -1
                    };
                    draftStack.Push(op);

                    if (occ >= 0)
                    {
                        dotGridC[occ] = oldC;
                        dotGridR[occ] = oldR;
                        shown[occ].X = (int)Math.Round(boardGx0 + oldC * boardGdx);
                        shown[occ].Y = (int)Math.Round(boardGy0 + oldR * boardGdy);
                        AnimateDotSnap(dots[occ], CellToX(oldC), CellToY(oldR));
                    }

                    dotGridC[idx] = newC;
                    dotGridR[idx] = newR;
                    shown[idx].X = (int)Math.Round(boardGx0 + newC * boardGdx);
                    shown[idx].Y = (int)Math.Round(boardGy0 + newR * boardGdy);

                    AnimateDotSnap(e, CellToX(newC), CellToY(newR));
                    SpawnWetRing(e, 40, 0.34);
                    UpdateDraftButtons();
                }
            }
            else
            {
                ScaleTo(e, 1.0, 160);
                AnimateDotSnap(e, CellToX(oldC), CellToY(oldR));
            }
            HideDotPreview();
        }

        int CountAbsorbOps()
        {
            int c = 0;
            foreach (DraftOp op in draftStack) if (op.IsAbsorb) c++;
            return c;
        }

        void UpdateDraftButtons()
        {
            int n = draftStack.Count;
            if (n <= 0)
            {
                ExitDraftUi();
                if (tStatus != null) tStatus.Text = "已全部撤回 · 桌面未做任何改动";
                return;
            }
            int abs = CountAbsorbOps();
            if (btnSave != null)
            {
                btnSave.Label.Text = abs > 0 ? ("应用布阵 (含收纳" + abs + ")") : ("应用布阵 (" + n + ")");
                btnSave.SetPrimary(true);
                btnSave.RetractMode = false;
            }
            if (btnRestore != null) { btnRestore.Label.Text = "撤销一步"; btnRestore.SetPrimary(false); btnRestore.RetractMode = true; }
            if (tStatus != null)
                tStatus.Text = abs > 0
                    ? ("布阵草稿 · 共 " + n + " 步（含收纳 " + abs + " 项入文件夹）· Ctrl+Z 单步撤销，Esc 重置")
                    : ("布阵草稿 · 共 " + n + " 步 · 点「撤销一步」(Ctrl+Z) 局部撤回，Esc 全部重置");
        }

        // 局部单步撤销：支持普通位移、多次收纳进文件夹的逐次吐出、以及一键推荐阵型的整批撤回
        void UndoLastDraftStep()
        {
            if (!draftDirty || draftStack.Count == 0) return;
            DraftOp op = draftStack.Pop();

            if (op.IsBatch && op.BatchOldC != null && op.BatchOldR != null)
            {
                int n = Math.Min(dots.Count, Math.Min(op.BatchOldC.Count, op.BatchOldR.Count));
                for (int i = 0; i < n; i++)
                {
                    if (absorbedIndices.Contains(i)) continue;
                    dotGridC[i] = op.BatchOldC[i];
                    dotGridR[i] = op.BatchOldR[i];
                    shown[i].X = (int)Math.Round(boardGx0 + op.BatchOldC[i] * boardGdx);
                    shown[i].Y = (int)Math.Round(boardGy0 + op.BatchOldR[i] * boardGdy);
                    AnimateDotSnap(dots[i], CellToX(op.BatchOldC[i]), CellToY(op.BatchOldR[i]));
                }
                UpdateDraftButtons();
                return;
            }

            if (op.IsAbsorb && op.IdxA >= 0 && op.IdxA < dots.Count)
            {
                absorbedIndices.Remove(op.IdxA);
                if (op.IdxA < dotAnimGen.Count) dotAnimGen[op.IdxA]++;
                Ellipse ea = dots[op.IdxA];
                // 目标文件夹若在收纳后被移动过，从文件夹当前格位吐出更自然
                int fC = (op.IdxB >= 0 && op.IdxB < dotGridC.Count) ? dotGridC[op.IdxB] : op.NewCA;
                int fR = (op.IdxB >= 0 && op.IdxB < dotGridR.Count) ? dotGridR[op.IdxB] : op.NewRA;
                double fx = CellToX(fC), fy = CellToY(fR);
                SpawnUndoGhostRing(fx, fy);

                ea.BeginAnimation(Canvas.LeftProperty, null);
                ea.BeginAnimation(Canvas.TopProperty, null);
                ea.BeginAnimation(UIElement.OpacityProperty, null);
                ea.Visibility = Visibility.Visible;
                ea.IsHitTestVisible = true;
                ea.Opacity = 1.0;
                Canvas.SetLeft(ea, fx - ea.Width * 0.5);
                Canvas.SetTop(ea, fy - ea.Height * 0.5);

                DoubleAnimation fadeIn = new DoubleAnimation(0.0, 1.0, TimeSpan.FromMilliseconds(220));
                fadeIn.EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut };
                fadeIn.FillBehavior = FillBehavior.Stop;
                ea.BeginAnimation(UIElement.OpacityProperty, fadeIn);
                ScaleTo(ea, 1.0, 240);

                dotGridC[op.IdxA] = op.OldCA;
                dotGridR[op.IdxA] = op.OldRA;
                shown[op.IdxA].X = (int)Math.Round(boardGx0 + op.OldCA * boardGdx);
                shown[op.IdxA].Y = (int)Math.Round(boardGy0 + op.OldRA * boardGdy);
                AnimateDotSnap(ea, CellToX(op.OldCA), CellToY(op.OldRA));
                UpdateDraftButtons();
                return;
            }

            if (op.IdxA >= 0 && op.IdxA < dots.Count)
            {
                SpawnUndoGhostRing(CellToX(op.NewCA), CellToY(op.NewRA));
                dotGridC[op.IdxA] = op.OldCA;
                dotGridR[op.IdxA] = op.OldRA;
                shown[op.IdxA].X = (int)Math.Round(boardGx0 + op.OldCA * boardGdx);
                shown[op.IdxA].Y = (int)Math.Round(boardGy0 + op.OldRA * boardGdy);
                AnimateDotSnap(dots[op.IdxA], CellToX(op.OldCA), CellToY(op.OldRA));
            }
            if (op.IdxB >= 0 && op.IdxB < dots.Count)
            {
                dotGridC[op.IdxB] = op.OldCB;
                dotGridR[op.IdxB] = op.OldRB;
                shown[op.IdxB].X = (int)Math.Round(boardGx0 + op.OldCB * boardGdx);
                shown[op.IdxB].Y = (int)Math.Round(boardGy0 + op.OldRB * boardGdy);
                AnimateDotSnap(dots[op.IdxB], CellToX(op.OldCB), CellToY(op.OldRB));
            }
            UpdateDraftButtons();
        }

        void SpawnUndoGhostRing(double cx, double cy)
        {
            Ellipse r = new Ellipse();
            r.Width = 18; r.Height = 18;
            r.Fill = Ink.InkRing(Ink.Verm());
            r.IsHitTestVisible = false;
            r.Opacity = 0;
            Canvas.SetLeft(r, cx - 9);
            Canvas.SetTop(r, cy - 9);
            ScaleTransform st = new ScaleTransform(1.3, 1.3);
            r.RenderTransformOrigin = new Point(0.5, 0.5);
            r.RenderTransform = st;
            wetLayer.Children.Add(r);

            DoubleAnimation sc = new DoubleAnimation(1.3, 0.25, TimeSpan.FromMilliseconds(320));
            sc.EasingFunction = new CubicEase { EasingMode = EasingMode.EaseIn };
            st.BeginAnimation(ScaleTransform.ScaleXProperty, sc);
            st.BeginAnimation(ScaleTransform.ScaleYProperty, sc);

            DoubleAnimationUsingKeyFrames op = new DoubleAnimationUsingKeyFrames();
            op.KeyFrames.Add(new LinearDoubleKeyFrame(0.55, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(50))));
            op.KeyFrames.Add(new EasingDoubleKeyFrame(0.0, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(320)),
                new CubicEase { EasingMode = EasingMode.EaseIn }));
            op.Completed += delegate { if (wetLayer.Children.Contains(r)) wetLayer.Children.Remove(r); };
            r.BeginAnimation(UIElement.OpacityProperty, op);
        }

        void AnimateDotSnap(Ellipse e, double targetCx, double targetCy)
        {
            double curL = Canvas.GetLeft(e);
            double curT = Canvas.GetTop(e);
            if (double.IsNaN(curL)) curL = targetCx - e.Width * 0.5;
            if (double.IsNaN(curT)) curT = targetCy - e.Height * 0.5;
            double toL = targetCx - e.Width * 0.5;
            double toT = targetCy - e.Height * 0.5;

            // 先写定最终本地值，再用 FillBehavior.Stop 过渡：彻底杜绝连续快速撤销时的 Completed 回调覆盖竞态
            e.BeginAnimation(Canvas.LeftProperty, null);
            e.BeginAnimation(Canvas.TopProperty, null);
            Canvas.SetLeft(e, toL);
            Canvas.SetTop(e, toT);

            DoubleAnimation ax = new DoubleAnimation(curL, toL, TimeSpan.FromMilliseconds(220));
            ax.EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut };
            ax.FillBehavior = FillBehavior.Stop;
            e.BeginAnimation(Canvas.LeftProperty, ax);

            DoubleAnimation ay = new DoubleAnimation(curT, toT, TimeSpan.FromMilliseconds(220));
            ay.EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut };
            ay.FillBehavior = FillBehavior.Stop;
            e.BeginAnimation(Canvas.TopProperty, ay);
        }

        void ExitDraftUi()
        {
            draftDirty = false;
            draftStack.Clear();
            absorbedIndices.Clear();
            ClearAbsorbHover();
            draftBackup = null;
            if (btnSave != null) { btnSave.Label.Text = "落子成记"; btnSave.SetPrimary(true); btnSave.RetractMode = false; }
            if (btnRestore != null) { btnRestore.Label.Text = "归位"; btnRestore.SetPrimary(false); btnRestore.RetractMode = false; }
        }

        void CancelDraft()
        {
            if (!draftDirty) return;
            if (draftBackup != null)
            {
                shown.Clear();
                for (int i = 0; i < draftBackup.Count; i++) shown.Add(draftBackup[i]);
            }
            ExitDraftUi();
            DrawBoard();
            if (tStatus != null) tStatus.Text = "已重置全部布阵 · 桌面未做任何改动";
        }

        // 安全将桌面文件/子文件夹移入目标桌面文件夹（防覆盖自动编号，绝不删文件）
        static bool MoveDesktopItemIntoFolder(string srcDisplayName, string dstFolderName)
        {
            string desk = Ink.DesktopDir();
            string srcPath = Ink.ResolveRealPath(srcDisplayName, desk);
            string dstDir = Ink.ResolveRealPath(dstFolderName, desk);
            if (string.IsNullOrEmpty(srcPath) || string.IsNullOrEmpty(dstDir)) return false;
            if (!Directory.Exists(dstDir)) return false;
            if (srcPath.Equals(dstDir, StringComparison.OrdinalIgnoreCase)) return false;

            string baseName = System.IO.Path.GetFileName(srcPath);
            string target = System.IO.Path.Combine(dstDir, baseName);
            if (File.Exists(target) || Directory.Exists(target))
            {
                string stem = System.IO.Path.GetFileNameWithoutExtension(baseName);
                string ext = System.IO.Path.GetExtension(baseName);
                for (int k = 1; k < 100; k++)
                {
                    string cand = System.IO.Path.Combine(dstDir, stem + "_" + k + ext);
                    if (!File.Exists(cand) && !Directory.Exists(cand)) { target = cand; break; }
                }
            }

            if (File.Exists(srcPath)) { File.Move(srcPath, target); return true; }
            if (Directory.Exists(srcPath)) { Directory.Move(srcPath, target); return true; }
            return false;
        }

        void ApplyDraft()
        {
            if (busy || !draftDirty) return;

            // 若包含「拖入文件夹收纳」，需显式弹窗确认再动真实文件
            List<DraftOp> absorbOps = new List<DraftOp>();
            DraftOp[] arr = draftStack.ToArray();
            for (int i = arr.Length - 1; i >= 0; i--)
                if (arr[i].IsAbsorb) absorbOps.Add(arr[i]);

            if (absorbOps.Count > 0)
            {
                MessageBoxResult cf = MessageBox.Show(
                    "本次布阵包含将 " + absorbOps.Count + " 个桌面项移入对应文件夹（不会删除任何文件，可随时在目标文件夹内找回）。\n\n确认执行吗？",
                    "确认收纳进文件夹", MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (cf != MessageBoxResult.Yes) return;
            }

            busy = true;
            try
            {
                // 应用前先把当前真实桌面自动备份进 layouts\，确保 100% 可回溯
                Snapshot before = Engine.Capture("布阵前 " + DateTime.Now.ToString("HH:mm"));
                Engine.Backup(root, before);

                // 1. 执行收纳进文件夹（仅针对用户显式确认的收纳项）
                int absorbedOk = 0;
                if (absorbOps.Count > 0)
                {
                    Ink.ResetIndex();
                    for (int i = 0; i < absorbOps.Count; i++)
                    {
                        try
                        {
                            if (MoveDesktopItemIntoFolder(absorbOps[i].SrcName, absorbOps[i].FolderName))
                                absorbedOk++;
                        }
                        catch { }
                    }
                    System.Threading.Thread.Sleep(240);
                }

                // 2. 应用剩余桌面图标坐标布阵
                Snapshot nextSnap = new Snapshot();
                nextSnap.SavedAt = Engine.Today();
                nextSnap.Note = absorbedOk > 0
                    ? ("布阵收纳(" + absorbedOk + ") " + DateTime.Now.ToString("HH:mm"))
                    : ("棋盘布阵 " + DateTime.Now.ToString("HH:mm"));
                nextSnap.ScreenW = before.ScreenW;
                nextSnap.ScreenH = before.ScreenH;
                nextSnap.Scale = before.Scale;
                nextSnap.LogicalW = before.LogicalW;
                nextSnap.LogicalH = before.LogicalH;
                nextSnap.GridX0 = (int)Math.Round(boardGx0);
                nextSnap.GridDx = (int)Math.Round(boardGdx);
                nextSnap.GridY0 = (int)Math.Round(boardGy0);
                nextSnap.GridDy = (int)Math.Round(boardGdy);
                nextSnap.GridRows = boardRi1 - boardRi0 + 1;
                for (int i = 0; i < shown.Count; i++)
                {
                    if (absorbedIndices.Contains(i)) continue;
                    nextSnap.Items.Add(new DeskIcon { Index = shown[i].Index, Name = shown[i].Name, X = shown[i].X, Y = shown[i].Y });
                }
                nextSnap.Count = nextSnap.Items.Count;

                bool blockedAuto = false;
                int drift = 0;
                int moved = ApplyDesktopExact(nextSnap, out blockedAuto, out drift);
                if (blockedAuto)
                {
                    if (tStatus != null)
                        tStatus.Text = "桌面已开启「自动排列图标」· 请右键桌面→查看→取消勾选「自动排列图标」后再试";
                    return;
                }

                // 落位后回扫真实桌面坐标，确保棋盘显示与真实桌面 100% 严丝合缝
                Snapshot actual = Engine.Capture(nextSnap.Note);
                JsonStore.Save(System.IO.Path.Combine(root, "layout_snapshot.json"), actual);
                Engine.Backup(root, actual);

                ExitDraftUi();
                RefreshMeta();
                DrawBoard();
                AnimSave();
                if (tStatus != null)
                {
                    tStatus.Text = absorbedOk > 0
                        ? ("已收纳 " + absorbedOk + " 项入文件夹并精准布阵（移动 " + moved + " 项）")
                        : ("已精准应用棋盘布阵 · 移动 " + moved + " 项（原布局已留底）");
                }
            }
            catch (Exception ex)
            {
                if (tStatus != null) tStatus.Text = "应用布阵失败 · " + ex.Message;
            }
            finally { busy = false; }
        }

        void OnPrimaryBtn()
        {
            if (draftDirty) ApplyDraft();
            else DoSave();
        }

        void OnSecondaryBtn()
        {
            if (draftDirty) UndoLastDraftStep();
            else DoRestore();
        }

        static string TypeLabel(string t)
        {
            if (t == "app") return "软件";
            if (t == "dir") return "文件夹";
            if (t == "arc") return "压缩包";
            if (t == "doc") return "文档";
            if (t == "sys") return "系统";
            return "其他";
        }

        // 悬停图例时高亮对应类型棋子，其余暂淡
        void FilterDotsByType(string typeOrNull)
        {
            for (int i = 0; i < dots.Count; i++)
            {
                Ellipse e = dots[i];
                bool match = (typeOrNull == null) || (i < dotTypes.Count && dotTypes[i] == typeOrNull);
                double targetOp = match ? 1.0 : 0.22;
                double targetSc = (typeOrNull != null && match) ? 1.22 : 1.0;

                DoubleAnimation op = new DoubleAnimation(targetOp, TimeSpan.FromMilliseconds(160));
                op.EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut };
                e.BeginAnimation(UIElement.OpacityProperty, op);
                ScaleTo(e, targetSc, 160);
            }
        }

        void ScaleTo(Ellipse e, double to, int ms)
        {
            if (!(e.RenderTransform is TransformGroup)) return;
            ScaleTransform sc = (ScaleTransform)((TransformGroup)e.RenderTransform).Children[0];
            DoubleAnimation a = new DoubleAnimation(to, TimeSpan.FromMilliseconds(ms));
            a.EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut };
            sc.BeginAnimation(ScaleTransform.ScaleXProperty, a);
            DoubleAnimation b = new DoubleAnimation(to, TimeSpan.FromMilliseconds(ms));
            b.EasingFunction = a.EasingFunction;
            sc.BeginAnimation(ScaleTransform.ScaleYProperty, b);
        }

        // ---------- 墨历（三十日墨痕）+ 交互五色图例 ----------
        UIElement Calendar()
        {
            Grid g = new Grid();
            g.RowDefinitions.Add(RowDef(62));
            g.RowDefinitions.Add(RowDef(30));

            Grid top = new Grid();
            top.ColumnDefinitions.Add(new ColumnDefinition());
            top.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(94) });

            Canvas bars = new Canvas();
            bars.Height = 54;
            bars.Width = 330;
            bars.HorizontalAlignment = HorizontalAlignment.Left;
            bars.VerticalAlignment = VerticalAlignment.Bottom;

            int[] cnt = new int[30];
            int totalSaves = 0;
            try
            {
                string dir = System.IO.Path.Combine(root, "layouts");
                if (Directory.Exists(dir))
                {
                    string[] fs = Directory.GetFiles(dir, "layout_*.json");
                    for (int i = 0; i < fs.Length; i++)
                    {
                        string nm = System.IO.Path.GetFileName(fs[i]);
                        if (nm.Length < 15) continue;
                        try
                        {
                            string ymd = nm.Substring(7, 8);
                            DateTime dt = DateTime.ParseExact(ymd, "yyyyMMdd", System.Globalization.CultureInfo.InvariantCulture);
                            int back = (int)(DateTime.Today - dt.Date).TotalDays;
                            if (back >= 0 && back < 30) { cnt[29 - back]++; totalSaves++; }
                        }
                        catch { }
                    }
                }
            }
            catch { }

            int mx = 2;
            for (int i = 0; i < 30; i++) if (cnt[i] > mx) mx = cnt[i];

            double step = 10.6;
            double totalW = 29 * step;
            double offset = 8;
            double baseY = 48;

            // 手绘水平时间基线：替代空白悬浮感
            Rough.Pen(bars, offset - 4, baseY, offset + totalW + 4, baseY,
                      Ink.Fg3(), 0.85, 0.22, 0.35, 0.25, 8100, null);

            // 三十日墨痕：无记录日画极淡短刻度（逢七略长出韵律）；有记录日画手绘竖笔墨痕
            for (int i = 0; i < 30; i++)
            {
                double x = offset + i * step;
                bool isToday = (i == 29);
                bool isWeek = ((29 - i) % 7 == 0);
                DateTime day = DateTime.Today.AddDays(-(29 - i));

                if (cnt[i] <= 0)
                {
                    double th = isWeek ? 4.5 : 2.5;
                    System.Windows.Shapes.Line tk = new System.Windows.Shapes.Line();
                    tk.X1 = x; tk.Y1 = baseY;
                    tk.X2 = x; tk.Y2 = baseY - th;
                    tk.Stroke = Ink.Br(isToday ? Ink.Verm() : Ink.Fg3());
                    tk.StrokeThickness = isWeek ? 1.1 : 0.9;
                    tk.Opacity = isToday ? 0.55 : (isWeek ? 0.28 : 0.16);
                    bars.Children.Add(tk);
                }
                else
                {
                    double h = 10.0 + 30.0 * cnt[i] / mx;
                    Color strokeCol = isToday ? Ink.Verm() : Ink.Fg();
                    double strokeOp = isToday ? 0.86 : 0.62;
                    Rough.Pen(bars, x, baseY - 1, x, baseY - h,
                              strokeCol, 2.5, strokeOp, 0.55, 0.38, 8200 + i * 29, null);

                    // 透明热区：悬停显示当日日期与落笔次数，点击打开回溯抽屉
                    Border hit = new Border();
                    hit.Width = 9;
                    hit.Height = h + 6;
                    hit.Background = Brushes.Transparent;
                    hit.Cursor = Cursors.Hand;
                    hit.ToolTip = day.ToString("MM.dd") + " · 落笔 " + cnt[i] + " 次（点击回溯）";
                    ToolTipService.SetInitialShowDelay(hit, 120);
                    hit.MouseLeftButtonUp += delegate { OpenHistory(); };
                    Canvas.SetLeft(hit, x - 4.5);
                    Canvas.SetTop(hit, baseY - h - 4);
                    bars.Children.Add(hit);
                }
            }
            // 分辨率自适应推荐阵型条（置于三十日墨历上方留白处；一键预演进草稿态，绝不直接改动真实桌面）
            StackPanel presetRow = new StackPanel();
            presetRow.Orientation = Orientation.Horizontal;
            presetRow.VerticalAlignment = VerticalAlignment.Top;
            presetRow.HorizontalAlignment = HorizontalAlignment.Left;
            presetRow.Margin = new Thickness(4, 1, 0, 0);

            TextBlock presetLbl = new TextBlock();
            presetLbl.Text = ResolutionTag() + " 分区阵型";
            presetLbl.FontSize = 8.5;
            presetLbl.Foreground = Ink.Br(Ink.Fg3());
            presetLbl.VerticalAlignment = VerticalAlignment.Center;
            presetLbl.Margin = new Thickness(0, 0, 6, 0);
            presetRow.Children.Add(presetLbl);

            string[] pKeys = { "work_zone", "quad_zone", "left_zone" };
            string[] pNames = { "效率分治", "四隅六区", "左翼精排" };
            string[] pTips =
            {
                "内置语义分区：左侧系统/办公/通讯（微信QQ等），右侧归档与影音游戏，中央留白",
                "内置语义分区：左上系统办公、左下通讯、右上资料归档、右下影音与游戏，最适配 2K/4K 屏",
                "内置语义分区：按系统→通讯→办公→影音→游戏→归档六大分区齐整排于左翼"
            };
            for (int pi = 0; pi < pKeys.Length; pi++)
            {
                string pk = pKeys[pi];
                Border pChip = new Border();
                pChip.CornerRadius = new CornerRadius(5);
                pChip.Padding = new Thickness(6, 2, 6, 2);
                pChip.Margin = new Thickness(0, 0, 5, 0);
                pChip.Background = Ink.Br(Ink.A(Ink.Dark ? (byte)0x16 : (byte)0x10, Ink.Fg().R, Ink.Fg().G, Ink.Fg().B));
                pChip.BorderBrush = Ink.Br(Ink.Hair());
                pChip.BorderThickness = new Thickness(0.8);
                pChip.Cursor = Cursors.Hand;
                pChip.ToolTip = pTips[pi] + "（仅在棋盘预演，点「应用布阵」才生效）";
                ToolTipService.SetInitialShowDelay(pChip, 150);

                TextBlock pTx = new TextBlock();
                pTx.Text = pNames[pi];
                pTx.FontSize = 8.5;
                pTx.Foreground = Ink.Br(Ink.Fg2());
                pChip.Child = pTx;

                pChip.MouseEnter += delegate
                {
                    pChip.BorderBrush = Ink.Br(Ink.Verm());
                    pTx.Foreground = Ink.Br(Ink.Verm());
                };
                pChip.MouseLeave += delegate
                {
                    pChip.BorderBrush = Ink.Br(Ink.Hair());
                    pTx.Foreground = Ink.Br(Ink.Fg2());
                };
                pChip.MouseLeftButtonUp += delegate { ApplyLayoutPreset(pk); };
                presetRow.Children.Add(pChip);
            }
            Grid.SetColumn(presetRow, 0);
            top.Children.Add(presetRow);

            Grid.SetColumn(bars, 0);
            top.Children.Add(bars);

            tCalDate = new TextBlock();
            tCalDate.FontFamily = Ink.Mono;
            tCalDate.FontSize = 13;
            tCalDate.HorizontalAlignment = HorizontalAlignment.Right;
            tCalDate.Foreground = Ink.Br(Ink.Fg2());

            tCalMeta = new TextBlock();
            tCalMeta.FontFamily = Ink.Sans;
            tCalMeta.FontSize = 8.5;
            tCalMeta.HorizontalAlignment = HorizontalAlignment.Right;
            tCalMeta.Margin = new Thickness(0, 2, 0, 0);
            tCalMeta.Foreground = Ink.Br(Ink.Fg3());
            tCalMeta.Text = totalSaves > 0 ? "近30日 · " + totalSaves + " 次" : "尚无落笔";

            StackPanel right = new StackPanel();
            right.VerticalAlignment = VerticalAlignment.Bottom;
            right.HorizontalAlignment = HorizontalAlignment.Right;
            right.Margin = new Thickness(0, 0, 2, 4);
            right.Children.Add(tCalDate);
            right.Children.Add(tCalMeta);

            Grid.SetColumn(right, 1);
            top.Children.Add(right);

            Grid.SetRow(top, 0);
            g.Children.Add(top);

            // 交互图例：六色古矿彩（含压缩包紫矿），悬停可联动高亮棋盘同类棋子
            legendCounts.Clear();
            legendDots.Clear();
            StackPanel leg = new StackPanel();
            leg.Orientation = Orientation.Horizontal;
            leg.VerticalAlignment = VerticalAlignment.Center;
            string[] ts = { "app", "dir", "arc", "doc", "sys", "other" };
            for (int i = 0; i < ts.Length; i++)
            {
                string ty = ts[i];
                Border chip = new Border();
                chip.Background = Brushes.Transparent;
                chip.Padding = new Thickness(1, 3, 3, 3);
                chip.Margin = new Thickness(0, 0, 7, 0);
                chip.Cursor = Cursors.Hand;

                StackPanel it = new StackPanel();
                it.Orientation = Orientation.Horizontal;

                Ellipse dot = new Ellipse();
                dot.Width = 5.5;
                dot.Height = 5.5;
                dot.Fill = Ink.Br(Ink.ByType(ty));
                dot.VerticalAlignment = VerticalAlignment.Center;
                it.Children.Add(dot);
                legendDots[ty] = dot;

                TextBlock tx = new TextBlock();
                tx.Text = TypeLabel(ty);
                tx.FontSize = 9;
                tx.Margin = new Thickness(4.5, 0, 0, 0);
                tx.VerticalAlignment = VerticalAlignment.Center;
                tx.Foreground = Ink.Br(Ink.Fg3());
                it.Children.Add(tx);

                TextBlock cntTx = new TextBlock();
                cntTx.FontFamily = Ink.Mono;
                cntTx.FontSize = 8.5;
                cntTx.VerticalAlignment = VerticalAlignment.Center;
                cntTx.Foreground = Ink.Br(Ink.Fg3());
                cntTx.Opacity = 0.72;
                it.Children.Add(cntTx);
                legendCounts[ty] = cntTx;

                chip.Child = it;
                chip.MouseEnter += delegate
                {
                    tx.Foreground = Ink.Br(Ink.Fg());
                    FilterDotsByType(ty);
                };
                chip.MouseLeave += delegate
                {
                    tx.Foreground = Ink.Br(Ink.Fg3());
                    FilterDotsByType(null);
                };
                leg.Children.Add(chip);
            }
            Grid.SetRow(leg, 1);
            g.Children.Add(leg);
            return g;
        }

        string ResolutionTag()
        {
            int sw = (snap != null && snap.ScreenW > 400) ? snap.ScreenW : (int)Math.Round(SystemParameters.PrimaryScreenWidth * GetDpiScale());
            int sh = (snap != null && snap.ScreenH > 300) ? snap.ScreenH : (int)Math.Round(SystemParameters.PrimaryScreenHeight * GetDpiScale());
            if (sh > 0 && (double)sw / sh >= 2.05) return "带鱼屏";
            if (sw >= 3600) return "4K";
            if (sw >= 2400) return "2K";
            return "1080p";
        }

        // ---------- 内置桌面语义分区引擎（系统核心 / 通讯社交 / 办公创作 / 影音媒体 / 游戏娱乐 / 资料归档） ----------
        static bool MatchAny(string lower, string[] kws)
        {
            for (int i = 0; i < kws.Length; i++)
                if (lower.IndexOf(kws[i], StringComparison.OrdinalIgnoreCase) >= 0) return true;
            return false;
        }

        static string DetectZone(string name, string fileType)
        {
            if (fileType == "sys") return "core";
            string s = (name ?? "").ToLowerInvariant();
            if (MatchAny(s, new string[] { "此电脑", "回收站", "控制面板", "网络", "计算机", "this pc", "recycle" }))
                return "core";
            if (MatchAny(s, new string[] { "微信", "wechat", "qq", "tim", "钉钉", "dingtalk", "飞书", "feishu", "lark", "企业微信", "wecom", "telegram", "discord", "slack", "zoom", "腾讯会议", "wemeet", "teams", "skype", "outlook", "foxmail", "mail", "邮件", "whatsapp" }))
                return "comm";
            if (MatchAny(s, new string[] { "steam", "epic", "wegame", "原神", "genshin", "崩坏", "星穹", "鸣潮", "英雄联盟", "lol", "league", "无畏契约", "valorant", "csgo", "cs2", "minecraft", "battle", "暴雪", "riot", "ubisoft", "origin", "xbox", "雷电", "mumu", "夜神", "加速器", "uu", "迅游", "roblox", "游戏", "game" }))
                return "game";
            if (MatchAny(s, new string[] { "网易云", "cloudmusic", "qq音乐", "qqmusic", "酷狗", "kugou", "酷我", "spotify", "music", "哔哩哔哩", "bilibili", "爱奇艺", "iqiyi", "腾讯视频", "qqvideo", "优酷", "youku", "芒果", "抖音", "douyin", "potplayer", "vlc", "mpv", "剪映", "jianying", "capcut", "obs", "迅雷", "thunder", "百度网盘", "baidunetdisk", "夸克", "quark", "阿里云盘", "chrome", "edge", "firefox", "safari", "浏览器", "音乐", "视频", "直播" }))
                return "media";
            if (fileType == "doc" || MatchAny(s, new string[] { "word", "excel", "powerpoint", "ppt", "wps", "pdf", "notion", "obsidian", "typora", "onenote", "xmind", "vscode", "visual studio", "code", "cursor", "pycharm", "idea", "webstorm", "git", "photoshop", "illustrator", "premiere", "figma", "blender", "autocad", "cad", "arcgis", "qgis", "matlab", "python", "anaconda", "postman", "navicat", "docker", "vmware", "terminal", "笔记", "作业", "论文", "报告", "合同", "简历", "表格", "计划", "项目", "办公" }))
                return "work";
            if (fileType == "dir" || fileType == "arc") return "vault";
            return "work";
        }

        static string ZoneLabel(string z)
        {
            if (z == "core") return "系统";
            if (z == "comm") return "通讯";
            if (z == "work") return "办公";
            if (z == "media") return "影音";
            if (z == "game") return "游戏";
            return "归档";
        }

        // 按分辨率网格 + 内置语义分区引擎生成推荐布局（进入草稿态预演，支持一键 Ctrl+Z 撤回，确认后才动桌面）
        void ApplyLayoutPreset(string scheme)
        {
            if (busy || dots.Count == 0 || shown.Count == 0) return;
            if (historyOverlay != null && historyOverlay.Visibility == Visibility.Visible) CloseHistory(true);
            EnsureDraftInitialized();

            List<int> oldC = new List<int>(dotGridC);
            List<int> oldR = new List<int>(dotGridR);

            Dictionary<string, int> zOrder = new Dictionary<string, int>();
            zOrder["core"] = 0; zOrder["comm"] = 1; zOrder["work"] = 2;
            zOrder["media"] = 3; zOrder["game"] = 4; zOrder["vault"] = 5;

            Dictionary<string, int> zCounts = new Dictionary<string, int>();
            string[] dotZone = new string[dots.Count];
            List<int> active = new List<int>();
            for (int i = 0; i < dots.Count; i++)
            {
                if (absorbedIndices.Contains(i)) continue;
                string ty = i < dotTypes.Count ? dotTypes[i] : "other";
                string z = DetectZone(shown[i].Name, ty);
                dotZone[i] = z;
                if (!zCounts.ContainsKey(z)) zCounts[z] = 0;
                zCounts[z]++;
                active.Add(i);
            }

            active.Sort(delegate (int a, int b)
            {
                int oa = zOrder.ContainsKey(dotZone[a]) ? zOrder[dotZone[a]] : 5;
                int ob = zOrder.ContainsKey(dotZone[b]) ? zOrder[dotZone[b]] : 5;
                if (oa != ob) return oa.CompareTo(ob);
                return string.Compare(shown[a].Name, shown[b].Name, StringComparison.OrdinalIgnoreCase);
            });

            int cMin = boardCi0, cMax = boardCi1, rMin = boardRi0, rMax = boardRi1;
            int totalCols = Math.Max(1, cMax - cMin + 1);
            int totalRows = Math.Max(1, rMax - rMin + 1);
            HashSet<string> used = new HashSet<string>();

            Action<int, int, int> place = delegate (int idx, int c, int r)
            {
                used.Add(c + ":" + r);
                dotGridC[idx] = c;
                dotGridR[idx] = r;
                shown[idx].X = (int)Math.Round(boardGx0 + c * boardGdx);
                shown[idx].Y = (int)Math.Round(boardGy0 + r * boardGdy);
                AnimateDotSnap(dots[idx], CellToX(c), CellToY(r));
            };

            Func<int, int, int, int, Point> nextFree = delegate (int startC, int dirC, int startR, int dirR)
            {
                int c = startC;
                int r = startR;
                for (int guard = 0; guard < totalCols * totalRows + 20; guard++)
                {
                    if (c >= cMin && c <= cMax && r >= rMin && r <= rMax && !used.Contains(c + ":" + r))
                        return new Point(c, r);
                    r += dirR;
                    if (r > rMax || r < rMin)
                    {
                        r = startR;
                        c += dirC;
                        if (c > cMax) c = cMin;
                        if (c < cMin) c = cMax;
                    }
                }
                return new Point(cMin, rMin);
            };

            if (scheme == "left_zone")
            {
                // 左翼精排：严格按 系统→通讯→办公→影音→游戏→归档 语义分区从左向右排列
                for (int i = 0; i < active.Count; i++)
                {
                    Point p = nextFree(cMin, 1, rMin, 1);
                    place(active[i], (int)p.X, (int)p.Y);
                }
            }
            else if (scheme == "work_zone")
            {
                // 效率分治：左翼（系统+办公+通讯），右翼上（资料归档），右翼下（影音+游戏），中间通透留白
                for (int i = 0; i < active.Count; i++)
                {
                    int idx = active[i];
                    string z = dotZone[idx];
                    Point p;
                    if (z == "core" || z == "work" || z == "comm") p = nextFree(cMin, 1, rMin, 1);
                    else if (z == "vault") p = nextFree(cMax, -1, rMin, 1);
                    else p = nextFree(cMax, -1, rMax, -1);
                    place(idx, (int)p.X, (int)p.Y);
                }
            }
            else
            {
                // 四隅六区：左上（系统+办公）、左下（通讯：微信/QQ等）、右上（归档）、右下（影音+游戏）
                for (int i = 0; i < active.Count; i++)
                {
                    int idx = active[i];
                    string z = dotZone[idx];
                    Point p;
                    if (z == "core" || z == "work") p = nextFree(cMin, 1, rMin, 1);
                    else if (z == "comm") p = nextFree(cMin, 1, rMax, -1);
                    else if (z == "vault") p = nextFree(cMax, -1, rMin, 1);
                    else p = nextFree(cMax, -1, rMax, -1);
                    place(idx, (int)p.X, (int)p.Y);
                }
            }

            DraftOp batch = new DraftOp { IsBatch = true, BatchOldC = oldC, BatchOldR = oldR };
            draftStack.Push(batch);
            UpdateDraftButtons();
            string sName = scheme == "left_zone" ? "左翼精排" : (scheme == "work_zone" ? "效率分治" : "四隅六区");
            List<string> zSummary = new List<string>();
            string[] zKeys = { "comm", "work", "media", "game", "vault" };
            for (int i = 0; i < zKeys.Length; i++)
                if (zCounts.ContainsKey(zKeys[i]) && zCounts[zKeys[i]] > 0)
                    zSummary.Add(ZoneLabel(zKeys[i]) + zCounts[zKeys[i]]);
            if (tStatus != null)
                tStatus.Text = ResolutionTag() + " · " + sName + "（" + string.Join(" / ", zSummary.ToArray()) + "）· 点「应用布阵」生效，Ctrl+Z 撤回";
        }

        // ---------- 按钮 ----------
        UIElement ActionBar()
        {
            Grid g = new Grid();
            g.VerticalAlignment = VerticalAlignment.Center;
            g.ColumnDefinitions.Add(new ColumnDefinition());
            g.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(12) });
            g.ColumnDefinitions.Add(new ColumnDefinition());

            btnSave = new InkButton("落子成记", true);
            btnSave.ToolTip = "记录当前桌面图标位置；拖拽棋子后变为「应用布阵」(Ctrl+S)";
            btnSave.MouseLeftButtonUp += delegate { OnPrimaryBtn(); };
            Grid.SetColumn(btnSave, 0);
            g.Children.Add(btnSave);

            btnRestore = new InkButton("归位", false);
            btnRestore.ToolTip = "恢复已保存的布局；拖拽棋子后变为「撤销布阵」(Ctrl+R)";
            btnRestore.MouseLeftButtonUp += delegate { OnSecondaryBtn(); };
            Grid.SetColumn(btnRestore, 2);
            g.Children.Add(btnRestore);

            return g;
        }

        // ---------- 底部一行 ----------
        UIElement StatusLine()
        {
            Grid g = new Grid();
            g.Margin = new Thickness(0, 4, 0, 0);
            g.ColumnDefinitions.Add(new ColumnDefinition());
            g.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            TextBlock tip = new TextBlock();
            tStatus = tip;
            tip.Text = "只动坐标 · 不动文件";
            tip.FontSize = 9;
            tip.VerticalAlignment = VerticalAlignment.Center;
            tip.TextTrimming = TextTrimming.CharacterEllipsis;
            tip.Foreground = Ink.Br(Ink.Fg3());
            g.Children.Add(tip);

            StackPanel tiny = new StackPanel();
            tiny.Orientation = Orientation.Horizontal;
            tiny.HorizontalAlignment = HorizontalAlignment.Right;
            tiny.VerticalAlignment = VerticalAlignment.Center;

            bottomPips.Clear();
            tiny.Children.Add(LinkCell("回溯", "查看历史快照并实时预览回溯", delegate { OpenHistory(); }));
            tiny.Children.Add(LinkCell("目录", "查看/更改存储路径、安装位置与内存状态", delegate { OpenDir(); }));
            tiny.Children.Add(LinkCell(Ink.Dark ? "宣纸" : "夜墨", "切换宣纸 / 夜墨材质", delegate { TogglePaper(); }));

            Grid.SetColumn(tiny, 1);
            g.Children.Add(tiny);
            return g;
        }

        // 落款式小链接：朱砂小点微胀 + 颜色缓动过渡 + 按压微缩
        UIElement LinkCell(string text, string tip, Action act)
        {
            StackPanel it = new StackPanel();
            it.Orientation = Orientation.Horizontal;
            it.Margin = new Thickness(13, 0, 0, 0);
            it.Cursor = Cursors.Hand;
            it.ToolTip = tip;
            it.RenderTransformOrigin = new Point(0.5, 0.5);
            ScaleTransform itScale = new ScaleTransform(1, 1);
            it.RenderTransform = itScale;

            Color restPip = Ink.A(0x88, Ink.Verm().R, Ink.Verm().G, Ink.Verm().B);
            SolidColorBrush pipBr = new SolidColorBrush(restPip);
            SolidColorBrush txtBr = new SolidColorBrush(Ink.Fg3());

            Ellipse pip = new Ellipse();
            pip.Width = 3.2;
            pip.Height = 3.2;
            pip.Fill = pipBr;
            pip.VerticalAlignment = VerticalAlignment.Center;
            pip.RenderTransformOrigin = new Point(0.5, 0.5);
            ScaleTransform pipScale = new ScaleTransform(1, 1);
            pip.RenderTransform = pipScale;
            it.Children.Add(pip);
            bottomPips.Add(pip);

            TextBlock t = new TextBlock();
            t.Text = text;
            t.FontSize = 9.5;
            t.Margin = new Thickness(4.5, 0, 0, 0);
            t.VerticalAlignment = VerticalAlignment.Center;
            t.Foreground = txtBr;
            it.Children.Add(t);

            it.MouseEnter += delegate
            {
                ColorAnimation ca1 = new ColorAnimation(Ink.Verm(), TimeSpan.FromMilliseconds(160));
                ColorAnimation ca2 = new ColorAnimation(Ink.Verm(), TimeSpan.FromMilliseconds(160));
                txtBr.BeginAnimation(SolidColorBrush.ColorProperty, ca1);
                pipBr.BeginAnimation(SolidColorBrush.ColorProperty, ca2);
                ScaleDown(pipScale, 1.45, 160);
            };
            it.MouseLeave += delegate
            {
                ColorAnimation ca1 = new ColorAnimation(Ink.Fg3(), TimeSpan.FromMilliseconds(180));
                ColorAnimation ca2 = new ColorAnimation(restPip, TimeSpan.FromMilliseconds(180));
                txtBr.BeginAnimation(SolidColorBrush.ColorProperty, ca1);
                pipBr.BeginAnimation(SolidColorBrush.ColorProperty, ca2);
                ScaleDown(pipScale, 1.0, 180);
                ScaleDown(itScale, 1.0, 140);
            };
            it.MouseLeftButtonDown += delegate { ScaleDown(itScale, 0.93, 75); };
            it.MouseLeftButtonUp += delegate
            {
                ScaleDown(itScale, 1.0, 150);
                act();
            };
            return it;
        }

        // ---------- 历史回溯与存址抽屉（悬于底部，保持上方棋盘实时可见预览） ----------
        UIElement HistoryDrawer()
        {
            historyOverlay = new Border();
            historyOverlay.VerticalAlignment = VerticalAlignment.Bottom;
            historyOverlay.Height = 186;
            historyOverlay.Margin = new Thickness(12, 0, 12, 10);
            historyOverlay.CornerRadius = new CornerRadius(14);
            historyOverlay.Background = Ink.Br(Ink.Card());
            historyOverlay.BorderBrush = Ink.Br(Ink.Hair());
            historyOverlay.BorderThickness = new Thickness(1);
            historyOverlay.Padding = new Thickness(14, 10, 14, 10);
            historyOverlay.Visibility = Visibility.Collapsed;
            historyOverlay.Opacity = 0;
            Panel.SetZIndex(historyOverlay, 70);

            Grid g = new Grid();
            g.RowDefinitions.Add(RowDef(24));
            g.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            g.RowDefinitions.Add(RowDef(20));
            g.RowDefinitions.Add(RowDef(30));

            // 顶行：标题 + 关闭
            Grid top = new Grid();
            TextBlock title = new TextBlock();
            title.Text = "历史回溯 · 点击任意节点预览棋局";
            title.FontFamily = Ink.Serif;
            title.FontSize = 11.5;
            title.VerticalAlignment = VerticalAlignment.Center;
            title.Foreground = Ink.Br(Ink.Fg());
            top.Children.Add(title);

            TextBlock closeTx = new TextBlock();
            closeTx.Text = "收起 (Esc)";
            closeTx.FontSize = 9.5;
            closeTx.HorizontalAlignment = HorizontalAlignment.Right;
            closeTx.VerticalAlignment = VerticalAlignment.Center;
            closeTx.Foreground = Ink.Br(Ink.Fg3());
            closeTx.Cursor = Cursors.Hand;
            closeTx.MouseEnter += delegate { closeTx.Foreground = Ink.Br(Ink.Verm()); };
            closeTx.MouseLeave += delegate { closeTx.Foreground = Ink.Br(Ink.Fg3()); };
            closeTx.MouseLeftButtonUp += delegate { CloseHistory(true); };
            top.Children.Add(closeTx);
            Grid.SetRow(top, 0);
            g.Children.Add(top);

            // 中部：可滚轮滚动历史快照列表（隐藏生硬的系统滚动条箭头）
            ScrollViewer sv = new ScrollViewer();
            sv.VerticalScrollBarVisibility = ScrollBarVisibility.Hidden;
            sv.HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled;
            sv.Margin = new Thickness(0, 4, 0, 4);
            historyList = new StackPanel();
            historyList.Orientation = Orientation.Vertical;
            sv.Content = historyList;
            Grid.SetRow(sv, 1);
            g.Children.Add(sv);

            // 存址记忆行
            Grid pathRow = new Grid();
            pathRow.ColumnDefinitions.Add(new ColumnDefinition());
            pathRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            historyPathTxt = new TextBlock();
            historyPathTxt.FontFamily = Ink.Mono;
            historyPathTxt.FontSize = 8.5;
            historyPathTxt.VerticalAlignment = VerticalAlignment.Center;
            historyPathTxt.TextTrimming = TextTrimming.CharacterEllipsis;
            historyPathTxt.Foreground = Ink.Br(Ink.Fg3());
            pathRow.Children.Add(historyPathTxt);

            TextBlock chgBtn = new TextBlock();
            chgBtn.Text = "更改存址";
            chgBtn.FontSize = 9;
            chgBtn.Margin = new Thickness(10, 0, 0, 0);
            chgBtn.VerticalAlignment = VerticalAlignment.Center;
            chgBtn.Cursor = Cursors.Hand;
            chgBtn.Foreground = Ink.Br(Ink.Verm());
            chgBtn.MouseLeftButtonUp += delegate { ChangeStorageDir(); };
            Grid.SetColumn(chgBtn, 1);
            pathRow.Children.Add(chgBtn);

            Grid.SetRow(pathRow, 2);
            g.Children.Add(pathRow);

            // 底行：设为此刻记忆 + 返回现状
            Grid act = new Grid();
            act.Margin = new Thickness(0, 4, 0, 0);
            act.ColumnDefinitions.Add(new ColumnDefinition());
            act.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(8) });
            act.ColumnDefinitions.Add(new ColumnDefinition());

            Border btnApply = MiniDrawerBtn("设为此刻记忆", true, delegate { ApplyHistoryRollback(); });
            Grid.SetColumn(btnApply, 0);
            act.Children.Add(btnApply);

            Border btnBack = MiniDrawerBtn("返回现状", false, delegate { CloseHistory(true); });
            Grid.SetColumn(btnBack, 2);
            act.Children.Add(btnBack);

            Grid.SetRow(act, 3);
            g.Children.Add(act);

            historyOverlay.Child = g;
            return historyOverlay;
        }

        Border MiniDrawerBtn(string text, bool isPrimary, Action onClick)
        {
            Border b = new Border();
            b.Height = 26;
            b.CornerRadius = new CornerRadius(7);
            b.Cursor = Cursors.Hand;
            b.BorderThickness = new Thickness(1);
            b.Background = Ink.Br(isPrimary ? Ink.PrimaryBg() : Brushes.Transparent.Color);
            b.BorderBrush = Ink.Br(isPrimary ? Ink.PrimaryBorder() : Ink.Hair());

            TextBlock tx = new TextBlock();
            tx.Text = text;
            tx.FontFamily = Ink.Serif;
            tx.FontSize = 11.5;
            tx.HorizontalAlignment = HorizontalAlignment.Center;
            tx.VerticalAlignment = VerticalAlignment.Center;
            tx.Foreground = Ink.Br(isPrimary ? Ink.PrimaryFg() : Ink.Fg());
            b.Child = tx;

            b.MouseEnter += delegate { b.BorderBrush = Ink.Br(Ink.Verm()); };
            b.MouseLeave += delegate { b.BorderBrush = Ink.Br(isPrimary ? Ink.PrimaryBorder() : Ink.Hair()); };
            b.MouseLeftButtonUp += delegate { onClick(); };
            return b;
        }

        void OpenHistory()
        {
            if (busy || historyOverlay == null) return;
            if (dirOverlay != null && dirOverlay.Visibility == Visibility.Visible) CloseDir();
            DisarmAll();
            previewSnap = null;
            previewPath = null;
            PopulateHistory();

            historyOverlay.Visibility = Visibility.Visible;
            DoubleAnimation a = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(200));
            a.EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut };
            historyOverlay.BeginAnimation(UIElement.OpacityProperty, a);
        }

        void CloseHistory(bool restoreCurrent)
        {
            if (historyOverlay == null || historyOverlay.Visibility != Visibility.Visible) return;
            previewSnap = null;
            previewPath = null;
            if (restoreCurrent)
            {
                RefreshMeta();
                DrawBoard();
                if (tStatus != null) tStatus.Text = "只动坐标 · 不动文件";
            }
            DoubleAnimation a = new DoubleAnimation(historyOverlay.Opacity, 0, TimeSpan.FromMilliseconds(160));
            a.EasingFunction = new CubicEase { EasingMode = EasingMode.EaseIn };
            a.Completed += delegate
            {
                if (historyOverlay != null) historyOverlay.Visibility = Visibility.Collapsed;
                if (historyList != null) historyList.Children.Clear();
                TrimMemory();
            };
            historyOverlay.BeginAnimation(UIElement.OpacityProperty, a);
        }

        // ---------- 目录与存址抽屉（支持中英文/空格路径、自定义存址、查看程序位置与内存占用） ----------
        UIElement DirDrawer()
        {
            dirOverlay = new Border();
            dirOverlay.VerticalAlignment = VerticalAlignment.Bottom;
            dirOverlay.Height = 186;
            dirOverlay.Margin = new Thickness(12, 0, 12, 10);
            dirOverlay.CornerRadius = new CornerRadius(14);
            dirOverlay.Background = Ink.Br(Ink.Card());
            dirOverlay.BorderBrush = Ink.Br(Ink.Hair());
            dirOverlay.BorderThickness = new Thickness(1);
            dirOverlay.Padding = new Thickness(14, 10, 14, 10);
            dirOverlay.Visibility = Visibility.Collapsed;
            dirOverlay.Opacity = 0;
            Panel.SetZIndex(dirOverlay, 72);

            Grid g = new Grid();
            g.RowDefinitions.Add(RowDef(24));
            g.RowDefinitions.Add(RowDef(42));
            g.RowDefinitions.Add(RowDef(36));
            g.RowDefinitions.Add(RowDef(30));
            g.RowDefinitions.Add(RowDef(32));

            // 顶行
            Grid top = new Grid();
            TextBlock title = new TextBlock();
            title.Text = "目录与存址 · 兼容任意中英文路径";
            title.FontFamily = Ink.Serif;
            title.FontSize = 11.5;
            title.VerticalAlignment = VerticalAlignment.Center;
            title.Foreground = Ink.Br(Ink.Fg());
            top.Children.Add(title);

            TextBlock closeTx = new TextBlock();
            closeTx.Text = "收起 (Esc)";
            closeTx.FontSize = 9.5;
            closeTx.HorizontalAlignment = HorizontalAlignment.Right;
            closeTx.VerticalAlignment = VerticalAlignment.Center;
            closeTx.Foreground = Ink.Br(Ink.Fg3());
            closeTx.Cursor = Cursors.Hand;
            closeTx.MouseEnter += delegate { closeTx.Foreground = Ink.Br(Ink.Verm()); };
            closeTx.MouseLeave += delegate { closeTx.Foreground = Ink.Br(Ink.Fg3()); };
            closeTx.MouseLeftButtonUp += delegate { CloseDir(); };
            top.Children.Add(closeTx);
            Grid.SetRow(top, 0);
            g.Children.Add(top);

            // 行 1：数据存储目录
            Grid rowData = new Grid();
            rowData.Margin = new Thickness(0, 4, 0, 0);
            rowData.ColumnDefinitions.Add(new ColumnDefinition());
            rowData.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            StackPanel dataSp = new StackPanel();
            dataSp.VerticalAlignment = VerticalAlignment.Center;
            TextBlock dataLbl = new TextBlock();
            dataLbl.Text = "快照与历史存址";
            dataLbl.FontSize = 9;
            dataLbl.Foreground = Ink.Br(Ink.Fg3());
            dataSp.Children.Add(dataLbl);

            dirDataTxt = new TextBlock();
            dirDataTxt.FontFamily = Ink.Mono;
            dirDataTxt.FontSize = 9.5;
            dirDataTxt.Margin = new Thickness(0, 2, 8, 0);
            dirDataTxt.TextTrimming = TextTrimming.CharacterEllipsis;
            dirDataTxt.Foreground = Ink.Br(Ink.Fg2());
            dataSp.Children.Add(dirDataTxt);
            rowData.Children.Add(dataSp);

            StackPanel dataBtns = new StackPanel();
            dataBtns.Orientation = Orientation.Horizontal;
            dataBtns.VerticalAlignment = VerticalAlignment.Center;
            dataBtns.Children.Add(DirActionLink("打开", delegate { DoOpenFolder(root); }));
            dataBtns.Children.Add(DirActionLink("更改存址", delegate { ChangeStorageDir(); RefreshDirInfo(); }));
            Grid.SetColumn(dataBtns, 1);
            rowData.Children.Add(dataBtns);
            Grid.SetRow(rowData, 1);
            g.Children.Add(rowData);

            // 行 2：程序安装位置
            Grid rowExe = new Grid();
            rowExe.ColumnDefinitions.Add(new ColumnDefinition());
            rowExe.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            StackPanel exeSp = new StackPanel();
            exeSp.VerticalAlignment = VerticalAlignment.Center;
            TextBlock exeLbl = new TextBlock();
            exeLbl.Text = "程序所在目录";
            exeLbl.FontSize = 9;
            exeLbl.Foreground = Ink.Br(Ink.Fg3());
            exeSp.Children.Add(exeLbl);

            dirExeTxt = new TextBlock();
            dirExeTxt.FontFamily = Ink.Mono;
            dirExeTxt.FontSize = 9;
            dirExeTxt.Margin = new Thickness(0, 1, 8, 0);
            dirExeTxt.TextTrimming = TextTrimming.CharacterEllipsis;
            dirExeTxt.Foreground = Ink.Br(Ink.Fg3());
            exeSp.Children.Add(dirExeTxt);
            rowExe.Children.Add(exeSp);

            UIElement openExeBtn = DirActionLink("打开", delegate
            {
                string ex = AppDomain.CurrentDomain.BaseDirectory.TrimEnd('\\');
                DoOpenFolder(ex);
            });
            Grid.SetColumn(openExeBtn, 1);
            rowExe.Children.Add(openExeBtn);
            Grid.SetRow(rowExe, 2);
            g.Children.Add(rowExe);

            // 行 3：内存状态与释放
            Grid rowMem = new Grid();
            rowMem.ColumnDefinitions.Add(new ColumnDefinition());
            rowMem.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            dirMemTxt = new TextBlock();
            dirMemTxt.FontFamily = Ink.Mono;
            dirMemTxt.FontSize = 9;
            dirMemTxt.VerticalAlignment = VerticalAlignment.Center;
            dirMemTxt.Foreground = Ink.Br(Ink.Fg3());
            rowMem.Children.Add(dirMemTxt);

            UIElement trimBtn = DirActionLink("释放内存", delegate
            {
                TrimMemory();
                RefreshDirInfo();
                if (tStatus != null) tStatus.Text = "已整理内存 · 当前 " + GetWorkingSetMB().ToString("0.0") + " MB";
            });
            Grid.SetColumn(trimBtn, 1);
            rowMem.Children.Add(trimBtn);
            Grid.SetRow(rowMem, 3);
            g.Children.Add(rowMem);

            // 行 4：快捷说明 + 关闭按钮
            Grid rowFoot = new Grid();
            rowFoot.Margin = new Thickness(0, 2, 0, 0);
            rowFoot.ColumnDefinitions.Add(new ColumnDefinition());
            rowFoot.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(86) });

            TextBlock tips = new TextBlock();
            tips.Text = "Ctrl+S 落笔 · Ctrl+R 归位 · 仅记录坐标不改文件";
            tips.FontSize = 8.5;
            tips.VerticalAlignment = VerticalAlignment.Center;
            tips.TextTrimming = TextTrimming.CharacterEllipsis;
            tips.Foreground = Ink.Br(Ink.Fg3());
            rowFoot.Children.Add(tips);

            Border doneBtn = MiniDrawerBtn("完成", true, delegate { CloseDir(); });
            Grid.SetColumn(doneBtn, 1);
            rowFoot.Children.Add(doneBtn);
            Grid.SetRow(rowFoot, 4);
            g.Children.Add(rowFoot);

            dirOverlay.Child = g;
            return dirOverlay;
        }

        UIElement DirActionLink(string text, Action act)
        {
            TextBlock t = new TextBlock();
            t.Text = text;
            t.FontSize = 9.5;
            t.Margin = new Thickness(10, 0, 0, 0);
            t.VerticalAlignment = VerticalAlignment.Center;
            t.Cursor = Cursors.Hand;
            t.Foreground = Ink.Br(Ink.Verm());
            t.MouseEnter += delegate { t.Opacity = 0.72; };
            t.MouseLeave += delegate { t.Opacity = 1.0; };
            t.MouseLeftButtonUp += delegate { act(); };
            return t;
        }

        void RefreshDirInfo()
        {
            if (dirDataTxt != null) { dirDataTxt.Text = root; dirDataTxt.ToolTip = root; }
            string ex = AppDomain.CurrentDomain.BaseDirectory.TrimEnd('\\');
            if (dirExeTxt != null) { dirExeTxt.Text = ex; dirExeTxt.ToolTip = ex; }
            if (dirMemTxt != null)
                dirMemTxt.Text = "当前内存 · " + GetWorkingSetMB().ToString("0.0") + " MB · 后台零轮询";
        }

        void OpenDir()
        {
            if (busy || dirOverlay == null) return;
            if (historyOverlay != null && historyOverlay.Visibility == Visibility.Visible) CloseHistory(true);
            DisarmAll();
            RefreshDirInfo();

            dirOverlay.Visibility = Visibility.Visible;
            DoubleAnimation a = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(200));
            a.EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut };
            dirOverlay.BeginAnimation(UIElement.OpacityProperty, a);
        }

        void CloseDir()
        {
            if (dirOverlay == null || dirOverlay.Visibility != Visibility.Visible) return;
            DoubleAnimation a = new DoubleAnimation(dirOverlay.Opacity, 0, TimeSpan.FromMilliseconds(160));
            a.EasingFunction = new CubicEase { EasingMode = EasingMode.EaseIn };
            a.Completed += delegate
            {
                if (dirOverlay != null) dirOverlay.Visibility = Visibility.Collapsed;
                TrimMemory();
            };
            dirOverlay.BeginAnimation(UIElement.OpacityProperty, a);
        }

        void PopulateHistory()
        {
            if (historyList == null) return;
            historyList.Children.Clear();
            if (historyPathTxt != null) historyPathTxt.Text = "存址: " + root;

            string dir = System.IO.Path.Combine(root, "layouts");
            if (!Directory.Exists(dir))
            {
                TextBlock empty = new TextBlock();
                empty.Text = "暂无历史快照（每次「落子成记」会自动留底）";
                empty.FontSize = 9.5;
                empty.Foreground = Ink.Br(Ink.Fg3());
                empty.Margin = new Thickness(2, 12, 2, 0);
                historyList.Children.Add(empty);
                return;
            }

            string[] files = Directory.GetFiles(dir, "layout_*.json");
            Array.Sort(files);
            Array.Reverse(files);

            if (files.Length == 0)
            {
                TextBlock empty = new TextBlock();
                empty.Text = "暂无历史快照";
                empty.FontSize = 9.5;
                empty.Foreground = Ink.Br(Ink.Fg3());
                empty.Margin = new Thickness(2, 12, 2, 0);
                historyList.Children.Add(empty);
                return;
            }

            int limit = Math.Min(files.Length, 30);
            for (int i = 0; i < limit; i++)
            {
                string fp = files[i];
                Snapshot hs = null;
                try { hs = JsonStore.Load(fp); } catch { }
                if (hs == null || hs.Items == null || hs.Items.Count == 0) continue;

                string ts = (hs.SavedAt != null && hs.SavedAt.Length >= 16)
                    ? hs.SavedAt.Substring(5, 11).Replace("-", ".")
                    : System.IO.Path.GetFileNameWithoutExtension(fp);
                string label = ts + "  ·  " + hs.Count + " 项" +
                    (!string.IsNullOrEmpty(hs.Note) ? "  ·  " + hs.Note : "");

                bool selected = (previewPath != null && previewPath == fp);
                Border row = new Border();
                row.CornerRadius = new CornerRadius(5);
                row.Padding = new Thickness(8, 4, 8, 4);
                row.Margin = new Thickness(0, 1, 0, 1);
                row.Cursor = Cursors.Hand;
                row.Background = selected
                    ? Ink.Br(Ink.A(0x22, Ink.Verm().R, Ink.Verm().G, Ink.Verm().B))
                    : Brushes.Transparent;

                TextBlock tx = new TextBlock();
                tx.Text = (selected ? "● " : "· ") + label;
                tx.FontFamily = Ink.Mono;
                tx.FontSize = 9.5;
                tx.Foreground = Ink.Br(selected ? Ink.Verm() : Ink.Fg2());
                tx.TextTrimming = TextTrimming.CharacterEllipsis;
                row.Child = tx;

                Snapshot capSnap = hs;
                string capFp = fp;
                string capTs = ts;
                row.MouseEnter += delegate
                {
                    if (previewPath != capFp)
                        row.Background = Ink.Br(Ink.A(0x10, Ink.Fg().R, Ink.Fg().G, Ink.Fg().B));
                };
                row.MouseLeave += delegate
                {
                    if (previewPath != capFp)
                        row.Background = Brushes.Transparent;
                };
                row.MouseLeftButtonUp += delegate
                {
                    previewSnap = capSnap;
                    previewPath = capFp;
                    shown.Clear();
                    for (int k = 0; k < capSnap.Items.Count; k++) shown.Add(capSnap.Items[k]);
                    if (tCount != null) tCount.Text = shown.Count + " 项";
                    DrawBoard();
                    if (tStatus != null) tStatus.Text = "预览历史 · " + capTs + " · " + capSnap.Count + " 项（未改动桌面）";
                    PopulateHistory();
                };
                historyList.Children.Add(row);
            }
        }

        void ApplyHistoryRollback()
        {
            if (previewSnap == null)
            {
                if (tStatus != null) tStatus.Text = "请先在列表中点选一份历史快照";
                return;
            }
            try
            {
                // 回溯前先把当前活跃快照留底到 layouts\，确保回溯本身 100% 可逆
                Snapshot cur = Engine.LoadSnapshot(root);
                if (cur != null && cur.Items != null && cur.Items.Count > 0)
                    Engine.Backup(root, cur);

                JsonStore.Save(System.IO.Path.Combine(root, "layout_snapshot.json"), previewSnap);
                string ts = (previewSnap.SavedAt != null && previewSnap.SavedAt.Length >= 16)
                    ? previewSnap.SavedAt.Substring(5, 11).Replace("-", ".") : "所选时刻";
                CloseHistory(false);
                RefreshMeta();
                DrawBoard();
                AnimSave();
                if (tStatus != null) tStatus.Text = "已回溯记忆至 " + ts + " · 点「归位」可应用到桌面";
            }
            catch (Exception ex)
            {
                if (tStatus != null) tStatus.Text = "回溯失败 · " + ex.Message;
            }
        }

        void ChangeStorageDir()
        {
            try
            {
                using (System.Windows.Forms.FolderBrowserDialog dlg = new System.Windows.Forms.FolderBrowserDialog())
                {
                    dlg.Description = "选择「墨守」快照与历史回溯的存储文件夹";
                    dlg.SelectedPath = root;
                    dlg.ShowNewFolderButton = true;
                    if (dlg.ShowDialog() != System.Windows.Forms.DialogResult.OK) return;
                    string chosen = dlg.SelectedPath;
                    if (string.IsNullOrEmpty(chosen) || !Directory.Exists(chosen)) return;
                    if (!CanWrite(chosen))
                    {
                        if (tStatus != null) tStatus.Text = "该目录无写入权限，请换一个文件夹";
                        return;
                    }
                    if (!chosen.Equals(root, StringComparison.OrdinalIgnoreCase))
                    {
                        Migrate(root, chosen);
                        root = chosen;
                        savedDataDir = chosen;
                        SaveSettings();
                        RefreshMeta();
                        DrawBoard();
                        PopulateHistory();
                        if (tStatus != null) tStatus.Text = "存址已记住 · " + chosen;
                    }
                }
            }
            catch (Exception ex)
            {
                if (tStatus != null) tStatus.Text = "更改存址失败 · " + ex.Message;
            }
        }

        // ---------- 首次使用指引与开场回顾抽屉（点顶栏「墨守」旁专属圆点随时回顾） ----------
        UIElement GuideDrawer()
        {
            guideOverlay = new Border();
            guideOverlay.VerticalAlignment = VerticalAlignment.Bottom;
            guideOverlay.Height = 188;
            guideOverlay.Margin = new Thickness(12, 0, 12, 10);
            guideOverlay.CornerRadius = new CornerRadius(14);
            guideOverlay.Background = Ink.Br(Ink.Card());
            guideOverlay.BorderBrush = Ink.Br(Ink.Hair());
            guideOverlay.BorderThickness = new Thickness(1);
            guideOverlay.Padding = new Thickness(14, 10, 14, 10);
            guideOverlay.Visibility = Visibility.Collapsed;
            guideOverlay.Opacity = 0;
            Panel.SetZIndex(guideOverlay, 74);

            Grid g = new Grid();
            g.RowDefinitions.Add(RowDef(24));
            g.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            g.RowDefinitions.Add(RowDef(30));

            Grid top = new Grid();
            TextBlock title = new TextBlock();
            title.Text = "墨守 · 使用指引";
            title.FontFamily = Ink.Serif;
            title.FontSize = 12;
            title.VerticalAlignment = VerticalAlignment.Center;
            title.Foreground = Ink.Br(Ink.Fg());
            top.Children.Add(title);

            TextBlock closeTx = new TextBlock();
            closeTx.Text = "收起 (Esc)";
            closeTx.FontSize = 9.5;
            closeTx.HorizontalAlignment = HorizontalAlignment.Right;
            closeTx.VerticalAlignment = VerticalAlignment.Center;
            closeTx.Foreground = Ink.Br(Ink.Fg3());
            closeTx.Cursor = Cursors.Hand;
            closeTx.MouseEnter += delegate { closeTx.Foreground = Ink.Br(Ink.Verm()); };
            closeTx.MouseLeave += delegate { closeTx.Foreground = Ink.Br(Ink.Fg3()); };
            closeTx.MouseLeftButtonUp += delegate { CloseGuide(true, true); };
            top.Children.Add(closeTx);
            Grid.SetRow(top, 0);
            g.Children.Add(top);

            StackPanel steps = new StackPanel { Margin = new Thickness(0, 6, 0, 4) };
            steps.Children.Add(GuideLine("一", "落子成记", "记录此刻桌面每个图标的精确坐标（只动坐标，绝不改动文件）。"));
            steps.Children.Add(GuideLine("二", "棋盘布阵", "按住棋盘上任意棋子拖动即可排布或互换；支持单步撤销 (Ctrl+Z) 与一键应用。"));
            steps.Children.Add(GuideLine("三", "归位回溯", "桌面被打乱后点「归位」还原；点「回溯」可预览并切回任意历史时刻。"));
            steps.Children.Add(GuideLine("四", "快捷操作", "四边四角可等比缩放，双击顶栏切换尺寸；点「墨守」旁圆点可随时回顾。"));
            Grid.SetRow(steps, 1);
            g.Children.Add(steps);

            Grid foot = new Grid();
            foot.ColumnDefinitions.Add(new ColumnDefinition());
            foot.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(8) });
            foot.ColumnDefinitions.Add(new ColumnDefinition());

            Border btnReplay = MiniDrawerBtn("重播开场动画", false, delegate
            {
                CloseGuide(true, false);
                Pause(180, delegate { PlayIntro(); });
            });
            Grid.SetColumn(btnReplay, 0);
            foot.Children.Add(btnReplay);

            Border btnOk = MiniDrawerBtn("知道了，开始使用", true, delegate { CloseGuide(true, true); });
            Grid.SetColumn(btnOk, 2);
            foot.Children.Add(btnOk);

            Grid.SetRow(foot, 2);
            g.Children.Add(foot);

            guideOverlay.Child = g;
            return guideOverlay;
        }

        UIElement GuideLine(string num, string head, string desc)
        {
            Grid row = new Grid { Margin = new Thickness(0, 2, 0, 2) };
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(20) });
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(58) });
            row.ColumnDefinitions.Add(new ColumnDefinition());

            TextBlock tNum = new TextBlock
            {
                Text = num + ".",
                FontFamily = Ink.Serif,
                FontSize = 10,
                Foreground = Ink.Br(Ink.Verm()),
                VerticalAlignment = VerticalAlignment.Top
            };
            TextBlock tHead = new TextBlock
            {
                Text = head,
                FontFamily = Ink.Serif,
                FontSize = 10,
                FontWeight = FontWeights.Medium,
                Foreground = Ink.Br(Ink.Fg()),
                VerticalAlignment = VerticalAlignment.Top
            };
            TextBlock tDesc = new TextBlock
            {
                Text = desc,
                FontSize = 9.5,
                Foreground = Ink.Br(Ink.Fg2()),
                TextWrapping = TextWrapping.Wrap
            };
            Grid.SetColumn(tNum, 0);
            Grid.SetColumn(tHead, 1);
            Grid.SetColumn(tDesc, 2);
            row.Children.Add(tNum);
            row.Children.Add(tHead);
            row.Children.Add(tDesc);
            return row;
        }

        void OpenGuide()
        {
            if (busy || guideOverlay == null) return;
            if (historyOverlay != null && historyOverlay.Visibility == Visibility.Visible) CloseHistory(true);
            if (dirOverlay != null && dirOverlay.Visibility == Visibility.Visible) CloseDir();

            guideOverlay.Visibility = Visibility.Visible;
            DoubleAnimation a = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(220));
            a.EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut };
            guideOverlay.BeginAnimation(UIElement.OpacityProperty, a);
        }

        void CloseGuide(bool markSeen, bool showHomingHint)
        {
            if (markSeen && !seenGuide)
            {
                seenGuide = true;
                SaveSettings();
            }
            if (guideOverlay == null || guideOverlay.Visibility != Visibility.Visible) return;
            DoubleAnimation a = new DoubleAnimation(guideOverlay.Opacity, 0, TimeSpan.FromMilliseconds(180));
            a.EasingFunction = new CubicEase { EasingMode = EasingMode.EaseIn };
            a.Completed += delegate
            {
                if (guideOverlay != null) guideOverlay.Visibility = Visibility.Collapsed;
                TrimMemory();
            };
            guideOverlay.BeginAnimation(UIElement.OpacityProperty, a);

            if (showHomingHint) PlayGuideHomingHint();
        }

        // 收起指引时：一枚朱砂微光从指引卡飞向左上角「墨守」旁回顾圆点，到达后脉动两下，直观提示后续回顾入口
        void PlayGuideHomingHint()
        {
            if (reviewDotEl == null || veilLayer == null) return;
            double w = ActualWidth > 0 ? ActualWidth : START_W;
            double h = ActualHeight > 0 ? ActualHeight : START_H;
            double k = fitScale != null ? fitScale.ScaleX : (w / WIN_W);
            if (k < 0.5) k = 1.0;

            veilLayer.Width = w;
            veilLayer.Height = h;

            Point from = new Point(w * 0.24, h * 0.68);
            Point to = AnchorOf(reviewDotEl, 52 * k, 32 * k);

            LaunchCinnabarSpark(from, to, 20, 480, k * 1.15, 0);
            SpawnVeilRing(to.X, to.Y, 34 * k, Ink.Verm(), 470, 460, 0.72);
            SpawnVeilRing(to.X, to.Y, 48 * k, Ink.Verm(), 620, 520, 0.42);

            // 目标回顾圆点本身也轻弹呼吸一次
            Pause(470, delegate
            {
                if (reviewDotEl != null && reviewDotScale != null)
                {
                    DoubleAnimationUsingKeyFrames psc = new DoubleAnimationUsingKeyFrames();
                    psc.KeyFrames.Add(new EasingDoubleKeyFrame(1.48, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(160)),
                        new CubicEase { EasingMode = EasingMode.EaseOut }));
                    psc.KeyFrames.Add(new EasingDoubleKeyFrame(1.0, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(460)),
                        new CubicEase { EasingMode = EasingMode.EaseInOut }));
                    reviewDotScale.BeginAnimation(ScaleTransform.ScaleXProperty, psc);
                    reviewDotScale.BeginAnimation(ScaleTransform.ScaleYProperty, psc);

                    DoubleAnimationUsingKeyFrames pop = new DoubleAnimationUsingKeyFrames();
                    pop.KeyFrames.Add(new LinearDoubleKeyFrame(1.0, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(140))));
                    pop.KeyFrames.Add(new LinearDoubleKeyFrame(1.0, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(480))));
                    pop.KeyFrames.Add(new EasingDoubleKeyFrame(0.48, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(820)),
                        new CubicEase { EasingMode = EasingMode.EaseOut }));
                    reviewDotEl.BeginAnimation(UIElement.OpacityProperty, pop);
                }
            });

            if (tStatus != null)
                tStatus.Text = "指引已收起 · 随时点左上角「墨守」旁圆点回顾";
            Pause(1220, delegate { if (!busy) veilLayer.Children.Clear(); });
        }

        // ---------- 启动 ----------
        void OnLoaded(object sender, RoutedEventArgs e)
        {
            Opacity = 0;
            DoubleAnimation a = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(220));
            a.EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut };
            BeginAnimation(Window.OpacityProperty, a);

            if (winScale != null)
            {
                winScale.ScaleX = 0.985;
                winScale.ScaleY = 0.985;
                DoubleAnimation sx = new DoubleAnimation(0.985, 1.0, TimeSpan.FromMilliseconds(260));
                sx.EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut };
                winScale.BeginAnimation(ScaleTransform.ScaleXProperty, sx);
                winScale.BeginAnimation(ScaleTransform.ScaleYProperty, sx);
            }

            Dispatcher.BeginInvoke(new Action(delegate
            {
                RefreshMeta();
                DrawBoard();
                PlayIntro();
            }), DispatcherPriority.Loaded);

            // --demo：自己走一遍换材质，用来验证过渡是否连续（不进产物交互路径）
            if (MoShouApp.Demo)
            {
                Pause(2400, delegate { TogglePaper(); });
                Pause(4200, delegate { TogglePaper(); });
            }

            if (MoShouApp.ShotDir != null) StartShot();
        }

        // 获取某控件中心在 rootLayer 下的精确坐标，确保开场动画落点与界面严丝合缝
        Point AnchorOf(UIElement el, double fbX, double fbY)
        {
            try
            {
                FrameworkElement fe = el as FrameworkElement;
                if (fe != null && fe.ActualWidth > 0 && fe.ActualHeight > 0 && rootLayer != null)
                {
                    GeneralTransform gt = fe.TransformToAncestor(rootLayer);
                    return gt.Transform(new Point(fe.ActualWidth * 0.5, fe.ActualHeight * 0.5));
                }
            }
            catch { }
            return new Point(fbX, fbY);
        }

        static void RevealEl(UIElement el, int delayMs, int durMs)
        {
            if (el == null) return;
            DoubleAnimation op = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(durMs));
            op.BeginTime = TimeSpan.FromMilliseconds(delayMs);
            op.EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut };
            el.BeginAnimation(UIElement.OpacityProperty, op);
        }

        static void ResetHidden(UIElement el)
        {
            if (el == null) return;
            el.BeginAnimation(UIElement.OpacityProperty, null);
            el.Opacity = 0;
        }

        // ---------- 开场动画：水墨太极顺时针旋舞丝滑化双键 + 朱砂日轮散三点 + 六色古矿彩直飞联动真实棋盘与图例 ----------
        void PlayIntro()
        {
            double w = ActualWidth > 0 ? ActualWidth : START_W;
            double h = ActualHeight > 0 ? ActualHeight : START_H;
            double k = fitScale != null ? fitScale.ScaleX : (w / WIN_W);
            if (k < 0.5) k = 1.0;

            UpdateLayout();
            veilLayer.Children.Clear();
            wetLayer.Children.Clear();
            veilLayer.Width = w;
            veilLayer.Height = h;

            // 先彻底清除上一次 HoldEnd 动画再归零，确保「重播开场动画」也从纯白宣纸起势
            ResetHidden(headerHost);
            ResetHidden(boardHostEl);
            ResetHidden(calHostEl);
            ResetHidden(actionBarEl);
            ResetHidden(statusLineEl);

            for (int i = 0; i < dots.Count; i++)
            {
                dots[i].BeginAnimation(UIElement.OpacityProperty, null);
                dots[i].Opacity = 0;
                if (dots[i].RenderTransform is TransformGroup)
                {
                    ScaleTransform dsc = (ScaleTransform)((TransformGroup)dots[i].RenderTransform).Children[0];
                    dsc.BeginAnimation(ScaleTransform.ScaleXProperty, null);
                    dsc.BeginAnimation(ScaleTransform.ScaleYProperty, null);
                    dsc.ScaleX = 0.42;
                    dsc.ScaleY = 0.42;
                }
            }

            double cx = w * 0.5;
            double cy = h * 0.39;
            Point btnL = AnchorOf(btnSave, w * 0.28, h * 0.84);
            Point btnR = AnchorOf(btnRestore, w * 0.72, h * 0.84);
            Point closePt = AnchorOf(closeBtnEl, w - 28 * k, 28 * k);

            // ========== 1. 水墨阴阳双珠（全程顺时针旋舞，绝不逆转回拐，顺切线丝滑融入「落子成记」与「归位」） ==========
            Color yinCol = Ink.PrimaryBg();
            Color yangCol = Ink.Dark ? Ink.C(0x2B, 0x2A, 0x20) : Ink.C(0xF6, 0xF2, 0xE8);
            Color yangRim = Ink.Dark ? Ink.C(0x6E, 0x6A, 0x5C) : Ink.C(0xC9, 0xBE, 0xA6);
            double orbDia = 54 * k;

            // 阴珠：有机水墨团 + 极淡宣纸内芯（阴中有阳）
            Canvas yinOrb = new Canvas { Width = orbDia, Height = orbDia, Opacity = 0, RenderTransformOrigin = new Point(0.5, 0.5), IsHitTestVisible = false };
            System.Windows.Shapes.Path yinBody = new System.Windows.Shapes.Path
            {
                Width = orbDia, Height = orbDia,
                Data = Ink.OrganicBlobGeo(orbDia, 7101, 0.06, 0.035, 0.016),
                Fill = Ink.InkWash(yinCol, 0.44)
            };
            Ellipse yinEye = new Ellipse { Width = orbDia * 0.22, Height = orbDia * 0.22, Fill = Ink.Soft(yangCol), Opacity = 0.72 };
            Canvas.SetLeft(yinEye, orbDia * 0.39);
            Canvas.SetTop(yinEye, orbDia * 0.39);
            yinOrb.Children.Add(yinBody);
            yinOrb.Children.Add(yinEye);

            // 阳珠：有机宣纸团 + 极淡深墨内芯（阳中有阴）
            Canvas yangOrb = new Canvas { Width = orbDia, Height = orbDia, Opacity = 0, RenderTransformOrigin = new Point(0.5, 0.5), IsHitTestVisible = false };
            System.Windows.Shapes.Path yangBody = new System.Windows.Shapes.Path
            {
                Width = orbDia, Height = orbDia,
                Data = Ink.OrganicBlobGeo(orbDia, 7102, 0.06, 0.035, 0.016),
                Fill = Ink.InkWash(yangCol, 0.54),
                Stroke = Ink.Br(Ink.A(0x88, yangRim.R, yangRim.G, yangRim.B)),
                StrokeThickness = 1.2 * k
            };
            Ellipse yangEye = new Ellipse { Width = orbDia * 0.22, Height = orbDia * 0.22, Fill = Ink.Soft(yinCol), Opacity = 0.68 };
            Canvas.SetLeft(yangEye, orbDia * 0.39);
            Canvas.SetTop(yangEye, orbDia * 0.39);
            yangOrb.Children.Add(yangBody);
            yangOrb.Children.Add(yangEye);

            // 阴珠：自 0° 顺时针变速旋满 425°（抵达 5 点钟方向，切线直指左下「落子成记」），顺切线丝滑落位
            AnimateYinYangClockwise(yinOrb, orbDia, cx, cy, 0.0, Math.PI * 2.36, 680, btnL, 380, k, true);
            // 阳珠：自 180° 同步顺时针变速旋满 425°，随后继续顺时针绕右上弧线丝滑滑入右下「归位」
            AnimateYinYangClockwise(yangOrb, orbDia, cx, cy, Math.PI, Math.PI * 2.36, 680, btnR, 420, k, false);

            // 阴阳相抱瞬间在画心漾开柔墨水晕
            SpawnVeilRing(cx, cy, 134 * k, Ink.Fg3(), 300, 480, 0.34);

            // 两枚主按钮在阴阳双珠落位瞬间柔和托出
            RevealEl(actionBarEl, 920, 300);

            // ========== 2. 右上角关闭键朱砂日轮亮起（540..1360ms），变速弧线散出三缕朱光落到底部三入口 ==========
            double sunDia = 50 * k;
            System.Windows.Shapes.Path sunCore = new System.Windows.Shapes.Path();
            sunCore.Width = sunDia; sunCore.Height = sunDia;
            sunCore.Data = Ink.OrganicBlobGeo(sunDia, 7201, 0.04, 0.025, 0.012);
            sunCore.Fill = Ink.InkWash(Ink.Verm(), 0.30);
            sunCore.Opacity = 0;
            Canvas.SetLeft(sunCore, closePt.X - sunDia * 0.5);
            Canvas.SetTop(sunCore, closePt.Y - sunDia * 0.5);
            ScaleTransform sunSc = new ScaleTransform(0.2, 0.2);
            sunCore.RenderTransformOrigin = new Point(0.5, 0.5);
            sunCore.RenderTransform = sunSc;
            veilLayer.Children.Add(sunCore);

            DoubleAnimation sunGrow = new DoubleAnimation(0.2, 1.32, TimeSpan.FromMilliseconds(380));
            sunGrow.BeginTime = TimeSpan.FromMilliseconds(540);
            sunGrow.EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut };
            sunSc.BeginAnimation(ScaleTransform.ScaleXProperty, sunGrow);
            sunSc.BeginAnimation(ScaleTransform.ScaleYProperty, sunGrow);

            DoubleAnimationUsingKeyFrames sunOp = new DoubleAnimationUsingKeyFrames();
            sunOp.BeginTime = TimeSpan.FromMilliseconds(540);
            sunOp.KeyFrames.Add(new LinearDoubleKeyFrame(0.86, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(120))));
            sunOp.KeyFrames.Add(new EasingDoubleKeyFrame(0.0, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(460)),
                new CubicEase { EasingMode = EasingMode.EaseOut }));
            sunCore.BeginAnimation(UIElement.OpacityProperty, sunOp);

            SpawnVeilRing(closePt.X, closePt.Y, 82 * k, Ink.Verm(), 580, 440, 0.52);
            RevealEl(headerHost, 640, 300);

            for (int p = 0; p < bottomPips.Count; p++)
            {
                Point target = AnchorOf(bottomPips[p], w * (0.68 + p * 0.10), h - 22 * k);
                int launchMs = 720 + p * 75;
                int flightMs = 420;
                LaunchCinnabarSpark(closePt, target, launchMs, flightMs, k, p);
                SpawnVeilRing(target.X, target.Y, 24 * k, Ink.Verm(), launchMs + flightMs - 30, 300, 0.58);
            }
            RevealEl(statusLineEl, 1140, 280);

            // ========== 3. 六色古矿彩变速直飞联动真实界面（920..2150ms）：六瓣迅绽 → 变速飞向棋盘同类重心与图例点 ==========
            RevealEl(boardHostEl, 960, 340);
            RevealEl(calHostEl, 1040, 320);

            string[] pigments = new string[] { "app", "dir", "arc", "doc", "sys", "other" };
            Dictionary<string, int> pigOrder = new Dictionary<string, int>();
            for (int m = 0; m < pigments.Length; m++) pigOrder[pigments[m]] = m;

            // 计算棋盘上每种颜色棋子的真实视觉重心（rootLayer 坐标系）
            Dictionary<string, Point> typeCentroids = new Dictionary<string, Point>();
            Dictionary<string, int> typeCnts = new Dictionary<string, int>();
            for (int i = 0; i < dots.Count; i++)
            {
                string ty = (i < dotTypes.Count) ? dotTypes[i] : "other";
                Point dp = AnchorOf(dots[i], cx, cy);
                if (!typeCentroids.ContainsKey(ty)) { typeCentroids[ty] = dp; typeCnts[ty] = 1; }
                else
                {
                    Point prev = typeCentroids[ty];
                    typeCentroids[ty] = new Point(prev.X + dp.X, prev.Y + dp.Y);
                    typeCnts[ty]++;
                }
            }
            for (int m = 0; m < pigments.Length; m++)
            {
                string ty = pigments[m];
                if (typeCnts.ContainsKey(ty) && typeCnts[ty] > 0)
                    typeCentroids[ty] = new Point(typeCentroids[ty].X / typeCnts[ty], typeCentroids[ty].Y / typeCnts[ty]);
            }

            double pigRad = 44 * k;
            double beadDia = 19 * k;
            for (int m = 0; m < pigments.Length; m++)
            {
                string ty = pigments[m];
                Color pc = Ink.ByType(ty);
                double a0 = -Math.PI * 0.5 + m * (2.0 * Math.PI / 6.0);
                double hx = cx + Math.Cos(a0) * pigRad;
                double hy = cy + Math.Sin(a0) * pigRad;

                Point legPt = legendDots.ContainsKey(ty) ? AnchorOf(legendDots[ty], w * (0.12 + m * 0.14), h * 0.72) : new Point(w * (0.12 + m * 0.14), h * 0.72);
                Point boardPt = typeCentroids.ContainsKey(ty) ? typeCentroids[ty] : legPt;

                int bloomStart = 920 + m * 35;
                int flyStart = 1220 + m * 55;
                int flyDur = 380;

                LaunchPigmentOrb(cx, cy, hx, hy, boardPt.X, boardPt.Y, beadDia, pc, bloomStart, flyStart, flyDur);
                SpawnVeilRing(boardPt.X, boardPt.Y, 54 * k, pc, flyStart + flyDur - 40, 360, 0.54);

                LaunchColorSpark(new Point(hx, hy), legPt, pc, flyStart + 25, 340, k, m);
                SpawnVeilRing(legPt.X, legPt.Y, 20 * k, pc, flyStart + 350, 280, 0.62);
            }

            // 棋盘真实棋子在对应矿彩珠落地瞬间，按颜色次第柔和唤醒
            for (int i = 0; i < dots.Count; i++)
            {
                Ellipse d = dots[i];
                if (!(d.RenderTransform is TransformGroup)) continue;
                ScaleTransform dsc = (ScaleTransform)((TransformGroup)d.RenderTransform).Children[0];
                string ty = (i < dotTypes.Count) ? dotTypes[i] : "other";
                int tier = pigOrder.ContainsKey(ty) ? pigOrder[ty] : 5;
                int dl = 1520 + tier * 55 + (i % 4) * 14;

                DoubleAnimation asc = new DoubleAnimation(0.42, 1.0, TimeSpan.FromMilliseconds(280));
                asc.BeginTime = TimeSpan.FromMilliseconds(dl);
                asc.EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut };
                dsc.BeginAnimation(ScaleTransform.ScaleXProperty, asc);
                dsc.BeginAnimation(ScaleTransform.ScaleYProperty, asc);

                DoubleAnimation aop = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(250));
                aop.BeginTime = TimeSpan.FromMilliseconds(dl);
                aop.EasingFunction = new SineEase { EasingMode = EasingMode.EaseOut };
                d.BeginAnimation(UIElement.OpacityProperty, aop);

                if ((i & 1) == 0) SpawnWetRing(d, dl + 50, 0.24);
            }

            // 2.26s 利落收束清场并释放开场临时内存（完整保留全部三幕美学，紧凑不拖沓）
            Pause(2260, delegate
            {
                veilLayer.Children.Clear();
                wetLayer.Children.Clear();
                TrimMemory();
                if (!seenGuide && MoShouApp.ShotDir == null)
                    Pause(140, delegate { OpenGuide(); });
            });
        }

        // 全程顺时针变速水墨太极轨迹：起势从容 → 中段相抱加速 → 末段沿顺时针切线 C1 连续丝滑飞入按钮
        void AnimateYinYangClockwise(Canvas orb, double dia, double cx, double cy,
                                     double phase0, double sweepRad, int orbitMs, Point target, int flyMs, double k, bool isYin)
        {
            Canvas.SetLeft(orb, cx - dia * 0.5);
            Canvas.SetTop(orb, cy - dia * 0.5);

            ScaleTransform sc = new ScaleTransform(0.35, 0.35);
            TranslateTransform tt = new TranslateTransform(0, 0);
            TransformGroup tg = new TransformGroup();
            tg.Children.Add(sc);
            tg.Children.Add(tt);
            orb.RenderTransform = tg;
            veilLayer.Children.Add(orb);

            DoubleAnimationUsingKeyFrames kx = new DoubleAnimationUsingKeyFrames();
            DoubleAnimationUsingKeyFrames ky = new DoubleAnimationUsingKeyFrames();

            // 阶段一：关键帧变速顺时针太极旋舞（慢起→中段提速相抱→匀畅出势，律动感强）
            int orbitSteps = 26;
            double lastX = 0, lastY = 0;
            double endAng = phase0 + sweepRad;
            for (int i = 0; i <= orbitSteps; i++)
            {
                double t = (double)i / orbitSteps;
                // S 型变速曲线：前 25% 轻盈起势，中段 25%..75% 提速旋汇，后段平滑衔接切线
                double s = t * t * (3.0 - 2.0 * t);
                double ramped = 0.28 * t + 0.72 * s;
                double ang = phase0 + sweepRad * ramped;
                double rad = (15.0 + 31.0 * Math.Pow(2.0 * ramped - 1.0, 2.0)) * k;
                lastX = Math.Cos(ang) * rad;
                lastY = Math.Sin(ang) * rad;
                TimeSpan kt = TimeSpan.FromMilliseconds(orbitMs * t);
                kx.KeyFrames.Add(new LinearDoubleKeyFrame(lastX, KeyTime.FromTimeSpan(kt)));
                ky.KeyFrames.Add(new LinearDoubleKeyFrame(lastY, KeyTime.FromTimeSpan(kt)));
            }

            // 阶段二：沿轨道末点的顺时针切线 (-sin(endAng), cos(endAng)) 顺势飞出，三次贝塞尔保证 C1 切线连续、绝不逆时针折返
            double tx = target.X - cx;
            double ty = target.Y - cy;
            double tanX = -Math.Sin(endAng);
            double tanY = Math.Cos(endAng);

            double c1x, c1y, c2x, c2y;
            if (isYin)
            {
                c1x = lastX + tanX * 68 * k;
                c1y = lastY + tanY * 68 * k;
                c2x = tx + 22 * k;
                c2y = ty - 42 * k;
            }
            else
            {
                c1x = lastX + tanX * 115 * k;
                c1y = lastY + tanY * 85 * k;
                c2x = tx + 52 * k;
                c2y = ty - 95 * k;
            }

            int flySteps = 16;
            for (int i = 1; i <= flySteps; i++)
            {
                double u = (double)i / flySteps;
                // 快出慢落变速曲线：初段借旋舞惯性迅捷离轨，末段优雅减速融入按钮
                double eu = 1.0 - Math.Pow(1.0 - u, 2.6);
                double om = 1.0 - eu;
                double px = om * om * om * lastX + 3 * om * om * eu * c1x + 3 * om * eu * eu * c2x + eu * eu * eu * tx;
                double py = om * om * om * lastY + 3 * om * om * eu * c1y + 3 * om * eu * eu * c2y + eu * eu * eu * ty;
                TimeSpan kt = TimeSpan.FromMilliseconds(orbitMs + flyMs * u);
                kx.KeyFrames.Add(new LinearDoubleKeyFrame(px, KeyTime.FromTimeSpan(kt)));
                ky.KeyFrames.Add(new LinearDoubleKeyFrame(py, KeyTime.FromTimeSpan(kt)));
            }
            tt.BeginAnimation(TranslateTransform.XProperty, kx);
            tt.BeginAnimation(TranslateTransform.YProperty, ky);

            DoubleAnimationUsingKeyFrames sx = new DoubleAnimationUsingKeyFrames();
            sx.KeyFrames.Add(new EasingDoubleKeyFrame(1.05, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(220)),
                new CubicEase { EasingMode = EasingMode.EaseOut }));
            sx.KeyFrames.Add(new LinearDoubleKeyFrame(0.98, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(orbitMs))));
            sx.KeyFrames.Add(new EasingDoubleKeyFrame(2.55, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(orbitMs + flyMs)),
                new CubicEase { EasingMode = EasingMode.EaseOut }));
            sc.BeginAnimation(ScaleTransform.ScaleXProperty, sx);

            DoubleAnimationUsingKeyFrames sy = new DoubleAnimationUsingKeyFrames();
            sy.KeyFrames.Add(new EasingDoubleKeyFrame(1.05, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(220)),
                new CubicEase { EasingMode = EasingMode.EaseOut }));
            sy.KeyFrames.Add(new LinearDoubleKeyFrame(0.98, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(orbitMs))));
            sy.KeyFrames.Add(new EasingDoubleKeyFrame(0.74, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(orbitMs + flyMs)),
                new CubicEase { EasingMode = EasingMode.EaseOut }));
            sc.BeginAnimation(ScaleTransform.ScaleYProperty, sy);

            DoubleAnimationUsingKeyFrames op = new DoubleAnimationUsingKeyFrames();
            op.KeyFrames.Add(new LinearDoubleKeyFrame(0.95, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(160))));
            op.KeyFrames.Add(new LinearDoubleKeyFrame(0.92, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(orbitMs + flyMs - 140))));
            op.KeyFrames.Add(new EasingDoubleKeyFrame(0.0, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(orbitMs + flyMs + 40)),
                new CubicEase { EasingMode = EasingMode.EaseOut }));
            orb.BeginAnimation(UIElement.OpacityProperty, op);
        }

        // 单色古矿彩珠：画心六瓣展开 → 直飞棋盘真实同类重心并化墨融入
        void LaunchPigmentOrb(double cx, double cy, double hx, double hy, double tx, double ty,
                              double dia, Color col, int bloomStart, int flyStart, int flyDur)
        {
            Ellipse bead = new Ellipse();
            bead.Width = dia; bead.Height = dia;
            bead.Fill = Ink.Soft(col);
            bead.Opacity = 0;
            bead.IsHitTestVisible = false;
            Canvas.SetLeft(bead, cx - dia * 0.5);
            Canvas.SetTop(bead, cy - dia * 0.5);

            ScaleTransform bsc = new ScaleTransform(0.3, 0.3);
            TranslateTransform btt = new TranslateTransform(0, 0);
            TransformGroup btg = new TransformGroup();
            btg.Children.Add(bsc);
            btg.Children.Add(btt);
            bead.RenderTransformOrigin = new Point(0.5, 0.5);
            bead.RenderTransform = btg;
            veilLayer.Children.Add(bead);

            double dx1 = hx - cx, dy1 = hy - cy;
            double dx2 = tx - cx, dy2 = ty - cy;
            double midX = (dx1 + dx2) * 0.5;
            double midY = (dy1 + dy2) * 0.5 - 18;
            int holdDur = Math.Max(200, flyStart - bloomStart);

            DoubleAnimationUsingKeyFrames kx = new DoubleAnimationUsingKeyFrames();
            DoubleAnimationUsingKeyFrames ky = new DoubleAnimationUsingKeyFrames();
            kx.BeginTime = TimeSpan.FromMilliseconds(bloomStart);
            ky.BeginTime = TimeSpan.FromMilliseconds(bloomStart);

            kx.KeyFrames.Add(new EasingDoubleKeyFrame(dx1, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(340)),
                new CubicEase { EasingMode = EasingMode.EaseOut }));
            ky.KeyFrames.Add(new EasingDoubleKeyFrame(dy1, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(340)),
                new CubicEase { EasingMode = EasingMode.EaseOut }));
            kx.KeyFrames.Add(new LinearDoubleKeyFrame(dx1, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(holdDur))));
            ky.KeyFrames.Add(new LinearDoubleKeyFrame(dy1, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(holdDur))));

            int steps = 12;
            for (int s = 1; s <= steps; s++)
            {
                double u = (double)s / steps;
                double eu = u < 0.5 ? 2 * u * u : 1 - Math.Pow(-2 * u + 2, 2) / 2.0;
                double px = (1 - eu) * (1 - eu) * dx1 + 2 * (1 - eu) * eu * midX + eu * eu * dx2;
                double py = (1 - eu) * (1 - eu) * dy1 + 2 * (1 - eu) * eu * midY + eu * eu * dy2;
                TimeSpan kt = TimeSpan.FromMilliseconds(holdDur + flyDur * u);
                kx.KeyFrames.Add(new LinearDoubleKeyFrame(px, KeyTime.FromTimeSpan(kt)));
                ky.KeyFrames.Add(new LinearDoubleKeyFrame(py, KeyTime.FromTimeSpan(kt)));
            }
            btt.BeginAnimation(TranslateTransform.XProperty, kx);
            btt.BeginAnimation(TranslateTransform.YProperty, ky);

            DoubleAnimationUsingKeyFrames bScale = new DoubleAnimationUsingKeyFrames();
            bScale.BeginTime = TimeSpan.FromMilliseconds(bloomStart);
            bScale.KeyFrames.Add(new EasingDoubleKeyFrame(1.12, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(340)),
                new CubicEase { EasingMode = EasingMode.EaseOut }));
            bScale.KeyFrames.Add(new LinearDoubleKeyFrame(1.05, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(holdDur))));
            bScale.KeyFrames.Add(new EasingDoubleKeyFrame(0.72, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(holdDur + flyDur - 60)),
                new CubicEase { EasingMode = EasingMode.EaseInOut }));
            bScale.KeyFrames.Add(new EasingDoubleKeyFrame(1.9, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(holdDur + flyDur + 140)),
                new CubicEase { EasingMode = EasingMode.EaseOut }));
            bsc.BeginAnimation(ScaleTransform.ScaleXProperty, bScale);
            bsc.BeginAnimation(ScaleTransform.ScaleYProperty, bScale);

            DoubleAnimationUsingKeyFrames bOp = new DoubleAnimationUsingKeyFrames();
            bOp.BeginTime = TimeSpan.FromMilliseconds(bloomStart);
            bOp.KeyFrames.Add(new LinearDoubleKeyFrame(0.95, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(220))));
            bOp.KeyFrames.Add(new LinearDoubleKeyFrame(0.92, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(holdDur + flyDur - 80))));
            bOp.KeyFrames.Add(new EasingDoubleKeyFrame(0.0, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(holdDur + flyDur + 140)),
                new CubicEase { EasingMode = EasingMode.EaseOut }));
            bead.BeginAnimation(UIElement.OpacityProperty, bOp);
        }

        // 彩色分灵小珠飞向对应图例圆点
        void LaunchColorSpark(Point from, Point to, Color col, int startMs, int durMs, double k, int idx)
        {
            double dia = 9 * k;
            Ellipse sp = new Ellipse();
            sp.Width = dia; sp.Height = dia;
            sp.Fill = Ink.Soft(col);
            sp.Opacity = 0;
            sp.IsHitTestVisible = false;
            Canvas.SetLeft(sp, from.X - dia * 0.5);
            Canvas.SetTop(sp, from.Y - dia * 0.5);

            TranslateTransform tt = new TranslateTransform(0, 0);
            sp.RenderTransform = tt;
            veilLayer.Children.Add(sp);

            double dx = to.X - from.X;
            double dy = to.Y - from.Y;
            double ctrlX = dx * 0.45 + ((idx % 2 == 0) ? -16 * k : 16 * k);
            double ctrlY = dy * 0.40;

            DoubleAnimationUsingKeyFrames kx = new DoubleAnimationUsingKeyFrames();
            DoubleAnimationUsingKeyFrames ky = new DoubleAnimationUsingKeyFrames();
            kx.BeginTime = TimeSpan.FromMilliseconds(startMs);
            ky.BeginTime = TimeSpan.FromMilliseconds(startMs);
            int steps = 10;
            for (int s = 1; s <= steps; s++)
            {
                double u = (double)s / steps;
                double eu = u < 0.5 ? 2 * u * u : 1 - Math.Pow(-2 * u + 2, 2) / 2.0;
                double px = 2 * (1 - eu) * eu * ctrlX + eu * eu * dx;
                double py = 2 * (1 - eu) * eu * ctrlY + eu * eu * dy;
                TimeSpan kt = TimeSpan.FromMilliseconds(durMs * u);
                kx.KeyFrames.Add(new LinearDoubleKeyFrame(px, KeyTime.FromTimeSpan(kt)));
                ky.KeyFrames.Add(new LinearDoubleKeyFrame(py, KeyTime.FromTimeSpan(kt)));
            }
            tt.BeginAnimation(TranslateTransform.XProperty, kx);
            tt.BeginAnimation(TranslateTransform.YProperty, ky);

            DoubleAnimationUsingKeyFrames op = new DoubleAnimationUsingKeyFrames();
            op.BeginTime = TimeSpan.FromMilliseconds(startMs);
            op.KeyFrames.Add(new LinearDoubleKeyFrame(0.92, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(70))));
            op.KeyFrames.Add(new LinearDoubleKeyFrame(0.85, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(durMs - 60))));
            op.KeyFrames.Add(new LinearDoubleKeyFrame(0.0, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(durMs + 30))));
            sp.BeginAnimation(UIElement.OpacityProperty, op);
        }

        // 阴阳双珠轨迹：0..920ms 绕画心舒缓旋汇相抱 → 920..1440ms 分赴目标按钮中心舒展融入
        void AnimateYinYangOrb(System.Windows.Shapes.Path orb, double dia, double cx, double cy,
                               double phase0, Point target, double k)
        {
            Canvas.SetLeft(orb, cx - dia * 0.5);
            Canvas.SetTop(orb, cy - dia * 0.5);

            ScaleTransform sc = new ScaleTransform(0.35, 0.35);
            TranslateTransform tt = new TranslateTransform(0, 0);
            TransformGroup tg = new TransformGroup();
            tg.Children.Add(sc);
            tg.Children.Add(tt);
            orb.RenderTransform = tg;
            veilLayer.Children.Add(orb);

            DoubleAnimationUsingKeyFrames kx = new DoubleAnimationUsingKeyFrames();
            DoubleAnimationUsingKeyFrames ky = new DoubleAnimationUsingKeyFrames();

            // 阶段一：270° 从容太极旋舞（18 个平滑采样点，中段紧抱至 15*k，再优雅旋开）
            int orbitSteps = 18;
            double lastX = 0, lastY = 0;
            for (int i = 0; i <= orbitSteps; i++)
            {
                double t = (double)i / orbitSteps;
                double ang = phase0 + Math.PI * 1.45 * t;
                double rad = (15.0 + 40.0 * Math.Pow(2.0 * t - 1.0, 2.0)) * k;
                lastX = Math.Cos(ang) * rad;
                lastY = Math.Sin(ang) * rad;
                TimeSpan kt = TimeSpan.FromMilliseconds(920 * t);
                kx.KeyFrames.Add(new LinearDoubleKeyFrame(lastX, KeyTime.FromTimeSpan(kt)));
                ky.KeyFrames.Add(new LinearDoubleKeyFrame(lastY, KeyTime.FromTimeSpan(kt)));
            }

            // 阶段二：舒缓弧线奔赴目标按钮中心（920..1420ms）
            double tx = target.X - cx;
            double ty = target.Y - cy;
            double midX = (lastX + tx) * 0.5 + (tx < 0 ? -28 * k : 28 * k);
            double midY = (lastY + ty) * 0.5 - 20 * k;
            int flySteps = 10;
            for (int i = 1; i <= flySteps; i++)
            {
                double u = (double)i / flySteps;
                double eu = u < 0.5 ? 2 * u * u : 1 - Math.Pow(-2 * u + 2, 2) / 2.0;
                double px = (1 - eu) * (1 - eu) * lastX + 2 * (1 - eu) * eu * midX + eu * eu * tx;
                double py = (1 - eu) * (1 - eu) * lastY + 2 * (1 - eu) * eu * midY + eu * eu * ty;
                TimeSpan kt = TimeSpan.FromMilliseconds(920 + 500 * u);
                kx.KeyFrames.Add(new LinearDoubleKeyFrame(px, KeyTime.FromTimeSpan(kt)));
                ky.KeyFrames.Add(new LinearDoubleKeyFrame(py, KeyTime.FromTimeSpan(kt)));
            }
            tt.BeginAnimation(TranslateTransform.XProperty, kx);
            tt.BeginAnimation(TranslateTransform.YProperty, ky);

            DoubleAnimationUsingKeyFrames sx = new DoubleAnimationUsingKeyFrames();
            sx.KeyFrames.Add(new EasingDoubleKeyFrame(1.05, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(320)),
                new CubicEase { EasingMode = EasingMode.EaseOut }));
            sx.KeyFrames.Add(new LinearDoubleKeyFrame(0.98, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(920))));
            sx.KeyFrames.Add(new EasingDoubleKeyFrame(2.55, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(1440)),
                new CubicEase { EasingMode = EasingMode.EaseOut }));
            sc.BeginAnimation(ScaleTransform.ScaleXProperty, sx);

            DoubleAnimationUsingKeyFrames sy = new DoubleAnimationUsingKeyFrames();
            sy.KeyFrames.Add(new EasingDoubleKeyFrame(1.05, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(320)),
                new CubicEase { EasingMode = EasingMode.EaseOut }));
            sy.KeyFrames.Add(new LinearDoubleKeyFrame(0.98, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(920))));
            sy.KeyFrames.Add(new EasingDoubleKeyFrame(0.75, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(1440)),
                new CubicEase { EasingMode = EasingMode.EaseOut }));
            sc.BeginAnimation(ScaleTransform.ScaleYProperty, sy);

            DoubleAnimationUsingKeyFrames op = new DoubleAnimationUsingKeyFrames();
            op.KeyFrames.Add(new LinearDoubleKeyFrame(0.95, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(220))));
            op.KeyFrames.Add(new LinearDoubleKeyFrame(0.92, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(1220))));
            op.KeyFrames.Add(new EasingDoubleKeyFrame(0.0, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(1460)),
                new CubicEase { EasingMode = EasingMode.EaseOut }));
            orb.BeginAnimation(UIElement.OpacityProperty, op);
        }

        // 右上角朱砂星点弧线飘落到底部入口点
        void LaunchCinnabarSpark(Point from, Point to, int startMs, int durMs, double k, int idx)
        {
            double dia = 12 * k;
            Ellipse sp = new Ellipse();
            sp.Width = dia; sp.Height = dia;
            sp.Fill = Ink.InkWash(Ink.Verm(), 0.35);
            sp.Opacity = 0;
            sp.IsHitTestVisible = false;
            Canvas.SetLeft(sp, from.X - dia * 0.5);
            Canvas.SetTop(sp, from.Y - dia * 0.5);

            TranslateTransform tt = new TranslateTransform(0, 0);
            sp.RenderTransform = tt;
            veilLayer.Children.Add(sp);

            double dx = to.X - from.X;
            double dy = to.Y - from.Y;
            double ctrlX = dx * 0.55 - (22 + idx * 14) * k;
            double ctrlY = dy * 0.32;

            DoubleAnimationUsingKeyFrames kx = new DoubleAnimationUsingKeyFrames();
            DoubleAnimationUsingKeyFrames ky = new DoubleAnimationUsingKeyFrames();
            kx.BeginTime = TimeSpan.FromMilliseconds(startMs);
            ky.BeginTime = TimeSpan.FromMilliseconds(startMs);

            int steps = 10;
            for (int s = 1; s <= steps; s++)
            {
                double u = (double)s / steps;
                double eu = u < 0.5 ? 2 * u * u : 1 - Math.Pow(-2 * u + 2, 2) / 2.0;
                double px = 2 * (1 - eu) * eu * ctrlX + eu * eu * dx;
                double py = 2 * (1 - eu) * eu * ctrlY + eu * eu * dy;
                TimeSpan kt = TimeSpan.FromMilliseconds(durMs * u);
                kx.KeyFrames.Add(new LinearDoubleKeyFrame(px, KeyTime.FromTimeSpan(kt)));
                ky.KeyFrames.Add(new LinearDoubleKeyFrame(py, KeyTime.FromTimeSpan(kt)));
            }
            tt.BeginAnimation(TranslateTransform.XProperty, kx);
            tt.BeginAnimation(TranslateTransform.YProperty, ky);

            DoubleAnimationUsingKeyFrames op = new DoubleAnimationUsingKeyFrames();
            op.BeginTime = TimeSpan.FromMilliseconds(startMs);
            op.KeyFrames.Add(new LinearDoubleKeyFrame(0.95, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(60))));
            op.KeyFrames.Add(new LinearDoubleKeyFrame(0.85, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(durMs - 50))));
            op.KeyFrames.Add(new LinearDoubleKeyFrame(0.0, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(durMs + 20))));
            sp.BeginAnimation(UIElement.OpacityProperty, op);
        }

        void SpawnVeilRing(double x, double y, double dia, Color col, int delayMs, int durMs, double peakOp)
        {
            Ellipse r = new Ellipse();
            r.Width = dia; r.Height = dia;
            r.Fill = Ink.InkRing(col);
            r.Opacity = 0;
            r.IsHitTestVisible = false;
            Canvas.SetLeft(r, x - dia * 0.5);
            Canvas.SetTop(r, y - dia * 0.5);
            ScaleTransform st = new ScaleTransform(0.22, 0.22);
            r.RenderTransformOrigin = new Point(0.5, 0.5);
            r.RenderTransform = st;
            veilLayer.Children.Add(r);

            DoubleAnimation sc = new DoubleAnimation(0.22, 1.18, TimeSpan.FromMilliseconds(durMs));
            sc.BeginTime = TimeSpan.FromMilliseconds(delayMs);
            sc.EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut };
            st.BeginAnimation(ScaleTransform.ScaleXProperty, sc);
            st.BeginAnimation(ScaleTransform.ScaleYProperty, sc);

            DoubleAnimationUsingKeyFrames op = new DoubleAnimationUsingKeyFrames();
            op.BeginTime = TimeSpan.FromMilliseconds(delayMs);
            op.KeyFrames.Add(new LinearDoubleKeyFrame(peakOp, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(70))));
            op.KeyFrames.Add(new EasingDoubleKeyFrame(0.0, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(durMs)),
                new CubicEase { EasingMode = EasingMode.EaseOut }));
            r.BeginAnimation(UIElement.OpacityProperty, op);
        }

        // 逐帧导出：先跟开场动画 intro(14)，再跟 save(10) → rest(12)
        void StartShot()
        {
            string dir = MoShouApp.ShotDir;
            try
            {
                if (!System.IO.Directory.Exists(dir)) System.IO.Directory.CreateDirectory(dir);
            }
            catch { return; }

            int n = 0;
            DispatcherTimer t0 = new DispatcherTimer();
            t0.Interval = TimeSpan.FromMilliseconds(110);
            t0.Tick += delegate
            {
                ShotFrame(dir, "intro", n++);
                if (n < 14) return;
                t0.Stop();

                AnimSave();
                n = 0;
                DispatcherTimer t1 = new DispatcherTimer();
                t1.Interval = TimeSpan.FromMilliseconds(110);
                t1.Tick += delegate
                {
                    ShotFrame(dir, "save", n++);
                    if (n < 10) return;
                    t1.Stop();

                    AnimRestore();
                    n = 0;
                    DispatcherTimer t2 = new DispatcherTimer();
                    t2.Interval = TimeSpan.FromMilliseconds(110);
                    t2.Tick += delegate
                    {
                        ShotFrame(dir, "rest", n++);
                        if (n < 12) return;
                        t2.Stop();
                        Pause(150, delegate { Environment.Exit(0); });
                    };
                    t2.Start();
                };
                t1.Start();
            };
            t0.Start();
        }

        void ShotFrame(string dir, string tag, int i)
        {
            try
            {
                double w = ActualWidth > 0 ? ActualWidth : START_W;
                double h = ActualHeight > 0 ? ActualHeight : START_H;
                System.Windows.Media.Imaging.RenderTargetBitmap rtb =
                    new System.Windows.Media.Imaging.RenderTargetBitmap(
                        (int)Math.Ceiling(w), (int)Math.Ceiling(h), 96, 96,
                        System.Windows.Media.PixelFormats.Pbgra32);
                rtb.Render(rootLayer);

                System.Windows.Media.Imaging.PngBitmapEncoder enc =
                    new System.Windows.Media.Imaging.PngBitmapEncoder();
                enc.Frames.Add(System.Windows.Media.Imaging.BitmapFrame.Create(rtb));

                string p = System.IO.Path.Combine(dir, tag + "_" + i.ToString("00") + ".png");
                System.IO.FileStream fs = new System.IO.FileStream(p, System.IO.FileMode.Create);
                enc.Save(fs);
                fs.Close();
            }
            catch { }
        }

        void RefreshMeta()
        {
            try
            {
                // 桌面随时可能增删文件，每次刷新都重建显示名索引
                Ink.ResetIndex();
                snap = Engine.LoadSnapshot(root);

                tRes.Text = (snap != null) ? snap.ScreenW + " × " + snap.ScreenH : "";
                tScale.Text = (snap != null && snap.Scale > 0)
                              ? ((int)Math.Round(snap.Scale * 100)) + "%" : "";

                if (snap != null && snap.Items.Count > 0)
                {
                    // 常态只读记忆，不碰桌面。扫描只留在「落笔成记」那一下。
                    shown.Clear();
                    for (int i = 0; i < snap.Items.Count; i++) shown.Add(snap.Items[i]);
                }
                else
                {
                    // 第一次运行：还没有任何记忆，扫一次把现状摆出来
                    List<DeskIcon> live = Desk.Scan();
                    ScreenState sc = Desk.GetScreen();
                    shown.Clear();
                    if (live != null)
                        for (int i = 0; i < live.Count; i++) shown.Add(live[i]);

                    tRes.Text = sc.PhysW + " × " + sc.PhysH;
                    tScale.Text = sc.ScaleText;
                    if (tStatus != null) tStatus.Text = "首次运行 · 点「落子成记」记住当前桌面布局";
                }

                tCount.Text = shown.Count + " 项";
            }
            catch (Exception ex)
            {
                if (tStatus != null) tStatus.Text = "读取失败 · " + ex.Message;
            }

            if (tCalDate != null) tCalDate.Text = DateTime.Now.ToString("MM.dd");
        }

        // 按压缩列序算延迟：消除主区与收纳列之间的空等，且右侧两列保持先后落子节奏
        int DotColDelay(int i, int baseMs, int perCol)
        {
            int col = (i < dotColRank.Count) ? dotColRank[i] : 0;
            if (col < 0) col = 0;
            return baseMs + col * perCol;
        }

        // 按压缩行序算延迟：供「归位」自上而下扫线掠过时逐行浮现落定
        int DotRowDelay(int i, int baseMs, int perRow)
        {
            int row = (i < dotRowRank.Count) ? dotRowRank[i] : 0;
            if (row < 0) row = 0;
            return baseMs + row * perRow;
        }

        // ---------- 保存动画：双毫巡盘 · 棋子轻起落印 · 印谱定格 ----------
        void AnimSave()
        {
            if (gridLines.Count == 0 && dots.Count == 0) return;
            wetLayer.Children.Clear();
            sweepLayer.Children.Clear();

            // 1. 起格：坐标网格清晰浮现
            FadeGrid(1.0, 2.8, 220);

            // 2. 双毫巡盘：朱砂竖笔主幕 + 滞后柔墨光带自左向右扫谱
            DrawSaveScan();

            // 3. 逐列落印：棋子微提轻落、落点洇出同心水痕，模拟围棋落子入谱定格
            for (int i = 0; i < dots.Count; i++)
                StampDot(dots[i], DotColDelay(i, 100, 52), (i & 1) == 0);

            // 4. 收谱光环：画心漾开一圈淡朱墨定格涟漪
            if (bw > 20 && bh > 20)
            {
                Ellipse seal = new Ellipse();
                double sd = Math.Min(bw, bh) * 0.72;
                seal.Width = sd; seal.Height = sd;
                seal.Fill = Ink.InkRing(Ink.Verm());
                seal.Opacity = 0;
                seal.IsHitTestVisible = false;
                Canvas.SetLeft(seal, bw * 0.5 - sd * 0.5);
                Canvas.SetTop(seal, bh * 0.5 - sd * 0.5);
                ScaleTransform ss = new ScaleTransform(0.35, 0.35);
                seal.RenderTransformOrigin = new Point(0.5, 0.5);
                seal.RenderTransform = ss;
                sweepLayer.Children.Add(seal);

                DoubleAnimation sGrow = new DoubleAnimation(0.35, 1.38, TimeSpan.FromMilliseconds(560));
                sGrow.BeginTime = TimeSpan.FromMilliseconds(680);
                sGrow.EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut };
                ss.BeginAnimation(ScaleTransform.ScaleXProperty, sGrow);
                ss.BeginAnimation(ScaleTransform.ScaleYProperty, sGrow);

                DoubleAnimationUsingKeyFrames sOp = new DoubleAnimationUsingKeyFrames();
                sOp.BeginTime = TimeSpan.FromMilliseconds(680);
                sOp.KeyFrames.Add(new LinearDoubleKeyFrame(0.28, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(90))));
                sOp.KeyFrames.Add(new EasingDoubleKeyFrame(0.0, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(560)),
                    new CubicEase { EasingMode = EasingMode.EaseOut }));
                seal.BeginAnimation(UIElement.OpacityProperty, sOp);
            }

            // 5. 收格清场
            Pause(1340, delegate
            {
                FadeGrid(2.8, 1.0, 360);
                sweepLayer.Children.Clear();
                wetLayer.Children.Clear();
            });
        }

        // 双毫巡盘线：主朱砂笔 + 宽柔雾尾迹 + 滞后清墨副笔，自左向右从容扫过
        void DrawSaveScan()
        {
            if (bw <= 0 || bh <= 0) return;

            Canvas ribbon = new Canvas();
            ribbon.Width = 34;
            ribbon.Height = bh;
            ribbon.IsHitTestVisible = false;
            ribbon.Opacity = 0;

            Rectangle trail = new Rectangle();
            trail.Width = 30;
            trail.Height = bh - 10;
            Color vc = Ink.Verm();
            LinearGradientBrush lg = new LinearGradientBrush();
            lg.StartPoint = new Point(0, 0);
            lg.EndPoint = new Point(1, 0);
            lg.GradientStops.Add(new GradientStop(Color.FromArgb(0, vc.R, vc.G, vc.B), 0.0));
            lg.GradientStops.Add(new GradientStop(Color.FromArgb(Ink.Dark ? (byte)46 : (byte)36, vc.R, vc.G, vc.B), 0.84));
            lg.GradientStops.Add(new GradientStop(Color.FromArgb(0, vc.R, vc.G, vc.B), 1.0));
            trail.Fill = lg;
            Canvas.SetLeft(trail, 2);
            Canvas.SetTop(trail, 5);
            ribbon.Children.Add(trail);

            Rough.Pen(ribbon, 30, 6, 30, bh - 6, Ink.Verm(), 1.65, 0.72, 0.45, 0.30, 9200, null);
            Rough.Pen(ribbon, 24, 10, 24, bh - 10, Ink.Fg2(), 0.9, 0.28, 0.40, 0.25, 9205, null);

            TranslateTransform tx = new TranslateTransform(-32, 0);
            ribbon.RenderTransform = tx;
            sweepLayer.Children.Add(ribbon);

            DoubleAnimation ta = new DoubleAnimation(-32, bw + 10, TimeSpan.FromMilliseconds(720));
            ta.BeginTime = TimeSpan.FromMilliseconds(70);
            ta.EasingFunction = new CubicEase { EasingMode = EasingMode.EaseInOut };
            tx.BeginAnimation(TranslateTransform.XProperty, ta);

            DoubleAnimationUsingKeyFrames of = new DoubleAnimationUsingKeyFrames();
            of.KeyFrames.Add(new LinearDoubleKeyFrame(0.0, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(60))));
            of.KeyFrames.Add(new LinearDoubleKeyFrame(1.0, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(150))));
            of.KeyFrames.Add(new LinearDoubleKeyFrame(0.92, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(660))));
            of.KeyFrames.Add(new EasingDoubleKeyFrame(0.0,
                KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(840)),
                new CubicEase { EasingMode = EasingMode.EaseOut }));
            ribbon.BeginAnimation(UIElement.OpacityProperty, of);
        }

        // 单枚棋子「轻起落印」：随扫线微微提起再清脆落盘定格 + 交点洇出同心水痕
        void StampDot(Ellipse e, int delay, bool withRing)
        {
            if (!(e.RenderTransform is TransformGroup)) return;
            TransformGroup tg = (TransformGroup)e.RenderTransform;
            ScaleTransform sc = (ScaleTransform)tg.Children[0];
            TranslateTransform tt = (TranslateTransform)tg.Children[1];

            tt.BeginAnimation(TranslateTransform.XProperty, null);
            tt.BeginAnimation(TranslateTransform.YProperty, null);
            tt.X = 0;
            tt.Y = 0;
            e.BeginAnimation(UIElement.OpacityProperty, null);
            e.Opacity = 1.0;

            // 垂直轻起再落子
            DoubleAnimationUsingKeyFrames ty = new DoubleAnimationUsingKeyFrames();
            ty.BeginTime = TimeSpan.FromMilliseconds(delay);
            ty.KeyFrames.Add(new EasingDoubleKeyFrame(-4.2,
                KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(110)),
                new CubicEase { EasingMode = EasingMode.EaseOut }));
            ty.KeyFrames.Add(new EasingDoubleKeyFrame(0.0,
                KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(260)),
                new CubicEase { EasingMode = EasingMode.EaseIn }));
            tt.BeginAnimation(TranslateTransform.YProperty, ty);

            // 落盘回弹微胀
            DoubleAnimationUsingKeyFrames sk = new DoubleAnimationUsingKeyFrames();
            sk.BeginTime = TimeSpan.FromMilliseconds(delay);
            sk.KeyFrames.Add(new EasingDoubleKeyFrame(1.30,
                KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(110)),
                new CubicEase { EasingMode = EasingMode.EaseOut }));
            sk.KeyFrames.Add(new EasingDoubleKeyFrame(0.92,
                KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(250)),
                new CubicEase { EasingMode = EasingMode.EaseIn }));
            sk.KeyFrames.Add(new EasingDoubleKeyFrame(1.0,
                KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(390)),
                new CubicEase { EasingMode = EasingMode.EaseOut }));
            sc.BeginAnimation(ScaleTransform.ScaleXProperty, sk);
            sc.BeginAnimation(ScaleTransform.ScaleYProperty, sk);

            if (withRing) SpawnWetRing(e, delay + 230, 0.38);
        }

        // 在棋子交点处生成一枚柔边水痕环
        void SpawnWetRing(Ellipse e, int delay, double peakOp)
        {
            double rad = e.Width * 0.5;
            Ellipse ring = new Ellipse();
            ring.Width = rad * 2;
            ring.Height = rad * 2;
            if (e.Tag is Color) ring.Fill = Ink.SoftRing((Color)e.Tag);
            else ring.Fill = e.Fill;
            ring.IsHitTestVisible = false;
            ring.Opacity = 0;
            Canvas.SetLeft(ring, Canvas.GetLeft(e));
            Canvas.SetTop(ring, Canvas.GetTop(e));
            ScaleTransform rs = new ScaleTransform(0.42, 0.42);
            ring.RenderTransform = rs;
            ring.RenderTransformOrigin = new Point(0.5, 0.5);
            wetLayer.Children.Add(ring);

            DoubleAnimation rsx = new DoubleAnimation(0.42, 2.25, TimeSpan.FromMilliseconds(380));
            rsx.BeginTime = TimeSpan.FromMilliseconds(delay);
            rsx.EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut };
            rs.BeginAnimation(ScaleTransform.ScaleXProperty, rsx);

            DoubleAnimation rsy = new DoubleAnimation(0.42, 2.25, TimeSpan.FromMilliseconds(380));
            rsy.BeginTime = TimeSpan.FromMilliseconds(delay);
            rsy.EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut };
            rs.BeginAnimation(ScaleTransform.ScaleYProperty, rsy);

            DoubleAnimationUsingKeyFrames ro = new DoubleAnimationUsingKeyFrames();
            ro.BeginTime = TimeSpan.FromMilliseconds(delay);
            ro.KeyFrames.Add(new LinearDoubleKeyFrame(peakOp,
                KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(55))));
            ro.KeyFrames.Add(new EasingDoubleKeyFrame(0.0,
                KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(380)),
                new CubicEase { EasingMode = EasingMode.EaseOut }));
            ring.BeginAnimation(UIElement.OpacityProperty, ro);
        }

        // 按倍率淡入淡出：主笔副笔各自的底色不同，不能拉成同一档，否则手绘笔根会被抹平
        void FadeGrid(double kFrom, double kTo, int ms)
        {
            for (int i = 0; i < gridLines.Count; i++)
            {
                System.Windows.Shapes.Shape ln = gridLines[i];
                object lv = ln.ReadLocalValue(UIElement.OpacityProperty);
                double b = (lv is double) ? (double)lv : 0.13;

                DoubleAnimation a = new DoubleAnimation(
                    b * kFrom, b * kTo, TimeSpan.FromMilliseconds(ms));
                a.BeginTime = TimeSpan.FromMilliseconds(Math.Min(i * 6, 160));
                a.EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut };
                ln.BeginAnimation(UIElement.OpacityProperty, a);
            }
        }

        // ---------- 恢复动画：散乱偏移 → 亮格定标 → 牵引归回交点 ----------
        // 语义清晰、零随机、零时序冲突：
        //   0..260ms：所有棋子按确定性黄金角偏移散开（示意桌面被打乱）；
        //   220..420ms：坐标网格亮起（示意目标格位定标）；
        //   340..980ms：按列序顺滑滑回各自精确交点 (0,0) 并轻顿落定 + 洇出水痕。
        void AnimRestore()
        {
            if (dots.Count == 0) return;
            wetLayer.Children.Clear();
            sweepLayer.Children.Clear();

            double[] offX = new double[dots.Count];
            double[] offY = new double[dots.Count];

            // Phase 1 (0..260ms)：确定性微散偏移，展示「偏离原位」
            for (int i = 0; i < dots.Count; i++)
            {
                Ellipse e = dots[i];
                if (!(e.RenderTransform is TransformGroup)) continue;
                TransformGroup tg = (TransformGroup)e.RenderTransform;
                ScaleTransform sc = (ScaleTransform)tg.Children[0];
                TranslateTransform tt = (TranslateTransform)tg.Children[1];

                double ang = i * 2.399963; // 黄金角，分布均匀且 100% 确定
                double dist = 12.0 + (i % 3) * 4.5;
                offX[i] = Math.Cos(ang) * dist;
                offY[i] = Math.Sin(ang) * dist;

                IEasingFunction outEz = new CubicEase { EasingMode = EasingMode.EaseOut };
                DoubleAnimation ax = new DoubleAnimation(0, offX[i], TimeSpan.FromMilliseconds(250));
                ax.EasingFunction = outEz;
                DoubleAnimation ay = new DoubleAnimation(0, offY[i], TimeSpan.FromMilliseconds(250));
                ay.EasingFunction = outEz;
                tt.BeginAnimation(TranslateTransform.XProperty, ax);
                tt.BeginAnimation(TranslateTransform.YProperty, ay);

                DoubleAnimation asc = new DoubleAnimation(1.0, 0.80, TimeSpan.FromMilliseconds(250));
                asc.EasingFunction = outEz;
                sc.BeginAnimation(ScaleTransform.ScaleXProperty, asc);
                sc.BeginAnimation(ScaleTransform.ScaleYProperty, asc);

                DoubleAnimation aop = new DoubleAnimation(1.0, 0.48, TimeSpan.FromMilliseconds(250));
                aop.EasingFunction = outEz;
                e.BeginAnimation(UIElement.OpacityProperty, aop);
            }

            // Phase 2 (200ms)：网格亮起定标
            Pause(200, delegate { FadeGrid(1.0, 2.7, 220); });

            // Phase 3 (340ms，确保 Phase 1 已彻底结束，零动画冲突)：按列顺滑牵引归回精确交点 (0,0)
            Pause(340, delegate
            {
                for (int i = 0; i < dots.Count; i++)
                {
                    Ellipse e = dots[i];
                    if (!(e.RenderTransform is TransformGroup)) continue;
                    TransformGroup tg = (TransformGroup)e.RenderTransform;
                    ScaleTransform sc = (ScaleTransform)tg.Children[0];
                    TranslateTransform tt = (TranslateTransform)tg.Children[1];

                    int dl = DotColDelay(i, 0, 38);
                    IEasingFunction glideEz = new CubicEase { EasingMode = EasingMode.EaseInOut };
                    IEasingFunction lockEz = new BackEase { Amplitude = 0.14, EasingMode = EasingMode.EaseOut };

                    DoubleAnimation rx = new DoubleAnimation(offX[i], 0, TimeSpan.FromMilliseconds(380));
                    rx.BeginTime = TimeSpan.FromMilliseconds(dl);
                    rx.EasingFunction = glideEz;
                    DoubleAnimation ry = new DoubleAnimation(offY[i], 0, TimeSpan.FromMilliseconds(380));
                    ry.BeginTime = TimeSpan.FromMilliseconds(dl);
                    ry.EasingFunction = glideEz;
                    tt.BeginAnimation(TranslateTransform.XProperty, rx);
                    tt.BeginAnimation(TranslateTransform.YProperty, ry);

                    DoubleAnimation rsc = new DoubleAnimation(0.80, 1.0, TimeSpan.FromMilliseconds(380));
                    rsc.BeginTime = TimeSpan.FromMilliseconds(dl);
                    rsc.EasingFunction = lockEz;
                    sc.BeginAnimation(ScaleTransform.ScaleXProperty, rsc);
                    sc.BeginAnimation(ScaleTransform.ScaleYProperty, rsc);

                    DoubleAnimation rop = new DoubleAnimation(0.48, 1.0, TimeSpan.FromMilliseconds(320));
                    rop.BeginTime = TimeSpan.FromMilliseconds(dl);
                    rop.EasingFunction = new SineEase { EasingMode = EasingMode.EaseOut };
                    e.BeginAnimation(UIElement.OpacityProperty, rop);

                    // 每两枚棋子配一枚柔淡归位水痕，大幅降低并发时钟数，保证 60fps 丝滑不卡顿
                    if ((i & 1) == 0) SpawnWetRing(e, dl + 300, 0.28);
                }
            });

            // Phase 4 (1260ms)：网格退回基线，清场
            Pause(1260, delegate
            {
                FadeGrid(2.7, 1.0, 320);
                wetLayer.Children.Clear();
            });
        }

        // ---------- 换材质：墨滴洇开 / 换一张纸 ----------
        // 顺序硬约束：先让遮罩盖满 → 遮罩底下换内容 → 再揭开。
        void TogglePaper()
        {
            if (busy) return;
            busy = true;
            DisarmAll();
            if (draftDirty) ExitDraftUi();

            double w = ActualWidth > 0 ? ActualWidth : START_W;
            double h = ActualHeight > 0 ? ActualHeight : START_H;

            veilLayer.Children.Clear();
            veilLayer.Width = w;
            veilLayer.Height = h;

            if (!Ink.Dark) DropInk(w, h);
            else RollPaper(w, h);
        }

        // 宣纸 → 夜墨：先快后慢宣纸吃墨（QuarticEase Out）+ Catmull-Rom 真曲线，全程 ~1.22s
        void DropInk(double w, double h)
        {
            Point mp = Mouse.GetPosition(this);
            double cx = mp.X, cy = mp.Y;
            if (cx < 24 || cx > w - 24 || cy < 24 || cy > h - 24) { cx = w * 0.5; cy = h * 0.62; }

            double d = Math.Sqrt(w * w + h * h) * 2.25;
            double mn = Math.Min(w, h);
            Random rnd = new Random(6611);

            List<System.Windows.Shapes.Path> blobs = new List<System.Windows.Shapes.Path>();

            // 最底均压罩：540ms 铺满整窗，消除换树瞬间四角跳明度
            System.Windows.Shapes.Path dome = Blob(cx, cy, d * 1.32, 0, 0, 0.58, 1.0, 0, 520, Ink.C(0x11, 0x11, 0x0D));
            dome.Opacity = 1;
            DoubleAnimation domeIn = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(540));
            domeIn.EasingFunction = new SineEase { EasingMode = EasingMode.EaseInOut };
            dome.BeginAnimation(UIElement.OpacityProperty, domeIn);
            blobs.Add(dome);

            // 极淡墨雾底
            blobs.Add(Blob(cx, cy, d * 0.62, 0, 0, 0.68, 0.14, 0, 480, Ink.C(0x4A, 0x46, 0x36)));

            // 7 块不规则方向墨云：浓淡衔接均匀，不再出现孤立灰盘
            for (int i = 0; i < 7; i++)
            {
                double a = (i * 51.4) + (rnd.NextDouble() - 0.5) * 32;
                double off = mn * (0.16 + rnd.NextDouble() * 0.24);
                double bx = cx + Math.Cos(a * Math.PI / 180.0) * off;
                double by = cy + Math.Sin(a * Math.PI / 180.0) * off;
                double bd = mn * (0.48 + rnd.NextDouble() * 0.28);
                blobs.Add(Blob(bx, by, bd, 0, 0, 0.72, 0.44, 20 + i * 32, 460, Ink.C(0x1F, 0x1E, 0x16)));
            }

            // 中层过渡墨团（消除中心核与外围云之间的断层，绝不读成硬边黑贴纸）
            blobs.Add(Blob(cx, cy, mn * 0.76, -mn * 0.02, mn * 0.015, 0.52, 0.64, 10, 440, Ink.C(0x19, 0x18, 0x12)));
            blobs.Add(Blob(cx, cy, mn * 0.54, mn * 0.015, -mn * 0.01, 0.44, 0.88, 0, 400, Ink.C(0x13, 0x13, 0x0E)));

            // 560ms 罩刚好满 → 遮罩底下同步换树 → 停 60ms → 按浓度错位离心散开（520ms）
            Pause(560, delegate
            {
                Ink.Dark = true;
                SaveSettings();
                Rebuild(false);
                Pause(60, delegate
                {
                    for (int i = 0; i < blobs.Count; i++)
                    {
                        int dly = (int)(blobs[i].Opacity * 130) + 15;

                        DoubleAnimation f = new DoubleAnimation(0, TimeSpan.FromMilliseconds(460));
                        f.BeginTime = TimeSpan.FromMilliseconds(dly);
                        f.EasingFunction = new SineEase { EasingMode = EasingMode.EaseInOut };
                        blobs[i].BeginAnimation(UIElement.OpacityProperty, f);

                        ScaleTransform st = blobs[i].Tag as ScaleTransform;
                        if (st != null)
                        {
                            DoubleAnimation gx = new DoubleAnimation(1.16, TimeSpan.FromMilliseconds(520));
                            gx.BeginTime = TimeSpan.FromMilliseconds(dly);
                            gx.EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut };
                            st.BeginAnimation(ScaleTransform.ScaleXProperty, gx);
                            st.BeginAnimation(ScaleTransform.ScaleYProperty, gx);
                        }
                    }
                    Pause(640, delegate { veilLayer.Children.Clear(); busy = false; TrimMemory(); });
                });
            });
        }

        // 夜墨 → 宣纸：一整张新纸从上卷下来盖住，上缘带手绘微褶，按实际 DPI 栅格化
        void RollPaper(double w, double h)
        {
            Grid sheet = new Grid();
            sheet.Width = w;
            sheet.Height = h + 3;
            Canvas.SetLeft(sheet, 0);
            Canvas.SetTop(sheet, 0);

            TranslateTransform ty = new TranslateTransform(0, -(h + 3));
            sheet.RenderTransform = ty;

            Border paper = new Border();
            LinearGradientBrush bg = new LinearGradientBrush();
            GradientStopCollection gs = new GradientStopCollection();
            gs.Add(new GradientStop(Ink.C(0xC9, 0xBE, 0xA2), 0.000));
            gs.Add(new GradientStop(Ink.C(0xE2, 0xDA, 0xC6), 0.030));
            gs.Add(new GradientStop(Ink.C(0xF4, 0xF0, 0xE6), 0.085));
            gs.Add(new GradientStop(Ink.C(0xF4, 0xF0, 0xE6), 0.880));
            gs.Add(new GradientStop(Ink.C(0xEC, 0xE4, 0xD0), 0.940));
            gs.Add(new GradientStop(Ink.C(0xCE, 0xC3, 0xA7), 0.985));
            gs.Add(new GradientStop(Ink.C(0xB0, 0xA4, 0x88), 1.000));
            bg.GradientStops = gs;
            bg.StartPoint = new Point(0, 0);
            bg.EndPoint = new Point(0, 1);
            paper.Background = bg;
            sheet.Children.Add(paper);

            Canvas fibers = new Canvas();
            fibers.IsHitTestVisible = false;
            Random rnd = new Random(8800);
            for (int i = 0; i < 110; i++)
            {
                double fx = rnd.NextDouble() * w;
                double fy = rnd.NextDouble() * (h + 3);
                double fl = 4 + rnd.NextDouble() * 18;
                System.Windows.Shapes.Line fb = new System.Windows.Shapes.Line();
                fb.X1 = fx; fb.Y1 = fy;
                fb.X2 = fx + fl; fb.Y2 = fy + (rnd.NextDouble() - 0.5) * 2.2;
                fb.Stroke = Ink.Br(Ink.C(0xBF, 0xB4, 0x98));
                fb.StrokeThickness = 0.7;
                fb.Opacity = 0.05 + rnd.NextDouble() * 0.07;
                fibers.Children.Add(fb);
            }
            sheet.Children.Add(fibers);

            Canvas edges = new Canvas();
            edges.IsHitTestVisible = false;
            sheet.Children.Add(edges);
            Rough.Pen(edges, -6, 4.6, w + 6, 4.6,
                      Ink.C(0x8A, 0x7E, 0x62), 1.4, 0.9, 1.5, 0.9, 9500, null);
            Rough.Pen(edges, -6, h - 4.6, w + 6, h - 4.6,
                      Ink.C(0x57, 0x50, 0x3E), 1.6, 0.95, 1.5, 0.9, 9600, null);

            sheet.CacheMode = new BitmapCache(GetDpiScale());
            veilLayer.Children.Add(sheet);

            DoubleAnimation a = new DoubleAnimation(-(h + 3), 0, TimeSpan.FromMilliseconds(320));
            a.EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseInOut };

            a.Completed += delegate
            {
                Ink.Dark = false;
                SaveSettings();
                Rebuild(false);
                Pause(20, delegate
                {
                    DoubleAnimation outA = new DoubleAnimation(0, h + 3, TimeSpan.FromMilliseconds(230));
                    outA.EasingFunction = new CubicEase { EasingMode = EasingMode.EaseIn };
                    outA.Completed += delegate { veilLayer.Children.Clear(); busy = false; TrimMemory(); };
                    ty.BeginAnimation(TranslateTransform.YProperty, outA);
                });
            };

            ty.BeginAnimation(TranslateTransform.YProperty, a);
        }

        // 有机墨滴：Catmull-Rom 真曲线 + QuarticEase Out 宣纸毛细扩散（先快后慢）
        System.Windows.Shapes.Path Blob(double cx, double cy, double dia, double ox, double oy,
                                        double feather, double op, int delay, int durMs, Color c)
        {
            int seed = (int)(dia * 977.0) ^ (int)(cx * 31.0) ^ (int)(cy * 17.0) ^ delay;
            Random rr = new Random(seed);

            System.Windows.Shapes.Path e = new System.Windows.Shapes.Path();
            e.Data = Ink.OrganicBlobGeo(dia, seed, 0.068, 0.042, 0.020);
            e.Width = dia;
            e.Height = dia;

            if (feather > 0.001)
            {
                double inner = Math.Max(0.02, 1.0 - feather);
                Color solid = Color.FromArgb(255, c.R, c.G, c.B);
                GradientStopCollection gsr = new GradientStopCollection();
                gsr.Add(new GradientStop(solid, 0.0));
                gsr.Add(new GradientStop(solid, inner));
                gsr.Add(new GradientStop(Color.FromArgb(145, c.R, c.G, c.B), inner + (1.0 - inner) * 0.50));
                gsr.Add(new GradientStop(Color.FromArgb(0, c.R, c.G, c.B), 1.0));
                RadialGradientBrush rb = new RadialGradientBrush(gsr);
                rb.Center = new Point(0.5, 0.5);
                rb.GradientOrigin = new Point(0.5, 0.5);
                rb.RadiusX = 0.5;
                rb.RadiusY = 0.5;
                rb.Freeze();
                e.Fill = rb;
            }
            else
            {
                e.Fill = Ink.Br(c);
            }

            e.Opacity = op;
            e.RenderTransformOrigin = new Point(0.5, 0.5);
            e.IsHitTestVisible = false;

            ScaleTransform st = new ScaleTransform(0.01, 0.01);
            TransformGroup tg = new TransformGroup();
            tg.Children.Add(st);
            tg.Children.Add(new RotateTransform(rr.NextDouble() * 360.0));
            e.RenderTransform = tg;
            e.Tag = st;

            Canvas.SetLeft(e, cx - dia * 0.5 + ox);
            Canvas.SetTop(e, cy - dia * 0.5 + oy);
            veilLayer.Children.Add(e);

            QuarticEase ez = new QuarticEase { EasingMode = EasingMode.EaseOut };
            DoubleAnimation sx = new DoubleAnimation(0.01, 1.0, TimeSpan.FromMilliseconds(durMs));
            sx.BeginTime = TimeSpan.FromMilliseconds(delay);
            sx.EasingFunction = ez;
            DoubleAnimation sy = new DoubleAnimation(0.01, 1.0, TimeSpan.FromMilliseconds(durMs));
            sy.BeginTime = TimeSpan.FromMilliseconds(delay);
            sy.EasingFunction = ez;
            st.BeginAnimation(ScaleTransform.ScaleXProperty, sx);
            st.BeginAnimation(ScaleTransform.ScaleYProperty, sy);
            return e;
        }

        void Rebuild(bool animate)
        {
            stageLayer.Children.Clear();
            stageLayer.Children.Add(BuildUi());
            ApplyFit();

            // 必须同步落地：遮罩还盖着的时候把棋盘、日历、计数全部画完。
            UpdateLayout();
            RefreshMeta();
            DrawBoard();

            if (animate) AnimSave();
        }

        void Pause(int ms, Action act)
        {
            DispatcherTimer t = new DispatcherTimer();
            t.Interval = TimeSpan.FromMilliseconds(ms);
            t.Tick += delegate { t.Stop(); act(); };
            t.Start();
        }

        // ---------- 动作（扫描与写入均放后台线程，彻底杜绝点击按钮时 UI 线程卡顿） ----------
        void DoSave()
        {
            if (busy) return;
            DisarmAll();
            busy = true;
            if (tStatus != null) tStatus.Text = "正在扫描桌面坐标…";
            string targetRoot = root;
            System.Threading.ThreadPool.QueueUserWorkItem(delegate
            {
                try
                {
                    Snapshot s = Engine.Capture("落子 " + DateTime.Now.ToString("HH:mm"));
                    JsonStore.Save(System.IO.Path.Combine(targetRoot, "layout_snapshot.json"), s);
                    Engine.Backup(targetRoot, s);
                    Dispatcher.BeginInvoke(new Action(delegate
                    {
                        shown.Clear();
                        for (int i = 0; i < s.Items.Count; i++) shown.Add(s.Items[i]);
                        RefreshMeta();
                        DrawBoard();
                        AnimSave();
                        if (tStatus != null) tStatus.Text = "已落子成记 · " + s.Count + " 项 · " + s.ScreenW + "×" + s.ScreenH;
                        busy = false;
                    }));
                }
                catch (Exception ex)
                {
                    Dispatcher.BeginInvoke(new Action(delegate
                    {
                        if (tStatus != null) tStatus.Text = "保存失败 · " + ex.Message;
                        busy = false;
                    }));
                }
            });
        }

        void DoRestore()
        {
            if (busy) return;
            Snapshot s = Engine.LoadSnapshot(root);
            if (s == null) { if (tStatus != null) tStatus.Text = "还没有记忆，先点「落子成记」"; return; }

            busy = true;
            if (!armed)
            {
                if (tStatus != null) tStatus.Text = "正在比对桌面布局…";
                System.Threading.ThreadPool.QueueUserWorkItem(delegate
                {
                    try
                    {
                        List<DeskIcon> cur = Desk.Scan();
                        ScreenState sc = Desk.GetScreen();
                        DiffResult diff = Engine.Diff(s, cur, sc);
                        Dispatcher.BeginInvoke(new Action(delegate
                        {
                            busy = false;
                            if (diff.MoveIndices.Count == 0 && diff.NewOnes.Count == 0)
                            {
                                if (tStatus != null) tStatus.Text = "已与记忆一致，无需改动";
                                AnimRestore();
                                return;
                            }
                            pending = s;
                            armed = true;
                            btnRestore.Label.Text = "确认归位 (" + diff.MoveIndices.Count + ")";
                            btnRestore.SetPrimary(true);
                            if (tStatus != null) tStatus.Text = "需移动 " + diff.MoveIndices.Count + " 项 · 再点确认（Esc 取消）";
                        }));
                    }
                    catch (Exception ex)
                    {
                        Dispatcher.BeginInvoke(new Action(delegate
                        {
                            busy = false;
                            DisarmAll();
                            if (tStatus != null) tStatus.Text = "比对失败 · " + ex.Message;
                        }));
                    }
                });
                return;
            }

            // 二次确认：后台执行 Engine.Apply（含 6 轮 Sleep 迭代），绝不卡死按钮与窗口动画
            Snapshot snapToApply = pending;
            pending = null;
            armed = false;
            btnRestore.Label.Text = "归位中…";
            btnRestore.SetPrimary(false);
            if (tStatus != null) tStatus.Text = "正在将图标归回原位…";

            System.Threading.ThreadPool.QueueUserWorkItem(delegate
            {
                try
                {
                    bool blockedAuto = false;
                    int drift = 0;
                    int moved = ApplyDesktopExact(snapToApply, out blockedAuto, out drift);
                    Dispatcher.BeginInvoke(new Action(delegate
                    {
                        btnRestore.Label.Text = "归位";
                        if (blockedAuto)
                        {
                            if (tStatus != null)
                                tStatus.Text = "桌面已开启「自动排列图标」· 请右键桌面→查看→取消勾选「自动排列图标」后再归位";
                            busy = false;
                            return;
                        }
                        RefreshMeta();
                        AnimRestore();
                        if (tStatus != null)
                        {
                            if (moved <= 0) tStatus.Text = "已与记忆一致";
                            else if (drift > 0) tStatus.Text = "已归位 " + moved + " 项（仍有 " + drift + " 项受系统网格微调）";
                            else tStatus.Text = "已精准归位 · 共移动 " + moved + " 项";
                        }
                        busy = false;
                    }));
                }
                catch (Exception ex)
                {
                    Dispatcher.BeginInvoke(new Action(delegate
                    {
                        DisarmAll();
                        if (tStatus != null) tStatus.Text = "归位失败 · " + ex.Message;
                        busy = false;
                    }));
                }
            });
        }

        void DoOpenFolder(string targetDir)
        {
            try
            {
                string d = string.IsNullOrEmpty(targetDir) ? root : targetDir;
                if (!Directory.Exists(d)) Directory.CreateDirectory(d);
                System.Diagnostics.ProcessStartInfo psi = new System.Diagnostics.ProcessStartInfo();
                psi.FileName = "explorer.exe";
                // 引号包裹路径，100% 兼容含空格与中文的任意路径
                psi.Arguments = "\"" + d + "\"";
                psi.UseShellExecute = true;
                System.Diagnostics.Process.Start(psi);
            }
            catch (Exception ex) { if (tStatus != null) tStatus.Text = "打开失败 · " + ex.Message; }
        }
    }

    // ============ 程序入口 ============
    public class MoShouApp : Application
    {
        public static bool Demo = false;
        // --shot <dir>：把动画逐帧渲染成 PNG 后退出。WPF 自己渲染，不受窗口遮挡与焦点影响，
        // 是唯一能在「用户正用着电脑」时可靠验证动画时序的通道。
        public static string ShotDir = null;

        [STAThread]
        public static void Main(string[] args)
        {
            Desk.EnablePerMonitorDpi();

            if (args != null && Array.IndexOf(args, "--demo") >= 0) Demo = true;

            if (args != null && Array.IndexOf(args, "--shot") >= 0)
            {
                int k = Array.IndexOf(args, "--shot");
                if (k + 1 < args.Length) ShotDir = args[k + 1];
            }

            MoShouApp app = new MoShouApp();
            MoShouWindow win = new MoShouWindow();

            app.DispatcherUnhandledException += delegate (object s, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
            {
                try
                {
                    string appDir = System.IO.Path.Combine(
                        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "MoShou");
                    if (!Directory.Exists(appDir)) Directory.CreateDirectory(appDir);
                    string p = System.IO.Path.Combine(appDir, "运行时错误.log");
                    System.IO.File.AppendAllText(p,
                        DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "\r\n" +
                        e.Exception.ToString() + "\r\n\r\n", System.Text.Encoding.UTF8);
                }
                catch { }
                e.Handled = true;
            };

            app.Run(win);
        }
    }
}
