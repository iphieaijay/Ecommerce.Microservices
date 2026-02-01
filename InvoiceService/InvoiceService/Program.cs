using FluentValidation;
using InvoiceService.Features.GetCustomerInvoices;
using InvoiceService.Features.GetInvoice;
using InvoiceService.Features.ProcessPayment;
using InvoiceService.Infrastructure.BackgroundServices;
using InvoiceService.Infrastructure.Persistence;
using InvoiceService.Infrastructure.Persistence.Repositories;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using HealthChecks.RabbitMQ;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.File("logs/invoice-service-.txt", rollingInterval: RollingInterval.Day)
    .CreateLogger();

builder.Host.UseSerilog();

// Add services to the container
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new()
    {
        Title = "Invoice Service API",
        Version = "v1",
        Description = "Microservice for managing customer invoices in an e-commerce platform"
    });
});
builder.Services.AddControllers();
// Database
builder.Services.AddDbContext<InvoiceDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        sqlOptions =>
        {
            sqlOptions.EnableRetryOnFailure(
                maxRetryCount: 5,
                maxRetryDelay: TimeSpan.FromSeconds(30),
                errorNumbersToAdd: null);
        }));


// Repositories
builder.Services.AddScoped<IInvoiceRepository, InvoiceRepository>();

// MediatR
builder.Services.AddMediatR(cfg =>
    cfg.RegisterServicesFromAssemblyContaining<Program>());

// FluentValidation
builder.Services.AddValidatorsFromAssemblyContaining<Program>();

// MassTransit with RabbitMQ
builder.Services.AddMassTransit(x =>
{
    // Add consumers
    x.AddConsumer<PaymentConfirmedConsumer>();

    x.UsingRabbitMq((context, cfg) =>
    {
        var rabbitMqConfig = builder.Configuration.GetSection("RabbitMQ");

        cfg.Host(rabbitMqConfig["Host"], rabbitMqConfig["VirtualHost"], h =>
        {
            h.Username(rabbitMqConfig["Username"]!);
            h.Password(rabbitMqConfig["Password"]!);
        });

        // Configure retry policy
        cfg.UseMessageRetry(r => r.Intervals(
            TimeSpan.FromSeconds(5),
            TimeSpan.FromSeconds(15),
            TimeSpan.FromSeconds(30)));

        // Configure error handling
        cfg.UseInMemoryOutbox();

        // Configure consumers
        cfg.ReceiveEndpoint("invoice-payment-confirmed", e =>
        {
            e.ConfigureConsumer<PaymentConfirmedConsumer>(context);

            // Configure prefetch and concurrency
            e.PrefetchCount = 16;
            e.UseMessageRetry(r => r.Intervals(
                TimeSpan.FromSeconds(5),
                TimeSpan.FromSeconds(15),
                TimeSpan.FromSeconds(30)));
        });

        cfg.ConfigureEndpoints(context);
    });
});

// Background Services
builder.Services.AddHostedService<RetryFailedInvoicesService>();

// CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});



var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseSerilogRequestLogging();

app.UseCors("AllowAll");

app.MapControllers();

app.MapHealthChecks("/health");

// Root endpoint
//app.MapGet("/", () => new
//{
//    Service = "Invoice Service",
//    Version = "1.0.0",
//    Status = "Running",
//    Timestamp = DateTime.UtcNow
//})
//.WithName("ServiceInfo")
//.WithTags("Service");

// Database migration on startup (for development only)
if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<InvoiceDbContext>();
    try
    {
        await db.Database.MigrateAsync();
        Log.Information("Database migration completed successfully");
    }
    catch (Exception ex)
    {
        Log.Error(ex, "Error during database migration");
    }
}

try
{
    Log.Information("Starting Invoice Service");
    await app.RunAsync();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Invoice Service terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}


