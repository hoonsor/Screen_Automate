using Xunit;
using AutoWizard.Core.Recording;
using System.Threading;

namespace AutoWizard.Tests.Recording
{
    public class RecorderTests
    {
        [Fact]
        public void StartRecording_ShouldSetIsRecordingToTrue()
        {
            // Arrange
            using var recorder = new Recorder();

            // Act
            recorder.StartRecording();

            // Assert
            Assert.True(recorder.IsRecording);

            // Cleanup
            recorder.StopRecording();
        }

        [Fact]
        public void StopRecording_ShouldSetIsRecordingToFalse()
        {
            // Arrange
            using var recorder = new Recorder();
            recorder.StartRecording();

            // Act
            recorder.StopRecording();

            // Assert
            Assert.False(recorder.IsRecording);
        }

        [Fact]
        public void GetRecordedActions_ShouldReturnEmptyListInitially()
        {
            // Arrange
            using var recorder = new Recorder();

            // Act
            var actions = recorder.GetRecordedActions();

            // Assert
            Assert.Empty(actions);
        }
    }
}
