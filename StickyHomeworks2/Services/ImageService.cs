using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.Win32;
using StickyHomeworks.Views;

namespace StickyHomeworks.Services;

public class ImageService
{
    public const string EmbeddedImageMarkerPrefix = "⌘IMG:";

    public const long MaxImageFileSize = 10 * 1024 * 1024; // 10MB

    public const double DefaultImageWidth = 300.0;

    public const double MaxCompressedWidth = 800.0;

    public const int JpegQuality = 85;

    private static readonly string[] AllowedImageExtensions = { ".png", ".jpg", ".jpeg", ".bmp", ".gif" };

    public string InsertImageFromFile(RichTextBox richTextBox, string filePath)
    {
        if (richTextBox == null || string.IsNullOrEmpty(filePath))
            return string.Empty;

        if (!File.Exists(filePath))
            return string.Empty;

        if (!AllowedImageExtensions.Contains(Path.GetExtension(filePath).ToLowerInvariant()))
            return string.Empty;

        var fileInfo = new FileInfo(filePath);
        if (fileInfo.Length > MaxImageFileSize)
            return string.Empty;

        var bitmap = LoadBitmapFromFile(filePath);
        if (bitmap == null)
            return string.Empty;

        var image = CreateImageElement(bitmap, DefaultImageWidth);
        var container = CreateBlockContainer(image);

        richTextBox.Document.Blocks.Add(container);
        richTextBox.CaretPosition = container.ElementEnd;
        richTextBox.Focus();

        return filePath;
    }

    public bool TryShowFileDialog(Window owner, out string insertedPath)
    {
        insertedPath = null;
        var dlg = new OpenFileDialog
        {
            Filter = "图片文件|*.png;*.jpg;*.jpeg;*.bmp;*.gif|所有文件|*.*",
            Title = "选择要插入的图片"
        };

        if (dlg.ShowDialog(owner) == true && File.Exists(dlg.FileName))
        {
            insertedPath = dlg.FileName;
            return true;
        }
        return false;
    }

    public BitmapImage LoadBitmapFromFile(string filePath)
    {
        try
        {
            using var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
            var bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.StreamSource = fs;
            bitmap.CacheOption = BitmapCacheOption.OnLoad;
            bitmap.EndInit();
            bitmap.Freeze();
            return bitmap;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"ImageService.LoadBitmapFromFile: {ex.Message}");
            return null;
        }
    }

    public Image CreateImageElement(BitmapImage bitmap, double displayWidth)
    {
        if (bitmap == null) return null;

        var image = new Image
        {
            Source = bitmap,
            Stretch = Stretch.Uniform,
            Width = displayWidth,
            Tag = displayWidth
        };

        if (bitmap.PixelWidth > 0)
        {
            image.Height = displayWidth * bitmap.PixelHeight / bitmap.PixelWidth;
        }

        return image;
    }

    public BlockUIContainer CreateBlockContainer(Image image)
    {
        var container = new BlockUIContainer(image);
        container.SetValue(Paragraph.MarginProperty, new Thickness(0, 4, 0, 4));
        return container;
    }

    public string EncodeImageToBase64(BitmapImage bitmap, double width, double height)
    {
        var compressed = CompressBitmap(bitmap);
        var jpegBytes = BitmapToJpegBytes(compressed);
        if (jpegBytes == null) return null;

        var b64 = Convert.ToBase64String(jpegBytes);
        return $"{b64}|{width}|{height}";
    }

    public BitmapImage CompressBitmap(BitmapImage original)
    {
        if (original == null) return null;

        if (original.PixelWidth <= MaxCompressedWidth && original.PixelHeight <= MaxCompressedWidth)
            return original;

        var scale = MaxCompressedWidth / original.PixelWidth;
        var newWidth = (int)(original.PixelWidth * scale);
        var newHeight = (int)(original.PixelHeight * scale);

        var resized = new TransformedBitmap(original, new ScaleTransform(scale, scale));

        var encoder = new JpegBitmapEncoder { QualityLevel = JpegQuality };
        encoder.Frames.Add(BitmapFrame.Create(resized));
        using var ms = new MemoryStream();
        encoder.Save(ms);

        ms.Position = 0;
        var result = new BitmapImage();
        result.BeginInit();
        result.StreamSource = ms;
        result.CacheOption = BitmapCacheOption.OnLoad;
        result.EndInit();
        result.Freeze();
        return result;
    }

    public bool TryDecodeBase64ToImage(string markerText, out Image image)
    {
        image = null;

        if (string.IsNullOrEmpty(markerText) || !markerText.StartsWith(EmbeddedImageMarkerPrefix, StringComparison.Ordinal))
            return false;

        var data = markerText.Substring(EmbeddedImageMarkerPrefix.Length);
        var parts = data.Split('|');
        if (parts.Length < 3) return false;

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

            image = new Image
            {
                Source = bitmap,
                Stretch = Stretch.Uniform,
                Width = savedWidth,
                Height = savedHeight,
                Tag = savedWidth
            };

            return true;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"ImageService.TryDecodeBase64ToImage: {ex.Message}");
            return false;
        }
    }

    public BlockUIContainer CreateContainerFromDecodedImage(Image image)
    {
        var container = new BlockUIContainer(image);
        container.SetValue(Paragraph.MarginProperty, new Thickness(0, 4, 0, 4));
        return container;
    }

    public bool ShowResizeDialog(Window owner, Image image)
    {
        if (image == null) return false;

        var originalWidth = image.Tag is double d ? d : image.Width;
        var currentZoom = Math.Round(image.Width / originalWidth * 100);

        var dialog = new ImageResizeDialog(
            currentZoom,
            (zoom) => ApplyZoom(image, originalWidth, zoom),
            () => ApplyZoom(image, originalWidth, currentZoom)
        ) { Owner = owner };

        var result = dialog.ShowDialog();
        if (result != true)
        {
            ApplyZoom(image, originalWidth, currentZoom);
        }
        return result == true;
    }

    public void ApplyZoom(Image image, double originalWidth, double zoomPercent)
    {
        if (image == null) return;
        var newWidth = originalWidth * zoomPercent / 100.0;
        var ratio = image.Height > 0 && image.Width > 0 ? image.Height / image.Width : 1.0;
        image.Width = newWidth;
        image.Height = newWidth * ratio;
    }

    public bool IsImageClick(DependencyObject hitObject, out Image clickedImage)
    {
        clickedImage = hitObject switch
        {
            Image img => img,
            TextBlock tb when VisualTreeHelper.GetParent(tb) is Image img => img,
            _ => null
        };
        return clickedImage != null;
    }

    private byte[] BitmapToJpegBytes(BitmapImage bitmap)
    {
        try
        {
            var encoder = new JpegBitmapEncoder { QualityLevel = JpegQuality };
            encoder.Frames.Add(BitmapFrame.Create(bitmap));
            using var ms = new MemoryStream();
            encoder.Save(ms);
            return ms.ToArray();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"ImageService.BitmapToJpegBytes: {ex.Message}");
            return null;
        }
    }

    public Paragraph CreatePlaceholderParagraph(string message)
    {
        return new Paragraph(new Run(message));
    }
}
