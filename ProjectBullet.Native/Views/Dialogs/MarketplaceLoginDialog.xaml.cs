using ProjectBullet.Native.ViewModels;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace ProjectBullet.Native.Views.Dialogs
{
    public partial class MarketplaceLoginDialog : Page
    {
        private readonly MarketplaceViewModel _vm;

        public MarketplaceLoginDialog(MarketplaceViewModel vm)
        {
            _vm = vm;
            InitializeComponent();

            logoutButton.Visibility = _vm.IsRegisteredUser ? Visibility.Visible : Visibility.Collapsed;
        }

        private void LoginPasswordKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter) _ = DoLoginAsync();
        }

        private void RegisterPasswordKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter) _ = DoRegisterAsync();
        }

        private async void DoLogin(object sender, RoutedEventArgs e) => await DoLoginAsync();

        private async System.Threading.Tasks.Task DoLoginAsync()
        {
            errorText.Text = string.Empty;
            var username = loginUsername.Text.Trim();
            var password = loginPassword.Password;

            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            {
                errorText.Text = "Please enter username and password.";
                return;
            }

            var (success, error) = await _vm.LoginAsync(username, password);

            if (success)
            {
                var dialog = Parent as MainDialog;
                if (dialog != null)
                {
                    dialog.DialogResult = true;
                    dialog.Close();
                }
            }
            else
            {
                errorText.Text = error ?? "Login failed.";
            }
        }

        private async void DoRegister(object sender, RoutedEventArgs e) => await DoRegisterAsync();

        private async System.Threading.Tasks.Task DoRegisterAsync()
        {
            errorText.Text = string.Empty;
            var username = registerUsername.Text.Trim();
            var password = registerPassword.Password;
            var confirm = registerConfirmPassword.Password;

            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            {
                errorText.Text = "Please enter username and password.";
                return;
            }

            if (password != confirm)
            {
                errorText.Text = "Passwords do not match.";
                return;
            }

            var (success, error) = await _vm.RegisterAsync(username, password);

            if (success)
            {
                var dialog = Parent as MainDialog;
                if (dialog != null)
                {
                    dialog.DialogResult = true;
                    dialog.Close();
                }
            }
            else
            {
                errorText.Text = error ?? "Registration failed.";
            }
        }

        private async void Logout(object sender, RoutedEventArgs e)
        {
            await _vm.LogoutAsync();
            var dialog = Parent as MainDialog;
            if (dialog != null)
            {
                dialog.DialogResult = true;
                dialog.Close();
            }
        }
    }
}
