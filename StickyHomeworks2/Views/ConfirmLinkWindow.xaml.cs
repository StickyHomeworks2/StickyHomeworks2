using System;
using System.Collections.Generic;
using System.Linq;
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

namespace StickyHomeworks2.Views
{
    /// <summary>
    /// ConfirmLinkWindow.xaml 的交互逻辑
    /// </summary>
    public partial class ConfirmLinkWindow : Window
    {
        public bool IsConfirmed { get; private set; }

        public ConfirmLinkWindow(string linkUri, string linkText)
        {
            InitializeComponent();
            LinkUriTextBlock.Text = linkUri;
            LinkTextTextBlock.Text = linkText;
            IsConfirmed = false;
        }

        private void YesButton_Click(object sender, RoutedEventArgs e)
        {
            IsConfirmed = true;
            this.Close();
        }

        private void NoButton_Click(object sender, RoutedEventArgs e)
        {
            IsConfirmed = false;
            this.Close();
        }
    }
}
