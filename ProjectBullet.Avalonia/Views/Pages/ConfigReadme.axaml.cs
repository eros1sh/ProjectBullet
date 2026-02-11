using Avalonia.Controls;
using ProjectBullet.Avalonia.Services;
using ProjectBullet.Avalonia.ViewModels;

namespace ProjectBullet.Avalonia.Views.Pages
{
    /// <summary>
    /// Interaction logic for ConfigReadme.axaml
    /// </summary>
    public partial class ConfigReadme : UserControl
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
            readmeTextBox.Text = vm.Readme;
        }

        private void ReadmeChanged(object sender, TextChangedEventArgs e)
        {
            var newText = readmeTextBox.Text;

            if (!string.IsNullOrWhiteSpace(newText))
            {
                vm.Readme = newText;
            }
        }
    }
}
