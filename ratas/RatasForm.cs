using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;

namespace Ratas
{
    static class Program
    {
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new RatasForm());
        }
    }

    sealed class RatasForm : Form
    {
        const int WhKeyboard = 13;
        const int WhMouse = 14;
        const int WmHotkey = 0x0312;
        const int WmKeyDown = 0x0100;
        const int WmKeyUp = 0x0101;
        const int WmSysKeyDown = 0x0104;
        const int WmSysKeyUp = 0x0105;
        const int WmMouseMove = 0x0200;
        const int WmLDown = 0x0201;
        const int WmLUp = 0x0202;
        const int WmRDown = 0x0204;
        const int WmRUp = 0x0205;

        readonly Label title = new Label();
        readonly Label status = new Label();
        readonly Label hint = new Label();
        readonly Button recordButton = new Button();
        readonly Button stopRecordButton = new Button();
        readonly Button playButton = new Button();
        readonly System.Windows.Forms.Timer clock = new System.Windows.Forms.Timer();

        readonly List<Step> steps = new List<Step>();
        readonly HashSet<ushort> held = new HashSet<ushort>();
        readonly object gate = new object();

        HookProc keyProc;
        HookProc mouseProc;
        IntPtr keyHook;
        IntPtr mouseHook;
        bool capturing;
        bool playing;
        bool hooksReady;
        int recordOrigin;
        int lastMoveAt;
        int lastX;
        int lastY;
        bool havePointer;
        int playLap;
        string routePath;

        delegate IntPtr HookProc(int code, IntPtr wParam, IntPtr lParam);

        [StructLayout(LayoutKind.Sequential)]
        struct KeyInfo
        {
            public uint Vk;
            public uint Scan;
            public uint Flags;
            public uint Time;
            public IntPtr Extra;
        }

        [StructLayout(LayoutKind.Sequential)]
        struct MouseInfo
        {
            public int X;
            public int Y;
            public uint Data;
            public uint Flags;
            public uint Time;
            public IntPtr Extra;
        }

        [DllImport("user32.dll", SetLastError = true, EntryPoint = "SetWindowsHookExW")]
        static extern IntPtr SetWindowsHookEx(int id, HookProc proc, IntPtr module, uint threadId);

        [DllImport("user32.dll")]
        static extern bool UnhookWindowsHookEx(IntPtr hook);

        [DllImport("user32.dll")]
        static extern IntPtr CallNextHookEx(IntPtr hook, int code, IntPtr wParam, IntPtr lParam);

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, EntryPoint = "GetModuleHandleW")]
        static extern IntPtr GetModuleHandle(string name);

        [DllImport("user32.dll")]
        static extern bool RegisterHotKey(IntPtr hwnd, int id, uint mods, uint vk);

        [DllImport("user32.dll")]
        static extern bool UnregisterHotKey(IntPtr hwnd, int id);

        [DllImport("user32.dll")]
        static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        static extern uint MapVirtualKey(uint code, uint map);

        [DllImport("user32.dll", SetLastError = true)]
        static extern uint SendInput(uint count, IntPtr inputs, int size);

        public RatasForm()
        {
            Text = "Ratas";
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            ClientSize = new Size(460, 320);
            StartPosition = FormStartPosition.CenterScreen;
            BackColor = Color.FromArgb(18, 22, 20);
            Font = new Font("Segoe UI", 10f);

            title.Text = "Checkpointų ratas";
            title.ForeColor = Color.FromArgb(231, 242, 234);
            title.Font = new Font("Segoe UI", 16f, FontStyle.Bold);
            title.AutoSize = false;
            title.Bounds = new Rectangle(22, 16, 416, 32);

            status.Text = "Rato dar nėra. Įrašyk vieną pilną ratą.";
            status.ForeColor = Color.FromArgb(0, 255, 133);
            status.Font = new Font("Segoe UI", 11f, FontStyle.Bold);
            status.AutoSize = false;
            status.Bounds = new Rectangle(22, 56, 416, 48);

            hint.Text = "F9 įrašo. Nueik į pirmą checkpointą, tada į antrą ir grįžk tuo pačiu keliu iki vietos, kur pradėjai. F10 baigia įrašą. F11 kartoja tą ratą be galo.";
            hint.ForeColor = Color.FromArgb(155, 176, 163);
            hint.AutoSize = false;
            hint.Bounds = new Rectangle(22, 108, 416, 96);

            StyleButton(recordButton, "Įrašyti F9", 22, Color.FromArgb(0, 255, 133), Color.FromArgb(12, 20, 16));
            StyleButton(stopRecordButton, "Baigti F10", 166, Color.FromArgb(36, 48, 42), Color.FromArgb(231, 242, 234));
            StyleButton(playButton, "Sukti F11", 310, Color.FromArgb(36, 48, 42), Color.FromArgb(231, 242, 234));
            recordButton.Click += delegate { BeginRecord(); };
            stopRecordButton.Click += delegate { EndRecord(); };
            playButton.Click += delegate { TogglePlay(); };

            Controls.Add(title);
            Controls.Add(status);
            Controls.Add(hint);
            Controls.Add(recordButton);
            Controls.Add(stopRecordButton);
            Controls.Add(playButton);

            clock.Interval = 200;
            clock.Tick += delegate { RefreshStatus(); };
            clock.Start();
        }

        static void StyleButton(Button button, string text, int x, Color back, Color fore)
        {
            button.Text = text;
            button.Bounds = new Rectangle(x, 228, 128, 40);
            button.FlatStyle = FlatStyle.Flat;
            button.FlatAppearance.BorderSize = 0;
            button.BackColor = back;
            button.ForeColor = fore;
            button.Cursor = Cursors.Hand;
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            routePath = RouteFile();
            try
            {
                keyProc = KeyboardHook;
                mouseProc = MouseHook;
                IntPtr module = GetModuleHandle(null);
                keyHook = SetWindowsHookEx(WhKeyboard, keyProc, module, 0);
                mouseHook = SetWindowsHookEx(WhMouse, mouseProc, module, 0);
                hooksReady = keyHook != IntPtr.Zero && mouseHook != IntPtr.Zero;
                RegisterHotKey(Handle, 1, 0, 0x78);
                RegisterHotKey(Handle, 2, 0, 0x79);
                RegisterHotKey(Handle, 3, 0, 0x7A);
            }
            catch (Exception ex)
            {
                status.Text = "Įrašymas nepavyko paleisti: " + ex.Message;
            }
            try
            {
                if (File.Exists(routePath))
                {
                    lock (gate)
                    {
                        steps.Clear();
                        steps.AddRange(Tape.Load(File.ReadAllText(routePath)));
                    }
                }
            }
            catch (Exception ex)
            {
                status.Text = "Rato failo nepavyko atidaryti: " + ex.Message;
            }
            RefreshStatus();
        }

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == WmHotkey)
            {
                int id = m.WParam.ToInt32();
                if (id == 1)
                {
                    BeginRecord();
                }
                else if (id == 2)
                {
                    EndRecord();
                }
                else if (id == 3)
                {
                    TogglePlay();
                }
                return;
            }
            base.WndProc(ref m);
        }

        void BeginRecord()
        {
            if (playing)
            {
                return;
            }
            lock (gate)
            {
                steps.Clear();
                held.Clear();
            }
            havePointer = false;
            recordOrigin = Environment.TickCount;
            lastMoveAt = recordOrigin;
            capturing = true;
            RefreshStatus();
        }

        void EndRecord()
        {
            if (!capturing)
            {
                return;
            }
            capturing = false;
            ReleaseStuck();
            try
            {
                string body;
                lock (gate)
                {
                    body = Tape.Save(steps);
                }
                File.WriteAllText(routePath, body);
            }
            catch (Exception ex)
            {
                status.Text = "Nepavyko išsaugoti rato: " + ex.Message;
                return;
            }
            RefreshStatus();
        }

        void TogglePlay()
        {
            if (playing)
            {
                StopPlay();
                return;
            }
            if (capturing)
            {
                EndRecord();
            }
            int count;
            lock (gate)
            {
                count = steps.Count;
            }
            if (count == 0)
            {
                status.Text = "Pirma įrašyk ratą: F9, nueik kelią, F10.";
                return;
            }
            playing = true;
            playLap = 0;
            playButton.Text = "Stoti F11";
            playButton.BackColor = Color.FromArgb(196, 92, 74);
            status.Text = "Per 3 sekundes paspausk ant FiveM lango.";
            var starter = new Thread(new ThreadStart(delegate
            {
                for (int i = 0; i < 30 && playing; i++)
                {
                    Thread.Sleep(100);
                }
                if (!playing)
                {
                    return;
                }
                PlayLoop();
            }));
            starter.IsBackground = true;
            starter.Start();
        }

        void StopPlay()
        {
            playing = false;
            ReleaseStuck();
            playButton.Text = "Sukti F11";
            playButton.BackColor = Color.FromArgb(36, 48, 42);
            RefreshStatus();
        }

        void PlayLoop()
        {
            while (playing)
            {
                playLap++;
                Step[] copy;
                lock (gate)
                {
                    copy = steps.ToArray();
                }
                int started = Environment.TickCount;
                for (int i = 0; i < copy.Length && playing; i++)
                {
                    int wait = Tape.WaitMs(started, Environment.TickCount, copy[i].Ms);
                    while (wait > 0 && playing)
                    {
                        Thread.Sleep(wait > 40 ? 40 : wait);
                        wait = Tape.WaitMs(started, Environment.TickCount, copy[i].Ms);
                    }
                    if (!playing)
                    {
                        break;
                    }
                    Send(copy[i]);
                }
                ReleaseStuck();
                if (playing)
                {
                    Thread.Sleep(300);
                }
            }
        }

        void Send(Step step)
        {
            bool x64 = IntPtr.Size == 8;
            byte[] bytes;
            if (step.Kind == StepKind.Key)
            {
                ushort scan = (ushort)MapVirtualKey(step.Vk, 0);
                bytes = Tape.KeyStroke(step.Vk, scan, step.Up, x64);
                lock (gate)
                {
                    if (step.Up)
                    {
                        held.Remove(step.Vk);
                    }
                    else
                    {
                        held.Add(step.Vk);
                    }
                }
            }
            else if (step.Kind == StepKind.Move)
            {
                bytes = Tape.MouseMove(step.Dx, step.Dy, x64);
            }
            else
            {
                bytes = Tape.MouseButton(step.Kind == StepKind.Right, step.Up, x64);
            }
            int size = bytes.Length;
            IntPtr raw = Marshal.AllocHGlobal(size);
            try
            {
                Marshal.Copy(bytes, 0, raw, size);
                SendInput(1, raw, size);
            }
            finally
            {
                Marshal.FreeHGlobal(raw);
            }
        }

        void ReleaseStuck()
        {
            ushort[] down;
            lock (gate)
            {
                down = new ushort[held.Count];
                held.CopyTo(down);
            }
            for (int i = 0; i < down.Length; i++)
            {
                Send(new Step { Kind = StepKind.Key, Vk = down[i], Up = true });
            }
        }

        IntPtr KeyboardHook(int code, IntPtr wParam, IntPtr lParam)
        {
            if (code >= 0 && capturing && GetForegroundWindow() != Handle)
            {
                int msg = wParam.ToInt32();
                if (msg == WmKeyDown || msg == WmKeyUp || msg == WmSysKeyDown || msg == WmSysKeyUp)
                {
                    var info = (KeyInfo)Marshal.PtrToStructure(lParam, typeof(KeyInfo));
                    bool up = msg == WmKeyUp || msg == WmSysKeyUp || (info.Flags & 0x80) != 0;
                    NoteKey((ushort)info.Vk, up);
                }
            }
            return CallNextHookEx(keyHook, code, wParam, lParam);
        }

        IntPtr MouseHook(int code, IntPtr wParam, IntPtr lParam)
        {
            if (code >= 0 && capturing && GetForegroundWindow() != Handle)
            {
                int msg = wParam.ToInt32();
                var info = (MouseInfo)Marshal.PtrToStructure(lParam, typeof(MouseInfo));
                if (msg == WmMouseMove)
                {
                    NoteMove(info.X, info.Y);
                }
                else if (msg == WmLDown || msg == WmLUp)
                {
                    NoteButton(StepKind.Left, msg == WmLUp);
                }
                else if (msg == WmRDown || msg == WmRUp)
                {
                    NoteButton(StepKind.Right, msg == WmRUp);
                }
            }
            return CallNextHookEx(mouseHook, code, wParam, lParam);
        }

        void NoteKey(ushort vk, bool up)
        {
            if (Tape.IsHotkey(vk))
            {
                return;
            }
            lock (gate)
            {
                bool already = held.Contains(vk);
                if (Tape.IsAutoRepeat(already, up))
                {
                    return;
                }
                if (up)
                {
                    held.Remove(vk);
                }
                else
                {
                    held.Add(vk);
                }
                steps.Add(new Step
                {
                    Ms = Environment.TickCount - recordOrigin,
                    Kind = StepKind.Key,
                    Up = up,
                    Vk = vk
                });
            }
        }

        void NoteMove(int x, int y)
        {
            if (!havePointer)
            {
                havePointer = true;
                lastX = x;
                lastY = y;
                return;
            }
            int dx = x - lastX;
            int dy = y - lastY;
            lastX = x;
            lastY = y;
            if (dx == 0 && dy == 0)
            {
                return;
            }
            int now = Environment.TickCount;
            lock (gate)
            {
                if (now - lastMoveAt < 15 && steps.Count > 0 && steps[steps.Count - 1].Kind == StepKind.Move)
                {
                    steps[steps.Count - 1].Dx += dx;
                    steps[steps.Count - 1].Dy += dy;
                    return;
                }
                lastMoveAt = now;
                steps.Add(new Step
                {
                    Ms = now - recordOrigin,
                    Kind = StepKind.Move,
                    Dx = dx,
                    Dy = dy
                });
            }
        }

        void NoteButton(StepKind kind, bool up)
        {
            lock (gate)
            {
                steps.Add(new Step
                {
                    Ms = Environment.TickCount - recordOrigin,
                    Kind = kind,
                    Up = up
                });
            }
        }

        void RefreshStatus()
        {
            if (IsDisposed)
            {
                return;
            }
            int count;
            int last = 0;
            lock (gate)
            {
                count = steps.Count;
                if (count > 0)
                {
                    last = steps[count - 1].Ms;
                }
            }
            if (!hooksReady)
            {
                status.Text = "Nepavyko klausyti klavišų. Paleisk Ratas dešiniu pelės mygtuku: Run as administrator.";
                return;
            }
            if (capturing)
            {
                int sec = (Environment.TickCount - recordOrigin) / 1000;
                status.Text = "Įrašau ratą… " + sec + " s. Grįžk iki starto ir spausk F10.";
                return;
            }
            if (playing)
            {
                status.Text = "Suku " + playLap + " ratą. F11 sustabdo.";
                return;
            }
            if (count == 0)
            {
                status.Text = "Rato dar nėra. Įrašyk vieną pilną ratą.";
                return;
            }
            status.Text = "Įrašytas ratas: " + (last / 1000) + " s. F11 suka jį iš naujo.";
        }

        string RouteFile()
        {
            string beside = Path.Combine(Path.GetDirectoryName(Application.ExecutablePath), "ratas.txt");
            try
            {
                string probe = beside + ".tmp";
                File.WriteAllText(probe, "ok");
                File.Delete(probe);
                return beside;
            }
            catch
            {
                string folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Ratas");
                Directory.CreateDirectory(folder);
                return Path.Combine(folder, "ratas.txt");
            }
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            playing = false;
            capturing = false;
            ReleaseStuck();
            clock.Stop();
            UnregisterHotKey(Handle, 1);
            UnregisterHotKey(Handle, 2);
            UnregisterHotKey(Handle, 3);
            if (keyHook != IntPtr.Zero)
            {
                UnhookWindowsHookEx(keyHook);
            }
            if (mouseHook != IntPtr.Zero)
            {
                UnhookWindowsHookEx(mouseHook);
            }
            base.OnFormClosing(e);
        }
    }
}
