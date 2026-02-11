using ProjectBullet.Avalonia.ViewModels;
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Avalonia.Controls;
using Avalonia.Interactivity;

namespace ProjectBullet.Avalonia.Views.Dialogs
{
    public partial class UpdateConfirmationDialog : UserControl
    {
        private readonly UpdateConfirmationDialogViewModel vm;

        public UpdateConfirmationDialog()
        {
            InitializeComponent();
        }

        public UpdateConfirmationDialog(Version current, Version remote)
        {
            InitializeComponent();

            vm = new UpdateConfirmationDialogViewModel(current, remote);
            DataContext = vm;
        }

        private void Confirm(object sender, RoutedEventArgs e)
        {
            var updaterFileName = RuntimeInformation.OSArchitecture switch
            {
                Architecture.Arm64 => "pb-native-updater-win-arm64.exe",
                Architecture.X64 => "pb-native-updater-win-x64.exe",
                Architecture.X86 => "pb-native-updater-win-x86.exe",
                _ => throw new NotImplementedException()
            };

            Process.Start(updaterFileName);
            Environment.Exit(0);
        }

        private void GoBack(object sender, RoutedEventArgs e)
        {
            if (this.VisualRoot is MainDialog dialog) dialog.Close();
        }
    }

    public class UpdateConfirmationDialogViewModel : ViewModelBase
    {
        private string currentVersion = string.Empty;
        public string CurrentVersion
        {
            get => currentVersion;
            set
            {
                currentVersion = value;
                OnPropertyChanged();
            }
        }

        private string remoteVersion = string.Empty;
        public string RemoteVersion
        {
            get => remoteVersion;
            set
            {
                remoteVersion = value;
                OnPropertyChanged();
            }
        }

        public UpdateConfirmationDialogViewModel(Version current, Version remote)
        {
            CurrentVersion = current.ToString();
            RemoteVersion = remote.ToString();
        }
    }
}
