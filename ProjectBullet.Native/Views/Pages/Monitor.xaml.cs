using ProjectBullet.Native.ViewModels;
using System.Windows.Controls;

namespace ProjectBullet.Native.Views.Pages
{
    /// <summary>
    /// Interaction logic for Monitor.xaml
    /// </summary>
    public partial class Monitor : Page
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
