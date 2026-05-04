using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;

namespace StickyHomeworks2.Helpers;

/// <summary>
/// Emoji.Wpf 等宿主下 <see cref="Hyperlink"/> 的 <c>RequestNavigate</c> 可能不可靠；
/// 在 <see cref="UIElement.PreviewMouseLeftButtonDown"/> 中根据命中位置打开确认框（与图片预览点击同一阶段）。
/// </summary>
public static class RichTextBoxHyperlinkClickHelper
{
    /// <summary>
    /// 若点击落在带 <see cref="Hyperlink.NavigateUri"/> 的链接上，则弹出确认并视结果打开系统浏览器；此时将 <paramref name="e"/>.Handled 置为 true。
    /// </summary>
    /// <returns>是否已按链接处理（含已弹出确认框）。</returns>
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

            var linkText = new TextRange(hl.ContentStart, hl.ContentEnd).Text.Trim();
            LinkConfirmationHelper.ConfirmAndOpenLink(hl.NavigateUri.ToString(), linkText);
            e.Handled = true;
            return true;
        }

        return false;
    }
}
