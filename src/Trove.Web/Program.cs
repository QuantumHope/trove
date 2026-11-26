using Microsoft.EntityFrameworkCore;
using Trove.Core.Playthroughs;
using Trove.Web.Components;
using Trove.Web.Data;
using Trove.Web.Playthroughs;
using Trove.Web.YouTube;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
    .AddInteractiveWebAssemblyComponents();

builder.Services.AddTroveDatabase(builder.Configuration, builder.Environment);

builder.Services.AddSingleton(TimeProvider.System);
builder.Services.Configure<YouTubeOptions>(builder.Configuration.GetSection(YouTubeOptions.Section));
builder.Services.AddHttpClient<YouTubeDataApi>();
builder.Services.AddSingleton<YouTubeSync>();
builder.Services.AddHostedService<YouTubeSyncWorker>();
builder.Services.AddScoped<IPlaythroughLibrary, DbPlaythroughLibrary>();

var app = builder.Build();

await app.Services.PrepareTroveDatabaseAsync();

if (app.Environment.IsDevelopment())
{
    app.UseWebAssemblyDebugging();
}
else
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
}

// No HTTPS redirect: Trove listens on localhost over HTTP, and tailscale serve gives it HTTPS on the tailnet.
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseAntiforgery();

app.MapStaticAssets();
app.MapPlaythroughApi();
app.MapRazorComponents<App>()
    .AddInteractiveWebAssemblyRenderMode()
    .AddAdditionalAssemblies(typeof(Trove.UI.Routes).Assembly);

app.Run();

/// <summary>The entry point, visible to the tests' <c>WebApplicationFactory</c>.</summary>
public partial class Program;
