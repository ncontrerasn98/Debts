using System.Reflection;
using System.Text;
using Debts.API.Middlewares;
using Debts.Application.Abstractions.Auth;
using Debts.Application.Abstractions.CreditScore;
using Debts.Application.Abstractions.Email;
using Debts.Application.Abstractions.Messaging;
using Debts.Application.Abstractions.Persistence;
using Debts.Application.Abstractions.Webhooks;
using Debts.Application.Commands.Auth.Login;
using Debts.Application.Commands.Auth.RefreshToken;
using Debts.Application.Commands.CreateUser;
using Debts.Application.Commands.Debts.CreateDebt;
using Debts.Application.Commands.SettleDebt;
using Debts.Application.Commands.Users.AssignRole;
using Debts.Application.Commands.Users.RevokeRole;
using Debts.Application.Common.Behaviors;
using Debts.Application.Queries.GetDebtById;
using Debts.Application.Queries.GetDebts;
using Debts.Application.Validators;
using Debts.Infrastructure;
using Debts.Infrastructure.BackgroundJobs;
using Debts.Infrastructure.CreditScore;
using Debts.Infrastructure.Logging;
using Debts.Infrastructure.Persistence.Auth;
using Debts.Infrastructure.Persistence.Email;
using Debts.Infrastructure.Persistence.Messaging.Consumers;
using Debts.Infrastructure.Persistence.Messaging.Producer;
using Debts.Infrastructure.Persistence.Repositories;
using Debts.Infrastructure.Webhooks;
using FluentValidation;
using MassTransit;
using MediatR;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Polly;
using Serilog;
using Serilog.Enrichers.Span;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

builder.Services.AddSwaggerGen();

builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
builder.Services.AddValidatorsFromAssembly(typeof(CreateDebtCommandValidator).Assembly);

builder.Services.AddValidatorsFromAssembly(typeof(CreateDebtCommandValidator).Assembly);
builder.Services.AddValidatorsFromAssembly(typeof(CreateUserCommandValidator).Assembly);

builder.Services.AddScoped<IDebtRepository, DebtRepository>();
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IRoleRepository, RoleRepository>();
builder.Services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
builder.Services.AddScoped<IOutboxMessagesRepository, OutboxMessagesRepository>();
builder.Services.AddScoped<IWebhookRepository, WebhookRepository>();

builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();

builder.Services.AddScoped<CreateDebtHandler>();
builder.Services.AddScoped<GetDebtsHandler>();
builder.Services.AddScoped<CreateUserHandler>();
builder.Services.AddScoped<GetDebtByIdHandler>();
builder.Services.AddScoped<SettleDebtHandler>();
builder.Services.AddScoped<RefreshTokenHandler>();
builder.Services.AddScoped<LoginHandler>();
builder.Services.AddScoped<AssignRoleHandler>();
builder.Services.AddScoped<RevokeRoleHandler>();

builder.Services.AddSingleton<IEventProducer, KafkaProducer>();
builder.Services.AddScoped<IMessageBus, MessageBus>();
builder.Services.AddScoped<IEmailSender, EmailSender>();

builder.Services.AddHostedService<DebtSettledConsumer>();
builder.Services.AddHostedService<OutboxProcessorJob>();

builder.Services.AddScoped<ITokenProvider, JwtTokenGenerator>();
builder.Services.AddScoped<IPasswordHasher, PasswordHasher>();

builder.Services.AddHttpClient<IWebhookDispatcher, WebhookDispatcher>();
builder.Services.AddScoped<IWebhookDispatcher, WebhookDispatcher>();

builder.Services.AddHostedService<WebhookRetryWorker>();

builder.Services.AddMediatR(cfg =>
{
    cfg.RegisterServicesFromAssembly(
        Assembly.Load("Debts.Application"));
});

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseMySql(
        builder.Configuration.GetConnectionString("Default"),
        new MySqlServerVersion(new Version(8, 0, 0))
    ));

builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration =
        builder.Configuration["Redis:ConnectionString"];
});

builder.Services.AddMassTransit(x =>
{
    x.AddConsumer<SendDebtSettledEmailConsumer>();

    x.UsingRabbitMq((context, cfg) =>
    {
        cfg.Host(
            builder.Configuration["RabbitMQ:Host"],
            h =>
            {
                h.Username(builder.Configuration["RabbitMQ:Username"]);
                h.Password(builder.Configuration["RabbitMQ:Password"]);
            });
        
        cfg.UseInMemoryOutbox();
        cfg.ConfigureEndpoints(context);
    });
});

builder.Services
    .AddOpenTelemetry()
    .WithTracing(tracing =>
    {
        tracing
            .SetResourceBuilder(
                OpenTelemetry.Resources.ResourceBuilder.CreateDefault()
                    .AddService("DebtApi"))
            .AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation()
            .AddEntityFrameworkCoreInstrumentation()
            .AddSource("MassTransit")
            .AddSource("Debts.Outbox")
            .AddSource("MassTransit-Messages")
            .AddSource("OutboxDispatcher")
            .AddSource("KafkaProducer")
            .AddSource("KafkaConsumer.DebtSettled")
            .AddOtlpExporter(options =>
            {
                options.Endpoint = new Uri(builder.Configuration["OpenTelemetry:Endpoint"]!);
            });
    });

builder.Services
    .AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme =
            JwtBearerDefaults.AuthenticationScheme;

        options.DefaultChallengeScheme =
            JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters =
            new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,

                ValidIssuer =
                    builder.Configuration["Jwt:Issuer"],

                ValidAudience =
                    builder.Configuration["Jwt:Audience"],

                IssuerSigningKey =
                    new SymmetricSecurityKey(
                        Encoding.UTF8.GetBytes(
                            builder.Configuration["Jwt:Key"]!))
            };

        options.Events = new JwtBearerEvents
        {
            OnAuthenticationFailed = context =>
            {
                Console.WriteLine(
                    $"AUTH FAILED: {context.Exception}");

                return Task.CompletedTask;
            },

            OnTokenValidated = context =>
            {
                Console.WriteLine(
                    "TOKEN VALIDATED");

                return Task.CompletedTask;
            },

            OnChallenge = context =>
            {
                Console.WriteLine(
                    "AUTH CHALLENGE");

                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition(
        "Bearer",
        new Microsoft.OpenApi.Models.OpenApiSecurityScheme
        {
            Name = "Authorization",
            Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT",
            In = Microsoft.OpenApi.Models.ParameterLocation.Header,
            Description = "Enter JWT token"
        });

    options.AddSecurityRequirement(
        new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
        {
            {
                new Microsoft.OpenApi.Models.OpenApiSecurityScheme
                {
                    Reference =
                        new Microsoft.OpenApi.Models.OpenApiReference
                        {
                            Type =
                                Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                            Id = "Bearer"
                        }
                },
                Array.Empty<string>()
            }
        });
});

var loggerFactory = LoggerFactory.Create(b => b.AddSerilog());
var logger = loggerFactory.CreateLogger<CreditScoreService>();

var combinedPolicy = Policy.WrapAsync(
    CreditScoreResiliencePolicies.GetFallbackPolicy(logger),
    CreditScoreResiliencePolicies.GetCircuitBreakerPolicy(logger),
    CreditScoreResiliencePolicies.GetRetryPolicy(logger),
    CreditScoreResiliencePolicies.GetTimeoutPolicy());

builder.Services.AddHttpClient<ICreditScoreService, CreditScoreService>(client =>
    {
        client.BaseAddress = new Uri(builder.Configuration["CreditScore:BaseUrl"]
                                     ?? "http://localhost:5017");
    })
    .AddPolicyHandler(combinedPolicy);

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .Enrich.WithSpan()
    .Enrich.FromLogContext()
    .Enrich.WithProperty("Application", "DebtApi")
    .Enrich.With(new TraceIdEnricher())
    .WriteTo.Console(
        theme: Serilog.Sinks.SystemConsole.Themes.AnsiConsoleTheme.Code,
        outputTemplate: "[{Timestamp:HH:mm:ss} {Level}] TraceId={TraceId} {Message}{NewLine}{Exception}")
    .CreateLogger();

builder.Host.UseSerilog();

builder.Services.AddAuthorization();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.UseMiddleware<ExceptionMiddleware>();
app.UseMiddleware<CorrelationMiddleware>();

app.Run();