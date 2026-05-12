using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum EntityRuntimeKind
{
    Farmland,
    Crop,
}

public static class EntityRuntimeFactory
{
    private static readonly Dictionary<EntityRuntimeKind, System.Func<IEntityRuntime>> map
        = new()
        {
            { EntityRuntimeKind.Farmland, () => new Farmland_Entity() },
            { EntityRuntimeKind.Crop, () => new Crops_Entity() },
        };

    public static IEntityRuntime Create(EntityRuntimeKind kind)
    {
        if (!map.TryGetValue(kind, out var ctor))
        {
            Debug.LogError($"Unknown EntityRuntimeKind: {kind}");
            return null;
        }

        IEntityRuntime entity = ctor();

        RuntimeRegisterUtility.RegisterAll(entity);

        return entity;
    }
}