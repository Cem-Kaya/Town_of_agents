#pragma warning disable OPENAI001
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
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
        var go_to_npc = ResponseTool.CreateFunctionTool(
        functionName: "go_to_npc",
        functionDescription: "Go to another NPC in the game map.",
        functionParameters: BinaryData.FromObjectAsJson(new
        {
            type = "object",
            properties = new
            {
                npc_name = new
                {
                    type = "string",
                    description = "Name of the other NPC. Only the names from the instructions are valid."
                }
            },
            required = new[] { "npc_name" },
            additionalProperties = false
        }),
            strictModeEnabled: true
            );

        _tools.Add(go_to_npc);

        var handover_item_to_detective = ResponseTool.CreateFunctionTool(
        functionName: "handover_item_to_detective",
        functionDescription: "Handover an item to detective.",
        functionParameters: BinaryData.FromObjectAsJson(new
        {
            type = "object",
            properties = new
            {
                item_name = new
                {
                    type = "string",
                    description = "Name of the item to handover. Only Items you currently have in the instructions are valid."
                },
                response = new
                {
                    type = "string",
                    description = "Short acknowledgement phrase."
                }
            },
            required = new[] { "item_name", "response" },
            additionalProperties = false
        }),
            strictModeEnabled: true
            );

        _tools.Add(handover_item_to_detective);

        var refuse_handover_item_to_detective = ResponseTool.CreateFunctionTool(
        functionName: "refuse_handover_item_to_detective",
        functionDescription: "Refuse handing over the item to detective. If you do not have asked item, do not call this method, just respond and tell you don't have it.",
        functionParameters: BinaryData.FromObjectAsJson(new
        {
            type = "object",
            properties = new
            {
                item_name = new
                {
                    type = "string",
                    description = "Name of the item you refused to handover. Only Items you currently have in the instructions are valid."
                },
                reason = new
                {
                    type = "string",
                    description = "Your reason to refuse."
                }
            },
            required = new[] { "item_name", "reason" },
            additionalProperties = false
        }),
            strictModeEnabled: true
            );

        _tools.Add(refuse_handover_item_to_detective);

        var arrest_suspect = ResponseTool.CreateFunctionTool(
        functionName: "arrest_suspect",
        functionDescription: "Arrest the suspected NPC. Only mayor is allowed to call this function.",
        functionParameters: BinaryData.FromObjectAsJson(new
        {
            type = "object",
            properties = new
            {
                suspect_npc_name = new
                {
                    type = "string",
                    description = "Name of the suspected NPC to arrest. Only the names from the instructions are valid"
                },
                response = new
                {
                    type = "string",
                    description = "A short excited text about the action, similar to 'let's go get him!'."
                }
            },
            required = new[] { "suspect_npc_name", "response" },
            additionalProperties = false
        }),
            strictModeEnabled: true
            );

        _tools.Add(arrest_suspect);
    }

    public static ReadOnlyCollection<FunctionTool> GetAvailableTools() => _tools.AsReadOnly();
    public static FunctionTool GetFunctionByName(string name) => _tools.FirstOrDefault(f => f.FunctionName == name);

    public static string TryGetValueAsString(Dictionary<string, object> parameters, string paramName)
    {
        if (parameters == null || !parameters.ContainsKey(paramName))
        {
            return null;
        }

        return parameters[paramName]?.ToString();
    }

    public static Dictionary<string, object> ParseParameters(ReadOnlyMemory<byte> functionCallArguments)
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

    public static Dictionary<string, string> LoadPrompts(string promptsFolder = "/StreamingAssets")
    {
        Dictionary<string, string> prompts = new Dictionary<string, string>();
        Directory.GetFiles(promptsFolder, "prompts_*");

        return prompts;
    }
}


#pragma warning restore OPENAI001