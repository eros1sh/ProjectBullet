using ProjectBullet.Core.Models.Marketplace;
using ProjectBullet.Native.Helpers;
using ProjectBullet.Native.Services;
using ProjectBullet.Native.ViewModels;
using ProjectBullet.Native.Views.Dialogs;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace ProjectBullet.Native.Views.Pages
{
    public partial class Marketplace : Page
    {
        private readonly MarketplaceViewModel vm;

        public Marketplace()
        {
            vm = SP.GetService<ViewModelsService>().Marketplace;
            DataContext = vm;

            InitializeComponent();

            itemsListView.SelectionChanged += ItemsListView_SelectionChanged;

            Loaded += async (_, _) =>
            {
                vm.RefreshAuthState();
                if (vm.Items.Count == 0)
                    await vm.LoadItemsAsync();
            };
        }

        private void ItemsListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (itemsListView.SelectedItem is MarketplaceItem selected && vm.IsOwnItem(selected))
            {
                updateButton.Visibility = Visibility.Visible;
            }
            else
            {
                updateButton.Visibility = Visibility.Collapsed;
            }
        }

        private async void Refresh(object sender, RoutedEventArgs e) => await vm.LoadItemsAsync();

        private async void Search(object sender, RoutedEventArgs e) => await vm.SearchAsync();

        private void SearchBoxKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
                _ = vm.SearchAsync();
        }

        private async void CategoryChanged(object sender, SelectionChangedEventArgs e)
        {
            if (categoryCombo.SelectedItem is ComboBoxItem item)
            {
                vm.SelectedCategory = item.Content?.ToString() ?? "all";
                vm.CurrentPage = 1;
                await vm.LoadItemsAsync();
            }
        }

        private async void SortChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sortCombo.SelectedItem is ComboBoxItem item)
            {
                vm.SelectedSort = item.Content?.ToString() ?? "newest";
                vm.CurrentPage = 1;
                await vm.LoadItemsAsync();
            }
        }

        private async void PrevPage(object sender, RoutedEventArgs e) => await vm.PrevPage();

        private async void NextPage(object sender, RoutedEventArgs e) => await vm.NextPage();

        private async void Download(object sender, RoutedEventArgs e)
        {
            if (itemsListView.SelectedItem is not MarketplaceItem selected)
            {
                Alert.Warning("No selection", "Please select an item to download.");
                return;
            }

            try
            {
                await vm.DownloadItemAsync(selected);
            }
            catch (Exception ex)
            {
                Alert.Exception(ex);
            }
        }

        private async void LoginLogout(object sender, RoutedEventArgs e)
        {
            if (vm.IsRegisteredUser)
            {
                // Logout
                await vm.LogoutAsync();
                vm.RefreshAuthState();
            }
            else
            {
                // Login dialog
                try
                {
                    var dialogPage = new MarketplaceLoginDialog(vm);
                    var dialog = new MainDialog(dialogPage, "Marketplace - Account", 420, 350);
                    dialog.ShowDialog();
                    vm.RefreshAuthState();
                }
                catch (Exception ex)
                {
                    Alert.Exception(ex);
                }
            }
        }

        private async void BrowserLogin(object sender, RoutedEventArgs e)
        {
            try
            {
                var (url, error) = await vm.GetLoginLinkAsync();
                if (url != null)
                {
                    Url.Open(url);
                }
                else
                {
                    Alert.Warning("Login Link", error ?? "Failed to generate login link.");
                }
            }
            catch (Exception ex)
            {
                Alert.Exception(ex);
            }
        }

        private void UpdateItem(object sender, RoutedEventArgs e)
        {
            if (itemsListView.SelectedItem is not MarketplaceItem selected)
                return;

            if (!vm.IsOwnItem(selected))
            {
                Alert.Warning("Not your item", "You can only update items you have uploaded.");
                return;
            }

            try
            {
                var dialogPage = new MarketplaceUploadDialog(vm);
                var dialog = new MainDialog(dialogPage, "Marketplace - Update Config", 500, 450);
                dialog.ShowDialog();
            }
            catch (Exception ex)
            {
                Alert.Exception(ex);
            }
        }
    }
}
