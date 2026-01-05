using System;
using System.Windows;
using UniversalLinkPeeker.Services;
using Microsoft.Win32;
using System.Drawing;
using System.Windows.Forms;
using System.Windows.Media;

namespace UniversalLinkPeeker
{
    public partial class App : System.Windows.Application
    {
        private InputHookService _inputHook;
        private TextExtractionService _textExtractor;
        private MainWindow _previewWindow;
        private bool _isActivationKeyHeld = false;
        private string _currentUrl;

        private NotifyIcon _notifyIcon;
        private UpdateService _updateService;
        private MenuWindow _menuWindow;
        private AppSettings _settings;

        public InputHookService InputHook => _inputHook;

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            _settings = SettingsService.Load();

            // Apply initial theme
            UpdateTheme();

            // Watch for system theme changes if in Auto mode
            SystemEvents.UserPreferenceChanged += (s, args) =>
            {
                if (_settings.Theme == AppTheme.Auto)
                {
                    UpdateTheme();
                }
            };

            InitializeNotifyIcon();
            _updateService = new UpdateService();

            _inputHook = new InputHookService();
            _inputHook.CurrentTriggerKey = _settings.TriggerKey;

            _textExtractor = new TextExtractionService();

            // Create window but don't show it yet
            _previewWindow = new MainWindow();

            _inputHook.ActivationKeyPressed += OnActivationKeyPressed;
            _inputHook.ActivationKeyReleased += OnActivationKeyReleased;
            _inputHook.MouseMoved += OnMouseMoved;
            _inputHook.MouseWheel += OnMouseWheel;
            _inputHook.CopyCommandTriggered += OnCopyCommandTriggered;

            // Check for updates on startup
            System.Threading.Tasks.Task.Run(async () =>
            {
                var info = await _updateService.CheckForUpdateAsync();
                if (info.IsUpdateAvailable)
                {
                    _notifyIcon.BalloonTipTitle = "Update Available";
                    _notifyIcon.BalloonTipText = $"Latest: {info.LatestVersion}";
                    _notifyIcon.ShowBalloonTip(5000);
                    _notifyIcon.BalloonTipClicked += (o, args) => _updateService.OpenLatestRelease(info.Url);
                }
            });
        }

        private void InitializeNotifyIcon()
        {
            _notifyIcon = new NotifyIcon();
            try
            {
                var iconPath = System.Diagnostics.Process.GetCurrentProcess().MainModule?.FileName;
                if (!string.IsNullOrEmpty(iconPath))
                {
                    _notifyIcon.Icon = Icon.ExtractAssociatedIcon(iconPath);
                }
                else
                {
                    _notifyIcon.Icon = SystemIcons.Application;
                }
            }
            catch
            {
                _notifyIcon.Icon = SystemIcons.Application;
            }

            _notifyIcon.Visible = true;
            _notifyIcon.Text = "Universal Link Peeker";

            // Handle MouseClick to show custom menu
            _notifyIcon.MouseClick += NotifyIcon_MouseClick;
        }

        private void NotifyIcon_MouseClick(object sender, System.Windows.Forms.MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
                ShowMenu();
            }
        }

        private void ShowMenu()
        {
            if (_menuWindow == null || !_menuWindow.IsLoaded)
            {
                _menuWindow = new MenuWindow();
            }

            var mousePos = System.Windows.Forms.Cursor.Position;

            // Adjust position to keep it on screen (simple logic)
            _menuWindow.Left = mousePos.X - _menuWindow.Width; // Show to the left of cursor usually
            _menuWindow.Top = mousePos.Y - _menuWindow.Height; // Show above cursor usually

            // Better positioning:
            // If near bottom right (tray), show top-left of cursor
            if (_menuWindow.Left < 0) _menuWindow.Left = mousePos.X;
            if (_menuWindow.Top < 0) _menuWindow.Top = mousePos.Y;

            _menuWindow.Show();
            _menuWindow.Activate();
        }

        public void UpdateTriggerKey(TriggerKey key)
        {
            if (_inputHook != null)
            {
                _inputHook.CurrentTriggerKey = key;
            }
        }


        public void UpdateTheme()
        {
            _settings = SettingsService.Load();

            bool isDark = true; // Default

            if (_settings.Theme == AppTheme.Auto)
            {
                isDark = IsSystemInDarkMode();
            }
            else
            {
                isDark = _settings.Theme == AppTheme.Dark;
            }

            // Update Resources
            var res = System.Windows.Application.Current.Resources;

            if (isDark)
            {
                res["WindowBackgroundBrush"] = new SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#202124"));
                res["WindowBorderBrush"] = new SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#3C4043"));
                res["HeaderBackgroundBrush"] = new SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#292A2D"));
                res["PrimaryTextBrush"] = new SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#E8EAED"));
                res["SecondaryTextBrush"] = new SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#9AA0A6"));
                res["ButtonHoverBrush"] = new SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#3C4043"));
                res["SeparatorBrush"] = new SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#3C4043"));
            }
            else
            {
                res["WindowBackgroundBrush"] = new SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#FFFFFF"));
                res["WindowBorderBrush"] = new SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#E5E5E5"));
                res["HeaderBackgroundBrush"] = new SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#F8F9FA"));
                res["PrimaryTextBrush"] = new SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#5F6368"));
                res["SecondaryTextBrush"] = new SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#5F6368"));
                res["ButtonHoverBrush"] = new SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#F1F3F4"));
                res["SeparatorBrush"] = new SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#E5E5E5"));
            }
        }

        private bool IsSystemInDarkMode()
        {
            try
            {
                using (var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize"))
                {
                    if (key != null)
                    {
                        var val = key.GetValue("AppsUseLightTheme");
                        if (val != null)
                        {
                            return (int)val == 0;
                        }
                    }
                }
            }
            catch { }
            return false; // Default to Light if detection fails, but app defaults to Dark usually. Let's default to Light here if unknown.
        }

        private async void OnActivationKeyPressed(object sender, EventArgs e)
        {
            if (_isActivationKeyHeld) return;
            _isActivationKeyHeld = true;

            await CheckAndPeekAsync();
        }

        private void OnActivationKeyReleased(object sender, EventArgs e)
        {
            _isActivationKeyHeld = false;
            _previewWindow.Hide();
            // Stop playing media if any
            _previewWindow.Navigate("about:blank");
            _currentUrl = null;
        }

        private void OnMouseMoved(object sender, System.Windows.Forms.MouseEventArgs e)
        {
            if (_isActivationKeyHeld && _previewWindow.Visibility == Visibility.Visible)
            {
                _previewWindow.UpdatePosition(e.Location);
            }
        }

        private void OnMouseWheel(object sender, System.Windows.Forms.MouseEventArgs e)
        {
            if (_isActivationKeyHeld && _previewWindow.Visibility == Visibility.Visible)
            {
                _previewWindow.Scroll(e.Delta);
                // Optionally handle the event to prevent background scrolling
                // But Gma.System.MouseKeyHook might not support 'Handled' fully for all hook types, or it might be 'e.Handled = true;'
                // Let's check if we can suppress it. 
                // e is MouseEventArgs from WinForms, checking if it has Handled.
                // It does usually have Handled in MouseKeyHook if it's derived from MouseEventExtArgs, but here the signature is MouseEventArgs.
                // Let's cast to see if it is MouseEventExtArgs which has Handled property.
                if (e is Gma.System.MouseKeyHook.MouseEventExtArgs args)
                {
                    args.Handled = true;
                }
            }
        }

        private void OnCopyCommandTriggered(object sender, EventArgs e)
        {
            if (_previewWindow.Visibility == Visibility.Visible && !string.IsNullOrEmpty(_currentUrl))
            {
                Dispatcher.Invoke(() =>
                {
                    try
                    {
                        System.Windows.Clipboard.SetText(_currentUrl);
                        _previewWindow.ShowNotification("Copied!");
                    }
                    catch { }
                });
            }
        }

        private async System.Threading.Tasks.Task CheckAndPeekAsync()
        {
            // Cursor position in pixels
            var mousePos = System.Windows.Forms.Cursor.Position;

            string url = null;

            // Run extraction on background thread to prevent UI freeze
            await System.Threading.Tasks.Task.Run(() => 
            {
                try
                {
                    // Check if key is still held before starting expensive operation
                    if (!_isActivationKeyHeld) return;
                    
                    url = _textExtractor.GetUrlUnderMouse(mousePos);
                }
                catch { }
            });

            // If key was released while we were processing, abort
            if (!_isActivationKeyHeld) return;

            if (!string.IsNullOrEmpty(url))
            {
                _currentUrl = url;
                _previewWindow.UpdatePosition(mousePos);
                _previewWindow.Navigate(url);
                _previewWindow.Show();
            }
        }

        protected override void OnExit(ExitEventArgs e)
        {
            _notifyIcon?.Dispose();
            _inputHook?.Dispose();
            _textExtractor?.Dispose();
            base.OnExit(e);
        }
    }
}
