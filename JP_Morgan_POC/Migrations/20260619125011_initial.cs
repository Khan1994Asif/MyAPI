using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JP_Morgan_POC.Migrations
{
    /// <inheritdoc />
    public partial class initial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "EmpContactDetails",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EmpAddress = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    EmpProfile = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    SynStatus = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmpContactDetails", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EmpContactDetails");
        }
    }
}
