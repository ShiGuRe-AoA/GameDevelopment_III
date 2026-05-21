using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class EntityRuntimeBase : IEntityRuntime, ISaveableEntity
{
    public int EntityId { get; protected set; }

    public bool Inited = false;
    public Vector3Int PivotPos { get; protected set; }

    public List<GameObject> RelativeObj => throw new System.NotImplementedException();

    public virtual EntitySaveData Save()
    {
        return EntitySaveUtility.SaveBase(this);
    }

    public virtual void Load(EntitySaveData data)
    {
        EntityId = data.EntityId;
        PivotPos = data.PivotPos;
    }

    public virtual void EntityInit(int entityId, Vector3Int pivotPos, WorldState worldState)
    {
        if (Inited) { return; }
        EntityId = entityId;
        PivotPos = pivotPos;
        Inited = true;
    }

    public virtual void OnAwake()
    {
        //throw new System.NotImplementedException();   
        RuntimeRegisterUtility.RegisterAll(this);
    }
    public virtual void OnDestroy()
    {
        //throw new System.NotImplementedException();
        RuntimeRegisterUtility.UnregisterAll(this);
    }
}