using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public struct EntitySaveData
{
    public int EntityId;
    public Vector3Int PivotPos;
}
public struct EntityRuntimeData
{
    public int EntityId;
    public Vector3Int PivotPos;
}
public static class EntitySaveUtility
{
    public static EntitySaveData SaveBase(IEntityRuntime entity)
    {
        return new EntitySaveData
        {
            EntityId = entity.EntityId,
            PivotPos = entity.PivotPos
        };
    }

    public static void LoadBase(EntitySaveData data, EntityRuntimeData target)
    {
        target.EntityId = data.EntityId;
        target.PivotPos = data.PivotPos;
    }
}