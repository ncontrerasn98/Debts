using CreditScore.Api.Consumers;
using CreditScore.Api.Data;
using CreditScore.Api.Endpoints;
using CreditScore.Api.Messaging;
using Microsoft.EntityFrameworkCore;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHostedService<KafkaTopicInitializer>();
builder.Services.AddHostedService<DebtSettledConsumer>();

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console(
        theme: Serilog.Sinks.SystemConsole.Themes.AnsiConsoleTheme.Code)
    .CreateLogger();

builder.Host.UseSerilog();

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

builder.Services.AddDbContext<CreditScoreDbContext>(options =>
    options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString)));

var app = builder.Build();

app.MapCreditScoreEndpoints();

app.Run();