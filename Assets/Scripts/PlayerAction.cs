using System;
using System.Collections.Generic;

public interface IPlayerAction
{
    /// <summary>
    /// Gets the uniqe name of the action.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// The exception raised during execution, if any. Otherwise null.
    /// </summary>
    public Exception Error { get; protected set; }

    /// <summary>
    /// The output of the action execution. Error message if execution fails.
    /// </summary>
    public string Output { get; protected set; }

    /// <summary>
    /// True if there are no errors during execution, false otherwise.
    /// </summary>
    public bool IsSuccessful { get; protected set; }

    /// <summary>
    /// Gets the execution parameters, if the Perform method is called.
    /// </summary>
    public Dictionary<string, string> Parameters { get; protected set; }
    
    /// <summary>
    /// Performs the action and set the Output property. 
    /// If any exception occurs during execution, it will be set to Error property.
    /// The output of the action will be assigned to Output property. In case of an error, it will contain error message.
    /// The status of the execution will be assigned to IsSuccessful propery.
    /// </summary>
    /// <param name="parameters">Action parameters.</param>
    public void Perform(Dictionary<string, string> parameters)
    {
        Error = null;
        Output = null;
        Parameters = parameters;

        try
        {
            Output = PerformInternal(parameters);
            IsSuccessful = true;
        }
        catch (Exception ex)
        {
            Error = ex;
            Output = $"Action failed: {ex.Message}";
            IsSuccessful = false;
        }
    }
    protected string PerformInternal(IDictionary<string, string> parameters);
}

public class ActionVisitOtherPlayer : IPlayerAction
{
    public string Name => "visit_other_player";

    Exception IPlayerAction.Error { get; set; }
    string IPlayerAction.Output{ get; set; }
    bool IPlayerAction.IsSuccessful { get; set; }
    Dictionary<string, string> IPlayerAction.Parameters { get; set; }

    string IPlayerAction.PerformInternal(IDictionary<string, string> parameters)
    {
        string targetNpcName = parameters?["player_name"];
        if (string.IsNullOrWhiteSpace(targetNpcName))
            return "The target player name is not specified. Please provide a valid player name.";

        return $"You are now visiting {targetNpcName}. You are both alone.";
    }
}
