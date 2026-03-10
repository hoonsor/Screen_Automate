using Xunit;
using AutoWizard.Core.Engine;
using AutoWizard.Core.Models;
using System.Collections.Generic;
using System.Threading.Tasks;
using ExecutionContext = AutoWizard.Core.Models.ExecutionContext;

namespace AutoWizard.Tests.Engine
{
    /// <summary>
    /// 用於測試的模擬指令（不調用任何 Windows API）
    /// </summary>
    public class TestAction : BaseAction
    {
        public bool ShouldSucceed { get; set; } = true;
        public int ExecutionTimeMs { get; set; } = 10;
        public bool WasExecuted { get; set; } = false;

        public override ActionResult Execute(ExecutionContext context)
        {
            WasExecuted = true;

            if (ExecutionTimeMs > 0)
            {
                Thread.Sleep(ExecutionTimeMs);
            }

            context.Log($"TestAction '{Name}' executed");

            return new ActionResult
            {
                Success = ShouldSucceed,
                Message = ShouldSucceed ? "OK" : "Simulated failure"
            };
        }
    }

    /// <summary>
    /// 用於模擬長時間執行的指令（可被取消）
    /// </summary>
    public class SlowTestAction : BaseAction
    {
        public int ExecutionTimeMs { get; set; } = 2000;

        public override ActionResult Execute(ExecutionContext context)
        {
            int elapsed = 0;
            int step = 50;
            while (elapsed < ExecutionTimeMs)
            {
                if (context.IsCancellationRequested)
                {
                    return new ActionResult
                    {
                        Success = false,
                        Message = "Cancelled during execution"
                    };
                }
                Thread.Sleep(step);
                elapsed += step;
            }

            return new ActionResult { Success = true, Message = "Slow action completed" };
        }
    }

    public class ScriptExecutorTests
    {
        [Fact]
        public async Task ExecuteAsync_WithEmptyActions_ShouldReturnCompleted()
        {
            // Arrange
            var executor = new ScriptExecutor();
            var actions = new List<BaseAction>();

            // Act
            var result = await executor.ExecuteAsync(actions);

            // Assert
            Assert.Equal(ExecutionStatus.Completed, result.Status);
            Assert.True(result.Duration.TotalMilliseconds >= 0);
            Assert.Null(result.ErrorMessage);
        }

        [Fact]
        public async Task ExecuteAsync_WithActions_ShouldExecuteAll()
        {
            // Arrange
            var executor = new ScriptExecutor();
            var action1 = new TestAction { Name = "Action1", ExecutionTimeMs = 10 };
            var action2 = new TestAction { Name = "Action2", ExecutionTimeMs = 10 };
            var action3 = new TestAction { Name = "Action3", ExecutionTimeMs = 10 };
            var actions = new List<BaseAction> { action1, action2, action3 };

            // Act
            var result = await executor.ExecuteAsync(actions);

            // Assert
            Assert.Equal(ExecutionStatus.Completed, result.Status);
            Assert.True(action1.WasExecuted);
            Assert.True(action2.WasExecuted);
            Assert.True(action3.WasExecuted);
            Assert.NotEmpty(result.Logs);
        }

        [Fact]
        public async Task ExecuteAsync_WithFailingAction_ShouldReturnFailed()
        {
            // Arrange
            var executor = new ScriptExecutor();
            var action1 = new TestAction { Name = "Good", ShouldSucceed = true, ExecutionTimeMs = 10 };
            var action2 = new TestAction { Name = "Bad", ShouldSucceed = false, ExecutionTimeMs = 10 };
            var action3 = new TestAction { Name = "Never", ShouldSucceed = true, ExecutionTimeMs = 10 };
            var actions = new List<BaseAction> { action1, action2, action3 };

            // Act
            var result = await executor.ExecuteAsync(actions);

            // Assert
            Assert.Equal(ExecutionStatus.Failed, result.Status);
            Assert.True(action1.WasExecuted);
            Assert.True(action2.WasExecuted);
            Assert.False(action3.WasExecuted); // 應在失敗後停止
        }

        [Fact]
        public async Task Stop_DuringExecution_ShouldCancel()
        {
            // Arrange
            var executor = new ScriptExecutor();
            var slowAction = new SlowTestAction { Name = "Slow", ExecutionTimeMs = 5000 };
            var actions = new List<BaseAction> { slowAction };

            // Act - 啟動執行後等 200ms 然後停止
            var executionTask = executor.ExecuteAsync(actions);
            await Task.Delay(200);
            executor.Stop();

            var result = await executionTask;

            // Assert - 結果應為 Cancelled 或 Failed (取決於取消時機)
            Assert.True(
                result.Status == ExecutionStatus.Cancelled || result.Status == ExecutionStatus.Failed,
                $"Expected Cancelled or Failed, but got {result.Status}"
            );
        }

        [Fact]
        public async Task ExecuteAsync_ShouldFireLogReceivedEvents()
        {
            // Arrange
            var executor = new ScriptExecutor();
            var logMessages = new List<string>();
            executor.LogReceived += msg => logMessages.Add(msg);

            var action = new TestAction { Name = "LogTest", ExecutionTimeMs = 10 };
            var actions = new List<BaseAction> { action };

            // Act
            await executor.ExecuteAsync(actions);

            // Assert
            Assert.NotEmpty(logMessages);
            Assert.Contains(logMessages, m => m.Contains("LogTest"));
        }

        [Fact]
        public async Task ExecuteAsync_ShouldFireStatusChangedEvents()
        {
            // Arrange
            var executor = new ScriptExecutor();
            var statuses = new List<ExecutionStatus>();
            executor.StatusChanged += status => statuses.Add(status);

            var action = new TestAction { Name = "StatusTest", ExecutionTimeMs = 10 };
            var actions = new List<BaseAction> { action };

            // Act
            await executor.ExecuteAsync(actions);

            // Assert - 應收到 Running 和 Completed
            Assert.Contains(ExecutionStatus.Running, statuses);
            Assert.Contains(ExecutionStatus.Completed, statuses);
        }

        [Fact]
        public async Task ExecuteAsync_WithVariables_ShouldPassVariablesToContext()
        {
            // Arrange
            var executor = new ScriptExecutor();
            var variables = new Dictionary<string, object>
            {
                { "TestVar", "Hello" },
                { "Counter", 42 }
            };

            var action = new TestAction { Name = "VarTest", ExecutionTimeMs = 10 };
            var actions = new List<BaseAction> { action };

            // Act
            var result = await executor.ExecuteAsync(actions, variables);

            // Assert
            Assert.Equal(ExecutionStatus.Completed, result.Status);
            Assert.True(action.WasExecuted);
        }
    }
}
