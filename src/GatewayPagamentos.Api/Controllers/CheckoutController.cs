using GatewayPagamentos.Api.Contracts;
using GatewayPagamentos.Api.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace GatewayPagamentos.Api.Controllers;

[ApiController]
[Route("api/v1/checkout")]
public sealed class CheckoutController : ControllerBase
{
    private readonly ICheckoutAppService _service;

    public CheckoutController(ICheckoutAppService service)
    {
        _service = service;
    }

    [HttpPost]
    [ProducesResponseType(typeof(CheckoutResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status408RequestTimeout)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status503ServiceUnavailable)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Criar(
        [FromBody] CreateCheckoutRequestDto request,
        CancellationToken ct)
    {
        var result = await _service.CriarAsync(request, ct);
        return CreatedAtAction(nameof(Consultar), new { id = result.Id }, result);
    }

    [HttpPost("autorizar")]
    [ProducesResponseType(typeof(CheckoutResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status408RequestTimeout)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status503ServiceUnavailable)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Autorizar(
        [FromBody] AuthorizeCheckoutRequestDto request,
        CancellationToken ct)
    {
        var result = await _service.AutorizarAsync(request, ct);
        return Ok(result);
    }

    [HttpGet("{id}")]
    [ProducesResponseType(typeof(CheckoutResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status408RequestTimeout)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status503ServiceUnavailable)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Consultar([FromRoute] string id, CancellationToken ct)
    {
        var result = await _service.ConsultarAsync(id, ct);
        return Ok(result);
    }

    [HttpPut("{id}/cancelar")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status408RequestTimeout)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status503ServiceUnavailable)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Cancelar([FromRoute] string id, CancellationToken ct)
    {
        await _service.CancelarAsync(id, ct);
        return NoContent();
    }
}
