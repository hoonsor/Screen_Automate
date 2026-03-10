using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
using AutoWizard.UI.ViewModels;

namespace AutoWizard.UI.Views
{
    /// <summary>
    /// MainWindow.xaml 的互動邏輯
    /// </summary>
    public partial class MainWindow : Window
    {
        // 全域快捷鍵 P/Invoke
        [DllImport("user32.dll")]
        private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);
        [DllImport("user32.dll")]
        private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

        private const int HOTKEY_RECORD = 1;  // Ctrl+F9
        private const int HOTKEY_RUN    = 2;  // Ctrl+F10
        private const uint MOD_CTRL     = 0x0002;
        private const uint VK_F9        = 0x78;
        private const uint VK_F10       = 0x79;
        private const int WM_HOTKEY     = 0x0312;

        private HwndSource? _hwndSource;

        public MainWindow()
        {
            InitializeComponent();
            Loaded += MainWindow_Loaded;
            Closed += MainWindow_Closed;
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            // 訂閱多選恢復事件
            if (DataContext is MainWindowViewModel vm)
            {
                vm.EditorViewModel.MultiSelectionRestoreRequested += OnMultiSelectionRestore;
            }

            // Enter 鍵切換指令啟用狀態
            ActionListBox.KeyDown += ActionListBox_KeyDown;

            // 註冊全域快捷鍵
            _hwndSource = HwndSource.FromHwnd(new WindowInteropHelper(this).Handle);
            _hwndSource?.AddHook(WndProc);
            var handle = new WindowInteropHelper(this).Handle;
            RegisterHotKey(handle, HOTKEY_RECORD, MOD_CTRL, VK_F9);
            RegisterHotKey(handle, HOTKEY_RUN, MOD_CTRL, VK_F10);
        }

        private void ActionListBox_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Return
                && DataContext is MainWindowViewModel vm
                && ActionListBox.SelectedItems.Count > 0)
            {
                foreach (var item in ActionListBox.SelectedItems)
                {
                    if (item is ActionNodeWrapper node && node.Action is AutoWizard.Core.Models.BaseAction action)
                    {
                        action.IsEnabled = !action.IsEnabled;
                    }
                }
                e.Handled = true;
            }
        }

        private void MainWindow_Closed(object? sender, EventArgs e)
        {
            var handle = new WindowInteropHelper(this).Handle;
            UnregisterHotKey(handle, HOTKEY_RECORD);
            UnregisterHotKey(handle, HOTKEY_RUN);
            _hwndSource?.RemoveHook(WndProc);
        }

        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (msg == WM_HOTKEY && DataContext is MainWindowViewModel vm)
            {
                int id = wParam.ToInt32();
                if (id == HOTKEY_RECORD)
                {
                    vm.RecordCommand.Execute(null);
                    handled = true;
                }
                else if (id == HOTKEY_RUN)
                {
                    vm.RunScriptCommand.Execute(null);
                    handled = true;
                }
            }
            return IntPtr.Zero;
        }

        private void OnExit(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        /// <summary>
        /// 同步 ListBox.SelectedItems（不支援 MVVM 綁定）到 EditorViewModel
        /// </summary>
        private void ActionListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sender is ListBox listBox && DataContext is MainWindowViewModel vm)
            {
                vm.EditorViewModel.SyncSelectedNodes(listBox.SelectedItems);
            }
        }

        /// <summary>
        /// 移動操作後，程式化地恢復 ListBox 的多選狀態
        /// </summary>
        private void OnMultiSelectionRestore(object? sender, System.Collections.Generic.List<ActionNodeWrapper> nodes)
        {
            // 暫時解除事件以避免遞迴觸發
            ActionListBox.SelectionChanged -= ActionListBox_SelectionChanged;
            try
            {
                ActionListBox.SelectedItems.Clear();
                foreach (var node in nodes)
                {
                    ActionListBox.SelectedItems.Add(node);
                }
            }
            finally
            {
                ActionListBox.SelectionChanged += ActionListBox_SelectionChanged;
            }
        }
        private void CodeEditor_ScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            if (LineNumberList != null)
            {
                var scrollViewer = GetScrollViewer(LineNumberList);
                if (scrollViewer != null)
                {
                    scrollViewer.ScrollToVerticalOffset(e.VerticalOffset);
                }
            }
        }

        private void CodeEditor_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (DataContext is MainWindowViewModel vm)
            {
                vm.EditorViewModel.UpdateCodeLines();
            }
        }

        private ScrollViewer? GetScrollViewer(DependencyObject depObj)
        {
            if (depObj is ScrollViewer sv) return sv;

            for (int i = 0; i < System.Windows.Media.VisualTreeHelper.GetChildrenCount(depObj); i++)
            {
                var child = System.Windows.Media.VisualTreeHelper.GetChild(depObj, i);
                var result = GetScrollViewer(child);
                if (result != null) return result;
            }
            return null;
        }

        /// <summary>
        /// 點擊「➕ 子指令 ▾」按鈕時，打開下拉 ContextMenu
        /// </summary>
        private void AddChildButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.ContextMenu != null)
            {
                button.ContextMenu.DataContext = DataContext;
                button.ContextMenu.PlacementTarget = button;
                button.ContextMenu.Placement = System.Windows.Controls.Primitives.PlacementMode.Bottom;
                button.ContextMenu.IsOpen = true;
            }
        }
    }
}

