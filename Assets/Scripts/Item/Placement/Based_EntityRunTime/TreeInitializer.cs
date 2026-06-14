using UnityEngine;

public class TreeInitializer : MonoBehaviour
{
    void Start()
    {
        IEntityRuntime entityRuntime = EntityRuntimeFactory.Create(EntityRuntimeKind.Tree);
        Vector3Int pivotPos = WorldState.Instance.WorldToCell(transform.position);

        // 将树的 Transform（锚点在根部）注入给 Tree_Entity，供倒下动画使用
        if (entityRuntime is Tree_Entity treeEntity)
            treeEntity.TreeTransform = transform;

        WorldState.Instance.PlaceEntity(pivotPos, entityRuntime, 1, 1);
    }
}
