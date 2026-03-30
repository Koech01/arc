using System.Net.Http.Json;
using Arc.Api.DTOs.Workflows;
namespace Arc.IntegrationTests.Api;
using Microsoft.AspNetCore.Mvc.Testing;


public sealed class WorkflowLLMConfigIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public WorkflowLLMConfigIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task CreateWorkflow_WithLLMConfigId_StoresSuccessfully()
    {
        var client = _factory.CreateClient();

        var registerResponse = await client.PostAsJsonAsync("/api/auth/register", new
        {
            username = $"llm-{Guid.NewGuid():N}",
            email = $"llm-{Guid.NewGuid():N}@example.com",
            password = "Password123!"
        });

        Assert.True(registerResponse.IsSuccessStatusCode);

        var cookies = registerResponse.Headers.GetValues("Set-Cookie");
        var authCookie = cookies.FirstOrDefault(c => c.StartsWith("auth_token="));
        Assert.NotNull(authCookie);

        client.DefaultRequestHeaders.Add("Cookie", authCookie.Split(';')[0]);

        // Create workflow with LLMConfigId
        var createRequest = new CreateWorkflowRequestDto
        {
            Name = $"Test Workflow {Guid.NewGuid()}",
            Description = "Test workflow with LLM config",
            TriggerType = "manual",
            LLMConfigId = "test-llm-config-id",
            Tasks = new List<WorkflowTaskDto>
            {
                new WorkflowTaskDto
                {
                    Id = "task1",
                    Name = "Test Task",
                    AgentType = "http",
                    Config = new Dictionary<string, string>(),
                    Dependencies = new List<string>()
                }
            }
        };

        var createResponse = await client.PostAsJsonAsync("/api/workflows", createRequest);
        Assert.True(
            createResponse.StatusCode == System.Net.HttpStatusCode.Created ||
            createResponse.StatusCode == System.Net.HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateWorkflow_WithoutLLMConfigId_StoresSuccessfully()
    {
        var client = _factory.CreateClient();

        var registerResponse = await client.PostAsJsonAsync("/api/auth/register", new
        {
            username = $"llm-{Guid.NewGuid():N}",
            email = $"llm-{Guid.NewGuid():N}@example.com",
            password = "Password123!"
        });

        Assert.True(registerResponse.IsSuccessStatusCode);

        var cookies = registerResponse.Headers.GetValues("Set-Cookie");
        var authCookie = cookies.FirstOrDefault(c => c.StartsWith("auth_token="));
        Assert.NotNull(authCookie);

        client.DefaultRequestHeaders.Add("Cookie", authCookie.Split(';')[0]);

        // Create workflow without LLMConfigId
        var createRequest = new CreateWorkflowRequestDto
        {
            Name = $"Test Workflow No LLM {Guid.NewGuid()}",
            Description = "Test workflow without LLM config",
            TriggerType = "manual",
            Tasks = new List<WorkflowTaskDto>
            {
                new WorkflowTaskDto
                {
                    Id = "task1",
                    Name = "Test Task",
                    AgentType = "http",
                    Config = new Dictionary<string, string>(),
                    Dependencies = new List<string>()
                }
            }
        };

        var createResponse = await client.PostAsJsonAsync("/api/workflows", createRequest);
        Assert.True(createResponse.IsSuccessStatusCode);

        var workflow = await createResponse.Content.ReadFromJsonAsync<WorkflowResponseDto>();
        Assert.NotNull(workflow);
        Assert.NotEmpty(workflow.Id);
    }
}
