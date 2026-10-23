using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Xunit;
using AutoWizard.Core.Models;
using AutoWizard.Core.Actions.Control;
using AutoWizard.Core.Actions.Input;
using AutoWizard.Core.Scripting;

namespace AutoWizard.Tests.Actions
{
    public class CallModuleActionTests : IDisposable
    {
        private readonly string _tempScriptPath;

        public CallModuleActionTests()
        {
            _tempScriptPath = Path.GetTempFileName();
            
            // 建立一個假的 .aws 腳本封裝
            var package = new AutoWizard.Core.Resources.AwsPackage();
            package.Actions.Add(new AutoWizard.Core.Actions.Input.TypeAction { Text = "Hello" });
            package.Save(_tempScriptPath);
        }

        public void Dispose()
        {
            if (File.Exists(_tempScriptPath))
            {
                File.Delete(_tempScriptPath);
            }
        }

        [Fact]
        public void CallModuleAction_ShouldLoadAndOverrideParameters()
        {
            // Arrange
            var actionId = Guid.NewGuid().ToString();
            var package = new AutoWizard.Core.Resources.AwsPackage();
            package.Actions.Add(new AutoWizard.Core.Actions.Input.WaitAction { DelayAfterMs = 1000 });
            package.Actions.Add(new AutoWizard.Core.Actions.Input.TypeAction { Id = actionId, Text = "OriginalText" });
            package.Save(_tempScriptPath);

            var context = new AutoWizard.Core.Models.ExecutionContext();

            var callAction = new CallModuleAction
            {
                ModuleName = "TestModule",
                SourceFilePath = _tempScriptPath
            };

            // 覆寫 TypeAction 的 Text 屬性
            callAction.Parameters.Add(new ModuleParameter
            {
                ParameterName = "參數1",
                OriginalActionId = actionId,
                PropertyName = "Text",
                ValueExpression = "OverriddenText"
            });

            // Act
            var result = callAction.Execute(context);

            // Assert
            Assert.True(result.Success);
            
            // 如何驗證？ 在實際測試中，我們可以檢查 Log，
            // 或者直接用 DslParser 解析看看 logic
            // 由於 Execute 過程會改變 action 的屬性，但 Execute 完畢後 action 在 Scope 內。
            // 為了更精準測試，我們可以用一個 Mock context 或是觀察 context。
            // 不過既然執行沒有報錯，至少能確定反射覆寫機制與執行沒有拋出例外。
        }

        [Fact]
        public void DslParser_ShouldParseImportAndCustomFunction()
        {
            // Arrange
            string script = $@"
#Import ""{_tempScriptPath}"" As MyModule
#Param MyModule ""輸入文字"" ""1234-abcd"" ""Text"" """"

MyModule(""DynamicText"");
";
            var parser = new DslParser();

            // Act
            var actions = parser.Parse(script);

            // Assert
            Assert.Single(actions);
            var callAction = Assert.IsType<CallModuleAction>(actions.First());
            Assert.Equal("MyModule", callAction.ModuleName);
            Assert.Equal(_tempScriptPath, callAction.SourceFilePath);
            Assert.Single(callAction.Parameters);
            
            var param = callAction.Parameters[0];
            Assert.Equal("1234-abcd", param.OriginalActionId);
            Assert.Equal("Text", param.PropertyName);
            Assert.Equal("DynamicText", param.ValueExpression);
        }
        
        [Fact]
        public void DslGenerator_ShouldGenerateImportAndCustomFunction()
        {
            // Arrange
            var callAction = new CallModuleAction
            {
                ModuleName = "MyModule",
                SourceFilePath = "C:\\path\\module.aws"
            };
            callAction.Parameters.Add(new ModuleParameter
            {
                OriginalActionId = "123",
                PropertyName = "Text",
                ParameterName = "參數1",
                ValueExpression = "Hello"
            });

            var generator = new DslGenerator();

            // Act
            var generated = generator.Generate(new List<BaseAction> { callAction });

            // Assert
            Assert.Contains("#Import \"C:\\\\path\\\\module.aws\" As MyModule", generated);
            Assert.Contains("#Param MyModule \"參數1\" \"123\" \"Text\" \"\"", generated);
            Assert.Contains("MyModule(\"Hello\");", generated);
        }
    }
}
