using GatewayPagamentos.IntegracoesC6.Models;

namespace GatewayPagamentos.IntegracoesC6;

public interface IC6TokenClient
{
    Task<TokenResponse> ObterTokenAsync(CancellationToken ct = default);
}
