using System;
using System.Collections.Generic;
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
    
    public static List<PromptInfo> LoadPrompts(string promptsFolder = "./StreamingAssets")
    {
        List<PromptInfo> prompts = new List<PromptInfo>();
        var flist = Directory.GetFiles(promptsFolder,"prompt_*");

        foreach (string path in flist)
        {
            FileInfo info = new FileInfo(path);
            if (info.Extension == ".meta")
                continue;
                
            string[] parts = info.Name.Replace(info.Extension, "").Split('_');
            if (parts.Length < 3)
                continue;

            string occupation = parts[1].Trim().ToLower();
            bool isCulprit = parts[2].Trim().ToLower() == "culprit";
            string prompt = File.ReadAllText(path);
            PromptInfo promptInfo = new PromptInfo(prompt, occupation, isCulprit);
            prompts.Add(promptInfo);
        }

        return prompts;
    }

    public static string LoadTownCollectiveMemory(string promptsFolder = "./StreamingAssets", string fileName = "town_collective_memory.txt")
    {
        string path = Path.Combine(promptsFolder, fileName);
        string content = File.ReadAllText(path);
        return content;
    }
    
    public static string LoadGeneralRules(string promptsFolder = "./StreamingAssets", string fileName="npc_default_rules.txt")
    {
        string path = Path.Combine(promptsFolder, fileName);
        string content = File.ReadAllText(path);
        return content;
    }
}