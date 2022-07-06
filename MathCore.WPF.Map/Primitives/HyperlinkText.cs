using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;

namespace MathCore.WPF.Map.Primitives;

/// <summary>Инструменты для работы с текстом гиперссылки</summary>
public static class HyperlinkText
{
    /// <summary>Регулярное выражение для разбора элементов гиперссылки</summary>
    private static readonly Regex __Regex = new(@"\[([^\]]+)\]\(([^\)]+)\)", RegexOptions.Compiled);

    /// <summary>Извлечение параметров ссылки из элементов Markdown [text](url)</summary>
    public static IEnumerable<Inline> TextToInlines(this string text)
    {
        while (text is { Length: > 0 })
            if (__Regex.Match(text) is { Success: true, Groups: { Count: 3 } groups } match &&
                Uri.TryCreate(groups[2].Value, UriKind.Absolute, out var uri))
            {
                yield return new Run { Text = text[..match.Index] };

                text = text[(match.Index + match.Length)..];

                var link = new Hyperlink { NavigateUri = uri };
                link.Inlines.Add(new Run { Text = match.Groups[1].Value });
                link.ToolTip = uri.ToString();
                link.RequestNavigate += (_, e) => System.Diagnostics.Process.Start(e.Uri.ToString());
                yield return link;
            }
            else
            {
                yield return new Run { Text = text };
                text = null;
            }
    }

    public static readonly DependencyProperty InlinesSourceProperty = DependencyProperty
       .RegisterAttached(
            "InlinesSource",
            typeof(string),
            typeof(HyperlinkText),
            new PropertyMetadata(null, InlinesSourcePropertyChanged));

    private static void InlinesSourcePropertyChanged(DependencyObject obj, DependencyPropertyChangedEventArgs e)
    {
        var inlines = obj switch
        {
            TextBlock block => block.Inlines,
            Paragraph paragraph => paragraph.Inlines,
            _ => null
        };

        inlines?.Clear();
        inlines?.AddRange(((string)e.NewValue).TextToInlines());
    }

    public static string GetInlinesSource(UIElement element) => (string)element.GetValue(InlinesSourceProperty);

    public static void SetInlinesSource(UIElement element, string value) => element.SetValue(InlinesSourceProperty, value);
}