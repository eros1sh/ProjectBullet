using System;
using Avalonia.Controls;
using Avalonia.Media;

namespace ProjectBullet.Avalonia.Controls
{
    /// <summary>
    /// Interaction logic for ColoredLog.axaml
    /// </summary>
    public partial class ColoredLog : UserControl
    {
        public int BufferSize { get; set; } = 30;
        private int count = 0;

        public ColoredLog()
        {
            InitializeComponent();
        }

        public void Append(string message, Color color, bool addTimestamp = true)
        {
            var brush = new SolidColorBrush(color);

            var block = new TextBlock
            {
                Text = $"[{DateTime.Now:dd/MM/yyyy HH:mm:ss}] {message}",
                Foreground = brush
            };

            log.Children.Add(block);
            count++;

            if (count > BufferSize)
            {
                log.Children.RemoveAt(0);
                count--;
            }

            scrollViewer.ScrollToEnd();
        }

        public void Clear()
        {
            log.Children.Clear();
            count = 0;
        }
    }
}
