using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using AutoWizard.Core.Actions.Input;
using AutoWizard.Core.Actions.Control;
using AutoWizard.Core.Actions.Vision;

namespace AutoWizard.Core.Models
{
    /// <summary>
    /// 指令執行結果
    /// </summary>
    public class ActionResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        [JsonIgnore]
        public Exception? Exception { get; set; }
        public Dictionary<string, object> Data { get; set; } = new();
    }

    /// <summary>
    /// 錯誤處理策略
    /// </summary>
    public class ErrorHandlingPolicy
    {
        public int RetryCount { get; set; } = 0;
        public int RetryIntervalMs { get; set; } = 1000;
        public string? JumpToLabel { get; set; }
        public bool ContinueOnError { get; set; } = false;
    }

    /// <summary>
    /// 執行上下文
    /// </summary>
    public class ExecutionContext
    {
        public Dictionary<string, object> Variables { get; set; } = new();
        public bool IsCancellationRequested { get; set; } = false;
        public Action<string>? LogAction { get; set; }
        public Action<byte[]>? ScreenshotAction { get; set; }
        
        /// <summary>
        /// 指令執行的目標視窗 Handle，若為 IntPtr.Zero 則代表全域桌面
        /// </summary>
        public IntPtr TargetWindowHandle { get; set; } = IntPtr.Zero;

        /// <summary>
        /// 指令執行的目標視窗標題
        /// </summary>
        public string TargetWindowTitle { get; set; } = string.Empty;
        
        public void Log(string message)
        {
            LogAction?.Invoke($"[{DateTime.Now:HH:mm:ss}] {message}");
        }

        /// <summary>
        /// 將字串中所有 {varName} 替換為實際值
        /// </summary>
        public string ResolveExpression(string input)
        {
            return Engine.ExpressionParser.Resolve(input, Variables);
        }

        /// <summary>
        /// 解析表達式為整數
        /// </summary>
        public int ResolveInt(string input, int fallback = 0)
        {
            return Engine.ExpressionParser.ResolveInt(input, Variables, fallback);
        }

        /// <summary>
        /// 解析表達式為浮點數
        /// </summary>
        public double ResolveDouble(string input, double fallback = 0.0)
        {
            return Engine.ExpressionParser.ResolveDouble(input, Variables, fallback);
        }

        /// <summary>
        /// 計算布林條件表達式
        /// </summary>
        public bool EvaluateCondition(string expression)
        {
            return Engine.ExpressionParser.EvaluateCondition(expression, Variables);
        }
    }

    /// <summary>
    /// 指令抽象基類
    /// </summary>
    [JsonPolymorphic(TypeDiscriminatorPropertyName = "$type")]
    [JsonDerivedType(typeof(ClickAction), "Click")]
    [JsonDerivedType(typeof(TypeAction), "Type")]
    [JsonDerivedType(typeof(KeyboardAction), "Keyboard")]
    [JsonDerivedType(typeof(IfAction), "If")]
    [JsonDerivedType(typeof(LoopAction), "Loop")]
    [JsonDerivedType(typeof(FindImageAction), "FindImage")]
    [JsonDerivedType(typeof(OCRAction), "OCR")]
    [JsonDerivedType(typeof(WaitAction), "Wait")]
    [JsonDerivedType(typeof(SetVariableAction), "SetVariable")]
    [JsonDerivedType(typeof(ScreenshotAction), "Screenshot")]

    public abstract class BaseAction : System.ComponentModel.INotifyPropertyChanged
    {
        public event System.ComponentModel.PropertyChangedEventHandler? PropertyChanged;
        public event EventHandler? Executing;

        protected void OnPropertyChanged([System.Runtime.CompilerServices.CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(propertyName));
        }

        protected bool SetProperty<T>(ref T storage, T value, [System.Runtime.CompilerServices.CallerMemberName] string? propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(storage, value)) return false;
            storage = value;
            OnPropertyChanged(propertyName);
            return true;
        }

        public string Id { get; set; } = Guid.NewGuid().ToString();

        private string _name = string.Empty;
        public string Name 
        { 
            get => _name;
            set => SetProperty(ref _name, value);
        }

        private string _description = string.Empty;
        public string Description 
        { 
            get => _description;
            set => SetProperty(ref _description, value);
        }

        private bool _isEnabled = true;
        public bool IsEnabled 
        { 
            get => _isEnabled;
            set => SetProperty(ref _isEnabled, value);
        }

        public ErrorHandlingPolicy ErrorPolicy { get; set; } = new();
        public int DelayBeforeMs { get; set; } = 0;
        public int DelayAfterMs { get; set; } = 0;

        /// <summary>
        /// 執行指令的核心邏輯
        /// </summary>
        public abstract ActionResult Execute(ExecutionContext context);

        /// <summary>
        /// 執行指令(包含延遲與錯誤處理)
        /// </summary>
        public ActionResult ExecuteWithPolicy(ExecutionContext context)
        {
            if (!IsEnabled)
            {
                return new ActionResult 
                { 
                    Success = true, 
                    Message = "Action is disabled, skipped." 
                };
            }

            Executing?.Invoke(this, EventArgs.Empty);

            // 執行前延遲
            if (DelayBeforeMs > 0)
            {
                System.Threading.Thread.Sleep(DelayBeforeMs);
            }

            ActionResult result;
            int attemptCount = 0;
            int maxAttempts = ErrorPolicy.RetryCount + 1;

            do
            {
                attemptCount++;
                
                try
                {
                    context.Log($"Executing: {Name} (Attempt {attemptCount}/{maxAttempts})");
                    result = Execute(context);

                    if (result.Success)
                    {
                        break;
                    }
                    else if (attemptCount < maxAttempts)
                    {
                        context.Log($"Failed: {result.Message}. Retrying in {ErrorPolicy.RetryIntervalMs}ms...");
                        System.Threading.Thread.Sleep(ErrorPolicy.RetryIntervalMs);
                    }
                }
                catch (Exception ex)
                {
                    result = new ActionResult
                    {
                        Success = false,
                        Message = ex.Message,
                        Exception = ex
                    };

                    if (attemptCount < maxAttempts)
                    {
                        context.Log($"Exception: {ex.Message}. Retrying in {ErrorPolicy.RetryIntervalMs}ms...");
                        System.Threading.Thread.Sleep(ErrorPolicy.RetryIntervalMs);
                    }
                }

            } while (attemptCount < maxAttempts && !result.Success);

            // 執行後延遲
            if (DelayAfterMs > 0)
            {
                System.Threading.Thread.Sleep(DelayAfterMs);
            }

            return result;
        }
    }

    /// <summary>
    /// 容器指令基類(支援巢狀結構)
    /// </summary>
    public abstract class ContainerAction : BaseAction
    {
        public List<BaseAction> Children { get; set; } = new();

        protected ActionResult ExecuteChildren(ExecutionContext context)
        {
            foreach (var child in Children)
            {
                if (context.IsCancellationRequested)
                {
                    return new ActionResult
                    {
                        Success = false,
                        Message = "Execution cancelled by user."
                    };
                }

                var result = child.ExecuteWithPolicy(context);
                
                if (!result.Success && !child.ErrorPolicy.ContinueOnError)
                {
                    return result;
                }
            }

            return new ActionResult { Success = true };
        }
    }
}
