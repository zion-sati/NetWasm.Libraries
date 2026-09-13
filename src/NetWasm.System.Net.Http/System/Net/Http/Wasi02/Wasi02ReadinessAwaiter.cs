// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading;
using System.Threading.Tasks;

namespace System.Net.Http.Wasi02;

internal sealed class Wasi02ReadinessAwaiter : IWasi02ReadinessAwaiter
{
    private readonly IWasi02Reactor _reactor;

    internal Wasi02ReadinessAwaiter(IWasi02Reactor reactor)
    {
        _reactor = reactor ?? throw new ArgumentNullException();
    }

    public Task WaitAsync(
        IWasi02Pollable pollable,
        CancellationToken cancellationToken)
    {
        if (pollable is null)
        {
            throw new ArgumentNullException();
        }

        return WaitCoreAsync(pollable, cancellationToken);
    }

    private async Task WaitCoreAsync(
        IWasi02Pollable pollable,
        CancellationToken cancellationToken)
    {
        using (pollable)
        using (var registration = _reactor.Register(pollable))
        {
            await registration.Completion.WaitAsync(cancellationToken).ConfigureAwait(false);
        }
    }
}
