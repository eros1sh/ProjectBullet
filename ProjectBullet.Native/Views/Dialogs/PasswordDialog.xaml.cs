using System;
using System.Windows;
using System.Windows.Controls;

namespace ProjectBullet.Native.Views.Dialogs
{
    /// <summary>
    /// Interaction logic for PasswordDialog.xaml
    /// </summary>
    public partial class PasswordDialog : Page
    {
        private readonly Action<string> onPassword;

        public PasswordDialog(string prompt, Action<string> onPassword)
        {
            this.onPassword = onPassword;
            InitializeComponent();

            promptText.Text = prompt;
            passwordBox.Focus();
        }

        private void Ok(object sender, RoutedEventArgs e)
        {
            var password = passwordBox.Password;

            if (string.IsNullOrEmpty(password))
                return;

            onPassword(password);
            ((MainDialog)Parent).Close();
        }

        private void Cancel(object sender, RoutedEventArgs e)
        {
            ((MainDialog)Parent).Close();
        }
    }
}
