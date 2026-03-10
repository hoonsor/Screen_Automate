using System;
using System.Runtime.InteropServices;

namespace AutoWizard.Core.Recording
{
    /// <summary>
    /// DPI 感知工具 - 處理高 DPI 螢幕的座標轉換
    /// </summary>
    public static class DpiHelper
    {
        [DllImport("user32.dll")]
        private static extern IntPtr GetDC(IntPtr hWnd);

        [DllImport("gdi32.dll")]
        private static extern int GetDeviceCaps(IntPtr hdc, int nIndex);

        [DllImport("user32.dll")]
        private static extern int ReleaseDC(IntPtr hWnd, IntPtr hDC);

        private const int LOGPIXELSX = 88;
        private const int LOGPIXELSY = 90;

        private static double? _dpiScaleX;
        private static double? _dpiScaleY;

        public static double DpiScaleX
        {
            get
            {
                if (_dpiScaleX == null)
                {
                    IntPtr hdc = GetDC(IntPtr.Zero);
                    int dpiX = GetDeviceCaps(hdc, LOGPIXELSX);
                    ReleaseDC(IntPtr.Zero, hdc);
                    _dpiScaleX = dpiX / 96.0;
                }
                return _dpiScaleX.Value;
            }
        }

        public static double DpiScaleY
        {
            get
            {
                if (_dpiScaleY == null)
                {
                    IntPtr hdc = GetDC(IntPtr.Zero);
                    int dpiY = GetDeviceCaps(hdc, LOGPIXELSY);
                    ReleaseDC(IntPtr.Zero, hdc);
                    _dpiScaleY = dpiY / 96.0;
                }
                return _dpiScaleY.Value;
            }
        }

        /// <summary>
        /// 將實體像素轉換為邏輯像素
        /// </summary>
        public static (int x, int y) PhysicalToLogical(int physicalX, int physicalY)
        {
            return (
                (int)(physicalX / DpiScaleX),
                (int)(physicalY / DpiScaleY)
            );
        }

        /// <summary>
        /// 將邏輯像素轉換為實體像素
        /// </summary>
        public static (int x, int y) LogicalToPhysical(int logicalX, int logicalY)
        {
            return (
                (int)(logicalX * DpiScaleX),
                (int)(logicalY * DpiScaleY)
            );
        }
    }
}
