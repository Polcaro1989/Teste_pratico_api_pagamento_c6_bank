using GatewayPagamentos.IntegracoesC6;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace GatewayPagamentos.Api.Health;

public sealed class C6HealthCheck : IHealthCheck
{
    private readonly IC6TokenClient _tokenClient;

    public C6HealthCheck(IC6TokenClient tokenClient)
    {
        _tokenClient = tokenClient;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            await _tokenClient.ObterTokenAsync(cancellationToken);
            return HealthCheckResult.Healthy("C6 auth ok");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("C6 auth failed", ex);
        }
    }
}
