using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.Extensions.Http;

internal sealed class DefaultHttpClientFactory : IHttpClientFactory, IHttpMessageHandlerFactory, IDisposable
{
    private readonly IServiceProvider _services;
    private readonly HttpClientFactoryOptionsStore _options;
    private readonly Dictionary<string, HandlerEntry> _entries = new(StringComparer.Ordinal);
    private readonly List<HandlerEntry> _expiredEntries = new();
    private bool _disposed;

    internal DefaultHttpClientFactory(IServiceProvider services, HttpClientFactoryOptionsStore options)
    {
        _services = services ?? throw new ArgumentNullException(nameof(services));
        _options = options ?? throw new ArgumentNullException(nameof(options));
    }

    public HttpClient CreateClient(string name)
    {
        ArgumentNullException.ThrowIfNull(name);
        HandlerLease lease = CreateLease(name);
        try
        {
            var client = new HttpClient(new LifetimeTrackingHandler(lease), disposeHandler: true);
            foreach (Action<HttpClient> action in lease.Options.HttpClientActions)
            {
                action(client);
            }

            foreach (Action<IServiceProvider, HttpClient> action in lease.Options.ServiceProviderHttpClientActions)
            {
                action(_services, client);
            }

            return client;
        }
        catch
        {
            lease.Release();
            throw;
        }
    }

    public HttpMessageHandler CreateHandler(string name)
    {
        ArgumentNullException.ThrowIfNull(name);
        return new LifetimeTrackingHandler(CreateLease(name));
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        foreach (HandlerEntry entry in _entries.Values)
        {
            entry.Expire(force: true);
        }
        foreach (HandlerEntry entry in _expiredEntries)
        {
            entry.Expire(force: true);
        }

        _entries.Clear();
        _expiredEntries.Clear();
    }

    private HandlerLease CreateLease(string name)
    {
        ThrowIfDisposed();
        CleanupExpiredEntries();
        HttpClientFactoryOptions options = _options.GetOrCreate(name);
        if (_entries.TryGetValue(name, out HandlerEntry? existing) && !existing.IsExpired)
        {
            return existing.Acquire();
        }

        if (existing is not null)
        {
            existing.Expire(force: false);
            _entries.Remove(name);
            _expiredEntries.Add(existing);
        }

        IServiceScope? scope = null;
        HttpMessageHandler? handler = null;
        HandlerEntry? entry = null;
        try
        {
            IServiceProvider handlerServices = _services;
            if (!options.SuppressHandlerScope)
            {
                var scopeFactory = _services.GetService(typeof(IServiceScopeFactory)) as IServiceScopeFactory
                    ?? throw new InvalidOperationException("IServiceScopeFactory is required for HTTP handler scopes.");
                scope = scopeFactory.CreateScope();
                handlerServices = scope.ServiceProvider;
            }

            var builder = new DefaultHttpMessageHandlerBuilder(handlerServices) { Name = name };
            foreach (Action<HttpMessageHandlerBuilder> action in options.HttpMessageHandlerBuilderActions)
            {
                action(builder);
            }

            foreach (Action<HttpMessageHandlerBuilder> action in options.LoggingBuilderActions)
            {
                action(builder);
            }

            handler = builder.Build();
            entry = new HandlerEntry(handler, options, scope);
            _entries.Add(name, entry);
            return entry.Acquire();
        }
        catch
        {
            if (entry is not null)
            {
                entry.Expire(force: true);
            }
            else
            {
                handler?.Dispose();
                scope?.Dispose();
            }

            throw;
        }
    }

    private void ThrowIfDisposed()
    {
        ObjectDisposedException.ThrowIf(_disposed, nameof(DefaultHttpClientFactory));
    }

    private void CleanupExpiredEntries()
    {
        for (int index = _expiredEntries.Count - 1; index >= 0; index--)
        {
            if (_expiredEntries[index].TryDisposeIfReady())
            {
                _expiredEntries.RemoveAt(index);
            }
        }
    }

    private sealed class HandlerEntry
    {
        private readonly HttpMessageHandler _handler;
        private readonly IServiceScope? _scope;
        private readonly DateTimeOffset? _expiresAt;
        private readonly List<WeakReference<HandlerLease>> _leases = new();
        private bool _expired;
        private bool _disposed;

        internal HandlerEntry(HttpMessageHandler handler, HttpClientFactoryOptions options, IServiceScope? scope)
        {
            _handler = handler ?? throw new ArgumentNullException(nameof(handler));
            Options = options ?? throw new ArgumentNullException(nameof(options));
            _scope = scope;
            _expiresAt = options.HandlerLifetime == HttpClientFactoryOptions.InfiniteHandlerLifetime
                ? null
                : TimeProvider.System.GetUtcNow() + options.HandlerLifetime;
        }

        internal HttpClientFactoryOptions Options { get; }

        internal bool IsExpired => _expired || _expiresAt is DateTimeOffset expiresAt && expiresAt <= TimeProvider.System.GetUtcNow();

        internal HandlerLease Acquire()
        {
            if (IsExpired)
            {
                throw new InvalidOperationException("The HTTP message handler has expired.");
            }

            PruneReleasedLeases();
            var lease = new HandlerLease(this, _handler, Options);
            _leases.Add(new WeakReference<HandlerLease>(lease));
            return lease;
        }

        internal void Release()
        {
            DisposeIfReady();
        }

        internal void Expire(bool force)
        {
            _expired = true;
            if (force)
            {
                DisposeNow();
            }
            else
            {
                DisposeIfReady();
            }
        }

        private void DisposeIfReady()
        {
            _ = TryDisposeIfReady();
        }

        internal bool TryDisposeIfReady()
        {
            PruneReleasedLeases();

            if (_expired && _leases.Count == 0)
            {
                DisposeNow();
            }

            return _disposed;
        }

        private void PruneReleasedLeases()
        {
            for (int index = _leases.Count - 1; index >= 0; index--)
            {
                if (!_leases[index].TryGetTarget(out HandlerLease? lease) || lease.IsReleased)
                {
                    _leases.RemoveAt(index);
                }
            }
        }

        private void DisposeNow()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            try
            {
                _handler.Dispose();
            }
            finally
            {
                _scope?.Dispose();
            }
        }
    }

    private sealed class HandlerLease
    {
        private HandlerEntry? _entry;

        internal HandlerLease(HandlerEntry entry, HttpMessageHandler handler, HttpClientFactoryOptions options)
        {
            _entry = entry;
            Handler = handler;
            Options = options;
        }

        internal HttpMessageHandler Handler { get; }

        internal HttpClientFactoryOptions Options { get; }

        internal bool IsReleased => _entry is null;

        internal void Release()
        {
            HandlerEntry? entry = _entry;
            if (entry is null)
            {
                return;
            }

            _entry = null;
            entry.Release();
        }
    }

    private sealed class LifetimeTrackingHandler : DelegatingHandler
    {
        private HandlerLease? _lease;

        internal LifetimeTrackingHandler(HandlerLease lease)
            : base(lease?.Handler ?? throw new ArgumentNullException(nameof(lease)))
        {
            _lease = lease;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                HandlerLease? lease = _lease;
                _lease = null;
                lease?.Release();
            }
        }
    }
}
