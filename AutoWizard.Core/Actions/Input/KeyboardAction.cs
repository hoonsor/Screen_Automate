using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using System.Threading;
using AutoWizard.Core.Input;

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

                var input = context.Input;
                if (IsKeyUp)
                {
                    // 只送 KeyUp（修飾鍵放開模式）
                    input.SendKeys(ReleaseOrder(vkCode));
                    context.Log($"KeyUp: {Key}");
                }
                else if (IsKeyDown)
                {
                    // 只送 KeyDown（修飾鍵按下不放開模式）
                    input.SendKeys(PressOrder(vkCode));
                    context.Log($"KeyDown: {Key}");
                }
                else
                {
                    // 標準模式: 按下修飾鍵 + 主鍵 Down，然後全部 Up（反向順序）
                    input.SendKeys(PressOrder(vkCode));

                    if (HoldDurationMs > 0)
                    {
                        Thread.Sleep(HoldDurationMs);
                    }

                    input.SendKeys(ReleaseOrder(vkCode));
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

        /// <summary>按下順序：Ctrl → Shift → Alt → Win → 主鍵。</summary>
        private List<KeyStroke> PressOrder(byte vkCode)
        {
            var list = new List<KeyStroke>();
            if (Modifiers.HasFlag(KeyModifiers.Ctrl)) list.Add(new KeyStroke(0xA2, false));
            if (Modifiers.HasFlag(KeyModifiers.Shift)) list.Add(new KeyStroke(0xA0, false));
            if (Modifiers.HasFlag(KeyModifiers.Alt)) list.Add(new KeyStroke(0xA4, false));
            if (Modifiers.HasFlag(KeyModifiers.Win)) list.Add(new KeyStroke(0x5B, false));
            list.Add(new KeyStroke(vkCode, false));
            return list;
        }

        /// <summary>放開順序：主鍵 → Win → Alt → Shift → Ctrl。</summary>
        private List<KeyStroke> ReleaseOrder(byte vkCode)
        {
            var list = new List<KeyStroke> { new(vkCode, true) };
            if (Modifiers.HasFlag(KeyModifiers.Win)) list.Add(new KeyStroke(0x5B, true));
            if (Modifiers.HasFlag(KeyModifiers.Alt)) list.Add(new KeyStroke(0xA4, true));
            if (Modifiers.HasFlag(KeyModifiers.Shift)) list.Add(new KeyStroke(0xA0, true));
            if (Modifiers.HasFlag(KeyModifiers.Ctrl)) list.Add(new KeyStroke(0xA2, true));
            return list;
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
