using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using TUnit.Assertions;
using TUnit.Core;

namespace NetWasm.Microsoft.Extensions.Http.Tests;

public sealed class HttpClientFactoryTests
{
    [Test]
    public async Task NamedClientAppliesBaseAddressAndHandlerOrder()
    {
        var events = new List<string>();
        var services = new ServiceCollection();
        services.AddHttpClient("api")
            .ConfigureHttpClient(client => client.BaseAddress = new Uri("https://example.test/root/"))
            .ConfigurePrimaryHttpMessageHandler(() => new RecordingHandler("primary", events))
            .AddHttpMessageHandler(() => new RecordingHandler("outer", events))
            .AddHttpMessageHandler(() => new RecordingHandler("inner", events));

        using var provider = services.BuildServiceProvider();
        var factory = provider.GetRequiredService<IHttpClientFactory>();
        using HttpClient client = factory.CreateClient("api");
        using HttpResponseMessage response = await client.GetAsync("items");

        await Assert.That(response.IsSuccessStatusCode).IsTrue();
        string[] expected =
        {
            "outer:before", "inner:before", "primary:before", "primary", "primary:after",
            "inner:after", "outer:after",
        };
        await Assert.That(events.Count).IsEqualTo(expected.Length);
        for (var index = 0; index < expected.Length; index++)
        {
            await Assert.That(events[index]).IsEqualTo(expected[index]);
        }
    }

    [Test]
    public async Task NamedHandlersAreReusedUntilLifetimeExpires()
    {
        int primaryCreations = 0;
        var services = new ServiceCollection();
        services.AddHttpClient("stable")
            .SetHandlerLifetime(TimeSpan.FromMinutes(5))
            .ConfigurePrimaryHttpMessageHandler(() =>
            {
                primaryCreations++;
                return new RecordingHandler("primary", new List<string>());
            });

        using var provider = services.BuildServiceProvider();
        var factory = provider.GetRequiredService<IHttpClientFactory>();
        using (HttpClient first = factory.CreateClient("stable"))
        using (HttpClient second = factory.CreateClient("stable"))
        {
            await first.GetAsync("https://example.test/one");
            await second.GetAsync("https://example.test/two");
        }

        await Assert.That(primaryCreations).IsEqualTo(1);
    }

    [Test]
    public async Task TypedClientUsesFactoryConfiguredHttpClient()
    {
        var services = new ServiceCollection();
        services.AddHttpClient<TypedApi>(client => client.BaseAddress = new Uri("https://typed.test/"));

        using var provider = services.BuildServiceProvider();
        TypedApi api = provider.GetRequiredService<TypedApi>();

        await Assert.That(api.Client.BaseAddress!.ToString()).IsEqualTo("https://typed.test/");
    }

    [Test]
    public async Task GenericHandlerUsesGeneratedActivation()
    {
        var observed = false;
        var services = new ServiceCollection();
        services.AddTransient<GeneratedDelegatingHandler>();
        services.AddHttpClient("generated")
            .ConfigurePrimaryHttpMessageHandler(() => new HeaderCheckingHandler(
                request => observed = request.Headers.Contains("X-Generated")))
            .AddHttpMessageHandler<GeneratedDelegatingHandler>();

        using var provider = services.BuildServiceProvider();
        using HttpClient client = provider.GetRequiredService<IHttpClientFactory>().CreateClient("generated");
        using HttpResponseMessage response = await client.GetAsync("https://example.test/generated");

        await Assert.That(response.IsSuccessStatusCode).IsTrue();
        await Assert.That(observed).IsTrue();
    }

    [Test]
    public async Task UnnamedTypedClientsKeepIndependentConfigurationAndInterfaceActivation()
    {
        var services = new ServiceCollection();
        services.AddHttpClient<TypedApi>(client => client.BaseAddress = new Uri("https://first.test/"));
        services.AddHttpClient<ISecondApi, SecondApi>(client => client.BaseAddress = new Uri("https://second.test/"));
        using var provider = services.BuildServiceProvider();
        TypedApi first = provider.GetRequiredService<TypedApi>();
        ISecondApi second = provider.GetRequiredService<ISecondApi>();
        var allFirst = provider.GetServices<TypedApi>();
        await Assert.That(first.Client.BaseAddress!.Host).IsEqualTo("first.test");
        await Assert.That(second.Client.BaseAddress!.Host).IsEqualTo("second.test");
        await Assert.That(new List<TypedApi>(allFirst).Count).IsEqualTo(1);
    }

#if NETWASM
    // NetWasm cleans expired handlers during factory operations. Desktop .NET uses a
    // periodic cleanup timer, so this immediate-disposal assertion only describes
    // the NetWasm implementation's deterministic cleanup behavior.
    [Test]
    public async Task ExpiredAbandonedClientDoesNotPermanentlyPinHandler()
    {
        var handlers = new List<DisposableHandler>();
        var services = new ServiceCollection();
        services.AddHttpClient("expiring")
            .SetHandlerLifetime(TimeSpan.FromSeconds(1))
            .ConfigurePrimaryHttpMessageHandler(() =>
            {
                var handler = new DisposableHandler();
                handlers.Add(handler);
                return handler;
            });
        using var provider = services.BuildServiceProvider();
        var factory = provider.GetRequiredService<IHttpClientFactory>();
        CreateAndAbandon(factory);
        await Task.Delay(TimeSpan.FromMilliseconds(1100));
        GC.Collect();
        GC.WaitForPendingFinalizers();
        using HttpClient replacement = factory.CreateClient("expiring");
        await Assert.That(handlers.Count).IsEqualTo(2);
        await Assert.That(handlers[0].Disposed).IsTrue();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void CreateAndAbandon(IHttpClientFactory factory) =>
        _ = factory.CreateClient("expiring");
#endif

    public sealed class TypedApi
    {
        public TypedApi(HttpClient client) => Client = client;

        public HttpClient Client { get; }
    }

    public interface ISecondApi { HttpClient Client { get; } }

    public sealed class SecondApi : ISecondApi
    {
        public SecondApi(HttpClient client) => Client = client;
        public HttpClient Client { get; }
    }

    public sealed class GeneratedDelegatingHandler : DelegatingHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            request.Headers.Add("X-Generated", "yes");
            return base.SendAsync(request, cancellationToken);
        }
    }

    private sealed class HeaderCheckingHandler(Action<HttpRequestMessage> observe) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            observe(request);
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
        }
    }

#if NETWASM
    // Used by the NetWasm-specific operation-driven cleanup regression above.
    private sealed class DisposableHandler : HttpMessageHandler
    {
        internal bool Disposed { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));

        protected override void Dispose(bool disposing)
        {
            if (disposing) Disposed = true;
            base.Dispose(disposing);
        }
    }
#endif

    private sealed class RecordingHandler(string name, List<string> events) : DelegatingHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            events.Add(name + ":before");
            HttpResponseMessage response;
            if (InnerHandler is null)
            {
                events.Add(name);
                response = new HttpResponseMessage(HttpStatusCode.OK);
            }
            else
            {
                response = await base.SendAsync(request, cancellationToken);
            }

            events.Add(name + ":after");
            return response;
        }
    }
}
