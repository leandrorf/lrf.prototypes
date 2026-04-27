using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace lrfdev.auth.api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddDeviceAuthorizationFlow : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "AllowDeviceAuthorization",
                table: "oauth_clients",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "oauth_device_flow_sessions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    DeviceCode = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    UserCode = table.Column<string>(type: "varchar(32)", maxLength: 32, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ClientId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    Scope = table.Column<string>(type: "varchar(1000)", maxLength: 1000, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Status = table.Column<string>(type: "varchar(32)", maxLength: 32, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    UserId = table.Column<Guid>(type: "char(36)", nullable: true, collation: "ascii_general_ci"),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    ExpiresAtUtc = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    IntervalSeconds = table.Column<int>(type: "int", nullable: false),
                    LastPollAtUtc = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_oauth_device_flow_sessions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_oauth_device_flow_sessions_oauth_clients_ClientId",
                        column: x => x.ClientId,
                        principalTable: "oauth_clients",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_oauth_device_flow_sessions_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "Id");
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_oauth_device_flow_sessions_ClientId",
                table: "oauth_device_flow_sessions",
                column: "ClientId");

            migrationBuilder.CreateIndex(
                name: "IX_oauth_device_flow_sessions_DeviceCode",
                table: "oauth_device_flow_sessions",
                column: "DeviceCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_oauth_device_flow_sessions_ExpiresAtUtc",
                table: "oauth_device_flow_sessions",
                column: "ExpiresAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_oauth_device_flow_sessions_UserCode",
                table: "oauth_device_flow_sessions",
                column: "UserCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_oauth_device_flow_sessions_UserId",
                table: "oauth_device_flow_sessions",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "oauth_device_flow_sessions");

            migrationBuilder.DropColumn(
                name: "AllowDeviceAuthorization",
                table: "oauth_clients");
        }
    }
}
