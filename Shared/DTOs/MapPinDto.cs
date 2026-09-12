namespace Shared.DTOs;

public sealed record MapPinDto(
    string City,
    string State,
    double Latitude,
    double Longitude,
    int Count,
    int SharedContactCount
);
