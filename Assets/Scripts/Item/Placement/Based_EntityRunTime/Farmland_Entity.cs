using UnityEngine;

public interface IPlantable
{
    bool CanPlant(ItemBase_SO seedItem);
    void Plant(ItemBase_SO seedItem);
}
public class Farmland_Entity : EntityRuntime, IPlantable
{
    public Vector2Int Pos { get; private set; }

    // 状态
    public bool IsTilled { get; private set; }
    public float WaterTimeLeft { get; private set; }
    public float FertTimeLeft { get; private set; }
    public float FertRank { get; private set; }

    // 作物（可选：用实例ID关联 CropRuntime）
    public int CropInstanceId { get; private set; } // 0 = none

    // 常量（可改为从SO读取）
    private const float MaxWaterTime = 24f;   // 一天
    private const float MaxFertTime = 72f;   // 三天

    public void Setup(Vector2Int pos)
    {
        Pos = pos;
        IsTilled = true;
    }

    public override void OnMinuteUpdate()
    {
        if (!IsTilled) return;

        if (WaterTimeLeft > 0)
            WaterTimeLeft = Mathf.Max(0, WaterTimeLeft - 1f);

        if (FertTimeLeft > 0)
            FertTimeLeft = Mathf.Max(0, FertTimeLeft - 1f);
    }

    public override void OnDateUpdate()
    {
        // 可选：跨天处理（如完全干涸、作物死亡判定等）
        if (CropInstanceId != 0 && WaterTimeLeft <= 0)
        {
            // TODO: 标记作物受影响（通过注册表找到 CropRuntime）
        }
    }

    // 行为 ----------------------

    public void Hoe()
    {
        IsTilled = true;
    }

    public void Water()
    {
        if (!IsTilled) return;
        WaterTimeLeft = MaxWaterTime;
    }

    public void ApplyFertilizer(float rank)
    {
        if (!IsTilled) return;
        FertRank = rank;
        FertTimeLeft = MaxFertTime;
    }

    public bool CanPlant(ItemBase_SO seedItem)
    {
        return IsTilled && CropInstanceId == 0;
    }

    public void Plant(ItemBase_SO seedItem)
    {
        if (!CanPlant(seedItem)) return;
        //TODO: 创建 CropRuntime 实例并关联 CropInstanceId
        Debug.LogError("傻逼，还没写这块的Todo就着急测试?");
    }

    public void ClearCrop()
    {
        CropInstanceId = 0;
    }

    public void ClearFarmland()
    {
        IsTilled = false;
        WaterTimeLeft = 0;
        FertTimeLeft = 0;
        FertRank = 0;
        CropInstanceId = 0;
    }

    // 存档（示意）
    public override void Save()
    {
        // TODO: 写入 SaveData（位置、状态、CropInstanceId 等）
    }

    public override void Load()
    {
        // TODO: 从 SaveData 恢复
    }
}