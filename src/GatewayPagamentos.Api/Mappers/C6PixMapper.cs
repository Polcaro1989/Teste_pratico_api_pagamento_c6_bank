using System.Text.Json;
using GatewayPagamentos.Api.Contracts;
using C6 = GatewayPagamentos.IntegracoesC6.Models;

namespace GatewayPagamentos.Api.Mappers;

public static class C6PixMapper
{
    public static C6.PixCriarCobrancaRequest ToC6(PixCriarCobrancaRequestDto source)
    {
        return new C6.PixCriarCobrancaRequest(
            Calendario: new C6.PixCobrancaCalendario(source.Calendario.Expiracao),
            Devedor: source.Devedor is null
                ? null
                : new C6.PixCobrancaDevedor(source.Devedor.Cpf, source.Devedor.Cnpj, source.Devedor.Nome),
            Valor: new C6.PixCobrancaValor(source.Valor.Original, source.Valor.ModalidadeAlteracao),
            Chave: source.Chave,
            SolicitacaoPagador: source.SolicitacaoPagador,
            InfoAdicionais: source.InfoAdicionais?.Select(x => new C6.PixInfoAdicional(x.Nome, x.Valor)).ToList());
    }

    public static C6.PixCriarCobvRequest ToC6(PixCriarCobrancaVencimentoRequestDto source)
    {
        return new C6.PixCriarCobvRequest(
            Calendario: new C6.PixCobvCalendario(
                source.Calendario.DataDeVencimento,
                source.Calendario.ValidadeAposVencimento),
            Devedor: source.Devedor is null
                ? null
                : new C6.PixCobvDevedor(
                    source.Devedor.Logradouro,
                    source.Devedor.Cidade,
                    source.Devedor.Uf,
                    source.Devedor.Cep,
                    source.Devedor.Cpf,
                    source.Devedor.Cnpj,
                    source.Devedor.Nome),
            Valor: new C6.PixCobvValor(source.Valor.Original),
            Chave: source.Chave,
            SolicitacaoPagador: source.SolicitacaoPagador,
            InfoAdicionais: source.InfoAdicionais?.Select(x => new C6.PixInfoAdicional(x.Nome, x.Valor)).ToList());
    }

    public static C6.PixConfigurarWebhookRequest ToC6(PixConfigurarWebhookRequestDto source)
    {
        return new C6.PixConfigurarWebhookRequest(source.WebhookUrl);
    }

    public static PixCobrancaResponseDto ToApi(C6.PixCobrancaResponse source)
    {
        return new PixCobrancaResponseDto
        {
            Calendario = source.Calendario is null
                ? null
                : new PixCobrancaCalendarioResponseDto
                {
                    Criacao = source.Calendario.Criacao,
                    Expiracao = source.Calendario.Expiracao,
                    AdditionalData = CloneAdditionalData(source.Calendario.AdditionalData)
                },
            Txid = source.Txid,
            Revisao = source.Revisao,
            Loc = source.Loc is null
                ? null
                : new PixCobrancaLocResponseDto
                {
                    Id = source.Loc.Id,
                    Location = source.Loc.Location,
                    TipoCob = source.Loc.TipoCob,
                    AdditionalData = CloneAdditionalData(source.Loc.AdditionalData)
                },
            Location = source.Location,
            RedirectUrl = ToRedirectUrl(source.Location),
            Status = source.Status,
            Devedor = source.Devedor is null
                ? null
                : new PixCobrancaDevedorDto(source.Devedor.Cpf, source.Devedor.Cnpj, source.Devedor.Nome),
            Valor = source.Valor is null
                ? null
                : new PixCobrancaValorResponseDto
                {
                    Original = source.Valor.Original,
                    ModalidadeAlteracao = source.Valor.ModalidadeAlteracao,
                    AdditionalData = CloneAdditionalData(source.Valor.AdditionalData)
                },
            Chave = source.Chave,
            SolicitacaoPagador = source.SolicitacaoPagador,
            InfoAdicionais = source.InfoAdicionais?.Select(x => new PixInfoAdicionalDto(x.Nome, x.Valor)).ToList(),
            AdditionalData = CloneAdditionalData(source.AdditionalData)
        };
    }

    private static Dictionary<string, JsonElement>? CloneAdditionalData(Dictionary<string, JsonElement>? source)
    {
        if (source is null || source.Count == 0)
        {
            return source;
        }

        return source.ToDictionary(x => x.Key, x => x.Value.Clone());
    }

    private static string? ToRedirectUrl(string? location)
    {
        if (string.IsNullOrWhiteSpace(location))
        {
            return null;
        }

        return location.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
               location.StartsWith("https://", StringComparison.OrdinalIgnoreCase)
            ? location
            : $"https://{location}";
    }
}
