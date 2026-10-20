using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Windows.Forms;

internal sealed class Launcher : Form
{
    private readonly string dir;

    [STAThread]
    private static void Main()
    {
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);
        Application.Run(new Launcher());
    }

    public Launcher()
    {
        dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "AutoFish");
        Directory.CreateDirectory(dir);
        Text = "Auto fish";
        FormBorderStyle = FormBorderStyle.FixedSingle;
        MaximizeBox = false;
        StartPosition = FormStartPosition.CenterScreen;
        ClientSize = new Size(460, 420);
        BackColor = Color.FromArgb(18, 20, 28);
        ForeColor = Color.White;
        Font = new Font("Segoe UI", 10f);

        var title = Label("AUTO FISH", 16f, FontStyle.Bold, Color.FromArgb(112, 161, 255), 24, 18, 410, 32);
        var sub = Label("Viena programa. Pasirink, ką atidaryti.", 10f, FontStyle.Regular, Color.FromArgb(180, 188, 204), 24, 52, 410, 24);
        Controls.Add(title);
        Controls.Add(sub);

        AddButton("Žvejyba", "E spaudiklis, žuvys ir AFK testas.", "fish.exe", "E_spaudiklis.exe", 96);
        AddButton("Kas žvejoja", "Kas dabar prisijungęs ir blokavimas.", "online.exe", "Auto_fish_online.exe", 186);
        AddButton("Raktų generatorius", "Niekam nesiųsk šitos dalies.", "keys.exe", "Auto_fish_raktai.exe", 276);
    }

    private void AddButton(string caption, string hint, string resource, string fileName, int top)
    {
        var button = new Button();
        button.Text = caption;
        button.Font = new Font("Segoe UI", 12f, FontStyle.Bold);
        button.ForeColor = Color.FromArgb(6, 34, 20);
        button.BackColor = Color.FromArgb(0, 211, 108);
        button.FlatStyle = FlatStyle.Flat;
        button.FlatAppearance.BorderSize = 0;
        button.SetBounds(24, top, 412, 42);
        button.Cursor = Cursors.Hand;
        button.Click += delegate { Open(resource, fileName); };
        Controls.Add(button);

        var note = Label(hint, 9f, FontStyle.Regular, Color.FromArgb(160, 170, 190), 28, top + 44, 400, 20);
        Controls.Add(note);
    }

    private static Label Label(string text, float size, FontStyle style, Color color, int x, int y, int w, int h)
    {
        var label = new Label();
        label.Text = text;
        label.Font = new Font("Segoe UI", size, style);
        label.ForeColor = color;
        label.BackColor = Color.FromArgb(18, 20, 28);
        label.SetBounds(x, y, w, h);
        return label;
    }

    private void Open(string resource, string fileName)
    {
        try
        {
            string path = Path.Combine(dir, fileName);
            if (!File.Exists(path))
            {
                using (Stream input = Assembly.GetExecutingAssembly().GetManifestResourceStream(resource))
                {
                    if (input == null)
                    {
                        MessageBox.Show("Trūksta " + fileName, "Auto fish", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }
                    using (FileStream output = File.Create(path))
                    {
                        input.CopyTo(output);
                    }
                }
            }
            Process.Start(new ProcessStartInfo(path) { WorkingDirectory = dir, UseShellExecute = true });
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Auto fish", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
}
