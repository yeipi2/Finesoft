using Asp.Versioning;
using fs_backend.Filters;
using fs_backend.Middleware;
using fs_backend.Validators;
using fn_backend.Services;
using fs_backend.Hubs;
using fs_backend.Identity;
using fs_backend.Repositories;
using fs_backend.Services;
using fs_front.Services;
using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using QuestPDF.Infrastructure;
using System.Text;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

QuestPDF.Settings.License = LicenseType.Community;

builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddValidatorsFromAssemblyContaining<ClientDtoValidator>();

builder.Services.AddControllers(options =>
{
    options.Filters.Add<ApiExceptionFilter>();
});
builder.Services.Configure<ApiBehaviorOptions>(options =>
{
    options.InvalidModelStateResponseFactory = context =>
    {
        var problemDetails = new ValidationProblemDetails(context.ModelState)
        {
            Status = StatusCodes.Status422UnprocessableEntity,
            Title = "Validation failed",
            Type = "https://httpstatuses.com/422"
        };

        return new UnprocessableEntityObjectResult(problemDetails);
    };
});

builder.Services.AddProblemDetails();

builder.Services
    .AddApiVersioning(options =>
    {
        options.DefaultApiVersion = new ApiVersion(1, 0);
        options.AssumeDefaultVersionWhenUnspecified = true;
        options.ReportApiVersions = true;
        options.ApiVersionReader = new UrlSegmentApiVersionReader();
    })
    .AddApiExplorer(options =>
    {
        options.GroupNameFormat = "'v'VVV";
        options.SubstituteApiVersionInUrl = true;
    });

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.CustomSchemaIds(type => type.FullName?.Replace("+", "."));
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "FineSoft API",
        Version = "v1"
    });
});
builder.Services.AddRazorPages();
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddFixedWindowLimiter("PublicApi", limiterOptions =>
    {
        limiterOptions.PermitLimit = 30;
        limiterOptions.Window = TimeSpan.FromMinutes(1);
        limiterOptions.QueueLimit = 0;
        limiterOptions.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
    });

    options.AddFixedWindowLimiter("AuthenticatedApi", limiterOptions =>
    {
        limiterOptions.PermitLimit = 100;
        limiterOptions.Window = TimeSpan.FromMinutes(1);
        limiterOptions.QueueLimit = 5;
        limiterOptions.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
    });

    options.AddFixedWindowLimiter("SensitiveEndpoints", limiterOptions =>
    {
        limiterOptions.PermitLimit = 10;
        limiterOptions.Window = TimeSpan.FromMinutes(1);
        limiterOptions.QueueLimit = 0;
        limiterOptions.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
    });

    options.AddPolicy("AuthenticatedPolicy", context => 
    {
        return RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: context.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? context.Connection.RemoteIpAddress?.ToString() ?? "anonymous",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 100,
                Window = TimeSpan.FromMinutes(1),
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 5
            });
    });

    options.AddPolicy("SensitivePolicy", context =>
    {
        return RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: context.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? context.Connection.RemoteIpAddress?.ToString() ?? "anonymous",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 10,
                Window = TimeSpan.FromMinutes(1),
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 0
            });
    });
});

builder.Services.AddMemoryCache();
builder.Services.AddSingleton<ICacheService, MemoryCacheService>();

builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IEmployeeService, EmployeeService>();
builder.Services.AddScoped<IClientService, ClientService>();
builder.Services.AddScoped<IProjectService, ProjectService>();
builder.Services.AddScoped<ITicketService, TicketService>();
builder.Services.AddScoped<IQuoteService, QuoteService>();
builder.Services.AddScoped<IPaymentPolicy, PaymentPolicy>();
builder.Services.AddScoped<IReceiptStorageService, ReceiptStorageService>();
builder.Services.AddScoped<IInvoiceStatusService, InvoiceStatusService>();
builder.Services.AddScoped<IInvoicePdfGenerator, InvoicePdfGenerator>();
builder.Services.AddScoped<IInvoiceMapper, InvoiceMapper>();
builder.Services.AddScoped<IMonthlyBillingService, MonthlyBillingService>();
builder.Services.AddScoped<IInvoiceNumberService, InvoiceNumberService>();
builder.Services.AddScoped<IInvoiceService, InvoiceService>();
builder.Services.AddScoped<IReportService, ReportService>();
builder.Services.AddScoped<IReportPdfGenerator, ReportPdfGenerator>();
builder.Services.AddScoped<IReportEmailPreferenceService, ReportEmailPreferenceService>();
builder.Services.AddHostedService<ReportEmailSchedulerService>();
builder.Services.AddScoped<IPermissionService, PermissionService>();
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<ISupervisorService, SupervisorService>();
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
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<IAuditService, AuditService>();

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

builder.Services.AddHealthChecks()
    .AddDbContextCheck<ApplicationDbContext>("Database", tags: new[] { "db", "sql" })
    .AddCheck("self", () => HealthCheckResult.Healthy("API is running"), tags: new[] { "api" });

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
    var userManager = services.GetRequiredService<UserManager<IdentityUser>>();
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    Console.WriteLine(">>> RUNTIME CONN: " + db.Database.GetConnectionString());
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
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "FineSoft API v1");
    });
}

app.UseGlobalExceptionHandling();
app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter();

app.UseStaticFiles();

app.MapHub<PermissionsHub>("/hubs/permissions");
app.MapHub<QuotesHub>("/hubs/quotes");

app.MapControllers();
app.MapIdentityApi<IdentityUser>();

app.MapHealthChecks("/health", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    ResponseWriter = async (context, report) =>
    {
        context.Response.ContentType = "application/json";
        var response = new
        {
            status = report.Status.ToString(),
            timestamp = DateTime.UtcNow,
            checks = report.Entries.Select(e => new
            {
                name = e.Key,
                status = e.Value.Status.ToString(),
                description = e.Value.Description?.ToString(),
                duration = e.Value.Duration.TotalMilliseconds
            })
        };
        await context.Response.WriteAsync(System.Text.Json.JsonSerializer.Serialize(response));
    }
});

app.MapHealthChecks("/health/ready", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("db"),
    ResponseWriter = async (context, report) =>
    {
        context.Response.ContentType = "application/json";
        await context.Response.WriteAsync(System.Text.Json.JsonSerializer.Serialize(new
        {
            status = report.Status.ToString(),
            database = report.Entries.FirstOrDefault().Value.Status.ToString()
        }));
    }
});

app.MapHealthChecks("/health/live", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("api")
});

app.Run();
