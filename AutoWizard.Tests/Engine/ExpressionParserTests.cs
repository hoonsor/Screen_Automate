using Xunit;
using AutoWizard.Core.Engine;
using System.Collections.Generic;

namespace AutoWizard.Tests.Engine
{
    public class ExpressionParserTests
    {
        private Dictionary<string, object> MakeVars(params (string key, object value)[] pairs)
        {
            var dict = new Dictionary<string, object>();
            foreach (var (key, value) in pairs)
                dict[key] = value;
            return dict;
        }

        #region Resolve — 變數替換

        [Fact]
        public void Resolve_SingleVariable_ShouldReplace()
        {
            var vars = MakeVars(("name", "World"));
            Assert.Equal("Hello World", ExpressionParser.Resolve("Hello {name}", vars));
        }

        [Fact]
        public void Resolve_MultipleVariables_ShouldReplaceAll()
        {
            var vars = MakeVars(("a", "X"), ("b", "Y"));
            Assert.Equal("X and Y", ExpressionParser.Resolve("{a} and {b}", vars));
        }

        [Fact]
        public void Resolve_UnknownVariable_ShouldKeepOriginal()
        {
            var vars = MakeVars(("name", "World"));
            Assert.Equal("Hello {unknown}", ExpressionParser.Resolve("Hello {unknown}", vars));
        }

        [Fact]
        public void Resolve_NullOrEmpty_ShouldReturnSafely()
        {
            Assert.Equal("", ExpressionParser.Resolve(null!, new Dictionary<string, object>()));
            Assert.Equal("", ExpressionParser.Resolve("", new Dictionary<string, object>()));
        }

        [Fact]
        public void Resolve_NoVariables_ShouldReturnOriginal()
        {
            Assert.Equal("plain text", ExpressionParser.Resolve("plain text", new Dictionary<string, object>()));
        }

        #endregion

        #region Evaluate — 算術運算

        [Theory]
        [InlineData("3 + 5", 8.0)]
        [InlineData("10 - 3", 7.0)]
        [InlineData("4 * 7", 28.0)]
        [InlineData("15 / 3", 5.0)]
        [InlineData("10 % 3", 1.0)]
        public void Evaluate_Arithmetic_ShouldCompute(string expr, double expected)
        {
            var result = ExpressionParser.Evaluate(expr, new Dictionary<string, object>());
            Assert.Equal(expected, result);
        }

        [Fact]
        public void Evaluate_WithVariables_ShouldSubstituteAndCompute()
        {
            var vars = MakeVars(("count", 10));
            Assert.Equal(11.0, ExpressionParser.Evaluate("{count} + 1", vars));
        }

        [Fact]
        public void Evaluate_DivisionByZero_ShouldReturnZero()
        {
            Assert.Equal(0.0, ExpressionParser.Evaluate("5 / 0", new Dictionary<string, object>()));
        }

        #endregion

        #region ResolveInt / ResolveDouble

        [Fact]
        public void ResolveInt_ShouldReturnInteger()
        {
            var vars = MakeVars(("x", 42));
            Assert.Equal(42, ExpressionParser.ResolveInt("{x}", vars));
        }

        [Fact]
        public void ResolveInt_InvalidValue_ShouldReturnFallback()
        {
            Assert.Equal(99, ExpressionParser.ResolveInt("abc", new Dictionary<string, object>(), 99));
        }

        [Fact]
        public void ResolveDouble_ShouldReturnDouble()
        {
            var vars = MakeVars(("pi", 3.14));
            Assert.Equal(3.14, ExpressionParser.ResolveDouble("{pi}", vars));
        }

        #endregion

        #region EvaluateCondition — 布林條件

        [Theory]
        [InlineData("5 == 5", true)]
        [InlineData("5 == 3", false)]
        [InlineData("5 != 3", true)]
        [InlineData("10 > 5", true)]
        [InlineData("3 > 5", false)]
        [InlineData("3 < 5", true)]
        [InlineData("5 >= 5", true)]
        [InlineData("5 <= 5", true)]
        [InlineData("4 >= 5", false)]
        public void EvaluateCondition_NumericComparisons(string expr, bool expected)
        {
            Assert.Equal(expected, ExpressionParser.EvaluateCondition(expr, new Dictionary<string, object>()));
        }

        [Fact]
        public void EvaluateCondition_StringEquals()
        {
            var vars = MakeVars(("name", "admin"));
            Assert.True(ExpressionParser.EvaluateCondition("{name} == admin", vars));
        }

        [Fact]
        public void EvaluateCondition_Contains()
        {
            var vars = MakeVars(("text", "Hello World"));
            Assert.True(ExpressionParser.EvaluateCondition("{text} contains World", vars));
        }

        [Fact]
        public void EvaluateCondition_Empty_ShouldReturnFalse()
        {
            Assert.False(ExpressionParser.EvaluateCondition("", new Dictionary<string, object>()));
        }

        [Fact]
        public void EvaluateCondition_BooleanLiteral()
        {
            Assert.True(ExpressionParser.EvaluateCondition("true", new Dictionary<string, object>()));
            Assert.False(ExpressionParser.EvaluateCondition("false", new Dictionary<string, object>()));
        }

        [Fact]
        public void EvaluateCondition_WithVariableArithmetic()
        {
            var vars = MakeVars(("count", 5));
            // {count} resolves to 5, so "5 > 0" → true
            Assert.True(ExpressionParser.EvaluateCondition("{count} > 0", vars));
        }

        #endregion
    }
}
