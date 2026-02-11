using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using ProjectBullet.Core.Entities;
using ProjectBullet.Avalonia.Helpers;
using ProjectBullet.Avalonia.Services;
using ProjectBullet.Avalonia.ViewModels;
using ProjectBullet.Avalonia.Views.Dialogs;
using RuriLib.Models.Environment;
using RuriLib.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace ProjectBullet.Avalonia.Views.Pages
{
    /// <summary>
    /// Interaction logic for Wordlists.axaml
    /// </summary>
    public partial class Wordlists : UserControl
    {
        private readonly WordlistsViewModel vm;
        private readonly EnvironmentSettings env;
        private readonly MainWindow window;

        private IEnumerable<WordlistEntity> SelectedWordlists => wordlistListView.SelectedItems.Cast<WordlistEntity>().ToList();

        public Wordlists()
        {
            vm = SP.GetService<ViewModelsService>().Wordlists;
            DataContext = vm;
            _ = vm.InitializeAsync();

            InitializeComponent();
            window = SP.GetService<MainWindow>();
            env = SP.GetService<RuriLibSettingsService>().Environment;

            AddHandler(DragDrop.DropEvent, HandleDrop);
        }

        private async void Add(object sender, RoutedEventArgs e)
            => await new MainDialog(new AddWordlistDialog(this), "Add a wordlist").ShowDialog(TopLevel.GetTopLevel(this) as Window);

        private async void DeleteSelected(object sender, RoutedEventArgs e)
        {
            foreach (var wordlist in SelectedWordlists)
            {
                await vm.DeleteAsync(wordlist);
            }

            Alert.Success("Done", "Successfully deleted the selected wordlist references from the DB");
        }

        private void DeleteAll(object sender, RoutedEventArgs e)
        {
            vm.DeleteAll();
            Alert.Success("Done", "Successfully deleted all wordlist references from the DB");
        }

        private async void DeleteNotFound(object sender, RoutedEventArgs e)
        {
            var deleted = await vm.DeleteNotFoundAsync();
            Alert.Success("Done", $"Successfully deleted {deleted} unresolved wordlist references from the DB");
        }

        private void UpdateSearch(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                vm.SearchString = filterTextbox.Text;
            }
        }

        private void Search(object sender, RoutedEventArgs e) => vm.SearchString = filterTextbox.Text;

        public async void AddWordlist(WordlistEntity wordlist)
        {
            try
            {
                await vm.AddAsync(wordlist);
            }
            catch (Exception ex)
            {
                Alert.Exception(ex);
            }
        }

        private async void HandleDrop(object sender, DragEventArgs e)
        {
            if (e.Data.Contains(DataFormats.Files))
            {
                var files = e.Data.GetFiles();
                if (files is null) return;

                foreach (var item in files)
                {
                    var file = item.Path.LocalPath;
                    if (!file.EndsWith(".txt")) continue;

                    try
                    {
                        var path = file;
                        var cwd = Directory.GetCurrentDirectory();

                        // Make the path relative if inside the CWD
                        if (path.StartsWith(cwd))
                        {
                            path = path[(cwd.Length + 1)..];
                        }

                        var firstLine = File.ReadLines(path).FirstOrDefault(l => !string.IsNullOrWhiteSpace(l)) ?? string.Empty;

                        var entity = new WordlistEntity
                        {
                            Name = Path.GetFileNameWithoutExtension(file),
                            FileName = path,
                            Type = env.RecognizeWordlistType(firstLine),
                            Purpose = string.Empty,
                            Total = File.ReadLines(path).Count()
                        };

                        await vm.AddAsync(entity);
                    }
                    catch
                    {

                    }
                }
            }
        }
    }
}
