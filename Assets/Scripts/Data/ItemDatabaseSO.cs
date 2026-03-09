using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Game/Item Database")]
public class ItemDatabaseSO : ScriptableObject
{
    public List<ItemBase_SO> items = new();

    // 运行时缓存（不序列化）
    private Dictionary<int, ItemBase_SO> _byId;
    private Dictionary<string, ItemBase_SO> _byStringId;

    public void BuildCache()
    {
        _byId = new Dictionary<int, ItemBase_SO>(items.Count);
        _byStringId = new Dictionary<string, ItemBase_SO>(items.Count);

        foreach (var it in items)
        {
            if (it == null) continue;

            if (it.ID_num <= 0)
                Debug.LogError($"Item stableId invalid: {it.name}", it);

            if (_byId.ContainsKey(it.ID_num))
                Debug.LogError($"Duplicate stableId {it.ID_num}: {it.name}", it);
            else
                _byId.Add(it.ID_num, it);

            if (!string.IsNullOrEmpty(it.ID_str))
            {
                if (_byStringId.ContainsKey(it.ID_str))
                    Debug.LogError($"Duplicate stringId {it.ID_str}: {it.name}", it);
                else
                    _byStringId.Add(it.ID_str, it);
            }
        }
    }

    public ItemBase_SO Get(int stableId)
    {
        if (_byId == null) BuildCache();
        return _byId.TryGetValue(stableId, out var it) ? it : null;
    }

    public ItemBase_SO Get(string stringId)
    {
        if (_byStringId == null) BuildCache();
        return _byStringId.TryGetValue(stringId, out var it) ? it : null;
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        // 在编辑器里尽早发现重复ID（可选）
        // 注意：OnValidate 可能频繁触发，不要写太重的逻辑
        _byId = null;
        _byStringId = null;
    }
#endif
}