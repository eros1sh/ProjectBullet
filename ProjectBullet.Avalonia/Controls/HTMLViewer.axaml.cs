using Ganss.Xss;
using ProjectBullet.Avalonia.Helpers;
using ProjectBullet.Avalonia.Utils;
using Avalonia;
using Avalonia.Controls;

namespace ProjectBullet.Avalonia.Controls
{
    /// <summary>
    /// Interaction logic for HTMLViewer.axaml
    /// </summary>
    public partial class HTMLViewer : UserControl
    {
        public string HTML
        {
            get => GetValue(HTMLProperty);
            set => SetValue(HTMLProperty, value);
        }

        public static readonly StyledProperty<string> HTMLProperty =
            AvaloniaProperty.Register<HTMLViewer, string>(nameof(HTML));

        static HTMLViewer()
        {
            HTMLProperty.Changed.AddClassHandler<HTMLViewer>((viewer, e) =>
            {
                var newValue = e.NewValue as string;
                if (!string.IsNullOrEmpty(newValue))
                {
                    var sanitizer = new HtmlSanitizer();
                    var html = sanitizer.Sanitize(newValue);
                    viewer.Render(html);
                }
            });
        }

        public HTMLViewer()
        {
            InitializeComponent();
            Render(string.Empty);
        }

        public void Render(string html)
        {
            // Since Avalonia does not have a built-in WebBrowser,
            // display sanitized text content as a placeholder
            htmlTextBlock.Text = html;
        }
    }
}
