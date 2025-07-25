using Microsoft.SemanticKernel;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace Aevatar.Workshop.Host;

public static class DemoPlugin
{
    // ImmutableDictionary ensures thread safety for concurrent reads
    private static readonly ImmutableDictionary<string, double> StateGDP2024 =
        new Dictionary<string, double>
        {
            { "NY", 2200000.0 }, // New York
            { "CA", 4100000.0 }, // California
            { "TX", 2700000.0 }, // Texas
            // 可扩展更多州
        }.ToImmutableDictionary();

    [KernelFunction("AddNumbers")]
    public static Task<int> AddNumbersAsync(int a, int b)
        => Task.FromResult(a + b);

    [KernelFunction("GetUSGDP2024")]
    public static Task<double> GetUSGDP2024Async()
        => Task.FromResult(28500000.0); // 单位：百万美元（示例数据）

    [KernelFunction("GetNYGDP2024")]
    public static Task<double> GetNYGDP2024Async()
        => Task.FromResult(2200000.0); // 单位：百万美元（示例数据）

    [KernelFunction("GetCAGDP2024")]
    public static Task<double> GetCAGDP2024Async()
        => Task.FromResult(4100000.0); // 单位：百万美元（示例数据）

    [KernelFunction("CalculatePercentage")]
    public static Task<double> CalculatePercentageAsync(double part, double whole)
        => Task.FromResult(whole != 0 ? (part / whole) * 100.0 : 0.0);

    [KernelFunction("GetStateGDP2024")]
    public static Task<double> GetStateGDP2024Async(string stateCode)
    {
        if (StateGDP2024.TryGetValue(stateCode.ToUpperInvariant(), out var gdp))
        {
            return Task.FromResult(gdp);
        }

        return Task.FromResult(0.0); // 未知州返回0
    }
}