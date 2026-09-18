using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace CityMapApp.Data.Migrations;

/// <inheritdoc />
public partial class InitialPostgresSchema : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "Submissions",
            columns: table => new
            {
                Id = table.Column<int>(type: "integer", nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                City = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                State = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                EmailAddress = table.Column<string>(type: "character varying(254)", maxLength: 254, nullable: true),
                Latitude = table.Column<double>(type: "double precision", nullable: false),
                Longitude = table.Column<double>(type: "double precision", nullable: false),
                CreatedUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                UserToken = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Submissions", x => x.Id);
            });

        migrationBuilder.CreateIndex(
            name: "IX_Submissions_UserToken",
            table: "Submissions",
            column: "UserToken",
            unique: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "Submissions");
    }
}
