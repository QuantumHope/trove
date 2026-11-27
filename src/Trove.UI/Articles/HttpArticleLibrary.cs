using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Trove.Core.Articles;

namespace Trove.UI.Articles;

/// <summary>The reading list through CORA's <c>/trove/api/articles</c> routes.</summary>
public sealed class HttpArticleLibrary(HttpClient http) : IArticleLibrary
{
    public async Task<ArticleBoard> GetBoardAsync(CancellationToken cancellationToken = default) =>
        await http.GetFromJsonAsync<ArticleBoard>("api/articles", cancellationToken)
        ?? throw new InvalidOperationException("The server sent no reading list.");

    public async Task<AddArticleResult> AddAsync(AddArticleRequest request, CancellationToken cancellationToken = default)
    {
        using var response = await http.PostAsJsonAsync("api/articles", request, cancellationToken);
        if (response.StatusCode == HttpStatusCode.BadRequest)
        {
            throw new ArgumentException(await ErrorAsync(response, "That isn't a web address Trove can save."));
        }

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<AddArticleResult>(cancellationToken)
            ?? throw new InvalidOperationException("The server sent no article.");
    }

    public async Task<Article> MoveAsync(string id, MoveArticleRequest request, CancellationToken cancellationToken = default)
    {
        using var response = await http.PostAsJsonAsync($"api/articles/{Uri.EscapeDataString(id)}/move", request, cancellationToken);
        await ThrowIfRefusedAsync(response);
        return await response.Content.ReadFromJsonAsync<Article>(cancellationToken)
            ?? throw new InvalidOperationException("The server sent no article.");
    }

    public async Task<Article> AddToCoraAsync(string id, CancellationToken cancellationToken = default)
    {
        // A 502 never gets here: the handler under this client turns it into a message, with the module's own words when
        // it was CORA the module couldn't reach.
        using var response = await http.PostAsync($"api/articles/{Uri.EscapeDataString(id)}/add-to-cora", null, cancellationToken);
        await ThrowIfRefusedAsync(response, "CORA couldn't read this page.");
        return await response.Content.ReadFromJsonAsync<Article>(cancellationToken)
            ?? throw new InvalidOperationException("The server sent no article.");
    }

    public async Task RemoveAsync(string id, CancellationToken cancellationToken = default)
    {
        using var response = await http.DeleteAsync($"api/articles/{Uri.EscapeDataString(id)}", cancellationToken);
        await ThrowIfRefusedAsync(response);
    }

    private static async Task ThrowIfRefusedAsync(HttpResponseMessage response, string fallback = "Trove couldn't save that change.")
    {
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            throw new KeyNotFoundException("That article isn't on the list any more.");
        }

        if (response.StatusCode == HttpStatusCode.BadRequest)
        {
            throw new InvalidOperationException(await ErrorAsync(response, fallback));
        }

        response.EnsureSuccessStatusCode();
    }

    // The module answers a refusal with a problem whose detail is in plain words.
    private static async Task<string> ErrorAsync(HttpResponseMessage response, string fallback)
    {
        try
        {
            using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            return CoraProblems.Detail(body.RootElement) ?? fallback;
        }
        catch (JsonException)
        {
            return fallback;
        }
    }
}
