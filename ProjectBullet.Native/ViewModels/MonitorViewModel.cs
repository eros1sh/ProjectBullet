using ProjectBullet.Core.Services;
using ProjectBullet.Native.Helpers;
using RuriLib.Models.Jobs;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Windows;

namespace ProjectBullet.Native.ViewModels
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
                    Application.Current?.Dispatcher?.Invoke(RefreshData);
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
    }
}
