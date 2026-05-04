using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Markup;
using System.Windows.Media.Imaging;
using Microsoft.Xaml.Behaviors;

namespace StickyHomeworks.Behaviors;

/// <summary>
/// 将 <see cref="RichTextBox"/> 的 <see cref="FlowDocument"/> 与 XAML 字符串做双向绑定；
/// 保存时把 <see cref="BlockUIContainer"/> 内的位图转为 <see cref="T:StickyHomeworks.RichTextBoxHelper.EmbeddedImageMarkerPrefix"/> 占位行，便于可靠序列化。
/// </summary>
public class RichTextBoxBindingBehavior : Behavior<RichTextBox>
{
    private static readonly IEqualityComparer<Image> ImageReferenceComparer = new ImageReferenceEqualityComparer();

    private int _applyXamlDepth;

    private sealed class ImageReferenceEqualityComparer : IEqualityComparer<Image>
    {
        public bool Equals(Image? x, Image? y) => ReferenceEquals(x, y);

        public int GetHashCode(Image obj) => RuntimeHelpers.GetHashCode(obj);
    }

    protected override void OnAttached()
    {
        base.OnAttached();
    }

    protected override void OnDetaching()
    {
        base.OnDetaching();
    }

    public string SaveDocument()
    {
        if (AssociatedObject == null)
            return string.Empty;
        return SaveDocumentWithEmbeddedImages(AssociatedObject.Document);
    }

    private string SaveDocumentWithEmbeddedImages(FlowDocument doc)
    {
        var tempDoc = new FlowDocument();
        var encodeCache = new Dictionary<Image, string>(ImageReferenceComparer);

        foreach (var block in doc.Blocks.ToList())
        {
            if (block is BlockUIContainer { Child: Image { Source: BitmapImage bitmap } img })
            {
                if (!encodeCache.TryGetValue(img, out var cachedData))
                {
                    var pngBytes = BitmapToPngBytes(bitmap);
                    if (pngBytes != null)
                    {
                        var b64 = Convert.ToBase64String(pngBytes);
                        cachedData = $"{b64}|{img.Width}|{img.Height}";
                        encodeCache[img] = cachedData;
                    }
                }

                if (cachedData != null)
                {
                    tempDoc.Blocks.Add(new Paragraph(new Run(RichTextBoxHelper.EmbeddedImageMarkerPrefix + cachedData)));
                }
                else
                {
                    Debug.WriteLine("RichTextBoxBindingBehavior: 图片无法编码为 PNG，已跳过该块。");
                    tempDoc.Blocks.Add(new Paragraph(new Run("[图片无法保存]")));
                }
            }
            else if (block is Paragraph para)
            {
                tempDoc.Blocks.Add(CloneBlock(para));
            }
            else
            {
                tempDoc.Blocks.Add(CloneBlock(block));
            }
        }

        return XamlWriter.Save(tempDoc);
    }

    private static Block CloneBlock(Block block)
    {
        var xaml = XamlWriter.Save(block);
        return (Block)XamlReader.Parse(xaml);
    }

    private static byte[]? BitmapToPngBytes(BitmapImage bitmap)
    {
        try
        {
            var encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(bitmap));
            using var ms = new MemoryStream();
            encoder.Save(ms);
            return ms.ToArray();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"RichTextBoxBindingBehavior.BitmapToPngBytes: {ex.Message}");
            return null;
        }
    }

    public static string GetDocumentXaml(DependencyObject obj)
    {
        return (string)obj.GetValue(DocumentXamlProperty);
    }

    public static void SetDocumentXaml(DependencyObject obj, string value)
    {
        obj.SetValue(DocumentXamlProperty, value);
    }

    public static readonly DependencyProperty DocumentXamlProperty = DependencyProperty.Register(
        nameof(DocumentXaml),
        typeof(string),
        typeof(RichTextBoxBindingBehavior),
        new FrameworkPropertyMetadata(
            string.Empty,
            FrameworkPropertyMetadataOptions.AffectsRender | FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
            OnDocumentXamlPropertyChanged));

    private static void OnDocumentXamlPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not RichTextBoxBindingBehavior b)
            return;

        if (b.AssociatedObject == null)
            return;

        if (b._applyXamlDepth > 0)
            return;

        b._applyXamlDepth++;
        try
        {
            var xaml = e.NewValue as string ?? string.Empty;
            b.AssociatedObject.Document = RichTextBoxHelper.ConvertDocument(xaml);
            b.AssociatedObject.Document.IsOptimalParagraphEnabled = true;
        }
        finally
        {
            b._applyXamlDepth--;
        }
    }

    public string DocumentXaml
    {
        get => (string)GetValue(DocumentXamlProperty);
        set => SetValue(DocumentXamlProperty, value);
    }
}
