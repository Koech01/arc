using FluentAssertions;
using Arc.Application.Execution;
namespace Arc.UnitTests.Execution;


public sealed class TemplateVariableSubstitutionTests
{
    [Fact]
    public void Substitute_WithSingleVariable_ReplacesCorrectly()
    {
        // Arrange
        var prompt = "Write an introduction for: {{task-1.output}}";
        var outputs = new Dictionary<string, string>
        {
            ["task-1"] = "AI in Healthcare"
        };

        // Act
        var result = TemplateVariableSubstitution.Substitute(prompt, outputs);

        // Assert
        result.Should().Be("Write an introduction for: AI in Healthcare");
    }

    [Fact]
    public void Substitute_WithMultipleVariables_ReplacesAll()
    {
        // Arrange
        var prompt = "Compare {{task-1}} with {{task-2.output}} and summarize.";
        var outputs = new Dictionary<string, string>
        {
            ["task-1"] = "Machine Learning",
            ["task-2"] = "Deep Learning"
        };

        // Act
        var result = TemplateVariableSubstitution.Substitute(prompt, outputs);

        // Assert
        result.Should().Be("Compare Machine Learning with Deep Learning and summarize.");
    }

    [Fact]
    public void Substitute_WithMissingVariable_KeepsPlaceholder()
    {
        // Arrange
        var prompt = "Use {{task-1}} and {{task-2}}";
        var outputs = new Dictionary<string, string>
        {
            ["task-1"] = "Available Output"
        };

        // Act
        var result = TemplateVariableSubstitution.Substitute(prompt, outputs);

        // Assert
        result.Should().Be("Use Available Output and {{task-2}}");
    }

    [Fact]
    public void Substitute_WithNoVariables_ReturnsOriginal()
    {
        // Arrange
        var prompt = "Simple prompt without variables";
        var outputs = new Dictionary<string, string>
        {
            ["task-1"] = "Output"
        };

        // Act
        var result = TemplateVariableSubstitution.Substitute(prompt, outputs);

        // Assert
        result.Should().Be("Simple prompt without variables");
    }

    [Fact]
    public void Substitute_WithEmptyOutputs_ReturnsOriginal()
    {
        // Arrange
        var prompt = "Use {{task-1}}";
        var outputs = new Dictionary<string, string>();

        // Act
        var result = TemplateVariableSubstitution.Substitute(prompt, outputs);

        // Assert
        result.Should().Be("Use {{task-1}}");
    }

    [Fact]
    public void Substitute_WithNullPrompt_ReturnsNull()
    {
        // Arrange
        string? prompt = null;
        var outputs = new Dictionary<string, string> { ["task-1"] = "Output" };

        // Act
        var result = TemplateVariableSubstitution.Substitute(prompt!, outputs);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void Substitute_WithDashesAndUnderscores_Works()
    {
        // Arrange
        var prompt = "Use {{task-1}} and {{task_2}} and {{task-3-final}}";
        var outputs = new Dictionary<string, string>
        {
            ["task-1"] = "First",
            ["task_2"] = "Second",
            ["task-3-final"] = "Third"
        };

        // Act
        var result = TemplateVariableSubstitution.Substitute(prompt, outputs);

        // Assert
        result.Should().Be("Use First and Second and Third");
    }

    [Fact]
    public void ContainsVariables_WithVariables_ReturnsTrue()
    {
        // Arrange
        var prompt = "Use {{task-1}} here";

        // Act
        var result = TemplateVariableSubstitution.ContainsVariables(prompt);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void ContainsVariables_WithoutVariables_ReturnsFalse()
    {
        // Arrange
        var prompt = "Simple prompt";

        // Act
        var result = TemplateVariableSubstitution.ContainsVariables(prompt);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void ExtractReferencedTaskIds_WithMultipleVariables_ReturnsAllIds()
    {
        // Arrange
        var prompt = "Use {{task-1}} and {{task-2.output}} and {{task-1}} again";

        // Act
        var result = TemplateVariableSubstitution.ExtractReferencedTaskIds(prompt);

        // Assert
        result.Should().BeEquivalentTo(new[] { "task-1", "task-2" });
    }

    [Fact]
    public void ExtractReferencedTaskIds_WithNoVariables_ReturnsEmpty()
    {
        // Arrange
        var prompt = "Simple prompt";

        // Act
        var result = TemplateVariableSubstitution.ExtractReferencedTaskIds(prompt);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public void Substitute_WithComplexRealWorldExample_Works()
    {
        // Arrange
        var prompt = @"
You are a technical writer. Based on the following information:

Title: {{task-1.output}}
Key Points: {{task-2}}
Target Audience: Software Developers

Write a comprehensive 500-word blog post introduction.";

        var outputs = new Dictionary<string, string>
        {
            ["task-1"] = "The Future of AI in Software Development",
            ["task-2"] = "AI-assisted coding, automated testing, intelligent code review"
        };

        // Act
        var result = TemplateVariableSubstitution.Substitute(prompt, outputs);

        // Assert
        result.Should().Contain("The Future of AI in Software Development");
        result.Should().Contain("AI-assisted coding, automated testing, intelligent code review");
        result.Should().NotContain("{{");
    }
}