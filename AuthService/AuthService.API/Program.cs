using Microsoft.OpenApi;
using Shared.Contracts.Responses;
using Shared.Infrastructure.Extensions;
using Shared.Infrastructure.Middleware;

var builder = WebApplication.CreateBuilder(args);

builder.AddSerilogLogging();
builder.Services
    .AddTelemetry("AuthService")
    .AddControllers();

builder.Services.AddOpenApi();
builder.Services.AddSwaggerGen(c =>
{
    c.MapType<ApiResponse>(() => new OpenApiSchema { Type = """object""" });
});
var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.UseCorrelationId();
app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseMiddleware<CorrelationIdMiddleware>();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();


app.Run();


