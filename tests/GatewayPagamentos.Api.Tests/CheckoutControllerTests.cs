using System.Net;
using GatewayPagamentos.Api.Contracts;
using GatewayPagamentos.Api.Controllers;
using GatewayPagamentos.Api.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
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
        _controller = new CheckoutController(
            _service.Object,
            NullLogger<CheckoutController>.Instance)
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
        var body = Assert.IsType<CheckoutResponseDto>(created.Value);
        Assert.Equal("chk-123", body.Id);
        _service.Verify(s => s.CriarAsync(request, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Criar_QuandoServiceLancaArgumentException_Retorna400()
    {
        var request = CriarRequestValido();
        _service.Setup(s => s.CriarAsync(request, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ArgumentException("Descricao obrigatoria"));

        var result = await _controller.Criar(request, CancellationToken.None);

        var problem = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status400BadRequest, problem.StatusCode);
    }

    [Fact]
    public async Task Criar_QuandoServiceLancaUnauthorized_Retorna503()
    {
        var request = CriarRequestValido();
        _service.Setup(s => s.CriarAsync(request, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("auth", null, HttpStatusCode.Unauthorized));

        var result = await _controller.Criar(request, CancellationToken.None);

        var problem = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status503ServiceUnavailable, problem.StatusCode);
    }

    [Fact]
    public async Task Criar_QuandoTimeout_Retorna408()
    {
        var request = CriarRequestValido();
        _service.Setup(s => s.CriarAsync(request, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new OperationCanceledException("timeout"));

        var result = await _controller.Criar(request, CancellationToken.None);

        var problem = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status408RequestTimeout, problem.StatusCode);
    }

    [Fact]
    public async Task Criar_QuandoErroGenerico_Retorna500()
    {
        var request = CriarRequestValido();
        _service.Setup(s => s.CriarAsync(request, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("boom"));

        var result = await _controller.Criar(request, CancellationToken.None);

        var problem = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status500InternalServerError, problem.StatusCode);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Consultar_IdInvalido_Retorna400(string id)
    {
        var result = await _controller.Consultar(id, CancellationToken.None);

        var problem = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status400BadRequest, problem.StatusCode);
        _service.Verify(s => s.ConsultarAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Consultar_NotFound_Retorna404()
    {
        _service.Setup(s => s.ConsultarAsync("naoexiste", It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("nf", null, HttpStatusCode.NotFound));

        var result = await _controller.Consultar("naoexiste", CancellationToken.None);

        var problem = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status404NotFound, problem.StatusCode);
    }

    [Fact]
    public async Task Cancelar_Sucesso_Retorna204()
    {
        var result = await _controller.Cancelar("123", CancellationToken.None);

        Assert.IsType<NoContentResult>(result);
        _service.Verify(s => s.CancelarAsync("123", It.IsAny<CancellationToken>()), Times.Once);
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
}
