using System;
using System.Collections.Generic;
using System.Linq;
using AutoWizard.Core.Models;

namespace AutoWizard.UI.ViewModels
{
    /// <summary>
    /// 可復原操作介面
    /// </summary>
    public interface IUndoableOperation
    {
        /// <summary>執行復原</summary>
        void Undo();

        /// <summary>執行重做</summary>
        void Redo();

        /// <summary>操作描述</summary>
        string Description { get; }
    }

    /// <summary>
    /// Undo/Redo 管理器，維護操作歷史堆疊（各最多 MaxHistory 筆）
    /// </summary>
    public class UndoRedoManager
    {
        public const int MaxHistory = 20;

        private readonly LinkedList<IUndoableOperation> _undoStack = new();
        private readonly LinkedList<IUndoableOperation> _redoStack = new();

        public bool CanUndo => _undoStack.Count > 0;
        public bool CanRedo => _redoStack.Count > 0;

        /// <summary>通知 CanUndo/CanRedo 狀態變化</summary>
        public event EventHandler? StateChanged;

        /// <summary>
        /// 推入一個可復原操作。同時清空 Redo 堆疊。
        /// </summary>
        public void Push(IUndoableOperation operation)
        {
            _undoStack.AddLast(operation);
            if (_undoStack.Count > MaxHistory)
            {
                _undoStack.RemoveFirst();
            }
            _redoStack.Clear();
            StateChanged?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>復原上一個操作</summary>
        public void Undo()
        {
            if (_undoStack.Count == 0) return;

            var op = _undoStack.Last!.Value;
            _undoStack.RemoveLast();

            op.Undo();

            _redoStack.AddLast(op);
            if (_redoStack.Count > MaxHistory)
            {
                _redoStack.RemoveFirst();
            }
            StateChanged?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>重做上一個復原的操作</summary>
        public void Redo()
        {
            if (_redoStack.Count == 0) return;

            var op = _redoStack.Last!.Value;
            _redoStack.RemoveLast();

            op.Redo();

            _undoStack.AddLast(op);
            if (_undoStack.Count > MaxHistory)
            {
                _undoStack.RemoveFirst();
            }
            StateChanged?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>清除所有歷史記錄</summary>
        public void Clear()
        {
            _undoStack.Clear();
            _redoStack.Clear();
            StateChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    /// <summary>
    /// 刪除操作的記錄（可復原）
    /// </summary>
    public class DeleteOperation : IUndoableOperation
    {
        /// <summary>被刪除指令的快照：(指令, 所屬清單, 原始索引)</summary>
        private readonly List<(BaseAction Action, IList<BaseAction> ParentList, int Index)> _deletedItems;
        private readonly Action _rebuildFlatList;

        public string Description { get; }

        public DeleteOperation(
            List<(BaseAction Action, IList<BaseAction> ParentList, int Index)> deletedItems,
            Action rebuildFlatList)
        {
            _deletedItems = deletedItems;
            _rebuildFlatList = rebuildFlatList;
            Description = $"刪除 {deletedItems.Count} 個指令";
        }

        public void Undo()
        {
            // 按原始索引正序插回（確保多項目時索引正確）
            foreach (var (action, parentList, index) in _deletedItems.OrderBy(x => x.Index))
            {
                var insertAt = Math.Min(index, parentList.Count);
                parentList.Insert(insertAt, action);
            }
            _rebuildFlatList();
        }

        public void Redo()
        {
            // 反向刪除（從高索引往低索引，避免索引偏移）
            foreach (var (action, parentList, _) in _deletedItems.OrderByDescending(x => x.Index))
            {
                parentList.Remove(action);
            }
            _rebuildFlatList();
        }
    }

    /// <summary>
    /// 移動操作的記錄（可復原）
    /// </summary>
    public class MoveOperation : IUndoableOperation
    {
        public enum MoveDirection { Up, Down }

        private readonly List<(BaseAction Action, IList<BaseAction> ParentList)> _movedItems;
        private readonly MoveDirection _direction;
        private readonly Action<List<(BaseAction, IList<BaseAction>)>, bool> _performMove;
        private readonly Action _rebuildFlatList;

        public string Description { get; }

        /// <param name="movedItems">被移動的指令及其所屬清單</param>
        /// <param name="direction">原始移動方向</param>
        /// <param name="performMove">執行移動的委派：(items, moveUp) => void</param>
        /// <param name="rebuildFlatList">重建扁平清單的委派</param>
        public MoveOperation(
            List<(BaseAction Action, IList<BaseAction> ParentList)> movedItems,
            MoveDirection direction,
            Action<List<(BaseAction, IList<BaseAction>)>, bool> performMove,
            Action rebuildFlatList)
        {
            _movedItems = movedItems;
            _direction = direction;
            _performMove = performMove;
            _rebuildFlatList = rebuildFlatList;
            Description = $"移動 {movedItems.Count} 個指令{(direction == MoveDirection.Up ? "上" : "下")}移";
        }

        public void Undo()
        {
            // 反向移動
            bool moveUp = _direction != MoveDirection.Up; // 反轉
            _performMove(_movedItems, moveUp);
            _rebuildFlatList();
        }

        public void Redo()
        {
            bool moveUp = _direction == MoveDirection.Up;
            _performMove(_movedItems, moveUp);
            _rebuildFlatList();
        }
    }

    /// <summary>
    /// 剪下/貼上操作的記錄（可復原）
    /// </summary>
    public class CutPasteOperation : IUndoableOperation
    {
        /// <summary>原始位置記錄：(指令, 原所屬清單, 原始索引)</summary>
        private readonly List<(BaseAction Action, IList<BaseAction> OriginalList, int OriginalIndex)> _originalPositions;
        /// <summary>被貼上的指令</summary>
        private readonly List<BaseAction> _pastedActions;
        /// <summary>貼上目標清單</summary>
        private readonly IList<BaseAction> _targetList;
        /// <summary>貼上起始索引</summary>
        private readonly int _insertIndex;
        private readonly Action _rebuildFlatList;

        public string Description { get; }

        public CutPasteOperation(
            List<(BaseAction Action, IList<BaseAction> OriginalList, int OriginalIndex)> originalPositions,
            List<BaseAction> pastedActions,
            IList<BaseAction> targetList,
            int insertIndex,
            Action rebuildFlatList)
        {
            _originalPositions = originalPositions;
            _pastedActions = pastedActions;
            _targetList = targetList;
            _insertIndex = insertIndex;
            _rebuildFlatList = rebuildFlatList;
            Description = $"剪下貼上 {pastedActions.Count} 個指令";
        }

        public void Undo()
        {
            // 從目標位置移除
            foreach (var action in _pastedActions)
            {
                _targetList.Remove(action);
            }

            // 按原始索引正序插回原位
            foreach (var (action, origList, origIndex) in _originalPositions.OrderBy(x => x.OriginalIndex))
            {
                var insertAt = Math.Min(origIndex, origList.Count);
                origList.Insert(insertAt, action);
            }
            _rebuildFlatList();
        }

        public void Redo()
        {
            // 從原位置移除
            foreach (var (action, origList, _) in _originalPositions.OrderByDescending(x => x.OriginalIndex))
            {
                origList.Remove(action);
            }

            // 插入到目標位置
            var idx = Math.Min(_insertIndex, _targetList.Count);
            for (int i = 0; i < _pastedActions.Count; i++)
            {
                _targetList.Insert(idx + i, _pastedActions[i]);
            }
            _rebuildFlatList();
        }
    }
}
