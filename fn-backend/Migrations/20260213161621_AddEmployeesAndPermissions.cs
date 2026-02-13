using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace fs_backend.Migrations
{
    /// <inheritdoc />
    public partial class AddEmployeesAndPermissions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Employees",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    FullName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    RFC = table.Column<string>(type: "nvarchar(13)", maxLength: 13, nullable: false),
                    CURP = table.Column<string>(type: "nvarchar(18)", maxLength: 18, nullable: false),
                    Position = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Department = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Phone = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Address = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    HireDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Salary = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    EmergencyContactName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    EmergencyContactPhone = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Employees", x => x.Id);
                });

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: 16,
                columns: new[] { "Code", "Description", "Module" },
                values: new object[] { "employees.view", "Ver lista de empleados", "Empleados" });

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: 17,
                columns: new[] { "Code", "Description", "Module" },
                values: new object[] { "employees.view_detail", "Ver detalles de empleado", "Empleados" });

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: 18,
                columns: new[] { "Code", "Description", "Module" },
                values: new object[] { "employees.create", "Crear empleados", "Empleados" });

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: 19,
                columns: new[] { "Code", "Description", "Module" },
                values: new object[] { "employees.edit", "Editar empleados", "Empleados" });

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: 20,
                columns: new[] { "Code", "Description", "Module" },
                values: new object[] { "employees.delete", "Eliminar empleados", "Empleados" });

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: 21,
                columns: new[] { "Code", "Description", "Module" },
                values: new object[] { "projects.view", "Ver lista de proyectos", "Proyectos" });

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: 22,
                columns: new[] { "Code", "Description", "Module" },
                values: new object[] { "projects.view_detail", "Ver detalles de proyecto", "Proyectos" });

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: 23,
                columns: new[] { "Code", "Description", "Module" },
                values: new object[] { "projects.create", "Crear proyectos", "Proyectos" });

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: 24,
                columns: new[] { "Code", "Description", "Module" },
                values: new object[] { "projects.edit", "Editar proyectos", "Proyectos" });

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: 25,
                columns: new[] { "Code", "Description", "Module" },
                values: new object[] { "projects.delete", "Eliminar proyectos", "Proyectos" });

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: 26,
                columns: new[] { "Action", "Code", "Description" },
                values: new object[] { "Ver", "quotes.view", "Ver lista de cotizaciones" });

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: 27,
                columns: new[] { "Action", "Code", "Description", "Module" },
                values: new object[] { "VerDetalle", "quotes.view_detail", "Ver detalles de cotización", "Cotizaciones" });

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: 28,
                columns: new[] { "Action", "Code", "Description", "Module" },
                values: new object[] { "Crear", "quotes.create", "Crear cotizaciones", "Cotizaciones" });

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: 29,
                columns: new[] { "Action", "Code", "Description", "Module" },
                values: new object[] { "Editar", "quotes.edit", "Editar cotizaciones", "Cotizaciones" });

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: 30,
                columns: new[] { "Action", "Code", "Description", "Module" },
                values: new object[] { "Eliminar", "quotes.delete", "Eliminar cotizaciones", "Cotizaciones" });

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: 31,
                columns: new[] { "Action", "Code", "Description", "Module" },
                values: new object[] { "ConvertirFactura", "quotes.convert", "Convertir a factura", "Cotizaciones" });

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: 32,
                columns: new[] { "Action", "Code", "Description" },
                values: new object[] { "Ver", "invoices.view", "Ver lista de facturas" });

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: 33,
                columns: new[] { "Action", "Code", "Description", "Module" },
                values: new object[] { "VerDetalle", "invoices.view_detail", "Ver detalles de factura", "Facturas" });

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: 34,
                columns: new[] { "Action", "Code", "Description", "Module" },
                values: new object[] { "Crear", "invoices.create", "Crear facturas", "Facturas" });

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: 35,
                columns: new[] { "Action", "Code", "Description", "Module" },
                values: new object[] { "Editar", "invoices.edit", "Editar facturas", "Facturas" });

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: 36,
                columns: new[] { "Action", "Code", "Description", "Module" },
                values: new object[] { "Eliminar", "invoices.delete", "Eliminar facturas", "Facturas" });

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: 37,
                columns: new[] { "Action", "Code", "Description", "Module" },
                values: new object[] { "RegistrarPago", "invoices.payment", "Registrar pagos", "Facturas" });

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: 38,
                columns: new[] { "Action", "Code", "Description", "Module" },
                values: new object[] { "Ver", "reports.view", "Ver reportes", "Reportes" });

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: 39,
                columns: new[] { "Action", "Code", "Description", "Module" },
                values: new object[] { "Exportar", "reports.export", "Exportar reportes", "Reportes" });

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: 40,
                columns: new[] { "Action", "Code", "Description", "Module" },
                values: new object[] { "Financieros", "reports.financial", "Ver reportes financieros", "Reportes" });

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: 41,
                columns: new[] { "Action", "Code", "Description" },
                values: new object[] { "Ver", "users.view", "Ver lista de usuarios" });

            migrationBuilder.InsertData(
                table: "Permissions",
                columns: new[] { "Id", "Action", "Code", "Description", "Module" },
                values: new object[,]
                {
                    { 42, "Crear", "users.create", "Crear usuarios", "Usuarios" },
                    { 43, "Editar", "users.edit", "Editar usuarios", "Usuarios" },
                    { 44, "Eliminar", "users.delete", "Eliminar usuarios", "Usuarios" },
                    { 45, "CambiarPassword", "users.change_password", "Cambiar contraseñas", "Usuarios" },
                    { 46, "AsignarRoles", "users.assign_roles", "Asignar roles", "Usuarios" }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Employees");

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: 42);

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: 43);

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: 44);

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: 45);

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: 46);

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: 16,
                columns: new[] { "Code", "Description", "Module" },
                values: new object[] { "projects.view", "Ver lista de proyectos", "Proyectos" });

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: 17,
                columns: new[] { "Code", "Description", "Module" },
                values: new object[] { "projects.view_detail", "Ver detalles de proyecto", "Proyectos" });

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: 18,
                columns: new[] { "Code", "Description", "Module" },
                values: new object[] { "projects.create", "Crear proyectos", "Proyectos" });

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: 19,
                columns: new[] { "Code", "Description", "Module" },
                values: new object[] { "projects.edit", "Editar proyectos", "Proyectos" });

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: 20,
                columns: new[] { "Code", "Description", "Module" },
                values: new object[] { "projects.delete", "Eliminar proyectos", "Proyectos" });

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: 21,
                columns: new[] { "Code", "Description", "Module" },
                values: new object[] { "quotes.view", "Ver lista de cotizaciones", "Cotizaciones" });

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: 22,
                columns: new[] { "Code", "Description", "Module" },
                values: new object[] { "quotes.view_detail", "Ver detalles de cotización", "Cotizaciones" });

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: 23,
                columns: new[] { "Code", "Description", "Module" },
                values: new object[] { "quotes.create", "Crear cotizaciones", "Cotizaciones" });

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: 24,
                columns: new[] { "Code", "Description", "Module" },
                values: new object[] { "quotes.edit", "Editar cotizaciones", "Cotizaciones" });

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: 25,
                columns: new[] { "Code", "Description", "Module" },
                values: new object[] { "quotes.delete", "Eliminar cotizaciones", "Cotizaciones" });

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: 26,
                columns: new[] { "Action", "Code", "Description" },
                values: new object[] { "ConvertirFactura", "quotes.convert", "Convertir a factura" });

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: 27,
                columns: new[] { "Action", "Code", "Description", "Module" },
                values: new object[] { "Ver", "invoices.view", "Ver lista de facturas", "Facturas" });

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: 28,
                columns: new[] { "Action", "Code", "Description", "Module" },
                values: new object[] { "VerDetalle", "invoices.view_detail", "Ver detalles de factura", "Facturas" });

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: 29,
                columns: new[] { "Action", "Code", "Description", "Module" },
                values: new object[] { "Crear", "invoices.create", "Crear facturas", "Facturas" });

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: 30,
                columns: new[] { "Action", "Code", "Description", "Module" },
                values: new object[] { "Editar", "invoices.edit", "Editar facturas", "Facturas" });

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: 31,
                columns: new[] { "Action", "Code", "Description", "Module" },
                values: new object[] { "Eliminar", "invoices.delete", "Eliminar facturas", "Facturas" });

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: 32,
                columns: new[] { "Action", "Code", "Description" },
                values: new object[] { "RegistrarPago", "invoices.payment", "Registrar pagos" });

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: 33,
                columns: new[] { "Action", "Code", "Description", "Module" },
                values: new object[] { "Ver", "reports.view", "Ver reportes", "Reportes" });

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: 34,
                columns: new[] { "Action", "Code", "Description", "Module" },
                values: new object[] { "Exportar", "reports.export", "Exportar reportes", "Reportes" });

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: 35,
                columns: new[] { "Action", "Code", "Description", "Module" },
                values: new object[] { "Financieros", "reports.financial", "Ver reportes financieros", "Reportes" });

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: 36,
                columns: new[] { "Action", "Code", "Description", "Module" },
                values: new object[] { "Ver", "users.view", "Ver lista de usuarios", "Usuarios" });

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: 37,
                columns: new[] { "Action", "Code", "Description", "Module" },
                values: new object[] { "Crear", "users.create", "Crear usuarios", "Usuarios" });

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: 38,
                columns: new[] { "Action", "Code", "Description", "Module" },
                values: new object[] { "Editar", "users.edit", "Editar usuarios", "Usuarios" });

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: 39,
                columns: new[] { "Action", "Code", "Description", "Module" },
                values: new object[] { "Eliminar", "users.delete", "Eliminar usuarios", "Usuarios" });

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: 40,
                columns: new[] { "Action", "Code", "Description", "Module" },
                values: new object[] { "CambiarPassword", "users.change_password", "Cambiar contraseñas", "Usuarios" });

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: 41,
                columns: new[] { "Action", "Code", "Description" },
                values: new object[] { "AsignarRoles", "users.assign_roles", "Asignar roles" });
        }
    }
}
