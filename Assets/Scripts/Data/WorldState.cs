using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Tilemaps;

public struct CellData  //需要通过单元格检索的全部信息
{
    public int EntityID;
    public bool Empty;
    public CellData(int value)
    {
        EntityID = value;
        Empty = true;
    }

}

public class WorldState : MonoBehaviour
{
    public Vector2Int MapSize;
    public Tilemap MainTile;
    public Vector3 cellSize;
    
    public CellData[] MapData;  //全部地图信息，存建筑用ID
    private Dictionary<int, EntityRuntime> Entitys;  //当前正在运行的设备实例，只存有运行数据的

    public ItemDatabaseSO ItemDatabase; //wup数据库
    private static WorldState _instance;
    public static WorldState Instance 
    {
        get 
        { 
            if (_instance == null)
            {
                _instance = FindObjectOfType<WorldState>();
                if (_instance == null)
                {
                    Debug.LogError("No WorldState instance found in the scene!");
                }
            }
            return _instance;
        }
    }
    private int nextEntityId = 1; // 用于生成唯一的EntityID
    private void Awake()
    {
        // 如果已经存在实例
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        InitMap(MapSize.x, MapSize.y);
        ItemRegistry.Init(ItemDatabase);
    }
    public void InitMap(int lenth, int height)
    {
        MapData = new CellData[lenth * height];
        for (int i = 0; i < MapData.Length; i++)
        {
            MapData[i] = new CellData(-1);
        }
        cellSize = MainTile.cellSize;
    }
    private bool RegisterEntity(int id, EntityRuntime rt)//仅允许使用此方法注册物体实例，以确保ID唯一且正确设置反向引用
    {
        if (Entitys == null) Entitys = new Dictionary<int, EntityRuntime>();
        if (rt == null) return false;

        if (Entitys.ContainsKey(id))
        {
            Debug.LogError($"Duplicate EntityId: {id}");
            return false;
        }

        Entitys.Add(id, rt);

        // 反向赋值便于查找
        rt.EntityId = id;
        rt.WorldState = this;
        nextEntityId ++;

        return true;
    }
    public Vector3Int WorldToCell(Vector3 worldPos)
    {
        return MainTile.WorldToCell(worldPos);
    }
    public Vector3 CellToWorld(Vector3Int cellPos)
    {
        return MainTile.GetCellCenterWorld(cellPos);
    }
    public bool CheckEmpty(Vector3Int cellPos)
    {
        int index = cellPos.y * MapSize.x + cellPos.x;
        if (index < 0 || index >= MapData.Length)
        {
            //Debug.LogError($"Cell position out of bounds: {cellPos}");
            return false;
        }
        return MapData[index].Empty;
    }
    public void PlaceEntity(Vector3Int cellPos, int itemID)
    {
        var def = ItemRegistry.Get(itemID);
        if (def == null)
        {
            Debug.LogError($"Invalid item ID: {itemID}");
            return;
        }
        Feature_Placement feature = def.GetFeature<Feature_Placement>();
        if(feature == null)
        {
            return;
        }
        Vector3 worldPos = feature.GetCenterWorldFromPivot(cellPos);
        GameObject obj = Instantiate(feature.prefabObj, worldPos, Quaternion.identity);
        EntityRuntime rt = obj.GetComponent<EntityRuntime>();
        for(int i = 0; i < feature.Length; i++)
        {
            for(int j = 0; j < feature.Height; j++)
            {
                Vector3Int pos = cellPos + new Vector3Int(i, j, 0);
                int index = pos.y * MapSize.x + pos.x;
                if (index < 0 || index >= MapData.Length)
                {
                    Debug.LogError($"Cell position out of bounds: {pos}");
                    continue;
                }
                MapData[index].EntityID = nextEntityId;
                MapData[index].Empty = false;
            }
        }
        RegisterEntity(nextEntityId, rt);
    }
}
