using System.Windows;
using System.Windows.Controls;
using System.Collections.ObjectModel;
using AutoWizard.Core.Models;

namespace AutoWizard.UI.Controls
{
    /// <summary>
    /// 容器指令塊 - 支援巢狀子指令
    /// </summary>
    public class ContainerActionBlock : ActionBlock
    {
        public static readonly DependencyProperty ChildActionsProperty =
            DependencyProperty.Register(nameof(ChildActions), typeof(ObservableCollection<BaseAction>), 
                typeof(ContainerActionBlock), new PropertyMetadata(null));

        public ObservableCollection<BaseAction> ChildActions
        {
            get => (ObservableCollection<BaseAction>)GetValue(ChildActionsProperty);
            set => SetValue(ChildActionsProperty, value);
        }

        public static readonly DependencyProperty AllowChildrenProperty =
            DependencyProperty.Register(nameof(AllowChildren), typeof(bool), 
                typeof(ContainerActionBlock), new PropertyMetadata(true));

        public bool AllowChildren
        {
            get => (bool)GetValue(AllowChildrenProperty);
            set => SetValue(AllowChildrenProperty, value);
        }

        public ContainerActionBlock()
        {
            ChildActions = new ObservableCollection<BaseAction>();
            AllowDrop = true;
            Drop += OnChildDrop;
        }

        private void OnChildDrop(object sender, DragEventArgs e)
        {
            if (!AllowChildren) return;

            if (e.Data.GetDataPresent(typeof(ActionBlock)))
            {
                var actionBlock = e.Data.GetData(typeof(ActionBlock)) as ActionBlock;
                if (actionBlock != null)
                {
                    // 創建對應的 BaseAction 並加入子集合
                    // TODO: 根據 ActionType 創建實際的 Action 實例
                    e.Handled = true;
                }
            }
        }
    }
}
