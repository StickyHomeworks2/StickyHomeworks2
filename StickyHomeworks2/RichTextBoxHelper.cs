using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using StickyHomeworks2.Helpers;

namespace StickyHomeworks;

public static class RichTextBoxHelper
{
    /// <summary>作业富文本中超链接的默认前景色（FlowDocument 内不继承外层隐式样式，需在加载/插入时显式应用）。</summary>
    public static Brush DefaultHyperlinkForeground { get; } = CreateDefaultHyperlinkBrush();

    private static Brush CreateDefaultHyperlinkBrush()
    {
        var b = new SolidColorBrush(Color.FromRgb(0x1A, 0x73, 0xE8));
        b.Freeze();
        return b;
    }

    /// <summary>与 <see cref="T:StickyHomeworks.Behaviors.RichTextBoxBindingBehavior"/> 保存逻辑共用的行内图片占位前缀。</summary>
    public const string EmbeddedImageMarkerPrefix = "⌘IMG:";

    public static FlowDocument ConvertDocument(string xaml)
    {
        if (string.IsNullOrEmpty(xaml))
        {
            var doc = new FlowDocument();
            doc.Blocks.Add(new Paragraph());
            doc.IsOptimalParagraphEnabled = true;
            return doc;
        }

        try
        {
            var cleanedXaml = xaml.Replace("<BitmapImage />", string.Empty, StringComparison.Ordinal)
                                  .Replace("<BitmapImage/>", string.Empty, StringComparison.Ordinal);

            var stream = new MemoryStream(Encoding.UTF8.GetBytes(cleanedXaml));
            var doc = (FlowDocument)XamlReader.Load(stream);
            doc.IsOptimalParagraphEnabled = true;
            RestoreEmbeddedImages(doc);
            RestoreEmojiRendering(doc);
            ApplyHyperlinkPresentation(doc);
            return doc;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"RichTextBoxHelper.ConvertDocument failed: {ex.Message}");
            var doc = new FlowDocument();
            var para = new Paragraph();
            doc.IsOptimalParagraphEnabled = true;
            para.Inlines.Add(new Run(xaml));
            doc.Blocks.Add(para);
            return doc;
        }
    }

    private static void RestoreEmojiRendering(FlowDocument doc)
    {
        Emoji.Wpf.FlowDocumentExtensions.SubstituteGlyphs(doc);

        foreach (var emoji in FindEmojiInlines(doc))
        {
            emoji.Foreground = Brushes.Black;
        }
    }

    private static IEnumerable<Emoji.Wpf.EmojiInline> FindEmojiInlines(FlowDocument doc)
    {
        for (var pointer = doc.ContentStart;
             pointer != null && pointer.CompareTo(doc.ContentEnd) < 0;
             pointer = pointer.GetNextContextPosition(LogicalDirection.Forward))
        {
            if (pointer.GetPointerContext(LogicalDirection.Forward) == TextPointerContext.ElementStart &&
                pointer.GetAdjacentElement(LogicalDirection.Forward) is Emoji.Wpf.EmojiInline emoji)
            {
                yield return emoji;
            }
        }
    }

    private static void RestoreEmbeddedImages(FlowDocument doc)
    {
        var replacements = new List<(Paragraph Para, BlockUIContainer Container)>();
        
        foreach (var block in doc.Blocks.ToList())
        {
            if (block is Paragraph para)
            {
                var runs = para.Inlines.OfType<Run>().ToList();
                foreach (var run in runs)
                {
                    if (run.Text.StartsWith(EmbeddedImageMarkerPrefix, StringComparison.Ordinal))
                    {
                        var data = run.Text.Substring(EmbeddedImageMarkerPrefix.Length);
                        var parts = data.Split('|');
                        if (parts.Length < 3)
                            continue;

                        try
                        {
                            var b64 = parts[0];
                            var savedWidth = double.Parse(parts[1]);
                            var savedHeight = double.Parse(parts[2]);

                            var bytes = Convert.FromBase64String(b64);
                            var ms = new MemoryStream(bytes);
                            var bitmap = new BitmapImage();
                            bitmap.BeginInit();
                            bitmap.StreamSource = ms;
                            bitmap.CacheOption = BitmapCacheOption.OnLoad;
                            bitmap.EndInit();
                            bitmap.Freeze();

                            var img = new Image
                            {
                                Source = bitmap,
                                Stretch = Stretch.Uniform,
                                Width = savedWidth,
                                Height = savedHeight,
                                Tag = savedWidth
                            };

                            var container = new BlockUIContainer(img);
                            container.SetValue(Paragraph.MarginProperty, new Thickness(0, 4, 0, 4));
                            
                            replacements.Add((para, container));
                            Debug.WriteLine($"恢复图片: {savedWidth}x{savedHeight}, Base64长度={b64.Length}");
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine($"恢复图片失败: {ex.Message}");
                        }
                        break;
                    }
                }
            }
        }

        foreach (var (para, container) in replacements)
        {
            doc.Blocks.Remove(para);
            doc.Blocks.Add(container);
        }
    }

    /// <summary>
    /// FlowDocument 中的 <see cref="Hyperlink"/> 不参与父级控件资源查找，隐式样式无效；
    /// 在从 XAML 加载后对每个链接设置前景色与「确认后打开」行为。
    /// </summary>
    public static void ApplyHyperlinkPresentation(FlowDocument doc)
    {
        foreach (var block in doc.Blocks)
            VisitBlockForHyperlinks(block);
    }

    private static void VisitBlockForHyperlinks(Block block)
    {
        switch (block)
        {
            case Paragraph p:
                foreach (var inline in p.Inlines)
                    VisitInlineForHyperlinks(inline);
                break;
            case Section s:
                foreach (var b in s.Blocks)
                    VisitBlockForHyperlinks(b);
                break;
            case Table t:
                foreach (var rg in t.RowGroups)
                foreach (var row in rg.Rows)
                foreach (var cell in row.Cells)
                foreach (var b in cell.Blocks)
                    VisitBlockForHyperlinks(b);
                break;
            case List list:
                foreach (var item in list.ListItems)
                foreach (var b in item.Blocks)
                    VisitBlockForHyperlinks(b);
                break;
        }
    }

    private static void VisitInlineForHyperlinks(Inline inline)
    {
        switch (inline)
        {
            case Hyperlink h:
                h.Foreground = DefaultHyperlinkForeground;
                HyperlinkBehavior.SetConfirmNavigation(h, true);
                foreach (var child in h.Inlines)
                    VisitInlineForHyperlinks(child);
                break;
            case Span span:
                foreach (var child in span.Inlines)
                    VisitInlineForHyperlinks(child);
                break;
        }
    }
}
