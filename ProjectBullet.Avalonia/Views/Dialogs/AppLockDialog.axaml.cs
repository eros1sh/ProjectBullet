using ProjectBullet.Core.Models.Settings;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;

namespace ProjectBullet.Avalonia.Views.Dialogs
{
    public partial class AppLockDialog : UserControl
    {
        private readonly AppLockSettings _settings;
        public bool Unlocked { get; private set; }

        public AppLockDialog()
        {
            InitializeComponent();
        }

        public AppLockDialog(AppLockSettings settings)
        {
            _settings = settings;
            InitializeComponent();
            passwordBox.Focus();
        }

        private void Unlock(object sender, RoutedEventArgs e) => TryUnlock();

        protected override void OnKeyDown(KeyEventArgs e)
        {
            if (e.Key == Key.Enter) TryUnlock();
            base.OnKeyDown(e);
        }

        private void TryUnlock()
        {
            var password = passwordBox.Text;

            if (string.IsNullOrEmpty(password))
            {
                errorText.Text = "Please enter a password.";
                return;
            }

            if (BCrypt.Net.BCrypt.Verify(password, _settings.PasswordHash))
            {
                Unlocked = true;
                if (this.VisualRoot is MainDialog dialog)
                {
                    dialog.Close(true);
                }
            }
            else
            {
                errorText.Text = "Incorrect password. Please try again.";
                passwordBox.Text = string.Empty;
                passwordBox.Focus();
            }
        }

        private void Exit(object sender, RoutedEventArgs e)
        {
            if (this.VisualRoot is MainDialog dialog)
            {
                dialog.Close(false);
            }
        }
    }
}
