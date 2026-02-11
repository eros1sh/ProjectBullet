using ProjectBullet.Core.Services;
using ProjectBullet.Avalonia.DTOs;
using ProjectBullet.Avalonia.Helpers;
using ProjectBullet.Avalonia.Views.Pages;
using RuriLib.Functions.Files;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;

namespace ProjectBullet.Avalonia.Views.Dialogs
{
    public partial class CreateConfigDialog : UserControl
    {
        private readonly object caller;

        public CreateConfigDialog()
        {
            InitializeComponent();
        }

        public CreateConfigDialog(object caller)
        {
            InitializeComponent();
            this.caller = caller;

            var settings = SP.GetService<ProjectBulletSettingsService>().Settings;
            authorTextbox.Text = settings.GeneralSettings.DefaultAuthor;
            nameTextbox.Focus();

            var categoryList = new List<string> { "Default" };
            categoryList.AddRange(SP.GetService<ConfigService>().Configs
                .Select(c => c.Metadata.Category)
                .Where(category => category != "Default")
                .Distinct());

            categoryCombobox.ItemsSource = categoryList;
            categoryCombobox.Text = "Default";
        }

        private void CreateAndClose()
        {
            if (caller is Configs page)
            {
                var dto = new ConfigForCreationDto
                {
                    Name = nameTextbox.Text,
                    Category = categoryCombobox.Text,
                    Author = authorTextbox.Text
                };

                // Check if name is ok
                if (string.IsNullOrWhiteSpace(dto.Name))
                {
                    Alert.Error("Invalid name", "The name cannot be blank");
                    return;
                }

                page.CreateConfig(dto);
            }
            if (this.VisualRoot is MainDialog dialog) dialog.Close();
        }

        private void Accept(object sender, RoutedEventArgs e) => CreateAndClose();

        protected override void OnKeyDown(KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                CreateAndClose();
            }
            base.OnKeyDown(e);
        }
    }
}
