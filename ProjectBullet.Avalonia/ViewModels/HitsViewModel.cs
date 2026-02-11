using Microsoft.EntityFrameworkCore;
using ProjectBullet.Core.Entities;
using ProjectBullet.Core.Repositories;
using ProjectBullet.Core.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace ProjectBullet.Avalonia.ViewModels
{
    public class HitsViewModel : ViewModelBase
    {
        private readonly ProjectBulletSettingsService obSettingsService;
        private readonly IHitRepository hitRepo;
        private bool initialized;
        private List<HitEntity> allHits = new();

        private ObservableCollection<HitEntity> hitsCollection;
        public ObservableCollection<HitEntity> HitsCollection
        {
            get => hitsCollection;
            private set
            {
                hitsCollection = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(Total));
            }
        }

        public int Total => HitsCollection?.Count ?? 0;

        private string searchString = string.Empty;
        public string SearchString
        {
            get => searchString;
            set
            {
                searchString = value;
                OnPropertyChanged();
                ApplyFilter();
            }
        }

        public IEnumerable<string> ConfigNames => new string[] { "All" }.Concat(
            allHits.GroupBy(h => h.ConfigName).Select(g => g.First().ConfigName));

        private string configFilter = "All";
        public string ConfigFilter
        {
            get => configFilter;
            set
            {
                configFilter = value;
                OnPropertyChanged();
                ApplyFilter();
            }
        }

        public IEnumerable<string> HitTypes => new string[] { "All" }.Concat(
            allHits.GroupBy(h => h.Type).Select(g => g.First().Type));

        private string typeFilter = "All";
        public string TypeFilter
        {
            get => typeFilter;
            set
            {
                typeFilter = value;
                OnPropertyChanged();
                ApplyFilter();
            }
        }

        public HitsViewModel()
        {
            obSettingsService = SP.GetService<ProjectBulletSettingsService>();
            hitRepo = SP.GetService<IHitRepository>();
            HitsCollection = new ObservableCollection<HitEntity>();
        }

        public async Task InitializeAsync()
        {
            if (!initialized)
            {
                await RefreshListAsync();
                initialized = true;
            }
        }

        public bool HitsFilter(HitEntity hit)
        {
            var captureOk = string.IsNullOrEmpty(searchString) || hit.CapturedData.Contains(searchString, StringComparison.OrdinalIgnoreCase);
            var configOk = configFilter == "All" || hit.ConfigName == configFilter;
            var typeOk = typeFilter == "All" || hit.Type == typeFilter;

            return captureOk && configOk && typeOk;
        }

        public async Task RefreshListAsync()
        {
            try
            {
                // TODO: Make this not fail when hits are being written and we try to read them!
                // A.k.a. make this use another repo, not the singleton, and refresh it when new hits come in
                var items = await hitRepo.GetAll().ToListAsync();
                allHits = items;
                ApplyFilter();
                OnPropertyChanged(nameof(ConfigNames));
                OnPropertyChanged(nameof(HitTypes));
            }
            catch
            {

            }
        }

        private void ApplyFilter()
        {
            var filtered = allHits.Where(h => HitsFilter(h));
            HitsCollection = new ObservableCollection<HitEntity>(filtered);
        }

        public Task Update(HitEntity hit) => hitRepo.UpdateAsync(hit);

        public async Task DeleteAsync(IEnumerable<HitEntity> hits)
        {
            await hitRepo.DeleteAsync(hits);
            await RefreshListAsync();
            OnPropertyChanged(nameof(Total));
        }

        public async Task PurgeAsync()
        {
            allHits.Clear();
            HitsCollection.Clear();
            await hitRepo.PurgeAsync();
            OnPropertyChanged(nameof(Total));
        }

        public async Task<int> DeleteDuplicatesAsync()
        {
            var duplicates = HitsCollection
                .GroupBy(h => h.GetHashCode(obSettingsService.Settings.GeneralSettings.IgnoreWordlistNameOnHitsDedupe))
                .Where(g => g.Count() > 1)
                .SelectMany(g => g.OrderBy(h => h.Date)
                .Reverse().Skip(1)).ToList();

            await hitRepo.DeleteAsync(duplicates);
            await RefreshListAsync();

            return duplicates.Count;
        }

        public override void UpdateViewModel()
        {
            _ = RefreshListAsync();
            base.UpdateViewModel();
        }
    }
}
