using System.Drawing;
using System.Runtime.InteropServices;
using AutoWizard.Core.Actions.Input;

namespace AutoWizard.Core.Input;

/// <summary>一個按鍵事件（虛擬鍵碼＋按下或放開）。</summary>
public readonly record struct KeyStroke(byte VirtualKey, bool KeyUp);

/// <summary>
/// 送出鍵盤滑鼠輸入的介面。動作一律透過 <see cref="Models.ExecutionContext.Input"/> 送出，
/// 不直接呼叫 SendInput；測試時換成 <see cref="RecordingInputSimulator"/>，只記錄、不碰真實的鍵盤滑鼠。
/// </summary>
public interface IInputSimulator
{
    void SendUnicodeChar(char c);
    void SendKeys(IReadOnlyList<KeyStroke> strokes);
    /// <summary>移動游標到螢幕座標（實體像素）。</summary>
    void MoveMouse(int screenX, int screenY);
    void MouseButton(MouseButton button, bool down, int screenX, int screenY);
    Point GetCursorPosition();
}

/// <summary>全域預設輸入實作。程式執行時是真的 SendInput；測試專案在啟動時換成只記錄的版本。</summary>
public static class InputSimulator
{
    public static IInputSimulator Default { get; set; } = new Win32InputSimulator();
}

/// <summary>以 Win32 SendInput 送出真實輸入。</summary>
public sealed class Win32InputSimulator : IInputSimulator
{
    private const uint INPUT_MOUSE = 0;
    private const uint INPUT_KEYBOARD = 1;
    private const uint KEYEVENTF_EXTENDEDKEY = 0x0001;
    private const uint KEYEVENTF_KEYUP = 0x0002;
    private const uint KEYEVENTF_UNICODE = 0x0004;
    private const uint MAPVK_VK_TO_VSC = 0;
    private const uint MOUSEEVENTF_MOVE = 0x0001;
    private const uint MOUSEEVENTF_LEFTDOWN = 0x0002;
    private const uint MOUSEEVENTF_LEFTUP = 0x0004;
    private const uint MOUSEEVENTF_RIGHTDOWN = 0x0008;
    private const uint MOUSEEVENTF_RIGHTUP = 0x0010;
    private const uint MOUSEEVENTF_MIDDLEDOWN = 0x0020;
    private const uint MOUSEEVENTF_MIDDLEUP = 0x0040;
    private const uint MOUSEEVENTF_ABSOLUTE = 0x8000;

    // 需要 EXTENDEDKEY 旗標的按鍵：PageUp/PageDown/End/Home、方向鍵、Insert/Delete、左右 Win、右 Alt
    private static readonly HashSet<byte> ExtendedKeys = new()
    {
        0x21, 0x22, 0x23, 0x24, 0x25, 0x26, 0x27, 0x28, 0x2D, 0x2E, 0x5B, 0x5C, 0xA4, 0xA5,
    };

    public void SendUnicodeChar(char c)
    {
        var inputs = new[]
        {
            Keyboard(new KEYBDINPUT { wScan = c, dwFlags = KEYEVENTF_UNICODE }),
            Keyboard(new KEYBDINPUT { wScan = c, dwFlags = KEYEVENTF_UNICODE | KEYEVENTF_KEYUP }),
        };
        Send(inputs);
    }

    public void SendKeys(IReadOnlyList<KeyStroke> strokes)
    {
        if (strokes.Count == 0) return;
        Send(strokes.Select(s =>
        {
            // 以虛擬鍵碼送出（附掃描碼供部分程式判讀），與原 KeyboardAction 行為相同
            uint flags = 0;
            if (ExtendedKeys.Contains(s.VirtualKey)) flags |= KEYEVENTF_EXTENDEDKEY;
            if (s.KeyUp) flags |= KEYEVENTF_KEYUP;
            return Keyboard(new KEYBDINPUT
            {
                wVk = s.VirtualKey,
                wScan = (ushort)MapVirtualKey(s.VirtualKey, MAPVK_VK_TO_VSC),
                dwFlags = flags,
            });
        }).ToArray());
    }

    public void MoveMouse(int screenX, int screenY)
    {
        var (ax, ay) = ToAbsolute(screenX, screenY);
        Send(new[] { Mouse(ax, ay, MOUSEEVENTF_ABSOLUTE | MOUSEEVENTF_MOVE) });
    }

    public void MouseButton(MouseButton button, bool down, int screenX, int screenY)
    {
        uint flag = (button, down) switch
        {
            (Actions.Input.MouseButton.Right, true) => MOUSEEVENTF_RIGHTDOWN,
            (Actions.Input.MouseButton.Right, false) => MOUSEEVENTF_RIGHTUP,
            (Actions.Input.MouseButton.Middle, true) => MOUSEEVENTF_MIDDLEDOWN,
            (Actions.Input.MouseButton.Middle, false) => MOUSEEVENTF_MIDDLEUP,
            (_, true) => MOUSEEVENTF_LEFTDOWN,
            (_, false) => MOUSEEVENTF_LEFTUP,
        };
        var (ax, ay) = ToAbsolute(screenX, screenY);
        Send(new[] { Mouse(ax, ay, flag | MOUSEEVENTF_ABSOLUTE | MOUSEEVENTF_MOVE) });
    }

    public Point GetCursorPosition()
    {
        GetCursorPos(out var p);
        return new Point(p.X, p.Y);
    }

    /// <summary>SendInput 絕對座標以主螢幕 0–65535 表示。</summary>
    private static (int X, int Y) ToAbsolute(int x, int y)
    {
        int w = GetSystemMetrics(0), h = GetSystemMetrics(1);
        return (x * 65536 / Math.Max(1, w), y * 65536 / Math.Max(1, h));
    }

    private static INPUT Keyboard(KEYBDINPUT ki) => new() { type = INPUT_KEYBOARD, union = new INPUTUNION { ki = ki } };

    private static INPUT Mouse(int ax, int ay, uint flags)
        => new() { type = INPUT_MOUSE, union = new INPUTUNION { mi = new MOUSEINPUT { dx = ax, dy = ay, dwFlags = flags } } };

    private static void Send(INPUT[] inputs)
    {
        uint sent = SendInput((uint)inputs.Length, inputs, Marshal.SizeOf<INPUT>());
        if (sent != inputs.Length)
            throw new InvalidOperationException(
                $"SendInput 只送出 {sent}/{inputs.Length} 個事件（錯誤碼 {Marshal.GetLastWin32Error()}）。" +
                "常見原因：目標視窗以系統管理員執行、桌面已鎖定，或在非互動式工作階段執行。");
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct INPUT
    {
        public uint type;
        public INPUTUNION union;
    }

    [StructLayout(LayoutKind.Explicit)]
    private struct INPUTUNION
    {
        [FieldOffset(0)] public MOUSEINPUT mi;
        [FieldOffset(0)] public KEYBDINPUT ki;
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

    [StructLayout(LayoutKind.Sequential)]
    private struct POINT
    {
        public int X;
        public int Y;
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern uint SendInput(uint nInputs, INPUT[] pInputs, int cbSize);

    [DllImport("user32.dll")]
    private static extern uint MapVirtualKey(uint uCode, uint uMapType);

    [DllImport("user32.dll")]
    private static extern int GetSystemMetrics(int nIndex);

    [DllImport("user32.dll")]
    private static extern bool GetCursorPos(out POINT lpPoint);
}

/// <summary>
/// 只記錄、不送出任何輸入的實作（測試與試跑用）。游標位置只在記憶體中移動。
/// </summary>
public sealed class RecordingInputSimulator : IInputSimulator
{
    private readonly object _gate = new();
    private readonly List<string> _events = new();
    private readonly System.Text.StringBuilder _text = new();
    private Point _cursor;

    /// <summary>依序記錄的事件，例如「char O」「key 0x0D down」「move 100,200」「button Left down 100,200」。</summary>
    public IReadOnlyList<string> Events { get { lock (_gate) return _events.ToList(); } }

    /// <summary>以 SendUnicodeChar 送出的所有字元。</summary>
    public string TypedText { get { lock (_gate) return _text.ToString(); } }

    public List<KeyStroke> KeyStrokes { get; } = new();

    public void SendUnicodeChar(char c)
    {
        lock (_gate)
        {
            _text.Append(c);
            _events.Add($"char {c}");
        }
    }

    public void SendKeys(IReadOnlyList<KeyStroke> strokes)
    {
        lock (_gate)
            foreach (var s in strokes)
            {
                KeyStrokes.Add(s);
                _events.Add($"key 0x{s.VirtualKey:X2} {(s.KeyUp ? "up" : "down")}");
            }
    }

    public void MoveMouse(int screenX, int screenY)
    {
        lock (_gate)
        {
            _cursor = new Point(screenX, screenY);
            _events.Add($"move {screenX},{screenY}");
        }
    }

    public void MouseButton(MouseButton button, bool down, int screenX, int screenY)
    {
        lock (_gate)
        {
            _cursor = new Point(screenX, screenY);
            _events.Add($"button {button} {(down ? "down" : "up")} {screenX},{screenY}");
        }
    }

    public Point GetCursorPosition()
    {
        lock (_gate) return _cursor;
    }
}
