using Prism.Commands;
using Prism.Mvvm;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;

namespace AutoWizard.UI.ViewModels
{
    public class ColorHistoryItem : BindableBase
    {
        private string _header = string.Empty;
        public string Header
        {
            get => _header;
            set => SetProperty(ref _header, value);
        }
        public int X { get; set; }
        public int Y { get; set; }
        public string HexColor { get; set; } = string.Empty;

        // Display string format X, Y
        public string DisplayCoordinates => $"{X}, {Y}";
    }

    public class ColorHistoryViewModel : BindableBase
    {
        private const int MaxHistory = 10;
        public ObservableCollection<ColorHistoryItem> HistoryList { get; } = new();

        private ColorHistoryItem? _selectedItem;
        public ColorHistoryItem? SelectedItem
        {
            get => _selectedItem;
            set => SetProperty(ref _selectedItem, value);
        }

        public ICommand FlashLocationCommand { get; }

        public System.Action<int, int>? FlashCoordinatesAction { get; set; }

        public ColorHistoryViewModel()
        {
            FlashLocationCommand = new DelegateCommand(OnFlashLocation, () => SelectedItem != null)
                .ObservesProperty(() => SelectedItem);
        }

        public void AddHistory(int x, int y, string hex)
        {
            // Insert at the beginning (recent 1)
            var newItem = new ColorHistoryItem
            {
                X = x,
                Y = y,
                HexColor = hex
            };
            
            HistoryList.Insert(0, newItem);

            // Update Headers
            for (int i = 0; i < HistoryList.Count; i++)
            {
                HistoryList[i].Header = $"最近第{i + 1}筆紀錄";
            }

            // Remove excess
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
                FlashCoordinatesAction?.Invoke(SelectedItem.X, SelectedItem.Y);
            }
        }
    }
}
