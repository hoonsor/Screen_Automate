using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text.Json.Serialization;
using System.Threading;

namespace AutoWizard.Core.Actions.Input
{
    public enum KeyModifiers
    {
        None = 0,
        Alt = 1,
        Ctrl = 2,
        Shift = 4,
        Win = 8
    }

    public class KeyboardAction : AutoWizard.Core.Models.BaseAction
    {
        public string Key { get; set; } = string.Empty;
        public KeyModifiers Modifiers { get; set; } = KeyModifiers.None;
        public int HoldDurationMs { get; set; } = 0;
        /// <summary>
        /// 若為 true，此指令只送出 KeyUp 事件（修飾鍵放開錄製用）
        /// </summary>
        public bool IsKeyUp { get; set; } = false;

        /// <summary>
        /// 若為 true，此指令只送出 KeyDown 事件（修飾鍵按下且不放開）
        /// </summary>
        public bool IsKeyDown { get; set; } = false;

        [DllImport("user32.dll", SetLastError = true)]
        private static extern uint SendInput(uint nInputs, INPUT[] pInputs, int cbSize);

        [DllImport("user32.dll")]
        private static extern uint MapVirtualKey(uint uCode, uint uMapType);

        private const uint INPUT_KEYBOARD = 1;
        private const uint KEYEVENTF_EXTENDEDKEY = 0x0001;
        private const uint KEYEVENTF_KEYUP = 0x0002;
        private const uint KEYEVENTF_SCANCODE = 0x0008;
        private const uint MAPVK_VK_TO_VSC = 0;

        [StructLayout(LayoutKind.Sequential)]
        private struct INPUT
        {
            public uint type;
            public INPUTUNION union;
        }

        [StructLayout(LayoutKind.Explicit)]
        private struct INPUTUNION
        {
            [FieldOffset(0)]
            public MOUSEINPUT mi;
            [FieldOffset(0)]
            public KEYBDINPUT ki;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct MOUSEINPUT
        {
            public int dx;
            public int dy;
            public uint mouseData;
            public uint dwFlags;
            public uint time;
            public IntPtr dwExtraInfo;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct KEYBDINPUT
        {
            public ushort wVk;
            public ushort wScan;
            public uint dwFlags;
            public uint time;
            public IntPtr dwExtraInfo;
        }

        // 需要 EXTENDEDKEY 旗標的按鍵
        [JsonIgnore]
        private static readonly HashSet<byte> ExtendedKeys = new()
        {
            0x21, 0x22, 0x23, 0x24, // PageUp, PageDown, End, Home
            0x25, 0x26, 0x27, 0x28, // Arrow keys
            0x2D, 0x2E,             // Insert, Delete
            0x5B, 0x5C,             // Left/Right Win
            0xA4, 0xA5              // Left/Right Alt (extended)
        };

        public override AutoWizard.Core.Models.ActionResult Execute(AutoWizard.Core.Models.ExecutionContext context)
        {
            try
            {
                if (string.IsNullOrEmpty(Key))
                {
                    return new AutoWizard.Core.Models.ActionResult
                    {
                        Success = false,
                        Message = "Key is empty"
                    };
                }

                // 解析按鍵代碼
                if (!KeyMap.TryGetValue(Key.ToUpper(), out byte vkCode))
                {
                    return new AutoWizard.Core.Models.ActionResult
                    {
                        Success = false,
                        Message = $"Unknown key: {Key}"
                    };
                }

                var inputList = new List<INPUT>();

                if (IsKeyUp)
                {
                    // 只送 KeyUp（修飾鍵放開模式）
                    inputList.Add(MakeKeyInput(vkCode, true));
                    if (Modifiers.HasFlag(KeyModifiers.Win)) inputList.Add(MakeKeyInput(0x5B, true));
                    if (Modifiers.HasFlag(KeyModifiers.Alt)) inputList.Add(MakeKeyInput(0xA4, true));
                    if (Modifiers.HasFlag(KeyModifiers.Shift)) inputList.Add(MakeKeyInput(0xA0, true));
                    if (Modifiers.HasFlag(KeyModifiers.Ctrl)) inputList.Add(MakeKeyInput(0xA2, true));
                    SendInput((uint)inputList.Count, inputList.ToArray(), Marshal.SizeOf(typeof(INPUT)));
                    context.Log($"KeyUp: {Key}");
                }
                else if (IsKeyDown)
                {
                    // 只送 KeyDown（修飾鍵按下不放開模式）
                    if (Modifiers.HasFlag(KeyModifiers.Ctrl)) inputList.Add(MakeKeyInput(0xA2, false));
                    if (Modifiers.HasFlag(KeyModifiers.Shift)) inputList.Add(MakeKeyInput(0xA0, false));
                    if (Modifiers.HasFlag(KeyModifiers.Alt)) inputList.Add(MakeKeyInput(0xA4, false));
                    if (Modifiers.HasFlag(KeyModifiers.Win)) inputList.Add(MakeKeyInput(0x5B, false));
                    inputList.Add(MakeKeyInput(vkCode, false));
                    
                    SendInput((uint)inputList.Count, inputList.ToArray(), Marshal.SizeOf(typeof(INPUT)));
                    context.Log($"KeyDown: {Key}");
                }
                else
                {
                    // 標準模式: 按下修飾鍵 + 主鍵 Down，然後全部 Up
                    if (Modifiers.HasFlag(KeyModifiers.Ctrl)) inputList.Add(MakeKeyInput(0xA2, false));
                    if (Modifiers.HasFlag(KeyModifiers.Shift)) inputList.Add(MakeKeyInput(0xA0, false));
                    if (Modifiers.HasFlag(KeyModifiers.Alt)) inputList.Add(MakeKeyInput(0xA4, false));
                    if (Modifiers.HasFlag(KeyModifiers.Win)) inputList.Add(MakeKeyInput(0x5B, false));

                    // 按下主鍵
                    inputList.Add(MakeKeyInput(vkCode, false));

                    // 先發送按下事件
                    var downInputs = inputList.ToArray();
                    SendInput((uint)downInputs.Length, downInputs, Marshal.SizeOf(typeof(INPUT)));

                    if (HoldDurationMs > 0)
                    {
                        Thread.Sleep(HoldDurationMs);
                    }

                    // 放開事件（反向順序）
                    var upList = new List<INPUT>();
                    upList.Add(MakeKeyInput(vkCode, true));
                    if (Modifiers.HasFlag(KeyModifiers.Win)) upList.Add(MakeKeyInput(0x5B, true));
                    if (Modifiers.HasFlag(KeyModifiers.Alt)) upList.Add(MakeKeyInput(0xA4, true));
                    if (Modifiers.HasFlag(KeyModifiers.Shift)) upList.Add(MakeKeyInput(0xA0, true));
                    if (Modifiers.HasFlag(KeyModifiers.Ctrl)) upList.Add(MakeKeyInput(0xA2, true));

                    var upInputs = upList.ToArray();
                    SendInput((uint)upInputs.Length, upInputs, Marshal.SizeOf(typeof(INPUT)));
                    context.Log($"Pressed keys: {Modifiers} + {Key}");
                }

                return new AutoWizard.Core.Models.ActionResult
                {
                    Success = true,
                    Message = $"Key {(IsKeyUp ? "Up" : "Press")} executed: {Modifiers} + {Key}"
                };
            }
            catch (Exception ex)
            {
                return new AutoWizard.Core.Models.ActionResult
                {
                    Success = false,
                    Message = $"Keyboard action failed: {ex.Message}",
                    Exception = ex
                };
            }
        }

        private INPUT MakeKeyInput(byte vkCode, bool isKeyUp)
        {
            uint scanCode = MapVirtualKey(vkCode, MAPVK_VK_TO_VSC);
            uint flags = 0;
            if (ExtendedKeys.Contains(vkCode)) flags |= KEYEVENTF_EXTENDEDKEY;
            if (isKeyUp) flags |= KEYEVENTF_KEYUP;

            return new INPUT
            {
                type = INPUT_KEYBOARD,
                union = new INPUTUNION
                {
                    ki = new KEYBDINPUT
                    {
                        wVk = vkCode,
                        wScan = (ushort)scanCode,
                        dwFlags = flags,
                        time = 0,
                        dwExtraInfo = IntPtr.Zero
                    }
                }
            };
        }

        [JsonIgnore]
        private static readonly Dictionary<string, byte> KeyMap = new Dictionary<string, byte>
        {
            {"BACKSPACE", 0x08}, {"TAB", 0x09}, {"ENTER", 0x0D}, {"SHIFT", 0xA0},
            {"CTRL", 0xA2}, {"ALT", 0xA4}, {"PAUSE", 0x13}, {"CAPSLOCK", 0x14},
            {"ESC", 0x1B}, {"SPACE", 0x20}, {"PAGEUP", 0x21}, {"PAGEDOWN", 0x22},
            {"END", 0x23}, {"HOME", 0x24}, {"LEFT", 0x25}, {"UP", 0x26},
            {"RIGHT", 0x27}, {"DOWN", 0x28}, {"PRINTSCREEN", 0x2C}, {"INSERT", 0x2D},
            {"DELETE", 0x2E}, {"0", 0x30}, {"1", 0x31}, {"2", 0x32}, {"3", 0x33},
            {"4", 0x34}, {"5", 0x35}, {"6", 0x36}, {"7", 0x37}, {"8", 0x38},
            {"9", 0x39}, {"A", 0x41}, {"B", 0x42}, {"C", 0x43}, {"D", 0x44},
            {"E", 0x45}, {"F", 0x46}, {"G", 0x47}, {"H", 0x48}, {"I", 0x49},
            {"J", 0x4A}, {"K", 0x4B}, {"L", 0x4C}, {"M", 0x4D}, {"N", 0x4E},
            {"O", 0x4F}, {"P", 0x50}, {"Q", 0x51}, {"R", 0x52}, {"S", 0x53},
            {"T", 0x54}, {"U", 0x55}, {"V", 0x56}, {"W", 0x57}, {"X", 0x58},
            {"Y", 0x59}, {"Z", 0x5A},
            // Numpad 數字鍵
            {"NUMPAD0", 0x60}, {"NUMPAD1", 0x61}, {"NUMPAD2", 0x62}, {"NUMPAD3", 0x63},
            {"NUMPAD4", 0x64}, {"NUMPAD5", 0x65}, {"NUMPAD6", 0x66}, {"NUMPAD7", 0x67},
            {"NUMPAD8", 0x68}, {"NUMPAD9", 0x69},
            // Numpad 運算鍵
            {"NUMPAD*", 0x6A}, {"NUMPAD+", 0x6B}, {"NUMPADENTER", 0x6C},
            {"NUMPAD-", 0x6D}, {"NUMPAD.", 0x6E}, {"NUMPAD/", 0x6F},
            // 鎖定鍵
            {"NUMLOCK", 0x90}, {"SCROLLLOCK", 0x91},
            // 功能鍵
            {"F1", 0x70}, {"F2", 0x71}, {"F3", 0x72},
            {"F4", 0x73}, {"F5", 0x74}, {"F6", 0x75}, {"F7", 0x76}, {"F8", 0x77},
            {"F9", 0x78}, {"F10", 0x79}, {"F11", 0x7A}, {"F12", 0x7B}
        };
    }
}

