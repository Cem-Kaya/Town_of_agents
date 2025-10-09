using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

public class PlayerInfo
{
    public string Name { get; set; }
    public string LLMInstructionsRegular { get; set; }
    public string LLMInstructionsCulprit { get; set; }    

    public PlayerInfo(string name)
    {
        Name = name;
    }

    public static PlayerInfo Parse(string line, string delimiter = ":")
    {
        var parts = line.Split(delimiter);
        if (parts.Length != 3)
            throw new ArgumentException("Input must contain exactly 3 parts separated by delimiter");

        return new PlayerInfo(parts[0].Trim())
        {
            LLMInstructionsRegular = parts[1].Trim(),
            LLMInstructionsCulprit = parts[2].Trim()
        };
    }

    public static List<PlayerInfo> ParseLines(string lines, string delimiter = ":")
    {
        var result = new List<PlayerInfo>();
        var lineArray = Regex.Split(lines, Environment.NewLine);

        foreach (var line in lineArray)
        {
            if (!string.IsNullOrWhiteSpace(line))
            {
                result.Add(Parse(line.Trim(), delimiter));
            }
        }

        return result;
    }
}
