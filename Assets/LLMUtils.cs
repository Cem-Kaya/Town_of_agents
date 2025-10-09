using System;
using System.IO;
using Microsoft.Extensions.Configuration;
using UnityEngine;

public static class LLMUtils
{
    private static IConfiguration _configuration;
    
    static LLMUtils()
    {
        try
        {
            var llmConfFile = Path.Combine(Application.streamingAssetsPath, "llm_settings.json");
            bool exists = File.Exists(llmConfFile);
            _configuration = new ConfigurationBuilder()
                .AddJsonFile(llmConfFile, optional: true, reloadOnChange: true)
                .AddEnvironmentVariables()
                .Build();
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error loading LLM configuration: {ex.Message}");
        }
    }
    
    public static string GetOpenAIApiKey(string variableName = "OPENAI_API_KEY", bool raiseError = true)
    {
        // Try configuration first, then environment variable
        var value = _configuration?[variableName] ?? Environment.GetEnvironmentVariable(variableName);
        
        if (value == null && raiseError)
        {
            throw new InvalidOperationException($"API key '{variableName}' not found in config or environment.");
        }
        
        return value ?? string.Empty;
    }
}