using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace identityserver.api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddPkceToAuthorizationCode : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CodeChallenge",
                table: "AuthorizationCodes",
                type: "varchar(512)",
                maxLength: 512,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "CodeChallengeMethod",
                table: "AuthorizationCodes",
                type: "varchar(10)",
                maxLength: 10,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CodeChallenge",
                table: "AuthorizationCodes");

            migrationBuilder.DropColumn(
                name: "CodeChallengeMethod",
                table: "AuthorizationCodes");
        }
    }
}
