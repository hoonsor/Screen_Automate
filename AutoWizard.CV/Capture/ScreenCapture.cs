using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace AutoWizard.CV.Capture
{
    /// <summary>
    /// 螢幕截圖引擎
    /// </summary>
    public class ScreenCapture
    {
        /// <summary>
        /// 截取整個螢幕
        /// </summary>
        public static Bitmap CaptureScreen()
        {
            return CaptureRegion(0, 0, Screen.PrimaryScreen.Bounds.Width, Screen.PrimaryScreen.Bounds.Height);
        }

        /// <summary>
        /// 截取指定區域
        /// </summary>
        public static Bitmap CaptureRegion(int x, int y, int width, int height)
        {
            try
            {
                var bitmap = new Bitmap(width, height, PixelFormat.Format24bppRgb);
                
                using (var graphics = Graphics.FromImage(bitmap))
                {
                    graphics.CopyFromScreen(x, y, 0, 0, new Size(width, height), CopyPixelOperation.SourceCopy);
                }

                return bitmap;
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to capture screen: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 截取指定視窗
        /// </summary>
        public static Bitmap? CaptureWindow(IntPtr hWnd)
        {
            try
            {
                RECT rect;
                if (!GetWindowRect(hWnd, out rect))
                {
                    return null;
                }

                int width = rect.Right - rect.Left;
                int height = rect.Bottom - rect.Top;

                var bitmap = new Bitmap(width, height, PixelFormat.Format24bppRgb);
                
                using (var graphics = Graphics.FromImage(bitmap))
                {
                    IntPtr hdcBitmap = graphics.GetHdc();
                    PrintWindow(hWnd, hdcBitmap, 0);
                    graphics.ReleaseHdc(hdcBitmap);
                }

                return bitmap;
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to capture window: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 取得 DPI 縮放比例
        /// </summary>
        public static float GetDpiScale()
        {
            using (var graphics = Graphics.FromHwnd(IntPtr.Zero))
            {
                return graphics.DpiX / 96.0f;
            }
        }

        /// <summary>
        /// 將邏輯座標轉換為實際螢幕座標 (考慮 DPI)
        /// </summary>
        public static Point LogicalToPhysical(int logicalX, int logicalY)
        {
            float scale = GetDpiScale();
            return new Point((int)(logicalX * scale), (int)(logicalY * scale));
        }

        /// <summary>
        /// 將實際螢幕座標轉換為邏輯座標
        /// </summary>
        public static Point PhysicalToLogical(int physicalX, int physicalY)
        {
            float scale = GetDpiScale();
            return new Point((int)(physicalX / scale), (int)(physicalY / scale));
        }

        #region Windows API

        [DllImport("user32.dll")]
        private static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

        [DllImport("user32.dll")]
        private static extern bool PrintWindow(IntPtr hWnd, IntPtr hdcBlt, uint nFlags);

        [StructLayout(LayoutKind.Sequential)]
        private struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }

        #endregion

        /// <summary>
        /// 簡化的 Screen 類別 (避免依賴 WinForms)
        /// </summary>
        private static class Screen
        {
            public static ScreenInfo PrimaryScreen => new ScreenInfo
            {
                Bounds = new Rectangle(0, 0, GetSystemMetrics(0), GetSystemMetrics(1))
            };

            [DllImport("user32.dll")]
            private static extern int GetSystemMetrics(int nIndex);
        }

        private class ScreenInfo
        {
            public Rectangle Bounds { get; set; }
        }
    }
}
