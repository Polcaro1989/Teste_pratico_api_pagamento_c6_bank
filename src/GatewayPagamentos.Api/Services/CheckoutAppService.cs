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
        Validar(request);
        var token = await _tokenClient.ObterTokenAsync(ct);
        _logger.LogInformation("Criando checkout Ref={Ref} Valor={Valor}", request.ExternalReferenceId, request.Amount);

        var c6Request = C6CheckoutMapper.ToC6(request);
        var c6Response = await _checkoutClient.CriarAsync(token.AccessToken, c6Request, ct);

        return C6CheckoutMapper.ToApi(c6Response);
    }

    public async Task<CheckoutResponseDto> AutorizarAsync(AuthorizeCheckoutRequestDto request, CancellationToken ct)
    {
        Validar(request);
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

    private static void Validar(CreateCheckoutRequestDto request)
    {
        if (request.Amount <= 0) throw new ArgumentException("Valor deve ser positivo", nameof(request.Amount));
        if (string.IsNullOrWhiteSpace(request.Description)) throw new ArgumentException("Descricao obrigatoria", nameof(request.Description));
        ValidarPagador(request.Payer);
        ValidarPagamento(request.Payment);
        if (string.IsNullOrWhiteSpace(request.RedirectUrl)) throw new ArgumentException("Redirect obrigatorio", nameof(request.RedirectUrl));
    }

    private static void Validar(AuthorizeCheckoutRequestDto request)
    {
        if (request.Amount <= 0) throw new ArgumentException("Valor deve ser positivo", nameof(request.Amount));
        if (string.IsNullOrWhiteSpace(request.Description)) throw new ArgumentException("Descricao obrigatoria", nameof(request.Description));
        ValidarPagador(request.Payer);
        ValidarPagamento(request.Payment);
        if (string.IsNullOrWhiteSpace(request.RedirectUrl)) throw new ArgumentException("Redirect obrigatorio", nameof(request.RedirectUrl));
    }

    private static void ValidarPagador(PayerDto pagador)
    {
        if (pagador is null) throw new ArgumentException("Pagador obrigatorio", nameof(PayerDto));
        if (string.IsNullOrWhiteSpace(pagador.Name)) throw new ArgumentException("Nome do pagador obrigatorio", nameof(pagador.Name));
        if (string.IsNullOrWhiteSpace(pagador.TaxId)) throw new ArgumentException("Documento do pagador obrigatorio", nameof(pagador.TaxId));
        if (string.IsNullOrWhiteSpace(pagador.Email)) throw new ArgumentException("Email do pagador obrigatorio", nameof(pagador.Email));
        if (string.IsNullOrWhiteSpace(pagador.PhoneNumber)) throw new ArgumentException("Telefone do pagador obrigatorio", nameof(pagador.PhoneNumber));
        if (pagador.Address is null) throw new ArgumentException("Endereco obrigatorio", nameof(pagador.Address));
    }

    private static void ValidarPagamento(PaymentDto pagamento)
    {
        if (pagamento is null) throw new ArgumentException("Pagamento obrigatorio", nameof(PaymentDto));
        if (pagamento.Card is null && pagamento.Pix is null)
            throw new ArgumentException("Informe cartao ou pix", nameof(pagamento));
    }
}
