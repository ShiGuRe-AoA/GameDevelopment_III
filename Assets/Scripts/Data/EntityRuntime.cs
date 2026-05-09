using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IInteractable
{
    public void OnInteract();
    public InteractPhase OnInteractDetected();
}
public class EntityRuntime
{
    public int EntityId { get; protected set; }
    public Vector3Int PivotPos { get; protected set; }

    public virtual void Init(int entityId,Vector3Int pivotPos, WorldState worldState)
    {
        EntityId = entityId;
        PivotPos = pivotPos;
    }
    public virtual void OnAwake()
    {

    }
    public virtual void OnTickUpdate(float deltaTime)
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
