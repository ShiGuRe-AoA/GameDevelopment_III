using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Tilemaps;

public interface IPlantable
{
    public float WaterTimeLeft { get; }
    public float FertTimeLeft { get; }
    public float FertRank { get;}
    public int CropInstanceId { get; } // 0 = none
    bool CanPlant(ItemBase_SO seedItem);
    void Plant(ItemBase_SO seedItem);


}
public class Farmland_Entity : EntityRuntimeBase, IPlantable, IInteractable, IMinuteUpdatable, IDateUpdatable
{
    public Vector3Int GridPos { get; private set; }

    // 状态
    public float WaterTimeLeft { get; private set; }
    public float FertTimeLeft { get; private set; }
    public float FertRank { get; private set; }

    // 作物（可选：用实例ID关联 CropRuntime）
    public int CropInstanceId { get; private set; } // 0 = none

    // 常量（可改为从SO读取）
    private const float MaxWaterTime = 1000f;   
    private const float MaxFertTime = 1000f;   


    // 初始化 ----------------------
    public void Init(Vector3Int pos)
    {
        GridPos = pos;
        CropInstanceId = 0;
    }

    // 更新 ----------------------

    public void OnMinuteUpdate()
    {
        if (WaterTimeLeft > 0)
            WaterTimeLeft = Mathf.Max(0, WaterTimeLeft - 1f);

        if (FertTimeLeft > 0)
            FertTimeLeft = Mathf.Max(0, FertTimeLeft - 1f);
    }

    public void OnDateUpdate(ComplexTime curTime)
    {
        // 可选：跨天处理（如完全干涸、作物死亡判定等）
        if (CropInstanceId != 0 && WaterTimeLeft <= 0)
        {
            // TODO: 标记作物受影响（通过注册表找到 CropRuntime）
        }
    }

    // 行为 ----------------------

    public void Water()
    {
        WaterTimeLeft = MaxWaterTime;
    }

    public void ApplyFertilizer(float rank)
    {
        FertRank = rank;
        FertTimeLeft = MaxFertTime;
    }

    public bool CanPlant(ItemBase_SO seedItem)
    {
        return CropInstanceId == 0;
    }

    public void Plant(ItemBase_SO seedItem)
    {
        if (!CanPlant(seedItem)) return;

        foreach (var feature in seedItem.Features)
        {
            if (feature is not Feature_Seed seedFeature)
                continue;

            if (EntityRuntimeFactory.Create(seedFeature.CropRuntimeKind) is not Crops_Entity newCropsRuntime)
            {
                Debug.LogError($"创建作物失败：{seedFeature.CropRuntimeKind} 不是 Crops_Entity 类型");
                return;
            }

            WorldState.Instance.PlaceTile(
                GridPos,
                seedFeature.SeedTiles[0],
                newCropsRuntime,
                2,
                out int entityId
            );

            newCropsRuntime.Init(seedItem, entityId, this, seedFeature.SeedTiles);
            CropInstanceId = entityId;

            return;
        }
    }
    public bool CanHarvest()
    {
        if (CropInstanceId <= 0) { return false; }

        IEntityRuntime cropRuntime = WorldState.Instance.GetEntity(CropInstanceId);
        if (cropRuntime is Crops_Entity cropsEntity && cropsEntity.canHarvest)
        {
            return true;
        }
        return false;
    }
    public bool TryHarvest()
    {
        if (CropInstanceId <= 0) { return false; }

        IEntityRuntime cropRuntime = WorldState.Instance.GetEntity(CropInstanceId);
        if (cropRuntime is Crops_Entity cropsEntity && cropsEntity.canHarvest)
        {
            int productID = cropsEntity.Product.ID_num;
            int spawnCount = cropsEntity.harvestedCount;

            WorldState.Instance.SpawnItem(GridPos, productID, spawnCount);
            WorldState.Instance.DestroyEntity(CropInstanceId);
            CropInstanceId = 0;
            WorldState.Instance.SwitchTile(GridPos, null,2); // 恢复为基础地块
            return true;
        }
        return false;
    }
    public void SwitchTile(TileBase income)
    {
        WorldState.Instance.SwitchTile(GridPos, income, 2);
    }
    public void ClearCrop()
    {
        CropInstanceId = 0;
    }

    public void ClearFarmland()
    {
        WaterTimeLeft = 0;
        FertTimeLeft = 0;
        FertRank = 0;
        CropInstanceId = 0;
    }
    public void OnInteract()
    {
        if(CropInstanceId <= 0) { return; }

        TryHarvest();
    }

    public InteractPhase OnInteractDetected()
    {
        if (CanHarvest())
        {
            return InteractPhase.Harvest;
        }
        return InteractPhase.None;
    }
}