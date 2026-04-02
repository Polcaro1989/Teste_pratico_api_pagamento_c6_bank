using System.Net;
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
    private readonly ILogger<CheckoutController> _logger;

    public CheckoutController(
        ICheckoutAppService service,
        ILogger<CheckoutController> logger)
    {
        _service = service;
        _logger = logger;
    }

    [HttpPost]
    [ProducesResponseType(typeof(CheckoutResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status408RequestTimeout)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Criar(
        [FromBody] CreateCheckoutRequestDto request,
        CancellationToken ct)
    {
        try
        {
            var result = await _service.CriarAsync(request, ct);
            return CreatedAtAction(nameof(Consultar), new { id = result.Id }, result);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Requisicao invalida ao criar checkout. Ref={Ref} Valor={Valor}", request.ExternalReferenceId, request.Amount);
            return Problem(title: "Requisicao invalida", detail: ex.Message, statusCode: StatusCodes.Status400BadRequest);
        }
        catch (HttpRequestException ex) when (ex.StatusCode is HttpStatusCode.BadRequest)
        {
            _logger.LogWarning(ex, "C6 rejeitou criacao de checkout. Ref={Ref}", request.ExternalReferenceId);
            return Problem(title: "Requisicao invalida", statusCode: StatusCodes.Status400BadRequest);
        }
        catch (HttpRequestException ex) when (ex.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
        {
            _logger.LogError(ex, "Falha de autenticacao com C6 na criacao. Ref={Ref}", request.ExternalReferenceId);
            return Problem(title: "Erro de autenticacao com provedor", statusCode: StatusCodes.Status503ServiceUnavailable);
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
        catch (OperationCanceledException ex)
        {
            _logger.LogWarning(ex, "Criacao de checkout cancelada/timeout. Ref={Ref}", request.ExternalReferenceId);
            return Problem(title: "Requisicao expirou", statusCode: StatusCodes.Status408RequestTimeout);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro inesperado ao criar checkout. Ref={Ref}", request.ExternalReferenceId);
            return Problem(title: "Erro interno do servidor", statusCode: StatusCodes.Status500InternalServerError);
        }
    }

    [HttpPost("autorizar")]
    [ProducesResponseType(typeof(CheckoutResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status408RequestTimeout)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Autorizar(
        [FromBody] AuthorizeCheckoutRequestDto request,
        CancellationToken ct)
    {
        try
        {
            var result = await _service.AutorizarAsync(request, ct);
            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Requisicao invalida ao autorizar checkout. Ref={Ref} Valor={Valor}", request.ExternalReferenceId, request.Amount);
            return Problem(title: "Requisicao invalida", detail: ex.Message, statusCode: StatusCodes.Status400BadRequest);
        }
        catch (HttpRequestException ex) when (ex.StatusCode is HttpStatusCode.BadRequest)
        {
            _logger.LogWarning(ex, "Erro de validacao ao autorizar checkout");
            return Problem(title: "Requisicao invalida", statusCode: StatusCodes.Status400BadRequest);
        }
        catch (HttpRequestException ex) when (ex.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
        {
            _logger.LogError(ex, "Falha de autenticacao com C6 na autorizacao. Ref={Ref}", request.ExternalReferenceId);
            return Problem(title: "Erro de autenticacao com provedor", statusCode: StatusCodes.Status503ServiceUnavailable);
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
        catch (OperationCanceledException ex)
        {
            _logger.LogWarning(ex, "Autorizacao de checkout cancelada/timeout. Ref={Ref}", request.ExternalReferenceId);
            return Problem(title: "Requisicao expirou", statusCode: StatusCodes.Status408RequestTimeout);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro inesperado ao autorizar checkout. Ref={Ref}", request.ExternalReferenceId);
            return Problem(title: "Erro interno do servidor", statusCode: StatusCodes.Status500InternalServerError);
        }
    }

    [HttpGet("{id}")]
    [ProducesResponseType(typeof(CheckoutResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status408RequestTimeout)]
    public async Task<IActionResult> Consultar([FromRoute] string id, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return Problem(title: "Id obrigatorio", statusCode: StatusCodes.Status400BadRequest);
        }

        try
        {
            var result = await _service.ConsultarAsync(id, ct);
            return Ok(result);
        }
        catch (HttpRequestException ex) when (ex.StatusCode is HttpStatusCode.NotFound)
        {
            _logger.LogInformation("Checkout nao encontrado: {Id}", id);
            return Problem(title: "Checkout nao encontrado", statusCode: StatusCodes.Status404NotFound);
        }
        catch (HttpRequestException ex) when (ex.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
        {
            _logger.LogError(ex, "Falha de autenticacao com C6 na consulta. Id={Id}", id);
            return Problem(title: "Erro de autenticacao com provedor", statusCode: StatusCodes.Status503ServiceUnavailable);
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
        catch (OperationCanceledException ex)
        {
            _logger.LogWarning(ex, "Consulta de checkout cancelada/timeout. Id={Id}", id);
            return Problem(title: "Requisicao expirou", statusCode: StatusCodes.Status408RequestTimeout);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro inesperado ao consultar checkout {Id}", id);
            return Problem(title: "Erro interno do servidor", statusCode: StatusCodes.Status500InternalServerError);
        }
    }

    [HttpPut("{id}/cancelar")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status408RequestTimeout)]
    public async Task<IActionResult> Cancelar([FromRoute] string id, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return Problem(title: "Id obrigatorio", statusCode: StatusCodes.Status400BadRequest);
        }

        try
        {
            await _service.CancelarAsync(id, ct);
            return NoContent();
        }
        catch (HttpRequestException ex) when (ex.StatusCode is HttpStatusCode.NotFound)
        {
            _logger.LogInformation("Checkout nao encontrado para cancelamento: {Id}", id);
            return Problem(title: "Checkout nao encontrado", statusCode: StatusCodes.Status404NotFound);
        }
        catch (HttpRequestException ex) when (ex.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
        {
            _logger.LogError(ex, "Falha de autenticacao com C6 ao cancelar. Id={Id}", id);
            return Problem(title: "Erro de autenticacao com provedor", statusCode: StatusCodes.Status503ServiceUnavailable);
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
        catch (OperationCanceledException ex)
        {
            _logger.LogWarning(ex, "Cancelamento de checkout cancelado/timeout. Id={Id}", id);
            return Problem(title: "Requisicao expirou", statusCode: StatusCodes.Status408RequestTimeout);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro inesperado ao cancelar checkout {Id}", id);
            return Problem(title: "Erro interno do servidor", statusCode: StatusCodes.Status500InternalServerError);
        }
    }
}
