using Xunit;
using AutoWizard.Core.Actions.Control;
using AutoWizard.Core.Models;
using System.Collections.Generic;
using ExecutionContext = AutoWizard.Core.Models.ExecutionContext;

namespace AutoWizard.Tests.Actions
{
    public class LoopActionTests
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
        public void Execute_CountLoop_ShouldExecuteNTimes()
        {
            int executionCount = 0;
            var bodyAction = new CountingAction(() => executionCount++);

            var loopAction = new LoopAction
            {
                Name = "CountLoop",
                LoopType = LoopType.Count,
                Count = 5,
                Children = new List<BaseAction> { bodyAction }
            };

            var context = CreateContext();
            var result = loopAction.Execute(context);

            Assert.True(result.Success);
            Assert.Equal(5, executionCount);
        }

        [Fact]
        public void Execute_CountLoop_Zero_ShouldNotExecute()
        {
            int executionCount = 0;
            var bodyAction = new CountingAction(() => executionCount++);

            var loopAction = new LoopAction
            {
                Name = "ZeroLoop",
                LoopType = LoopType.Count,
                Count = 0,
                Children = new List<BaseAction> { bodyAction }
            };

            var context = CreateContext();
            var result = loopAction.Execute(context);

            Assert.True(result.Success);
            Assert.Equal(0, executionCount);
        }

        [Fact]
        public void Execute_ForeachLoop_ShouldIterateOverItems()
        {
            var capturedValues = new List<object>();
            var bodyAction = new VariableCaptureAction("item", capturedValues);

            var loopAction = new LoopAction
            {
                Name = "ForeachLoop",
                LoopType = LoopType.Foreach,
                ForeachVariable = "item",
                ForeachList = new List<string> { "A", "B", "C" },
                Children = new List<BaseAction> { bodyAction }
            };

            var context = CreateContext();
            var result = loopAction.Execute(context);

            Assert.True(result.Success);
            Assert.Equal(3, capturedValues.Count);
            Assert.Equal("A", capturedValues[0]);
            Assert.Equal("B", capturedValues[1]);
            Assert.Equal("C", capturedValues[2]);
        }

        [Fact]
        public void Execute_CountLoop_ShouldSetLoopIndex()
        {
            var capturedIndices = new List<object>();
            var bodyAction = new VariableCaptureAction("_loopIndex", capturedIndices);

            var loopAction = new LoopAction
            {
                Name = "IndexLoop",
                LoopType = LoopType.Count,
                Count = 3,
                Children = new List<BaseAction> { bodyAction }
            };

            var context = CreateContext();
            var result = loopAction.Execute(context);

            Assert.Equal(3, capturedIndices.Count);
            Assert.Equal(0, capturedIndices[0]);
            Assert.Equal(1, capturedIndices[1]);
            Assert.Equal(2, capturedIndices[2]);
        }

        [Fact]
        public void Execute_CountLoop_Cancellation_ShouldStopEarly()
        {
            int executionCount = 0;
            var context = CreateContext();
            var bodyAction = new CountingAction(() => executionCount++);
            var cancelAction = new CancelAfterNAction(3, context);

            var loopAction = new LoopAction
            {
                Name = "CancelLoop",
                LoopType = LoopType.Count,
                Count = 100,
                Children = new List<BaseAction> { bodyAction, cancelAction }
            };

            var result = loopAction.Execute(context);

            Assert.True(executionCount <= 4); // 應在取消後很快停止
        }
    }

    #region Test Helpers

    internal class CountingAction : BaseAction
    {
        private readonly System.Action _callback;
        
        public CountingAction(System.Action callback) 
        { 
            _callback = callback;
            Name = "Counter";
        }

        public override ActionResult Execute(ExecutionContext context)
        {
            _callback();
            return new ActionResult { Success = true };
        }
    }

    internal class VariableCaptureAction : BaseAction
    {
        private readonly string _variableName;
        private readonly List<object> _capturedValues;

        public VariableCaptureAction(string variableName, List<object> capturedValues)
        {
            _variableName = variableName;
            _capturedValues = capturedValues;
            Name = "VariableCapture";
        }

        public override ActionResult Execute(ExecutionContext context)
        {
            if (context.Variables.TryGetValue(_variableName, out var value))
            {
                _capturedValues.Add(value);
            }
            return new ActionResult { Success = true };
        }
    }

    internal class CancelAfterNAction : BaseAction
    {
        private readonly int _cancelAfter;
        private readonly ExecutionContext _targetContext;
        private int _count = 0;

        public CancelAfterNAction(int cancelAfter, ExecutionContext targetContext)
        {
            _cancelAfter = cancelAfter;
            _targetContext = targetContext;
            Name = "CancelAfterN";
        }

        public override ActionResult Execute(ExecutionContext context)
        {
            _count++;
            if (_count >= _cancelAfter)
            {
                _targetContext.IsCancellationRequested = true;
            }
            return new ActionResult { Success = true };
        }
    }

    #endregion
}
