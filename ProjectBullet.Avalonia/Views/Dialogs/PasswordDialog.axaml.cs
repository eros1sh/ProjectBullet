using System;
using Avalonia.Controls;
using Avalonia.Interactivity;

namespace ProjectBullet.Avalonia.Views.Dialogs
{
    public partial class PasswordDialog : UserControl
    {
        private readonly Action<string> onPassword;

        public PasswordDialog()
        {
            InitializeComponent();
        }

        public PasswordDialog(string prompt, Action<string> onPassword)
        {
            this.onPassword = onPassword;
            InitializeComponent();

            promptText.Text = prompt;
            passwordBox.Focus();
        }

        private void Ok(object sender, RoutedEventArgs e)
        {
            var password = passwordBox.Text;

            if (string.IsNullOrEmpty(password))
                return;

            onPassword(password);
            if (this.VisualRoot is MainDialog dialog) dialog.Close();
        }

        private void Cancel(object sender, RoutedEventArgs e)
        {
            if (this.VisualRoot is MainDialog dialog) dialog.Close();
        }
    }
}
