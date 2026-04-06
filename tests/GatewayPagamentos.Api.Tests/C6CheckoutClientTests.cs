using System.Net;
using System.Text;
using GatewayPagamentos.IntegracoesC6;
using Xunit;

namespace GatewayPagamentos.Api.Tests;

public class C6CheckoutClientTests
{
    [Fact]
    public async Task CancelarAsync_QuandoPrimeiraTentativaFunciona_DeveChamarUmaVez()
    {
        var handler = new SequencedCancelHandler(
        [
            new HttpResponseMessage(HttpStatusCode.NoContent)
        ]);

        var sut = CriarSut(handler);

        await sut.CancelarAsync("token", "chk-123");

        Assert.Equal(1, handler.CallCount);
    }

    [Fact]
    public async Task CancelarAsync_QuandoPrimeiraRetorna415_DeveUsarFallbackEAceitarSucesso()
    {
        var handler = new SequencedCancelHandler(
        [
            new HttpResponseMessage(HttpStatusCode.UnsupportedMediaType)
            {
                Content = new StringContent("{\"detail\":\"Unsupported\"}", Encoding.UTF8, "application/json")
            },
            new HttpResponseMessage(HttpStatusCode.NoContent)
        ]);

        var sut = CriarSut(handler);

        await sut.CancelarAsync("token", "chk-123");

        Assert.Equal(2, handler.CallCount);
    }

    [Fact]
    public async Task CancelarAsync_QuandoTodasTentativasFalham_PropagaHttpRequestException()
    {
        var handler = new SequencedCancelHandler(
        [
            new HttpResponseMessage(HttpStatusCode.UnsupportedMediaType),
            new HttpResponseMessage(HttpStatusCode.UnsupportedMediaType),
            new HttpResponseMessage(HttpStatusCode.UnsupportedMediaType),
            new HttpResponseMessage(HttpStatusCode.UnsupportedMediaType),
            new HttpResponseMessage(HttpStatusCode.UnsupportedMediaType)
            {
                Content = new StringContent("{\"detail\":\"Unsupported Media Type\"}", Encoding.UTF8, "application/json")
            }
        ]);

        var sut = CriarSut(handler);

        var ex = await Assert.ThrowsAsync<HttpRequestException>(() => sut.CancelarAsync("token", "chk-123"));

        Assert.Equal(HttpStatusCode.UnsupportedMediaType, ex.StatusCode);
        Assert.Equal(5, handler.CallCount);
    }

    private static C6CheckoutClient CriarSut(HttpMessageHandler handler)
    {
        var factory = new FakeHttpClientFactory(handler);
        return new C6CheckoutClient(factory);
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
                BaseAddress = new Uri("https://api.test")
            };
        }
    }

    private sealed class SequencedCancelHandler : HttpMessageHandler
    {
        private readonly Queue<HttpResponseMessage> _responses;

        public SequencedCancelHandler(IEnumerable<HttpResponseMessage> responses)
        {
            _responses = new Queue<HttpResponseMessage>(responses);
        }

        public int CallCount { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            CallCount++;

            if (_responses.Count == 0)
            {
                throw new InvalidOperationException("Sem respostas configuradas para o handler");
            }

            return Task.FromResult(_responses.Dequeue());
        }
    }
}
