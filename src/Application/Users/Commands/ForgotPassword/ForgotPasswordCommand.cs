using SAPFIAI.Application.Common.Models;
using FluentValidation;
using System.Text.Json.Serialization;

namespace SAPFIAI.Application.Users.Commands.ForgotPassword;

public record ForgotPasswordCommand : IRequest<Result>
{
    public string? Email { get; init; }

    public string? UserId { get; init; }

    public string? NewPassword { get; init; }

    [JsonIgnore]
    public string? IpAddress { get; init; }

    [JsonIgnore]
    public string? UserAgent { get; init; }
}

public class ForgotPasswordCommandValidator : AbstractValidator<ForgotPasswordCommand>
{
    public ForgotPasswordCommandValidator()
    {
        When(x => string.IsNullOrEmpty(x.UserId), () =>
        {
            RuleFor(x => x.Email)
                .NotEmpty().WithMessage("El email es requerido")
                .EmailAddress().WithMessage("El email no es válido");
        });

        When(x => !string.IsNullOrEmpty(x.UserId), () =>
        {
            RuleFor(x => x.NewPassword)
                .NotEmpty().WithMessage("La nueva contraseña es requerida")
                .MinimumLength(8).WithMessage("La contraseña debe tener al menos 8 caracteres")
                .Matches("[A-Z]").WithMessage("Debe contener al menos una letra mayúscula")
                .Matches("[a-z]").WithMessage("Debe contener al menos una letra minúscula")
                .Matches("[0-9]").WithMessage("Debe contener al menos un número")
                .Matches("[^a-zA-Z0-9]").WithMessage("Debe contener al menos un carácter especial");
        });
    }
}
