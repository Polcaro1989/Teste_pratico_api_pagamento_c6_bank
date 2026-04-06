using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using GatewayPagamentos.IntegracoesC6.Models;

namespace GatewayPagamentos.IntegracoesC6;

public sealed class C6CheckoutClient : IC6CheckoutClient
{
    private readonly IHttpClientFactory _factory;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public C6CheckoutClient(IHttpClientFactory factory)
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

    public async Task<CheckoutResponse> CriarAsync(string token, CheckoutCriarRequest request, CancellationToken ct = default)
    {
        using var client = CreateApiClient(token);
        var response = await client.PostAsJsonAsync("/v1/checkouts/", request, JsonOptions, ct);
        await EnsureSuccessOrThrowAsync(response, ct);
        return await response.Content.ReadFromJsonAsync<CheckoutResponse>(JsonOptions, ct)
               ?? throw new InvalidOperationException("Resposta de criação vazia");
    }

    public async Task<CheckoutResponse> AutorizarAsync(string token, CheckoutAutorizarRequest request, CancellationToken ct = default)
    {
        using var client = CreateApiClient(token);
        var response = await client.PostAsJsonAsync("/v1/checkouts/authorize", request, JsonOptions, ct);
        await EnsureSuccessOrThrowAsync(response, ct);
        return await response.Content.ReadFromJsonAsync<CheckoutResponse>(JsonOptions, ct)
               ?? throw new InvalidOperationException("Resposta de autorização vazia");
    }

    public async Task<CheckoutResponse> ConsultarAsync(string token, string id, CancellationToken ct = default)
    {
        using var client = CreateApiClient(token);
        using var request = new HttpRequestMessage(HttpMethod.Get, $"/v1/checkouts/{id}");
        request.Content = new FormUrlEncodedContent(Array.Empty<KeyValuePair<string, string>>());
        var response = await client.SendAsync(request, ct);
        await EnsureSuccessOrThrowAsync(response, ct);
        return await response.Content.ReadFromJsonAsync<CheckoutResponse>(JsonOptions, ct)
               ?? throw new InvalidOperationException("Resposta de consulta vazia");
    }

    public async Task CancelarAsync(string token, string id, CancellationToken ct = default)
    {
        using var client = CreateApiClient(token);

        var attempts = new Func<Task<HttpResponseMessage>>[]
        {
            () => SendCancelEmptyContentWithTypeAsync(client, id, "application/json", ct),
            () => SendCancelEmptyContentWithTypeAsync(client, id, "application/x-www-form-urlencoded", ct),
            () => SendCancelNoBodyWithHeaderAsync(client, id, "application/json", ct),
            () => SendCancelFormUrlEncodedAsync(client, id, ct),
            () => SendCancelWithoutBodyAsync(client, id, ct)
        };

        HttpResponseMessage? lastResponse = null;
        string? lastBody = null;

        for (var index = 0; index < attempts.Length; index++)
        {
            lastResponse = await attempts[index]();
            if (lastResponse.IsSuccessStatusCode)
            {
                lastResponse.Dispose();
                return;
            }

            lastBody = await lastResponse.Content.ReadAsStringAsync(ct);
            var canTryNext = index < attempts.Length - 1 && ShouldRetryCancelWithAnotherMediaType(lastResponse.StatusCode, lastBody);
            if (!canTryNext)
            {
                throw BuildHttpRequestException(lastResponse, lastBody);
            }

            lastResponse.Dispose();
        }

        throw lastResponse is null
            ? new HttpRequestException("Falha inesperada ao cancelar checkout: nenhuma tentativa executada")
            : BuildHttpRequestException(lastResponse, lastBody ?? string.Empty);
    }

    private static async Task EnsureSuccessOrThrowAsync(HttpResponseMessage response, CancellationToken ct)
    {
        if (response.IsSuccessStatusCode)
        {
            return;
        }

        var body = await response.Content.ReadAsStringAsync(ct);
        throw BuildHttpRequestException(response, body);
    }

    private static HttpRequestException BuildHttpRequestException(HttpResponseMessage response, string body)
    {
        var bodySnippet = string.IsNullOrWhiteSpace(body)
            ? "sem corpo"
            : (body.Length > 500 ? $"{body[..500]}..." : body);

        return new HttpRequestException(
            $"C6 retornou {(int)response.StatusCode} {response.ReasonPhrase}: {bodySnippet}",
            null,
            response.StatusCode);
    }

    private static bool ShouldRetryCancelWithAnotherMediaType(System.Net.HttpStatusCode statusCode, string body)
    {
        if (statusCode == System.Net.HttpStatusCode.UnsupportedMediaType)
        {
            return true;
        }

        if (statusCode != System.Net.HttpStatusCode.BadRequest)
        {
            return false;
        }

        return body.Contains("Header parameter 'Content-Type' is required", StringComparison.OrdinalIgnoreCase)
               || body.Contains("No request body is expected", StringComparison.OrdinalIgnoreCase);
    }

    private static Task<HttpResponseMessage> SendCancelEmptyContentWithTypeAsync(HttpClient client, string id, string mediaType, CancellationToken ct)
    {
        using var request = new HttpRequestMessage(HttpMethod.Put, $"/v1/checkouts/{id}/cancel");
        var content = new ByteArrayContent(Array.Empty<byte>());
        content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(mediaType);
        request.Content = content;
        return client.SendAsync(request, ct);
    }

    private static Task<HttpResponseMessage> SendCancelNoBodyWithHeaderAsync(HttpClient client, string id, string mediaType, CancellationToken ct)
    {
        using var request = new HttpRequestMessage(HttpMethod.Put, $"/v1/checkouts/{id}/cancel");
        request.Headers.TryAddWithoutValidation("Content-Type", mediaType);
        return client.SendAsync(request, ct);
    }

    private static Task<HttpResponseMessage> SendCancelFormUrlEncodedAsync(HttpClient client, string id, CancellationToken ct)
    {
        using var request = new HttpRequestMessage(HttpMethod.Put, $"/v1/checkouts/{id}/cancel");
        request.Content = new FormUrlEncodedContent(Array.Empty<KeyValuePair<string, string>>());
        return client.SendAsync(request, ct);
    }

    private static Task<HttpResponseMessage> SendCancelWithoutBodyAsync(HttpClient client, string id, CancellationToken ct)
    {
        using var request = new HttpRequestMessage(HttpMethod.Put, $"/v1/checkouts/{id}/cancel");
        return client.SendAsync(request, ct);
    }
}
