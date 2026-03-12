using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DropPost;

class TrayApp : ApplicationContext
{
    private readonly NotifyIcon _icon;
    private AppSettings _settings;
    private Uploader _uploader;

    public TrayApp()
    {
        _settings = AppSettings.Load();
        _uploader = new Uploader(_settings);

        _icon = new NotifyIcon
        {
            Icon  = MakeIcon(),
            Text  = "DropPost",
            Visible = true,
        };
        _icon.DoubleClick += (_, _) => _ = UploadFileAsync();

        RebuildMenu();

        // First-run nudge
        if (string.IsNullOrWhiteSpace(_settings.ApiKey))
        {
            _icon.ShowBalloonTip(4000, "DropPost",
                "Right-click → Settings to configure your server URL and API key.",
                ToolTipIcon.Info);
        }
    }

    // ── Menu ─────────────────────────────────────────────────────────────────

    private void RebuildMenu()
    {
        _icon.ContextMenuStrip?.Dispose();
        var menu = new ContextMenuStrip();

        var uploadFile = new ToolStripMenuItem("Upload File...");
        uploadFile.Font = new Font(uploadFile.Font, FontStyle.Bold); // default action
        uploadFile.Click += (_, _) => _ = UploadFileAsync();

        var uploadClip = new ToolStripMenuItem("Upload Clipboard Text");
        uploadClip.Click += (_, _) => _ = UploadClipboardAsync();

        // Expiry submenu
        var expiryMenu = new ToolStripMenuItem("Expiry");
        foreach (var opt in new[] { "1h", "6h", "24h", "7d", "30d", "never" })
        {
            var o = opt;
            var item = new ToolStripMenuItem(o) { Checked = _settings.DefaultExpiry == o };
            item.Click += (_, _) =>
            {
                _settings.DefaultExpiry = o;
                _settings.Save();
                RebuildMenu();
            };
            expiryMenu.DropDownItems.Add(item);
        }

        menu.Items.Add(uploadFile);
        menu.Items.Add(uploadClip);
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add(new ToolStripMenuItem("Browse Files...", null, (_, _) => OpenBrowser()));
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add(expiryMenu);
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add(new ToolStripMenuItem("Settings...", null, (_, _) => OpenSettings()));
        menu.Items.Add(new ToolStripMenuItem("Exit", null, (_, _) => Exit()));

        _icon.ContextMenuStrip = menu;
    }

    // ── Upload actions ────────────────────────────────────────────────────────

    private async Task UploadFileAsync()
    {
        if (!CheckSettings()) return;

        using var dlg = new OpenFileDialog { Title = "Upload file", Multiselect = false };
        if (dlg.ShowDialog() != DialogResult.OK) return;

        SetStatus("uploading…");
        try
        {
            var url = await _uploader.UploadFileAsync(dlg.FileName, _settings.DefaultExpiry);
            Clipboard.SetText(url);
            _icon.ShowBalloonTip(5000, "Uploaded ✓", url, ToolTipIcon.Info);
        }
        catch (Exception ex)
        {
            _icon.ShowBalloonTip(5000, "Upload failed", ex.Message, ToolTipIcon.Error);
        }
        finally { SetStatus("DropPost"); }
    }

    private async Task UploadClipboardAsync()
    {
        if (!CheckSettings()) return;
        if (!Clipboard.ContainsText())
        {
            _icon.ShowBalloonTip(3000, "DropPost", "Clipboard has no text.", ToolTipIcon.Warning);
            return;
        }

        // Capture clipboard text on UI thread before going async
        var text = Clipboard.GetText();

        SetStatus("uploading…");
        try
        {
            var url = await _uploader.UploadTextAsync(text, _settings.DefaultExpiry);
            Clipboard.SetText(url);
            _icon.ShowBalloonTip(5000, "Uploaded ✓", url, ToolTipIcon.Info);
        }
        catch (Exception ex)
        {
            _icon.ShowBalloonTip(5000, "Upload failed", ex.Message, ToolTipIcon.Error);
        }
        finally { SetStatus("DropPost"); }
    }

    // ── Settings ──────────────────────────────────────────────────────────────

    private void OpenBrowser()
    {
        if (!CheckSettings()) return;
        new FileBrowserForm(_settings).Show();
    }

    private void OpenSettings()
    {
        using var form = new SettingsForm(_settings);
        if (form.ShowDialog() != DialogResult.OK) return;
        _settings = form.Result;
        _settings.Save();
        _uploader = new Uploader(_settings);
        RebuildMenu();
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private bool CheckSettings()
    {
        if (!string.IsNullOrWhiteSpace(_settings.ApiKey) &&
            !_settings.ServerUrl.Contains("YOUR_DOMAIN"))
            return true;

        _icon.ShowBalloonTip(4000, "DropPost",
            "Configure server URL and API key in Settings first.", ToolTipIcon.Warning);
        OpenSettings();
        return false;
    }

    private void SetStatus(string text) =>
        _icon.Text = text.Length > 63 ? text[..63] : text; // NotifyIcon max tooltip = 63 chars

    private void Exit()
    {
        _icon.Visible = false;
        Application.Exit();
    }

    // ── Icon (drawn programmatically — no .ico file needed) ──────────────────

    [DllImport("user32.dll")] private static extern bool DestroyIcon(IntPtr hIcon);

    private static Icon MakeIcon()
    {
        using var bmp = new Bitmap(16, 16);
        using (var g = Graphics.FromImage(bmp))
        {
            g.Clear(Color.Transparent);
            // Blue circle
            using var fill = new SolidBrush(Color.FromArgb(0, 120, 215));
            g.FillEllipse(fill, 0, 0, 15, 15);
            // White up-arrow
            Point[] arrow =
            [
                new(8, 2), new(4, 7), new(6, 7),
                new(6, 13), new(10, 13), new(10, 7), new(12, 7),
            ];
            g.FillPolygon(Brushes.White, arrow);
        }
        var hIcon = bmp.GetHicon();
        var icon  = (Icon)Icon.FromHandle(hIcon).Clone();
        DestroyIcon(hIcon);
        return icon;
    }
}
