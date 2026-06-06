using System.IO;
using System.Text.Json;
using FileDock.Models;
using Microsoft.Win32;

namespace FileDock.Services;

public sealed class SettingsService
{
    private const string RunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string RunValueName = "FileDock";

    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true
    };

    public string SettingsDirectory { get; } =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "FileDock");

    public string SettingsPath => Path.Combine(SettingsDirectory, "settings.json");

    public AppSettings Load()
    {
        if (!File.Exists(SettingsPath))
        {
            return new AppSettings();
        }

        try
        {
            var json = File.ReadAllText(SettingsPath);
            return JsonSerializer.Deserialize<AppSettings>(json, _jsonOptions) ?? new AppSettings();
        }
        catch
        {
            return new AppSettings();
        }
    }

    public void Save(AppSettings settings)
    {
        Directory.CreateDirectory(SettingsDirectory);
        var json = JsonSerializer.Serialize(settings, _jsonOptions);
        File.WriteAllText(SettingsPath, json);
    }

    public void ApplyStartupRegistration(bool enabled)
    {
        using var runKey = Registry.CurrentUser.CreateSubKey(RunKeyPath, writable: true);
        if (runKey is null)
        {
            return;
        }

        if (enabled)
        {
            var exePath = Environment.ProcessPath ?? string.Empty;
            runKey.SetValue(RunValueName, $"\"{exePath}\"");
        }
        else
        {
            runKey.DeleteValue(RunValueName, throwOnMissingValue: false);
        }
    }
}
