using DG.Tweening.Core.Easing;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class RuntimeRegisterUtility
{
    public static void RegisterAll(object obj)
    {
        if (obj == null) return;

        TimeManager.Instance?.Register(obj);

    }

    public static void UnregisterAll(object obj)
    {
        if (obj == null) return;

        TimeManager.Instance?.Unregister(obj);

    }
}