using StickyHomeworks2.Views;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace StickyHomeworks2.Helpers
{
    public static class LinkConfirmationHelper
    {
        public static void ConfirmAndOpenLink(string linkUri, string linkText)
        {
            // 弹出确认窗口
            ConfirmLinkWindow confirmWindow = new ConfirmLinkWindow(linkUri, linkText);
            confirmWindow.ShowDialog();

            // 如果用户确认打开链接
            if (confirmWindow.IsConfirmed)
            {
                ProcessStartInfo psi = new ProcessStartInfo
                {
                    FileName = linkUri,
                    UseShellExecute = true
                };
                try
                {
                    Process.Start(psi);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"无法打开链接: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
    }
}
