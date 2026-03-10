using Xunit;
using AutoWizard.Core.Resources;
using AutoWizard.Core.Models;
using System.IO;
using System.Collections.Generic;

namespace AutoWizard.Tests.Resources
{
    public class AwsPackageVariableTests
    {
        private string GetTempFilePath() => Path.Combine(Path.GetTempPath(), $"test_{System.Guid.NewGuid()}.aws");

        [Fact]
        public void SaveAndLoad_WithVariables_ShouldPreserveVariables()
        {
            var filePath = GetTempFilePath();
            try
            {
                var package = new AwsPackage
                {
                    ScriptName = "變數測試",
                    Variables = new List<VariableDefinition>
                    {
                        new VariableDefinition { Name = "counter", Type = VariableType.Integer, DefaultValue = "10", Description = "計數器" },
                        new VariableDefinition { Name = "name", Type = VariableType.String, DefaultValue = "AutoWizard", Description = "名稱" },
                        new VariableDefinition { Name = "threshold", Type = VariableType.Double, DefaultValue = "0.85", Description = "門檻值" },
                        new VariableDefinition { Name = "enabled", Type = VariableType.Boolean, DefaultValue = "true", Description = "啟用" }
                    }
                };

                // Act
                package.Save(filePath);
                var loaded = AwsPackage.Load(filePath);

                // Assert
                Assert.Equal(4, loaded.Variables.Count);

                Assert.Equal("counter", loaded.Variables[0].Name);
                Assert.Equal(VariableType.Integer, loaded.Variables[0].Type);
                Assert.Equal("10", loaded.Variables[0].DefaultValue);
                Assert.Equal("計數器", loaded.Variables[0].Description);

                Assert.Equal("name", loaded.Variables[1].Name);
                Assert.Equal(VariableType.String, loaded.Variables[1].Type);
                Assert.Equal("AutoWizard", loaded.Variables[1].DefaultValue);

                Assert.Equal("threshold", loaded.Variables[2].Name);
                Assert.Equal(VariableType.Double, loaded.Variables[2].Type);
                Assert.Equal("0.85", loaded.Variables[2].DefaultValue);

                Assert.Equal("enabled", loaded.Variables[3].Name);
                Assert.Equal(VariableType.Boolean, loaded.Variables[3].Type);
                Assert.Equal("true", loaded.Variables[3].DefaultValue);
            }
            finally
            {
                File.Delete(filePath);
            }
        }

        [Fact]
        public void SaveAndLoad_EmptyVariables_ShouldWork()
        {
            var filePath = GetTempFilePath();
            try
            {
                var package = new AwsPackage { ScriptName = "No Variables" };
                package.Save(filePath);
                var loaded = AwsPackage.Load(filePath);

                Assert.Empty(loaded.Variables);
            }
            finally
            {
                File.Delete(filePath);
            }
        }
    }
}
