using UnityEngine;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

public class PlayerInfo
{
    public string Name { get; set; }
    public string Personality { get; set; } = "You are a regular townsfolk, calm but somewhat scared of law enforcement.";
    public string PersonalityAsCulprit { get; set; } = "A little anxious and suspicious.";

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
            Personality = parts[1].Trim(),
            PersonalityAsCulprit = parts[2].Trim()
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
