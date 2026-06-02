using System.Reflection;
using System.Security.Claims;
using System.Text;
using System.Threading.RateLimiting;
using Debts.API.Filters;
using Debts.API.Middlewares;
using Debts.Application.Abstractions.Audit;
using Debts.Application.Abstractions.Auth;
using Debts.Application.Abstractions.CreditScore;
using Debts.Application.Abstractions.Email;
using Debts.Application.Abstractions.Idempotency;
using Debts.Application.Abstractions.Messaging;
using Debts.Application.Abstractions.Persistence;
using Debts.Application.Abstractions.Webhooks;
using Debts.Application.Commands.Auth.Login;
using Debts.Application.Commands.Auth.RefreshToken;
using Debts.Application.Commands.CreateUser;
using Debts.Application.Commands.Debts.CreateDebt;
using Debts.Application.Commands.Debts.SettleDebt;
using Debts.Application.Commands.SettleDebt;
using Debts.Application.Commands.Users.AssignRole;
using Debts.Application.Commands.Users.RevokeRole;
using Debts.Application.Common.Behaviors;
using Debts.Application.Queries.GetDebtById;
using Debts.Application.Queries.GetDebts;
using Debts.Application.Sagas;
using Debts.Application.Sagas.CreateDebt;
using Debts.Application.Sagas.CreateDebt.Messages;
using Debts.Application.Validators;
using Debts.Infrastructure;
using Debts.Infrastructure.BackgroundJobs;
using Debts.Infrastructure.CreditScore;
using Debts.Infrastructure.HealthChecks;
using Debts.Infrastructure.Logging;
using Debts.Infrastructure.Persistence.Audit;
using Debts.Infrastructure.Persistence.Auth;
using Debts.Infrastructure.Persistence.Email;
using Debts.Infrastructure.Persistence.Idempotency;
using Debts.Infrastructure.Persistence.Messaging;
using Debts.Infrastructure.Persistence.Messaging.Consumers;
using Debts.Infrastructure.Persistence.Messaging.Producer;
using Debts.Infrastructure.Persistence.Repositories;
using Debts.Infrastructure.Webhooks;
using FluentValidation;
using HealthChecks.UI.Client;
using MassTransit;
using MediatR;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.IdentityModel.Tokens;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Polly;
using Prometheus;
using RabbitMQ.Client;
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

builder.Services.AddScoped<KafkaHealthCheck>();

builder.Services.AddHostedService<KafkaTopicInitializer>();
builder.Services.AddHostedService<DebtSettledConsumer>();
builder.Services.AddHostedService<OutboxProcessorJob>();
builder.Services.AddHostedService<DebtSettlementReversedConsumer>();

builder.Services.AddScoped<ITokenProvider, JwtTokenGenerator>();
builder.Services.AddScoped<IPasswordHasher, PasswordHasher>();
builder.Services.AddScoped<IIdempotencyService, IdempotencyService>();
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();
builder.Services.AddScoped<IAuditService, AuditService>();
builder.Services.AddScoped<ITokenBlacklistService, TokenBlacklistService>();

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

    x.AddSagaStateMachine<DebtCreationSaga, DebtCreationSagaState>()
        .EntityFrameworkRepository(r =>
        {
            r.ConcurrencyMode = ConcurrencyMode.Optimistic;
            r.AddDbContext<DbContext, AppDbContext>((provider, options) =>
            {
                options.UseMySql(
                    builder.Configuration.GetConnectionString("Default"),
                    new MySqlServerVersion(new Version(8, 0, 0)));
            });
        });

    // Registrar Activities
    x.AddActivities(typeof(CreateDebtActivity).Assembly);

    x.AddRequestClient<CreateDebtRequested>();

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

builder.Services.AddGrpcClient<Shared.Contracts.CreditScoreService.CreditScoreServiceClient>(options =>
    {
        options.Address = new Uri(builder.Configuration["CreditScore:GrpcUrl"] ?? "http://localhost:5018");
    })
    .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
    {
        // Permitir HTTP sin TLS para desarrollo
        ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
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
    
    options.OperationFilter<IdempotencyKeyOperationFilter>();
});

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = 429;

    options.AddPolicy("mixed-per-user", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier)
                          ?? httpContext.Connection.RemoteIpAddress?.ToString()
                          ?? "anonymous",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                Window = TimeSpan.FromSeconds(10),
                PermitLimit = 3,
                QueueLimit = 2,
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst
            }));
});

builder.Services.AddHealthChecks()
    // Liveness — solo verifica que la app está viva
    .AddCheck("live", () => HealthCheckResult.Healthy(), tags: new[] { "live" })
    
    // Readiness — verifica dependencias
    .AddMySql(
        builder.Configuration["ConnectionStrings:Default"]!,
        name: "mysql",
        tags: new[] { "ready" })
    .AddRedis(
        builder.Configuration["Redis:ConnectionString"]!,
        name: "redis",
        tags: new[] { "ready" })
    .AddRabbitMQ(
        sp =>
        {
            var factory = new ConnectionFactory
            {
                HostName = builder.Configuration["RabbitMQ:Host"],
                UserName = builder.Configuration["RabbitMQ:Username"],
                Password = builder.Configuration["RabbitMQ:Password"]
            };
            return factory.CreateConnectionAsync();
        },
        name: "rabbitmq",
        tags: new[] { "ready" })
    .AddCheck<KafkaHealthCheck>(
        "kafka",
        tags: new[] { "ready" });

var loggerFactory = LoggerFactory.Create(b => b.AddSerilog());
var logger = loggerFactory.CreateLogger<CreditScoreGrpcClient>(); 
var combinedPolicy = Policy.WrapAsync(
    CreditScoreResiliencePolicies.GetFallbackPolicy(logger),
    CreditScoreResiliencePolicies.GetCircuitBreakerPolicy(logger),
    CreditScoreResiliencePolicies.GetRetryPolicy(logger),
    CreditScoreResiliencePolicies.GetTimeoutPolicy());


builder.Services.AddGrpcClient<Shared.Contracts.CreditScoreService.CreditScoreServiceClient>(options =>
    {
        options.Address = new Uri(builder.Configuration["CreditScore:GrpcUrl"] ?? "http://localhost:5018");
    })
    .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
    {
        ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
    })
    .AddPolicyHandler(combinedPolicy);

builder.Services.AddScoped<ICreditScoreService, CreditScoreGrpcClient>();

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
builder.Services.AddHttpContextAccessor();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseRouting();
app.UseHttpMetrics();

app.UseMiddleware<ExceptionMiddleware>();        // 1. atrapa todo
app.UseMiddleware<CorrelationMiddleware>();      // 2. agrega correlationId
app.UseMiddleware<TokenBlacklistMiddleware>();   // 3. verifica blacklist

app.UseHttpsRedirection();
app.UseAuthentication();                         // 4. valida el JWT
app.UseAuthorization();                          // 5. verifica permisos

app.UseRateLimiter();                            // 6. limita requests por usuario autenticado
app.UseMiddleware<IdempotencyMiddleware>();       // 7. idempotencia para usuarios autenticados

app.MapControllers();

// Health checks y métricas
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

app.MapMetrics();

app.Run();