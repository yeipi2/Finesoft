using Blazored.LocalStorage;
using fs_front;
using fs_front.Services;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Radzen;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.Configuration["WebApiAddress"]!) });

builder.Services.AddCascadingAuthenticationState();
builder.Services.AddAuthorizationCore();

// registro de servicios
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

builder.Services.AddBlazoredLocalStorage();

builder.Services.AddRadzenComponents();

await builder.Build().RunAsync();