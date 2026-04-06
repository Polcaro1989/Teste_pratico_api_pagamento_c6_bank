using GatewayPagamentos.Api.Contracts;

namespace GatewayPagamentos.Api.Services;

public interface ICheckoutAppService
{
    Task<CheckoutResponseDto> CriarAsync(CreateCheckoutRequestDto request, CancellationToken ct);
    Task<CheckoutResponseDto> AutorizarAsync(AuthorizeCheckoutRequestDto request, CancellationToken ct);
    Task<CheckoutResponseDto> ConsultarAsync(string id, CancellationToken ct);
    Task CancelarAsync(string id, CancellationToken ct);
}
