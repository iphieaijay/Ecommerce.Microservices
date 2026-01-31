
// NotificationService.AppHost/Program.cs
var builder = DistributedApplication.CreateBuilder(args);

builder.AddProject<Projects.NotificationService>("notificationservice");

// Infrastructure
var rabbitmq = builder.AddRabbitMQ("messaging")
    .WithManagementPlugin()
    .WithDataVolume();

var redis = builder.AddRedis("cache")
    .WithDataVolume();

var postgres = builder.AddPostgres("postgres")
    .WithDataVolume()
    .AddDatabase("notificationdb");

var seq = builder.AddSeq("seq");

// Notification Service
var notificationService = builder.AddProject<Projects.NotificationService_Api>("notification-api")
    .WithReference(rabbitmq)
    .WithReference(redis)
    .WithReference(postgres)
    .WithReference(seq);

builder.Build().Run();
