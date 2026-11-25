using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using MessageApi.Models;
using MessageApi.Services;
using Microsoft.AspNetCore.Mvc;

namespace MessageApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class MessagesController(IMessageStore store) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<PaginatedMessages>(StatusCodes.Status200OK)]
    public async Task<ActionResult<PaginatedMessages>> GetAll(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        page = page < 1 ? 1 : page;
        pageSize = pageSize is < 1 or > 100 ? 10 : pageSize;

        try
        {
            var messages = await store.GetAllAsync(page, pageSize, cancellationToken);
            return Ok(messages);
        }
        catch (AmazonDynamoDBException exception)
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new
            {
                detail = "The message database is unavailable.",
                error = exception.ErrorCode
            });
        }
    }

    [HttpPost]
    [ProducesResponseType<MessageItem>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<MessageItem>> Send(
        [FromBody] SendMessageRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Text))
        {
            return BadRequest(new { detail = "Message text is required." });
        }

        if (request.Text.Trim().Length > 500)
        {
            return BadRequest(new { detail = "Message text cannot exceed 500 characters." });
        }

        if (request.Author?.Trim().Length > 100)
        {
            return BadRequest(new { detail = "Author cannot exceed 100 characters." });
        }

        try
        {
            var message = await store.SendAsync(request.Text, request.Author, cancellationToken);
            return Created($"/api/messages/{message.Id}", message);
        }
        catch (AmazonDynamoDBException exception)
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new
            {
                detail = "The message database is unavailable.",
                error = exception.ErrorCode
            });
        }
    }
}
