using MassTransit;
using Microsoft.EntityFrameworkCore;
using Serilog;
using DotNetEnv;
using DeliveryValidationService.Api.Data;
using DeliveryValidationService.Api.Services;
using DeliveryValidationService.Api.Services.Implementations;
using DeliveryValidationService.Api.Consumers;

// Load .env file in development
if (Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Development")
{
    Env.Load();
}

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .CreateLogger();

builder.Host.UseSerilog();

// Add services
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Database - Handle both Railway and local
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
if (builder.Environment.IsProduction())
{
    // Railway PostgreSQL connection
    var pgHost = Environment.GetEnvironmentVariable("PGHOST");
    var pgDatabase = Environment.GetEnvironmentVariable("PGDATABASE");
    var pgUser = Environment.GetEnvironmentVariable("PGUSER");
    var pgPassword = Environment.GetEnvironmentVariable("PGPASSWORD");
    var pgPort = Environment.GetEnvironmentVariable("PGPORT") ?? "5432";

    if (!string.IsNullOrEmpty(pgHost))
    {
        connectionString = $"Host={pgHost};Database={pgDatabase};Username={pgUser};Password={pgPassword};Port={pgPort};SSL Mode=Require;Trust Server Certificate=true";
    }
}

builder.Services.AddDbContext<ValidationDbContext>(options =>
    options.UseNpgsql(connectionString));

// AI Services
builder.Services.AddScoped<IAiValidationService, AiValidationService>();
builder.Services.AddScoped<IBodyPartDetectionService, BodyPartDetectionService>();
builder.Services.AddScoped<IOcrService, TesseractOcrService>();
builder.Services.AddScoped<ITextSimilarityService, TextSimilarityService>();
builder.Services.AddScoped<ICloudinaryService, CloudinaryService>();

// HTTP Client for downloading images
builder.Services.AddHttpClient();

// Get RabbitMQ URL - CloudAMQP
var rabbitMqUrl = builder.Configuration.GetConnectionString("RabbitMQ")
                 ?? Environment.GetEnvironmentVariable("CLOUDAMQP_URL");

if (string.IsNullOrEmpty(rabbitMqUrl))
{
    throw new InvalidOperationException("CloudAMQP URL not configured. Set CLOUDAMQP_URL environment variable or RabbitMQ connection string.");
}

// MassTransit + CloudAMQP
builder.Services.AddMassTransit(x =>
{
    // Add consumers
    x.AddConsumer<DeliveryRequestConsumer>();
    x.AddConsumer<ValidationFeedbackConsumer>();

    x.UsingRabbitMq((context, cfg) =>
    {
        Log.Information("Connecting to RabbitMQ: {Host}", new Uri(rabbitMqUrl).Host);

        // CloudAMQP connection
        cfg.Host(new Uri(rabbitMqUrl));

        // CloudAMQP free tier optimizations
        cfg.PrefetchCount = 1;  // Limit for free tier
        cfg.UseMessageRetry(r => r.Intervals(1000, 2000, 5000)); // Conservative retries

        // Configure delivery validation queue
        cfg.ReceiveEndpoint("delivery-validation-queue", e =>
        {
            e.ConfigureConsumer<DeliveryRequestConsumer>(context);
            e.ConcurrentMessageLimit = 1; // Free tier limitation

            // Enable message retry
            e.UseMessageRetry(r => r.Intervals(1000, 2000, 5000));
        });

        // Configure feedback queue
        cfg.ReceiveEndpoint("validation-feedback-queue", e =>
        {
            e.ConfigureConsumer<ValidationFeedbackConsumer>(context);
            e.ConcurrentMessageLimit = 1;

            // Enable message retry for feedback queue too
            e.UseMessageRetry(r => r.Intervals(1000, 2000, 5000));
        });

        cfg.ConfigureEndpoints(context);
    });
});

// Health checks
builder.Services.AddHealthChecks()
    .AddDbContextCheck<ValidationDbContext>()
    .AddRabbitMQ(options =>
    {
        options.ConnectionUri = new Uri(rabbitMqUrl);
    });

var app = builder.Build();

// Configure pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

// Health check endpoint
app.MapHealthChecks("/health");

// Ensure database is created and migrated
using (var scope = app.Services.CreateScope())
{
    try
    {
        var context = scope.ServiceProvider.GetRequiredService<ValidationDbContext>();
        await context.Database.EnsureCreatedAsync();
        Log.Information("Database initialized successfully");
    }
    catch (Exception ex)
    {
        Log.Error(ex, "Failed to initialize database");
        throw;
    }
}

Log.Information("Delivery Validation Service starting...");

app.Run();

// Ensure proper cleanup
Log.CloseAndFlush();