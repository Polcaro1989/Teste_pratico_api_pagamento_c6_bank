namespace GatewayPagamentos.Api.Contracts;

public sealed record AddressDto(
    string Street,
    int Number,
    string? Complement,
    string City,
    string State,
    string ZipCode);

public sealed record PayerDto(
    string Name,
    string TaxId,
    string Email,
    string PhoneNumber,
    AddressDto Address);

public sealed record CardInfoDto(string Token);

public sealed record CardDto(
    string Authenticate,
    bool Capture,
    bool FixedInstallments,
    int Installments,
    string InterestType,
    bool Recurrent,
    bool SaveCard,
    string Type,
    string? SoftDescriptor = null,
    CardInfoDto? CardInfo = null);

public sealed record PixPaymentDto(string Key);

public sealed record PaymentDto(
    CardDto? Card,
    PixPaymentDto? Pix);

public sealed record CreateCheckoutRequestDto(
    decimal Amount,
    string Description,
    string ExternalReferenceId,
    PayerDto Payer,
    PaymentDto Payment,
    string RedirectUrl);

public sealed record AuthorizeCheckoutRequestDto(
    decimal Amount,
    string Description,
    string ExternalReferenceId,
    PayerDto Payer,
    PaymentDto Payment,
    string RedirectUrl);

public sealed record CheckoutResponseDto(
    string Id,
    string? Url,
    string? Status);
