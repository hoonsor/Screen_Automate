using Prism.Mvvm;
using System.Collections.ObjectModel;

namespace AutoWizard.UI.ViewModels
{
    public class ToolboxViewModel : BindableBase
    {
        public ObservableCollection<ToolboxItem> InputActions { get; } = new();
        public ObservableCollection<ToolboxItem> ControlActions { get; } = new();
        public ObservableCollection<ToolboxItem> VisionActions { get; } = new();
        public ObservableCollection<ToolboxItem> UtilityActions { get; } = new();

        public ToolboxViewModel()
        {
            InitializeToolbox();
        }

        private void InitializeToolbox()
        {
            // 輸入指令
            InputActions.Add(new ToolboxItem
            {
                ActionType = "Click",
                Title = "點擊",
                Color = "#4CAF50",
                Description = "模擬滑鼠短按點擊"
            });
            InputActions.Add(new ToolboxItem
            {
                ActionType = "MouseDown",
                Title = "按下",
                Color = "#4CAF50",
                Description = "按住滑鼠按鍵(用於拖拉起點)"
            });
            InputActions.Add(new ToolboxItem
            {
                ActionType = "MouseUp",
                Title = "放開",
                Color = "#4CAF50",
                Description = "放開滑鼠按鍵(用於拖拉終點)"
            });
            InputActions.Add(new ToolboxItem
            {
                ActionType = "Type",
                Title = "輸入文字",
                Color = "#4CAF50",
                Description = "模擬鍵盤輸入"
            });
            InputActions.Add(new ToolboxItem
            {
                ActionType = "Keyboard",
                Title = "快捷鍵",
                Color = "#4CAF50",
                Description = "鍵盤快捷鍵組合 (Ctrl/Alt/Shift+Key)"
            });

            // 控制流指令
            ControlActions.Add(new ToolboxItem
            {
                ActionType = "If",
                Title = "條件判斷",
                Color = "#FF9800",
                Description = "根據條件執行不同分支"
            });
            ControlActions.Add(new ToolboxItem
            {
                ActionType = "Loop",
                Title = "迴圈",
                Color = "#FF9800",
                Description = "重複執行指令"
            });
            ControlActions.Add(new ToolboxItem
            {
                ActionType = "SetVariable",
                Title = "設定變數",
                Color = "#FF9800",
                Description = "設定或運算變數值"
            });
            ControlActions.Add(new ToolboxItem
            {
                ActionType = "Window",
                Title = "視窗綁定",
                Color = "#FF9800",
                Description = "綁定特定視窗，後續點擊將相對於此視窗"
            });

            // 視覺辨識指令
            VisionActions.Add(new ToolboxItem
            {
                ActionType = "FindImage",
                Title = "尋找影像",
                Color = "#2196F3",
                Description = "在螢幕上尋找指定影像"
            });
            VisionActions.Add(new ToolboxItem
            {
                ActionType = "OCR",
                Title = "OCR 辨識",
                Color = "#2196F3",
                Description = "辨識螢幕上的文字"
            });

            // 工具指令
            UtilityActions.Add(new ToolboxItem
            {
                ActionType = "Wait",
                Title = "等待",
                Color = "#9C27B0",
                Description = "延遲等待指定時間"
            });
            UtilityActions.Add(new ToolboxItem
            {
                ActionType = "Screenshot",
                Title = "螢幕截圖",
                Color = "#9C27B0",
                Description = "擷取螢幕或指定區域"
            });
        }
    }

    public class ToolboxItem
    {
        public string ActionType { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Color { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
    }
}
