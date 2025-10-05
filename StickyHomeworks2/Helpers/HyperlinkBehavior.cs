using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Navigation;

namespace StickyHomeworks2.Helpers
{
    public static class HyperlinkBehavior
    {
        public static readonly DependencyProperty ConfirmNavigationProperty =
            DependencyProperty.RegisterAttached(
                "ConfirmNavigation",
                typeof(bool),
                typeof(HyperlinkBehavior),
                new PropertyMetadata(false, OnConfirmNavigationChanged));

        public static void SetConfirmNavigation(DependencyObject element, bool value)
        {
            element.SetValue(ConfirmNavigationProperty, value);
        }

        public static bool GetConfirmNavigation(DependencyObject element)
        {
            return (bool)element.GetValue(ConfirmNavigationProperty);
        }

        private static void OnConfirmNavigationChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var hyperlink = d as Hyperlink;
            if (hyperlink != null)
            {
                hyperlink.RequestNavigate += Hyperlink_RequestNavigate;
            }
        }

        private static void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            // 阻止默认行为
            e.Handled = true;

            // 获取链接内容
            string linkUri = e.Uri.ToString();
            string linkText = GetHyperlinkText((Hyperlink)sender);

            // 调用通用方法确认并打开链接
            LinkConfirmationHelper.ConfirmAndOpenLink(linkUri, linkText);
        }

        private static string GetHyperlinkText(Hyperlink hyperlink)
        {
            if (hyperlink != null && hyperlink.Inlines.Count > 0)
            {
                var run = hyperlink.Inlines.FirstInline as Run;
                if (run != null)
                {
                    return run.Text;
                }
            }
            return string.Empty;
        }
    }
}

