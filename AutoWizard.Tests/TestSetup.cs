using System.Runtime.CompilerServices;
using AutoWizard.Core.Input;

namespace AutoWizard.Tests
{
    /// <summary>
    /// 測試組件載入時執行：把全域預設輸入換成只記錄的假實作，
    /// 任何測試（包含忘了指定 context.Input 的）都不會真的送出鍵盤或滑鼠輸入。
    /// 真的需要送出輸入的測試必須自己 new Win32InputSimulator，並標上 [Trait("Category", "Interactive")]。
    /// </summary>
    internal static class TestSetup
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            InputSimulator.Default = new RecordingInputSimulator();
        }
    }
}
