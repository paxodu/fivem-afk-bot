using System;
using System.Diagnostics;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace Apelsinai
{
    static class Program
    {
        [STAThread]
        [DllImport("user32.dll")]
        static extern bool SetProcessDPIAware();

        static void Main()
        {
            SetProcessDPIAware();
            if (RestartAsAdmin())
            {
                return;
            }
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new ApelsinasForm());
        }

        static bool RestartAsAdmin()
        {
            if (IsAdmin())
            {
                return false;
            }
            try
            {
                var start = new ProcessStartInfo();
                start.FileName = Application.ExecutablePath;
                start.UseShellExecute = true;
                start.Verb = "runas";
                Process.Start(start);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        static bool IsAdmin()
        {
            try
            {
                var identity = WindowsIdentity.GetCurrent();
                var principal = new WindowsPrincipal(identity);
                return principal.IsInRole(WindowsBuiltInRole.Administrator);
            }
            catch (Exception)
            {
                return false;
            }
        }
    }

    sealed class ApelsinasForm : Form
    {
        const int WmHotkey = 0x0312;

        readonly Label status = new Label();
        readonly Button toggle = new Button();
        readonly TextBox log = new TextBox();
        readonly System.Windows.Forms.Timer clock = new System.Windows.Forms.Timer();

        volatile bool running = true;
        int misses;
        int clicksOnThis;
        int lastClickTick;
        bool warnedBlocked;

        [DllImport("user32.dll")]
        static extern bool RegisterHotKey(IntPtr hwnd, int id, uint mods, uint vk);

        [DllImport("user32.dll")]
        static extern bool UnregisterHotKey(IntPtr hwnd, int id);

        [DllImport("user32.dll")]
        static extern bool SetCursorPos(int x, int y);

        [DllImport("user32.dll", SetLastError = true)]
        static extern uint SendInput(uint count, IntPtr inputs, int size);

        [DllImport("user32.dll")]
        static extern bool ClipCursor(IntPtr rect);

        [DllImport("user32.dll")]
        static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        static extern bool SetForegroundWindow(IntPtr hwnd);

        [DllImport("user32.dll")]
        static extern bool ShowWindow(IntPtr hwnd, int command);

        [DllImport("user32.dll")]
        static extern bool IsWindowVisible(IntPtr hwnd);

        [DllImport("user32.dll")]
        static extern bool AttachThreadInput(uint from, uint to, bool attach);

        [DllImport("user32.dll")]
        static extern uint GetWindowThreadProcessId(IntPtr hwnd, out uint processId);

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        static extern int GetWindowText(IntPtr hwnd, StringBuilder text, int count);

        [DllImport("user32.dll")]
        static extern bool EnumWindows(EnumWindowsProc callback, IntPtr lParam);

        [DllImport("kernel32.dll")]
        static extern uint GetCurrentThreadId();

        delegate bool EnumWindowsProc(IntPtr hwnd, IntPtr lParam);

        public ApelsinasForm()
        {
            Text = "Apelsinai";
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            ClientSize = new Size(420, 360);
            StartPosition = FormStartPosition.CenterScreen;
            BackColor = Color.FromArgb(20, 12, 8);
            Font = new Font("Segoe UI", 10f);

            var title = new Label();
            title.Text = "Apelsinai";
            title.ForeColor = Color.FromArgb(255, 244, 234);
            title.Font = new Font("Segoe UI", 16f, FontStyle.Bold);
            title.Bounds = new Rectangle(22, 16, 376, 32);

            var hint = new Label();
            hint.Text = "Kai ant apelsino matai baltą ratą, paspaudžiu pelę ant jo. Raudoną ratą praleidžiu. FiveM laikyk lango režimu. Jei žaidimas paleistas kaip administratorius, šitą programą paleisk taip pat.";
            hint.ForeColor = Color.FromArgb(210, 184, 164);
            hint.Bounds = new Rectangle(22, 54, 376, 58);

            status.Text = "Laukiu balto rato";
            status.ForeColor = Color.FromArgb(255, 138, 42);
            status.Font = new Font("Segoe UI", 11f, FontStyle.Bold);
            status.Bounds = new Rectangle(22, 118, 376, 28);

            toggle.Text = "Stabdyti";
            toggle.Bounds = new Rectangle(22, 156, 140, 38);
            toggle.FlatStyle = FlatStyle.Flat;
            toggle.FlatAppearance.BorderSize = 0;
            toggle.BackColor = Color.FromArgb(255, 138, 42);
            toggle.ForeColor = Color.FromArgb(42, 18, 4);
            toggle.Click += delegate { SetRunning(!running); };

            var hotkey = new Label();
            hotkey.Text = "F8 pristabdo arba vėl įjungia.";
            hotkey.ForeColor = Color.FromArgb(210, 184, 164);
            hotkey.Bounds = new Rectangle(176, 164, 220, 24);

            log.Multiline = true;
            log.ReadOnly = true;
            log.BorderStyle = BorderStyle.None;
            log.BackColor = Color.FromArgb(36, 22, 15);
            log.ForeColor = Color.FromArgb(255, 244, 234);
            log.Bounds = new Rectangle(22, 210, 376, 128);
            log.ScrollBars = ScrollBars.Vertical;

            Controls.Add(title);
            Controls.Add(hint);
            Controls.Add(status);
            Controls.Add(toggle);
            Controls.Add(hotkey);
            Controls.Add(log);
            Write("Laukiu apelsino su baltu ratu.");

            clock.Interval = 180;
            clock.Tick += delegate { Look(); };
            clock.Start();
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            RegisterHotKey(Handle, 1, 0, 0x77);
        }

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == WmHotkey && m.WParam.ToInt32() == 1)
            {
                SetRunning(!running);
                return;
            }
            base.WndProc(ref m);
        }

        void SetRunning(bool on)
        {
            running = on;
            if (on)
            {
                toggle.Text = "Stabdyti";
                toggle.BackColor = Color.FromArgb(255, 138, 42);
                toggle.ForeColor = Color.FromArgb(42, 18, 4);
                status.Text = "Laukiu balto rato";
                Write("Vėl stebiu.");
            }
            else
            {
                toggle.Text = "Stebėti";
                toggle.BackColor = Color.FromArgb(58, 42, 34);
                toggle.ForeColor = Color.FromArgb(255, 244, 234);
                status.Text = "Sustabdyta";
                Write("Sustabdyta.");
            }
        }

        void Look()
        {
            if (!running)
            {
                return;
            }
            try
            {
                Scan();
            }
            catch (Exception ex)
            {
                Write("Klaida: " + ex.Message);
            }
        }

        void Scan()
        {
            Rectangle area = SystemInformation.VirtualScreen;
            using (var bitmap = new Bitmap(area.Width, area.Height))
            {
                using (var graphics = Graphics.FromImage(bitmap))
                {
                    try
                    {
                        graphics.CopyFromScreen(area.X, area.Y, 0, 0, area.Size, CopyPixelOperation.SourceCopy | CopyPixelOperation.CaptureBlt);
                    }
                    catch (Exception)
                    {
                        graphics.CopyFromScreen(area.X, area.Y, 0, 0, area.Size);
                    }
                }
                Hit hit = Finder.Find(bitmap);
                if (hit == null)
                {
                    misses++;
                    if (misses >= 3)
                    {
                        clicksOnThis = 0;
                    }
                    if (clicksOnThis == 0 && running)
                    {
                        status.Text = "Laukiu balto rato";
                    }
                    return;
                }
                misses = 0;
                int now = Environment.TickCount;
                if (clicksOnThis >= 4)
                {
                    return;
                }
                if (clicksOnThis > 0 && unchecked(now - lastClickTick) < 450)
                {
                    return;
                }
                int x = area.X + hit.X;
                int y = area.Y + hit.Y;
                FocusGame();
                bool reached = ClickFruit(x, y, area);
                clicksOnThis++;
                lastClickTick = Environment.TickCount;
                status.Text = "Spusteliu";
                Write("Matau baltą ratą, spusteliu ant apelsino.");
                if (!reached && !warnedBlocked)
                {
                    warnedBlocked = true;
                    Write("Windows užblokavo pelę. Uždaryk ir paleisk programą kaip administratorius, jei FiveM irgi taip paleistas.");
                }
            }
        }

        static void FocusGame()
        {
            IntPtr game = FindGameWindow();
            if (game == IntPtr.Zero)
            {
                return;
            }
            IntPtr foreground = GetForegroundWindow();
            if (foreground == game)
            {
                return;
            }
            uint ignored;
            uint foregroundThread = GetWindowThreadProcessId(foreground, out ignored);
            uint gameThread = GetWindowThreadProcessId(game, out ignored);
            uint mine = GetCurrentThreadId();
            AttachThreadInput(mine, foregroundThread, true);
            AttachThreadInput(mine, gameThread, true);
            ShowWindow(game, 9);
            SetForegroundWindow(game);
            AttachThreadInput(mine, gameThread, false);
            AttachThreadInput(mine, foregroundThread, false);
        }

        static IntPtr FindGameWindow()
        {
            IntPtr found = IntPtr.Zero;
            EnumWindows(delegate(IntPtr hwnd, IntPtr lParam)
            {
                if (!IsWindowVisible(hwnd) || !IsGameWindow(hwnd))
                {
                    return true;
                }
                found = hwnd;
                return false;
            }, IntPtr.Zero);
            return found;
        }

        static bool IsGameWindow(IntPtr hwnd)
        {
            uint processId;
            GetWindowThreadProcessId(hwnd, out processId);
            string name = "";
            try
            {
                using (Process process = Process.GetProcessById((int)processId))
                {
                    name = process.ProcessName ?? "";
                }
            }
            catch (Exception)
            {
                name = "";
            }
            string lower = name.ToLowerInvariant();
            if (lower.Contains("fivem") || lower.Contains("gtaprocess") || lower == "gta5")
            {
                return true;
            }
            var title = new StringBuilder(512);
            GetWindowText(hwnd, title, title.Capacity);
            string text = title.ToString();
            return text.IndexOf("FiveM", StringComparison.OrdinalIgnoreCase) >= 0
                || text.IndexOf("Grand Theft Auto", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        static bool ClickFruit(int x, int y, Rectangle screen)
        {
            ClipCursor(IntPtr.Zero);
            SetCursorPos(x, y);
            Thread.Sleep(70);
            bool moved = SendMouse(x, y, screen, 0x0001);
            Thread.Sleep(40);
            bool down = SendMouse(x, y, screen, 0x0002);
            Thread.Sleep(80);
            bool up = SendMouse(x, y, screen, 0x0004);
            return moved && down && up;
        }

        static bool SendMouse(int x, int y, Rectangle screen, int button)
        {
            bool x64 = IntPtr.Size == 8;
            int size = x64 ? 40 : 28;
            int at = x64 ? 8 : 4;
            var data = new byte[size];
            WriteI32(data, at, Finder.AbsoluteAxis(x, screen.X, screen.Width));
            WriteI32(data, at + 4, Finder.AbsoluteAxis(y, screen.Y, screen.Height));
            int flags = button | 0x0001 | 0x8000 | 0x4000;
            WriteI32(data, at + 12, flags);
            IntPtr raw = Marshal.AllocHGlobal(size);
            try
            {
                Marshal.Copy(data, 0, raw, size);
                return SendInput(1, raw, size) == 1;
            }
            finally
            {
                Marshal.FreeHGlobal(raw);
            }
        }

        static void WriteI32(byte[] data, int at, int value)
        {
            uint bits = unchecked((uint)value);
            data[at] = (byte)bits;
            data[at + 1] = (byte)(bits >> 8);
            data[at + 2] = (byte)(bits >> 16);
            data[at + 3] = (byte)(bits >> 24);
        }

        void Write(string line)
        {
            log.AppendText(DateTime.Now.ToString("HH:mm:ss") + "  " + line + Environment.NewLine);
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            running = false;
            clock.Stop();
            UnregisterHotKey(Handle, 1);
            base.OnFormClosing(e);
        }
    }
}
