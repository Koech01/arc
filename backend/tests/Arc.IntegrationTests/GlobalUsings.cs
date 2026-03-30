global using Xunit;
[assembly: CollectionBehavior(DisableTestParallelization = true)]


internal static class IntegrationTestEnvironment
{
	[System.Runtime.CompilerServices.ModuleInitializer]
	internal static void ConfigureEnvironment()
	{
		Environment.SetEnvironmentVariable("RateLimiting__PermitLimit", "100000");
		Environment.SetEnvironmentVariable("RateLimiting__WindowSeconds", "60");

	}
}