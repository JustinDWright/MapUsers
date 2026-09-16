using System.Net;
using CityMapApp.Services;
using Xunit;

namespace CityMapApp.Tests.Services;

public sealed class GeocodingServiceTests
{
    [Fact]
    public async Task GeocodeAsync_ReturnsCoordinates_AndEscapesQueryValues()
    {
        var handler = new StubHttpMessageHandler(_ => JsonResponse("""
            [{ "lat": "37.0965", "lon": "-113.5684" }]
            """));
        using var client = new HttpClient(handler) { BaseAddress = new Uri("https://example.test/") };
        var service = new GeocodingService(client);

        var result = await service.GeocodeAsync("St. George & Washington", "UT");

        Assert.Equal(new GeoResult(37.0965, -113.5684), result);
        Assert.Equal(
            "/search?city=St.%20George%20%26%20Washington&state=UT&format=jsonv2&limit=1",
            handler.RequestUri!.PathAndQuery
        );
    }

    [Fact]
    public async Task GeocodeAsync_ReturnsNull_WhenTheServiceFails()
    {
        using var client = new HttpClient(new StubHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.ServiceUnavailable)))
        {
            BaseAddress = new Uri("https://example.test/")
        };

        var result = await new GeocodingService(client).GeocodeAsync("St. George", "UT");

        Assert.Null(result);
    }

    [Theory]
    [InlineData("[]")]
    [InlineData("[{ \"lat\": \"not-a-number\", \"lon\": \"-113.5684\" }]")]
    [InlineData("[{ \"lat\": \"37.0965\", \"lon\": \"not-a-number\" }]")]
    public async Task GeocodeAsync_ReturnsNull_WhenThePayloadHasNoUsableCoordinates(string json)
    {
        using var client = new HttpClient(new StubHttpMessageHandler(_ => JsonResponse(json)))
        {
            BaseAddress = new Uri("https://example.test/")
        };

        var result = await new GeocodingService(client).GeocodeAsync("St. George", "UT");

        Assert.Null(result);
    }

    private static HttpResponseMessage JsonResponse(string json) => new(HttpStatusCode.OK)
    {
        Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json")
    };

    private sealed class StubHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> responseFactory)
        : HttpMessageHandler
    {
        public Uri? RequestUri { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            RequestUri = request.RequestUri;
            return Task.FromResult(responseFactory(request));
        }
    }
}
