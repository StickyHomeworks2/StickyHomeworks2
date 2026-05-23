using System;
using System.Collections.Generic;
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
using ElysiaFramework;
using Microsoft.Extensions.Logging;
using StickyHomeworks.Services;
using StickyHomeworks2.Helpers;

namespace StickyHomeworks;

public static class RichTextBoxHelper
{
    private static readonly Lazy<ImageService> _imageService = new(() => new ImageService(AppEx.GetService<ILogger<ImageService>>()));

    /// <summary>作业富文本中超链接的默认前景色（FlowDocument 内不继承外层隐式样式，需在加载/插入时显式应用）。</summary>
    public static Brush DefaultHyperlinkForeground { get; } = CreateDefaultHyperlinkBrush();

    private static Brush CreateDefaultHyperlinkBrush()
    {
        var b = new SolidColorBrush(Color.FromRgb(0x1A, 0x73, 0xE8));
        b.Freeze();
        return b;
    }

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
        var blocksToRemove = new List<Block>();
        var blocksToAdd = new List<Block>();

        foreach (var block in doc.Blocks.ToList())
        {
            if (block is Paragraph para)
            {
                var runs = para.Inlines.OfType<Run>().ToList();

                var hasImageMarker = runs.Any(r => r.Text.StartsWith(ImageService.EmbeddedImageMarkerPrefix, StringComparison.Ordinal));
                var hasOnlyImageMarkers = runs.All(r => r.Text.StartsWith(ImageService.EmbeddedImageMarkerPrefix, StringComparison.Ordinal))
                    && para.Inlines.All(i => i is Run);

                if (hasOnlyImageMarkers && hasImageMarker)
                {
                    foreach (var run in runs)
                    {
                        if (run.Text.StartsWith(ImageService.EmbeddedImageMarkerPrefix, StringComparison.Ordinal))
                        {
                            if (_imageService.Value.TryDecodeBase64ToImage(run.Text, out var image))
                            {
                                var container = _imageService.Value.CreateContainerFromDecodedImage(image);
                                replacements.Add((para, container));
                                Debug.WriteLine($"恢复图片: {image.Width}x{image.Height}");
                            }
                        }
                    }
                }
                else if (hasImageMarker)
                {
                    var inlineContainers = new List<BlockUIContainer>();
                    foreach (var run in runs)
                    {
                        if (run.Text.StartsWith(ImageService.EmbeddedImageMarkerPrefix, StringComparison.Ordinal))
                        {
                            if (_imageService.Value.TryDecodeBase64ToImage(run.Text, out var image))
                            {
                                inlineContainers.Add(_imageService.Value.CreateContainerFromDecodedImage(image));
                                run.Text = string.Empty;
                                Debug.WriteLine($"恢复行内图片: {image.Width}x{image.Height}");
                            }
                            else
                            {
                                run.Text = "[图片无法恢复]";
                            }
                        }
                    }

                    if (inlineContainers.Any())
                    {
                        var idx = -1;
                        for (var i = 0; i < doc.Blocks.Count; i++)
                        {
                            if (doc.Blocks.ElementAt(i) == para)
                            {
                                idx = i;
                                break;
                            }
                        }

                        var textRuns = para.Inlines.OfType<Run>().Any(r => !string.IsNullOrEmpty(r.Text))
                            || para.Inlines.Any(i => i is not Run);

                        if (textRuns)
                        {
                            blocksToAdd.InsertRange(0, inlineContainers);
                            blocksToRemove.Add(para);
                        }
                        else
                        {
                            foreach (var container in inlineContainers)
                            {
                                replacements.Add((para, container));
                            }
                        }
                    }
                }
            }
        }

        foreach (var (para, container) in replacements)
        {
            doc.Blocks.Remove(para);
            doc.Blocks.Add(container);
        }

        if (blocksToRemove.Any())
        {
            foreach (var block in blocksToRemove)
            {
                doc.Blocks.Remove(block);
            }
            for (var i = blocksToAdd.Count - 1; i >= 0; i--)
            {
                var firstBlock = doc.Blocks.FirstOrDefault();
                if (firstBlock != null)
                {
                    doc.Blocks.Remove(blocksToAdd[i]);
                    doc.Blocks.InsertBefore(firstBlock, blocksToAdd[i]);
                }
                else
                {
                    doc.Blocks.Add(blocksToAdd[i]);
                }
            }
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
