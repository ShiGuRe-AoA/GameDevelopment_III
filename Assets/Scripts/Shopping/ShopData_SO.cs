using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Game/Shop/ShopItemDef")]
public class ShopItemDefSO : ScriptableObject
{
    public ItemBase_SO Item;
    public int Price;
    public int MaxStock;      // 最大库存
    public int RestockAmount; // 每次补货数量
    public bool Infinite;     // 是否无限
}