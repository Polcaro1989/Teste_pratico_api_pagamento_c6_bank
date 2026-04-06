using System.Text.RegularExpressions;
using FluentValidation;
using GatewayPagamentos.Api.Contracts;

namespace GatewayPagamentos.Api.Validators;

public sealed class PixCriarCobrancaValidator : AbstractValidator<PixCriarCobrancaRequestDto>
{
    public PixCriarCobrancaValidator()
    {
        RuleFor(x => x.Calendario).NotNull().SetValidator(new PixCobrancaCalendarioValidator());
        RuleFor(x => x.Valor).NotNull().SetValidator(new PixCobrancaValorValidator());
        RuleFor(x => x.Chave)
            .NotEmpty()
            .MaximumLength(77);
        RuleFor(x => x.SolicitacaoPagador)
            .MaximumLength(140);

        RuleFor(x => x.InfoAdicionais)
            .Must(x => x is null || x.Count <= 50)
            .WithMessage("infoAdicionais deve ter no maximo 50 itens.");

        RuleForEach(x => x.InfoAdicionais!)
            .SetValidator(new PixInfoAdicionalValidator())
            .When(x => x.InfoAdicionais is not null);

        When(x => x.Devedor is not null, () =>
        {
            RuleFor(x => x.Devedor!).SetValidator(new PixCobrancaDevedorValidator());
        });
    }
}

public sealed class PixCobrancaCalendarioValidator : AbstractValidator<PixCobrancaCalendarioDto>
{
    public PixCobrancaCalendarioValidator()
    {
        RuleFor(x => x.Expiracao)
            .GreaterThan(0)
            .LessThanOrEqualTo(604800);
    }
}

public sealed class PixCobrancaValorValidator : AbstractValidator<PixCobrancaValorDto>
{
    private static readonly Regex ValorRegex = new(@"^\d{1,10}\.\d{2}$", RegexOptions.Compiled);

    public PixCobrancaValorValidator()
    {
        RuleFor(x => x.Original)
            .NotEmpty()
            .Must(x => ValorRegex.IsMatch(x))
            .WithMessage("valor.original invalido. Use formato numerico com 2 casas decimais (ex.: 1.00).");

        RuleFor(x => x.ModalidadeAlteracao)
            .Must(x => x is null || x is 0 or 1)
            .WithMessage("valor.modalidadeAlteracao invalido. Use 0 ou 1.");
    }
}

public sealed class PixCobvValorValidator : AbstractValidator<PixCobvValorDto>
{
    private static readonly Regex ValorRegex = new(@"^\d{1,10}\.\d{2}$", RegexOptions.Compiled);

    public PixCobvValorValidator()
    {
        RuleFor(x => x.Original)
            .NotEmpty()
            .Must(x => ValorRegex.IsMatch(x))
            .WithMessage("valor.original invalido. Use formato numerico com 2 casas decimais (ex.: 1.00).");
    }
}

public sealed class PixCobrancaDevedorValidator : AbstractValidator<PixCobrancaDevedorDto>
{
    private static readonly Regex CpfRegex = new(@"^\d{11}$", RegexOptions.Compiled);
    private static readonly Regex CnpjRegex = new(@"^\d{14}$", RegexOptions.Compiled);

    public PixCobrancaDevedorValidator()
    {
        RuleFor(x => x)
            .Must(TerApenasUmDocumento)
            .WithMessage("devedor deve informar apenas um documento: cpf ou cnpj.");

        RuleFor(x => x.Nome)
            .NotEmpty()
            .MaximumLength(200)
            .When(x => !string.IsNullOrWhiteSpace(x.Cpf) || !string.IsNullOrWhiteSpace(x.Cnpj));

        RuleFor(x => x.Cpf!)
            .Must(x => CpfRegex.IsMatch(x))
            .WithMessage("devedor.cpf invalido. Use 11 digitos.")
            .When(x => !string.IsNullOrWhiteSpace(x.Cpf));

        RuleFor(x => x.Cnpj!)
            .Must(x => CnpjRegex.IsMatch(x))
            .WithMessage("devedor.cnpj invalido. Use 14 digitos.")
            .When(x => !string.IsNullOrWhiteSpace(x.Cnpj));
    }

    private static bool TerApenasUmDocumento(PixCobrancaDevedorDto devedor)
    {
        var temCpf = !string.IsNullOrWhiteSpace(devedor.Cpf);
        var temCnpj = !string.IsNullOrWhiteSpace(devedor.Cnpj);
        return temCpf ^ temCnpj;
    }
}

public sealed class PixInfoAdicionalValidator : AbstractValidator<PixInfoAdicionalDto>
{
    public PixInfoAdicionalValidator()
    {
        RuleFor(x => x.Nome).NotEmpty().MaximumLength(50);
        RuleFor(x => x.Valor).NotEmpty().MaximumLength(200);
    }
}

public sealed class PixCriarCobrancaVencimentoValidator : AbstractValidator<PixCriarCobrancaVencimentoRequestDto>
{
    public PixCriarCobrancaVencimentoValidator()
    {
        RuleFor(x => x.Calendario).NotNull().SetValidator(new PixCobrancaVencimentoCalendarioValidator());
        RuleFor(x => x.Valor).NotNull().SetValidator(new PixCobvValorValidator());
        RuleFor(x => x.Chave).NotEmpty().MaximumLength(77);
        RuleFor(x => x.SolicitacaoPagador).MaximumLength(140);

        RuleFor(x => x.InfoAdicionais)
            .Must(x => x is null || x.Count <= 50)
            .WithMessage("infoAdicionais deve ter no maximo 50 itens.");

        RuleForEach(x => x.InfoAdicionais!)
            .SetValidator(new PixInfoAdicionalValidator())
            .When(x => x.InfoAdicionais is not null);

        When(x => x.Devedor is not null, () =>
        {
            RuleFor(x => x.Devedor!).SetValidator(new PixCobrancaVencimentoDevedorValidator());
        });
    }
}

public sealed class PixCobrancaVencimentoCalendarioValidator : AbstractValidator<PixCobrancaVencimentoCalendarioDto>
{
    public PixCobrancaVencimentoCalendarioValidator()
    {
        RuleFor(x => x.DataDeVencimento)
            .NotEmpty()
            .Must(x => DateOnly.TryParse(x, out _))
            .WithMessage("calendario.dataDeVencimento invalido. Use formato YYYY-MM-DD.");

        RuleFor(x => x.ValidadeAposVencimento)
            .Must(x => x is null || x >= 0)
            .WithMessage("calendario.validadeAposVencimento deve ser >= 0.");
    }
}

public sealed class PixCobrancaVencimentoDevedorValidator : AbstractValidator<PixCobrancaVencimentoDevedorDto>
{
    private static readonly Regex CpfRegex = new(@"^\d{11}$", RegexOptions.Compiled);
    private static readonly Regex CnpjRegex = new(@"^\d{14}$", RegexOptions.Compiled);

    public PixCobrancaVencimentoDevedorValidator()
    {
        RuleFor(x => x)
            .Must(TerApenasUmDocumento)
            .WithMessage("devedor deve informar apenas um documento: cpf ou cnpj.");

        RuleFor(x => x.Cpf!)
            .Must(x => CpfRegex.IsMatch(x))
            .WithMessage("devedor.cpf invalido. Use 11 digitos.")
            .When(x => !string.IsNullOrWhiteSpace(x.Cpf));

        RuleFor(x => x.Cnpj!)
            .Must(x => CnpjRegex.IsMatch(x))
            .WithMessage("devedor.cnpj invalido. Use 14 digitos.")
            .When(x => !string.IsNullOrWhiteSpace(x.Cnpj));

        RuleFor(x => x.Nome)
            .MaximumLength(200);
    }

    private static bool TerApenasUmDocumento(PixCobrancaVencimentoDevedorDto devedor)
    {
        var temCpf = !string.IsNullOrWhiteSpace(devedor.Cpf);
        var temCnpj = !string.IsNullOrWhiteSpace(devedor.Cnpj);
        return temCpf ^ temCnpj;
    }
}

public sealed class PixConfigurarWebhookValidator : AbstractValidator<PixConfigurarWebhookRequestDto>
{
    public PixConfigurarWebhookValidator()
    {
        RuleFor(x => x.WebhookUrl)
            .NotEmpty()
            .Must(url => Uri.TryCreate(url, UriKind.Absolute, out var uri) &&
                         (uri.Scheme == Uri.UriSchemeHttps || uri.Scheme == Uri.UriSchemeHttp))
            .WithMessage("webhookUrl invalida.");
    }
}
