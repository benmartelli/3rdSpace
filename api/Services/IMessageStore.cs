using MessageApi.Models;

namespace MessageApi.Services;

public interface IMessageStore
{
    Task<PaginatedMessages> GetAllAsync(int page, int pageSize, CancellationToken cancellationToken);
    Task<MessageItem> SendAsync(string text, string? author, CancellationToken cancellationToken);
}
