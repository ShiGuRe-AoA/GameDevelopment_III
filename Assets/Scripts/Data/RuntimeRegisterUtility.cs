using DG.Tweening.Core.Easing;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class RuntimeRegisterUtility
{
    public static void RegisterAll(object obj)
    {
        if (obj == null) return;

        Debug.Log($"Register Entity: {obj.ToString()}");
        TimeManager.Instance?.Register(obj);
        //WorldState.Instance?.RegisterEntity(obj);

    }

    public static void UnregisterAll(object obj)
    {
        if (obj == null) return;

        TimeManager.Instance?.Unregister(obj);
        //WorldState.Instance?.UnRegisterEntity(obj);

    }
}