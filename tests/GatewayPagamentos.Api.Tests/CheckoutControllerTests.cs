using System.Net;
using GatewayPagamentos.Api.Controllers;
using GatewayPagamentos.IntegracoesC6;
using GatewayPagamentos.IntegracoesC6.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace GatewayPagamentos.Api.Tests;

public class CheckoutControllerTests
{
    private static CheckoutController BuildController(
        Mock<IC6TokenClient> tokenMock,
        Mock<IC6CheckoutClient> checkoutMock)
    {
        var controller = new CheckoutController(
            tokenMock.Object,
            checkoutMock.Object,
            NullLogger<CheckoutController>.Instance)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            }
        };

        return controller;
    }

    [Fact]
    public async Task Criar_RetornaCreated()
    {
        var tokenMock = new Mock<IC6TokenClient>();
        tokenMock.Setup(t => t.ObterTokenAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new TokenResponse("access", "bearer", 3600));

        var checkoutMock = new Mock<IC6CheckoutClient>();
        checkoutMock.Setup(c => c.CriarAsync("access", It.IsAny<CheckoutCriarRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CheckoutResponse("abc", "url", "status"));

        var controller = BuildController(tokenMock, checkoutMock);

        var result = await controller.Criar(new CheckoutCriarRequest(1, "desc", "ref",
            new Pagador("n", "d", "e", "p", new Endereco("r", 1, null, "c", "s", "z")),
            new Pagamento(null, new PixPagamento("k")),
            "u"), CancellationToken.None);

        var created = Assert.IsType<CreatedAtActionResult>(result);
        Assert.Equal(201, created.StatusCode);
        Assert.Equal("Consultar", created.ActionName);
        var response = Assert.IsType<CheckoutResponse>(created.Value);
        Assert.Equal("abc", response.Id);
    }

    [Fact]
    public async Task Consultar_IdVazio_Retorna400()
    {
        var controller = BuildController(new Mock<IC6TokenClient>(), new Mock<IC6CheckoutClient>());

        var result = await controller.Consultar(string.Empty, CancellationToken.None);

        var problem = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status400BadRequest, problem.StatusCode);
    }

    [Fact]
    public async Task Consultar_NotFound_Retorna404()
    {
        var tokenMock = new Mock<IC6TokenClient>();
        tokenMock.Setup(t => t.ObterTokenAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new TokenResponse("access", "bearer", 3600));

        var checkoutMock = new Mock<IC6CheckoutClient>();
        checkoutMock.Setup(c => c.ConsultarAsync("access", "naoexiste", It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("nf", null, HttpStatusCode.NotFound));

        var controller = BuildController(tokenMock, checkoutMock);

        var result = await controller.Consultar("naoexiste", CancellationToken.None);

        var problem = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status404NotFound, problem.StatusCode);
    }

    [Fact]
    public async Task Cancelar_Ok_Retorna204()
    {
        var tokenMock = new Mock<IC6TokenClient>();
        tokenMock.Setup(t => t.ObterTokenAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new TokenResponse("access", "bearer", 3600));

        var checkoutMock = new Mock<IC6CheckoutClient>();
        checkoutMock.Setup(c => c.CancelarAsync("access", "123", It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var controller = BuildController(tokenMock, checkoutMock);

        var result = await controller.Cancelar("123", CancellationToken.None);

        Assert.IsType<NoContentResult>(result);
    }
}
