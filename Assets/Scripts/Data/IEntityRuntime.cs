using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IEntityRuntime
{
    int EntityId { get; }
    Vector3Int PivotPos { get; }

    void EntityInit(int entityId, Vector3Int pivotPos, WorldState worldState);
    void OnAwake();

    void OnDestroy();
}
