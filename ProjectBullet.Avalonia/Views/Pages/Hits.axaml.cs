using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using Newtonsoft.Json;
using ProjectBullet.Core.Entities;
using ProjectBullet.Core.Models.Data;
using ProjectBullet.Core.Models.Hits;
using ProjectBullet.Core.Models.Jobs;
using ProjectBullet.Core.Services;
using ProjectBullet.Avalonia.Extensions;
using ProjectBullet.Avalonia.Helpers;
using ProjectBullet.Avalonia.Services;
using ProjectBullet.Avalonia.ViewModels;
using Avalonia.Threading;
using RuriLib.Extensions;
using RuriLib.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProjectBullet.Avalonia.Views.Pages
{
    /// <summary>
    /// Interaction logic for Hits.axaml
    /// </summary>
    public partial class Hits : UserControl
    {
        private readonly HitsViewModel vm;
        private readonly ConfigService configService;
        private readonly MainWindow window;
        private readonly RuriLibSettingsService rlSettingsService;

        private IEnumerable<HitEntity> SelectedHits => hitsListView.SelectedItems.Cast<HitEntity>().ToList();

        private readonly Func<HitEntity, string> captureMapping = new (hit => $"{hit.Data} | {hit.CapturedData}");
        private readonly Func<HitEntity, string> fullMapping = new(hit =>
            "Data = " + hit.Data +
            " | Type = " + hit.Type +
            " | Config = " + hit.ConfigName +
            " | Wordlist = " + hit.WordlistName +
            " | Proxy = " + hit.Proxy +
            " | Date = " + hit.Date.ToLongDateString() +
            " | CapturedData = " + hit.CapturedData);

        public Hits()
        {
            vm = SP.GetService<ViewModelsService>().Hits;
            DataContext = vm;
            _ = vm.InitializeAsync();

            InitializeComponent();
            window = SP.GetService<MainWindow>();
            configService = SP.GetService<ConfigService>();
            rlSettingsService = SP.GetService<RuriLibSettingsService>();
            var env = SP.GetService<RuriLibSettingsService>().Environment;

            // HACK: Hardcoded stuff
            var menu = hitsListView.ContextMenu;
            var copyMenu = (MenuItem)menu.Items[0];
            var saveMenu = (MenuItem)menu.Items[1];

            foreach (var f in env.ExportFormats)
            {
                var copyItem = new MenuItem();
                copyItem.Header = f.Format;
                copyItem.Click += new EventHandler<RoutedEventArgs>(CopySelectedCustom);
                ((MenuItem)copyMenu.Items[4]).Items.Add(copyItem);

                var saveItem = new MenuItem();
                saveItem.Header = f.Format;
                saveItem.Click += new EventHandler<RoutedEventArgs>(SaveSelectedCustom);
                ((MenuItem)saveMenu.Items[3]).Items.Add(saveItem);
            }
        }

        public void UpdateViewModel() => vm.UpdateViewModel();

        private async void DeleteSelected(object sender, RoutedEventArgs e)
        {
            try
            {
                await vm.DeleteAsync(SelectedHits);
                Alert.Success("Done", "Successfully deleted the selected hits from the DB");
            }
            catch (Exception ex)
            {
                Alert.Exception(ex);
            }
        }

        private async void Purge(object sender, RoutedEventArgs e)
        {
            if (await Alert.ChoiceAsync("Are you REALLY sure?", "This will delete ALL your hits, not just the ones you filtered. Are you sure you want to do this?"))
            {
                try
                {
                    await vm.PurgeAsync();
                    Alert.Success("Done", "Successfully deleted all hits from the DB");
                }
                catch (Exception ex)
                {
                    Alert.Exception(ex);
                }
            }
        }

        private void UpdateSearch(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                vm.SearchString = filterTextbox.Text;
            }
        }

        private void Search(object sender, RoutedEventArgs e) => vm.SearchString = filterTextbox.Text;

        private async void DeleteDuplicates(object sender, RoutedEventArgs e)
        {
            var deleted = await vm.DeleteDuplicatesAsync();
            Alert.Success("Done", $"Successfully deleted {deleted} duplicate hits");
        }

        private void SelectAll(object sender, RoutedEventArgs e) => hitsListView.SelectAll();

        private async void SendToRecheck(object sender, RoutedEventArgs e)
        {
            if (!SelectedHits.Any())
            {
                return;
            }

            var firstHit = SelectedHits.First();

            var jobOptions = (MultiRunJobOptions)JobOptionsFactory.CreateNew(JobType.MultiRun);
            var wordlistType = rlSettingsService.Environment.RecognizeWordlistType(firstHit.Data);

            // Get the config
            var config = configService.Configs.FirstOrDefault(c => c.Metadata.Name == firstHit.ConfigName);

            // If we cannot find a config with that id anymore, don't set it
            if (config == null)
            {
                Alert.Warning("Config not found", $"Could not find the config these hits refer to ({firstHit.ConfigName})");
            }
            else
            {
                jobOptions.ConfigId = config.Id;
                jobOptions.Bots = config.Settings.GeneralSettings.SuggestedBots;
                wordlistType = config.Settings.DataSettings.AllowedWordlistTypes.First();
            }

            // Write the temporary file
            var tempFile = Path.GetTempFileName();
            await File.WriteAllLinesAsync(tempFile, SelectedHits.Select(h => h.Data)).ConfigureAwait(false);
            var dataPoolOptions = new FileDataPoolOptions
            {
                FileName = tempFile,
                WordlistType = wordlistType
            };
            jobOptions.DataPool = dataPoolOptions;

            // Create the job entity and add it to the database
            var jsonSettings = new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.Auto };
            var jobOptionsWrapper = new JobOptionsWrapper { Options = jobOptions };

            var entity = new JobEntity
            {
                CreationDate = DateTime.Now,
                JobType = JobType.MultiRun,
                JobOptions = JsonConvert.SerializeObject(jobOptionsWrapper, jsonSettings)
            };

            await Dispatcher.UIThread.InvokeAsync(async () =>
            {
                var jobs = SP.GetService<ViewModelsService>().Jobs;
                var jobVM = await jobs.CreateJobAsync(jobOptions);
                window.DisplayJob(jobVM);
            });
        }

        private void CopySelected(object sender, RoutedEventArgs e)
            => SelectedHits.CopyToClipboard(h => h.Data);

        private void CopySelectedProxies(object sender, RoutedEventArgs e)
            => SelectedHits.CopyToClipboard(h => h.Proxy);

        private void CopySelectedWithCapture(object sender, RoutedEventArgs e)
            => SelectedHits.CopyToClipboard(captureMapping);

        private void CopySelectedFull(object sender, RoutedEventArgs e)
            => SelectedHits.CopyToClipboard(fullMapping);

        private void CopySelectedCustom(object sender, RoutedEventArgs e)
        {
            var format = (sender as MenuItem).Header.ToString().Unescape();
            SelectedHits.CopyToClipboard(h => ApplyCustomFormat(h, format));
        }

        private async Task<string> GetSaveFileAsync()
        {
            var topLevel = TopLevel.GetTopLevel(this);
            var file = await topLevel.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
            {
                Title = "Save hits",
                FileTypeChoices = new[]
                {
                    new FilePickerFileType("TXT files") { Patterns = new[] { "*.txt" } }
                }
            });

            return file?.Path.LocalPath;
        }

        private void SaveSelected(object sender, RoutedEventArgs e)
            => TrySave(h => h.Data);

        private void SaveSelectedWithCapture(object sender, RoutedEventArgs e)
            => TrySave(captureMapping);

        private void SaveSelectedFull(object sender, RoutedEventArgs e)
            => TrySave(fullMapping);

        private void SaveSelectedCustom(object sender, RoutedEventArgs e)
        {
            var format = (sender as MenuItem).Header.ToString().Unescape();
            TrySave(h => ApplyCustomFormat(h, format));
        }

        private async void TrySave(Func<HitEntity, string> mapping)
        {
            try
            {
                var fileName = await GetSaveFileAsync();
                if (!string.IsNullOrWhiteSpace(fileName))
                {
                    SelectedHits.SaveToFile(fileName, mapping);
                }
            }
            catch (Exception ex)
            {
                Alert.Exception(ex);
            }
        }

        private static string ApplyCustomFormat(HitEntity hit, string format)
            => new StringBuilder(format)
                .Replace("<DATA>", hit.Data)
                .Replace("<PROXY>", hit.Proxy)
                .Replace("<DATE>", hit.Date.ToLongDateString() + " " + hit.Date.ToLongTimeString())
                .Replace("<CONFIG>", hit.ConfigName)
                .Replace("<WORDLIST>", hit.WordlistName)
                .Replace("<TYPE>", hit.Type)
                .Replace("<CAPTURE>", hit.CapturedData)
                .ToString();
    }
}
