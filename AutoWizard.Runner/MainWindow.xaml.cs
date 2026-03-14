using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using AutoWizard.Core.Engine;
using AutoWizard.Core.Models;
using AutoWizard.Core.Resources;
using Newtonsoft.Json;
using Gma.System.MouseKeyHook;
using System.Windows.Interop;
using System.Runtime.InteropServices;

namespace AutoWizard.Runner
{
    public partial class MainWindow : Window
    {
        private ScriptExecutor _executor;
        private AwsPackage _scriptPackage;
        private IKeyboardMouseEvents _globalHook;
        private bool _isRunning = false;
        private readonly List<string> _executionLogs = new List<string>();
        private string _runtimeScriptDirectory = AppDomain.CurrentDomain.BaseDirectory;

        private Dictionary<string, TextBox> _variableInputs = new Dictionary<string, TextBox>();

        public MainWindow()
        {
            InitializeComponent();
            _executor = new ScriptExecutor();
            _executor.LogReceived += OnLogReceived;
            _executor.StatusChanged += OnStatusChanged;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                // Hook ScrollLock
                _globalHook = Hook.GlobalEvents();
                _globalHook.KeyDown += GlobalHook_KeyDown;

                LoadEmbeddedScript();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to initialize: {ex.Message}", "Fatal Error", MessageBoxButton.OK, MessageBoxImage.Error);
                Application.Current.Shutdown();
            }
        }

        private void GlobalHook_KeyDown(object sender, System.Windows.Forms.KeyEventArgs e)
        {
            if (e.KeyCode == System.Windows.Forms.Keys.Scroll)
            {
                e.Handled = true;
                Dispatcher.Invoke(() =>
                {
                    if (_isRunning)
                    {
                        StopButton_Click(null, null);
                    }
                    else
                    {
                        RunButton_Click(null, null);
                    }
                });
            }
        }

        private void LoadEmbeddedScript()
        {
            try
            {
                bool loaded = false;

                // 1. 先嘗試讀取單一執行檔附加的 Payload (最後 16 bytes: Length + Magic)
                var exePath = System.Diagnostics.Process.GetCurrentProcess().MainModule?.FileName;
                if (!string.IsNullOrEmpty(exePath) && File.Exists(exePath))
                {
                    using var fs = new FileStream(exePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                    if (fs.Length > 16)
                    {
                        fs.Seek(-8, SeekOrigin.End);
                        byte[] magicBytes = new byte[8];
                        fs.Read(magicBytes, 0, 8);
                        if (Encoding.ASCII.GetString(magicBytes) == "AWSPACKG")
                        {
                            fs.Seek(-16, SeekOrigin.End);
                            byte[] lenBytes = new byte[8];
                            fs.Read(lenBytes, 0, 8);
                            long payloadLength = BitConverter.ToInt64(lenBytes, 0);

                            if (payloadLength > 0 && fs.Length >= payloadLength + 16)
                            {
                                fs.Seek(-16 - payloadLength, SeekOrigin.End);
                                var tempFile = Path.GetTempFileName();
                                
                                using (var tempFs = File.Create(tempFile))
                                {
                                    byte[] buffer = new byte[81920];
                                    long remaining = payloadLength;
                                    while (remaining > 0)
                                    {
                                        int toRead = (int)Math.Min(remaining, buffer.Length);
                                        int read = fs.Read(buffer, 0, toRead);
                                        if (read == 0) break;
                                        tempFs.Write(buffer, 0, read);
                                        remaining -= read;
                                    }
                                }
                                
                                _scriptPackage = AwsPackage.Load(tempFile);
                                File.Delete(tempFile);
                                loaded = true;

                                // 準備執行緒環境：將影像釋放到佔存資料夾，因為找圖指令需要實體檔案
                                _runtimeScriptDirectory = Path.Combine(Path.GetTempPath(), "AutoWizardRuntime_" + Guid.NewGuid().ToString("N"));
                                Directory.CreateDirectory(_runtimeScriptDirectory);
                                var imageDir = Path.Combine(_runtimeScriptDirectory, "Images");
                                if (_scriptPackage.ImageResources.Count > 0)
                                {
                                    Directory.CreateDirectory(imageDir);
                                    foreach(var img in _scriptPackage.ImageResources)
                                    {
                                        File.WriteAllBytes(Path.Combine(imageDir, img.Key), img.Value);
                                    }
                                }
                            }
                        }
                    }
                }

                // 2. Fallback checking local directory for script.aws
                if (!loaded)
                {
                    var localScript = "script.aws";
                    if (File.Exists(localScript))
                    {
                        _scriptPackage = AwsPackage.Load(localScript);
                        // Local execution stays in current directory for images
                        _runtimeScriptDirectory = AppDomain.CurrentDomain.BaseDirectory;
                        loaded = true;
                    }
                }

                if (!loaded)
                {
                    throw new Exception("找不到附加的腳本封裝，也找不到 local script.aws。");
                }

                // Update UI
                ScriptNameText.Text = _scriptPackage.ScriptName;
                ActionCountText.Text = $"{_scriptPackage.Actions.Count} Actions";

                // Generate UI for specific variables
                // Only variables without color_check prefix that have a default value or user-defined
                var inputVars = _scriptPackage.Variables
                    .Where(v => !v.Name.StartsWith("color_check_") && v.Name != "Time" && v.Name != "Date")
                    .ToList();

                VariablesPanel.Children.Clear();
                _variableInputs.Clear();

                foreach (var v in inputVars)
                {
                    var panel = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 5) };
                    
                    var label = new TextBlock
                    {
                        Text = $"{v.Name}:",
                        Width = 100,
                        VerticalAlignment = VerticalAlignment.Center,
                        FontWeight = FontWeights.SemiBold
                    };

                    var input = new TextBox
                    {
                        Text = v.DefaultValue,
                        Width = 180,
                        Height = 25,
                        VerticalContentAlignment = VerticalAlignment.Center,
                        Padding = new Thickness(4, 0, 0, 0)
                    };

                    _variableInputs.Add(v.Name, input);
                    
                    panel.Children.Add(label);
                    panel.Children.Add(input);
                    VariablesPanel.Children.Add(panel);
                }

                if (inputVars.Count == 0)
                {
                    VariablesPanel.Children.Add(new TextBlock { Text = "無需要輸入的變數", Foreground = Brushes.Gray, FontStyle = FontStyles.Italic });
                }

                LogMessage("腳本載入成功，等待執行...");
            }
            catch (Exception ex)
            {
                ScriptNameText.Text = "載入失敗";
                ScriptNameText.Foreground = Brushes.Red;
                MessageBox.Show($"腳本載入失敗: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void RunButton_Click(object sender, RoutedEventArgs e)
        {
            if (_isRunning || _scriptPackage == null || _scriptPackage.Actions.Count == 0) return;

            // Prepare Variables
            var vars = new Dictionary<string, object>();
            foreach (var v in _scriptPackage.Variables)
            {
                vars[v.Name] = v.GetTypedDefaultValue();
            }

            // Override with UI inputs
            foreach (var kvp in _variableInputs)
            {
                vars[kvp.Key] = kvp.Value.Text;
            }

            // Inject ScriptDirectory for relative paths (like Images)
            vars["ScriptDirectory"] = _runtimeScriptDirectory;

            _isRunning = true;
            RunButton.IsEnabled = false;
            StopButton.IsEnabled = true;
            HumanLikeCheckbox.IsEnabled = false;
            StatusText.Text = "執行中...";
            StatusText.Foreground = Brushes.Orange;

            // Apply Human Like settings to Executor
            _executor.ForceHumanLikeBehavior = HumanLikeCheckbox.IsChecked == true;
            
            LogMessage("開始執行...");

            try
            {
                // 無窮迴圈執行
                while (_isRunning)
                {
                    var result = await _executor.ExecuteAsync(_scriptPackage.Actions, vars);

                    if (result.Status == ExecutionStatus.Failed)
                    {
                        LogMessage($"執行失敗: {result.ErrorMessage}");
                        break; // 失敗中斷迴圈
                    }
                    else if (result.Status == ExecutionStatus.Cancelled)
                    {
                        LogMessage("執行已取消");
                        break; // 取消中斷迴圈
                    }
                    
                    // 若需要間隔，可以在此等待
                    await Task.Delay(50);
                }
            }
            catch (Exception ex)
            {
                LogMessage($"執行異常: {ex.Message}");
            }
            finally
            {
                _isRunning = false;
                RunButton.IsEnabled = true;
                StopButton.IsEnabled = false;
                HumanLikeCheckbox.IsEnabled = true;
                StatusText.Text = "就緒";
                StatusText.Foreground = Brushes.Green;
                LogMessage("執行停止");
            }
        }

        private void StopButton_Click(object sender, RoutedEventArgs e)
        {
            if (!_isRunning) return;
            LogMessage("正在停止...");
            _executor.Stop();
            // _isRunning 會在 Run 迴圈結束後設置為 false
        }

        private void OnLogReceived(string message)
        {
            Dispatcher.BeginInvoke(DispatcherPriority.Normal, new Action(() =>
            {
                LogMessage(message);
            }));
        }

        private void LogMessage(string message)
        {
            string time = DateTime.Now.ToString("HH:mm:ss.fff");
            string formattedMsg = $"[{time}] {message}";
            LogListBox.Items.Add(formattedMsg);
            _executionLogs.Add(formattedMsg);
            if (LogListBox.Items.Count > 100) // UI limit
            {
                LogListBox.Items.RemoveAt(0);
            }
            LogListBox.ScrollIntoView(LogListBox.Items[LogListBox.Items.Count - 1]);
        }

        private void ExportLogButton_Click(object sender, RoutedEventArgs e)
        {
            if (_executionLogs.Count == 0)
            {
                MessageBox.Show("目前沒有日誌可以匯出。", "匯出提示", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var dialog = new Microsoft.Win32.SaveFileDialog
            {
                Title = "匯出執行日誌",
                Filter = "文字檔 (*.txt)|*.txt|所有檔案 (*.*)|*.*",
                DefaultExt = ".txt",
                FileName = $"RunnerLogs_{DateTime.Now:yyyyMMdd_HHmmss}.txt"
            };

            if (dialog.ShowDialog() == true)
            {
                try
                {
                    File.WriteAllLines(dialog.FileName, _executionLogs);
                    MessageBox.Show($"日誌已成功匯出至:\n{dialog.FileName}", "匯出成功", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"匯出日誌失敗: {ex.Message}", "錯誤", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void OnStatusChanged(ExecutionStatus status)
        {
            // Do not handle here, logic is in Run loop
        }

        protected override void OnClosed(EventArgs e)
        {
            if (_globalHook != null)
            {
                _globalHook.KeyDown -= GlobalHook_KeyDown;
                _globalHook.Dispose();
            }
            if (_isRunning)
            {
                _executor.Stop();
            }

            // Cleanup temp directory if created
            if (_runtimeScriptDirectory != AppDomain.CurrentDomain.BaseDirectory && Directory.Exists(_runtimeScriptDirectory))
            {
                try { Directory.Delete(_runtimeScriptDirectory, true); } catch { /* Ignore cleanup errors on exit */ }
            }

            base.OnClosed(e);
        }
    }
}