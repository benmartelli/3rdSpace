# Working on it

## Messages API deployment

The API is ready to run in AWS Lambda with API Gateway HTTP API.

### API Gateway routes

Create routes that proxy all supported requests to the Lambda function:

| Method | API Gateway route | Handler |
|---|---|---|
| `GET` | `/api/messages` | `MessagesController.GetAll` |
| `POST` | `/api/messages` | `MessagesController.Send` |
| `ANY` | `/{proxy+}` | ASP.NET Core routing fallback |

For API Gateway payload format 2.0, configure the HTTP API integration as Lambda proxy/integration. Route `/api/messages` directly and add `/{proxy+}` for health checks or future controller routes. Do not configure separate Lambda functions for each endpoint.

Query parameters are read from `page` and `pageSize`:

- `GET /api/messages?page=1&pageSize=10`
- `POST /api/messages` with `{ "text": "Hello", "author": "Optional name" }`

Set the Lambda handler according to the generated executable package. For ASP.NET Core minimal Lambda deployment, use `MessageApi` as the executable assembly or let the Lambda package tooling configure the handler.

A custom domain can map the API Gateway stage to `api.3rdspace.nz`. Deploy the static site to S3/CloudFront and update `API_BASE_URL` to the custom API URL.

The current `LocalMessageStore` writes to a local JSON file and is suitable only for local development or an ephemeral test deployment. Lambda storage is not persistent. Before production use, replace `IMessageStore` with a DynamoDB implementation.

## DynamoDB Lambda permissions

The Lambda execution role needs table permissions for the configured table ARN:

- `dynamodb:Scan` — retrieve paginated message items
- `dynamodb:PutItem` — create a message

Example resource policy:

```json
{
  "Effect": "Allow",
  "Action": ["dynamodb:Scan", "dynamodb:PutItem"],
  "Resource": "arn:aws:dynamodb:ap-southeast-2:189324273358:table/messages"
}
```

Configure these optional Lambda environment variables:

- `DYNAMODB_TABLE_ARN=arn:aws:dynamodb:ap-southeast-2:189324273358:table/messages`
- `DYNAMODB_TABLE_NAME=messages`

The table partition key must be a String named `id`. Message items use `id`, `text`, `createdAt`, and `author` attributes.
