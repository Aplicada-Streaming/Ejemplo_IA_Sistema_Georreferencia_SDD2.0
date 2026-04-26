namespace Sgr.Tests.Integration.Common;

/// <summary>
/// xUnit collection that serializes the integration tests using WebApplicationFactory.
/// Reason: Program.cs uses Serilog's static <c>Log.Logger</c> bootstrap, which races
/// across parallel host builds and intermittently fails CreateClient.
/// </summary>
[CollectionDefinition(Name)]
public sealed class WebApiCollection
{
    public const string Name = "WebApi";
}
