using System.Text;

namespace CharManJur.Helpers;

// Turns scratch pad text into labels: **bold**, *italic*, and lines starting with "- " as bullets.
public static class ScratchMarkup
{
    public static List<View> Render(string? text)
    {
        var views = new List<View>();

        foreach (var raw in (text ?? string.Empty).Replace("\r\n", "\n").Split('\n'))
        {
            if (raw.Length == 0)
            {
                views.Add(new BoxView { HeightRequest = 8, Color = Colors.Transparent });
                continue;
            }

            bool bullet = raw.StartsWith("- ");
            var label = new Label
            {
                FontSize = 14,
                LineBreakMode = LineBreakMode.WordWrap,
                FormattedText = Spans(bullet ? raw[2..] : raw)
            };

            if (!bullet)
            {
                views.Add(label);
                continue;
            }

            var row = new Grid { ColumnDefinitions = { new ColumnDefinition(GridLength.Auto), new ColumnDefinition(GridLength.Star) }, ColumnSpacing = 8, Margin = new Thickness(10, 0, 0, 0) };
            row.Add(new Label { Text = "•", FontSize = 14, VerticalOptions = LayoutOptions.Start }, 0, 0);
            row.Add(label, 1, 0);
            views.Add(row);
        }

        return views;
    }

    // A marker only formats when it has a partner later on the line; otherwise it stays as a plain asterisk.
    private static FormattedString Spans(string line)
    {
        var result = new FormattedString();
        var text = new StringBuilder();
        bool bold = false, italic = false;

        void Flush()
        {
            if (text.Length == 0) return;
            var span = new Span { Text = text.ToString() };
            if (bold && italic) span.FontAttributes = FontAttributes.Bold | FontAttributes.Italic;
            else if (bold) span.FontAttributes = FontAttributes.Bold;
            else if (italic) span.FontAttributes = FontAttributes.Italic;
            result.Spans.Add(span);
            text.Clear();
        }

        for (int i = 0; i < line.Length; i++)
        {
            if (line[i] == '*')
            {
                bool isDouble = i + 1 < line.Length && line[i + 1] == '*';
                if (isDouble)
                {
                    if (bold || line.IndexOf("**", i + 2, StringComparison.Ordinal) >= 0)
                    {
                        Flush();
                        bold = !bold;
                        i++;
                        continue;
                    }
                }
                else if (italic || line.IndexOf('*', i + 1) >= 0)
                {
                    Flush();
                    italic = !italic;
                    continue;
                }
            }
            text.Append(line[i]);
        }

        Flush();
        return result;
    }
}