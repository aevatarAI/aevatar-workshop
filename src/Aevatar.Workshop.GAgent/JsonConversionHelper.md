# JsonConversionHelper Usage Guide

## Purpose
`JsonConversionHelper` is a utility class that converts JSON objects to basic .NET types that can be serialized by Orleans and other systems. It handles both Newtonsoft.Json.Linq types (JObject, JArray, etc.) and System.Text.Json types (JsonElement). Additionally, it provides helper methods for environment dictionary conversion and result formatting for UI display.

## When to Use
Use this helper when:
- You receive JSON data from APIs that needs to be passed to Orleans Grains
- You have mixed JSON types (JObject/JsonElement) that need conversion
- You need to ensure all data is serializable by Orleans
- You want to convert complex JSON structures to basic .NET types
- You need to format results for display in UI
- You need to sanitize environment dictionaries

## Main Methods

### 1. ConvertToBasicTypes (Dictionary)
Converts a dictionary that may contain JSON objects to a dictionary with only basic .NET types.

### 2. ConvertToBasicTypes (IEnumerable)
Converts a list that may contain JSON objects to a list with only basic .NET types.

### 3. ConvertValue
Converts a single value that may be a JSON object to a basic .NET type.

### 4. ConvertEnvironmentDictionary
Converts environment dictionary ensuring all values are non-null strings.

### 5. FormatResultForDisplay
Formats results for better display in UI with type information and formatting.

### 6. NeedsConversion
Checks if a type needs conversion (i.e., it's a JSON type that Orleans can't serialize).

## Usage Examples

### Basic Dictionary Conversion
```csharp
using Aevatar.Workshop.GAgent;

// Convert a dictionary that may contain JObject/JsonElement
var input = new Dictionary<string, object>
{
    ["name"] = "John",
    ["data"] = someJObject,  // JObject from Newtonsoft.Json
    ["config"] = someJsonElement  // JsonElement from System.Text.Json
};

var converted = JsonConversionHelper.ConvertToBasicTypes(input);
// Now all values are basic .NET types
```

### Single Value Conversion
```csharp
// Convert a single JObject
JObject jObj = JObject.Parse(@"{""name"": ""test"", ""value"": 123}");
var converted = JsonConversionHelper.ConvertValue(jObj);
// Result: Dictionary<string, object> { ["name"] = "test", ["value"] = 123L }

// Convert a JsonElement
JsonElement element = JsonDocument.Parse(@"[1, 2, 3]").RootElement;
var converted = JsonConversionHelper.ConvertValue(element);
// Result: List<object> { 1, 2, 3 }
```

### List Conversion
```csharp
var inputList = new List<object> { jObject1, jsonElement2, "string", 123 };
var convertedList = JsonConversionHelper.ConvertToBasicTypes(inputList);
// All JSON objects are converted to basic types
```

### Environment Dictionary Conversion
```csharp
var env = new Dictionary<string, string> { ["PATH"] = "/usr/bin", ["HOME"] = null };
var sanitized = JsonConversionHelper.ConvertEnvironmentDictionary(env);
// Result: { ["PATH"] = "/usr/bin", ["HOME"] = "" }
```

### Format Result for Display
```csharp
// Format a JSON string for display
var jsonStr = @"{""name"": ""test"", ""value"": 123}";
var formatted = JsonConversionHelper.FormatResultForDisplay(jsonStr);
// Result: { type = "json", value = jsonStr, formatted = prettified JSON }

// Format an array for display
var list = new List<object> { 1, 2, 3 };
var formatted = JsonConversionHelper.FormatResultForDisplay(list);
// Result: { type = "array", count = 3, items = [...] }
```

### Check if Conversion is Needed
```csharp
var needsConversion = JsonConversionHelper.NeedsConversion(value.GetType());
if (needsConversion)
{
    value = JsonConversionHelper.ConvertValue(value);
}
```

## In Controllers

### Example: MCP Controller (Complete Usage)
```csharp
[HttpPost("initialize")]
public async Task<IActionResult> Initialize([FromBody] MCPInitRequest request)
{
    // Convert environment dictionary
    var config = new MCPGAgentConfig
    {
        Server = request.Servers.Select(s => new MCPServerConfig
        {
            ServerName = s.ServerName ?? string.Empty,
            Command = s.Command ?? string.Empty,
            Args = s.Args ?? new List<string>(),
            Env = JsonConversionHelper.ConvertEnvironmentDictionary(s.Environment)
        }).First()
    };
    // ... rest of initialization
}

[HttpPost("tool-call")]
public async Task<IActionResult> CallTool([FromBody] ToolCallRequest request)
{
    // Convert arguments before passing to Orleans
    var toolCallEvent = new ToolCallEvent
    {
        ToolName = request.ToolName,
        Arguments = JsonConversionHelper.ConvertToBasicTypes(
            request.Arguments ?? new Dictionary<string, object>()
        )
    };
    
    // ... execute tool call ...
    
    // Format result for display
    if (response?.Success == true && response.Result != null)
    {
        return Ok(new
        {
            success = true,
            resultDisplay = new
            {
                hasResult = true,
                resultType = response.Result?.GetType().Name ?? "Unknown",
                resultContent = response.Result,
                formattedResult = JsonConversionHelper.FormatResultForDisplay(response.Result)
            }
        });
    }
}
```

### Example: Generic API Handler
```csharp
public async Task<IActionResult> ProcessData([FromBody] Dictionary<string, object> data)
{
    // Ensure all nested JSON objects are converted
    var processableData = JsonConversionHelper.ConvertToBasicTypes(data);
    
    // Now safe for Orleans serialization
    await grain.ProcessData(processableData);
}
```

## Supported Conversions

### Newtonsoft.Json.Linq Types
- `JObject` → `Dictionary<string, object>`
- `JArray` → `List<object>`
- `JValue` → Appropriate .NET type (string, long, double, bool, etc.)
- `JToken` → Based on token type

### System.Text.Json Types
- `JsonElement` (Object) → `Dictionary<string, object>`
- `JsonElement` (Array) → `List<object>`
- `JsonElement` (String/Number/Bool) → Appropriate .NET type

### Additional Features
- Handles nested structures recursively
- Preserves null values
- Converts special types (DateTime, Guid, Uri, TimeSpan)
- Falls back to string representation for unknown types
- Formats results for UI display with type information
- Sanitizes environment dictionaries

## FormatResultForDisplay Features

The `FormatResultForDisplay` method provides rich formatting for UI display:

### JSON Strings
- Detects and formats JSON strings
- Provides both raw and prettified versions
- Includes type information

### Multiline Strings
- Provides line count
- Shows preview for long strings (first 5 lines)

### Arrays
- Shows element count
- Recursively formats each item

### Objects
- Shows property count
- Recursively formats each property

### Complex Objects
- Attempts JSON serialization
- Provides type name information
- Falls back to ToString for unknown types

## Performance Considerations
- The conversion is recursive, so deeply nested structures may impact performance
- Consider caching converted results if the same data is used multiple times
- For large datasets, consider streaming or pagination approaches
- FormatResultForDisplay creates new objects for display, so use sparingly in high-frequency scenarios 