using System;
using System.Windows;

namespace StickyHomeworks2.Views;

/// <summary>
/// 插入超链接：收集 URL 与可选显示文字，并校验协议白名单。
/// </summary>
public partial class InsertLinkWindow : Window
{
    public Uri? ResultUri { get; private set; }
    public string ResultDisplayText { get; private set; } = string.Empty;

    public InsertLinkWindow(string? defaultDisplayText)
    {
        InitializeComponent();
        if (!string.IsNullOrWhiteSpace(defaultDisplayText))
            DisplayTextBox.Text = defaultDisplayText.Trim();
        UrlTextBox.Focus();
    }

    private void OkButton_OnClick(object sender, RoutedEventArgs e)
    {
        var raw = UrlTextBox.Text.Trim();
        if (!TryCreateAllowedUri(raw, out var uri, out var err))
        {
            MessageBox.Show(this, err, "地址无效", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var display = DisplayTextBox.Text.Trim();
        if (string.IsNullOrEmpty(display))
            display = uri.ToString();

        ResultUri = uri;
        ResultDisplayText = display;
        DialogResult = true;
        Close();
    }

    private void CancelButton_OnClick(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    /// <summary>
    /// 仅允许 http、https、mailto 的绝对 URI。
    /// </summary>
    public static bool TryCreateAllowedUri(string input, out Uri uri, out string errorMessage)
    {
        uri = null!;
        errorMessage = string.Empty;
        if (string.IsNullOrWhiteSpace(input))
        {
            errorMessage = "请输入链接地址。";
            return false;
        }

        var trimmed = input.Trim();

        if (!Uri.TryCreate(trimmed, UriKind.Absolute, out var u))
        {
            errorMessage = "无法解析为有效地址。";
            return false;
        }

        if (u.Scheme.Equals(Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase) ||
            u.Scheme.Equals(Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase))
        {
            if (string.IsNullOrEmpty(u.Host))
            {
                errorMessage = "http(s) 链接必须包含主机名。";
                return false;
            }

            uri = u;
            return true;
        }

        if (u.Scheme.Equals("mailto", StringComparison.OrdinalIgnoreCase))
        {
            var path = u.AbsolutePath;
            var userInfo = u.UserInfo;
            var host = u.Host;
            if (string.IsNullOrEmpty(userInfo) && string.IsNullOrEmpty(host) && string.IsNullOrEmpty(path))
            {
                errorMessage = "mailto: 后请填写邮箱地址。";
                return false;
            }

            uri = u;
            return true;
        }

        errorMessage = "仅支持 http、https 与 mailto 协议。";
        return false;
    }
}
