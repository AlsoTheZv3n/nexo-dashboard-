using Dashboard.Core.Entities;
using FluentValidation;

namespace Dashboard.Api.Contracts;

public sealed record CreateApiKeyRequest(string Name, UserRole Role, int? ExpiresInDays);

public sealed record ApiKeyDto(
    Guid id,
    string name,
    string prefix,
    string role,
    bool isActive,
    DateTimeOffset createdAt,
    DateTimeOffset? expiresAt,
    DateTimeOffset? lastUsedAt);

public sealed record ApiKeyCreatedDto(ApiKeyDto key, string plaintext);

public sealed class CreateApiKeyRequestValidator : AbstractValidator<CreateApiKeyRequest>
{
    public CreateApiKeyRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(128);
        RuleFor(x => x.Role).IsInEnum();
        RuleFor(x => x.ExpiresInDays).InclusiveBetween(1, 3650).When(x => x.ExpiresInDays.HasValue);
    }
}
