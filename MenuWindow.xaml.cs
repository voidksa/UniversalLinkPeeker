using System;
using System.Windows;

namespace UniversalLinkPeeker
{
    public partial class MenuWindow : Window
    {
        public MenuWindow()
        {
            InitializeComponent();
        }

        private void Window_Deactivated(object sender, EventArgs e)
        {
            this.Hide();
        }

        private void Settings_Click(object sender, RoutedEventArgs e)
        {
            this.Hide();
            // Show Settings Window
            var settings = new SettingsWindow();
            settings.Show();
        }

        private void Exit_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.Application.Current.Shutdown();
        }
    }
}
