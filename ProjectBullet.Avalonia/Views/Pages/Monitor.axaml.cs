using Avalonia.Controls;
using ProjectBullet.Avalonia.ViewModels;

namespace ProjectBullet.Avalonia.Views.Pages
{
    public partial class Monitor : UserControl
    {
        private readonly MonitorViewModel vm;

        public Monitor()
        {
            vm = new MonitorViewModel();
            DataContext = vm;
            InitializeComponent();
        }
    }
}
