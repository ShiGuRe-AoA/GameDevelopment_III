using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Game/Fishing/LootTable")]
public class FishingLootTable_SO : ScriptableObject
{
    public List<FishingLootEntry> entries = new();

    public ItemStack Roll()
    {
        float totalWeight = 0f;

        foreach (var entry in entries)
        {
            if (entry.item == null) continue;
            totalWeight += Mathf.Max(0f, entry.weight);
        }

        if (totalWeight <= 0f)
        {
            return default;
        }

        float randomValue = Random.Range(0f, totalWeight);
        float currentWeight = 0f;

        foreach (var entry in entries)
        {
            if (entry.item == null) continue;

            currentWeight += Mathf.Max(0f, entry.weight);

            if (randomValue <= currentWeight)
            {
                return new ItemStack
                {
                    itemId = entry.item.ID_num,
                    count = Random.Range(entry.minCount, entry.maxCount + 1)
                };
            }
        }

        return default;
    }
}

[System.Serializable]
public class FishingLootEntry
{
    public ItemBase_SO item;
    public int minCount = 1;
    public int maxCount = 1;
    public float weight = 1f;
}