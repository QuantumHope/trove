using Trove.Core.Playthroughs;

namespace Trove.Web.Playthroughs;

/// <summary>The API the browser's board calls.</summary>
public static class PlaythroughEndpoints
{
    public static IEndpointRouteBuilder MapPlaythroughApi(this IEndpointRouteBuilder app)
    {
        var api = app.MapGroup("/api/playthroughs").DisableAntiforgery();

        api.MapGet("", (IPlaythroughLibrary library, CancellationToken ct) => library.GetBoardAsync(ct));

        api.MapPost("{videoId}/move", (string videoId, MoveRequest request, IPlaythroughLibrary library, CancellationToken ct) =>
            Handle(() => library.MoveAsync(videoId, request, ct)));

        api.MapPut("{videoId}/rating", (string videoId, RateRequest request, IPlaythroughLibrary library, CancellationToken ct) =>
            Handle(() => library.RateAsync(videoId, request.Rating, ct)));

        api.MapPost("sync", (IPlaythroughLibrary library, CancellationToken ct) => library.SyncAsync(ct));

        return app;
    }

    private static async Task<IResult> Handle(Func<Task<Playthrough>> action)
    {
        try
        {
            return TypedResults.Ok(await action());
        }
        catch (KeyNotFoundException ex)
        {
            return TypedResults.Problem(ex.Message, statusCode: StatusCodes.Status404NotFound);
        }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException)
        {
            return TypedResults.Problem(ex.Message, statusCode: StatusCodes.Status400BadRequest);
        }
    }
}
