using System.Text.Json;
using System.Text.Json.Serialization;

namespace GatewayPagamentos.Api.Contracts;

public sealed record PixCobrancaCalendarioDto(
    [property: JsonPropertyName("expiracao")] int Expiracao);

public sealed record PixCobrancaDevedorDto(
    [property: JsonPropertyName("cpf")] string? Cpf,
    [property: JsonPropertyName("cnpj")] string? Cnpj,
    [property: JsonPropertyName("nome")] string? Nome);

public sealed record PixCobrancaValorDto(
    [property: JsonPropertyName("original")] string Original,
    [property: JsonPropertyName("modalidadeAlteracao")] int? ModalidadeAlteracao = null);

public sealed record PixCobvValorDto(
    [property: JsonPropertyName("original")] string Original);

public sealed record PixInfoAdicionalDto(
    [property: JsonPropertyName("nome")] string Nome,
    [property: JsonPropertyName("valor")] string Valor);

public sealed record PixCriarCobrancaRequestDto(
    [property: JsonPropertyName("calendario")] PixCobrancaCalendarioDto Calendario,
    [property: JsonPropertyName("devedor")] PixCobrancaDevedorDto? Devedor,
    [property: JsonPropertyName("valor")] PixCobrancaValorDto Valor,
    [property: JsonPropertyName("chave")] string Chave,
    [property: JsonPropertyName("solicitacaoPagador")] string? SolicitacaoPagador,
    [property: JsonPropertyName("infoAdicionais")] IReadOnlyList<PixInfoAdicionalDto>? InfoAdicionais);

public sealed record PixCobrancaVencimentoCalendarioDto(
    [property: JsonPropertyName("dataDeVencimento")] string DataDeVencimento,
    [property: JsonPropertyName("validadeAposVencimento")] int? ValidadeAposVencimento = null);

public sealed record PixCobrancaVencimentoDevedorDto(
    [property: JsonPropertyName("logradouro")] string? Logradouro,
    [property: JsonPropertyName("cidade")] string? Cidade,
    [property: JsonPropertyName("uf")] string? Uf,
    [property: JsonPropertyName("cep")] string? Cep,
    [property: JsonPropertyName("cpf")] string? Cpf,
    [property: JsonPropertyName("cnpj")] string? Cnpj,
    [property: JsonPropertyName("nome")] string? Nome);

public sealed record PixCriarCobrancaVencimentoRequestDto(
    [property: JsonPropertyName("calendario")] PixCobrancaVencimentoCalendarioDto Calendario,
    [property: JsonPropertyName("devedor")] PixCobrancaVencimentoDevedorDto? Devedor,
    [property: JsonPropertyName("valor")] PixCobvValorDto Valor,
    [property: JsonPropertyName("chave")] string Chave,
    [property: JsonPropertyName("solicitacaoPagador")] string? SolicitacaoPagador,
    [property: JsonPropertyName("infoAdicionais")] IReadOnlyList<PixInfoAdicionalDto>? InfoAdicionais);

public sealed record PixConfigurarWebhookRequestDto(
    [property: JsonPropertyName("webhookUrl")] string WebhookUrl);

public sealed class PixCobrancaCalendarioResponseDto
{
    [JsonPropertyName("criacao")]
    public string? Criacao { get; init; }

    [JsonPropertyName("expiracao")]
    public int? Expiracao { get; init; }

    [JsonExtensionData]
    public Dictionary<string, JsonElement>? AdditionalData { get; init; }
}

public sealed class PixCobrancaLocResponseDto
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

public sealed class PixCobrancaValorResponseDto
{
    [JsonPropertyName("original")]
    public string? Original { get; init; }

    [JsonPropertyName("modalidadeAlteracao")]
    public int? ModalidadeAlteracao { get; init; }

    [JsonExtensionData]
    public Dictionary<string, JsonElement>? AdditionalData { get; init; }
}

public sealed class PixCobrancaResponseDto
{
    [JsonPropertyName("calendario")]
    public PixCobrancaCalendarioResponseDto? Calendario { get; init; }

    [JsonPropertyName("txid")]
    public string? Txid { get; init; }

    [JsonPropertyName("revisao")]
    public int? Revisao { get; init; }

    [JsonPropertyName("loc")]
    public PixCobrancaLocResponseDto? Loc { get; init; }

    [JsonPropertyName("location")]
    public string? Location { get; init; }

    [JsonPropertyName("redirect_url")]
    public string? RedirectUrl { get; init; }

    [JsonPropertyName("status")]
    public string? Status { get; init; }

    [JsonPropertyName("devedor")]
    public PixCobrancaDevedorDto? Devedor { get; init; }

    [JsonPropertyName("valor")]
    public PixCobrancaValorResponseDto? Valor { get; init; }

    [JsonPropertyName("chave")]
    public string? Chave { get; init; }

    [JsonPropertyName("solicitacaoPagador")]
    public string? SolicitacaoPagador { get; init; }

    [JsonPropertyName("infoAdicionais")]
    public IReadOnlyList<PixInfoAdicionalDto>? InfoAdicionais { get; init; }

    [JsonExtensionData]
    public Dictionary<string, JsonElement>? AdditionalData { get; init; }
}
