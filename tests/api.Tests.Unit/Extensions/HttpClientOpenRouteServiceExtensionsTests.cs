using api.Extensions;
using api.Options;
using TUnit.Assertions.Extensions;

namespace api.Tests.Unit.Extensions;

public class HttpClientOpenRouteServiceExtensionsTests
{
    [Test]
    public async Task ConfigureForOpenRouteService_SetsBaseAddress_WhenCalled()
    {
        // Arrange
        using var client = new HttpClient();
        var options = new OpenRouteServiceOptions
        {
            Title = "OpenRouteService",
            BaseUrl = "https://api.openrouteservice.org",
            ApiKey = "test-api-key"
        };

        // Act
        var result = client.ConfigureForOpenRouteService(options);

        // Assert
        await Assert.That(result.BaseAddress).IsNotNull();
        await Assert.That(result.BaseAddress!.ToString()).IsEqualTo(options.BaseUrl + "/");
    }

    [Test]
    public async Task ConfigureForOpenRouteService_SetsAuthorizationHeader_WhenCalled()
    {
        // Arrange
        using var client = new HttpClient();
        var options = new OpenRouteServiceOptions
        {
            Title = "OpenRouteService",
            BaseUrl = "https://api.openrouteservice.org",
            ApiKey = "test-api-key-12345"
        };

        // Act
        var result = client.ConfigureForOpenRouteService(options);

        // Assert
        await Assert.That(result.DefaultRequestHeaders.Authorization).IsNotNull();
        await Assert.That(result.DefaultRequestHeaders.Authorization!.Scheme).IsEqualTo("Bearer");
        await Assert.That(result.DefaultRequestHeaders.Authorization.Parameter).IsEqualTo(options.ApiKey);
    }

    [Test]
    public async Task ConfigureForOpenRouteService_SetsAcceptHeader_WhenCalled()
    {
        // Arrange
        using var client = new HttpClient();
        var options = new OpenRouteServiceOptions
        {
            Title = "OpenRouteService",
            BaseUrl = "https://api.openrouteservice.org",
            ApiKey = "test-api-key"
        };

        // Act
        var result = client.ConfigureForOpenRouteService(options);

        // Assert
        await Assert.That(result.DefaultRequestHeaders.Accept).HasCount().EqualTo(1);
        await Assert.That(result.DefaultRequestHeaders.Accept.Any(h =>
            h.MediaType == "application/json")).IsTrue();
    }

    [Test]
    public async Task ConfigureForOpenRouteService_ReturnsSameInstance_WhenCalled()
    {
        // Arrange
        using var client = new HttpClient();
        var options = new OpenRouteServiceOptions
        {
            Title = "OpenRouteService",
            BaseUrl = "https://api.openrouteservice.org",
            ApiKey = "test-api-key"
        };

        // Act
        var result = client.ConfigureForOpenRouteService(options);

        // Assert
        await Assert.That(ReferenceEquals(result, client)).IsTrue();
    }
}
