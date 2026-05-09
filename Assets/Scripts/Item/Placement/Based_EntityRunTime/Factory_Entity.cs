using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class Production
{
    private float timeCount;
    private Recipe_SO recipe;
    public Production(Recipe_SO recipe)
    {
        this.recipe = recipe;
    }
    public void Update(float tickTime, out bool isFinish)
    {
        timeCount += TimeManager.Instance.TickToMinuteFloat(tickTime);

        if(timeCount > recipe.TimeCost)
        {
            isFinish = true;
        }
        isFinish = false;
    }
    public List<ItemStack> GetProducts()
    {
        return recipe.Products;
    }
    public float GetProgress()
    {
        return Mathf.Clamp01(timeCount / recipe.TimeCost);
    }
    public Recipe_SO GetRecipe()
    {
        return recipe;
    }
}
public class Factory_Entity : EntityRuntime, IInteractable
{
    private ItemContainer resourcesContainer;
    private ItemContainer productsContainer;

    private List<Recipe_SO> recipes;

    private bool producePause = false;

    private Production curProduction;
    private Recipe_SO curRecipe => curProduction?.GetRecipe();
    public void OnInteract()
    {
        throw new System.NotImplementedException();
    }

    public InteractPhase OnInteractDetected()
    {
        throw new System.NotImplementedException();
    }
    public void Init(
        int entityId, 
        Vector3Int pivotPos, 
        WorldState worldState, 
        List<Recipe_SO> recipes,
        ItemContainer resourcesContainer,
        ItemContainer productsContainer
        )
    {
        base.Init(entityId, pivotPos, worldState);

        this.resourcesContainer = resourcesContainer;
        this.productsContainer = productsContainer;

        this.recipes = recipes;

        producePause = false;
    }
    public override void OnTickUpdate(float deltaTime)
    {
        base.OnTickUpdate(deltaTime);

        if (producePause)
        {
            return;
        }
        //加工时间增加

        if (curProduction != null)
        {
            curProduction.Update(deltaTime, out bool isFinish);

            if (isFinish)
            {
                if (curRecipe.CheckResourceCountEnough(resourcesContainer.Items))
                {
                    curProduction = new Production(curRecipe);
                }
                else
                {
                    curProduction = null;
                }
            }
        }
    }
    public override void OnDateUpdate(ComplexTime curTime)
    {
        base.OnDateUpdate(curTime);

        if (producePause)
        {
            return;
        }
        //按跳过时间批量加工

        float restTime = TimeManager.Instance.TimeDistToNextDay(curTime);

    }
    void Start()
    {
        
    }

    void Update()
    {
        
    }
}
