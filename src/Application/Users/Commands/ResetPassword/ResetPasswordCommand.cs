using SAPFIAI.Application.Common.Models;
using FluentValidation;
using System.Text.Json.Serialization;

namespace SAPFIAI.Application.Users.Commands.ResetPassword;

public record ResetPasswordCommand : IRequest<Result>
{
    public required string OldPassword { get; init; }

    public required string NewPassword { get; init; }

    public required string ConfirmPassword { get; init; }

    [JsonIgnore]
    public string? UserId { get; init; }

    [JsonIgnore]
    public string? IpAddress { get; init; }

    [JsonIgnore]
    public string? UserAgent { get; init; }
}

public class ResetPasswordCommandValidator : AbstractValidator<ResetPasswordCommand>
{
    public ResetPasswordCommandValidator()
    {
        RuleFor(x => x.OldPassword)
            .NotEmpty().WithMessage("La contraseña actual es requerida");

        RuleFor(x => x.NewPassword)
            .NotEmpty().WithMessage("La nueva contraseña es requerida")
            .MinimumLength(8).WithMessage("La contraseña debe tener al menos 8 caracteres")
            .Matches("[A-Z]").WithMessage("Debe contener al menos una letra mayúscula")
            .Matches("[a-z]").WithMessage("Debe contener al menos una letra minúscula")
            .Matches("[0-9]").WithMessage("Debe contener al menos un número")
            .Matches("[^a-zA-Z0-9]").WithMessage("Debe contener al menos un carácter especial")
            .NotEqual(x => x.OldPassword).WithMessage("La nueva contraseña no puede ser igual a la anterior");

        RuleFor(x => x.ConfirmPassword)
            .NotEmpty().WithMessage("Debe confirmar la contraseña")
            .Equal(x => x.NewPassword).WithMessage("Las contraseñas no coinciden");
    }
}
