using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace fs_backend.Migrations
{
    /// <inheritdoc />
    public partial class SeedPermissions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Tickets_Projects_ProjectId",
                table: "Tickets");

            migrationBuilder.AlterColumn<int>(
                name: "ProjectId",
                table: "Tickets",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<string>(
                name: "Description",
                table: "Permissions",
                type: "nvarchar(255)",
                maxLength: 255,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.InsertData(
                table: "Permissions",
                columns: new[] { "Id", "Action", "Code", "Description", "Module" },
                values: new object[,]
                {
                    { 1, "Ver", "dashboard.view", "Ver dashboard", "Dashboard" },
                    { 2, "Ver", "tickets.view", "Ver lista de tickets", "Tickets" },
                    { 3, "VerDetalle", "tickets.view_detail", "Ver detalles de ticket", "Tickets" },
                    { 4, "Crear", "tickets.create", "Crear tickets", "Tickets" },
                    { 5, "Editar", "tickets.edit", "Editar tickets", "Tickets" },
                    { 6, "Eliminar", "tickets.delete", "Eliminar tickets", "Tickets" },
                    { 7, "Comentar", "tickets.comment", "Agregar comentarios", "Tickets" },
                    { 8, "Actividades", "tickets.activity", "Gestionar actividades", "Tickets" },
                    { 9, "Estadisticas", "tickets.stats", "Ver estadísticas", "Tickets" },
                    { 10, "Asignar", "tickets.assign", "Asignar tickets a usuarios", "Tickets" },
                    { 11, "Ver", "clients.view", "Ver lista de clientes", "Clientes" },
                    { 12, "VerDetalle", "clients.view_detail", "Ver detalles de cliente", "Clientes" },
                    { 13, "Crear", "clients.create", "Crear clientes", "Clientes" },
                    { 14, "Editar", "clients.edit", "Editar clientes", "Clientes" },
                    { 15, "Eliminar", "clients.delete", "Eliminar clientes", "Clientes" },
                    { 16, "Ver", "projects.view", "Ver lista de proyectos", "Proyectos" },
                    { 17, "VerDetalle", "projects.view_detail", "Ver detalles de proyecto", "Proyectos" },
                    { 18, "Crear", "projects.create", "Crear proyectos", "Proyectos" },
                    { 19, "Editar", "projects.edit", "Editar proyectos", "Proyectos" },
                    { 20, "Eliminar", "projects.delete", "Eliminar proyectos", "Proyectos" },
                    { 21, "Ver", "quotes.view", "Ver lista de cotizaciones", "Cotizaciones" },
                    { 22, "VerDetalle", "quotes.view_detail", "Ver detalles de cotización", "Cotizaciones" },
                    { 23, "Crear", "quotes.create", "Crear cotizaciones", "Cotizaciones" },
                    { 24, "Editar", "quotes.edit", "Editar cotizaciones", "Cotizaciones" },
                    { 25, "Eliminar", "quotes.delete", "Eliminar cotizaciones", "Cotizaciones" },
                    { 26, "ConvertirFactura", "quotes.convert", "Convertir a factura", "Cotizaciones" },
                    { 27, "Ver", "invoices.view", "Ver lista de facturas", "Facturas" },
                    { 28, "VerDetalle", "invoices.view_detail", "Ver detalles de factura", "Facturas" },
                    { 29, "Crear", "invoices.create", "Crear facturas", "Facturas" },
                    { 30, "Editar", "invoices.edit", "Editar facturas", "Facturas" },
                    { 31, "Eliminar", "invoices.delete", "Eliminar facturas", "Facturas" },
                    { 32, "RegistrarPago", "invoices.payment", "Registrar pagos", "Facturas" },
                    { 33, "Ver", "reports.view", "Ver reportes", "Reportes" },
                    { 34, "Exportar", "reports.export", "Exportar reportes", "Reportes" },
                    { 35, "Financieros", "reports.financial", "Ver reportes financieros", "Reportes" },
                    { 36, "Ver", "users.view", "Ver lista de usuarios", "Usuarios" },
                    { 37, "Crear", "users.create", "Crear usuarios", "Usuarios" },
                    { 38, "Editar", "users.edit", "Editar usuarios", "Usuarios" },
                    { 39, "Eliminar", "users.delete", "Eliminar usuarios", "Usuarios" },
                    { 40, "CambiarPassword", "users.change_password", "Cambiar contraseñas", "Usuarios" },
                    { 41, "AsignarRoles", "users.assign_roles", "Asignar roles", "Usuarios" }
                });

            migrationBuilder.AddForeignKey(
                name: "FK_Tickets_Projects_ProjectId",
                table: "Tickets",
                column: "ProjectId",
                principalTable: "Projects",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Tickets_Projects_ProjectId",
                table: "Tickets");

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: 3);

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: 4);

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: 5);

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: 6);

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: 7);

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: 8);

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: 9);

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: 10);

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: 11);

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: 12);

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: 13);

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: 14);

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: 15);

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: 16);

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: 17);

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: 18);

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: 19);

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: 20);

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: 21);

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: 22);

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: 23);

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: 24);

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: 25);

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: 26);

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: 27);

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: 28);

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: 29);

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: 30);

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: 31);

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: 32);

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: 33);

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: 34);

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: 35);

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: 36);

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: 37);

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: 38);

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: 39);

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: 40);

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: 41);

            migrationBuilder.AlterColumn<int>(
                name: "ProjectId",
                table: "Tickets",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Description",
                table: "Permissions",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(255)",
                oldMaxLength: 255);

            migrationBuilder.AddForeignKey(
                name: "FK_Tickets_Projects_ProjectId",
                table: "Tickets",
                column: "ProjectId",
                principalTable: "Projects",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
