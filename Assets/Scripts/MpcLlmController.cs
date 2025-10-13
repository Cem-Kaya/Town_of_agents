// Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
#pragma warning disable OPENAI001
using OpenAI.Responses;
using System;
using System.Collections.Generic;

public class MpcLlmController
{
    private readonly OpenAIResponseClient client;    

    public MpcLlmController(string apiKey, string model, NPCInteractable subject)
    {
        if (string.IsNullOrWhiteSpace(apiKey))
            throw new ArgumentException("The api key cannot be null.", nameof(apiKey));

        if (string.IsNullOrWhiteSpace(model))
            throw new ArgumentException("The model cannot be null.", nameof(model));

        if (subject == null)
            throw new ArgumentException("The subject NPCInteractable cannot be null.", nameof(subject));

        client = new(model, apiKey);

        Model = model;
        Subject = subject;        
    }

    public string Model { get; }
    public NPCInteractable Subject { get; }
    public string Name { get => Subject.displayName; }
    public string ReasoningEffortLevel { get; set; } = "low";
    public List<ChatHistoryItem> History { get; } = new List<ChatHistoryItem>();
    public string Instructions { get; set; }
    private List<ResponseItem> internalHistory = new List<ResponseItem>();

    public ChatResponse SendPrompt(string from, string input)
    {
        input = $"[{from} says]: {input}";
        LogToHistory(from, Name, input);

        ResponseCreationOptions options = new();
        options.ReasoningOptions = new ResponseReasoningOptions();
        options.ReasoningOptions.ReasoningEffortLevel = "low";
        options.Instructions = Instructions;

        //Add the possible local functions that the LLM agent can call.
        foreach (FunctionTool tool in LLMTools.GetAvailableTools())
            options.Tools.Add(tool);
            
        ResponseContentPart[] contentParts = { ResponseContentPart.CreateInputTextPart(input) };
        internalHistory.Add(ResponseItem.CreateUserMessageItem(contentParts));
        
        OpenAIResponse response;
        bool actionRequired;
        ChatResponse retval;
        string outputTextOverride;

        do
        {
            actionRequired = false;
            outputTextOverride = null;
            retval = new ChatResponse(from, Name);
            response = (OpenAIResponse)client.CreateResponse(internalHistory, options);            

            retval.FullJson = Newtonsoft.Json.JsonConvert.SerializeObject(response);
            internalHistory.AddRange(response.OutputItems);

            foreach (ResponseItem outputItem in response.OutputItems)
            {
                if (outputItem is FunctionCallResponseItem functionCall)
                {
                    var actionResponse = PerformActionSafe(functionCall);
                    var callRes = ResponseItem.CreateFunctionCallOutputItem(functionCall.CallId, actionResponse.Output);
                    internalHistory.Add(callRes);
                    //inputItems.Add(new FunctionCallOutputResponseItem(functionCall.CallId, actionResponse.Output));
                    retval.Message = $"The player performed the activity: '{functionCall.FunctionName}'.";
                    if (actionResponse.Parameters.ContainsKey("reason"))
                        outputTextOverride = actionResponse.Parameters["reason"].ToString();
                    else if (actionResponse.Parameters.ContainsKey("response"))
                        outputTextOverride = actionResponse.Parameters["response"].ToString();
                    //We do not support the automated interaction yet!!!.
                    actionRequired = false;
                }
            }

            //var textItems = response.OutputItems.OfType<MessageResponseItem>();
            if (!actionRequired)
                retval.Message = response.GetOutputText();

            if (outputTextOverride != null)
                retval.Message = outputTextOverride;

            LogToHistory(retval);
        }
        while (actionRequired);

        return retval;
    }
    
    private ActionResponse PerformActionSafe(FunctionCallResponseItem functionCall)
    {
        try
        {
            string functionName = functionCall.FunctionName;
            var parameters = LLMTools.ParseParameters(functionCall.FunctionArguments);
            return Subject.PerformAction(functionName, parameters);
        }
        catch (Exception ex)
        {
            ActionResponse response = new ActionResponse(functionCall.FunctionName);
            response.Error = ex;
            response.IsSuccessful = false;
            response.Output = "You cannot perform the required task due to personal excuses at the moment.";
            return response;
        }
    }

    private void LogToHistory(string from, string to, string message)
    {
        var historyItem = new ChatHistoryItem(from, to, message);
        History.Add(historyItem);
    }
    
    private void LogToHistory(ChatResponse response)
    {
        var historyItem = new ChatHistoryItem(response);
        //Console.WriteLine(historyItem.ToUnityLogString());
        //Debug.Log(historyItem.ToUnityLogString());
        History.Add(historyItem);
    }

    public void PrintHistory()
    {
        History.ForEach(Console.WriteLine);
    }

}

// Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
#pragma warning restore OPENAI001
