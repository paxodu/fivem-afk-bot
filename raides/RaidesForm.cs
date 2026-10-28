using System;
using System.Diagnostics;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace Raides
{
    static class Program
    {
        [DllImport("user32.dll")]
        static extern bool SetProcessDPIAware();

        [STAThread]
        static void Main()
        {
            SetProcessDPIAware();
            if (RestartAsAdmin())
            {
                return;
            }
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new RaidesForm());
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

    sealed class RaidesForm : Form
    {
        const int WmHotkey = 0x0312;

        readonly Label status = new Label();
        readonly Button toggle = new Button();
        readonly TextBox log = new TextBox();
        readonly System.Windows.Forms.Timer clock = new System.Windows.Forms.Timer();

        volatile bool running = true;
        int misses;
        int repeats;
        char lastLetter;
        int lastPressTick;
        bool warnedBlocked;
        IntPtr cachedGame;
        int cachedGameTick;

        [DllImport("user32.dll")]
        static extern bool RegisterHotKey(IntPtr hwnd, int id, uint mods, uint vk);

        [DllImport("user32.dll")]
        static extern bool UnregisterHotKey(IntPtr hwnd, int id);

        [DllImport("user32.dll", SetLastError = true)]
        static extern uint SendInput(uint count, IntPtr inputs, int size);

        [DllImport("user32.dll")]
        static extern uint MapVirtualKey(uint code, uint map);

        [DllImport("user32.dll")]
        static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        static extern bool SetForegroundWindow(IntPtr hwnd);

        [DllImport("user32.dll")]
        static extern bool ShowWindow(IntPtr hwnd, int command);

        [DllImport("user32.dll")]
        static extern bool IsWindowVisible(IntPtr hwnd);

        [DllImport("user32.dll")]
        static extern bool IsWindow(IntPtr hwnd);

        [DllImport("user32.dll")]
        static extern bool AttachThreadInput(uint from, uint to, bool attach);

        [DllImport("user32.dll")]
        static extern uint GetWindowThreadProcessId(IntPtr hwnd, out uint processId);

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        static extern int GetWindowText(IntPtr hwnd, StringBuilder text, int count);

        [DllImport("user32.dll")]
        static extern bool EnumWindows(EnumWindowsProc callback, IntPtr lParam);

        [DllImport("user32.dll")]
        static extern bool GetClientRect(IntPtr hwnd, out WindowRect rect);

        [DllImport("user32.dll")]
        static extern bool ClientToScreen(IntPtr hwnd, ref CursorPoint point);

        [DllImport("kernel32.dll")]
        static extern uint GetCurrentThreadId();

        delegate bool EnumWindowsProc(IntPtr hwnd, IntPtr lParam);

        [StructLayout(LayoutKind.Sequential)]
        struct CursorPoint
        {
            public int X;
            public int Y;
        }

        [StructLayout(LayoutKind.Sequential)]
        struct WindowRect
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }

        public RaidesForm()
        {
            Text = "Žalios raidės";
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            ClientSize = new Size(420, 360);
            StartPosition = FormStartPosition.Manual;
            Location = new Point(12, 12);
            BackColor = Color.FromArgb(12, 22, 16);
            Font = new Font("Segoe UI", 10f);

            var title = new Label();
            title.Text = "Žalios raidės";
            title.ForeColor = Color.FromArgb(236, 252, 240);
            title.Font = new Font("Segoe UI", 16f, FontStyle.Bold);
            title.Bounds = new Rectangle(22, 16, 376, 32);

            var hint = new Label();
            hint.Text = "Kai raidė yra žalioje kortelėje, paspaudžiu ją. Tamsią raidę praleidžiu. FiveM laikyk lango režimu. Jei žaidimas paleistas kaip administratorius, šitą programą paleisk taip pat.";
            hint.ForeColor = Color.FromArgb(176, 214, 186);
            hint.Bounds = new Rectangle(22, 54, 376, 64);

            status.Text = "Laukiu žalios raidės";
            status.ForeColor = Color.FromArgb(64, 214, 96);
            status.Font = new Font("Segoe UI", 11f, FontStyle.Bold);
            status.Bounds = new Rectangle(22, 124, 376, 28);

            toggle.Text = "Stabdyti";
            toggle.Bounds = new Rectangle(22, 160, 140, 38);
            toggle.FlatStyle = FlatStyle.Flat;
            toggle.FlatAppearance.BorderSize = 0;
            toggle.BackColor = Color.FromArgb(64, 214, 96);
            toggle.ForeColor = Color.FromArgb(8, 32, 14);
            toggle.Click += delegate { SetRunning(!running); };

            var hotkey = new Label();
            hotkey.Text = "F7 pristabdo arba vėl įjungia.";
            hotkey.ForeColor = Color.FromArgb(176, 214, 186);
            hotkey.Bounds = new Rectangle(176, 168, 220, 24);

            log.Multiline = true;
            log.ReadOnly = true;
            log.BorderStyle = BorderStyle.None;
            log.BackColor = Color.FromArgb(18, 36, 26);
            log.ForeColor = Color.FromArgb(236, 252, 240);
            log.Bounds = new Rectangle(22, 210, 376, 128);
            log.ScrollBars = ScrollBars.Vertical;

            Controls.Add(title);
            Controls.Add(hint);
            Controls.Add(status);
            Controls.Add(toggle);
            Controls.Add(hotkey);
            Controls.Add(log);
            Write("Laukiu žalios raidės, pavyzdžiui D.");

            clock.Interval = 90;
            clock.Tick += delegate { Look(); };
            clock.Start();
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            RegisterHotKey(Handle, 1, 0, 0x76);
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
                toggle.BackColor = Color.FromArgb(64, 214, 96);
                toggle.ForeColor = Color.FromArgb(8, 32, 14);
                status.Text = "Laukiu žalios raidės";
                Write("Vėl stebiu.");
            }
            else
            {
                toggle.Text = "Stebėti";
                toggle.BackColor = Color.FromArgb(32, 52, 40);
                toggle.ForeColor = Color.FromArgb(236, 252, 240);
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
            Rectangle area = GameArea();
            LetterHit hit;
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
                hit = Letters.Find(bitmap);
            }
            if (hit == null)
            {
                misses++;
                if (misses >= 2)
                {
                    repeats = 0;
                    lastLetter = '\0';
                    if (running)
                    {
                        status.Text = "Laukiu žalios raidės";
                    }
                }
                return;
            }
            misses = 0;
            int now = Environment.TickCount;
            if (hit.Letter == lastLetter)
            {
                if (repeats >= 3)
                {
                    return;
                }
                if (repeats > 0 && unchecked(now - lastPressTick) < 420)
                {
                    return;
                }
            }
            else
            {
                repeats = 0;
            }
            FocusGame();
            bool reached = Press(hit.Letter);
            lastLetter = hit.Letter;
            repeats++;
            lastPressTick = Environment.TickCount;
            status.Text = "Spusteliu " + hit.Letter;
            Write("Žalia raidė " + hit.Letter + ", spaudžiu.");
            if (!reached && !warnedBlocked)
            {
                warnedBlocked = true;
                Write("Windows užblokavo klavišą. Paleisk programą kaip administratorius, jei FiveM irgi taip paleistas.");
            }
        }

        static bool Press(char letter)
        {
            uint vk = letter;
            ushort scan = (ushort)MapVirtualKey(vk, 0);
            if (scan == 0)
            {
                return false;
            }
            bool down = SendKey(scan, false);
            Thread.Sleep(55);
            bool up = SendKey(scan, true);
            return down && up;
        }

        static bool SendKey(ushort scan, bool up)
        {
            bool x64 = IntPtr.Size == 8;
            int size = x64 ? 40 : 28;
            int at = x64 ? 8 : 4;
            var data = new byte[size];
            data[0] = 1;
            data[at + 2] = (byte)(scan & 0xFF);
            data[at + 3] = (byte)(scan >> 8);
            int flags = up ? 10 : 8;
            data[at + 4] = (byte)flags;
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

        void FocusGame()
        {
            IntPtr game = GameWindow();
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

        Rectangle GameArea()
        {
            IntPtr game = GameWindow();
            WindowRect rect;
            var origin = new CursorPoint();
            if (game != IntPtr.Zero && GetClientRect(game, out rect) && ClientToScreen(game, ref origin))
            {
                int width = rect.Right - rect.Left;
                int height = rect.Bottom - rect.Top;
                if (width > 200 && height > 200)
                {
                    return new Rectangle(origin.X, origin.Y, width, height);
                }
            }
            return SystemInformation.VirtualScreen;
        }

        IntPtr GameWindow()
        {
            int now = Environment.TickCount;
            if (cachedGame != IntPtr.Zero && IsWindow(cachedGame) && unchecked(now - cachedGameTick) < 2000)
            {
                return cachedGame;
            }
            cachedGame = FindGameWindow();
            cachedGameTick = now;
            return cachedGame;
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
