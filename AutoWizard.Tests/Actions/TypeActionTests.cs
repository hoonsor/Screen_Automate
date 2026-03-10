using Xunit;
using AutoWizard.Core.Actions.Input;
using AutoWizard.Core.Models;

namespace AutoWizard.Tests.Actions
{
    public class TypeActionTests
    {
        [Fact]
        public void Properties_ShouldBeSetCorrectly()
        {
            var action = new TypeAction
            {
                Text = "Hello World",
                Name = "TypeHello"
            };

            Assert.Equal("Hello World", action.Text);
            Assert.Equal("TypeHello", action.Name);
        }

        [Fact]
        public void DefaultValues_ShouldBeReasonable()
        {
            var action = new TypeAction();

            Assert.NotNull(action.Text);
            Assert.NotNull(action.ErrorPolicy);
            Assert.Equal(0, action.ErrorPolicy.RetryCount);
        }
    }
}
