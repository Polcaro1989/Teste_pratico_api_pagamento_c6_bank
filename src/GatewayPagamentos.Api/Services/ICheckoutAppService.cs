using GatewayPagamentos.IntegracoesC6.Models;

namespace GatewayPagamentos.Api.Services;

public interface ICheckoutAppService
{
    Task<CheckoutResponse> CriarAsync(CheckoutCriarRequest request, CancellationToken ct);
    Task<CheckoutResponse> AutorizarAsync(CheckoutAutorizarRequest request, CancellationToken ct);
    Task<CheckoutResponse> ConsultarAsync(string id, CancellationToken ct);
    Task CancelarAsync(string id, CancellationToken ct);
}
