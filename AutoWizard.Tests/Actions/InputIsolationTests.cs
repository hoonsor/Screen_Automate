using System.Reflection;
using System.Runtime.InteropServices;
using AutoWizard.Core.Actions.Input;
using AutoWizard.Core.Engine;
using AutoWizard.Core.Input;
using AutoWizard.Core.Models;
using ExecutionContext = AutoWizard.Core.Models.ExecutionContext;

namespace AutoWizard.Tests.Actions
{
    /// <summary>確認測試不會送出真實輸入，並驗證各動作送出的內容。</summary>
    public class InputIsolationTests
    {
        [Fact]
        public void TestDefault_IsRecordingSimulator_NotRealInput()
        {
            Assert.IsType<RecordingInputSimulator>(InputSimulator.Default);
            Assert.IsType<RecordingInputSimulator>(new ExecutionContext().Input);
        }

        [Fact]
        public void OnlyWin32InputSimulator_DeclaresSendInput()
        {
            // 防止日後有動作又直接 P/Invoke SendInput（繞過可替換的輸入介面）
            var offenders = typeof(ClickAction).Assembly.GetTypes()
                .SelectMany(t => t.GetMethods(BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.DeclaredOnly)
                    .Where(m => m.GetCustomAttribute<DllImportAttribute>() != null)
                    .Where(m => m.Name is "SendInput" or "keybd_event" or "mouse_event" or "SetCursorPos")
                    .Select(m => $"{t.FullName}.{m.Name}"))
                .Where(name => !name.StartsWith(typeof(Win32InputSimulator).FullName + "."))
                .ToList();
            Assert.Empty(offenders);
        }

        [Fact]
        public void TypeAction_SendsCharactersToSimulator()
        {
            var input = new RecordingInputSimulator();
            var ctx = new ExecutionContext { Input = input };
            ctx.Variables["name"] = "人事";
            var result = new TypeAction { Text = "Hi {name}", IntervalMinMs = 0, IntervalMaxMs = 1 }.Execute(ctx);
            Assert.True(result.Success);
            Assert.Equal("Hi 人事", input.TypedText);
        }

        [Fact]
        public void KeyboardAction_CtrlEnter_PressesThenReleasesInReverseOrder()
        {
            var input = new RecordingInputSimulator();
            var result = new KeyboardAction { Key = "ENTER", Modifiers = KeyModifiers.Ctrl }.Execute(new ExecutionContext { Input = input });
            Assert.True(result.Success);
            Assert.Equal(new[] { "key 0xA2 down", "key 0x0D down", "key 0x0D up", "key 0xA2 up" }, input.Events);
        }

        [Fact]
        public void KeyboardAction_KeyDownOnly_DoesNotRelease()
        {
            var input = new RecordingInputSimulator();
            new KeyboardAction { Key = "A", Modifiers = KeyModifiers.Shift, IsKeyDown = true }.Execute(new ExecutionContext { Input = input });
            Assert.All(input.KeyStrokes, k => Assert.False(k.KeyUp));
        }

        [Fact]
        public void ClickAction_DoubleRightClick_MovesThenClicksTwice()
        {
            var input = new RecordingInputSimulator();
            var ctx = new ExecutionContext { Input = input };
            var result = new ClickAction { X = 120, Y = 340, Button = MouseButton.Right, ClickType = ClickType.Double }.Execute(ctx);
            Assert.True(result.Success);
            Assert.Equal(new[]
            {
                "move 120,340",
                "button Right down 120,340", "button Right up 120,340",
                "button Right down 120,340", "button Right up 120,340",
            }, input.Events);
        }

        [Fact]
        public void ClickAction_HumanLike_EndsAtTarget()
        {
            var input = new RecordingInputSimulator();
            input.MoveMouse(0, 0);
            new ClickAction { X = 500, Y = 400, IsHumanLike = true, HumanLikeDurationMs = 100 }.Execute(new ExecutionContext { Input = input });
            var moves = input.Events.Where(e => e.StartsWith("move ")).ToList();
            Assert.True(moves.Count > 5);
            Assert.Equal("move 500,400", moves[^1]);
        }

        [Fact]
        public async Task ScriptExecutor_UsesConfiguredSimulator()
        {
            var input = new RecordingInputSimulator();
            var executor = new ScriptExecutor { Input = input, ActionIntervalMs = 0 };
            await executor.ExecuteAsync(new List<BaseAction> { new TypeAction { Text = "ok", IntervalMinMs = 0, IntervalMaxMs = 1 } });
            Assert.Equal("ok", input.TypedText);
        }

        /// <summary>
        /// 真的送出輸入的測試：用 SendInput 送出「未指派」的虛擬鍵碼 0xE8（Windows 未定義用途，一般程式會忽略），
        /// 確認真實實作能呼叫成功。不用 F13–F24：巨集鍵盤或 AutoHotkey 常綁定這些鍵。
        /// 不做滑鼠移動：測試程序沒有 DPI 感知，在高縮放螢幕上曾把游標移走 528 像素。
        /// 標記 Interactive，平常與 CI 以 --filter "Category!=Interactive" 排除。
        /// </summary>
        [Fact]
        [Trait("Category", "Interactive")]
        public void Win32Simulator_SendsUnassignedKey_WithoutError()
        {
            var real = new Win32InputSimulator();
            real.SendKeys(new[] { new KeyStroke(0xE8, false), new KeyStroke(0xE8, true) });
        }
    }
}
