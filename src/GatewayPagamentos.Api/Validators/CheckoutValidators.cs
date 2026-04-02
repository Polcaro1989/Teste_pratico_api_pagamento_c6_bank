using FluentValidation;
using GatewayPagamentos.IntegracoesC6.Models;

namespace GatewayPagamentos.Api.Validators;

public sealed class CheckoutCriarValidator : AbstractValidator<CheckoutCriarRequest>
{
    public CheckoutCriarValidator()
    {
        RuleFor(x => x.Valor).GreaterThan(0);
        RuleFor(x => x.Descricao).NotEmpty();
        RuleFor(x => x.ReferenciaExterna).NotEmpty();
        RuleFor(x => x.UrlRedirect).NotEmpty();
        RuleFor(x => x.Pagador).SetValidator(new PagadorValidator());
        RuleFor(x => x.Pagamento).SetValidator(new PagamentoValidator());
    }
}

public sealed class CheckoutAutorizarValidator : AbstractValidator<CheckoutAutorizarRequest>
{
    public CheckoutAutorizarValidator()
    {
        RuleFor(x => x.Valor).GreaterThan(0);
        RuleFor(x => x.Descricao).NotEmpty();
        RuleFor(x => x.ReferenciaExterna).NotEmpty();
        RuleFor(x => x.UrlRedirect).NotEmpty();
        RuleFor(x => x.Pagador).SetValidator(new PagadorValidator());
        RuleFor(x => x.Pagamento).SetValidator(new PagamentoValidator());
    }
}

public sealed class PagadorValidator : AbstractValidator<Pagador>
{
    public PagadorValidator()
    {
        RuleFor(x => x.Nome).NotEmpty();
        RuleFor(x => x.Documento).NotEmpty();
        RuleFor(x => x.Email).NotEmpty();
        RuleFor(x => x.Endereco).SetValidator(new EnderecoValidator());
    }
}

public sealed class EnderecoValidator : AbstractValidator<Endereco>
{
    public EnderecoValidator()
    {
        RuleFor(x => x.Logradouro).NotEmpty();
        RuleFor(x => x.Numero).GreaterThan(0);
        RuleFor(x => x.Cidade).NotEmpty();
        RuleFor(x => x.Estado).NotEmpty();
        RuleFor(x => x.Cep).NotEmpty();
    }
}

public sealed class PagamentoValidator : AbstractValidator<Pagamento>
{
    public PagamentoValidator()
    {
        RuleFor(x => x).Must(p => p.Cartao is not null || p.Pix is not null)
            .WithMessage("Informe cartão ou pix");
    }
}
