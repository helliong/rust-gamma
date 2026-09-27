using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;
using Microsoft.Win32;

namespace RustGammaPro
{
    static class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            bool createdNew;
            using (Mutex mutex = new Mutex(true, "RustGammaPro_SingleInstanceMutex", out createdNew))
            {
                if (!createdNew)
                {
                    MessageBox.Show("Rust Gamma Pro is already running in system tray.\nRust Gamma Pro уже запущен и находится в трее.",
                        "Rust Gamma Pro", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);

                bool startMinimized = false;
                foreach (string arg in args)
                {
                    if (arg.Equals("/minimized", StringComparison.OrdinalIgnoreCase) ||
                        arg.Equals("-minimized", StringComparison.OrdinalIgnoreCase))
                    {
                        startMinimized = true;
                    }
                }

                Application.Run(new MainForm(startMinimized));
            }
        }
    }

    public class AppConfig
    {
        public double NightGamma = 1.5;
        public uint HotkeyVK = 0x7A; // F11
        public string HotkeyName = "F11";
        public bool PlaySound = true;
        public bool AutoDetectRust = true;
        public bool AutoRestoreOnExit = true;
        public bool AutoStart = false;
        public string MonitorMode = "AOC"; // "AOC", "PRIMARY", "ALL"
        public string Language = "RU"; // "RU" or "EN"

        private static string ConfigPath
        {
            get { return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config.ini"); }
        }

        public static AppConfig Load()
        {
            AppConfig cfg = new AppConfig();
            if (!File.Exists(ConfigPath)) return cfg;

            try
            {
                string[] lines = File.ReadAllLines(ConfigPath);
                foreach (string line in lines)
                {
                    string trimmed = line.Trim();
                    if (string.IsNullOrEmpty(trimmed) || trimmed.StartsWith("#") || trimmed.StartsWith(";")) continue;
                    int eq = trimmed.IndexOf('=');
                    if (eq < 0) continue;
                    string key = trimmed.Substring(0, eq).Trim();
                    string val = trimmed.Substring(eq + 1).Trim();

                    if (key.Equals("NightGamma", StringComparison.OrdinalIgnoreCase))
                    {
                        double g;
                        if (double.TryParse(val, NumberStyles.Any, CultureInfo.InvariantCulture, out g)) cfg.NightGamma = g;
                    }
                    else if (key.Equals("HotkeyVK", StringComparison.OrdinalIgnoreCase))
                    {
                        uint vk;
                        if (uint.TryParse(val, out vk)) cfg.HotkeyVK = vk;
                    }
                    else if (key.Equals("HotkeyName", StringComparison.OrdinalIgnoreCase))
                    {
                        cfg.HotkeyName = val;
                    }
                    else if (key.Equals("PlaySound", StringComparison.OrdinalIgnoreCase))
                    {
                        cfg.PlaySound = (val == "1" || val.Equals("true", StringComparison.OrdinalIgnoreCase));
                    }
                    else if (key.Equals("AutoDetectRust", StringComparison.OrdinalIgnoreCase))
                    {
                        cfg.AutoDetectRust = (val == "1" || val.Equals("true", StringComparison.OrdinalIgnoreCase));
                    }
                    else if (key.Equals("AutoRestoreOnExit", StringComparison.OrdinalIgnoreCase))
                    {
                        cfg.AutoRestoreOnExit = (val == "1" || val.Equals("true", StringComparison.OrdinalIgnoreCase));
                    }
                    else if (key.Equals("MonitorMode", StringComparison.OrdinalIgnoreCase))
                    {
                        cfg.MonitorMode = val;
                    }
                    else if (key.Equals("Language", StringComparison.OrdinalIgnoreCase))
                    {
                        cfg.Language = val.ToUpperInvariant();
                    }
                }
            }
            catch { }
            return cfg;
        }

        public void Save()
        {
            try
            {
                using (StreamWriter sw = new StreamWriter(ConfigPath, false))
                {
                    sw.WriteLine("# Rust Gamma Pro Configuration");
                    sw.WriteLine("NightGamma=" + NightGamma.ToString("0.00", CultureInfo.InvariantCulture));
                    sw.WriteLine("HotkeyVK=" + HotkeyVK);
                    sw.WriteLine("HotkeyName=" + HotkeyName);
                    sw.WriteLine("PlaySound=" + (PlaySound ? "1" : "0"));
                    sw.WriteLine("AutoDetectRust=" + (AutoDetectRust ? "1" : "0"));
                    sw.WriteLine("AutoRestoreOnExit=" + (AutoRestoreOnExit ? "1" : "0"));
                    sw.WriteLine("MonitorMode=" + MonitorMode);
                    sw.WriteLine("Language=" + Language);
                }
            }
            catch { }
        }
    }

    public class MainForm : Form
    {
        // Win32 API (Works identically on NVIDIA, AMD Radeon and Intel)
        [DllImport("user32.dll")]
        static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);
        [DllImport("user32.dll")]
        static extern bool UnregisterHotKey(IntPtr hWnd, int id);

        [DllImport("gdi32.dll", CharSet = CharSet.Unicode)]
        static extern IntPtr CreateDC(string lpszDriver, string lpszDevice, string lpszOutput, IntPtr lpInitData);
        [DllImport("gdi32.dll")]
        static extern bool DeleteDC(IntPtr hdc);
        [DllImport("gdi32.dll")]
        static extern bool GetDeviceGammaRamp(IntPtr hdc, ref RAMP lpRamp);
        [DllImport("gdi32.dll")]
        static extern bool SetDeviceGammaRamp(IntPtr hdc, ref RAMP lpRamp);

        [DllImport("user32.dll")]
        static extern IntPtr MonitorFromWindow(IntPtr hwnd, uint dwFlags);
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        static extern bool GetMonitorInfo(IntPtr hMonitor, ref MONITORINFOEX lpmi);

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        static extern bool EnumDisplayDevices(string lpDevice, uint iDevNum, ref DISPLAY_DEVICE lpDisplayDevice, uint dwFlags);

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
        public struct RAMP
        {
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 256)]
            public ushort[] Red;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 256)]
            public ushort[] Green;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 256)]
            public ushort[] Blue;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        public struct MONITORINFOEX
        {
            public int cbSize;
            public RECT rcMonitor;
            public RECT rcWork;
            public int dwFlags;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
            public string szDevice;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct RECT { public int Left, Top, Right, Bottom; }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        public struct DISPLAY_DEVICE
        {
            public int cb;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)] public string DeviceName;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)] public string DeviceString;
            public int StateFlags;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)] public string DeviceID;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)] public string DeviceKey;
        }

        const int HOTKEY_ID = 9001;
        const uint MOD_NOREPEAT = 0x4000;

        private AppConfig config;
        private NotifyIcon trayIcon;
        private ContextMenuStrip trayMenu;
        private ToolStripMenuItem trayStatusItem;
        private ToolStripMenuItem traySettingsItem;
        private ToolStripMenuItem trayToggleItem;
        private ToolStripMenuItem trayAutoStartItem;
        private ToolStripMenuItem trayExitItem;
        private System.Windows.Forms.Timer watcherTimer;

        // UI контролы
        private Label lblSub;
        private Button btnLang;
        private GroupBox grpGamma;
        private Label lblGDesc;
        private Label lblGammaValue;
        private TrackBar trackGamma;
        private Button btnTestGamma;

        private GroupBox grpControls;
        private Label lblKey;
        private ComboBox cbHotkey;
        private Label lblMon;
        private ComboBox cbMonitor;

        private GroupBox grpAuto;
        private CheckBox chkAutoDetect;
        private CheckBox chkAutoRestore;
        private CheckBox chkSound;
        private CheckBox chkAutoStart;

        private Label lblGameStatus;
        private Label lblVersion;
        private LinkLabel linkWebsite;
        private Button btnSaveMinimize;

        private bool isNight = false;
        private bool rustDetected = false;
        private bool isTestingGamma = false;
        private System.Windows.Forms.Timer testTimer = null;
        private string activeDeviceName = @"\\.\DISPLAY2";
        private RAMP originalRamp;
        private bool hasOriginalRamp = false;
        private bool allowActualClose = false;

        private struct HotkeyOption
        {
            public string NameRU;
            public string NameEN;
            public uint VK;
            public HotkeyOption(string nameRU, string nameEN, uint vk)
            {
                NameRU = nameRU;
                NameEN = nameEN;
                VK = vk;
            }
        }

        private List<HotkeyOption> hotkeyOptions = new List<HotkeyOption>()
        {
            new HotkeyOption("F11 (Рекомендуется)", "F11 (Recommended)", 0x7A),
            new HotkeyOption("F10", "F10", 0x79),
            new HotkeyOption("F9", "F9", 0x78),
            new HotkeyOption("F8", "F8", 0x77),
            new HotkeyOption("F7", "F7", 0x76),
            new HotkeyOption("F6", "F6", 0x75),
            new HotkeyOption("NumPad * (Звёздочка)", "NumPad * (Asterisk)", 0x6A),
            new HotkeyOption("NumPad - (Минус)", "NumPad - (Minus)", 0x6D),
            new HotkeyOption("Scroll Lock", "Scroll Lock", 0x91),
            new HotkeyOption("Pause / Break", "Pause / Break", 0x13)
        };

        public MainForm(bool startMinimized)
        {
            config = AppConfig.Load();

            BuildUI();
            InitializeTray();
            UpdateLocalization();
            InitializeGamma();
            ApplyHotkey();

            watcherTimer = new System.Windows.Forms.Timer();
            watcherTimer.Interval = 1000;
            watcherTimer.Tick += WatcherTimer_Tick;
            watcherTimer.Start();

            CheckRustProcess();

            if (startMinimized)
            {
                this.WindowState = FormWindowState.Minimized;
                this.ShowInTaskbar = false;
                this.Hide();
            }
            else
            {
                this.StartPosition = FormStartPosition.CenterScreen;
            }
        }

        private void BuildUI()
        {
            this.Size = new Size(520, 650);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor = Color.FromArgb(24, 25, 29);
            this.ForeColor = Color.FromArgb(235, 235, 240);
            this.Font = new Font("Segoe UI", 9f, FontStyle.Regular);

            string iconPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "app.ico");
            if (File.Exists(iconPath)) this.Icon = new Icon(iconPath);

            // Верхняя плашка (Header)
            Panel headerPanel = new Panel() { Dock = DockStyle.Top, Height = 75, BackColor = Color.FromArgb(18, 19, 23) };
            
            PictureBox picLogo = new PictureBox() { Size = new Size(54, 54), Location = new Point(16, 10), SizeMode = PictureBoxSizeMode.Zoom };
            string imgPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "icon_clean.png");
            if (File.Exists(imgPath)) picLogo.Image = Image.FromFile(imgPath);
            else if (File.Exists(iconPath)) picLogo.Image = (this.Icon != null) ? this.Icon.ToBitmap() : null;

            Label lblTitle = new Label() { Text = "RUST GAMMA PRO", Location = new Point(78, 14), AutoSize = true, Font = new Font("Segoe UI", 13.5f, FontStyle.Bold), ForeColor = Color.FromArgb(255, 140, 40) };
            lblSub = new Label() { Text = "Аппаратное ночное зрение для Rust (NVIDIA & AMD)", Location = new Point(80, 42), AutoSize = true, Font = new Font("Segoe UI", 8.5f), ForeColor = Color.FromArgb(160, 165, 175) };

            // Кнопка переключения языка в шапке справа
            btnLang = new Button()
            {
                Location = new Point(418, 20),
                Size = new Size(74, 34),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(36, 38, 46),
                ForeColor = Color.FromArgb(255, 160, 50),
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnLang.FlatAppearance.BorderColor = Color.FromArgb(70, 75, 85);
            btnLang.Click += (s, e) => {
                config.Language = (config.Language == "RU") ? "EN" : "RU";
                config.Save();
                UpdateLocalization();
            };

            headerPanel.Controls.Add(picLogo);
            headerPanel.Controls.Add(lblTitle);
            headerPanel.Controls.Add(lblSub);
            headerPanel.Controls.Add(btnLang);
            this.Controls.Add(headerPanel);

            // Основной контейнер
            Panel content = new Panel() { Location = new Point(16, 85), Size = new Size(472, 470) };

            // 1. Блок Гаммы
            grpGamma = CreateGroupBox("1. Яркость ночью (Гамма)", 0, 112);
            lblGDesc = new Label() { Text = "Рекомендуется 1.40 – 1.60. Стандартная дневная гамма: 1.00", Location = new Point(14, 22), AutoSize = true, ForeColor = Color.FromArgb(170, 175, 185) };
            
            trackGamma = new TrackBar() { Location = new Point(12, 48), Size = new Size(210, 45), Minimum = 110, Maximum = 220, TickFrequency = 10, SmallChange = 5, LargeChange = 10 };
            trackGamma.Value = Math.Max(110, Math.Min(220, (int)Math.Round(config.NightGamma * 100)));
            trackGamma.ValueChanged += (s, e) => {
                double val = trackGamma.Value / 100.0;
                lblGammaValue.Text = val.ToString("0.00") + "x";
                config.NightGamma = val;
                if (isNight) ApplyGamma(config.NightGamma);
            };

            lblGammaValue = new Label() { Text = config.NightGamma.ToString("0.00") + "x", Location = new Point(226, 52), AutoSize = true, Font = new Font("Segoe UI", 12f, FontStyle.Bold), ForeColor = Color.FromArgb(255, 150, 40) };
            
            btnTestGamma = new Button() { 
                Text = "Проверить (2 сек)", 
                Location = new Point(302, 46), 
                Size = new Size(155, 34), 
                FlatStyle = FlatStyle.Flat, 
                BackColor = Color.FromArgb(45, 47, 55), 
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnTestGamma.Click += (s, e) => TestGamma2Sec();

            grpGamma.Controls.Add(lblGDesc);
            grpGamma.Controls.Add(trackGamma);
            grpGamma.Controls.Add(lblGammaValue);
            grpGamma.Controls.Add(btnTestGamma);
            content.Controls.Add(grpGamma);

            // 2. Блок Клавиши и Монитора
            grpControls = CreateGroupBox("2. Горячая клавиша и монитор", 122, 105);
            
            lblKey = new Label() { Text = "Клавиша переключения:", Location = new Point(14, 26), AutoSize = true };
            cbHotkey = new ComboBox() { Location = new Point(185, 23), Size = new Size(270, 26), DropDownStyle = ComboBoxStyle.DropDownList, BackColor = Color.FromArgb(36, 38, 44), ForeColor = Color.White };
            
            cbHotkey.SelectedIndexChanged += (s, e) => {
                if (cbHotkey.SelectedIndex >= 0 && cbHotkey.SelectedIndex < hotkeyOptions.Count)
                {
                    HotkeyOption opt = hotkeyOptions[cbHotkey.SelectedIndex];
                    config.HotkeyVK = opt.VK;
                    config.HotkeyName = opt.NameEN.Split(' ')[0];
                    ApplyHotkey();
                }
            };

            lblMon = new Label() { Text = "Целевой экран:", Location = new Point(14, 62), AutoSize = true };
            cbMonitor = new ComboBox() { Location = new Point(185, 59), Size = new Size(270, 26), DropDownStyle = ComboBoxStyle.DropDownList, BackColor = Color.FromArgb(36, 38, 44), ForeColor = Color.White };
            cbMonitor.SelectedIndexChanged += (s, e) => {
                if (cbMonitor.SelectedIndex == 2) config.MonitorMode = "ALL";
                else if (cbMonitor.SelectedIndex == 1) config.MonitorMode = "PRIMARY";
                else config.MonitorMode = "AOC";
                DetectTargetDisplay();
            };

            grpControls.Controls.Add(lblKey);
            grpControls.Controls.Add(cbHotkey);
            grpControls.Controls.Add(lblMon);
            grpControls.Controls.Add(cbMonitor);
            content.Controls.Add(grpControls);

            // 3. Блок Автоматизации
            grpAuto = CreateGroupBox("3. Автоматизация и звук", 237, 138);
            chkAutoDetect = new CheckBox() { Text = "Следить за запуском Rust (активировать хоткей только в игре)", Location = new Point(16, 24), AutoSize = true, Checked = config.AutoDetectRust };
            chkAutoDetect.CheckedChanged += (s, e) => { config.AutoDetectRust = chkAutoDetect.Checked; };

            chkAutoRestore = new CheckBox() { Text = "Автоматически сбрасывать гамму на 1.0 при закрытии игры", Location = new Point(16, 51), AutoSize = true, Checked = config.AutoRestoreOnExit };
            chkAutoRestore.CheckedChanged += (s, e) => { config.AutoRestoreOnExit = chkAutoRestore.Checked; };

            chkSound = new CheckBox() { Text = "Звуковой сигнал при переключении (высокий / низкий тон)", Location = new Point(16, 78), AutoSize = true, Checked = config.PlaySound };
            chkSound.CheckedChanged += (s, e) => { config.PlaySound = chkSound.Checked; };

            chkAutoStart = new CheckBox() { Text = "Запускать вместе с Windows (в трее около часов)", Location = new Point(16, 105), AutoSize = true, Checked = IsAutoStartEnabled() };
            chkAutoStart.CheckedChanged += (s, e) => { SetAutoStart(chkAutoStart.Checked); };

            grpAuto.Controls.Add(chkAutoDetect);
            grpAuto.Controls.Add(chkAutoRestore);
            grpAuto.Controls.Add(chkSound);
            grpAuto.Controls.Add(chkAutoStart);
            content.Controls.Add(grpAuto);

            // 4. Статус игры
            lblGameStatus = new Label() { 
                Text = "Статус: ⚪ Ожидание запуска RustClient.exe...", 
                Location = new Point(4, 388), 
                AutoSize = true, 
                Font = new Font("Segoe UI", 9.5f, FontStyle.Regular),
                ForeColor = Color.FromArgb(170, 175, 185) 
            };
            content.Controls.Add(lblGameStatus);

            // Версия и сайт
            lblVersion = new Label() { 
                Text = "Версия 1.0  •  Сайт разработчика: ", 
                Location = new Point(4, 415), 
                AutoSize = true, 
                Font = new Font("Segoe UI", 8.5f), 
                ForeColor = Color.FromArgb(140, 145, 155) 
            };
            content.Controls.Add(lblVersion);

            linkWebsite = new LinkLabel() { 
                Text = "helliong.space", 
                Location = new Point(205, 415), 
                AutoSize = true, 
                Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
                LinkColor = Color.FromArgb(255, 145, 45), 
                ActiveLinkColor = Color.FromArgb(255, 190, 80), 
                VisitedLinkColor = Color.FromArgb(255, 145, 45),
                Cursor = Cursors.Hand
            };
            linkWebsite.LinkClicked += (s, e) => {
                try { Process.Start(new ProcessStartInfo("https://helliong.space") { UseShellExecute = true }); }
                catch { try { Process.Start("https://helliong.space"); } catch { } }
            };
            content.Controls.Add(linkWebsite);

            this.Controls.Add(content);

            // Подвал (Footer)
            Panel footer = new Panel() { Dock = DockStyle.Bottom, Height = 65, BackColor = Color.FromArgb(18, 19, 23) };
            
            btnSaveMinimize = new Button()
            {
                Text = "✔ Сохранить и свернуть в трей",
                Location = new Point(95, 12),
                Size = new Size(320, 40),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(235, 100, 30),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnSaveMinimize.FlatAppearance.BorderSize = 0;
            btnSaveMinimize.Click += (s, e) => {
                config.Save();
                this.Hide();
                string title = (config.Language == "RU") ? "Rust Gamma Pro" : "Rust Gamma Pro";
                string msg = (config.Language == "RU")
                    ? "Программа свёрнута в трей и готова к работе.\nНажмите " + config.HotkeyName + " для переключения."
                    : "App minimized to tray and active.\nPress " + config.HotkeyName + " to toggle.";
                trayIcon.ShowBalloonTip(2000, title, msg, ToolTipIcon.Info);
            };

            footer.Controls.Add(btnSaveMinimize);
            this.Controls.Add(footer);

            // Крестик прячет в трей
            this.FormClosing += (s, e) => {
                if (!allowActualClose)
                {
                    e.Cancel = true;
                    config.Save();
                    this.Hide();
                }
            };
        }

        private void UpdateLocalization()
        {
            bool isRU = (config.Language == "RU");

            this.Text = isRU ? "Rust Gamma Pro — Настройки" : "Rust Gamma Pro — Settings";
            lblSub.Text = isRU ? "Аппаратное ночное зрение для Rust (NVIDIA & AMD)" : "Hardware Night Vision for Rust (NVIDIA & AMD)";
            btnLang.Text = isRU ? "🌐 EN" : "🌐 RU";

            grpGamma.Text = isRU ? "1. Яркость ночью (Гамма)" : "1. Night Brightness (Gamma)";
            lblGDesc.Text = isRU ? "Рекомендуется 1.40 – 1.60. Стандартная дневная гамма: 1.00" : "Recommended 1.40 – 1.60. Default day gamma: 1.00";
            if (!isTestingGamma) btnTestGamma.Text = isRU ? "Проверить (2 сек)" : "Test (2 sec)";

            grpControls.Text = isRU ? "2. Горячая клавиша и монитор" : "2. Hotkey & Target Display";
            lblKey.Text = isRU ? "Клавиша переключения:" : "Toggle Hotkey:";
            lblMon.Text = isRU ? "Целевой экран:" : "Target Display:";

            // Обновляем список горячих клавиш
            int curKeyIdx = cbHotkey.SelectedIndex;
            cbHotkey.Items.Clear();
            for (int i = 0; i < hotkeyOptions.Count; i++)
            {
                cbHotkey.Items.Add(isRU ? hotkeyOptions[i].NameRU : hotkeyOptions[i].NameEN);
                if (hotkeyOptions[i].VK == config.HotkeyVK) curKeyIdx = i;
            }
            cbHotkey.SelectedIndex = Math.Max(0, curKeyIdx);

            // Обновляем список мониторов
            int curMonIdx = (config.MonitorMode == "ALL") ? 2 : ((config.MonitorMode == "PRIMARY") ? 1 : 0);
            cbMonitor.Items.Clear();
            if (isRU)
            {
                cbMonitor.Items.Add("Авто (AOC Q27G2G / Экран Rust)");
                cbMonitor.Items.Add("Только основной монитор");
                cbMonitor.Items.Add("Все подключенные мониторы");
            }
            else
            {
                cbMonitor.Items.Add("Auto (AOC Q27G2G / Rust Screen)");
                cbMonitor.Items.Add("Primary Display Only");
                cbMonitor.Items.Add("All Connected Displays");
            }
            cbMonitor.SelectedIndex = curMonIdx;

            grpAuto.Text = isRU ? "3. Автоматизация и звук" : "3. Automation & Sound";
            chkAutoDetect.Text = isRU ? "Следить за запуском Rust (активировать хоткей только в игре)" : "Watch Rust process (activate hotkey in-game only)";
            chkAutoRestore.Text = isRU ? "Автоматически сбрасывать гамму на 1.0 при закрытии игры" : "Auto-restore gamma to 1.0 on game exit";
            chkSound.Text = isRU ? "Звуковой сигнал при переключении (высокий / низкий тон)" : "Sound feedback on toggle (high / low tone)";
            chkAutoStart.Text = isRU ? "Запускать вместе с Windows (в трее около часов)" : "Start with Windows (system tray)";

            UpdateGameStatusLabel();

            lblVersion.Text = isRU ? "Версия 1.0  •  Сайт разработчика: " : "Version 1.0  •  Developer website: ";
            linkWebsite.Location = new Point(isRU ? 205 : 195, 415);

            btnSaveMinimize.Text = isRU ? "✔ Сохранить и свернуть в трей" : "✔ Save & Minimize to Tray";

            // Трей меню
            traySettingsItem.Text = isRU ? "⚙ Настройки..." : "⚙ Settings...";
            trayAutoStartItem.Text = isRU ? "Автозапуск с Windows" : "Start with Windows";
            trayExitItem.Text = isRU ? "Выход" : "Exit";

            UpdateTrayTooltip();
        }

        private void UpdateGameStatusLabel()
        {
            bool isRU = (config.Language == "RU");
            if (rustDetected)
            {
                lblGameStatus.Text = isRU ? "Статус: 🟢 Rust запущен (в игре!)" : "Status: 🟢 Rust is running (in-game!)";
                lblGameStatus.ForeColor = Color.FromArgb(70, 220, 100);
                trayStatusItem.Text = isRU ? "Rust запущен (в игре)" : "Rust is running (in-game)";
            }
            else
            {
                lblGameStatus.Text = isRU ? "Статус: ⚪ Ожидание запуска RustClient.exe..." : "Status: ⚪ Waiting for RustClient.exe...";
                lblGameStatus.ForeColor = Color.FromArgb(170, 175, 185);
                trayStatusItem.Text = isRU ? "Ожидание запуска Rust..." : "Waiting for Rust...";
            }
        }

        private GroupBox CreateGroupBox(string text, int top, int height)
        {
            GroupBox gb = new GroupBox();
            gb.Text = text;
            gb.Location = new Point(0, top);
            gb.Size = new Size(472, height);
            gb.ForeColor = Color.FromArgb(255, 180, 90);
            gb.Font = new Font("Segoe UI", 9.5f, FontStyle.Bold);
            return gb;
        }

        private void TestGamma2Sec()
        {
            if (isTestingGamma) return;
            isTestingGamma = true;
            btnTestGamma.Enabled = false;
            btnTestGamma.Text = (config.Language == "RU") ? "Тест (2 сек)..." : "Testing (2s)...";

            ApplyGamma(config.NightGamma);
            if (config.PlaySound)
            {
                ThreadPool.QueueUserWorkItem(_ => { try { Console.Beep(900, 100); } catch { } });
            }

            if (testTimer != null)
            {
                testTimer.Stop();
                testTimer.Dispose();
            }

            testTimer = new System.Windows.Forms.Timer();
            testTimer.Interval = 2000;
            testTimer.Tick += (s, e) => {
                testTimer.Stop();
                testTimer.Dispose();
                testTimer = null;

                if (!isNight)
                {
                    RestoreGamma();
                    if (config.PlaySound)
                    {
                        ThreadPool.QueueUserWorkItem(_ => { try { Console.Beep(450, 140); } catch { } });
                    }
                }

                btnTestGamma.Text = (config.Language == "RU") ? "Проверить (2 сек)" : "Test (2 sec)";
                btnTestGamma.Enabled = true;
                isTestingGamma = false;
            };
            testTimer.Start();
        }

        private void InitializeTray()
        {
            trayMenu = new ContextMenuStrip();
            var title = new ToolStripMenuItem("Rust Gamma Pro v1.0") { Enabled = false, Font = new Font(SystemFonts.DefaultFont, FontStyle.Bold) };
            trayStatusItem = new ToolStripMenuItem("...") { Enabled = false };
            
            traySettingsItem = new ToolStripMenuItem("⚙ Настройки...", null, (s, e) => ShowSettingsWindow());
            trayToggleItem = new ToolStripMenuItem("Переключить День/Ночь (" + config.HotkeyName + ")", null, (s, e) => ToggleGamma());
            trayAutoStartItem = new ToolStripMenuItem("Автозапуск с Windows", null, (s, e) => {
                bool newState = !IsAutoStartEnabled();
                SetAutoStart(newState);
                trayAutoStartItem.Checked = newState;
                chkAutoStart.Checked = newState;
            });
            trayAutoStartItem.Checked = IsAutoStartEnabled();

            trayExitItem = new ToolStripMenuItem("Выход", null, (s, e) => {
                allowActualClose = true;
                RestoreGamma();
                config.Save();
                Application.Exit();
            });

            trayMenu.Items.Add(title);
            trayMenu.Items.Add(trayStatusItem);
            trayMenu.Items.Add(new ToolStripSeparator());
            trayMenu.Items.Add(traySettingsItem);
            trayMenu.Items.Add(trayToggleItem);
            trayMenu.Items.Add(trayAutoStartItem);
            trayMenu.Items.Add(new ToolStripSeparator());
            trayMenu.Items.Add(trayExitItem);

            trayIcon = new NotifyIcon();
            trayIcon.ContextMenuStrip = trayMenu;
            trayIcon.Visible = true;

            string iconPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "app.ico");
            if (File.Exists(iconPath)) trayIcon.Icon = new Icon(iconPath);
            else trayIcon.Icon = SystemIcons.Application;

            trayIcon.DoubleClick += (s, e) => ShowSettingsWindow();
        }

        public void ShowSettingsWindow()
        {
            this.Show();
            this.WindowState = FormWindowState.Normal;
            this.BringToFront();
            this.Activate();
        }

        private void ApplyHotkey()
        {
            UnregisterHotKey(this.Handle, HOTKEY_ID);
            RegisterHotKey(this.Handle, HOTKEY_ID, MOD_NOREPEAT, config.HotkeyVK);
            UpdateTrayTooltip();
        }

        private void InitializeGamma()
        {
            DetectTargetDisplay();

            IntPtr hdc = CreateDC(null, activeDeviceName, null, IntPtr.Zero);
            if (hdc != IntPtr.Zero)
            {
                originalRamp = new RAMP();
                if (GetDeviceGammaRamp(hdc, ref originalRamp))
                {
                    hasOriginalRamp = true;
                }
                DeleteDC(hdc);
            }
        }

        private void DetectTargetDisplay()
        {
            DISPLAY_DEVICE dev = new DISPLAY_DEVICE();
            dev.cb = Marshal.SizeOf(dev);
            uint i = 0;
            string aocDev = null;
            string primaryDev = null;

            while (EnumDisplayDevices(null, i, ref dev, 0))
            {
                if ((dev.StateFlags & 1) != 0)
                {
                    bool isPrim = (dev.StateFlags & 4) != 0;
                    if (isPrim) primaryDev = dev.DeviceName;

                    DISPLAY_DEVICE mon = new DISPLAY_DEVICE();
                    mon.cb = Marshal.SizeOf(mon);
                    uint j = 0;
                    while (EnumDisplayDevices(dev.DeviceName, j, ref mon, 0))
                    {
                        string id = (mon.DeviceID ?? "").ToUpper();
                        string name = (mon.DeviceString ?? "").ToUpper();
                        if (id.Contains("AOC") || name.Contains("AOC") || id.Contains("2702"))
                        {
                            aocDev = dev.DeviceName;
                        }
                        j++;
                    }
                }
                i++;
            }

            if (config.MonitorMode == "PRIMARY") activeDeviceName = primaryDev ?? @"\\.\DISPLAY2";
            else activeDeviceName = aocDev ?? primaryDev ?? @"\\.\DISPLAY2";
        }

        private void CheckRustProcess()
        {
            Process[] procs = Process.GetProcessesByName("RustClient");
            bool nowRunning = procs.Length > 0;

            if (nowRunning && !rustDetected)
            {
                rustDetected = true;
                UpdateGameStatusLabel();

                string title = (config.Language == "RU") ? "Rust запущен!" : "Rust detected!";
                string body = (config.Language == "RU")
                    ? "Управление гаммой активировано.\nНажмите " + config.HotkeyName + " для режима ночи."
                    : "Gamma control active.\nPress " + config.HotkeyName + " for night mode.";
                trayIcon.ShowBalloonTip(3000, title, body, ToolTipIcon.Info);

                try
                {
                    if (procs[0].MainWindowHandle != IntPtr.Zero)
                    {
                        IntPtr hMon = MonitorFromWindow(procs[0].MainWindowHandle, 2);
                        if (hMon != IntPtr.Zero)
                        {
                            MONITORINFOEX mi = new MONITORINFOEX();
                            mi.cbSize = Marshal.SizeOf(mi);
                            if (GetMonitorInfo(hMon, ref mi))
                            {
                                activeDeviceName = mi.szDevice;
                            }
                        }
                    }
                }
                catch { }

                UpdateTrayTooltip();
            }
            else if (!nowRunning && rustDetected)
            {
                rustDetected = false;
                UpdateGameStatusLabel();
                
                if (config.AutoRestoreOnExit && isNight)
                {
                    RestoreGamma();
                    isNight = false;
                }
                UpdateTrayTooltip();
            }

            if (isNight)
            {
                ApplyGamma(config.NightGamma);
            }
        }

        private void WatcherTimer_Tick(object sender, EventArgs e)
        {
            CheckRustProcess();
        }

        private void ToggleGamma()
        {
            if (config.AutoDetectRust && !rustDetected)
            {
                return;
            }

            isNight = !isNight;
            if (isNight)
            {
                ApplyGamma(config.NightGamma);
                if (config.PlaySound) ThreadPool.QueueUserWorkItem(_ => { try { Console.Beep(900, 100); } catch { } });
            }
            else
            {
                RestoreGamma();
                if (config.PlaySound) ThreadPool.QueueUserWorkItem(_ => { try { Console.Beep(450, 140); } catch { } });
            }
            UpdateTrayTooltip();
        }

        private void ApplyGamma(double gammaVal)
        {
            if (config.MonitorMode == "ALL")
            {
                DISPLAY_DEVICE dev = new DISPLAY_DEVICE();
                dev.cb = Marshal.SizeOf(dev);
                uint i = 0;
                while (EnumDisplayDevices(null, i, ref dev, 0))
                {
                    if ((dev.StateFlags & 1) != 0) ApplyGammaToDevice(dev.DeviceName, gammaVal);
                    i++;
                }
            }
            else
            {
                ApplyGammaToDevice(activeDeviceName, gammaVal);
            }
        }

        private void ApplyGammaToDevice(string deviceName, double gammaVal)
        {
            IntPtr hdc = CreateDC(null, deviceName, null, IntPtr.Zero);
            if (hdc != IntPtr.Zero)
            {
                RAMP ramp = new RAMP();
                ramp.Red = new ushort[256];
                ramp.Green = new ushort[256];
                ramp.Blue = new ushort[256];

                for (int i = 0; i < 256; i++)
                {
                    ushort val = (ushort)Math.Min(65535, Math.Max(0, Math.Pow(i / 255.0, 1.0 / gammaVal) * 65535.0 + 0.5));
                    ramp.Red[i] = val;
                    ramp.Green[i] = val;
                    ramp.Blue[i] = val;
                }

                SetDeviceGammaRamp(hdc, ref ramp);
                DeleteDC(hdc);
            }
        }

        private void RestoreGamma()
        {
            if (config.MonitorMode == "ALL")
            {
                DISPLAY_DEVICE dev = new DISPLAY_DEVICE();
                dev.cb = Marshal.SizeOf(dev);
                uint i = 0;
                while (EnumDisplayDevices(null, i, ref dev, 0))
                {
                    if ((dev.StateFlags & 1) != 0) ApplyGammaToDevice(dev.DeviceName, 1.0);
                    i++;
                }
            }
            else
            {
                IntPtr hdc = CreateDC(null, activeDeviceName, null, IntPtr.Zero);
                if (hdc != IntPtr.Zero)
                {
                    if (hasOriginalRamp) SetDeviceGammaRamp(hdc, ref originalRamp);
                    else ApplyGammaToDevice(activeDeviceName, 1.0);
                    DeleteDC(hdc);
                }
            }
        }

        private void UpdateTrayTooltip()
        {
            bool isRU = (config.Language == "RU");
            string state = isNight 
                ? (isRU ? string.Format("НОЧЬ ({0:0.00})", config.NightGamma) : string.Format("NIGHT ({0:0.00})", config.NightGamma))
                : (isRU ? "ДЕНЬ (1.00)" : "DAY (1.00)");
            string rustState = rustDetected 
                ? (isRU ? "Rust: В игре" : "Rust: In-game")
                : (isRU ? "Rust: Ожидание" : "Rust: Waiting");

            trayIcon.Text = string.Format("Rust Gamma Pro\n{0} | {1}\n{2}: {3}", 
                rustState, state, isRU ? "Хоткей" : "Hotkey", config.HotkeyName);
            trayToggleItem.Text = isNight 
                ? (isRU ? "Выключить ночь (вернуть 1.0)" : "Turn Night OFF (restore 1.0)")
                : (isRU ? string.Format("Включить ночь ({0})", config.HotkeyName) : string.Format("Turn Night ON ({0})", config.HotkeyName));
        }

        private bool IsAutoStartEnabled()
        {
            try
            {
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run", false))
                {
                    return key != null && key.GetValue("RustGammaPro") != null;
                }
            }
            catch { return false; }
        }

        private void SetAutoStart(bool enable)
        {
            try
            {
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run", true))
                {
                    if (key == null) return;
                    if (enable)
                    {
                        key.SetValue("RustGammaPro", "\"" + Application.ExecutablePath + "\" /minimized");
                    }
                    else
                    {
                        if (key.GetValue("RustGammaPro") != null) key.DeleteValue("RustGammaPro", false);
                    }
                }
            }
            catch { }
        }

        protected override void WndProc(ref Message m)
        {
            const int WM_HOTKEY = 0x0312;
            const int WM_QUERYENDSESSION = 0x0011;
            const int WM_ENDSESSION = 0x0016;

            if (m.Msg == WM_HOTKEY && m.WParam.ToInt32() == HOTKEY_ID)
            {
                ToggleGamma();
            }
            else if (m.Msg == WM_QUERYENDSESSION || m.Msg == WM_ENDSESSION)
            {
                RestoreGamma();
            }

            base.WndProc(ref m);
        }
    }
}
