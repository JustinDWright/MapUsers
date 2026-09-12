using System.Globalization;
using System.Text.Json.Serialization;

namespace CityMapApp.Services;

public sealed class GeocodingService(HttpClient httpClient) : IGeocodingService
{
    public async Task<GeoResult?> GeocodeAsync(
        string city,
        string state,
        CancellationToken cancellationToken = default
    )
    {
        var requestUri =
            $"search?city={Uri.EscapeDataString(city)}&state={Uri.EscapeDataString(state)}&format=jsonv2&limit=1";

        var response = await httpClient.GetAsync(requestUri, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        var payload = await response.Content.ReadFromJsonAsync<List<GeoCodeResult>>(
            cancellationToken
        );

        var firstResult = payload?.FirstOrDefault();
        if (firstResult is null)
        {
            return null;
        }

        var latParsed = double.TryParse(
            firstResult.Latitude,
            NumberStyles.Float,
            CultureInfo.InvariantCulture,
            out var latitude
        );

        var longParsed = double.TryParse(
            firstResult.Longitude,
            NumberStyles.Float,
            CultureInfo.InvariantCulture,
            out var longitude
        );

        if (!latParsed || !longParsed)
        {
            return null;
        }

        return new GeoResult(latitude, longitude);
    }

    private sealed record GeoCodeResult(
        [property: JsonPropertyName("lat")] string Latitude,
        [property: JsonPropertyName("lon")] string Longitude
    );
}
