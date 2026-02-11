using ProjectBullet.Avalonia.ViewModels;
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;

namespace ProjectBullet.Avalonia.Views.Dialogs
{
    public partial class AutoUpdateDialog : UserControl
    {
        private readonly AutoUpdateDialogViewModel vm;
        private readonly DispatcherTimer countdownTimer;
        private int secondsRemaining = 10;

        public AutoUpdateDialog()
        {
            InitializeComponent();
        }

        public AutoUpdateDialog(Version current, Version remote)
        {
            InitializeComponent();

            vm = new AutoUpdateDialogViewModel(current, remote);
            DataContext = vm;

            countdownTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1)
            };
            countdownTimer.Tick += CountdownTick;
            countdownTimer.Start();

            UpdateCountdownText();
        }

        private void CountdownTick(object sender, EventArgs e)
        {
            secondsRemaining--;
            UpdateCountdownText();

            if (secondsRemaining <= 0)
            {
                countdownTimer.Stop();
                LaunchUpdater();
            }
        }

        private void UpdateCountdownText()
        {
            vm.CountdownText = $"Updating in {secondsRemaining} seconds...";
        }

        private void UpdateNow(object sender, RoutedEventArgs e)
        {
            countdownTimer.Stop();
            LaunchUpdater();
        }

        private void Cancel(object sender, RoutedEventArgs e)
        {
            countdownTimer.Stop();
            if (this.VisualRoot is MainDialog dialog) dialog.Close();
        }

        private void LaunchUpdater()
        {
            var updaterFileName = RuntimeInformation.OSArchitecture switch
            {
                Architecture.Arm64 => "pb-native-updater-win-arm64.exe",
                Architecture.X64 => "pb-native-updater-win-x64.exe",
                Architecture.X86 => "pb-native-updater-win-x86.exe",
                _ => throw new NotImplementedException()
            };

            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = updaterFileName,
                    Arguments = "--silent --channel release",
                    UseShellExecute = false
                });
            }
            catch
            {
                // Fallback: try without silent mode
                try { Process.Start(updaterFileName); } catch { }
            }

            Environment.Exit(0);
        }
    }

    public class AutoUpdateDialogViewModel : ViewModelBase
    {
        public string CurrentVersion { get; }
        public string RemoteVersion { get; }

        private string countdownText = string.Empty;
        public string CountdownText
        {
            get => countdownText;
            set
            {
                countdownText = value;
                OnPropertyChanged();
            }
        }

        public AutoUpdateDialogViewModel(Version current, Version remote)
        {
            CurrentVersion = current.ToString();
            RemoteVersion = remote.ToString();
        }
    }
}
