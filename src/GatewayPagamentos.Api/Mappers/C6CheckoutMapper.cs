using GatewayPagamentos.Api.Contracts;
using C6 = GatewayPagamentos.IntegracoesC6.Models;

namespace GatewayPagamentos.Api.Mappers;

public static class C6CheckoutMapper
{
    public static C6.CheckoutCriarRequest ToC6(CreateCheckoutRequestDto source)
    {
        return new C6.CheckoutCriarRequest(
            Valor: source.Amount,
            Descricao: source.Description,
            ReferenciaExterna: source.ExternalReferenceId,
            Pagador: ToC6(source.Payer),
            Pagamento: ToC6(source.Payment),
            UrlRedirect: source.RedirectUrl);
    }

    public static C6.CheckoutAutorizarRequest ToC6(AuthorizeCheckoutRequestDto source)
    {
        return new C6.CheckoutAutorizarRequest(
            Valor: source.Amount,
            Descricao: source.Description,
            ReferenciaExterna: source.ExternalReferenceId,
            Pagador: ToC6(source.Payer),
            Pagamento: ToC6(source.Payment),
            UrlRedirect: source.RedirectUrl);
    }

    public static CheckoutResponseDto ToApi(C6.CheckoutResponse source)
    {
        return new CheckoutResponseDto(
            Id: source.Id,
            Url: source.Url,
            Status: source.Status);
    }

    private static C6.Pagador ToC6(PayerDto source)
    {
        return new C6.Pagador(
            Nome: source.Name,
            Documento: source.TaxId,
            Email: source.Email,
            Telefone: source.PhoneNumber,
            Endereco: ToC6(source.Address));
    }

    private static C6.Endereco ToC6(AddressDto source)
    {
        return new C6.Endereco(
            Logradouro: source.Street,
            Numero: source.Number,
            Complemento: source.Complement,
            Cidade: source.City,
            Estado: source.State,
            Cep: source.ZipCode);
    }

    private static C6.Pagamento ToC6(PaymentDto source)
    {
        return new C6.Pagamento(
            Cartao: source.Card is null ? null : ToC6(source.Card),
            Pix: source.Pix is null ? null : new C6.PixPagamento(source.Pix.Key));
    }

    private static C6.Cartao ToC6(CardDto source)
    {
        return new C6.Cartao(
            Autenticacao: source.Authenticate,
            Capturar: source.Capture,
            ParcelasFixas: source.FixedInstallments,
            Parcelas: source.Installments,
            TipoJuros: source.InterestType,
            Recorrente: source.Recurrent,
            SalvarCartao: source.SaveCard,
            Tipo: source.Type,
            DescricaoSuave: source.SoftDescriptor,
            DadosCartao: source.CardInfo is null ? null : new C6.InfoCartao(source.CardInfo.Token));
    }
}
