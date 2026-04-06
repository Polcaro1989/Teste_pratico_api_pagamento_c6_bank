using System.Text.Json;
using System.Text.Json.Serialization;

namespace GatewayPagamentos.IntegracoesC6.Models;

public sealed record PixCobrancaCalendario(
    [property: JsonPropertyName("expiracao")] int Expiracao);

public sealed record PixCobrancaDevedor(
    [property: JsonPropertyName("cpf")] string? Cpf,
    [property: JsonPropertyName("cnpj")] string? Cnpj,
    [property: JsonPropertyName("nome")] string? Nome);

public sealed record PixCobrancaValor(
    [property: JsonPropertyName("original")] string Original,
    [property: JsonPropertyName("modalidadeAlteracao")] int? ModalidadeAlteracao = null);

public sealed record PixCobvValor(
    [property: JsonPropertyName("original")] string Original);

public sealed record PixInfoAdicional(
    [property: JsonPropertyName("nome")] string Nome,
    [property: JsonPropertyName("valor")] string Valor);

public sealed record PixCriarCobrancaRequest(
    [property: JsonPropertyName("calendario")] PixCobrancaCalendario Calendario,
    [property: JsonPropertyName("devedor")] PixCobrancaDevedor? Devedor,
    [property: JsonPropertyName("valor")] PixCobrancaValor Valor,
    [property: JsonPropertyName("chave")] string Chave,
    [property: JsonPropertyName("solicitacaoPagador")] string? SolicitacaoPagador,
    [property: JsonPropertyName("infoAdicionais")] IReadOnlyList<PixInfoAdicional>? InfoAdicionais);

public sealed record PixCobvCalendario(
    [property: JsonPropertyName("dataDeVencimento")] string DataDeVencimento,
    [property: JsonPropertyName("validadeAposVencimento")] int? ValidadeAposVencimento = null);

public sealed record PixCobvDevedor(
    [property: JsonPropertyName("logradouro")] string? Logradouro,
    [property: JsonPropertyName("cidade")] string? Cidade,
    [property: JsonPropertyName("uf")] string? Uf,
    [property: JsonPropertyName("cep")] string? Cep,
    [property: JsonPropertyName("cpf")] string? Cpf,
    [property: JsonPropertyName("cnpj")] string? Cnpj,
    [property: JsonPropertyName("nome")] string? Nome);

public sealed record PixCriarCobvRequest(
    [property: JsonPropertyName("calendario")] PixCobvCalendario Calendario,
    [property: JsonPropertyName("devedor")] PixCobvDevedor? Devedor,
    [property: JsonPropertyName("valor")] PixCobvValor Valor,
    [property: JsonPropertyName("chave")] string Chave,
    [property: JsonPropertyName("solicitacaoPagador")] string? SolicitacaoPagador,
    [property: JsonPropertyName("infoAdicionais")] IReadOnlyList<PixInfoAdicional>? InfoAdicionais);

public sealed record PixConfigurarWebhookRequest(
    [property: JsonPropertyName("webhookUrl")] string WebhookUrl);

public sealed class PixCobrancaCalendarioResponse
{
    [JsonPropertyName("criacao")]
    public string? Criacao { get; init; }

    [JsonPropertyName("expiracao")]
    public int? Expiracao { get; init; }

    [JsonExtensionData]
    public Dictionary<string, JsonElement>? AdditionalData { get; init; }
}

public sealed class PixCobrancaLocResponse
{
    [JsonPropertyName("id")]
    public int? Id { get; init; }

    [JsonPropertyName("location")]
    public string? Location { get; init; }

    [JsonPropertyName("tipoCob")]
    public string? TipoCob { get; init; }

    [JsonExtensionData]
    public Dictionary<string, JsonElement>? AdditionalData { get; init; }
}

public sealed class PixCobrancaValorResponse
{
    [JsonPropertyName("original")]
    public string? Original { get; init; }

    [JsonPropertyName("modalidadeAlteracao")]
    public int? ModalidadeAlteracao { get; init; }

    [JsonExtensionData]
    public Dictionary<string, JsonElement>? AdditionalData { get; init; }
}

public sealed class PixCobrancaResponse
{
    [JsonPropertyName("calendario")]
    public PixCobrancaCalendarioResponse? Calendario { get; init; }

    [JsonPropertyName("txid")]
    public string? Txid { get; init; }

    [JsonPropertyName("revisao")]
    public int? Revisao { get; init; }

    [JsonPropertyName("loc")]
    public PixCobrancaLocResponse? Loc { get; init; }

    [JsonPropertyName("location")]
    public string? Location { get; init; }

    [JsonPropertyName("status")]
    public string? Status { get; init; }

    [JsonPropertyName("devedor")]
    public PixCobrancaDevedor? Devedor { get; init; }

    [JsonPropertyName("valor")]
    public PixCobrancaValorResponse? Valor { get; init; }

    [JsonPropertyName("chave")]
    public string? Chave { get; init; }

    [JsonPropertyName("solicitacaoPagador")]
    public string? SolicitacaoPagador { get; init; }

    [JsonPropertyName("infoAdicionais")]
    public IReadOnlyList<PixInfoAdicional>? InfoAdicionais { get; init; }

    [JsonExtensionData]
    public Dictionary<string, JsonElement>? AdditionalData { get; init; }
}
