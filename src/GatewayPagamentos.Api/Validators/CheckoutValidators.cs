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
        RuleFor(x => x.TaxId).NotEmpty();
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
        RuleFor(x => x.State).NotEmpty();
        RuleFor(x => x.ZipCode).NotEmpty();
    }
}

public sealed class PaymentValidator : AbstractValidator<PaymentDto>
{
    public PaymentValidator()
    {
        RuleFor(x => x).Must(p => p.Card is not null || p.Pix is not null)
            .WithMessage("Informe cartao ou pix");

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
