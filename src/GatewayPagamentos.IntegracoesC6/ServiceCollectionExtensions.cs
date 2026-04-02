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

        var retryPolicy = HttpPolicyExtensions
            .HandleTransientHttpError()
            .WaitAndRetryAsync(3, attempt => TimeSpan.FromMilliseconds(200 * attempt));

        services.AddHttpClient("c6-auth", client =>
            {
                client.BaseAddress = new Uri(settings.TokenUrl);
                client.Timeout = TimeSpan.FromSeconds(30);
            })
            .ConfigurePrimaryHttpMessageHandler(() => BuildHandler(settings))
            .AddPolicyHandler(retryPolicy);

        services.AddHttpClient("c6-api", client =>
            {
                client.BaseAddress = new Uri(settings.BaseUrl);
                client.Timeout = TimeSpan.FromSeconds(30);
            })
            .ConfigurePrimaryHttpMessageHandler(() => BuildHandler(settings))
            .AddPolicyHandler(retryPolicy);

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

        var cert = new X509Certificate2(settings.ClientCertificatePath, settings.ClientCertificatePassword);
        handler.ClientCertificates.Add(cert);
        return handler;
    }
}
