using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Shouldly;
using Xunit.Abstractions;

namespace Aevatar.Workshop.Tests;

public class ConfigParsingTests
{
    private readonly ITestOutputHelper _testOutputHelper;

    public ConfigParsingTests(ITestOutputHelper testOutputHelper)
    {
        _testOutputHelper = testOutputHelper;
    }

    [Fact]
    public void Should_Parse_Configuration_With_Arrays_Correctly()
    {
        // Arrange
        var configData = new Dictionary<string, string>
        {
            ["MCPServers:server1:Command"] = "npx",
            ["MCPServers:server1:Args:0"] = "-y",
            ["MCPServers:server1:Args:1"] = "@modelcontextprotocol/server-filesystem",
            ["MCPServers:server1:Description"] = "File system access",
            ["MCPServers:server1:Enabled"] = "true",
            ["MCPServers:server2:Command"] = "node",
            ["MCPServers:server2:Args:0"] = "server.js",
            ["MCPServers:server2:Description"] = "Custom server",
            ["MCPServers:server2:Enabled"] = "false"
        };

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configData)
            .Build();

        // Act
        var json = GetConfigurationJson(configuration, "MCPServers");
        _testOutputHelper.WriteLine($"Generated JSON: {json}");

        // Assert
        var jsonDoc = JsonDocument.Parse(json);
        var root = jsonDoc.RootElement;
        
        root.TryGetProperty("server1", out var server1).ShouldBeTrue();
        server1.GetProperty("Command").GetString().ShouldBe("npx");
        
        // Check Args array
        server1.TryGetProperty("Args", out var args).ShouldBeTrue();
        args.ValueKind.ShouldBe(JsonValueKind.Array);
        args.GetArrayLength().ShouldBe(2);
        args[0].GetString().ShouldBe("-y");
        args[1].GetString().ShouldBe("@modelcontextprotocol/server-filesystem");
    }

    [Fact]
    public void Should_Parse_Configuration_With_Nested_Objects()
    {
        // Arrange
        var configData = new Dictionary<string, string>
        {
            ["SystemLLMConfigs:OpenAI:ProviderEnum"] = "OpenAI",
            ["SystemLLMConfigs:OpenAI:ModelIdEnum"] = "OpenAI",
            ["SystemLLMConfigs:OpenAI:ModelName"] = "gpt-4",
            ["SystemLLMConfigs:OpenAI:Endpoint"] = "https://api.openai.com",
            ["SystemLLMConfigs:OpenAI:ApiKey"] = "test-key",
            ["SystemLLMConfigs:DeepSeek:ProviderEnum"] = "DeepSeek",
            ["SystemLLMConfigs:DeepSeek:ModelIdEnum"] = "DeepSeek", 
            ["SystemLLMConfigs:DeepSeek:ModelName"] = "deepseek-chat",
            ["SystemLLMConfigs:DeepSeek:Endpoint"] = "https://api.deepseek.com",
            ["SystemLLMConfigs:DeepSeek:ApiKey"] = "test-key-2"
        };

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configData)
            .Build();

        // Act
        var json = GetConfigurationJson(configuration, "SystemLLMConfigs");
        _testOutputHelper.WriteLine($"Generated JSON: {json}");

        // Assert
        var jsonDoc = JsonDocument.Parse(json);
        var root = jsonDoc.RootElement;
        
        root.TryGetProperty("OpenAI", out var openAi).ShouldBeTrue();
        openAi.GetProperty("ProviderEnum").GetString().ShouldBe("OpenAI");
        openAi.GetProperty("ModelName").GetString().ShouldBe("gpt-4");
        
        root.TryGetProperty("DeepSeek", out var deepSeek).ShouldBeTrue();
        deepSeek.GetProperty("ProviderEnum").GetString().ShouldBe("DeepSeek");
    }

    // Copy the configuration parsing logic from ConfigSyncService
    private string GetConfigurationJson(IConfiguration configuration, string sectionName)
    {
        var section = configuration.GetSection(sectionName);
        if (!section.Exists())
        {
            return "{}";
        }

        // Convert IConfigurationSection to dictionary
        var dict = new Dictionary<string, object>();
        foreach (var child in section.GetChildren())
        {
            if (child.GetChildren().Any())
            {
                // This is a complex object
                var childDict = GetSectionAsDictionary(child);
                dict[child.Key] = ConvertArrayRepresentation(childDict);
            }
            else
            {
                // This is a simple value
                dict[child.Key] = child.Value ?? string.Empty;
            }
        }

        return JsonSerializer.Serialize(dict);
    }

    private object ConvertArrayRepresentation(Dictionary<string, object> dict)
    {
        // Check if this dictionary represents an array
        if (dict.ContainsKey("_isArray") && dict.ContainsKey("_items") && 
            dict["_isArray"] is bool isArray && isArray)
        {
            var items = dict["_items"] as List<object>;
            if (items != null)
            {
                // Convert any nested dictionaries that might also be arrays
                var convertedItems = new List<object>();
                foreach (var item in items)
                {
                    if (item is Dictionary<string, object> itemDict)
                    {
                        convertedItems.Add(ConvertArrayRepresentation(itemDict));
                    }
                    else
                    {
                        convertedItems.Add(item);
                    }
                }
                return convertedItems;
            }
        }

        // Not an array, process nested dictionaries
        var result = new Dictionary<string, object>();
        foreach (var kvp in dict)
        {
            if (kvp.Value is Dictionary<string, object> nestedDict)
            {
                result[kvp.Key] = ConvertArrayRepresentation(nestedDict);
            }
            else
            {
                result[kvp.Key] = kvp.Value;
            }
        }
        return result;
    }

    private Dictionary<string, object> GetSectionAsDictionary(IConfigurationSection section)
    {
        var dict = new Dictionary<string, object>();
        var children = section.GetChildren().ToList();
        
        // Check if this section represents an array
        if (children.Any() && children.All(child => child.Key.All(char.IsDigit)))
        {
            // This is an array - return it as a list
            var array = new List<object>();
            foreach (var child in children.OrderBy(c => int.Parse(c.Key)))
            {
                if (child.GetChildren().Any())
                {
                    array.Add(GetSectionAsDictionary(child));
                }
                else
                {
                    array.Add(child.Value ?? string.Empty);
                }
            }
            // Return a dictionary with the array marked
            dict["_isArray"] = true;
            dict["_items"] = array;
            return dict;
        }

        // Process as regular object
        foreach (var child in children)
        {
            if (child.GetChildren().Any())
            {
                // This is a nested object or array
                dict[child.Key] = GetSectionAsDictionary(child);
            }
            else
            {
                // This is a simple value
                dict[child.Key] = child.Value ?? string.Empty;
            }
        }

        return dict;
    }
} 