using ProjectBullet.Avalonia.ViewModels;
using RuriLib.Extensions;
using RuriLib.Models.Blocks.Settings;
using System;
using System.Collections.Generic;
using Avalonia.Controls;
using Avalonia.Interactivity;

namespace ProjectBullet.Avalonia.Controls
{
    /// <summary>
    /// Interaction logic for BoolSettingViewer.axaml
    /// </summary>
    public partial class BoolSettingViewer : UserControl
    {
        private BoolSettingViewerViewModel vm;

        public BlockSetting Setting
        {
            get => vm?.Setting;
            set
            {
                if (value.FixedSetting is not BoolSetting)
                {
                    throw new Exception("Invalid setting type for this UC");
                }

                vm = new BoolSettingViewerViewModel(value);
                DataContext = vm;

                tabControl.SelectedIndex = vm.Mode switch
                {
                    SettingInputMode.Variable => 0,
                    SettingInputMode.Fixed => 1,
                    _ => throw new NotImplementedException()
                };

                buttonTabControl.SelectedIndex = vm.Mode switch
                {
                    SettingInputMode.Variable => 0,
                    SettingInputMode.Fixed => 1,
                    _ => throw new NotImplementedException()
                };
            }
        }

        public BoolSettingViewer()
        {
            InitializeComponent();
        }

        // Constant -> Variable
        private void VariableMode(object sender, RoutedEventArgs e)
        {
            vm.Mode = SettingInputMode.Variable;
            tabControl.SelectedIndex = 0;
            buttonTabControl.SelectedIndex = 0;
        }

        // Variable -> Constant
        private void ConstantMode(object sender, RoutedEventArgs e)
        {
            vm.Mode = SettingInputMode.Fixed;
            tabControl.SelectedIndex = 1;
            buttonTabControl.SelectedIndex = 1;
        }
    }

    public class BoolSettingViewerViewModel : ViewModelBase
    {
        public BlockSetting Setting { get; init; }

        public string Name => Setting.ReadableName;

        public string Description => Setting.Description;

        public IEnumerable<string> Suggestions => Utils.Suggestions.GetInputVariableSuggestions(Setting);

        public SettingInputMode Mode
        {
            get => Setting.InputMode;
            set
            {
                Setting.InputMode = value;
                OnPropertyChanged();
            }
        }

        public string VariableName
        {
            get => Setting.InputVariableName;
            set
            {
                Setting.InputVariableName = value;
                OnPropertyChanged();
            }
        }

        public bool Value
        {
            get => (Setting.FixedSetting as BoolSetting).Value;
            set
            {
                var s = Setting.FixedSetting as BoolSetting;
                s.Value = value;
                OnPropertyChanged();
            }
        }

        public BoolSettingViewerViewModel(BlockSetting setting)
        {
            Setting = setting;
        }
    }
}
