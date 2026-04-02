using System.Net;
using GatewayPagamentos.IntegracoesC6;
using GatewayPagamentos.IntegracoesC6.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace GatewayPagamentos.Api.Controllers;

[ApiController]
[Route("api/v1/checkout")]
public sealed class CheckoutController : ControllerBase
{
    private readonly IC6TokenClient _tokenClient;
    private readonly IC6CheckoutClient _checkoutClient;
    private readonly ILogger<CheckoutController> _logger;

    public CheckoutController(
        IC6TokenClient tokenClient,
        IC6CheckoutClient checkoutClient,
        ILogger<CheckoutController> logger)
    {
        _tokenClient = tokenClient;
        _checkoutClient = checkoutClient;
        _logger = logger;
    }

    [HttpPost]
    [ProducesResponseType(typeof(CheckoutResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Criar(
        [FromBody] CheckoutCriarRequest request,
        CancellationToken ct)
    {
        try
        {
            var token = await _tokenClient.ObterTokenAsync(ct);
            var result = await _checkoutClient.CriarAsync(token.AccessToken, request, ct);
            return CreatedAtAction(nameof(Consultar), new { id = result.Id }, result);
        }
        catch (HttpRequestException ex) when (ex.StatusCode is HttpStatusCode.BadRequest)
        {
            _logger.LogWarning(ex, "Erro de validação ao criar checkout");
            return Problem(title: "Requisição inválida", statusCode: StatusCodes.Status400BadRequest);
        }
        catch (HttpRequestException ex) when (ex.StatusCode.HasValue)
        {
            _logger.LogError(ex, "Erro HTTP ao criar checkout");
            return StatusCode((int)ex.StatusCode.Value, new ProblemDetails
            {
                Title = "Erro na chamada ao provedor C6",
                Status = (int)ex.StatusCode.Value
            });
        }
    }

    [HttpPost("autorizar")]
    [ProducesResponseType(typeof(CheckoutResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Autorizar(
        [FromBody] CheckoutAutorizarRequest request,
        CancellationToken ct)
    {
        try
        {
            var token = await _tokenClient.ObterTokenAsync(ct);
            var result = await _checkoutClient.AutorizarAsync(token.AccessToken, request, ct);
            return Ok(result);
        }
        catch (HttpRequestException ex) when (ex.StatusCode is HttpStatusCode.BadRequest)
        {
            _logger.LogWarning(ex, "Erro de validação ao autorizar checkout");
            return Problem(title: "Requisição inválida", statusCode: StatusCodes.Status400BadRequest);
        }
        catch (HttpRequestException ex) when (ex.StatusCode.HasValue)
        {
            _logger.LogError(ex, "Erro HTTP ao autorizar checkout");
            return StatusCode((int)ex.StatusCode.Value, new ProblemDetails
            {
                Title = "Erro na chamada ao provedor C6",
                Status = (int)ex.StatusCode.Value
            });
        }
    }

    [HttpGet("{id}")]
    [ProducesResponseType(typeof(CheckoutResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Consultar([FromRoute] string id, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return Problem(title: "Id obrigatório", statusCode: StatusCodes.Status400BadRequest);
        }

        try
        {
            var token = await _tokenClient.ObterTokenAsync(ct);
            var result = await _checkoutClient.ConsultarAsync(token.AccessToken, id, ct);
            return Ok(result);
        }
        catch (HttpRequestException ex) when (ex.StatusCode is HttpStatusCode.NotFound)
        {
            _logger.LogInformation("Checkout não encontrado: {Id}", id);
            return Problem(title: "Checkout não encontrado", statusCode: StatusCodes.Status404NotFound);
        }
        catch (HttpRequestException ex) when (ex.StatusCode.HasValue)
        {
            _logger.LogError(ex, "Erro HTTP ao consultar checkout {Id}", id);
            return StatusCode((int)ex.StatusCode.Value, new ProblemDetails
            {
                Title = "Erro na chamada ao provedor C6",
                Status = (int)ex.StatusCode.Value
            });
        }
    }

    [HttpPut("{id}/cancelar")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Cancelar([FromRoute] string id, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return Problem(title: "Id obrigatório", statusCode: StatusCodes.Status400BadRequest);
        }

        try
        {
            var token = await _tokenClient.ObterTokenAsync(ct);
            await _checkoutClient.CancelarAsync(token.AccessToken, id, ct);
            return NoContent();
        }
        catch (HttpRequestException ex) when (ex.StatusCode is HttpStatusCode.NotFound)
        {
            _logger.LogInformation("Checkout não encontrado para cancelamento: {Id}", id);
            return Problem(title: "Checkout não encontrado", statusCode: StatusCodes.Status404NotFound);
        }
        catch (HttpRequestException ex) when (ex.StatusCode.HasValue)
        {
            _logger.LogError(ex, "Erro HTTP ao cancelar checkout {Id}", id);
            return StatusCode((int)ex.StatusCode.Value, new ProblemDetails
            {
                Title = "Erro na chamada ao provedor C6",
                Status = (int)ex.StatusCode.Value
            });
        }
    }
}
