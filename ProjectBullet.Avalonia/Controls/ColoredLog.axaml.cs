using System;
using System.Collections.Generic;
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
        private readonly Queue<TextBlock> buffer = new();

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
            buffer.Enqueue(block);

            while (buffer.Count > BufferSize)
            {
                var old = buffer.Dequeue();
                log.Children.Remove(old);
            }

            scrollViewer.ScrollToEnd();
        }

        public void Clear()
        {
            log.Children.Clear();
            buffer.Clear();
        }
    }
}
