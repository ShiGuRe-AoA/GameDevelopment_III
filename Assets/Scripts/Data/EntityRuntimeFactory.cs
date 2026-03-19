using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum EntityRuntimeKind
{
    Farmland,
    
}

public static class EntityRuntimeFactory
{
    private static readonly Dictionary<EntityRuntimeKind, System.Func<EntityRuntime>> map
        = new()
        {
            { EntityRuntimeKind.Farmland, () => new Farmland_Entity() },

        };

    public static EntityRuntime Create(EntityRuntimeKind kind)
    {
        return map.TryGetValue(kind, out var ctor) ? ctor() : null;
    }
}