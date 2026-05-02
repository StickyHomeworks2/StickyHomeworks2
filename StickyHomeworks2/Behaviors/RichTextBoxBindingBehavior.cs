using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.Xaml.Behaviors;

namespace StickyHomeworks.Behaviors;

public class RichTextBoxBindingBehavior : Behavior<RichTextBox>
{
    private static HashSet<Thread> _recursionProtection = new HashSet<Thread>();
    private bool _isLoading;
    private const string ImageMarker = "⌘IMG:";
    private readonly Dictionary<Image, string> _imageCache = new();

    protected override void OnAttached()
    {
        base.OnAttached();
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

        foreach (var block in doc.Blocks.ToList())
        {
            if (block is BlockUIContainer { Child: Image { Source: BitmapImage bitmap } img })
            {
                if (!_imageCache.TryGetValue(img, out var cachedData))
                {
                    var pngBytes = BitmapToPngBytes(bitmap);
                    if (pngBytes != null)
                    {
                        var b64 = Convert.ToBase64String(pngBytes);
                        cachedData = $"{b64}|{img.Width}|{img.Height}";
                        _imageCache[img] = cachedData;
                    }
                }
                
                if (cachedData != null)
                {
                    tempDoc.Blocks.Add(new Paragraph(new Run(ImageMarker + cachedData)));
                }
                else
                {
                    tempDoc.Blocks.Add(new Paragraph());
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
        catch
        {
            return null;
        }
    }

    protected override void OnDetaching()
    {
        base.OnDetaching();
    }

    public static string GetDocumentXaml(DependencyObject obj)
    {
        return (string)obj.GetValue(DocumentXamlProperty);
    }

    public static void SetDocumentXaml(DependencyObject obj, string value)
    {
        _recursionProtection.Add(Thread.CurrentThread);
        obj.SetValue(DocumentXamlProperty, value);
        _recursionProtection.Remove(Thread.CurrentThread);
    }

    public static readonly DependencyProperty DocumentXamlProperty = DependencyProperty.Register(
        nameof(DocumentXaml), typeof(string), typeof(RichTextBoxBindingBehavior), new FrameworkPropertyMetadata(
            "",
            FrameworkPropertyMetadataOptions.AffectsRender | FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
            (obj, e) =>
            {
                if (_recursionProtection.Contains(Thread.CurrentThread))
                    return;

                if (obj is not RichTextBoxBindingBehavior b)
                    return;

                b._isLoading = true;
                b.AssociatedObject.Document = RichTextBoxHelper.ConvertDocument(GetDocumentXaml(b));
                b.AssociatedObject.Document.IsOptimalParagraphEnabled = true;
                b._isLoading = false;
            }
        ));

    public string DocumentXaml
    {
        get => (string)GetValue(DocumentXamlProperty);
        set => SetValue(DocumentXamlProperty, value);
    }
}
