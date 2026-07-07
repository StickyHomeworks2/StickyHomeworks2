using ElysiaFramework;
using StickyHomeworks.Services;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Forms;
using System.Windows.Media;
using System.Windows.Shapes;
using Application = System.Windows.Application;
using MessageBox = System.Windows.MessageBox;

namespace StickyHomeworks.Views
{
    /// <summary>
    /// Taskbar.xaml 的交互逻辑 - 任务栏图标右键菜单
    /// </summary>
    public partial class Taskbar : ContextMenu
    {
        public Taskbar()
        {
            InitializeComponent();
            this.Opened += Taskbar_Opened;
        }
        // <<<上色部分：开始>>>
        private void Taskbar_Opened(object sender, RoutedEventArgs e)
        {
            Dispatcher.BeginInvoke(() =>
            {
                try
                {
                    var background = Application.Current.TryFindResource("MaterialDesignPaper") as Brush;
                    if (background == null) return;

                    PaintRectangles(this, background);
                    
                    // 同步菜单项状态
                    var settingsService = AppEx.GetService<SettingsService>();
                    if (settingsService != null)
                    {
                        foreach (var item in Items)
                        {
                            if (item is MenuItem menuItem && menuItem.Header.ToString() == "显示主界面")
                            {
                                menuItem.IsChecked = settingsService.Settings.IsMainWindowVisible;
                                break;
                            }
                        }
                    }
                }
                catch { }
            }, System.Windows.Threading.DispatcherPriority.Loaded);
        }

        private static void PaintRectangles(DependencyObject parent, Brush brush)
        {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);

                if (child is Rectangle rect && rect.Width <= 40)
                    rect.Fill = brush;

                PaintRectangles(child, brush);
            }
        }
// <<<上色部分：结束>>>

        private void MenuItemShowMainWindow_OnClick(object sender, RoutedEventArgs e)
        {
            if (sender is MenuItem menuItem)
            {
                // 切换主窗口可见性
                var settingsService = AppEx.GetService<SettingsService>();
                var mainWindow = AppEx.GetService<MainWindow>();
                if (settingsService != null && mainWindow != null)
                {
                    settingsService.Settings.IsMainWindowVisible = !settingsService.Settings.IsMainWindowVisible;
                    menuItem.IsChecked = settingsService.Settings.IsMainWindowVisible;
                    
                    if (settingsService.Settings.IsMainWindowVisible)
                    {
                        mainWindow.Show();
                        mainWindow.Activate();
                    }
                    else
                    {
                        mainWindow.Hide();
                    }
                }
            }
        }

        private void MenuItemSettings_OnClick(object sender, RoutedEventArgs e)
        {
            try
            {
                var settingsWindow = AppEx.GetService<SettingsWindow>();
                if (settingsWindow != null)
                {
                    if (!settingsWindow.IsOpened)
                    {
                        settingsWindow.IsOpened = true;
                        settingsWindow.Show();
                    }
                    else
                    {
                        if (settingsWindow.WindowState == WindowState.Minimized)
                        {
                            settingsWindow.WindowState = WindowState.Normal;
                        }
                        settingsWindow.Activate();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"打开设置时发生错误: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void MenuItemRestartApp_OnClick(object sender, RoutedEventArgs e)
        {
            try
            {
                System.Windows.Forms.Application.Restart();
                Application.Current.Shutdown();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"重启应用时发生错误: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void MenuItemExitApp_OnClick(object sender, RoutedEventArgs e)
        {
            try
            {
                var mainWindow = AppEx.GetService<MainWindow>();
                if (mainWindow != null)
                {
                    mainWindow.ViewModel.IsClosing = true;
                    // 清理托盘图标
                    mainWindow.TrayIconView.Dispose();
                }
                Application.Current.Shutdown();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"退出应用时发生错误: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Show the ContextMenu at current mouse position. This replaces the placeholder NotImplementedException.
        /// Callers that expect Window.ShowDialog() should be updated to call ShowAtMousePoint() instead.
        /// </summary>
        internal void ShowDialog()
        {
            // Instead of throwing, display the context menu at the mouse cursor
            try
            {
                // PlacementTarget must be set for ContextMenu when opened from code
                // Create an invisible PlacementTarget if none is available
                var placementTarget = new System.Windows.Controls.Primitives.Popup();

                // Use mouse position for placement
                this.Placement = PlacementMode.MousePoint;
                this.IsOpen = true;
            }
            catch (Exception ex)
            {
                // Fallback: do not crash the app, show a message for debugging
                MessageBox.Show($"无法打开右键菜单: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Preferred explicit method name for showing the ContextMenu at the mouse.
        /// </summary>
        public void ShowAtMousePoint()
        {
            this.Placement = PlacementMode.MousePoint;
            this.IsOpen = true;
        }
    }
}
