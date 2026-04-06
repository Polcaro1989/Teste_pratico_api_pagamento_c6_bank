using GatewayPagamentos.Api.Contracts;
using GatewayPagamentos.Api.Validators;
using Xunit;

namespace GatewayPagamentos.Api.Tests;

public class CheckoutValidatorsTests
{
    private readonly CheckoutCriarValidator _validator = new();

    [Fact]
    public void Validate_ComTaxId11Digitos_DevePassar()
    {
        var request = CriarRequest("12345678910", "01311-000", "sp");

        var result = _validator.Validate(request);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_ComTaxId14Digitos_DevePassar()
    {
        var request = CriarRequest("04252011000110", "01311000", "RJ");

        var result = _validator.Validate(request);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_ComTaxIdCepUfInvalidos_DeveFalharNosCamposCorretos()
    {
        var request = CriarRequest("abc123", "12a34-567", "XX");

        var result = _validator.Validate(request);
        var fields = result.Errors.Select(e => e.PropertyName).ToHashSet();

        Assert.False(result.IsValid);
        Assert.Contains("Payer.TaxId", fields);
        Assert.Contains("Payer.Address.ZipCode", fields);
        Assert.Contains("Payer.Address.State", fields);
    }

    [Fact]
    public void Validate_ComCardEPixJuntos_DevePassar()
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

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_ComCardSemCardInfo_DevePassar()
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

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_ComExternalReferenceIdMaiorQue10_DeveFalhar()
    {
        var request = CriarRequest("52998224725", "01311000", "SP") with
        {
            ExternalReferenceId = "ABCDEFGHIJK"
        };

        var result = _validator.Validate(request);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "ExternalReferenceId");
    }

    [Fact]
    public void Validate_ComExternalReferenceIdComCaracterEspecial_DeveFalhar()
    {
        var request = CriarRequest("52998224725", "01311000", "SP") with
        {
            ExternalReferenceId = "REF-123"
        };

        var result = _validator.Validate(request);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "ExternalReferenceId");
    }

    private static CreateCheckoutRequestDto CriarRequest(string taxId, string zipCode, string state)
    {
        return new CreateCheckoutRequestDto(
            Amount: 150,
            Description: "descricao",
            ExternalReferenceId: "REF123",
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
