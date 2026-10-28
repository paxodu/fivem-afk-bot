using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;

namespace Apelsinai
{
    static class Program
    {
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new ApelsinasForm());
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
        bool holding;
        int misses;
        int busy;

        [DllImport("user32.dll")]
        static extern bool RegisterHotKey(IntPtr hwnd, int id, uint mods, uint vk);

        [DllImport("user32.dll")]
        static extern bool UnregisterHotKey(IntPtr hwnd, int id);

        [DllImport("user32.dll", SetLastError = true)]
        static extern uint SendInput(uint count, IntPtr inputs, int size);

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
            hint.Text = "Kai ant apelsino matai baltą ratą, paspaudžiu E. Raudoną ratą praleidžiu. FiveM laikyk lango režimu.";
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
            if (!running || Interlocked.Exchange(ref busy, 1) == 1)
            {
                return;
            }
            ThreadPool.QueueUserWorkItem(delegate
            {
                try
                {
                    Scan();
                }
                catch (Exception ex)
                {
                    BeginInvoke((Action)delegate { Write("Klaida: " + ex.Message); });
                }
                finally
                {
                    Interlocked.Exchange(ref busy, 0);
                }
            });
        }

        void Scan()
        {
            Rectangle area = SystemInformation.VirtualScreen;
            using (var bitmap = new Bitmap(area.Width, area.Height))
            {
                using (var graphics = Graphics.FromImage(bitmap))
                {
                    graphics.CopyFromScreen(area.X, area.Y, 0, 0, area.Size);
                }
                Hit hit = Finder.Find(bitmap);
                if (hit == null)
                {
                    misses++;
                    if (misses >= 3)
                    {
                        holding = false;
                    }
                    if (!holding)
                    {
                        BeginInvoke((Action)delegate
                        {
                            if (running)
                            {
                                status.Text = "Laukiu balto rato";
                            }
                        });
                    }
                    return;
                }
                misses = 0;
                if (holding)
                {
                    return;
                }
                PressE();
                holding = true;
                BeginInvoke((Action)delegate
                {
                    status.Text = "Spaudžiu E";
                    Write("Matau baltą ratą, spaudžiu E.");
                });
            }
        }

        static void PressE()
        {
            Tap(false);
            Thread.Sleep(50);
            Tap(true);
        }

        static void Tap(bool up)
        {
            bool x64 = IntPtr.Size == 8;
            int size = x64 ? 40 : 28;
            int at = x64 ? 8 : 4;
            var data = new byte[size];
            data[0] = 1;
            data[at + 2] = 0x12;
            data[at + 4] = (byte)(8 | (up ? 2 : 0));
            IntPtr raw = Marshal.AllocHGlobal(size);
            try
            {
                Marshal.Copy(data, 0, raw, size);
                SendInput(1, raw, size);
            }
            finally
            {
                Marshal.FreeHGlobal(raw);
            }
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
