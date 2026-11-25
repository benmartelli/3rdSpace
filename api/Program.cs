using Amazon.Lambda.AspNetCoreServer.Hosting;
using MessageApi.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("AWS_LAMBDA_FUNCTION_NAME")))
{
    builder.Services.AddSingleton<IMessageStore, DynamoDbMessageStore>();
}
else
{
    builder.Services.AddSingleton<IMessageStore, LocalMessageStore>();
}

builder.Services.AddCors(options =>
{
    options.AddPolicy("MessagesSite", policy => policy
        .WithOrigins("http://localhost:3000", "http://localhost:5242", "http://localhost:8000", "http://localhost:8080", "http://127.0.0.1:5500", "http://127.0.0.1:8000", "https://3rdspace.surf", "https://www.3rdspace.surf", "https://3rdspace.nz", "https://www.3rdspace.nz")
        .AllowAnyMethod()
        .AllowAnyHeader());
});
builder.Services.AddAWSLambdaHosting(LambdaEventSource.HttpApi);

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("MessagesSite");

app.MapControllers();

app.Run();
