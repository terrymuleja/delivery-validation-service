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

// Railway provides DATABASE_URL environment variable
var databaseUrl = Environment.GetEnvironmentVariable("DATABASE_URL");
if (!string.IsNullOrEmpty(databaseUrl))
{
    // Railway PostgreSQL connection using DATABASE_URL
    connectionString = databaseUrl;
    Log.Information("Using Railway DATABASE_URL for connection");
}
else if (builder.Environment.IsProduction())
{
    // Fallback: Individual PostgreSQL environment variables (if manually set)
    var pgHost = Environment.GetEnvironmentVariable("PGHOST");
    var pgDatabase = Environment.GetEnvironmentVariable("PGDATABASE");
    var pgUser = Environment.GetEnvironmentVariable("PGUSER");
    var pgPassword = Environment.GetEnvironmentVariable("PGPASSWORD");
    var pgPort = Environment.GetEnvironmentVariable("PGPORT") ?? "5432";

    if (!string.IsNullOrEmpty(pgHost))
    {
        connectionString = $"Host={pgHost};Database={pgDatabase};Username={pgUser};Password={pgPassword};Port={pgPort};SSL Mode=Require;Trust Server Certificate=true";
        Log.Information("Using individual PostgreSQL environment variables");
    }
}

if (string.IsNullOrEmpty(connectionString))
{
    throw new InvalidOperationException("No database connection string found. Ensure DATABASE_URL environment variable is set or DefaultConnection is configured.");
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

// IMPROVED: Database initialization with proper migration support
using (var scope = app.Services.CreateScope())
{
    try
    {
        var context = scope.ServiceProvider.GetRequiredService<ValidationDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

        logger.LogInformation("Starting database initialization...");

        // Check if migrations are pending
        var pendingMigrations = await context.Database.GetPendingMigrationsAsync();

        if (pendingMigrations.Any())
        {
            logger.LogInformation("Applying {Count} pending migrations: {Migrations}",
                pendingMigrations.Count(), string.Join(", ", pendingMigrations));

            await context.Database.MigrateAsync();
            logger.LogInformation("Database migrations applied successfully");
        }
        else
        {
            logger.LogInformation("No pending migrations found");

            // Fallback: If no migrations exist, ensure database is created
            var created = await context.Database.EnsureCreatedAsync();
            if (created)
            {
                logger.LogInformation("Database created successfully (no migrations found)");
            }
            else
            {
                logger.LogInformation("Database already exists");
            }
        }

        // Optional: Verify database connection
        await context.Database.CanConnectAsync();
        logger.LogInformation("Database connection verified successfully");

        // Optional: Add any seeding logic here
        // await SeedDataAsync(context, logger);
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

// Optional: Add this method for seeding data
/*
static async Task SeedDataAsync(ValidationDbContext context, ILogger logger)
{
    try
    {
        // Add any initial data seeding logic here
        // Example:
        // if (!await context.SomeEntities.AnyAsync())
        // {
        //     context.SomeEntities.AddRange(GetInitialData());
        //     await context.SaveChangesAsync();
        //     logger.LogInformation("Initial data seeded successfully");
        // }
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Failed to seed initial data");
        // Don't throw here - seeding failure shouldn't stop the app
    }
}
*/