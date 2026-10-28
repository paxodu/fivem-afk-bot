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
        int lastNoteTick;
        bool warnedBlocked;
        bool warnedBlank;
        bool cursorKnown;
        bool cursorSticks;
        IntPtr cachedGame;
        int cachedGameTick;

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
        static extern bool GetCursorPos(out CursorPoint point);

        [DllImport("user32.dll")]
        static extern bool GetClientRect(IntPtr hwnd, out WindowRect rect);

        [DllImport("user32.dll")]
        static extern bool ClientToScreen(IntPtr hwnd, ref CursorPoint point);

        [DllImport("user32.dll")]
        static extern bool IsWindow(IntPtr hwnd);

        [DllImport("user32.dll")]
        static extern void mouse_event(uint flags, uint dx, uint dy, uint data, UIntPtr extra);

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
            StartPosition = FormStartPosition.Manual;
            Location = new Point(12, 12);
            BackColor = Color.FromArgb(20, 12, 8);
            Font = new Font("Segoe UI", 10f);

            var title = new Label();
            title.Text = "Apelsinai";
            title.ForeColor = Color.FromArgb(255, 244, 234);
            title.Font = new Font("Segoe UI", 16f, FontStyle.Bold);
            title.Bounds = new Rectangle(22, 16, 376, 32);

            var hint = new Label();
            hint.Text = "Pelę dedu ant apelsino su baltu ratu ir paspaudžiu. Jei FiveM laiko kamerą, pirmiausia atsuku į tą apelsiną. Raudoną praleidžiu. Žaidimą laikyk lango režimu.";
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

            clock.Interval = 120;
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
            Rectangle area = GameArea();
            Hit hit;
            bool blank;
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
                hit = Finder.Find(bitmap);
                blank = hit == null && LooksBlank(bitmap);
            }
            if (hit == null)
            {
                misses++;
                if (misses >= 3)
                {
                    clicksOnThis = 0;
                    if (running)
                    {
                        status.Text = "Laukiu balto rato";
                    }
                }
                if (blank && !warnedBlank)
                {
                    warnedBlank = true;
                    Write("Nematau žaidimo vaizdo. FiveM laikyk lango režimu, ne išskirtiniu visame ekrane.");
                }
                return;
            }
            misses = 0;
            Act(area.X + hit.X, area.Y + hit.Y, area.X + area.Width / 2, area.Y + area.Height / 2);
        }

        void Act(int fruitX, int fruitY, int centerX, int centerY)
        {
            FocusGame();
            KeepWindowOff(fruitX, fruitY);
            ClipCursor(IntPtr.Zero);
            if (!cursorKnown || cursorSticks)
            {
                SetCursorPos(fruitX, fruitY);
                Thread.Sleep(80);
                CursorPoint here;
                GetCursorPos(out here);
                bool stayed = Math.Abs(here.X - fruitX) < 35 && Math.Abs(here.Y - fruitY) < 35;
                if (!cursorKnown)
                {
                    cursorKnown = true;
                    cursorSticks = stayed;
                }
                if (stayed)
                {
                    if (!ClickAllowed())
                    {
                        return;
                    }
                    SetCursorPos(fruitX, fruitY);
                    NoteClick(LeftClick());
                    return;
                }
            }
            int moveX;
            int moveY;
            bool ready;
            AimMath.Step(fruitX - centerX, fruitY - centerY, out moveX, out moveY, out ready);
            if (ready)
            {
                if (!ClickAllowed())
                {
                    return;
                }
                NoteClick(LeftClick());
                return;
            }
            bool moved = SendRelative(moveX, moveY);
            status.Text = "Taikausi į baltą ratą";
            int now = Environment.TickCount;
            if (unchecked(now - lastNoteTick) > 900)
            {
                lastNoteTick = now;
                Write("Matau baltą ratą, taikausi į apelsiną.");
            }
            if (!moved && !warnedBlocked)
            {
                warnedBlocked = true;
                Write("Windows užblokavo pelę. Paleisk programą kaip administratorius, jei FiveM irgi taip paleistas.");
            }
        }

        bool ClickAllowed()
        {
            int now = Environment.TickCount;
            if (clicksOnThis >= 4)
            {
                return false;
            }
            if (clicksOnThis > 0 && unchecked(now - lastClickTick) < 550)
            {
                return false;
            }
            return true;
        }

        void NoteClick(bool reached)
        {
            clicksOnThis++;
            lastClickTick = Environment.TickCount;
            status.Text = "Spusteliu";
            Write("Spusteliu ant apelsino su baltu ratu.");
            if (!reached && !warnedBlocked)
            {
                warnedBlocked = true;
                Write("Windows užblokavo pelę. Paleisk programą kaip administratorius, jei FiveM irgi taip paleistas.");
            }
        }

        void KeepWindowOff(int x, int y)
        {
            var spot = new Rectangle(x - 50, y - 50, 100, 100);
            if (!Bounds.IntersectsWith(spot))
            {
                return;
            }
            Location = new Point(12, 12);
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

        static bool LooksBlank(Bitmap bitmap)
        {
            long sum = 0;
            int count = 0;
            int width = Math.Max(1, bitmap.Width);
            int height = Math.Max(1, bitmap.Height);
            for (int i = 0; i < 48; i++)
            {
                Color color = bitmap.GetPixel((i * 97) % width, (i * 53) % height);
                sum += color.R + color.G + color.B;
                count++;
            }
            return count > 0 && sum / count < 4;
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

        static bool LeftClick()
        {
            bool down = SendPacket(0, 0, 0x0002);
            Thread.Sleep(70);
            bool up = SendPacket(0, 0, 0x0004);
            return down && up;
        }

        static bool SendRelative(int dx, int dy)
        {
            return SendPacket(dx, dy, 0x0001);
        }

        static bool SendPacket(int dx, int dy, int flags)
        {
            bool x64 = IntPtr.Size == 8;
            int size = x64 ? 40 : 28;
            int at = x64 ? 8 : 4;
            var data = new byte[size];
            WriteI32(data, at, dx);
            WriteI32(data, at + 4, dy);
            WriteI32(data, at + 12, flags);
            IntPtr raw = Marshal.AllocHGlobal(size);
            try
            {
                Marshal.Copy(data, 0, raw, size);
                if (SendInput(1, raw, size) == 1)
                {
                    return true;
                }
                mouse_event(unchecked((uint)flags), unchecked((uint)dx), unchecked((uint)dy), 0, UIntPtr.Zero);
                return false;
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
