using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EntityRuntime
{
    public int EntityId { get; protected set; }
    protected WorldState WorldState;

    public virtual void Init(int entityId, WorldState worldState)
    {
        EntityId = entityId;
        WorldState = worldState;
    }
    public virtual void OnAwake()
    {

    }
    public virtual void OnInteract()
    {

    }
    public virtual void OnTickUpdate()
    {

    }
    public virtual void OnMinuteUpdate()
    {

    }
    public virtual void OnDateUpdate(ComplexTime curTime)
    {

    } 
    public virtual void OnDestroy()
    { 

    }
    public virtual int GetID()
    {
        return EntityId;
    }
    public virtual void Save()
    {

    }//TODO：这玩意肯定不长这样，有参数
    public virtual void Load()
    {

    }//TODO：这玩意肯+定不长这样，有返回类型
}
