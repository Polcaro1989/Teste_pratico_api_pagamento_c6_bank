using System.IO;
using System.Net.Http;
using System.Security.Cryptography.X509Certificates;
using Microsoft.Extensions.DependencyInjection;
using Polly;
using Polly.Extensions.Http;

namespace GatewayPagamentos.IntegracoesC6;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddC6Clients(this IServiceCollection services, C6Settings settings)
    {
        services.AddSingleton(settings);
        services.AddScoped<IC6TokenClient, C6TokenClient>();
        services.AddScoped<IC6CheckoutClient, C6CheckoutClient>();

        var retryPolicy = HttpPolicyExtensions
            .HandleTransientHttpError()
            .OrResult(resp => resp.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
            .WaitAndRetryAsync(
                retryCount: 3,
                sleepDurationProvider: attempt =>
                {
                    var jitter = Random.Shared.Next(0, 100);
                    return TimeSpan.FromMilliseconds(200 * attempt + jitter);
                });

        var breakerPolicy = Policy
            .Handle<HttpRequestException>()
            .OrResult<HttpResponseMessage>(r => (int)r.StatusCode >= 500)
            .CircuitBreakerAsync(
                handledEventsAllowedBeforeBreaking: 5,
                durationOfBreak: TimeSpan.FromSeconds(30),
                onBreak: (ex, ts) => Console.WriteLine($"Breaker aberto por {ts}. Motivo: {ex}"),
                onReset: () => Console.WriteLine("Breaker resetado"),
                onHalfOpen: () => Console.WriteLine("Breaker em half-open"));

        services.AddHttpClient("c6-auth", client =>
            {
                client.BaseAddress = new Uri(settings.TokenUrl);
                client.Timeout = TimeSpan.FromSeconds(20);
            })
            .ConfigurePrimaryHttpMessageHandler(() => BuildHandler(settings))
            .AddPolicyHandler(retryPolicy)
            .AddPolicyHandler(breakerPolicy);

        services.AddHttpClient("c6-api", client =>
            {
                client.BaseAddress = new Uri(settings.BaseUrl);
                client.Timeout = TimeSpan.FromSeconds(20);
            })
            .ConfigurePrimaryHttpMessageHandler(() => BuildHandler(settings))
            .AddPolicyHandler(retryPolicy)
            .AddPolicyHandler(breakerPolicy);

        return services;
    }

    private static HttpMessageHandler BuildHandler(C6Settings settings)
    {
        var handler = new HttpClientHandler
        {
            ClientCertificateOptions = ClientCertificateOption.Manual,
            SslProtocols = System.Security.Authentication.SslProtocols.Tls12 |
                           System.Security.Authentication.SslProtocols.Tls13
        };

        var env = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
        var isDevelopment = string.Equals(env, "Development", StringComparison.OrdinalIgnoreCase);

        var certPath = settings.ClientCertificatePath;
        var certPassword = settings.ClientCertificatePassword;

        if (!string.IsNullOrWhiteSpace(certPath) && File.Exists(certPath))
        {
            try
            {
                var cert = X509CertificateLoader.LoadPkcs12FromFile(
                    certPath,
                    certPassword,
                    X509KeyStorageFlags.EphemeralKeySet);
                handler.ClientCertificates.Add(cert);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(
                    $"Erro ao carregar certificado em '{certPath}'",
                    ex);
            }
        }
        else if (!isDevelopment)
        {
            throw new InvalidOperationException(
                $"Certificado não encontrado em '{certPath}'. Configure C6__ClientCertificatePath/C6__ClientCertificatePassword.");
        }
        else
        {
            // Ambiente de desenvolvimento sem certificado: permite subir API/Swagger.
            handler.ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;
        }

        return handler;
    }
}
