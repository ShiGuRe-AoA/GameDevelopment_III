using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Game/DataBase/Item Database")]
public class ItemDatabaseSO : ScriptableObject
{
    public List<ItemBase_SO> items = new();

    // ����ʱ���棨�����л���
    private Dictionary<int, ItemBase_SO> _byId;
    private Dictionary<string, ItemBase_SO> _byStringId;

    public void Init()
    {
        Debug.Log($"Initializing ItemsData");
        foreach (var it in items)
        {
            if (it != null)
                it.Init();
        }
    }
    public void BuildCache()
    {
        Init();

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
        // 在编辑器内若发生重复ID则丢弃缓存
        // 注意：OnValidate 调用频繁，不要写太重的逻辑
        _byId = null;
        _byStringId = null;
    }

    /// <summary>清空列表并自动查找 Assets/Data 目录下所有 ItemBase_SO 加入列表</summary>
    [ContextMenu("Auto Collect Items")]
    private void AutoCollectItems()
    {
        items.Clear();
        string[] guids = UnityEditor.AssetDatabase.FindAssets("t:ItemBase_SO", new[] { "Assets/Data" });
        foreach (string guid in guids)
        {
            string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
            var so = UnityEditor.AssetDatabase.LoadAssetAtPath<ItemBase_SO>(path);
            if (so != null)
                items.Add(so);
        }
        UnityEditor.EditorUtility.SetDirty(this);
        // 清掉缓存让下次 Get 时重建
        _byId = null;
        _byStringId = null;
        Debug.Log($"[ItemDatabaseSO] Collected {items.Count} ItemBase_SO from Assets/Data");
    }
#endif
}