using Microsoft.EntityFrameworkCore;
using Notification.Service.Data;
using Notification.Service.Hubs;
using Prometheus;
using Serilog;
using Notification.Service.Consumers;
using Notification.Service.Endpoints;
using Notification.Service.Messaging;
using Notification.Service.Services;
using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHostedService<KafkaTopicInitializer>();

builder.Services.AddScoped<IEmailSender, EmailSender>();
builder.Services.AddHostedService<DebtSettledConsumer>();
builder.Services.AddHostedService<CreditScoreUpdatedConsumer>();

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console(
        theme: Serilog.Sinks.SystemConsole.Themes.AnsiConsoleTheme.Code)
    .CreateLogger();

builder.Host.UseSerilog();

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

builder.Services.AddDbContext<NotificationDbContext>(options =>
    options.UseMySql(connectionString, new MySqlServerVersion(new Version(8, 0, 0))));

builder.Services.AddSignalR()
    .AddStackExchangeRedis(
        builder.Configuration["Redis:ConnectionString"]!,
        options =>
        {
            options.Configuration.ChannelPrefix =
                RedisChannel.Literal("notifications");
        });

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy
            .SetIsOriginAllowed(_ => true)
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy
            .WithOrigins("http://localhost:5236") // gateway
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<NotificationDbContext>();
    db.Database.Migrate();
}

app.UseSwagger();
app.UseSwaggerUI();

app.UseRouting();        // primero
app.UseHttpMetrics();
app.UseStaticFiles();
app.UseCors();           // después de routing

app.MapNotificationEndpoints();
app.MapHub<NotificationHub>("/hubs/notifications");
app.MapMetrics();
Console.WriteLine("Hub registered at /hubs/notifications");
app.Run();