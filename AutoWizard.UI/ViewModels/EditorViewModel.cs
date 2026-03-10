using Prism.Commands;
using Prism.Mvvm;
using System;
using System.Text.Json;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using AutoWizard.Core.Models;
using AutoWizard.Core.Actions.Control;

namespace AutoWizard.UI.ViewModels
{
    public class EditorViewModel : BindableBase
    {
        private ActionNodeWrapper? _selectedNode;
        public ActionNodeWrapper? SelectedNode
        {
            get => _selectedNode;
            set
            {
                if (SetProperty(ref _selectedNode, value))
                {
                    var action = value?.Action;
                    SetProperty(ref _selectedAction, action, nameof(SelectedAction));
                    SelectedActionChanged?.Invoke(this, action);
                    MoveUpCommand.RaiseCanExecuteChanged();
                    MoveDownCommand.RaiseCanExecuteChanged();
                    DeleteActionCommand.RaiseCanExecuteChanged();
                    AddChildCommand.RaiseCanExecuteChanged();
                    CutCommand.RaiseCanExecuteChanged();
                    CopyCommand.RaiseCanExecuteChanged();
                    PasteCommand.RaiseCanExecuteChanged();
                }
            }
        }

        private BaseAction? _selectedAction;
        public BaseAction? SelectedAction
        {
            get => _selectedAction;
            set
            {
                if (SetProperty(ref _selectedAction, value))
                {
                    // 從 FlattenedNodes 找到對應的 wrapper
                    var node = FlattenedNodes.FirstOrDefault(n => n.Action == value);
                    if (node != null && node != _selectedNode)
                    {
                        SelectedNode = node;
                    }
                }
            }
        }

        /// <summary>頂層 Action 清單（實際資料來源）</summary>
        public ObservableCollection<BaseAction> Actions { get; } = new();

        /// <summary>扁平化顯示清單（UI 綁定來源）</summary>
        public ObservableCollection<ActionNodeWrapper> FlattenedNodes { get; } = new();

        /// <summary>目前多選的節點清單</summary>
        public List<ActionNodeWrapper> SelectedNodes { get; } = new();

        /// <summary>
        /// 由 View 的 SelectionChanged 事件呼叫，同步 ListBox.SelectedItems
        /// </summary>
        public void SyncSelectedNodes(IList items)
        {
            SelectedNodes.Clear();
            foreach (var item in items)
            {
                if (item is ActionNodeWrapper node)
                {
                    SelectedNodes.Add(node);
                }
            }
            MoveUpCommand.RaiseCanExecuteChanged();
            MoveDownCommand.RaiseCanExecuteChanged();
            DeleteActionCommand.RaiseCanExecuteChanged();
            CutCommand.RaiseCanExecuteChanged();
            CopyCommand.RaiseCanExecuteChanged();
            PasteCommand.RaiseCanExecuteChanged();
        }

        private string _scriptName = "未命名腳本";
        public string ScriptName
        {
            get => _scriptName;
            set => SetProperty(ref _scriptName, value);
        }

        private bool _isDirty;
        public bool IsDirty
        {
            get => _isDirty;
            set => SetProperty(ref _isDirty, value);
        }

        private bool _isCodeViewMode;
        public bool IsCodeViewMode
        {
            get => _isCodeViewMode;
            set
            {
                if (_isCodeViewMode != value)
                {
                    if (value)
                    {
                        // Switch to Code View
                        SyncVisualToCode();
                        SetProperty(ref _isCodeViewMode, value);
                    }
                    else
                    {
                        // Switch to Visual View
                        if (SyncCodeToVisual())
                        {
                            SetProperty(ref _isCodeViewMode, value);
                        }
                    }
                }
            }
        }

        private string _jsonSource = string.Empty;
        public string JsonSource
        {
            get => _jsonSource;
            set => SetProperty(ref _jsonSource, value);
        }

        private static readonly JsonSerializerOptions _jsonOptions = new()
        {
            WriteIndented = true,
            PropertyNameCaseInsensitive = true,
            Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() }
        };

        public ObservableCollection<CodeLineInfo> CodeLines { get; } = new();

        private void SyncVisualToCode()
        {
            try
            {
                var generator = new Core.Scripting.DslGenerator();
                JsonSource = generator.Generate(Actions);
                UpdateCodeLines();
            }
            catch (Exception ex)
            {
                JsonSource = $"// Error generating script: {ex.Message}";
                CodeLines.Clear();
            }
        }

        private bool SyncCodeToVisual()
        {
            try
            {
                var parser = new Core.Scripting.DslParser();
                var newActions = parser.Parse(JsonSource);
                
                if (newActions != null)
                {
                    Actions.Clear();
                    Actions.AddRange(newActions);
                    RebuildFlatList();
                    IsDirty = true;
                    // UpdateCodeLines(); // No need, switching away from code view
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Script Syntax Error:\n{ex.Message}", "Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                return false;
            }
        }

        public void UpdateCodeLines()
        {
            CodeLines.Clear();
            if (string.IsNullOrEmpty(JsonSource)) return;

            var lines = JsonSource.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
            for (int i = 0; i < lines.Length; i++)
            {
                CodeLines.Add(new CodeLineInfo { LineNumber = i + 1 });
            }
        }


        // Events
        public event EventHandler<BaseAction?>? SelectedActionChanged;

        // Commands
        public DelegateCommand MoveUpCommand { get; }
        public DelegateCommand MoveDownCommand { get; }
        public DelegateCommand DeleteActionCommand { get; }
        public DelegateCommand AddChildCommand { get; }
        public DelegateCommand UndoCommand { get; }
        public DelegateCommand RedoCommand { get; }
        public DelegateCommand CutCommand { get; }
        public DelegateCommand CopyCommand { get; }
        public DelegateCommand PasteCommand { get; }

        /// <summary>Undo/Redo 管理器</summary>
        public UndoRedoManager UndoManager { get; } = new();

        /// <summary>剪下的指令剪貼簿：(Action, 原所屬清單, 原始索引)</summary>
        private List<(BaseAction Action, IList<BaseAction> OriginalList, int OriginalIndex)> _clipboard = new();

        /// <summary>剖貼簿是否來自剪下（true=剪下，false=複製）</summary>
        private bool _isClipboardFromCut;

        private bool _suppressRebuild;

        public EditorViewModel()
        {
            MoveUpCommand = new DelegateCommand(OnMoveUp, CanMoveUp);
            MoveDownCommand = new DelegateCommand(OnMoveDown, CanMoveDown);
            DeleteActionCommand = new DelegateCommand(OnDeleteAction, () => GetSelectedActionNodes().Count > 0);
            AddChildCommand = new DelegateCommand(OnAddChild, CanAddChild);
            UndoCommand = new DelegateCommand(OnUndo, () => UndoManager.CanUndo);
            RedoCommand = new DelegateCommand(OnRedo, () => UndoManager.CanRedo);
            CutCommand = new DelegateCommand(OnCut, () => GetSelectedActionNodes().Count > 0);
            CopyCommand = new DelegateCommand(OnCopy, () => GetSelectedActionNodes().Count > 0);
            PasteCommand = new DelegateCommand(OnPaste, () => _clipboard.Count > 0 && SelectedNode != null);

            UndoManager.StateChanged += (_, _) =>
            {
                UndoCommand.RaiseCanExecuteChanged();
                RedoCommand.RaiseCanExecuteChanged();
            };

            Actions.CollectionChanged += (_, _) =>
            {
                if (!_suppressRebuild) RebuildFlatList();
            };
        }

        #region Flat List Builder

        /// <summary>
        /// 遞迴走訪 Actions 樹，產生扁平化節點清單
        /// </summary>
        public void RebuildFlatList()
        {
            var previousSelectedAction = SelectedNode?.Action;
            FlattenedNodes.Clear();

            foreach (var action in Actions)
            {
                FlattenAction(action, 0, null, Actions);
            }

            // 嘗試恢復選取
            if (previousSelectedAction != null)
            {
                var restored = FlattenedNodes.FirstOrDefault(n => n.Action == previousSelectedAction);
                if (restored != null)
                {
                    SelectedNode = restored;
                }
            }
        }

        private void FlattenAction(BaseAction action, int depth, BaseAction? parent, IList<BaseAction> parentList)
        {
            // 加入指令節點
            FlattenedNodes.Add(new ActionNodeWrapper
            {
                Action = action,
                Depth = depth,
                NodeType = NodeType.Action,
                ParentAction = parent,
                ParentList = parentList
            });

            // 處理容器子指令
            if (action is IfAction ifAction)
            {
                // Then 段落
                FlattenedNodes.Add(new ActionNodeWrapper
                {
                    Depth = depth + 1,
                    NodeType = NodeType.SectionHeader,
                    SectionLabel = "✅ 條件成立",
                    ParentAction = action,
                    ParentList = ifAction.ThenActions
                });

                foreach (var child in ifAction.ThenActions)
                {
                    FlattenAction(child, depth + 1, action, ifAction.ThenActions);
                }

                // Else 段落
                FlattenedNodes.Add(new ActionNodeWrapper
                {
                    Depth = depth + 1,
                    NodeType = NodeType.SectionHeader,
                    SectionLabel = "❌ 條件不成立",
                    ParentAction = action,
                    ParentList = ifAction.ElseActions
                });

                foreach (var child in ifAction.ElseActions)
                {
                    FlattenAction(child, depth + 1, action, ifAction.ElseActions);
                }

                // 容器結尾
                FlattenedNodes.Add(new ActionNodeWrapper
                {
                    Depth = depth,
                    NodeType = NodeType.ContainerEnd,
                    SectionLabel = "End If",
                    ParentAction = action
                });
            }
            else if (action is LoopAction loopAction)
            {
                // 子指令段落
                FlattenedNodes.Add(new ActionNodeWrapper
                {
                    Depth = depth + 1,
                    NodeType = NodeType.SectionHeader,
                    SectionLabel = "🔄 迴圈內容",
                    ParentAction = action,
                    ParentList = loopAction.Children
                });

                foreach (var child in loopAction.Children)
                {
                    FlattenAction(child, depth + 1, action, loopAction.Children);
                }

                // 容器結尾
                FlattenedNodes.Add(new ActionNodeWrapper
                {
                    Depth = depth,
                    NodeType = NodeType.ContainerEnd,
                    SectionLabel = "End Loop",
                    ParentAction = action
                });
            }
        }

        #endregion

        #region Public Methods

        public void AddAction(BaseAction action)
        {
            Actions.Add(action);
            // 選取新節點
            var node = FlattenedNodes.FirstOrDefault(n => n.Action == action);
            if (node != null) SelectedNode = node;
            IsDirty = true;
        }

        /// <summary>
        /// 將 action 新增到指定容器的子清單
        /// </summary>
        public void AddActionToContainer(BaseAction action, IList<BaseAction> targetList)
        {
            targetList.Add(action);
            RebuildFlatList();
            var node = FlattenedNodes.FirstOrDefault(n => n.Action == action);
            if (node != null) SelectedNode = node;
            IsDirty = true;
        }

        public void InsertAction(int index, BaseAction action)
        {
            if (index < 0) index = 0;
            if (index > Actions.Count) index = Actions.Count;
            Actions.Insert(index, action);
            var node = FlattenedNodes.FirstOrDefault(n => n.Action == action);
            if (node != null) SelectedNode = node;
            IsDirty = true;
        }

        public void RemoveAction(BaseAction action)
        {
            // 嘗試從頂層移除
            if (Actions.Remove(action))
            {
                IsDirty = true;
                return;
            }

            // 嘗試從所屬容器子清單移除
            var node = FlattenedNodes.FirstOrDefault(n => n.Action == action);
            if (node?.ParentList != null)
            {
                node.ParentList.Remove(action);
                RebuildFlatList();
                IsDirty = true;
            }
        }

        public void ClearActions()
        {
            Actions.Clear();
            SelectedNode = null;
            IsDirty = false;
            UndoManager.Clear();
        }

        #endregion

        #region Command Handlers

        private bool CanMoveUp()
        {
            var actionNodes = GetSelectedActionNodes();
            if (actionNodes.Count == 0) return false;

            // 按 ParentList 分組，檢查是否已在最頂端
            var grouped = actionNodes.GroupBy(n => n.ParentList);
            foreach (var group in grouped)
            {
                var list = group.Key;
                if (list == null) return false;
                var indices = group.Select(n => list.IndexOf(n.Action!)).OrderBy(i => i).ToList();
                // 如果選取的索引是 0,1,2... 的連續序列，則無法上移
                bool allAtTop = true;
                for (int i = 0; i < indices.Count; i++)
                {
                    if (indices[i] != i) { allAtTop = false; break; }
                }
                if (allAtTop) return false;
            }
            return true;
        }

        private void OnMoveUp()
        {
            var actionNodes = GetSelectedActionNodes();
            if (actionNodes.Count == 0) return;

            var movedItems = actionNodes
                .Where(n => n.Action != null && n.ParentList != null)
                .Select(n => (Action: n.Action!, ParentList: n.ParentList!))
                .ToList();

            PerformMoveItems(movedItems, moveUp: true);

            // 記錄 Undo 操作
            var moveOp = new MoveOperation(
                movedItems,
                MoveOperation.MoveDirection.Up,
                (items, up) => PerformMoveItems(items, up),
                RebuildFlatList);
            UndoManager.Push(moveOp);

            var selectedActions = actionNodes.Select(n => n.Action!).ToHashSet();
            RebuildFlatList();
            RestoreMultiSelection(selectedActions);
            IsDirty = true;
        }

        private bool CanMoveDown()
        {
            var actionNodes = GetSelectedActionNodes();
            if (actionNodes.Count == 0) return false;

            // 檢查同一 ParentList 中的選取節點是否已經在最底端（連續的底端區塊）
            var grouped = actionNodes.GroupBy(n => n.ParentList);
            foreach (var group in grouped)
            {
                var list = group.Key;
                if (list == null) return false;
                var indices = group.Select(n => list.IndexOf(n.Action!)).OrderByDescending(i => i).ToList();
                bool allAtBottom = true;
                for (int i = 0; i < indices.Count; i++)
                {
                    if (indices[i] != list.Count - 1 - i) { allAtBottom = false; break; }
                }
                if (allAtBottom) return false;
            }
            return true;
        }

        private void OnMoveDown()
        {
            var actionNodes = GetSelectedActionNodes();
            if (actionNodes.Count == 0) return;

            var movedItems = actionNodes
                .Where(n => n.Action != null && n.ParentList != null)
                .Select(n => (Action: n.Action!, ParentList: n.ParentList!))
                .ToList();

            PerformMoveItems(movedItems, moveUp: false);

            // 記錄 Undo 操作
            var moveOp = new MoveOperation(
                movedItems,
                MoveOperation.MoveDirection.Down,
                (items, up) => PerformMoveItems(items, up),
                RebuildFlatList);
            UndoManager.Push(moveOp);

            var selectedActions = actionNodes.Select(n => n.Action!).ToHashSet();
            RebuildFlatList();
            RestoreMultiSelection(selectedActions);
            IsDirty = true;
        }

        private void OnDeleteAction()
        {
            var actionNodes = GetSelectedActionNodes();
            if (actionNodes.Count == 0) return;

            // 收集要刪除的 action 並記錄原始索引，用於 Undo 復原
            var toDelete = actionNodes
                .Where(n => n.Action != null && n.ParentList != null)
                .Select(n => (Action: n.Action!, List: n.ParentList!, Index: n.ParentList!.IndexOf(n.Action!)))
                .ToList();

            // 記錄 Undo 操作（必須在刪除前記錄索引）
            var deleteOp = new DeleteOperation(toDelete, RebuildFlatList);

            _suppressRebuild = true;
            try
            {
                // 從高索引往低索引刪除，避免索引偏移
                foreach (var (action, list, _) in toDelete.OrderByDescending(x => x.Index))
                {
                    list.Remove(action);
                }
            }
            finally
            {
                _suppressRebuild = false;
            }

            UndoManager.Push(deleteOp);

            RebuildFlatList();
            SelectedNodes.Clear();

            // 選取最近的可用節點
            if (FlattenedNodes.Count > 0)
            {
                var nearest = FlattenedNodes.FirstOrDefault(n => n.Action != null && n.NodeType == NodeType.Action);
                SelectedNode = nearest;
            }
            else
            {
                SelectedNode = null;
            }

            IsDirty = true;
        }

        private bool CanAddChild()
        {
            if (SelectedNode == null) return false;
            // 可以新增子指令的情況：選取的是容器指令，或是段落標題
            return SelectedNode.Action is ContainerAction
                || SelectedNode.NodeType == NodeType.SectionHeader;
        }

        private void OnAddChild()
        {
            // 由 MainWindowViewModel 處理實際的新增邏輯
            AddChildRequested?.Invoke(this, SelectedNode!);
        }

        /// <summary>
        /// 請求新增子指令事件（由 MainWindowViewModel 訂閱）
        /// </summary>
        public event EventHandler<ActionNodeWrapper>? AddChildRequested;

        /// <summary>
        /// 執行移動操作的共用方法（供 OnMoveUp/OnMoveDown 及 MoveOperation 復原使用）
        /// </summary>
        internal void PerformMoveItems(List<(BaseAction Action, IList<BaseAction> ParentList)> items, bool moveUp)
        {
            _suppressRebuild = true;
            try
            {
                var movedActions = new HashSet<BaseAction>();

                // 按 ParentList 分組
                var grouped = items.GroupBy(x => x.ParentList);
                foreach (var group in grouped)
                {
                    var list = group.Key;
                    if (list == null) continue;

                    if (moveUp)
                    {
                        var sorted = group.OrderBy(x => list.IndexOf(x.Action)).ToList();
                        foreach (var (action, _) in sorted)
                        {
                            int idx = list.IndexOf(action);
                            if (idx > 0 && !movedActions.Contains(list[idx - 1]))
                            {
                                list.RemoveAt(idx);
                                list.Insert(idx - 1, action);
                                movedActions.Add(action);
                            }
                        }
                    }
                    else
                    {
                        var sorted = group.OrderByDescending(x => list.IndexOf(x.Action)).ToList();
                        foreach (var (action, _) in sorted)
                        {
                            int idx = list.IndexOf(action);
                            if (idx < list.Count - 1 && !movedActions.Contains(list[idx + 1]))
                            {
                                list.RemoveAt(idx);
                                list.Insert(idx + 1, action);
                                movedActions.Add(action);
                            }
                        }
                    }
                }
            }
            finally
            {
                _suppressRebuild = false;
            }
        }

        private void OnUndo()
        {
            UndoManager.Undo();
            // RebuildFlatList 已在 Operation 的 Undo 中呼叫
        }

        private void OnRedo()
        {
            UndoManager.Redo();
            // RebuildFlatList 已在 Operation 的 Redo 中呼叫
        }

        #endregion

        #region Multi-Selection Helpers

        /// <summary>
        /// 取得所有選中的 Action 節點（過濾掉段落標題與容器結尾）
        /// </summary>
        private List<ActionNodeWrapper> GetSelectedActionNodes()
        {
            // 優先使用多選清單，如果為空則使用單選
            if (SelectedNodes.Count > 0)
            {
                return SelectedNodes
                    .Where(n => n.NodeType == NodeType.Action && n.Action != null)
                    .ToList();
            }

            if (SelectedNode?.NodeType == NodeType.Action && SelectedNode?.Action != null)
            {
                return new List<ActionNodeWrapper> { SelectedNode };
            }

            return new List<ActionNodeWrapper>();
        }

        /// <summary>
        /// 在 RebuildFlatList 後恢復多選狀態
        /// </summary>
        private void RestoreMultiSelection(HashSet<BaseAction> selectedActions)
        {
            // 通知 View 恢復選取（透過事件）
            var restoredNodes = FlattenedNodes
                .Where(n => n.Action != null && selectedActions.Contains(n.Action))
                .ToList();

            if (restoredNodes.Count > 0)
            {
                SelectedNode = restoredNodes[0];
                SelectedNodes.Clear();
                SelectedNodes.AddRange(restoredNodes);
                MultiSelectionRestoreRequested?.Invoke(this, restoredNodes);
            }
        }

        /// <summary>
        /// 請求 View 恢復多選狀態的事件
        /// </summary>
        public event EventHandler<List<ActionNodeWrapper>>? MultiSelectionRestoreRequested;

        #endregion

        #region Cut / Paste

        /// <summary>
        /// 剪下選取的指令（類似 Windows 檔案總管：號碼變淡，貼上後才真正移除）
        /// </summary>
        private void OnCut()
        {
            var actionNodes = GetSelectedActionNodes();
            if (actionNodes.Count == 0) return;

            // 清除上一次剪下狀態
            ClearClipboard();

            // 記錄剪下的指令
            _clipboard = actionNodes
                .Where(n => n.Action != null && n.ParentList != null)
                .Select(n => (Action: n.Action!, OriginalList: n.ParentList!, OriginalIndex: n.ParentList!.IndexOf(n.Action!)))
                .ToList();

            // 標記 IsCut 狀態（視覺變淡）
            var cutActions = _clipboard.Select(c => c.Action).ToHashSet();
            foreach (var node in FlattenedNodes)
            {
                if (node.Action != null && cutActions.Contains(node.Action))
                {
                    node.IsCut = true;
                }
            }

            PasteCommand.RaiseCanExecuteChanged();
            _isClipboardFromCut = true;
        }

        /// <summary>
        /// 複製選取的指令（深拷貝，不影響原始指令）
        /// </summary>
        private void OnCopy()
        {
            var actionNodes = GetSelectedActionNodes();
            if (actionNodes.Count == 0) return;

            // 清除上一次剪下狀態
            ClearClipboard();

            // 使用 JSON 序列化/反序列化進行深拷貝
            var options = new JsonSerializerOptions { WriteIndented = false };
            _clipboard = actionNodes
                .Where(n => n.Action != null && n.ParentList != null)
                .Select(n =>
                {
                    // 深拷貝 Action
                    var json = JsonSerializer.Serialize<BaseAction>(n.Action!, options);
                    var cloned = JsonSerializer.Deserialize<BaseAction>(json, options)!;
                    cloned.Id = Guid.NewGuid().ToString(); // 給新 ID
                    return (Action: cloned, OriginalList: n.ParentList!, OriginalIndex: n.ParentList!.IndexOf(n.Action!));
                })
                .ToList();

            _isClipboardFromCut = false;
            PasteCommand.RaiseCanExecuteChanged();
        }

        /// <summary>
        /// 貼上剪下的指令到選取指令的下方
        /// </summary>
        private void OnPaste()
        {
            if (_clipboard.Count == 0 || SelectedNode == null) return;

            // 決定插入位置：選取節點的下方
            var targetNode = SelectedNode;
            IList<BaseAction> targetList;
            int insertIndex;

            if (targetNode.NodeType == NodeType.SectionHeader && targetNode.ParentList != null)
            {
                targetList = targetNode.ParentList;
                insertIndex = 0;
            }
            else if (targetNode.Action != null && targetNode.ParentList != null)
            {
                targetList = targetNode.ParentList;
                insertIndex = targetList.IndexOf(targetNode.Action) + 1;
            }
            else
            {
                targetList = Actions;
                insertIndex = Actions.Count;
            }

            if (_isClipboardFromCut)
            {
                // === 剪下貼上：從原位置移除並插入新位置 ===
                var removeRecords = _clipboard
                    .Select(c => (c.Action, c.OriginalList, c.OriginalIndex))
                    .ToList();

                _suppressRebuild = true;
                try
                {
                    foreach (var (action, origList, _) in removeRecords.OrderByDescending(x => x.OriginalIndex))
                    {
                        origList.Remove(action);
                    }

                    if (insertIndex > targetList.Count) insertIndex = targetList.Count;
                    for (int i = 0; i < _clipboard.Count; i++)
                    {
                        targetList.Insert(insertIndex + i, _clipboard[i].Action);
                    }
                }
                finally
                {
                    _suppressRebuild = false;
                }

                var pastedActions = _clipboard.Select(c => c.Action).ToList();
                var pasteOp = new CutPasteOperation(removeRecords, pastedActions, targetList, insertIndex, RebuildFlatList);
                UndoManager.Push(pasteOp);

                _clipboard.Clear();
                RebuildFlatList();

                var pastedSet = pastedActions.ToHashSet();
                RestoreMultiSelection(pastedSet);
            }
            else
            {
                // === 複製貼上：直接插入深拷貝的副本，保留剪貼簿以便多次貼上 ===
                var options = new JsonSerializerOptions { WriteIndented = false };
                var clonedActions = _clipboard.Select(c =>
                {
                    var json = JsonSerializer.Serialize<BaseAction>(c.Action, options);
                    var cloned = JsonSerializer.Deserialize<BaseAction>(json, options)!;
                    cloned.Id = Guid.NewGuid().ToString();
                    return cloned;
                }).ToList();

                _suppressRebuild = true;
                try
                {
                    if (insertIndex > targetList.Count) insertIndex = targetList.Count;
                    for (int i = 0; i < clonedActions.Count; i++)
                    {
                        targetList.Insert(insertIndex + i, clonedActions[i]);
                    }
                }
                finally
                {
                    _suppressRebuild = false;
                }

                // 記錄為 DeleteOperation 的反向（Undo 時刪除插入的副本）
                var insertRecords = clonedActions
                    .Select((a, i) => (Action: a, ParentList: targetList, Index: insertIndex + i))
                    .ToList();
                var deleteOp = new DeleteOperation(insertRecords, RebuildFlatList);
                UndoManager.Push(deleteOp);

                RebuildFlatList();

                var insertedSet = clonedActions.ToHashSet();
                RestoreMultiSelection(insertedSet);
            }

            PasteCommand.RaiseCanExecuteChanged();
            IsDirty = true;
        }

        /// <summary>
        /// 清除剪貼簿並重置所有 IsCut 狀態
        /// </summary>
        public void ClearClipboard()
        {
            if (_clipboard.Count == 0) return;

            var cutActions = _clipboard.Select(c => c.Action).ToHashSet();
            foreach (var node in FlattenedNodes)
            {
                if (node.Action != null && cutActions.Contains(node.Action))
                {
                    node.IsCut = false;
                }
            }
            _clipboard.Clear();
            PasteCommand.RaiseCanExecuteChanged();
        }

        #endregion
    }

    public class CodeLineInfo
    {
        public int LineNumber { get; set; }
        public string BackgroundColor { get; set; } = "Transparent";
    }
}
