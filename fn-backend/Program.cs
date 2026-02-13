using fn_backend.Services;
using fs_backend.Identity;
using fs_backend.Repositories;
using fs_backend.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Infrastructure;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using fs_backend.Hubs;

// 1) Crear el builder de la aplicación
var builder = WebApplication.CreateBuilder(args);

// 2) Configurar licencia de QuestPDF (Community)
QuestPDF.Settings.License = LicenseType.Community;

// 3) Servicios base (MVC, Swagger, Razor Pages)
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.CustomSchemaIds(type => type.FullName?.Replace("+", "."));
    // Usa el namespace completo y reemplaza '+' por '.' para clases anidadas
});
builder.Services.AddRazorPages();

// 4) Registro de servicios propios (DI)
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IEmployeeService, EmployeeService>();
builder.Services.AddScoped<IClientService, ClientService>();
builder.Services.AddScoped<IProjectService, ProjectService>();
builder.Services.AddScoped<ITicketService, TicketService>();
builder.Services.AddScoped<IQuoteService, QuoteService>();
builder.Services.AddScoped<IInvoiceService, InvoiceService>();
builder.Services.AddScoped<IReportService, ReportService>();
builder.Services.AddScoped<IPermissionService, PermissionService>(); // ⭐ SERVICIO DE PERMISOS
builder.Services.AddSignalR();

// 5) Conexión a base de datos
var conn = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<ApplicationDbContext>(o => o.UseSqlServer(conn));

// 6) Configuración JWT
var jwt = builder.Configuration.GetSection("Jwt");
var key = jwt["Key"]!;

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,

        ValidIssuer = jwt["Issuer"],
        ValidAudience = jwt["Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)),

        ClockSkew = TimeSpan.FromMinutes(1)
    };
});

// 7) Servicio para emitir JWT
builder.Services.AddScoped<IJwtTokenService, JwtTokenService>();

// 8) Identity + Roles + EF Core
builder.Services.AddAuthorization();
builder.Services.AddIdentityApiEndpoints<IdentityUser>()
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>();

// ============================================
// POLÍTICAS DE AUTORIZACIÓN
// ============================================
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy =>
        policy.RequireRole("Admin"));

    options.AddPolicy("AdminOrAdministracion", policy =>
        policy.RequireRole("Admin", "Administracion"));

    options.AddPolicy("CanManageTickets", policy =>
        policy.RequireRole("Admin", "Administracion", "Empleado"));

    options.AddPolicy("CanViewReports", policy =>
        policy.RequireRole("Admin", "Administracion", "Supervisor"));

    options.AddPolicy("CanCreateTicket", policy =>
        policy.RequireAuthenticatedUser());
});

// 9) Construir la app
var app = builder.Build();

// ============================================
// SEED DE ROLES Y USUARIO ADMIN - ACTUALIZADO
// ============================================
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = services.GetRequiredService<UserManager<IdentityUser>>();

        // ⭐ CREAR LOS 5 ROLES DEL SISTEMA
        string[] roles = { "Admin", "Administracion", "Empleado", "Supervisor", "Cliente" };

        foreach (var role in roles)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new IdentityRole(role));
                Console.WriteLine($"✅ Rol '{role}' creado exitosamente");
            }
            else
            {
                Console.WriteLine($"ℹ️  Rol '{role}' ya existe");
            }
        }

        // Crear usuario admin si no existe
        var adminEmail = "admin@finesoft.com";
        var adminUser = await userManager.FindByEmailAsync(adminEmail);

        if (adminUser == null)
        {
            var newAdmin = new IdentityUser
            {
                UserName = adminEmail,
                Email = adminEmail,
                EmailConfirmed = true
            };

            var result = await userManager.CreateAsync(newAdmin, "Admin123!");

            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(newAdmin, "Admin");
                Console.WriteLine("✅ Usuario Admin creado exitosamente");
                Console.WriteLine($"   Email: {adminEmail}");
                Console.WriteLine("   Password: Admin123!");
            }
            else
            {
                Console.WriteLine("❌ Error al crear usuario admin:");
                foreach (var error in result.Errors)
                {
                    Console.WriteLine($"   - {error.Description}");
                }
            }
        }
        else
        {
            Console.WriteLine("ℹ️  Usuario Admin ya existe");

            // Asegurar que el admin tenga el rol Admin
            if (!await userManager.IsInRoleAsync(adminUser, "Admin"))
            {
                await userManager.AddToRoleAsync(adminUser, "Admin");
                Console.WriteLine("✅ Rol 'Admin' asignado al usuario admin");
            }
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"❌ Error en seed de datos: {ex.Message}");
    }
}

// 10) Configuración CORS (permitir cualquier origen)
app.UseCors(policy =>
{
    policy.AllowAnyHeader();
    policy.AllowAnyMethod();
    policy.AllowAnyOrigin();
});

// 11) Pipeline HTTP
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}


app.MapHub<PermissionsHub>("/hubs/permissions");



app.UseAuthentication();
app.UseAuthorization();

// 12) Mapear endpoints
app.MapControllers();
app.MapIdentityApi<IdentityUser>();

app.UseHttpsRedirection();

// 13) Ejecutar la app
app.Run();