using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//TODO:待定基因遗传逻辑
public struct Gene
{

}
public class Genome
{
    public List<Gene> Genes;
    public Genome()
    {
        Genes = new();
    }
}
public class Crops_Entity : EntityRuntime
{
    protected Genome cropGenome;
    protected float maxGrowthTime;  //生长时间（单位，游戏分钟）
    protected float currentGrowthTime;  //当前生长时间（单位，游戏分钟）
    protected bool needWater;  //是否需要浇水
    protected Farmland_Entity farmland_Entity; //关联的农田实体
    protected bool canHarvest;  //是否可以收获

    public virtual void Init(int entityId, WorldState worldState, Farmland_Entity farmland_Entity, Genome genome = null, bool needWater = true )
    {
        EntityId = entityId;
        WorldState = worldState;
        cropGenome = (genome == null) ? new Genome() : genome;
        this.needWater = needWater;
        canHarvest = false;
    }
    public override void OnMinuteUpdate()
    {
        base.OnMinuteUpdate();
        currentGrowthTime += 1.0f;

        if(currentGrowthTime > maxGrowthTime && !canHarvest)
        {
            canHarvest = true;
        }
    }
    public override void OnDateUpdate(ComplexTime curTime)
    {
        base.OnDateUpdate(curTime);
        float TimeDeltaTime = TimeManager.Instance.TimeDistant(curTime.Hour, TimeManager.Instance.dayBeginHour);
        currentGrowthTime += TimeDeltaTime;

        if (currentGrowthTime > maxGrowthTime && !canHarvest)
        {
            canHarvest = true;
        }
    }
}
