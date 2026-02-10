using ProjectBullet.Native.Extensions;
using ProjectBullet.Native.Services;
using ProjectBullet.Native.ViewModels;
using System.Windows.Controls;

namespace ProjectBullet.Native.Views.Pages
{
    /// <summary>
    /// Interaction logic for ConfigReadme.xaml
    /// </summary>
    public partial class ConfigReadme : Page
    {
        private readonly ConfigReadmeViewModel vm;

        public ConfigReadme()
        {
            vm = SP.GetService<ViewModelsService>().ConfigReadme;
            DataContext = vm;

            InitializeComponent();
        }

        // TODO: Find out why the preview doesn't update when navigating to the page
        public void UpdateViewModel()
        {
            vm.UpdateViewModel();
            readmeRTB.Document.Blocks.Clear();
            readmeRTB.AppendText(vm.Readme);
        }

        private void ReadmeChanged(object sender, TextChangedEventArgs e)
        {
            var newText = readmeRTB.GetText();

            if (!string.IsNullOrWhiteSpace(newText))
            {
                vm.Readme = newText;
            }
        }
    }
}
