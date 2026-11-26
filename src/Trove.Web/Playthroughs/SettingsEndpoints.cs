using Trove.Core.Settings;

namespace Trove.Web.Playthroughs;

/// <summary>The Settings screen's API. The key goes in; only its last four characters ever come back out.</summary>
public static class SettingsEndpoints
{
    public static IEndpointRouteBuilder MapSettingsApi(this IEndpointRouteBuilder app)
    {
        var api = app.MapGroup("/api/settings/youtube").DisableAntiforgery();

        api.MapGet("", (IYouTubeSettings settings, CancellationToken ct) => settings.GetAsync(ct));

        api.MapPut("key", (SaveYouTubeKeyRequest request, IYouTubeSettings settings, CancellationToken ct) =>
            settings.SaveKeyAsync(request.ApiKey, ct));

        api.MapDelete("key", (IYouTubeSettings settings, CancellationToken ct) => settings.RemoveKeyAsync(ct));

        return app;
    }
}
