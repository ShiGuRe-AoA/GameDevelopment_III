using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

[CreateAssetMenu(menuName = "Game/DataBase/Tile Database")]
public class TileDatabaseSO : ScriptableObject
{
    public List<TileBase_SO> tiles = new();

    // 运行时缓存（不序列化）
    private Dictionary<int, TileBase_SO> _byId;
    private Dictionary<string, TileBase_SO> _byStringId;

    public void BuildCache()
    {
        _byId = new Dictionary<int, TileBase_SO>(tiles.Count);
        _byStringId = new Dictionary<string, TileBase_SO>(tiles.Count);

        foreach (var tile in tiles)
        {
            if (tile == null) continue;

            if (tile.ID_Num <= 0)
                Debug.LogError($"Tile stableId invalid: {tile.name}", tile);

            if (_byId.ContainsKey(tile.ID_Num))
                Debug.LogError($"Duplicate stableId {tile.ID_Num}: {tile.name}", tile);
            else
                _byId.Add(tile.ID_Num, tile);

            if (!string.IsNullOrEmpty(tile.ID_Str))
            {
                if (_byStringId.ContainsKey(tile.ID_Str))
                    Debug.LogError($"Duplicate stringId {tile.ID_Str}: {tile.name}", tile);
                else
                    _byStringId.Add(tile.ID_Str, tile);
            }
        }
    }

    public TileBase_SO Get(int stableId)
    {
        if (_byId == null) BuildCache();
        return _byId.TryGetValue(stableId, out var tile) ? tile : null;
    }

    public TileBase_SO Get(string stringId)
    {
        if (_byStringId == null) BuildCache();
        return _byStringId.TryGetValue(stringId, out var tile) ? tile : null;
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        // 编辑器检测重复ID
        _byId = null;
        _byStringId = null;
    }
#endif
}