using GatewayPagamentos.IntegracoesC6;
using GatewayPagamentos.IntegracoesC6.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace GatewayPagamentos.Api.Controllers;

[ApiController]
[Route("api/checkout")]
public sealed class CheckoutController : ControllerBase
{
    private readonly C6TokenClient _tokenClient;
    private readonly C6CheckoutClient _checkoutClient;

    public CheckoutController(C6TokenClient tokenClient, C6CheckoutClient checkoutClient)
    {
        _tokenClient = tokenClient;
        _checkoutClient = checkoutClient;
    }

    [HttpPost]
    [ProducesResponseType(typeof(CheckoutResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> Criar(
        [FromBody] CheckoutCriarRequest request,
        CancellationToken ct)
    {
        var token = await _tokenClient.ObterTokenAsync(ct);
        var result = await _checkoutClient.CriarAsync(token.AccessToken, request, ct);
        return Ok(result);
    }

    [HttpPost("autorizar")]
    [ProducesResponseType(typeof(CheckoutResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> Autorizar(
        [FromBody] CheckoutAutorizarRequest request,
        CancellationToken ct)
    {
        var token = await _tokenClient.ObterTokenAsync(ct);
        var result = await _checkoutClient.AutorizarAsync(token.AccessToken, request, ct);
        return Ok(result);
    }

    [HttpGet("{id}")]
    [ProducesResponseType(typeof(CheckoutResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> Consultar([FromRoute] string id, CancellationToken ct)
    {
        var token = await _tokenClient.ObterTokenAsync(ct);
        var result = await _checkoutClient.ConsultarAsync(token.AccessToken, id, ct);
        return Ok(result);
    }

    [HttpPut("{id}/cancelar")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Cancelar([FromRoute] string id, CancellationToken ct)
    {
        var token = await _tokenClient.ObterTokenAsync(ct);
        await _checkoutClient.CancelarAsync(token.AccessToken, id, ct);
        return NoContent();
    }
}
