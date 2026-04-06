using GatewayPagamentos.Api.Contracts;
using GatewayPagamentos.Api.Services;
using GatewayPagamentos.IntegracoesC6;
using GatewayPagamentos.IntegracoesC6.Models;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace GatewayPagamentos.Api.Tests;

public class CheckoutAppServiceTests
{
    private readonly Mock<IC6TokenClient> _tokenClient = new();
    private readonly Mock<IC6CheckoutClient> _checkoutClient = new();
    private readonly CheckoutAppService _service;

    public CheckoutAppServiceTests()
    {
        _service = new CheckoutAppService(
            _tokenClient.Object,
            _checkoutClient.Object,
            new Mock<ILogger<CheckoutAppService>>().Object);
    }

    [Fact]
    public async Task CriarAsync_ComRequestValido_MapeiaEChamaClienteC6()
    {
        var request = CriarRequestValido();
        _tokenClient.Setup(x => x.ObterTokenAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new TokenResponse("token-1", "Bearer", 3600));

        _checkoutClient.Setup(x => x.CriarAsync(
                "token-1",
                It.Is<CheckoutCriarRequest>(r =>
                    r.Valor == request.Amount &&
                    r.ReferenciaExterna == request.ExternalReferenceId &&
                    r.Pagador.Documento == request.Payer.TaxId &&
                    r.Pagador.Endereco.Estado == request.Payer.Address.State &&
                    r.Pagador.Endereco.Cep == request.Payer.Address.ZipCode),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CheckoutResponse("chk-1", "https://checkout", "pending"));

        var result = await _service.CriarAsync(request, CancellationToken.None);

        Assert.Equal("chk-1", result.Id);
        Assert.Equal("https://checkout", result.Url);
        Assert.Equal("pending", result.Status);
        _tokenClient.Verify(x => x.ObterTokenAsync(It.IsAny<CancellationToken>()), Times.Once);
        _checkoutClient.VerifyAll();
    }

    [Fact]
    public async Task AutorizarAsync_ComRequestValido_MapeiaEChamaClienteC6()
    {
        var request = CriarAuthorizeRequestValido();
        _tokenClient.Setup(x => x.ObterTokenAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new TokenResponse("token-1", "Bearer", 3600));

        _checkoutClient.Setup(x => x.AutorizarAsync(
                "token-1",
                It.Is<CheckoutAutorizarRequest>(r =>
                    r.Valor == request.Amount &&
                    r.ReferenciaExterna == request.ExternalReferenceId &&
                    r.Pagador.Documento == request.Payer.TaxId),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CheckoutResponse("chk-2", "https://checkout", "authorized"));

        var result = await _service.AutorizarAsync(request, CancellationToken.None);

        Assert.Equal("chk-2", result.Id);
        Assert.Equal("authorized", result.Status);
        _tokenClient.Verify(x => x.ObterTokenAsync(It.IsAny<CancellationToken>()), Times.Once);
        _checkoutClient.VerifyAll();
    }

    [Fact]
    public async Task ConsultarAsync_ComIdVazio_LancaArgumentException()
    {
        var ex = await Assert.ThrowsAsync<ArgumentException>(() => _service.ConsultarAsync("", CancellationToken.None));

        Assert.Equal("id", ex.ParamName);
    }

    [Fact]
    public async Task CancelarAsync_ComIdVazio_LancaArgumentException()
    {
        var ex = await Assert.ThrowsAsync<ArgumentException>(() => _service.CancelarAsync(" ", CancellationToken.None));

        Assert.Equal("id", ex.ParamName);
    }

    [Fact]
    public async Task ConsultarAsync_ComIdValido_UsaTokenEConsultaNoClienteC6()
    {
        _tokenClient.Setup(x => x.ObterTokenAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new TokenResponse("token-1", "Bearer", 3600));

        _checkoutClient.Setup(x => x.ConsultarAsync("token-1", "chk-123", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CheckoutResponse("chk-123", "https://checkout", "pending"));

        var result = await _service.ConsultarAsync("chk-123", CancellationToken.None);

        Assert.Equal("chk-123", result.Id);
        _tokenClient.Verify(x => x.ObterTokenAsync(It.IsAny<CancellationToken>()), Times.Once);
        _checkoutClient.Verify(x => x.ConsultarAsync("token-1", "chk-123", It.IsAny<CancellationToken>()), Times.Once);
    }

    private static CreateCheckoutRequestDto CriarRequestValido()
    {
        return new CreateCheckoutRequestDto(
            Amount: 100,
            Description: "descricao",
            ExternalReferenceId: "ref-1",
            Payer: CriarPagadorValido(),
            Payment: new PaymentDto(Card: null, Pix: new PixPaymentDto("pix-key")),
            RedirectUrl: "https://callback");
    }

    private static AuthorizeCheckoutRequestDto CriarAuthorizeRequestValido()
    {
        return new AuthorizeCheckoutRequestDto(
            Amount: 200,
            Description: "descricao",
            ExternalReferenceId: "ref-2",
            Payer: CriarPagadorValido(),
            Payment: new PaymentDto(Card: null, Pix: new PixPaymentDto("pix-key")),
            RedirectUrl: "https://callback");
    }

    private static PayerDto CriarPagadorValido()
    {
        return new PayerDto(
            Name: "Cliente",
            TaxId: "52998224725",
            Email: "cliente@teste.com",
            PhoneNumber: "+5511999999999",
            Address: new AddressDto(
                Street: "Av Paulista",
                Number: 1000,
                Complement: null,
                City: "Sao Paulo",
                State: "SP",
                ZipCode: "01311-000"));
    }
}
