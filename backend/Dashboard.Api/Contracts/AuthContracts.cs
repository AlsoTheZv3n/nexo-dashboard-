using FluentValidation;

namespace Dashboard.Api.Contracts;

public sealed record LoginRequest(string Username, string Password);

public sealed record LoginResponse(
    string AccessToken,
    string RefreshToken,
    DateTimeOffset AccessExpiresAt,
    UserInfo User);

public sealed record RefreshRequest(string RefreshToken);

public sealed record UserInfo(Guid Id, string Username, string Role);

public sealed class LoginRequestValidator : AbstractValidator<LoginRequest>
{
    public LoginRequestValidator()
    {
        RuleFor(x => x.Username).NotEmpty().MaximumLength(64);
        RuleFor(x => x.Password).NotEmpty().MaximumLength(256);
    }
}

public sealed class RefreshRequestValidator : AbstractValidator<RefreshRequest>
{
    public RefreshRequestValidator()
    {
        RuleFor(x => x.RefreshToken).NotEmpty();
    }
}
