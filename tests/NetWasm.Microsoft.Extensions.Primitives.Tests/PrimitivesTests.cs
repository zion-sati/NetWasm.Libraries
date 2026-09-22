// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Primitives;
using TUnit.Assertions;
using TUnit.Core;

namespace NetWasm.Microsoft.Extensions.Primitives.Tests;

public sealed class PrimitivesTests
{
    [Test]
    public async Task StringValuesPreservesScalarAndMultipleValues()
    {
        StringValues scalar = "one";
        await Assert.That(scalar.Count).IsEqualTo(1);
        await Assert.That(scalar[0]).IsEqualTo("one");
        await Assert.That((string?)scalar).IsEqualTo("one");

        StringValues multiple = new string?[] { "one", null, "two" };
        await Assert.That(multiple.Count).IsEqualTo(3);
        await Assert.That(multiple.ToString()).IsEqualTo("one,two");
        await Assert.That(StringValues.Concat(scalar, multiple).Count).IsEqualTo(4);
        await Assert.That(multiple.Equals(new string?[] { "one", null, "two" })).IsTrue();
    }

    [Test]
    public async Task StringValuesMatchesEmptyNullArrayAndHashSemantics()
    {
        StringValues empty = StringValues.Empty;
        StringValues oneNull = new string?[] { null };
        string?[]? emptyArray = empty;
        string?[]? defaultArray = default(StringValues);
        string scalarText = "value";
        StringValues scalar = scalarText;
        var enumerator = scalar.GetEnumerator();
        bool resetRejected = false;
        try
        {
            ((IEnumerator)enumerator).Reset();
        }
        catch (NotSupportedException)
        {
            resetRejected = true;
        }

        await Assert.That(empty.Equals((string?)null)).IsTrue();
        await Assert.That(oneNull.Equals((string?)null)).IsFalse();
        await Assert.That(emptyArray).IsNotNull();
        await Assert.That(emptyArray!.Length).IsEqualTo(0);
        await Assert.That(defaultArray).IsNull();
        await Assert.That(empty.GetHashCode()).IsEqualTo(0);
        await Assert.That(scalar.GetHashCode()).IsEqualTo(scalarText.GetHashCode());
        await Assert.That(resetRejected).IsTrue();
    }

    [Test]
    public async Task StringSegmentAndTokenizerAvoidSubstringForBasicOperations()
    {
        var segment = new StringSegment("  alpha|beta  ", 2, 10);
        await Assert.That(segment.Value).IsEqualTo("alpha|beta");
        await Assert.That(segment.Trim()).IsEqualTo(segment);
        await Assert.That(segment.StartsWith("alpha", StringComparison.Ordinal)).IsTrue();
        await Assert.That(segment.Subsegment(6).Value).IsEqualTo("beta");

        var tokens = new List<string?>();
        foreach (var token in segment.Split(new[] { '|' }))
        {
            tokens.Add(token.Value);
        }

        await Assert.That(tokens.Count).IsEqualTo(2);
        await Assert.That(tokens[0]).IsEqualTo("alpha");
        await Assert.That(tokens[1]).IsEqualTo("beta");
        await Assert.That(StringSegmentComparer.OrdinalIgnoreCase.Equals("A", "a"))
            .IsTrue();
    }

    [Test]
    public async Task CancellationAndCompositeTokensPropagateChanges()
    {
        using var source = new CancellationTokenSource();
        var token = new CancellationChangeToken(source.Token);
        var callbackCount = 0;
        using var registration = token.RegisterChangeCallback(_ => callbackCount++, null);

        source.Cancel();
        await Assert.That(token.HasChanged).IsTrue();
        await Assert.That(callbackCount).IsEqualTo(1);

        using var secondSource = new CancellationTokenSource();
        var composite = new CompositeChangeToken(new IChangeToken[]
        {
            token,
            new CancellationChangeToken(secondSource.Token),
        });
        var compositeCallbackCount = 0;
        using var compositeRegistration = composite.RegisterChangeCallback(
            _ => compositeCallbackCount++,
            null);

        secondSource.Cancel();
        await Assert.That(composite.HasChanged).IsTrue();
        await Assert.That(compositeCallbackCount).IsEqualTo(1);
    }

    [Test]
    public async Task ChangeTokenOnChangeCanBeDisposed()
    {
        using var source = new CancellationTokenSource();
        using var replacement = new CancellationTokenSource();
        var first = true;
        var callbackCount = 0;
        using (ChangeToken.OnChange(
                   () =>
                   {
                       if (first)
                       {
                           first = false;
                           return new CancellationChangeToken(source.Token);
                       }

                       return new CancellationChangeToken(replacement.Token);
                   },
                   () => callbackCount++))
        {
            source.Cancel();
        }

        await Assert.That(callbackCount).IsEqualTo(1);
    }
}
