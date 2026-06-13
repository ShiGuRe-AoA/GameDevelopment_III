using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 商店售卖列表配置。
/// 在 Assets 右键 → Create → Game/Shop/ShopInventory 创建。
/// </summary>
[CreateAssetMenu(menuName = "Game/Shop/ShopInventory")]
public class ShopInventorySO : ScriptableObject
{
    [Tooltip("商店名称（调试用）")]
    public string StoreName;

    [Tooltip("售卖物品列表（引用已有的 ShopItemDefSO）")]
    public List<ShopItemDefSO> Goods = new();
}
