namespace Microsoft.Extensions.Configuration;

// Internal bridge used by the source-generated binder to distinguish an absent
// key from a key whose provider value is explicitly null.
internal interface IConfigurationSectionValueAccessor
{
    bool TryGetValue(out string? value);
}
