namespace Shared.Requests;

public sealed record SubmissionRequest(
    string City,
    string State,
    string? Name = null,
    string? EmailAddress = null
);
