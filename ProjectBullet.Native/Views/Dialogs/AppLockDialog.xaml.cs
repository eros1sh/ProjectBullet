using ProjectBullet.Core.Models.Settings;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace ProjectBullet.Native.Views.Dialogs
{
    public partial class AppLockDialog : Page
    {
        private readonly AppLockSettings _settings;
        public bool Unlocked { get; private set; }

        public AppLockDialog(AppLockSettings settings)
        {
            _settings = settings;
            InitializeComponent();
            passwordBox.Focus();
        }

        private void Unlock(object sender, RoutedEventArgs e) => TryUnlock();

        private void PasswordBoxKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter) TryUnlock();
        }

        private void TryUnlock()
        {
            var password = passwordBox.Password;

            if (string.IsNullOrEmpty(password))
            {
                errorText.Text = "Please enter a password.";
                return;
            }

            if (BCrypt.Net.BCrypt.Verify(password, _settings.PasswordHash))
            {
                Unlocked = true;
                var dialog = Parent as MainDialog;
                if (dialog != null)
                {
                    dialog.DialogResult = true;
                    dialog.Close();
                }
            }
            else
            {
                errorText.Text = "Incorrect password. Please try again.";
                passwordBox.Password = string.Empty;
                passwordBox.Focus();
            }
        }

        private void Exit(object sender, RoutedEventArgs e)
        {
            var dialog = Parent as MainDialog;
            if (dialog != null)
            {
                dialog.DialogResult = false;
                dialog.Close();
            }
        }
    }
}
