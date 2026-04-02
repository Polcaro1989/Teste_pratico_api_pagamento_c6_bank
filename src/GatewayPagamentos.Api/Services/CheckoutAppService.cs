using GatewayPagamentos.IntegracoesC6;
using GatewayPagamentos.IntegracoesC6.Models;

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

    public async Task<CheckoutResponse> CriarAsync(CheckoutCriarRequest request, CancellationToken ct)
    {
        Validar(request);
        var token = await _tokenClient.ObterTokenAsync(ct);
        _logger.LogInformation("Criando checkout Ref={Ref} Valor={Valor}", request.ReferenciaExterna, request.Valor);
        return await _checkoutClient.CriarAsync(token.AccessToken, request, ct);
    }

    public async Task<CheckoutResponse> AutorizarAsync(CheckoutAutorizarRequest request, CancellationToken ct)
    {
        Validar(request);
        var token = await _tokenClient.ObterTokenAsync(ct);
        _logger.LogInformation("Autorizando checkout Ref={Ref} Valor={Valor}", request.ReferenciaExterna, request.Valor);
        return await _checkoutClient.AutorizarAsync(token.AccessToken, request, ct);
    }

    public async Task<CheckoutResponse> ConsultarAsync(string id, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(id))
            throw new ArgumentException("Id obrigatório", nameof(id));

        var token = await _tokenClient.ObterTokenAsync(ct);
        _logger.LogInformation("Consultando checkout Id={Id}", id);
        return await _checkoutClient.ConsultarAsync(token.AccessToken, id, ct);
    }

    public async Task CancelarAsync(string id, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(id))
            throw new ArgumentException("Id obrigatório", nameof(id));

        var token = await _tokenClient.ObterTokenAsync(ct);
        _logger.LogInformation("Cancelando checkout Id={Id}", id);
        await _checkoutClient.CancelarAsync(token.AccessToken, id, ct);
    }

    private static void Validar(CheckoutCriarRequest request)
    {
        if (request.Valor <= 0) throw new ArgumentException("Valor deve ser positivo", nameof(request.Valor));
        if (string.IsNullOrWhiteSpace(request.Descricao)) throw new ArgumentException("Descrição obrigatória", nameof(request.Descricao));
        ValidarPagador(request.Pagador);
        ValidarPagamento(request.Pagamento);
        if (string.IsNullOrWhiteSpace(request.UrlRedirect)) throw new ArgumentException("Redirect obrigatório", nameof(request.UrlRedirect));
    }

    private static void Validar(CheckoutAutorizarRequest request)
    {
        if (request.Valor <= 0) throw new ArgumentException("Valor deve ser positivo", nameof(request.Valor));
        if (string.IsNullOrWhiteSpace(request.Descricao)) throw new ArgumentException("Descrição obrigatória", nameof(request.Descricao));
        ValidarPagador(request.Pagador);
        ValidarPagamento(request.Pagamento);
        if (string.IsNullOrWhiteSpace(request.UrlRedirect)) throw new ArgumentException("Redirect obrigatório", nameof(request.UrlRedirect));
    }

    private static void ValidarPagador(Pagador pagador)
    {
        if (pagador is null) throw new ArgumentException("Pagador obrigatório", nameof(Pagador));
        if (string.IsNullOrWhiteSpace(pagador.Nome)) throw new ArgumentException("Nome do pagador obrigatório", nameof(pagador.Nome));
        if (string.IsNullOrWhiteSpace(pagador.Documento)) throw new ArgumentException("Documento do pagador obrigatório", nameof(pagador.Documento));
        if (string.IsNullOrWhiteSpace(pagador.Email)) throw new ArgumentException("Email do pagador obrigatório", nameof(pagador.Email));
        if (pagador.Endereco is null) throw new ArgumentException("Endereço obrigatório", nameof(pagador.Endereco));
    }

    private static void ValidarPagamento(Pagamento pagamento)
    {
        if (pagamento is null) throw new ArgumentException("Pagamento obrigatório", nameof(Pagamento));
        if (pagamento.Cartao is null && pagamento.Pix is null)
            throw new ArgumentException("Informe cartão ou pix", nameof(pagamento));
    }
}
