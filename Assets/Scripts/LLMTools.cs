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
    private static Dictionary<string, Type> _toolImpl = new Dictionary<string, Type>();

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
        _toolImpl.Add(visit_tool.FunctionName, typeof(ActionVisitOtherPlayer));
    }

    public static ReadOnlyCollection<FunctionTool> GetAvailableTools() => _tools.AsReadOnly();
    public static FunctionTool GetFunctionByName(string name) => _tools.FirstOrDefault(f => f.FunctionName == name);
    public static IPlayerAction CallFunction(FunctionCallResponseItem functionCall)
    {
        if (!_toolImpl.ContainsKey(functionCall.FunctionName))
            throw new NotImplementedException($"Unknown function: {functionCall.FunctionName}.");

        Type implementer = _toolImpl[functionCall.FunctionName];
        IPlayerAction action = Activator.CreateInstance(implementer) as IPlayerAction;
        if (action == null)
            throw new InvalidCastException($"The expected implementation type must be IPlayerAction. Found {implementer} instead.");        

        using JsonDocument argumentsJson = JsonDocument.Parse(functionCall.FunctionArguments);
        Dictionary<string, string> parameters = new Dictionary<string, string>();

        parameters = argumentsJson.Deserialize<Dictionary<string, string>>();
        action.Perform(parameters);
        return action;
    }

   
}

#pragma warning restore OPENAI001