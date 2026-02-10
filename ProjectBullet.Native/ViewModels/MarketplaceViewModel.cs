using ProjectBullet.Core.Models.Marketplace;
using ProjectBullet.Core.Services;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace ProjectBullet.Native.ViewModels
{
    public class MarketplaceViewModel : ViewModelBase
    {
        private readonly MarketplaceApiService _api;

        public ObservableCollection<MarketplaceItem> Items { get; set; } = new();

        private string searchString = string.Empty;
        public string SearchString
        {
            get => searchString;
            set
            {
                searchString = value;
                OnPropertyChanged();
            }
        }

        private string selectedCategory = "all";
        public string SelectedCategory
        {
            get => selectedCategory;
            set
            {
                selectedCategory = value;
                OnPropertyChanged();
            }
        }

        private string selectedSort = "newest";
        public string SelectedSort
        {
            get => selectedSort;
            set
            {
                selectedSort = value;
                OnPropertyChanged();
            }
        }

        private int currentPage = 1;
        public int CurrentPage
        {
            get => currentPage;
            set
            {
                currentPage = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(PageInfo));
            }
        }

        private int totalPages = 1;
        public int TotalPages
        {
            get => totalPages;
            set
            {
                totalPages = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(PageInfo));
            }
        }

        public string PageInfo => $"Page {CurrentPage} / {TotalPages}";

        private bool isLoading;
        public bool IsLoading
        {
            get => isLoading;
            set
            {
                isLoading = value;
                OnPropertyChanged();
            }
        }

        private string statusMessage = "Ready.";
        public string StatusMessage
        {
            get => statusMessage;
            set
            {
                statusMessage = value;
                OnPropertyChanged();
            }
        }

        public string Username => _api?.Username ?? string.Empty;
        public bool IsAuthenticated => _api?.IsAuthenticated ?? false;
        public bool IsRegisteredUser => _api?.IsRegisteredUser ?? false;
        public string LoginButtonText => IsRegisteredUser ? "Logout" : "Login";

        public MarketplaceViewModel()
        {
            _api = SP.GetService<MarketplaceApiService>();
        }

        public async Task<(string Url, string Error)> GetLoginLinkAsync()
            => await _api.GetLoginLinkAsync();

        public bool IsOwnItem(MarketplaceItem item)
        {
            if (item == null || !IsRegisteredUser) return false;
            return item.UserId == (_api.CurrentUser?.Id ?? 0);
        }

        public async Task LoadItemsAsync()
        {
            IsLoading = true;
            StatusMessage = "Loading items...";

            try
            {
                var response = await _api.GetItemsAsync(
                    SelectedCategory, SearchString, SelectedSort, CurrentPage);

                Items.Clear();
                foreach (var item in response.Items)
                {
                    Items.Add(item);
                }

                TotalPages = Math.Max(1, response.TotalPages);
                CurrentPage = Math.Max(1, response.Page);
                StatusMessage = $"Found {response.TotalItems} items.";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }

        public async Task SearchAsync()
        {
            CurrentPage = 1;
            await LoadItemsAsync();
        }

        public async Task NextPage()
        {
            if (CurrentPage < TotalPages)
            {
                CurrentPage++;
                await LoadItemsAsync();
            }
        }

        public async Task PrevPage()
        {
            if (CurrentPage > 1)
            {
                CurrentPage--;
                await LoadItemsAsync();
            }
        }

        public async Task<bool> DownloadItemAsync(MarketplaceItem item, string password = null)
        {
            StatusMessage = $"Downloading {item.Name}...";

            try
            {
                var (data, fileName, error) = await _api.DownloadItemAsync(item.Id, password);

                if (data == null)
                {
                    StatusMessage = $"Download failed: {error}";
                    return false;
                }

                // Save to UserData/Configs
                var configDir = Path.Combine(Directory.GetCurrentDirectory(), "UserData", "Configs");
                Directory.CreateDirectory(configDir);
                var filePath = Path.Combine(configDir, fileName);
                await File.WriteAllBytesAsync(filePath, data);

                StatusMessage = $"Downloaded {fileName} to Configs!";
                return true;
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error: {ex.Message}";
                return false;
            }
        }

        public async Task SyncMyConfigsAsync()
        {
            if (!IsRegisteredUser) return;

            StatusMessage = "Syncing your shared configs...";

            try
            {
                var myItems = await _api.GetMyItemsAsync();
                var configDir = Path.Combine(Directory.GetCurrentDirectory(), "UserData", "Configs");
                Directory.CreateDirectory(configDir);
                var synced = 0;

                foreach (var item in myItems.Items)
                {
                    var (data, fileName, error) = await _api.DownloadItemAsync(item.Id);
                    if (data != null)
                    {
                        var filePath = Path.Combine(configDir, fileName);
                        await File.WriteAllBytesAsync(filePath, data);
                        synced++;
                    }
                }

                StatusMessage = synced > 0
                    ? $"Synced {synced} config(s) to Configs folder."
                    : "No configs to sync.";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Sync error: {ex.Message}";
            }
        }

        public async Task<(bool Success, string Error)> LoginAsync(string username, string password)
        {
            var result = await _api.LoginAsync(username, password);
            if (result.Success)
            {
                RefreshAuthState();
                await SyncMyConfigsAsync();
            }
            return result;
        }

        public async Task<(bool Success, string Error)> RegisterAsync(string username, string password)
        {
            var result = await _api.RegisterAsync(username, password);
            if (result.Success)
            {
                RefreshAuthState();
            }
            return result;
        }

        public async Task LogoutAsync()
        {
            await _api.Logout();
            RefreshAuthState();
        }

        public async Task<(bool Success, string Error)> UploadItemAsync(
            string name, string category, string description,
            string version, string password, string filePath)
        {
            StatusMessage = $"Uploading {name}...";
            var result = await _api.UploadItemAsync(name, category, description, version, password, filePath);
            StatusMessage = result.Success ? $"Uploaded {name} successfully!" : $"Upload failed: {result.Error}";
            return result;
        }

        public void RefreshAuthState()
        {
            OnPropertyChanged(nameof(Username));
            OnPropertyChanged(nameof(IsAuthenticated));
            OnPropertyChanged(nameof(IsRegisteredUser));
            OnPropertyChanged(nameof(LoginButtonText));
        }
    }
}
