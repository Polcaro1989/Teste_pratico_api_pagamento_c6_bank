using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using GatewayPagamentos.IntegracoesC6.Models;

namespace GatewayPagamentos.IntegracoesC6;

public sealed class C6PixClient : IC6PixClient
{
    private readonly IHttpClientFactory _factory;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public C6PixClient(IHttpClientFactory factory)
    {
        _factory = factory;
    }

    private HttpClient CreateApiClient(string token)
    {
        var client = _factory.CreateClient("c6-api");
        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    public async Task<PixCobrancaResponse> CriarCobrancaImediataAsync(
        string token,
        PixCriarCobrancaRequest request,
        CancellationToken ct = default)
    {
        using var client = CreateApiClient(token);
        var response = await client.PostAsJsonAsync("/v2/pix/cob", request, JsonOptions, ct);
        await EnsureSuccessOrThrowAsync(response, ct);
        return await response.Content.ReadFromJsonAsync<PixCobrancaResponse>(JsonOptions, ct)
               ?? throw new InvalidOperationException("Resposta de criação PIX vazia");
    }

    public async Task<PixCobrancaResponse> CriarCobrancaImediataComTxidAsync(
        string token,
        string txid,
        PixCriarCobrancaRequest request,
        CancellationToken ct = default)
    {
        using var client = CreateApiClient(token);
        var response = await client.PutAsJsonAsync($"/v2/pix/cob/{txid}", request, JsonOptions, ct);
        await EnsureSuccessOrThrowAsync(response, ct);
        return await response.Content.ReadFromJsonAsync<PixCobrancaResponse>(JsonOptions, ct)
               ?? throw new InvalidOperationException("Resposta de criação PIX com txid vazia");
    }

    public async Task<PixCobrancaResponse> ConsultarCobrancaImediataAsync(
        string token,
        string txid,
        CancellationToken ct = default)
    {
        using var client = CreateApiClient(token);
        var response = await client.GetAsync($"/v2/pix/cob/{txid}", ct);
        await EnsureSuccessOrThrowAsync(response, ct);
        return await response.Content.ReadFromJsonAsync<PixCobrancaResponse>(JsonOptions, ct)
               ?? throw new InvalidOperationException("Resposta de consulta PIX vazia");
    }

    public async Task<JsonElement> ListarCobrancasImediatasAsync(
        string token,
        DateTimeOffset inicio,
        DateTimeOffset fim,
        string? cpf,
        string? cnpj,
        string? status,
        bool? locationPresente,
        int? paginaAtual,
        int? itensPorPagina,
        CancellationToken ct = default)
    {
        using var client = CreateApiClient(token);
        var url = BuildUrl(
            "/v2/pix/cob",
            [
                ("inicio", inicio.ToString("yyyy-MM-ddTHH:mm:ssK")),
                ("fim", fim.ToString("yyyy-MM-ddTHH:mm:ssK")),
                ("cpf", cpf),
                ("cnpj", cnpj),
                ("status", status),
                ("locationPresente", locationPresente?.ToString().ToLowerInvariant()),
                ("paginacao.paginaAtual", paginaAtual?.ToString()),
                ("paginacao.itensPorPagina", itensPorPagina?.ToString())
            ]);

        var response = await client.GetAsync(url, ct);
        await EnsureSuccessOrThrowAsync(response, ct);
        return await ReadJsonElementAsync(response, ct);
    }

    public async Task<JsonElement> CriarCobrancaComVencimentoAsync(
        string token,
        string txid,
        PixCriarCobvRequest request,
        CancellationToken ct = default)
    {
        using var client = CreateApiClient(token);
        var response = await client.PutAsJsonAsync($"/v2/pix/cobv/{txid}", request, JsonOptions, ct);
        await EnsureSuccessOrThrowAsync(response, ct);
        return await ReadJsonElementAsync(response, ct);
    }

    public async Task<JsonElement> ConsultarCobrancaComVencimentoAsync(
        string token,
        string txid,
        CancellationToken ct = default)
    {
        using var client = CreateApiClient(token);
        var response = await client.GetAsync($"/v2/pix/cobv/{txid}", ct);
        await EnsureSuccessOrThrowAsync(response, ct);
        return await ReadJsonElementAsync(response, ct);
    }

    public async Task<JsonElement> ListarCobrancasComVencimentoAsync(
        string token,
        DateTimeOffset inicio,
        DateTimeOffset fim,
        string? cpf,
        string? cnpj,
        string? status,
        bool? locationPresente,
        int? paginaAtual,
        int? itensPorPagina,
        CancellationToken ct = default)
    {
        using var client = CreateApiClient(token);
        var url = BuildUrl(
            "/v2/pix/cobv",
            [
                ("inicio", inicio.ToString("yyyy-MM-ddTHH:mm:ssK")),
                ("fim", fim.ToString("yyyy-MM-ddTHH:mm:ssK")),
                ("cpf", cpf),
                ("cnpj", cnpj),
                ("status", status),
                ("locationPresente", locationPresente?.ToString().ToLowerInvariant()),
                ("paginacao.paginaAtual", paginaAtual?.ToString()),
                ("paginacao.itensPorPagina", itensPorPagina?.ToString())
            ]);

        var response = await client.GetAsync(url, ct);
        await EnsureSuccessOrThrowAsync(response, ct);
        return await ReadJsonElementAsync(response, ct);
    }

    public async Task<JsonElement> ConfigurarWebhookAsync(
        string token,
        string chave,
        PixConfigurarWebhookRequest request,
        CancellationToken ct = default)
    {
        using var client = CreateApiClient(token);
        var response = await client.PutAsJsonAsync($"/v2/pix/webhook/{Uri.EscapeDataString(chave)}", request, JsonOptions, ct);
        await EnsureSuccessOrThrowAsync(response, ct);
        return await ReadJsonElementAsync(response, ct);
    }

    private static async Task EnsureSuccessOrThrowAsync(HttpResponseMessage response, CancellationToken ct)
    {
        if (response.IsSuccessStatusCode)
        {
            return;
        }

        var body = await response.Content.ReadAsStringAsync(ct);
        var bodySnippet = string.IsNullOrWhiteSpace(body)
            ? "sem corpo"
            : (body.Length > 500 ? $"{body[..500]}..." : body);

        throw new HttpRequestException(
            $"C6 PIX retornou {(int)response.StatusCode} {response.ReasonPhrase}: {bodySnippet}",
            null,
            response.StatusCode);
    }

    private static string BuildUrl(string basePath, IEnumerable<(string Key, string? Value)> query)
    {
        var list = query
            .Where(x => !string.IsNullOrWhiteSpace(x.Value))
            .Select(x => $"{Uri.EscapeDataString(x.Key)}={Uri.EscapeDataString(x.Value!)}")
            .ToList();

        if (list.Count == 0)
        {
            return basePath;
        }

        return $"{basePath}?{string.Join("&", list)}";
    }

    private static async Task<JsonElement> ReadJsonElementAsync(HttpResponseMessage response, CancellationToken ct)
    {
        var body = await response.Content.ReadAsStringAsync(ct);
        if (string.IsNullOrWhiteSpace(body))
        {
            return JsonDocument.Parse("{}").RootElement.Clone();
        }

        return JsonDocument.Parse(body).RootElement.Clone();
    }
}
