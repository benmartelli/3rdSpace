using System.Text.Json;
using MessageApi.Models;

namespace MessageApi.Services;

public sealed class LocalMessageStore : IMessageStore
{
    private readonly string _filePath;
    private readonly SemaphoreSlim _gate = new(1, 1);

    public LocalMessageStore(IWebHostEnvironment environment)
    {
        var storageDirectory = string.IsNullOrEmpty(Environment.GetEnvironmentVariable("AWS_LAMBDA_FUNCTION_NAME"))
            ? Path.Combine(environment.ContentRootPath, "App_Data")
            : Path.Combine(Path.GetTempPath(), "3rdspace-messages");

        _filePath = Path.Combine(storageDirectory, "messages.json");
    }

    public async Task<PaginatedMessages> GetAllAsync(int page, int pageSize, CancellationToken cancellationToken)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            var messages = await ReadAsync(cancellationToken);
            var ordered = messages.OrderByDescending(message => message.CreatedAt).ToList();
            var totalPages = (int)Math.Ceiling(ordered.Count / (double)pageSize);
            var items = ordered.Skip((page - 1) * pageSize).Take(pageSize).ToList();

            return new PaginatedMessages(items, page, pageSize, ordered.Count, totalPages);
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<MessageItem> SendAsync(string text, string? author, CancellationToken cancellationToken)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            var messages = await ReadAsync(cancellationToken);
            var normalizedAuthor = string.IsNullOrWhiteSpace(author) ? "Anonymous" : author.Trim();
            var message = new MessageItem(Guid.NewGuid().ToString("N"), text.Trim(), DateTimeOffset.UtcNow, normalizedAuthor);
            messages.Add(message);
            await WriteAsync(messages, cancellationToken);
            return message;
        }
        finally
        {
            _gate.Release();
        }
    }

    private async Task<List<MessageItem>> ReadAsync(CancellationToken cancellationToken)
    {
        if (!File.Exists(_filePath))
        {
            return [];
        }

        await using var stream = File.OpenRead(_filePath);
        return await JsonSerializer.DeserializeAsync<List<MessageItem>>(stream, cancellationToken: cancellationToken) ?? [];
    }

    private async Task WriteAsync(List<MessageItem> messages, CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(_filePath)!);
        await using var stream = File.Create(_filePath);
        await JsonSerializer.SerializeAsync(stream, messages, new JsonSerializerOptions
        {
            WriteIndented = true
        }, cancellationToken);
    }
}
