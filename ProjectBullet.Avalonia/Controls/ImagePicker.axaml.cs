using ProjectBullet.Core.Helpers;
using ProjectBullet.Avalonia.Helpers;
using ProjectBullet.Avalonia.Utils;
using ProjectBullet.Avalonia.ViewModels;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media.Imaging;
using Avalonia.Platform.Storage;

namespace ProjectBullet.Avalonia.Controls
{
    /// <summary>
    /// Interaction logic for ImagePicker.axaml
    /// </summary>
    public partial class ImagePicker : UserControl
    {
        private ImagePickerViewModel vm;
        public event EventHandler<byte[]> ImageChanged;

        public ImagePicker(byte[] imageBytes)
        {
            InitializeComponent();
            vm = new ImagePickerViewModel
            {
                ImageBytes = imageBytes
            };
            DataContext = vm;
        }

        private async void OpenImage(object sender, RoutedEventArgs e)
        {
            var topLevel = TopLevel.GetTopLevel(this);
            if (topLevel is null) return;

            var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                Title = "Select an image",
                AllowMultiple = false,
                FileTypeFilter = new[]
                {
                    new FilePickerFileType("Images")
                    {
                        Patterns = new[] { "*.ico", "*.jpg", "*.jpeg", "*.png", "*.bmp" }
                    }
                }
            });

            if (files.Count > 0)
            {
                try
                {
                    var file = files[0];
                    var path = file.Path.LocalPath;
                    vm.SetImageFromFile(path);
                    ImageChanged?.Invoke(this, vm.ImageBytes);
                }
                catch (Exception ex)
                {
                    Alert.Exception(ex);
                }
            }
        }
    }

    public class ImagePickerViewModel : ViewModelBase
    {
        private byte[] imageBytes;
        public byte[] ImageBytes
        {
            get => imageBytes;
            set
            {
                imageBytes = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(Image));
            }
        }

        public Bitmap Image => ImageBytes is null ? null : Images.BytesToBitmap(ImageBytes);

        public void SetImageFromFile(string fileName)
            => ImageBytes = ImageEditor.ToCompatibleFormat(File.ReadAllBytes(fileName));
    }
}
