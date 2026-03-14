using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Interop;

namespace AutoWizard.UI.Views
{
    public partial class SmartCaptureOverlay : Window
    {
        private Point _startPoint;
        private bool _isDragging;
        private System.Drawing.Bitmap? _originalBitmap;
        
        public Rect SelectedRegion { get; private set; } = Rect.Empty;
        public System.Drawing.Rectangle SelectedRectPhysical { get; private set; } = System.Drawing.Rectangle.Empty;
        public System.Drawing.Color PickedColor { get; private set; } = System.Drawing.Color.Empty;
        public Point PickedPoint { get; private set; } = new Point(-1, -1);
        public bool IsConfirmed { get; private set; } = false;
        public bool IsColorPickerMode { get; set; } = false;
        
        // Measure Tool properties
        public bool IsMeasureMode { get; set; } = false;
        private int _measureClicks = 0;
        public Point MeasureStartPoint { get; private set; }
        public Point MeasureEndPoint { get; private set; }

        public SmartCaptureOverlay(System.Drawing.Bitmap bitmap)
        {
            InitializeComponent();
            _originalBitmap = bitmap;
            BackgroundImage.Source = Imaging.CreateBitmapSourceFromHBitmap(
                bitmap.GetHbitmap(),
                IntPtr.Zero,
                Int32Rect.Empty,
                BitmapSizeOptions.FromEmptyOptions());
            
            Loaded += (s, e) =>
            {
                UpdateDimmingMask(Rect.Empty);
                // Apply color picker UI on load if mode was pre-set
                if (IsColorPickerMode)
                {
                    MagnifierPanel.Visibility = Visibility.Visible;
                    HintText.Text = "點擊滑鼠左鍵選取顏色 (按 Esc 取消)";
                    DimmingLayer.Fill = new SolidColorBrush(Color.FromArgb(0x60, 0, 0, 0));
                }
                else if (IsMeasureMode)
                {
                    HintText.Text = "點擊第一點開始量測，點擊第二點完成 (按 Esc 取消)";
                    DimmingLayer.Fill = new SolidColorBrush(Color.FromArgb(0x40, 0, 0, 0));
                }
            };
            
            KeyDown += (s, e) =>
            {
                if (e.Key == Key.Escape)
                {
                    Close();
                }
            };
        }

        /// <summary>
        /// 切換為取色器模式 (在 ShowDialog 前呼叫)
        /// </summary>
        public void SetColorPickerMode()
        {
            IsColorPickerMode = true;
        }

        private void UpdateDimmingMask(Rect selection)
        {
            if (DimmingLayer == null) return;

            var screenRect = new Rect(0, 0, ActualWidth, ActualHeight);
            var screenGeo = new RectangleGeometry(screenRect);
            
            Geometry geometry;
            if (selection.IsEmpty || selection.Width == 0 || selection.Height == 0)
            {
                geometry = screenGeo;
            }
            else
            {
                var selectGeo = new RectangleGeometry(selection);
                geometry = new CombinedGeometry(GeometryCombineMode.Exclude, screenGeo, selectGeo);
            }
            
            DimmingLayer.Data = geometry;
        }

        private void OnMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                _startPoint = e.GetPosition(SelectionCanvas);

                if (IsColorPickerMode)
                {
                    // Color Picker Mode: Pick pixel and close
                    PickedColor = GetColorAt(_startPoint);
                    PickedPoint = new Point((int)(_startPoint.X * _dpiScaleX), (int)(_startPoint.Y * _dpiScaleY));
                    IsConfirmed = true;
                    Close();
                    return;
                }

                if (IsMeasureMode)
                {
                    if (_measureClicks == 0)
                    {
                        MeasureStartPoint = _startPoint;
                        _measureClicks++;
                        
                        MeasureLine.X1 = _startPoint.X;
                        MeasureLine.Y1 = _startPoint.Y;
                        MeasureLine.X2 = _startPoint.X;
                        MeasureLine.Y2 = _startPoint.Y;
                        MeasureLine.Visibility = Visibility.Visible;
                        
                        MeasureTextBorder.Visibility = Visibility.Visible;
                        Canvas.SetLeft(MeasureTextBorder, _startPoint.X + 15);
                        Canvas.SetTop(MeasureTextBorder, _startPoint.Y + 15);
                        MeasureText.Text = "dX: 0, dY: 0\nDist: 0.0 px";
                    }
                    else if (_measureClicks == 1)
                    {
                        MeasureEndPoint = _startPoint;
                        IsConfirmed = true;
                        Close();
                    }
                    return;
                }

                // Region Capture Mode
                _isDragging = true;
                
                Canvas.SetLeft(SelectionRect, _startPoint.X);
                Canvas.SetTop(SelectionRect, _startPoint.Y);
                SelectionRect.Width = 0;
                SelectionRect.Height = 0;
                SelectionRect.Visibility = Visibility.Visible;
                
                UpdateDimmingMask(Rect.Empty); // Reset dimming

                SelectionCanvas.CaptureMouse();
            }
        }

        private double _dpiScaleX = 1.0;
        private double _dpiScaleY = 1.0;

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            var source = PresentationSource.FromVisual(this);
            if (source?.CompositionTarget != null)
            {
                _dpiScaleX = source.CompositionTarget.TransformToDevice.M11;
                _dpiScaleY = source.CompositionTarget.TransformToDevice.M22;
            }
        }

        private System.Drawing.Color GetColorAt(Point p)
        {
            if (_originalBitmap == null) return System.Drawing.Color.Black;

            int x = (int)(p.X * _dpiScaleX);
            int y = (int)(p.Y * _dpiScaleY);

            if (x < 0) x = 0;
            if (y < 0) y = 0;
            if (x >= _originalBitmap.Width) x = _originalBitmap.Width - 1;
            if (y >= _originalBitmap.Height) y = _originalBitmap.Height - 1;

            return _originalBitmap.GetPixel(x, y);
        }

        private void OnMouseMove(object sender, MouseEventArgs e)
        {
            if (IsColorPickerMode)
            {
                // Update magnifier
                var pos = e.GetPosition(SelectionCanvas);
                UpdateMagnifier(pos);
                
                // Move the magnifier panel with offset to avoid covering cursor
                double panelX = pos.X + 20;
                double panelY = pos.Y + 20;
                // Clamp to screen
                if (panelX + 138 > ActualWidth) panelX = pos.X - 158;
                if (panelY + 170 > ActualHeight) panelY = pos.Y - 190;
                Canvas.SetLeft(MagnifierPanel, panelX);
                Canvas.SetTop(MagnifierPanel, panelY);
                return;
            }

            if (IsMeasureMode && _measureClicks == 1)
            {
                var pos = e.GetPosition(SelectionCanvas);
                MeasureLine.X2 = pos.X;
                MeasureLine.Y2 = pos.Y;
                
                // Calculate physical pixels
                int startPx = (int)(MeasureStartPoint.X * _dpiScaleX);
                int startPy = (int)(MeasureStartPoint.Y * _dpiScaleY);
                int currentPx = (int)(pos.X * _dpiScaleX);
                int currentPy = (int)(pos.Y * _dpiScaleY);
                
                int dx = currentPx - startPx;
                int dy = currentPy - startPy;
                double dist = Math.Sqrt(dx * dx + dy * dy);
                
                MeasureText.Text = $"dX: {Math.Abs(dx)} px\ndY: {Math.Abs(dy)} px\nDist: {dist:F1} px";
                
                Canvas.SetLeft(MeasureTextBorder, pos.X + 15);
                Canvas.SetTop(MeasureTextBorder, pos.Y + 15);
                return;
            }

            if (_isDragging)
            {
                var currentPoint = e.GetPosition(SelectionCanvas);
                
                var x = Math.Min(currentPoint.X, _startPoint.X);
                var y = Math.Min(currentPoint.Y, _startPoint.Y);
                var width = Math.Abs(currentPoint.X - _startPoint.X);
                var height = Math.Abs(currentPoint.Y - _startPoint.Y);

                Canvas.SetLeft(SelectionRect, x);
                Canvas.SetTop(SelectionRect, y);
                SelectionRect.Width = width;
                SelectionRect.Height = height;

                UpdateDimmingMask(new Rect(x, y, width, height));
            }
        }

        private void UpdateMagnifier(Point pos)
        {
            if (_originalBitmap == null) return;

            const int zoom = 8;   // Each pixel shown as 8x8 block
            const int halfPixels = 8; // Show 8 pixels around center (15x15 pixel area)

            int centerX = (int)(pos.X * _dpiScaleX);
            int centerY = (int)(pos.Y * _dpiScaleY);

            // Region to magnify
            int srcW = halfPixels * 2 + 1;
            int srcH = halfPixels * 2 + 1;
            int dstW = srcW * zoom;
            int dstH = srcH * zoom;

            var magnified = new System.Drawing.Bitmap(dstW, dstH);
            using (var g = System.Drawing.Graphics.FromImage(magnified))
            {
                g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor;
                g.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.Half;

                for (int py = 0; py < srcH; py++)
                {
                    for (int px = 0; px < srcW; px++)
                    {
                        int bx = Math.Max(0, Math.Min(_originalBitmap.Width - 1, centerX - halfPixels + px));
                        int by = Math.Max(0, Math.Min(_originalBitmap.Height - 1, centerY - halfPixels + py));
                        var col = _originalBitmap.GetPixel(bx, by);
                        using var brush = new System.Drawing.SolidBrush(col);
                        g.FillRectangle(brush, px * zoom, py * zoom, zoom, zoom);
                    }
                }
                // Draw thin gridlines
                using var pen = new System.Drawing.Pen(System.Drawing.Color.FromArgb(40, 0, 0, 0), 1);
                for (int i = 0; i <= srcW; i++)
                    g.DrawLine(pen, i * zoom, 0, i * zoom, dstH);
                for (int i = 0; i <= srcH; i++)
                    g.DrawLine(pen, 0, i * zoom, dstW, i * zoom);
            }

            MagnifierImage.Source = BitmapToImageSource(magnified);

            // Update color swatch and label for center pixel
            int cx = Math.Max(0, Math.Min(_originalBitmap.Width - 1, centerX));
            int cy = Math.Max(0, Math.Min(_originalBitmap.Height - 1, centerY));
            var centerColor = _originalBitmap.GetPixel(cx, cy);
            var hex = $"#{centerColor.R:X2}{centerColor.G:X2}{centerColor.B:X2}";
            ColorHexLabel.Text = hex;
            ColorSwatch.Background = new SolidColorBrush(Color.FromRgb(centerColor.R, centerColor.G, centerColor.B));
        }

        private static System.Windows.Media.Imaging.BitmapSource BitmapToImageSource(System.Drawing.Bitmap bitmap)
        {
            var hBitmap = bitmap.GetHbitmap();
            var source = System.Windows.Interop.Imaging.CreateBitmapSourceFromHBitmap(
                hBitmap, IntPtr.Zero, Int32Rect.Empty,
                System.Windows.Media.Imaging.BitmapSizeOptions.FromEmptyOptions());
            DeleteObject(hBitmap);
            return source;
        }

        [System.Runtime.InteropServices.DllImport("gdi32.dll")]
        private static extern bool DeleteObject(IntPtr hObject);

        private void OnMouseUp(object sender, MouseButtonEventArgs e)
        {
            if (_isDragging)
            {
                _isDragging = false;
                SelectionCanvas.ReleaseMouseCapture();

                var currentPoint = e.GetPosition(SelectionCanvas);
                var x = Math.Min(currentPoint.X, _startPoint.X);
                var y = Math.Min(currentPoint.Y, _startPoint.Y);
                var width = Math.Abs(currentPoint.X - _startPoint.X);
                var height = Math.Abs(currentPoint.Y - _startPoint.Y);

                if (width > 5 && height > 5)
                {
                    SelectedRegion = new Rect(x, y, width, height);

                    // Ensure physical bounds do not exceed original bitmap
                    int physX = Math.Max(0, (int)(x * _dpiScaleX));
                    int physY = Math.Max(0, (int)(y * _dpiScaleY));
                    int physW = Math.Min(_originalBitmap?.Width - physX ?? int.MaxValue, (int)(width * _dpiScaleX));
                    int physH = Math.Min(_originalBitmap?.Height - physY ?? int.MaxValue, (int)(height * _dpiScaleY));

                    SelectedRectPhysical = new System.Drawing.Rectangle(physX, physY, physW, physH);
                    
                    IsConfirmed = true;
                    Close();
                }
                else
                {
                    // Too small, reset
                    SelectionRect.Visibility = Visibility.Collapsed;
                }
            }
        }
    }
}
