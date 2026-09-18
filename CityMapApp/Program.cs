using CityMapApp.Components;
using CityMapApp.Data;
using CityMapApp.Models;
using CityMapApp.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Shared.DTOs;
using Shared.Requests;
using Shared.Responses;

var builder = WebApplication.CreateBuilder(args);
const string userTokenCookieName = "citymap-user-token";
const string openStreetMapUrl = "https://nominatim.openstreetmap.org/";
const int maximumDbAttempts = 5;

builder.AddServiceDefaults();

builder.Services.AddRazorComponents().AddInteractiveServerComponents();
builder.Services.AddScoped(sp =>
{
    var navigationManager = sp.GetRequiredService<NavigationManager>();
    var baseAddress = builder.Configuration["InternalBaseAddress"] ?? navigationManager.BaseUri;
    return new HttpClient { BaseAddress = new Uri(baseAddress) };
});

builder.Services.AddDbContext<CityMapDbContext>(options =>
{
    if (builder.Configuration["Database:Provider"] == "InMemory")
    {
        options.UseInMemoryDatabase(builder.Configuration["Database:Name"] ?? "CityMapApp.Tests");
        return;
    }

    options.UseNpgsql(
        builder.Configuration.GetConnectionString("CityMapDb")
            ?? throw new InvalidOperationException(
                "The ConnectionStrings:CityMapDb configuration value is required."
            )
    );
});

builder.Services.AddHttpClient<IGeocodingService, GeocodingService>(client =>
{
    client.BaseAddress = new Uri(openStreetMapUrl);
    client.DefaultRequestHeaders.UserAgent.ParseAdd("CityMapApp/1.0");
});

builder.Services.AddRateLimiter(options =>
{
    options.AddFixedWindowLimiter(
        "submission",
        limiterOptions =>
        {
            limiterOptions.PermitLimit = 5;
            limiterOptions.Window = TimeSpan.FromHours(1);
            limiterOptions.QueueLimit = 0;
        }
    );

    options.AddFixedWindowLimiter(
        "map",
        limiterOptions =>
        {
            limiterOptions.PermitLimit = 60;
            limiterOptions.Window = TimeSpan.FromMinutes(1);
            limiterOptions.QueueLimit = 0;
        }
    );
});

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<CityMapDbContext>();
    InitializeDatabase(dbContext, builder.Configuration.GetValue("Database:UseMigrations", true));
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment()) { }
else
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.UseRateLimiter();
app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>().AddInteractiveServerRenderMode();

app.MapPost(
        "/api/submissions",
        async (
            SubmissionRequest request,
            HttpContext httpContext,
            CityMapDbContext dbContext,
            IGeocodingService geocodingService,
            CancellationToken cancellationToken
        ) =>
        {
            TryValidateSubmission(request);

            var existingToken = httpContext.Request.Cookies[userTokenCookieName];

            if (!string.IsNullOrWhiteSpace(existingToken))
            {
                return Results.Ok(new SubmissionResponse(false, "Already submitted"));
            }

            var alreadySubmitted = await dbContext
                .Submissions.AsNoTracking()
                .AnyAsync(
                    submission =>
                        submission.UserToken == existingToken
                        || submission.EmailAddress == request.EmailAddress,
                    cancellationToken
                );

            if (alreadySubmitted)
            {
                return Results.Ok(new SubmissionResponse(false, "Already submitted"));
            }            

            var geocodeResult = await geocodingService.GeocodeAsync(
                request.City,
                request.State,
                cancellationToken
            );

            if (geocodeResult is null)
            {
                return Results.BadRequest(
                    new SubmissionResponse(false, "Unable to locate city/state")
                );
            }

            var token = string.IsNullOrWhiteSpace(existingToken)
                ? Guid.NewGuid().ToString("N")
                : existingToken;

            var submission = new Submission
            {
                City = request.City.Trim(),
                State = request.State.Trim(),
                Name = TrimToNull(request.Name),
                EmailAddress = TrimToNull(request.EmailAddress),
                Latitude = geocodeResult.Latitude,
                Longitude = geocodeResult.Longitude,
                CreatedUtc = DateTime.UtcNow,
                UserToken = token,
            };

            dbContext.Submissions.Add(submission);
            await dbContext.SaveChangesAsync(cancellationToken);

            httpContext.Response.Cookies.Append(
                userTokenCookieName,
                token,
                new CookieOptions
                {
                    HttpOnly = true,
                    Secure = true,
                    SameSite = SameSiteMode.Lax,
                    Expires = DateTimeOffset.UtcNow.AddYears(5),
                }
            );

            return Results.Ok(new SubmissionResponse(true, null));
        }
    )
    .RequireRateLimiting("submission");

app.MapGet(
    "/api/submissions/me",
    async (
        HttpContext httpContext,
        CityMapDbContext dbContext,
        CancellationToken cancellationToken
    ) =>
    {
        var existingToken = httpContext.Request.Cookies[userTokenCookieName];
        if (string.IsNullOrWhiteSpace(existingToken))
        {
            return Results.Ok(new SubmissionStatusResponse(false));
        }

        var hasSubmitted = await dbContext
            .Submissions.AsNoTracking()
            .AnyAsync(submission => submission.UserToken == existingToken, cancellationToken);

        return Results.Ok(new SubmissionStatusResponse(hasSubmitted));
    }
);

app.MapGet(
    "/api/submissions/all",
    async (CityMapDbContext dbContext, CancellationToken cancellationToken) =>
    {
        var users = await dbContext
            .Submissions.AsNoTracking()
            .OrderBy(submission => submission.Name)
            .Select(submission => new MapSubmissionDto(
                submission.Name,
                submission.EmailAddress,
                submission.City,
                submission.State,
                submission.Latitude,
                submission.Longitude
            ))
            .ToListAsync(cancellationToken);

        return Results.Ok(users);
    }
);

app.MapDelete(
        "/api/submissions/me",
        async (
            HttpContext httpContext,
            CityMapDbContext dbContext,
            CancellationToken cancellationToken
        ) =>
        {
            var existingToken = httpContext.Request.Cookies[userTokenCookieName];

            if (!string.IsNullOrWhiteSpace(existingToken))
            {
                var submission = await dbContext.Submissions.FirstOrDefaultAsync(
                    item => item.UserToken == existingToken,
                    cancellationToken
                );

                if (submission is not null)
                {
                    dbContext.Submissions.Remove(submission);
                    await dbContext.SaveChangesAsync(cancellationToken);
                }
            }

            httpContext.Response.Cookies.Delete(userTokenCookieName);
            return Results.Ok(new SubmissionStatusResponse(false));
        }
    )
    .RequireRateLimiting("submission");

app.MapDelete(
        "/api/submissions/all/force",
        async (
            HttpContext httpContext,
            CityMapDbContext dbContext,
            CancellationToken cancellationToken
        ) =>
        {
            var submissionsToDelete = await dbContext.Submissions.ToListAsync(cancellationToken);

            if (submissionsToDelete.Count == 0)
            {
                return Results.Ok(new DeleteResponse(false, "No map submissions to delete"));
            }

            dbContext.Submissions.RemoveRange(submissionsToDelete);
            await dbContext.SaveChangesAsync(cancellationToken);

            httpContext.Response.Cookies.Delete(userTokenCookieName);

            return Results.Ok(new DeleteResponse(true, "Deleted all map submissions"));
        }
    )
    .RequireRateLimiting("submission");

app.MapGet(
        "/api/map",
        async (CityMapDbContext dbContext, CancellationToken cancellationToken) =>
        {
            var groupedPins = await dbContext
                .Submissions.AsNoTracking()
                .GroupBy(submission => new
                {
                    submission.City,
                    submission.State,
                    submission.Latitude,
                    submission.Longitude,
                })
                .Select(group => new
                {
                    group.Key.City,
                    group.Key.State,
                    group.Key.Latitude,
                    group.Key.Longitude,
                    Count = group.Count(),
                    SharedContactCount = group.Count(submission =>
                        submission.Name != null && submission.EmailAddress != null
                    ),
                })
                .ToListAsync(cancellationToken);

            var pins = groupedPins
                .Select(pin => new MapPinDto(
                    pin.City,
                    pin.State,
                    pin.Latitude,
                    pin.Longitude,
                    pin.Count,
                    pin.SharedContactCount
                ))
                .OrderByDescending(pin => pin.Count)
                .ToList();

            return Results.Ok(pins);
        }
    )
    .RequireRateLimiting("map");

app.MapGet(
        "/api/map/users",
        async (
            string city,
            string state,
            CityMapDbContext dbContext,
            CancellationToken cancellationToken
        ) =>
        {
            if (string.IsNullOrWhiteSpace(city) || string.IsNullOrWhiteSpace(state))
            {
                return Results.BadRequest();
            }

            var users = await dbContext
                .Submissions.AsNoTracking()
                .Where(submission =>
                    submission.City == city
                    && submission.State == state
                    && submission.Name != null
                    && submission.EmailAddress != null
                )
                .OrderBy(submission => submission.Name)
                .Select(submission => new MapUserDto(submission.Name!, submission.EmailAddress!))
                .ToListAsync(cancellationToken);

            return Results.Ok(users);
        }
    )
    .RequireRateLimiting("map");

app.MapGet(
        "/api/map/users/all",
        async (CityMapDbContext dbContext, CancellationToken cancellationToken) =>
        {
            var users = await dbContext
                .Submissions.AsNoTracking()
                .Where(submission => submission.Name != null && submission.EmailAddress != null)
                .OrderBy(submission => submission.Name)
                .Select(submission => new MapUserDto(submission.Name!, submission.EmailAddress!))
                .ToListAsync(cancellationToken);

            return Results.Ok(users);
        }
    )
    .RequireRateLimiting("map");

app.MapDefaultEndpoints();

app.Run();

static void TryValidateSubmission(SubmissionRequest request)
{
    if (string.IsNullOrWhiteSpace(request.City) || string.IsNullOrWhiteSpace(request.State))
    {
        throw new ArgumentException("City and state are required.");
    }

    if (request.City.Trim().Length > 100 || request.State.Trim().Length > 50)
    {
        throw new ArgumentException("City and state must be 100 characters or less.");
    }

    if (!string.IsNullOrWhiteSpace(request.Name) && request.Name.Trim().Length > 100)
    {
        throw new ArgumentException("Name must be 100 characters or less.");
    }

    if (string.IsNullOrWhiteSpace(request.EmailAddress))
    {
        throw new ArgumentException("Email address is required.");
    }

    var emailAddress = request.EmailAddress.Trim();
    if (emailAddress.Length > 255
        || !new System.ComponentModel.DataAnnotations.EmailAddressAttribute().IsValid(emailAddress))
    {
        throw new ArgumentException("Email address is invalid.");
    }
}

static string? TrimToNull(string? value)
{
    return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}

static void InitializeDatabase(CityMapDbContext dbContext, bool useMigrations)
{
    if (!useMigrations)
    {
        dbContext.Database.EnsureCreated();
        return;
    }

    for (var attempt = 1; attempt <= maximumDbAttempts; attempt++)
    {
        try
        {
            dbContext.Database.Migrate();
            return;
        }
        catch (NpgsqlException) when (attempt < maximumDbAttempts)
        {
            Thread.Sleep(TimeSpan.FromSeconds(attempt * 2));
        }
    }

    dbContext.Database.Migrate();
}

public partial class Program { }
