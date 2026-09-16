using System.Net;
using System.Net.Http.Json;
using CityMapApp.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Shared.DTOs;
using Shared.Requests;
using Shared.Responses;
using Xunit;

namespace CityMapApp.Tests;

public sealed class ProgramEndpointsTests : IDisposable
{
    private readonly TestApplicationFactory _factory = new();

    [Fact]
    public async Task SubmissionStatus_ReturnsFalse_WhenNoSubmissionCookieIsPresent()
    {
        using var client = CreateClient();

        var response = await client.GetFromJsonAsync<SubmissionStatusResponse>("/api/submissions/me");

        Assert.NotNull(response);
        Assert.False(response.HasSubmitted);
    }

    [Fact]
    public async Task Submit_ReturnsBadRequest_ForAnInvalidRequest()
    {
        using var client = CreateClient();

        var response = await client.PostAsJsonAsync("/api/submissions", new SubmissionRequest("", "UT"));
        var body = await response.Content.ReadFromJsonAsync<SubmissionResponse>();

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(new SubmissionResponse(false, "Invalid city or state"), body);
    }

    [Fact]
    public async Task Submit_PersistsLocation_AndMarksTheCookieAsSubmitted()
    {
        using var client = CreateClient();

        var response = await client.PostAsJsonAsync(
            "/api/submissions",
            new SubmissionRequest(" St. George ", " UT ", " Alice ", " alice@example.test ")
        );
        var body = await response.Content.ReadFromJsonAsync<SubmissionResponse>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(new SubmissionResponse(true, null), body);

        client.DefaultRequestHeaders.Add("Cookie", GetCookie(response));
        var status = await client.GetFromJsonAsync<SubmissionStatusResponse>("/api/submissions/me");
        var duplicate = await client.PostAsJsonAsync(
            "/api/submissions",
            new SubmissionRequest("St. George", "UT")
        );

        Assert.NotNull(status);
        Assert.True(status.HasSubmitted);
        Assert.Equal(new SubmissionResponse(false, "Already submitted"), await duplicate.Content.ReadFromJsonAsync<SubmissionResponse>());
    }

    [Fact]
    public async Task MapEndpoints_GroupLocations_AndReturnOnlySharedContacts()
    {
        await SubmitAsync(new SubmissionRequest("St. George", "UT", "Alice", "alice@example.test"));
        await SubmitAsync(new SubmissionRequest("St. George", "UT"));
        await SubmitAsync(new SubmissionRequest("Cedar City", "UT", "Zed", "zed@example.test"));

        using var client = CreateClient();

        var pins = await client.GetFromJsonAsync<List<MapPinDto>>("/api/map");
        var users = await client.GetFromJsonAsync<List<MapUserDto>>(
            "/api/map/users?city=St.%20George&state=UT"
        );

        Assert.NotNull(pins);
        var stGeorge = Assert.Single(pins, pin => pin.City == "St. George" && pin.State == "UT");
        Assert.Equal(2, stGeorge.Count);
        Assert.Equal(1, stGeorge.SharedContactCount);
        Assert.Equal([new MapUserDto("Alice", "alice@example.test")], users);
    }

    private HttpClient CreateClient() => _factory.CreateClient(
        new WebApplicationFactoryClientOptions { BaseAddress = new Uri("https://localhost") }
    );

    public void Dispose() => _factory.Dispose();

    private async Task SubmitAsync(SubmissionRequest request)
    {
        using var client = CreateClient();
        using var response = await client.PostAsJsonAsync("/api/submissions", request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    private static string GetCookie(HttpResponseMessage response)
    {
        var setCookie = Assert.Single(response.Headers.GetValues("Set-Cookie"));
        return setCookie.Split(';', 2)[0];
    }

    public sealed class TestApplicationFactory : WebApplicationFactory<Program>
    {
        private readonly string _databasePath = Path.Combine(
            Path.GetTempPath(),
            $"citymap-tests-{Guid.NewGuid():N}.db"
        );

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseSetting("ConnectionStrings:CityMapDb", $"Data Source={_databasePath}");
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<IGeocodingService>();
                services.AddSingleton<IGeocodingService>(new FakeGeocodingService());
            });
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (disposing)
            {
                try
                {
                    File.Delete(_databasePath);
                }
                catch (IOException)
                {
                    // SQLite can retain a short-lived handle after the test host shuts down.
                }
            }
        }
    }

    private sealed class FakeGeocodingService : IGeocodingService
    {
        public Task<GeoResult?> GeocodeAsync(string city, string state, CancellationToken cancellationToken = default) =>
            Task.FromResult<GeoResult?>(new(37.0965, -113.5684));
    }
}
