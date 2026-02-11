using ProjectBullet.Core.Services;
using ProjectBullet.Native.Helpers;
using System;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace ProjectBullet.Native.Views.Dialogs
{
    public partial class OB2MigrationDialog : Page
    {
        private CancellationTokenSource cts;
        private bool isMigrating;

        public OB2MigrationDialog()
        {
            InitializeComponent();
        }

        private void Browse(object sender, RoutedEventArgs e)
        {
            var dialog = new Microsoft.Win32.OpenFolderDialog
            {
                Title = "Select OpenBullet 2 folder"
            };

            if (dialog.ShowDialog() == true)
            {
                pathTextBox.Text = dialog.FolderName;
                ValidateFolder(dialog.FolderName);
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
            progressBar.Visibility = Visibility.Visible;
            progressBar.IsIndeterminate = false;
            resultText.Visibility = Visibility.Collapsed;
            closeButton.Visibility = Visibility.Collapsed;

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

                // Build result summary
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
                resultText.Visibility = Visibility.Visible;
                statusText.Text = "Done!";
                progressBar.Value = progressBar.Maximum;

                // Reload configs so newly imported ones show up
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
                closeButton.Visibility = Visibility.Visible;
                cts?.Dispose();
                cts = null;
            }
        }

        private void Close(object sender, RoutedEventArgs e)
        {
            cts?.Cancel();
            ((MainDialog)Parent).Close();
        }
    }
}
