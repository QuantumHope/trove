using System.Text.Json;

namespace Trove.UI;

/// <summary>Reading the problems CORA and the module answer with.</summary>
public static class CoraProblems
{
    /// <summary>
    /// What a 502 is about, in the words to show. A 502 has two senders. CORA sends one when it can't reach the module,
    /// titled "The Trove module …", and that is said as it always has been. The module sends one when it can't reach
    /// CORA's own services for a person's request, and its detail already says what went wrong and what to try, so that
    /// is shown as written. Null for the first, or for a body that says nothing usable.
    /// </summary>
    public static string? GatewayReason(string body)
    {
        try
        {
            using var problem = JsonDocument.Parse(body);
            var root = problem.RootElement;
            if (root.ValueKind != JsonValueKind.Object)
            {
                return null;
            }

            if (root.TryGetProperty("title", out var title) && title.ValueKind == JsonValueKind.String
                && title.GetString()!.StartsWith("The Trove module", StringComparison.Ordinal))
            {
                return null;
            }

            return Detail(root);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    /// <summary>A problem's detail, when it is a non-empty string.</summary>
    public static string? Detail(JsonElement problem) =>
        problem.ValueKind == JsonValueKind.Object
        && problem.TryGetProperty("detail", out var detail)
        && detail.ValueKind == JsonValueKind.String
        && detail.GetString() is { Length: > 0 } text
            ? text
            : null;
}
