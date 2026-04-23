
using System.Collections;
using System.Collections.Generic;
using UnityEngine;



[CreateAssetMenu(menuName = "Game/Animation/AnimationDef_SO")]
public class ActionDefinition_SO : ScriptableObject
{
    [Header("Animation")]
    public string AnimatorStateName;
    public AnimationClip Clip;
    public float Duration => Clip != null ? Clip.length : 0f;
    [Header("Timing")]
    public List<ActionTimelineEvent> Events = new();
    public List<ActionTimelineRange> Ranges = new();

    [Header("Default Locks")]
    public bool LockMove = true;
    public bool LockInteract = true;

    public bool IsValid()
    {
        if (Duration <= 0f) return false;

        return true;
    }
    public ActionTimelineEvent GetEvent(string name)
    {
        ActionTimelineEvent outcome = Events.Find(e => e.name == name);
        if (outcome == null)
        {
            Debug.LogError("Invalid Action Event Name!");
        }
        return outcome;
    }
    public ActionTimelineRange GetRange(string name) 
    {
        ActionTimelineRange outcome = Ranges.Find(e => e.name == name);
        if (outcome == null)
        {
            Debug.LogError("Invalid Action Range Name!");
        }
        return outcome;
    }
}
