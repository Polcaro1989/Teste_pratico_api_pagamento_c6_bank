using System.Text.RegularExpressions;
using System.Text.Json;
using GatewayPagamentos.Api.Contracts;
using GatewayPagamentos.Api.Mappers;
using GatewayPagamentos.IntegracoesC6;

namespace GatewayPagamentos.Api.Services;

public sealed class PixAppService : IPixAppService
{
    private static readonly Regex TxidRegex = new("^[a-zA-Z0-9]{26,35}$", RegexOptions.Compiled);
    private static readonly Regex ChaveRegex = new(@"^\S{1,77}$", RegexOptions.Compiled);

    private readonly IC6TokenClient _tokenClient;
    private readonly IC6PixClient _pixClient;
    private readonly ILogger<PixAppService> _logger;

    public PixAppService(
        IC6TokenClient tokenClient,
        IC6PixClient pixClient,
        ILogger<PixAppService> logger)
    {
        _tokenClient = tokenClient;
        _pixClient = pixClient;
        _logger = logger;
    }

    public async Task<PixCobrancaResponseDto> CriarCobrancaImediataAsync(PixCriarCobrancaRequestDto request, CancellationToken ct)
    {
        var token = await _tokenClient.ObterTokenAsync(ct);
        _logger.LogInformation("Criando cobranca PIX imediata (sem txid) chave={Chave}", request.Chave);

        var c6Request = C6PixMapper.ToC6(request);
        var c6Response = await _pixClient.CriarCobrancaImediataAsync(token.AccessToken, c6Request, ct);
        return C6PixMapper.ToApi(c6Response);
    }

    public async Task<PixCobrancaResponseDto> CriarCobrancaImediataComTxidAsync(string txid, PixCriarCobrancaRequestDto request, CancellationToken ct)
    {
        EnsureTxidValido(txid);

        var token = await _tokenClient.ObterTokenAsync(ct);
        _logger.LogInformation("Criando cobranca PIX imediata txid={Txid}", txid);

        var c6Request = C6PixMapper.ToC6(request);
        var c6Response = await _pixClient.CriarCobrancaImediataComTxidAsync(token.AccessToken, txid, c6Request, ct);
        return C6PixMapper.ToApi(c6Response);
    }

    public async Task<PixCobrancaResponseDto> ConsultarCobrancaImediataAsync(string txid, CancellationToken ct)
    {
        EnsureTxidValido(txid);

        var token = await _tokenClient.ObterTokenAsync(ct);
        _logger.LogInformation("Consultando cobranca PIX txid={Txid}", txid);

        var c6Response = await _pixClient.ConsultarCobrancaImediataAsync(token.AccessToken, txid, ct);
        return C6PixMapper.ToApi(c6Response);
    }

    public async Task<JsonElement> ListarCobrancasImediatasAsync(
        DateTimeOffset inicio,
        DateTimeOffset fim,
        string? cpf,
        string? cnpj,
        string? status,
        bool? locationPresente,
        int? paginaAtual,
        int? itensPorPagina,
        CancellationToken ct)
    {
        EnsurePeriodoValido(inicio, fim);
        EnsureFiltroDocumentoValido(cpf, cnpj);

        var token = await _tokenClient.ObterTokenAsync(ct);
        _logger.LogInformation("Listando cobrancas PIX imediatas inicio={Inicio} fim={Fim}", inicio, fim);

        return await _pixClient.ListarCobrancasImediatasAsync(
            token.AccessToken,
            inicio,
            fim,
            cpf,
            cnpj,
            status,
            locationPresente,
            paginaAtual,
            itensPorPagina,
            ct);
    }

    public async Task<JsonElement> CriarCobrancaComVencimentoAsync(
        string txid,
        PixCriarCobrancaVencimentoRequestDto request,
        CancellationToken ct)
    {
        EnsureTxidValido(txid);

        var token = await _tokenClient.ObterTokenAsync(ct);
        _logger.LogInformation("Criando cobranca PIX com vencimento txid={Txid}", txid);

        var c6Request = C6PixMapper.ToC6(request);
        return await _pixClient.CriarCobrancaComVencimentoAsync(token.AccessToken, txid, c6Request, ct);
    }

    public async Task<JsonElement> ConsultarCobrancaComVencimentoAsync(string txid, CancellationToken ct)
    {
        EnsureTxidValido(txid);

        var token = await _tokenClient.ObterTokenAsync(ct);
        _logger.LogInformation("Consultando cobranca PIX com vencimento txid={Txid}", txid);

        return await _pixClient.ConsultarCobrancaComVencimentoAsync(token.AccessToken, txid, ct);
    }

    public async Task<JsonElement> ListarCobrancasComVencimentoAsync(
        DateTimeOffset inicio,
        DateTimeOffset fim,
        string? cpf,
        string? cnpj,
        string? status,
        bool? locationPresente,
        int? paginaAtual,
        int? itensPorPagina,
        CancellationToken ct)
    {
        EnsurePeriodoValido(inicio, fim);
        EnsureFiltroDocumentoValido(cpf, cnpj);

        var token = await _tokenClient.ObterTokenAsync(ct);
        _logger.LogInformation("Listando cobrancas PIX com vencimento inicio={Inicio} fim={Fim}", inicio, fim);

        return await _pixClient.ListarCobrancasComVencimentoAsync(
            token.AccessToken,
            inicio,
            fim,
            cpf,
            cnpj,
            status,
            locationPresente,
            paginaAtual,
            itensPorPagina,
            ct);
    }

    public async Task<JsonElement> ConfigurarWebhookAsync(string chave, PixConfigurarWebhookRequestDto request, CancellationToken ct)
    {
        EnsureChaveValida(chave);

        var token = await _tokenClient.ObterTokenAsync(ct);
        _logger.LogInformation("Configurando webhook PIX para chave={Chave}", chave);

        var c6Request = C6PixMapper.ToC6(request);
        return await _pixClient.ConfigurarWebhookAsync(token.AccessToken, chave, c6Request, ct);
    }

    private static void EnsureTxidValido(string txid)
    {
        if (string.IsNullOrWhiteSpace(txid))
        {
            throw new ArgumentException("txid obrigatorio", nameof(txid));
        }

        if (!TxidRegex.IsMatch(txid))
        {
            throw new ArgumentException("txid invalido. Use de 26 a 35 caracteres alfanumericos.", nameof(txid));
        }
    }

    private static void EnsurePeriodoValido(DateTimeOffset inicio, DateTimeOffset fim)
    {
        if (inicio == default || fim == default)
        {
            throw new ArgumentException("inicio e fim sao obrigatorios.");
        }

        if (fim < inicio)
        {
            throw new ArgumentException("fim deve ser maior ou igual a inicio.");
        }
    }

    private static void EnsureFiltroDocumentoValido(string? cpf, string? cnpj)
    {
        if (!string.IsNullOrWhiteSpace(cpf) && !string.IsNullOrWhiteSpace(cnpj))
        {
            throw new ArgumentException("Use apenas um filtro de documento: cpf ou cnpj.");
        }
    }

    private static void EnsureChaveValida(string chave)
    {
        if (string.IsNullOrWhiteSpace(chave))
        {
            throw new ArgumentException("chave obrigatoria.", nameof(chave));
        }

        if (!ChaveRegex.IsMatch(chave))
        {
            throw new ArgumentException("chave invalida.", nameof(chave));
        }
    }
}
