namespace TimerApp.Desktop.Models;

using System;
using System.IO;
using System.Text.Json;

public class AppSettings
{
    public bool VoiceEnabled    { get; set; } = false;
    public int  VoiceDeviceIndex { get; set; } = -1; // -1 = padrão do sistema

    private static readonly string SettingsFile = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "TibiaTimer", "settings.json");

    private static readonly JsonSerializerOptions Opts = new() { WriteIndented = true };

    public static AppSettings Load()
    {
        if (!File.Exists(SettingsFile)) return new();
        try { return JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(SettingsFile), Opts) ?? new(); }
        catch { return new(); }
    }

    public void Save()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(SettingsFile)!);
        File.WriteAllText(SettingsFile, JsonSerializer.Serialize(this, Opts));
    }
}
