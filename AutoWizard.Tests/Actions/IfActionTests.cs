using Xunit;
using AutoWizard.Core.Actions.Control;
using AutoWizard.Core.Models;
using System.Collections.Generic;
using ExecutionContext = AutoWizard.Core.Models.ExecutionContext;

namespace AutoWizard.Tests.Actions
{
    /// <summary>
    /// Mock Action 用於測試控制流
    /// </summary>
    public class MockAction : BaseAction
    {
        public bool WasExecuted { get; set; } = false;
        public bool ShouldSucceed { get; set; } = true;

        public override ActionResult Execute(ExecutionContext context)
        {
            WasExecuted = true;
            context.Log($"MockAction '{Name}' executed");
            return new ActionResult
            {
                Success = ShouldSucceed,
                Message = ShouldSucceed ? "OK" : "Failed"
            };
        }
    }

    public class IfActionTests
    {
        private ExecutionContext CreateContext(Dictionary<string, object>? variables = null)
        {
            return new ExecutionContext
            {
                Variables = variables ?? new Dictionary<string, object>(),
                IsCancellationRequested = false,
                LogAction = _ => { }
            };
        }

        [Fact]
        public void Execute_VariableEquals_ConditionTrue_ShouldExecuteThenActions()
        {
            // Arrange
            var thenAction = new MockAction { Name = "Then" };
            var elseAction = new MockAction { Name = "Else" };
            var ifAction = new IfAction
            {
                Name = "TestIf",
                ConditionType = ConditionType.VariableEquals,
                LeftOperand = "{myVar}",
                RightOperand = "hello",
                ThenActions = new List<BaseAction> { thenAction },
                ElseActions = new List<BaseAction> { elseAction }
            };

            var context = CreateContext(new Dictionary<string, object>
            {
                { "myVar", "hello" }
            });

            // Act
            var result = ifAction.Execute(context);

            // Assert
            Assert.True(result.Success);
            Assert.True(thenAction.WasExecuted);
            Assert.False(elseAction.WasExecuted);
        }

        [Fact]
        public void Execute_VariableEquals_ConditionFalse_ShouldExecuteElseActions()
        {
            var thenAction = new MockAction { Name = "Then" };
            var elseAction = new MockAction { Name = "Else" };
            var ifAction = new IfAction
            {
                Name = "TestIf",
                ConditionType = ConditionType.VariableEquals,
                LeftOperand = "{myVar}",
                RightOperand = "hello",
                ThenActions = new List<BaseAction> { thenAction },
                ElseActions = new List<BaseAction> { elseAction }
            };

            var context = CreateContext(new Dictionary<string, object>
            {
                { "myVar", "world" }
            });

            var result = ifAction.Execute(context);

            Assert.True(result.Success);
            Assert.False(thenAction.WasExecuted);
            Assert.True(elseAction.WasExecuted);
        }

        [Fact]
        public void Execute_VariableNotSet_ShouldExecuteElse()
        {
            var thenAction = new MockAction { Name = "Then" };
            var elseAction = new MockAction { Name = "Else" };
            var ifAction = new IfAction
            {
                Name = "TestIf",
                ConditionType = ConditionType.VariableEquals,
                LeftOperand = "{nonexistent}",
                RightOperand = "hello",
                ThenActions = new List<BaseAction> { thenAction },
                ElseActions = new List<BaseAction> { elseAction }
            };

            var context = CreateContext();

            var result = ifAction.Execute(context);

            Assert.True(result.Success);
            Assert.False(thenAction.WasExecuted);
            Assert.True(elseAction.WasExecuted);
        }

        [Fact]
        public void Execute_FileExistsCondition_WithExistingFile_ShouldExecuteThen()
        {
            var tempFile = System.IO.Path.GetTempFileName();
            try
            {
                var thenAction = new MockAction { Name = "Then" };
                var elseAction = new MockAction { Name = "Else" };
                var ifAction = new IfAction
                {
                    Name = "FileCheck",
                    ConditionType = ConditionType.FileExists,
                    LeftOperand = tempFile,
                    RightOperand = "",
                    ThenActions = new List<BaseAction> { thenAction },
                    ElseActions = new List<BaseAction> { elseAction }
                };

                var context = CreateContext();

                var result = ifAction.Execute(context);

                Assert.True(result.Success);
                Assert.True(thenAction.WasExecuted);
                Assert.False(elseAction.WasExecuted);
            }
            finally
            {
                System.IO.File.Delete(tempFile);
            }
        }

        [Fact]
        public void Execute_EmptyElseActions_ShouldNotThrow()
        {
            var thenAction = new MockAction { Name = "Then" };
            var ifAction = new IfAction
            {
                Name = "NoElse",
                ConditionType = ConditionType.VariableEquals,
                LeftOperand = "{flag}",
                RightOperand = "true",
                ThenActions = new List<BaseAction> { thenAction },
                ElseActions = new List<BaseAction>()
            };

            // flag 不存在 → 條件 false → 走 else (空)
            var context = CreateContext();

            var result = ifAction.Execute(context);
            Assert.True(result.Success);
            Assert.False(thenAction.WasExecuted);
        }
    }
}
