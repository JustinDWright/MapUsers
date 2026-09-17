namespace Shared.DTOs;

public sealed record MapSubmissionDto(
    string? Name,
    string? Email,
    string City,
    string State,
    double Latitude,
    double Longitude);