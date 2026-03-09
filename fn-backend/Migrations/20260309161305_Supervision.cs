using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace fs_backend.Migrations
{
    /// <inheritdoc />
    public partial class Supervision : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: 21,
                columns: new[] { "Code", "Description", "Module" },
                values: new object[] { "supervisor.view", "Ver panel de supervisión", "Supervisor" });

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: 22,
                columns: new[] { "Action", "Code", "Description", "Module" },
                values: new object[] { "VerEmpleados", "supervisor.employees", "Ver historial de empleados", "Supervisor" });

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: 23,
                columns: new[] { "Action", "Code", "Description" },
                values: new object[] { "Ver", "projects.view", "Ver lista de proyectos" });

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: 24,
                columns: new[] { "Action", "Code", "Description" },
                values: new object[] { "VerDetalle", "projects.view_detail", "Ver detalles de proyecto" });

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: 25,
                columns: new[] { "Action", "Code", "Description" },
                values: new object[] { "Crear", "projects.create", "Crear proyectos" });

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: 26,
                columns: new[] { "Action", "Code", "Description", "Module" },
                values: new object[] { "Editar", "projects.edit", "Editar proyectos", "Proyectos" });

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: 27,
                columns: new[] { "Action", "Code", "Description", "Module" },
                values: new object[] { "Eliminar", "projects.delete", "Eliminar proyectos", "Proyectos" });

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: 28,
                columns: new[] { "Action", "Code", "Description" },
                values: new object[] { "Ver", "quotes.view", "Ver lista de cotizaciones" });

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: 29,
                columns: new[] { "Action", "Code", "Description" },
                values: new object[] { "VerDetalle", "quotes.view_detail", "Ver detalles de cotización" });

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: 30,
                columns: new[] { "Action", "Code", "Description" },
                values: new object[] { "Crear", "quotes.create", "Crear cotizaciones" });

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: 31,
                columns: new[] { "Action", "Code", "Description" },
                values: new object[] { "Editar", "quotes.edit", "Editar cotizaciones" });

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: 32,
                columns: new[] { "Action", "Code", "Description", "Module" },
                values: new object[] { "Eliminar", "quotes.delete", "Eliminar cotizaciones", "Cotizaciones" });

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: 33,
                columns: new[] { "Action", "Code", "Description", "Module" },
                values: new object[] { "ConvertirFactura", "quotes.convert", "Convertir a factura", "Cotizaciones" });

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: 34,
                columns: new[] { "Action", "Code", "Description" },
                values: new object[] { "Ver", "invoices.view", "Ver lista de facturas" });

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: 35,
                columns: new[] { "Action", "Code", "Description" },
                values: new object[] { "VerDetalle", "invoices.view_detail", "Ver detalles de factura" });

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: 36,
                columns: new[] { "Action", "Code", "Description" },
                values: new object[] { "Crear", "invoices.create", "Crear facturas" });

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: 37,
                columns: new[] { "Action", "Code", "Description" },
                values: new object[] { "Editar", "invoices.edit", "Editar facturas" });

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: 38,
                columns: new[] { "Action", "Code", "Description", "Module" },
                values: new object[] { "Eliminar", "invoices.delete", "Eliminar facturas", "Facturas" });

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: 39,
                columns: new[] { "Action", "Code", "Description", "Module" },
                values: new object[] { "RegistrarPago", "invoices.payment", "Registrar pagos", "Facturas" });

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: 40,
                columns: new[] { "Action", "Code", "Description" },
                values: new object[] { "Ver", "reports.view", "Ver reportes" });

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: 41,
                columns: new[] { "Action", "Code", "Description", "Module" },
                values: new object[] { "Exportar", "reports.export", "Exportar reportes", "Reportes" });

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: 42,
                columns: new[] { "Action", "Code", "Description", "Module" },
                values: new object[] { "Financieros", "reports.financial", "Ver reportes financieros", "Reportes" });

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: 43,
                columns: new[] { "Action", "Code", "Description" },
                values: new object[] { "Ver", "users.view", "Ver lista de usuarios" });

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: 44,
                columns: new[] { "Action", "Code", "Description" },
                values: new object[] { "Crear", "users.create", "Crear usuarios" });

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: 45,
                columns: new[] { "Action", "Code", "Description" },
                values: new object[] { "Editar", "users.edit", "Editar usuarios" });

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: 46,
                columns: new[] { "Action", "Code", "Description" },
                values: new object[] { "Eliminar", "users.delete", "Eliminar usuarios" });

            migrationBuilder.InsertData(
                table: "Permissions",
                columns: new[] { "Id", "Action", "Code", "Description", "Module" },
                values: new object[,]
                {
                    { 47, "CambiarPassword", "users.change_password", "Cambiar contraseñas", "Usuarios" },
                    { 48, "AsignarRoles", "users.assign_roles", "Asignar roles", "Usuarios" }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: 47);

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: 48);

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
                columns: new[] { "Action", "Code", "Description", "Module" },
                values: new object[] { "VerDetalle", "projects.view_detail", "Ver detalles de proyecto", "Proyectos" });

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: 23,
                columns: new[] { "Action", "Code", "Description" },
                values: new object[] { "Crear", "projects.create", "Crear proyectos" });

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: 24,
                columns: new[] { "Action", "Code", "Description" },
                values: new object[] { "Editar", "projects.edit", "Editar proyectos" });

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: 25,
                columns: new[] { "Action", "Code", "Description" },
                values: new object[] { "Eliminar", "projects.delete", "Eliminar proyectos" });

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: 26,
                columns: new[] { "Action", "Code", "Description", "Module" },
                values: new object[] { "Ver", "quotes.view", "Ver lista de cotizaciones", "Cotizaciones" });

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
                columns: new[] { "Action", "Code", "Description" },
                values: new object[] { "Crear", "quotes.create", "Crear cotizaciones" });

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: 29,
                columns: new[] { "Action", "Code", "Description" },
                values: new object[] { "Editar", "quotes.edit", "Editar cotizaciones" });

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: 30,
                columns: new[] { "Action", "Code", "Description" },
                values: new object[] { "Eliminar", "quotes.delete", "Eliminar cotizaciones" });

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: 31,
                columns: new[] { "Action", "Code", "Description" },
                values: new object[] { "ConvertirFactura", "quotes.convert", "Convertir a factura" });

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: 32,
                columns: new[] { "Action", "Code", "Description", "Module" },
                values: new object[] { "Ver", "invoices.view", "Ver lista de facturas", "Facturas" });

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
                columns: new[] { "Action", "Code", "Description" },
                values: new object[] { "Crear", "invoices.create", "Crear facturas" });

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: 35,
                columns: new[] { "Action", "Code", "Description" },
                values: new object[] { "Editar", "invoices.edit", "Editar facturas" });

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: 36,
                columns: new[] { "Action", "Code", "Description" },
                values: new object[] { "Eliminar", "invoices.delete", "Eliminar facturas" });

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: 37,
                columns: new[] { "Action", "Code", "Description" },
                values: new object[] { "RegistrarPago", "invoices.payment", "Registrar pagos" });

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
                columns: new[] { "Action", "Code", "Description" },
                values: new object[] { "Financieros", "reports.financial", "Ver reportes financieros" });

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: 41,
                columns: new[] { "Action", "Code", "Description", "Module" },
                values: new object[] { "Ver", "users.view", "Ver lista de usuarios", "Usuarios" });

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: 42,
                columns: new[] { "Action", "Code", "Description", "Module" },
                values: new object[] { "Crear", "users.create", "Crear usuarios", "Usuarios" });

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: 43,
                columns: new[] { "Action", "Code", "Description" },
                values: new object[] { "Editar", "users.edit", "Editar usuarios" });

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: 44,
                columns: new[] { "Action", "Code", "Description" },
                values: new object[] { "Eliminar", "users.delete", "Eliminar usuarios" });

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: 45,
                columns: new[] { "Action", "Code", "Description" },
                values: new object[] { "CambiarPassword", "users.change_password", "Cambiar contraseñas" });

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: 46,
                columns: new[] { "Action", "Code", "Description" },
                values: new object[] { "AsignarRoles", "users.assign_roles", "Asignar roles" });
        }
    }
}
