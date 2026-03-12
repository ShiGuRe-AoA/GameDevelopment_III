using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public struct ResourcesStack
{
    public ItemBase_SO Item;
    public int Num;
}
[CreateAssetMenu(menuName = "Game/Item/RecipeSO")]
public class Recipe_SO : MonoBehaviour
{
    public List<ResourcesStack> Resources;
    public int TimeCost;

    public ItemBase_SO Result;
}
