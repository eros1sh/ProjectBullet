using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;

namespace ProjectBullet.Avalonia.Controls
{
    /// <summary>
    /// Interaction logic for MultipleSelector.axaml
    /// </summary>
    public partial class MultipleSelector : UserControl
    {
        public ObservableCollection<string> SelectedValues
        {
            get => GetValue(SelectedValuesProperty);
            set => SetValue(SelectedValuesProperty, value);
        }

        public static readonly StyledProperty<ObservableCollection<string>> SelectedValuesProperty =
            AvaloniaProperty.Register<MultipleSelector, ObservableCollection<string>>(nameof(SelectedValues));

        public IEnumerable<string> PossibleValues
        {
            get => GetValue(PossibleValuesProperty);
            set => SetValue(PossibleValuesProperty, value);
        }

        public static readonly StyledProperty<IEnumerable<string>> PossibleValuesProperty =
            AvaloniaProperty.Register<MultipleSelector, IEnumerable<string>>(nameof(PossibleValues));

        static MultipleSelector()
        {
            SelectedValuesProperty.Changed.AddClassHandler<MultipleSelector>((selector, e) =>
            {
                selector.UpdateControls();
            });

            PossibleValuesProperty.Changed.AddClassHandler<MultipleSelector>((selector, e) =>
            {
                selector.UpdateControls();
            });
        }

        public void UpdateControls()
        {
            if (SelectedValues is not null)
            {
                selectedValuesControl.ItemsSource = null;
                var selectedList = new ObservableCollection<string>(SelectedValues);
                selectedValuesControl.ItemsSource = selectedList;
            }

            if (PossibleValues is not null && SelectedValues is not null)
            {
                var notSelectedList = new ObservableCollection<string>(PossibleValues.Where(v => !SelectedValues.Contains(v)));
                notSelectedValuesControl.ItemsSource = notSelectedList;
            }
        }

        public MultipleSelector()
        {
            InitializeComponent();
        }

        private void MoveAllRight(object sender, RoutedEventArgs e)
        {
            SelectedValues.Clear();

            var notSelectedList = new ObservableCollection<string>(PossibleValues);
            notSelectedValuesControl.ItemsSource = notSelectedList;
            selectedValuesControl.ItemsSource = new ObservableCollection<string>();

            SelectedValues = SelectedValues; // HACK: Needed to notify...
        }

        private void MoveAllLeft(object sender, RoutedEventArgs e)
        {
            SelectedValues.Clear();

            foreach (var value in PossibleValues)
            {
                SelectedValues.Add(value);
            }

            var selectedList = new ObservableCollection<string>(SelectedValues);
            selectedValuesControl.ItemsSource = selectedList;
            notSelectedValuesControl.ItemsSource = new ObservableCollection<string>();

            SelectedValues = SelectedValues;
        }

        private void MoveItem(object sender, PointerPressedEventArgs e)
        {
            var value = (sender as Label)?.Content as string;
            if (value is null) return;

            if (SelectedValues.Contains(value))
            {
                SelectedValues.Remove(value);
            }
            else
            {
                SelectedValues.Add(value);
            }

            UpdateControls();
            SelectedValues = SelectedValues;
        }
    }
}
