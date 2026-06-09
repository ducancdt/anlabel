using System.Globalization;
using System.Windows;
using System.Windows.Media;
using Brush = System.Windows.Media.Brush;
using Brushes = System.Windows.Media.Brushes;
using FontFamily = System.Windows.Media.FontFamily;
using FlowDirection = System.Windows.FlowDirection;
using ANLAbel.Core.Enums;
using ANLAbel.Core.Models;

namespace ANLAbel.Printing.RenderPipeline;

public static class TextBoxOverflowDetector
{
    public const double HorizontalPaddingDip = 0;

    public static bool IsOverflowing(LabelObject item, string value, double widthDip, double heightDip, double pixelsPerDip = 1.0)
    {
        if (item.Type != ObjectType.TextBox || widthDip <= 0 || heightDip <= 0)
        {
            return false;
        }

        var contentWidth = Math.Max(1, widthDip - HorizontalPaddingDip * 2);
        var wrappedText = WrapTextToBox(item, value, contentWidth, pixelsPerDip);
        var lineCount = CountDisplayLines(wrappedText);
        var lineHeight = CreateFormattedText(item, "Ag", Brushes.Black, pixelsPerDip).Height;
        var requiredHeight = lineCount * lineHeight;

        return requiredHeight > heightDip + lineHeight * 0.2;
    }

    public static FormattedText CreateFormattedText(LabelObject item, string value, Brush brush, double pixelsPerDip = 1.0)
    {
        var typeface = new Typeface(
            new FontFamily(item.Style.FontFamily),
            item.Style.Italic ? FontStyles.Italic : FontStyles.Normal,
            item.Style.Bold ? FontWeights.Bold : FontWeights.Normal,
            FontStretches.Normal);

        var text = new FormattedText(
            value,
            CultureInfo.CurrentCulture,
            FlowDirection.LeftToRight,
            typeface,
            item.Style.FontSizePt * 96.0 / 72.0,
            brush,
            pixelsPerDip)
        {
            TextAlignment = item.Style.Alignment switch
            {
                TextAlignmentMode.Center => TextAlignment.Center,
                TextAlignmentMode.Right => TextAlignment.Right,
                _ => TextAlignment.Left
            }
        };

        if (item.Style.Underline)
        {
            text.SetTextDecorations(TextDecorations.Underline);
        }

        return text;
    }

    public static string WrapTextToBox(LabelObject item, string value, double widthDip, double pixelsPerDip = 1.0)
    {
        if (string.IsNullOrEmpty(value))
        {
            return value;
        }

        widthDip = Math.Max(1, widthDip);
        var output = new System.Text.StringBuilder(value.Length + 16);
        var line = new System.Text.StringBuilder();

        for (var i = 0; i < value.Length; i++)
        {
            var current = value[i];
            if (current == '\r')
            {
                continue;
            }

            if (current == '\n')
            {
                AppendLine(output, line);
                continue;
            }

            line.Append(current);
            if (MeasureLineWidth(item, line.ToString(), pixelsPerDip) <= widthDip)
            {
                continue;
            }

            var overflowingChar = line[^1];
            line.Length--;
            if (line.Length > 0)
            {
                TrimTrailingSpaces(line);
                output.Append(line);
                output.AppendLine();
                line.Clear();
            }

            if (!char.IsWhiteSpace(overflowingChar))
            {
                line.Append(overflowingChar);
            }
        }

        output.Append(line);
        return output.ToString();
    }

    private static double MeasureLineWidth(LabelObject item, string value, double pixelsPerDip)
    {
        if (string.IsNullOrEmpty(value))
        {
            return 0;
        }

        return CreateFormattedText(item, value, Brushes.Black, pixelsPerDip).WidthIncludingTrailingWhitespace;
    }

    private static void AppendLine(System.Text.StringBuilder output, System.Text.StringBuilder line)
    {
        output.Append(line);
        output.AppendLine();
        line.Clear();
    }

    private static void TrimTrailingSpaces(System.Text.StringBuilder value)
    {
        while (value.Length > 0 && char.IsWhiteSpace(value[^1]))
        {
            value.Length--;
        }
    }

    private static int CountDisplayLines(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return 1;
        }

        var count = 1;
        foreach (var ch in value)
        {
            if (ch == '\n')
            {
                count++;
            }
        }

        return value.EndsWith(Environment.NewLine, StringComparison.Ordinal)
            ? Math.Max(1, count - 1)
            : count;
    }
}
