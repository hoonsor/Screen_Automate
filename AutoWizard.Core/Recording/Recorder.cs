using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using AutoWizard.Core.Models;
using AutoWizard.Core.Actions.Input;
using System.Drawing;

namespace AutoWizard.Core.Recording
{
    /// <summary>
    /// 錄製器 - 捕捉使用者操作並轉換為指令
    /// </summary>
    public class Recorder : IDisposable
    {
        private readonly GlobalHook _hook;
        private readonly List<BaseAction> _recordedActions;
        private readonly HashSet<int> _pressedKeys = new();
        private bool _isRecording;
        private bool _isPaused;
        private DateTime _lastEventTime;
        private const int EventMergeThresholdMs = 500;
        private const int VK_F9 = 0x78;
        private const int VK_SCROLL = 0x91; // ScrollLock

        public event EventHandler<ActionRecordedEventArgs>? ActionRecorded;
        /// <summary>
        /// 當使用者按下 F9 要求停止錄製時觸發
        /// </summary>
        public event EventHandler? StopRequested;
        
        /// <summary>
        /// 當使用者按下 F11 要求即時截圖時觸發
        /// </summary>
        public event EventHandler? SmartCaptureRequested;

        public Recorder()
        {
            _hook = new GlobalHook();
            _hook.MouseEvent += OnMouseEvent;
            _hook.KeyboardEvent += OnKeyboardEvent;
            _recordedActions = new List<BaseAction>();
        }

        public bool IsRecording => _isRecording;
        public bool IsPaused => _isPaused;

        [DllImport("user32.dll")]
        private static extern int ToUnicode(uint wVirtKey, uint wScanCode, byte[] lpKeyState,
            [Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pwszBuff, int cchBuff, uint wFlags);

        [DllImport("user32.dll")]
        private static extern uint MapVirtualKey(uint uCode, uint uMapType);

        [DllImport("user32.dll")]
        private static extern bool GetKeyboardState(byte[] lpKeyState);

        public void StartRecording()
        {
            if (_isRecording) return;

            _recordedActions.Clear();
            _pressedKeys.Clear();
            _isRecording = true;
            _lastEventTime = DateTime.Now;
            _hook.Start();
        }

        public void StopRecording()
        {
            if (!_isRecording) return;

            _isRecording = false;
            _hook.Stop();
        }

        public void PauseRecording()
        {
            _isPaused = true;
        }

        public void ResumeRecording()
        {
            _pressedKeys.Clear();
            _isPaused = false;
        }

        public List<BaseAction> GetRecordedActions()
        {
            return new List<BaseAction>(_recordedActions);
        }

        // ...

        private POINT? _lastMouseDownPos;
        private DateTime _lastMouseDownTime;
        private MouseButton _lastMouseDownButton;
        private const int DragDistanceThreshold = 5; // pixels
        private const int DragTimeThresholdMs = 200; // ms

        private void OnMouseEvent(object? sender, MouseEventArgs e)
        {
            if (!_isRecording || _isPaused) return;

            if (e.EventType == MouseEventType.LeftButtonDown || e.EventType == MouseEventType.RightButtonDown)
            {
                _lastMouseDownPos = new POINT { x = e.X, y = e.Y };
                _lastMouseDownTime = DateTime.Now;
                _lastMouseDownButton = e.EventType == MouseEventType.LeftButtonDown ? MouseButton.Left : MouseButton.Right;
            }
            else if (e.EventType == MouseEventType.LeftButtonUp || e.EventType == MouseEventType.RightButtonUp)
            {
                var upButton = e.EventType == MouseEventType.LeftButtonUp ? MouseButton.Left : MouseButton.Right;
                
                // Ensure the Up event matches the last Down event button
                if (_lastMouseDownPos.HasValue && _lastMouseDownButton == upButton)
                {
                    double distance = Math.Sqrt(Math.Pow(e.X - _lastMouseDownPos.Value.x, 2) + Math.Pow(e.Y - _lastMouseDownPos.Value.y, 2));
                    double timeElapsed = (DateTime.Now - _lastMouseDownTime).TotalMilliseconds;

                    if (distance > DragDistanceThreshold || timeElapsed > DragTimeThresholdMs)
                    {
                        // It's a drag or a long press -> Emit Down then Up
                        var downAction = CreateClickAction(_lastMouseDownPos.Value.x, _lastMouseDownPos.Value.y, upButton.ToString(), ClickType.Down);
                        var upAction = CreateClickAction(e.X, e.Y, upButton.ToString(), ClickType.Up);
                        
                        _recordedActions.Add(downAction);
                        ActionRecorded?.Invoke(this, new ActionRecordedEventArgs { Action = downAction });
                        
                        _recordedActions.Add(upAction);
                        ActionRecorded?.Invoke(this, new ActionRecordedEventArgs { Action = upAction });
                    }
                    else
                    {
                        // It's a regular click
                        var action = CreateClickAction(e.X, e.Y, upButton.ToString(), ClickType.Single);
                        _recordedActions.Add(action);
                        ActionRecorded?.Invoke(this, new ActionRecordedEventArgs { Action = action });
                    }
                }
                
                _lastMouseDownPos = null;
            }
        }

        private void OnKeyboardEvent(object? sender, KeyboardEventArgs e)
        {
            if (!_isRecording) return;

            // ScrollLock 智慧截圖 (只處理 KeyDown)
            if (e.IsKeyDown && e.VirtualKeyCode == VK_SCROLL)
            {
                e.Handled = true;
                SmartCaptureRequested?.Invoke(this, EventArgs.Empty);
                return;
            }

            // F9 停止錄製 (只處理 KeyDown)
            if (e.IsKeyDown && e.VirtualKeyCode == VK_F9)
            {
                e.Handled = true;
                StopRequested?.Invoke(this, EventArgs.Empty);
                return;
            }

            if (_isPaused) return;

            // 處理長按產生的重複事件 (Auto-repeat)
            if (e.IsKeyDown)
            {
                if (!_pressedKeys.Add(e.VirtualKeyCode))
                    return; // 已經按下，忽略重複事件
            }
            else
            {
                _pressedKeys.Remove(e.VirtualKeyCode);
            }

            // 修飾鍵（Ctrl/Shift/Alt）單獨錄製 Down 和 Up
            if (IsModifierKey(e.VirtualKeyCode))
            {
                var modKeyName = GetModifierKeyName(e.VirtualKeyCode);
                if (modKeyName != null)
                {
                    var modAction = new KeyboardAction
                    {
                        Name = $"修飾鍵 {modKeyName} {(e.IsKeyDown ? "↓" : "↑")}",
                        Description = $"{(e.IsKeyDown ? "按下" : "放開")} {modKeyName}",
                        Key = modKeyName,
                        Modifiers = KeyModifiers.None,
                        IsKeyDown = e.IsKeyDown,
                        IsKeyUp = !e.IsKeyDown
                    };
                    _recordedActions.Add(modAction);
                    ActionRecorded?.Invoke(this, new ActionRecordedEventArgs { Action = modAction });
                }
                return;
            }

            // 一般按鍵只處理 KeyDown
            if (!e.IsKeyDown) return;

            // 如果有修飾鍵 (Ctrl/Alt/Win)，或者不產生字元，使用 KeyboardAction
            var modifiers = GetCurrentModifiers();
            if (modifiers != KeyModifiers.None)
            {
                // 組合鍵 (e.g. Ctrl+C)
                var keyName = GetKeyName(e.VirtualKeyCode);
                var action = new KeyboardAction
                {
                    Name = $"組合鍵 {modifiers} + {keyName}",
                    Description = $"按下 {modifiers} + {keyName}",
                    Key = keyName,
                    Modifiers = modifiers
                };
                _recordedActions.Add(action);
                ActionRecorded?.Invoke(this, new ActionRecordedEventArgs { Action = action });
                return;
            }

            // 沒有修飾鍵的情況
            // 先檢查是否為特殊鍵（Enter, Tab, 方向鍵, F1-F12 等）
            var specialKeyName = GetSpecialKeyName(e.VirtualKeyCode);
            if (specialKeyName != null)
            {
                // 特殊鍵使用 KeyboardAction 回放（非 TypeAction）
                var kbAction = new KeyboardAction
                {
                    Name = $"按鍵 {specialKeyName}",
                    Description = $"按下 {specialKeyName}",
                    Key = specialKeyName,
                    Modifiers = KeyModifiers.None
                };
                _recordedActions.Add(kbAction);
                ActionRecorded?.Invoke(this, new ActionRecordedEventArgs { Action = kbAction });
                return;
            }

            // 嘗試產生一般輸入（可列印字元）
            var charAction = CreateKeyAction(e.VirtualKeyCode, e.ScanCode);
            if (charAction != null)
            {
                _recordedActions.Add(charAction);
                ActionRecorded?.Invoke(this, new ActionRecordedEventArgs { Action = charAction });
            }
        }

        private static string? GetModifierKeyName(int vkCode) => vkCode switch
        {
            0x10 or 0xA0 or 0xA1 => "SHIFT",
            0x11 or 0xA2 or 0xA3 => "CTRL",
            0x12 or 0xA4 or 0xA5 => "ALT",
            0x5B or 0x5C => "WIN",
            _ => null
        };

        private KeyModifiers GetCurrentModifiers()
        {
            KeyModifiers mods = KeyModifiers.None;
            if (IsKeyPressed(0x11)) mods |= KeyModifiers.Ctrl;  // VK_CONTROL
            if (IsKeyPressed(0x10)) mods |= KeyModifiers.Shift; // VK_SHIFT
            if (IsKeyPressed(0x12)) mods |= KeyModifiers.Alt;   // VK_MENU
            if (IsKeyPressed(0x5B) || IsKeyPressed(0x5C)) mods |= KeyModifiers.Win;
            return mods;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct POINT
        {
            public int x;
            public int y;
        }

        private bool IsKeyPressed(int vkCode)
        {
            return (GetKeyState(vkCode) & 0x8000) != 0;
        }

        [DllImport("user32.dll")]
        private static extern short GetKeyState(int nVirtKey);

        private string GetKeyName(int vkCode)
        {
            // 特殊鍵名稱優先
            var special = GetSpecialKeyName(vkCode);
            if (special != null) return special;

            // 數字鍵 0-9 (VK_0 ~ VK_9 = 0x30 ~ 0x39)
            if (vkCode >= 0x30 && vkCode <= 0x39)
                return ((char)vkCode).ToString();

            // 字母鍵 A-Z (VK_A ~ VK_Z = 0x41 ~ 0x5A)
            if (vkCode >= 0x41 && vkCode <= 0x5A)
                return ((char)vkCode).ToString();

            // 嘗試轉換為字元（其他鍵）
            var ch = VirtualKeyToChar(vkCode, 0);
            return ch.HasValue ? ch.Value.ToString().ToUpper() : $"VK_{vkCode}";
        }

        private BaseAction CreateClickAction(int x, int y, string button, ClickType type = ClickType.Single)
        {
            var mouseButton = button.ToLower() == "left" ? MouseButton.Left : MouseButton.Right;
            var buttonName = mouseButton == MouseButton.Left ? "左鍵" : "右鍵";
            var typeName = type switch
            {
                ClickType.Down => "按下",
                ClickType.Up => "放開",
                _ => "點擊"
            };
            
            return new ClickAction
            {
                Name = $"{typeName} ({x}, {y})",
                Description = $"{buttonName}{typeName}座標 ({x}, {y})",
                X = x,
                Y = y,
                Button = mouseButton,
                ClickType = type
            };
        }

        private BaseAction? CreateKeyAction(int virtualKeyCode, int scanCode)
        {
            // 嘗試轉換為可列印字元
            char? ch = VirtualKeyToChar(virtualKeyCode, scanCode);
            if (ch.HasValue)
            {
                string text = ch.Value.ToString();
                return new TypeAction
                {
                    Name = $"輸入 \"{text}\"",
                    Description = $"輸入文字: {text}",
                    Text = text
                };
            }

            // 無法辨識的按鍵，忽略
            return null;
        }

        /// <summary>
        /// 將虛擬鍵碼轉換為可列印字元（如果可能的話）
        /// </summary>
        private char? VirtualKeyToChar(int virtualKeyCode, int scanCode)
        {
            try
            {
                byte[] keyboardState = new byte[256];
                GetKeyboardState(keyboardState);

                var sb = new StringBuilder(4);
                uint sc = scanCode != 0 ? (uint)scanCode : MapVirtualKey((uint)virtualKeyCode, 0);
                int result = ToUnicode((uint)virtualKeyCode, sc, keyboardState, sb, sb.Capacity, 0);

                if (result > 0)
                {
                    char c = sb[0];
                    // 過濾控制字元
                    if (!char.IsControl(c))
                        return c;
                }
            }
            catch
            {
                // 忽略轉換失敗
            }
            return null;
        }

        /// <summary>
        /// 檢查是否為修飾鍵
        /// </summary>
        private bool IsModifierKey(int virtualKeyCode)
        {
            return virtualKeyCode switch
            {
                0x10 or 0x11 or 0x12 => true, // Shift, Ctrl, Alt
                0xA0 or 0xA1 => true,         // Left/Right Shift
                0xA2 or 0xA3 => true,         // Left/Right Ctrl
                0xA4 or 0xA5 => true,         // Left/Right Alt
                0x5B or 0x5C => true,         // Left/Right Win
                _ => false
            };
        }

        /// <summary>
        /// 取得特殊鍵（非可列印字元鍵）的名稱
        /// </summary>
        private string? GetSpecialKeyName(int virtualKeyCode)
        {
            return virtualKeyCode switch
            {
                0x08 => "Backspace",
                0x09 => "Tab",
                0x0D => "Enter",
                0x1B => "Esc",
                0x20 => "Space",
                0x21 => "PageUp",
                0x22 => "PageDown",
                0x23 => "End",
                0x24 => "Home",
                0x25 => "Left",
                0x26 => "Up",
                0x27 => "Right",
                0x28 => "Down",
                0x2D => "Insert",
                0x2E => "Delete",
                // 修飾鍵
                0x10 or 0xA0 or 0xA1 => "Shift",
                0x11 or 0xA2 or 0xA3 => "Ctrl",
                0x12 or 0xA4 or 0xA5 => "Alt",
                0x5B or 0x5C => null,  // Win 鍵可能觸發系統選單，暫不錄製
                // Numpad 數字鍵（用 VK 碼回放，不觸發 IME）
                0x60 => "Numpad0",
                0x61 => "Numpad1",
                0x62 => "Numpad2",
                0x63 => "Numpad3",
                0x64 => "Numpad4",
                0x65 => "Numpad5",
                0x66 => "Numpad6",
                0x67 => "Numpad7",
                0x68 => "Numpad8",
                0x69 => "Numpad9",
                // Numpad 運算鍵
                0x6A => "Numpad*",
                0x6B => "Numpad+",
                0x6C => "NumpadEnter",
                0x6D => "Numpad-",
                0x6E => "Numpad.",
                0x6F => "Numpad/",
                // 功能鍵
                0x70 => "F1",
                0x71 => "F2",
                0x72 => "F3",
                0x73 => "F4",
                0x74 => "F5",
                0x75 => "F6",
                0x76 => "F7",
                0x77 => "F8",
                // F9 (0x78) 已在上層攔截
                0x79 => "F10",
                0x7A => "F11",
                0x7B => "F12",
                0x2C => "PrintScreen",
                0x13 => "Pause",
                0x14 => "CapsLock",
                0x90 => "NumLock",
                0x91 => "ScrollLock",
                _ => null
            };
        }

        public void Dispose()
        {
            StopRecording();
            _hook.Dispose();
            GC.SuppressFinalize(this);
        }
    }

    public class ActionRecordedEventArgs : EventArgs
    {
        public required BaseAction Action { get; set; }
    }
}
