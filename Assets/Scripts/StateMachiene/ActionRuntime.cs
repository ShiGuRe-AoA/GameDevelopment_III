using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum ActionStage
{
    Windup,
    Recovery,
    Finished,
    Error
}

[System.Serializable]
public class ActionTimelineEvent
{
    public ActionTimelineEvent(string name, float time)
    {
        this.name = name;
        this.time = time;
    }
    public string name;
    public float time;
}

[System.Serializable]
public class ActionTimelineRange
{
    public ActionTimelineRange(string name, float start, float end)
    {
        this.name = name;
        this.start = start;
        this.end = end;
    }
    public string name;
    public float start;
    public float end;
}
public class ActionRuntime
{
    public ActionDefinition_SO Definition { get; private set; }
    public float Elapsed { get; private set; }
    public bool EffectApplied { get; private set; }

    public ActionRuntime(ActionDefinition_SO definition)
    {
        Definition = definition;
        Elapsed = 0f;
        EffectApplied = false;
    }

    public void Tick(float deltaTime)
    {
        if (Definition == null) return;
        if (IsFinished()) return;

        Elapsed += deltaTime;

        if (Elapsed > Definition.Duration)
        {
            Elapsed = Definition.Duration;
        }
    }

    public void MarkEffectApplied()
    {
        EffectApplied = true;
    }

    public void Reset()
    {
        Elapsed = 0f;
        EffectApplied = false;
    }

    public bool CanApplyEffect()
    {
        if (Definition == null) return false;
        if (EffectApplied) return false;

        return Elapsed >= Definition.GetEvent("EffectTime").time;
    }

    public bool CanCancel()
    {
        if (Definition == null) return false;
        if (IsFinished()) return false;

        return Elapsed >= Definition.GetEvent("CancelStart").time;
    }
    public bool IsFinished()
    {
        if (Definition == null) return true;
        return Elapsed >= Definition.Duration;
    }

    public float GetNormalizedTime()
    {
        if (Definition == null || Definition.Duration <= 0f)
        {
            return 0f;
        }

        return Elapsed / Definition.Duration;
    }
}