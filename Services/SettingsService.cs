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
    private string BackupPath => Path.Combine(SettingsDirectory, "settings.json.bak");
    private string TempPath => Path.Combine(SettingsDirectory, "settings.json.tmp");

    public AppSettings Load()
    {
        return TryLoad(SettingsPath) ?? TryLoad(BackupPath) ?? new AppSettings();
    }

    public void Save(AppSettings settings)
    {
        try
        {
            Directory.CreateDirectory(SettingsDirectory);
            var json = JsonSerializer.Serialize(settings, _jsonOptions);
            File.WriteAllText(TempPath, json);

            if (File.Exists(SettingsPath))
            {
                File.Replace(TempPath, SettingsPath, BackupPath, ignoreMetadataErrors: true);
            }
            else
            {
                File.Move(TempPath, SettingsPath);
                File.Copy(SettingsPath, BackupPath, overwrite: true);
            }
        }
        catch
        {
            TryDeleteTempFile();
        }
    }

    private void TryDeleteTempFile()
    {
        try
        {
            if (File.Exists(TempPath))
            {
                File.Delete(TempPath);
            }
        }
        catch
        {
            // Best-effort cleanup only; saving settings must not crash the app.
        }
    }

    private AppSettings? TryLoad(string path)
    {
        if (!File.Exists(path))
        {
            return null;
        }

        try
        {
            var json = File.ReadAllText(path);
            return JsonSerializer.Deserialize<AppSettings>(json, _jsonOptions);
        }
        catch
        {
            return null;
        }
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
