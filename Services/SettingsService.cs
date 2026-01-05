using System;
using System.IO;
using System.Text.Json;
using UniversalLinkPeeker.Services;

namespace UniversalLinkPeeker.Services
{
    public enum AppTheme
    {
        Auto,
        Dark,
        Light
    }

    public class AppSettings
    {
        public TriggerKey TriggerKey { get; set; } = TriggerKey.Shift;
        public AppTheme Theme { get; set; } = AppTheme.Auto;
    }

    public static class SettingsService
    {
        private static readonly string SettingsPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "UniversalLinkPeeker",
            "settings.json");

        public static AppSettings Load()
        {
            try
            {
                if (File.Exists(SettingsPath))
                {
                    var json = File.ReadAllText(SettingsPath);
                    return JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
                }
            }
            catch { }
            return new AppSettings();
        }

        public static void Save(AppSettings settings)
        {
            try
            {
                var dir = Path.GetDirectoryName(SettingsPath);
                if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);

                var json = JsonSerializer.Serialize(settings);
                File.WriteAllText(SettingsPath, json);
            }
            catch { }
        }
    }
}
