using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using AutoWizard.Core.Models;

namespace AutoWizard.UI.ViewModels
{
    /// <summary>
    /// 節點類型
    /// </summary>
    public enum NodeType
    {
        /// <summary>實際指令</summary>
        Action,
        /// <summary>段落標題（如「條件成立」）</summary>
        SectionHeader,
        /// <summary>容器結尾標記</summary>
        ContainerEnd
    }

    /// <summary>
    /// 扁平化樹節點，包裝 BaseAction 並攜帶深度/段落中繼資料
    /// </summary>
    public class ActionNodeWrapper : INotifyPropertyChanged
    {
        /// <summary>實際指令（段落標題和容器結尾時為 null）</summary>
        public BaseAction? Action { get; set; }

        /// <summary>巢狀深度（0 = 頂層）</summary>
        public int Depth { get; set; }

        /// <summary>節點類型</summary>
        public NodeType NodeType { get; set; } = NodeType.Action;

        /// <summary>段落標題文字（如「子指令」「條件成立」「條件不成立」）</summary>
        public string SectionLabel { get; set; } = string.Empty;

        /// <summary>所屬容器指令</summary>
        public BaseAction? ParentAction { get; set; }

        /// <summary>所屬的子清單引用，用於增刪操作</summary>
        public IList<BaseAction>? ParentList { get; set; }

        /// <summary>左側縮排 Margin（Depth * 24）</summary>
        public double LeftIndent => Depth * 24.0;

        /// <summary>是否為容器指令</summary>
        public bool IsContainer => Action is ContainerAction;

        private bool _isCut;
        /// <summary>是否處於剪下狀態（視覺變淡）</summary>
        public bool IsCut
        {
            get => _isCut;
            set
            {
                if (_isCut != value)
                {
                    _isCut = value;
                    OnPropertyChanged();
                }
            }
        }

        private bool _isExecuting;
        /// <summary>是否正在執行中（顯示高亮）</summary>
        public bool IsExecuting
        {
            get => _isExecuting;
            set
            {
                if (_isExecuting != value)
                {
                    _isExecuting = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>用於顯示的名稱</summary>
        public string DisplayName
        {
            get
            {
                if (NodeType == NodeType.SectionHeader)
                    return SectionLabel;
                if (NodeType == NodeType.ContainerEnd)
                    return "";
                return Action?.Name ?? "";
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
