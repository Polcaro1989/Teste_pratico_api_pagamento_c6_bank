using System.Net;
using GatewayPagamentos.Api.Contracts;
using GatewayPagamentos.Api.Controllers;
using GatewayPagamentos.Api.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace GatewayPagamentos.Api.Tests;

public class CheckoutControllerTests
{
    private readonly Mock<ICheckoutAppService> _service;
    private readonly CheckoutController _controller;

    public CheckoutControllerTests()
    {
        _service = new Mock<ICheckoutAppService>();
        _controller = new CheckoutController(_service.Object)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            }
        };
    }

    [Fact]
    public async Task Criar_ComRequestValido_RetornaCreated()
    {
        var request = CriarRequestValido();
        var response = new CheckoutResponseDto("chk-123", "url", "status");
        _service.Setup(s => s.CriarAsync(request, It.IsAny<CancellationToken>())).ReturnsAsync(response);

        var result = await _controller.Criar(request, CancellationToken.None);

        var created = Assert.IsType<CreatedAtActionResult>(result);
        Assert.Equal(StatusCodes.Status201Created, created.StatusCode);
        Assert.Equal("chk-123", ((CheckoutResponseDto)created.Value!).Id);
    }

    [Fact]
    public async Task Autorizar_ComRequestValido_RetornaOk()
    {
        var request = CriarAuthorizeRequestValido();
        var response = new CheckoutResponseDto("chk-123", "url", "authorized");
        _service.Setup(s => s.AutorizarAsync(request, It.IsAny<CancellationToken>())).ReturnsAsync(response);

        var result = await _controller.Autorizar(request, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal("authorized", ((CheckoutResponseDto)ok.Value!).Status);
    }

    [Fact]
    public async Task Consultar_Sucesso_RetornaOk()
    {
        _service.Setup(s => s.ConsultarAsync("id-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CheckoutResponseDto("id-1", "url", "pending"));

        var result = await _controller.Consultar("id-1", CancellationToken.None);

        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task Cancelar_Sucesso_RetornaNoContent()
    {
        var result = await _controller.Cancelar("id-1", CancellationToken.None);

        Assert.IsType<NoContentResult>(result);
        _service.Verify(s => s.CancelarAsync("id-1", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Criar_QuandoServiceLancaExcecao_PropagaExcecao()
    {
        var request = CriarRequestValido();
        _service.Setup(s => s.CriarAsync(request, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("auth", null, HttpStatusCode.Unauthorized));

        await Assert.ThrowsAsync<HttpRequestException>(() => _controller.Criar(request, CancellationToken.None));
    }

    private static CreateCheckoutRequestDto CriarRequestValido()
    {
        return new CreateCheckoutRequestDto(
            Amount: 100,
            Description: "desc",
            ExternalReferenceId: "ref",
            Payer: new PayerDto(
                Name: "n",
                TaxId: "d",
                Email: "dev@teste.com",
                PhoneNumber: "+5511999999999",
                Address: new AddressDto("r", 1, null, "c", "s", "z")),
            Payment: new PaymentDto(
                Card: null,
                Pix: new PixPaymentDto("k")),
            RedirectUrl: "https://callback");
    }

    private static AuthorizeCheckoutRequestDto CriarAuthorizeRequestValido()
    {
        return new AuthorizeCheckoutRequestDto(
            Amount: 100,
            Description: "desc",
            ExternalReferenceId: "ref",
            Payer: new PayerDto(
                Name: "n",
                TaxId: "d",
                Email: "dev@teste.com",
                PhoneNumber: "+5511999999999",
                Address: new AddressDto("r", 1, null, "c", "s", "z")),
            Payment: new PaymentDto(
                Card: null,
                Pix: new PixPaymentDto("k")),
            RedirectUrl: "https://callback");
    }
}
