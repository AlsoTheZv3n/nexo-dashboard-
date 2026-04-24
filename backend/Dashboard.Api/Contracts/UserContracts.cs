using Dashboard.Core.Entities;
using FluentValidation;

namespace Dashboard.Api.Contracts;

public sealed record UserDto(
    Guid id,
    string username,
    string role,
    bool isActive,
    DateTimeOffset createdAt,
    DateTimeOffset? lastLoginAt);

public sealed record CreateUserRequest(string Username, string Password, UserRole Role);

public sealed record UpdateUserRequest(UserRole? Role, bool? IsActive);

public sealed record ResetPasswordRequest(string NewPassword);

public sealed class CreateUserRequestValidator : AbstractValidator<CreateUserRequest>
{
    private static readonly char[] AllowedExtraChars = ['.', '_', '-'];

    public CreateUserRequestValidator()
    {
        RuleFor(x => x.Username)
            .NotEmpty()
            .MaximumLength(64)
            .Must(u => !string.IsNullOrEmpty(u) && u.All(c => char.IsLetterOrDigit(c) || AllowedExtraChars.Contains(c)))
            .WithMessage("Username must be alphanumeric (. _ - allowed).");
        RuleFor(x => x.Password).NotEmpty().MinimumLength(8).MaximumLength(256);
        RuleFor(x => x.Role).IsInEnum();
    }
}

public sealed class UpdateUserRequestValidator : AbstractValidator<UpdateUserRequest>
{
    public UpdateUserRequestValidator()
    {
        RuleFor(x => x.Role!).IsInEnum().When(x => x.Role.HasValue);
    }
}

public sealed class ResetPasswordRequestValidator : AbstractValidator<ResetPasswordRequest>
{
    public ResetPasswordRequestValidator()
    {
        RuleFor(x => x.NewPassword).NotEmpty().MinimumLength(8).MaximumLength(256);
    }
}
