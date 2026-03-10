using System;
using System.Runtime.InteropServices;
using System.Diagnostics;

namespace AutoWizard.Core.Recording
{
    /// <summary>
    /// 全域滑鼠與鍵盤 Hook (使用 Windows API)
    /// </summary>
    public class GlobalHook : IDisposable
    {
        private const int WH_MOUSE_LL = 14;
        private const int WH_KEYBOARD_LL = 13;

        private IntPtr _mouseHookHandle = IntPtr.Zero;
        private IntPtr _keyboardHookHandle = IntPtr.Zero;
        private LowLevelProc? _mouseProc;
        private LowLevelProc? _keyboardProc;

        public event EventHandler<MouseEventArgs>? MouseEvent;
        public event EventHandler<KeyboardEventArgs>? KeyboardEvent;

        private delegate IntPtr LowLevelProc(int nCode, IntPtr wParam, IntPtr lParam);

        #region Windows API

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelProc lpfn, IntPtr hMod, uint dwThreadId);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr GetModuleHandle(string lpModuleName);

        [StructLayout(LayoutKind.Sequential)]
        private struct POINT
        {
            public int x;
            public int y;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct MSLLHOOKSTRUCT
        {
            public POINT pt;
            public uint mouseData;
            public uint flags;
            public uint time;
            public IntPtr dwExtraInfo;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct KBDLLHOOKSTRUCT
        {
            public uint vkCode;
            public uint scanCode;
            public uint flags;
            public uint time;
            public IntPtr dwExtraInfo;
        }

        #endregion

        public void Start()
        {
            if (_mouseHookHandle != IntPtr.Zero || _keyboardHookHandle != IntPtr.Zero)
                return;

            using (Process curProcess = Process.GetCurrentProcess())
            using (ProcessModule? curModule = curProcess.MainModule)
            {
                if (curModule != null)
                {
                    _mouseProc = MouseHookCallback;
                    _keyboardProc = KeyboardHookCallback;

                    _mouseHookHandle = SetWindowsHookEx(WH_MOUSE_LL, _mouseProc, 
                        GetModuleHandle(curModule.ModuleName), 0);
                    _keyboardHookHandle = SetWindowsHookEx(WH_KEYBOARD_LL, _keyboardProc, 
                        GetModuleHandle(curModule.ModuleName), 0);
                }
            }
        }

        public void Stop()
        {
            if (_mouseHookHandle != IntPtr.Zero)
            {
                UnhookWindowsHookEx(_mouseHookHandle);
                _mouseHookHandle = IntPtr.Zero;
            }

            if (_keyboardHookHandle != IntPtr.Zero)
            {
                UnhookWindowsHookEx(_keyboardHookHandle);
                _keyboardHookHandle = IntPtr.Zero;
            }
        }

        private IntPtr MouseHookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0)
            {
                MSLLHOOKSTRUCT hookStruct = Marshal.PtrToStructure<MSLLHOOKSTRUCT>(lParam);
                
                var args = new MouseEventArgs
                {
                    X = hookStruct.pt.x,
                    Y = hookStruct.pt.y,
                    EventType = (MouseEventType)wParam.ToInt32(),
                    Timestamp = DateTime.Now
                };

                MouseEvent?.Invoke(this, args);
            }

            return CallNextHookEx(_mouseHookHandle, nCode, wParam, lParam);
        }

        private IntPtr KeyboardHookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0)
            {
                KBDLLHOOKSTRUCT hookStruct = Marshal.PtrToStructure<KBDLLHOOKSTRUCT>(lParam);
                
                var args = new KeyboardEventArgs
                {
                    VirtualKeyCode = (int)hookStruct.vkCode,
                    ScanCode = (int)hookStruct.scanCode,
                    IsKeyDown = wParam.ToInt32() == 0x100 || wParam.ToInt32() == 0x104,
                    Timestamp = DateTime.Now
                };

                KeyboardEvent?.Invoke(this, args);

                if (args.Handled)
                    return (IntPtr)1;
            }

            return CallNextHookEx(_keyboardHookHandle, nCode, wParam, lParam);
        }

        public void Dispose()
        {
            Stop();
            GC.SuppressFinalize(this);
        }
    }

    public class MouseEventArgs : EventArgs
    {
        public int X { get; set; }
        public int Y { get; set; }
        public MouseEventType EventType { get; set; }
        public DateTime Timestamp { get; set; }
    }

    public class KeyboardEventArgs : EventArgs
    {
        public int VirtualKeyCode { get; set; }
        public int ScanCode { get; set; }
        public bool IsKeyDown { get; set; }
        public DateTime Timestamp { get; set; }
        public bool Handled { get; set; }
    }

    public enum MouseEventType
    {
        MouseMove = 0x200,
        LeftButtonDown = 0x201,
        LeftButtonUp = 0x202,
        RightButtonDown = 0x204,
        RightButtonUp = 0x205,
        MiddleButtonDown = 0x207,
        MiddleButtonUp = 0x208,
        MouseWheel = 0x20A
    }
}
