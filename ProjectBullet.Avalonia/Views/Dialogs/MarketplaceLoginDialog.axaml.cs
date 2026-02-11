using ProjectBullet.Avalonia.ViewModels;
using System;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;

namespace ProjectBullet.Avalonia.Views.Dialogs
{
    public partial class MarketplaceLoginDialog : UserControl
    {
        private readonly MarketplaceViewModel _vm;

        public MarketplaceLoginDialog()
        {
            InitializeComponent();
        }

        public MarketplaceLoginDialog(MarketplaceViewModel vm)
        {
            _vm = vm;
            InitializeComponent();

            logoutButton.IsVisible = _vm.IsRegisteredUser;
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                if (tabControl.SelectedIndex == 0)
                    _ = DoLoginAsync();
                else
                    _ = DoRegisterAsync();
            }
            base.OnKeyDown(e);
        }

        private async void DoLogin(object sender, RoutedEventArgs e) => await DoLoginAsync();

        private async System.Threading.Tasks.Task DoLoginAsync()
        {
            errorText.Text = string.Empty;
            var username = loginUsername.Text?.Trim();
            var password = loginPassword.Text;

            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            {
                errorText.Text = "Please enter username and password.";
                return;
            }

            var (success, error) = await _vm.LoginAsync(username, password);

            if (success)
            {
                if (this.VisualRoot is MainDialog dialog)
                {
                    dialog.Close(true);
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
            var username = registerUsername.Text?.Trim();
            var password = registerPassword.Text;
            var confirm = registerConfirmPassword.Text;

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
                if (this.VisualRoot is MainDialog dialog)
                {
                    dialog.Close(true);
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
            if (this.VisualRoot is MainDialog dialog)
            {
                dialog.Close(true);
            }
        }
    }
}
