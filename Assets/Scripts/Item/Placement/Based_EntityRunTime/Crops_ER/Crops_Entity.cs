using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

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
public class Crops_Entity : EntityRuntimeBase, IMinuteUpdatable,IDateUpdatable
{
    protected Genome cropGenome;
    protected List<TileBase> cropTiles = new();
    protected int tileCount => cropTiles.Count;

    public bool canHarvest { get; private set; }  //是否可以收获
    public ItemBase_SO Product { get; private set; }    //产物ID（对应的物品ID）

    public int harvestedCount { get; private set; } = 4; //产物产量


    protected float maxGrowthTime;  //生长时间（单位，游戏分钟）
    protected float currentGrowthTime;  //当前生长时间（单位，游戏分钟）
    protected bool needWater;  //是否需要浇水
    protected Farmland_Entity farmland_Entity; //关联的农田实体
    protected ItemBase_SO seedItem; //作物对应的物品ID

    public virtual void Init(ItemBase_SO seedItem, int entityId, Farmland_Entity farmland_Entity,List<TileBase> cropTiles ,Genome genome = null, bool needWater = true )
    {
        EntityId = entityId;
        cropGenome = (genome == null) ? new Genome() : genome;
        this.needWater = needWater;
        this.seedItem = seedItem;
        this.farmland_Entity = farmland_Entity;
        this.cropTiles = cropTiles;
        canHarvest = false;

        maxGrowthTime = 2f; //默认生长时间为120分钟（2小时），可根据作物类型调整

        foreach(var feature in seedItem.Features)
        {
            if(feature is Feature_Seed seedFeature)
            {
                Product = seedFeature.Product;
            }
        }
    }

    public void OnMinuteUpdate()
    {
        if (needWater)
        {
            if(farmland_Entity == null)
            {
                Debug.LogError($"Crops_Entity {EntityId} has no associated Farmland_Entity!");
            }
            if(farmland_Entity.WaterTimeLeft > 0)
            {
                Debug.Log($"CropsGrowing:{currentGrowthTime}=={maxGrowthTime}");
                currentGrowthTime += 1.0f;

                float growthPercnt = currentGrowthTime / maxGrowthTime;
                float tileDivision = 1.0f / tileCount;

                int tileIndex = Mathf.Min((int)(growthPercnt / tileDivision), tileCount - 1);

                WorldState.Instance.SwitchTile(farmland_Entity.GridPos, cropTiles[tileIndex],2);
            }
        }

        if(currentGrowthTime > maxGrowthTime && !canHarvest)
        {
            canHarvest = true;
        }
    }
    public void OnDateUpdate(ComplexTime curTime)
    {
        float TimeDeltaTime = TimeManager.Instance.TimeDistant(curTime.Hour, TimeManager.Instance.dayBeginHour);
        currentGrowthTime += TimeDeltaTime;

        if (currentGrowthTime > maxGrowthTime && !canHarvest)
        {
            canHarvest = true;
        }
    }
}
