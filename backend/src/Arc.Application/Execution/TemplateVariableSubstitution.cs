namespace Arc.Application.Execution;
using System.Text.RegularExpressions;


/// <summary>
/// Handles template variable substitution in prompts using industry-standard syntax.
/// Supports: {{task-id.output}} or {{task-id}}
/// </summary>
public static class TemplateVariableSubstitution
{
    private static readonly Regex VariablePattern = new Regex(
        @"\{\{([a-zA-Z0-9_-]+)(?:\.output)?\}\}",
        RegexOptions.Compiled | RegexOptions.IgnoreCase
    );

    /// <summary>
    /// Substitutes template variables in the prompt with actual values from previous task outputs.
    /// Example: "Write intro for: {{task-1.output}}" -> "Write intro for: AI in Healthcare"
    /// </summary>
    public static string Substitute(string prompt, IReadOnlyDictionary<string, string> dependencyOutputs)
    {
        if (string.IsNullOrWhiteSpace(prompt))
            return prompt;

        if (dependencyOutputs == null || dependencyOutputs.Count == 0)
            return prompt;

        return VariablePattern.Replace(prompt, match =>
        {
            var taskId = match.Groups[1].Value;
            
            if (dependencyOutputs.TryGetValue(taskId, out var output))
            {
                return output ?? string.Empty;
            }

            // Keep original placeholder if task output not found
            return match.Value;
        });
    }

    /// <summary>
    /// Checks if a prompt contains any template variables.
    /// </summary>
    public static bool ContainsVariables(string prompt)
    {
        if (string.IsNullOrWhiteSpace(prompt))
            return false;

        return VariablePattern.IsMatch(prompt);
    }

    /// <summary>
    /// Extracts all task IDs referenced in template variables.
    /// </summary>
    public static IReadOnlyList<string> ExtractReferencedTaskIds(string prompt)
    {
        if (string.IsNullOrWhiteSpace(prompt))
            return Array.Empty<string>();

        var matches = VariablePattern.Matches(prompt);
        return matches.Select(m => m.Groups[1].Value).Distinct().ToList();
    }
}