using CityMapApp.Data;
using CityMapApp.Models;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace CityMapApp.Tests.Data;

public sealed class CityMapDbContextTests
{
    [Fact]
    public void Model_RequiresCoreFields_AndAppliesConfiguredLengths()
    {
        using var dbContext = CreateContext();
        var entity = dbContext.Model.FindEntityType(typeof(Submission));

        Assert.NotNull(entity);
        Assert.False(entity.FindProperty(nameof(Submission.City))!.IsNullable);
        Assert.Equal(100, entity.FindProperty(nameof(Submission.City))!.GetMaxLength());
        Assert.False(entity.FindProperty(nameof(Submission.State))!.IsNullable);
        Assert.Equal(50, entity.FindProperty(nameof(Submission.State))!.GetMaxLength());
        Assert.Equal(100, entity.FindProperty(nameof(Submission.Name))!.GetMaxLength());
        Assert.Equal(254, entity.FindProperty(nameof(Submission.EmailAddress))!.GetMaxLength());
        Assert.False(entity.FindProperty(nameof(Submission.UserToken))!.IsNullable);
        Assert.Equal(64, entity.FindProperty(nameof(Submission.UserToken))!.GetMaxLength());
    }

    [Fact]
    public async Task SaveChangesAsync_RejectsDuplicateUserTokens()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<CityMapDbContext>()
            .UseSqlite(connection)
            .Options;

        await using (var setupContext = new CityMapDbContext(options))
        {
            await setupContext.Database.EnsureCreatedAsync();
            setupContext.Submissions.Add(CreateSubmission("same-token"));
            await setupContext.SaveChangesAsync();
        }

        await using var duplicateContext = new CityMapDbContext(options);
        duplicateContext.Submissions.Add(CreateSubmission("same-token"));

        await Assert.ThrowsAsync<DbUpdateException>(() => duplicateContext.SaveChangesAsync());
    }

    [Fact]
    public void Model_DefinesAUniqueIndex_ForUserTokens()
    {
        using var dbContext = CreateContext();
        var entity = dbContext.Model.FindEntityType(typeof(Submission));

        var index = Assert.Single(
            entity!.GetIndexes(),
            index => index.Properties.Select(property => property.Name).SequenceEqual([nameof(Submission.UserToken)])
        );

        Assert.True(index.IsUnique);
    }

    private static CityMapDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<CityMapDbContext>()
            .UseSqlite("Data Source=:memory:")
            .Options;
        return new CityMapDbContext(options);
    }

    private static Submission CreateSubmission(string userToken) => new()
    {
        City = "St. George",
        State = "UT",
        Latitude = 37.0965,
        Longitude = -113.5684,
        CreatedUtc = DateTime.UtcNow,
        UserToken = userToken,
    };
}
