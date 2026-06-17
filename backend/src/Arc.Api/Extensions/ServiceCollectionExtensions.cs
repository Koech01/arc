using Arc.Api.Filters;
using Arc.Application.LLM;
using Arc.Application.Admin;
using Arc.Infrastructure.LLM;
using Arc.Infrastructure.Admin;
using Arc.Application.Planning;
using Arc.Application.Identity;
using Arc.Application.Webhooks;
using Arc.Application.Execution;
using Arc.Application.Telemetry;
using Arc.Application.Workflows;
using Arc.Infrastructure.Webhooks;
using Arc.Application.Persistence;
using Arc.Infrastructure.Identity;
using Arc.Infrastructure.Demo;
using Arc.Infrastructure.Execution;
using Arc.Infrastructure.Workflows;
using Arc.Application.Orchestration;
using Arc.Application.Notifications;
using Arc.Infrastructure.Persistence;
using Arc.Application.RegressionGates;
using Arc.Infrastructure.Notifications;
using Arc.Infrastructure.RegressionGates;


namespace Arc.Api.Extensions
{
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Registers Application layer services.
        /// </summary>
        public static IServiceCollection AddApplicationServices(this IServiceCollection services)
        {
            // Authentication services
            services.AddScoped<IAuthenticationService, DeterministicAuthenticationService>();

            // Planner
            services.AddScoped<IPlanner, DeterministicPlannerV1>();

            // Dynamic LLM provider service
            services.AddScoped<ILLMProviderService, DynamicLLMProviderService>();

            // Agent executor (now uses dynamic LLM provider service)
            services.AddScoped<IAgentExecutor, DeterministicAgentExecutorV1>();

            // Execution engine
            services.AddScoped<IExecutionEngine, DeterministicExecutionEngineV1>(sp =>
            {
                var executor = sp.GetRequiredService<IAgentExecutor>();
                var auditLogger = sp.GetRequiredService<IAuditLogger>();
                var cache = sp.GetRequiredService<ITaskExecutionCache>();
                var userContext = sp.GetRequiredService<IUserContext>();
                var webhookDispatcher = sp.GetRequiredService<IWebhookDispatcher>();
                var notificationService = sp.GetRequiredService<INotificationService>();
                return new DeterministicExecutionEngineV1(executor, auditLogger, cache, userContext, webhookDispatcher, notificationService);
            });

            // Orchestrator
            services.AddScoped<IOrchestrator, DeterministicOrchestratorV1>();

            // Deterministic execution replayer
            services.AddScoped<IExecutionReplayer, DeterministicExecutionReplayer>();

            // Deterministic batch executor
            services.AddScoped<IBatchExecutor, DeterministicBatchExecutorV1>();

            // Deterministic execution comparer
            services.AddScoped<IExecutionComparer, DeterministicExecutionComparer>();

            // Deterministic execution transformer
            services.AddScoped<IExecutionTransformer, DeterministicExecutionTransformer>();

            // Deterministic execution profiler
            services.AddScoped<IExecutionProfiler, DeterministicExecutionProfiler>();

            // Deterministic execution visualizer
            services.AddScoped<IExecutionVisualizer, DeterministicExecutionVisualizer>();

            // Workflow executor
            services.AddScoped<IWorkflowExecutor, DeterministicWorkflowExecutor>();

            // Deterministic execution exporter (for export/import feature)
            services.AddScoped<IExecutionExporter, DeterministicExecutionExporter>();

            // Deterministic execution importer (for export/import feature)
            services.AddScoped<IExecutionImporter, DeterministicExecutionImporter>();

            // Admin stats service
            services.AddScoped<IAdminStatsService, PostgresAdminStatsService>();

            // Admin user lifecycle management service
            services.AddScoped<IAdminUserService, AdminUserService>();

            // Maintenance mode (in-process singleton - resets on restart by design)
            services.AddSingleton<IMaintenanceModeService, InMemoryMaintenanceModeService>();

            // System configuration snapshot service
            services.AddScoped<ISystemConfigurationService, SystemConfigurationService>();

            // AdminActionLoggingFilter registered for ServiceFilter resolution
            services.AddScoped<AdminActionLoggingFilter>();

            // Regression gate service
            services.AddScoped<IRegressionGateService, DeterministicRegressionGateService>();

            return services;
        }

        /// <summary>
        /// Registers Infrastructure layer services.
        /// </summary>
        public static IServiceCollection AddInfrastructureServices(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            var connectionString = configuration.GetConnectionString("PostgreSQL");

            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new InvalidOperationException("ConnectionStrings:PostgreSQL is required.");
            }

            services.AddSingleton<IDatabaseContext>(sp =>
            {
                var logger = sp.GetRequiredService<ILogger<PostgresDatabaseContext>>();
                return new PostgresDatabaseContext(connectionString, logger);
            });

            services.AddScoped<IUserRepository, PostgresUserRepository>();
            services.AddScoped<IPasswordResetRepository, PostgresPasswordResetRepository>();
            services.AddScoped<IExecutionResultStore, PostgresExecutionResultStore>();
            services.AddScoped<IAuditLogger, PostgresAuditLogger>();
            services.AddScoped<ITaskExecutionCache, PostgresTaskExecutionCache>();
            services.AddScoped<IExecutionTemplateStore, PostgresExecutionTemplateStore>();
            services.AddScoped<IWorkflowRepository, PostgresWorkflowRepository>();
            services.AddScoped<IWebhookRepository, PostgresWebhookRepository>();
            services.AddScoped<INotificationRepository, PostgresNotificationRepository>();
            services.AddScoped<IUserPreferencesRepository, PostgresUserPreferencesRepository>();
            services.AddScoped<ILLMConfigurationRepository, PostgresLLMConfigurationRepository>();
            services.AddScoped<IAdminAuditLogger, PostgresAdminAuditLogger>();
            services.AddScoped<ILoginHistoryRepository, PostgresLoginHistoryRepository>();
            services.AddScoped<IRegressionGateRepository, PostgresRegressionGateRepository>();
            services.AddScoped<IGoldenExecutionStore, PostgresGoldenExecutionStore>();

            // Authentication infrastructure
            services.AddScoped<IPasswordHashingService, BCryptPasswordHashingService>();
            services.AddScoped<IJwtTokenService, JwtTokenService>();
            services.AddScoped<IEmailService, ConsoleEmailService>();
            services.AddScoped<DatabaseSeeder>();
            services.AddScoped<DemoWorkspaceSeeder>();

            // User context for identity resolution
            services.AddHttpContextAccessor();
            services.AddScoped<IUserContext, HttpUserContext>();

            // LLM provider factory (for dynamic provider creation)
            services.AddHttpClient();
            services.AddSingleton<LLMProviderFactory>(sp =>
            {
                var httpClient = sp.GetRequiredService<IHttpClientFactory>().CreateClient();
                var loggerFactory = sp.GetRequiredService<ILoggerFactory>();
                return new LLMProviderFactory(httpClient, loggerFactory);
            });

            // Webhook dispatcher for execution event notifications
            services.AddHttpClient<IWebhookDispatcher, DeterministicWebhookDispatcher>()
                .ConfigureHttpClient(client =>
                {
                    client.Timeout = TimeSpan.FromSeconds(10);
                });

            // Notification service for user notifications
            services.AddScoped<INotificationService, DeterministicNotificationService>();

            return services;
        }

        /// <summary>
        /// Initializes database schema, seeds admin account, and seeds demo workspace when configured.
        /// </summary>
        public static async Task InitializeDatabaseAsync(this IServiceProvider serviceProvider)
        {
            var dbContext = serviceProvider.GetService<IDatabaseContext>();
            if (dbContext != null)
            {
                await dbContext.InitializeAsync();
            }

            using var scope = serviceProvider.CreateScope();
            var seeder = scope.ServiceProvider.GetRequiredService<DatabaseSeeder>();
            await seeder.SeedAsync();

            var demoSeeder = scope.ServiceProvider.GetRequiredService<DemoWorkspaceSeeder>();
            await demoSeeder.SeedOnStartupAsync();
        }
    }
}