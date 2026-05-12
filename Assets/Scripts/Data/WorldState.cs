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
public class DetailedCellData
{
    public List<int> EntityID = new();
    public float WaterTimeLast;   //含水时间（倒计时,单位，游戏分钟）
    public float FertTimeLast;  //肥料剩余时间（倒计时，单位，游戏分钟）
    public float FertRank;      //肥料等级
    public DetailedCellData(int entityID = -1, float waterTime = 0, float fertTime = 0, float fertRank = 0)
    {
        WaterTimeLast = waterTime;
        FertTimeLast = fertTime;
        FertRank = fertRank;

        if (entityID != -1)
        {
            EntityID.Add(entityID);
        }
    }

    public void AddEntity(int entityID)
    {
        if (EntityID == null) EntityID = new List<int>();
        if (!EntityID.Contains(entityID))
        {
            EntityID.Add(entityID);
        }
    }   

    public bool CheckEmpty()
    {
        return EntityID == null || EntityID.Count == 0;
    }
}


public class WorldState : MonoBehaviour
{
    //========================= 暂用
    [Header("钱")]
    public int coin;
    //=========================

    [Header("地图设置")]
    public Vector2Int MapSize;
    public Vector3 cellSize;

    [Header("瓦片层级")]
    public Tilemap MainTile;
    public Tilemap OverlapTile;
    public Tilemap UperTile;

    [Header("地图存储")]
    private BasicCellData[] BasicMapData;  //全部地图信息，存建筑用ID
    private Dictionary<Vector3Int, DetailedCellData> DetailedMapData = new();    //详细地块信息
    public Dictionary<int, IEntityRuntime> Entitys { get; private set; } = new();  //当前正在运行的设备实例，只存有运行数据的

    [Header("数据引用")]     
    public ItemDatabaseSO ItemDatabase; //wup数据库

    [Header("特殊引用")]     
    [SerializeField] private TileBase farmlandTile;
    [SerializeField] private Transform playerTransform;
    [SerializeField] private GameObject DroppedItem_Prefab;


    [Header("暴露数据")]
    [SerializeField] private BackpackContainer backpackContainer_Mono;
    public ItemContainer backpackContainer 
    {
        get { return backpackContainer_Mono?.GetContainer(); }
    }

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
        BasicMapData = new BasicCellData[lenth * height];
        for (int i = 0; i < BasicMapData.Length; i++)
        {
            BasicMapData[i] = BasicCellData.Create();
        }
        cellSize = MainTile.cellSize;
        Debug.Log("MapInitComplete");
    }
    private bool RegisterEntity(int id,Vector3Int pivotPos, IEntityRuntime rt)//仅允许使用此方法注册物体实例，以确保ID唯一且正确设置反向引用
    {
        if (Entitys == null) Entitys = new Dictionary<int, IEntityRuntime>();
        if (rt == null) return false;

        if (Entitys.ContainsKey(id))
        {
            Debug.LogError($"Duplicate EntityId: {id}");
            return false;
        }

        Entitys.Add(id, rt);

        // 反向赋值便于查找
        rt.Init(id, pivotPos, this);

        nextEntityId ++;
        Debug.Log($"EntityRegisteredInID:{id}");
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
        IEntityRuntime rt = obj.GetComponent<IEntityRuntime>();
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
        RegisterEntity(nextEntityId, cellPos, rt);
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
        IEntityRuntime rt = obj.GetComponent<IEntityRuntime>();
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
        RegisterEntity(nextEntityId, cellPos, rt);
    }

    public void PlaceTile(Vector3Int cellPos, TileBase tile,IEntityRuntime runtimeSc,int tileLayer, out int EntityID)
    {
        Tilemap targetTilemap;
        switch (tileLayer)
        {
            case 0:
                targetTilemap = MainTile;
                break;
            case 1:
                targetTilemap = OverlapTile;
                break;
            case 2:
                targetTilemap = UperTile;
                break;
            default:
                Debug.LogError($"Invalid tile layer: {tileLayer}");
                EntityID = -1;
                return;
        }

        targetTilemap.SetTile(cellPos, tile);
        if (!DetailedMapData.ContainsKey(cellPos)) { ApplyDetailedMapData(cellPos, nextEntityId); }
        EntityID = nextEntityId;
        RegisterEntity(nextEntityId, cellPos, runtimeSc);
    }
    public void SwitchTile(Vector3Int cellPos, TileBase tile,int tileLayer)
    {
        Tilemap targetTilemap;
        switch (tileLayer)
        {
            case 0:
                targetTilemap = MainTile;
                break;
            case 1:
                targetTilemap = OverlapTile;
                break;
            case 2:
                targetTilemap = UperTile;
                break;
            default:
                Debug.LogError($"Invalid tile layer: {tileLayer}");
                return;
        }
        targetTilemap.SetTile(cellPos, tile);
    }
    public BasicCellData GetCell(Vector3Int target, out bool hasDetail, out DetailedCellData detailedData)
    {
        int index = Index(target.x, target.y);

        if (DetailedMapData.TryGetValue(target, out detailedData))
        {
            hasDetail = true;
        }
        else
        {
            hasDetail = false;
            detailedData = default;
        }

        return BasicMapData[index];
    }
    public IEntityRuntime GetEntity(int entityID)
    {
        if (Entitys != null && Entitys.TryGetValue(entityID, out var runtime))
        {
            return runtime;
        }
        Debug.LogError($"Entity ID not found: {entityID}");
        return null;
    }

    //=========================================================================================
    //地图块拓展信息的创建与销毁
    public void ApplyDetailedMapData(List<Vector3Int> grids)
    {
        foreach(var grid in grids)
        {
            if (!DetailedMapData.ContainsKey(grid))
            {
                DetailedCellData newCell = new DetailedCellData();
                DetailedMapData.Add(grid, newCell);
            }
        }
    }
    public void ApplyDetailedMapData(Vector3Int grid)
    {
        if (!DetailedMapData.ContainsKey(grid))
        {
            DetailedCellData newCell = new DetailedCellData();
            DetailedMapData.Add(grid, newCell);
        }
    }
    public void ApplyDetailedMapData(List<Vector3Int> grids, int entityID)
    {
        foreach (var grid in grids)
        {
            if (!DetailedMapData.ContainsKey(grid))
            {
                DetailedCellData newCell = new DetailedCellData(entityID);
                DetailedMapData.Add(grid, newCell);
            }
        }
    }
    public void ApplyDetailedMapData(Vector3Int grid, int entityID)
    {
        if (!DetailedMapData.ContainsKey(grid))
        {
            DetailedCellData newCell = new DetailedCellData(entityID);
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
    //掉落物生成
    public void SpawnItem(Vector3Int gridPos, ItemStack stack, float randomOffset = 0.5f)
    {
        Vector3 dropPosition = CellToWorld(gridPos);
        Vector3 offset = new Vector3(Random.Range(-randomOffset, randomOffset), Random.Range(-randomOffset, randomOffset), 0);
        GameObject newDrop = Instantiate(DroppedItem_Prefab, dropPosition + offset, Quaternion.identity);
        DroppedItem dropScript = newDrop.GetComponent<DroppedItem>();
        dropScript.Init(stack);
    }
    public void SpawnItem(Vector3Int gridPos, int itemID, int count, float randomOffset = 0.5f)
    {
        for (int i = 0; i < count; i++) 
        { 
            Vector3 dropPosition = CellToWorld(gridPos);
            Vector3 offset = new Vector3(Random.Range(-randomOffset, randomOffset), Random.Range(-randomOffset, randomOffset), 0);
            GameObject newDrop = Instantiate(DroppedItem_Prefab, dropPosition + offset, Quaternion.identity);
            DroppedItem dropScript = newDrop.GetComponent<DroppedItem>();
            dropScript.Init(new ItemStack { count = count, itemId = itemID});
        }
    }



    //=========================================================================================
    //玩家相关
    public float PlayerDist(Vector3 pos)
    {
        Vector3 playerPos = playerTransform.position;
        return ((playerPos - pos).magnitude);
    }

    public Vector3 PlayerPos()
    {
        return playerTransform.position;
    }

    //=========================================================================================
    //生命周期
    public void DestroyEntity(int entityID)
    {
        IEntityRuntime entityRuntime = GetEntity(entityID);
        Vector3Int pivotPos = entityRuntime.PivotPos;

        Entitys.Remove(entityID);

        if (DetailedMapData.ContainsKey(pivotPos))
        {
            DetailedMapData[pivotPos].EntityID.Remove(entityID);
            if (DetailedMapData[pivotPos].CheckEmpty())
            {
                DetailedMapData.Remove(pivotPos);
            }
        }
    }
    public void InteractAt(Vector3Int interactPos)
    {
;
        if (!DetailedMapData.ContainsKey(interactPos)){ return; }

        DetailedCellData cellData = DetailedMapData[interactPos];
        foreach(var entityID in cellData.EntityID)
        {
            IEntityRuntime entity = GetEntity(entityID);
            if (entity != null)
            {
                if(entity is IInteractable interactable)
                {
                    interactable.OnInteract();
                }
            }
        }
    }
    public InteractPhase DetectInteract(Vector3Int interactPos)
    {
        if (!DetailedMapData.ContainsKey(interactPos)) { return InteractPhase.None; }

        DetailedCellData cellData = DetailedMapData[interactPos];
        foreach (var entityID in cellData.EntityID)
        {
            IEntityRuntime entity = GetEntity(entityID);
            if (entity != null)
            {
                if (entity is IInteractable interactable)
                {
                    return interactable.OnInteractDetected();
                }
            }
        }
        return InteractPhase.None;
    }
    //=========================================================================================






    public void ItemInteract(Vector3Int targetGridPos, List<ToolType> toolTypes)
    {
        BasicCellData cell = GetCell(targetGridPos, out bool hasDetail, out DetailedCellData detailedData);
        if (toolTypes.Contains(ToolType.Hoe))   //具备锄头属性
        {
            if(cell.Type == GridType.Soil && CheckEmpty(targetGridPos)) //地块检测
            {
                cell.Type = GridType.Farmland;
                Farmland_Entity entity = (Farmland_Entity)EntityRuntimeFactory.Create(EntityRuntimeKind.Farmland);
                entity.Init(targetGridPos);
                Debug.Log("FarmlandPlaced!");
                PlaceTile(targetGridPos, farmlandTile, entity,1, out int ID);//放置实体瓦片
            }
        }
        if (toolTypes.Contains(ToolType.WateringCan))
        {
            if (!hasDetail) { return; }

            if (!detailedData.CheckEmpty()) //有实体
            {
                foreach (var entityID in detailedData.EntityID)
                { 
                    IEntityRuntime entity = GetEntity(entityID);
                    if (entity is Farmland_Entity farmland)
                    {
                        farmland.Water();
                        Debug.Log("FarmlandWatered!");
                        DetailedMapData[targetGridPos] = detailedData; //更新详细数据
                    }
                }
            }
        }
    }
}
