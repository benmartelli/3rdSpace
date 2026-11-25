using Amazon;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using MessageApi.Models;

namespace MessageApi.Services;

public sealed class DynamoDbMessageStore : IMessageStore
{
    private readonly IAmazonDynamoDB _client;
    private readonly string _tableName;

    public DynamoDbMessageStore()
    {
        var tableArn = Environment.GetEnvironmentVariable("DYNAMODB_TABLE_ARN")
            ?? "arn:aws:dynamodb:ap-southeast-2:189324273358:table/messages";
        var arnParts = tableArn.Split(':', StringSplitOptions.None);
        var region = arnParts.Length > 3 ? arnParts[3] : Environment.GetEnvironmentVariable("AWS_REGION") ?? "ap-southeast-2";
        var resource = arnParts.Length > 5 ? arnParts[^1] : "table/messages";
        var tableName = Environment.GetEnvironmentVariable("DYNAMODB_TABLE_NAME")
            ?? (resource.StartsWith("table/", StringComparison.Ordinal) ? resource[6..] : resource);

        _client = new AmazonDynamoDBClient(RegionEndpoint.GetBySystemName(region));
        _tableName = tableName;
    }

    public async Task<PaginatedMessages> GetAllAsync(int page, int pageSize, CancellationToken cancellationToken)
    {
        var messages = await ScanAllAsync(cancellationToken);
        var ordered = messages.OrderByDescending(message => message.CreatedAt).ToList();
        var totalPages = (int)Math.Ceiling(ordered.Count / (double)pageSize);
        var items = ordered.Skip((page - 1) * pageSize).Take(pageSize).ToList();

        return new PaginatedMessages(items, page, pageSize, ordered.Count, totalPages);
    }

    public async Task<MessageItem> SendAsync(string text, string? author, CancellationToken cancellationToken)
    {
        var normalizedAuthor = string.IsNullOrWhiteSpace(author) ? "Anonymous" : author.Trim();
        var message = new MessageItem(
            Guid.NewGuid().ToString("N"),
            text.Trim(),
            DateTimeOffset.UtcNow,
            normalizedAuthor);

        var request = new PutItemRequest
        {
            TableName = _tableName,
            Item = new Dictionary<string, AttributeValue>
            {
                ["id"] = new() { S = message.Id },
                ["text"] = new() { S = message.Text },
                ["createdAt"] = new() { S = message.CreatedAt.ToUniversalTime().ToString("O") },
                ["author"] = new() { S = message.Author }
            }
        };

        await _client.PutItemAsync(request, cancellationToken);
        return message;
    }

    private async Task<List<MessageItem>> ScanAllAsync(CancellationToken cancellationToken)
    {
        var messages = new List<MessageItem>();
        Dictionary<string, AttributeValue>? exclusiveStartKey = null;

        do
        {
            var response = await _client.ScanAsync(new ScanRequest
            {
                TableName = _tableName,
                ExclusiveStartKey = exclusiveStartKey
            }, cancellationToken);

            foreach (var item in response.Items ?? [])
            {
                var id = item.TryGetValue("id", out var idValue) ? idValue?.S : null;
                var text = item.TryGetValue("text", out var textValue) ? textValue?.S : null;
                var author = item.TryGetValue("author", out var authorValue) ? authorValue?.S : null;

                if (string.IsNullOrEmpty(id) || string.IsNullOrEmpty(text))
                {
                    continue;
                }

                DateTimeOffset createdAt = DateTimeOffset.MinValue;
                if (item.TryGetValue("createdAt", out var createdValue)
                    && !string.IsNullOrEmpty(createdValue?.S)
                    && DateTimeOffset.TryParse(createdValue.S, out var parsedCreatedAt))
                {
                    createdAt = parsedCreatedAt;
                }

                messages.Add(new MessageItem(id, text, createdAt, string.IsNullOrWhiteSpace(author) ? "Anonymous" : author));
            }

            exclusiveStartKey = response.LastEvaluatedKey is { Count: > 0 }
                ? response.LastEvaluatedKey
                : null;
        }
        while (exclusiveStartKey is not null);

        return messages;
    }
}
