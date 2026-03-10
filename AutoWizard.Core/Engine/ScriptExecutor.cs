using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using AutoWizard.Core.Models;

namespace AutoWizard.Core.Engine
{
    /// <summary>
    /// 腳本執行引擎
    /// </summary>
    public class ScriptExecutor
    {
        private CancellationTokenSource? _cancellationTokenSource;
        private Models.ExecutionContext? _context;
        private List<string> _logs = new();

        /// <summary>
        /// 動作之間的間隔延遲（毫秒），預設 200ms
        /// </summary>
        public int ActionIntervalMs { get; set; } = 200;

        public event Action<string>? LogReceived;
        public event Action<byte[]>? ScreenshotCaptured;
        public event Action<ExecutionStatus>? StatusChanged;

        /// <summary>
        /// 每個動作開始執行前觸發，參數為動作名稱
        /// </summary>
        public event Action<string>? ActionStarting;

        /// <summary>
        /// 目標視窗標題（若指定，執行前會自動激活該視窗）
        /// </summary>
        public string? TargetWindowTitle { get; set; }

        #region Win32 API

        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr FindWindow(string? lpClassName, string lpWindowName);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool IsIconic(IntPtr hWnd);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        private const int SW_RESTORE = 9;

        /// <summary>
        /// 嘗試激活目標視窗（依標題模糊匹配）
        /// </summary>
        private bool TryActivateTargetWindow()
        {
            if (string.IsNullOrWhiteSpace(TargetWindowTitle)) return false;

            var hWnd = FindWindow(null, TargetWindowTitle);
            if (hWnd == IntPtr.Zero)
            {
                OnLog($"[窗口激活] 找不到視窗: {TargetWindowTitle}");
                return false;
            }

            // 若視窗被最小化，先還原
            if (IsIconic(hWnd))
            {
                ShowWindow(hWnd, SW_RESTORE);
                Thread.Sleep(200); // 等待視窗動畫
            }

            var success = SetForegroundWindow(hWnd);
            if (success)
            {
                OnLog($"[窗口激活] 已激活: {TargetWindowTitle}");
                Thread.Sleep(100); // 短暫等待視窗激活完成
            }
            else
            {
                OnLog($"[窗口激活] 激活失敗: {TargetWindowTitle}");
            }

            return success;
        }

        #endregion

        /// <summary>
        /// 執行腳本
        /// </summary>
        public async Task<ExecutionResult> ExecuteAsync(List<BaseAction> actions, Dictionary<string, object>? initialVariables = null)
        {
            _cancellationTokenSource = new CancellationTokenSource();
            _logs.Clear();

            _context = new Models.ExecutionContext
            {
                Variables = initialVariables ?? new Dictionary<string, object>(),
                IsCancellationRequested = false,
                LogAction = OnLog,
                ScreenshotAction = OnScreenshot
            };

            // 註冊取消回調，確保取消指令能即時傳播到正在執行的 Action
            _cancellationTokenSource.Token.Register(() =>
            {
                if (_context != null)
                    _context.IsCancellationRequested = true;
            });

            StatusChanged?.Invoke(ExecutionStatus.Running);

            var result = new ExecutionResult
            {
                StartTime = DateTime.Now,
                Status = ExecutionStatus.Running
            };

            try
            {
                await Task.Run(() =>
                {
                    OnLog($"[腳本] 開始執行，共 {actions.Count} 個動作" + 
                          (string.IsNullOrWhiteSpace(TargetWindowTitle) ? "" : $"，目標視窗: {TargetWindowTitle}"));

                    for (int i = 0; i < actions.Count; i++)
                    {
                        if (_cancellationTokenSource.Token.IsCancellationRequested)
                        {
                            _context.IsCancellationRequested = true;
                            result.Status = ExecutionStatus.Cancelled;
                            break;
                        }

                        var action = actions[i];

                        // 嘗試激活目標視窗（Desktop 模式）
                        TryActivateTargetWindow();

                        // 記錄進度
                        OnLog($"[{i + 1}/{actions.Count}] 執行: {action.Name}");

                        // 觸發動作開始事件
                        ActionStarting?.Invoke(action.Name);

                        var actionResult = action.ExecuteWithPolicy(_context);

                        if (!actionResult.Success)
                        {
                            if (action.ErrorPolicy.ContinueOnError)
                            {
                                OnLog($"[{i + 1}/{actions.Count}] ⚠️ 失敗但繼續執行: {actionResult.Message}");
                            }
                            else
                            {
                                OnLog($"[{i + 1}/{actions.Count}] ❌ 失敗: {actionResult.Message}");
                                result.Status = ExecutionStatus.Failed;
                                result.ErrorMessage = actionResult.Message;
                                result.Exception = actionResult.Exception;
                                break;
                            }
                        }

                        // 動作間隔延遲（最後一個動作後不延遲）
                        if (i < actions.Count - 1 && ActionIntervalMs > 0)
                        {
                            Thread.Sleep(ActionIntervalMs);
                        }
                    }

                    if (result.Status == ExecutionStatus.Running)
                    {
                        result.Status = ExecutionStatus.Completed;
                    }

                }, _cancellationTokenSource.Token);
            }
            catch (Exception ex)
            {
                result.Status = ExecutionStatus.Failed;
                result.ErrorMessage = ex.Message;
                result.Exception = ex;
            }
            finally
            {
                result.EndTime = DateTime.Now;
                result.Logs = new List<string>(_logs);
                StatusChanged?.Invoke(result.Status);
            }

            return result;
        }

        /// <summary>
        /// 停止執行
        /// </summary>
        public void Stop()
        {
            _cancellationTokenSource?.Cancel();
        }

        private void OnLog(string message)
        {
            _logs.Add(message);
            LogReceived?.Invoke(message);
        }

        private void OnScreenshot(byte[] screenshot)
        {
            ScreenshotCaptured?.Invoke(screenshot);
        }
    }

    /// <summary>
    /// 執行狀態
    /// </summary>
    public enum ExecutionStatus
    {
        Idle,
        Running,
        Completed,
        Failed,
        Cancelled
    }

    /// <summary>
    /// 執行結果
    /// </summary>
    public class ExecutionResult
    {
        public ExecutionStatus Status { get; set; } = ExecutionStatus.Idle;
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public TimeSpan Duration => EndTime - StartTime;
        public string? ErrorMessage { get; set; }
        public Exception? Exception { get; set; }
        public List<string> Logs { get; set; } = new();
    }
}
