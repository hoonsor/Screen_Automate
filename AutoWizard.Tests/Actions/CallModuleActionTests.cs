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

            // 只記錄輸入，不真的打字（舊版會把 OverriddenText 打進使用者當下有焦點的輸入框）
            var input = new AutoWizard.Core.Input.RecordingInputSimulator();
            var context = new AutoWizard.Core.Models.ExecutionContext { Input = input };

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
            // TypeAction 的 Text 已被參數覆寫：記錄到的輸入是 OverriddenText，而不是原本的 OriginalText
            Assert.Equal("OverriddenText", input.TypedText);
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
