using System;
using System.Net.Http;
using System.Reflection;
using System.Text.Json;
using System.Threading.Tasks;

namespace UniversalLinkPeeker.Services
{
    public class UpdateInfo
    {
        public bool IsUpdateAvailable { get; set; }
        public string CurrentVersion { get; set; } = "";
        public string LatestVersion { get; set; } = "";
        public string Url { get; set; } = "";
    }

    public class UpdateService
    {
        private readonly HttpClient _http = new HttpClient();

        public UpdateService()
        {
            _http.DefaultRequestHeaders.UserAgent.ParseAdd("UniversalLinkPeeker/1.0");
        }

        public async Task<UpdateInfo> CheckForUpdateAsync()
        {
            var current = GetCurrentVersion();
            var info = new UpdateInfo { CurrentVersion = current };

            try
            {
                var json = await _http.GetStringAsync("https://api.github.com/repos/voidksa/UniversalLinkPeeker/releases/latest");
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;
                var tag = root.TryGetProperty("tag_name", out var t) ? t.GetString() ?? "" : "";
                var url = root.TryGetProperty("html_url", out var u) ? u.GetString() ?? "" : "";

                var latest = tag.StartsWith("v", StringComparison.OrdinalIgnoreCase) ? tag.Substring(1) : tag;
                info.LatestVersion = latest;
                info.Url = string.IsNullOrWhiteSpace(url) ? "https://github.com/voidksa/UniversalLinkPeeker/releases/latest" : url;

                if (TryParseVersion(latest, out var l) && TryParseVersion(current, out var c))
                {
                    info.IsUpdateAvailable = l > c;
                }
            }
            catch
            {
            }

            return info;
        }

        public void OpenLatestRelease(string url)
        {
            try
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(url) { UseShellExecute = true });
            }
            catch
            {
            }
        }

        private static string GetCurrentVersion()
        {
            var attr = Assembly.GetEntryAssembly()?.GetCustomAttribute<AssemblyInformationalVersionAttribute>();
            var v = attr?.InformationalVersion ?? "1.1.0";
            return v.Contains('+') ? v.Split('+')[0] : v;
        }

        private static bool TryParseVersion(string s, out Version v)
        {
            if (Version.TryParse(s, out var parsed))
            {
                v = parsed;
                return true;
            }
            v = new Version(0, 0, 0);
            return false;
        }
    }
}
