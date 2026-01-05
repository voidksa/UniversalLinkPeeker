using System;
using System.Windows;
using UniversalLinkPeeker.Services;
using Microsoft.Win32;
using System.Drawing;
using System.Windows.Forms;

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
        private ContextMenuStrip _contextMenu;

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            InitializeNotifyIcon();

            _inputHook = new InputHookService();
            _textExtractor = new TextExtractionService();

            // Create window but don't show it yet
            _previewWindow = new MainWindow();

            _inputHook.ActivationKeyPressed += OnActivationKeyPressed;
            _inputHook.ActivationKeyReleased += OnActivationKeyReleased;
            _inputHook.MouseMoved += OnMouseMoved;
            _inputHook.MouseWheel += OnMouseWheel;
            _inputHook.CopyCommandTriggered += OnCopyCommandTriggered;

            SystemEvents.UserPreferenceChanged += (s, args) => ApplyThemeToContextMenu();
        }

        private void InitializeNotifyIcon()
        {
            _notifyIcon = new NotifyIcon();
            try
            {
                // Use the application's own icon (embedded resource)
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

            _contextMenu = new ContextMenuStrip();

            var triggerMenu = new ToolStripMenuItem("Trigger Key");
            var shiftItem = new ToolStripMenuItem("Shift", null, (s, e) => SetTriggerKey(TriggerKey.Shift));
            var ctrlItem = new ToolStripMenuItem("Ctrl", null, (s, e) => SetTriggerKey(TriggerKey.Ctrl));
            var altItem = new ToolStripMenuItem("Alt", null, (s, e) => SetTriggerKey(TriggerKey.Alt));

            shiftItem.Checked = true; // Default

            triggerMenu.DropDownItems.Add(shiftItem);
            triggerMenu.DropDownItems.Add(ctrlItem);
            triggerMenu.DropDownItems.Add(altItem);

            _contextMenu.Items.Add(triggerMenu);
            _contextMenu.Items.Add("-");
            _contextMenu.Items.Add("Exit", null, (s, e) => Shutdown());

            _notifyIcon.ContextMenuStrip = _contextMenu;

            ApplyThemeToContextMenu();
        }

        private void ApplyThemeToContextMenu()
        {
            if (_contextMenu == null) return;

            bool isDark = IsDarkMode();

            if (isDark)
            {
                _contextMenu.Renderer = new ToolStripProfessionalRenderer(new DarkColorTable());
                _contextMenu.ForeColor = Color.White;
                foreach (ToolStripItem item in _contextMenu.Items)
                {
                    UpdateItemColor(item, Color.White, Color.FromArgb(40, 40, 40));
                }
            }
            else
            {
                _contextMenu.Renderer = new ToolStripProfessionalRenderer(new ProfessionalColorTable()); // Default
                _contextMenu.ForeColor = Color.Black;
                foreach (ToolStripItem item in _contextMenu.Items)
                {
                    UpdateItemColor(item, Color.Black, Color.White);
                }
            }
        }

        private void UpdateItemColor(ToolStripItem item, Color foreColor, Color backColor)
        {
            item.ForeColor = foreColor;
            // item.BackColor = backColor; // Renderer handles background mostly, but setting it helps in some modes
            if (item is ToolStripMenuItem menuItem)
            {
                foreach (ToolStripItem subItem in menuItem.DropDownItems)
                {
                    UpdateItemColor(subItem, foreColor, backColor);
                }
            }
        }

        private bool IsDarkMode()
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
            return false; // Default to Light
        }

        private void SetTriggerKey(TriggerKey key)
        {
            _inputHook.CurrentTriggerKey = key;
            var triggerMenu = (ToolStripMenuItem)_notifyIcon.ContextMenuStrip.Items[0];
            foreach (ToolStripMenuItem item in triggerMenu.DropDownItems)
            {
                item.Checked = item.Text == key.ToString();
            }
        }

        private void OnActivationKeyPressed(object sender, EventArgs e)
        {
            if (_isActivationKeyHeld) return;
            _isActivationKeyHeld = true;

            CheckAndPeek();
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
                    }
                    catch { }
                });
            }
        }

        private void CheckAndPeek()
        {
            // Cursor position in pixels
            var mousePos = System.Windows.Forms.Cursor.Position;

            // Extract URL
            string url = _textExtractor.GetUrlUnderMouse(mousePos);

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

    public class DarkColorTable : ProfessionalColorTable
    {
        public override Color MenuItemSelected => Color.FromArgb(60, 60, 60);
        public override Color MenuItemBorder => Color.FromArgb(60, 60, 60);
        public override Color MenuBorder => Color.FromArgb(40, 40, 40);
        public override Color MenuItemPressedGradientBegin => Color.FromArgb(40, 40, 40);
        public override Color MenuItemPressedGradientEnd => Color.FromArgb(40, 40, 40);
        public override Color MenuItemSelectedGradientBegin => Color.FromArgb(60, 60, 60);
        public override Color MenuItemSelectedGradientEnd => Color.FromArgb(60, 60, 60);
        public override Color ToolStripDropDownBackground => Color.FromArgb(40, 40, 40);
        public override Color ImageMarginGradientBegin => Color.FromArgb(40, 40, 40);
        public override Color ImageMarginGradientMiddle => Color.FromArgb(40, 40, 40);
        public override Color ImageMarginGradientEnd => Color.FromArgb(40, 40, 40);
    }
}
