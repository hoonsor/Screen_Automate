using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using AutoWizard.Core.Models;
using AutoWizard.Core.Actions.Input;

var options = new JsonSerializerOptions
{
    WriteIndented = true,
    PropertyNameCaseInsensitive = true,
    Converters = { new JsonStringEnumConverter() }
};

var actions = new List<BaseAction>
{
    new ClickAction { Name = ""Test Click"", X = 100, Y = 200 },
    new KeyboardAction { Name = ""Test Key"", Key = ""ENTER"" }
};

var json = JsonSerializer.Serialize(actions, options);
Console.WriteLine(json);
