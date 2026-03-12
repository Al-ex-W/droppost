using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DropPost;

class RemoteFile
{
    [JsonPropertyName("file_name")]    public string FileName       { get; set; } = "";
    [JsonPropertyName("file_size")]    public long   FileSize        { get; set; }
    [JsonPropertyName("creation_date_utc")] public string CreatedAt { get; set; } = "";
    [JsonPropertyName("expires_at_utc")]    public string? ExpiresAt { get; set; }

    public string SizeDisplay => FileSize switch
    {
        < 1024              => $"{FileSize} B",
        < 1024 * 1024       => $"{FileSize / 1024.0:F1} KB",
        _                   => $"{FileSize / 1024.0 / 1024.0:F1} MB",
    };

    public string Url(string serverUrl) => $"{serverUrl}/{FileName}";
}

class FileBrowserForm : Form
{
    private readonly AppSettings _settings;
    private static readonly HttpClient Http = new();

    private readonly ListView _list;
    private readonly Button _refreshBtn;
    private readonly Button _copyBtn;
    private readonly Button _openBtn;
    private readonly Button _deleteBtn;
    private readonly Label _statusLabel;

    public FileBrowserForm(AppSettings settings)
    {
        _settings = settings;

        Text = "DropPost — Files";
        Size = new Size(780, 480);
        MinimumSize = new Size(600, 360);
        StartPosition = FormStartPosition.CenterScreen;
        AutoScaleMode = AutoScaleMode.Dpi;

        // ── ListView ─────────────────────────────────────────────────────────
        _list = new ListView
        {
            Dock = DockStyle.Fill,
            View = View.Details,
            FullRowSelect = true,
            GridLines = true,
            MultiSelect = false,
            Font = new Font("Segoe UI", 9),
        };
        _list.Columns.Add("File name",  340);
        _list.Columns.Add("Size",        70);
        _list.Columns.Add("Uploaded",   140);
        _list.Columns.Add("Expires",    140);
        _list.DoubleClick += (_, _) => CopyUrl();

        // ── Bottom bar ────────────────────────────────────────────────────────
        var bar = new Panel { Dock = DockStyle.Bottom, Height = 40, Padding = new Padding(6, 6, 6, 6) };

        _refreshBtn = new Button { Text = "Refresh",      Width = 80,  Left = 6,   Top = 6 };
        _copyBtn    = new Button { Text = "Copy URL",     Width = 80,  Left = 92,  Top = 6 };
        _openBtn    = new Button { Text = "Open",         Width = 70,  Left = 178, Top = 6 };
        _deleteBtn  = new Button { Text = "Delete",       Width = 70,  Left = 254, Top = 6, ForeColor = Color.Firebrick };
        _statusLabel = new Label { Left = 340, Top = 10, Width = 400, Text = "Loading…" };

        _refreshBtn.Click += (_, _) => _ = LoadAsync();
        _copyBtn.Click    += (_, _) => CopyUrl();
        _openBtn.Click    += (_, _) => OpenInBrowser();
        _deleteBtn.Click  += (_, _) => _ = DeleteSelectedAsync();

        bar.Controls.AddRange(new Control[] { _refreshBtn, _copyBtn, _openBtn, _deleteBtn, _statusLabel });

        Controls.Add(_list);
        Controls.Add(bar);

        _ = LoadAsync();
    }

    // ── Load file list ────────────────────────────────────────────────────────

    private async Task LoadAsync()
    {
        SetStatus("Loading…");
        _refreshBtn.Enabled = false;
        try
        {
            var req = new HttpRequestMessage(HttpMethod.Get, $"{_settings.ServerUrl}/list");
            req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _settings.ApiKey);
            var resp = await Http.SendAsync(req);
            resp.EnsureSuccessStatusCode();
            var json = await resp.Content.ReadAsStringAsync();
            var files = JsonSerializer.Deserialize<List<RemoteFile>>(json) ?? [];
            PopulateList(files);
            SetStatus($"{files.Count} file{(files.Count == 1 ? "" : "s")}");
        }
        catch (Exception ex)
        {
            SetStatus($"Error: {ex.Message}");
        }
        finally
        {
            _refreshBtn.Enabled = true;
        }
    }

    private void PopulateList(List<RemoteFile> files)
    {
        _list.Items.Clear();
        foreach (var f in files)
        {
            var item = new ListViewItem(f.FileName);
            item.SubItems.Add(f.SizeDisplay);
            item.SubItems.Add(f.CreatedAt);
            item.SubItems.Add(f.ExpiresAt ?? "never");
            item.Tag = f;
            _list.Items.Add(item);
        }
    }

    // ── Actions ───────────────────────────────────────────────────────────────

    private RemoteFile? Selected =>
        _list.SelectedItems.Count > 0 ? _list.SelectedItems[0].Tag as RemoteFile : null;

    private void CopyUrl()
    {
        if (Selected is { } f)
        {
            Clipboard.SetText(f.Url(_settings.ServerUrl));
            SetStatus($"Copied: {f.Url(_settings.ServerUrl)}");
        }
    }

    private void OpenInBrowser()
    {
        if (Selected is { } f)
            Process.Start(new ProcessStartInfo(f.Url(_settings.ServerUrl)) { UseShellExecute = true });
    }

    private async Task DeleteSelectedAsync()
    {
        if (Selected is not { } f) return;
        if (MessageBox.Show($"Delete {f.FileName}?", "DropPost", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes)
            return;

        try
        {
            var req = new HttpRequestMessage(HttpMethod.Delete, $"{_settings.ServerUrl}/{f.FileName}");
            req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _settings.ApiKey);
            var resp = await Http.SendAsync(req);
            resp.EnsureSuccessStatusCode();
            await LoadAsync();
        }
        catch (Exception ex)
        {
            SetStatus($"Delete failed: {ex.Message}");
        }
    }

    private void SetStatus(string msg) =>
        _statusLabel.Text = msg;
}
