using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Trove.Core.Playthroughs;
using Trove.Core.Settings;
using Trove.UI.Playthroughs;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddScoped(_ => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });
builder.Services.AddScoped<IPlaythroughLibrary, HttpPlaythroughLibrary>();
builder.Services.AddScoped<IYouTubeSettings, HttpYouTubeSettings>();

await builder.Build().RunAsync();
