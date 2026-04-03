using System.Net.Http.Json;
using System.Text.Json;
using GatewayPagamentos.IntegracoesC6.Models;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace GatewayPagamentos.IntegracoesC6;

public sealed class C6TokenClient : IC6TokenClient
{
    private const string CacheKey = "c6-auth-token";
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
    };

    private readonly IHttpClientFactory _factory;
    private readonly C6Settings _settings;
    private readonly IMemoryCache _cache;
    private readonly ILogger<C6TokenClient> _logger;
    private readonly TimeProvider _timeProvider;
    private readonly SemaphoreSlim _tokenLock = new(1, 1);

    public C6TokenClient(
        IHttpClientFactory factory,
        C6Settings settings,
        IMemoryCache cache,
        ILogger<C6TokenClient> logger,
        TimeProvider? timeProvider = null)
    {
        _factory = factory;
        _settings = settings;
        _cache = cache;
        _logger = logger;
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    public async Task<TokenResponse> ObterTokenAsync(CancellationToken ct = default)
    {
        if (TryGetCachedToken(out var cachedToken))
        {
            return cachedToken;
        }

        await _tokenLock.WaitAsync(ct);
        try
        {
            if (TryGetCachedToken(out cachedToken))
            {
                return cachedToken;
            }

            var token = await RequestTokenAsync(ct);
            var now = _timeProvider.GetUtcNow();
            var safeTtl = CalculateSafeTtl(token.ExpiresIn);
            var expiresAt = now.Add(safeTtl);

            _cache.Set(CacheKey, new CachedToken(token, expiresAt));

            _logger.LogDebug("Token C6 armazenado em cache ate {ExpiresAtUtc}", expiresAt);
            return token;
        }
        finally
        {
            _tokenLock.Release();
        }
    }

    private async Task<TokenResponse> RequestTokenAsync(CancellationToken ct)
    {
        using var client = _factory.CreateClient("c6-auth");

        var form = new Dictionary<string, string>
        {
            ["grant_type"] = "client_credentials",
            ["client_id"] = _settings.ClientId,
            ["client_secret"] = _settings.ClientSecret
        };

        using var content = new FormUrlEncodedContent(form);
        var response = await client.PostAsync("", content, ct);
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<TokenResponse>(JsonOptions, ct)
               ?? throw new InvalidOperationException("Resposta de token vazia");
    }

    private bool TryGetCachedToken(out TokenResponse token)
    {
        if (_cache.TryGetValue(CacheKey, out CachedToken? cached) &&
            cached is not null &&
            cached.ExpiresAtUtc > _timeProvider.GetUtcNow())
        {
            token = cached.Token;
            return true;
        }

        _cache.Remove(CacheKey);
        token = default!;
        return false;
    }

    private static TimeSpan CalculateSafeTtl(int expiresInSeconds)
    {
        var rawTtl = TimeSpan.FromSeconds(Math.Max(expiresInSeconds, 5));
        var refreshMargin = rawTtl > TimeSpan.FromMinutes(2)
            ? TimeSpan.FromMinutes(1)
            : TimeSpan.FromSeconds(10);

        var safeTtl = rawTtl - refreshMargin;
        return safeTtl < TimeSpan.FromSeconds(5) ? TimeSpan.FromSeconds(5) : safeTtl;
    }

    private sealed record CachedToken(TokenResponse Token, DateTimeOffset ExpiresAtUtc);
}
