// Arc Demo Workspace Seeder — manual force-reseed script.
//
// Usage (from repo root):
//   dotnet run --project backend/scripts/seed-demo.csproj
//   make seed-demo
//
// Demo login: demo@arc.com / DemoArc2026!

using Arc.Application.Execution;
using Arc.Application.Identity;
using Arc.Application.LLM;
using Arc.Application.Notifications;
using Arc.Application.Persistence;
using Arc.Application.RegressionGates;
using Arc.Application.Telemetry;
using Arc.Application.Webhooks;
using Arc.Application.Workflows;
using Arc.Infrastructure.Demo;
using Arc.Infrastructure.Identity;
using Arc.Infrastructure.LLM;
using Arc.Infrastructure.Notifications;
using Arc.Infrastructure.Persistence;
using Arc.Infrastructure.RegressionGates;
using Arc.Infrastructure.Webhooks;
using Arc.Infrastructure.Workflows;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

static string FindRepoRoot()
{
    var dir = AppContext.BaseDirectory;
    while (dir is not null)
    {
        if (Directory.Exists(Path.Combine(dir, "backend", "src", "Arc.Api")))
            return dir;
        dir = Directory.GetParent(dir)?.FullName;
    }
    throw new InvalidOperationException("Could not find Arc repo root. Run from the Arc repository.");
}

var repoRoot = FindRepoRoot();
var envFile = Path.Combine(repoRoot, ".env");
if (File.Exists(envFile))
{
    foreach (var line in File.ReadAllLines(envFile))
    {
        if (string.IsNullOrWhiteSpace(line) || line.StartsWith('#')) continue;
        var parts = line.Split('=', 2);
        if (parts.Length == 2)
            Environment.SetEnvironmentVariable(parts[0].Trim(), parts[1].Trim());
    }
}

var configuration = new ConfigurationBuilder()
    .SetBasePath(Path.Combine(repoRoot, "backend", "src", "Arc.Api"))
    .AddJsonFile("appsettings.json", optional: true)
    .AddJsonFile("appsettings.Development.json", optional: true)
    .AddEnvironmentVariables()
    .Build();

var connectionString = configuration.GetConnectionString("PostgreSQL")
    ?? throw new InvalidOperationException("ConnectionStrings:PostgreSQL is required. Check your .env file.");

var services = new ServiceCollection();
services.AddLogging(b => b.AddConsole().SetMinimumLevel(LogLevel.Information));
services.AddSingleton<IConfiguration>(configuration);
services.AddSingleton<IDatabaseContext>(sp =>
{
    var logger = sp.GetRequiredService<ILogger<PostgresDatabaseContext>>();
    return new PostgresDatabaseContext(connectionString, logger);
});
services.AddScoped<IUserRepository, PostgresUserRepository>();
services.AddScoped<IPasswordHashingService, BCryptPasswordHashingService>();
services.AddScoped<IUserPreferencesRepository, PostgresUserPreferencesRepository>();
services.AddScoped<ILLMConfigurationRepository, PostgresLLMConfigurationRepository>();
services.AddScoped<IWorkflowRepository, PostgresWorkflowRepository>();
services.AddScoped<IExecutionTemplateStore, PostgresExecutionTemplateStore>();
services.AddScoped<IExecutionResultStore, PostgresExecutionResultStore>();
services.AddScoped<IAuditLogger, PostgresAuditLogger>();
services.AddScoped<IGoldenExecutionStore, PostgresGoldenExecutionStore>();
services.AddScoped<IRegressionGateRepository, PostgresRegressionGateRepository>();
services.AddScoped<IWebhookRepository, PostgresWebhookRepository>();
services.AddScoped<INotificationRepository, PostgresNotificationRepository>();
services.AddScoped<DemoWorkspaceSeeder>();

await using var provider = services.BuildServiceProvider();

Console.WriteLine("── Arc Demo Workspace Seeder ──");
var db = provider.GetRequiredService<IDatabaseContext>();
await db.InitializeAsync();

using var scope = provider.CreateScope();
var seeder = scope.ServiceProvider.GetRequiredService<DemoWorkspaceSeeder>();
await seeder.ReseedAsync();
Console.WriteLine($"✔ Demo workspace ready. Login: {DemoWorkspaceSeeder.DemoEmail} / DemoArc2026!");
