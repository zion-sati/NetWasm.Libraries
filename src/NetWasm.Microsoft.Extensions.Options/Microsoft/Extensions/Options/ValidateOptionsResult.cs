using System;
using System.Collections.Generic;

namespace Microsoft.Extensions.Options;

/// <summary>Describes the result of an options validation.</summary>
public sealed class ValidateOptionsResult
{
    private ValidateOptionsResult(bool succeeded, bool skipped, IEnumerable<string>? failures)
    {
        Succeeded = succeeded;
        Skipped = skipped;
        Failures = failures;
        FailureMessage = failures is null ? null : string.Join("; ", failures);
    }

    public static ValidateOptionsResult Skip { get; } = new(succeeded: false, skipped: true, failures: null);

    public static ValidateOptionsResult Success { get; } = new(succeeded: true, skipped: false, failures: null);

    public bool Succeeded { get; }

    public bool Skipped { get; }

    public bool Failed => Failures is not null;

    public string? FailureMessage { get; }

    public IEnumerable<string>? Failures { get; }

    public static ValidateOptionsResult Fail(string failureMessage)
    {
        ArgumentNullException.ThrowIfNull(failureMessage);
        return new ValidateOptionsResult(false, false, new[] { failureMessage });
    }

    public static ValidateOptionsResult Fail(IEnumerable<string> failures)
    {
        ArgumentNullException.ThrowIfNull(failures);
        var copy = new List<string>(failures);
        return new ValidateOptionsResult(false, false, copy);
    }
}
