// Copyright (c) 2026 Zion Sati and NetWasm contributors (https://github.com/zion-sati/NetWasm).
// SPDX-License-Identifier: MIT

namespace NetWasm.Microsoft.Extensions.DependencyInjection.Generator;

internal sealed class GeneratedCallSiteBuilder : IGeneratedCallSiteBuilder
{
    public GeneratedCallSiteModel Build(ActivationModel activation)
    {
        Guard.NotNull(activation, nameof(activation));
        var parameters = activation.Facts.Parameters
            .Select(parameter => new GeneratedCallSiteParameter(
                parameter.TypeDisplay,
                parameter.HasDefaultValue,
                parameter.DefaultExpression,
                parameter.ServiceKeyExpression,
                parameter.LookupMode,
                parameter.ConstantExpression))
            .ToArray();
        return new GeneratedCallSiteModel(activation, parameters);
    }
}
