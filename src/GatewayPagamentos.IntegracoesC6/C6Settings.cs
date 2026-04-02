namespace GatewayPagamentos.IntegracoesC6;

public sealed record C6Settings
{
    public required string BaseUrl { get; init; }
    public required string TokenUrl { get; init; }
    public required string ClientId { get; init; }
    public required string ClientSecret { get; init; }
    public required string ClientCertificatePath { get; init; }
    public required string ClientCertificatePassword { get; init; }
}
