using PokeTeams.Core.PokeApi;
using PokeTeams.Web.Components;
using PokeTeams.Web.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddMemoryCache();
builder.Services.AddHttpClient<PokeApiClient>(c => c.BaseAddress = PokeApiClient.BaseAddress);
builder.Services.AddScoped<TeamState>(); // scoped = one per Blazor circuit

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.UseAntiforgery();

// Liveness probe for the deploy smoke test (Phase 3).
app.MapGet("/healthz", () => Results.Text("ok"));

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
