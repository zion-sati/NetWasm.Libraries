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
                parameter.LookupMode))
            .ToArray();
        return new GeneratedCallSiteModel(activation, parameters);
    }
}
