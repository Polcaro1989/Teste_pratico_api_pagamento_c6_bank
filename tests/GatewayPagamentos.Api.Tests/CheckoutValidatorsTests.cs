using GatewayPagamentos.Api.Contracts;
using GatewayPagamentos.Api.Validators;
using Xunit;

namespace GatewayPagamentos.Api.Tests;

public class CheckoutValidatorsTests
{
    private readonly CheckoutCriarValidator _validator = new();

    [Fact]
    public void Validate_ComCpfCepEUfValidos_DevePassar()
    {
        var request = CriarRequest("529.982.247-25", "01311-000", "sp");

        var result = _validator.Validate(request);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_ComCnpjValido_DevePassar()
    {
        var request = CriarRequest("04.252.011/0001-10", "01311000", "RJ");

        var result = _validator.Validate(request);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_ComTaxIdCepUfInvalidos_DeveFalharNosCamposCorretos()
    {
        var request = CriarRequest("12345678900", "12a34-567", "XX");

        var result = _validator.Validate(request);
        var fields = result.Errors.Select(e => e.PropertyName).ToHashSet();

        Assert.False(result.IsValid);
        Assert.Contains("Payer.TaxId", fields);
        Assert.Contains("Payer.Address.ZipCode", fields);
        Assert.Contains("Payer.Address.State", fields);
    }

    [Fact]
    public void Validate_ComCardEPixJuntos_DeveFalhar()
    {
        var request = CriarRequest("52998224725", "01311000", "SP") with
        {
            Payment = new PaymentDto(
                Card: new CardDto(
                    Authenticate: "3DS",
                    Capture: true,
                    FixedInstallments: false,
                    Installments: 1,
                    InterestType: "NONE",
                    Recurrent: false,
                    SaveCard: false,
                    Type: "CREDIT",
                    SoftDescriptor: null,
                    CardInfo: new CardInfoDto("tok")),
                Pix: new PixPaymentDto("pix-key"))
        };

        var result = _validator.Validate(request);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "Payment");
    }

    [Fact]
    public void Validate_ComCardSemCardInfo_DeveFalhar()
    {
        var request = CriarRequest("52998224725", "01311000", "SP") with
        {
            Payment = new PaymentDto(
                Card: new CardDto(
                    Authenticate: "3DS",
                    Capture: true,
                    FixedInstallments: false,
                    Installments: 1,
                    InterestType: "NONE",
                    Recurrent: false,
                    SaveCard: false,
                    Type: "CREDIT",
                    SoftDescriptor: null,
                    CardInfo: null),
                Pix: null)
        };

        var result = _validator.Validate(request);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "Payment.Card.CardInfo");
    }

    private static CreateCheckoutRequestDto CriarRequest(string taxId, string zipCode, string state)
    {
        return new CreateCheckoutRequestDto(
            Amount: 150,
            Description: "descricao",
            ExternalReferenceId: "ref-1",
            Payer: new PayerDto(
                Name: "Cliente",
                TaxId: taxId,
                Email: "cliente@teste.com",
                PhoneNumber: "+5511999999999",
                Address: new AddressDto(
                    Street: "Rua A",
                    Number: 10,
                    Complement: null,
                    City: "Sao Paulo",
                    State: state,
                    ZipCode: zipCode)),
            Payment: new PaymentDto(
                Card: null,
                Pix: new PixPaymentDto("pix-key")),
            RedirectUrl: "https://callback");
    }
}
