namespace Microsoft.Extensions.Caching.Memory;

public delegate void PostEvictionDelegate(object key, object? value, EvictionReason reason, object? state);

public sealed class PostEvictionCallbackRegistration
{
    public PostEvictionDelegate? EvictionCallback { get; set; }

    public object? State { get; set; }
}
