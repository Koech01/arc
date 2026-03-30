using System.Text.Json;
using Arc.Domain.Models;
using Arc.Application.Results;
using System.Text.Json.Serialization;
namespace Arc.Infrastructure.Persistence;


public class ExecutionResultJsonConverter : JsonConverter<ExecutionResult>
{
    public override ExecutionResult Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        using var doc = JsonDocument.ParseValue(ref reader);
        var root = doc.RootElement;

        var userIdElement = root.GetProperty("userId");
        var userIdGuid = userIdElement.GetProperty("value").GetGuid();
        var userId = new UserId(userIdGuid);

        var tasksElement = root.GetProperty("tasks");
        var tasks = JsonSerializer.Deserialize<List<TaskExecutionResult>>(tasksElement.GetRawText(), options)
            ?? new List<TaskExecutionResult>();

        // Read executionId when present (records stored after the fix); fall back to
        // the derived-ID constructor for records stored before the fix.
        if (root.TryGetProperty("executionId", out var idEl) && idEl.ValueKind == JsonValueKind.String)
        {
            var executionId = idEl.GetString();
            if (!string.IsNullOrWhiteSpace(executionId))
                return new ExecutionResult(executionId, userId, tasks);
        }

        return new ExecutionResult(userId, tasks);
    }

    public override void Write(Utf8JsonWriter writer, ExecutionResult value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();

        writer.WriteString("executionId", value.ExecutionId);

        writer.WritePropertyName("userId");
        JsonSerializer.Serialize(writer, value.UserId, options);

        writer.WritePropertyName("tasks");
        JsonSerializer.Serialize(writer, value.Tasks, options);

        writer.WriteEndObject();
    }
}
