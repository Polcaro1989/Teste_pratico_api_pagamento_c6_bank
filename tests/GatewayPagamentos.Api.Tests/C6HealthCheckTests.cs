using GatewayPagamentos.Api.Health;
using GatewayPagamentos.IntegracoesC6;
using GatewayPagamentos.IntegracoesC6.Models;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Moq;
using Xunit;

namespace GatewayPagamentos.Api.Tests;

public class C6HealthCheckTests
{
    [Fact]
    public async Task CheckHealthAsync_ComChamadasFrequentes_DeveExecutarAuthUmaVezPorJanela()
    {
        var tokenClient = new Mock<IC6TokenClient>();
        tokenClient.Setup(x => x.ObterTokenAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new TokenResponse("token-1", "Bearer", 3600));

        var timeProvider = new MutableTimeProvider(new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero));
        var sut = new C6HealthCheck(tokenClient.Object, timeProvider);
        var context = new HealthCheckContext();

        var first = await sut.CheckHealthAsync(context, CancellationToken.None);
        var second = await sut.CheckHealthAsync(context, CancellationToken.None);
        timeProvider.Advance(TimeSpan.FromMinutes(3));
        var third = await sut.CheckHealthAsync(context, CancellationToken.None);

        Assert.Equal(HealthStatus.Healthy, first.Status);
        Assert.Equal(HealthStatus.Healthy, second.Status);
        Assert.Equal(HealthStatus.Healthy, third.Status);
        tokenClient.Verify(x => x.ObterTokenAsync(It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    [Fact]
    public async Task CheckHealthAsync_QuandoAuthFalha_DeveRetornarUnhealthy()
    {
        var tokenClient = new Mock<IC6TokenClient>();
        tokenClient.Setup(x => x.ObterTokenAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("erro"));

        var sut = new C6HealthCheck(tokenClient.Object, new MutableTimeProvider(DateTimeOffset.UtcNow));
        var result = await sut.CheckHealthAsync(new HealthCheckContext(), CancellationToken.None);

        Assert.Equal(HealthStatus.Unhealthy, result.Status);
        Assert.Equal("C6 auth failed", result.Description);
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
