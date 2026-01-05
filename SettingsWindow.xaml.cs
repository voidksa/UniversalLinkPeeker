using System.Windows;
using UniversalLinkPeeker.Services;

namespace UniversalLinkPeeker
{
    public partial class SettingsWindow : Window
    {
        private AppSettings _settings;

        public SettingsWindow()
        {
            InitializeComponent();
            LoadSettings();
        }

        private void LoadSettings()
        {
            _settings = SettingsService.Load();

            switch (_settings.TriggerKey)
            {
                case TriggerKey.Shift: ShiftRadio.IsChecked = true; break;
                case TriggerKey.Ctrl: CtrlRadio.IsChecked = true; break;
                case TriggerKey.Alt: AltRadio.IsChecked = true; break;
            }

            switch (_settings.Theme)
            {
                case AppTheme.Auto: AutoThemeRadio.IsChecked = true; break;
                case AppTheme.Dark: DarkThemeRadio.IsChecked = true; break;
                case AppTheme.Light: LightThemeRadio.IsChecked = true; break;
            }

            CheckStartupState();
        }

        private void CheckStartupState()
        {
            try
            {
                using (var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run", false))
                {
                    if (key != null)
                    {
                        var value = key.GetValue("UniversalLinkPeeker");
                        RunOnStartupCheck.IsChecked = value != null;
                    }
                }
            }
            catch { }
        }

        private void RunOnStartup_Checked(object sender, RoutedEventArgs e)
        {
            SetStartup(true);
        }

        private void RunOnStartup_Unchecked(object sender, RoutedEventArgs e)
        {
            SetStartup(false);
        }

        private void SetStartup(bool enable)
        {
            try
            {
                using (var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run", true))
                {
                    if (key != null)
                    {
                        if (enable)
                        {
                            var exePath = System.Diagnostics.Process.GetCurrentProcess().MainModule?.FileName;
                            if (exePath != null)
                            {
                                key.SetValue("UniversalLinkPeeker", $"\"{exePath}\"");
                            }
                        }
                        else
                        {
                            key.DeleteValue("UniversalLinkPeeker", false);
                        }
                    }
                }
            }
            catch { }
        }

        private void TriggerKey_Checked(object sender, RoutedEventArgs e)
        {
            if (_settings == null) return;

            if (ShiftRadio.IsChecked == true) _settings.TriggerKey = TriggerKey.Shift;
            else if (CtrlRadio.IsChecked == true) _settings.TriggerKey = TriggerKey.Ctrl;
            else if (AltRadio.IsChecked == true) _settings.TriggerKey = TriggerKey.Alt;

            SettingsService.Save(_settings);
            ((App)System.Windows.Application.Current).UpdateTriggerKey(_settings.TriggerKey);
        }

        private void Theme_Checked(object sender, RoutedEventArgs e)
        {
            if (_settings == null) return;

            if (AutoThemeRadio.IsChecked == true) _settings.Theme = AppTheme.Auto;
            else if (DarkThemeRadio.IsChecked == true) _settings.Theme = AppTheme.Dark;
            else if (LightThemeRadio.IsChecked == true) _settings.Theme = AppTheme.Light;

            SettingsService.Save(_settings);
            ((App)System.Windows.Application.Current).UpdateTheme();
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void GitHub_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "https://github.com/voidksa/UniversalLinkPeeker",
                    UseShellExecute = true
                });
            }
            catch { }
        }
    }
}
