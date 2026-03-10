using Xunit;
using AutoWizard.Core.Resources;
using AutoWizard.Core.Models;
using AutoWizard.Core.Actions.Input;
using System.Collections.Generic;
using System.IO;

namespace AutoWizard.Tests.Resources
{
    public class AwsPackageTests
    {
        private string GetTempFilePath() => Path.Combine(Path.GetTempPath(), $"test_{System.Guid.NewGuid()}.aws");

        [Fact]
        public void SaveAndLoad_RoundTrip_ShouldPreserveScriptName()
        {
            // Arrange
            var filePath = GetTempFilePath();
            try
            {
                var package = new AwsPackage
                {
                    ScriptName = "我的測試腳本"
                };

                // Act
                package.Save(filePath);
                var loaded = AwsPackage.Load(filePath);

                // Assert
                Assert.Equal("我的測試腳本", loaded.ScriptName);
            }
            finally
            {
                File.Delete(filePath);
            }
        }

        [Fact]
        public void SaveAndLoad_EmptyActions_ShouldWork()
        {
            var filePath = GetTempFilePath();
            try
            {
                var package = new AwsPackage { ScriptName = "Empty" };
                package.Save(filePath);
                var loaded = AwsPackage.Load(filePath);

                Assert.Empty(loaded.Actions);
            }
            finally
            {
                File.Delete(filePath);
            }
        }

        [Fact]
        public void AddImageResource_ShouldStoreAndRetrieve()
        {
            // Arrange
            var package = new AwsPackage();
            var imageData = new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A };

            // Act
            package.AddImageResource("test.png", imageData);
            var retrieved = package.GetImageResource("test.png");

            // Assert
            Assert.NotNull(retrieved);
            Assert.Equal(imageData, retrieved);
        }

        [Fact]
        public void GetImageResource_NonExistent_ShouldReturnNull()
        {
            var package = new AwsPackage();
            var result = package.GetImageResource("nonexistent.png");
            Assert.Null(result);
        }

        [Fact]
        public void SaveAndLoad_WithImageResources_ShouldPreserveImages()
        {
            var filePath = GetTempFilePath();
            try
            {
                var imageData = new byte[] { 0x01, 0x02, 0x03, 0x04, 0x05 };
                var package = new AwsPackage { ScriptName = "ImageTest" };
                package.AddImageResource("screenshot.png", imageData);

                package.Save(filePath);
                var loaded = AwsPackage.Load(filePath);

                Assert.Single(loaded.ImageResources);
                var loadedImage = loaded.GetImageResource("screenshot.png");
                Assert.NotNull(loadedImage);
                Assert.Equal(imageData, loadedImage);
            }
            finally
            {
                File.Delete(filePath);
            }
        }

        [Fact]
        public void DefaultValues_ShouldBeCorrect()
        {
            var package = new AwsPackage();

            Assert.Equal("未命名腳本", package.ScriptName);
            Assert.Empty(package.Actions);
            Assert.Empty(package.ImageResources);
        }

        [Fact]
        public void SaveAndLoad_WithActions_ShouldPreserveActionTypes()
        {
            var filePath = GetTempFilePath();
            try
            {
                var package = new AwsPackage
                {
                    ScriptName = "ActionTest",
                    Actions = new List<BaseAction>
                    {
                        new ClickAction { Name = "測試點擊", X = 100, Y = 200, Button = MouseButton.Left },
                        new TypeAction { Name = "測試輸入", Text = "Hello" }
                    }
                };

                package.Save(filePath);
                var loaded = AwsPackage.Load(filePath);

                Assert.Equal(2, loaded.Actions.Count);
                Assert.IsType<ClickAction>(loaded.Actions[0]);
                Assert.IsType<TypeAction>(loaded.Actions[1]);

                var click = (ClickAction)loaded.Actions[0];
                Assert.Equal(100, click.X);
                Assert.Equal(200, click.Y);
                Assert.Equal("測試點擊", click.Name);

                var type = (TypeAction)loaded.Actions[1];
                Assert.Equal("Hello", type.Text);
            }
            finally
            {
                File.Delete(filePath);
            }
        }
        [Fact]
        public void SaveAndLoad_WithAllActionTypes_ShouldRoundTrip()
        {
            var filePath = GetTempFilePath();
            try
            {
                var package = new AwsPackage
                {
                    ScriptName = "AllTypesTest",
                    Actions = new List<BaseAction>
                    {
                        new ClickAction { Name = "點擊", X = 100, Y = 200, Button = MouseButton.Left, ClickType = ClickType.Double },
                        new TypeAction { Name = "輸入", Text = "Hello World", Mode = InputMode.Simulate },
                        new KeyboardAction { Name = "按鍵", Key = "ENTER", Modifiers = KeyModifiers.Ctrl },
                        new WaitAction { Name = "等待", DurationMs = 2000, WaitType = WaitType.Fixed },
                        new AutoWizard.Core.Actions.Control.SetVariableAction { Name = "設變數", VariableName = "counter", ValueExpression = "42" },
                        new ScreenshotAction { Name = "截圖", CaptureFull = true, SaveToVariable = "img" }
                    }
                };

                package.Save(filePath);
                var loaded = AwsPackage.Load(filePath);

                Assert.Equal(6, loaded.Actions.Count);
                Assert.IsType<ClickAction>(loaded.Actions[0]);
                Assert.IsType<TypeAction>(loaded.Actions[1]);
                Assert.IsType<KeyboardAction>(loaded.Actions[2]);
                Assert.IsType<WaitAction>(loaded.Actions[3]);
                Assert.IsType<AutoWizard.Core.Actions.Control.SetVariableAction>(loaded.Actions[4]);
                Assert.IsType<ScreenshotAction>(loaded.Actions[5]);

                var kb = (KeyboardAction)loaded.Actions[2];
                Assert.Equal("ENTER", kb.Key);
                Assert.Equal(KeyModifiers.Ctrl, kb.Modifiers);

                var wait = (WaitAction)loaded.Actions[3];
                Assert.Equal(2000, wait.DurationMs);
                Assert.Equal(WaitType.Fixed, wait.WaitType);

                var ss = (ScreenshotAction)loaded.Actions[5];
                Assert.True(ss.CaptureFull);
                Assert.Equal("img", ss.SaveToVariable);
            }
            finally
            {
                File.Delete(filePath);
            }
        }
        [Fact]
        public void Load_OldFormatWithoutTypeDiscriminator_ShouldGiveClearErrorMessage()
        {
            // 模擬舊格式的 .aws 檔案（缺少 $type 辨別碼）
            var filePath = GetTempFilePath();
            try
            {
                using (var archive = System.IO.Compression.ZipFile.Open(filePath, System.IO.Compression.ZipArchiveMode.Create))
                {
                    // 寫入缺少 $type 的 script.json
                    var scriptEntry = archive.CreateEntry("script.json");
                    using (var writer = new StreamWriter(scriptEntry.Open()))
                    {
                        writer.Write("[{\"Name\":\"Test Click\",\"X\":100,\"Y\":200}]");
                    }

                    var metadataEntry = archive.CreateEntry("metadata.json");
                    using (var writer = new StreamWriter(metadataEntry.Open()))
                    {
                        writer.Write("{\"Name\":\"OldScript\",\"Version\":\"1.0\"}");
                    }
                }

                var ex = Assert.Throws<System.InvalidOperationException>(() => AwsPackage.Load(filePath));
                Assert.Contains("舊版格式", ex.Message);
            }
            finally
            {
                File.Delete(filePath);
            }
        }
    }
}
