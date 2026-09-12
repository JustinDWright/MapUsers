namespace CityMapApp.Services;

public interface IGeocodingService
{
    Task<GeoResult?> GeocodeAsync(
        string city,
        string state,
        CancellationToken cancellationToken = default
    );
}

public sealed record GeoResult(double Latitude, double Longitude);
