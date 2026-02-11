using ProjectBullet.Avalonia.Services;
using ProjectBullet.Avalonia.ViewModels;
using ProjectBullet.Avalonia.Views.Pages;
using RuriLib.Models.Blocks;
using RuriLib.Models.Trees;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;

namespace ProjectBullet.Avalonia.Views.Dialogs
{
    public partial class AddBlockDialog : UserControl
    {
        private readonly AddBlockDialogViewModel vm;
        private readonly object caller;

        public AddBlockDialog()
        {
            InitializeComponent();
        }

        public AddBlockDialog(object caller)
        {
            this.caller = caller;

            vm = new AddBlockDialogViewModel();
            DataContext = vm;

            InitializeComponent();
            filterTextBox.Focus();
        }

        private void GoUp(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrWhiteSpace(vm.Filter))
            {
                filterTextBox.Text = string.Empty;
                vm.Filter = string.Empty;
            }
            else
            {
                vm.GoUp();
            }
        }

        private void SelectCategory(object sender, RoutedEventArgs e)
            => vm.SelectCategory((CategoryTreeNode)(sender as Button).Tag);

        private void SelectDescriptor(object sender, RoutedEventArgs e) => SelectDescriptorInternal(sender);
        private void SelectDescriptorPointer(object sender, PointerPressedEventArgs e) => SelectDescriptorInternal(sender);
        private void SelectDescriptorInternal(object sender)
        {
            var descriptor = (BlockDescriptor)(sender as Control).Tag;
            vm.SelectDescriptor(descriptor);

            if (caller is ConfigStacker page)
            {
                page.CreateBlock(descriptor);
            }

            if (this.VisualRoot is MainDialog dialog) dialog.Close();
        }

        private void Search(object sender, RoutedEventArgs e) => vm.Filter = filterTextBox.Text;

        protected override void OnKeyDown(KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                vm.Filter = filterTextBox.Text;
            }
            base.OnKeyDown(e);
        }
    }

    public class AddBlockDialogViewModel : ViewModelBase
    {
        private readonly VolatileSettingsService volatileSettings;

        private CategoryTreeNode currentNode;
        public CategoryTreeNode CurrentNode
        {
            get => currentNode;
            set
            {
                currentNode = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(CanGoUp));

                CreateCollection();
            }
        }

        public bool CanGoUp => CurrentNode.Parent is not null;

        private ObservableCollection<object> nodesCollection;
        public ObservableCollection<object> NodesCollection
        {
            get => nodesCollection;
            set
            {
                nodesCollection = value;
                OnPropertyChanged();
            }
        }

        private string filter = string.Empty;
        public string Filter
        {
            get => filter;
            set
            {
                filter = value;
                OnPropertyChanged();
                CreateCollection();
            }
        }

        private ObservableCollection<BlockDescriptor> recentDescriptors;
        public ObservableCollection<BlockDescriptor> RecentDescriptors
        {
            get => recentDescriptors;
            set
            {
                recentDescriptors = value;
                OnPropertyChanged();
            }
        }

        public AddBlockDialogViewModel()
        {
            volatileSettings = SP.GetService<VolatileSettingsService>();

            var root = RuriLib.Globals.DescriptorsRepository.AsTree();
            CurrentNode = root
                .SubCategories.First(s => s.Name == "RuriLib")
                .SubCategories.First(s => s.Name == "Blocks");

            RecentDescriptors = new ObservableCollection<BlockDescriptor>(volatileSettings.RecentDescriptors.Take(8));
        }

        public void SelectDescriptor(BlockDescriptor descriptor) => volatileSettings.AddRecentDescriptor(descriptor);

        public void SelectCategory(CategoryTreeNode node)
        {
            CurrentNode = node;
            CreateCollection();
        }

        public void GoUp()
        {
            if (CurrentNode.Parent is not null)
            {
                CurrentNode = CurrentNode.Parent;
            }
        }

        private void CreateCollection()
        {
            var composite = new ObservableCollection<object>();

            if (string.IsNullOrWhiteSpace(filter))
            {
                foreach (var sub in currentNode.SubCategories)
                    composite.Add(sub);
                foreach (var desc in currentNode.Descriptors)
                    composite.Add(desc);
            }
            else
            {
                foreach (var desc in RuriLib.Globals.DescriptorsRepository.Descriptors.Values
                    .Where(d => d.Name.Contains(filter, StringComparison.OrdinalIgnoreCase)))
                {
                    composite.Add(desc);
                }
            }

            NodesCollection = composite;
        }
    }
}
