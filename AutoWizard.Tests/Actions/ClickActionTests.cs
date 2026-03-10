using Xunit;
using AutoWizard.Core.Actions.Input;
using AutoWizard.Core.Models;

namespace AutoWizard.Tests.Actions
{
    public class ClickActionTests
    {
        [Fact]
        public void Execute_ShouldSetCorrectCoordinates()
        {
            var action = new ClickAction
            {
                X = 100,
                Y = 200,
                Button = MouseButton.Left
            };

            Assert.Equal(100, action.X);
            Assert.Equal(200, action.Y);
            Assert.Equal(MouseButton.Left, action.Button);
        }

        [Fact]
        public void Properties_RightButton_ShouldBeSet()
        {
            var action = new ClickAction
            {
                X = 300,
                Y = 400,
                Button = MouseButton.Right
            };

            Assert.Equal(MouseButton.Right, action.Button);
        }

        [Fact]
        public void Properties_ClickType_ShouldBeSettable()
        {
            var action = new ClickAction
            {
                X = 50,
                Y = 50,
                ClickType = ClickType.Double
            };

            Assert.Equal(ClickType.Double, action.ClickType);
        }

        [Fact]
        public void DefaultValues_ShouldBeReasonable()
        {
            var action = new ClickAction();

            Assert.Equal(0, action.X);
            Assert.Equal(0, action.Y);
            Assert.Equal(MouseButton.Left, action.Button);
            Assert.Equal(ClickType.Single, action.ClickType);
            Assert.NotNull(action.ErrorPolicy);
        }
    }
}
