// Copyright (c) 2026 Zion Sati and NetWasm contributors (https://github.com/zion-sati/NetWasm).
// SPDX-License-Identifier: MIT

using Microsoft.Extensions.DependencyInjection;

namespace NetWasm.Microsoft.Extensions.DependencyInjection.StockCompiledLibrary;

public interface IStockGreeting
{
    string Text { get; }
}

public static class StockGreetingServiceCollectionExtensions
{
    public static IServiceCollection AddStockGreeting(this IServiceCollection services, string text) =>
        services.AddSingleton<IStockGreeting>(_ => new StockGreeting(text));

    private sealed class StockGreeting(string text) : IStockGreeting
    {
        public string Text { get; } = text;
    }
}
