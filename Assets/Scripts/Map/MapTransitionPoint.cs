using UnityEngine;

/// <summary>
/// 地图传送触发器。
/// 挂载在带有 Collider2D（IsTrigger = true）的 GameObject 上，
/// 玩家进入时触发传送到目标地图的指定出生点。
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class MapTransitionPoint : MonoBehaviour
{
    [Header("传送目标")]
    [SerializeField] private MapIdentity targetMap = MapIdentity.None;

    [Header("目标出生点（留空使用目标地图默认出生点）")]
    [SerializeField] private string spawnPointName = "";

    private void OnValidate()
    {
        // 确保 Collider2D 是 Trigger
        var col = GetComponent<Collider2D>();
        if (col != null && !col.isTrigger)
            col.isTrigger = true;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (targetMap == MapIdentity.None) return;

        if (other.CompareTag("Player"))
        {
            MapManager.Instance?.TransitionTo(targetMap, spawnPointName);
        }
    }
}
