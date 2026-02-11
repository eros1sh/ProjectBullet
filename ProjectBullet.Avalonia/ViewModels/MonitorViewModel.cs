using ProjectBullet.Core.Services;
using ProjectBullet.Avalonia.Helpers;
using RuriLib.Models.Jobs;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Avalonia.Media;

namespace ProjectBullet.Avalonia.ViewModels
{
    public class MonitorViewModel : ViewModelBase, IDisposable
    {
        private readonly JobManagerService jobManagerService;
        private readonly Timer refreshTimer;

        public int TotalJobs => jobManagerService.Jobs.Count();
        public int RunningJobs => jobManagerService.Jobs.Count(j => j.Status == JobStatus.Running);
        public int WaitingJobs => jobManagerService.Jobs.Count(j => j.Status == JobStatus.Waiting);
        public int IdleJobs => jobManagerService.Jobs.Count(j => j.Status == JobStatus.Idle);

        public int TotalHits => jobManagerService.Jobs
            .OfType<MultiRunJob>()
            .Sum(j => j.DataHits);

        public int TotalTested => jobManagerService.Jobs
            .OfType<MultiRunJob>()
            .Sum(j => j.DataTested);

        public int TotalCustom => jobManagerService.Jobs
            .OfType<MultiRunJob>()
            .Sum(j => j.DataCustom);

        public int TotalFails => jobManagerService.Jobs
            .OfType<MultiRunJob>()
            .Sum(j => j.DataInvalid);

        public int TotalErrors => jobManagerService.Jobs
            .OfType<MultiRunJob>()
            .Sum(j => j.DataErrors);

        public int TotalCPM => jobManagerService.Jobs
            .OfType<MultiRunJob>()
            .Sum(j => j.CPM);

        public string MemoryUsage => $"{Process.GetCurrentProcess().WorkingSet64 / 1024 / 1024} MB";

        public string HitRate
        {
            get
            {
                var tested = TotalTested;
                if (tested == 0) return "0.0%";
                return $"{(double)TotalHits / tested * 100:F1}%";
            }
        }

        public bool ProgressBarVisibility =>
            jobManagerService.Jobs.Any(j => j.Status == JobStatus.Running);

        public double OverallProgressPercent
        {
            get
            {
                var totalSize = jobManagerService.Jobs.OfType<MultiRunJob>().Sum(j => j.DataPool?.Size ?? 0);
                if (totalSize == 0) return 0;
                return Math.Min((double)TotalTested / totalSize * 100, 100);
            }
        }

        public string OverallProgressText
        {
            get
            {
                var totalSize = jobManagerService.Jobs.OfType<MultiRunJob>().Sum(j => j.DataPool?.Size ?? 0);
                return $"{OverallProgressPercent:F1}% ({TotalTested:N0} / {totalSize:N0})";
            }
        }

        private ObservableCollection<JobMonitorItem> jobItems = new();
        public ObservableCollection<JobMonitorItem> JobItems
        {
            get => jobItems;
            set { jobItems = value; OnPropertyChanged(); }
        }

        public MonitorViewModel()
        {
            jobManagerService = SP.GetService<JobManagerService>();
            RefreshData();

            refreshTimer = new Timer(_ =>
            {
                try
                {
                    global::Avalonia.Threading.Dispatcher.UIThread.Post(RefreshData);
                }
                catch { }
            }, null, 1000, 1000);
        }

        private void RefreshData()
        {
            OnPropertyChanged(nameof(TotalJobs));
            OnPropertyChanged(nameof(RunningJobs));
            OnPropertyChanged(nameof(WaitingJobs));
            OnPropertyChanged(nameof(IdleJobs));
            OnPropertyChanged(nameof(TotalHits));
            OnPropertyChanged(nameof(TotalTested));
            OnPropertyChanged(nameof(TotalCustom));
            OnPropertyChanged(nameof(TotalFails));
            OnPropertyChanged(nameof(TotalErrors));
            OnPropertyChanged(nameof(TotalCPM));
            OnPropertyChanged(nameof(MemoryUsage));
            OnPropertyChanged(nameof(HitRate));
            OnPropertyChanged(nameof(ProgressBarVisibility));
            OnPropertyChanged(nameof(OverallProgressPercent));
            OnPropertyChanged(nameof(OverallProgressText));

            var items = new List<JobMonitorItem>();
            foreach (var job in jobManagerService.Jobs)
            {
                if (job is MultiRunJob mrj)
                {
                    var total = mrj.DataPool?.Size ?? 0;
                    var progress = total > 0 ? (double)mrj.DataTested / total * 100 : 0;
                    if (progress > 100) progress = 100;

                    items.Add(new JobMonitorItem
                    {
                        Id = mrj.Id,
                        Status = mrj.Status.ToString(),
                        ConfigName = mrj.Config?.Metadata?.Name ?? "N/A",
                        Tested = mrj.DataTested,
                        Hits = mrj.DataHits,
                        Custom = mrj.DataCustom,
                        Fails = mrj.DataInvalid,
                        Errors = mrj.DataErrors,
                        CPM = mrj.CPM,
                        Progress = progress,
                        Bots = mrj.Bots,
                        Elapsed = mrj.Elapsed.ToString(@"hh\:mm\:ss")
                    });
                }
                else if (job is ProxyCheckJob pcj)
                {
                    var total = pcj.Total;
                    var tested = pcj.Tested;
                    var progress = total > 0 ? (double)tested / total * 100 : 0;
                    if (progress > 100) progress = 100;

                    items.Add(new JobMonitorItem
                    {
                        Id = pcj.Id,
                        Status = pcj.Status.ToString(),
                        ConfigName = "[Proxy Check]",
                        Tested = tested,
                        Hits = pcj.Working,
                        Custom = 0,
                        Fails = pcj.NotWorking,
                        Errors = 0,
                        CPM = pcj.CPM,
                        Progress = progress,
                        Bots = pcj.Bots,
                        Elapsed = pcj.Elapsed.ToString(@"hh\:mm\:ss")
                    });
                }
            }

            JobItems = new ObservableCollection<JobMonitorItem>(items);
        }

        public void Dispose()
        {
            refreshTimer?.Dispose();
        }
    }

    public class JobMonitorItem
    {
        private static readonly ISolidColorBrush GreenBrush = new SolidColorBrush(Color.FromRgb(0x4C, 0xAF, 0x50));
        private static readonly ISolidColorBrush OrangeBrush = new SolidColorBrush(Color.FromRgb(0xFF, 0x98, 0x00));
        private static readonly ISolidColorBrush PurpleBrush = new SolidColorBrush(Color.FromRgb(0x9C, 0x27, 0xB0));
        private static readonly ISolidColorBrush RedBrush = new SolidColorBrush(Color.FromRgb(0xF4, 0x43, 0x36));
        private static readonly ISolidColorBrush GrayBrush = new SolidColorBrush(Color.FromRgb(0x9E, 0x9E, 0x9E));

        public int Id { get; set; }
        public string Status { get; set; }
        public string ConfigName { get; set; }
        public int Tested { get; set; }
        public int Hits { get; set; }
        public int Custom { get; set; }
        public int Fails { get; set; }
        public int Errors { get; set; }
        public int CPM { get; set; }
        public double Progress { get; set; }
        public int Bots { get; set; }
        public string Elapsed { get; set; }
        public string ProgressString => $"{Progress:F1}%";
        public double ProgressBarWidth => Progress / 100.0 * 90.0;

        public ISolidColorBrush StatusColor => Status switch
        {
            "Running" => GreenBrush,
            "Waiting" => OrangeBrush,
            "Pausing" or "Paused" => PurpleBrush,
            "Stopping" => RedBrush,
            _ => GrayBrush
        };
    }
}
