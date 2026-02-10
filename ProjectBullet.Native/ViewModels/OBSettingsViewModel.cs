using ProjectBullet.Core.Models.Settings;
using ProjectBullet.Core.Services;
using ProjectBullet.Native.Helpers;
using ProjectBullet.Telegram.Services;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace ProjectBullet.Native.ViewModels
{
    public class OBSettingsViewModel : ViewModelBase
    {
        private readonly ProjectBulletSettingsService service;
        private GeneralSettings General => service.Settings.GeneralSettings;
        private RemoteSettings Remote => service.Settings.RemoteSettings;
        private CustomizationSettings Customization => service.Settings.CustomizationSettings;
        private TelegramSettings Telegram => service.Settings.TelegramSettings;
        private AppLockSettings AppLock => service.Settings.AppLockSettings;
        private MarketplaceSettings Marketplace => service.Settings.MarketplaceSettings;

        public OBSettingsViewModel()
        {
            service = SP.GetService<ProjectBulletSettingsService>();
            CreateCollections();
        }

        public ConfigSection ConfigSectionOnLoad
        {
            get => General.ConfigSectionOnLoad;
            set
            {
                General.ConfigSectionOnLoad = value;
                OnPropertyChanged();
            }
        }

        public bool AutoSetRecommendedBots
        {
            get => General.AutoSetRecommendedBots;
            set
            {
                General.AutoSetRecommendedBots = value;
                OnPropertyChanged();
            }
        }

        public bool WarnConfigNotSaved
        {
            get => General.WarnConfigNotSaved;
            set
            {
                General.WarnConfigNotSaved = value;
                OnPropertyChanged();
            }
        }

        public string DefaultAuthor
        {
            get => General.DefaultAuthor;
            set
            {
                General.DefaultAuthor = value;
                OnPropertyChanged();
            }
        }

        public bool EnableJobLogging
        {
            get => General.EnableJobLogging;
            set
            {
                General.EnableJobLogging = value;
                OnPropertyChanged();
            }
        }

        public int LogBufferSize
        {
            get => General.LogBufferSize;
            set
            {
                General.LogBufferSize = value;
                OnPropertyChanged();
            }
        }

        public JobDisplayMode DefaultJobDisplayMode
        {
            get => General.DefaultJobDisplayMode;
            set
            {
                General.DefaultJobDisplayMode = value;
                OnPropertyChanged();
            }
        }

        public bool GroupCapturesInDebugger
        {
            get => General.GroupCapturesInDebugger;
            set
            {
                General.GroupCapturesInDebugger = value;
                OnPropertyChanged();
            }
        }

        public bool IgnoreWordlistNameOnHitsDedupe
        {
            get => General.IgnoreWordlistNameOnHitsDedupe;
            set
            {
                General.IgnoreWordlistNameOnHitsDedupe = value;
                OnPropertyChanged();
            }
        }

        private ObservableCollection<ProxyCheckTarget> proxyCheckTargetsCollection;
        public ObservableCollection<ProxyCheckTarget> ProxyCheckTargetsCollection
        {
            get => proxyCheckTargetsCollection;
            set
            {
                proxyCheckTargetsCollection = value;
                OnPropertyChanged();
            }
        }

        private ObservableCollection<CustomSnippet> customSnippetsCollection;
        public ObservableCollection<CustomSnippet> CustomSnippetsCollection
        {
            get => customSnippetsCollection;
            set
            {
                customSnippetsCollection = value;
                OnPropertyChanged();
            }
        }

        private ObservableCollection<RemoteConfigsEndpoint> remoteConfigsEndointsCollection;
        public ObservableCollection<RemoteConfigsEndpoint> RemoteConfigsEndpointsCollection
        {
            get => remoteConfigsEndointsCollection;
            set
            {
                remoteConfigsEndointsCollection = value;
                OnPropertyChanged();
            }
        }

        public bool PlaySoundOnHit
        {
            get => Customization.PlaySoundOnHit;
            set
            {
                Customization.PlaySoundOnHit = value;
                OnPropertyChanged();
            }
        }

        public bool WordWrap
        {
            get => Customization.WordWrap;
            set
            {
                Customization.WordWrap = value;
                OnPropertyChanged();
            }
        }

        public string BackgroundMain
        {
            get => Customization.BackgroundMain;
            set
            {
                Customization.BackgroundMain = value;
                
                // Call this instead of SetAppColor because otherwise it will not
                // update the background if we previously set an image
                RefreshTheme();

                OnPropertyChanged();
            }
        }

        public string BackgroundSecondary
        {
            get => Customization.BackgroundSecondary;
            set
            {
                Customization.BackgroundSecondary = value;
                Brush.SetAppColor("BackgroundSecondary", value);
                OnPropertyChanged();
            }
        }

        public string BackgroundInput
        {
            get => Customization.BackgroundInput;
            set
            {
                Customization.BackgroundInput = value;
                Brush.SetAppColor("BackgroundInput", value);
                OnPropertyChanged();
            }
        }

        public string ForegroundMain
        {
            get => Customization.ForegroundMain;
            set
            {
                Customization.ForegroundMain = value;
                Brush.SetAppColor("ForegroundMain", value);
                OnPropertyChanged();
            }
        }

        public string ForegroundInput
        {
            get => Customization.ForegroundInput;
            set
            {
                Customization.ForegroundInput = value;
                Brush.SetAppColor("ForegroundInput", value);
                OnPropertyChanged();
            }
        }

        public string ForegroundGood
        {
            get => Customization.ForegroundGood;
            set
            {
                Customization.ForegroundGood = value;
                Brush.SetAppColor("ForegroundGood", value);
                OnPropertyChanged();
            }
        }

        public string ForegroundBad
        {
            get => Customization.ForegroundBad;
            set
            {
                Customization.ForegroundBad = value;
                Brush.SetAppColor("ForegroundBad", value);
                OnPropertyChanged();
            }
        }

        public string ForegroundCustom
        {
            get => Customization.ForegroundCustom;
            set
            {
                Customization.ForegroundCustom = value;
                Brush.SetAppColor("ForegroundCustom", value);
                OnPropertyChanged();
            }
        }

        public string ForegroundRetry
        {
            get => Customization.ForegroundRetry;
            set
            {
                Customization.ForegroundRetry = value;
                Brush.SetAppColor("ForegroundRetry", value);
                OnPropertyChanged();
            }
        }

        public string ForegroundBanned
        {
            get => Customization.ForegroundBanned;
            set
            {
                Customization.ForegroundBanned = value;
                Brush.SetAppColor("ForegroundBanned", value);
                OnPropertyChanged();
            }
        }

        public string ForegroundToCheck
        {
            get => Customization.ForegroundToCheck;
            set
            {
                Customization.ForegroundToCheck = value;
                Brush.SetAppColor("ForegroundToCheck", value);
                OnPropertyChanged();
            }
        }

        public string ForegroundMenuSelected
        {
            get => Customization.ForegroundMenuSelected;
            set
            {
                Customization.ForegroundMenuSelected = value;
                Brush.SetAppColor("ForegroundMenuSelected", value);
                OnPropertyChanged();
            }
        }

        public string SuccessButton
        {
            get => Customization.SuccessButton;
            set
            {
                Customization.SuccessButton = value;
                Brush.SetAppColor("SuccessButton", value);
                OnPropertyChanged();
            }
        }

        public string PrimaryButton
        {
            get => Customization.PrimaryButton;
            set
            {
                Customization.PrimaryButton = value;
                Brush.SetAppColor("PrimaryButton", value);
                OnPropertyChanged();
            }
        }

        public string WarningButton
        {
            get => Customization.WarningButton;
            set
            {
                Customization.WarningButton = value;
                Brush.SetAppColor("WarningButton", value);
                OnPropertyChanged();
            }
        }

        public string DangerButton
        {
            get => Customization.DangerButton;
            set
            {
                Customization.DangerButton = value;
                Brush.SetAppColor("DangerButton", value);
                OnPropertyChanged();
            }
        }

        public string ForegroundButton
        {
            get => Customization.ForegroundButton;
            set
            {
                Customization.ForegroundButton = value;
                Brush.SetAppColor("ForegroundButton", value);
                OnPropertyChanged();
            }
        }

        public string BackgroundButton
        {
            get => Customization.BackgroundButton;
            set
            {
                Customization.BackgroundButton = value;
                Brush.SetAppColor("BackgroundButton", value);
                OnPropertyChanged();
            }
        }

        public string BackgroundImagePath
        {
            get => Customization.BackgroundImagePath;
            private set
            {
                Customization.BackgroundImagePath = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(ShowBackgroundImage));

                BackgroundImage = new(new Uri(value));
            }
        }

        public double BackgroundOpacity
        {
            get => Customization.BackgroundOpacity;
            set
            {
                Customization.BackgroundOpacity = value;
                OnPropertyChanged();
                RefreshTheme();
            }
        }

        private BitmapImage backgroundImage;
        public BitmapImage BackgroundImage
        {
            get => backgroundImage;
            set
            {
                backgroundImage = value;
                OnPropertyChanged();
                RefreshTheme();
            }
        }

        public bool ShowBackgroundImage => !string.IsNullOrEmpty(BackgroundImagePath);

        public void SetBackgroundImage(string path) => BackgroundImagePath = path;

        public async Task Save()
        {
            General.ProxyCheckTargets = ProxyCheckTargetsCollection.ToList();
            General.CustomSnippets = CustomSnippetsCollection.ToList();
            Remote.ConfigsEndpoints = RemoteConfigsEndpointsCollection.ToList();
            await service.SaveAsync();

            // Restart Telegram bot and webhook polling with new settings
            SP.GetService<TelegramBotService>().Restart();
            SP.GetService<WebhookPollingService>().Restart();
        }

        public void Reset()
        {
            service.Recreate();
            CreateCollections();
            UpdateViewModel();
            RefreshTheme();
        }

        public void ResetCustomization()
        {
            service.Settings.CustomizationSettings = new CustomizationSettings();
            UpdateViewModel();
            RefreshTheme();
        }

        private void RefreshTheme() => SP.GetService<MainWindow>().SetTheme(Customization);

        public void AddProxyCheckTarget() => ProxyCheckTargetsCollection.Add(new ProxyCheckTarget());
        public void RemoveProxyCheckTarget(ProxyCheckTarget target) => ProxyCheckTargetsCollection.Remove(target);

        public void AddCustomSnippet() => CustomSnippetsCollection.Add(new CustomSnippet());
        public void RemoveCustomSnippet(CustomSnippet snippet) => CustomSnippetsCollection.Remove(snippet);

        public void AddRemoteConfigsEndpoint() => RemoteConfigsEndpointsCollection.Add(new RemoteConfigsEndpoint());
        public void RemoveRemoteConfigsEndpoint(RemoteConfigsEndpoint endpoint) => RemoteConfigsEndpointsCollection.Remove(endpoint);

        public bool TelegramEnabled
        {
            get => Telegram.Enabled;
            set
            {
                Telegram.Enabled = value;
                OnPropertyChanged();
            }
        }

        public string TelegramBotToken
        {
            get => Telegram.BotToken;
            set
            {
                Telegram.BotToken = value;
                OnPropertyChanged();
            }
        }

        public long TelegramChatId
        {
            get => Telegram.ChatId;
            set
            {
                Telegram.ChatId = value;
                OnPropertyChanged();
            }
        }

        public bool TelegramSendHitNotifications
        {
            get => Telegram.SendHitNotifications;
            set
            {
                Telegram.SendHitNotifications = value;
                OnPropertyChanged();
            }
        }

        public string TelegramWhitelistedUserIds
        {
            get => string.Join(", ", Telegram.WhitelistedUserIds);
            set
            {
                Telegram.WhitelistedUserIds = value
                    .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                    .Where(s => long.TryParse(s, out _))
                    .Select(long.Parse)
                    .ToList();
                OnPropertyChanged();
            }
        }

        public bool TelegramEnableJobStartStopNotifications
        {
            get => Telegram.EnableJobStartStopNotifications;
            set
            {
                Telegram.EnableJobStartStopNotifications = value;
                OnPropertyChanged();
            }
        }

        public bool TelegramEnhancedHitFormat
        {
            get => Telegram.EnhancedHitFormat;
            set
            {
                Telegram.EnhancedHitFormat = value;
                OnPropertyChanged();
            }
        }

        public bool TelegramEnableDailySummary
        {
            get => Telegram.EnableDailySummary;
            set
            {
                Telegram.EnableDailySummary = value;
                OnPropertyChanged();
            }
        }

        public string TelegramDailySummaryTime
        {
            get => Telegram.DailySummaryTime;
            set
            {
                Telegram.DailySummaryTime = value;
                OnPropertyChanged();
            }
        }

        // App Lock settings
        public bool AppLockEnabled
        {
            get => AppLock.Enabled;
            set
            {
                AppLock.Enabled = value;
                OnPropertyChanged();
            }
        }

        public int AppLockAutoLockMinutes
        {
            get => AppLock.AutoLockMinutes;
            set
            {
                AppLock.AutoLockMinutes = value;
                OnPropertyChanged();
            }
        }

        public void SetAppLockPasswordHash(string hash)
        {
            AppLock.PasswordHash = hash;
        }

        // Webhook relay settings
        public bool TelegramUseWebhookRelay
        {
            get => Telegram.UseWebhookRelay;
            set
            {
                Telegram.UseWebhookRelay = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(WebhookRelayActive));
            }
        }

        public bool WebhookRelayActive => Telegram.UseWebhookRelay && !string.IsNullOrEmpty(Telegram.WebhookUrl);

        public string TelegramWebhookUrl => Telegram.WebhookUrl;

        private string webhookStatus = string.Empty;
        public string WebhookStatus
        {
            get => webhookStatus;
            set
            {
                webhookStatus = value;
                OnPropertyChanged();
            }
        }

        public async Task SetupWebhookAsync()
        {
            if (string.IsNullOrEmpty(Telegram.BotToken))
            {
                WebhookStatus = "Bot token is required.";
                return;
            }

            WebhookStatus = "Setting up webhook...";
            var api = SP.GetService<MarketplaceApiService>();
            var (result, error) = await api.SetupWebhookAsync(Telegram.BotToken);

            if (result != null)
            {
                Telegram.WebhookUrl = result.WebhookUrl;
                Telegram.WebhookSecret = result.Secret;
                Telegram.UseWebhookRelay = true;
                await service.SaveAsync();

                OnPropertyChanged(nameof(TelegramWebhookUrl));
                OnPropertyChanged(nameof(TelegramUseWebhookRelay));
                OnPropertyChanged(nameof(WebhookRelayActive));

                // Restart services with new mode
                SP.GetService<TelegramBotService>().Restart();
                SP.GetService<WebhookPollingService>().Restart();

                WebhookStatus = "Webhook relay active.";
            }
            else
            {
                WebhookStatus = $"Setup failed: {error}";
            }
        }

        public async Task DeleteWebhookAsync()
        {
            var api = SP.GetService<MarketplaceApiService>();
            var (success, error) = await api.DeleteWebhookAsync();

            Telegram.WebhookUrl = string.Empty;
            Telegram.WebhookSecret = string.Empty;
            Telegram.UseWebhookRelay = false;
            await service.SaveAsync();

            OnPropertyChanged(nameof(TelegramWebhookUrl));
            OnPropertyChanged(nameof(TelegramUseWebhookRelay));
            OnPropertyChanged(nameof(WebhookRelayActive));

            // Stop webhook polling and restart with long polling
            SP.GetService<WebhookPollingService>().Stop();
            SP.GetService<TelegramBotService>().Restart();

            WebhookStatus = success ? "Webhook relay disabled." : $"Warning: {error}";
        }

        // Marketplace settings
        public string MarketplaceUsername => string.IsNullOrEmpty(Marketplace.CachedUsername)
            ? "(not logged in)"
            : Marketplace.CachedUsername;

        public bool IsMarketplaceLoggedIn => !string.IsNullOrEmpty(Marketplace.CachedUsername) && Marketplace.IsRegisteredUser;

        private void CreateCollections()
        {
            ProxyCheckTargetsCollection = new ObservableCollection<ProxyCheckTarget>(General.ProxyCheckTargets);
            CustomSnippetsCollection = new ObservableCollection<CustomSnippet>(General.CustomSnippets);
            RemoteConfigsEndpointsCollection = new ObservableCollection<RemoteConfigsEndpoint>(Remote.ConfigsEndpoints);
        }
    }
}
