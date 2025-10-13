#pragma warning disable OPENAI001
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text.Json;
using OpenAI.Responses;
using Unity.VisualScripting;

public class LLMTools
{
    private static List<FunctionTool> _tools = new List<FunctionTool>();    

    static LLMTools()
    {
        InitializeTools();
    }

    private static void InitializeTools()
    {
        var visit_tool = ResponseTool.CreateFunctionTool(
        functionName: "visit_other_player",
        functionDescription: "Go to another player in the game map.",
        functionParameters: BinaryData.FromObjectAsJson(new
        {
            type = "object",
            properties = new
            {
                player_name = new
                {
                    type = "string",
                    description = "Name of the player. Only the names from the instructions are valid."
                },
                explanation = new
                {
                    type = "string",
                    description = "The reason to visit the other player. Start with 'Hi [player_name]' and explain briefly why you are here. Keep it brief."
                }
            },
            required = new[] { "player_name", "explanation" },
            additionalProperties = false
        }),
            strictModeEnabled: true
            );

        _tools.Add(visit_tool);
    }

    public static ReadOnlyCollection<FunctionTool> GetAvailableTools() => _tools.AsReadOnly();
    public static FunctionTool GetFunctionByName(string name) => _tools.FirstOrDefault(f => f.FunctionName == name);

    public static Dictionary<string,object> ParseParameters(ReadOnlyMemory<byte> functionCallArguments)
    {
        using JsonDocument argumentsJson = JsonDocument.Parse(functionCallArguments);
        Dictionary<string, JsonElement> jp = argumentsJson.Deserialize<Dictionary<string, JsonElement>>();

        Dictionary<string, object> parameters = new Dictionary<string, object>();
        foreach (var item in jp)
        {
            switch (item.Value.ValueKind)
            {
                // case JsonValueKind.Undefined:
                //     break;
                // case JsonValueKind.Object:
                //     break;
                case JsonValueKind.Array:
                    break;
                case JsonValueKind.String:
                    parameters.Add(item.Key, item.Value.GetString());
                    break;
                case JsonValueKind.Number:
                    parameters.Add(item.Key, item.Value.ConvertTo<float>());
                    break;
                case JsonValueKind.True:
                    parameters.Add(item.Key, true);
                    break;
                case JsonValueKind.False:
                    parameters.Add(item.Key, false);
                    break;
                case JsonValueKind.Null:
                    parameters.Add(item.Key, null);
                    break;
                default:
                    parameters.Add(item.Key, item.Value.ConvertTo<string>());
                    break;
            }
        }
        return parameters;
    }

}

#pragma warning restore OPENAI001