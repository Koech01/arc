using FluentAssertions;
using Arc.Application.LLM;
namespace Arc.UnitTests.LLM;
using Arc.Infrastructure.LLM;


public sealed class FakeLlmProviderTests
{
    private readonly ILLMProvider _provider = new FakeLlmProvider();

    [Fact]
    public async Task GenerateText_SamePrompt_ProducesSameOutput()
    {
        var prompt = "Test deterministic prompt";

        var output1 = await _provider.GenerateTextAsync(prompt, CancellationToken.None);
        var output2 = await _provider.GenerateTextAsync(prompt, CancellationToken.None);

        output1.Should().Be(output2);
    }

    [Fact]
    public async Task GenerateText_DifferentPrompts_ProduceDifferentOutputs()
    {
        var prompt1 = "Prompt A";
        var prompt2 = "Prompt B";

        var output1 = await _provider.GenerateTextAsync(prompt1, CancellationToken.None);
        var output2 = await _provider.GenerateTextAsync(prompt2, CancellationToken.None);

        output1.Should().NotBe(output2);
    }

    [Fact]
    public async Task GenerateText_NullPrompt_ThrowsArgumentNullException()
    {
        Func<Task> act = async () => await _provider.GenerateTextAsync(null!, CancellationToken.None);
        await act.Should().ThrowAsync<ArgumentNullException>();
    }
}