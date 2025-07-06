# DeepSeek Support in AI Tool Calling Demo

## Problem
The AI Tool Calling Demo was hardcoded to use OpenAI API regardless of the configured LLM provider.

## Solution
Modified `ToolCallingAIGAgent.cs` to support multiple LLM providers based on configuration:

### Supported Providers
1. **OpenAI** - Default provider
2. **DeepSeek** - Uses OpenAI-compatible API with custom endpoint
3. **Azure** - Azure OpenAI Service
4. **Google** - Not yet implemented (needs Google AI SDK)

### Implementation Details

```csharp
switch (config.ProviderEnum)
{
    case LLMProviderEnum.DeepSeek:
        // DeepSeek uses OpenAI-compatible API
        var deepSeekClient = new OpenAI.OpenAIClient(
            new ApiKeyCredential(config.ApiKey),
            new OpenAIClientOptions { Endpoint = new Uri(config.Endpoint ?? "https://api.deepseek.com") }
        );
        kernelBuilder.AddOpenAIChatCompletion(modelId, deepSeekClient);
        break;
        
    case LLMProviderEnum.Azure:
        kernelBuilder.AddAzureOpenAIChatCompletion(deploymentName, endpoint, apiKey);
        break;
        
    case LLMProviderEnum.OpenAI:
    default:
        kernelBuilder.AddOpenAIChatCompletion(modelId, apiKey);
        break;
}
```

## Why DeepSeek Uses OpenAI Client

DeepSeek provides an OpenAI-compatible API, which means:
- It follows the same API structure as OpenAI
- It uses the same request/response format
- It can be accessed using the OpenAI SDK with a custom endpoint

This is why Semantic Kernel can use `AddOpenAIChatCompletion` with DeepSeek - we just need to specify the DeepSeek endpoint.

## Configuration Example

When configuring DeepSeek in the LLM settings:
```json
{
  "DeepSeek": {
    "ProviderEnum": "DeepSeek",
    "ModelIdEnum": "DeepSeek", 
    "ModelName": "deepseek-chat",
    "Endpoint": "https://api.deepseek.com",
    "ApiKey": "your-deepseek-api-key"
  }
}
```

## Testing
1. Configure DeepSeek in the LLM settings
2. Select "DeepSeek" in the AI Tool Calling Demo
3. The demo will now use DeepSeek API instead of OpenAI
4. Check logs to confirm: "Kernel built successfully with provider: DeepSeek" 