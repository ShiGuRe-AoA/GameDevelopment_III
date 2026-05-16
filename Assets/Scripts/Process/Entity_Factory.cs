using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

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

        if (timeCount > recipe.TimeCost)
        {
            isFinish = true;
        }
        else isFinish = false;
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

public class Entity_Factory : ItemContainer_Base, IEntityRuntime, IMinuteUpdatable, ISaveableEntity, IInteractable, ITickUpdatable
{
    public ItemContainer resourcesContainer;
    public ItemContainer productsContainer;

    [SerializeField] private List<Recipe_SO> recipes;
    [SerializeField] private bool isLocked;
    [SerializeField] private Image progress_IMG;

    private bool producePause;

    private Production curProduction;
    private Recipe_SO curRecipe => curProduction.GetRecipe();

    public Transform processPanel;

    public int EntityId { get; private set; }

    public Vector3Int PivotPos { get; private set; }

    public void EntityInit(int entityId, Vector3Int pivotPos, WorldState worldState)
    {
        EntityId = entityId;
        PivotPos = pivotPos;
    }
    protected override void Awake()
    {
        base.Awake();


        resourcesContainer = containers[0];
        productsContainer = containers[1];

    }
    public void Start()
    {
        Vector3Int pivot = WorldState.Instance.WorldToCell(transform.position);
        WorldState.Instance.PlaceEntity(pivot, this as IEntityRuntime, 4, 3);
        RuntimeRegisterUtility.RegisterAll(this);
    }
    public void OnAwake()
    {

    }

    public void OnDestroy()
    {
        RuntimeRegisterUtility.UnregisterAll(this);
    }

    public void OnTickUpdate(float deltaTime)
    {
        if (producePause)
        {
            return;
        }
        //加工时间增加
        progress_IMG.fillAmount = GetProgress();

        if (curProduction != null)
        {
            curProduction.Update(deltaTime, out bool isFinish);

            if (isFinish)
            {
                Debug.Log("Process Complete!");

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

    public void OnMinuteUpdate()
    {
        
    }
    public void OnInteract()
    {
        if(isLocked) { return; }


        if (!processPanel.gameObject.activeInHierarchy)
        {
            processPanel.gameObject.SetActive(true);
        }
    }

    public InteractPhase OnInteractDetected()
    {
        return InteractPhase.OpenDoor;
    }
    public void Load(EntitySaveData data)
    {
        throw new System.NotImplementedException();
    }
    public EntitySaveData Save()
    {
        throw new System.NotImplementedException();
    }
    public void Unlock()
    {
        isLocked = false;
    }

    public void BeginProduce()
    {
        if(curProduction is not null) { return; }

        foreach(var recipe in recipes)
        {
            if (recipe.CheckRecipeItems(resourcesContainer.Items))
            {
                if (recipe.CheckResourceCountEnough(resourcesContainer.Items))
                {
                    curProduction = new Production(recipe);
                    return;
                }
                else { return; }
            }
        }
    }

    public bool GenerateProduction(Production curProduction)
    {
        var recipe = curProduction.GetRecipe();
        List<ItemStack> products = recipe.Products;

        foreach (var stack in products)
        {
            if(SlotController.Instance.TryAddItem(stack, productsContainer))
            {
                continue;
            }
            else
            {
                //中间存储货物逻辑待处理
                return false;
            }
        }

        return true;
    }
    void Update()
    {
        
    }
    public float GetProgress()
    {
        if(curProduction == null)
        {
            return 0f;
        }
        return curProduction.GetProgress();
    }
}
