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
    public List<string> History { get; } = new List<string>();
    public string Instructions { get; set; }

    public string SendPrompt(string from, string input, bool returnJson)
    {
        LogToHistory(from, input);

        ResponseCreationOptions options = new();
        options.ReasoningOptions = new ResponseReasoningOptions();
        options.ReasoningOptions.ReasoningEffortLevel = "low";
        options.Instructions = Instructions;
        options.PreviousResponseId = previousConversationId;
        //options.ReasoningOptions.ReasoningSummaryVerbosity = "concise";//detailed, auto

        //options.Tools.Add(ResponseTool.CreateWebSearchTool());
        ResponseContentPart[] contentParts = { ResponseContentPart.CreateInputTextPart(input) };
        ResponseItem[] messageItemsCollection = { ResponseItem.CreateUserMessageItem(contentParts) };
        OpenAIResponse response = (OpenAIResponse)client.CreateResponse(messageItemsCollection, options);
        previousConversationId = response.Id;

        string outputText = response.GetOutputText();
        LogToHistory(Name, outputText);

        if (returnJson)
        {
            string jsonResponse = Newtonsoft.Json.JsonConvert.SerializeObject(response);
            return jsonResponse;
        }

        return outputText;
    }

    private void LogToHistory(string who, string what)
    {
        string log = $"[{DateTime.Now:F}]\t[{who}]\t{what}";
        History.Add(log);
    }

    public void PrintHistory()
    {
        History.ForEach(Console.WriteLine);
    }

}

// Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
#pragma warning restore OPENAI001
