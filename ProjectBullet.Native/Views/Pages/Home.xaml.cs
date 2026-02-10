using ProjectBullet.Core.Repositories;
using ProjectBullet.Core.Services;
using ProjectBullet.Native.Helpers;
using ProjectBullet.Native.Services;
using ProjectBullet.Native.ViewModels;
using ProjectBullet.Native.Views.Dialogs;
using RuriLib.Models.Jobs;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Controls;

namespace ProjectBullet.Native.Views.Pages
{
    /// <summary>
    /// Interaction logic for Home.xaml
    /// </summary>
    public partial class Home : Page
    {
        private readonly HomeViewModel vm;

        public Home()
        {
            InitializeComponent();

            vm = new HomeViewModel();
            DataContext = vm;
        }

        private void ShowChangelog(object sender, RoutedEventArgs e)
            => new MainDialog(new ShowChangelogDialog(), "Changelog", true).ShowDialog();

        private void ShowUpdateConfirmation(object sender, RoutedEventArgs e)
            => new MainDialog(new UpdateConfirmationDialog(vm.CurrentVersion, vm.RemoteVersion), "Update confirmation").ShowDialog();

        private void NewJob(object sender, RoutedEventArgs e)
            => SP.GetService<MainWindow>().NavigateTo(MainWindowPage.Jobs);

        private void ImportConfig(object sender, RoutedEventArgs e)
            => SP.GetService<MainWindow>().NavigateTo(MainWindowPage.Configs);

        private void CheckProxies(object sender, RoutedEventArgs e)
            => SP.GetService<MainWindow>().NavigateTo(MainWindowPage.Proxies);
    }

    public class HomeViewModel : ViewModelBase
    {
        private readonly AnnouncementService annService;
        private readonly UpdateService updateService;
        private readonly JobManagerService jobManager;
        private readonly ConfigService configService;
        private readonly IHitRepository hitRepo;
        private readonly IProxyRepository proxyRepo;
        private readonly Timer refreshTimer;

        public bool UpdateAvailable => updateService.IsUpdateAvailable;
        public Version CurrentVersion => updateService.CurrentVersion;
        public Version RemoteVersion => updateService.RemoteVersion;

        private string announcement = "Loading announcement...";
        public string Announcement
        {
            get => announcement;
            set
            {
                announcement = value;
                OnPropertyChanged();
            }
        }

        private long totalHits;
        public long TotalHits
        {
            get => totalHits;
            set
            {
                totalHits = value;
                OnPropertyChanged();
            }
        }

        public int ActiveJobs => jobManager.Jobs.Count(j => j.Status == JobStatus.Running);
        public int TotalConfigs => configService.Configs.Count;

        private int totalProxies;
        public int TotalProxies
        {
            get => totalProxies;
            set
            {
                totalProxies = value;
                OnPropertyChanged();
            }
        }

        private ObservableCollection<ActiveJobInfo> activeJobsList = new();
        public ObservableCollection<ActiveJobInfo> ActiveJobsList
        {
            get => activeJobsList;
            set
            {
                activeJobsList = value;
                OnPropertyChanged();
            }
        }

        public HomeViewModel()
        {
            annService = SP.GetService<AnnouncementService>();
            updateService = SP.GetService<UpdateService>();
            jobManager = SP.GetService<JobManagerService>();
            configService = SP.GetService<ConfigService>();
            hitRepo = SP.GetService<IHitRepository>();
            proxyRepo = SP.GetService<IProxyRepository>();

            updateService.UpdateAvailable += NotifyUpdateAvailable;

            FetchAnnouncement();
            FetchAsyncCounts();

            refreshTimer = new Timer(_ => RefreshStats(), null, 1000, 1000);
        }

        private async void FetchAnnouncement() => Announcement = await annService.FetchAnnouncementAsync();

        private async void FetchAsyncCounts()
        {
            try
            {
                TotalHits = await hitRepo.CountAsync();
                TotalProxies = proxyRepo.GetAll().Count();
            }
            catch
            {
                // Ignore errors during initial count fetch
            }
        }

        private void RefreshStats()
        {
            try
            {
                OnPropertyChanged(nameof(ActiveJobs));
                OnPropertyChanged(nameof(TotalConfigs));

                var jobs = jobManager.Jobs
                    .Where(j => j is MultiRunJob)
                    .Cast<MultiRunJob>()
                    .Where(j => j.Status != JobStatus.Idle)
                    .Select(j => new ActiveJobInfo
                    {
                        ConfigName = j.Config?.Metadata?.Name ?? "N/A",
                        Status = j.Status.ToString(),
                        Progress = $"{Math.Clamp(j.Progress * 100, 0, 100):F1}%",
                        CPM = j.CPM,
                        Hits = j.DataHits
                    })
                    .ToList();

                Application.Current?.Dispatcher?.Invoke(() =>
                {
                    ActiveJobsList = new ObservableCollection<ActiveJobInfo>(jobs);
                });
            }
            catch
            {
                // Ignore errors during periodic refresh
            }
        }

        private void NotifyUpdateAvailable()
        {
            OnPropertyChanged(nameof(UpdateAvailable));
            OnPropertyChanged(nameof(CurrentVersion));
            OnPropertyChanged(nameof(RemoteVersion));
        }
    }

    public class ActiveJobInfo
    {
        public string ConfigName { get; set; }
        public string Status { get; set; }
        public string Progress { get; set; }
        public int CPM { get; set; }
        public int Hits { get; set; }
    }
}
