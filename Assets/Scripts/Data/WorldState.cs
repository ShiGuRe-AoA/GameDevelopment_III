using System;
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
    [SerializeField] private int _coin;
    public int coin
    {
        get => _coin;
        set
        {
            if (_coin == value) return;
            _coin = value;
            OnCoinChanged?.Invoke(_coin);
        }
    }

    /// <summary>金币变化事件（参数为新的金币值）</summary>
    public static event Action<int> OnCoinChanged;

    /// <summary>尝试扣除金币。成功返回 true，金币不足返回 false</summary>
    public bool TrySpendCoin(int amount)
    {
        if (coin < amount) return false;
        coin -= amount;
        return true;
    }

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
    public Dictionary<int, IWorldObject> WorldObjs { get; private set; } = new();

    [Header("数据引用")]
    public ItemDatabaseSO ItemDatabase;

    [Header("特殊引用")]
    [SerializeField] private TileBase farmlandTile;
    [SerializeField] private Transform playerTransform;
    public Transform PlayerTransform => playerTransform;
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
    private int nextObjId = 1;

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
        coin = 500;
        BasicMapData = new BasicCellData[lenth * height];
        for (int i = 0; i < BasicMapData.Length; i++)
        {
            BasicMapData[i] = BasicCellData.Create();
        }

        cellSize = MainTile.cellSize;
        Debug.Log("MapInitComplete");
    }

    public bool RegisterEntity(int id, Vector3Int pivotPos, IEntityRuntime rt)
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
        rt.EntityInit(id, pivotPos, this);
        rt.OnAwake();
        nextEntityId++;
        Debug.Log($"EntityRegisteredInID:{id}");
        return true;
    }
    public bool RegisterEntity(object obj)
    {
        return obj is IEntityRuntime runtime && RegisterEntity(runtime.EntityId, runtime.PivotPos, runtime);
    }
    public bool UnRegisterEntity(int id)
    {
        if (Entitys.ContainsKey(id))
        {
            Entitys.Remove(id);
            return true;
        }
        return false;
    }
    public bool UnRegisterEntity(object obj)
    {
        return obj is IEntityRuntime runtime && UnRegisterEntity(runtime.EntityId);
    }

    public bool RegisterWorldObject(int id, Vector3 worldPos, IWorldObject obj)
    {
        if(WorldObjs == null)
        {
            WorldObjs = new Dictionary<int, IWorldObject>();
        }
        
        if(obj == null)
        {
            return false;
        }

        if (WorldObjs.ContainsKey(id))
        {
            Debug.LogError($"Duplicate ObjectId: {id}");
            return false;
        }

        WorldObjs.Add(id, obj);
        obj.ObjectInit(id, worldPos, this);
        nextObjId++;
        Debug.Log($"ObjRegisteredInID:{id}");
        return true;
    }

    public bool RegisterWorldObject(object obj)
    {
        return obj is IWorldObject worldObj && RegisterWorldObject(worldObj.ObjectId, worldObj.WorldPos, worldObj);
    }

    public bool UnRegisterWorldObject(int id)
    {
        if (WorldObjs.ContainsKey(id))
        {
            WorldObjs.Remove(id);
            return true;
        }
        return false;
    }

    public bool UnRegisterWorldObject(object obj)
    {
        return obj is IWorldObject worldObj && UnRegisterWorldObject(worldObj.ObjectId);
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

    private GameObject InstantiatePlacementFromFeature(Feature_Placement feature, Vector3Int cellPos, out IEntityRuntime rt)
    {
        Vector3 worldPos = feature.GetCenterWorldFromPivot(cellPos);
        GameObject obj = Instantiate(feature.prefabObj, worldPos, Quaternion.identity);
        rt = obj.GetComponent<IEntityRuntime>();
        if (rt == null)
        {
            Debug.LogError($"Placed object {feature.prefabObj.name} is missing IEntityRuntime.");
            Destroy(obj);
            return null;
        }
        return obj;
    }

    public void PlaceEntity(Vector3Int cellPos, IEntityRuntime rt, int length, int height)
    {
        if (rt == null)
        {
            Debug.LogError("Placed object is missing IEntityRuntime.");
            return;
        }

        List<Vector3Int> occupiedCells = BuildOccupiedCells(cellPos, length, height);
        if (occupiedCells == null) return;

        foreach (Vector3Int pos in occupiedCells)
        {
            AddEntityToDetailedMapData(pos, nextEntityId);
            SetCellEmpty(pos, false);
        }

        TrackEntityCells(nextEntityId, occupiedCells);
        RegisterEntity(nextEntityId, cellPos, rt);
    }

    private void PlaceEntityFromFeature(Vector3Int cellPos, Feature_Placement feature)
    {
        GameObject obj = InstantiatePlacementFromFeature(feature, cellPos, out IEntityRuntime rt);
        if (obj == null) return;

        List<Vector3Int> occupiedCells = BuildOccupiedCells(cellPos, feature.Length, feature.Height);
        if (occupiedCells == null) { Destroy(obj); return; }

        foreach (Vector3Int pos in occupiedCells)
        {
            AddEntityToDetailedMapData(pos, nextEntityId);
            SetCellEmpty(pos, false);
        }

        TrackEntityCells(nextEntityId, occupiedCells);
        RegisterEntity(nextEntityId, cellPos, rt);
    }

    public void PlaceEntity(Vector3Int cellPos, int itemID)
    {
        var def = ItemRegistry.Get(itemID);
        if (def == null) { Debug.LogError($"Invalid item ID: {itemID}"); return; }

        var feature = def.GetFeature<Feature_Placement>();
        if (feature == null) return;

        PlaceEntityFromFeature(cellPos, feature);
    }

    public void PlaceEntity(Vector3Int cellPos, string itemID)
    {
        var def = ItemRegistry.Get(itemID);
        if (def == null) { Debug.LogError($"Invalid item ID: {itemID}"); return; }

        var feature = def.GetFeature<Feature_Placement>();
        if (feature == null) return;

        PlaceEntityFromFeature(cellPos, feature);
    }

    private Tilemap GetTilemap(int layer)
    {
        switch (layer)
        {
            case 0: return MainTile;
            case 1: return OverlapTile;
            case 2: return UperTile;
            default:
                Debug.LogError($"Invalid tile layer: {layer}");
                return null;
        }
    }

    public void PlaceTile(Vector3Int cellPos, TileBase tile, IEntityRuntime runtimeSc, int tileLayer, out int EntityID)
    {
        Tilemap targetTilemap = GetTilemap(tileLayer);
        if (targetTilemap == null) { EntityID = -1; return; }

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
        Tilemap targetTilemap = GetTilemap(tileLayer);
        if (targetTilemap == null) return;

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

    private Vector3 GetRandomDropOffset(float randomOffset)
    {
        return new Vector3(
            UnityEngine.Random.Range(-randomOffset, randomOffset),
            UnityEngine.Random.Range(-randomOffset, randomOffset),
            0f
        );
    }

    private void SpawnDrop(Vector3 worldPos, ItemStack stack)
    {
        GameObject newDrop = Instantiate(DroppedItem_Prefab, worldPos, Quaternion.identity);
        newDrop.GetComponent<DroppedItem>().Init(stack);
    }

    public void SpawnItem(Vector3Int gridPos, ItemStack stack, float randomOffset = 0.5f)
    {
        Vector3 pos = CellToWorld(gridPos) + GetRandomDropOffset(randomOffset);
        SpawnDrop(pos, stack);
    }

    public void SpawnItem(Vector3Int gridPos, int itemID, int count, float randomOffset = 0.5f)
    {
        for (int i = 0; i < count; i++)
        {
            Vector3 pos = CellToWorld(gridPos) + GetRandomDropOffset(randomOffset);
            SpawnDrop(pos, new ItemStack { itemId = itemID, count = 1 });
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

    private List<int> GetEntityIdsOnCell(Vector3Int cellPos)
    {
        if (!DetailedMapData.TryGetValue(cellPos, out DetailedCellData cellData))
            return null;
        return new List<int>(cellData.EntityID); // snapshot to avoid mutation during iteration
    }

    // for common Interact -- Harvest/OpenDoor/...
    public void Interact(Vector3Int interactPos)
    {
        List<int> entityIds = GetEntityIdsOnCell(interactPos);
        if (entityIds == null) return;

        foreach (int entityID in entityIds)
        {
            TryInteractObject(GetEntity(entityID));
        }
    }

    public void Interact(IWorldObject obj)
    {
        TryInteractObject(obj);
    }

    public InteractPhase DetectInteract(Vector3Int interactPos)
    {
        List<int> entityIds = GetEntityIdsOnCell(interactPos);
        if (entityIds == null) return InteractPhase.None;

        foreach (int entityID in entityIds)
        {
            InteractPhase phase = TryDetectInteractObject(GetEntity(entityID));

            if (phase != InteractPhase.None)
                return phase;
        }

        return InteractPhase.None;
    }

    public InteractPhase DetectInteract(IWorldObject obj)
    {
        return TryDetectInteractObject(obj);
    }

    private void TryInteractObject(object obj)
    {
        if (obj is IInteractable interactable)
            interactable.OnInteract();
        if (obj is IEntityInteractable entityInteractable)
            entityInteractable.OnEntityInteract();
    }

    private InteractPhase TryDetectInteractObject(object obj)
    {
        if (obj is IInteractable interactable)
            return interactable.OnInteractDetected();
        if (obj is IEntityInteractable entityInteractable)
            return entityInteractable.OnEntityInteractDetected();
        return InteractPhase.None;
    }

    // for Entity UI Interact -- Open Panel/...
    public void EntityInteractAt(Vector3Int interactPos)
    {
        List<int> entityIds = GetEntityIdsOnCell(interactPos);
        if (entityIds == null) return;

        foreach (int entityID in entityIds)
        {
            if (GetEntity(entityID) is IEntityInteractable interactable)
                interactable.OnEntityInteract();
        }
    }
    public InteractPhase EntityDetectInteract(Vector3Int interactPos)
    {
        List<int> entityIds = GetEntityIdsOnCell(interactPos);
        if (entityIds == null) return InteractPhase.None;

        foreach (int entityID in entityIds)
        {
            if (GetEntity(entityID) is IEntityInteractable interactable)
                return interactable.OnEntityInteractDetected();
        }

        return InteractPhase.None;
    }

    private IHoverTarget currentHoverTarget;
    public void Hover(IHoverTarget target = null)
    {
        if (target == currentHoverTarget)
            return;

        currentHoverTarget?.OnHoverExit();

        currentHoverTarget = target;

        currentHoverTarget?.OnHoverEnter();
    }

    public void ItemInteract(Vector3Int targetGridPos, List<ToolType> toolTypes, PlayerContext ctx)
    {
        BasicCellData cell = GetCell(targetGridPos, out bool hasDetail, out DetailedCellData detailedData);

        if (toolTypes.Contains(ToolType.Hoe))
            TryHoeTile(targetGridPos, cell);

        if (toolTypes.Contains(ToolType.WateringCan))
            TryWaterTile(targetGridPos, hasDetail, detailedData);

        if (toolTypes.Contains(ToolType.Axe))
            TryLogTile(targetGridPos, hasDetail, detailedData);

        if (toolTypes.Contains(ToolType.FishingRod) && cell.Type == GridType.Water)
            FishingSystem.Instance.BeginFishing(ctx.PlayerController);

        if (toolTypes.Contains(ToolType.Bell))
            CustomerAttractSystem.Instance.AttractCustomers(ctx);
    }

    private void TryHoeTile(Vector3Int cellPos, BasicCellData cell)
    {
        if (cell.Type != GridType.Soil || !CheckEmpty(cellPos)) return;

        SetCellType(cellPos, GridType.Farmland);
        SetCellEmpty(cellPos, false);

        Farmland_Entity entity = (Farmland_Entity)EntityRuntimeFactory.Create(EntityRuntimeKind.Farmland);
        entity.Init(cellPos);
        PlaceTile(cellPos, farmlandTile, entity, 1, out _);
    }

    private void TryWaterTile(Vector3Int cellPos, bool hasDetail, DetailedCellData detailedData)
    {
        if (!hasDetail || detailedData.CheckEmpty()) return;
        if (!TryGetEntityOnCell(cellPos, out Farmland_Entity farmland)) return;

        farmland.Water();
        SyncDetailedStateWithRuntime(cellPos);
    }

    private void TryLogTile(Vector3Int cellPos, bool hasDetail, DetailedCellData detailedData)
    {
        if (!hasDetail || detailedData.CheckEmpty()) return;
        if (!TryGetEntityOnCell(cellPos, out Tree_Entity tree)) return;

        tree.Logging();
        SyncDetailedStateWithRuntime(cellPos);
    }
}
