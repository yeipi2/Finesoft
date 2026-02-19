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

var builder = WebApplication.CreateBuilder(args);

QuestPDF.Settings.License = LicenseType.Community;

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.CustomSchemaIds(type => type.FullName?.Replace("+", "."));
});
builder.Services.AddRazorPages();

builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IEmployeeService, EmployeeService>();
builder.Services.AddScoped<IClientService, ClientService>();
builder.Services.AddScoped<IProjectService, ProjectService>();
builder.Services.AddScoped<ITicketService, TicketService>();
builder.Services.AddScoped<IQuoteService, QuoteService>();
builder.Services.AddScoped<IInvoiceService, InvoiceService>();
builder.Services.AddScoped<IReportService, ReportService>();
builder.Services.AddScoped<IPermissionService, PermissionService>();
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddSignalR();

var conn = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<ApplicationDbContext>(o => o.UseSqlServer(conn));

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

builder.Services.AddScoped<IJwtTokenService, JwtTokenService>();

builder.Services.AddAuthorization();
builder.Services.AddIdentityApiEndpoints<IdentityUser>()
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddErrorDescriber<fn_backend.Identity.SpanishIdentityErrorDescriber>();

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy => policy.RequireRole("Admin"));
    options.AddPolicy("AdminOrAdministracion", policy => policy.RequireRole("Admin", "Administracion"));
    options.AddPolicy("CanManageTickets", policy => policy.RequireRole("Admin", "Administracion", "Empleado"));
    options.AddPolicy("CanViewReports", policy => policy.RequireRole("Admin", "Administracion", "Supervisor"));
    options.AddPolicy("CanCreateTicket", policy => policy.RequireAuthenticatedUser());
});

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
    var userManager = services.GetRequiredService<UserManager<IdentityUser>>();
    try
    {
        foreach (var role in new[] { "Admin", "Administracion", "Empleado", "Supervisor", "Cliente" })
            if (!await roleManager.RoleExistsAsync(role))
                await roleManager.CreateAsync(new IdentityRole(role));

        var adminEmail = "admin@finesoft.com";
        var adminUser = await userManager.FindByEmailAsync(adminEmail);
        if (adminUser == null)
        {
            var newAdmin = new IdentityUser { UserName = adminEmail, Email = adminEmail, EmailConfirmed = true };
            var result = await userManager.CreateAsync(newAdmin, "Admin123!");
            if (result.Succeeded)
                await userManager.AddToRoleAsync(newAdmin, "Admin");
        }
        else if (!await userManager.IsInRoleAsync(adminUser, "Admin"))
            await userManager.AddToRoleAsync(adminUser, "Admin");
    }
    catch (Exception ex) { Console.WriteLine($"❌ Error en seed: {ex.Message}"); }
}

// ─────────────────────────────────────────────────────────────────────────────
// ⭐ CORS CORREGIDO PARA SIGNALR
//
// PROBLEMA: AllowAnyOrigin() es INCOMPATIBLE con AllowCredentials().
// SignalR usa WebSockets que requieren AllowCredentials().
// Por eso la conexión fallaba silenciosamente — el backend rechazaba el handshake.
//
// SOLUCIÓN: origen exacto del frontend + AllowCredentials().
// ─────────────────────────────────────────────────────────────────────────────
app.UseCors(policy =>
{
    policy
        .WithOrigins(
            "https://localhost:7204",   // ← tu frontend (ajusta si cambia el puerto)
            "http://localhost:5204"
        )
        .AllowAnyHeader()
        .AllowAnyMethod()
        .AllowCredentials();           // ← REQUERIDO por SignalR WebSockets
});

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapHub<PermissionsHub>("/hubs/permissions");
app.MapHub<QuotesHub>("/hubs/quotes");

app.UseAuthentication();
app.UseAuthorization();

app.UseStaticFiles();

app.MapControllers();
app.MapIdentityApi<IdentityUser>();

app.UseHttpsRedirection();

app.Run();