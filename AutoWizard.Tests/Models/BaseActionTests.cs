using Xunit;
using AutoWizard.Core.Models;
using System.Collections.Generic;
using ExecutionContext = AutoWizard.Core.Models.ExecutionContext;

namespace AutoWizard.Tests.Models
{
    /// <summary>
    /// 可控制結果的測試用 Action
    /// </summary>
    public class PolicyTestAction : BaseAction
    {
        public int ExecuteCallCount { get; set; } = 0;
        public int FailUntilAttempt { get; set; } = 0;

        public override ActionResult Execute(ExecutionContext context)
        {
            ExecuteCallCount++;
            
            if (FailUntilAttempt > 0 && ExecuteCallCount < FailUntilAttempt)
            {
                return new ActionResult
                {
                    Success = false,
                    Message = $"Failed on attempt {ExecuteCallCount}"
                };
            }

            return new ActionResult { Success = true, Message = "OK" };
        }
    }

    public class BaseActionTests
    {
        private ExecutionContext CreateContext()
        {
            return new ExecutionContext
            {
                Variables = new Dictionary<string, object>(),
                IsCancellationRequested = false,
                LogAction = _ => { }
            };
        }

        [Fact]
        public void ExecuteWithPolicy_SuccessfulAction_ShouldReturnSuccess()
        {
            var action = new PolicyTestAction { Name = "Success" };
            var context = CreateContext();

            var result = action.ExecuteWithPolicy(context);

            Assert.True(result.Success);
            Assert.Equal(1, action.ExecuteCallCount);
        }

        [Fact]
        public void ExecuteWithPolicy_WithRetry_ShouldRetryOnFailure()
        {
            var action = new PolicyTestAction
            {
                Name = "RetryTest",
                FailUntilAttempt = 3,
                ErrorPolicy = new ErrorHandlingPolicy
                {
                    RetryCount = 5,
                    RetryIntervalMs = 0
                }
            };
            var context = CreateContext();

            var result = action.ExecuteWithPolicy(context);

            Assert.True(result.Success);
            Assert.Equal(3, action.ExecuteCallCount);
        }

        [Fact]
        public void ExecuteWithPolicy_ExceedRetryCount_ShouldReturnFailure()
        {
            var action = new PolicyTestAction
            {
                Name = "AlwaysFail",
                FailUntilAttempt = 999,
                ErrorPolicy = new ErrorHandlingPolicy
                {
                    RetryCount = 2,
                    RetryIntervalMs = 0
                }
            };
            var context = CreateContext();

            var result = action.ExecuteWithPolicy(context);

            Assert.False(result.Success);
            Assert.Equal(3, action.ExecuteCallCount); // 1 + 2 retries
        }

        [Fact]
        public void ExecuteWithPolicy_NoRetry_ShouldFailImmediately()
        {
            var action = new PolicyTestAction
            {
                Name = "NoRetry",
                FailUntilAttempt = 999,
                ErrorPolicy = new ErrorHandlingPolicy { RetryCount = 0 }
            };
            var context = CreateContext();

            var result = action.ExecuteWithPolicy(context);

            Assert.False(result.Success);
            Assert.Equal(1, action.ExecuteCallCount);
        }

        [Fact]
        public void DefaultErrorPolicy_ShouldHaveZeroRetries()
        {
            var action = new PolicyTestAction { Name = "Default" };

            Assert.Equal(0, action.ErrorPolicy.RetryCount);
            Assert.False(action.ErrorPolicy.ContinueOnError);
        }
    }
}
