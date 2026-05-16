using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public struct ItemStackVis
{
    public ItemBase_SO def;
    public int count;
}

[CreateAssetMenu(menuName = "Game/Item/Recipe_SO")]
public class Recipe_SO : ScriptableObject
{
    [HideInInspector] public List<ItemStack> Resources = new();
    [HideInInspector] public List<ItemStack> Products = new();

    public List<ItemStackVis> Resources_vis = new();
    public List<ItemStackVis> Products_vis = new();

    public float TimeCost = 1f;

#if UNITY_EDITOR
    private void OnValidate()
    {
        SyncRuntimeStacks();
    }
#endif

    private void SyncRuntimeStacks()
    {
        Resources.Clear();
        Products.Clear();

        foreach (var vis in Resources_vis)
        {
            if (vis.def == null)
                continue;

            Resources.Add(TransStack(vis));
        }

        foreach (var vis in Products_vis)
        {
            if (vis.def == null)
                continue;

            Products.Add(TransStack(vis));
        }
    }

    private ItemStack TransStack(ItemStackVis vis)
    {
        ItemStack newStack = new();
        newStack.itemId = vis.def.ID_num;
        newStack.count = vis.count;

        return newStack;
    }

    /// <summary>
    /// 只检查物品种类是否匹配，不检查数量。
    /// 适合用于判断是否显示产物虚影。
    /// </summary>
    public bool CheckRecipeItems(ItemStack[] income)
    {
        Dictionary<int, ItemStack> incomeMap = BuildItemMap(income);
        Dictionary<int, ItemStack> recipeMap = BuildItemMap(Resources);

        List<ItemStack> same = new();
        List<ItemStack> onlyIncome = new();
        List<ItemStack> onlyRecipe = new();

        foreach (var incomePair in incomeMap)
        {
            int itemID = incomePair.Key;

            if (recipeMap.ContainsKey(itemID))
            {
                same.Add(incomePair.Value);
            }
            else
            {
                onlyIncome.Add(incomePair.Value);
            }
        }

        foreach (var recipePair in recipeMap)
        {
            int itemID = recipePair.Key;

            if (!incomeMap.ContainsKey(itemID))
            {
                onlyRecipe.Add(recipePair.Value);
            }
        }

        return onlyIncome.Count <= 0 && onlyRecipe.Count <= 0;
    }

    /// <summary>
    /// 检查数量是否足够。
    /// 前提一般是：物品种类已经匹配。
    /// 适合用于每轮生产开始前判断是否继续生产。
    /// </summary>
    public bool CheckResourceCountEnough(ItemStack[] income)
    {
        Dictionary<int, int> incomeCountMap = BuildItemCountMap(income);
        Dictionary<int, int> recipeCountMap = BuildItemCountMap(Resources);

        foreach (var recipePair in recipeCountMap)
        {
            int itemID = recipePair.Key;
            int needCount = recipePair.Value;

            if (!incomeCountMap.TryGetValue(itemID, out int haveCount))
                return false;

            if (haveCount < needCount)
                return false;
        }

        return true;
    }

    private Dictionary<int, ItemStack> BuildItemMap(IEnumerable<ItemStack> items)
    {
        Dictionary<int, ItemStack> map = new();

        foreach (var item in items)
        {
            if (item.itemId == -1)
                continue;

            if (!map.ContainsKey(item.itemId))
            {
                map.Add(item.itemId, item);
            }
        }

        return map;
    }

    private Dictionary<int, int> BuildItemCountMap(IEnumerable<ItemStack> items)
    {
        Dictionary<int, int> map = new();

        foreach (var item in items)
        {
            if (item.itemId == -1)
                continue;

            if (map.ContainsKey(item.itemId))
            {
                map[item.itemId] += item.count;
            }
            else
            {
                map.Add(item.itemId, item.count);
            }
        }

        return map;
    }
}