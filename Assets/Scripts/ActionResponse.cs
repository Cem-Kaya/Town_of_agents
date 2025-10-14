using System;
using System.Collections.Generic;
using UnityEngine;

public class ActionResponse
{
    public ActionResponse(string name)
    {
        Name = name;
        Timestamp = DateTime.Now;
    }

    public DateTime Timestamp{ get; }
    /// <summary>
    /// Gets the uniqe name of the action.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// The exception raised during execution, if any. Otherwise null.
    /// </summary>
    public Exception Error { get; set; }

    /// <summary>
    /// The output of the action execution. Error message if execution fails.
    /// </summary>
    public string Output { get; set; }

    /// <summary>
    /// True if there are no errors during execution, false otherwise.
    /// </summary>
    public bool IsSuccessful { get; set; }

    /// <summary>
    /// Gets or sets the execution parameters, if the Perform method is called.
    /// </summary>
    public Dictionary<string, object> Parameters { get; set; }

    public string ToLogString(bool includeTimestamp)
    {
        if (IsSuccessful)
        {
            if (includeTimestamp)
                return $"[{Timestamp:F}]\tAction Performed. [{Name}]:\t{Output}";

            return $"Action Performed. [{Name}]:\t{Output}";
        }

        if (includeTimestamp)
            return $"[{Timestamp:F}]\tAction Failed. [{Name}]:\t{Output}";
        
        return $"Action Failed. [{Name}]:\t{Output}";
    }
}
