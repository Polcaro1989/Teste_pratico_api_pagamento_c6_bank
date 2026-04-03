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
        RuleFor(x => x.ExternalReferenceId).NotEmpty();
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
        RuleFor(x => x.ExternalReferenceId).NotEmpty();
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
            .Must(BrasilValidationRules.IsCpfOrCnpj)
            .WithMessage("TaxId invalido. Informe CPF ou CNPJ validos.");
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
            .Must(p => (p.Card is null) ^ (p.Pix is null))
            .WithMessage("Informe somente um metodo de pagamento: cartao ou pix.");

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
        RuleFor(x => x.CardInfo).NotNull().WithMessage("CardInfo obrigatorio para pagamento com cartao.");

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
    private static readonly Regex DigitsRegex = new("[^0-9]", RegexOptions.Compiled);
    private static readonly Regex CepRegex = new(@"^\d{5}-?\d{3}$", RegexOptions.Compiled);
    private static readonly HashSet<string> Ufs =
    [
        "AC", "AL", "AP", "AM", "BA", "CE", "DF", "ES", "GO", "MA",
        "MT", "MS", "MG", "PA", "PB", "PR", "PE", "PI", "RJ", "RN",
        "RS", "RO", "RR", "SC", "SP", "SE", "TO"
    ];

    public static bool IsCpfOrCnpj(string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return false;
        }

        var digits = OnlyDigits(input);
        return IsCpf(digits) || IsCnpj(digits);
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

    private static bool IsCpf(string digits)
    {
        if (digits.Length != 11 || AllDigitsEqual(digits))
        {
            return false;
        }

        var first = CalculateCpfDigit(digits.AsSpan(0, 9), 10);
        var second = CalculateCpfDigit(digits.AsSpan(0, 10), 11);

        return first == (digits[9] - '0') && second == (digits[10] - '0');
    }

    private static bool IsCnpj(string digits)
    {
        if (digits.Length != 14 || AllDigitsEqual(digits))
        {
            return false;
        }

        var weights1 = new[] { 5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2 };
        var weights2 = new[] { 6, 5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2 };

        var first = CalculateCnpjDigit(digits.AsSpan(0, 12), weights1);
        var second = CalculateCnpjDigit(digits.AsSpan(0, 13), weights2);

        return first == (digits[12] - '0') && second == (digits[13] - '0');
    }

    private static int CalculateCpfDigit(ReadOnlySpan<char> digits, int weightStart)
    {
        var sum = 0;
        for (var i = 0; i < digits.Length; i++)
        {
            sum += (digits[i] - '0') * (weightStart - i);
        }

        var remainder = sum % 11;
        return remainder < 2 ? 0 : 11 - remainder;
    }

    private static int CalculateCnpjDigit(ReadOnlySpan<char> digits, ReadOnlySpan<int> weights)
    {
        var sum = 0;
        for (var i = 0; i < digits.Length; i++)
        {
            sum += (digits[i] - '0') * weights[i];
        }

        var remainder = sum % 11;
        return remainder < 2 ? 0 : 11 - remainder;
    }

    private static bool AllDigitsEqual(string digits)
    {
        var first = digits[0];
        for (var i = 1; i < digits.Length; i++)
        {
            if (digits[i] != first)
            {
                return false;
            }
        }

        return true;
    }

    private static string OnlyDigits(string value) => DigitsRegex.Replace(value, string.Empty);
}
