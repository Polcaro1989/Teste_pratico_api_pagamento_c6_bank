using GatewayPagamentos.Api.Contracts;
using GatewayPagamentos.Api.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace GatewayPagamentos.Api.Controllers;

[ApiController]
[Route("v2/pix")]
public sealed class PixController : ControllerBase
{
    private readonly IPixAppService _service;

    public PixController(IPixAppService service)
    {
        _service = service;
    }

    [HttpPost("cob")]
    [ProducesResponseType(typeof(PixCobrancaResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status408RequestTimeout)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status503ServiceUnavailable)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> CriarCobrancaSemTxid(
        [FromBody] PixCriarCobrancaRequestDto request,
        CancellationToken ct)
    {
        var result = await _service.CriarCobrancaImediataAsync(request, ct);
        return Created($"/v2/pix/cob/{result.Txid}", result);
    }

    [HttpPut("cob/{txid}")]
    [ProducesResponseType(typeof(PixCobrancaResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status408RequestTimeout)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status503ServiceUnavailable)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> CriarCobrancaComTxid(
        [FromRoute] string txid,
        [FromBody] PixCriarCobrancaRequestDto request,
        CancellationToken ct)
    {
        var result = await _service.CriarCobrancaImediataComTxidAsync(txid, request, ct);
        return Created($"/v2/pix/cob/{result.Txid}", result);
    }

    [HttpGet("cob/{txid}")]
    [ProducesResponseType(typeof(PixCobrancaResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status408RequestTimeout)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status503ServiceUnavailable)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> ConsultarCobranca([FromRoute] string txid, CancellationToken ct)
    {
        var result = await _service.ConsultarCobrancaImediataAsync(txid, ct);
        return Ok(result);
    }

    [HttpGet("cob")]
    [ProducesResponseType(typeof(JsonElement), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status408RequestTimeout)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status503ServiceUnavailable)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> ListarCobrancasImediatas(
        [FromQuery] DateTimeOffset inicio,
        [FromQuery] DateTimeOffset fim,
        [FromQuery] string? cpf,
        [FromQuery] string? cnpj,
        [FromQuery] string? status,
        [FromQuery] bool? locationPresente,
        [FromQuery(Name = "paginacao.paginaAtual")] int? paginaAtual,
        [FromQuery(Name = "paginacao.itensPorPagina")] int? itensPorPagina,
        CancellationToken ct)
    {
        var result = await _service.ListarCobrancasImediatasAsync(
            inicio, fim, cpf, cnpj, status, locationPresente, paginaAtual, itensPorPagina, ct);
        return Ok(result);
    }

    [HttpPut("cobv/{txid}")]
    [ProducesResponseType(typeof(JsonElement), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status408RequestTimeout)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status503ServiceUnavailable)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> CriarCobrancaComVencimento(
        [FromRoute] string txid,
        [FromBody] PixCriarCobrancaVencimentoRequestDto request,
        CancellationToken ct)
    {
        var result = await _service.CriarCobrancaComVencimentoAsync(txid, request, ct);
        return Created($"/v2/pix/cobv/{txid}", result);
    }

    [HttpGet("cobv/{txid}")]
    [ProducesResponseType(typeof(JsonElement), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status408RequestTimeout)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status503ServiceUnavailable)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> ConsultarCobrancaComVencimento([FromRoute] string txid, CancellationToken ct)
    {
        var result = await _service.ConsultarCobrancaComVencimentoAsync(txid, ct);
        return Ok(result);
    }

    [HttpGet("cobv")]
    [ProducesResponseType(typeof(JsonElement), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status408RequestTimeout)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status503ServiceUnavailable)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> ListarCobrancasComVencimento(
        [FromQuery] DateTimeOffset inicio,
        [FromQuery] DateTimeOffset fim,
        [FromQuery] string? cpf,
        [FromQuery] string? cnpj,
        [FromQuery] string? status,
        [FromQuery] bool? locationPresente,
        [FromQuery(Name = "paginacao.paginaAtual")] int? paginaAtual,
        [FromQuery(Name = "paginacao.itensPorPagina")] int? itensPorPagina,
        CancellationToken ct)
    {
        var result = await _service.ListarCobrancasComVencimentoAsync(
            inicio, fim, cpf, cnpj, status, locationPresente, paginaAtual, itensPorPagina, ct);
        return Ok(result);
    }

    [HttpPut("webhook/{chave}")]
    [ProducesResponseType(typeof(JsonElement), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status408RequestTimeout)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status503ServiceUnavailable)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> ConfigurarWebhook(
        [FromRoute] string chave,
        [FromBody] PixConfigurarWebhookRequestDto request,
        CancellationToken ct)
    {
        var result = await _service.ConfigurarWebhookAsync(chave, request, ct);
        return Ok(result);
    }
}
