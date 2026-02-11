using ProjectBullet.Core.Services;
using ProjectBullet.Avalonia.Helpers;
using System;
using System.Text;
using System.Threading;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Platform.Storage;

namespace ProjectBullet.Avalonia.Views.Dialogs
{
    public partial class OB2MigrationDialog : UserControl
    {
        private CancellationTokenSource cts;
        private bool isMigrating;

        public OB2MigrationDialog()
        {
            InitializeComponent();
        }

        private async void Browse(object sender, RoutedEventArgs e)
        {
            var topLevel = TopLevel.GetTopLevel(this);
            var folders = await topLevel.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
            {
                Title = "Select OpenBullet 2 folder",
                AllowMultiple = false
            });

            if (folders.Count > 0)
            {
                var folderPath = folders[0].Path.LocalPath;
                pathTextBox.Text = folderPath;
                ValidateFolder(folderPath);
            }
        }

        private void ValidateFolder(string path)
        {
            var dbPath = OB2MigrationService.ValidateOB2Folder(path);
            if (dbPath != null)
            {
                validationText.Text = $"Valid OB2 installation found. DB: {dbPath}";
                validationText.Foreground = Brushes.LimeGreen;
                startButton.IsEnabled = true;
            }
            else
            {
                validationText.Text = "Could not find OpenBullet2.db in selected folder, UserData/, or UserData/Database/.";
                validationText.Foreground = Brushes.OrangeRed;
                startButton.IsEnabled = false;
            }
        }

        private async void StartMigration(object sender, RoutedEventArgs e)
        {
            if (isMigrating) return;
            isMigrating = true;
            startButton.IsEnabled = false;
            progressBar.IsVisible = true;
            progressBar.IsIndeterminate = false;
            resultText.IsVisible = false;
            closeButton.IsVisible = false;

            cts = new CancellationTokenSource();

            var options = new MigrationOptions
            {
                MigrateConfigs = migrateConfigsCheckBox.IsChecked == true,
                MigrateProxies = migrateProxiesCheckBox.IsChecked == true,
                MigrateWordlists = migrateWordlistsCheckBox.IsChecked == true,
                MigrateJobs = migrateJobsCheckBox.IsChecked == true,
                MigrateHits = migrateHitsCheckBox.IsChecked == true
            };

            var progress = new Progress<(string status, int current, int total)>(p =>
            {
                statusText.Text = p.status;
                if (p.total > 0)
                {
                    progressBar.IsIndeterminate = false;
                    progressBar.Maximum = p.total;
                    progressBar.Value = p.current;
                }
                else
                {
                    progressBar.IsIndeterminate = true;
                }
            });

            try
            {
                var service = SP.GetService<OB2MigrationService>();
                var result = await service.MigrateAsync(pathTextBox.Text, options, progress, cts.Token);

                var sb = new StringBuilder();
                sb.AppendLine("Migration completed!");
                sb.AppendLine();
                if (options.MigrateConfigs)
                    sb.AppendLine($"Configs: {result.ConfigsMigrated} migrated, {result.ConfigsSkipped} skipped");
                if (options.MigrateProxies)
                    sb.AppendLine($"Proxy Groups: {result.ProxyGroupsMigrated} migrated, {result.ProxyGroupsSkipped} skipped");
                if (options.MigrateProxies)
                    sb.AppendLine($"Proxies: {result.ProxiesMigrated} migrated");
                if (options.MigrateWordlists)
                    sb.AppendLine($"Wordlists: {result.WordlistsMigrated} migrated, {result.WordlistsSkipped} skipped");
                if (options.MigrateJobs)
                    sb.AppendLine($"Jobs: {result.JobsMigrated} migrated");
                if (options.MigrateHits)
                {
                    sb.AppendLine($"Hits: {result.HitsMigrated} migrated");
                    sb.AppendLine($"Records: {result.RecordsMigrated} migrated");
                }
                if (result.Errors.Count > 0)
                {
                    sb.AppendLine();
                    sb.AppendLine($"Errors ({result.Errors.Count}):");
                    foreach (var err in result.Errors.GetRange(0, Math.Min(result.Errors.Count, 10)))
                        sb.AppendLine($"  - {err}");
                    if (result.Errors.Count > 10)
                        sb.AppendLine($"  ... and {result.Errors.Count - 10} more");
                }

                resultText.Text = sb.ToString();
                resultText.IsVisible = true;
                statusText.Text = "Done!";
                progressBar.Value = progressBar.Maximum;

                try
                {
                    var configService = SP.GetService<ConfigService>();
                    await configService.ReloadConfigsAsync();
                }
                catch { }
            }
            catch (OperationCanceledException)
            {
                statusText.Text = "Migration cancelled.";
            }
            catch (Exception ex)
            {
                Alert.Exception(ex);
                statusText.Text = $"Migration failed: {ex.Message}";
            }
            finally
            {
                isMigrating = false;
                closeButton.IsVisible = true;
                cts?.Dispose();
                cts = null;
            }
        }

        private void CloseDialog(object sender, RoutedEventArgs e)
        {
            cts?.Cancel();
            if (this.VisualRoot is MainDialog dialog) dialog.Close();
        }
    }
}
