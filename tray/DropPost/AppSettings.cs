using System;
using System.IO;
using System.Text.Json;
using Microsoft.Win32;

namespace DropPost;

class AppSettings
{
    public string ServerUrl      { get; set; } = "https://YOUR_DOMAIN";
    public string ApiKey         { get; set; } = "";
    public string DefaultExpiry  { get; set; } = "24h";
    public bool   StartWithWindows { get; set; } = false;

    private static string SettingsPath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "DropPost", "settings.json");

    public static AppSettings Load()
    {
        try
        {
            if (File.Exists(SettingsPath))
                return JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(SettingsPath))
                       ?? new AppSettings();
        }
        catch { }
        return new AppSettings();
    }

    public void Save()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(SettingsPath)!);
        File.WriteAllText(SettingsPath,
            JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true }));
        ApplyAutoStart();
    }

    private void ApplyAutoStart()
    {
        using var key = Registry.CurrentUser.OpenSubKey(
            @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", writable: true)!;
        if (StartWithWindows)
            key.SetValue("DropPost", $"\"{Environment.ProcessPath}\"");
        else
            key.DeleteValue("DropPost", throwOnMissingValue: false);
    }
}
