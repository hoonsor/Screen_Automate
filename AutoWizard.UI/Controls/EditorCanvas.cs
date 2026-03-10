using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using AutoWizard.Core.Models;

namespace AutoWizard.UI.Controls
{
    /// <summary>
    /// 編輯器畫布 - 支援拖放與指令管理
    /// </summary>
    public class EditorCanvas : Canvas
    {
        public static readonly DependencyProperty AllowDropProperty =
            DependencyProperty.Register(nameof(AllowDrop), typeof(bool), typeof(EditorCanvas),
                new PropertyMetadata(true));

        public EditorCanvas()
        {
            AllowDrop = true;
            Drop += OnDrop;
            DragOver += OnDragOver;
        }

        private void OnDragOver(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(typeof(ActionBlock)))
            {
                e.Effects = DragDropEffects.Move;
            }
            else
            {
                e.Effects = DragDropEffects.None;
            }
            e.Handled = true;
        }

        private void OnDrop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(typeof(ActionBlock)))
            {
                var actionBlock = e.Data.GetData(typeof(ActionBlock)) as ActionBlock;
                if (actionBlock != null)
                {
                    // 取得滑鼠位置
                    Point dropPosition = e.GetPosition(this);
                    
                    // 創建新的 ActionBlock 實例
                    var newBlock = new ActionBlock
                    {
                        ActionType = actionBlock.ActionType,
                        ActionTitle = actionBlock.ActionTitle,
                        ActionColor = actionBlock.ActionColor
                    };

                    // 設定位置
                    SetLeft(newBlock, dropPosition.X);
                    SetTop(newBlock, dropPosition.Y);

                    // 加入畫布
                    Children.Add(newBlock);
                }
            }
            e.Handled = true;
        }
    }
}
