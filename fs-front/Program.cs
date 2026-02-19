using Blazored.LocalStorage;
using fs_front;
using fs_front.Repositories;
using fs_front.Services;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Radzen;

// 1) Crear el host de Blazor WebAssembly con configuración por defecto.
var builder = WebAssemblyHostBuilder.CreateDefault(args);

// 2) Registrar los componentes raíz del front-end.
//    - "#app" es el div donde se monta la app.
//    - "head::after" permite inyectar elementos en el <head>.
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// 3) Registrar un handler que intercepta respuestas HTTP no autorizadas.
builder.Services.AddScoped<UnauthorizedHandler>();

// 4) Crear un HttpClient llamado "ApiClient" con base URL desde config.
//    Luego se le añade el handler para manejar 401/403.
builder.Services.AddHttpClient("ApiClient", client =>
{
    client.BaseAddress = new Uri(builder.Configuration["WebApiAddress"]!);
})
.AddHttpMessageHandler<UnauthorizedHandler>();

// 5) Registrar el HttpClient "ApiClient" como HttpClient por defecto
//    para inyección (DI).
builder.Services.AddScoped(sp =>
    sp.GetRequiredService<IHttpClientFactory>().CreateClient("ApiClient"));

// 6) Habilitar el estado de autenticación y autorización en Blazor.
builder.Services.AddCascadingAuthenticationState();
builder.Services.AddAuthorizationCore();

// 7) Registro de servicios propios de la app (DI).
//    Aquí se conectan las interfaces con sus implementaciones.
builder.Services.AddScoped<PermissionsRealtimeClient>();
builder.Services.AddScoped<IAuthService, CustomAuthStateProvider>();
builder.Services.AddScoped<IEmployeeApiService, EmployeeApiService>();
builder.Services.AddScoped<AuthenticationStateProvider, CustomAuthStateProvider>();
builder.Services.AddScoped<IUserApiService, UserApiService>();
builder.Services.AddScoped<IClientApiService, ClientApiService>();
builder.Services.AddScoped<IProjectApiService, ProjectApiService>();
builder.Services.AddScoped<IServiceApiService, ServiceApiService>();
builder.Services.AddScoped<ITypeServiceApiService, TypeServiceApiService>();
builder.Services.AddScoped<ITypeActivityApiService, TypeActivityApiService>();
builder.Services.AddScoped<ITicketApiService, TicketApiService>();
builder.Services.AddScoped<IQuoteApiService, QuoteApiService>();
builder.Services.AddScoped<IInvoiceApiService, InvoiceApiService>();
builder.Services.AddScoped<IReportApiService, ReportApiService>();
builder.Services.AddScoped<IPermissionApiService, PermissionApiService>();
builder.Services.AddScoped<fs_front.Services.PermissionService>();
builder.Services.AddScoped<fs_front.Services.ThemeService>();

// 8) Habilitar LocalStorage para guardar datos en el navegador.
builder.Services.AddBlazoredLocalStorage();

// 9) Registrar los componentes de Radzen (UI).
builder.Services.AddRadzenComponents();

// 10) Construir y ejecutar la app.
await builder.Build().RunAsync();
