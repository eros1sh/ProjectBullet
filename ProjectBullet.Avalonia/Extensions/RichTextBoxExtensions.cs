using Avalonia.Media;
using AvaloniaEdit;
using AvaloniaEdit.Document;
using System;
using System.Linq;

namespace ProjectBullet.Avalonia.Extensions
{
    // Converted from WPF RichTextBox to AvaloniaEdit TextEditor
    public static class TextEditorExtensions
    {
        public static void AppendColoredText(this TextEditor editor, string text, Color color)
        {
            // AvaloniaEdit doesn't natively support per-line coloring through the same API.
            // For now, append the text. Color rendering will be handled by syntax highlighting
            // or a custom DocumentColorizingTransformer.
            editor.Document.Insert(editor.Document.TextLength, text);
        }

        public static string[] Lines(this TextEditor editor)
        {
            return editor.Text.Split(new string[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);
        }

        public static string GetText(this TextEditor editor)
            => editor.Text;

        public static string GetTextFromLines(this TextEditor editor)
            => editor.Lines().Aggregate((current, next) => current + next);
    }
}
