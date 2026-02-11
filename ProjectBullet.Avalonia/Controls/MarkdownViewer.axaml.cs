using Markdig;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Layout;
using Avalonia.Media;

namespace ProjectBullet.Avalonia.Controls
{
    public partial class MarkdownViewer : UserControl
    {
        public string MarkdownText
        {
            get => GetValue(MarkdownTextProperty);
            set => SetValue(MarkdownTextProperty, value);
        }

        public static readonly StyledProperty<string> MarkdownTextProperty =
            AvaloniaProperty.Register<MarkdownViewer, string>(nameof(MarkdownText));

        private static readonly MarkdownPipeline Pipeline =
            new MarkdownPipelineBuilder().UseAdvancedExtensions().Build();

        static MarkdownViewer()
        {
            MarkdownTextProperty.Changed.AddClassHandler<MarkdownViewer>((viewer, e) =>
            {
                viewer.RenderMarkdown(e.NewValue as string);
            });
        }

        public MarkdownViewer()
        {
            InitializeComponent();
        }

        private void RenderMarkdown(string? markdown)
        {
            contentPanel.Children.Clear();
            if (string.IsNullOrEmpty(markdown)) return;

            var document = Markdown.Parse(markdown, Pipeline);

            foreach (var block in document)
            {
                var element = RenderBlock(block);
                if (element != null)
                    contentPanel.Children.Add(element);
            }
        }

        private Control? RenderBlock(Block block)
        {
            return block switch
            {
                HeadingBlock heading => RenderHeading(heading),
                ParagraphBlock paragraph => RenderParagraph(paragraph),
                ListBlock list => RenderList(list),
                CodeBlock code => RenderCodeBlock(code),
                ThematicBreakBlock => new Border
                {
                    Height = 1,
                    Background = new SolidColorBrush(Color.Parse("#333")),
                    Margin = new Thickness(0, 12)
                },
                QuoteBlock quote => RenderQuote(quote),
                _ => null
            };
        }

        private TextBlock RenderHeading(HeadingBlock heading)
        {
            var tb = new TextBlock
            {
                TextWrapping = TextWrapping.Wrap,
                Foreground = Brushes.White,
                FontWeight = FontWeight.Bold,
                Margin = new Thickness(0, heading.Level <= 2 ? 16 : 10, 0, 6),
                FontSize = heading.Level switch
                {
                    1 => 24,
                    2 => 20,
                    3 => 17,
                    4 => 15,
                    _ => 14
                }
            };
            SetInlines(tb, heading.Inline);
            return tb;
        }

        private TextBlock RenderParagraph(ParagraphBlock paragraph)
        {
            var tb = new TextBlock
            {
                TextWrapping = TextWrapping.Wrap,
                Foreground = new SolidColorBrush(Color.Parse("#D1D5DB")),
                Margin = new Thickness(0, 0, 0, 8),
                LineHeight = 22
            };
            SetInlines(tb, paragraph.Inline);
            return tb;
        }

        private StackPanel RenderList(ListBlock list)
        {
            var panel = new StackPanel { Margin = new Thickness(16, 0, 0, 8) };
            int idx = 1;

            foreach (var item in list)
            {
                if (item is not ListItemBlock li) continue;

                var row = new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    Margin = new Thickness(0, 2)
                };

                row.Children.Add(new TextBlock
                {
                    Text = list.IsOrdered ? $"{idx++}. " : "\u2022 ",
                    Foreground = new SolidColorBrush(Color.Parse("#8B5CF6")),
                    VerticalAlignment = VerticalAlignment.Top,
                    Width = list.IsOrdered ? 24 : 16
                });

                var content = new StackPanel();
                foreach (var sub in li)
                {
                    var el = RenderBlock(sub);
                    if (el == null) continue;
                    if (el is TextBlock t) t.Margin = new Thickness(0);
                    content.Children.Add(el);
                }
                row.Children.Add(content);
                panel.Children.Add(row);
            }

            return panel;
        }

        private Border RenderCodeBlock(CodeBlock code)
        {
            return new Border
            {
                Background = new SolidColorBrush(Color.Parse("#1A1A2E")),
                BorderBrush = new SolidColorBrush(Color.Parse("#333")),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(6),
                Padding = new Thickness(12, 8),
                Margin = new Thickness(0, 4, 0, 8),
                Child = new TextBlock
                {
                    Text = code.Lines.ToString().TrimEnd(),
                    FontFamily = new FontFamily("Cascadia Code, Consolas, monospace"),
                    Foreground = new SolidColorBrush(Color.Parse("#E5E7EB")),
                    FontSize = 13,
                    TextWrapping = TextWrapping.Wrap
                }
            };
        }

        private Border RenderQuote(QuoteBlock quote)
        {
            var panel = new StackPanel();
            foreach (var sub in quote)
            {
                var el = RenderBlock(sub);
                if (el != null) panel.Children.Add(el);
            }

            return new Border
            {
                BorderBrush = new SolidColorBrush(Color.Parse("#8B5CF6")),
                BorderThickness = new Thickness(3, 0, 0, 0),
                Padding = new Thickness(12, 4, 0, 4),
                Margin = new Thickness(0, 4, 0, 8),
                Child = panel
            };
        }

        private void SetInlines(TextBlock tb, ContainerInline? container)
        {
            if (container == null) return;
            var inlines = new InlineCollection();
            PopulateInlines(inlines, container);
            tb.Inlines = inlines;
        }

        private void PopulateInlines(InlineCollection target, ContainerInline container)
        {
            foreach (var inline in container)
            {
                switch (inline)
                {
                    case LiteralInline literal:
                        target.Add(new Run(literal.Content.ToString()));
                        break;

                    case EmphasisInline emphasis:
                        Span span = emphasis.DelimiterCount >= 2 ? new Bold() : new Italic();
                        PopulateInlines(span.Inlines, emphasis);
                        target.Add(span);
                        break;

                    case CodeInline code:
                        var cs = new Span();
                        cs.SetValue(TextElement.FontFamilyProperty,
                            new FontFamily("Cascadia Code, Consolas, monospace"));
                        cs.SetValue(TextElement.ForegroundProperty,
                            new SolidColorBrush(Color.Parse("#A78BFA")));
                        cs.Inlines.Add(new Run(code.Content));
                        target.Add(cs);
                        break;

                    case LinkInline link:
                        var ls = new Span();
                        ls.SetValue(TextElement.ForegroundProperty,
                            new SolidColorBrush(Color.Parse("#8B5CF6")));
                        PopulateInlines(ls.Inlines, link);
                        target.Add(ls);
                        break;

                    case LineBreakInline:
                        target.Add(new LineBreak());
                        break;

                    case AutolinkInline autolink:
                        var als = new Span();
                        als.SetValue(TextElement.ForegroundProperty,
                            new SolidColorBrush(Color.Parse("#8B5CF6")));
                        als.Inlines.Add(new Run(autolink.Url));
                        target.Add(als);
                        break;

                    case HtmlInline:
                        break;
                }
            }
        }
    }
}
