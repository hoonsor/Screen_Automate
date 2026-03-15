using Prism.Commands;
using Prism.Mvvm;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;

namespace AutoWizard.UI.ViewModels
{
    public class ScreenshotHistoryItem : BindableBase
    {
        private string _header = string.Empty;
        public string Header
        {
            get => _header;
            set => SetProperty(ref _header, value);
        }
        public int CenterX { get; set; }
        public int CenterY { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
        public string FilePath { get; set; } = string.Empty;

        // Display string format X, Y
        public string DisplayCoordinates => $"{CenterX}, {CenterY}";

        public string RelativeImagePath
        {
            get
            {
                if (string.IsNullOrEmpty(FilePath)) return string.Empty;
                int idx = FilePath.IndexOf("Images\\", System.StringComparison.OrdinalIgnoreCase);
                if (idx >= 0)
                {
                    return FilePath.Substring(idx);
                }
                return FilePath;
            }
        }
    }

    public class ScreenshotHistoryViewModel : BindableBase
    {
        private const int MaxHistory = 5;
        public ObservableCollection<ScreenshotHistoryItem> HistoryList { get; } = new();

        private ScreenshotHistoryItem? _selectedItem;
        public ScreenshotHistoryItem? SelectedItem
        {
            get => _selectedItem;
            set => SetProperty(ref _selectedItem, value);
        }

        public ICommand FlashLocationCommand { get; }

        public System.Action<int, int, int, int>? FlashRectAction { get; set; }

        public ScreenshotHistoryViewModel()
        {
            FlashLocationCommand = new DelegateCommand(OnFlashLocation, () => SelectedItem != null)
                .ObservesProperty(() => SelectedItem);
        }

        public void AddHistory(int x, int y, int width, int height, string filePath)
        {
            var newItem = new ScreenshotHistoryItem
            {
                CenterX = x + (width / 2),
                CenterY = y + (height / 2),
                Width = width,
                Height = height,
                FilePath = filePath
            };
            
            HistoryList.Insert(0, newItem);

            for (int i = 0; i < HistoryList.Count; i++)
            {
                HistoryList[i].Header = $"最近第{i + 1}筆紀錄";
            }

            while (HistoryList.Count > MaxHistory)
            {
                HistoryList.RemoveAt(HistoryList.Count - 1);
            }

            if (SelectedItem == null)
            {
                SelectedItem = HistoryList.First();
            }
        }

        private void OnFlashLocation()
        {
            if (SelectedItem != null)
            {
                // We recreate the original rect from Center and Width/Height
                int originX = SelectedItem.CenterX - (SelectedItem.Width / 2);
                int originY = SelectedItem.CenterY - (SelectedItem.Height / 2);
                FlashRectAction?.Invoke(originX, originY, SelectedItem.Width, SelectedItem.Height);
            }
        }
    }
}
