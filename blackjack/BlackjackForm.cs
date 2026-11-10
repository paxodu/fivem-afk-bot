using System;
using System.Diagnostics;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace Blackjackas
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
            Application.Run(new BlackjackForm());
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

    sealed class BlackjackForm : Form
    {
        const int WmHotkey = 0x0312;

        readonly Label status = new Label();
        readonly Button toggle = new Button();
        readonly TextBox log = new TextBox();
        readonly System.Windows.Forms.Timer clock = new System.Windows.Forms.Timer();

        volatile bool running = true;
        int misses;
        int lastTry;
        int actedPlayer;
        int actedDealer;
        int actedTick;
        string acted;
        bool warnedBlocked;
        IntPtr cachedGame;
        int cachedGameTick;

        [DllImport("user32.dll")]
        static extern bool RegisterHotKey(IntPtr hwnd, int id, uint mods, uint vk);

        [DllImport("user32.dll")]
        static extern bool UnregisterHotKey(IntPtr hwnd, int id);

        [DllImport("user32.dll")]
        static extern bool SetCursorPos(int x, int y);

        [DllImport("user32.dll")]
        static extern bool GetCursorPos(out CursorPoint point);

        [DllImport("user32.dll")]
        static extern bool ClipCursor(IntPtr rect);

        [DllImport("user32.dll", SetLastError = true)]
        static extern uint SendInput(uint count, IntPtr inputs, int size);

        [DllImport("user32.dll")]
        static extern uint MapVirtualKey(uint code, uint mapType);

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

        public BlackjackForm()
        {
            Text = "Blackjack";
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            ClientSize = new Size(440, 360);
            StartPosition = FormStartPosition.Manual;
            Location = new Point(12, 12);
            BackColor = Color.FromArgb(8, 18, 14);
            Font = new Font("Segoe UI", 10f);

            var title = new Label();
            title.Text = "Blackjack";
            title.ForeColor = Color.FromArgb(236, 248, 240);
            title.Font = new Font("Segoe UI", 16f, FontStyle.Bold);
            title.Bounds = new Rectangle(22, 16, 396, 32);

            var hint = new Label();
            hint.Text = "Skaitau taškus po kortomis ir spaudžiu Imti, Sustoti arba Dvigubinti. Statymui paspaudžiu žetoną 100. ALL-IN neliečiu. Laimėti negarantuoju.";
            hint.ForeColor = Color.FromArgb(176, 210, 190);
            hint.Bounds = new Rectangle(22, 52, 396, 72);

            status.Text = "Laukiu stalo";
            status.ForeColor = Color.FromArgb(72, 196, 120);
            status.Font = new Font("Segoe UI", 11f, FontStyle.Bold);
            status.Bounds = new Rectangle(22, 128, 396, 28);

            toggle.Text = "Stabdyti";
            toggle.Bounds = new Rectangle(22, 164, 140, 38);
            toggle.FlatStyle = FlatStyle.Flat;
            toggle.FlatAppearance.BorderSize = 0;
            toggle.BackColor = Color.FromArgb(72, 196, 120);
            toggle.ForeColor = Color.FromArgb(8, 28, 16);
            toggle.Click += delegate { SetRunning(!running); };

            var hotkey = new Label();
            hotkey.Text = "F7 pristabdo arba vėl įjungia.";
            hotkey.ForeColor = Color.FromArgb(176, 210, 190);
            hotkey.Bounds = new Rectangle(176, 172, 240, 24);

            log.Multiline = true;
            log.ReadOnly = true;
            log.BorderStyle = BorderStyle.None;
            log.BackColor = Color.FromArgb(16, 32, 24);
            log.ForeColor = Color.FromArgb(236, 248, 240);
            log.Bounds = new Rectangle(22, 214, 396, 124);
            log.ScrollBars = ScrollBars.Vertical;

            Controls.Add(title);
            Controls.Add(hint);
            Controls.Add(status);
            Controls.Add(toggle);
            Controls.Add(hotkey);
            Controls.Add(log);
            Write("Laukiu statymo arba kortų.");

            clock.Interval = 220;
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
                toggle.BackColor = Color.FromArgb(72, 196, 120);
                toggle.ForeColor = Color.FromArgb(8, 28, 16);
                status.Text = "Laukiu stalo";
                Write("Vėl stebiu.");
            }
            else
            {
                toggle.Text = "Stebėti";
                toggle.BackColor = Color.FromArgb(28, 46, 36);
                toggle.ForeColor = Color.FromArgb(236, 248, 240);
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
            Hand hand;
            Bet bet;
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
                hand = Play.Find(bitmap);
                bet = hand.SawCircles ? null : Table.Find(bitmap);
            }
            if (hand.SawCircles)
            {
                acted = hand.OurTurn ? acted : null;
                PlayHand(hand);
                return;
            }
            acted = null;
            if (bet == null)
            {
                misses++;
                if (misses >= 3)
                {
                    lastTry = 0;
                    if (running)
                    {
                        status.Text = "Laukiu stalo";
                    }
                }
                return;
            }
            misses = 0;
            int now = Environment.TickCount;
            if (lastTry != 0 && unchecked(now - lastTry) < 700)
            {
                status.Text = "Statau 100";
                return;
            }
            FocusGame();
            KeepOff(area.X + bet.ChipX, area.Y + bet.ChipY);
            KeepOff(area.X + bet.ConfirmX, area.Y + bet.ConfirmY);
            bool chip = ClickAt(area.X + bet.ChipX, area.Y + bet.ChipY);
            Thread.Sleep(250);
            bool confirm = ClickAt(area.X + bet.ConfirmX, area.Y + bet.ConfirmY);
            lastTry = Environment.TickCount;
            status.Text = "Statau 100";
            Write("Matau statymą. Spaudžiu žetoną 100 ir Patvirtinti.");
            if (!(chip && confirm) && !warnedBlocked)
            {
                warnedBlocked = true;
                Write("Windows užblokavo pelę. Paleisk programą kaip administratorius, jei FiveM irgi taip paleistas.");
            }
        }

        void PlayHand(Hand hand)
        {
            misses = 0;
            if (!hand.OurTurn)
            {
                status.Text = hand.Player >= 2 ? "Dalintojas žaidžia" : "Matau kortas, taškai neaiškūs";
                return;
            }
            string action = Play.Choose(hand.Player, hand.Dealer, hand.CanDouble);
            if (action == null)
            {
                status.Text = "Matau kortas, taškai neaiškūs";
                return;
            }
            string name = Play.Lithuanian(action);
            int now = Environment.TickCount;
            bool same = acted == action && actedPlayer == hand.Player && actedDealer == hand.Dealer;
            if (same && unchecked(now - actedTick) < 900)
            {
                status.Text = name;
                return;
            }
            FocusGame();
            bool reached = PressAction(action);
            acted = action;
            actedPlayer = hand.Player;
            actedDealer = hand.Dealer;
            actedTick = Environment.TickCount;
            status.Text = name;
            string dealer = hand.Dealer >= 1 ? hand.Dealer.ToString() : "?";
            Write("Tavo " + hand.Player + " prieš dalintojo " + dealer + ". " + name + ".");
            if (!reached && !warnedBlocked)
            {
                warnedBlocked = true;
                Write("Windows užblokavo klavišą. Paleisk programą kaip administratorius, jei FiveM irgi taip paleistas.");
            }
        }

        static bool PressAction(string action)
        {
            uint vk = 0x0D;
            if (action == "stand")
            {
                vk = 0x20;
            }
            else if (action == "double")
            {
                vk = 0xA0;
            }
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

        void KeepOff(int x, int y)
        {
            var spot = new Rectangle(x - 40, y - 40, 80, 80);
            if (!Bounds.IntersectsWith(spot))
            {
                return;
            }
            Location = new Point(12, 12);
        }

        static bool ClickAt(int x, int y)
        {
            ClipCursor(IntPtr.Zero);
            SetCursorPos(x, y);
            Thread.Sleep(50);
            SendPacket(3, 0, 0x0001);
            Thread.Sleep(20);
            SendPacket(-3, 0, 0x0001);
            Thread.Sleep(30);
            SetCursorPos(x, y);
            Thread.Sleep(40);
            bool down = SendPacket(0, 0, 0x0002);
            Thread.Sleep(90);
            bool up = SendPacket(0, 0, 0x0004);
            return down && up;
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
            int bestArea = 0;
            EnumWindows(delegate(IntPtr hwnd, IntPtr lParam)
            {
                if (!IsWindowVisible(hwnd) || !IsGameWindow(hwnd))
                {
                    return true;
                }
                WindowRect rect;
                if (!GetClientRect(hwnd, out rect))
                {
                    return true;
                }
                int area = (rect.Right - rect.Left) * (rect.Bottom - rect.Top);
                if (area > bestArea)
                {
                    bestArea = area;
                    found = hwnd;
                }
                return true;
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
