using System.Text.Json;
using Arc.Domain.Models;
using System.Text.Json.Serialization;
namespace Arc.Infrastructure.Persistence;


public class UserIdJsonConverter : JsonConverter<UserId>
{
    public override UserId Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.StartObject)
        {
            reader.Read();
            if (reader.TokenType == JsonTokenType.PropertyName)
            {
                var propertyName = reader.GetString();
                if (propertyName?.Equals("Value", StringComparison.OrdinalIgnoreCase) == true || 
                    propertyName?.Equals("value", StringComparison.OrdinalIgnoreCase) == true)
                {
                    reader.Read();
                    var guid = reader.GetGuid();
                    reader.Read();
                    return new UserId(guid);
                }
            }
        }
        else if (reader.TokenType == JsonTokenType.String)
        {
            return UserId.From(reader.GetString()!);
        }
        
        throw new JsonException("Unable to deserialize UserId");
    }

    public override void Write(Utf8JsonWriter writer, UserId value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();
        writer.WriteString("value", value.Value);
        writer.WriteEndObject();
    }
}