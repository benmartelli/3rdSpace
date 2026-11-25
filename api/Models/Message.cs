namespace MessageApi.Models;

public sealed record MessageItem(string Id, string Text, DateTimeOffset CreatedAt, string Author = "Anonymous");

public sealed record SendMessageRequest(string Text, string? Author = null);

public sealed record PaginatedMessages(
    IReadOnlyList<MessageItem> Items,
    int Page,
    int PageSize,
    int TotalCount,
    int TotalPages);
