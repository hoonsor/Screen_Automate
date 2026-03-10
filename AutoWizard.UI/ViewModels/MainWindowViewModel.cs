using Prism.Mvvm;
using Prism.Commands;
using System.Windows.Input;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Threading;
using AutoWizard.Core.Engine;
using AutoWizard.Core.Models;
using AutoWizard.Core.Actions.Input;
using AutoWizard.Core.Actions.Control;
using AutoWizard.Core.Actions.Vision;
using System.Drawing;
using System.Threading.Tasks;

namespace AutoWizard.UI.ViewModels
{
    public class MainWindowViewModel : BindableBase
    {
        private string _title = "AutoWizard Desktop";
        public string Title
        {
            get => _title;
            set => SetProperty(ref _title, value);
        }

        private string _statusMessage = "就緒";
        public string StatusMessage
        {
            get => _statusMessage;
            set => SetProperty(ref _statusMessage, value);
        }

        private bool _isRunning;
        public bool IsRunning
        {
            get => _isRunning;
            set
            {
                if (SetProperty(ref _isRunning, value))
                {
                    // 通知按鈕重新評估 CanExecute
                    ((DelegateCommand)RunScriptCommand).RaiseCanExecuteChanged();
                    ((DelegateCommand)StopScriptCommand).RaiseCanExecuteChanged();
                    ((DelegateCommand)RecordCommand).RaiseCanExecuteChanged();
                }
            }
        }

        private string _executionStatusText = string.Empty;
        public string ExecutionStatusText
        {
            get => _executionStatusText;
            set => SetProperty(ref _executionStatusText, value);
        }

        private bool _isLogPanelExpanded = false;
        public bool IsLogPanelExpanded
        {
            get => _isLogPanelExpanded;
            set => SetProperty(ref _isLogPanelExpanded, value);
        }

        public ObservableCollection<string> ExecutionLogs { get; } = new();

        public ICommand NewScriptCommand { get; }
        public ICommand OpenScriptCommand { get; }
        public ICommand SaveScriptCommand { get; }
        public ICommand SaveScriptAsCommand { get; }
        public ICommand RunScriptCommand { get; }
        public ICommand StopScriptCommand { get; }
        public ICommand RecordCommand { get; }
        public ICommand ToggleRunCommand { get; }
        public ICommand ClearLogsCommand { get; }
        public ICommand ExportLogsCommand { get; }
        public DelegateCommand<string> AddActionCommand { get; }
        public DelegateCommand<string> AddChildActionCommand { get; }

        // 子 ViewModels
        public ToolboxViewModel ToolboxViewModel { get; }
        public EditorViewModel EditorViewModel { get; }
        public PropertiesViewModel PropertiesViewModel { get; }
        public VariablesViewModel VariablesViewModel { get; }

        // 執行引擎
        private readonly ScriptExecutor _executor;
        private Core.Recording.Recorder? _recorder;
        private Views.RecordingOverlay? _recordingOverlay;
        private Views.RecordingToolbar? _recordingToolbar;
        private string? _currentFilePath;

        public MainWindowViewModel()
        {
            _executor = new ScriptExecutor();
            _executor.LogReceived += OnLogReceived;
            _executor.StatusChanged += OnExecutionStatusChanged;

            NewScriptCommand = new DelegateCommand(OnNewScript);
            OpenScriptCommand = new DelegateCommand(OnOpenScript);
            SaveScriptCommand = new DelegateCommand(OnSaveScript);
            SaveScriptAsCommand = new DelegateCommand(OnSaveScriptAs);
            RunScriptCommand = new DelegateCommand(OnRunScript, CanRunScript);
            StopScriptCommand = new DelegateCommand(OnStopScript, CanStopScript);
            RecordCommand = new DelegateCommand(OnRecord, () => !IsRunning);
            ClearLogsCommand = new DelegateCommand(() => 
            {
                ExecutionLogs.Clear();
                ((DelegateCommand)ExportLogsCommand).RaiseCanExecuteChanged();
            });
            ExportLogsCommand = new DelegateCommand(OnExportLogs, () => ExecutionLogs.Any());
            AddActionCommand = new DelegateCommand<string>(OnAddAction);
            AddChildActionCommand = new DelegateCommand<string>(OnAddChildWithType);
            ToggleRunCommand = new DelegateCommand(OnToggleRun);

            // 初始化子 ViewModels
            ToolboxViewModel = new ToolboxViewModel();
            EditorViewModel = new EditorViewModel();
            PropertiesViewModel = new PropertiesViewModel();
            VariablesViewModel = new VariablesViewModel();

            VariablesViewModel.PickColorAction = PickColorInternal;

            // 選取連動 → 屬性面板
            EditorViewModel.SelectedActionChanged += (_, action) =>
            {
                PropertiesViewModel.SelectedItem = action;
            };

            // 子指令新增請求
            EditorViewModel.AddChildRequested += (_, node) =>
            {
                OnAddChildToContainer(node);
            };

            // 當 Actions 變更時，重新評估 RunScript 的 CanExecute
            EditorViewModel.Actions.CollectionChanged += (_, _) =>
            {
                ((DelegateCommand)RunScriptCommand).RaiseCanExecuteChanged();
            };

            // 確保啟動時就建立內置變數
            VariablesViewModel.EnsureBuiltInColorVariables();
        }

        #region Add Action Factory

        private BaseAction? CreateAction(string actionType)
        {
            return actionType switch
            {
                "Click" => new ClickAction { Name = "點擊", X = 0, Y = 0, Button = AutoWizard.Core.Actions.Input.MouseButton.Left, ClickType = ClickType.Single },
                "MouseDown" => new ClickAction { Name = "按下", X = 0, Y = 0, Button = AutoWizard.Core.Actions.Input.MouseButton.Left, ClickType = ClickType.Down },
                "MouseUp" => new ClickAction { Name = "放開", X = 0, Y = 0, Button = AutoWizard.Core.Actions.Input.MouseButton.Left, ClickType = ClickType.Up },
                "Type" => new TypeAction { Name = "輸入文字", Text = "" },
                "If" => new IfAction { Name = "條件判斷", ConditionType = ConditionType.VariableEquals },
                "Loop" => new LoopAction { Name = "迴圈", LoopType = LoopType.Count, Count = 3 },
                "FindImage" => new FindImageAction { Name = "尋找影像" },
                "OCR" => new OCRAction { Name = "OCR 辨識" },
                "Wait" => new WaitAction { Name = "等待", DurationMs = 1000 },
                "Keyboard" => new KeyboardAction { Name = "快捷鍵", Key = "Enter" },
                "SetVariable" => new SetVariableAction { Name = "設定變數", VariableName = "var1", ValueExpression = "" },
                "Screenshot" => new ScreenshotAction { Name = "螢幕截圖" },
                "Window" => new WindowAction { Name = "視窗綁定", TargetWindowTitle = "" },
                _ => null
            };
        }

        private void OnAddAction(string actionType)
        {
            var action = CreateAction(actionType);
            if (action != null)
            {
                // 檢查是否有選取的容器，若有則加入該容器
                if (EditorViewModel.SelectedNode != null && 
                    (EditorViewModel.SelectedNode.Action is ContainerAction || EditorViewModel.SelectedNode.NodeType == NodeType.SectionHeader))
                {
                    // 根據選取節點決定目標清單
                    System.Collections.Generic.IList<BaseAction>? targetList = null;
                    
                    if (EditorViewModel.SelectedNode.NodeType == NodeType.SectionHeader)
                    {
                        targetList = EditorViewModel.SelectedNode.ParentList;
                    }
                    else if (EditorViewModel.SelectedNode.Action is LoopAction loop)
                    {
                        targetList = loop.Children;
                    }
                    else if (EditorViewModel.SelectedNode.Action is IfAction ifAction)
                    {
                        targetList = ifAction.ThenActions;
                    }

                    if (targetList != null)
                    {
                        EditorViewModel.AddActionToContainer(action, targetList);
                        StatusMessage = $"已新增至容器: {action.Name}";
                        return;
                    }
                }

                // 預設行為：新增至根目錄
                EditorViewModel.AddAction(action);
                StatusMessage = $"已新增: {action.Name}";
            }
        }

        private void OnAddChildToContainer(ActionNodeWrapper node)
        {
            // Fallback：預設新增 Click（保持向後相容）
            AddChildToContainerWithType(node, "Click");
        }

        /// <summary>
        /// 由 AddChildActionCommand 觸發，根據使用者選擇的指令類型新增子指令
        /// </summary>
        private void OnAddChildWithType(string actionType)
        {
            var selectedNode = EditorViewModel.SelectedNode;
            if (selectedNode == null) return;

            // 確認選取的是容器或段落標題
            if (selectedNode.Action is ContainerAction || selectedNode.NodeType == NodeType.SectionHeader)
            {
                AddChildToContainerWithType(selectedNode, actionType);
            }
        }

        /// <summary>
        /// 將指定類型的指令新增至容器的子清單
        /// </summary>
        private void AddChildToContainerWithType(ActionNodeWrapper node, string actionType)
        {
            // 決定目標清單
            System.Collections.Generic.IList<BaseAction>? targetList = null;

            if (node.NodeType == NodeType.SectionHeader)
            {
                targetList = node.ParentList;
            }
            else if (node.Action is LoopAction loopAction)
            {
                targetList = loopAction.Children;
            }
            else if (node.Action is IfAction ifAction)
            {
                targetList = ifAction.ThenActions;
            }

            if (targetList != null)
            {
                var child = CreateAction(actionType);
                if (child != null)
                {
                    EditorViewModel.AddActionToContainer(child, targetList);
                    StatusMessage = $"已新增子指令: {child.Name}";
                }
            }
        }

        #endregion

        #region Script File Operations

        private void OnNewScript()
        {
            EditorViewModel.ClearActions();
            EditorViewModel.ScriptName = "未命名腳本";
            _currentFilePath = null;
            // 重置為未儲存狀態 (IsDirty = false, 但因為是新腳本，視為未命名)
            // 這裡我們讓 IsDirty = false，標題顯示 "未命名腳本"
            EditorViewModel.IsDirty = false; 
            Title = "AutoWizard Desktop"; 
            StatusMessage = "已建立新腳本";

            VariablesViewModel.EnsureBuiltInColorVariables();
        }

        private void OnOpenScript()
        {
            var dialog = new Microsoft.Win32.OpenFileDialog
            {
                Filter = "AutoWizard Script (*.aws)|*.aws|All Files (*.*)|*.*",
                Title = "開啟腳本"
            };

            if (dialog.ShowDialog() == true)
            {
                try
                {
                    var package = Core.Resources.AwsPackage.Load(dialog.FileName);
                    EditorViewModel.ClearActions();
                    foreach (var action in package.Actions)
                    {
                        EditorViewModel.AddAction(action);
                    }
                    EditorViewModel.ScriptName = package.ScriptName;
                    _currentFilePath = dialog.FileName;
                    
                    // 載入變數
                    VariablesViewModel.Variables.Clear();
                    foreach (var v in package.Variables)
                    {
                        VariablesViewModel.Variables.Add(v);
                    }

                    // 重置 Dirty 狀態 (因為剛載入)
                    EditorViewModel.IsDirty = false;
                    
                    VariablesViewModel.EnsureBuiltInColorVariables();

                    // 更新視窗標題
                    Title = $"{package.ScriptName} - AutoWizard Desktop";

                    if (package.Actions.Count == 0)
                    {
                        StatusMessage = $"警告: 腳本 '{package.ScriptName}' 中沒有指令";
                    }
                    else
                    {
                        StatusMessage = $"已開啟: {package.ScriptName}";
                    }
                }
                catch (Exception ex)
                {
                    var fullMsg = ex.InnerException != null 
                        ? $"{ex.Message} → {ex.InnerException.Message}" 
                        : ex.Message;
                    StatusMessage = $"開啟失敗: {fullMsg}";
                }
            }
        }

        private void OnSaveScript()
        {
            if (string.IsNullOrEmpty(_currentFilePath))
            {
                OnSaveScriptAs();
            }
            else
            {
                SaveScriptToPath(_currentFilePath);
            }
        }

        private void OnSaveScriptAs()
        {
            var dialog = new Microsoft.Win32.SaveFileDialog
            {
                Filter = "AutoWizard Script (*.aws)|*.aws",
                Title = "另存新檔",
                FileName = EditorViewModel.ScriptName
            };

            if (dialog.ShowDialog() == true)
            {
                _currentFilePath = dialog.FileName;
                // 以使用者選擇的檔案名稱更新腳本名稱
                var savedName = System.IO.Path.GetFileNameWithoutExtension(dialog.FileName);
                EditorViewModel.ScriptName = savedName;

                SaveScriptToPath(_currentFilePath);
            }
        }

        private void SaveScriptToPath(string filePath)
        {
            try
            {
                var package = new Core.Resources.AwsPackage
                {
                    ScriptName = EditorViewModel.ScriptName,
                    Actions = EditorViewModel.Actions.ToList(),
                    Variables = VariablesViewModel.Variables.ToList()
                };
                package.Save(filePath);
                
                // 更新 UI 狀態
                EditorViewModel.IsDirty = false;
                Title = $"{EditorViewModel.ScriptName} - AutoWizard Desktop";
                StatusMessage = $"已儲存: {filePath}";
            }
            catch (Exception ex)
            {
                StatusMessage = $"儲存失敗: {ex.Message}";
            }
        }

        #endregion

        #region Script Execution

        private Dictionary<string, object> BuildInitialVariables()
        {
            var dict = new Dictionary<string, object>();
            foreach (var v in VariablesViewModel.Variables)
            {
                dict[v.Name] = v.GetTypedDefaultValue() ?? "";
            }
            return dict;
        }

        private bool CanRunScript() => !IsRunning && EditorViewModel.Actions.Count > 0;
        private bool CanStopScript() => IsRunning;

        private async void OnRunScript()
        {
            if (IsRunning || EditorViewModel.Actions.Count == 0)
                return;

            IsRunning = true;
            ExecutionLogs.Clear();
            ((DelegateCommand)ExportLogsCommand).RaiseCanExecuteChanged();
            IsLogPanelExpanded = true;
            StatusMessage = "執行中...";
            ExecutionStatusText = "⏳ 正在執行腳本";

            var actions = EditorViewModel.Actions.ToList();
            
            // 訂閱所有指令的執行事件
            foreach(var wrapper in EditorViewModel.FlattenedNodes)
            {
                if (wrapper.Action != null)
                {
                    wrapper.IsExecuting = false; // 清除舊狀態
                    wrapper.Action.Executing -= OnActionExecuting; // 避免重複訂閱
                    wrapper.Action.Executing += OnActionExecuting;
                }
            }

            // 最小化主視窗，避免遮擋目標 UI 元素
            var mainWindow = Application.Current?.MainWindow;
            WindowState? previousState = null;
            if (mainWindow != null)
            {
                previousState = mainWindow.WindowState;
                mainWindow.WindowState = WindowState.Minimized;
            }

            try
            {
                // 啟動延遲 — 讓使用者有時間準備
                await Task.Delay(1000);

                var result = await _executor.ExecuteAsync(actions, BuildInitialVariables());

                // 恢復主視窗
                if (mainWindow != null && previousState.HasValue)
                {
                    mainWindow.WindowState = previousState.Value;
                    mainWindow.Activate();
                }

                // 顯示結果摘要
                switch (result.Status)
                {
                    case ExecutionStatus.Completed:
                        StatusMessage = $"✅ 執行完成 ({result.Duration.TotalSeconds:F1}s)";
                        ExecutionStatusText = $"已完成 — {actions.Count} 個指令, 耗時 {result.Duration.TotalSeconds:F1} 秒";
                        break;
                    case ExecutionStatus.Cancelled:
                        StatusMessage = "⚠️ 執行已取消";
                        ExecutionStatusText = "使用者取消執行";
                        break;
                    case ExecutionStatus.Failed:
                        StatusMessage = $"❌ 執行失敗: {result.ErrorMessage}";
                        ExecutionStatusText = $"錯誤: {result.ErrorMessage}";
                        break;
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"❌ 執行異常: {ex.Message}";
                ExecutionStatusText = $"未預期的例外: {ex.Message}";
            }
            finally
            {
                IsRunning = false;
                
                // 清除所有執行中狀態和訂閱
                foreach(var wrapper in EditorViewModel.FlattenedNodes)
                {
                    if (wrapper.Action != null)
                    {
                        wrapper.Action.Executing -= OnActionExecuting;
                        Application.Current?.Dispatcher?.BeginInvoke(System.Windows.Threading.DispatcherPriority.Normal, () =>
                        {
                            wrapper.IsExecuting = false;
                        });
                    }
                }

                // 確保視窗恢復（即使發生異常）
                if (mainWindow != null && previousState.HasValue && mainWindow.WindowState == WindowState.Minimized)
                {
                    mainWindow.WindowState = previousState.Value;
                    mainWindow.Activate();
                }
            }
        }

        private void OnActionExecuting(object? sender, EventArgs e)
        {
            if (sender is BaseAction action)
            {
                Application.Current?.Dispatcher?.BeginInvoke(System.Windows.Threading.DispatcherPriority.Normal, () =>
                {
                    // 先清除所有高亮
                    foreach(var wrapper in EditorViewModel.FlattenedNodes)
                    {
                        wrapper.IsExecuting = false;
                    }

                    // 再高亮當前這個
                    var currentNode = EditorViewModel.FlattenedNodes.FirstOrDefault(n => n.Action == action);
                    if (currentNode != null)
                    {
                        currentNode.IsExecuting = true;
                    }
                });
            }
        }

        private void OnStopScript()
        {
            if (!IsRunning)
                return;

            _executor.Stop();
            StatusMessage = "⏹️ 正在停止...";
        }

        private void OnToggleRun()
        {
            if (IsRunning)
            {
                OnStopScript();
            }
            else
            {
                OnRunScript();
            }
        }

        private void OnLogReceived(string message)
        {
            // 確保在 UI 執行緒更新 ObservableCollection
            Application.Current?.Dispatcher?.BeginInvoke(DispatcherPriority.Normal, () =>
            {
                string time = DateTime.Now.ToString("HH:mm:ss.fff");
                ExecutionLogs.Add($"[{time}] {message}");
                
                if (ExecutionLogs.Count > 1000)
                {
                    ExecutionLogs.RemoveAt(0);
                }

                ((DelegateCommand)ExportLogsCommand).RaiseCanExecuteChanged();
            });
        }
        
        private async void OnExportLogs()
        {
            if (ExecutionLogs.Count == 0) return;

            var dialog = new Microsoft.Win32.SaveFileDialog
            {
                Title = "匯出執行日誌",
                Filter = "文字檔 (*.txt)|*.txt|所有檔案 (*.*)|*.*",
                DefaultExt = ".txt",
                FileName = $"ExecutionLogs_{DateTime.Now:yyyyMMdd_HHmmss}.txt"
            };

            if (dialog.ShowDialog() == true)
            {
                try
                {
                    await System.IO.File.WriteAllLinesAsync(dialog.FileName, ExecutionLogs);
                    StatusMessage = $"日誌已成功匯出至: {dialog.FileName}";
                }
                catch (Exception ex)
                {
                    StatusMessage = $"匯出日誌失敗: {ex.Message}";
                }
            }
        }

        private void OnExecutionStatusChanged(ExecutionStatus status)
        {
            Application.Current?.Dispatcher?.BeginInvoke(DispatcherPriority.Normal, () =>
            {
                switch (status)
                {
                    case ExecutionStatus.Running:
                        ExecutionStatusText = "⏳ 正在執行...";
                        break;
                    case ExecutionStatus.Completed:
                        ExecutionStatusText = "✅ 執行完成";
                        break;
                    case ExecutionStatus.Failed:
                        ExecutionStatusText = "❌ 執行失敗";
                        break;
                    case ExecutionStatus.Cancelled:
                        ExecutionStatusText = "⚠️ 已取消";
                        break;
                }
            });
        }

        #endregion

        #region Recording

        private void OnRecord()
        {
            try
            {
                if (_recorder == null)
                {
                    _recorder = new Core.Recording.Recorder();
                    _recorder.ActionRecorded += OnActionRecorded;
                    _recorder.StopRequested += (_, _) => StopRecordingFromEvent();
                    _recorder.SmartCaptureRequested += (_, _) => StartSmartCaptureFromEvent();
                }

                if (_recorder.IsRecording)
                {
                    StopRecordingInternal();
                }
                else
                {
                    _recorder.StartRecording();
                    StatusMessage = "錄製中... (F9 停止, ScrollLock 截圖)";
                    
                    // 顯示錄製遮罩 (紅色邊框，滑鼠穿透)
                    _recordingOverlay = new Views.RecordingOverlay();
                    _recordingOverlay.Show();

                    // 顯示錄製工具列 (可點擊)
                    _recordingToolbar = new Views.RecordingToolbar();
                    _recordingToolbar.StopClicked += (_, _) => StopRecordingFromEvent();
                    _recordingToolbar.ScreenshotClicked += (_, _) => StartSmartCaptureFromEvent();
                    _recordingToolbar.Show();
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"錄製啟動失敗: {ex.Message}";
                MessageBox.Show($"無法啟動錄製: {ex.Message}\n{ex.StackTrace}", "錯誤", MessageBoxButton.OK, MessageBoxImage.Error);
                
                // 清理資源
                try
                {
                    _recorder?.StopRecording();
                    _recordingOverlay?.Close();
                    _recordingToolbar?.Close();
                }
                catch { /* 忽略清理錯誤 */ }
            }
        }

        private void OnActionRecorded(object? sender, Core.Recording.ActionRecordedEventArgs e)
        {
            Application.Current?.Dispatcher?.BeginInvoke(System.Windows.Threading.DispatcherPriority.Normal, () =>
            {
                EditorViewModel.AddAction(e.Action);
            });
        }

        /// <summary>
        /// 從非 UI 執行緒事件觸發停止錄製（F9 或按鈕）
        /// </summary>
        private void StopRecordingFromEvent()
        {
            Application.Current?.Dispatcher?.BeginInvoke(System.Windows.Threading.DispatcherPriority.Normal, () =>
            {
                StopRecordingInternal();
            });
        }

        /// <summary>
        /// 停止錄製的核心邏輯（必須在 UI 執行緒呼叫）
        /// </summary>
        private void StopRecordingInternal()
        {
            if (_recorder == null || !_recorder.IsRecording) return;

            _recorder.StopRecording();
            StatusMessage = "錄製已停止";
            
            // 隱藏錄製遮罩
            _recordingOverlay?.Close();
            _recordingOverlay = null;

            // 隱藏工具列
            _recordingToolbar?.Close();
            _recordingToolbar = null;
            
            // 注意：OnActionRecorded 事件已經即時將指令加入編輯器，
            // 不需要在此處再次新增，否則會造成重複。
        }

        private void StartSmartCaptureFromEvent()
        {
            Application.Current?.Dispatcher?.BeginInvoke(System.Windows.Threading.DispatcherPriority.Normal, () =>
            {
                StartSmartCaptureDuringRecording();
            });
        }

        // 螢幕設定屬性
        private int _screenWidth = (int)SystemParameters.PrimaryScreenWidth;
        public int ScreenWidth
        {
            get => _screenWidth;
            set => SetProperty(ref _screenWidth, value);
        }

        private int _screenHeight = (int)SystemParameters.PrimaryScreenHeight;
        public int ScreenHeight
        {
            get => _screenHeight;
            set => SetProperty(ref _screenHeight, value);
        }

        private double _screenScale = 100;
        public double ScreenScale
        {
            get => _screenScale;
            set => SetProperty(ref _screenScale, value);
        }

        private async void StartSmartCaptureDuringRecording()
        {
            if (_recorder == null || !_recorder.IsRecording) return;

            _recorder.PauseRecording();
            StatusMessage = "暫停錄製 - 進行截圖...";
            
            // 隱藏工具列，但保留紅框並變綠 (Green Border)
            _recordingToolbar?.Hide();
            _recordingOverlay?.SetBorderColor(System.Windows.Media.Colors.Lime); // Green
            
            // 等待渲染
            await Task.Delay(300);

            try
            {
                // 截圖 (包含綠框)
                var screenshot = AutoWizard.CV.Capture.ScreenCapture.CaptureScreen();
                
                // 截圖後隱藏綠框，準備顯示 Overlay
                _recordingOverlay?.Hide();

                var overlay = new Views.SmartCaptureOverlay(screenshot);
                overlay.ShowDialog();

                if (overlay.IsConfirmed)
                {
                    var rect = overlay.SelectedRectPhysical;
                    var imagesDir = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Images");
                    if (!System.IO.Directory.Exists(imagesDir))
                        System.IO.Directory.CreateDirectory(imagesDir);

                    // 讓使用者命名截圖檔（預設帶入日期時間）
                    var defaultName = $"SmartCapture_{DateTime.Now:yyyyMMdd_HHmmss}";
                    var nameDialog = new Views.CaptureNameDialog(defaultName);
                    nameDialog.ShowDialog();
                    var baseName = nameDialog.IsConfirmed && !string.IsNullOrWhiteSpace(nameDialog.ResultFileName)
                        ? nameDialog.ResultFileName
                        : defaultName;
                    var fileName = baseName.EndsWith(".png", StringComparison.OrdinalIgnoreCase) ? baseName : baseName + ".png";
                    var filePath = System.IO.Path.Combine(imagesDir, fileName);
                    
                    if (rect.Width > 0 && rect.Height > 0)
                    {
                        var target = new System.Drawing.Bitmap(rect.Width, rect.Height);
                        using (var g = System.Drawing.Graphics.FromImage(target))
                        {
                            g.DrawImage(screenshot, new System.Drawing.Rectangle(0, 0, target.Width, target.Height),
                                        rect, System.Drawing.GraphicsUnit.Pixel);
                        }
                        target.Save(filePath, System.Drawing.Imaging.ImageFormat.Png);

                        var varName = $"img_{DateTime.Now:HHmmss}";
                        var findAction = new FindImageAction
                        {
                            Name = "尋找截圖影像",
                            Description = $"尋找影像 {fileName}",
                            TemplateImagePath = $"Images\\\\{fileName}", // 使用相對路徑
                            ClickWhenFound = false,
                            SaveToVariable = varName
                        };
                        
                        var clickAction = new AutoWizard.Core.Actions.Input.ClickAction
                        {
                            Name = "點擊影像中心",
                            Description = $"點擊 {fileName} 中心",
                            XExpression = $"{{{varName}_X}}",
                            YExpression = $"{{{varName}_Y}}",
                            Button = AutoWizard.Core.Actions.Input.MouseButton.Left,
                            ClickType = AutoWizard.Core.Actions.Input.ClickType.Single
                        };
                        
                        EditorViewModel.AddAction(findAction);
                        EditorViewModel.AddAction(clickAction);
                        StatusMessage = $"已新增截圖與點擊指令: {fileName}";
                    }
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"截圖失敗: {ex.Message}";
                MessageBox.Show($"截圖發生錯誤: {ex.Message}", "錯誤", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                if (_recorder != null && _recorder.IsRecording)
                {
                    _recordingToolbar?.Show();
                    _recordingOverlay?.Show();
                    _recordingOverlay?.SetBorderColor(System.Windows.Media.Color.FromRgb(0xFF, 0x44, 0x44)); // Restore Red (FF4444)
                    
                    _recorder.ResumeRecording();
                    StatusMessage = "錄製中... (F9 停止, ScrollLock 截圖)";
                }
            }
        }


        #region Tools & Helpers

        private ICommand _smartCaptureCommand;
        public ICommand SmartCaptureCommand => _smartCaptureCommand ??= new DelegateCommand(OnSmartCapture);

        private ICommand _pickColorCommand;
        public ICommand PickColorCommand => _pickColorCommand ??= new DelegateCommand(OnPickColor);

        private async void OnSmartCapture()
        {
            // 最小化主視窗
            var mainWindow = Application.Current.MainWindow;
            var previousState = mainWindow.WindowState;
            mainWindow.WindowState = WindowState.Minimized;
            
            await Task.Delay(300); // 等待動畫

            try
            {
                // 截圖
                var screenshot = CV.Capture.ScreenCapture.CaptureScreen();
                
                // 顯示 Overlay
                var overlay = new Views.SmartCaptureOverlay(screenshot);
                overlay.ShowDialog();

                if (overlay.IsConfirmed)
                {
                    // 裁切影像
                    var rect = overlay.SelectedRectPhysical;
                    
                    // 儲存路徑
                    var imagesDir = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Images");
                    if (!System.IO.Directory.Exists(imagesDir))
                        System.IO.Directory.CreateDirectory(imagesDir);

                    // 讓使用者命名截圖檔（預設帶入日期時間）
                    var defaultName = $"SmartCapture_{DateTime.Now:yyyyMMdd_HHmmss}";
                    var nameDialog = new Views.CaptureNameDialog(defaultName);
                    nameDialog.ShowDialog();
                    var baseName = nameDialog.IsConfirmed && !string.IsNullOrWhiteSpace(nameDialog.ResultFileName)
                        ? nameDialog.ResultFileName
                        : defaultName;
                    var fileName = baseName.EndsWith(".png", StringComparison.OrdinalIgnoreCase) ? baseName : baseName + ".png";
                    var filePath = System.IO.Path.Combine(imagesDir, fileName);

                    // 安全檢查
                    if (rect.Width > 0 && rect.Height > 0)
                    {
                        var target = new Bitmap(rect.Width, rect.Height);
                        using (var g = Graphics.FromImage(target))
                        {
                            g.DrawImage(screenshot, new Rectangle(0, 0, target.Width, target.Height),
                                        rect, GraphicsUnit.Pixel);
                        }
                        target.Save(filePath, System.Drawing.Imaging.ImageFormat.Png);

                        var varName = $"img_{DateTime.Now:HHmmss}";
                        var findAction = new AutoWizard.Core.Actions.Vision.FindImageAction
                        {
                            Name = "尋找截圖影像",
                            Description = $"尋找影像 {fileName}",
                            TemplateImagePath = $"Images\\\\{fileName}",
                            ClickWhenFound = false,
                            SaveToVariable = varName
                        };
                        
                        var clickAction = new AutoWizard.Core.Actions.Input.ClickAction
                        {
                            Name = "點擊影像中心",
                            Description = $"點擊 {fileName} 中心",
                            XExpression = $"{{{varName}_X}}",
                            YExpression = $"{{{varName}_Y}}",
                            Button = AutoWizard.Core.Actions.Input.MouseButton.Left,
                            ClickType = AutoWizard.Core.Actions.Input.ClickType.Single
                        };

                        EditorViewModel.AddAction(findAction);
                        EditorViewModel.AddAction(clickAction);
                        StatusMessage = $"已新增截圖與點擊指令: {fileName}";
                    }
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"截圖失敗: {ex.Message}";
            }
            finally
            {
                mainWindow.WindowState = previousState;
                mainWindow.Activate();
            }
        }

        private int _nextColorVariableIndex = 1;

        private async void OnPickColor()
        {
            var result = await PickColorInternal();
            if (result.HasValue)
            {
                var hex = result.Value.Hex;
                Clipboard.SetText(hex);

                // 自動填入變數 color_check_1 ~ color_check_10
                string varName = $"color_check_{_nextColorVariableIndex}";
                var targetVar = VariablesViewModel.Variables.FirstOrDefault(v => v.Name == varName);
                if (targetVar != null)
                {
                    targetVar.DefaultValue = $"{result.Value.X},{result.Value.Y},{hex},0";
                }

                StatusMessage = $"顏色已複製並自動填入 {varName}: {hex} at ({result.Value.X},{result.Value.Y})";

                // 循環索引
                _nextColorVariableIndex++;
                if (_nextColorVariableIndex > 10)
                {
                    _nextColorVariableIndex = 1;
                }
            }
        }

        private async Task<(string Hex, int X, int Y)?> PickColorInternal()
        {
            var mainWindow = Application.Current.MainWindow;
            var previousState = mainWindow.WindowState;
            mainWindow.WindowState = WindowState.Minimized;
            
            await Task.Delay(300);

            try
            {
                var screenshot = CV.Capture.ScreenCapture.CaptureScreen();
                var overlay = new Views.SmartCaptureOverlay(screenshot)
                {
                    IsColorPickerMode = true
                };
                overlay.ShowDialog();

                if (overlay.IsConfirmed)
                {
                    var c = overlay.PickedColor;
                    var hex = $"#{c.R:X2}{c.G:X2}{c.B:X2}";
                    return (hex, (int)overlay.PickedPoint.X, (int)overlay.PickedPoint.Y);
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"取色失敗: {ex.Message}";
            }
            finally
            {
                mainWindow.WindowState = previousState;
                mainWindow.Activate();
            }

            return null;
        }

        #endregion

        #endregion

    }
}
