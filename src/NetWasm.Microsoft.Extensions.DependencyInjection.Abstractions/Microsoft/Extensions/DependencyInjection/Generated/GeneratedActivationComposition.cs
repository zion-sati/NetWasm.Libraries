// Copyright (c) 2026 Zion Sati and NetWasm contributors (https://github.com/zion-sati/NetWasm).
// SPDX-License-Identifier: MIT

using System;

namespace Microsoft.Extensions.DependencyInjection.Generated;

/// <summary>Composes each activation capability independently for the public facade.</summary>
internal static class GeneratedActivationComposition
{
    internal static IGeneratedFactoryCreator CreateFactoryCreator(GeneratedActivationLookup lookup)
    {
        ArgumentNullException.ThrowIfNull(lookup);
        var selector = new GeneratedFactoryShapeSelector(
            new GeneratedActivationRegistrationReader(lookup),
            new GeneratedActivationAssignabilityReader(lookup));
        return new GeneratedFactoryCreator(selector);
    }
}
