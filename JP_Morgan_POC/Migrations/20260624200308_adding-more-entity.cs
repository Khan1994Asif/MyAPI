using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JP_Morgan_POC.Migrations
{
    /// <inheritdoc />
    public partial class addingmoreentity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "dbo");

            migrationBuilder.RenameTable(
                name: "EmpContactDetails",
                newName: "EmpContactDetails",
                newSchema: "dbo");

            migrationBuilder.CreateTable(
                name: "EmpContactDetails_Outbox",
                schema: "dbo",
                columns: table => new
                {
                    OutboxId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EmpId = table.Column<int>(type: "int", nullable: false),
                    EventType = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    RetryCount = table.Column<int>(type: "int", nullable: false),
                    LockedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ProcessedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ErrorMessage = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmpContactDetails_Outbox", x => x.OutboxId);
                });

            migrationBuilder.CreateTable(
                name: "SyncControl",
                schema: "dbo",
                columns: table => new
                {
                    EntityName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    IsEnabled = table.Column<bool>(type: "bit", nullable: false),
                    LastSchemaHash = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    LastMetadataRefreshUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    StopReason = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SyncControl", x => x.EntityName);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EmpContactDetails_Outbox",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "SyncControl",
                schema: "dbo");

            migrationBuilder.RenameTable(
                name: "EmpContactDetails",
                schema: "dbo",
                newName: "EmpContactDetails");
        }
    }
}
