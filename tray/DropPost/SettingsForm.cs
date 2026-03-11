using System.Drawing;
using System.Windows.Forms;

namespace DropPost;

class SettingsForm : Form
{
    public AppSettings Result { get; private set; }

    private readonly TextBox _url;
    private readonly TextBox _key;
    private readonly CheckBox _autoStart;

    public SettingsForm(AppSettings current)
    {
        Result = current;

        Text = "DropPost — Settings";
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = MinimizeBox = false;
        StartPosition = FormStartPosition.CenterScreen;
        AutoScaleMode = AutoScaleMode.Dpi;
        ClientSize = new Size(420, 180);
        Padding = new Padding(12);

        int lw = 90, x = 108, w = 292, row = 16, gap = 32;

        Controls.Add(new Label { Text = "Server URL", Left = 12, Top = row, Width = lw, TextAlign = System.Drawing.ContentAlignment.MiddleRight });
        _url = new TextBox { Left = x, Top = row - 2, Width = w, Text = current.ServerUrl };
        Controls.Add(_url);

        row += gap;
        Controls.Add(new Label { Text = "API Key", Left = 12, Top = row, Width = lw, TextAlign = System.Drawing.ContentAlignment.MiddleRight });
        _key = new TextBox { Left = x, Top = row - 2, Width = w, UseSystemPasswordChar = true, Text = current.ApiKey };
        Controls.Add(_key);

        row += gap - 8;
        var show = new CheckBox { Text = "Show key", Left = x, Top = row, AutoSize = true };
        show.CheckedChanged += (_, _) => _key.UseSystemPasswordChar = !show.Checked;
        Controls.Add(show);

        row += gap;
        _autoStart = new CheckBox { Text = "Start with Windows", Left = x, Top = row, AutoSize = true, Checked = current.StartWithWindows };
        Controls.Add(_autoStart);

        row += gap + 4;
        var save   = new Button { Text = "Save",   DialogResult = DialogResult.OK,     Left = x + w - 160, Top = row, Width = 75 };
        var cancel = new Button { Text = "Cancel", DialogResult = DialogResult.Cancel, Left = x + w - 78,  Top = row, Width = 75 };
        save.Click += (_, _) => Commit();
        Controls.Add(save);
        Controls.Add(cancel);
        AcceptButton = save;
        CancelButton = cancel;
    }

    private void Commit() =>
        Result = new AppSettings
        {
            ServerUrl      = _url.Text.TrimEnd('/'),
            ApiKey         = _key.Text.Trim(),
            DefaultExpiry  = Result.DefaultExpiry,
            StartWithWindows = _autoStart.Checked,
        };
}
