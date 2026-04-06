using GatewayPagamentos.Api.Contracts;
using System.Text.Json;

namespace GatewayPagamentos.Api.Services;

public interface IPixAppService
{
    Task<PixCobrancaResponseDto> CriarCobrancaImediataAsync(PixCriarCobrancaRequestDto request, CancellationToken ct);
    Task<PixCobrancaResponseDto> CriarCobrancaImediataComTxidAsync(string txid, PixCriarCobrancaRequestDto request, CancellationToken ct);
    Task<PixCobrancaResponseDto> ConsultarCobrancaImediataAsync(string txid, CancellationToken ct);
    Task<JsonElement> ListarCobrancasImediatasAsync(
        DateTimeOffset inicio,
        DateTimeOffset fim,
        string? cpf,
        string? cnpj,
        string? status,
        bool? locationPresente,
        int? paginaAtual,
        int? itensPorPagina,
        CancellationToken ct);
    Task<JsonElement> CriarCobrancaComVencimentoAsync(string txid, PixCriarCobrancaVencimentoRequestDto request, CancellationToken ct);
    Task<JsonElement> ConsultarCobrancaComVencimentoAsync(string txid, CancellationToken ct);
    Task<JsonElement> ListarCobrancasComVencimentoAsync(
        DateTimeOffset inicio,
        DateTimeOffset fim,
        string? cpf,
        string? cnpj,
        string? status,
        bool? locationPresente,
        int? paginaAtual,
        int? itensPorPagina,
        CancellationToken ct);
    Task<JsonElement> ConfigurarWebhookAsync(string chave, PixConfigurarWebhookRequestDto request, CancellationToken ct);
}
