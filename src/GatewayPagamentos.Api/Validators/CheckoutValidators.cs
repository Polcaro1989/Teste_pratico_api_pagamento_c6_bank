using System.Text.RegularExpressions;
using FluentValidation;
using GatewayPagamentos.Api.Contracts;

namespace GatewayPagamentos.Api.Validators;

public sealed class CheckoutCriarValidator : AbstractValidator<CreateCheckoutRequestDto>
{
    public CheckoutCriarValidator()
    {
        RuleFor(x => x.Amount).GreaterThan(0);
        RuleFor(x => x.Description).NotEmpty();
        RuleFor(x => x.ExternalReferenceId)
            .NotEmpty()
            .Matches("^[a-zA-Z0-9]{1,10}$")
            .WithMessage("ExternalReferenceId invalido. Use apenas alfanumerico com 1 a 10 caracteres.");
        RuleFor(x => x.RedirectUrl).NotEmpty();
        RuleFor(x => x.Payer).SetValidator(new PayerValidator());
        RuleFor(x => x.Payment).SetValidator(new PaymentValidator());
    }
}

public sealed class CheckoutAutorizarValidator : AbstractValidator<AuthorizeCheckoutRequestDto>
{
    public CheckoutAutorizarValidator()
    {
        RuleFor(x => x.Amount).GreaterThan(0);
        RuleFor(x => x.Description).NotEmpty();
        RuleFor(x => x.ExternalReferenceId)
            .NotEmpty()
            .Matches("^[a-zA-Z0-9]{1,10}$")
            .WithMessage("ExternalReferenceId invalido. Use apenas alfanumerico com 1 a 10 caracteres.");
        RuleFor(x => x.RedirectUrl).NotEmpty();
        RuleFor(x => x.Payer).SetValidator(new PayerValidator());
        RuleFor(x => x.Payment).SetValidator(new PaymentValidator());
    }
}

public sealed class PayerValidator : AbstractValidator<PayerDto>
{
    public PayerValidator()
    {
        RuleFor(x => x.Name).NotEmpty();
        RuleFor(x => x.TaxId)
            .NotEmpty()
            .Must(BrasilValidationRules.IsTaxIdFormat)
            .WithMessage("TaxId invalido. Informe somente digitos com 11 ou 14 caracteres.");
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.PhoneNumber)
            .NotEmpty()
            .Matches(@"^\+?[0-9]{10,15}$")
            .WithMessage("PhoneNumber invalido");
        RuleFor(x => x.Address).SetValidator(new AddressValidator());
    }
}

public sealed class AddressValidator : AbstractValidator<AddressDto>
{
    public AddressValidator()
    {
        RuleFor(x => x.Street).NotEmpty();
        RuleFor(x => x.Number).GreaterThan(0);
        RuleFor(x => x.City).NotEmpty();
        RuleFor(x => x.State)
            .NotEmpty()
            .Must(BrasilValidationRules.IsUf)
            .WithMessage("State invalido. Use UF de 2 letras (ex.: SP).");
        RuleFor(x => x.ZipCode)
            .NotEmpty()
            .Must(BrasilValidationRules.IsCep)
            .WithMessage("ZipCode invalido. Use 8 digitos (ex.: 01311000 ou 01311-000).");
    }
}

public sealed class PaymentValidator : AbstractValidator<PaymentDto>
{
    public PaymentValidator()
    {
        RuleFor(x => x)
            .Must(p => p.Card is not null || p.Pix is not null)
            .WithMessage("Informe cartao ou pix.");

        When(x => x.Card is not null, () =>
        {
            RuleFor(x => x.Card!).SetValidator(new CardValidator());
        });

        When(x => x.Pix is not null, () =>
        {
            RuleFor(x => x.Pix!).SetValidator(new PixPaymentValidator());
        });
    }
}

public sealed class CardValidator : AbstractValidator<CardDto>
{
    public CardValidator()
    {
        RuleFor(x => x.Authenticate).NotEmpty();
        RuleFor(x => x.Installments).GreaterThan(0);
        RuleFor(x => x.InterestType).NotEmpty();
        RuleFor(x => x.Type).NotEmpty();

        When(x => x.CardInfo is not null, () =>
        {
            RuleFor(x => x.CardInfo!).SetValidator(new CardInfoValidator());
        });
    }
}

public sealed class CardInfoValidator : AbstractValidator<CardInfoDto>
{
    public CardInfoValidator()
    {
        RuleFor(x => x.Token).NotEmpty();
    }
}

public sealed class PixPaymentValidator : AbstractValidator<PixPaymentDto>
{
    public PixPaymentValidator()
    {
        RuleFor(x => x.Key).NotEmpty();
    }
}

internal static class BrasilValidationRules
{
    private static readonly Regex TaxIdRegex = new(@"^(\d{11}|\d{14})$", RegexOptions.Compiled);
    private static readonly Regex CepRegex = new(@"^\d{5}-?\d{3}$", RegexOptions.Compiled);
    private static readonly HashSet<string> Ufs =
    [
        "AC", "AL", "AP", "AM", "BA", "CE", "DF", "ES", "GO", "MA",
        "MT", "MS", "MG", "PA", "PB", "PR", "PE", "PI", "RJ", "RN",
        "RS", "RO", "RR", "SC", "SP", "SE", "TO"
    ];

    public static bool IsTaxIdFormat(string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return false;
        }

        return TaxIdRegex.IsMatch(input.Trim());
    }

    public static bool IsCep(string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return false;
        }

        return CepRegex.IsMatch(input.Trim());
    }

    public static bool IsUf(string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return false;
        }

        var uf = input.Trim().ToUpperInvariant();
        return uf.Length == 2 && Ufs.Contains(uf);
    }
}
