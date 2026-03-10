using Xunit;
using AutoWizard.Core.Models;

namespace AutoWizard.Tests.Models
{
    public class VariableDefinitionTests
    {
        [Fact]
        public void GetTypedDefaultValue_String_ShouldReturnString()
        {
            var v = new VariableDefinition { Type = VariableType.String, DefaultValue = "hello" };
            var result = v.GetTypedDefaultValue();
            Assert.IsType<string>(result);
            Assert.Equal("hello", result);
        }

        [Fact]
        public void GetTypedDefaultValue_Integer_ShouldReturnInt()
        {
            var v = new VariableDefinition { Type = VariableType.Integer, DefaultValue = "42" };
            var result = v.GetTypedDefaultValue();
            Assert.IsType<int>(result);
            Assert.Equal(42, result);
        }

        [Fact]
        public void GetTypedDefaultValue_Integer_InvalidValue_ShouldReturnZero()
        {
            var v = new VariableDefinition { Type = VariableType.Integer, DefaultValue = "abc" };
            var result = v.GetTypedDefaultValue();
            Assert.IsType<int>(result);
            Assert.Equal(0, result);
        }

        [Fact]
        public void GetTypedDefaultValue_Double_ShouldReturnDouble()
        {
            var v = new VariableDefinition { Type = VariableType.Double, DefaultValue = "3.14" };
            var result = v.GetTypedDefaultValue();
            Assert.IsType<double>(result);
            Assert.Equal(3.14, result);
        }

        [Fact]
        public void GetTypedDefaultValue_Double_InvalidValue_ShouldReturnZero()
        {
            var v = new VariableDefinition { Type = VariableType.Double, DefaultValue = "xyz" };
            var result = v.GetTypedDefaultValue();
            Assert.IsType<double>(result);
            Assert.Equal(0.0, result);
        }

        [Fact]
        public void GetTypedDefaultValue_Boolean_True_ShouldReturnTrue()
        {
            var v = new VariableDefinition { Type = VariableType.Boolean, DefaultValue = "true" };
            var result = v.GetTypedDefaultValue();
            Assert.IsType<bool>(result);
            Assert.True((bool)result!);
        }

        [Fact]
        public void GetTypedDefaultValue_Boolean_False_ShouldReturnFalse()
        {
            var v = new VariableDefinition { Type = VariableType.Boolean, DefaultValue = "false" };
            var result = v.GetTypedDefaultValue();
            Assert.IsType<bool>(result);
            Assert.False((bool)result!);
        }

        [Fact]
        public void GetTypedDefaultValue_Boolean_Invalid_ShouldReturnFalse()
        {
            var v = new VariableDefinition { Type = VariableType.Boolean, DefaultValue = "invalid" };
            var result = v.GetTypedDefaultValue();
            Assert.IsType<bool>(result);
            Assert.False((bool)result!);
        }

        [Fact]
        public void DefaultValues_ShouldBeCorrect()
        {
            var v = new VariableDefinition();
            Assert.Equal(string.Empty, v.Name);
            Assert.Equal(VariableType.String, v.Type);
            Assert.Equal(string.Empty, v.DefaultValue);
            Assert.Equal(string.Empty, v.Description);
        }
    }
}
