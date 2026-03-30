using Serilog;
using System.Text;
using FluentValidation;
using Arc.Api.Extensions;
using Arc.Api.Middleware;
using Microsoft.AspNetCore.Mvc;
using FluentValidation.AspNetCore;
using System.Threading.RateLimiting;
using Microsoft.IdentityModel.Tokens;
using Microsoft.AspNetCore.Authentication.JwtBearer;


var builder = WebApplication.CreateBuilder(args);

// Logging (Serilog)
builder.Host.UseSerilog((context, services, configuration) =>
{
    configuration
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext()
        .WriteTo.Console();
});

// Configure Kestrel to listen on all network interfaces
builder.WebHost.ConfigureKestrel(options =>
{
    options.ListenAnyIP(5266);
});

// Add Services
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
    })
    .ConfigureApiBehaviorOptions(options =>
    {
        // Return validation errors in a consistent format
        options.InvalidModelStateResponseFactory = context =>
        {
            var errors = context.ModelState
                .Where(e => e.Value?.Errors.Count > 0)
                .Select(e => new { Field = e.Key, Errors = e.Value!.Errors.Select(x => x.ErrorMessage) });

            return new BadRequestObjectResult(new { Message = "Validation failed", Errors = errors });
        };
    });

// FluentValidation integration
builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddValidatorsFromAssemblyContaining<Program>();

// JWT Authentication
var jwtSecretKey = builder.Configuration["Jwt:SecretKey"] ?? "your-super-secret-jwt-key-that-is-at-least-32-characters-long";
var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "Arc";
var jwtAudience = builder.Configuration["Jwt:Audience"] ?? "Arc";

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecretKey)),
            ValidateIssuer = true,
            ValidIssuer = jwtIssuer,
            ValidateAudience = true,
            ValidAudience = jwtAudience,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromMinutes(2)
        };
        
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var token = context.Request.Cookies["auth_token"];
                if (!string.IsNullOrEmpty(token))
                {
                    context.Token = token;
                }
                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorization();

// CORS
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins("http://localhost:5173", "http://localhost:5266")
              .AllowCredentials()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Rate limiting 
builder.Services.AddMemoryCache();
var rateLimitPermitLimit = builder.Configuration.GetValue<int?>("RateLimiting:PermitLimit") ?? 50;
var rateLimitWindowSeconds = builder.Configuration.GetValue<int?>("RateLimiting:WindowSeconds") ?? 60;

builder.Services.AddRateLimiter(options =>
{
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
    {
        // Partition per IP so each client gets their own independent limit
        var partitionKey = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        return RateLimitPartition.GetFixedWindowLimiter<string>(
            partitionKey: partitionKey,
            factory: key => new FixedWindowRateLimiterOptions
            {
                PermitLimit = rateLimitPermitLimit,
                Window = TimeSpan.FromSeconds(rateLimitWindowSeconds),
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 0
            });
    });

    options.RejectionStatusCode = 429;
});

// Application & Infrastructure DI
builder.Services.AddApplicationServices();   // Extension in Arc.Application
builder.Services.AddInfrastructureServices(builder.Configuration); // Extension in Arc.Infrastructure

var app = builder.Build();

// Initialize database schema
await app.Services.InitializeDatabaseAsync();

// Middleware pipeline
app.UseSerilogRequestLogging();
app.UseGlobalExceptionMiddleware();

app.UseHttpsRedirection();

app.UseCors();

// Rate limiting middleware
app.UseRateLimiter();

// Maintenance mode: returns 503 for non-admin paths when enabled
app.UseMaintenanceModeMiddleware();

// Authentication & Authorization
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// Swagger
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Serve React SPA (production)
app.UseDefaultFiles();
app.UseStaticFiles();
app.MapFallbackToFile("index.html");

app.Run();
public partial class Program { }