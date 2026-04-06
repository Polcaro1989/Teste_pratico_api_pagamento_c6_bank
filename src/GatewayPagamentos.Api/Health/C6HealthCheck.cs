using GatewayPagamentos.IntegracoesC6;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace GatewayPagamentos.Api.Health;

public sealed class C6HealthCheck : IHealthCheck
{
    private static readonly TimeSpan ProbeInterval = TimeSpan.FromMinutes(2);

    private readonly IC6TokenClient _tokenClient;
    private readonly TimeProvider _timeProvider;
    private readonly SemaphoreSlim _probeLock = new(1, 1);

    private DateTimeOffset _nextProbeUtc = DateTimeOffset.MinValue;
    private HealthCheckResult _lastResult = HealthCheckResult.Degraded("Aguardando primeira validacao de auth C6");

    public C6HealthCheck(IC6TokenClient tokenClient, TimeProvider? timeProvider = null)
    {
        _tokenClient = tokenClient;
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        var now = _timeProvider.GetUtcNow();
        if (now < _nextProbeUtc)
        {
            return _lastResult;
        }

        await _probeLock.WaitAsync(cancellationToken);
        try
        {
            now = _timeProvider.GetUtcNow();
            if (now < _nextProbeUtc)
            {
                return _lastResult;
            }

            try
            {
                await _tokenClient.ObterTokenAsync(cancellationToken);
                _lastResult = HealthCheckResult.Healthy("C6 auth ok");
            }
            catch (Exception ex)
            {
                _lastResult = HealthCheckResult.Unhealthy("C6 auth failed", ex);
            }

            _nextProbeUtc = now.Add(ProbeInterval);
            return _lastResult;
        }
        finally
        {
            _probeLock.Release();
        }
    }
}
