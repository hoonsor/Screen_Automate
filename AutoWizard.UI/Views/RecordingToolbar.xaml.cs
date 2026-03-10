using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;

namespace AutoWizard.UI.Views
{
    /// <summary>
    /// 錄製工具列視窗 - 浮動在螢幕頂部中央，包含停止按鈕和計時器
    /// </summary>
    public partial class RecordingToolbar : Window
    {
        private readonly DispatcherTimer _timer;
        private DateTime _startTime;

        /// <summary>
        /// 當使用者點擊停止按鈕時觸發
        /// </summary>
        public event EventHandler? StopClicked;
        public event EventHandler? ScreenshotClicked;

        public RecordingToolbar()
        {
            InitializeComponent();

            // 計時器
            _startTime = DateTime.Now;
            _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            _timer.Tick += (_, _) =>
            {
                var elapsed = DateTime.Now - _startTime;
                TimerText.Text = $"{(int)elapsed.TotalMinutes:D2}:{elapsed.Seconds:D2}";
            };

            Loaded += OnLoaded;
            Closed += (_, _) => _timer.Stop();

            // 允許拖曳移動
            MouseLeftButtonDown += (_, _) =>
            {
                try { DragMove(); } catch { /* 忽略已釋放的滑鼠 */ }
            };
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            // 定位在螢幕頂部中央
            var screenWidth = SystemParameters.PrimaryScreenWidth;
            Left = (screenWidth - ActualWidth) / 2;
            Top = 10;

            _startTime = DateTime.Now;
            _timer.Start();
        }

        private void StopButton_Click(object sender, RoutedEventArgs e)
        {
            StopClicked?.Invoke(this, EventArgs.Empty);
        }

        private void ScreenshotButton_Click(object sender, RoutedEventArgs e)
        {
            ScreenshotClicked?.Invoke(this, EventArgs.Empty);
        }

        public void UpdateStatus(string message)
        {
            StatusText.Text = message;
        }
    }
}
