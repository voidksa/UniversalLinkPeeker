using System;
using System.Windows;
using UniversalLinkPeeker.Services;

namespace UniversalLinkPeeker
{
    public partial class App : System.Windows.Application
    {
        private InputHookService _inputHook;
        private TextExtractionService _textExtractor;
        private MainWindow _previewWindow;
        private bool _isActivationKeyHeld = false;
        private string _currentUrl;

        private System.Windows.Forms.NotifyIcon _notifyIcon;

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
        }

        private void InitializeNotifyIcon()
        {
            _notifyIcon = new System.Windows.Forms.NotifyIcon();
            _notifyIcon.Icon = System.Drawing.SystemIcons.Information; // Placeholder icon
            _notifyIcon.Visible = true;
            _notifyIcon.Text = "Universal Link Peeker";

            var contextMenu = new System.Windows.Forms.ContextMenuStrip();

            var triggerMenu = new System.Windows.Forms.ToolStripMenuItem("Trigger Key");
            var shiftItem = new System.Windows.Forms.ToolStripMenuItem("Shift", null, (s, e) => SetTriggerKey(TriggerKey.Shift));
            var ctrlItem = new System.Windows.Forms.ToolStripMenuItem("Ctrl", null, (s, e) => SetTriggerKey(TriggerKey.Ctrl));
            var altItem = new System.Windows.Forms.ToolStripMenuItem("Alt", null, (s, e) => SetTriggerKey(TriggerKey.Alt));

            shiftItem.Checked = true; // Default

            triggerMenu.DropDownItems.Add(shiftItem);
            triggerMenu.DropDownItems.Add(ctrlItem);
            triggerMenu.DropDownItems.Add(altItem);

            contextMenu.Items.Add(triggerMenu);
            contextMenu.Items.Add("-");
            contextMenu.Items.Add("Exit", null, (s, e) => Shutdown());

            _notifyIcon.ContextMenuStrip = contextMenu;
        }

        private void SetTriggerKey(TriggerKey key)
        {
            _inputHook.CurrentTriggerKey = key;
            var triggerMenu = (System.Windows.Forms.ToolStripMenuItem)_notifyIcon.ContextMenuStrip.Items[0];
            foreach (System.Windows.Forms.ToolStripMenuItem item in triggerMenu.DropDownItems)
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
}
