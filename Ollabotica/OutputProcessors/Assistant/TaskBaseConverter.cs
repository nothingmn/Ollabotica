using System.Text.Json;
using System.Text.Json.Serialization;

namespace Ollabotica.OutputProcessors.Assistant;

public class TaskBaseConverter : JsonConverter<TaskBase>
{
    public override TaskBase Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        // Parse the JSON document
        using (JsonDocument jsonDoc = JsonDocument.ParseValue(ref reader))
        {
            var rootElement = jsonDoc.RootElement;
            string taskType = rootElement.GetProperty("task").GetString();

            TaskBase task;

            // Use the "Task" property to decide which concrete class to deserialize into
            switch (taskType.ToLowerInvariant())
            {
                case "createreminder":
                    task = JsonSerializer.Deserialize<CreateReminder>(rootElement.GetRawText(), options);
                    break;
                case "deletereminder":
                    task = JsonSerializer.Deserialize<DeleteReminder>(rootElement.GetRawText(), options);
                    break;
                case "createtimer":
                    task = JsonSerializer.Deserialize<CreateTimer>(rootElement.GetRawText(), options);
                    break;
                case "deletetimer":
                    task = JsonSerializer.Deserialize<DeleteTimer>(rootElement.GetRawText(), options);
                    break;
                case "createevent":
                    task = JsonSerializer.Deserialize<CreateEvent>(rootElement.GetRawText(), options);
                    break;
                case "sendmessage":
                    task = JsonSerializer.Deserialize<SendMessage>(rootElement.GetRawText(), options);
                    break;
                case "sendemail":
                    task = JsonSerializer.Deserialize<SendEmail>(rootElement.GetRawText(), options);
                    break;
                default:
                    throw new NotSupportedException($"Task type '{taskType}' is not supported.");
            }

            return task;
        }
    }

    public override void Write(Utf8JsonWriter writer, TaskBase value, JsonSerializerOptions options)
    {
        // Serialization not implemented in this example
        throw new NotImplementedException();
    }
}
