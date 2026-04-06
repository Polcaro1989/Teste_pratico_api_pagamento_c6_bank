using System.IO;
using System.Net.Http;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Microsoft.Extensions.DependencyInjection;
using Polly;
using Polly.Extensions.Http;
using Serilog;

namespace GatewayPagamentos.IntegracoesC6;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddC6Clients(this IServiceCollection services, C6Settings settings)
    {
        services.AddSingleton(settings);
        services.AddMemoryCache();
        services.AddSingleton<IC6TokenClient, C6TokenClient>();
        services.AddScoped<IC6CheckoutClient, C6CheckoutClient>();
        services.AddScoped<IC6PixClient, C6PixClient>();

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
                onBreak: (ex, ts) => Log.Warning(ex.Exception, "Breaker aberto por {Duration}", ts),
                onReset: () => Log.Information("Breaker resetado"),
                onHalfOpen: () => Log.Information("Breaker em half-open"));

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
                var cert = LoadClientCertificate(certPath, certPassword);
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
        else if (isDevelopment && settings.AllowInsecureServerCertificateInDevelopment)
        {
            handler.ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;
        }
        else
        {
            throw new InvalidOperationException(
                $"Certificado não encontrado em '{certPath}'. Configure C6__ClientCertificatePath/C6__ClientCertificatePassword. " +
                "Para bypass local em Development, habilite C6__AllowInsecureServerCertificateInDevelopment=true.");
        }

        return handler;
    }

    private static X509Certificate2 LoadClientCertificate(string certPath, string certPassword)
    {
        Exception? lastError = null;

        foreach (var flags in ResolveCertificateKeyStorageFlags())
        {
            try
            {
                return X509CertificateLoader.LoadPkcs12FromFile(certPath, certPassword, flags);
            }
            catch (CryptographicException ex)
            {
                lastError = ex;
            }
        }

        throw new InvalidOperationException(
            $"Não foi possível carregar o certificado usando os modos suportados no sistema operacional. Caminho: '{certPath}'",
            lastError);
    }

    private static X509KeyStorageFlags[] ResolveCertificateKeyStorageFlags()
    {
        // No Windows, Schannel falha com EphemeralKeySet em mTLS e alguns ambientes bloqueiam MachineKeySet.
        if (OperatingSystem.IsWindows())
        {
            return
            [
                X509KeyStorageFlags.UserKeySet | X509KeyStorageFlags.PersistKeySet,
                X509KeyStorageFlags.MachineKeySet | X509KeyStorageFlags.PersistKeySet
            ];
        }

        return [X509KeyStorageFlags.EphemeralKeySet];
    }
}
