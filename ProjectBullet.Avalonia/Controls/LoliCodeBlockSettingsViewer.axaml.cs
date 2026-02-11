using AvaloniaEdit.Highlighting;
using AvaloniaEdit.Highlighting.Xshd;
using AvaloniaEdit.Search;
using ProjectBullet.Core.Services;
using ProjectBullet.Avalonia.ViewModels;
using RuriLib.Models.Blocks;
using System;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using System.Xml;

namespace ProjectBullet.Avalonia.Controls
{
    /// <summary>
    /// Interaction logic for LoliCodeBlockSettingsViewer.axaml
    /// </summary>
    public partial class LoliCodeBlockSettingsViewer : UserControl
    {
        private readonly LoliCodeBlockSettingsViewerViewModel vm;
        private readonly ProjectBulletSettingsService obSettingsService;

        public LoliCodeBlockSettingsViewer(BlockViewModel blockVM)
        {
            if (blockVM.Block is not LoliCodeBlockInstance)
            {
                throw new Exception("Wrong block type for this UC");
            }

            obSettingsService = SP.GetService<ProjectBulletSettingsService>();
            vm = new LoliCodeBlockSettingsViewerViewModel(blockVM);
            DataContext = vm;

            InitializeComponent();

            editor.WordWrap = obSettingsService.Settings.CustomizationSettings.WordWrap;
            editor.Text = vm.Script;
            HighlightSyntax();
            SearchPanel.Install(editor);
        }

        private void EditorLostFocus(object sender, RoutedEventArgs e) => vm.Script = editor.Text;

        private void HighlightSyntax()
        {
            using var reader = XmlReader.Create("Highlighting/LoliCode.xshd");
            editor.SyntaxHighlighting = HighlightingLoader.Load(reader, HighlightingManager.Instance);
            editor.TextArea.TextView.LinkTextForegroundBrush = new SolidColorBrush(Colors.DodgerBlue);
            editor.TextArea.TextView.LinkTextUnderline = false;
        }
    }

    public class LoliCodeBlockSettingsViewerViewModel : BlockSettingsViewerViewModel
    {
        public LoliCodeBlockInstance LoliCodeBlock => Block as LoliCodeBlockInstance;

        public string Script
        {
            get => LoliCodeBlock.Script;
            set
            {
                LoliCodeBlock.Script = value;
                OnPropertyChanged();
            }
        }

        public LoliCodeBlockSettingsViewerViewModel(BlockViewModel block) : base(block)
        {

        }
    }
}
