using GatewayPagamentos.IntegracoesC6.Models;
using System.Text.Json;

namespace GatewayPagamentos.IntegracoesC6;

public interface IC6PixClient
{
    Task<PixCobrancaResponse> CriarCobrancaImediataAsync(
        string token,
        PixCriarCobrancaRequest request,
        CancellationToken ct = default);

    Task<PixCobrancaResponse> CriarCobrancaImediataComTxidAsync(
        string token,
        string txid,
        PixCriarCobrancaRequest request,
        CancellationToken ct = default);

    Task<PixCobrancaResponse> ConsultarCobrancaImediataAsync(
        string token,
        string txid,
        CancellationToken ct = default);

    Task<JsonElement> ListarCobrancasImediatasAsync(
        string token,
        DateTimeOffset inicio,
        DateTimeOffset fim,
        string? cpf,
        string? cnpj,
        string? status,
        bool? locationPresente,
        int? paginaAtual,
        int? itensPorPagina,
        CancellationToken ct = default);

    Task<JsonElement> CriarCobrancaComVencimentoAsync(
        string token,
        string txid,
        PixCriarCobvRequest request,
        CancellationToken ct = default);

    Task<JsonElement> ConsultarCobrancaComVencimentoAsync(
        string token,
        string txid,
        CancellationToken ct = default);

    Task<JsonElement> ListarCobrancasComVencimentoAsync(
        string token,
        DateTimeOffset inicio,
        DateTimeOffset fim,
        string? cpf,
        string? cnpj,
        string? status,
        bool? locationPresente,
        int? paginaAtual,
        int? itensPorPagina,
        CancellationToken ct = default);

    Task<JsonElement> ConfigurarWebhookAsync(
        string token,
        string chave,
        PixConfigurarWebhookRequest request,
        CancellationToken ct = default);
}
