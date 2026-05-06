using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ActionRegistry : MonoBehaviour
{
    public static Dictionary<string, ActionDefinition_SO> PlayerAction = new();
    public static void RegistryAction(Dictionary<string, ActionDefinition_SO> target, List<ActionDefinition_SO> actions)
    {
        foreach (var action in actions)
        {
            target.Add(action.AnimatorStateName, action);
        }
    }

    public static ActionDefinition_SO Get(Dictionary<string, ActionDefinition_SO> target, string actionName)
    {
        if (!target.ContainsKey(actionName))
        {
            return null;
        }
        return target[actionName];
    }
}
