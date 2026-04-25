namespace Dashboard.Api.Contracts;

public sealed record NotificationDto(
    Guid id,
    string kind,
    string title,
    string body,
    string severity,
    DateTimeOffset triggeredAt,
    string? linkPath);

public sealed record NotificationsResponse(IReadOnlyList<NotificationDto> items, int unreadCount);
