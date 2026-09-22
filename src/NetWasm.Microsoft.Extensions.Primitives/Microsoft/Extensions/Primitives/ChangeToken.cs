// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Extensions.Primitives;

public static class ChangeToken
{
    public static IDisposable OnChange(
        Func<IChangeToken?> changeTokenProducer,
        Action changeTokenConsumer)
    {
        ArgumentNullException.ThrowIfNull(changeTokenProducer);
        ArgumentNullException.ThrowIfNull(changeTokenConsumer);
        return new SyncRegistration<Action>(changeTokenProducer, static action => action(), changeTokenConsumer);
    }

    public static IDisposable OnChange<TState>(
        Func<IChangeToken?> changeTokenProducer,
        Action<TState> changeTokenConsumer,
        TState state)
    {
        ArgumentNullException.ThrowIfNull(changeTokenProducer);
        ArgumentNullException.ThrowIfNull(changeTokenConsumer);
        return new SyncRegistration<TState>(changeTokenProducer, changeTokenConsumer, state);
    }

    public static IDisposable OnChange(
        Func<IChangeToken?> changeTokenProducer,
        Func<Task> changeTokenConsumer)
    {
        ArgumentNullException.ThrowIfNull(changeTokenProducer);
        ArgumentNullException.ThrowIfNull(changeTokenConsumer);
        return new AsyncRegistration<Func<Task>>(changeTokenProducer, static consumer => consumer(), changeTokenConsumer);
    }

    public static IDisposable OnChange<TState>(
        Func<IChangeToken?> changeTokenProducer,
        Func<TState, Task> changeTokenConsumer,
        TState state)
    {
        ArgumentNullException.ThrowIfNull(changeTokenProducer);
        ArgumentNullException.ThrowIfNull(changeTokenConsumer);
        return new AsyncRegistration<TState>(changeTokenProducer, changeTokenConsumer, state);
    }

    private abstract class Registration<TState> : IDisposable
    {
        private readonly object _lock = new();
        private IDisposable? _registration;
        private bool _disposed;

        protected Registration(Func<IChangeToken?> producer, TState state)
        {
            Producer = producer;
            State = state;
        }

        protected Func<IChangeToken?> Producer { get; }

        protected TState State { get; }

        protected abstract void OnTokenChanged();

        protected void Register(IChangeToken? token)
        {
            if (token is null)
            {
                return;
            }

            lock (_lock)
            {
                if (_disposed)
                {
                    return;
                }
            }

            var registration = token.RegisterChangeCallback(
                static state => ((Registration<TState>)state!).OnTokenChanged(),
                this);
            if (token.HasChanged && token.ActiveChangeCallbacks)
            {
                registration.Dispose();
                return;
            }

            lock (_lock)
            {
                if (_disposed)
                {
                    registration.Dispose();
                }
                else
                {
                    _registration = registration;
                }
            }
        }

        public void Dispose()
        {
            IDisposable? registration;
            lock (_lock)
            {
                if (_disposed)
                {
                    return;
                }

                _disposed = true;
                registration = _registration;
                _registration = null;
            }

            registration?.Dispose();
        }
    }

    private sealed class SyncRegistration<TState> : Registration<TState>
    {
        private readonly Action<TState> _consumer;

        public SyncRegistration(
            Func<IChangeToken?> producer,
            Action<TState> consumer,
            TState state)
            : base(producer, state)
        {
            _consumer = consumer;
            Register(producer());
        }

        protected override void OnTokenChanged()
        {
            var next = Producer();
            try
            {
                _consumer(State);
            }
            finally
            {
                Register(next);
            }
        }
    }

    private sealed class AsyncRegistration<TState> : Registration<TState>
    {
        private readonly Func<TState, Task> _consumer;

        public AsyncRegistration(
            Func<IChangeToken?> producer,
            Func<TState, Task> consumer,
            TState state)
            : base(producer, state)
        {
            _consumer = consumer;
            Register(producer());
        }

        protected override void OnTokenChanged()
        {
            var next = Producer();
            Task task;
            try
            {
                task = _consumer(State) ?? throw new InvalidOperationException();
            }
            catch
            {
                Register(next);
                throw;
            }

            if (task.Status == TaskStatus.RanToCompletion)
            {
                Register(next);
            }
            else
            {
                _ = AwaitConsumer(task, next);
            }
        }

        private async Task AwaitConsumer(Task task, IChangeToken? next)
        {
            try
            {
                await task.ConfigureAwait(false);
            }
            finally
            {
                Register(next);
            }
        }
    }
}
