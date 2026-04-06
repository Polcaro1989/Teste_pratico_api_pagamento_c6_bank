using GatewayPagamentos.IntegracoesC6.Models;

namespace GatewayPagamentos.IntegracoesC6;

public interface IC6CheckoutClient
{
    Task<CheckoutResponse> CriarAsync(string token, CheckoutCriarRequest request, CancellationToken ct = default);
    Task<CheckoutResponse> AutorizarAsync(string token, CheckoutAutorizarRequest request, CancellationToken ct = default);
    Task<CheckoutResponse> ConsultarAsync(string token, string id, CancellationToken ct = default);
    Task CancelarAsync(string token, string id, CancellationToken ct = default);
}
