using System;

public static class LLMUtils
{
    public static string GetEnvironmentVariable(string name, bool raiseError)
    {
        var value = Environment.GetEnvironmentVariable(name);
        if (value == null && raiseError)
        {
            throw new InvalidOperationException($"Environment variable '{name}' is not set.");
        }

        return value ?? string.Empty;
    }

    public static string GetOpenAIApiKey(string variableName = "OPENAI_API_KEY", bool raiseError = true)
    {
        return GetEnvironmentVariable(variableName, raiseError: raiseError);
    }
}