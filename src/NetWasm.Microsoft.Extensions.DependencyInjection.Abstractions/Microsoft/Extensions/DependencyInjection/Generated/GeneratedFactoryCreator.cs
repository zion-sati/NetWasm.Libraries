using System;
using System.Collections.Generic;

namespace Microsoft.Extensions.DependencyInjection.Generated;

internal interface IGeneratedFactoryCreator
{
    GeneratedFactorySelection Create(Type implementationType, IReadOnlyList<Type> suppliedTypes);
}

internal sealed class GeneratedFactoryCreator : IGeneratedFactoryCreator
{
    private readonly IGeneratedFactoryShapeSelector _selector;

    internal GeneratedFactoryCreator(IGeneratedFactoryShapeSelector selector)
    {
        _selector = selector ?? throw new ArgumentNullException(nameof(selector));
    }

    public GeneratedFactorySelection Create(Type implementationType, IReadOnlyList<Type> suppliedTypes) =>
        _selector.Select(implementationType, suppliedTypes);
}
