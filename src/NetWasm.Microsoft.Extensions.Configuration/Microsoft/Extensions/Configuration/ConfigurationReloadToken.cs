using System;
using System.Threading;
using Microsoft.Extensions.Primitives;

namespace Microsoft.Extensions.Configuration;

public sealed class ConfigurationReloadToken : IChangeToken
{
    private readonly CancellationTokenSource _source = new();

    public bool ActiveChangeCallbacks { get; private set; } = true;

    public bool HasChanged => _source.IsCancellationRequested;

    public IDisposable RegisterChangeCallback(Action<object?> callback, object? state)
    {
        ArgumentNullException.ThrowIfNull(callback);
        return _source.Token.Register(static value =>
        {
            var registration = ((CallbackState)value!);
            registration.Callback(registration.State);
        }, new CallbackState(callback, state));
    }

    public void OnReload() => _source.Cancel();

    private sealed record CallbackState(Action<object?> Callback, object? State);
}
