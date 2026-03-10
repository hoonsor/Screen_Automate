using Xunit;
using AutoWizard.Core.Resources;
using AutoWizard.Core.Models;
using AutoWizard.Core.Actions.Input;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace AutoWizard.Tests.Resources
{
    public class AwsSerializationDebugTests
    {
        [Fact]
        public void Serialize_Actions_ShouldIncludeTypeDiscriminator()
        {
            var options = new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNameCaseInsensitive = true,
                Converters = { new JsonStringEnumConverter() }
            };

            var actions = new List<BaseAction>
            {
                new ClickAction { Name = "Test Click", X = 100, Y = 200 },
                new KeyboardAction { Name = "Test Key", Key = "ENTER" }
            };

            var json = JsonSerializer.Serialize(actions, options);
            
            // Must contain $type discriminator
            Assert.Contains("$type", json);
            Assert.Contains("Click", json);
            Assert.Contains("Keyboard", json);
            // Must contain subclass properties
            Assert.Contains("\"X\"", json);
            Assert.Contains("\"Key\"", json);
        }
    }
}
