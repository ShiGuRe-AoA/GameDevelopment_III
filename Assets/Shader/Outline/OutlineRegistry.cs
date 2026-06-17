using System.Collections.Generic;
using UnityEngine;

public static class OutlineRegistry
{
    private static readonly List<Renderer> renderers = new();
    private static readonly HashSet<Renderer> set = new();

    public static List<Renderer> Renderers => renderers;

    public static int Count => renderers.Count;

    public static void Add(Renderer renderer)
    {
        if (renderer == null)
            return;

        if (set.Add(renderer))
            renderers.Add(renderer);
    }

    public static void Remove(Renderer renderer)
    {
        if (renderer == null)
            return;

        if (set.Remove(renderer))
            renderers.Remove(renderer);
    }

    public static void CleanupNulls()
    {
        for (int i = renderers.Count - 1; i >= 0; i--)
        {
            var r = renderers[i];

            if (r == null)
            {
                set.Remove(r);
                renderers.RemoveAt(i);
            }
        }
    }
}