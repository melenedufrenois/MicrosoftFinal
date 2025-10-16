using LoLProject.WebApp.Clients;
using LoLProject.WebApp.Components;
using LoLProject.WebApp;


var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// ANTIFORGERY
builder.Services.AddAntiforgery();

// HttpClient typé vers l’API en FORÇANT l’URL HTTP exposée par Aspire
builder.Services.AddHttpClient<ITodoClient, TodoClient>((sp, client) =>
{
    var cfg = sp.GetRequiredService<IConfiguration>();
    var httpUrl = cfg["services:apiservice:http:0"];
    client.BaseAddress = new Uri(httpUrl ?? "http://apiservice");
})
.ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
{
    ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
});

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

// ANTIFORGERY
app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
