using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JP_Morgan_POC.Migrations
{
    /// <inheritdoc />
    public partial class AddedFnLNames : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "EmpFirstName",
                table: "EmpContactDetails",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "EmpLastName",
                table: "EmpContactDetails",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EmpFirstName",
                table: "EmpContactDetails");

            migrationBuilder.DropColumn(
                name: "EmpLastName",
                table: "EmpContactDetails");
        }
    }
}
