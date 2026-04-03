using System.Net;
using System.Text;
using GatewayPagamentos.IntegracoesC6;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace GatewayPagamentos.Api.Tests;

public class C6TokenClientTests
{
    [Fact]
    public async Task ObterTokenAsync_QuandoTokenAindaValido_DeveReutilizarCache()
    {
        var handler = new SequencedTokenHandler([("token-1", 120)]);
        var timeProvider = new MutableTimeProvider(new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero));
        var sut = CriarSut(handler, timeProvider);

        var first = await sut.ObterTokenAsync();
        var second = await sut.ObterTokenAsync();

        Assert.Equal("token-1", first.AccessToken);
        Assert.Equal("token-1", second.AccessToken);
        Assert.Equal(1, handler.CallCount);
    }

    [Fact]
    public async Task ObterTokenAsync_QuandoTokenExpira_DeveRenovar()
    {
        var handler = new SequencedTokenHandler([("token-1", 15), ("token-2", 15)]);
        var timeProvider = new MutableTimeProvider(new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero));
        var sut = CriarSut(handler, timeProvider);

        var first = await sut.ObterTokenAsync();
        timeProvider.Advance(TimeSpan.FromSeconds(4));
        var cached = await sut.ObterTokenAsync();
        timeProvider.Advance(TimeSpan.FromSeconds(2));
        var refreshed = await sut.ObterTokenAsync();

        Assert.Equal("token-1", first.AccessToken);
        Assert.Equal("token-1", cached.AccessToken);
        Assert.Equal("token-2", refreshed.AccessToken);
        Assert.Equal(2, handler.CallCount);
    }

    private static C6TokenClient CriarSut(SequencedTokenHandler handler, TimeProvider timeProvider)
    {
        var settings = new C6Settings
        {
            BaseUrl = "https://api.test",
            TokenUrl = "https://auth.test",
            ClientId = "client-id",
            ClientSecret = "client-secret",
            ClientCertificatePath = "cert.pfx",
            ClientCertificatePassword = "pwd",
            AllowInsecureServerCertificateInDevelopment = false
        };

        var cache = new MemoryCache(new MemoryCacheOptions());
        var logger = new Mock<ILogger<C6TokenClient>>().Object;
        var factory = new FakeHttpClientFactory(handler);

        return new C6TokenClient(factory, settings, cache, logger, timeProvider);
    }

    private sealed class FakeHttpClientFactory : IHttpClientFactory
    {
        private readonly HttpMessageHandler _handler;

        public FakeHttpClientFactory(HttpMessageHandler handler)
        {
            _handler = handler;
        }

        public HttpClient CreateClient(string name)
        {
            return new HttpClient(_handler, disposeHandler: false)
            {
                BaseAddress = new Uri("https://auth.test")
            };
        }
    }

    private sealed class SequencedTokenHandler : HttpMessageHandler
    {
        private readonly Queue<(string AccessToken, int ExpiresIn)> _responses;

        public SequencedTokenHandler(IEnumerable<(string AccessToken, int ExpiresIn)> responses)
        {
            _responses = new Queue<(string AccessToken, int ExpiresIn)>(responses);
        }

        public int CallCount { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            CallCount++;
            if (_responses.Count == 0)
            {
                throw new InvalidOperationException("Sem respostas configuradas para o handler");
            }

            var response = _responses.Dequeue();
            var json = $$"""
                {"access_token":"{{response.AccessToken}}","token_type":"Bearer","expires_in":{{response.ExpiresIn}}}
                """;

            return Task.FromResult(
                new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(json, Encoding.UTF8, "application/json")
                });
        }
    }

    private sealed class MutableTimeProvider : TimeProvider
    {
        private DateTimeOffset _current;

        public MutableTimeProvider(DateTimeOffset current)
        {
            _current = current;
        }

        public override DateTimeOffset GetUtcNow() => _current;

        public void Advance(TimeSpan delta)
        {
            _current = _current.Add(delta);
        }
    }
}
