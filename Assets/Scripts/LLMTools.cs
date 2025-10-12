#pragma warning disable OPENAI001
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text.Json;
using OpenAI.Responses;

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
    public static Tuple<string,Dictionary<string,string>> CallFunction(FunctionCallResponseItem functionCall)
    {
        FunctionTool fcall = GetFunctionByName(functionCall.FunctionName);
        if (fcall == null)
            throw new NotImplementedException($"Unknown function: {functionCall.FunctionName}.");

        using JsonDocument argumentsJson = JsonDocument.Parse(functionCall.FunctionArguments);
        Dictionary<string, string> parameters = new Dictionary<string, string>();

        parameters = argumentsJson.Deserialize<Dictionary<string, string>>();

        string functionOutput = "";
        if (functionCall.FunctionName == "visit_other_player")
            functionOutput = VisitOtherPlayer(parameters);

        return new Tuple<string, Dictionary<string, string>>(functionOutput, parameters);
    }

    public static string VisitOtherPlayer(IDictionary<string, string> parameters)
    {
        string targetNpcName = parameters?["player_name"];
        if (string.IsNullOrWhiteSpace(targetNpcName))
            return "The target player name is not specified. Please provide a valid player name.";

        return $"You are now visiting {targetNpcName}. You are both alone.";
    }

}

#pragma warning restore OPENAI001