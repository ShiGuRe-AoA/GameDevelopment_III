using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class EntityRuntimeBase : IEntityRuntime, ISaveableEntity
{
    public int EntityId { get; protected set; }
    public Vector3Int PivotPos { get; protected set; }

    public virtual EntitySaveData Save()
    {
        return EntitySaveUtility.SaveBase(this);
    }

    public virtual void Load(EntitySaveData data)
    {
        EntityId = data.EntityId;
        PivotPos = data.PivotPos;
    }

    public void Init(int entityId, Vector3Int pivotPos, WorldState worldState)
    {
        EntityId = entityId;
        PivotPos = pivotPos;
    }

    public void OnAwake()
    {
        //throw new System.NotImplementedException();
    }
    public void OnDestroy()
    {
        //throw new System.NotImplementedException();
    }
}