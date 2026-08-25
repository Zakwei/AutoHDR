using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Win32;

namespace AutoHDR
{
    static class Program
    {
        public static string AppDir { get; private set; }
        public static Action<string> Log { get; private set; }

        [STAThread]
        static void Main()
        {
            AppDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string logFile = Path.Combine(AppDir, "AutoHDR.log");

            Log = msg =>
            {
                string line = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {msg}";
                try
                {
                    File.AppendAllText(logFile, line + Environment.NewLine);
                }
                catch { }
                Debug.WriteLine(line);
            };

            Locale.LoadFromFile(AppDir);

            string[] args = Environment.GetCommandLineArgs();
            if (args.Length > 1)
            {
                string first = args[1].ToLowerInvariant();
                if (first == "/install" || first == "/autostart")
                {
                    SetAutoStart(true);
                    Log(Locale.Get("AutoStartEnabled"));
                    return;
                }
                if (first == "/uninstall" || first == "/noautostart")
                {
                    SetAutoStart(false);
                    Log(Locale.Get("AutoStartDisabled"));
                    return;
                }
                if (first == "/settings" || first == "/ustawienia")
                {
                    Application.EnableVisualStyles();
                    Application.SetCompatibleTextRenderingDefault(false);
                    using (var form = new SettingsForm(AppDir))
                    {
                        Application.Run(form);
                    }
                    return;
                }
            }

            AppDomain.CurrentDomain.UnhandledException += (s, e) =>
            {
                Log?.Invoke(Locale.Get("UnhandledException") + ": " + e.ExceptionObject);
            };

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            try
            {
                using (var form = new TrayForm(AppDir, Log))
                {
                    Application.Run(form);
                }
            }
            catch (Exception ex)
            {
                Log?.Invoke(Locale.Get("ApplicationError") + ": " + ex);
                MessageBox.Show(Locale.Get("StartupError") + ":\n" + ex.Message, "AutoHDR", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        public static bool IsAutoStartEnabled()
        {
            try
            {
                using (var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run"))
                {
                    string value = key?.GetValue("AutoHDR") as string;
                    return !string.IsNullOrEmpty(value) && value.Equals(Application.ExecutablePath, StringComparison.OrdinalIgnoreCase);
                }
            }
            catch (Exception ex)
            {
                Log?.Invoke(Locale.Get("AutoStartReadError") + ": " + ex.Message);
                return false;
            }
        }

        public static void SetAutoStart(bool enabled)
        {
            try
            {
                using (var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run", true))
                {
                    if (enabled)
                        key.SetValue("AutoHDR", Application.ExecutablePath);
                    else
                        key.DeleteValue("AutoHDR", false);
                }
            }
            catch (Exception ex)
            {
                Log?.Invoke(Locale.Get("AutoStartSetError") + ": " + ex.Message);
            }
        }
    }

    public class TrayForm : Form
    {
        private readonly string _appDir;
        private readonly Action<string> _log;
        private readonly HdrController _hdr;
        private GameMonitor _monitor;
        private NotifyIcon _notifyIcon;
        private ContextMenuStrip _menu;
        private ToolStripMenuItem _hdrItem;
        private ToolStripMenuItem _gamesItem;
        private ToolStripMenuItem _statusItem;
        private ToolStripMenuItem _autoStartItem;
        private ToolStripMenuItem _languageItem;
        private Timer _statusTimer;

        public TrayForm(string appDir, Action<string> log)
        {
            _appDir = appDir;
            _log = log;
            _hdr = new HdrController();
            InitializeComponents();
        }

        private void InitializeComponents()
        {
            SuspendLayout();

            WindowState = FormWindowState.Minimized;
            StartPosition = FormStartPosition.Manual;
            Location = new Point(-1000, -1000);
            ShowInTaskbar = false;
            Opacity = 0;
            Size = new Size(1, 1);
            FormBorderStyle = FormBorderStyle.SizableToolWindow;
            Text = "AutoHDR";

            _menu = new ContextMenuStrip();

            var versionItem = new ToolStripMenuItem("AutoHDR v1.0") { Enabled = false };
            _menu.Items.Add(versionItem);

            _hdrItem = new ToolStripMenuItem(Locale.Get("HDRStatus")) { Enabled = false };
            _gamesItem = new ToolStripMenuItem(Locale.Get("GamesCount")) { Enabled = false };
            _statusItem = new ToolStripMenuItem(Locale.Get("GameNone")) { Enabled = false };

            _menu.Items.Add(_hdrItem);
            _menu.Items.Add(_gamesItem);
            _menu.Items.Add(_statusItem);
            _menu.Items.Add(new ToolStripSeparator());

            _menu.Items.Add(Locale.Get("RefreshGameList"), null, (s, e) => BeginRefresh());
            _menu.Items.Add(Locale.Get("OpenConfigFolder"), null, (s, e) => OpenConfigDir());

            _autoStartItem = new ToolStripMenuItem(Locale.Get("RunWithWindows"))
            {
                Checked = Program.IsAutoStartEnabled(),
                CheckOnClick = true
            };
            _autoStartItem.Click += (s, e) =>
            {
                Program.SetAutoStart(_autoStartItem.Checked);
                _log?.Invoke(_autoStartItem.Checked ? Locale.Get("AutoStartEnabled") : Locale.Get("AutoStartDisabled"));
            };
            _menu.Items.Add(_autoStartItem);

            _menu.Items.Add(Locale.Get("EditGameList"), null, (s, e) => OpenSettings());

            _languageItem = new ToolStripMenuItem(Locale.Get("Language"));
            BuildLanguageMenu();
            _menu.Items.Add(_languageItem);

            _menu.Items.Add(new ToolStripSeparator());
            _menu.Items.Add(Locale.Get("Exit"), null, (s, e) => Exit());

            _notifyIcon = new NotifyIcon();
            _notifyIcon.Text = "AutoHDR";
            _notifyIcon.ContextMenuStrip = _menu;
            _notifyIcon.Icon = GenerateIcon();
            _notifyIcon.Visible = true;

            _statusTimer = new Timer();
            _statusTimer.Interval = 2000;
            _statusTimer.Tick += (s, e) => UpdateStatus();
            _statusTimer.Start();

            FormClosing += (s, e) =>
            {
                if (e.CloseReason == CloseReason.UserClosing)
                {
                    e.Cancel = true;
                    Hide();
                }
            };

            ResumeLayout(false);
        }

        private void BuildLanguageMenu()
        {
            _languageItem.DropDownItems.Clear();

            var plItem = new ToolStripMenuItem(Locale.Get("Polish"))
            {
                Checked = Locale.Culture == "pl",
                Tag = "pl"
            };
            plItem.Click += (s, e) => ChangeLanguage("pl");

            var enItem = new ToolStripMenuItem(Locale.Get("English"))
            {
                Checked = Locale.Culture == "en",
                Tag = "en"
            };
            enItem.Click += (s, e) => ChangeLanguage("en");

            _languageItem.DropDownItems.Add(plItem);
            _languageItem.DropDownItems.Add(enItem);
        }

        private void ChangeLanguage(string culture)
        {
            if (Locale.Culture == culture) return;

            if (MessageBox.Show(Locale.Get("RestartRequired"), "AutoHDR", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                Locale.Culture = culture;
                Locale.SaveToFile(_appDir);
                Application.Restart();
            }
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            Hide();

            try
            {
                bool supported = _hdr.IsHDRSupported();
                bool enabled = _hdr.IsHDREnabled();
                _log?.Invoke(Locale.Format("HDRSupportedEnabled", supported, enabled));
            }
            catch (Exception ex)
            {
                _log?.Invoke(Locale.Get("HDRReadError") + ": " + ex.Message);
            }

            BeginRefresh();
        }

        private Icon GenerateIcon()
        {
            try
            {
                using (var bmp = new Bitmap(16, 16))
                using (var g = Graphics.FromImage(bmp))
                {
                    g.Clear(Color.Transparent);
                    g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                    using (var b = new SolidBrush(Color.Orange))
                    {
                        g.FillEllipse(b, 1, 1, 14, 14);
                    }
                    using (var p = new Pen(Color.Black, 1))
                    {
                        g.DrawEllipse(p, 1, 1, 14, 14);
                    }
                    return Icon.FromHandle(bmp.GetHicon());
                }
            }
            catch
            {
                return SystemIcons.Application;
            }
        }

        private async void BeginRefresh()
        {
            _statusItem.Text = Locale.Get("ScanningGames");
            try
            {
                await RefreshGamesAsync();
                _notifyIcon?.ShowBalloonTip(2000, "AutoHDR", _gamesItem.Text, ToolTipIcon.Info);
            }
            catch (Exception ex)
            {
                _log?.Invoke(Locale.Get("RefreshError") + ": " + ex.Message);
            }
        }

        private Task RefreshGamesAsync()
        {
            return Task.Run(() =>
            {
                _log?.Invoke(Locale.Get("ScanningGames"));
                var games = GameDetector.DetectAll(_appDir);

                if (InvokeRequired)
                    Invoke(new Action<HashSet<string>>(UpdateMonitor), games);
                else
                    UpdateMonitor(games);
            });
        }

        private void UpdateMonitor(HashSet<string> games)
        {
            _monitor?.Dispose();
            _monitor = new GameMonitor(games, _hdr, _log);
            _gamesItem.Text = Locale.Format("GamesCountStatus", games.Count);
            _log?.Invoke(Locale.Format("GamesDetected", games.Count));
            UpdateStatus();
        }

        private void UpdateStatus()
        {
            if (InvokeRequired)
            {
                Invoke(new Action(UpdateStatus));
                return;
            }

            try
            {
                bool hdrEnabled = _hdr.IsHDREnabled();
                _hdrItem.Text = hdrEnabled ? Locale.Get("HDROn") : Locale.Get("HDROff");
                _statusItem.Text = (_monitor != null && _monitor.GameRunning) ? Locale.Get("GameYes") : Locale.Get("GameNone");
            }
            catch (Exception ex)
            {
                _log?.Invoke(Locale.Get("HDRReadError") + ": " + ex.Message);
            }
        }

        private void OpenConfigDir()
        {
            try
            {
                Process.Start("explorer.exe", _appDir);
            }
            catch (Exception ex)
            {
                _log?.Invoke(Locale.Get("ConfigFolderOpenError") + ": " + ex.Message);
            }
        }

        private void OpenSettings()
        {
            try
            {
                using (var dlg = new SettingsForm(_appDir))
                {
                    if (dlg.ShowDialog(this) == DialogResult.OK)
                    {
                        BeginRefresh();
                    }
                }
            }
            catch (Exception ex)
            {
                _log?.Invoke(Locale.Get("SettingsOpenError") + ": " + ex.Message);
            }
        }

        private void Exit()
        {
            _statusTimer?.Stop();
            _statusTimer?.Dispose();

            _monitor?.Dispose();

            try
            {
                _hdr.EndGameMode();
            }
            catch (Exception ex)
            {
                _log?.Invoke(Locale.Get("HDRRestoreError") + ": " + ex.Message);
            }

            if (_notifyIcon != null)
            {
                _notifyIcon.Visible = false;
                _notifyIcon.Dispose();
            }

            Application.Exit();
        }
    }
}
