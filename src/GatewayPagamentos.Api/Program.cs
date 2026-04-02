using System.Text.Json;
using FluentValidation;
using FluentValidation.AspNetCore;
using GatewayPagamentos.Api.Health;
using GatewayPagamentos.Api.Middleware;
using GatewayPagamentos.Api.Services;
using GatewayPagamentos.Api.Validators;
using GatewayPagamentos.IntegracoesC6;
using Microsoft.OpenApi.Models;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((ctx, cfg) => cfg
    .ReadFrom.Configuration(ctx.Configuration)
    .Enrich.WithThreadId()
    .WriteTo.Console());

var c6Settings = builder.Configuration.GetSection("C6").Get<C6Settings>()
                 ?? throw new InvalidOperationException("Seção 'C6' ausente em appsettings.json");

builder.Services.AddControllers()
    .AddJsonOptions(opts => opts.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase)
    .AddFluentValidation();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Gateway de Pagamentos C6", Version = "v1" });
});

builder.Services.AddC6Clients(c6Settings);
builder.Services.AddScoped<ICheckoutAppService, CheckoutAppService>();
builder.Services.AddValidatorsFromAssemblyContaining<CheckoutCriarValidator>();
builder.Services.AddHealthChecks()
    .AddCheck<C6HealthCheck>("c6");

var app = builder.Build();

app.UseSerilogRequestLogging();
app.UseMiddleware<CorrelationIdMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Gateway de Pagamentos C6 v1");
        c.RoutePrefix = "swagger";
    });
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();
app.MapHealthChecks("/health");

app.Run();
