using System;
using System.Windows;
using System.Windows.Controls;

namespace ProjectBullet.Native.Views.Dialogs
{
    public partial class AppLockSetupDialog : Page
    {
        private readonly Action<string> _onPasswordSet;

        public AppLockSetupDialog(Action<string> onPasswordSet)
        {
            _onPasswordSet = onPasswordSet;
            InitializeComponent();
            passwordBox.Focus();
        }

        private void Save(object sender, RoutedEventArgs e)
        {
            var password = passwordBox.Password;
            var confirm = confirmBox.Password;

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
            ((MainDialog)Parent).Close();
        }

        private void Cancel(object sender, RoutedEventArgs e)
        {
            ((MainDialog)Parent).Close();
        }
    }
}
