using System.Windows;
using System.Windows.Input;

namespace AutoWizard.UI.Views
{
    public partial class CaptureNameDialog : Window
    {
        /// <summary>使用者確認的檔案名稱（不含副檔名）</summary>
        public string ResultFileName { get; private set; } = string.Empty;
        public bool IsConfirmed { get; private set; } = false;

        public CaptureNameDialog(string defaultFileName)
        {
            InitializeComponent();
            FileNameBox.Text = defaultFileName;
            Loaded += (s, e) =>
            {
                FileNameBox.Focus();
                // 選取不含副檔名的部分
                var dotIndex = defaultFileName.LastIndexOf('.');
                FileNameBox.SelectionStart = 0;
                FileNameBox.SelectionLength = dotIndex >= 0 ? dotIndex : defaultFileName.Length;
            };
        }

        private void OnSave(object sender, RoutedEventArgs e)
        {
            Confirm();
        }

        private void OnCancel(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void FileNameBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return)
            {
                Confirm();
                e.Handled = true;
            }
            else if (e.Key == Key.Escape)
            {
                Close();
                e.Handled = true;
            }
        }

        private void Confirm()
        {
            var name = FileNameBox.Text.Trim();
            if (string.IsNullOrEmpty(name)) return;

            // 移除非法字元
            foreach (var c in System.IO.Path.GetInvalidFileNameChars())
                name = name.Replace(c, '_');

            ResultFileName = name;
            IsConfirmed = true;
            Close();
        }
    }
}
