using System;
using Avalonia.Controls;
using Avalonia.Interactivity;

namespace ProjectBullet.Avalonia.Views.Dialogs
{
    public partial class AppLockSetupDialog : UserControl
    {
        private readonly Action<string> _onPasswordSet;

        public AppLockSetupDialog()
        {
            InitializeComponent();
        }

        public AppLockSetupDialog(Action<string> onPasswordSet)
        {
            _onPasswordSet = onPasswordSet;
            InitializeComponent();
            passwordBox.Focus();
        }

        private void Save(object sender, RoutedEventArgs e)
        {
            var password = passwordBox.Text;
            var confirm = confirmBox.Text;

            if (string.IsNullOrEmpty(password))
            {
                errorText.Text = "Password cannot be empty.";
                return;
            }

            if (password.Length < 4)
            {
                errorText.Text = "Password must be at least 4 characters.";
                return;
            }

            if (password != confirm)
            {
                errorText.Text = "Passwords do not match.";
                return;
            }

            var hash = BCrypt.Net.BCrypt.HashPassword(password);
            _onPasswordSet(hash);
            if (this.VisualRoot is MainDialog dialog) dialog.Close();
        }

        private void Cancel(object sender, RoutedEventArgs e)
        {
            if (this.VisualRoot is MainDialog dialog) dialog.Close();
        }
    }
}
