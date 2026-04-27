using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace lrfdev.auth.api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddOAuthAuthorizationCodePkce : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "oauth_clients",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    ClientId = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ClientSecretHash = table.Column<string>(type: "varchar(512)", maxLength: 512, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Name = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    RedirectUris = table.Column<string>(type: "varchar(2000)", maxLength: 2000, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    AllowedScopes = table.Column<string>(type: "varchar(1000)", maxLength: 1000, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    RequirePkce = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    IsConfidentialClient = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    IsActive = table.Column<bool>(type: "tinyint(1)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_oauth_clients", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "oauth_authorization_codes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    Code = table.Column<string>(type: "varchar(300)", maxLength: 300, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ClientId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    UserId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    RedirectUri = table.Column<string>(type: "varchar(1000)", maxLength: 1000, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Scope = table.Column<string>(type: "varchar(1000)", maxLength: 1000, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CodeChallenge = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CodeChallengeMethod = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ExpiresAtUtc = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    ConsumedAtUtc = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_oauth_authorization_codes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_oauth_authorization_codes_oauth_clients_ClientId",
                        column: x => x.ClientId,
                        principalTable: "oauth_clients",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_oauth_authorization_codes_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_oauth_authorization_codes_ClientId",
                table: "oauth_authorization_codes",
                column: "ClientId");

            migrationBuilder.CreateIndex(
                name: "IX_oauth_authorization_codes_Code",
                table: "oauth_authorization_codes",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_oauth_authorization_codes_ExpiresAtUtc",
                table: "oauth_authorization_codes",
                column: "ExpiresAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_oauth_authorization_codes_UserId",
                table: "oauth_authorization_codes",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_oauth_clients_ClientId",
                table: "oauth_clients",
                column: "ClientId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "oauth_authorization_codes");

            migrationBuilder.DropTable(
                name: "oauth_clients");
        }
    }
}
