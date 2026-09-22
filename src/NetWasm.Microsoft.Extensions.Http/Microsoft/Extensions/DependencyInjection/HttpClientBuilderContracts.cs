using System.Collections.Generic;

namespace Microsoft.Extensions.DependencyInjection;

public interface IHttpClientBuilder
{
    string Name { get; }

    IServiceCollection Services { get; }
}
