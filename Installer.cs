using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.IO.Compression;
using System.Reflection;
using System.Windows.Forms;
using Microsoft.Win32;

namespace RustGammaSetup
{
    static class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            bool isUninstall = false;
            foreach (string arg in args)
            {
                if (arg.Equals("/uninstall", StringComparison.OrdinalIgnoreCase) ||
                    arg.Equals("--uninstall", StringComparison.OrdinalIgnoreCase))
                {
                    isUninstall = true;
                }
            }

            string exeName = Path.GetFileName(Application.ExecutablePath).ToLowerInvariant();
            if (exeName.Contains("uninstall"))
            {
                isUninstall = true;
            }

            if (isUninstall)
            {
                RunUninstaller();
            }
            else
            {
                Application.Run(new SetupForm());
            }
        }

        static void RunUninstaller()
        {
            DialogResult dr = MessageBox.Show(
                "Вы действительно хотите удалить Rust Gamma Pro и все его компоненты с вашего компьютера?",
                "Удаление Rust Gamma Pro",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (dr != DialogResult.Yes) return;

            try
            {
                // Закрываем RustGammaPro, если он запущен
                foreach (Process p in Process.GetProcessesByName("RustGammaPro"))
                {
                    try { p.Kill(); } catch { }
                }

                string installDir = AppDomain.CurrentDomain.BaseDirectory.TrimEnd('\\', '/');

                // Удаляем ярлык с Рабочего стола
                string desktopLnk = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "Rust Gamma Pro.lnk");
                if (File.Exists(desktopLnk))
                {
                    try { File.Delete(desktopLnk); } catch { }
                }

                // Удаляем папку в меню Пуск
                string startMenuDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.StartMenu), @"Programs\Rust Gamma Pro");
                if (Directory.Exists(startMenuDir))
                {
                    try { Directory.Delete(startMenuDir, true); } catch { }
                }

                // Удаляем запись автозапуска
                using (RegistryKey runKey = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run", true))
                {
                    if (runKey != null && runKey.GetValue("RustGammaPro") != null)
                    {
                        runKey.DeleteValue("RustGammaPro", false);
                    }
                }

                // Удаляем запись деинсталляции
                using (RegistryKey uninstKey = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Uninstall", true))
                {
                    if (uninstKey != null)
                    {
                        uninstKey.DeleteSubKeyTree("RustGammaPro", false);
                    }
                }

                // Самоудаление папки программы в фоне через 1.5 секунды
                ProcessStartInfo psi = new ProcessStartInfo();
                psi.FileName = "cmd.exe";
                psi.Arguments = string.Format("/c ping 127.0.0.1 -n 2 > nul & rmdir /s /q \"{0}\"", installDir);
                psi.WindowStyle = ProcessWindowStyle.Hidden;
                psi.CreateNoWindow = true;
                Process.Start(psi);

                MessageBox.Show("Rust Gamma Pro был успешно удален с вашего компьютера.", "Удаление завершено", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка при удалении: " + ex.Message, "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }

    public class SetupForm : Form
    {
        private TextBox txtPath;
        private Button btnBrowse;
        private CheckBox chkDesktop;
        private CheckBox chkStartMenu;
        private CheckBox chkAutoStart;
        private CheckBox chkLaunchNow;
        private ProgressBar progressBar;
        private Label lblStatus;
        private Button btnInstall;
        private bool isFinished = false;

        public SetupForm()
        {
            BuildUI();
        }

        private void BuildUI()
        {
            this.Text = "Установка Rust Gamma Pro v1.0.0";
            this.Size = new Size(500, 480);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor = Color.FromArgb(24, 25, 29);
            this.ForeColor = Color.FromArgb(235, 235, 240);
            this.Font = new Font("Segoe UI", 9f);

            // Шапка
            Panel header = new Panel() { Dock = DockStyle.Top, Height = 75, BackColor = Color.FromArgb(18, 19, 23) };
            
            PictureBox logo = new PictureBox() { Size = new Size(54, 54), Location = new Point(16, 10), SizeMode = PictureBoxSizeMode.Zoom };
            string iconClean = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "icon_clean.png");
            if (File.Exists(iconClean)) logo.Image = Image.FromFile(iconClean);
            
            Label title = new Label() { Text = "RUST GAMMA PRO", Location = new Point(78, 14), AutoSize = true, Font = new Font("Segoe UI", 13.5f, FontStyle.Bold), ForeColor = Color.FromArgb(255, 140, 40) };
            Label sub = new Label() { Text = "Мастер установки на ваш компьютер", Location = new Point(80, 42), AutoSize = true, Font = new Font("Segoe UI", 8.5f), ForeColor = Color.FromArgb(160, 165, 175) };

            header.Controls.Add(logo);
            header.Controls.Add(title);
            header.Controls.Add(sub);
            this.Controls.Add(header);

            // Контент
            Panel content = new Panel() { Location = new Point(16, 85), Size = new Size(452, 310) };

            // Папка установки
            GroupBox grpPath = new GroupBox() { Text = "Папка установки:", Location = new Point(0, 10), Size = new Size(452, 75), ForeColor = Color.FromArgb(255, 180, 90), Font = new Font("Segoe UI", 9f, FontStyle.Bold) };
            
            string defaultDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), @"Programs\Rust Gamma Pro");
            txtPath = new TextBox() { Text = defaultDir, Location = new Point(14, 28), Size = new Size(330, 25), BackColor = Color.FromArgb(36, 38, 44), ForeColor = Color.White, Font = new Font("Segoe UI", 9f) };
            
            btnBrowse = new Button() { Text = "Обзор...", Location = new Point(352, 26), Size = new Size(85, 27), FlatStyle = FlatStyle.Flat, BackColor = Color.FromArgb(45, 47, 55), ForeColor = Color.White, Font = new Font("Segoe UI", 8.5f) };
            btnBrowse.Click += (s, e) => {
                using (FolderBrowserDialog fbd = new FolderBrowserDialog())
                {
                    fbd.SelectedPath = txtPath.Text;
                    if (fbd.ShowDialog() == DialogResult.OK)
                    {
                        txtPath.Text = fbd.SelectedPath;
                    }
                }
            };

            grpPath.Controls.Add(txtPath);
            grpPath.Controls.Add(btnBrowse);
            content.Controls.Add(grpPath);

            // Опции ярлыков и запуска
            GroupBox grpOptions = new GroupBox() { Text = "Дополнительные действия:", Location = new Point(0, 95), Size = new Size(452, 130), ForeColor = Color.FromArgb(255, 180, 90), Font = new Font("Segoe UI", 9f, FontStyle.Bold) };
            
            chkDesktop = new CheckBox() { Text = "Создать ярлык на Рабочем столе", Location = new Point(16, 24), AutoSize = true, Checked = true, Font = new Font("Segoe UI", 9f) };
            chkStartMenu = new CheckBox() { Text = "Создать ярлык в меню «Пуск»", Location = new Point(16, 50), AutoSize = true, Checked = true, Font = new Font("Segoe UI", 9f) };
            chkAutoStart = new CheckBox() { Text = "Запускать автоматически вместе с Windows", Location = new Point(16, 76), AutoSize = true, Checked = true, Font = new Font("Segoe UI", 9f) };
            chkLaunchNow = new CheckBox() { Text = "Запустить Rust Gamma Pro после установки", Location = new Point(16, 102), AutoSize = true, Checked = true, Font = new Font("Segoe UI", 9f) };

            grpOptions.Controls.Add(chkDesktop);
            grpOptions.Controls.Add(chkStartMenu);
            grpOptions.Controls.Add(chkAutoStart);
            grpOptions.Controls.Add(chkLaunchNow);
            content.Controls.Add(grpOptions);

            // Прогресс
            progressBar = new ProgressBar() { Location = new Point(0, 240), Size = new Size(452, 18), Visible = false };
            lblStatus = new Label() { Text = "Нажмите «Установить» для начала копирования файлов.", Location = new Point(0, 265), AutoSize = true, ForeColor = Color.FromArgb(160, 165, 175) };

            content.Controls.Add(progressBar);
            content.Controls.Add(lblStatus);
            this.Controls.Add(content);

            // Подвал с кнопкой
            Panel footer = new Panel() { Dock = DockStyle.Bottom, Height = 60, BackColor = Color.FromArgb(18, 19, 23) };
            
            btnInstall = new Button()
            {
                Text = "Установить",
                Location = new Point(146, 12),
                Size = new Size(200, 36),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(235, 100, 30),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 10f, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnInstall.FlatAppearance.BorderSize = 0;
            btnInstall.Click += (s, e) => {
                if (isFinished)
                {
                    this.Close();
                }
                else
                {
                    DoInstall();
                }
            };

            footer.Controls.Add(btnInstall);
            this.Controls.Add(footer);
        }

        private void DoInstall()
        {
            string targetDir = txtPath.Text.Trim();
            if (string.IsNullOrEmpty(targetDir)) return;

            btnInstall.Enabled = false;
            btnBrowse.Enabled = false;
            txtPath.Enabled = false;
            chkDesktop.Enabled = false;
            chkStartMenu.Enabled = false;
            chkAutoStart.Enabled = false;
            chkLaunchNow.Enabled = false;

            progressBar.Visible = true;
            progressBar.Style = ProgressBarStyle.Marquee;
            lblStatus.Text = "Распаковка и установка файлов...";

            try
            {
                if (!Directory.Exists(targetDir))
                {
                    Directory.CreateDirectory(targetDir);
                }

                // 1. Распаковываем встроенный ZIP-архив
                using (Stream resStream = Assembly.GetExecutingAssembly().GetManifestResourceStream("payload.zip"))
                {
                    if (resStream == null)
                    {
                        throw new Exception("Не найден встроенный пакет установки (payload.zip)!");
                    }

                    using (ZipArchive archive = new ZipArchive(resStream, ZipArchiveMode.Read))
                    {
                        foreach (ZipArchiveEntry entry in archive.Entries)
                        {
                            string destPath = Path.Combine(targetDir, entry.FullName);
                            string destDir = Path.GetDirectoryName(destPath);
                            if (!Directory.Exists(destDir)) Directory.CreateDirectory(destDir);
                            if (!string.IsNullOrEmpty(entry.Name))
                            {
                                entry.ExtractToFile(destPath, true);
                            }
                        }
                    }
                }

                // 2. Копируем себя как Uninstall.exe
                string uninstallerPath = Path.Combine(targetDir, "Uninstall.exe");
                try
                {
                    File.Copy(Application.ExecutablePath, uninstallerPath, true);
                }
                catch { }

                string exePath = Path.Combine(targetDir, "RustGammaPro.exe");

                // 3. Создаем ярлык на Рабочем столе
                if (chkDesktop.Checked)
                {
                    string desktopPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "Rust Gamma Pro.lnk");
                    CreateShortcut(desktopPath, exePath, targetDir);
                }

                // 4. Создаем ярлыки в меню Пуск
                if (chkStartMenu.Checked)
                {
                    string startMenuDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.StartMenu), @"Programs\Rust Gamma Pro");
                    if (!Directory.Exists(startMenuDir)) Directory.CreateDirectory(startMenuDir);

                    string appLnk = Path.Combine(startMenuDir, "Rust Gamma Pro.lnk");
                    CreateShortcut(appLnk, exePath, targetDir);

                    string uninstLnk = Path.Combine(startMenuDir, "Удалить Rust Gamma Pro.lnk");
                    CreateShortcut(uninstLnk, uninstallerPath, targetDir);
                }

                // 5. Автозапуск с Windows
                if (chkAutoStart.Checked)
                {
                    using (RegistryKey runKey = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run", true))
                    {
                        if (runKey != null)
                        {
                            runKey.SetValue("RustGammaPro", "\"" + exePath + "\" /minimized");
                        }
                    }
                }

                // 6. Регистрация в "Установка и удаление программ"
                using (RegistryKey uninstKey = Registry.CurrentUser.CreateSubKey(@"Software\Microsoft\Windows\CurrentVersion\Uninstall\RustGammaPro"))
                {
                    if (uninstKey != null)
                    {
                        uninstKey.SetValue("DisplayName", "Rust Gamma Pro");
                        uninstKey.SetValue("DisplayVersion", "1.0.0");
                        uninstKey.SetValue("DisplayIcon", "\"" + exePath + "\",0");
                        uninstKey.SetValue("Publisher", "helliong");
                        uninstKey.SetValue("URLInfoAbout", "https://helliong.space");
                        uninstKey.SetValue("InstallLocation", targetDir);
                        uninstKey.SetValue("UninstallString", "\"" + uninstallerPath + "\" /uninstall");
                        uninstKey.SetValue("QuietUninstallString", "\"" + uninstallerPath + "\" /uninstall /quiet");
                        uninstKey.SetValue("NoModify", 1, RegistryValueKind.DWord);
                        uninstKey.SetValue("NoRepair", 1, RegistryValueKind.DWord);
                    }
                }

                progressBar.Style = ProgressBarStyle.Continuous;
                progressBar.Value = 100;
                lblStatus.Text = "Установка успешно завершена!";
                lblStatus.ForeColor = Color.FromArgb(70, 220, 100);

                btnInstall.Text = "Завершить";
                btnInstall.Enabled = true;
                isFinished = true;

                if (chkLaunchNow.Checked)
                {
                    Process.Start(new ProcessStartInfo(exePath) { WorkingDirectory = targetDir });
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка при установке: " + ex.Message, "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                btnInstall.Enabled = true;
                lblStatus.Text = "Ошибка установки.";
                lblStatus.ForeColor = Color.FromArgb(255, 100, 100);
            }
        }

        private static void CreateShortcut(string shortcutPath, string targetPath, string workingDir)
        {
            try
            {
                Type shellType = Type.GetTypeFromProgID("WScript.Shell");
                if (shellType == null) return;
                dynamic shell = Activator.CreateInstance(shellType);
                dynamic shortcut = shell.CreateShortcut(shortcutPath);
                shortcut.TargetPath = targetPath;
                shortcut.WorkingDirectory = workingDir;
                shortcut.IconLocation = targetPath + ",0";
                shortcut.Description = "Rust Gamma Pro";
                shortcut.Save();
            }
            catch { }
        }
    }
}
