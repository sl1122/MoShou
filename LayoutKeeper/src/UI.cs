// LayoutKeeper - 桌面布局管家 · 界面层
// 纯代码 WPF（无 XAML），墨夜水墨风格：宣纸白字、朱砂印章、青墨主按钮、spring 动效
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace LayoutKeeper
{
    // ---------- 调色板：墨夜 ----------
    static class Palette
    {
        public static readonly Color BgTop = Color.FromRgb(0x26, 0x24, 0x20);      // 宣纸墨（暖）
        public static readonly Color BgBottom = Color.FromRgb(0x0C, 0x0B, 0x0A);   // 焦墨
        public static readonly Color Card = Color.FromArgb(0x16, 0xF2, 0xEC, 0xDD);      // 宣纸 8%
        public static readonly Color CardBorder = Color.FromArgb(0x22, 0xF2, 0xEC, 0xDD);
        public static readonly Color Ink = Color.FromRgb(0x33, 0x50, 0x5A);       // 青墨（主按钮亮）
        public static readonly Color InkDark = Color.FromRgb(0x1F, 0x33, 0x39);   // 青墨（主按钮暗）
        public static readonly Color Cinnabar = Color.FromRgb(0xB8, 0x48, 0x3C);  // 朱砂（印章）
        public static readonly Color CinnabarDark = Color.FromRgb(0x8E, 0x30, 0x27);
        public static readonly Color Ochre = Color.FromRgb(0xC8, 0x8A, 0x3D);      // 赭石（待移动）
        public static readonly Color Moss = Color.FromRgb(0x7E, 0x9A, 0x6E);       // 青苔（成功）
        public static readonly Color Paper = Color.FromRgb(0xED, 0xE8, 0xDE);      // 宣纸白
        public static readonly Color Ash = Color.FromRgb(0x8C, 0x8A, 0x82);        // 淡墨灰

        public static Brush Solid(Color c) { return new SolidColorBrush(c); }

        public static Brush Vert(Color a, Color b)
        {
            LinearGradientBrush g = new LinearGradientBrush();
            g.StartPoint = new Point(0, 0);
            g.EndPoint = new Point(0, 1);
            g.GradientStops.Add(new GradientStop(a, 0));
            g.GradientStops.Add(new GradientStop(b, 1));
            return g;
        }

        public static Brush Diag(Color a, Color b)
        {
            LinearGradientBrush g = new LinearGradientBrush();
            g.StartPoint = new Point(0, 0);
            g.EndPoint = new Point(1, 1);
            g.GradientStops.Add(new GradientStop(a, 0));
            g.GradientStops.Add(new GradientStop(b, 1));
            return g;
        }

        public static FontFamily Sans { get { return new FontFamily("Microsoft YaHei UI, Segoe UI"); } }
        public static FontFamily Serif { get { return new FontFamily("SimSun, Microsoft YaHei UI, Segoe UI"); } }
    }

    // ---------- 按钮 ----------
    class GlassButton : Border
    {
        public TextBlock Label;
        public TextBlock Sub;
        public ScaleTransform Scale;
        public bool IsPrimary;

        public GlassButton(string text, bool primary)
        {
            IsPrimary = primary;
            CornerRadius = new CornerRadius(12);
            Height = 54;
            VerticalAlignment = VerticalAlignment.Center;

            Scale = new ScaleTransform(1, 1);
            RenderTransformOrigin = new Point(0.5, 0.5);
            RenderTransform = Scale;
            Cursor = Cursors.Hand;

            Label = new TextBlock();
            Label.Text = text;
            Label.FontFamily = Palette.Sans;
            Label.FontSize = 14.5;
            Label.FontWeight = FontWeights.SemiBold;
            Label.HorizontalAlignment = HorizontalAlignment.Center;
            Label.VerticalAlignment = VerticalAlignment.Center;
            Label.Foreground = new SolidColorBrush(Palette.Paper);

            Sub = new TextBlock();
            Sub.FontFamily = Palette.Sans;
            Sub.FontSize = 10.5;
            Sub.Visibility = Visibility.Collapsed;
            Sub.HorizontalAlignment = HorizontalAlignment.Center;
            Sub.Margin = new Thickness(0, 34, 0, 0);
            Sub.Foreground = new SolidColorBrush(Color.FromArgb(0xB0, 0xFF, 0xFF, 0xFF));

            StackPanel sp = new StackPanel();
            sp.Children.Add(Label);
            sp.Children.Add(Sub);
            Child = sp;

            ApplyVisual();
            Wire();
        }

        void ApplyVisual()
        {
            if (IsPrimary)
            {
                Background = Palette.Diag(Color.FromRgb(0x44, 0x63, 0x6E), Palette.InkDark);
                BorderBrush = new SolidColorBrush(Color.FromArgb(0x3A, 0xF2, 0xEC, 0xDD));
                BorderThickness = new Thickness(1);
                Effect = Shadow(0x44, Colors.Black, 16, 4);
            }
            else
            {
                Background = new SolidColorBrush(Palette.Card);
                BorderBrush = new SolidColorBrush(Palette.CardBorder);
                BorderThickness = new Thickness(1);
                Effect = Shadow(0x28, Colors.Black, 10, 2);
            }
        }

        static DropShadowEffect Shadow(byte alpha, Color color, double blur, double depth)
        {
            DropShadowEffect e = new DropShadowEffect();
            e.Color = color;
            e.Opacity = alpha / 255.0;
            e.BlurRadius = blur;
            e.ShadowDepth = depth;
            e.Direction = 270;
            return e;
        }

        void Wire()
        {
            MouseEnter += delegate
            {
                Animate(1.022, 220, false);
                if (!IsPrimary) Background = new SolidColorBrush(Color.FromArgb(0x24, 0xF2, 0xEC, 0xDD));
            };
            MouseLeave += delegate
            {
                Animate(1.0, 260, false);
                ApplyVisual();
            };
            MouseLeftButtonDown += delegate (object s, MouseButtonEventArgs e) { Animate(0.958, 110, false); };
            MouseLeftButtonUp += delegate (object s, MouseButtonEventArgs e) { Animate(1.018, 340, true); };
        }

        public void Animate(double to, int ms, bool spring)
        {
            DoubleAnimation a = new DoubleAnimation(to, TimeSpan.FromMilliseconds(ms));
            a.EasingFunction = spring ? (IEasingFunction)new BackEase() { Amplitude = 0.6, EasingMode = EasingMode.EaseOut }
                                      : (IEasingFunction)new CubicEase() { EasingMode = EasingMode.EaseOut };
            Scale.BeginAnimation(ScaleTransform.ScaleXProperty, a);
            Scale.BeginAnimation(ScaleTransform.ScaleYProperty, a);
        }

        public void SetPrimary(bool primary)
        {
            IsPrimary = primary;
            ApplyVisual();
        }
    }

    // ---------- 主窗口 ----------
    public class MainWindow : Window
    {
        string root;

        TextBlock txtRes;
        TextBlock txtScale;
        TextBlock txtIcons;
        TextBlock txtStatus;
        TextBlock txtSnapTime;

        Canvas preview;
        Border previewHost;

        GlassButton btnSave;
        GlassButton btnRestore;
        GlassButton btnCompact;
        GlassButton btnOpenFolder;

        Snapshot pending = null;
        bool restoringArmed = false;
        ScreenState lastScreen;

        public MainWindow()
        {
            root = ResolveRoot();

            Title = "布局管家";
            Width = 462;
            Height = 712;
            WindowStyle = WindowStyle.None;
            ResizeMode = ResizeMode.NoResize;
            AllowsTransparency = true;
            Background = Brushes.Transparent;
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
            ShowInTaskbar = true;
            FontFamily = Palette.Sans;

            Content = BuildUi();
            Loaded += OnLoaded;

            // 快捷键：Ctrl+S 保存 / Ctrl+R 恢复 / Ctrl+T 紧致 / Esc 关闭
            KeyDown += delegate (object s, KeyEventArgs e)
            {
                ModifierKeys mk = Keyboard.Modifiers;
                bool ctrl = (mk & ModifierKeys.Control) == ModifierKeys.Control;

                if (ctrl && e.Key == Key.S) { DoSave(); e.Handled = true; }
                else if (ctrl && e.Key == Key.R) { DoRestore(); e.Handled = true; }
                else if (ctrl && e.Key == Key.T) { DoCompact(); e.Handled = true; }
                else if (e.Key == Key.Escape) { Close(); }
            };

            // 拖动后把窗口夹在屏幕内，避免被拖出可视区域
            bool clamping = false;
            LocationChanged += delegate
            {
                if (clamping) return;
                clamping = true;
                try
                {
                    double waW = SystemParameters.WorkArea.Width;
                    double waH = SystemParameters.WorkArea.Height;
                    double l = Left;
                    double t = Top;
                    if (l < -8) l = 0;
                    if (t < -8) t = 0;
                    if (l > waW - 120) l = waW - 120;
                    if (t > waH - 80) t = waH - 80;
                    if (l != Left) Left = l;
                    if (t != Top) Top = t;
                }
                finally { clamping = false; }
            };

            Refresh();
        }

        static DropShadowEffect Shadow(byte alpha, Color color, double blur, double depth)
        {
            DropShadowEffect e = new DropShadowEffect();
            e.Color = color;
            e.Opacity = alpha / 255.0;
            e.BlurRadius = blur;
            e.ShadowDepth = depth;
            e.Direction = 270;
            return e;
        }

        string ResolveRoot()
        {
            string exeDir = AppDomain.CurrentDomain.BaseDirectory.TrimEnd('\\');
            string parent = System.IO.Path.GetDirectoryName(exeDir);
            if (parent != null && System.IO.File.Exists(System.IO.Path.Combine(parent, "layout_snapshot.json")))
                return parent;
            return exeDir;
        }

        // ---------- 界面搭建 ----------
        UIElement BuildUi()
        {
            Border shell = new Border();
            shell.CornerRadius = new CornerRadius(24);
            shell.Background = Palette.Vert(Palette.BgTop, Palette.BgBottom);
            shell.BorderBrush = new SolidColorBrush(Color.FromArgb(0x20, 0xF2, 0xEC, 0xDD));
            shell.BorderThickness = new Thickness(1);
            shell.Margin = new Thickness(16);
            shell.ClipToBounds = true;

            DropShadowEffect sh = new DropShadowEffect();
            sh.Color = Colors.Black;
            sh.Opacity = 0.6;
            sh.BlurRadius = 36;
            sh.ShadowDepth = 14;
            shell.Effect = sh;

            Grid stack = new Grid();

            // 水墨晕染层（不影响交互）
            stack.Children.Add(InkHalo());
            Grid inner = new Grid();
            inner.RowDefinitions.Add(Row(88));   // 头部
            inner.RowDefinitions.Add(Row(78));   // 状态卡
            inner.RowDefinitions.Add(Row(258));  // 预览
            inner.RowDefinitions.Add(Row(216));  // 按钮
            inner.RowDefinitions.Add(Row(44));   // 底部

            inner.Children.Add(Header());

            Border cards = StatusCards();
            Grid.SetRow(cards, 1);
            inner.Children.Add(cards);

            Border pv = PreviewArea();
            Grid.SetRow(pv, 2);
            inner.Children.Add(pv);

            StackPanel actions = ActionBar();
            Grid.SetRow(actions, 3);
            inner.Children.Add(actions);

            TextBlock foot = new TextBlock();
            foot.Text = "只调整图标位置 · 不移动、不改名、不删除文件";
            foot.FontSize = 10.5;
            foot.Foreground = new SolidColorBrush(Palette.Ash);
            foot.HorizontalAlignment = HorizontalAlignment.Center;
            foot.VerticalAlignment = VerticalAlignment.Center;
            Grid.SetRow(foot, 4);
            inner.Children.Add(foot);

            stack.Children.Add(inner);
            shell.Child = stack;

            Grid outer = new Grid();
            outer.Background = Brushes.Transparent;
            outer.Children.Add(shell);
            outer.Children.Add(CloseButton());
            return outer;
        }

        UIElement CloseButton()
        {
            Border b = new Border();
            b.Width = 26;
            b.Height = 26;
            b.CornerRadius = new CornerRadius(13);
            b.HorizontalAlignment = HorizontalAlignment.Right;
            b.VerticalAlignment = VerticalAlignment.Top;
            b.Margin = new Thickness(0, 30, 34, 0);
            b.Background = Brushes.Transparent;
            b.Cursor = Cursors.Hand;

            TextBlock t = new TextBlock();
            t.Text = "✕";
            t.FontSize = 12;
            t.FontFamily = Palette.Sans;
            t.Foreground = new SolidColorBrush(Color.FromArgb(0xFF, 0x8C, 0x8A, 0x82));
            t.HorizontalAlignment = HorizontalAlignment.Center;
            t.VerticalAlignment = VerticalAlignment.Center;
            b.Child = t;

            b.MouseEnter += delegate
            {
                b.Background = new SolidColorBrush(Color.FromArgb(0x2A, 0xB8, 0x48, 0x3C));
                t.Foreground = new SolidColorBrush(Palette.Paper);
            };
            b.MouseLeave += delegate
            {
                b.Background = Brushes.Transparent;
                t.Foreground = new SolidColorBrush(Color.FromArgb(0xFF, 0x8C, 0x8A, 0x82));
            };
            b.MouseLeftButtonDown += delegate (object s, MouseButtonEventArgs e) { e.Handled = true; };
            b.MouseLeftButtonUp += delegate (object s, MouseButtonEventArgs e) { e.Handled = true; Close(); };
            return b;
        }

        // 水墨晕染层：右上淡白水痕 + 左下浓墨积墨
        static UIElement InkHalo()
        {
            Grid halo = new Grid();
            halo.IsHitTestVisible = false;

            Ellipse e1 = new Ellipse();
            e1.Width = 420;
            e1.Height = 300;
            e1.HorizontalAlignment = HorizontalAlignment.Right;
            e1.VerticalAlignment = VerticalAlignment.Top;
            e1.Margin = new Thickness(0, -120, -140, 0);
            RadialGradientBrush rg1 = new RadialGradientBrush();
            rg1.GradientStops.Add(new GradientStop(Color.FromArgb(0x22, 0xFF, 0xFB, 0xF0), 0));
            rg1.GradientStops.Add(new GradientStop(Color.FromArgb(0x00, 0xFF, 0xFB, 0xF0), 1));
            e1.Fill = rg1;
            halo.Children.Add(e1);

            Ellipse e2 = new Ellipse();
            e2.Width = 380;
            e2.Height = 320;
            e2.HorizontalAlignment = HorizontalAlignment.Left;
            e2.VerticalAlignment = VerticalAlignment.Bottom;
            e2.Margin = new Thickness(-130, 0, 0, -150);
            RadialGradientBrush rg2 = new RadialGradientBrush();
            rg2.GradientStops.Add(new GradientStop(Color.FromArgb(0x44, 0x00, 0x00, 0x00), 0));
            rg2.GradientStops.Add(new GradientStop(Color.FromArgb(0x00, 0x00, 0x00, 0x00), 1));
            e2.Fill = rg2;
            halo.Children.Add(e2);

            return halo;
        }

        static RowDefinition Row(double h)
        {
            RowDefinition r = new RowDefinition();
            r.Height = new GridLength(h);
            return r;
        }

        UIElement Header()
        {
            Grid h = new Grid();
            h.Background = Brushes.Transparent;
            h.Margin = new Thickness(26, 20, 26, 6);
            h.MouseLeftButtonDown += delegate (object s, MouseButtonEventArgs e) { try { DragMove(); } catch { } };
            h.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(52) });
            h.ColumnDefinitions.Add(new ColumnDefinition());

            // 朱砂印章
            Border seal = new Border();
            seal.Width = 44;
            seal.Height = 44;
            seal.CornerRadius = new CornerRadius(8);
            seal.HorizontalAlignment = HorizontalAlignment.Left;
            seal.VerticalAlignment = VerticalAlignment.Top;
            seal.Background = new SolidColorBrush(Palette.Cinnabar);
            seal.BorderBrush = new SolidColorBrush(Color.FromArgb(0x60, 0xFF, 0xFF, 0xFF));
            seal.BorderThickness = new Thickness(1);
            seal.Effect = Shadow(0x50, Color.FromRgb(0x60, 0x14, 0x10), 12, 3);

            TextBlock sealTxt = new TextBlock();
            sealTxt.Text = "布";
            sealTxt.FontFamily = Palette.Serif;
            sealTxt.FontSize = 21;
            sealTxt.FontWeight = FontWeights.Bold;
            sealTxt.Foreground = new SolidColorBrush(Color.FromArgb(0xF2, 0xFF, 0xF3, 0xEC));
            sealTxt.HorizontalAlignment = HorizontalAlignment.Center;
            sealTxt.VerticalAlignment = VerticalAlignment.Center;
            seal.Child = sealTxt;

            StackPanel txt = new StackPanel();
            txt.Margin = new Thickness(14, 0, 0, 0);
            txt.VerticalAlignment = VerticalAlignment.Top;

            TextBlock t1 = new TextBlock();
            t1.Text = "布局管家";
            t1.FontFamily = Palette.Serif;
            t1.FontSize = 22;
            t1.FontWeight = FontWeights.Bold;
            t1.Foreground = new SolidColorBrush(Palette.Paper);
            t1.LineHeight = 30;

            TextBlock t2 = new TextBlock();
            t2.Text = "LayoutKeeper · 一键记住你的桌面";
            t2.FontSize = 11;
            t2.Margin = new Thickness(0, 4, 0, 0);
            t2.Foreground = new SolidColorBrush(Palette.Ash);

            txt.Children.Add(t1);
            txt.Children.Add(t2);

            Grid.SetColumn(seal, 0);
            Grid.SetColumn(txt, 1);
            h.Children.Add(seal);
            h.Children.Add(txt);
            return h;
        }

        Border StatusCards()
        {
            Border host = new Border();
            host.Margin = new Thickness(26, 6, 26, 6);
            host.CornerRadius = new CornerRadius(14);
            host.Background = new SolidColorBrush(Palette.Card);
            host.BorderBrush = new SolidColorBrush(Palette.CardBorder);
            host.BorderThickness = new Thickness(1);
            host.Padding = new Thickness(12, 8, 12, 8);

            Grid row = new Grid();
            row.ColumnDefinitions.Add(new ColumnDefinition());
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1) });
            row.ColumnDefinitions.Add(new ColumnDefinition());
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1) });
            row.ColumnDefinitions.Add(new ColumnDefinition());

            txtRes = Metric("分辨率", "--");
            txtScale = Metric("缩放", "--");
            txtIcons = Metric("图标", "--");

            row.Children.Add(Wrap(txtRes, 0));
            row.Children.Add(Divider(1));
            row.Children.Add(Wrap(txtScale, 2));
            row.Children.Add(Divider(3));
            row.Children.Add(Wrap(txtIcons, 4));

            host.Child = row;
            return host;
        }

        static UIElement Divider(int col)
        {
            Border b = new Border();
            b.Width = 1;
            b.Background = new SolidColorBrush(Color.FromArgb(0x14, 0xF2, 0xEC, 0xDD));
            b.Margin = new Thickness(0, 8, 0, 8);
            Grid.SetColumn(b, col);
            return b;
        }

        static UIElement Wrap(TextBlock tb, int col)
        {
            StackPanel sp = (StackPanel)tb.Tag;
            Grid.SetColumn(sp, col);
            return sp;
        }

        TextBlock Metric(string label, string value)
        {
            StackPanel sp = new StackPanel();
            sp.HorizontalAlignment = HorizontalAlignment.Center;
            sp.VerticalAlignment = VerticalAlignment.Center;

            TextBlock v = new TextBlock();
            v.Text = value;
            v.FontSize = 16;
            v.FontWeight = FontWeights.SemiBold;
            v.LineHeight = 22;
            v.HorizontalAlignment = HorizontalAlignment.Center;
            v.Foreground = new SolidColorBrush(Palette.Paper);

            TextBlock l = new TextBlock();
            l.Text = label;
            l.FontSize = 10;
            l.LineHeight = 14;
            l.HorizontalAlignment = HorizontalAlignment.Center;
            l.Margin = new Thickness(0, 3, 0, 0);
            l.Foreground = new SolidColorBrush(Palette.Ash);

            sp.Children.Add(v);
            sp.Children.Add(l);
            v.Tag = sp;
            return v;
        }

        Border PreviewArea()
        {
            Border host = new Border();
            host.Margin = new Thickness(26, 6, 26, 8);

            Grid g = new Grid();
            g.RowDefinitions.Add(new RowDefinition { Height = new GridLength(24) });
            g.RowDefinitions.Add(new RowDefinition());

            Grid headLine = new Grid();
            TextBlock lbl = new TextBlock();
            lbl.Text = "桌面预览";
            lbl.FontSize = 11;
            lbl.Foreground = new SolidColorBrush(Palette.Ash);
            lbl.VerticalAlignment = VerticalAlignment.Center;

            txtSnapTime = new TextBlock();
            txtSnapTime.Text = "尚无快照";
            txtSnapTime.FontSize = 10.5;
            txtSnapTime.HorizontalAlignment = HorizontalAlignment.Right;
            txtSnapTime.VerticalAlignment = VerticalAlignment.Center;
            txtSnapTime.Foreground = new SolidColorBrush(Palette.Ash);

            headLine.Children.Add(lbl);
            headLine.Children.Add(txtSnapTime);
            Grid.SetRow(headLine, 0);
            g.Children.Add(headLine);

            previewHost = new Border();
            previewHost.CornerRadius = new CornerRadius(12);
            previewHost.Background = new SolidColorBrush(Color.FromRgb(0x08, 0x09, 0x0A));
            previewHost.BorderBrush = new SolidColorBrush(Color.FromArgb(0x1C, 0xF2, 0xEC, 0xDD));
            previewHost.BorderThickness = new Thickness(1);
            previewHost.ClipToBounds = true;
            Grid.SetRow(previewHost, 1);
            g.Children.Add(previewHost);

            preview = new Canvas();
            preview.ClipToBounds = true;
            previewHost.Child = preview;

            host.Child = g;
            return host;
        }

        StackPanel ActionBar()
        {
            StackPanel sp = new StackPanel();
            sp.Margin = new Thickness(26, 4, 26, 2);

            btnSave = new GlassButton("保存当前布局", true);
            btnSave.Margin = new Thickness(0, 0, 0, 9);
            btnSave.MouseLeftButtonUp += delegate { DoSave(); };

            btnRestore = new GlassButton("恢复布局", false);
            btnRestore.Margin = new Thickness(0, 0, 0, 9);
            btnRestore.MouseLeftButtonUp += delegate { DoRestore(); };

            Grid bottomRow = new Grid();
            bottomRow.ColumnDefinitions.Add(new ColumnDefinition());
            bottomRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(9) });
            bottomRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(140) });

            btnCompact = new GlassButton("紧致补洞", false);
            btnCompact.MouseLeftButtonUp += delegate { DoCompact(); };

            btnOpenFolder = new GlassButton("打开目录", false);
            btnOpenFolder.MouseLeftButtonUp += delegate { DoOpenFolder(); };

            Grid.SetColumn(btnCompact, 0);
            Grid.SetColumn(btnOpenFolder, 2);
            bottomRow.Children.Add(btnCompact);
            bottomRow.Children.Add(btnOpenFolder);

            sp.Children.Add(btnSave);
            sp.Children.Add(btnRestore);
            sp.Children.Add(bottomRow);

            txtStatus = new TextBlock();
            txtStatus.Text = "";
            txtStatus.FontSize = 11;
            txtStatus.TextAlignment = TextAlignment.Center;
            txtStatus.Margin = new Thickness(0, 9, 0, 0);
            txtStatus.Foreground = new SolidColorBrush(Palette.Ash);
            sp.Children.Add(txtStatus);

            return sp;
        }

        // ---------- 动效 ----------
        void OnLoaded(object sender, RoutedEventArgs e)
        {
            DoubleAnimation fade = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(400));
            fade.EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut };
            BeginAnimation(OpacityProperty, fade);
        }

        void Pulse(UIElement el)
        {
            if (!(el is FrameworkElement)) return;
            FrameworkElement fe = (FrameworkElement)el;
            ScaleTransform st = new ScaleTransform(1, 1);
            fe.RenderTransformOrigin = new Point(0.5, 0.5);
            fe.RenderTransform = st;

            DoubleAnimation grow = new DoubleAnimation(1, 1.035, TimeSpan.FromMilliseconds(140));
            DoubleAnimation back = new DoubleAnimation(1.035, 1, TimeSpan.FromMilliseconds(330));
            back.EasingFunction = new BackEase { Amplitude = 0.7, EasingMode = EasingMode.EaseOut };
            grow.Completed += delegate { st.BeginAnimation(ScaleTransform.ScaleXProperty, back); st.BeginAnimation(ScaleTransform.ScaleYProperty, back); };
            grow.EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut };
            st.BeginAnimation(ScaleTransform.ScaleXProperty, grow);
            st.BeginAnimation(ScaleTransform.ScaleYProperty, grow);
        }

        void Status(string text, Color color)
        {
            txtStatus.Text = text;
            txtStatus.Foreground = new SolidColorBrush(color);
            DoubleAnimation a = new DoubleAnimation(0.35, 1, TimeSpan.FromMilliseconds(260));
            txtStatus.BeginAnimation(OpacityProperty, a);
        }

        // ---------- 数据刷新 ----------
        void Refresh()
        {
            try
            {
                lastScreen = Desk.GetScreen();
                List<DeskIcon> items = Desk.Scan();

                txtRes.Text = lastScreen.PhysW + "×" + lastScreen.PhysH;
                txtScale.Text = lastScreen.ScaleText;
                txtIcons.Text = items.Count + " 项";

                DrawPreview(items, null);
            }
            catch (Exception ex)
            {
                Status("读取桌面失败：" + ex.Message, Palette.Ochre);
            }

            Snapshot snap = Engine.LoadSnapshot(root);
            if (snap != null && !string.IsNullOrEmpty(snap.SavedAt))
            {
                txtSnapTime.Text = "快照 " + snap.SavedAt + " · " + snap.Count + " 项";
            }
            else
            {
                txtSnapTime.Text = "尚无快照";
            }
        }

        void DrawPreview(List<DeskIcon> items, DiffResult diff)
        {
            preview.Children.Clear();
            double cw = preview.ActualWidth;
            double ch = preview.ActualHeight;
            if (cw <= 0 || ch <= 0)
            {
                cw = 394; ch = 218;
            }

            int pw = lastScreen != null && lastScreen.PhysW > 0 ? lastScreen.PhysW : 2560;
            int ph = lastScreen != null && lastScreen.PhysH > 0 ? lastScreen.PhysH : 1440;

            double ratio = pw / (double)ph;
            double w = cw - 14;
            double h = w / ratio;
            if (h > ch - 14) { h = ch - 14; w = h * ratio; }
            double ox = (cw - w) / 2.0;
            double oy = (ch - h) / 2.0;

            Rectangle bg = new Rectangle();
            bg.Width = w;
            bg.Height = h;
            bg.RadiusX = 8;
            bg.RadiusY = 8;
            bg.Fill = new SolidColorBrush(Color.FromRgb(0x0E, 0x0F, 0x11));
            bg.Stroke = new SolidColorBrush(Color.FromArgb(0x14, 0xF2, 0xEC, 0xDD));
            bg.StrokeThickness = 1;
            Canvas.SetLeft(bg, ox);
            Canvas.SetTop(bg, oy);
            preview.Children.Add(bg);

            if (lastScreen != null && lastScreen.Dx > 0)
            {
                int step = Math.Max(1, lastScreen.Dx);
                for (int x = 0; x < pw; x += step * 2)
                {
                    Line ln = new Line();
                    ln.X1 = ox + x * w / pw;
                    ln.X2 = ln.X1;
                    ln.Y1 = oy;
                    ln.Y2 = oy + h;
                    ln.Stroke = new SolidColorBrush(Color.FromArgb(0x0C, 0xF2, 0xEC, 0xDD));
                    ln.StrokeThickness = 1;
                    preview.Children.Add(ln);
                }
            }

            HashSet<string> moveNames = new HashSet<string>(StringComparer.Ordinal);
            HashSet<string> newNames = new HashSet<string>(StringComparer.Ordinal);
            if (diff != null)
            {
                foreach (DeskIcon d in diff.MoveTargets) moveNames.Add(d.Name);
                foreach (string n in diff.NewOnes) newNames.Add(n);
            }

            int shown = 0;
            foreach (DeskIcon d in items)
            {
                double px = ox + (d.X / (double)pw) * w;
                double py = oy + (d.Y / (double)ph) * h;

                Rectangle dot = new Rectangle();
                dot.Width = 3.8;
                dot.Height = 3.8;
                dot.RadiusX = 1.3;
                dot.RadiusY = 1.3;

                Color c = Color.FromArgb(0x76, 0xED, 0xE8, 0xDE);
                if (newNames.Contains(d.Name)) c = Palette.Ink;
                else if (moveNames.Contains(d.Name)) c = Palette.Ochre;

                dot.Fill = new SolidColorBrush(c);
                Canvas.SetLeft(dot, px);
                Canvas.SetTop(dot, py);
                preview.Children.Add(dot);

                dot.Opacity = 0;
                DoubleAnimation fa = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(180));
                fa.BeginTime = TimeSpan.FromMilliseconds(Math.Min(shown * 4, 420));
                dot.BeginAnimation(OpacityProperty, fa);
                shown++;
            }
        }

        // ---------- 动作 ----------
        void DoSave()
        {
            try
            {
                Snapshot s = Engine.Capture("手动保存 " + DateTime.Now.ToString("HH:mm"));
                string main = System.IO.Path.Combine(root, "layout_snapshot.json");
                JsonStore.Save(main, s);
                Engine.Backup(root, s);

                Pulse(btnSave);
                Refresh();
                Status("已保存 " + s.Count + " 项 · " + s.ScreenW + "×" + s.ScreenH + " / " + ((int)Math.Round(s.Scale * 100)) + "%", Palette.Moss);
            }
            catch (Exception ex)
            {
                Status("保存失败：" + ex.Message, Palette.Ochre);
            }
        }

        void DoRestore()
        {
            try
            {
                Snapshot snap = Engine.LoadSnapshot(root);
                if (snap == null) { Status("还没有快照，先点「保存当前布局」", Palette.Ochre); return; }

                if (!restoringArmed)
                {
                    List<DeskIcon> items = Desk.Scan();
                    ScreenState sc = Desk.GetScreen();
                    DiffResult diff = Engine.Diff(snap, items, sc);

                    DrawPreview(items, diff);

                    if (diff.MoveIndices.Count == 0 && diff.NewOnes.Count == 0)
                    {
                        Status("桌面已与快照完全一致，无需改动", Palette.Moss);
                        return;
                    }

                    string msg = "需移动 " + diff.MoveIndices.Count + " 项";
                    if (diff.NewOnes.Count > 0) msg += " · 新增补位 " + diff.NewOnes.Count + " 项";
                    if (diff.Missing.Count > 0) msg += " · 已消失 " + diff.Missing.Count + " 项";
                    Status(msg + " — 再点一次确认执行", Palette.Ochre);

                    pending = snap;
                    restoringArmed = true;
                    btnRestore.Label.Text = "确认执行 (" + diff.MoveIndices.Count + ")";
                    btnRestore.SetPrimary(true);
                    Pulse(btnRestore);
                    return;
                }

                int moved = Engine.Apply(pending, 6);
                pending = null;
                restoringArmed = false;
                btnRestore.Label.Text = "恢复布局";
                btnRestore.SetPrimary(false);

                Pulse(btnRestore);
                Refresh();
                Status(moved > 0 ? "已恢复，共校正 " + moved + " 次坐标" : "桌面已与快照一致", Palette.Moss);
            }
            catch (Exception ex)
            {
                Status("恢复失败：" + ex.Message, Palette.Ochre);
                restoringArmed = false;
                pending = null;
                btnRestore.Label.Text = "恢复布局";
                btnRestore.SetPrimary(false);
            }
        }

        void DoCompact()
        {
            try
            {
                Snapshot snap = Engine.Capture("紧致补洞前");
                ScreenState sc = Desk.GetScreen();
                Snapshot compacted = Engine.Compact(snap, sc);

                int moved = Engine.Apply(compacted, 6);

                string main = System.IO.Path.Combine(root, "layout_snapshot.json");
                JsonStore.Save(main, compacted);
                Engine.Backup(root, compacted);

                Pulse(btnCompact);
                Refresh();
                Status("已紧致排列 " + compacted.Count + " 项 · 校正 " + moved + " 次", Palette.Moss);
            }
            catch (Exception ex)
            {
                Status("紧致失败：" + ex.Message, Palette.Ochre);
            }
        }

        void DoOpenFolder()
        {
            try { System.Diagnostics.Process.Start("explorer.exe", root); }
            catch (Exception ex) { Status("打开目录失败：" + ex.Message, Palette.Ochre); }
        }

        protected override void OnRenderSizeChanged(SizeChangedInfo sizeInfo)
        {
            base.OnRenderSizeChanged(sizeInfo);
            if (previewHost != null && preview != null && previewHost.ActualWidth > 0)
            {
                try
                {
                    List<DeskIcon> items = Desk.Scan();
                    DrawPreview(items, null);
                }
                catch { }
            }
        }
    }

    public class LKApp : Application
    {
        [STAThread]
        public static void Main()
        {
            Desk.EnablePerMonitorDpi();
            LKApp app = new LKApp();
            app.Run(new MainWindow());
        }
    }
}
