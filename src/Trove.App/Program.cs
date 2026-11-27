using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Trove.App;
using Trove.Core.Articles;
using Trove.Core.Playthroughs;
using Trove.UI;
using Trove.UI.Articles;
using Trove.UI.Playthroughs;
using Trove.UI.Setup;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<Routes>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddSingleton(TimeProvider.System);
// The base address is /trove/, so "api/playthroughs" lands on CORA's /trove/api/playthroughs.
builder.Services.AddScoped(sp => new HttpClient(new CoraApiHandler(sp.GetRequiredService<NavigationManager>()) { InnerHandler = new HttpClientHandler() })
{
    BaseAddress = new Uri(builder.HostEnvironment.BaseAddress),
});
builder.Services.AddScoped<IPlaythroughLibrary, HttpPlaythroughLibrary>();
builder.Services.AddScoped<IArticleLibrary, HttpArticleLibrary>();
builder.Services.AddScoped<ITroveSetup, HttpTroveSetup>();

await builder.Build().RunAsync();
