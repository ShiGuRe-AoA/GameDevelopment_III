using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Tilemaps;
using static UnityEditor.PlayerSettings;

public enum GridType
{
    Water,
    Soil,
    Farmland,
    Grass
}
public struct BasicCellData  //需要通过单元格检索的全部信息
{
    public GridType Type;
    public bool Empty;
    public static BasicCellData Create(GridType type = GridType.Soil)
    {
        bool empty;
        if(type == GridType.Water)
        {
            empty = false;
        }
        else
        {
            empty = true;
        }
        return new BasicCellData { Type = type, Empty = empty };
    }
}
public struct DetailedCellData
{
    public int EntityID;
    public float WaterTimeLast;   //含水时间（倒计时,单位，游戏分钟）
    public float FertTimeLast;  //肥料剩余时间（倒计时，单位，游戏分钟）
    public float FertRank;      //肥料等级
    public static DetailedCellData Create(int entityID = -1, float waterTime = 0, float fertTime = 0, float fertRank = 0)
    {
        return new DetailedCellData { EntityID = entityID, WaterTimeLast = waterTime, FertTimeLast = fertTime, FertRank = fertRank };
    }
}


public class WorldState : MonoBehaviour
{
    public Vector2Int MapSize;
    public Tilemap MainTile;
    public Tilemap OverlapTile;
    public Vector3 cellSize;
    
    public BasicCellData[] BasicMapData;  //全部地图信息，存建筑用ID
    public Dictionary<Vector3Int, DetailedCellData> DetailedMapData = new();    //详细地块信息


    private Dictionary<int, EntityRuntime> Entitys;  //当前正在运行的设备实例，只存有运行数据的

    private Dictionary<string, TileBase> TileDict;

    public ItemDatabaseSO ItemDatabase; //wup数据库
    public TileDatabaseSO TileDatabase; //wup数据库
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
        ItemRegistry.Init(ItemDatabase, TileDatabase);
    }
    public void InitMap(int lenth, int height)
    {
        BasicMapData = new BasicCellData[lenth * height];
        for (int i = 0; i < BasicMapData.Length; i++)
        {
            BasicMapData[i] = BasicCellData.Create();
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
        rt.Init(id, this);

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
    private int Index(int x,int y)//二维坐标映射一维ID
    {
        int index = y * MapSize.x + x;
        return index;
    }
    public bool CheckEmpty(Vector3Int cellPos)
    {
        int index = Index(cellPos.x, cellPos.y);
        if (index < 0 || index >= BasicMapData.Length)
        {
            //Debug.LogError($"Cell position out of bounds: {cellPos}");
            return false;
        }
        return BasicMapData[index].Empty;
    }
    public void PlaceEntity(Vector3Int cellPos, int itemID)//放置设施，包含实例注册
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
                if (index < 0 || index >= BasicMapData.Length)
                {
                    Debug.LogError($"Cell position out of bounds: {pos}");
                    continue;
                }


                Debug.LogError($"你这家伙，忘了写这块的TODO了！");
                //TODO：EntityID已列入DetailedCellData，填写加入哈希字典的逻辑
                ApplyDetailedMapData(pos, nextEntityId);

                //MapData[index].EntityID = nextEntityId;
                BasicMapData[index].Empty = false;
            }
        }
        RegisterEntity(nextEntityId, rt);
    }
    public void PlaceEntity(Vector3Int cellPos, string itemID)
    {
        var def = ItemRegistry.Get(itemID);
        if (def == null)
        {
            Debug.LogError($"Invalid item ID: {itemID}");
            return;
        }
        Feature_Placement feature = def.GetFeature<Feature_Placement>();
        if (feature == null)
        {
            return;
        }
        Vector3 worldPos = feature.GetCenterWorldFromPivot(cellPos);
        GameObject obj = Instantiate(feature.prefabObj, worldPos, Quaternion.identity);
        EntityRuntime rt = obj.GetComponent<EntityRuntime>();
        for (int i = 0; i < feature.Length; i++)
        {
            for (int j = 0; j < feature.Height; j++)
            {
                Vector3Int pos = cellPos + new Vector3Int(i, j, 0);
                int index = pos.y * MapSize.x + pos.x;
                if (index < 0 || index >= BasicMapData.Length)
                {
                    Debug.LogError($"Cell position out of bounds: {pos}");
                    continue;
                }
                //MapData[index].EntityID = nextEntityId;
                BasicMapData[index].Empty = false;
            }
        }
        RegisterEntity(nextEntityId, rt);
    }

    public void PlaceTile(Vector3Int cellPos, string tileID)
    {
        var thisTile = ItemRegistry.GetTile(tileID);
        OverlapTile.SetTile(cellPos, thisTile.Tile);
        ApplyDetailedMapData(cellPos, nextEntityId);
       // RegisterEntity(nextEntityId, rt);
    }
    public BasicCellData GetCell(Vector3Int target)
    {
        int index = Index(target.x, target.y);
        return BasicMapData[index];
    }


    //=========================================================================================
    //地图块拓展信息的创建与销毁
    public void ApplyDetailedMapData(List<Vector3Int> grids)
    {
        foreach(var grid in grids)
        {
            if (!DetailedMapData.ContainsKey(grid))
            {
                DetailedCellData newCell = DetailedCellData.Create();
                DetailedMapData.Add(grid, newCell);
            }
        }
    }
    public void ApplyDetailedMapData(Vector3Int grid)
    {
        if (!DetailedMapData.ContainsKey(grid))
        {
            DetailedCellData newCell = DetailedCellData.Create();
            DetailedMapData.Add(grid, newCell);
        }
    }
    public void ApplyDetailedMapData(List<Vector3Int> grids, int entityID)
    {
        foreach (var grid in grids)
        {
            if (!DetailedMapData.ContainsKey(grid))
            {
                DetailedCellData newCell = DetailedCellData.Create();
                newCell.EntityID = entityID;    
                DetailedMapData.Add(grid, newCell);
            }
        }
    }
    public void ApplyDetailedMapData(Vector3Int grid, int entityID)
    {
        if (!DetailedMapData.ContainsKey(grid))
        {
            DetailedCellData newCell = DetailedCellData.Create();
            newCell.EntityID = entityID;
            DetailedMapData.Add(grid, newCell);
        }
    }
    public void ReleaseDetailedMapData(List<Vector3Int> grids)
    {
        foreach(var grid in grids)
        {
            if (DetailedMapData.ContainsKey(grid))
            {
                DetailedMapData.Remove(grid);
            }
        }
    }
    //=========================================================================================




    public void ItemInteract(Vector3Int targetGridPos, List<ToolType> toolTypes)
    {
        BasicCellData cell = GetCell(targetGridPos);
        if (toolTypes.Contains(ToolType.Hoe))   //具备锄头属性
        {
            if(cell.Type == GridType.Soil && CheckEmpty(targetGridPos)) //地块检测
            {
                cell.Type = GridType.Farmland;

                PlaceTile(targetGridPos, "Farmland_Tile");//放置实体瓦片
            }
        }
    }
}
