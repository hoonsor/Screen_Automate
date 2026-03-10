using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using AutoWizard.Core.Models;
using AutoWizard.Core.Actions.Vision;

namespace AutoWizard.Core.Validation
{
    public enum ValidationLevel
    {
        Info,
        Warning,
        Error
    }

    public class ValidationResult
    {
        public ValidationLevel Level { get; set; }
        public string Message { get; set; } = string.Empty;
        public string ActionId { get; set; } = string.Empty; // Optional: Link to specific action
        public BaseAction? SourceAction { get; set; }
    }

    public class ScriptValidator
    {
        public List<ValidationResult> Validate(IEnumerable<BaseAction> actions)
        {
            var results = new List<ValidationResult>();

            foreach (var action in actions)
            {
                ValidateAction(action, results);
            }

            return results;
        }

        private void ValidateAction(BaseAction action, List<ValidationResult> results)
        {
            // 1. Check FindImageAction
            if (action is FindImageAction findImage)
            {
                if (string.IsNullOrWhiteSpace(findImage.TemplateImagePath))
                {
                    results.Add(new ValidationResult
                    {
                        Level = ValidationLevel.Error,
                        Message = $"[FindImage] Template image path is empty.",
                        SourceAction = action
                    });
                }
                else if (!File.Exists(findImage.TemplateImagePath))
                {
                     results.Add(new ValidationResult
                    {
                        Level = ValidationLevel.Error,
                        Message = $"[FindImage] Image not found: {findImage.TemplateImagePath}",
                        SourceAction = action
                    });
                }
            }

            // 2. Check Variable Syntax (Simple Regex for unclosed braces)
            ValidateStringProperty(action.Description, "Description", action, results);
            
            // 3. Recursive check for containers
            if (action is Actions.Control.IfAction ifAction)
            {
                foreach (var child in ifAction.ThenActions) ValidateAction(child, results);
                foreach (var child in ifAction.ElseActions) ValidateAction(child, results);
            }
            else if (action is ContainerAction container)
            {
                foreach (var child in container.Children) ValidateAction(child, results);
            }
        }

        private void ValidateStringProperty(string? value, string propertyName, BaseAction action, List<ValidationResult> results)
        {
            if (string.IsNullOrEmpty(value)) return;

            // Check for unclosed {{ or }}
            int openCount = Regex.Matches(value, "{{").Count;
            int closeCount = Regex.Matches(value, "}}").Count;

            if (openCount != closeCount)
            {
                 results.Add(new ValidationResult
                {
                    Level = ValidationLevel.Warning,
                    Message = $"[{action.GetType().Name}] Mismatched variable braces in {propertyName}.",
                    SourceAction = action
                });
            }
        }
    }
}
