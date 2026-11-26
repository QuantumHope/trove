using Trove.Web.Components;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
    .AddInteractiveWebAssemblyComponents();

var app = builder.Build();

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
app.MapRazorComponents<App>()
    .AddInteractiveWebAssemblyRenderMode()
    .AddAdditionalAssemblies(typeof(Trove.UI.Routes).Assembly);

app.Run();

/// <summary>The entry point, visible to the tests' <c>WebApplicationFactory</c>.</summary>
public partial class Program;
