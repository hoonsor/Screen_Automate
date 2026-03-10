using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Windows.Forms;

namespace AutoWizard.Core.Actions.Input
{
    public class ScreenshotAction : AutoWizard.Core.Models.BaseAction
    {
        public string SavePath { get; set; } = string.Empty;
        public string SaveToVariable { get; set; } = string.Empty;
        
        // Region properties
        public int RegionX { get; set; } = 0;
        public int RegionY { get; set; } = 0;
        public int RegionWidth { get; set; } = 0;
        public int RegionHeight { get; set; } = 0;
        
        public bool CaptureFull { get; set; } = true;

        public override AutoWizard.Core.Models.ActionResult Execute(AutoWizard.Core.Models.ExecutionContext context)
        {
            try
            {
                // Determine capture bounds
                Rectangle bounds;
                if (CaptureFull)
                {
                    bounds = Screen.PrimaryScreen.Bounds;
                }
                else
                {
                    bounds = new Rectangle(RegionX, RegionY, RegionWidth, RegionHeight);
                }

                using (Bitmap bitmap = new Bitmap(bounds.Width, bounds.Height))
                {
                    using (Graphics g = Graphics.FromImage(bitmap))
                    {
                        g.CopyFromScreen(bounds.Location, Point.Empty, bounds.Size);
                    }

                    // Save to file if path provided
                    if (!string.IsNullOrWhiteSpace(SavePath))
                    {
                        string resolvedPath = context.ResolveExpression(SavePath);
                        // Ensure directory exists
                        string dir = Path.GetDirectoryName(resolvedPath);
                        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                        {
                            Directory.CreateDirectory(dir);
                        }
                        bitmap.Save(resolvedPath, ImageFormat.Png);
                        context.Log($"Screenshot saved to {resolvedPath}");
                    }

                    // Save to variable (Base64) if variable name provided
                    if (!string.IsNullOrWhiteSpace(SaveToVariable))
                    {
                        using (MemoryStream ms = new MemoryStream())
                        {
                            bitmap.Save(ms, ImageFormat.Png);
                            byte[] byteImage = ms.ToArray();
                            string base64 = Convert.ToBase64String(byteImage);
                            context.Variables[SaveToVariable] = base64;
                            context.Log($"Screenshot saved to variable {SaveToVariable} (Base64)");
                        }
                    }
                }

                return new AutoWizard.Core.Models.ActionResult
                {
                    Success = true,
                    Message = "Screenshot captured successfully"
                };
            }
            catch (Exception ex)
            {
                return new AutoWizard.Core.Models.ActionResult
                {
                    Success = false,
                    Message = $"Screenshot failed: {ex.Message}",
                    Exception = ex
                };
            }
        }
    }
}
