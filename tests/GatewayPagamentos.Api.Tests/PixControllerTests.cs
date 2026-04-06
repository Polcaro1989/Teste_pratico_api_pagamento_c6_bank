using GatewayPagamentos.Api.Contracts;
using GatewayPagamentos.Api.Controllers;
using GatewayPagamentos.Api.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using System.Text.Json;
using Xunit;

namespace GatewayPagamentos.Api.Tests;

public class PixControllerTests
{
    private readonly Mock<IPixAppService> _service;
    private readonly PixController _controller;

    public PixControllerTests()
    {
        _service = new Mock<IPixAppService>();
        _controller = new PixController(_service.Object)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            }
        };
    }

    [Fact]
    public async Task CriarCobrancaSemTxid_ComRequestValido_RetornaCreated()
    {
        var request = CriarRequestValido();
        var response = CriarResponseValida();

        _service.Setup(s => s.CriarCobrancaImediataAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        var result = await _controller.CriarCobrancaSemTxid(request, CancellationToken.None);

        var created = Assert.IsType<CreatedResult>(result);
        Assert.Equal(StatusCodes.Status201Created, created.StatusCode);
        Assert.NotNull(created.Value);
    }

    [Fact]
    public async Task CriarCobrancaComTxid_ComRequestValido_RetornaCreated()
    {
        var request = CriarRequestValido();
        var response = CriarResponseValida();
        const string txid = "AbCdEf1234567890AbCdEf1234";

        _service.Setup(s => s.CriarCobrancaImediataComTxidAsync(txid, request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        var result = await _controller.CriarCobrancaComTxid(txid, request, CancellationToken.None);

        var created = Assert.IsType<CreatedResult>(result);
        Assert.Equal(StatusCodes.Status201Created, created.StatusCode);
    }

    [Fact]
    public async Task ConsultarCobranca_ComTxidValido_RetornaOk()
    {
        const string txid = "AbCdEf1234567890AbCdEf1234";
        var response = CriarResponseValida();

        _service.Setup(s => s.ConsultarCobrancaImediataAsync(txid, It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        var result = await _controller.ConsultarCobranca(txid, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(StatusCodes.Status200OK, ok.StatusCode);
    }

    [Fact]
    public async Task ListarCobrancasImediatas_ComParametrosValidos_RetornaOk()
    {
        var inicio = DateTimeOffset.Parse("2026-04-06T00:00:00-03:00");
        var fim = DateTimeOffset.Parse("2026-04-06T23:59:59-03:00");
        var payload = JsonDocument.Parse("{\"cobs\":[]}").RootElement.Clone();

        _service.Setup(s => s.ListarCobrancasImediatasAsync(
                inicio, fim, null, null, null, null, null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(payload);

        var result = await _controller.ListarCobrancasImediatas(
            inicio, fim, null, null, null, null, null, null, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(StatusCodes.Status200OK, ok.StatusCode);
    }

    [Fact]
    public async Task CriarCobrancaComVencimento_ComRequestValido_RetornaCreated()
    {
        const string txid = "AbCdEf1234567890AbCdEf1234";
        var request = new PixCriarCobrancaVencimentoRequestDto(
            Calendario: new PixCobrancaVencimentoCalendarioDto("2026-12-31", 30),
            Devedor: new PixCobrancaVencimentoDevedorDto(
                Logradouro: "Rua A 123",
                Cidade: "Sao Paulo",
                Uf: "SP",
                Cep: "01001000",
                Cpf: "12345678901",
                Cnpj: null,
                Nome: "Jose da Silva"),
            Valor: new PixCobvValorDto("1.00"),
            Chave: "SUA_CHAVE_PIX",
            SolicitacaoPagador: "Teste cobv",
            InfoAdicionais: null);
        var response = JsonDocument.Parse("{\"txid\":\"AbCdEf1234567890AbCdEf1234\"}").RootElement.Clone();

        _service.Setup(s => s.CriarCobrancaComVencimentoAsync(txid, request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        var result = await _controller.CriarCobrancaComVencimento(txid, request, CancellationToken.None);

        var created = Assert.IsType<CreatedResult>(result);
        Assert.Equal(StatusCodes.Status201Created, created.StatusCode);
    }

    [Fact]
    public async Task ConfigurarWebhook_ComRequestValido_RetornaOk()
    {
        const string chave = "SUA_CHAVE_PIX";
        var request = new PixConfigurarWebhookRequestDto("https://meusistema.com.br/webhooks/pix");
        var response = JsonDocument.Parse("{\"webhookUrl\":\"https://meusistema.com.br/webhooks/pix\"}").RootElement.Clone();

        _service.Setup(s => s.ConfigurarWebhookAsync(chave, request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        var result = await _controller.ConfigurarWebhook(chave, request, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(StatusCodes.Status200OK, ok.StatusCode);
    }

    private static PixCriarCobrancaRequestDto CriarRequestValido()
    {
        return new PixCriarCobrancaRequestDto(
            Calendario: new PixCobrancaCalendarioDto(3600),
            Devedor: new PixCobrancaDevedorDto(Cpf: "12345678901", Cnpj: null, Nome: "Jose da Silva"),
            Valor: new PixCobrancaValorDto("1.00", 1),
            Chave: "SUA_CHAVE_PIX",
            SolicitacaoPagador: "Teste",
            InfoAdicionais:
            [
                new PixInfoAdicionalDto("pedido", "123")
            ]);
    }

    private static PixCobrancaResponseDto CriarResponseValida()
    {
        return new PixCobrancaResponseDto
        {
            Txid = "AbCdEf1234567890AbCdEf1234",
            Status = "ATIVA",
            Chave = "SUA_CHAVE_PIX",
            Location = "pix.example.com/qr/abc"
        };
    }
}

