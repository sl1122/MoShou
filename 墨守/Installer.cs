// 墨守 MoShou · 水墨风单文件自解压安装/卸载程序
// 编译：csc /target:winexe /platform:anycpu /optimize+ /codepage:65001
//       /win32icon:MoShou.ico /resource:墨守.exe,MoShou.exe /out:墨守_安装程序.exe Installer.cs
using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Microsoft.Win32;

namespace MoShouInstaller
{
    public class SetupWindow : Window
    {
        static Color C(byte r, byte g, byte b) { return Color.FromRgb(r, g, b); }
        static SolidColorBrush Br(Color c) { return new SolidColorBrush(c); }

        static readonly Color BgCol = C(0xF4, 0xF0, 0xE6);
        static readonly Color CardCol = C(0xEC, 0xE6, 0xD8);
        static readonly Color FgCol = C(0x17, 0x16, 0x0F);
        static readonly Color Fg2Col = C(0x4B, 0x49, 0x3F);
        static readonly Color Fg3Col = C(0x8A, 0x85, 0x77);
        static readonly Color VermCol = C(0xA9, 0x3E, 0x30);
        static readonly Color HairCol = Color.FromArgb(0x28, 0x17, 0x16, 0x0F);

        static readonly FontFamily Serif = new FontFamily("Noto Serif SC,Source Han Serif SC,SimSun,serif");
        static readonly FontFamily Sans = new FontFamily("Microsoft YaHei UI,Microsoft YaHei");
        static readonly FontFamily Mono = new FontFamily("Consolas,Cascadia Mono");

        TextBox tbPath;
        CheckBox chkDesk, chkMenu, chkAgree;
        TextBlock lblStatus;
        Border btnMain;
        TextBlock btnMainTx;
        bool installed = false;
        string installedExe = null;

        public SetupWindow()
        {
            Title = "墨守 · 安装向导";
            Width = 500;
            Height = 472;
            WindowStyle = WindowStyle.None;
            ResizeMode = ResizeMode.NoResize;
            AllowsTransparency = true;
            Background = Brushes.Transparent;
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
            FontFamily = Sans;

            Border shell = new Border();
            shell.CornerRadius = new CornerRadius(18);
            shell.Background = Br(BgCol);
            shell.BorderBrush = Br(HairCol);
            shell.BorderThickness = new Thickness(1);
            shell.Padding = new Thickness(24, 18, 24, 20);

            Grid root = new Grid();
            root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(46) });
            root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(86) });
            root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(126) });
            root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(76) });
            root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(56) });
            root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

            // 头部（可拖动 + 关闭按钮）
            Grid head = new Grid();
            head.Background = Brushes.Transparent;
            head.MouseLeftButtonDown += delegate { try { DragMove(); } catch { } };

            StackPanel titleSp = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
            TextBlock tName = new TextBlock
            {
                Text = "墨守",
                FontFamily = Serif,
                FontSize = 22,
                Foreground = Br(FgCol),
                VerticalAlignment = VerticalAlignment.Center
            };
            TextBlock tSub = new TextBlock
            {
                Text = "桌面布局记忆与恢复 · 安装向导",
                FontSize = 11.5,
                Foreground = Br(Fg3Col),
                Margin = new Thickness(12, 4, 0, 0),
                VerticalAlignment = VerticalAlignment.Center
            };
            titleSp.Children.Add(tName);
            titleSp.Children.Add(tSub);
            head.Children.Add(titleSp);

            Border btnX = new Border
            {
                Width = 34,
                Height = 34,
                Background = Brushes.Transparent,
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Center,
                Cursor = Cursors.Hand
            };
            TextBlock btnXTx = new TextBlock
            {
                Text = "✕",
                FontSize = 14,
                Foreground = Br(VermCol),
                Opacity = 0.62,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            btnX.Child = btnXTx;
            btnX.MouseEnter += delegate { btnXTx.Opacity = 1.0; };
            btnX.MouseLeave += delegate { btnXTx.Opacity = 0.62; };
            // 必须在 MouseLeftButtonDown 拦截事件，防止冒泡给 head 触发 DragMove 吞掉点击
            btnX.MouseLeftButtonDown += delegate (object s, MouseButtonEventArgs e)
            {
                e.Handled = true;
                Close();
            };
            head.Children.Add(btnX);
            KeyDown += delegate (object s, KeyEventArgs e)
            {
                if (e.Key == Key.Escape) { Close(); e.Handled = true; }
            };
            Grid.SetRow(head, 0);
            root.Children.Add(head);

            // 安装路径区
            StackPanel pathBox = new StackPanel { Margin = new Thickness(0, 8, 0, 0) };
            pathBox.Children.Add(new TextBlock
            {
                Text = "安装目录（支持任意中英文路径，默认无需管理员权限）",
                FontSize = 11,
                Foreground = Br(Fg2Col),
                Margin = new Thickness(0, 0, 0, 6)
            });

            Grid pathRow = new Grid();
            pathRow.ColumnDefinitions.Add(new ColumnDefinition());
            pathRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(8) });
            pathRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(76) });

            string defDir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "墨守");

            Border tbWrap = new Border
            {
                Background = Br(CardCol),
                BorderBrush = Br(HairCol),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(8),
                Height = 34,
                Padding = new Thickness(8, 0, 8, 0)
            };
            tbPath = new TextBox
            {
                Text = defDir,
                FontFamily = Mono,
                FontSize = 11.5,
                Background = Brushes.Transparent,
                BorderThickness = new Thickness(0),
                Foreground = Br(FgCol),
                VerticalContentAlignment = VerticalAlignment.Center
            };
            tbWrap.Child = tbPath;
            pathRow.Children.Add(tbWrap);

            Border btnBrowse = SmallBtn("浏览…", delegate { BrowseFolder(); });
            Grid.SetColumn(btnBrowse, 2);
            pathRow.Children.Add(btnBrowse);
            pathBox.Children.Add(pathRow);
            Grid.SetRow(pathBox, 1);
            root.Children.Add(pathBox);

            // 免责声明
            Border discCard = new Border
            {
                Background = Br(CardCol),
                BorderBrush = Br(HairCol),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(10),
                Padding = new Thickness(12, 10, 12, 10),
                Margin = new Thickness(0, 4, 0, 4)
            };
            TextBlock discTx = new TextBlock
            {
                Text = "免责声明与安全说明：\n" +
                       "1. 本工具仅读写 Windows 桌面图标坐标，绝不移动、改名或删除任何文件。\n" +
                       "2. 「归位」操作需二次确认；首次使用请先点击「落子成记」保存当前布局。\n" +
                       "3. 纯本地单文件运行，不联网、不收集任何数据、后台零轮询、内存占用极低。\n" +
                       "4. 本软件按现状提供，使用前建议知悉上述说明；可随时通过系统设置完整卸载。",
                FontSize = 10.5,
                LineHeight = 19,
                TextWrapping = TextWrapping.Wrap,
                Foreground = Br(Fg2Col)
            };
            discCard.Child = discTx;
            Grid.SetRow(discCard, 2);
            root.Children.Add(discCard);

            // 选项区
            StackPanel opts = new StackPanel { Margin = new Thickness(2, 8, 0, 0) };
            chkDesk = MakeCheck("创建桌面快捷方式", true);
            chkMenu = MakeCheck("创建开始菜单快捷方式", true);
            chkAgree = MakeCheck("我已阅读并同意上述免责声明", true);
            opts.Children.Add(chkDesk);
            opts.Children.Add(chkMenu);
            opts.Children.Add(chkAgree);
            Grid.SetRow(opts, 3);
            root.Children.Add(opts);

            // 主按钮
            btnMain = new Border
            {
                Height = 46,
                CornerRadius = new CornerRadius(10),
                Background = Br(FgCol),
                BorderBrush = Br(FgCol),
                BorderThickness = new Thickness(1),
                Cursor = Cursors.Hand,
                Margin = new Thickness(0, 8, 0, 0)
            };
            btnMainTx = new TextBlock
            {
                Text = "开始安装",
                FontFamily = Serif,
                FontSize = 16,
                Foreground = Br(BgCol),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            btnMain.Child = btnMainTx;
            btnMain.MouseEnter += delegate { btnMain.BorderBrush = Br(VermCol); };
            btnMain.MouseLeave += delegate { btnMain.BorderBrush = Br(FgCol); };
            btnMain.MouseLeftButtonUp += delegate { OnMainAction(); };
            Grid.SetRow(btnMain, 4);
            root.Children.Add(btnMain);

            // 状态栏
            lblStatus = new TextBlock
            {
                Text = "单文件绿色安装 · 体积不足 200 KB · 零运行库依赖",
                FontSize = 10,
                Foreground = Br(Fg3Col),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Bottom,
                Margin = new Thickness(0, 6, 0, 0)
            };
            Grid.SetRow(lblStatus, 5);
            root.Children.Add(lblStatus);

            shell.Child = root;
            Content = shell;
        }

        CheckBox MakeCheck(string text, bool isChecked)
        {
            CheckBox cb = new CheckBox();
            cb.Content = text;
            cb.IsChecked = isChecked;
            cb.FontSize = 11;
            cb.Foreground = Br(Fg2Col);
            cb.Margin = new Thickness(0, 2, 0, 2);
            cb.Cursor = Cursors.Hand;
            return cb;
        }

        Border SmallBtn(string text, Action act)
        {
            Border b = new Border
            {
                Height = 34,
                CornerRadius = new CornerRadius(8),
                Background = Br(CardCol),
                BorderBrush = Br(HairCol),
                BorderThickness = new Thickness(1),
                Cursor = Cursors.Hand
            };
            TextBlock t = new TextBlock
            {
                Text = text,
                FontSize = 11.5,
                Foreground = Br(FgCol),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            b.Child = t;
            b.MouseEnter += delegate { b.BorderBrush = Br(VermCol); };
            b.MouseLeave += delegate { b.BorderBrush = Br(HairCol); };
            b.MouseLeftButtonUp += delegate { act(); };
            return b;
        }

        void BrowseFolder()
        {
            using (System.Windows.Forms.FolderBrowserDialog dlg = new System.Windows.Forms.FolderBrowserDialog())
            {
                dlg.Description = "选择「墨守」安装文件夹（支持任意中英文路径）";
                if (Directory.Exists(tbPath.Text)) dlg.SelectedPath = tbPath.Text;
                dlg.ShowNewFolderButton = true;
                if (dlg.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    string p = dlg.SelectedPath;
                    if (!p.EndsWith("墨守", StringComparison.OrdinalIgnoreCase) &&
                        !p.EndsWith("MoShou", StringComparison.OrdinalIgnoreCase))
                        p = Path.Combine(p, "墨守");
                    tbPath.Text = p;
                }
            }
        }

        void OnMainAction()
        {
            if (installed)
            {
                try
                {
                    if (!string.IsNullOrEmpty(installedExe) && File.Exists(installedExe))
                    {
                        ProcessStartInfo psi = new ProcessStartInfo(installedExe);
                        psi.WorkingDirectory = Path.GetDirectoryName(installedExe);
                        psi.UseShellExecute = true;
                        Process.Start(psi);
                    }
                }
                catch { }
                Close();
                return;
            }

            if (chkAgree.IsChecked != true)
            {
                lblStatus.Foreground = Br(VermCol);
                lblStatus.Text = "请先勾选同意免责声明";
                return;
            }

            string dir = (tbPath.Text != null) ? tbPath.Text.Trim() : "";
            if (dir.Length == 0)
            {
                lblStatus.Foreground = Br(VermCol);
                lblStatus.Text = "请输入或选择有效的安装目录";
                return;
            }

            try
            {
                if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
                // 写权限探测
                string probe = Path.Combine(dir, ".inst_probe.tmp");
                File.WriteAllText(probe, "ok");
                File.Delete(probe);

                // 关闭可能正在运行的旧墨守进程
                Process[] procs = Process.GetProcessesByName("墨守");
                for (int i = 0; i < procs.Length; i++)
                {
                    try { procs[i].Kill(); procs[i].WaitForExit(1500); } catch { }
                }

                // 释放内嵌的 墨守.exe（绝不携带作者个人快照）
                string targetExe = Path.Combine(dir, "墨守.exe");
                using (Stream rs = Assembly.GetExecutingAssembly().GetManifestResourceStream("MoShou.exe"))
                {
                    if (rs == null) throw new Exception("安装包内部资源缺失");
                    using (FileStream fs = new FileStream(targetExe, FileMode.Create, FileAccess.Write))
                    {
                        rs.CopyTo(fs);
                    }
                }

                // 复制自身作为卸载程序
                string selfExe = Assembly.GetExecutingAssembly().Location;
                string uninstExe = Path.Combine(dir, "卸载墨守.exe");
                if (!selfExe.Equals(uninstExe, StringComparison.OrdinalIgnoreCase))
                    File.Copy(selfExe, uninstExe, true);

                // 快捷方式（COM WScript.Shell，100% 支持中英文/空格路径）
                if (chkDesk.IsChecked == true)
                {
                    string deskLnk = Path.Combine(
                        Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory), "墨守.lnk");
                    CreateShortcut(deskLnk, targetExe, dir, "墨守 · 桌面图标布局记忆与恢复");
                }
                if (chkMenu.IsChecked == true)
                {
                    string menuDir = Path.Combine(
                        Environment.GetFolderPath(Environment.SpecialFolder.Programs), "墨守");
                    if (!Directory.Exists(menuDir)) Directory.CreateDirectory(menuDir);
                    CreateShortcut(Path.Combine(menuDir, "墨守.lnk"), targetExe, dir, "墨守 · 桌面图标布局记忆与恢复");
                    CreateShortcut(Path.Combine(menuDir, "卸载墨守.lnk"), uninstExe, dir, "卸载墨守", "--uninstall \"" + dir + "\"");
                }

                // 注册当前用户卸载项（无需管理员权限）
                using (RegistryKey rk = Registry.CurrentUser.CreateSubKey(
                    @"Software\Microsoft\Windows\CurrentVersion\Uninstall\MoShou"))
                {
                    if (rk != null)
                    {
                        rk.SetValue("DisplayName", "墨守（桌面布局记忆与恢复）");
                        rk.SetValue("DisplayVersion", "1.9");
                        rk.SetValue("Publisher", "MoShou");
                        rk.SetValue("InstallLocation", dir);
                        rk.SetValue("DisplayIcon", targetExe);
                        rk.SetValue("UninstallString", "\"" + uninstExe + "\" --uninstall \"" + dir + "\"");
                        rk.SetValue("NoModify", 1, RegistryValueKind.DWord);
                        rk.SetValue("NoRepair", 1, RegistryValueKind.DWord);
                    }
                }

                installed = true;
                installedExe = targetExe;
                btnMainTx.Text = "立即启动墨守";
                lblStatus.Foreground = Br(Fg2Col);
                lblStatus.Text = "安装完成 · " + dir;
            }
            catch (UnauthorizedAccessException)
            {
                lblStatus.Foreground = Br(VermCol);
                lblStatus.Text = "该目录需要管理员权限，请换到非系统盘或默认目录";
            }
            catch (Exception ex)
            {
                lblStatus.Foreground = Br(VermCol);
                lblStatus.Text = "安装失败：" + ex.Message;
            }
        }

        static void CreateShortcut(string lnkPath, string targetPath, string workDir, string desc)
        {
            CreateShortcut(lnkPath, targetPath, workDir, desc, "");
        }

        static void CreateShortcut(string lnkPath, string targetPath, string workDir, string desc, string args)
        {
            try
            {
                Type t = Type.GetTypeFromProgID("WScript.Shell");
                object shell = Activator.CreateInstance(t);
                object sc = t.InvokeMember("CreateShortcut", BindingFlags.InvokeMethod, null, shell, new object[] { lnkPath });
                Type st = sc.GetType();
                st.InvokeMember("TargetPath", BindingFlags.SetProperty, null, sc, new object[] { targetPath });
                st.InvokeMember("WorkingDirectory", BindingFlags.SetProperty, null, sc, new object[] { workDir });
                st.InvokeMember("Description", BindingFlags.SetProperty, null, sc, new object[] { desc });
                st.InvokeMember("IconLocation", BindingFlags.SetProperty, null, sc, new object[] { targetPath + ",0" });
                if (!string.IsNullOrEmpty(args))
                    st.InvokeMember("Arguments", BindingFlags.SetProperty, null, sc, new object[] { args });
                st.InvokeMember("Save", BindingFlags.InvokeMethod, null, sc, null);
                Marshal.ReleaseComObject(sc);
                Marshal.ReleaseComObject(shell);
            }
            catch { }
        }

        public static void PerformUninstall(string dir, bool removeUserData)
        {
            Process[] procs = Process.GetProcessesByName("墨守");
            for (int i = 0; i < procs.Length; i++)
            {
                try { procs[i].Kill(); procs[i].WaitForExit(1500); } catch { }
            }

            string deskLnk = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory), "墨守.lnk");
            if (File.Exists(deskLnk)) File.Delete(deskLnk);

            string menuDir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.Programs), "墨守");
            if (Directory.Exists(menuDir)) Directory.Delete(menuDir, true);

            try
            {
                Registry.CurrentUser.DeleteSubKeyTree(
                    @"Software\Microsoft\Windows\CurrentVersion\Uninstall\MoShou", false);
            }
            catch { }

            string exeFile = Path.Combine(dir, "墨守.exe");
            if (File.Exists(exeFile)) File.Delete(exeFile);

            if (removeUserData)
            {
                try
                {
                    string snapF = Path.Combine(dir, "layout_snapshot.json");
                    if (File.Exists(snapF)) File.Delete(snapF);
                    string layD = Path.Combine(dir, "layouts");
                    if (Directory.Exists(layD)) Directory.Delete(layD, true);
                    string appD = Path.Combine(
                        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "MoShou");
                    if (Directory.Exists(appD)) Directory.Delete(appD, true);
                }
                catch { }
            }

            // 延迟 1 秒清理卸载器自身，并在目录为空时移除整个安装目录
            string selfExe = Assembly.GetExecutingAssembly().Location;
            ProcessStartInfo psi = new ProcessStartInfo("cmd.exe",
                "/c ping 127.0.0.1 -n 2 >nul & del /f /q \"" + selfExe + "\" & rmdir \"" + dir + "\" 2>nul");
            psi.WindowStyle = ProcessWindowStyle.Hidden;
            psi.CreateNoWindow = true;
            Process.Start(psi);
        }
    }

    // 独立宣纸风卸载窗口：双击安装目录中的「卸载墨守.exe」或从系统「应用和功能」卸载时直接进入
    public class UninstallWindow : Window
    {
        static Color C(byte r, byte g, byte b) { return Color.FromRgb(r, g, b); }
        static SolidColorBrush Br(Color c) { return new SolidColorBrush(c); }

        static readonly Color BgCol = C(0xF4, 0xF0, 0xE6);
        static readonly Color CardCol = C(0xEC, 0xE6, 0xD8);
        static readonly Color FgCol = C(0x17, 0x16, 0x0F);
        static readonly Color Fg2Col = C(0x4B, 0x49, 0x3F);
        static readonly Color Fg3Col = C(0x8A, 0x85, 0x77);
        static readonly Color VermCol = C(0xA9, 0x3E, 0x30);
        static readonly Color HairCol = Color.FromArgb(0x28, 0x17, 0x16, 0x0F);

        static readonly FontFamily Serif = new FontFamily("Noto Serif SC,Source Han Serif SC,SimSun,serif");
        static readonly FontFamily Sans = new FontFamily("Microsoft YaHei UI,Microsoft YaHei");
        static readonly FontFamily Mono = new FontFamily("Consolas,Cascadia Mono");

        string targetDir;
        CheckBox chkDelData;
        TextBlock lblStatus;
        Border btnOk, btnCancel;
        TextBlock btnOkTx;
        bool done = false;

        public UninstallWindow(string dir)
        {
            targetDir = dir;
            Title = "卸载墨守";
            Width = 460;
            Height = 336;
            WindowStyle = WindowStyle.None;
            ResizeMode = ResizeMode.NoResize;
            AllowsTransparency = true;
            Background = Brushes.Transparent;
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
            FontFamily = Sans;

            Border shell = new Border();
            shell.CornerRadius = new CornerRadius(18);
            shell.Background = Br(BgCol);
            shell.BorderBrush = Br(HairCol);
            shell.BorderThickness = new Thickness(1);
            shell.Padding = new Thickness(24, 18, 24, 20);

            Grid root = new Grid();
            root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(44) });
            root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(112) });
            root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(44) });
            root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(54) });
            root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

            Grid head = new Grid();
            head.Background = Brushes.Transparent;
            head.MouseLeftButtonDown += delegate { try { DragMove(); } catch { } };

            StackPanel titleSp = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
            titleSp.Children.Add(new TextBlock
            {
                Text = "墨守",
                FontFamily = Serif,
                FontSize = 21,
                Foreground = Br(FgCol),
                VerticalAlignment = VerticalAlignment.Center
            });
            titleSp.Children.Add(new TextBlock
            {
                Text = "卸载向导",
                FontSize = 11.5,
                Foreground = Br(VermCol),
                Margin = new Thickness(10, 4, 0, 0),
                VerticalAlignment = VerticalAlignment.Center
            });
            head.Children.Add(titleSp);

            Border btnX = new Border
            {
                Width = 34,
                Height = 34,
                Background = Brushes.Transparent,
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Center,
                Cursor = Cursors.Hand
            };
            TextBlock btnXTx = new TextBlock
            {
                Text = "✕",
                FontSize = 14,
                Foreground = Br(VermCol),
                Opacity = 0.62,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            btnX.Child = btnXTx;
            btnX.MouseEnter += delegate { btnXTx.Opacity = 1.0; };
            btnX.MouseLeave += delegate { btnXTx.Opacity = 0.62; };
            btnX.MouseLeftButtonDown += delegate (object s, MouseButtonEventArgs e) { e.Handled = true; Close(); };
            head.Children.Add(btnX);
            KeyDown += delegate (object s, KeyEventArgs e) { if (e.Key == Key.Escape) { Close(); e.Handled = true; } };
            Grid.SetRow(head, 0);
            root.Children.Add(head);

            Border infoCard = new Border
            {
                Background = Br(CardCol),
                BorderBrush = Br(HairCol),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(10),
                Padding = new Thickness(14, 12, 14, 12),
                Margin = new Thickness(0, 6, 0, 6)
            };
            StackPanel infoSp = new StackPanel();
            infoSp.Children.Add(new TextBlock
            {
                Text = "将从以下位置移除「墨守」程序、桌面/开始菜单快捷方式及系统卸载项：",
                FontSize = 11,
                Foreground = Br(Fg2Col),
                TextWrapping = TextWrapping.Wrap
            });
            infoSp.Children.Add(new TextBlock
            {
                Text = targetDir,
                FontFamily = Mono,
                FontSize = 11,
                Foreground = Br(FgCol),
                Margin = new Thickness(0, 6, 0, 6),
                TextWrapping = TextWrapping.Wrap
            });
            infoSp.Children.Add(new TextBlock
            {
                Text = "不会改动或删除您桌面上的任何个人文件。",
                FontSize = 10.5,
                Foreground = Br(Fg3Col)
            });
            infoCard.Child = infoSp;
            Grid.SetRow(infoCard, 1);
            root.Children.Add(infoCard);

            chkDelData = new CheckBox
            {
                Content = "同时清除我保存的桌面布局快照与历史回溯记录（默认保留）",
                IsChecked = false,
                FontSize = 11,
                Foreground = Br(Fg2Col),
                VerticalAlignment = VerticalAlignment.Center,
                Cursor = Cursors.Hand
            };
            Grid.SetRow(chkDelData, 2);
            root.Children.Add(chkDelData);

            Grid btns = new Grid { Margin = new Thickness(0, 6, 0, 0) };
            btns.ColumnDefinitions.Add(new ColumnDefinition());
            btns.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(10) });
            btns.ColumnDefinitions.Add(new ColumnDefinition());

            btnCancel = new Border
            {
                Height = 42,
                CornerRadius = new CornerRadius(10),
                Background = Br(CardCol),
                BorderBrush = Br(HairCol),
                BorderThickness = new Thickness(1),
                Cursor = Cursors.Hand
            };
            btnCancel.Child = new TextBlock
            {
                Text = "暂不卸载",
                FontFamily = Serif,
                FontSize = 14,
                Foreground = Br(FgCol),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            btnCancel.MouseEnter += delegate { btnCancel.BorderBrush = Br(Fg2Col); };
            btnCancel.MouseLeave += delegate { btnCancel.BorderBrush = Br(HairCol); };
            btnCancel.MouseLeftButtonUp += delegate { Close(); };
            Grid.SetColumn(btnCancel, 0);
            btns.Children.Add(btnCancel);

            btnOk = new Border
            {
                Height = 42,
                CornerRadius = new CornerRadius(10),
                Background = Br(VermCol),
                BorderBrush = Br(VermCol),
                BorderThickness = new Thickness(1),
                Cursor = Cursors.Hand
            };
            btnOkTx = new TextBlock
            {
                Text = "确认卸载",
                FontFamily = Serif,
                FontSize = 14,
                Foreground = Br(BgCol),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            btnOk.Child = btnOkTx;
            btnOk.MouseEnter += delegate { btnOk.Opacity = 0.90; };
            btnOk.MouseLeave += delegate { btnOk.Opacity = 1.0; };
            btnOk.MouseLeftButtonUp += delegate { OnUninstall(); };
            Grid.SetColumn(btnOk, 2);
            btns.Children.Add(btnOk);

            Grid.SetRow(btns, 3);
            root.Children.Add(btns);

            lblStatus = new TextBlock
            {
                Text = "若需再次使用，随时重新运行安装程序即可",
                FontSize = 10,
                Foreground = Br(Fg3Col),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Bottom
            };
            Grid.SetRow(lblStatus, 4);
            root.Children.Add(lblStatus);

            shell.Child = root;
            Content = shell;
        }

        void OnUninstall()
        {
            if (done) { Close(); return; }
            try
            {
                SetupWindow.PerformUninstall(targetDir, chkDelData.IsChecked == true);
                done = true;
                btnOkTx.Text = "完成并关闭";
                btnOk.Background = Br(FgCol);
                btnOk.BorderBrush = Br(FgCol);
                btnCancel.Visibility = Visibility.Collapsed;
                chkDelData.IsEnabled = false;
                lblStatus.Foreground = Br(Fg2Col);
                lblStatus.Text = "「墨守」已卸载完成，关闭窗口后将自动清理残留。";
            }
            catch (Exception ex)
            {
                lblStatus.Foreground = Br(VermCol);
                lblStatus.Text = "卸载失败：" + ex.Message;
            }
        }
    }

    public class Program
    {
        [STAThread]
        public static void Main(string[] args)
        {
            string selfExe = Assembly.GetExecutingAssembly().Location;
            string selfName = Path.GetFileNameWithoutExtension(selfExe) ?? "";
            string selfDir = Path.GetDirectoryName(selfExe) ?? "";

            bool isUninstallMode = false;
            string uninstDir = selfDir;

            if (args != null && args.Length >= 1 && args[0] == "--uninstall")
            {
                isUninstallMode = true;
                if (args.Length >= 2 && !string.IsNullOrEmpty(args[1])) uninstDir = args[1];
            }
            else if (selfName.IndexOf("卸载", StringComparison.OrdinalIgnoreCase) >= 0 ||
                     selfName.IndexOf("uninstall", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                // 直接双击安装目录里的「卸载墨守.exe」时自动进入卸载模式，不再误开安装向导
                isUninstallMode = true;
            }

            Application app = new Application();
            if (isUninstallMode)
                app.Run(new UninstallWindow(uninstDir));
            else
                app.Run(new SetupWindow());
        }
    }
}
