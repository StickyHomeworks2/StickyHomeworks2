using System.Diagnostics;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Markup;
using System.Windows.Threading;
using Microsoft.Xaml.Behaviors;

namespace StickyHomeworks.Behaviors;

public class RichTextBoxBindingBehavior : Behavior<RichTextBox>
{
    private static HashSet<Thread> _recursionProtection = new HashSet<Thread>();
    private DispatcherTimer? _debounceTimer;
    private string? _pendingXaml;
    private bool _isLoading;

    protected override void OnAttached()
    {
        _debounceTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(500) };
        _debounceTimer.Tick += (s, e) =>
        {
            _debounceTimer.Stop();
            if (_pendingXaml != null)
            {
                SetDocumentXaml(this, _pendingXaml);
                _pendingXaml = null;
            }
        };

        AssociatedObject.TextChanged += (obj2, e2) =>
        {
            if (_isLoading)
                return;
            
            var sw = new Stopwatch();
            sw.Start();
            RichTextBox richTextBox2 = obj2 as RichTextBox;
            if (richTextBox2 != null)
            {
                var xaml = XamlWriter.Save(richTextBox2.Document);
                _pendingXaml = xaml;
                _debounceTimer.Stop();
                _debounceTimer.Start();
            }
        };
        base.OnAttached();
    }

    protected override void OnDetaching()
    {
        _debounceTimer?.Stop();
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
                var sw = new Stopwatch();
                sw.Start();
                if (_recursionProtection.Contains(Thread.CurrentThread))
                {
                    Debug.WriteLine(sw.Elapsed.ToString());
                    return;
                }

                if (obj is not RichTextBoxBindingBehavior b)
                    return;
                var richTextBox = b.AssociatedObject;

                var documentXaml = GetDocumentXaml(b);
                b._isLoading = true;
                richTextBox.Document = RichTextBoxHelper.ConvertDocument(documentXaml);
                richTextBox.Document.IsOptimalParagraphEnabled = true;
                b._isLoading = false;
            }
        ));

    public string DocumentXaml
    {
        get { return (string)GetValue(DocumentXamlProperty); }
        set { SetValue(DocumentXamlProperty, value); }
    }
}
