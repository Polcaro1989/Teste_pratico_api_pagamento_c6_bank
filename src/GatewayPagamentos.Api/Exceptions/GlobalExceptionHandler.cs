using System.Net;
using FluentValidation;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace GatewayPagamentos.Api.Exceptions;

public sealed class GlobalExceptionHandler : IExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> _logger;

    public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger)
    {
        _logger = logger;
    }

    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        var (statusCode, title, detail, logLevel) = MapException(exception);

        _logger.Log(logLevel, exception, "Unhandled exception mapped to {StatusCode}", statusCode);

        var problem = new ProblemDetails
        {
            Status = statusCode,
            Title = title,
            Detail = detail,
            Instance = httpContext.Request.Path
        };
        problem.Extensions["trace_id"] = httpContext.TraceIdentifier;

        httpContext.Response.StatusCode = statusCode;
        await httpContext.Response.WriteAsJsonAsync(problem, cancellationToken);
        return true;
    }

    private static (int StatusCode, string Title, string? Detail, LogLevel LogLevel) MapException(Exception exception)
    {
        return exception switch
        {
            ValidationException ex => (
                StatusCodes.Status400BadRequest,
                "Requisicao invalida",
                ex.Message,
                LogLevel.Warning),

            ArgumentException ex => (
                StatusCodes.Status400BadRequest,
                "Requisicao invalida",
                ex.Message,
                LogLevel.Warning),

            InvalidOperationException ex when ex.Message.Contains("Certificado", StringComparison.OrdinalIgnoreCase) => (
                StatusCodes.Status503ServiceUnavailable,
                "Falha de certificado mTLS do provedor",
                null,
                LogLevel.Error),

            HttpRequestException { StatusCode: HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden } => (
                StatusCodes.Status503ServiceUnavailable,
                "Erro de autenticacao com provedor",
                null,
                LogLevel.Error),

            HttpRequestException ex when ex.StatusCode.HasValue => (
                (int)ex.StatusCode.Value,
                "Erro na chamada ao provedor C6",
                null,
                LogLevel.Error),

            HttpRequestException => (
                StatusCodes.Status503ServiceUnavailable,
                "Provedor C6 indisponivel",
                null,
                LogLevel.Error),

            OperationCanceledException => (
                StatusCodes.Status408RequestTimeout,
                "Requisicao expirou",
                null,
                LogLevel.Warning),

            _ => (
                StatusCodes.Status500InternalServerError,
                "Erro interno do servidor",
                null,
                LogLevel.Error)
        };
    }
}
