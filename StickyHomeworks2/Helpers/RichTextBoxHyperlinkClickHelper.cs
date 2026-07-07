using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Media3D;

namespace StickyHomeworks2.Helpers;

/// <summary>
/// Emoji.Wpf 等宿主下 <see cref="Hyperlink"/> 的 <c>RequestNavigate</c> 可能不可靠；
/// 在 <see cref="UIElement.PreviewMouseLeftButtonDown"/> 中根据命中位置打开确认框（与图片预览点击同一阶段）。
/// </summary>
public static class RichTextBoxHyperlinkClickHelper
{
    /// <summary>
    /// 为 true 时（例如侧栏编辑、独立编辑窗）：仅当用户按住 Ctrl 并左键点击链接时才打开链接，避免编辑时误触。
    /// </summary>
    public static readonly DependencyProperty RequireCtrlToOpenHyperlinksProperty =
        DependencyProperty.RegisterAttached(
            "RequireCtrlToOpenHyperlinks",
            typeof(bool),
            typeof(RichTextBoxHyperlinkClickHelper),
            new PropertyMetadata(false));

    public static void SetRequireCtrlToOpenHyperlinks(RichTextBox element, bool value) =>
        element.SetValue(RequireCtrlToOpenHyperlinksProperty, value);

    public static bool GetRequireCtrlToOpenHyperlinks(RichTextBox element) =>
        (bool)element.GetValue(RequireCtrlToOpenHyperlinksProperty);

    internal static RichTextBox? TryGetHostRichTextBox(Hyperlink hl)
    {
        var doc = GetFlowDocumentForHyperlink(hl);
        if (doc == null)
            return null;
        if (Keyboard.FocusedElement is RichTextBox focusRtb && ReferenceEquals(focusRtb.Document, doc))
            return focusRtb;
        for (DependencyObject? d = Mouse.DirectlyOver as DependencyObject; d != null; d = GetNavigateLookupParentStep(d))
        {
            if (d is RichTextBox rtb && ReferenceEquals(rtb.Document, doc))
                return rtb;
        }

        return null;
    }

    private static DependencyObject? GetNavigateLookupParentStep(DependencyObject d)
    {
        if (d is Visual || d is Visual3D)
            return VisualTreeHelper.GetParent(d);

        if (d is FrameworkContentElement fce && fce.Parent != null)
            return fce.Parent;

        return LogicalTreeHelper.GetParent(d);
    }

    private static FlowDocument? GetFlowDocumentForHyperlink(Hyperlink hl)
    {
        for (DependencyObject? o = hl; o != null;)
        {
            if (o is FlowDocument fd)
                return fd;
            o = o is TextElement te ? te.Parent : LogicalTreeHelper.GetParent(o);
        }

        return null;
    }
    public static bool TryHandleHyperlinkMouseLeftButtonDown(RichTextBox? richTextBox, MouseButtonEventArgs e)
    {
        if (richTextBox?.Document == null)
            return false;
        if (e.ChangedButton != MouseButton.Left)
            return false;

        TextPointer? pointer;
        try
        {
            pointer = richTextBox.GetPositionFromPoint(e.GetPosition(richTextBox), snapToText: true);
        }
        catch (InvalidOperationException)
        {
            return false;
        }

        if (pointer == null)
            return false;

        // Run / Hyperlink / Paragraph 等均派生自 FrameworkContentElement，沿 Parent 上溯即可。
        for (DependencyObject? d = pointer.Parent; d != null; d = d is FrameworkContentElement fce ? fce.Parent : null)
        {
            if (d is not Hyperlink hl || hl.NavigateUri == null)
                continue;

            if (GetRequireCtrlToOpenHyperlinks(richTextBox) &&
                (Keyboard.Modifiers & ModifierKeys.Control) != ModifierKeys.Control)
                return false;

            var linkText = new TextRange(hl.ContentStart, hl.ContentEnd).Text.Trim();
            LinkConfirmationHelper.ConfirmAndOpenLink(hl.NavigateUri.ToString(), linkText);
            e.Handled = true;
            return true;
        }

        return false;
    }
}
