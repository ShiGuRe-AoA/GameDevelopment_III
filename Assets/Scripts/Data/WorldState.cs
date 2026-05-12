using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public enum GridType
{
    Water,
    Soil,
    Farmland,
    Grass
}

public struct BasicCellData
{
    public GridType Type;
    public bool Empty;

    public static BasicCellData Create(GridType type = GridType.Soil)
    {
        bool empty = type != GridType.Water;
        return new BasicCellData { Type = type, Empty = empty };
    }
}

public class DetailedCellData
{
    public List<int> EntityID = new();
    public float WaterTimeLast;
    public float FertTimeLast;
    public float FertRank;

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
        if (EntityID == null)
        {
            EntityID = new List<int>();
        }

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
    [Header("钱")]
    public int coin;

    [Header("地图设置")]
    public Vector2Int MapSize;
    public Vector3 cellSize;

    [Header("瓦片层级")]
    public Tilemap MainTile;
    public Tilemap OverlapTile;
    public Tilemap UperTile;

    [Header("地图存储")]
    private BasicCellData[] BasicMapData;
    private readonly Dictionary<Vector3Int, DetailedCellData> DetailedMapData = new();
    private readonly Dictionary<int, List<Vector3Int>> entityOccupiedCells = new();
    public Dictionary<int, IEntityRuntime> Entitys { get; private set; } = new();

    [Header("数据引用")]
    public ItemDatabaseSO ItemDatabase;

    [Header("特殊引用")]
    [SerializeField] private TileBase farmlandTile;
    [SerializeField] private Transform playerTransform;
    [SerializeField] private GameObject DroppedItem_Prefab;

    [Header("暴露数据")]
    [SerializeField] private BackpackContainer backpackContainer_Mono;
    public ItemContainer backpackContainer => backpackContainer_Mono?.GetContainer();

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

    private int nextEntityId = 1;

    private void Awake()
    {
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

    private bool RegisterEntity(int id, Vector3Int pivotPos, IEntityRuntime rt)
    {
        if (Entitys == null)
        {
            Entitys = new Dictionary<int, IEntityRuntime>();
        }

        if (rt == null)
        {
            return false;
        }

        if (Entitys.ContainsKey(id))
        {
            Debug.LogError($"Duplicate EntityId: {id}");
            return false;
        }

        Entitys.Add(id, rt);
        rt.Init(id, pivotPos, this);
        nextEntityId++;
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

    private int Index(int x, int y)
    {
        return y * MapSize.x + x;
    }

    private bool TryGetIndex(Vector3Int cellPos, out int index)
    {
        index = -1;
        if (cellPos.x < 0 || cellPos.x >= MapSize.x || cellPos.y < 0 || cellPos.y >= MapSize.y)
        {
            return false;
        }

        index = Index(cellPos.x, cellPos.y);
        return index >= 0 && index < BasicMapData.Length;
    }

    private bool TryGetCellData(Vector3Int cellPos, out BasicCellData data)
    {
        data = default;
        if (!TryGetIndex(cellPos, out int index))
        {
            return false;
        }

        data = BasicMapData[index];
        return true;
    }

    private void SetCellData(Vector3Int cellPos, BasicCellData data)
    {
        if (!TryGetIndex(cellPos, out int index))
        {
            Debug.LogError($"Cell position out of bounds: {cellPos}");
            return;
        }

        BasicMapData[index] = data;
    }

    private void SetCellType(Vector3Int cellPos, GridType type)
    {
        if (!TryGetCellData(cellPos, out BasicCellData data))
        {
            Debug.LogError($"Cell position out of bounds: {cellPos}");
            return;
        }

        data.Type = type;
        SetCellData(cellPos, data);
    }

    private void SetCellEmpty(Vector3Int cellPos, bool empty)
    {
        if (!TryGetCellData(cellPos, out BasicCellData data))
        {
            Debug.LogError($"Cell position out of bounds: {cellPos}");
            return;
        }

        data.Empty = empty;
        SetCellData(cellPos, data);
    }

    private DetailedCellData GetOrCreateDetailedCell(Vector3Int grid)
    {
        if (!DetailedMapData.TryGetValue(grid, out DetailedCellData detail))
        {
            detail = new DetailedCellData();
            DetailedMapData.Add(grid, detail);
        }

        return detail;
    }

    private void AddEntityToDetailedMapData(Vector3Int grid, int entityID)
    {
        DetailedCellData detail = GetOrCreateDetailedCell(grid);
        detail.AddEntity(entityID);
    }

    private void TrackEntityCells(int entityID, List<Vector3Int> occupiedCells)
    {
        entityOccupiedCells[entityID] = new List<Vector3Int>(occupiedCells);
    }

    private bool IsDetailedStateEmpty(DetailedCellData detail)
    {
        return detail.WaterTimeLast <= 0f &&
               detail.FertTimeLast <= 0f &&
               Mathf.Approximately(detail.FertRank, 0f);
    }

    private void RefreshCellOccupancy(Vector3Int cellPos)
    {
        if (!TryGetCellData(cellPos, out BasicCellData data))
        {
            return;
        }

        bool hasBlockingEntity = DetailedMapData.TryGetValue(cellPos, out DetailedCellData detail) && !detail.CheckEmpty();
        data.Empty = !hasBlockingEntity && data.Type != GridType.Water;
        SetCellData(cellPos, data);
    }

    private void SyncDetailedStateWithRuntime(Vector3Int cellPos)
    {
        if (!DetailedMapData.TryGetValue(cellPos, out DetailedCellData detail))
        {
            return;
        }

        Farmland_Entity farmland = null;
        foreach (int entityID in detail.EntityID)
        {
            if (Entitys != null &&
                Entitys.TryGetValue(entityID, out IEntityRuntime runtime) &&
                runtime is Farmland_Entity foundFarmland)
            {
                farmland = foundFarmland;
                break;
            }
        }

        if (farmland == null)
        {
            detail.WaterTimeLast = 0f;
            detail.FertTimeLast = 0f;
            detail.FertRank = 0f;
            return;
        }

        detail.WaterTimeLast = farmland.WaterTimeLeft;
        detail.FertTimeLast = farmland.FertTimeLeft;
        detail.FertRank = farmland.FertRank;
    }

    public void RefreshDetailedState(Vector3Int cellPos)
    {
        SyncDetailedStateWithRuntime(cellPos);
    }

    private List<Vector3Int> BuildOccupiedCells(Vector3Int pivotPos, int length, int height)
    {
        List<Vector3Int> occupiedCells = new List<Vector3Int>(length * height);
        for (int x = 0; x < length; x++)
        {
            for (int y = 0; y < height; y++)
            {
                Vector3Int pos = pivotPos + new Vector3Int(x, y, 0);
                if (!TryGetIndex(pos, out _))
                {
                    Debug.LogError($"Cell position out of bounds: {pos}");
                    return null;
                }

                occupiedCells.Add(pos);
            }
        }

        return occupiedCells;
    }

    public bool CheckEmpty(Vector3Int cellPos)
    {
        if (!TryGetCellData(cellPos, out BasicCellData data))
        {
            return false;
        }

        return data.Empty;
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
        if (feature == null)
        {
            return;
        }

        Vector3 worldPos = feature.GetCenterWorldFromPivot(cellPos);
        GameObject obj = Instantiate(feature.prefabObj, worldPos, Quaternion.identity);
        IEntityRuntime rt = obj.GetComponent<IEntityRuntime>();
        if (rt == null)
        {
            Debug.LogError($"Placed object {feature.prefabObj.name} is missing IEntityRuntime.");
            Destroy(obj);
            return;
        }

        List<Vector3Int> occupiedCells = BuildOccupiedCells(cellPos, feature.Length, feature.Height);
        if (occupiedCells == null)
        {
            Destroy(obj);
            return;
        }

        foreach (Vector3Int pos in occupiedCells)
        {
            AddEntityToDetailedMapData(pos, nextEntityId);
            SetCellEmpty(pos, false);
        }

        TrackEntityCells(nextEntityId, occupiedCells);
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
        if (rt == null)
        {
            Debug.LogError($"Placed object {feature.prefabObj.name} is missing IEntityRuntime.");
            Destroy(obj);
            return;
        }

        List<Vector3Int> occupiedCells = BuildOccupiedCells(cellPos, feature.Length, feature.Height);
        if (occupiedCells == null)
        {
            Destroy(obj);
            return;
        }

        foreach (Vector3Int pos in occupiedCells)
        {
            AddEntityToDetailedMapData(pos, nextEntityId);
            SetCellEmpty(pos, false);
        }

        TrackEntityCells(nextEntityId, occupiedCells);
        RegisterEntity(nextEntityId, cellPos, rt);
    }

    public void PlaceTile(Vector3Int cellPos, TileBase tile, IEntityRuntime runtimeSc, int tileLayer, out int EntityID)
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

        if (!TryGetIndex(cellPos, out _))
        {
            Debug.LogError($"Cell position out of bounds: {cellPos}");
            EntityID = -1;
            return;
        }

        targetTilemap.SetTile(cellPos, tile);
        AddEntityToDetailedMapData(cellPos, nextEntityId);
        TrackEntityCells(nextEntityId, new List<Vector3Int> { cellPos });
        SetCellEmpty(cellPos, false);

        EntityID = nextEntityId;
        RegisterEntity(nextEntityId, cellPos, runtimeSc);
        SyncDetailedStateWithRuntime(cellPos);
    }

    public void SwitchTile(Vector3Int cellPos, TileBase tile, int tileLayer)
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
        if (!TryGetIndex(target, out int index))
        {
            hasDetail = false;
            detailedData = default;
            Debug.LogError($"Cell position out of bounds: {target}");
            return BasicCellData.Create(GridType.Water);
        }

        SyncDetailedStateWithRuntime(target);
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
        if (Entitys != null && Entitys.TryGetValue(entityID, out IEntityRuntime runtime))
        {
            return runtime;
        }

        Debug.LogError($"Entity ID not found: {entityID}");
        return null;
    }

    public bool TryGetEntityOnCell<T>(Vector3Int cellPos, out T target) where T : class, IEntityRuntime
    {
        target = null;
        if (!DetailedMapData.TryGetValue(cellPos, out DetailedCellData detail) || detail.CheckEmpty())
        {
            return false;
        }

        foreach (int entityID in detail.EntityID)
        {
            if (Entitys != null &&
                Entitys.TryGetValue(entityID, out IEntityRuntime runtime) &&
                runtime is T typedRuntime)
            {
                target = typedRuntime;
                return true;
            }
        }

        return false;
    }

    public void ApplyDetailedMapData(List<Vector3Int> grids)
    {
        foreach (Vector3Int grid in grids)
        {
            GetOrCreateDetailedCell(grid);
        }
    }

    public void ApplyDetailedMapData(Vector3Int grid)
    {
        GetOrCreateDetailedCell(grid);
    }

    public void ApplyDetailedMapData(List<Vector3Int> grids, int entityID)
    {
        foreach (Vector3Int grid in grids)
        {
            AddEntityToDetailedMapData(grid, entityID);
        }
    }

    public void ApplyDetailedMapData(Vector3Int grid, int entityID)
    {
        AddEntityToDetailedMapData(grid, entityID);
    }

    public void ReleaseDetailedMapData(List<Vector3Int> grids)
    {
        foreach (Vector3Int grid in grids)
        {
            if (DetailedMapData.TryGetValue(grid, out DetailedCellData detail) &&
                detail.CheckEmpty() &&
                IsDetailedStateEmpty(detail))
            {
                DetailedMapData.Remove(grid);
            }
        }
    }

    public void SpawnItem(Vector3Int gridPos, ItemStack stack, float randomOffset = 0.5f)
    {
        Vector3 dropPosition = CellToWorld(gridPos);
        Vector3 offset = new Vector3(
            Random.Range(-randomOffset, randomOffset),
            Random.Range(-randomOffset, randomOffset),
            0f
        );
        GameObject newDrop = Instantiate(DroppedItem_Prefab, dropPosition + offset, Quaternion.identity);
        DroppedItem dropScript = newDrop.GetComponent<DroppedItem>();
        dropScript.Init(stack);
    }

    public void SpawnItem(Vector3Int gridPos, int itemID, int count, float randomOffset = 0.5f)
    {
        for (int i = 0; i < count; i++)
        {
            Vector3 dropPosition = CellToWorld(gridPos);
            Vector3 offset = new Vector3(
                Random.Range(-randomOffset, randomOffset),
                Random.Range(-randomOffset, randomOffset),
                0f
            );
            GameObject newDrop = Instantiate(DroppedItem_Prefab, dropPosition + offset, Quaternion.identity);
            DroppedItem dropScript = newDrop.GetComponent<DroppedItem>();
            dropScript.Init(new ItemStack { count = count, itemId = itemID });
        }
    }

    public float PlayerDist(Vector3 pos)
    {
        Vector3 playerPos = playerTransform.position;
        return (playerPos - pos).magnitude;
    }

    public Vector3 PlayerPos()
    {
        return playerTransform.position;
    }

    public void DestroyEntity(int entityID)
    {
        IEntityRuntime entityRuntime = GetEntity(entityID);
        if (entityRuntime == null)
        {
            return;
        }

        RuntimeRegisterUtility.UnregisterAll(entityRuntime);
        Entitys.Remove(entityID);

        if (!entityOccupiedCells.TryGetValue(entityID, out List<Vector3Int> occupiedCells))
        {
            occupiedCells = new List<Vector3Int> { entityRuntime.PivotPos };
        }
        entityOccupiedCells.Remove(entityID);

        foreach (Vector3Int occupiedCell in occupiedCells)
        {
            if (!DetailedMapData.TryGetValue(occupiedCell, out DetailedCellData detail))
            {
                continue;
            }

            detail.EntityID.Remove(entityID);
            SyncDetailedStateWithRuntime(occupiedCell);

            if (detail.CheckEmpty() && IsDetailedStateEmpty(detail))
            {
                DetailedMapData.Remove(occupiedCell);
            }

            RefreshCellOccupancy(occupiedCell);
        }

        if (entityRuntime is Component component)
        {
            Destroy(component.gameObject);
        }
    }

    public void InteractAt(Vector3Int interactPos)
    {
        if (!DetailedMapData.ContainsKey(interactPos))
        {
            return;
        }

        DetailedCellData cellData = DetailedMapData[interactPos];
        List<int> entityIdsSnapshot = new List<int>(cellData.EntityID);
        foreach (int entityID in entityIdsSnapshot)
        {
            IEntityRuntime entity = GetEntity(entityID);
            if (entity is IInteractable interactable)
            {
                interactable.OnInteract();
            }
        }
    }

    public InteractPhase DetectInteract(Vector3Int interactPos)
    {
        if (!DetailedMapData.ContainsKey(interactPos))
        {
            return InteractPhase.None;
        }

        DetailedCellData cellData = DetailedMapData[interactPos];
        List<int> entityIdsSnapshot = new List<int>(cellData.EntityID);
        foreach (int entityID in entityIdsSnapshot)
        {
            IEntityRuntime entity = GetEntity(entityID);
            if (entity is IInteractable interactable)
            {
                return interactable.OnInteractDetected();
            }
        }

        return InteractPhase.None;
    }

    public void ItemInteract(Vector3Int targetGridPos, List<ToolType> toolTypes)
    {
        BasicCellData cell = GetCell(targetGridPos, out bool hasDetail, out DetailedCellData detailedData);
        if (toolTypes.Contains(ToolType.Hoe))
        {
            if (cell.Type == GridType.Soil && CheckEmpty(targetGridPos))
            {
                SetCellType(targetGridPos, GridType.Farmland);
                SetCellEmpty(targetGridPos, false);

                Farmland_Entity entity = (Farmland_Entity)EntityRuntimeFactory.Create(EntityRuntimeKind.Farmland);
                entity.Init(targetGridPos);
                Debug.Log("FarmlandPlaced!");
                PlaceTile(targetGridPos, farmlandTile, entity, 1, out _);
            }
        }

        if (toolTypes.Contains(ToolType.WateringCan))
        {
            if (!hasDetail || detailedData.CheckEmpty())
            {
                return;
            }

            if (!TryGetEntityOnCell(targetGridPos, out Farmland_Entity farmland))
            {
                return;
            }

            farmland.Water();
            SyncDetailedStateWithRuntime(targetGridPos);
            Debug.Log("FarmlandWatered!");
        }
    }
}
