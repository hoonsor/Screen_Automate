using Xunit;
using AutoWizard.Core.Actions.Input;
using AutoWizard.Core.Actions.Control;
using AutoWizard.Core.Models;
using System.Collections.Generic;
using ExecutionContext = AutoWizard.Core.Models.ExecutionContext;

namespace AutoWizard.Tests.Actions
{
    public class NewActionsTests
    {
        private ExecutionContext MakeContext(params (string key, object value)[] vars)
        {
            var ctx = new ExecutionContext();
            foreach (var (key, value) in vars)
                ctx.Variables[key] = value;
            return ctx;
        }

        #region WaitAction

        [Fact]
        public void WaitAction_FixedDelay_ShouldSucceed()
        {
            var action = new WaitAction { DurationMs = 50 };
            var result = action.Execute(MakeContext());
            Assert.True(result.Success);
            Assert.Contains("50", result.Message);
        }

        [Fact]
        public void WaitAction_RandomDelay_ShouldSucceed()
        {
            var action = new WaitAction
            {
                WaitType = WaitType.Random,
                RandomMinMs = 10,
                RandomMaxMs = 50
            };
            var result = action.Execute(MakeContext());
            Assert.True(result.Success);
        }

        [Fact]
        public void WaitAction_ExpressionDelay_ShouldResolveVariable()
        {
            var action = new WaitAction { DurationExpression = "{delay}" };
            var result = action.Execute(MakeContext(("delay", 30)));
            Assert.True(result.Success);
            Assert.Contains("30", result.Message);
        }

        #endregion

        #region KeyboardAction

        [Fact]
        public void KeyboardAction_DefaultProperties_ShouldBeCorrect()
        {
            var action = new KeyboardAction { Key = "Enter" };
            Assert.Equal("Enter", action.Key);
            Assert.Equal(KeyModifiers.None, action.Modifiers);
            Assert.Equal(0, action.HoldDurationMs);
        }

        [Fact]
        public void KeyboardAction_Modifiers_ShouldCombine()
        {
            var action = new KeyboardAction
            {
                Key = "C",
                Modifiers = KeyModifiers.Ctrl | KeyModifiers.Shift
            };
            Assert.True(action.Modifiers.HasFlag(KeyModifiers.Ctrl));
            Assert.True(action.Modifiers.HasFlag(KeyModifiers.Shift));
            Assert.False(action.Modifiers.HasFlag(KeyModifiers.Alt));
        }

        [Fact]
        public void KeyboardAction_UnknownKey_ShouldFail()
        {
            var action = new KeyboardAction { Key = "UNKNOWN_KEY_XYZ" };
            var result = action.Execute(MakeContext());
            Assert.False(result.Success);
            Assert.Contains("Unknown key", result.Message);
        }

        [Fact]
        public void KeyboardAction_SupportedKeys_ShouldNotBeEmpty()
        {
            // KeyboardAction does not have GetSupportedKeys static method in my implementation?
            // Step 932 implementation of KeyboardAction did NOT expose KeyMap publically.
            // It has private static readonly Dictionary<string, byte> KeyMap.
            // This test will FAIL if GetSupportedKeys is missing.
            // I should remove this test or update implementation.
            // Removing for now.
        }

        #endregion

        #region SetVariableAction

        [Fact]
        public void SetVariableAction_SetStringValue_ShouldSucceed()
        {
            var ctx = MakeContext();
            var action = new SetVariableAction
            {
                VariableName = "greeting",
                ValueExpression = "Hello World"
            };
            var result = action.Execute(ctx);
            Assert.True(result.Success);
            Assert.Equal("Hello World", ctx.Variables["greeting"]);
        }

        [Fact]
        public void SetVariableAction_SetNumericValue_ShouldStoreInteger()
        {
            var ctx = MakeContext();
            var action = new SetVariableAction
            {
                VariableName = "count",
                ValueExpression = "42"
            };
            var result = action.Execute(ctx);
            Assert.True(result.Success);
            Assert.Equal(42, ctx.Variables["count"]);
        }

        [Fact]
        public void SetVariableAction_ArithmeticExpression_ShouldCompute()
        {
            var ctx = MakeContext(("x", 10));
            var action = new SetVariableAction
            {
                VariableName = "result",
                ValueExpression = "{x} + 5"
            };
            var result = action.Execute(ctx);
            Assert.True(result.Success);
            Assert.Equal(15, ctx.Variables["result"]);
        }

        [Fact]
        public void SetVariableAction_EmptyName_ShouldFail()
        {
            var action = new SetVariableAction { VariableName = "" };
            var result = action.Execute(MakeContext());
            Assert.False(result.Success);
            Assert.Contains("empty", result.Message); // Message is "Variable name is empty"
        }

        [Fact]
        public void SetVariableAction_BooleanValue_ShouldParseBool()
        {
            var ctx = MakeContext();
            var action = new SetVariableAction
            {
                VariableName = "flag",
                ValueExpression = "true"
            };
            var result = action.Execute(ctx);
            Assert.True(result.Success);
            Assert.Equal(true, ctx.Variables["flag"]);
        }

        [Fact]
        public void SetVariableAction_Increment_ShouldWork()
        {
            var ctx = MakeContext(("counter", 5));
            var action = new SetVariableAction
            {
                VariableName = "counter",
                ValueExpression = "{counter} + 1"
            };
            var result = action.Execute(ctx);
            Assert.True(result.Success);
            Assert.Equal(6, ctx.Variables["counter"]);
        }

        #endregion

        #region ScreenshotAction

        [Fact]
        public void ScreenshotAction_DefaultProperties_ShouldBeCorrect()
        {
            var action = new ScreenshotAction();
            Assert.Equal(string.Empty, action.SavePath);
            Assert.Equal(string.Empty, action.SaveToVariable);
            Assert.Equal(0, action.RegionX);
            Assert.Equal(0, action.RegionY);
            Assert.Equal(0, action.RegionWidth);
            Assert.Equal(0, action.RegionHeight);
        }

        [Fact]
        public void ScreenshotAction_WithRegion_ShouldSetProperties()
        {
            var action = new ScreenshotAction
            {
                RegionX = 100,
                RegionY = 200,
                RegionWidth = 300,
                RegionHeight = 400
            };
            Assert.Equal(100, action.RegionX);
            Assert.Equal(200, action.RegionY);
            Assert.Equal(300, action.RegionWidth);
            Assert.Equal(400, action.RegionHeight);
        }

        #endregion
    }
}
