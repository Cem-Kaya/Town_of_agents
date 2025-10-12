// Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
#pragma warning disable OPENAI001
using OpenAI.Responses;
using System;
using System.Collections.Generic;
using UnityEngine;

public class MpcLlmController
{
    private readonly OpenAIResponseClient client;
    private string previousConversationId;

    public MpcLlmController(string apiKey, string model, string name)
    {
        if (string.IsNullOrWhiteSpace(apiKey))
            throw new ArgumentException("The api key cannot be null.", nameof(apiKey));

        if (string.IsNullOrWhiteSpace(model))
            throw new ArgumentException("The model cannot be null.", nameof(model));

        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("The name cannot be null.", nameof(name));

        client = new(model, apiKey);

        Model = model;
        Name = name;
    }

    public string Model { get; }
    public string Name { get; }
    public string ReasoningEffortLevel { get; set; } = "low";
    public List<ChatHistoryItem> History { get; } = new List<ChatHistoryItem>();
    public string Instructions { get; set; }

    public string SendPrompt(string from, string input, bool returnJson)
    {
        LogToHistory(from, input);

        ResponseCreationOptions options = new();
        options.ReasoningOptions = new ResponseReasoningOptions();
        options.ReasoningOptions.ReasoningEffortLevel = "low";
        options.Instructions = Instructions;
        options.PreviousResponseId = previousConversationId;        
        
        //Add the possible local functions that the LLM agent can call.
        foreach(FunctionTool tool in LLMTools.GetAvailableTools())
            options.Tools.Add(tool);

        //options.Tools.Add(ResponseTool.CreateWebSearchTool());
        ResponseContentPart[] contentParts = { ResponseContentPart.CreateInputTextPart(input) };
        List<ResponseItem> inputItems = new List<ResponseItem>
        {
            ResponseItem.CreateUserMessageItem(contentParts)
        };
        
        OpenAIResponse response;
        bool actionRequired;
        string outputText;
        do
        {
            actionRequired = false;
            outputText = "";
            response = (OpenAIResponse)client.CreateResponse(inputItems, options);
            previousConversationId = response.Id;
            options.PreviousResponseId = previousConversationId;

            //Is this necessary with history tracking above??? need to verify.
            //inputItems.AddRange(response.OutputItems);

            foreach (ResponseItem outputItem in response.OutputItems)
            {
                if (outputItem is FunctionCallResponseItem functionCall)
                {
                    var returnValue = LLMTools.CallFunction(functionCall);
                    inputItems.Add(new FunctionCallOutputResponseItem(functionCall.CallId, returnValue.Item1));
                    string msg = $"The player performed the activity: '{functionCall.FunctionName}'.";
                    LogActionToHistory(Name, msg, functionCall.FunctionName, returnValue);
                    actionRequired = true;
                }
            }

            //var textItems = response.OutputItems.OfType<MessageResponseItem>();
            if (!actionRequired)
                outputText = response.GetOutputText();
        }
        while (actionRequired);

        // string outputText = response.GetOutputText();
        // //LogToHistory(Name, outputText);

        if (returnJson)
        {
            string jsonResponse = Newtonsoft.Json.JsonConvert.SerializeObject(response);
            return jsonResponse;
        }

        LogToHistory(Name, outputText);
        return outputText;
    }

    private void LogToHistory(string who, string what)
    {
        var historyItem = new ChatHistoryItem(who, what);
        //Console.WriteLine(historyItem.ToUnityLogString());
        Debug.Log(historyItem.ToUnityLogString());
        History.Add(historyItem);
    }   

    private void LogActionToHistory(string who, string what, string functionName, Tuple<string,Dictionary<string,string>> returnValue)
    {
        string functionOutput = returnValue.Item1;
        var parameters = returnValue.Item2;

        var historyItem = new ChatHistoryItem(who, what);
        historyItem.PlayerPerformedActivity = true;
        historyItem.ActivityName = functionName;
        historyItem.ActivityParameters = parameters;
        historyItem.ActivityResult = functionOutput;
        //Console.WriteLine(historyItem.ToUnityLogString());
        History.Add(historyItem);
    }

    public void PrintHistory()
    {
        History.ForEach(Console.WriteLine);
    }

}

// Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
#pragma warning restore OPENAI001
