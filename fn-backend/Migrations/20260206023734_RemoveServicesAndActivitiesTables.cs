using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace fs_backend.Migrations
{
    /// <inheritdoc />
    public partial class RemoveServicesAndActivitiesTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_InvoiceItems_Services_ServiceId",
                table: "InvoiceItems");

            migrationBuilder.DropForeignKey(
                name: "FK_QuoteItems_Services_ServiceId",
                table: "QuoteItems");

            migrationBuilder.DropForeignKey(
                name: "FK_Tickets_Services_ServiceId",
                table: "Tickets");

            migrationBuilder.DropTable(
                name: "Services");

            migrationBuilder.DropTable(
                name: "TypeActivities");

            migrationBuilder.DropTable(
                name: "TypeServices");

            migrationBuilder.DropIndex(
                name: "IX_Tickets_ServiceId",
                table: "Tickets");

            migrationBuilder.DropIndex(
                name: "IX_QuoteItems_ServiceId",
                table: "QuoteItems");

            migrationBuilder.DropIndex(
                name: "IX_InvoiceItems_ServiceId",
                table: "InvoiceItems");

            migrationBuilder.DropColumn(
                name: "ServiceId",
                table: "Tickets");

            migrationBuilder.DropColumn(
                name: "ServiceId",
                table: "QuoteItems");

            migrationBuilder.DropColumn(
                name: "ServiceId",
                table: "InvoiceItems");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ServiceId",
                table: "Tickets",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "ServiceId",
                table: "QuoteItems",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ServiceId",
                table: "InvoiceItems",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "TypeActivities",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TypeActivities", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TypeServices",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TypeServices", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Services",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProjectId = table.Column<int>(type: "int", nullable: false),
                    TypeActivityId = table.Column<int>(type: "int", nullable: false),
                    TypeServiceId = table.Column<int>(type: "int", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    HourlyRate = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Services", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Services_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Services_TypeActivities_TypeActivityId",
                        column: x => x.TypeActivityId,
                        principalTable: "TypeActivities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Services_TypeServices_TypeServiceId",
                        column: x => x.TypeServiceId,
                        principalTable: "TypeServices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Tickets_ServiceId",
                table: "Tickets",
                column: "ServiceId");

            migrationBuilder.CreateIndex(
                name: "IX_QuoteItems_ServiceId",
                table: "QuoteItems",
                column: "ServiceId");

            migrationBuilder.CreateIndex(
                name: "IX_InvoiceItems_ServiceId",
                table: "InvoiceItems",
                column: "ServiceId");

            migrationBuilder.CreateIndex(
                name: "IX_Services_ProjectId",
                table: "Services",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_Services_TypeActivityId",
                table: "Services",
                column: "TypeActivityId");

            migrationBuilder.CreateIndex(
                name: "IX_Services_TypeServiceId",
                table: "Services",
                column: "TypeServiceId");

            migrationBuilder.AddForeignKey(
                name: "FK_InvoiceItems_Services_ServiceId",
                table: "InvoiceItems",
                column: "ServiceId",
                principalTable: "Services",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_QuoteItems_Services_ServiceId",
                table: "QuoteItems",
                column: "ServiceId",
                principalTable: "Services",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Tickets_Services_ServiceId",
                table: "Tickets",
                column: "ServiceId",
                principalTable: "Services",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
