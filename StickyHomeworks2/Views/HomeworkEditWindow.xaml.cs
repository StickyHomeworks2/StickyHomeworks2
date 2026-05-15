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
using System.Windows.Threading;
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
using StickyHomeworks.Models;
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
    private sealed class HomeworkTemplatePartUi
    {
        public string Label { get; init; } = "";
        public CheckBox CheckBox { get; init; } = null!;
    }

    private sealed class HomeworkTemplateBookUiRow
    {
        public string BookName { get; init; } = "";
        public CheckBox BookCheckBox { get; init; } = null!;
        public WrapPanel PagesPanel { get; init; } = null!;
        public List<HomeworkTemplatePartUi> PartRows { get; } = new();
    }

    private readonly List<HomeworkTemplateBookUiRow> _homeworkTemplateBookUiRows = new();
    private bool _syncingHomeworkTemplateBookUi;

    private RichTextBox _relatedRichTextBox = new();
    private System.Windows.Threading.DispatcherTimer? _selectionChangedDebounceTimer;
    private System.Windows.Threading.DispatcherTimer? _templateUiRefreshDebounceTimer;
    public MainWindow MainWindow { get; }
    public SettingsService SettingsService { get; }
    public TimeMachineService TimeMachineService { get; }
    public ImageService ImageService { get; }


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
        _selectionChangedDebounceTimer?.Stop();
        _selectionChangedDebounceTimer = null;
        _templateUiRefreshDebounceTimer?.Stop();
        _templateUiRefreshDebounceTimer = null;
        ViewModel.BeforeTextPointerStart = null;
        ViewModel.BeforeTextPointerEnd = null;
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
            BuildHomeworkTemplateChips();
        }
    }

    public HomeworkEditWindow(MainWindow mainWindow, SettingsService settingsService, TimeMachineService timeMachineService, ImageService imageService)
    {
        MainWindow = mainWindow;
        SettingsService = settingsService;
        TimeMachineService = timeMachineService;
        ImageService = imageService;
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

        BuildHomeworkTemplateChips();
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

    private static BitmapImage BitmapImageFromSource(BitmapSource bitmapSource)
    {
        var bitmap = new BitmapImage();
        bitmap.BeginInit();
        var encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(bitmapSource));
        using var ms = new MemoryStream();
        encoder.Save(ms);
        ms.Position = 0;
        bitmap.StreamSource = ms;
        bitmap.CacheOption = BitmapCacheOption.OnLoad;
        bitmap.EndInit();
        bitmap.Freeze();
        return bitmap;
    }

    private void ButtonInsertImage_OnClick(object sender, RoutedEventArgs e)
    {
        if (ImageService.TryShowFileDialog(this, out var path))
        {
            ImageService.InsertImageFromFile(RelatedRichTextBox, path);
        }
    }

    private void OnRichTextBoxPasting(object sender, DataObjectPastingEventArgs e)
    {
        if (e.SourceDataObject.GetDataPresent(DataFormats.Bitmap))
        {
            e.CancelCommand();

            if (e.SourceDataObject.GetData(DataFormats.Bitmap) is BitmapSource bitmapSource)
            {
                var bitmap = BitmapImageFromSource(bitmapSource);

                var image = ImageService.CreateImageElement(bitmap, ImageService.DefaultImageWidth);
                var container = ImageService.CreateBlockContainer(image);

                var rtb = (RichTextBox)sender;
                var pos = rtb.CaretPosition;
                Block insertAfter = null;
                if (pos.Paragraph != null)
                {
                    insertAfter = pos.Paragraph;
                }
                else
                {
                    rtb.Document.Blocks.Add(container);
                    rtb.CaretPosition = container.ElementEnd;
                    rtb.Focus();
                    return;
                }

                var blocks = rtb.Document.Blocks.ToList();
                var idx = blocks.IndexOf(insertAfter);
                if (idx >= 0)
                {
                    var nextBlock = (idx + 1 < blocks.Count) ? blocks[idx + 1] : null;
                    rtb.Document.Blocks.Remove(insertAfter);

                    if (nextBlock != null)
                    {
                        rtb.Document.Blocks.InsertBefore(nextBlock, container);
                        rtb.Document.Blocks.InsertBefore(nextBlock, insertAfter);
                    }
                    else
                    {
                        rtb.Document.Blocks.Add(insertAfter);
                        rtb.Document.Blocks.Add(container);
                    }
                }
                else
                {
                    rtb.Document.Blocks.Add(container);
                }

                rtb.CaretPosition = container.ElementEnd;
                rtb.Focus();
            }
        }
    }

    private void RichTextBoxOnPreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (RichTextBoxHyperlinkClickHelper.TryHandleHyperlinkMouseLeftButtonDown(RelatedRichTextBox, e))
            return;

        var hit = VisualTreeHelper.HitTest(RelatedRichTextBox, e.GetPosition(RelatedRichTextBox));

        if (ImageService.IsImageClick(hit?.VisualHit, out var clickedImage))
        {
            ImageService.ShowResizeDialog(this, clickedImage);
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
        DataObject.AddPastingHandler(richTextBox, OnRichTextBoxPasting);
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
            RefreshHomeworkTemplateBookUi();
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

        RefreshHomeworkTemplateBookUi();
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
        if (_templateUiRefreshDebounceTimer == null)
        {
            _templateUiRefreshDebounceTimer = new DispatcherTimer(
                DispatcherPriority.Background,
                Dispatcher)
            {
                Interval = TimeSpan.FromMilliseconds(80)
            };
            _templateUiRefreshDebounceTimer.Tick += (_, _) =>
            {
                _templateUiRefreshDebounceTimer?.Stop();
                RefreshHomeworkTemplateBookUi();
            };
        }
        else
            _templateUiRefreshDebounceTimer.Stop();

        _templateUiRefreshDebounceTimer.Start();
    }

    private void UnregisterOldTextBox(RichTextBox richTextBox)
    {
        _templateUiRefreshDebounceTimer?.Stop();
        richTextBox.TextChanged -= RichTextBoxOnTextChanged;
        richTextBox.SelectionChanged -= RichTextBoxOnSelectionChanged;
        richTextBox.PreviewMouseLeftButtonDown -= RichTextBoxOnPreviewMouseLeftButtonDown;
        DataObject.RemovePastingHandler(richTextBox, OnRichTextBoxPasting);
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

    public void FlushEditingToModel() => SaveDocumentToHomework();

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

        var homeworkControl = FindParentHomeworkControl(RelatedRichTextBox);
        if (homeworkControl?.Homework != null)
        {
            behavior.SuppressDocumentXamlApply = true;
            try
            {
                homeworkControl.Homework.Content = xaml;
            }
            finally
            {
                behavior.SuppressDocumentXamlApply = false;
            }
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

    private void BuildHomeworkTemplateChips()
    {
        if (!IsInitialized)
            return;

        WpHomeworkTemplateQuickActions.Children.Clear();
        WpHomeworkTemplateSubjectBooks.Children.Clear();
        WpHomeworkTemplateCommonBooks.Children.Clear();
        _homeworkTemplateBookUiRows.Clear();

        var template = SettingsService.Settings.HomeworkTemplate;
        HomeworkTemplateConfig.Normalize(template);

        var subject = MainWindow.ViewModel.SelectedHomework?.Subject ?? "";

        foreach (var a in template.QuickActions)
        {
            var label = a;
            WpHomeworkTemplateQuickActions.Children.Add(CreateHomeworkTemplateChip(label,
                () => HomeworkTemplateRichTextHelper.InsertPlainAtCaret(RelatedRichTextBox, label)));
        }

        if (!string.IsNullOrEmpty(subject) &&
            template.SubjectBooks.TryGetValue(subject, out var sb) &&
            sb.Count > 0)
        {
            foreach (var kv in sb.OrderBy(static x => x.Key))
                AddHomeworkTemplateBookGroup(WpHomeworkTemplateSubjectBooks, kv.Key, kv.Value);
        }

        if (template.CommonBooks.Count > 0)
        {
            foreach (var kv in template.CommonBooks.OrderBy(static x => x.Key))
                AddHomeworkTemplateBookGroup(WpHomeworkTemplateCommonBooks, kv.Key, kv.Value);
        }

        RefreshHomeworkTemplateBookUi();
    }

    private void SaveHomeworkAndRestoreTemplateCaret(string? bookNameIfStillInDoc)
    {
        SaveDocumentToHomework();
        var rtb = RelatedRichTextBox;
        if (rtb != null)
        {
            HomeworkTemplateRichTextHelper.RestoreCaretAfterTemplateSave(rtb, bookNameIfStillInDoc);
            RefreshHomeworkTemplateBookUi();
        }
    }

    private bool ShouldForkNewHomeworkForAnotherBook(RichTextBox? rtb, string bookNameToCheck)
    {
        if (rtb?.Document == null || string.IsNullOrEmpty(bookNameToCheck))
            return false;

        foreach (var row in _homeworkTemplateBookUiRows)
        {
            if (string.Equals(row.BookName, bookNameToCheck, StringComparison.Ordinal))
                continue;
            if (HomeworkTemplateRichTextHelper.DocumentContainsBookLine(rtb, row.BookName))
                return true;
        }

        return false;
    }

    private async Task ForkNewHomeworkAndEnsureBookLineAsync(string bookName)
    {
        MainWindow.ViewModel.IsUpdatingHomeworkSubject = true;
        try
        {
            SaveDocumentToHomework();

            var cur = MainWindow.ViewModel.SelectedHomework;
            var hw = new Homework
            {
                Subject = cur?.Subject ?? "",
                DueTime = cur?.DueTime ?? DateTime.Today
            };
            if (cur?.Tags != null)
            {
                foreach (var t in cur.Tags)
                    hw.Tags.Add(t);
            }

            MainWindow.ProfileService.Profile.Homeworks.Add(hw);
            MainWindow.ViewModel.SelectedHomework = hw;
            MainWindow.ViewModel.EditingHomework = hw;
            MainWindow.ProfileService.SaveProfile();

            // 等新选中 Homework 绑定到 RichTextBox、FlowDocument 就绪后再插入模板行，避免作用在旧文档上。
            await Dispatcher.InvokeAsync(static () => { }, DispatcherPriority.Loaded);

            HomeworkTemplateRichTextHelper.EnsureBookLine(RelatedRichTextBox, bookName);
            SaveHomeworkAndRestoreTemplateCaret(bookName);
            MainWindow.RepositionHomeworkEditWindow();
        }
        finally
        {
            MainWindow.ViewModel.IsUpdatingHomeworkSubject = false;
        }
    }

    private void RefreshHomeworkTemplateBookUi()
    {
        if (_homeworkTemplateBookUiRows.Count == 0)
            return;

        _syncingHomeworkTemplateBookUi = true;
        try
        {
            var line = HomeworkTemplateRichTextHelper.GetCaretParagraphPlainText(RelatedRichTextBox);
            foreach (var row in _homeworkTemplateBookUiRows)
            {
                var bookOnLine = HomeworkTemplateRichTextHelper.ParagraphLeadsWithBook(line, row.BookName);
                row.BookCheckBox.IsChecked = bookOnLine;
                row.PagesPanel.Visibility = bookOnLine ? Visibility.Visible : Visibility.Collapsed;

                foreach (var part in row.PartRows)
                {
                    var pageOnLine = HomeworkTemplateRichTextHelper.LineContainsPartToken(line, part.Label);
                    part.CheckBox.IsChecked = pageOnLine;
                }
            }
        }
        finally
        {
            _syncingHomeworkTemplateBookUi = false;
        }
    }

    private Button CreateHomeworkTemplateChip(string label, Action onClick)
    {
        var flat = TryFindResource("MaterialDesignFlatButton") as Style;
        var b = new Button
        {
            Content = label,
            Margin = new Thickness(4, 2, 4, 2),
            MinHeight = 32,
            Padding = new Thickness(8, 4, 8, 4),
            Style = flat
        };
        b.Click += (_, _) =>
        {
            onClick();
            SaveDocumentToHomework();
            RefreshHomeworkTemplateBookUi();
        };
        return b;
    }

    private void AddHomeworkTemplateBookGroup(Panel booksPanel, string bookName, ICollection<string> parts)
    {
        var sp = new StackPanel
        {
            Margin = new Thickness(0, 4, 8, 8),
            Orientation = Orientation.Vertical
        };

        var chkStyle = TryFindResource("MaterialDesignCheckBox") as Style;
        var cb = new CheckBox
        {
            Content = bookName,
            Margin = new Thickness(4, 2, 4, 2),
            MinHeight = 28,
            VerticalContentAlignment = VerticalAlignment.Center
        };
        if (chkStyle != null)
            cb.Style = chkStyle;

        void OnBookChecked(bool wantLine)
        {
            if (_syncingHomeworkTemplateBookUi)
                return;
            if (!wantLine)
            {
                HomeworkTemplateRichTextHelper.RemoveBookLine(RelatedRichTextBox, bookName);
                SaveHomeworkAndRestoreTemplateCaret(null);
                return;
            }
            if (ShouldForkNewHomeworkForAnotherBook(RelatedRichTextBox, bookName))
            {
                _ = ForkNewHomeworkAndEnsureBookLineAsync(bookName);
                return;
            }

            HomeworkTemplateRichTextHelper.EnsureBookLine(RelatedRichTextBox, bookName);
            SaveHomeworkAndRestoreTemplateCaret(bookName);
        }

        cb.Checked += (_, _) => OnBookChecked(true);
        cb.Unchecked += (_, _) => OnBookChecked(false);

        sp.Children.Add(cb);

        var wp = new WrapPanel
        {
            Visibility = Visibility.Collapsed,
            Margin = new Thickness(24, 4, 0, 0)
        };

        var rowInfo = new HomeworkTemplateBookUiRow
        {
            BookName = bookName,
            BookCheckBox = cb,
            PagesPanel = wp
        };

        foreach (var p in parts)
        {
            var partLabel = p;
            var partCb = CreateHomeworkTemplatePartCheckBox(bookName, partLabel);
            wp.Children.Add(partCb);
            rowInfo.PartRows.Add(new HomeworkTemplatePartUi { Label = partLabel, CheckBox = partCb });
        }

        sp.Children.Add(wp);
        booksPanel.Children.Add(sp);
        _homeworkTemplateBookUiRows.Add(rowInfo);
    }

    private CheckBox CreateHomeworkTemplatePartCheckBox(string bookName, string partLabel)
    {
        var chkStyle = TryFindResource("MaterialDesignCheckBox") as Style;
        var cb = new CheckBox
        {
            Content = partLabel,
            Margin = new Thickness(4, 2, 4, 2),
            MinHeight = 26,
            VerticalContentAlignment = VerticalAlignment.Center
        };
        if (chkStyle != null)
            cb.Style = chkStyle;

        void OnPart(bool want)
        {
            if (_syncingHomeworkTemplateBookUi)
                return;
            if (want)
                HomeworkTemplateRichTextHelper.EnsurePartOnBookParagraph(RelatedRichTextBox, bookName, partLabel);
            else
                HomeworkTemplateRichTextHelper.RemoveLastPartOnBookParagraph(RelatedRichTextBox, bookName, partLabel);

            SaveHomeworkAndRestoreTemplateCaret(bookName);
        }

        cb.Checked += (_, _) => OnPart(true);
        cb.Unchecked += (_, _) => OnPart(false);
        return cb;
    }

    private void Selector_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        SubjectChanged?.Invoke(this, EventArgs.Empty);
        BuildHomeworkTemplateChips();
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