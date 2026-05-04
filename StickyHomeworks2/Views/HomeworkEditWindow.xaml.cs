using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using ElysiaFramework;
using Microsoft.Win32;
using StickyHomeworks;
using StickyHomeworks.Services;
using StickyHomeworks.ViewModels;
using StickyHomeworks2.Helpers;
using StickyHomeworks2.Views;

namespace StickyHomeworks.Views;

/// <summary>
/// HomeworkEditWindow.xaml 的交互逻辑
/// </summary>
public partial class HomeworkEditWindow : Window, INotifyPropertyChanged
{
    private RichTextBox _relatedRichTextBox = new();
    private System.Windows.Threading.DispatcherTimer? _selectionChangedDebounceTimer;
    public MainWindow MainWindow { get; }
    public SettingsService SettingsService { get; }
    public TimeMachineService TimeMachineService { get; }

    public HomeworkEditViewModel ViewModel { get; } = new();

    public bool IsOpened { get; set; } = false;

    public event EventHandler? EditingFinished;

    public event EventHandler? SubjectChanged;

    public void TryOpen()
    {
        if (IsOpened)
            return;
        Show();
        Activate();
        IsOpened = true;
    }

    public void TryClose()
    {
        if (!IsOpened)
            return;
        IsOpened = false;
        Hide();
    }

    public RichTextBox RelatedRichTextBox
    {
        get => _relatedRichTextBox;
        set
        {
            UnregisterOldTextBox(_relatedRichTextBox);
            RegisterNewTextBox(value);
            _relatedRichTextBox = value;
            ViewModel.BeforeTextPointerStart = null;
            ViewModel.BeforeTextPointerEnd = null;
            OnPropertyChanged();
        }
    }

    public HomeworkEditWindow(MainWindow mainWindow, SettingsService settingsService, TimeMachineService timeMachineService)
    {
        MainWindow = mainWindow;
        SettingsService = settingsService;
        TimeMachineService = timeMachineService;
        DataContext = this;
        InitializeComponent();
        ViewModel.PropertyChanged += ViewModelOnPropertyChanged;
        Loaded += HomeworkEditWindow_Loaded;
    }

    private static readonly string[] CommonEmojis = new[]
    {
        "😀", "😃", "😄", "😁", "😅", "😂", "🤣", "😊", "😇", "🙂",
        "😉", "😌", "😍", "🥰", "😘", "😗", "😙", "😚", "😋", "😛",
        "😝", "😜", "🤪", "🤨", "🧐", "🤓", "😎", "🤩", "🥳", "😏",
        "📚", "📖", "📝", "✏️", "📓", "📔", "📒", "📕", "📗", "📘",
        "✅", "❌", "⚠️", "🔔", "📌", "📎", "📁", "📂", "🗂️", "📅",
        "⏰", "⏱️", "⏲️", "🕐", "🕑", "🕒", "🕓", "🕔", "🕕", "🕖",
        "👍", "👎", "👏", "🙌", "🤝", "💪", "🙏", "✌️", "🤞", "👌",
        "❤️", "🧡", "💛", "💚", "💙", "💜", "🖤", "🤍", "💯", "💥",
        "⭐", "🌟", "✨", "🎉", "🎊", "🎈", "🎁", "🏆", "🥇", "🎯",
        "🔥", "💡", "📢", "💬", "💭", "🗯️", "♨️", "🔴", "🟠", "🟡"
    };

    private void HomeworkEditWindow_Loaded(object? sender, RoutedEventArgs e)
    {
        foreach (var emoji in CommonEmojis)
        {
            var btn = new Button
            {
                Content = emoji,
                Style = (Style)FindResource("MaterialDesignFlatButton"),
                Margin = new Thickness(2),
                Padding = new Thickness(4),
                FontSize = 20,
                MinWidth = 36,
                MinHeight = 36
            };
            btn.Click += (s, e) =>
            {
                InsertEmoji(emoji);
                EmojiPickerPopup.IsOpen = false;
            };
            EmojiPanel.Children.Add(btn);
        }
    }

    private void InsertEmoji(string emoji)
    {
        var richTextBox = RelatedRichTextBox;
        if (richTextBox == null)
            return;
        
        var textRange = richTextBox.Selection;
        if (textRange != null)
        {
            var start = textRange.Start.GetInsertionPosition(LogicalDirection.Forward);
            textRange.Text = emoji;
            var end = textRange.End.GetInsertionPosition(LogicalDirection.Backward);
            var emojiRange = new TextRange(start, end);
            emojiRange.ClearAllProperties();
            textRange.Select(end, end);
            richTextBox.Focus();
        }
    }

    private void ButtonInsertLink_OnClick(object sender, RoutedEventArgs e)
    {
        if (RelatedRichTextBox?.Document == null)
            return;

        var rtb = RelatedRichTextBox;
        var sel = rtb.Selection;

        if (!sel.IsEmpty)
        {
            if (sel.Start.Paragraph != sel.End.Paragraph)
            {
                MessageBox.Show(this,
                    "无法在跨多个段落的选区插入链接。请将选区限定在同一段落内，或分多次插入。",
                    "插入链接",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
                return;
            }
        }
        else if (rtb.CaretPosition.Paragraph == null)
        {
            MessageBox.Show(this,
                "请将光标放在文本段落中再插入链接。",
                "插入链接",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
            return;
        }

        var defaultDisplay = sel.IsEmpty ? null : sel.Text;

        var dlg = new InsertLinkWindow(defaultDisplay);
        if (Application.Current?.MainWindow != null && Application.Current.MainWindow.IsVisible)
            dlg.Owner = Application.Current.MainWindow;
        else
            dlg.Owner = this;

        if (dlg.ShowDialog() != true || dlg.ResultUri == null)
            return;

        TextPointer insertPos;
        if (!sel.IsEmpty)
        {
            var clear = new TextRange(sel.Start, sel.End);
            insertPos = clear.Start;
            clear.Text = string.Empty;
            insertPos = clear.Start;
        }
        else
        {
            insertPos = rtb.CaretPosition;
        }

        if (insertPos.Paragraph == null)
        {
            MessageBox.Show(this, "无法确定插入位置。", "插入链接", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var hyperlink = new Hyperlink(new Run(dlg.ResultDisplayText))
        {
            NavigateUri = dlg.ResultUri,
            Foreground = RichTextBoxHelper.DefaultHyperlinkForeground
        };
        HyperlinkBehavior.SetConfirmNavigation(hyperlink, true);

        InsertInlineAtTextPointer(insertPos, hyperlink);
        rtb.CaretPosition = hyperlink.ContentEnd;
        rtb.Focus();
        SaveDocumentToHomework();
    }

    /// <summary>
    /// 若指针位于 Run 中间则拆分为两段，以便在边界插入内联元素。
    /// </summary>
    private static TextPointer PrepareInsertionPosition(TextPointer position)
    {
        if (position.Parent is not Run run || string.IsNullOrEmpty(run.Text))
            return position;

        var before = new TextRange(run.ContentStart, position).Text ?? string.Empty;
        var beforeLen = before.Length;
        if (beforeLen <= 0 || beforeLen >= run.Text.Length)
            return position;

        var tailText = run.Text.Substring(beforeLen);
        run.Text = run.Text.Substring(0, beforeLen);
        var tailRun = new Run(tailText);

        switch (run.Parent)
        {
            case Paragraph p:
                p.Inlines.InsertAfter(run, tailRun);
                break;
            case Hyperlink h:
                h.Inlines.InsertAfter(run, tailRun);
                break;
            case Span span:
                span.Inlines.InsertAfter(run, tailRun);
                break;
            default:
                return position;
        }

        return run.ContentEnd;
    }

    private static void InsertInlineAtTextPointer(TextPointer pos, Inline inline)
    {
        pos = PrepareInsertionPosition(pos);

        var backward = pos.GetAdjacentElement(LogicalDirection.Backward) as Inline;
        var forward = pos.GetAdjacentElement(LogicalDirection.Forward) as Inline;

        InlineCollection? coll = null;
        for (var p = pos.Parent; p != null; p = LogicalTreeHelper.GetParent(p))
        {
            if (p is Hyperlink hl)
            {
                coll = hl.Inlines;
                break;
            }

            if (p is Paragraph paragraph)
            {
                coll = paragraph.Inlines;
                break;
            }
        }

        if (coll == null)
            return;

        if (backward != null && coll.Contains(backward))
        {
            coll.InsertAfter(backward, inline);
            return;
        }

        if (forward != null && coll.Contains(forward))
        {
            coll.InsertBefore(forward, inline);
            return;
        }

        coll.Add(inline);
    }

    private void ButtonInsertImage_OnClick(object sender, RoutedEventArgs e)
    {
        var dlg = new OpenFileDialog
        {
            Filter = "图片文件|*.png;*.jpg;*.jpeg;*.bmp;*.gif|所有文件|*.*",
            Title = "选择要插入的图片"
        };
        if (dlg.ShowDialog(this) == true && File.Exists(dlg.FileName))
        {
            InsertImageFromFile(dlg.FileName);
        }
    }

    private void InsertImageFromFile(string filePath)
    {
        if (RelatedRichTextBox == null)
            return;

        var image = new Image
        {
            Source = new BitmapImage(new Uri(filePath, UriKind.Absolute)),
            Stretch = Stretch.Uniform,
            Width = 300.0,
            Tag = 300.0
        };
        var source = (BitmapImage)image.Source;
        image.Height = 300.0 * source.PixelHeight / source.PixelWidth;

        var container = new BlockUIContainer(image);
        container.SetValue(Paragraph.MarginProperty, new Thickness(0, 4, 0, 4));
        RelatedRichTextBox.Document.Blocks.Add(container);
        RelatedRichTextBox.CaretPosition = container.ElementEnd;
        RelatedRichTextBox.Focus();
    }

    private void RichTextBoxOnPreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (RichTextBoxHyperlinkClickHelper.TryHandleHyperlinkMouseLeftButtonDown(RelatedRichTextBox, e))
            return;

        var hit = VisualTreeHelper.HitTest(RelatedRichTextBox, e.GetPosition(RelatedRichTextBox));
        
        Image? clickedImage = hit?.VisualHit switch
        {
            Image img => img,
            TextBlock tb when VisualTreeHelper.GetParent(tb) is Image img => img,
            _ => null
        };

        if (clickedImage != null)
        {
            var originalWidth = clickedImage.Tag is double d ? d : clickedImage.Width;
            var currentZoom = Math.Round(clickedImage.Width / originalWidth * 100);
            var originalHeight = clickedImage.Height;
            var originalImageWidth = clickedImage.Width;
            var originalImageHeight = clickedImage.Height;

            void ApplyZoom(double zoomPercent)
            {
                var newWidth = originalWidth * zoomPercent / 100.0;
                var ratio = originalImageHeight / originalImageWidth;
                clickedImage.Width = newWidth;
                clickedImage.Height = newWidth * ratio;
            }

            var dialog = new ImageResizeDialog(currentZoom, ApplyZoom, () => ApplyZoom(currentZoom)) { Owner = this };
            if (dialog.ShowDialog() != true)
            {
                ApplyZoom(currentZoom);
            }
            e.Handled = true;
        }
    }

    protected override void OnInitialized(EventArgs e)
    {
        ViewModel.FontFamilies =
            new ObservableCollection<FontFamily>(from i in Fonts.SystemFontFamilies orderby i.ToString() select i)
                { (FontFamily)FindResource("HarmonyOsSans") };
        base.OnInitialized(e);
    }

    private void ViewModelOnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (ViewModel.IsRestoringSelection)
        {
            return;
        }
        var s = RelatedRichTextBox.Selection;
        switch (e.PropertyName)
        {
            case nameof(ViewModel.TextColor):
                s.ApplyPropertyValue(TextElement.ForegroundProperty, new SolidColorBrush(ViewModel.TextColor));
                break;
            case nameof(ViewModel.Font):
                s.ApplyPropertyValue(TextElement.FontFamilyProperty, ViewModel.Font);
                break;
            case nameof(ViewModel.FontSize):
                s.ApplyPropertyValue(TextElement.FontSizeProperty, Math.Max(ViewModel.FontSize, 8));
                break;
        }
    }

    private void RegisterNewTextBox(RichTextBox richTextBox)
    {
        richTextBox.TextChanged += RichTextBoxOnTextChanged;
        richTextBox.SelectionChanged += RichTextBoxOnSelectionChanged;
        richTextBox.PreviewMouseLeftButtonDown += RichTextBoxOnPreviewMouseLeftButtonDown;
    }

    private void RichTextBoxOnSelectionChanged(object sender, RoutedEventArgs e)
    {
        if (ViewModel.IsRestoringSelection)
            return;
        if (RelatedRichTextBox.Selection.Start.Paragraph != null)
            ViewModel.SelectedParagraph = RelatedRichTextBox.Selection.Start.Paragraph;

        var s = RelatedRichTextBox.Selection;
        if (!MainWindow.IsActive)
        {
            if (ViewModel.BeforeTextPointerStart == null || ViewModel.BeforeTextPointerEnd == null)
                return;
            if (!IsValidTextPointer(ViewModel.BeforeTextPointerStart) || !IsValidTextPointer(ViewModel.BeforeTextPointerEnd))
            {
                ViewModel.BeforeTextPointerStart = null;
                ViewModel.BeforeTextPointerEnd = null;
                return;
            }
            ViewModel.IsRestoringSelection = true;
            RelatedRichTextBox.Selection.Select(ViewModel.BeforeTextPointerStart, ViewModel.BeforeTextPointerEnd);
            ViewModel.IsRestoringSelection = false;
            return;
        }

        ViewModel.BeforeTextPointerStart = s.Start;
        ViewModel.BeforeTextPointerEnd = s.End;

        _selectionChangedDebounceTimer?.Stop();
        _selectionChangedDebounceTimer = new System.Windows.Threading.DispatcherTimer(
            System.Windows.Threading.DispatcherPriority.Background,
            Dispatcher)
        {
            Interval = TimeSpan.FromMilliseconds(150)
        };
        _selectionChangedDebounceTimer.Tick += (_, _) =>
        {
            _selectionChangedDebounceTimer?.Stop();
            _selectionChangedDebounceTimer = null;
            UpdateToolbarFromSelection();
        };
        _selectionChangedDebounceTimer.Start();
    }

    private void UpdateToolbarFromSelection()
    {
        if (RelatedRichTextBox == null)
            return;

        ViewModel.IsRestoringSelection = true;

        var s = RelatedRichTextBox.Selection;
        var w = s.GetPropertyValue(TextElement.FontWeightProperty);
        if (w is FontWeight weight)
        {
            ViewModel.IsBold = weight >= FontWeights.Bold;
        }

        ViewModel.IsItalic = Equals(s.GetPropertyValue(TextElement.FontStyleProperty), FontStyles.Italic);
        if (s.GetPropertyValue(Paragraph.TextDecorationsProperty) is TextDecorationCollection decorations)
        {
            ViewModel.IsUnderlined = decorations.Contains(TextDecorations.Underline[0]);
            ViewModel.IsStrikeThrough = decorations.Contains(TextDecorations.Strikethrough[0]);
        }

        if (s.GetPropertyValue(TextElement.ForegroundProperty) is SolidColorBrush fg)
        {
            ViewModel.TextColor = fg.Color;
        }
        if (s.GetPropertyValue(TextElement.FontFamilyProperty) is FontFamily font)
        {
            ViewModel.Font = font;
        }

        if (s.GetPropertyValue(TextElement.FontSizeProperty) is double fontSize)
        {
            ViewModel.FontSize = fontSize;
        }
        ViewModel.IsRestoringSelection = false;
    }

    private void RichTextBoxOnTextChanged(object sender, TextChangedEventArgs e)
    {
    }

    private void UnregisterOldTextBox(RichTextBox richTextBox)
    {
        richTextBox.TextChanged -= RichTextBoxOnTextChanged;
        richTextBox.SelectionChanged -= RichTextBoxOnSelectionChanged;
        richTextBox.PreviewMouseLeftButtonDown -= RichTextBoxOnPreviewMouseLeftButtonDown;
        var hw = FindParentHomeworkControl(richTextBox);
        RichTextBoxHyperlinkClickHelper.SetRequireCtrlToOpenHyperlinks(richTextBox, hw?.IsEditing ?? false);
    }

    private static bool IsValidTextPointer(TextPointer? pointer)
    {
        if (pointer == null)
            return false;
        try
        {
            _ = pointer.Parent;
            return true;
        }
        catch
        {
            return false;
        }
    }

    private void ListBoxTextStyles_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (ViewModel.IsRestoringSelection)
            return;
        var s = RelatedRichTextBox.Selection;
        s.ApplyPropertyValue(TextElement.FontWeightProperty, ViewModel.IsBold? FontWeights.Bold : FontWeights.Regular);
        s.ApplyPropertyValue(TextElement.FontStyleProperty, ViewModel.IsItalic ? FontStyles.Italic : FontStyles.Normal);
        var decorations = new TextDecorationCollection();
        if (ViewModel.IsUnderlined)
            decorations.Add(TextDecorations.Underline);
        if (ViewModel.IsStrikeThrough)
            decorations.Add(TextDecorations.Strikethrough);
        s.ApplyPropertyValue(Paragraph.TextDecorationsProperty, decorations);
        RelatedRichTextBox.Focus();
    }

    private void ButtonClearColor_OnClick(object sender, RoutedEventArgs e)
    {
        var s = RelatedRichTextBox.Selection;
        s.ApplyPropertyValue(TextElement.ForegroundProperty, GetValue(TextElement.ForegroundProperty));
    }

    private void ButtonFontSizeDecrease_OnClick(object sender, RoutedEventArgs e)
    {
        ViewModel.FontSize -= 2;
    }

    private void ButtonFontSizeIncrease_OnClick(object sender, RoutedEventArgs e)
    {
        ViewModel.FontSize += 2;
    }

    private void ButtonEditingDone_OnClick(object sender, RoutedEventArgs e)
    {
        SaveDocumentToHomework();
        EditingFinished?.Invoke(this, EventArgs.Empty);
        AppEx.GetService<ProfileService>().SaveProfile();
        // 备份由 MainWindow.ExitEditingMode 触发，此处不再重复调用
    }

    private void SaveDocumentToHomework()
    {
        if (RelatedRichTextBox == null)
            return;

        var behavior = FindRichTextBoxBindingBehavior(RelatedRichTextBox);
        if (behavior == null)
            return;

        var xaml = behavior.SaveDocument();
        if (string.IsNullOrEmpty(xaml))
            return;

        // 清空 TextPointer，防止 Document 被替换后 SelectionChanged 使用失效指针
        ViewModel.BeforeTextPointerStart = null;
        ViewModel.BeforeTextPointerEnd = null;

        // 将保存的 XAML 写回到 Homework.Content
        var homeworkControl = FindParentHomeworkControl(RelatedRichTextBox);
        if (homeworkControl?.Homework != null)
        {
            homeworkControl.Homework.Content = xaml;
        }
    }

    private static StickyHomeworks.Behaviors.RichTextBoxBindingBehavior? FindRichTextBoxBindingBehavior(RichTextBox richTextBox)
    {
        if (richTextBox == null)
            return null;

        var behaviors = Microsoft.Xaml.Behaviors.Interaction.GetBehaviors(richTextBox);
        foreach (var behavior in behaviors)
        {
            if (behavior is StickyHomeworks.Behaviors.RichTextBoxBindingBehavior b)
                return b;
        }
        return null;
    }

    private static StickyHomeworks.Controls.HomeworkControl? FindParentHomeworkControl(DependencyObject child)
    {
        while (child != null)
        {
            if (child is StickyHomeworks.Controls.HomeworkControl control)
                return control;
            child = System.Windows.Media.VisualTreeHelper.GetParent(child);
        }
        return null;
    }

    private void Selector_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        SubjectChanged?.Invoke(this, EventArgs.Empty);
    }

    private void ButtonAddToColor_OnClick(object sender, RoutedEventArgs e)
    {
        if (SettingsService.Settings.SavedColors.Contains(ViewModel.TextColor))
            return;
        SettingsService.Settings.SavedColors.Insert(0, ViewModel.TextColor);
        while (SettingsService.Settings.SavedColors.Count > 6)
        {
            SettingsService.Settings.SavedColors.RemoveAt(6);
        }
        SettingsService.SaveSettings();
    }

    private void ListBoxColors_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (ViewModel.IsUpdatingColor)
            return;
        ViewModel.IsUpdatingColor = true;
        foreach (var i in e.AddedItems)
        {
            if (i is Color c)
                ViewModel.TextColor = c;
        }

        if (sender is ListBox l)
            l.SelectedIndex = -1;
        ViewModel.IsUpdatingColor = false;
    }

    private void ButtonEmoji_OnClick(object sender, RoutedEventArgs e)
    {
        EmojiPickerPopup.IsOpen = true;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    protected bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }
}