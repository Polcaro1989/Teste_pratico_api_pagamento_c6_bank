using GatewayPagamentos.Api.Contracts;
using GatewayPagamentos.Api.Mappers;
using GatewayPagamentos.IntegracoesC6.Models;
using Xunit;

namespace GatewayPagamentos.Api.Tests;

public class C6CheckoutMapperTests
{
    [Fact]
    public void ToC6_CreateCheckoutRequest_DeveMapearTodosCamposCriticos()
    {
        var request = new CreateCheckoutRequestDto(
            Amount: 345.67m,
            Description: "Pedido 1",
            ExternalReferenceId: "ref-123",
            Payer: new PayerDto(
                Name: "Cliente",
                TaxId: "52998224725",
                Email: "cliente@teste.com",
                PhoneNumber: "+5511999999999",
                Address: new AddressDto(
                    Street: "Rua A",
                    Number: 100,
                    Complement: "Ap 1",
                    City: "Sao Paulo",
                    State: "SP",
                    ZipCode: "01311000")),
            Payment: new PaymentDto(
                Card: new CardDto(
                    Authenticate: "3DS",
                    Capture: true,
                    FixedInstallments: false,
                    Installments: 1,
                    InterestType: "NONE",
                    Recurrent: false,
                    SaveCard: false,
                    Type: "CREDIT",
                    SoftDescriptor: "LOJA",
                    CardInfo: new CardInfoDto("card-token")),
                Pix: null),
            RedirectUrl: "https://callback");

        var mapped = C6CheckoutMapper.ToC6(request);

        Assert.Equal(request.Amount, mapped.Valor);
        Assert.Equal(request.ExternalReferenceId, mapped.ReferenciaExterna);
        Assert.Equal(request.Payer.TaxId, mapped.Pagador.Documento);
        Assert.Equal(request.Payer.Address.State, mapped.Pagador.Endereco.Estado);
        Assert.Equal(request.Payment.Card!.CardInfo!.Token, mapped.Pagamento.Cartao!.DadosCartao!.Token);
    }

    [Fact]
    public void ToApi_RespostaC6_DeveMapearIdUrlEStatus()
    {
        var c6Response = new CheckoutResponse("chk-123", "https://checkout", "approved");

        var mapped = C6CheckoutMapper.ToApi(c6Response);

        Assert.Equal("chk-123", mapped.Id);
        Assert.Equal("https://checkout", mapped.Url);
        Assert.Equal("approved", mapped.Status);
    }
}
