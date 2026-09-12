namespace CityMapApp.Models;

public sealed class Submission
{
    public int Id { get; set; }
    public string City { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public string? Name { get; set; }
    public string? EmailAddress { get; set; }
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public DateTime CreatedUtc { get; set; }
    public string UserToken { get; set; } = string.Empty;
}
