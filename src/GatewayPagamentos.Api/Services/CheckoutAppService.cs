using GatewayPagamentos.Api.Contracts;
using GatewayPagamentos.Api.Mappers;
using GatewayPagamentos.IntegracoesC6;

namespace GatewayPagamentos.Api.Services;

public sealed class CheckoutAppService : ICheckoutAppService
{
    private readonly IC6TokenClient _tokenClient;
    private readonly IC6CheckoutClient _checkoutClient;
    private readonly ILogger<CheckoutAppService> _logger;

    public CheckoutAppService(
        IC6TokenClient tokenClient,
        IC6CheckoutClient checkoutClient,
        ILogger<CheckoutAppService> logger)
    {
        _tokenClient = tokenClient;
        _checkoutClient = checkoutClient;
        _logger = logger;
    }

    public async Task<CheckoutResponseDto> CriarAsync(CreateCheckoutRequestDto request, CancellationToken ct)
    {
        var token = await _tokenClient.ObterTokenAsync(ct);
        _logger.LogInformation("Criando checkout Ref={Ref} Valor={Valor}", request.ExternalReferenceId, request.Amount);

        var c6Request = C6CheckoutMapper.ToC6(request);
        var c6Response = await _checkoutClient.CriarAsync(token.AccessToken, c6Request, ct);
        return C6CheckoutMapper.ToApi(c6Response);
    }

    public async Task<CheckoutResponseDto> AutorizarAsync(AuthorizeCheckoutRequestDto request, CancellationToken ct)
    {
        var token = await _tokenClient.ObterTokenAsync(ct);
        _logger.LogInformation("Autorizando checkout Ref={Ref} Valor={Valor}", request.ExternalReferenceId, request.Amount);

        var c6Request = C6CheckoutMapper.ToC6(request);
        var c6Response = await _checkoutClient.AutorizarAsync(token.AccessToken, c6Request, ct);
        return C6CheckoutMapper.ToApi(c6Response);
    }

    public async Task<CheckoutResponseDto> ConsultarAsync(string id, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(id))
            throw new ArgumentException("Id obrigatorio", nameof(id));

        var token = await _tokenClient.ObterTokenAsync(ct);
        _logger.LogInformation("Consultando checkout Id={Id}", id);

        var c6Response = await _checkoutClient.ConsultarAsync(token.AccessToken, id, ct);
        return C6CheckoutMapper.ToApi(c6Response);
    }

    public async Task CancelarAsync(string id, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(id))
            throw new ArgumentException("Id obrigatorio", nameof(id));

        var token = await _tokenClient.ObterTokenAsync(ct);
        _logger.LogInformation("Cancelando checkout Id={Id}", id);
        await _checkoutClient.CancelarAsync(token.AccessToken, id, ct);
    }
}
