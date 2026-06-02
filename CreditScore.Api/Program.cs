using CreditScore.Api.Consumers;
using CreditScore.Api.Data;
using CreditScore.Api.Endpoints;
using CreditScore.Api.Healthchecks;
using CreditScore.Api.Hubs;
using CreditScore.Api.Messaging;
using CreditScore.Api.Services;
using HealthChecks.UI.Client;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Prometheus;
using Serilog;
using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddGrpc();

builder.Services.AddHostedService<KafkaTopicInitializer>();
builder.Services.AddHostedService<DebtSettledConsumer>();

builder.Services.AddSingleton<IKafkaProducer, KafkaProducer>();

builder.Services.AddHealthChecks()
    .AddCheck("live", () => HealthCheckResult.Healthy(), tags: new[] { "live" })
    .AddMySql(
        builder.Configuration["ConnectionStrings:Default"]!,
        name: "mysql",
        tags: new[] { "ready" })
    .AddCheck<KafkaHealthCheck>(
        "kafka",
        tags: new[] { "ready" });

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console(
        theme: Serilog.Sinks.SystemConsole.Themes.AnsiConsoleTheme.Code)
    .CreateLogger();

builder.Host.UseSerilog();
builder.Services.AddSignalR()
    .AddStackExchangeRedis("redis:6379", options =>
    {
        options.Configuration.ChannelPrefix = 
            RedisChannel.Literal("creditscore"); // prefijo para no mezclar con otros datos
    });

builder.Services.AddDbContext<CreditScoreDbContext>(options =>
    options.UseMySql(
        builder.Configuration.GetConnectionString("Default"),
        new MySqlServerVersion(new Version(8, 0, 0))
    ));

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy
            .SetIsOriginAllowed(_ => true) // permite cualquier origen en desarrollo
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

var app = builder.Build();
app.UseRouting();
app.UseCors();
app.UseHttpMetrics(); 
app.UseStaticFiles();
app.MapCreditScoreEndpoints();
app.MapGrpcService<CreditScoreGrpcService>();
app.MapHub<CreditScoreHub>("/hubs/creditscore");

app.MapHealthChecks("/health/live", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("live"),
    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
});

app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready"),
    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
});

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<CreditScoreDbContext>();
    db.Database.Migrate();
}
app.MapMetrics();
app.Run();