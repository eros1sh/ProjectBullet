using Avalonia.Controls;

namespace ProjectBullet.Avalonia
{
    public partial class MainDialog : Window
    {
        public MainDialog()
        {
            InitializeComponent();
        }

        public MainDialog(UserControl content, string title, bool canResize = false)
        {
            InitializeComponent();

            Content = content;
            Title = title;
            CanResize = canResize;
        }

        public MainDialog(UserControl content, string title, int initialWidth, int initialHeight)
        {
            InitializeComponent();

            Content = content;
            Title = title;
            CanResize = true;
            Width = initialWidth;
            Height = initialHeight;
        }

        public new async System.Threading.Tasks.Task<T> ShowDialog<T>(Window owner)
        {
            return await base.ShowDialog<T>(owner ?? GetOwnerWindow());
        }

        public new async System.Threading.Tasks.Task ShowDialog(Window owner)
        {
            await base.ShowDialog(owner ?? GetOwnerWindow());
        }

        private Window GetOwnerWindow()
        {
            if (global::Avalonia.Application.Current?.ApplicationLifetime
                is global::Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop)
            {
                return desktop.MainWindow;
            }
            return null;
        }
    }
}
