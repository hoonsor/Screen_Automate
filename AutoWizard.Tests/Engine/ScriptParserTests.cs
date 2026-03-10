using Xunit;
using AutoWizard.Core.Engine;
using AutoWizard.Core.Models;
using System.Collections.Generic;
using System.Text.Json;

namespace AutoWizard.Tests.Engine
{
    public class ScriptParserTests
    {
        private readonly ScriptParser _parser = new();

        #region Basic Parsing

        [Fact]
        public void ParseScript_ValidClickAction_ShouldReturnClickAction()
        {
            var json = @"{
                ""Workflow"": [
                    {
                        ""Type"": ""Click"",
                        ""Parameters"": { ""X"": 100, ""Y"": 200, ""Button"": ""Left"" }
                    }
                ]
            }";

            var script = _parser.ParseScript(json);
            var actions = _parser.BuildActions(script);

            Assert.Single(actions);
            var clickAction = Assert.IsType<AutoWizard.Core.Actions.Input.ClickAction>(actions[0]);
            Assert.Equal(100, clickAction.X);
            Assert.Equal(200, clickAction.Y);
        }

        [Fact]
        public void ParseScript_ValidTypeAction_ShouldReturnTypeAction()
        {
            var json = @"{
                ""Workflow"": [
                    {
                        ""Type"": ""Type"",
                        ""Parameters"": { ""Text"": ""Hello World"" }
                    }
                ]
            }";

            var script = _parser.ParseScript(json);
            var actions = _parser.BuildActions(script);

            Assert.Single(actions);
            var typeAction = Assert.IsType<AutoWizard.Core.Actions.Input.TypeAction>(actions[0]);
            Assert.Equal("Hello World", typeAction.Text);
        }

        [Fact]
        public void ParseScript_MultipleActions_ShouldReturnAllActions()
        {
            var json = @"{
                ""Workflow"": [
                    { ""Type"": ""Click"", ""Parameters"": { ""X"": 10, ""Y"": 20 } },
                    { ""Type"": ""Type"", ""Parameters"": { ""Text"": ""Test"" } },
                    { ""Type"": ""Click"", ""Parameters"": { ""X"": 30, ""Y"": 40 } }
                ]
            }";

            var script = _parser.ParseScript(json);
            var actions = _parser.BuildActions(script);

            Assert.Equal(3, actions.Count);
        }

        #endregion

        #region Control Flow Parsing

        [Fact]
        public void ParseScript_IfAction_ShouldParseCorrectly()
        {
            var json = @"{
                ""Workflow"": [
                    {
                        ""Type"": ""If"",
                        ""Parameters"": { 
                            ""ConditionType"": ""VariableEquals"",
                            ""LeftOperand"": ""{myVar}"",
                            ""RightOperand"": ""hello""
                        },
                        ""ThenActions"": [
                            { ""Type"": ""Click"", ""Parameters"": { ""X"": 10, ""Y"": 20 } }
                        ],
                        ""ElseActions"": [
                            { ""Type"": ""Click"", ""Parameters"": { ""X"": 30, ""Y"": 40 } }
                        ]
                    }
                ]
            }";

            var script = _parser.ParseScript(json);
            var actions = _parser.BuildActions(script);

            Assert.Single(actions);
            var ifAction = Assert.IsType<AutoWizard.Core.Actions.Control.IfAction>(actions[0]);
            Assert.NotEmpty(ifAction.ThenActions);
            Assert.NotEmpty(ifAction.ElseActions);
        }

        [Fact]
        public void ParseScript_LoopAction_ShouldParseCorrectly()
        {
            var json = @"{
                ""Workflow"": [
                    {
                        ""Type"": ""Loop"",
                        ""Parameters"": { 
                            ""LoopType"": ""Count"",
                            ""Count"": 5
                        },
                        ""Children"": [
                            { ""Type"": ""Click"", ""Parameters"": { ""X"": 10, ""Y"": 20 } }
                        ]
                    }
                ]
            }";

            var script = _parser.ParseScript(json);
            var actions = _parser.BuildActions(script);

            Assert.Single(actions);
            var loopAction = Assert.IsType<AutoWizard.Core.Actions.Control.LoopAction>(actions[0]);
            Assert.Equal(5, loopAction.Count);
        }

        #endregion

        #region Edge Cases

        [Fact]
        public void ParseScript_UnknownActionType_ShouldBeIgnored()
        {
            var json = @"{
                ""Workflow"": [
                    { ""Type"": ""UnknownAction"", ""Parameters"": {} },
                    { ""Type"": ""Click"", ""Parameters"": { ""X"": 10, ""Y"": 20 } }
                ]
            }";

            var script = _parser.ParseScript(json);
            var actions = _parser.BuildActions(script);

            Assert.Single(actions);
        }

        [Fact]
        public void ParseScript_EmptyWorkflow_ShouldReturnEmptyList()
        {
            var json = @"{ ""Workflow"": [] }";

            var script = _parser.ParseScript(json);
            var actions = _parser.BuildActions(script);

            Assert.Empty(actions);
        }

        [Fact]
        public void ParseScript_WithErrorPolicy_ShouldSetCorrectly()
        {
            var json = @"{
                ""Workflow"": [
                    {
                        ""Type"": ""Click"",
                        ""Parameters"": { ""X"": 10, ""Y"": 20 },
                        ""ErrorPolicy"": {
                            ""RetryCount"": 3,
                            ""RetryIntervalMs"": 1000,
                            ""ContinueOnError"": true
                        }
                    }
                ]
            }";

            var script = _parser.ParseScript(json);
            var actions = _parser.BuildActions(script);

            Assert.Single(actions);
            Assert.Equal(3, actions[0].ErrorPolicy.RetryCount);
            Assert.True(actions[0].ErrorPolicy.ContinueOnError);
        }

        #endregion
    }
}
