using System;
using System.Windows;
using System.Windows.Media;

namespace UniversalLinkPeeker
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            InitializeWebView();
        }

        private async void InitializeWebView()
        {
            try
            {
                await webView.EnsureCoreWebView2Async();
                webView.CoreWebView2.Settings.AreDefaultContextMenusEnabled = false;
                webView.CoreWebView2.Settings.AreDevToolsEnabled = false;
                webView.ZoomFactor = 0.6; // 60% zoom to see more content

                webView.NavigationCompleted += (s, e) =>
                {
                    webView.ZoomFactor = 0.6;
                    // Show WebView and hide loading when ready
                    webView.Visibility = Visibility.Visible;
                    LoadingGrid.Visibility = Visibility.Collapsed;
                };


                // Security: Prevent any downloads
                webView.CoreWebView2.DownloadStarting += (s, e) =>
                {
                    e.Cancel = true;
                    e.Handled = true;
                };

                // Security: Prevent new windows
                webView.CoreWebView2.NewWindowRequested += (s, e) =>
                {
                    e.Handled = true;
                };
            }
            catch (Exception)
            {
                // Silent fail or log? For now, silent.
            }
        }

        public void Navigate(string url)
        {
            if (IsUnsafeUrl(url))
            {
                // Navigate to a safe error page or just blank
                url = "about:blank";
            }

            // Update Header
            try
            {
                Uri uri = new Uri(url);
                UrlTitle.Text = uri.Host; // Show domain in header
            }
            catch
            {
                UrlTitle.Text = "Link Preview";
            }

            // Reset UI state for loading
            LoadingGrid.Visibility = Visibility.Visible;
            webView.Visibility = Visibility.Hidden; // Hide until loaded

            if (webView.CoreWebView2 != null)
            {
                webView.CoreWebView2.Navigate(url);
            }
            else
            {
                try
                {
                    webView.Source = new Uri(url);
                }
                catch { }
            }
        }

        private bool IsUnsafeUrl(string url)
        {
            if (string.IsNullOrWhiteSpace(url)) return true;

            try
            {
                Uri uri = new Uri(url);
                string path = uri.AbsolutePath.ToLowerInvariant();

                string[] unsafeExtensions = new[] {
                    ".exe", ".msi", ".bat", ".cmd", ".scr", ".com",
                    ".zip", ".rar", ".7z", ".tar", ".gz", ".iso", ".dmg"
                };

                foreach (var ext in unsafeExtensions)
                {
                    if (path.EndsWith(ext)) return true;
                }
            }
            catch
            {
                // If we can't parse it, treat as safe (let WebView2 handle it) or unsafe?
                // For now, let WebView2 handle it, but DownloadStarting will catch it if it tries to download.
            }

            return false;
        }

        public async void Scroll(int delta)
        {
            if (webView.CoreWebView2 != null)
            {
                // Scroll amount: usually one "notch" is 120.
                // Windows default is 3 lines.
                // Let's scroll 100 pixels per notch for smooth-ish feel.
                // delta > 0 => Scroll Up => scrollTop decreases.
                // delta < 0 => Scroll Down => scrollTop increases.
                // window.scrollBy(x, y): y > 0 is down, y < 0 is up.
                // If delta is 120 (Up), we want y to be negative.
                // So y = -delta;

                int scrollAmount = (int)((double)delta / 120 * -150); // -150px per notch

                try
                {
                    await webView.CoreWebView2.ExecuteScriptAsync($"window.scrollBy(0, {scrollAmount})");
                }
                catch { }
            }
        }

        public void UpdatePosition(System.Drawing.Point mousePos)
        {
            var dpi = VisualTreeHelper.GetDpi(this);
            double scaleX = dpi.DpiScaleX;
            double scaleY = dpi.DpiScaleY;

            // Convert pixels to DIPs
            double mouseX = mousePos.X / scaleX;
            double mouseY = mousePos.Y / scaleY;

            double offset = 15;
            double left = mouseX + offset;
            double top = mouseY + offset;

            // Simple screen bounds check (Primary screen only for MVP)
            double screenWidth = SystemParameters.PrimaryScreenWidth;
            double screenHeight = SystemParameters.PrimaryScreenHeight;

            if (left + Width > screenWidth)
            {
                left = mouseX - Width - offset;
            }

            if (top + Height > screenHeight)
            {
                top = mouseY - Height - offset;
            }

            // Ensure not off-screen top/left
            if (left < 0) left = 0;
            if (top < 0) top = 0;

            this.Left = left;
            this.Top = top;
        }

        public async void ShowNotification(string message)
        {
            NotificationText.Text = message;
            try
            {
                // Open popup first so it measures
                CopyPopup.IsOpen = true;

                // Measure and center horizontally over MainBorder, and place slightly above bottom
                CopyPopupBorder.Measure(new System.Windows.Size(double.PositiveInfinity, double.PositiveInfinity));
                double contentWidth = CopyPopupBorder.ActualWidth > 0 ? CopyPopupBorder.ActualWidth : CopyPopupBorder.DesiredSize.Width;
                double horizontalCenter = (MainBorder.ActualWidth - contentWidth) / 2;

                CopyPopup.HorizontalOffset = horizontalCenter;
                CopyPopup.VerticalOffset = -20; // lift above the bottom edge

                await System.Threading.Tasks.Task.Delay(1500);
            }
            finally
            {
                CopyPopup.IsOpen = false;
            }
        }
    }
}
