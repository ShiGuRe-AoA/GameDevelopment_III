using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TreeInitializer : MonoBehaviour
{
    void Start()
    {
        IEntityRuntime entityRuntime = EntityRuntimeFactory.Create(EntityRuntimeKind.Tree);
        Vector3Int pivotPos = WorldState.Instance.WorldToCell(transform.position);

        WorldState.Instance.PlaceEntity(pivotPos, entityRuntime, 1, 1);
    }
}
